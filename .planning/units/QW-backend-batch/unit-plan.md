---
unit: QW-backend-batch
title: Backend quick-wins hardening batch (B-007, B-008, B-034, B-036, B-055)
status: approved
severity: Medium/Low (5 small independent fixes)
source: docs/project-plan/EXTERNAL_REVIEW_TRIAGE.md
date: 2026-06-29
---

# Unit plan — backend quick-wins hardening batch

Five **independent, small** fixes from the triaged external review, batched into one PR (all backend,
all low-risk, each with its own test). Both real Highs are already shipped (B-011 #86, B-002 #87); these
are the council-ordered quick wins.

## Items

### B-007 — order-email CTA `$<guid>` 404 (NEW-BUG, XS)
`EmailService.cs:66,76` build `$"{baseUrl}/account/orders/${orderId}"` — the stray `$` becomes a literal
`$` in the URL → `/account/orders/$<guid>` → 404 (only fires when `Email:UsePlaceholder=false`).
**Fix:** drop the `$` → `{orderId}`. **Test:** the rendered order-confirmation/shipped body contains
`/account/orders/{guid}` and NOT `/account/orders/$`.

### B-008-residual — stop echoing raw `ArgumentException.Message` (ARCH-04/SR-18, XS)
`ExceptionHandlingMiddleware.cs:38` maps any unhandled `ArgumentException` to `BadRequest` with the raw
`arg.Message` (info disclosure: internal param names/state), and `:62` includes `ArgumentException` in the
`detail` whitelist. **Fix:** map to a generic `"Invalid request"` and remove `ArgumentException` from the
`detail` whitelist. (The High-half guest-token claim was a verified FALSE_POSITIVE.) Validation exceptions
(FluentValidation / app `ValidationException`) keep their messages — those are intended, user-facing.
**Test:** an endpoint that lets an `ArgumentException` propagate returns BadRequest with generic message +
null detail (not the raw message). Existing `AddressesControllerTests` only assert the status (failure-Result
path), so unaffected.

### B-034 — rate-limit the public installation lead endpoint (NEW-SEC, XS)
`InstallationController.CreateInstallationRequest` (`:33`) is anonymous, writes PII + an outbox email, and is
throttled only by the global 100/min/IP — vs contact's 5/min `strict`. **Fix:** add
`[EnableRateLimiting("strict")]` (policy already registered, `Program.cs:273`). **Test:** integration —
the (N+1)th rapid POST returns 429.

### B-036 — clamp pagination/count bounds on public endpoints (PERF-02, S)
Public `GetProducts`/`SearchProducts` take `pageNumber`/`pageSize` and several endpoints take `count` with
NO bounds: `pageSize=0` → div-by-zero garbage in `PaginatedList.TotalPages`; `pageNumber` huge →
`(pageNumber-1)*pageSize` int-overflow → negative `Skip` → 500; `pageSize=100000`/`count=100000` →
fetch-all DoS. **Fix:** a small Api-layer `QueryBounds` helper — `PageNumber ∈ [1,100_000]` (the upper cap
keeps `Skip` overflow-safe), `PageSize ∈ [1,100]`, `Count ∈ [1,24]` — applied at the controller boundary to
**every anonymous public list/count endpoint**: Products (`GetProducts`/`SearchProducts` + 5 count endpoints),
Promotions (list + featured), Brands (list + featured `limit` + brand-by-slug `productPage`/`productPageSize`),
Questions (product), Reviews (product), and Orders (authenticated, clamped for consistency); plus a defensive
`PaginatedList` floor (`pageSize≥1`, `pageNumber≥1`) so `TotalPages` can never divide by zero for ANY caller.
**(Council round 2 widened this from Products-only after Codex flagged Brands/Questions/Reviews/Promotions-list
were still unbounded.)** Admin dashboard `count` endpoints stay out of scope: auth-gated AND `count=10` default
has different semantics (an admin may legitimately want 50 recent orders), so a 1..24 clamp would be wrong. **Tests:** `QueryBounds` unit tests (0/-1/huge → bounds); integration on
`/api/products?pageSize=0`, `?pageSize=100000`, `?pageNumber=0`, `?count=100000` → 200, bounded, no 500;
`PaginatedList` unit test (pageSize=0 → no div-by-zero).

### B-055 — bound + charset-validate inbound `X-Correlation-Id` (NEW-SEC, XS)
`CorrelationIdMiddleware.cs:22` accepts the first inbound `X-Correlation-Id` with no length/charset bound and
echoes + log-pushes it verbatim (log-forging via CR/LF or control chars, oversized-id bloat). **Fix:**
accept the inbound id only if it matches `^[A-Za-z0-9._-]{1,128}$`, else generate a new GUID. **Test:**
integration — a too-long / bad-char id is replaced (response echoes a fresh GUID, not the supplied value);
a valid id is still echoed (existing test).

## Acceptance criteria
1. Order-email links resolve to `/account/orders/{guid}` (no `$`).
2. An unhandled `ArgumentException` returns a generic BadRequest (no raw message / detail leak).
3. The installation lead endpoint 429s after the strict budget.
4. `?pageSize=0|−1|100000`, `?pageNumber=0|huge`, `?count=100000|−1` all return a sane bounded 200 (never 500).
5. A malformed `X-Correlation-Id` is replaced with a fresh GUID; a valid one is echoed.
6. All existing tests still pass; no migration; no i18n; no frontend change.

## Test / verification plan
- Backend unit: `QueryBounds` (clamp matrix) + `PaginatedList` (pageSize=0). Application/Core suites stay green.
- Integration (Api.Tests, Testcontainers): pagination bounds on the real products endpoints; installation
  rate-limit 429; correlation-id validation (extend `CorrelationIdTests`); B-007 rendered-body assertion
  (unit on `EmailService` or via the placeholder path).
- Cross-vendor Codex council on the combined diff (standing rule).
- Runtime `/acceptance`: drive each endpoint against the running API and observe the bounded/secured behavior.

## Out of scope (tracked follow-ups)
- **Admin list pagination** (products/orders/customers/questions/reviews/inventory/notifications) — still
  unbounded (council round-2 Low). Auth-gated (an admin can only break their own request), so deferred; the
  `pageSize=0` div-by-zero is real but self-inflicted. Tracked as **FOUND-QW-admin-pagination** in the backlog.
- Admin dashboard `count` endpoints — auth-gated AND `count` has different semantics (an admin may want 50
  recent orders), so a 1..24 clamp would be wrong.
- The full O-2 error-contract rewrite (B-008 is just the residual info-leak slice).
- B-018/B-020 frontend error-states (separate FE PR) and the B-002 cleanups.
