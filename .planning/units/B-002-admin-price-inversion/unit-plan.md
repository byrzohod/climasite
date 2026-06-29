---
unit: B-002-admin-price-inversion
title: Fix admin product-list current-vs-compare price inversion
status: approved
severity: High
source: docs/project-plan/EXTERNAL_REVIEW_TRIAGE.md (B-002 = BUG-06 admin slice)
date: 2026-06-29
---

# Unit plan — B-002: admin product list inverts current vs compare-at price

## Context / Definition of Ready

The external multi-agent review's **only other real High** (alongside B-011, already
shipped in #86). On the admin **product list**, an on-sale product shows the **higher
compare-at price prominently as if it were the live price**, and the **real selling
price struck-through** as if it were the old price — the exact inverse of reality.
Admins make merchandising/discount decisions off this screen, so the inversion is a
correctness bug, not a cosmetic one.

Two independent defects compound:

1. **Backend** — `GetAdminProductsQuery.cs:112` maps `SalePrice = p.CompareAtPrice`
   **raw**, bypassing the `ProductPricing.GetSalePrice` contract that the public side
   (`GetProductsQueryHandler.cs:156`) correctly uses. Result: `SalePrice` is non-null
   even when the product is **not** on sale (e.g. `CompareAtPrice == BasePrice`, or
   legacy `CompareAtPrice < BasePrice`), painting a fake "sale".
2. **Frontend** — `admin-products.component.ts:177-178` renders `salePrice` (the
   original/higher compare-at) in the prominent `.sale-price` slot and `price`
   (BasePrice, the current selling price) in the struck-through `.old-price` slot —
   backwards versus the convention used by **every other** price renderer in the app
   (`product-card`, `product-detail`, `brand-detail`, `promotion-detail`,
   `similar-products`, `cart`, `mini-cart`, …): prominent = `basePrice`, struck =
   `salePrice`.

**Contract (single source of truth — `ProductPricing.cs`):** `BasePrice` is the current
selling price (always emphasized); the `SalePrice` DTO field carries the original
compare-at (struck-through when on sale, **null otherwise**); on sale ⇔
`CompareAtPrice > BasePrice`.

**Verified NOT affected:** admin product **detail/edit** path
(`GetAdminProductByIdQuery.cs:44`) maps to a distinct `CompareAtPrice` DTO field shown
in a labeled "Compare-at price" form input — correct, untouched. No other admin or
public surface mismaps (full repo sweep done).

## Scope / approach

- **Backend (1 line + 1 using):** `GetAdminProductsQuery.cs` →
  `SalePrice = ProductPricing.GetSalePrice(p.BasePrice, p.CompareAtPrice)`. Now null
  unless genuinely on sale; carries the original compare-at when on sale. Mirrors the
  public handler exactly.
- **Frontend (swap 2 interpolations):** `admin-products.component.ts` on-sale branch →
  prominent `.sale-price` shows `product.price` (current), struck `.old-price` shows
  `product.salePrice` (original). CSS class names are kept (now consistent with the
  rest of the app: `.sale-price` = current selling price). The `salePrice != null`
  guard is correct post-backend-fix (null ⇒ not on sale ⇒ single price rendered).

No DTO shape change, no migration, no new i18n strings, no new colors/z-index.

## Acceptance criteria

1. On-sale product (CompareAt 599.99 > Base 499.99): admin list shows **€499.99
   prominent** and **€599.99 struck-through**.
2. Not-on-sale product (CompareAt null): admin list shows the single BasePrice, no
   struck-through element.
3. "Fake sale" guard (CompareAt ≤ Base): `SalePrice == null` from the API — no struck
   element rendered (this is the case the raw mapping got wrong).
4. Works in light + dark and EN/BG/DE (no new strings; dual-currency pipe unchanged).
5. All existing admin-product tests still pass.

## Test / verification plan

- **Backend unit (`GetAdminProductsQueryHandlerTests`)** — extend `SeedProduct` with an
  optional `compareAtPrice`; add:
  - `Handle_OnSaleProduct_MapsBasePriceAndOriginalCompareAt` (Price==Base, SalePrice==CompareAt).
  - `Handle_NotOnSale_NullCompareAt_SalePriceIsNull`.
  - **Mutation-killer:** `Handle_CompareAtNotAboveBase_SalePriceIsNull` (CompareAt == Base
    and CompareAt < Base) — fails against the old raw `SalePrice = p.CompareAtPrice`.
- **Frontend spec (`admin-products.component.spec.ts`)** — correct the mock to a real
  on-sale shape (`price: 499.99, salePrice: 599.99`); add an inversion-guard test
  asserting the prominent `.sale-price` cell contains the **current** price and the
  struck `.old-price` contains the **original** — fails against the pre-swap template.
- **Mutation intent:** reverting either half independently must turn at least one new
  test red.
- **Runtime `/acceptance`:** drive the real admin product list with a seeded on-sale
  product; confirm current price prominent + original struck, in light & dark.
- Cross-vendor Codex council on the diff (standing rule; merchandising-correctness).

## Out of scope

- BUG-06's already-shipped public-side pricing (this is purely its admin slice).
- Renaming `.old-price` → `.original-price` for cross-app naming parity (cosmetic; not
  needed for correctness — tracked as a nicety, not done here).
- Inventory/stock display, the edit form's compare-at input (already correct).
