---
unit: B-018-B-020-load-error-states
title: Real error+retry states for account-orders & cart (instead of fake "empty")
status: approved
severity: Medium (×2, frontend UX)
source: docs/project-plan/EXTERNAL_REVIEW_TRIAGE.md (B-018=UX-05, B-020=NEW-UX)
date: 2026-06-29
---

# Unit plan — B-018 / B-020: load-failure error+retry states

Two related frontend UX defects: a **load failure is silently rendered as an empty state**, so the user is
told "you have no orders" / "your cart is empty" when the request actually failed. Fix: distinguish failure
from a true zero-count, show a real error message + Retry.

## B-018 — account orders (UX-05)
`orders.component.ts:836` — `loadOrders()`'s error handler sets `paginatedOrders` to an **empty result**, so
`orders().length === 0` → the "no orders" empty-state renders on a network/5xx failure (`:151`).
**Fix:**
- Add a `loadError = signal(false)`; reset to `false` at the start of `loadOrders()`, set `true` in the error
  handler (and stop clobbering `paginatedOrders` with an empty result).
- Template: add an `@else if (loadError())` error+retry branch **between** the loading and the
  `orders().length === 0` branches; guard the results-info with `!loadError()`. Retry calls `loadOrders()`.
- i18n: `account.orders.errors.loadListFailed` (en/bg/de) — the existing `loadFailed` is the order-**detail**
  singular message.

## B-020 — cart (NEW-UX; the existing `cart-error` branch is unreachable)
`cart.service.ts:107` — `loadCart()`'s `catchError` sets `createEmptyCart()` and never sets `_error`, so a
failure renders as an empty cart and the existing `@else if (error())` branch (`cart.component.ts:26`) can
never fire.
**Fix:**
- `loadCart` catchError: `this._error.set('cart.errors.loadFailed')`; **don't clobber** — drop the
  `_cart.set(createEmptyCart())` so a previously-loaded cart is preserved and a first-load failure leaves the
  cart null (the error branch wins via template precedence: error before isEmpty).
- `cart.component.ts` error branch: add a Retry button (`(click)="cartService.loadCart()"`,
  `data-testid="cart-retry"`, label `common.retry`).
- i18n: `cart.errors.loadFailed` (en/bg/de).

## Acceptance criteria
1. Orders load failure → error message + Retry (`data-testid="orders-retry"`); NOT the empty-state. A true
   zero-count (successful load, no orders) still shows the empty-state.
2. Cart load failure → the `cart-error` branch renders with a Retry (`data-testid="cart-retry"`); a true
   empty cart still shows the empty-state; a reload failure does not wipe an already-loaded cart.
3. Retry re-issues the load; on success the normal list/cart renders.
4. Works light+dark, EN/BG/DE (keys via i18n). No backend change.

## Test / verification plan
- `cart.service.spec.ts`: loadCart 5xx → `error()` set to `cart.errors.loadFailed`, `isEmpty` not forced, a
  previously-loaded cart is preserved; success path still clears error.
- `cart.component.spec.ts`: error signal → `cart-error` + `cart-retry` visible (not `empty-cart`); retry click
  calls `loadCart`.
- `orders.component.spec.ts`: getOrders error → `orders-retry` visible, `orders-empty` NOT rendered; retry
  calls `loadOrders`; a real empty success → `orders-empty` shown (regression guard).
- i18n key-presence check (the `npm test` i18n gate) stays green across en/bg/de.
- Cross-vendor Codex council on the diff; `/acceptance` drive both failure paths in the real app.

## Scope expansion (council round 1)
The Codex council flagged that **checkout** and the **mini-cart drawer** also render a fake-empty cart on a
first-load failure (same B-020 bug, since `isEmpty()` is true when `_cart` is null). Both now render the
`cartService.error()` error+Retry branch before isEmpty. Added `role="alert"` to all four error containers
(cart/orders/checkout/mini-cart) for live-region a11y.

## Out of scope (tracked follow-ups)
- **FOUND-loaderr-race** (Low): no latest-request guard on `loadOrders`/`loadCart` — a stale failing request
  can resolve after a newer success and re-show the error. Pre-existing `.subscribe()` pattern; tracked.
- Other cart mutation errors (add/update/remove/clear) already set `_error` correctly.
- Order-detail (`order-details.component`) load error (separate; already has `loadFailed`).
