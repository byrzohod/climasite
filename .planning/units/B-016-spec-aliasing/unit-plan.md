---
unit: B-016-spec-aliasing
title: Canonical HVAC spec resolver — fix recommendation fit-scoring + HVAC facets for real (display-key) products
status: approved            # owner picked "recalibrate to realistic" + "thorough (shared resolver)" 2026-07-01
owner: byrzohod
council: pending            # design council (Codex gpt-5.5@xhigh) BEFORE code; diff council BEFORE merge
acceptance: pending         # /acceptance on the REAL running app before /trunk-merge
---

# Unit plan — B-016: canonical HVAC spec resolver (+ realistic BTU calibration)

## Context / Definition of Ready
Backlog **B-016 (Medium, M)**: recommendation scoring reads canonical camelCase spec keys
(`btu`,`isInverter`,`minTemp`,`noiseLevel`,`recommendedRoomTypes`,`seer`,…) that the seeded/admin catalog
never emits — `DataSeeder` writes **display keys** (`"BTU"` int, `"Noise Level"`="22 dB", `"SEER Rating"`,
`"BTU Cooling"`/`"BTU Heating"`) and **omits `isInverter`/`minTemp`/`recommendedRoomTypes` entirely**
(inverter is a tag/feature; min-temp is buried in an `"Operating Range"` °F string). Council already
downgraded the review claim ("100% of prod broken") → seeding is Dev/Test-gated, but the **real defect**
stands: no canonical HVAC spec schema is enforced anywhere, so fit-scoring silently collapses to fallback
for any display-key product (unit tests stay green only because they seed canonical keys).

**Verified against live code (file:line):**
- Readers of canonical keys: `RecommendationScoringService.cs:41-44`, `GetRecommendationsQueryHandler.cs:91-93,154,159`
  (its OWN duplicate `ExtractIntSpec`/`ExtractBoolSpec`), `ProductRepository.cs:290` + `GetFilterOptionsQuery.cs:124`
  (`hvacSpecKeys` facets — already `OrdinalIgnoreCase` on the key, but NOT name-alias-aware).
- Writers of display keys: `DataSeeder.cs:247…472` (12 products). Write choke-point = `Product.SetSpecifications`
  (Core:174), called by the seeder + both admin & non-admin `CreateProduct`/`UpdateProduct` handlers.
- Values arrive as raw CLR types in-memory (seed/tests) but as `JsonElement` after EF JSONB round-trip — the
  resolver MUST handle both (existing `Extract*` helpers already do; centralise that).
- **Nested defect (confirmed):** the sizing model = `A=90 / B=110 / C=140 BTU/m²` (`RecommendationScoringService.cs:18-23`),
  **mirrored on the FE** (`home-wizard-state.service.ts:41-46`, `A=90/B=110/C=140`) for a live "required BTU" preview.
  Wizard area slider clamps **10–120 m²**, default **24 m²** (`home-wizard-state.service.ts:15,26`). At 90–140 BTU/m²
  a 24 m² room "needs" ~2640 BTU, so real 9000–24000 BTU units land at 3–9× → **past the 1.5× fall-off cutoff → fit=0
  for every real product** even after aliasing. The scale is ~2.3–2.8× below realistic (~215–320 BTU/m²).
- PDP spec table renders **raw keys with no label map** (`product-detail.component.ts` → `@for (spec of getSpecs())`)
  ⇒ we must NOT rename display keys to canonical (would show ugly `btu`/`isInverter` rows).

## Owner decisions (2026-07-01, batched)
1. **Recalibrate to realistic** BTU/m² (sync FE+BE) — recommendations must actually rank real units for real rooms.
   Exact table proposed below for sign-off.
2. **Thorough (shared resolver)** — one resolver fixes recommendations, HVAC filter facets, AND the admin/seed
   write boundary. Full test coverage.

## Design

### 1. `HvacSpecResolver` — single source of truth (Application, `Features/Products/Specifications/`)
Pure, dependency-free static utility. Canonical key → alias set + typed value parsers. Handles CLR primitives
AND `JsonElement`. Deterministic key normalisation: `Normalize(s)` = lower-case, drop all non-alphanumerics
(`"BTU Rating"`, `"btu_rating"`, `"BTU-Rating"` all collapse to the same token). Alias table (canonical → raw
spellings, normalised at build):
- `btu` ← btu, "BTU Rating", "BTU Cooling", "Cooling BTU", "Cooling Capacity", btu/h, btus
- `noiseLevel` ← noiseLevel, "Noise Level", noise, "Sound Level"
- `isInverter` ← isInverter, inverter, "Inverter Technology"  *(bool)*
- `minTemp` ← minTemp, "Min Temp", "Minimum Temperature", "Min Operating Temp"  *(int °C — deliberately NOT
  "Operating Range": that's an ambiguous °F range string; parsing it risks a °F/°C unit bug)*
- `recommendedRoomTypes` ← recommendedRoomTypes, "Recommended Room Types", "Room Types"  *(list)*
- facet keys: `seer`←seer,"SEER Rating"; `eer`; `hspf`; `energyRating`←"Energy Rating","Energy Class";
  `voltage`; `refrigerantType`←refrigerant,"Refrigerant Type"; `fuelType`←fuel; `afue`

API: `int? GetInt(specs, canonical)` (parses `"22 dB"`,`"12000 BTU"`,`"SEER 21"`, JsonElement number/string) ·
`bool? GetBool(specs, canonical)` (bool / true|yes|1) · `List<string> GetStringList(specs, canonical)`
(List<string> / string[] / JsonElement array / CSV) · `string? GetString(specs, canonical)` ·
`IReadOnlyCollection<string> CanonicalKeys` · `bool IsCanonicalKey(string rawKey)` (for DTO stripping / facet grouping).

**Wiring (read side):** `RecommendationScoringService` and `GetRecommendationsQueryHandler` drop their private
`Extract*` helpers and call the resolver (`GetInt(...) ?? 0`, `GetBool(...) ?? false`, `GetStringList(...)`).
`ProductRepository.ExtractSpecificationOptions` groups facets under the **canonical** key (so `"SEER Rating"` and
`seer` merge) and normalises the value via the resolver (so `"12000"`/`"12000 BTU"` don't split into two buckets).

### 2. Seed data (`DataSeeder.cs`) — additive canonical machine fields
For the AC / heat-pump / cooling products, ADD the machine fields the resolver can't derive from display keys —
`isInverter` (bool), `minTemp` (int °C), `recommendedRoomTypes` (list) — plus a **human display row** for the
useful ones (e.g. `"Min. Operating Temp": "-15°C"`) so the PDP still reads nicely. `btu`/`noiseLevel`/`seer`
already alias from the existing `"BTU"`/`"Noise Level"`/`"SEER Rating"` display keys (no dup needed; keep them).
Give each product realistic `btu` (already 9000–24000) and a spread so any wizard setting finds a good match.

### 3. Write-boundary enrichment (non-destructive) — `SpecificationNormalizer.Enrich(specs)`
Called from the seeder's `CreateProduct` and both admin/non-admin `CreateProduct`/`UpdateProduct` handlers
(NOT in the Core entity — keep the entity dumb). For each recognised numeric/bool alias present, ensure the
**canonical** key exists with the parsed value, **without removing** the admin-typed display key. This
materialises canonical values in the DB (boundary enforcement) while the resolver remains the safety net for
any un-enriched/legacy row. Does NOT invent `isInverter`/`minTemp`/`roomTypes` (no display source) — those come
from seed data or an admin who types them.

### 4. PDP display — strip canonical machine keys from the PUBLIC ProductDto
`GetProductBySlugQuery` maps `Specifications = HvacSpecResolver.StripCanonicalKeys(product.Specifications)`
so the public PDP shows only human display rows (never a raw `btu`/`isInverter`/`minTemp`). Admin DTO keeps the
full map (admins may inspect/edit canonical keys). Net: customers see `"BTU": 12000`, `"Min. Operating Temp": "-15°C"`;
never `isInverter: true`.

### 5. BTU calibration — PROPOSED table (owner sign-off in this plan)
Keep the "cold zone needs the biggest unit" ordering (heating-inclusive load), scale to realistic BTU/m²:

| Zone | Current | **Proposed** | Rationale |
|---|---|---|---|
| A (coastal/warm) | 90 | **200** | ~20 BTU/sqft ≈ 215/m² baseline, milder heating need |
| B (temperate/default) | 110 | **250** | moderate cooling + heating |
| C (alpine/cold) | 140 | **320** | heating-dominated → largest capacity |

Maps real units to real rooms (B=250): 9000→36 m², 12000→48 m², 18000→72 m², 24000→96 m² — all inside the
10–120 m² slider. **Default wizard area 24 → 35 m²** (a modest living room) so the default view (35 m² · B ·
required 8750) shows the 9000 BTU unit as a near-perfect (0.9×) match instead of a fall-off-cutoff 0. Sync all
three mirror sites: BE `ZoneMultipliers` (scoring) + the handler's local `multipliers` dict
(`GetRecommendationsQueryHandler.cs:135`) + FE `home-wizard-state.service.ts:41-46,15`. Fit curve (perfect
0.9–1.1×, zero at 0.5×/1.5×) unchanged.

## Scope / approach (ships as ONE PR — BE + FE + seed; NO EF migration — JSONB is schemaless)
- Add `HvacSpecResolver` + `SpecificationNormalizer` (Application).
- Rewire scoring service, recommendation handler, facet reader to the resolver.
- Recalibrate multipliers (BE ×2 sites + FE ×1) + FE default area.
- Enrich seed data with canonical machine fields + display rows; enrich admin/non-admin create/update.
- Strip canonical keys from the public ProductDto.

## Acceptance criteria
- A product built the way `DataSeeder` builds them (display keys + added machine fields) scores a **meaningful,
  discriminating** fit (NOT the flat fallback) — proven by a DataSeeder-seeded scoring test (the explicit B-016 ask).
- HVAC filter facets appear for the seeded catalog (`btu`/`seer`/`energyRating`/`refrigerantType` chips render),
  with `"SEER Rating"` and `seer` merged into one bucket.
- Recommendation cards show real BTU / inverter / noise for seeded products.
- FE "required BTU" preview matches the recalibrated BE (no FE/BE drift).
- PDP spec table shows human rows only (no raw `btu`/`isInverter`/`minTemp`).
- Works light+dark, EN/BG/DE; six green CI checks; council-clean; `/acceptance` PASS committed at the merged tip.

## Test / verification plan
- **Resolver unit matrix** (Application): case/space/punct aliasing; display→canonical (`"BTU Rating"`→btu,
  `"Noise Level"`→noiseLevel, `"SEER Rating"`→seer); unit-stripping (`"22 dB"`→22, `"12000 BTU"`→12000);
  JsonElement number/string/bool/array; missing→null; bad→null; NOT "Operating Range"→minTemp.
- **DataSeeder-seeded scoring test** (the B-016 ask): construct a product via the seeder's `CreateProduct`-shape
  (display keys + machine fields) → assert non-fallback, size-discriminating score across areas.
- **Facet alias test**: `"SEER Rating":"21"` + `"seer":21` collapse to one `seer` facet; `"BTU Cooling"`→btu bucket.
- **Write-normalization test**: admin create with `{"BTU":12000}` ⇒ stored specs also contain canonical `btu:12000`,
  display `"BTU"` retained; resolver reads 12000.
- **PDP-strip test**: public ProductDto omits `btu`/`isInverter`; admin DTO keeps them.
- **Recalibration**: update existing `RecommendationScoringServiceTests` + `RecommendationsControllerTests` to the
  new multipliers (they encode 90/110/140 + synthetic btu 2640); re-derive expected fits.
- **FE**: update `home-wizard-state.service` spec for new multipliers/default; recommendations render real specs.
- **Mutation / break-probe**: revert one alias (e.g. remove `"BTU Rating"`→btu) ⇒ the seeded scoring test fails;
  restore. Prove the tests are load-bearing.
- **CI** (six required checks) = evidence of record. Cross-vendor Codex council on the diff.

## Out of scope (file as follow-ups if surfaced)
- Fit-curve redesign (undersize/oversize asymmetry, smallest-available-unit handling for tiny rooms).
- A structured typed HVAC profile column / migration off the free-form JSONB map.
- FE i18n label map for spec-table keys.
- Deriving `isInverter`/`minTemp` from tags/features/operating-range strings (rejected: fragile, unit-ambiguous).

---

## Post-council revisions (design council = Codex gpt-5.5@xhigh, 2026-07-01 — 3 High / 4 Med, ALL resolved by descope)
The council verified against live code and surfaced issues that make this unit **simpler + more correct**.
Revised decisions (these SUPERSEDE the matching sections above):

- **[High] DROP write-boundary enrichment (§3 `SpecificationNormalizer`).** The admin UI can't even send specs
  (`admin-products.component.ts` drops `detail.specifications` on hydrate and never posts them; the exposed admin
  write API dispatches `Features/Products/Commands`, not `Features/Admin/...`). And once every machine read goes
  through the resolver, materialising canonical keys in the DB adds nothing but risk (stale `btu` if `"BTU"` is
  edited but the derived key isn't; double-counting). **The shared resolver AT EVERY READ BOUNDARY is the
  canonical-schema enforcement** — no data duplication, no drift, single source of truth. This resolves the
  staleness + wrong-command-path Highs. Known pre-existing limitation (separate follow-up): the admin UI has no
  spec editor, so admin-created products carry no HVAC specs → fallback recommendations (unchanged by this unit).

- **[High] Facets = BACKEND EXTRACTION CORRECTNESS ONLY (no selectable chips, no apply-path).** There is no
  server-side spec-value filtering and the product-list sidebar renders no spec facets
  (`ProductsController.GetProducts`/`GetProductsQuery` take no spec params; `product.service.ts` serialises none).
  So we fix `ExtractSpecificationOptions` (BOTH copies: `ProductRepository.cs:287` + `GetFilterOptionsQuery.cs:128`)
  to group by the **canonical** key via the resolver **with per-(product,canonical,value) dedupe** (a product
  holding both `"SEER Rating"` and `seer` counts ONCE), and stop there. Building selectable HVAC facet chips +
  the end-to-end filter apply-path is a **separate UX unit** (explicitly out of scope; removed from acceptance).

- **[Med] Per-key TYPED parser, not "first number wins."** Resolver is data-driven: each canonical key has a
  descriptor `{ orderedAliases[], kind: Int|Decimal|Bool|String|StringList, unitReject?[] }`.
  - `btu` Int, **ordered** aliases so cooling/generic BTU beats heating: `btu,"BTU","BTU Rating","Cooling BTU",
    "BTU Cooling","Cooling Capacity"` … **`"BTU Heating"` LAST** (heat pumps expose both — cooling load drives the
    sizing fit; min-temp handles cold-climate suitability separately).
  - `noiseLevel` Int(dB), **reject values containing `"sone"`** → null (the ERV's `"Noise Level":"0.8 sones"` must
    NOT become 1 dB).
  - `minTemp` Int °C (negatives ok), aliases only explicit min-temp — **never `"Operating Range"`** (°F range string).
  - `seer/eer/hspf/afue` Decimal-safe (`"HSPF":10.5`); `energyRating/voltage/refrigerantType/fuelType` String
    (never numeric — `"R-32"` stays `"R-32"`). Invariant culture throughout.

- **[Med] Tests must use REAL BTUs + a display-only path.** Update the 90/110/140-era fixtures:
  `RecommendationScoringServiceTests` (2640/3640), `RecommendationsControllerTests` (2640/3640), and the E2E
  `HomeV3Tests` (2600 btu + old default slider). Add the **load-bearing** test the current E2E factory masks (it
  writes canonical keys): a product with **ONLY display keys** → resolver recovers `btu`/`noiseLevel` after an EF
  **JSONB round-trip** → meaningful, size-discriminating score. Plus an API-create-with-display-only-keys
  integration test.

- **[revised §3/§4] PDP strip = exactly the 3 no-display-equivalent machine keys** `{isInverter, minTemp,
  recommendedRoomTypes}` (case-insensitive exact match), since `Normalize("BTU")==Normalize("btu")` — stripping by
  normalisation would also remove the customer-facing `"BTU"` row. We add ONLY these 3 to seed (btu/noiseLevel/seer
  already alias from existing display keys), so no display collision. **No new human display rows, no new i18n**
  (existing features/specs already convey inverter/operating-range to customers) → dodges the i18n-label Low.

- **[Low] i18n FAQ:** EN correctly says "~20 BTU **per square foot**" (≈215/m², consistent with recalibration).
  Check BG/DE `faq...sizing.answer` — if they mistranslate to "per square **meter**", fix (pre-existing bug).

**Net:** no `SpecificationNormalizer`, no admin-command edits, no FE facet UI, no migration. Deliverables =
resolver + rewire 3 read sites + recalibrate 3 mirror sites + additive seed machine fields + public-DTO strip +
tests. Design is council-clean after descope; the harder bar is the **diff council** before merge.
