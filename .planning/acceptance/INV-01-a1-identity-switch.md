---
unit: INV-01-checkout-reservations (Wave A1)
gate: acceptance
result: PASS
date: 2026-07-01
branch: feature/inv-01-a1-identity-switch
commit: 607e7c7
env: Development, real API on :5029 against shared-infra Postgres :5432 + Redis :6379 (2260-product dev DB)
---

# /acceptance — INV-01 Wave A1: guest-identity switch (cookie now authoritative + legacy-cart migration)

**Scope:** A1 makes the A0 signed `cs_guest` cookie **authoritative** for cart + checkout keying (was dark in A0),
and migrates a returning guest's pre-existing legacy cart onto the trusted cookie id so nothing is lost. No
reservation/hold logic (A2). Acceptance bar: (a) the spoofable client id is ignored when a cookie exists; (b) a
pre-existing legacy cart migrates onto the cookie; (c) checkout resolves the cookie cart.

## Scenarios driven against the REAL running app

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | Build a guest cart with a cookie jar; add an item while the **body carries a stale legacy id** | cart keyed by the **cookie id**, the client-supplied id ignored | **PASS** — cart `guestSessionId == cookie C1` (`-S4pdkXz…`); body `stale-legacy-ignored` ignored; cookie-only GET returns qty 2 |
| 2 | DB-seed a **pre-existing legacy cart** (`session_id=L`, qty 3); fresh cookie jar → `GET /api/cart?guestSessionId=L` | mint cookie C2 + **migrate L→C2** (re-key, C2 had no cart); returning guest recovers the cart | **PASS** — migrated cart returned qty 3, keyed by C2 (`Fbj3Ptau…`); cookie-only GET still sees it; **DB: 0 carts under L**, the seeded cart row now carries `session_id=C2` (atomic re-key) |
| 3 | `POST /api/payments/create-intent` with jar J1 (cookie cart qty 2), body carries a stale id | checkout resolves the **cookie** cart, non-zero amount | **PASS** — real intent `pi_3ToV5J…`, amount €1199.98, currency EUR (resolved cookie C1's cart, body id ignored) |

Cleanup: the acceptance test carts (cookie C1/C2 + the seeded legacy cart) were deleted from the dev DB afterward.

## Automated evidence (this branch)
- `dotnet build ClimaSite.sln`: 0 errors (1 pre-existing GDPR CS8602 warning).
- `dotnet test tests/ClimaSite.Core.Tests`: **430 passed**.
- `dotnet test tests/ClimaSite.Application.Tests`: **996 passed** (MigrateGuestCartCommand: re-key / merge / no-op / idempotency under `ExecutionStrategyAttempts=2`).
- `dotnet test tests/ClimaSite.Api.Tests`: **493 passed** — incl. `GuestIdentitySwitchTests` (3: cart+checkout via cookie, AllowLegacyId=false gating), `MigrateGuestCartConcurrencyTests` (real 2-concurrent-migration race, break-probe: 23505 without the advisory lock), `MigrateGuestCartRetrySafetyTests` (stale-tracker break-probe: merges 5 not 50 without `ClearChangeTracker`). `PaymentMoneyPathTests` + `CartControllerTests` unchanged & green.
- `ng test` payment+checkout specs (ChromeHeadless): **62 passed** (`withCredentials`).
- `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes`: **clean**. `git diff --check` trailing-ws = the known CRLF false-positive.

## Mechanism (council-forged)
- Trusted-id resolution via a shared scoped `IGuestCartIdentity.ResolveAsync` (cookie wins; legacy fallback only under `AllowLegacyId`) wired into cart + payments + orders controllers.
- **Legacy-cart migration** `MigrateGuestCartCommand`: convergent re-key (`RekeyGuestCartAsync` atomic `ExecuteUpdate`) / per-item merge+delete, serialized by a **transaction-scoped Postgres advisory lock** on the cookie id (`pg_advisory_xact_lock(hashtextextended(cookieId,0))`) — prevents the concurrent-merge `cart_items` unique-violation (23505) + a context-poisoning catch; `ClearChangeTracker()` first in each retry attempt (no stale-tracked double-merge). `MintPathPrefixes` reverted to `/api/cart` only (checkout validates the existing cookie).

## Council history
Diff: R1 REWORK (concurrent-merge race + broad mint prefixes) → reworked (advisory lock instead of the unsafe catch — implementation found the real failure is 23505, not `DbUpdateConcurrencyException`; mint → `/api/cart`) → **R2 APPROVE-WITH-CHANGES** (ChangeTracker-clear on retry; two other items were non-issues — read-only `git diff` vantage + CRLF) → applied + break-probe-guarded.

## Verdict
**PASS** — zero blocker, zero major. The cookie is authoritative for cart + checkout, the spoofable client id is
ignored, a returning guest's legacy cart migrates onto the cookie (atomic re-key, race- + retry-safe), and checkout
resolves the cookie cart. Ready for PR → CI → squash-merge. **A2 (reservations core)** builds on this trusted identity.
