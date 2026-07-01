---
unit: INV-01-checkout-reservations (Wave A0)
gate: acceptance
result: PASS
date: 2026-07-01
branch: feature/inv-01-a0-guest-token
commit: <squash-merge tip of PR feature/inv-01-a0-guest-token>
env: Development, real API on :5029 against shared-infra Postgres :5432 + Redis :6379 (2260-product dev DB)
---

# /acceptance — INV-01 Wave A0: server-minted signed guest-session token (shipped DARK)

**Scope reminder:** A0 ships the signed `cs_guest` cookie **dark** — minted, validated, and published on
`IGuestSessionAccessor`, but NOT yet authoritative for cart-keying (that flip + legacy migration + legacy-reject
lands in Wave A). So the acceptance bar is: (a) the cookie infrastructure works correctly on the real pipeline,
(b) minting is scoped to cart paths, (c) **zero behavior change** — existing guest carts still key off the legacy id.

## Scenarios driven against the REAL running app (curl)

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | `GET /api/cart` with no cookie | `Set-Cookie: cs_guest=<id.exp.sig>; path=/; samesite=lax; httponly`, 30-day expiry | **PASS** — `cs_guest=cWi3…Dzs`, 3-part signed token, `httponly` + `samesite=lax` + `expires=+30d` present |
| 2 | `GET /health` (no cookie) | NO `Set-Cookie` (mint-scoped) | **PASS** — no Set-Cookie |
| 3 | `GET /api/products` (no cookie, cacheable) | NO `Set-Cookie` (mint-scoped) | **PASS** — no Set-Cookie |
| 4 | Replay a valid minted cookie on `/api/cart` | validated + reused, NO re-mint; token = 3 parts (`id.exp.sig`) | **PASS** — reused, no new Set-Cookie; confirmed 3 parts |
| 5 | Forged cookie (tamper last char) | rejected → fresh mint with a DIFFERENT id | **PASS** — re-minted, new id ≠ forged id |
| 6 | Guest cart round-trip via a **legacy** `guestSessionId`, no cookie | cart still keyed by the legacy id (dark ship, no regression) | **PASS** — add qty 2 → `cart.guestSessionId == "accept-legacy-…"`, item retrievable by the same legacy id |

**Cookie flags verified on the wire:** `HttpOnly` ✓ · `SameSite=Lax` ✓ · `Path=/` ✓ · `expires` +30d ✓ ·
(Dev is http → unprefixed `cs_guest`, `Secure` off; deployed HTTPS envs use `__Host-cs_guest` + `Secure`). Token is
always the 3-part `id.expUnixSeconds.HMAC(id.exp)` form (never a bare id); expiry is cryptographically enforced.

## Automated evidence (this branch)
- `dotnet build ClimaSite.sln`: 0 errors (1 pre-existing GDPR CS8602 warning, unrelated).
- `dotnet test tests/ClimaSite.Core.Tests`: **430 passed**.
- `dotnet test tests/ClimaSite.Application.Tests`: **986 passed** (token service: 18 cases incl. tampered id/exp/sig,
  foreign-key, 9 malformed shapes, expired-but-validly-signed → rejected, future-exp → accepted).
- `dotnet test tests/ClimaSite.Api.Tests`: **488 passed** (guest-session middleware 5 + mint-limiter 4;
  CartControllerTests unchanged & green — carts are revert-safe/dark).
- `ng test cart.service.spec` (ChromeHeadless): **38 passed** (`withCredentials` on every cart call).
- `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes`: **clean** (exit 0). `git diff --check` trailing-ws flags
  are the known CRLF false-positive (`grep` confirms no real trailing spaces; the real gate passes).

## Council history (design + diff)
Design: R1 REWORK → R2 REWORK-narrow → **R3 CONVERGED** (unit-plan). Diff: R1 REWORK (3 Highs from a half-switch:
legacy carts orphaned / checkout not wired / over-cap legacy bypass) → **reworked to ship DARK** (revert the cart
behavior switch; the Highs dissolve) → R2 **APPROVE-WITH-CHANGES** (IPv6 mint-keying, mint-scope→`/api/cart` only,
Secure/`__Host-` on all deployed envs, comment accuracy) → all applied + re-verified (build + suites + format clean).

## Verdict
**PASS** — zero blocker, zero major. The signed guest-cookie foundation works correctly on the real pipeline,
minting is scoped to `/api/cart`, and there is no behavior change (existing guest carts key off the legacy id
exactly as before). Ready for PR → CI → squash-merge. Wave A consumes the accessor and flips cart+checkout keying.
