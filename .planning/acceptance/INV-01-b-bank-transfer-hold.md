---
unit: INV-01-checkout-reservations (Wave B)
gate: acceptance
result: PASS
date: 2026-07-02
branch: feature/inv-01-b-bank-transfer-hold
commit: 9dbe3ff
env: Development, real API on :5029 (+ background reservation sweeper) against shared-infra Postgres :5432 + Redis :6379
---

# /acceptance — INV-01 Wave B: bank-transfer hold-with-expiry (close the offline-order stock leak)

**Scope:** a bank-transfer order now RESERVES a `Kind=BankTransfer` hold at order-create (no physical decrement),
CONSUMES it (decrement) when an admin marks it paid, and the sweeper auto-EXPIRES + auto-CANCELS the order if unpaid
past a 3-day TTL — closing the old leak where offline orders hard-decremented stock and never restocked if the wire
never arrived. This is the LAST INV-01 slice.

## Scenarios driven against the REAL running app

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | `POST /api/orders` with `paymentMethod=bank` **+ a `paymentIntentId`** | rejected (a bank order must never carry a PI — else the Stripe webhook would mark it Paid without consuming the hold) | **PASS** — HTTP **400** "A bank-transfer order cannot carry a payment intent." |
| 2 | `POST /api/orders` with `paymentMethod=bank`, **no PI**, cart = 2 units (variant stock 20) | order `Pending`; a bank hold is created; **stock UNCHANGED (20), reserved=2 held** — NOT hard-decremented | **PASS** — order `ORD-…-B336` Pending; `stock=20 reserved=2`; 1 active bank hold |
| 3 | Leave the bank order unpaid, expire its hold | the background sweeper auto-**cancels** the order + **releases** the hold; stock never leaked | **PASS** — within 20 s: `order.status=Cancelled`, `reserved=0`, 0 active holds, `stock=20` (never decremented), hold `Expired` |

Scenario 2+3 together prove the leak is closed: an unpaid offline order **holds** stock (doesn't consume it) and the
hold is automatically returned. Scenario 1 proves the cross-path guard (a bank order can never be flipped Paid by the
webhook without consuming). Cleanup: the bank order + reservations deleted, the variant reset to `stock=50 reserved=0`.

The **mark-paid → consume → decrement** path (admin `PUT /api/admin/orders/{id}/status` → Paid) is covered by the
Testcontainers integration suite rather than re-driven via the auth'd admin API — see below.

## Automated evidence (this branch)
- `dotnet build ClimaSite.sln`: 0 errors. Core **430** / Application **1045** / Api integration **526** — all green.
  `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes`: clean.
- `BankTransferReservationTests` (Testcontainers) covers the full lifecycle + the mark-paid path with break-probes,
  each confirmed to FAIL with its fix removed:
  - reserve is available-gated (a bank hold can't steal a card-held unit); the `(order_id,variant_id)` filtered-unique
    dedupes; a bank order HOLDS at create (stock unchanged); mark-paid CONSUMES (decrement + hold Consumed).
  - **`ConcurrentMarkPaidAndCancel_BankOrder_NeverLeaksStock`** (15× concurrent mark-paid + cancel) — always settles to
    Paid+sold OR Cancelled+released, never Cancelled-with-stock-decremented-not-restocked (break-probe: remove the
    order-row lock ⇒ the race leaks).
  - `MarkBankOrderPaid_FromPaymentFailed_ConsumesHold_DecrementsStock`; multi-line one-hold-pre-expired ⇒ mark-paid
    FAILS + rolls back (all-or-nothing, no partial decrement); admin status→Cancelled releases the hold immediately.

## Mechanism
`ReserveBankOrderAsync` (order-keyed, available-gated, whole-batch rollback) / `ConsumeBankOrderAsync` (decrement-first,
all-or-nothing via `BankConsumeResult`) / `ReleaseBankOrderAsync` / `SweepExpiredBankHoldsAsync` (expire + auto-cancel).
**All order-status transitions (mark-paid / cancel / sweep-cancel) serialize on an order-row `FOR UPDATE` lock** taken
BEFORE the variant locks (uniform order → deadlock-free), re-reading status/holds under the lock. Consume fires on ANY
→Paid (incl. `PaymentFailed→Paid`). Filtered-unique `(order_id, variant_id) WHERE status='Active' AND kind='BankTransfer'`.
`CancelOrderCommand`: bank Active-hold ⇒ release (no restock); card Consumed ⇒ atomic `IncrementVariantStockAsync`.

## Council history
Diff R1 REWORK (4 order-lifecycle gaps: PaymentFailed→Paid skips consume, cancel-races-mark-paid from an unlocked
snapshot, partial consume, admin-cancel leaks) → reworked around the order-row lock → R2 REWORK (1 [H]: a bank order
carrying a PI would be flipped Paid by the webhook without consuming) → reworked (reject `bank + PI`) → intended
lifecycle **confirmed unbreakable** ("I cannot break the mark-paid/cancel/sweep lifecycle"). The paid-order admin
STATUS-endpoint no-restock asymmetry (all order types, pre-INV-01) is a tracked follow-up, not a B blocker.

## Verdict
**PASS** — zero blocker, zero major. A bank order holds stock instead of hard-decrementing; unpaid holds auto-expire +
auto-cancel the order (leak closed); a bank order can't carry a payment intent; and the mark-paid / cancel / sweep
lifecycle is serialized so no interleaving leaks or double-applies. **This completes INV-01** (A0–A3 + B).
