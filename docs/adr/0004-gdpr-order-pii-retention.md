# ADR 0004 — GDPR erasure anonymizes Order PII but retains the invoice record

**Status:** Accepted
**Date:** 2026-06-27
**Supersedes:** —

## Context

GDPR **Article 17** (right to erasure) requires us to delete a user's personal data on request. Our
account-deletion handler (`DeleteUserDataCommand`) already deletes cart/wishlist/addresses/review-votes
and anonymizes reviews + the user record — but it did **not** touch `Orders`, which store directly
identifying personal data: `CustomerEmail`, `CustomerPhone`, and the customer name + full address inside
`ShippingAddress` / `BillingAddress` (SEC-14). The deletion confirmation email even *claimed* "order
history (anonymized) — retained for 7 years for tax/legal compliance", which was false.

Two obligations are in tension:
- **Art. 17(1)** — erase the data subject's personal data without undue delay.
- **Tax/accounting law** — invoices and order records must be retained for a statutory period (EU member
  states commonly require ~6–10 years; we use a 7-year retention statement). GDPR **Art. 17(3)(b)**
  explicitly exempts processing "necessary for compliance with a legal obligation" from the erasure right.

## Decision

On account deletion we **anonymize** the directly-identifying personal data on the user's orders while
**retaining** the order/invoice record itself:

- **Scrubbed** (`Order.AnonymizePersonalData()`): `CustomerEmail` → `anonymized@deleted.local`,
  `CustomerPhone` → null, `ShippingAddress` → `{ anonymized: true }` (this dict also held the customer
  name), `BillingAddress` → null.
- **Retained** for the statutory accounting-retention period: the order id/number, line items
  (product, SKU, quantity, price), monetary totals, currency, status, and timestamps — none of which
  identify the data subject once the contact/address fields are scrubbed.

Orders are matched by `Order.UserId`. (Guest orders placed without an account are out of scope of an
account deletion and are addressed by the separate retention-sweep, future work.)

## Alternatives considered

1. **Hard-delete orders** — cleanest "erasure", but violates the tax/accounting retention obligation and
   destroys legitimate business records (revenue, returns/chargebacks). Rejected.
2. **Retain orders unchanged** — keeps PII indefinitely, violating Art. 17. Rejected (the prior state).
3. **Anonymize PII, retain the record (chosen)** — satisfies Art. 17 for the personal data while keeping
   the legally-required, now non-identifying, invoice record. Standard industry practice.

## Consequences

- The deletion confirmation email's "order history (anonymized)" statement is now accurate.
- Order analytics/revenue reporting are unaffected (amounts/items/dates retained); customer-level
  attribution is intentionally lost for deleted users.
- A deleted user's orders can no longer be looked up by email/phone (by design).
- Tested by `GdprControllerTests` (asserts the order PII is scrubbed after deletion).
- Follow-up: a scheduled retention sweep to hard-purge anonymized orders past the 7-year window, and a
  decision on guest-order retention.
