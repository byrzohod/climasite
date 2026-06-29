---
unit: B-002-admin-price-inversion
surface: ui (real running Angular app) + api (real running host) + automated unit/DOM evidence
result: PASS
date: 2026-06-29
commit: fix/b-002-admin-price-inversion tip (validated against the working diff; final squash tip on merge)
driver: exploratory runtime — drove the REAL running stack (API `dotnet run` on :5029 + freshly-rebuilt
  `ng serve` on :4200) against the shared-infra Postgres (db `climasite`). Created an on-sale fixture via the
  real admin API, then verified the admin product-list price column renders the CURRENT price prominent and the
  ORIGINAL (compare-at) struck-through — in light AND dark — via Playwright reading the live DOM + screenshots.
---

# Acceptance — B-002 (admin product list current-vs-compare price inversion)

## What this unit changed
Backend `GetAdminProductsQuery` now maps `SalePrice = ProductPricing.GetSalePrice(BasePrice, CompareAtPrice)`
(null unless `CompareAt > Base`) instead of echoing the raw `CompareAtPrice`; the admin product-list template
now renders `product.price` (current) in the prominent `.sale-price` slot and `product.salePrice` (original) in
the struck `.old-price` slot — matching the app-wide convention. No DTO/migration/i18n change.

## Environment
- API via `dotnet run` (real boot path, auto-migrate + seed) against shared-infra Postgres 17, db `climasite`.
- Frontend via a **freshly restarted** `ng serve` (the prior session's 22h-old dev server was serving a STALE
  bundle — see Scenario 0; the restart compiled current source).
- Auth: injected a freshly-minted admin access token (`admin@climasite.local`) into `localStorage`, the real
  session-hydration path (`loadUserFromToken` → `GET /api/auth/me`).

## Scenarios driven
| # | Scenario | Expected | Result |
|---|---|---|---|
| 0 | First drive hit a STALE dev-server bundle (22h `ng serve`) | catch staleness | ✅ caught — page showed the OLD inverted render (€599.99 prominent / €499.99 struck); restarted `ng serve`, re-ran |
| 1 | Create on-sale product via real admin API (`POST /api/admin/products`, base 499.99 / compareAt 599.99) | 201 | ✅ `201`, id `76e3d662…` |
| 2 | Real fixed handler: `GET /api/admin/products?search=B002-ONSALE` | `price=499.99`, `salePrice=599.99` (original > current) | ✅ exactly that; invariant `salePrice > price` holds |
| 3 | Control: a not-on-sale product in the same list | `salePrice` null/absent (no fake sale) | ✅ `salePrice` key absent on the sampled row |
| 4 | Admin list UI (light) — price column of the on-sale row | current prominent, original struck | ✅ `.sale-price`="€499.99 / 977.90 лв" (fontWeight 600, text-decoration none); `.old-price`="€599.99 / 1,173.48 лв" (text-decoration **line-through**) |
| 5 | Admin list UI (dark) | same, correct contrast | ✅ identical mapping; dark body bg `rgb(15,23,42)` |
| 6 | Console errors on the admin page | none from this change | ✅ none introduced (see minor below) |

Screenshots: `scratchpad/b002-admin-light.png`, `scratchpad/b002-admin-dark.png`.

## Automated evidence (complements the runtime drive)
- Backend: `GetAdminProductsQueryHandlerTests` 13/13. **Mutation-proven**: temporarily reverting the handler to
  the raw `SalePrice = p.CompareAtPrice` turned exactly the `Handle_CompareAtNotAboveBase_SalePriceIsNull`
  Theory (CompareAt==Base and CompareAt<Base) RED and nothing else — confirming it is the real guard (the
  on-sale/null Facts are vacuous against this mutation, by design, and are commented as characterizations).
- Frontend: full suite 1742/1742, incl. the new inversion-guard DOM test (renders the real template + dualPrice
  pipe, asserts prominent=current / struck=original; verified it fails on the pre-swap template).
- Cross-vendor Codex council (`gpt-5.5`@`xhigh`) on the diff: **0 findings**, correct + complete.
- 3-lens Claude verification: SEMANTICS=CONFIRMED_CORRECT, COMPLETENESS=confirmed (admin list was the only
  inverted shipped surface; admin detail/edit correct), TEST-MEANINGFULNESS Medium (vacuous headline test) →
  addressed by re-commenting + centering the Theory as the guard.

## Minor / tracked follow-ups (NOT blockers; pre-existing, off-critical-path)
1. **Missing `assets/images/no-image.svg`** — the admin list (and any imageless product) 404s on the placeholder
   image fallback. Pre-existing (present with the old bundle too), unrelated to B-002. Trivial fix (add the
   asset or fix the fallback). Tracked in the backlog.
2. **Orphan inverted-convention components** — `frequently-bought` + `product-variants` use the opposite sale
   convention (`salePrice < price`). Confirmed **unwired/dead code** (zero template usages) → latent, not
   shipped; would mis-render only if ever consumed against the standard DTOs. Tracked in the backlog.

## Verdict
**PASS** — zero blocker, zero major; the two minors are pre-existing, off-critical-path, and tracked. The admin
product list now shows the current selling price prominently and the original compare-at struck-through, in
light and dark, matching the rest of the app and the `ProductPricing` contract.
