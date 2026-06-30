---
unit: frontend-cleanups
surface: ui (real running app — asset) + automated unit/build evidence
result: PASS
date: 2026-06-30
commit: fix/frontend-cleanups tip (validated against the working diff; final squash tip on merge)
driver: served-asset check against the real running `ng serve`, plus the full frontend suite + lint.
---

# Acceptance — frontend follow-up cleanups

## Scenarios
| # | Item | Check | Result |
|---|---|---|---|
| 1 | FOUND-B002-noimage | `GET /assets/images/fallbacks/no-product-image.svg` (the repointed path) | ✅ 200 `image/svg+xml` |
| 2 | FOUND-B002-noimage | the old missing `assets/images/no-image.svg` is now unreferenced | ✅ 404, 0 code refs |
| 3 | FOUND-B002-orphans | `frequently-bought` + `product-variants` deleted; build + suite still green | ✅ no dangling import/route/barrel |
| 4 | FOUND-loaderr-race | stale failing load ignored after a newer success | ✅ unit-tested (cart + orders) |

## Automated evidence
- Full frontend suite **1724** green (the ~27 orphan-spec tests removed; +2 deterministic load-race tests:
  cart via `HttpTestingController.match`, orders via `Subject`s — both prove a stale failing load is ignored
  after a newer successful one). `ng lint` clean (0 errors; the 6 warnings are pre-existing, in untouched files).
- Cross-vendor Codex council on the diff (see PR).

## Verdict
**PASS** — imageless products now resolve a real placeholder (`no-product-image.svg`) instead of a broken
`no-image.svg`; the two dead inverted-convention components are gone; and `loadOrders`/`loadCart` no longer let
a stale request re-show an error or overwrite fresh data. Frontend only; no backend change; no migration.
