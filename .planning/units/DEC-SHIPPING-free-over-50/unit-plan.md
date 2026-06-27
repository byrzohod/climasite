---
unit: DEC-SHIPPING-free-over-50
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
---

# Unit plan — DEC-SHIPPING: free standard shipping over €50

## Owner decision
Free **standard** shipping when the order **subtotal ≥ €50**, else €5.99. Express €15.99 / overnight
€19.99 unchanged. HVAC is big-ticket so most real orders ship free; small orders cover shipping.

## Context (verified — money path)
- **Source of truth:** `CheckoutPricing.GetShippingCost(method)` (no subtotal today). Callers:
  `CalculateTotal(subtotal, method)` → Stripe amount (`CreatePaymentIntentCommand:92`, has `subtotal`);
  `CreateOrderCommand:240` → `order.SetShippingCost(GetShippingCost(method))` (has `order.Subtotal`).
- **Latent bug to fix:** the checkout order-summary shipping line (`checkout.component.ts:353-362`) reads
  `cartService.cart()?.shipping`, which is hardcoded `0` (`cart.service.ts:124`) → the summary always
  shows "Free shipping" and a **total that omits** express/overnight cost. Must reflect the selected
  method so **displayed == charged**.
- The option labels (`shippingCost` map, BUG-11) are static `{standard:5.99,…}` → standard must become
  threshold-aware.

## Scope
**Server (single source of truth):**
1. `CheckoutPricing`: add `public const decimal FreeShippingThreshold = 50m;`. Change signature to
   `GetShippingCost(string? method, decimal subtotal)` with `"standard" => subtotal >= FreeShippingThreshold ? 0m : 5.99m`
   (express/overnight unchanged; unknown → 9.99m). `CalculateTotal` passes the subtotal through. Replace
   the now-stale `$`/"free" NOTE with the threshold rule.
2. `CreateOrderCommand:240` → `GetShippingCost(request.ShippingMethod, order.Subtotal)`.

**UI (mirror the server — keep displayed == charged):**
3. `checkout.component.ts`: standard `shippingCost` reflects the cart subtotal vs €50 (compute, e.g.
   `standardShipping()` = subtotal ≥ 50 ? 0 : 5.99); option label shows "Free" when 0.
4. The order-summary shipping line + total: compute the **selected method's** cost (threshold-aware) so
   the summary shipping line and the grand total match what Stripe charges. Show "Free shipping" only when
   the selected method's cost is truly 0.

## Acceptance criteria
- [ ] Server: `GetShippingCost("standard", 50) == 0`, `("standard", 49.99) == 5.99`, `("standard", 50.01) == 0`;
      express/overnight/unknown unchanged. `CalculateTotal` reflects it. Unit tests incl. the boundary.
- [ ] `CreateOrderCommand` persists the threshold-aware shipping; the persisted order total ==
      `CalculateTotal(subtotal, method)` == the verified Stripe intent amount (existing money-path tests pass).
- [ ] Checkout option label for standard shows "Free" when subtotal ≥ €50, else €5.99.
- [ ] Checkout order-summary shipping line + grand total reflect the **selected** method (no more always-free) —
      displayed == charged for standard(<50 / ≥50), express, overnight.
- [ ] `dotnet test tests/ClimaSite.Application.Tests` + `tests/ClimaSite.Api.Tests` (money-path) green;
      `ng test` green; works in light/dark + EN/BG/DE (i18n keys reused: `cart.summary.freeShipping`).

## Test / verification plan
- **Server unit:** extend `CheckoutPricingTests` — boundary `[InlineData]` (49.99→5.99, 50→0, 50.01→0,
  0→5.99), express/overnight unchanged, the 2-arg signature; `CalculateTotal` boundary.
- **Money path:** existing `PaymentMoneyPathTests` / `CreatePaymentIntent*` / `CreateOrderCommand*` use
  self-referential `CalculateTotal(...)` so they stay valid; confirm they pass with the new logic.
- **UI:** extend `checkout.component.spec.ts` — standard cost flips at the €50 boundary; summary shipping
  line + total reflect the selected method.
- **Adversarial verify:** a reviewer confirms NO other shipping-cost source diverges (cart.service,
  product-card `[freeShipping]` badge @ €500 is a separate per-product marketing badge — note only),
  and that displayed == charged at every touchpoint.

## Out of scope
- Product-card per-product `[freeShipping]` badge threshold (€500) — separate marketing badge; note for consistency follow-up.
- Free-shipping promo type (`Promotion.FreeShipping`) — existing, unrelated.
