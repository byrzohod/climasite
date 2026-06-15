# ClimaSite — Bugs & Technical Debt Register

**Date:** 2026-06-11
**Sources:** `docs/project-plan/_review/bugs.md` (primary), `docs/project-plan/_review/architecture.md` (primary), with cross-references to `_review/testing.md`, `_review/security.md`, `_review/performance.md`, and `_review/devops.md`. All file paths are relative to the repo root unless noted.

**How to use this document:** This is the single register of known defects and structural debt as of 2026-06-11, written for a developer or AI agent picking up the project from documentation alone. Read "Likely bugs" top-to-bottom before touching checkout, payments, cart, or auth — the top items are launch blockers that the green test suites do **not** catch. Before editing any file listed in "Fragile areas", read that section's handling notes. The TODO triage table tells you which of the 24 in-code TODOs hide real broken behavior vs. harmless notes. "Verification" labels: items marked *independently verified* were confirmed by a second reviewer pass against the working tree; items marked *reviewer-verified* were confirmed once but had no independent verifier pass (the review only double-verified P0/P1); *Needs confirmation* means the claim could not be fully established from the repo. Where `docs/plans/18-project-completion.md` or `CLAUDE.md` claim a feature is "Complete", this register is the more recent and more accurate source — stale claims are called out explicitly.

---

## 1. Likely bugs (ordered by production impact)

### Priority index

| ID | Title | Priority | Risk | Verification |
|----|-------|----------|------|--------------|
| BUG-01 | `paymentIntentId` silently dropped — Stripe webhook can never reconcile any order | P0 | Critical (money, silent) | Independently verified |
| BUG-02 | Payment amount is client-supplied, charged in BGN vs EUR orders, excludes shipping | P0 | Critical (money, exploitable) | Independently verified |
| BUG-03 | Guest cart merge always returns 400 — guest cart vanishes on login | P1 | High (deterministic, conversion killer, silent) | Independently verified (P0→P1 adjusted) |
| BUG-04 | Card charged before order exists; no compensation; double-submit window | P1 | High (money) | Independently verified |
| BUG-05 | Stock decrement has no concurrency control — oversell; "reservations" don't exist | P1 | High (inventory integrity) | Independently verified |
| BUG-06 | Sale-price display inverted sitewide (higher price shown as the deal price) | P1 | High (pricing trust/compliance) | Independently verified |
| BUG-07 | Forgot-password never sends email; reset token logged at Information level | P1 | High (lockout + token leak) | Independently verified |
| BUG-08 | Concurrent 401s at token expiry log the user out (refresh storm) | P1 | Medium (session loss) | Independently verified |
| BUG-09 | Wishlist concurrency guard is per-process; multi-instance → unhandled 500s | P2 | Medium (latent, scaling) | Reviewer-verified |
| BUG-10 | Cart/wishlist product names ignore BG/DE translations | P2 | Medium (i18n DoD violation) | Reviewer-verified |
| BUG-11 | Cart/checkout render `$` (USD) via bare `\| currency` pipe | P2 | Medium (3 currencies in one journey) | Reviewer-verified |
| BUG-12 | `AverageRating`/`ReviewCount` hardcoded 0 in every product DTO — stars always empty | P2 | Medium (social proof dead) | Reviewer-verified |
| BUG-13 | Flat 20% VAT everywhere; DE legal rate is 19%; shipping untaxed | P2 | Medium (tax compliance) | Reviewer-verified |
| BUG-14 | Admin related-products search is a stub returning `[]` | P2 | Low (admin feature dead) | Reviewer-verified |
| BUG-15 | Guest checkout unreachable: route auth-guarded while backend supports it | P2 | Medium (docs/reality conflict) | Reviewer-verified |
| BUG-16 | Admin "notify customer" checkbox is a silent no-op | P2 | Medium (trust, silent) | Reviewer-verified |
| BUG-17 | Latent crash/FK traps if a product row is ever hard-deleted | P3 | Low (latent) | Reviewer-verified |
| BUG-18 | Stripe webhook returns 200 on "no matching order" — events permanently lost | P2 | Medium (after BUG-01 fix) | Reviewer-verified |

> The money-path bugs compound: BUG-02 charges the wrong amount, BUG-04 charges it at the wrong time, BUG-01 then loses the link between the charge and the order. Fix BUG-01 + BUG-02 together (the server must validate the intent's amount/currency against the order), then BUG-04, then BUG-18. **Blocking decision (Needs confirmation):** the intended store currency — EUR (all displays and orders) vs BGN (current Stripe charge) — must be decided before any money fix; see Open question 1 in `_review/bugs.md`.

---

### BUG-01 — Stripe payment never reconciled: `paymentIntentId` silently dropped

- **Priority/Risk:** P0 / Critical — every card order stays `Pending` forever after a successful charge; refund and payment-failure webhooks are no-ops; failure is silent (webhook returns 200, logs say "no matching order").
- **What happens:** Frontend sends `paymentIntentId` in the create-order body (`src/ClimaSite.Web/src/app/core/services/checkout.service.ts:87-107`, `checkout.component.ts:1185`), but `CreateOrderCommand` (`src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs:12-21`) has no such field — System.Text.Json drops it. `Order.SetPaymentInfo` exists (`src/ClimaSite.Core/Entities/Order.cs`) but is never called. The webhook matches on `o.PaymentIntentId == request.PaymentIntentId` (`src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs:39-49`), which is always null.
- **Fix:** Add `PaymentIntentId` (and `PaymentMethod`) to `CreateOrderCommand`, persist via `order.SetPaymentInfo(...)` in the handler, and verify the intent's amount/currency/status against the order total via `IPaymentService` before accepting. Effort: Small.
- **Acceptance:** Integration test — create order with `paymentIntentId`, assert persisted; simulated `payment_intent.succeeded` transitions the order to Paid.

### BUG-02 — Payment amount client-supplied, BGN vs EUR, excludes shipping

- **Priority/Risk:** P0 / Critical — (1) any authenticated user can confirm a €0.01 intent and place the order (price manipulation); (2) honest users underpay ~49% (X BGN charged for an X EUR order; BGN pegged 1.95583/EUR); (3) the charge omits the 5.99–15.99 shipping added server-side.
- **What happens:** `PaymentsController.CreatePaymentIntent` charges `request.Amount` with currency defaulting to `"bgn"`, no server-side validation (`src/ClimaSite.Api/Controllers/PaymentsController.cs:42-58`). Frontend passes the client-computed cart total and hardcodes `'bgn'` (`checkout.component.ts:1147-1150`). Cart total = subtotal + tax only (`cart.service.ts:31`); order total adds shipping (`CreateOrderCommand.cs:190-197`); orders hardcode EUR (`CreateOrderCommand.cs:163`). `StripePaymentService.cs:35` also truncates with `(long)(amount * 100)`.
- **Fix:** Compute the charge server-side from the server-calculated order total (subtotal + tax + shipping) in the store currency; never accept a client amount. Webhook must reject amount/currency mismatches. Effort: Medium. **Depends on the store-currency decision.**
- **Acceptance:** POST `/api/payments/create-intent` with an arbitrary amount is rejected/ignored; charge equals server order total including shipping.

### BUG-03 — Guest cart merge always 400s; guest cart vanishes on login

- **Priority/Risk:** P1 / High — 100% reproducible: every guest who logs in mid-shopping sees their cart vanish. Failure is silently swallowed (`console.warn` only). The review's verifier adjusted this P0→P1 because the guest cart row is never deleted server-side (items recoverable, not destroyed) and the fix is one line — but note the downgrade rationale "guest checkout exists as an alternative funnel" conflicts with BUG-15 (the checkout route is auth-guarded, so guests *cannot* check out without logging in). Treat as a deterministic launch blocker either way.
- **What happens:** Frontend POSTs `/api/cart/merge` with `{userId}` body + `X-Session-Id` header (`src/ClimaSite.Web/src/app/core/services/cart.service.ts:190-205`); backend requires non-nullable `[FromQuery] string guestSessionId` (`src/ClimaSite.Api/Controllers/CartController.cs:110-116`) → deterministic 400 under `[ApiController]` + NRT. `auth.service.ts:133-142` swallows it; `GetCartQuery.cs:27-38` then prefers the (empty) user cart. API tests pass because they call the endpoint correctly; no E2E covers login-merge (`_review/testing.md` finding 9).
- **Fix:** Change `CartService.mergeCart` to `POST /api/cart/merge?guestSessionId=${this.getSessionId()}` (drop the body; backend takes the user from JWT). Add the missing E2E. Effort: Small.
- **Acceptance:** E2E — guest adds 2 items, logs in, both items still visible; guest cart row removed server-side.

### BUG-04 — Charge before order, no compensation, double-submit window

- **Priority/Risk:** P1 / High — money captured even when order creation fails (stock-out, validation, network); because of BUG-01 the orphaned charge cannot even be found later. Double-click during the multi-second confirm phase can double-charge.
- **What happens:** `placeOrder()` confirms Stripe payment first (`checkout.component.ts:1174-1177`), then calls `createOrder` (`:1185`); order-failure path only sets an error message (`:1199-1201`). The button guard `checkoutService.isProcessing()` is set inside `createOrder()` (`checkout.service.ts:88`) — i.e., *after* intent creation + confirmation. `CancelPaymentIntentAsync` exists server-side but is never called from the frontend. No idempotency key anywhere.
- **Fix:** Set a processing flag at the top of `placeOrder()`; create the order (Pending) *before* confirming payment, or auto-cancel the intent when `createOrder` fails; add an idempotency key (e.g., unique index on `orders.payment_intent_id`). Effort: Medium. Builds on BUG-01/BUG-02.

### BUG-05 — Stock oversell under concurrent checkout; documented "reservations" are fictional

- **Priority/Risk:** P1 / High — HVAC units are high-value, low-stock; overselling the last unit during a promotion is realistic and expensive. CLAUDE.md claims "stock reservations" that do not exist (only a hardcoded `ReservedQuantity = 0` DTO field, `GetProductBySlugQuery.cs:96`).
- **What happens:** `CreateOrderCommand.cs:128-146,182,209` validates `StockQuantity` in memory then `variant.AdjustStock(-qty)` under default ReadCommitted — classic lost update. No concurrency token on `ProductVariant`, no `SELECT FOR UPDATE`, no atomic decrement. Same read-then-write pattern in admin `AdjustStockCommand.cs:47-62` / `InventoryController`.
- **Fix:** Either Postgres `xmin` concurrency token (`UseXminAsConcurrencyToken`) + retry on `DbUpdateConcurrencyException`, or atomic conditional `ExecuteUpdateAsync` with a `stock >= qty` guard, failing the order on 0 rows. Effort: Medium. Independent of payment fixes.
- **Acceptance:** Two parallel CreateOrder calls for the last unit — exactly one succeeds; stock never negative.

### BUG-06 — Sale-price display inverted sitewide

- **Priority/Risk:** P1 / High — every on-sale product shows the **higher** original price as the deal price across catalog, search, detail, cart, wishlist, recommendations, and even schema.org markup (`structured-data.service.ts:52,:94`). Seeded example: DualZone Pro shows "€1099.99" big with "€899.99" struck out; checkout charges 899.99. Customers pay *less* than displayed (why this is P1 not P0), but it is a consumer-protection/price-indication problem.
- **What happens:** Domain semantics are `BasePrice` = current price, `CompareAtPrice` = higher original (`Product.cs:284,293`). 14 DTO projections map `SalePrice = CompareAtPrice` (e.g., `GetProductsQueryHandler.cs:108`, `SearchProductsQuery.cs:113`, `AddToCartCommand.cs:175`, new `WishlistApplicationService.cs:176`), while UI templates render `salePrice` as the active price (`product-card.component.ts:154-158`, `product-detail.component.ts:87-91`). `GetBrandBySlugQuery.cs:86` alone uses the opposite (correct) mapping — the contract is internally inconsistent. Zero tests assert SalePrice semantics.
- **Fix:** Pick one contract (recommended: SalePrice = current discounted price, matching FE templates and the brand query), centralize the mapping in **one shared helper** instead of 14 copy-pasted projections, and add a snapshot/unit test on it. Effort: Medium. Coordinate with the new wishlist mapper.

### BUG-07 — Forgot-password never sends email; reset token logged

- **Priority/Risk:** P1 / High — production users who forget their password are permanently locked out while the UI reports success; anyone with log access can take over any account that requested a reset (overlaps `_review/security.md` finding 2).
- **What happens:** `ForgotPasswordCommandHandler.cs:34-42` generates the token, logs it at Information level (`"... Token: {Token}"`), and the email send is commented out. `EmailService.SendPasswordResetEmailAsync` is fully implemented (`src/ClimaSite.Infrastructure/Services/EmailService.cs:51-58`) with **zero production callers** — as is the entire transactional email layer (welcome, order confirmation; `_review/architecture.md` finding 1). Caveat: `EmailService.cs:23` defaults `Email:UsePlaceholder` to true, so wiring alone isn't enough — config must flip too.
- **Fix:** Inject `IEmailService`, call `SendPasswordResetEmailAsync` with a reset URL, remove the token from the log line; also wire `SendOrderConfirmationEmailAsync` (CreateOrder or webhook success) and `SendWelcomeEmailAsync` (register). Effort: Small.

### BUG-08 — Refresh-storm guard logs users out on concurrent 401s

- **Priority/Risk:** P1 / Medium — random session loss every ~15 min (token expiry) whenever multiple requests are in flight, e.g., mid-checkout.
- **What happens:** `AuthService.refreshToken()` returns `throwError('Token refresh already in progress')` when `_isRefreshing` is true (`auth.service.ts:202-208`); the interceptor treats any refresh error as fatal and clears auth state (`auth.interceptor.ts:49-55`). A page load with an expired token fires parallel 401s; all but the first hit the guard and clear auth while the real refresh may still succeed. A startup-restore path also races interceptor-driven refreshes. The correct pattern already exists in-repo: `WishlistService.fetchInFlight$` (`wishlist.service.ts:242-278`).
- **Fix:** Share a single refresh observable (`shareReplay`); concurrent callers wait and retry with the new token; only `clearAuthState` when the shared refresh itself fails. Effort: Small.

### BUG-09 — Wishlist concurrency guard is per-process only

- **Priority/Risk:** P2 / Medium — works correctly single-instance today (10/10 new unit tests pass), but the moment Railway runs >1 replica, concurrent adds race past the static `SemaphoreSlim` dictionary to the DB unique index (`WishlistConfiguration.cs:107`), and `AddToWishlistCommand.cs:66-73`'s check-then-insert throws an **unhandled `DbUpdateException` → 500** instead of the idempotent 200. The semaphore dictionary (`WishlistApplicationService.cs:10,24-37`) also never evicts entries (slow leak, one `SemaphoreSlim` per user forever).
- **Fix:** Catch the unique-violation `DbUpdateException` in the add handler and return the existing wishlist DTO — make the DB constraint the real guard — then delete (or demote to fast-path) the semaphore. Effort: Small. **Needs confirmation:** Railway replica count (`railway.toml` suggests single instance; could not verify from repo).
- **Note:** Plan 18 (`docs/plans/18-project-completion.md`, WISH-100) marks this slice DONE with "per-user mutation serialization" as a feature — that claim is true only for single-instance deployment.

### BUG-10 — Cart/wishlist product names ignore translations

- **Priority/Risk:** P2 / Medium — violates the project's own Definition of Done ("works in all languages"). A BG user sees Bulgarian names in the catalog and English names in cart/wishlist for the same product.
- **What happens:** Catalog queries translate via `GetTranslatedContent(request.LanguageCode)` (`GetProductsQueryHandler.cs:100`); cart and wishlist mappers use raw `product.Name` and accept no language code (`AddToCartCommand.cs:171`, `MergeGuestCartCommand.cs:152`, `GetCartQuery.cs`, `WishlistApplicationService.cs:169`). Cart/wishlist services send no `?lang=` param.
- **Fix:** Thread `LanguageCode` into cart/wishlist queries (same `?lang=` convention as `product.service.ts:40-42`) and use `GetTranslatedContent` in the mappers. Effort: Medium. Cheapest to fix while the wishlist mapper is still new code.

### BUG-11 — Cart/checkout show USD via bare `| currency` pipe

- **Priority/Risk:** P2 / Medium — one purchase journey shows €899.99 (product page), $899.99 (cart/checkout: `checkout.component.ts:294,326,334,342,348,352`; `cart.component.ts:73-76,116,136-157`), charges 899.99 **BGN** (BUG-02), confirms €899.99.
- **Fix:** Set `DEFAULT_CURRENCY_CODE` in app config or pass `'EUR'` to every pipe; grep for bare `| currency` as the acceptance check. Effort: Small. Depends on the store-currency decision.

### BUG-12 — Star ratings always empty: `AverageRating`/`ReviewCount` hardcoded 0

- **Priority/Risk:** P2 / Medium — Reviews & Ratings is documented "Complete" but every product card, search result, featured slot, and wishlist item shows zero stars regardless of actual reviews. Real aggregates exist only in `GetProductReviewsQuery.cs:108` (detail-page reviews section).
- **Where:** `GetProductsQueryHandler.cs:113-114`, `GetProductBySlugQuery.cs:67`, `SearchProductsQuery.cs:119`, `GetFeaturedProductsQuery.cs:57`, `WishlistApplicationService.cs:179-180`; rendered by `product-card.component.ts:140-141`.
- **Fix:** Project approved-review aggregates into product queries (subquery, or denormalized columns updated on review approval to avoid N+1). Effort: Medium.

### BUG-13 — Flat 20% VAT; DE legal rate is 19%; shipping untaxed

- **Priority/Risk:** P2 / Medium — tax-compliance issue for a platform explicitly targeting DE; EU rules generally require VAT on shipping too.
- **Where:** `AddToCartCommand.cs:185-186` (TODO acknowledges DE=19%), `MergeGuestCartCommand.cs:168`, `CreateOrderCommand.cs:200-202` (tax on subtotal only; shipping added untaxed; shipping table hardcoded `:190-196`).
- **Fix:** Country-based VAT table applied to (subtotal + shipping) at order time from the shipping address; label cart tax as estimated. Effort: Medium. Shares store-configuration work with API-014.

### BUG-14 — Admin related-products search always returns empty

- **Priority/Risk:** P2 / Low — admins cannot curate related products (Plan 17 documented "Complete"). `searchProducts()` ends with `this.searchResults.set([])` unconditionally (`src/ClimaSite.Web/src/app/features/admin/products/components/related-products-manager/related-products-manager.component.ts:515-526`).
- **Fix:** Wire the existing GET `/api/products` search with debounce. Effort: Small.

### BUG-15 — Guest checkout unreachable despite backend + docs support

- **Priority/Risk:** P2 / Medium — `app.routes.ts:82-89` guards `checkout` with `authGuard`, while `OrdersController.cs:28-39` is `[AllowAnonymous]` with guest-session support and CLAUDE.md states "guest checkout stores email". `PaymentsController` is `[Authorize]`, so guests would 401 on payment anyway. Either the business rule or the code is wrong; the half-wired backend guest path is untested dead code meanwhile.
- **Fix:** **Decision needed** — enable guest checkout (remove guard, allow anonymous create-intent tied to a guest cart, *after* BUG-01/02 fixes) or delete the backend guest path and fix the docs. Effort: Medium.

### BUG-16 — Admin "notify customer" is a silent no-op

- **Priority/Risk:** P2 / Medium — admins believe customers were emailed about shipping/cancellation; nothing is sent. `UpdateOrderStatusCommand.cs:73` has only `// TODO: If NotifyCustomer is true, send email notification`.
- **Fix:** Invoke `IEmailService` (and the in-app `Notification` entity) when `NotifyCustomer` is true; until then hide/disable the checkbox. Effort: Small. Part of Plan 12/Plan 18 NOT-* work.

### BUG-17 — Latent traps on product hard-delete

- **Priority/Risk:** P3 / Low (latent — deletion is soft today via `DeleteProductCommand.cs:42-44`). (1) Cart mapping uses `products.First(...)` → `InvalidOperationException` if the row is gone (`AddToCartCommand.cs:160`, `MergeGuestCartCommand.cs:143`); (2) `order_items` FKs configured `DeleteBehavior.SetNull` on **non-nullable** Guid columns (`OrderConfiguration.cs:214-222` vs `OrderItem.cs:6-7`) → NOT NULL violation; (3) CartItem→Product is Cascade, silently emptying carts.
- **Fix:** Make `OrderItem.ProductId`/`VariantId` nullable (rows already snapshot name/sku/price); `FirstOrDefault` + "unavailable" item in cart mapping. Effort: Small (needs a migration).

### BUG-18 — Stripe webhook acknowledges unmatched events with 200

- **Priority/Risk:** P2 / Medium (becomes observable only after BUG-01 is fixed) — `WebhooksController.cs:91-93` returns 200 when no order matches, so Stripe never retries; a webhook arriving before the order row exists is permanently lost.
- **Fix:** Return a retryable status (or persist unmatched events for later reconciliation) for "order not yet created". Effort: Small. Sequence after BUG-01.

### Minor/latent observations (P3, reviewer-verified, from `_review/bugs.md`)

| Item | Location | Note |
|------|----------|------|
| `WishlistItem.PriceWhenAdded` never populated | `AddToWishlistCommand.cs:72`; `SetPriceWhenAdded` has no callers | NotifyOnSale/price-drop UX has no data to work with |
| `clearWishlist` failure swallowed | `wishlist.service.ts:159-162` | Optimistic clear; server items reappear on next fetch |
| Wishlist guest→login merge aborts on first bad item | `wishlist.service.ts:259-262` (`concatMap`) | One 400 skips all remaining items that cycle |
| EF retry-strategy closures re-Add tracked entities | `CreateOrderCommandHandler`, `WishlistApplicationService` | Theoretical unless a retrying execution strategy is enabled; the `strategy is null` checks are dead code (see TD-12) |
| Order number collision | `CreateOrderCommand.cs:221-226` | `ORD-yyyyMMdd-HHmmss` + 4 random chars ≈ 1/65k per second |
| `CartItemDto.sku` typed non-optional in TS but nullable in C# | `Program.cs:116` (`WhenWritingNull`) | Runtime `undefined` vs typed `string` |
| Cart `shipping` always 0 from backend | shipping only added at order time | Misleading "free shipping" impression in cart UI |

---

## 2. TODO/FIXME triage (all 24 in-code hits)

From `_review/bugs.md`. **Four TODOs mask genuinely broken user-facing behavior** (bold below).

| # | Location | Text (abridged) | Triage |
|---|----------|-----------------|--------|
| 1 | `related-products-manager.component.ts:524` | Implement actual product search | **Hidden broken feature** → BUG-14 |
| 2 | `auth.service.ts:72` | AUTH-015 session expiry warnings | Harmless UX note (refresh-storm BUG-08 is separate) |
| 3 | `profile.component.ts:69` | phone validation pattern | Harmless |
| 4–5 | `ForgotPasswordCommandHandler.cs:14,36` | Inject email service / send reset link | **Hidden broken flow** → BUG-07 |
| 6 | `UpdateOrderStatusCommand.cs:73` | NotifyCustomer email | **Hidden no-op** → BUG-16 |
| 7 | `CreateOrderCommand.cs:63` | Add ILogger | Harmless |
| 8 | `CreateOrderCommand.cs:162` | Currency from store config | **Masks real bug** → part of BUG-02 |
| 9 | `CreateOrderCommand.cs:185` | API-014 hardcoded shipping | Known debt; interacts with BUG-02 |
| 10 | `CreateOrderCommand.cs:200` | VAT per country | Money/legal gap → BUG-13 |
| 11 | `AddToCartCommand.cs:139` | Guest cart cleanup job | P3 — DB bloat only; expiry filtered at read (`GetCartQuery.cs:37`); blocked by TD-09 (no job infra) |
| 12 | `AddToCartCommand.cs:185` | VAT configurable | Same as #10 |
| 13 | `Program.cs:108` | API-012 RFC7807 ProblemDetails | Consistency debt → TD-06 |
| 14 | `CreateReviewCommand.cs:82` | single-query optimization | Harmless perf note |
| 15–16 | `AdminCategoriesController.cs:104,113` | API-008 N+1 reorder | P3 perf debt, admin-only |
| 17 | `QuestionsController.cs:47` | rate limiting for questions | Real gap — anonymous spam vector on a public endpoint, P2/P3 |
| 18–19 | `AdminReviewsController.cs:132,144` | API-009 N+1 bulk moderate | P3 perf debt |
| 20 | `AdminProductsController.cs:72` | slug duplication | Harmless |
| 21–24 | `AdminQuestionsController.cs:201,212,235,246` | API-010 N+1 moderation | P3 perf debt |

---

## 3. Technical debt items

Primary source: `_review/architecture.md`. Performance-specific debt (output-cache invalidation, ILIKE search, missing pageSize clamps, no SSR, image pipeline) is catalogued in `_review/performance.md`; test-coverage debt in `_review/testing.md`; CI/deploy debt in `_review/devops.md`.

| ID | Debt | Priority | Risk | Detail & fix |
|----|------|----------|------|--------------|
| TD-01 | **Transactional email layer fully orphaned** | P1 | High | `EmailService.cs` implements welcome/reset/order-confirmation; zero call sites (only GDPR delete uses generic `SendEmailAsync`). Also `Email:UsePlaceholder` defaults to true (`EmailService.cs:23`). Wire BUG-07/BUG-16 paths; effort Small. |
| TD-02 | **Dead duplicate Auth command tree; unit tests test the dead copy** | P1 | High | Live tree: `src/ClimaSite.Application/Auth/`. Dead tree: `src/ClimaSite.Application/Features/Auth/` — and `tests/ClimaSite.Application.Tests/Features/Auth/Commands/*` import the **dead** one, so login/register/refresh handlers have zero real unit coverage (mitigated by 22 `AuthControllerTests` integration tests). Worse, `src/ClimaSite.Application/Features/AGENTS.md:70` wrongly declares `Features/Auth/` canonical. Delete the dead tree, retarget tests, fix AGENTS.md **before any further auth work**. Effort Medium. |
| TD-03 | **Repository/UnitOfWork layer is 100% dead scaffolding** (~700 lines) | P2 | Medium | `Core/Interfaces/I*Repository.cs`, `IUnitOfWork` (no implementation at all), `Infrastructure/Repositories/*`, DI regs at `Infrastructure/DependencyInjection.cs:84-86` — zero consumers. Real pattern: 102/117 handlers use `IApplicationDbContext` directly. Delete and update CLAUDE.md/AGENTS.md, which still document the dead pattern. Effort Small. |
| TD-04 | **CachingBehavior never registered — Redis query caching silently inactive** | P2 | Medium | `Application/DependencyInjection.cs:21-22` registers only Validation+Logging; 14 queries implement `ICacheableQuery` inertly. Partly mitigated by a 5-min output cache on anonymous GETs (`Program.cs:245-250`, `:302`) — which itself has zero invalidation (see `_review/performance.md` finding 3). Either register the behavior **with an invalidation strategy** or delete it; do not leave half-wired. Effort Medium. |
| TD-05 | **EF migrations split across two live folders, single snapshot in the older one** | P2 | Medium | `Infrastructure/Migrations/` (2 migrations + the only `ApplicationDbContextModelSnapshot.cs`) vs `Infrastructure/Data/Migrations/` (6 later migrations, no snapshot). One logical chain; deleting either folder breaks applied-migration history. Consolidate via pure move + namespace edit (migration IDs unchanged), in isolation, using the project's db-migrate skill. Effort Small. |
| TD-06 | **Three incompatible API error shapes; CLAUDE.md falsely claims ProblemDetails** | P2 | Medium | (1) `ExceptionHandlingMiddleware.cs:46-58` `{status,message,detail}`; (2) controllers' `BadRequest(new { message })` (e.g., `WishlistController.cs:42-67`); (3) default `ValidationProblemDetails` for binding errors. Handler error style also split (41 files `Result<T>` vs 6 throwing). Adopt RFC 7807 once, in a coordinated FE+BE change (see RF-06). Effort Large. |
| TD-07 | **Five 1,000–1,600-line single-file god components** | P2 | Medium | `header.component.ts` (1,566), `product-list` (1,435), `checkout` (1,231 — money path), `order-details` (1,082), `product-detail` (1,030). Logic is modest (150–330 lines); the bulk is inline template + ~800–1,000 lines inline SCSS that defeats stylelint/IDE tooling and breeds merge conflicts. See RF-04. |
| TD-08 | **Hand-mirrored TS DTOs, no OpenAPI codegen** | P2 | Medium | Every TS model hand-written across 17 services despite Swagger being enabled; wishlist contract carries duplicate (`ImageUrl`/`PrimaryImageUrl`) and never-populated (`averageRating`/`reviewCount`) fields (`WishlistApplicationService.cs:173-180`, `wishlist.service.ts:10-52`). Two contract-drift bugs above (BUG-01, BUG-03) are exactly this failure mode. See RF-07. |
| TD-09 | **No background-job infrastructure at all** | P2 | High (blocker) | Zero `IHostedService`/`BackgroundService`/Hangfire/Quartz hits. Hard blocker for Plan 12 notifications, wishlist `NotifyOnSale` (data stored, nothing scans it), guest-cart cleanup (TODO #11). Needs an ADR — per the user's global rules, **the user decides** the mechanism (BackgroundService + DB outbox vs RabbitMQ from shared-infra vs Hangfire). Precedes Plan 18 NOT-* work. |
| TD-10 | **Six of eight shared directives are dead code** | P2 | Low | `magnetic-hover`, `split-text`, `scroll-progress`, `animate-on-scroll`, `count-up`, `optimized-image` — zero usages; only `RevealDirective` is live. CLAUDE.md Quick Reference still advertises `CountUpDirective`. Delete + fix docs; this is the unfinished tail of Animation Audit 21F (Plan 18 ANIM-*). Effort Small. |
| TD-11 | **Mapster registered but completely unused** | P3 | Low | `MappingConfig.cs` + DI lines; zero `IRegister`/`.Adapt` anywhere. Manual `Select` projections are the real (good, EF-translatable) convention. Remove package + registration; document the convention. Effort Small. |
| TD-12 | **MockDbContext fake warps production code** | P3 | Medium | `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs` (312 lines, 29 hand-mirrored DbSets): no-op transactions, `CreateExecutionStrategy()` returns null — which forced the dead `strategy is null` branch into `WishlistApplicationService.cs:30`. Cannot catch constraint/concurrency bugs (exactly the BUG-05/BUG-09 class). Add a real-provider fixture (SQLite in-memory or Testcontainers) for transaction-using handlers; delete the null-branch. |
| TD-13 | **Core domain depends on ASP.NET Identity** | P3 | Low | `ApplicationUser : IdentityUser<Guid>` (`Core/Entities/ApplicationUser.cs`). Pragmatic and common; record as a deliberate ADR exception, do not refactor. |
| TD-14 | **Application folder taxonomy split** | P3 | Low | Top-level `Auth/` (handler-per-file) vs `Features/*` (single-file command+validator+handler). Combined with TD-02 there are three places "LoginCommand" lives. Unify after TD-02. |
| TD-15 | **Agent-config tree drift** | P3 | Low | `.claude/skills/`, `.codex/skills/climasite-*`, `.opencode/skills/` have diverging copies of the same procedures (e.g., db-migrate). Pick one canonical source + sync. |
| TD-16 | **CLAUDE.md/AGENTS.md describe a stack that does not exist** | P2 | High (agent-driven repo) | Documented but fictional: TypeScript Playwright suite + `npx playwright test` + `fixtures/test-data-factory.ts` (actual: Playwright **for .NET**, `dotnet test tests/ClimaSite.E2E`, `Infrastructure/TestDataFactory.cs`); `tests/ClimaSite.Web.Tests` (doesn't exist); repository pattern; ProblemDetails; `Accept-Language` header (reality: `?lang=` query param); "stock reservations". Root `dotnet test` also runs E2E (needs live servers), so the "NON-NEGOTIABLE" post-implementation command fails as written. One documentation pass after the TD-03/TD-06 decisions; see also `_review/docs.md`. |

---

## 4. Fragile areas (handle with care)

High-blast-radius zones from `_review/architecture.md`. Read this before editing.

1. **Stripe payment path** — `src/ClimaSite.Api/Controllers/WebhooksController.cs`, `PaymentsController.cs`, `src/ClimaSite.Infrastructure/Services/StripePaymentService.cs`, `CreateOrderCommand.cs`. Money + webhook signatures + the BUG-01/02/04/18 cluster — fixes here must land as a coordinated set with the store-currency decision made first. Note: this path currently has **zero integration tests and the only full-order E2E deliberately avoids card payment** (`_review/testing.md` findings 2–3) — add tests with (or before) any fix. Use the security-review and deploy-checklist skills.
2. **Auth token chain** — `Infrastructure/Services/TokenService.cs`, `Application/Auth/Handlers/RefreshTokenCommandHandler.cs`, `src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts`, `auth.service.ts`. Recently refactored for a circular dependency; refresh races (BUG-08) are easy to reintroduce, and the **live** handlers have no unit tests (TD-02) — fix the test targeting before touching the code.
3. **EF migration chain** — both `src/ClimaSite.Infrastructure/Migrations/` and `src/ClimaSite.Infrastructure/Data/Migrations/` are live with a single snapshot (TD-05). Never rename migration IDs, delete files, or "tidy" these folders except via the dedicated consolidation. Migrations also run at app startup with no rollback story (`_review/devops.md` finding 3).
4. **ExceptionHandlingMiddleware error contract** — `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs`. The `{status,message,detail}` shape is parsed by frontend services; changing it outside the coordinated ProblemDetails migration (RF-06) silently breaks error UX.
5. **Guest-cart merge + DataSeeder** — `Application/Features/Cart/Commands/MergeGuestCartCommand.cs` and `Infrastructure/Data/DataSeeder.cs` (833 lines). The merge is a core business rule with the live BUG-03 sitting on it; the seeder is an implicit contract for E2E tests and dev environments (and seeds `Admin123!` unconditionally — `_review/devops.md` finding 1). Also note `MergeGuestCartCommand` lets any authenticated user merge any guessable `guestSessionId` (client-generated, `cart.service.ts:47`) — entropy flagged for the security review.
6. **New wishlist slice (uncommitted branch `feature/plan18-wishlist-completion`)** — `WishlistApplicationService.cs` and friends are the cheapest they will ever be to fix (BUG-09, BUG-10, BUG-12 wishlist fields, TD-12 null-branch) **before merge**; after merge each becomes a migration/compat question.

---

## 5. Refactoring recommendations (incremental — no rewrites)

Ordered by value-to-risk ratio. None of these change user-visible behavior except where noted.

| ID | Refactor | Priority | Risk of doing it | Notes |
|----|----------|----------|------------------|-------|
| RF-01 | **Delete verified dead code** (~1,800 lines): repository layer + `IUnitOfWork` (TD-03), dead `Features/Auth` tree (TD-02 — retarget the 3 test files first), Mapster (TD-11), 6 unused directives (TD-10), `environment.prod.ts` (`_review/devops.md` finding 13) | P1–P2 | Low — all zero-consumer, verified by grep | Do TD-02 first; it unblocks honest auth test coverage. Acceptance: solution builds, greps return zero hits, docs updated in the same PR. |
| RF-02 | **Centralize price mapping** — one shared `ProductPricingMapper` (or extension) replacing 14 copy-pasted `SalePrice` projections | P1 | Medium — touches every product query | This *is* the BUG-06 fix done right; add the missing SalePrice-semantics unit test. Includes the structured-data service. |
| RF-03 | **Consolidate EF migrations into `Data/Migrations/`** (TD-05) | P2 | Low if done as pure move + namespace edit; high if botched | Isolated PR, no schema changes bundled; verify with `dotnet ef migrations list` + clean-DB `database update`. |
| RF-04 | **Mechanically split the five god components** (TD-07): extract `templateUrl`/`styleUrls` first (zero behavior change), then extract children (header: mega-menu, search overlay, account dropdown; checkout: payment step, address step). Add ESLint `max-lines` ~600 warning for new files | P2 | Low (mechanical step) / Medium (child extraction) | Do **after** the wishlist branch merges and after the BUG-04 checkout fix to avoid conflicts on `checkout.component.ts`. |
| RF-05 | **Replace the wishlist semaphore with DB-constraint-backed idempotency** (BUG-09): catch unique-violation, return existing DTO, delete the static dictionary and the `strategy is null` branch; add a real-EF-provider test fixture (TD-12) | P2 | Low | Cheapest pre-merge. Both required unique indexes already exist (`WishlistConfiguration.cs:46` `Wishlists(user_id)`, `:107` `WishlistItems(wishlist_id, product_id)`) — `_review/architecture.md` finding 8's "add indexes via migration" step is already done; only the exception handling is missing. The fixture then also serves CreateOrder transaction tests. |
| RF-06 | **Single error contract (RFC 7807)**: convert middleware to ProblemDetails / `IExceptionHandler` + `AddProblemDetails`, add a base-controller helper mapping `Result<T>` failures, update the frontend error interceptor — one coordinated change (TD-06) | P2 | High — every FE error path parses the old shapes | Until done, fix CLAUDE.md to document the real `{status,message,detail}` contract instead. Do not change shapes piecemeal. |
| RF-07 | **Generate TS models from Swagger** (`openapi-typescript`) in CI and diff against committed models (TD-08) | P2 | Low | Would have caught BUG-01 and BUG-03 at build time. Start by deleting never-populated wishlist fields or populating them (BUG-12). |
| RF-08 | **Background-job ADR + email outbox processor** (TD-09) | P2 | Medium | User decision on mechanism required first (global rule: user decides infra). The outbox processor is the prerequisite for TD-01/BUG-16/Plan 12. |
| RF-09 | **Unify Application taxonomy** — move live `Auth/` under `Features/` after TD-02 (TD-14) | P3 | Low — pure file moves + namespaces | Sequence strictly after RF-01's dead-tree deletion. |
| RF-10 | **Documentation truth pass on CLAUDE.md/AGENTS.md** (TD-16) | P1 (cheap, high leverage in an agent-driven repo) | None | Write once, after the TD-03/TD-06 direction decisions, covering: real E2E commands, `?lang=` convention, no reservations, real error contract, real data-access pattern, living key files only. Must also fix `src/ClimaSite.Application/Features/AGENTS.md`'s "KNOWN ISSUE" note, which declares the **dead** `Features/Auth/` tree canonical (TD-02). See `_review/docs.md` for the full drift inventory. |

---

## 6. Open questions blocking fixes (Needs confirmation)

1. **Store currency** — EUR or BGN? Blocks BUG-02, BUG-11, BUG-13 ordering. (Owner: user decision.)
2. **Railway replica count / multi-instance plans** — determines urgency of BUG-09 and TD-09 design. Not verifiable from repo.
3. **Guest checkout in scope?** — decides BUG-15 direction (enable vs delete the half-wired path).
4. **Why is `Checkout_SavedAddress_CanBeUsed` NotExecuted in every recorded E2E run** despite being a plain `[Fact]`? (`tests/ClimaSite.E2E`; see `_review/bugs.md` test-results table.)
5. **Background-job mechanism** — BackgroundService + outbox vs RabbitMQ (already in shared infra) vs Hangfire (RF-08).

## 7. Relationship to existing plans

- `docs/plans/18-project-completion.md` remains the active execution plan; its Phase 0/1 and WISH-* completions are genuine, **but** its "Wishlist DONE" status should be read with the BUG-09/BUG-10/BUG-12 caveats above, and its Phase 4 coverage targets are currently unmeasured (no coverage gate exists — `_review/testing.md` finding 6). The NOT-* (notifications) tasks are blocked by TD-09/RF-08.
- `docs/plans/12-notifications-system.md` cannot start until the background-job ADR (RF-08) lands.
- The CLAUDE.md status table ("Inventory Management: Complete" with reservations, "Reviews & Ratings: Complete", "Performance Optimizations: Complete", "Cart merges when guest user logs in") is contradicted by BUG-05, BUG-12, TD-04, and BUG-03 respectively — treat this register as authoritative until the RF-10 documentation pass.
