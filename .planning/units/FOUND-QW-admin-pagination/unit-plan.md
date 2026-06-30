---
unit: FOUND-QW-admin-pagination
title: Clamp admin list pagination bounds (QueryBounds)
status: approved
severity: Low (auth-gated div-by-zero / overflow)
source: QW-backend-batch council round 2 (FOUND-QW-admin-pagination)
date: 2026-06-30
---

# Unit plan — clamp admin list pagination bounds

The public B-036 fix clamped every anonymous list endpoint, but the **admin** list endpoints were left
(auth-gated). They drive their own `Math.Ceiling(count / (double)PageSize)` + `Skip`/`Take`, so `pageSize=0`
→ div-by-zero garbage / 500 and a huge `pageNumber` → negative-Skip overflow / 500 — on an admin's own
screen. Low severity (an admin can only break their own request), but a real edge.

## Scope
Apply the existing `QueryBounds` (PageNumber 1..100000, PageSize 1..100) at the controller boundary of every
auth-gated paginated list endpoint:
- Individual-int-param controllers: `AdminProductsController` (1), `AdminQuestionsController` (2),
  `AdminReviewsController` (2) — `QueryBounds.PageNumber/PageSize(...)`.
- Bound-record controllers: `AdminOrdersController`, `AdminCustomersController`, **`InventoryController`**,
  **`AdminInstallationController`**, **`NotificationsController`** (the list) — clamp via `query with { … }`.
  **(Inventory + installation-requests + the notifications list were added in council round 2 — the first pass
  missed the bound-record endpoints my param grep didn't surface.)**

Out of scope (deliberate): `AdminDashboardController` `count` endpoints + `Notifications` `recentCount` —
different "recent N" semantics (a 1..24 clamp would be wrong); auth-gated and bounded by own data.

## Test / verification
- `AdminPaginationBoundsTests` (integration, admin-authenticated): every admin list endpoint with
  `pageSize=0` / huge `pageNumber` / `pageSize=100000` → status < 500. Exact clamps pinned by `QueryBoundsTests`.
- `/acceptance`: drive the real admin API with `pageSize=100000` (cap 100 on the 2259-product dev DB) + `pageSize=0`.
- Cross-vendor Codex council on the diff.
