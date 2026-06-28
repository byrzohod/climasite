---
unit: SEARCH-01-fts
type: NORMAL
status: approved
created: 2026-06-28
plan_status: approved
design: ../../design/DESIGN.md
council: design-council DONE (Codex gpt-5.5@xhigh + Claude plan-critic, 2026-06-28) — both returned
  BROKEN-as-first-drafted; all High/Medium folded into THIS revision. Diff-council required before merge.
---

# Unit plan — SEARCH-01-fts: public product search ILIKE → Postgres full-text search

## Context / Definition of Ready
Public product search today is `LOWER(col) LIKE '%term%'` with **no index**. Two public paths exist, and
the one users actually hit is the **weaker** one (verified 2026-06-28):
- **Header box** → `/products?search=` → `getProducts()` → **`GET /api/products?searchTerm=`** →
  **`GetProductsQueryHandler`** (matches name/description/brand/model ONLY — no short_desc/tags/translations).
- **`SearchProductsQueryHandler`** (`GET /api/products/search`) is richer (adds short_desc/tags/translations
  + crude rank) but has **no public UI consumer** (admin services only).

Stack: .NET 10, EF Core 10.0.1, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0, PostgreSQL 16, snake_case.
**OPS-08: nothing deployed → no live data** → v1 index build is in-transaction (no CONCURRENTLY).

### Schema-build mechanism (load-bearing — verified, was misdiagnosed in draft 1)
Integration tests build their schema via **`MigrateAsync()` at app startup**, NOT `EnsureCreatedAsync()`.
`TestWebApplicationFactory` runs the real `Program.cs:34` → `SeedDatabaseAsync` → `DataSeeder.SeedAsync`
→ `await _context.Database.MigrateAsync()` (`DataSeeder.cs:52`), unconditionally, triggered by
`factory.CreateClient()` in the `IntegrationTestBase` **constructor** — BEFORE `InitializeAsync`'s
`EnsureCreatedAsync()` (which is then a no-op). **Therefore: ALL FTS DDL lives in the migration `Up()`;
that is the single code path that builds both prod and the test schema.** No test-fixture bootstrap is
needed. Consequence to respect: a broken FTS migration throws at `MigrateAsync` → host boot fails → the
ENTIRE integration collection errors (not just FTS tests) — so the migration must be correct + idempotent.

## Ratified decisions (owner, 2026-06-28) — unchanged by the council
- **D1 — Language:** ONE config `climasite_search` = `simple` + `unaccent` (baked into the config MAPPING,
  never `unaccent(col)`), used identically by the stored vector AND `plainto_tsquery`.
- **D2 — Recall: a TRUE superset of today** (see "Recall model" — restored after the council showed the
  draft's superset claim was false).
- **D3 — Unify both public paths** via one `IProductSearchService`.
- **D4 (agent):** admin search out of scope. **D5 (agent):** exclude `meta_*` from the vector.

## Design change from the council (within implementation latitude; the 3 ratified decisions stand)
The draft's **two generated tsvector columns** are replaced by **ONE trigger-maintained denormalized
`products.search_vector`** (product fields + tags + ALL translations in one doc). Forced + improved by:
- `array_to_string(tags,' ')` is **STABLE not IMMUTABLE** → illegal in a STORED generated column (Codex/M1).
  A trigger function has no immutability restriction.
- A generated column **cannot reference another table** → translations would need a 2nd column + a query-time
  join/correlate that produced invalid SQL (unbound alias) and 3×-per-product fan-out breaking COUNT/paging
  (plan-critic H1) and broke cross-table multi-term AND (H2). One denormalized doc deletes all three.

## Recall model (D2 — a provable superset of today's matches)
Match = **FTS branch OR substring branch** (per product `p`):
- **FTS branch:** `p.search_vector @@ plainto_tsquery('climasite_search', @rawQuery)`.
  `plainto_tsquery` (NOT `websearch_to_tsquery`) → all terms ANDed as lexemes, and it can't misparse model
  codes like `MSZ-AP25` (where websearch's `-` = negation). Cross-field/cross-language AND works because the
  doc is denormalized.
- **Substring branch (restores today's per-term substring recall across EVERY field today searches):** ALL
  terms must substring-match somewhere, expressed in static SQL over a `text[]` of pre-escaped terms:
  `NOT EXISTS (SELECT 1 FROM unnest(@terms) term WHERE NOT ( p.name ILIKE '%'||term||'%' ESCAPE '\' OR
  p.brand ILIKE … OR p.model ILIKE … OR p.sku ILIKE … OR p.short_description ILIKE … OR p.description ILIKE …
  OR EXISTS(SELECT 1 FROM unnest(p.tags) tg WHERE tg ILIKE …) OR EXISTS(SELECT 1 FROM product_translations t
  WHERE t.product_id=p.id AND (t.name ILIKE … OR t.short_description ILIKE … OR t.description ILIKE …)) ))`.
  Terms are LIKE-escaped (`% _ \`) in C# before being passed (Codex L1/M-sec, plan-critic L1) so a query of
  `%` can't match the whole catalog.

## Ranking (replaces the crude Name=10/Brand=5)
`score = ts_rank_cd('{0.1,0.2,0.4,1.0}', p.search_vector, q)  -- {D,C,B,A}; A=name highest (council-confirmed)
       + CASE WHEN lower(p.sku)=lower(@rawQuery) THEN 2.0 WHEN p.sku ILIKE @skuPrefix ESCAPE '\' THEN 1.0 ELSE 0 END`
Substring-only matches get `ts_rank_cd = 0` → they appear, after FTS matches, ordered by `p.name`.
Vector setweights (in the trigger): **A** = name + translation names · **B** = brand + model ·
**C** = `array_to_string(tags,' ')` · **D** = short_description + description + translation short/desc.

## One query — match + facets + rank + page + total (no COUNT drift)
Single parameterized `FromSqlInterpolated` against a **keyless** entity `ProductSearchHit { Guid Id;
double Score; long TotalCount }` (registered `HasNoKey()`; `SqlQuery<(Guid,double)>` is scalar-only — M3).
`COUNT(*) OVER() AS total_count` returns the total alongside the page in ONE statement → no separate-COUNT
predicate drift (M4). Facets are **null-guarded parameters**, never string-built (M4):
`AND (@catIds IS NULL OR p.category_id = ANY(@catIds))` · `(@brands IS NULL OR lower(p.brand)=ANY(@brands))`
· price bounds · `(NOT @inStock OR EXISTS(variants stock>0))` · `(NOT @onSale OR compare_at_price>base_price)`
· `(NOT @featured OR p.is_featured)`. `ORDER BY` = relevance (`score DESC, name`) when a term is present and
no explicit user sort, else the requested `name|price|newest`. `OFFSET/LIMIT` for paging.
Then EF hydrates entities by id (Includes Images/Variants/Translations), **re-orders in memory by the SQL id
order**, projects `ProductBriefDto` via the unchanged `Product.GetTranslatedContent(lang)`.

## Handlers (D3 — complete filter object; preserves ALL existing filters/sorts — Codex H5/plan-critic)
- **New** `IProductSearchService.SearchAsync(ProductSearchFilter)` → `(IReadOnlyList<Guid> orderedIds, int
  total)`. `ProductSearchFilter` carries the **complete** surface: RawQuery, EscapedTerms, CategoryIds
  (resolved), Brands, MinPrice, MaxPrice, **InStock, OnSale, IsFeatured, SortBy, SortDescending**, Page, Size.
  Impl in Infrastructure (uses the concrete `ApplicationDbContext` for the keyless type).
- `SearchProductsQueryHandler`: resolve category-descendants + normalized brands (as today), build the filter,
  call the service; keep hydration + DTO + the `ICacheableQuery` cache (untouched).
- `GetProductsQueryHandler`: when `SearchTerm` is **non-blank**, route through the service with its full
  filter (CategoryId→1-elem list, Brand, price, InStock, OnSale, IsFeatured, SortBy/Desc). When blank/absent,
  the existing EF path is **unchanged** — must keep "blank searchTerm → all products" and NOT start 400-ing
  like `/search` does (plan-critic L2).

## Migration (one file, `src/ClimaSite.Infrastructure/Data/Migrations/<ts>_AddProductFullTextSearch.cs`)
DDL is **inlined literally** in the migration (frozen — never reference a mutable shared const; Codex M10).
Model: `HasPostgresExtension("unaccent")`, `HasPostgresExtension("pg_trgm")`; shadow `search_vector` property
(`Property<NpgsqlTsVector>("search_vector").HasColumnType("tsvector")` — plain column, NOT computed); GIN
index on it; pg_trgm GIN indexes on `name, brand, sku, model, short_description, description`; register the
keyless `ProductSearchHit`.
`Up()` order: (1) `CREATE EXTENSION IF NOT EXISTS unaccent; pg_trgm;` (2) idempotent `climasite_search`
config (`DO $$ … IF NOT EXISTS(pg_ts_config) … $$`); (3) `ADD COLUMN search_vector tsvector`; (4) the
`fn_compute_product_tsv(...)` function + the `products` BEFORE INSERT/UPDATE trigger + the
`product_translations` AFTER INSERT/UPDATE/DELETE trigger (`UPDATE products SET search_vector=…WHERE id=…`);
(5) backfill existing rows (`UPDATE products SET search_vector = fn_compute_product_tsv(...)`); (6) GIN +
trgm `CREATE INDEX` (in-transaction, non-concurrent — safe, no prod data).
`Down()` order is **load-bearing** (M2): drop triggers → functions → indexes → `search_vector` column →
`DROP TEXT SEARCH CONFIGURATION IF EXISTS climasite_search`; leave extensions.
**ModelSnapshot regen** via `dotnet ef migrations add` (keeps the model consistent; harmless since Migrate is
the real schema path).

## Acceptance criteria
- [ ] Header search (`/products?search=`) AND `/api/products/search` both FTS-ranked via the shared service;
      all `GetProductsQuery` filters/sorts preserved.
- [ ] SKU-exact ranks first; SKU/model substring (`AP2`→`MSZ-AP25`) matches; tag & translation tokens match.
- [ ] Multilang: a token only in a BG/DE translation returns the product; `unaccent` folds `Gerät`=`Gerat`.
- [ ] **Recall is a provable superset** (the substring branch keeps every pre-existing match, incl.
      substring-in-description, tag-substring, and cross-language multi-term).
- [ ] The migration applies cleanly via `MigrateAsync` (so the whole integration collection boots).
- [ ] Parameterized: an injection-y / wildcard term (`%`, `a' OR '1'='1`) is literal → no catalog-wide match.
- [ ] All 6 CI checks green; diff-council clean; committed `/acceptance` PASS at merged tip.

## Test / verification plan
Existing E2E (`ProductBrowsingTests`, `ProductFilteringTests`, `ProductCatalogTests`, `LanguageTests`) assert
only **presence/empty-state** (NO ranking/recall — plan-critic H3), so they stay green but are NOT the safety
net. New `tests/ClimaSite.Api.Tests/Controllers/ProductSearchFtsTests.cs` (real Postgres via the Testcontainer
the suite already boots) asserts the REAL contract:
1. **Ranking:** term-in-name ranks above term-only-in-description.
2. **SKU-exact boost** ranks that product first.
3. **Code substring (trgm):** `AP2` matches sku/model `MSZ-AP25`.
4. **Recall — substring-in-description:** today's `"condition"`→`"air conditioner"` still returns it.
5. **Recall — tag substring:** a tag substring still returns the product.
6. **Recall — cross-language multi-term:** `mitsubishi <bg-only-token>` (one term base, one term BG translation
   only) returns the product (denormalized doc).
7. **Multi-term AND:** two terms both required.
8. **Case-insensitivity** `TEST`==`test`. 9. **Diacritics** `Gerat`==`Gerät`. 10. **Facet+search+pagination
   total** correct (window COUNT). 11. **Empty/blank + punctuation-only** `"!!!"` → no crash, ILIKE path.
12. **Injection/wildcard** `%`/`a' OR '1'='1'` literal → not whole-catalog.
- **Mutation gate:** flip the weight array to `{1.0,0.4,0.2,0.1}` (rank reversal) → presence tests still
  pass but **test #1 MUST fail** (proves the suite checks ordering, not just presence).
- Plus a manual `/acceptance` pass against the running app; CI is the evidence of record.

## Rollout
Single PR is acceptable (OPS-08 = nothing deployed; each commit green). Expand-contract still governs commit
ordering: (1) migration+trigger+indexes; (2) `IProductSearchService` + rewire both handlers (+ FTS tests);
(3) remove dead ILIKE term-loops. Optional `Search:FtsEnabled` flag deferred — the recall superset makes the
cutover safe (only ordering changes), so a flag adds little pre-launch.

## Files in play
- `src/ClimaSite.Infrastructure/Data/Migrations/<ts>_AddProductFullTextSearch.cs` (+ ModelSnapshot)
- `src/ClimaSite.Infrastructure/Data/Configurations/ProductConfiguration.cs`, `ProductTranslationConfiguration.cs`
- `src/ClimaSite.Infrastructure/Data/ApplicationDbContext.cs` (extensions + keyless `ProductSearchHit`)
- `src/ClimaSite.Application/Common/Interfaces/IProductSearchService.cs` + `Models/ProductSearchFilter.cs` [new]
- `src/ClimaSite.Infrastructure/Search/ProductSearchService.cs` [new]
- `src/ClimaSite.Application/Features/Products/Queries/SearchProductsQuery.cs`, `GetProductsQueryHandler.cs`
- `tests/ClimaSite.Api.Tests/Controllers/ProductSearchFtsTests.cs` [new]

## Out of scope
Admin search (D4), autocomplete redesign, search analytics, Meilisearch, the future CONCURRENTLY index
migration (only once a live prod DB with data exists).

## DoD gates
Green CI (6 checks) · diff-council clean on the migration + raw SQL (fix every High/Medium, re-council) ·
committed `/acceptance` PASS at merged tip · update CHANGELOG / STATE.md / PROJECT_STATUS / BACKLOG / KB
(new FTS ADR) in the PR.
