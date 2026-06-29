---
unit: QW-backend-batch
surface: api (real running host) + automated unit/integration evidence
result: PASS
date: 2026-06-29
commit: fix/backend-quickwins-batch tip (validated against the working diff; final squash tip on merge)
driver: exploratory runtime — drove the REAL running API (`dotnet run` on :5029, Development env so rate
  limiting is ON) against shared-infra Postgres (db `climasite`, ~2259 seeded products) with curl: rate-limit
  flood, oversized pagination/count, and malformed correlation-id, observing the bounded/secured behavior.
---

# Acceptance — backend quick-wins hardening batch (B-007, B-008, B-034, B-036, B-055)

## Scenarios driven (real running API)
| # | Item | Scenario | Expected | Result |
|---|---|---|---|---|
| 1 | B-036 | `GET /api/products?pageSize=100000` on the 2259-product DB | items capped at 100 | ✅ **100 items** returned, totalCount 2241 |
| 2 | B-036 | `GET /api/products?pageSize=0` | 200, no div-by-zero | ✅ 200 |
| 3 | B-036 | `GET /api/products?pageNumber=2147483647` (int.MaxValue) | 200, no Skip overflow | ✅ 200 |
| 4 | B-036 | `GET /api/products/featured?count=100000` | ≤ 24 | ✅ 5 (all featured; no error) |
| 5 | B-055 | `X-Correlation-Id: bad value with spaces!` | echoed value is a fresh GUID | ✅ echoed `c035ea6b-…` (GUID) |
| 6 | B-055 | `X-Correlation-Id: valid-abc_123.45` | echoed verbatim | ✅ echoed `valid-abc_123.45` |
| 7 | B-034 | 8 rapid POSTs to `/api/installation/requests` | 429 after the strict 5/min budget | ✅ requests 6–8 → **429** |
| 8 | B-036 (round 2) | `GET /api/brands?pageSize=100000` | items capped at 100 | ✅ 14 items (all brands; ≤100) |
| 9 | B-036 (round 2) | `GET /api/brands?pageSize=0`, `/api/promotions?pageSize=0`, `/api/promotions?pageNumber=2147483647`, `/api/reviews/product/<rnd>?pageSize=0` | 200, no 500 | ✅ all 200 |
| 10 | B-036 (round 2) | `GET /api/brands/featured?limit=100000` | ≤ 24 | ✅ 7 (all featured) |

Note on scenario 7: requests 1–5 returned 500 — that is the **Development-only** Developer Exception Page
rendering the `ValidationException` from the deliberately-invalid test body (all-zero productId). In
Testing/Prod (no dev page) `ExceptionHandlingMiddleware` maps that to a clean 400 — verified by the existing
integration tests. The rate limiter (the thing under test) fired correctly regardless.

## Automated evidence
- **B-007** (email URL): `EmailServiceTests.BuildOrderUrl_…` — asserts `/account/orders/{guid}` with no `$`
  (the placeholder send mode logs only body length, so the URL is guarded at its single source via the new
  `BuildOrderUrl` helper used by both order emails).
- **B-008** (error leak): `ExceptionHandlingMiddlewareTests` — ArgumentException → generic "Invalid request" +
  null detail (no raw message); ValidationException still echoes its user-facing message; NotFound → 404.
- **B-034** (rate limit): `InstallationRateLimitTests` pins the `[EnableRateLimiting("strict")]` attribute
  (the live 429 above is the behavioral proof, since rate limiting is disabled in the Testing env).
- **B-036** (bounds): `QueryBoundsTests` pins the exact clamps (PageSize 1..100, Count 1..24, PageNumber
  1..100000, Days 1..730, overflow-safe) + `PaginatedListTests` (pageSize 0 → no div-by-zero) +
  `ProductsControllerTests` + `PublicPaginationBoundsTests` integration (out-of-bounds params on **every**
  anonymous public list/count/range endpoint — products/promotions/brands/questions/reviews/price-history →
  status < 500). **Scope widened in council round 2** from Products-only to all anonymous public endpoints
  (Brands/Questions/Reviews/Promotions-list) + Orders (auth, for consistency); the public price-history
  `daysBack` range was clamped in round 2's follow-up. Admin list pagination (auth-gated) is a tracked
  follow-up (FOUND-QW-admin-pagination).
- **B-055** (correlation-id): `CorrelationIdTests` — overlong/illegal id → replaced with a GUID; valid id echoed.
- Suites: Application **873**, Core **424**, Api integration **411** — all green. `dotnet format` gate passes.
  No frontend change.
- Cross-vendor Codex council (`gpt-5.5`@`xhigh`) ran **3 rounds**: round 1 confirmed B-007/B-008/B-034 and
  the products-side B-036/B-055; round 2 flagged B-036 under-scoped (→ widened to all anonymous public
  endpoints) + a B-055 `$`→`\z` anchor (→ fixed) + a public price-history range (→ clamped); round 3 confirms
  clean (the round-2 "whitespace" Medium was a CRLF artifact of `git diff --check`, not the real
  `dotnet format` gate, which passes).

## Verdict
**PASS** — zero blocker, zero major. All five fixes proven (live where behavioral, unit/integration where
deterministic). No migration, no i18n, no frontend change.
