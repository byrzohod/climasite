---
unit: INV-01-checkout-reservations (Wave A2)
gate: acceptance
result: PASS
date: 2026-07-02
branch: feature/inv-01-a2-reservations-core
commit: <squash-merge tip of PR feature/inv-01-a2-reservations-core>
env: Development, real API on :5029 (+ background reservation sweeper) against shared-infra Postgres :5432 + Redis :6379, real Stripe test mode
---

# /acceptance — INV-01 Wave A2: reservations CORE (the money-path behavior change)

**Scope:** A2 makes checkout reserve stock at PaymentIntent creation (BEFORE the card is charged), converts the
hold to a sale at order-create, and auto-expires abandoned holds — so the losing last-unit buyer is **blocked
before payment**, not charged-then-refunded. This is the headline behavior of INV-01.

## Scenarios driven against the REAL running app

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | **Concurrent last-unit race** — variant set to `stock=1`; two independent guest carts each hold it; both fire `POST /api/payments/create-intent` CONCURRENTLY | exactly ONE reserves + gets a Stripe intent; the OTHER is rejected **before any charge**; `reserved_quantity==1` (not 2) | **PASS** — A: HTTP 200, real intent `pi_3Todgg…` €599.99; **B: HTTP 400 "Insufficient stock" — NO Stripe intent created**; DB `stock=1 reserved=1`, exactly 1 active hold |
| 2 | **Abandoned-hold auto-expiry** — the winner's hold is left unpaid past its TTL | the background sweeper (polling 30 s) expires the hold and returns the unit to availability | **PASS** — log "Reservation sweeper started; polling every 00:00:30"; after backdating `expires_at`, within 10 s `reserved_quantity→0`, hold→`Expired`, `available = stock−reserved = 1` again |
| 3 | **Admin cannot drain a held unit** | admin reducing stock below `reserved_quantity` is rejected | **PASS (integration)** — covered by the Testcontainers break-probes `AdminAdjustStock_CannotDrainAHeldUnit` + `AdminBulkAdjustStock_CannotSetBelowHeldUnits` (each fails with its guard removed); not re-driven via the auth'd admin API |

Scenario 1 is the core proof: **the loser (B) never reached Stripe** — the reserve fails atomically at create-intent
before the intent is created, so there is no charge to refund. Cleanup: reservations + race carts deleted, the test
variant reset to `stock=50 reserved=0`.

## Automated evidence (this branch)
- `dotnet build ClimaSite.sln`: 0 errors (1 pre-existing GDPR CS8602 warning).
- Core **430** / Application **1020** / Api integration **509** — all green. `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes`: clean.
- **Mandatory break-probes (each confirmed to FAIL with its fix removed, then reverted):** (A) over-reserve — remove the
  `stock-reserved>=q` gate ⇒ two carts reserve the last unit; (B) reserve-vs-sweeper — remove the `expires_at<=NOW()`
  CAS ⇒ a live future-leased hold is expired; (S) sibling refund — remove `RefundOrFailAsync` ⇒ a placed order is
  refunded; reserved-aware decrement, sweep reconciler, reconcile-to-target own-expired-hold reuse, admin single +
  bulk drain guards, stale-consumed re-sell. (D) reverse-order = a no-deadlock correctness assertion (EnableRetryOnFailure
  masks a hard deadlock probe).

## Mechanism (council-forged, verified in code)
`SELECT … FOR UPDATE` per-variant lock (P0) serialises every hold mutation; ascending-`variant_id` lock ordering on
every multi-variant loop; **clock-independent** `reserved_quantity == Σ Active holds` (the sweeper is the sole
releaser); reserve reconcile-to-target; consume decrement-first; expiry-CAS; **reserved-aware on EVERY stock writer**
(`TryDecrementVariantStockAsync`, single + bulk admin adjust); order-create idempotency (deterministic order.Id +
top-of-delegate lookup + `RefundOrFailAsync` on all refund sites + PK/intent duplicate catch); change-tracker-neutral
service (`AsNoTracking` reads + from-state-gated SQL writes — retry-safe, doesn't detach the caller's cart).

## Council history
Design R3 CONVERGED. Diff: R1 REWORK (2 Highs: stale-consumed under-sell + admin-bypass) + a Claude concurrency
verifier (corroborated) → reworked → R2 APPROVE-WITH-CHANGES (bulk-atomic + orphan-refund tests) → applied. Both legs
confirmed the core unbreakable: "could not break the re-reserve+consume path under the variant lock"; P1 last-unit
race safe; AsNoTracking retry-safe; refund coverage complete.

## Verdict
**PASS** — zero blocker, zero major. The losing last-unit buyer is blocked **before payment**, no concurrent path
over-reserves or over-sells, abandoned holds auto-expire, and no stock writer (checkout, admin single, admin bulk)
can drain a held unit. Ready for PR → CI → squash-merge. **A3** adds reservation-aware availability *display* on
PDP/cart; **B** reworks bank-transfer to hold-with-expiry.
