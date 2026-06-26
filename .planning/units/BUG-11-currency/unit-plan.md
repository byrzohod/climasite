---
unit: BUG-11-currency
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-26
design: ../../design/DESIGN.md
---

# Unit plan — BUG-11 / DEC-CURRENCY: one consistent display currency (EUR)

## Context (verified against code)
DEC-CURRENCY = charge in **EUR** (transitional dual EUR/BGN display is a SEPARATE, larger follow-up).
Current state:
- **18 bare `| currency`** (→ render USD `$` by default) in checkout, cart, mini-cart-drawer, mini-cart-item.
- 18 files already use explicit `| currency:'EUR'` (correct).
- **No `DEFAULT_CURRENCY_CODE`** configured (`app.config.ts`).
- **Shipping-option labels are WRONG amounts AND wrong symbol** vs the server: `CheckoutPricing.cs`
  charges standard **€5.99** / express **€15.99** / overnight **€19.99**, but the checkout UI shows
  standard **"free"**, express **"$9.99"**, overnight **"$19.99"** (the backend file even has a NOTE
  documenting this mismatch). So displayed shipping ≠ charged shipping — a money-path display defect.

## Scope
Make every displayed price render in **EUR**, and make the shipping-option labels match what the server
actually charges. Out of scope: dual EUR/BGN transitional display (tracked as a DEC-CURRENCY follow-up).

1. `app.config.ts`: add `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' }` (safety net so any bare
   `| currency` renders €, not $).
2. Convert the 18 bare `| currency` → `| currency:'EUR'` in the 4 offender files (explicit; satisfies
   "no bare pipes" + prevents future drift).
3. Fix the checkout shipping-option labels to the **real EUR amounts** from `CheckoutPricing.cs`
   (standard €5.99, express €15.99, overnight €19.99) — sourced via a small client-side shipping-cost
   map that mirrors the server (with a "keep in sync with CheckoutPricing.cs" comment), rendered through
   `| currency:'EUR'`. Remove the `$9.99`/`$19.99` literals and the "free" standard label (unless free
   standard is a product decision — if so that's a backend change, out of scope; UI must match the
   server's actual charge).
4. Confirm order-confirmation / cart / checkout / mini-cart all render EUR.

## Acceptance criteria
- [ ] `grep -rn "| currency }}" src/app | grep -v "currency:'"` → **0** (no bare currency pipes).
- [ ] `grep -rnE '\$[0-9]+\.[0-9]' src/app` (excluding template-literals) → **0** ($ price literals gone).
- [ ] Checkout shipping-option labels show **€5.99 / €15.99 / €19.99**, matching `CheckoutPricing.cs`.
- [ ] `DEFAULT_CURRENCY_CODE='EUR'` provided in `app.config.ts`.
- [ ] product / cart / checkout / mini-cart / order-confirmation all render the **same** currency (€).
- [ ] `ng test` green (incl. any updated cart/checkout/confirmation specs); `ng build` clean.

## Test / verification plan
- **Automated:** add/extend a cart or checkout component spec asserting the shipping-cost map values
  (5.99/15.99/19.99) and that the currency code used is EUR; run `ng test --watch=false`.
- **Grep acceptance:** the two grep checks above must return 0.
- **Manual/visual:** open /checkout, /cart, the mini-cart, and an order-confirmation in the running app —
  every price shows € and the shipping options show €5.99/€15.99/€19.99.
- **Money-path consistency:** the selected shipping option's label amount == the cart summary `shipping`
  line == the server charge (CheckoutPricing).

## Out of scope (tracked follow-ups)
- Dual EUR/BGN transitional display (the fuller DEC-CURRENCY requirement) — needs a shared price
  pipe/component + a peg constant (1.95583) + design for placement; separate unit.
- Whether standard shipping should be FREE (product/backend decision) vs the current €5.99.
