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

On account deletion we **erase the personal data** on the user's orders while **retaining the accounting
order record** itself:

- **Scrubbed** (`Order.AnonymizePersonalData()`): `CustomerEmail` → `anonymized@deleted.local`,
  `CustomerPhone` → null, `ShippingAddress` → `{ anonymized: true }` (this dict also held the customer
  name), `BillingAddress` → null, and the free-text `Notes` + `CancellationReason` → null (they may hold
  names/phones/delivery instructions).
- **Outbox cleared** (`DeleteUserDataCommandHandler`): the user's `OutboxMessages` (email-queue) rows are
  deleted — they carry the recipient address + order payloads, and a still-`Pending` row would otherwise
  send a post-erasure email to the original address.
- **Retained** for the statutory accounting-retention period: the order id/number, line items
  (product, SKU, quantity, price), monetary totals, currency, status, timestamps, and the payment-processor
  join key (`PaymentIntentId`) — none of which identify the data subject once the fields above are scrubbed.
- **`Order.UserId` is intentionally kept** as an internal retention/audit key (it now points to an
  already-anonymized user row). Because the order row therefore still *correlates* to that internal id,
  this is honestly **pseudonymization-grade erasure of the personal data**, not full anonymization — but no
  directly-identifying personal data remains readable on the order.

Orders are matched by `Order.UserId`. (Guest orders placed without an account — including a logged-in
user's *prior* guest checkouts under the same email — are **out of scope** of an account deletion here and
are addressed by the separate retention-sweep, future work.)

> **Invoice note:** these are the live order's *source* fields. A *new* invoice generated for a deleted
> user (`GenerateInvoiceQuery`) will therefore show anonymized buyer data — intended (GDPR-erasure-first).
> If local tax law requires the *already-issued* invoice document to retain buyer name/address, that
> document must be archived separately at issue time; it is not the live order record this ADR governs.

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
