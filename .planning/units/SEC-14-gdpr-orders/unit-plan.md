---
unit: SEC-14-gdpr-orders
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
adr: ../../../docs/adr/0004-gdpr-order-pii-retention.md
---

# Unit plan — SEC-14: anonymize Order PII on GDPR erasure (retain the invoice)

## Context (verified)
`DeleteUserDataCommand` deletes cart/wishlist/addresses/votes + anonymizes reviews + the user, but never
touched `Orders` — which hold `CustomerEmail`, `CustomerPhone`, and the name+address in
`ShippingAddress`/`BillingAddress`. The confirmation email even falsely claimed "order history
(anonymized)". Decision (owner: "best for a real company") + lawful basis recorded in **ADR-0004**:
anonymize the order PII, RETAIN the invoice record for tax/accounting (Art. 17(3)(b)).

## Scope
1. `Order.AnonymizePersonalData()` — scrub `CustomerEmail` → `anonymized@deleted.local`, `CustomerPhone`
   → null, `ShippingAddress` → `{ anonymized: true }` (the dict held the name), `BillingAddress` → null.
2. `DeleteUserDataCommandHandler` — after the review anonymization, load the user's orders
   (`o.UserId == userId`) and call `AnonymizePersonalData()` on each (inside the existing
   execution-strategy transaction).
3. ADR-0004 (legal basis) + index row.

## Acceptance criteria
- [x] After deletion, the user's orders have anonymized email/phone/address (name gone) and the order
      record is RETAINED (id/items/amounts/dates intact).
- [x] Integration test (`GdprControllerTests.DeleteAccount_AnonymizesOrderPii_ButRetainsTheOrderRecord`).
- [x] The confirmation email's "order history (anonymized)" claim is now true.
- [x] `dotnet test tests/ClimaSite.Api.Tests --filter ~Gdpr` green (10/10).

## Out of scope (follow-ups)
- A scheduled retention sweep to hard-purge anonymized orders past the 7-year window.
- Guest-order (no `UserId`) retention/erasure policy.
