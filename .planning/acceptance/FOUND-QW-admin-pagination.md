---
unit: FOUND-QW-admin-pagination
surface: api (real running host, admin-authenticated) + automated integration evidence
result: PASS
date: 2026-06-30
commit: fix/admin-pagination-bounds tip (validated against the working diff; final squash tip on merge)
driver: exploratory runtime — logged into the REAL running API (`dotnet run` :5029, Development) as the
  seeded admin and hit each admin list endpoint with out-of-bounds pagination against shared-infra Postgres
  (db `climasite`, ~2260 products).
---

# Acceptance — FOUND-QW-admin-pagination

## Scenarios driven (real running admin API)
| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | `GET /api/admin/products?pageSize=100000` (2260-product DB) | items capped at 100 | ✅ **100** items, total 2260 |
| 2 | `GET /api/admin/products?pageSize=0` | 200, no div-by-zero | ✅ 200 |
| 3 | `GET /api/admin/products?pageNumber=2147483647` | 200, no Skip overflow | ✅ 200 |
| 4 | `GET /api/admin/orders?pageSize=0` | 200 | ✅ 200 |
| 5 | `GET /api/admin/customers?pageSize=0` | 200 | ✅ 200 |
| 6 | `GET /api/admin/reviews/pending?pageSize=0` | 200 | ✅ 200 |
| 7 | `GET /api/admin/questions/pending?pageSize=0` | 200 | ✅ 200 |

## Automated evidence
- `AdminPaginationBoundsTests` (integration, admin-authenticated, real Testcontainers Postgres) — every
  auth-gated paginated/count endpoint (products/orders/customers/questions/reviews/**inventory**/
  **installation-requests**/**notifications** + the **dashboard** recent-orders/low-stock/top-products `count`)
  with `pageSize=0` / huge `pageNumber` / `pageSize=100000` / `count=-1` / `count=100000` → status < 500
  (**20 cases**). Exact clamp values pinned by `QueryBoundsTests`. Full Api.Tests **446** green; `dotnet format` passes.
- Cross-vendor Codex council (**3 rounds**): round 1 caught inventory + installation-requests (Medium) + the
  notifications list (Low); round 2 caught the dashboard `count` crash/DoS (Medium, `Take(-1)` / fetch-all) →
  fixed with a generous `DashboardCount` (1..100) clamp; round 3 clean. The first pass missed the bound-record
  endpoints the param grep didn't surface — the council closed every gap.

## Verdict
**PASS** — every admin list endpoint now clamps untrusted pagination at the controller edge via `QueryBounds`,
so an admin can no longer 500 their own screen with `pageSize=0` / a huge `pageNumber`, and a huge `pageSize`
caps at 100. AdminDashboard `count` + Notifications `recentCount` deliberately excluded (different "recent N"
semantics). No DB migration; no i18n; no frontend change.
