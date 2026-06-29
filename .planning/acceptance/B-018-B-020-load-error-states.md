---
unit: B-018-B-020-load-error-states
surface: ui (real running Angular app) + automated unit/DOM evidence
result: PASS
date: 2026-06-29
commit: fix/frontend-load-error-states tip (validated against the working diff; final squash tip on merge)
driver: exploratory runtime — drove the REAL running app (`ng serve` :4200 + API :5029) with Playwright
  route-interception forcing the cart/orders GET to 500, observing that each surface renders a real error +
  Retry (not a fake empty), and that Retry recovers.
---

# Acceptance — B-018 / B-020 load-failure error+retry states

## What changed
A load failure was being rendered as an empty state ("you have no orders" / "your cart is empty"). Now each
surface distinguishes a failure from a true zero-count and shows an error message + Retry:
- **B-018 orders**: `loadError` signal + an `@else if (loadError())` error+retry branch before the empty branch.
- **B-020 cart**: `loadCart` sets `_error` (no longer clobbers the cart with a fake empty); the cart page,
  **and (council round-1) checkout + the mini-cart drawer** now render the error+Retry branch before isEmpty.

## Scenarios driven (real running app, cart/orders GET forced to 500)
| # | Surface | Expected | Result |
|---|---|---|---|
| 1 | Cart page | `cart-error` + `cart-retry`, NOT `empty-cart` | ✅ |
| 2 | Cart page | Retry recovers (no error after a successful reload) | ✅ |
| 3 | Account orders | `orders-error` + `orders-retry`, NOT `orders-empty` | ✅ ("Failed to load your orders" + Retry) |
| 4 | Account orders | Retry re-issues the load and shows the list | ✅ |
| 5 | Checkout | `checkout-cart-error` + `checkout-cart-retry`, NOT `checkout-empty` | ✅ |

Screenshots: `scratchpad/loaderr-cart.png`, `loaderr-orders.png`, `loaderr-checkout.png`.

## Automated evidence
- `cart.service.spec`: load 5xx → `error()` = `cart.errors.loadFailed`, loaded cart preserved (not blanked);
  error cleared on a subsequent successful load. (Both fail on the pre-fix code.)
- `cart.component.spec` (new render block): error → `cart-error` + `cart-retry`, not `empty-cart`; Retry → loadCart.
- `orders.component.spec`: error → `orders-error` + `orders-retry`, `orders-empty` absent; Retry recovers; a true
  empty success still shows the empty-state (regression guard).
- `mini-cart-drawer.component.spec` (new block): error → `mini-cart-error` + `mini-cart-retry`, not
  `mini-cart-empty`/footer; Retry → loadCart.
- Full FE suite 1748 green; i18n parity check green (935 keys × en/bg/de; added `cart.errors.loadFailed` +
  `account.orders.errors.loadListFailed`).
- Cross-vendor Codex council (2 rounds): round 1 caught the checkout+mini-cart gap (Medium) + a11y live-region
  (Low) → both fixed; the request-race Low is a tracked follow-up.

## Tracked follow-up (non-blocking)
- **FOUND-loaderr-race** (Low): `loadOrders`/`loadCart` have no latest-request guard — an older failing request
  can resolve after a newer success and re-show the error. Pre-existing `.subscribe()` pattern; fix with a
  sequence id or `switchMap`.

## Verdict
**PASS** — zero blocker, zero major. Every cart/orders load-failure surface now shows a real, accessible
(`role="alert"`) error + Retry instead of a misleading empty state. No backend change; no migration.
