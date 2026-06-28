---
unit: SEARCH-01-fts
surface: api + ui-path (header search)
result: PASS
date: 2026-06-28
commit: feature/search-fts tip (validated at 2d0ad04 + non-behavioral cleanup; final squash tip on merge)
driver: exploratory runtime — real running API (ASPNETCORE_ENVIRONMENT=Development) against a fresh
  superuser Postgres 16 + Redis, seeded with the REAL DataSeeder demo catalog (not test data)
---

# Acceptance — SEARCH-01-fts (Postgres FTS)

## Environment
- API booted via `dotnet run` in **Development** (not the test harness) on :5029, against a throwaway
  `postgres:16-alpine` (superuser `climasite`) on :55432 + `redis:7-alpine` on :6379.
- This exercises the path the integration tests do NOT: a Development-environment boot, the migration
  applying to a DB that then gets the **real seeded demo catalog** (14 brands / 12 categories / 14 products
  + relations), and `CREATE EXTENSION unaccent/pg_trgm` under a real (non-Testcontainer) boot.

## Scenarios driven (real seeded data)
| # | Scenario | Result |
|---|---|---|
| 1 | **App boots in Development** — migration applies, extensions created, demo catalog seeds | ✅ clean boot, `Database seeding completed successfully`, no PostgresException |
| 2 | **Triggers populate vectors on real seed** — `SELECT search_vector` on seeded products | ✅ all sampled products have a non-empty `search_vector` |
| 3 | **Header path** `GET /api/products?searchTerm=cool` | ✅ 5 results, **ranked**: name-matches CoolLine / CoolMaster first, then description-matches ArcticBreeze / EcoHeat, then PortaCool (substring) |
| 4 | **Rich path** `GET /api/products/search?q=cool` | ✅ identical results to the header path (both unified through the service) |
| 5 | **Ranking name > description** (real data) | ✅ name-matches rank above products that only match `cool` in their description/tags |
| 6 | **Recall superset** — ArcticBreeze/EcoHeat (no `cool` in the name) still returned via description/tags | ✅ |
| 7 | **Multi-term AND** `searchTerm=portable cooler` → 0 | ✅ correct: `portable` matches 3 products, `cooler` matches 0, and 0 products contain both (verified in SQL) |
| 8 | **Single facet** `searchTerm=arctic` → 1 (ArcticBreeze) | ✅ |
| 9 | **No-match** `searchTerm=zzznotathing` → 0; junk/encoded term → 0 | ✅ no crash, empty |

## Notes / accepted
- The DataSeeder demo catalog seeds **no `product_translations`** (English-only), so the BG/DE + diacritic
  paths were NOT exercised at runtime here — they are covered by the FTS integration tests (cross-language
  multi-term + `gerat`→`Gerät`) against a translation-seeded DB.
- **Deploy note (OPS-08):** the migration runs `CREATE EXTENSION unaccent/pg_trgm`, which needs a DB role
  with the privilege. Fine under a superuser (local + most managed PG default roles allow these two), but
  the production role on Railway/managed PG must be able to `CREATE EXTENSION` — capture in the deploy runbook.
- Substring-fallback seq-scan is an accepted v1 scale limitation (documented in the unit-plan).

## Verdict
**PASS** — zero blockers, zero majors. FTS works end-to-end in a real Development boot against the real
seeded catalog: correct ranking, recall superset, AND-semantics, facets, and empty/no-match handling, with
trigger-maintained vectors. Automated coverage (17 real-Postgres integration tests + mutation gate) +
design/diff/re-council (all clean) complete the gate.
