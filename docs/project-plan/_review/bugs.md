# Bugs & Reliability — Review Findings (2026-06-11)

## Summary

The codebase is well-structured (Clean Architecture, CQRS, consistent Result pattern) and the latest local test runs are green (206/207 E2E, all unit/API suites passing, new wishlist tests 10/10, solution builds clean) — but the payment/checkout pipeline has three compounding production blockers that the test suites do not catch.

First, the Stripe charge amount is supplied by the client, charged in BGN while every order and price display is EUR, and excludes shipping — so real charges are wrong in unit, value, and tamper-resistance. Second, the `paymentIntentId` the frontend sends on order creation is silently dropped because `CreateOrderCommand` has no such field, so `Order.PaymentIntentId` is always null and the Stripe webhook can never reconcile a payment — no order will ever be auto-marked Paid/PaymentFailed/Refunded. Third, the guest-cart merge call is a frontend/backend contract mismatch (body vs. required query param) that always returns 400, is swallowed with a `console.warn`, and causes guest cart contents to vanish from the UI on login.

Beyond payments, there is a sitewide sale-price inversion (`SalePrice` is mapped from `CompareAtPrice`, the higher original price, while the UI renders `salePrice` as the deal price), stock decrement with no concurrency control (oversell under concurrent checkout; the documented "stock reservations" do not exist), a forgot-password flow that never sends email and logs the reset token, and an auth-refresh design that logs users out when multiple 401s race.

The new wishlist work is comparatively solid: the DTO contract matches 1:1, a DB unique index backs the concurrent-add path, and mutations are serialized — though only per-process, so a multi-instance deployment degrades to unhandled 500s on races. The 24 TODOs are mostly honest notes, but four of them mask genuinely broken user-facing behavior. Recorded E2E failures (41 in `e2e-current.trx`) were fixed by the final run on 2026-06-06; the only persistent skip is `Checkout_SavedAddress_CanBeUsed`.

## Findings

### 1. Stripe payment is never reconciled to orders: paymentIntentId silently dropped, webhook can never match

- **Finding:** The `paymentIntentId` sent by the frontend on order creation is silently dropped (no matching field on `CreateOrderCommand`), so `Order.PaymentIntentId` is always null and the Stripe webhook can never match a payment to an order — no order is ever auto-marked Paid/PaymentFailed/Refunded.
- **Category:** broken-flow
- **Severity/Priority:** P0 (Critical) — verification: confirmed
- **Evidence:** Frontend sends `paymentIntentId` in the create-order body (`src/ClimaSite.Web/src/app/core/services/checkout.service.ts:87-107`, `checkout.component.ts:1185`), but the endpoint binds `[FromBody] CreateOrderCommand` (`src/ClimaSite.Api/Controllers/OrdersController.cs:32`) whose record has no `PaymentIntentId` or `PaymentMethod` field (`src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs:12-21`) — System.Text.Json silently ignores the extra fields. `Order.SetPaymentIntent` exists (`src/ClimaSite.Core/Entities/Order.cs:22,187`) but grep shows it is never called from any handler. The webhook looks up orders by `o.PaymentIntentId == request.PaymentIntentId` (`src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs:39-49`), which is always null, so `payment_intent.succeeded`/`failed` and `charge.refunded` events never match any order and are acknowledged with "no matching order".
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/OrdersController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/checkout.service.ts`
- **Why it matters:** Every card order stays Pending forever even after a successful charge; refund and payment-failure webhooks are no-ops. Fulfillment, "verified purchase" logic, and customer order status all depend on this transition. This is a silent failure — webhooks return 200 and logs say "no order found".
- **Recommended fix:** Add `PaymentIntentId` (and `PaymentMethod`) to `CreateOrderCommand`, call `order.SetPaymentIntent(...)` in the handler, and optionally verify the intent's amount/currency/status against the order total via `IPaymentService` before accepting the order. Estimated effort: Small.
- **Acceptance criteria:** Integration test: create order with `paymentIntentId`, assert it is persisted; post a simulated `payment_intent.succeeded` webhook for that intent and assert the order transitions to Paid. E2E checkout shows the order leaving Pending.
- **Dependencies or follow-up:** Should be fixed together with the amount/currency finding so the server can validate the intent against the order total.
- **Confidence:** verified. Two verifier passes confirmed every link in the chain: `Program.cs:113-116` sets no `UnmappedMemberHandling` so the fields are silently dropped; the only setter, `Order.SetPaymentInfo` (the finding misnames it `SetPaymentIntent` but cites the correct line), is called only from test files (which mask the gap by pre-seeding `PaymentIntentId`); the webhook 200-acks unmatched events and the only alternate path to Paid is manual admin status update. Pre-launch status does not downgrade severity — the core money flow fails silently on the happy path; P0 upheld.

### 2. Payment amount is client-supplied, charged in BGN against EUR order totals, and excludes shipping

- **Finding:** The Stripe payment intent is created from a client-supplied amount, hardcoded to BGN while orders and all price displays are EUR, with no server-side validation against any cart/order — and the charged cart total omits the server-side shipping cost.
- **Category:** security/money-correctness
- **Severity/Priority:** P0 (Critical) — verification: confirmed
- **Evidence:** `PaymentsController.CreatePaymentIntent` charges `request.Amount` with currency defaulting to "bgn" with no server-side validation against any cart/order (`src/ClimaSite.Api/Controllers/PaymentsController.cs:42-58`). The frontend passes the client-computed cart total and hardcodes 'bgn' (`src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts:1147-1150`). `cartService.total()` is `CartDto.Total = subtotal + tax` only (`cart.service.ts:31`; `AddToCartCommand.cs` `MapCartToDto`), while the order total adds shipping of 5.99–15.99 server-side (`CreateOrderCommand.cs:190-197`). Orders are hardcoded to EUR (`CreateOrderCommand.cs:163`) and all prices render with `currency:'EUR'`. `StripePaymentService` truncates with `(long)(amount * 100)` (`StripePaymentService.cs:35`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Services/StripePaymentService.cs`
- **Why it matters:** Three money bugs in one flow: (1) any authenticated user can create and confirm an intent for €0.01 via the API and then place the order — classic price manipulation; (2) honest users are charged X BGN (~0.51 EUR/BGN) for an X EUR order, ~49% underpayment; (3) even ignoring currency, the charge omits shipping. Real revenue loss on every card order.
- **Recommended fix:** Compute the charge server-side: create the payment intent from the server-calculated order total (subtotal + tax + shipping) in the store currency, never from a client-supplied amount. Either create the order first (Pending) and derive the intent from it, or recompute the cart total server-side in create-intent and store the expected amount for webhook verification. Estimated effort: Medium.
- **Acceptance criteria:** API test: POST `/api/payments/create-intent` with arbitrary amount is rejected or ignored; charge amount equals server order total in the configured currency including shipping; webhook handler rejects intents whose amount/currency mismatch the order.
- **Dependencies or follow-up:** Pairs with the paymentIntentId persistence fix; decide store currency (EUR vs BGN) first — also affects the `| currency` USD display finding.
- **Confidence:** verified. Two verifier passes confirmed P0: the only check is `Amount > 0` with no FluentValidation validator for `CreatePaymentIntentRequest`, and the webhook marks orders Paid on PaymentIntentId match alone with zero amount/currency verification — so the €0.01 price manipulation, ~49% BGN/EUR underpayment, and uncharged shipping are all real and unguarded; pre-launch/test-mode status is temporary and does not justify a downgrade.

### 3. Guest cart merge on login always fails (400) due to FE/BE contract mismatch; guest cart items vanish after login

- **Finding:** The frontend calls the cart merge endpoint with a body and header while the backend requires a non-nullable `[FromQuery]` parameter, so every merge request 400s; the failure is silently swallowed and guest cart items disappear from the UI after login.
- **Category:** broken-flow/contract-drift
- **Severity/Priority:** P1 (High) — verification: adjusted
- **Evidence:** Frontend: `this.http.post(`${apiUrl}/merge`, { userId }, { headers })` — `guestSessionId` only in the `X-Session-Id` header, none in the URL (`src/ClimaSite.Web/src/app/core/services/cart.service.ts:190-205`). Backend: `MergeGuestCart([FromQuery] string guestSessionId)` (`src/ClimaSite.Api/Controllers/CartController.cs:110-116`) — a required non-nullable `[FromQuery]` param under `[ApiController]`, so the request 400s before the handler runs. The login flow swallows the failure with `catchError -> console.warn('Failed to merge guest cart, continuing with login')` (`src/ClimaSite.Web/src/app/auth/services/auth.service.ts:133-142`). After login, `GetCartQuery` prefers UserId over GuestSessionId (`GetCartQuery.cs:27-38`), so the UI switches to the (empty) user cart and guest items disappear. API tests pass because they call the endpoint with the query param directly (`tests/ClimaSite.Api.Tests/Controllers/CartControllerTests.cs:436,473`); no E2E test covers login-merge.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/cart.service.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/CartController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/services/auth.service.ts`
- **Why it matters:** CLAUDE.md advertises "Cart merges when guest user logs in" as a core business rule. In reality every guest who logs in mid-shopping loses their visible cart — a direct conversion killer, and silent (only a `console.warn`).
- **Recommended fix:** Change `CartService.mergeCart` to POST `/api/cart/merge?guestSessionId=${this.getSessionId()}` (drop the `userId` body — the backend takes the user from the JWT), or change the endpoint to read the body/header. Add an E2E test: add as guest, log in, assert items present. Estimated effort: Small.
- **Acceptance criteria:** E2E: guest adds 2 items, logs in, cart still shows both items and the guest cart row is removed server-side.
- **Dependencies or follow-up:** None.
- **Confidence:** verified. Verifier: first pass confirmed at P0 — 100% reproducible (NRT enabled, no suppression of the implicit-required behavior, no middleware mapping `X-Session-Id` for this endpoint; the FE spec even asserts the broken request shape, and no E2E covers login-merge). Second pass adjusted P0→P1 for a pre-launch app: the guest cart row is never deleted server-side (items recoverable, not destroyed), the funnel is not hard-blocked since guest checkout exists, and the fix is one line — a deterministic launch blocker, not an active incident.

### 4. Checkout charges the card before creating the order, with no compensation and a double-submit window

- **Finding:** `placeOrder()` confirms the Stripe payment before creating the order, has no cancel/refund compensation if order creation fails, and the processing guard is set too late, leaving a multi-second double-submit window with no idempotency key anywhere in the pipeline.
- **Category:** money-correctness/runtime-risk
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** `placeOrder()` confirms the Stripe payment first (`checkout.component.ts:1174-1177`) and only then calls `createOrder` (`:1185`); the error path on order failure just sets an error message — no cancel/refund of the captured intent (`:1199-1201`, and `CreateOrderCommand` can fail on empty cart/stock at `CreateOrderCommand.cs:115-145`). The place-order button is disabled only by `checkoutService.isProcessing()` (`checkout.component.ts:304`), which is set inside `createOrder()` (`checkout.service.ts:88`) — i.e., AFTER the multi-second intent-creation + Stripe confirmation phase, so double-clicks during that phase run the whole payment twice. There is no idempotency key anywhere in the order pipeline.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/checkout.service.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`
- **Why it matters:** Customer money is captured even when the order is never created (stock ran out, validation failed, network blip) — and because paymentIntentId is not persisted (see P0 finding) there is no way to even find the orphaned charge later. The double-submit window can double-charge a card.
- **Recommended fix:** Set a processing flag at the top of `placeOrder()` and disable the button for the whole flow; create the order (Pending) BEFORE confirming payment, or auto-cancel/refund the intent when `createOrder` fails; add an idempotency key (e.g., paymentIntentId unique index on orders). Estimated effort: Medium.
- **Acceptance criteria:** Manual/E2E: rapid double-click on place-order produces exactly one intent and one order; forcing `createOrder` to fail after payment leaves no captured uncancelled intent.
- **Dependencies or follow-up:** Builds on the payment-intent persistence and server-side amount fixes.
- **Confidence:** verified. Verifier confirmed P1: the existing cancel-intent endpoint (`PaymentsController.cs:101`) is never called from the frontend, grep finds no idempotency key, and — worse than claimed — paymentIntentId is also dropped server-side, so orphaned charges are unfindable; P1/High correctly calibrated since a failure or double-click is required to trigger.

### 5. Stock decrement has no concurrency control — concurrent checkouts can oversell; documented "stock reservations" do not exist

- **Finding:** Order creation validates and decrements stock with an in-memory read-then-write pattern under default ReadCommitted, with no concurrency token, lock, or atomic update — so concurrent checkouts can oversell; the "stock reservations" claimed in documentation exist only as a hardcoded `ReservedQuantity = 0` DTO field.
- **Category:** concurrency
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** `CreateOrderCommand` validates `StockQuantity` in memory then calls `variant.AdjustStock(-qty)` and saves (`CreateOrderCommand.cs:128-146,182,209`) inside a default ReadCommitted transaction — two concurrent orders both read stock=5, both pass, both write absolute values (classic lost update). `ProductVariant` has no concurrency token: grep for xmin/RowVersion/IsConcurrencyToken only hits Identity's `ConcurrencyStamp` in migrations. No `SELECT FOR UPDATE` / atomic `UPDATE ... SET stock = stock - n`. `grep -i reservation` across src finds only a hardcoded `ReservedQuantity = 0` DTO field (`GetProductBySlugQuery.cs:96`, `ProductDto.cs:62`) — yet CLAUDE.md states "Inventory updates must be transactional (use stock reservations)" and lists Inventory Management as Complete with reservations. Admin `InventoryController` stock updates have the same read-then-write pattern.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Core/Entities/ProductVariant.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/InventoryController.cs`
- **Why it matters:** HVAC units are high-value, low-stock items — overselling the last unit during a promotion is a realistic and expensive failure. Documentation claims a safeguard that does not exist.
- **Recommended fix:** Either add a Postgres xmin concurrency token (`UseXminAsConcurrencyToken`) on `product_variants` and retry on `DbUpdateConcurrencyException`, or decrement atomically with a conditional UPDATE (`ExecuteUpdateAsync` with `stock >= qty` guard) and fail the order if 0 rows are affected. Estimated effort: Medium.
- **Acceptance criteria:** Concurrency test: two parallel CreateOrder calls for the last unit — exactly one succeeds, stock never goes negative and never double-decrements.
- **Dependencies or follow-up:** None; independent of payment fixes.
- **Confidence:** verified. Verifier confirmed end-to-end: the transaction is opened with no isolation level (default ReadCommitted); repo-wide greps for concurrency tokens, `FOR UPDATE`/`ExecuteUpdate`/advisory locks, and `DbUpdateConcurrencyException` all come up empty; reservations exist only as hardcoded `ReservedQuantity = 0`; and admin `AdjustStockCommand.cs:47-62` has the same read-then-write pattern. P1 correctly calibrated — a real oversell defect on the money path that needs concurrent traffic to trigger.

### 6. Sale-price display is inverted sitewide: the HIGHER original price renders as the deal price

- **Finding:** Nearly every DTO maps `SalePrice = CompareAtPrice` (the higher original price) while the UI renders `salePrice` as the prominent deal price and `basePrice` struck through, inverting the sale display across catalog, search, detail, cart, wishlist, and recommendations.
- **Category:** money-correctness/contract-drift
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** Domain semantics: `BasePrice` is the current selling price, `CompareAtPrice` is the higher original (`IsOnSale => CompareAtPrice > BasePrice`, `Product.cs:284`; `DiscountPercentage` formula `:293`). But nearly every DTO maps `SalePrice = CompareAtPrice` (`GetProductsQueryHandler.cs:108`, `GetFeaturedProductsQuery.cs:51`, `SearchProductsQuery.cs:113`, `GetRelatedProductsQuery.cs:64`, `GetRecommendationsQueryHandler.cs:105`, `GetProductBySlugQuery.cs:58`, Promotions `:84`, Cart `AddToCartCommand.cs:175`, `MergeGuestCartCommand.cs:158`, `ReorderCommand.cs:219`, new `WishlistApplicationService.cs:176`). The UI renders `salePrice` as the big deal price and `basePrice` struck through (`product-card.component.ts:154-158`, `product-detail.component.ts:87-91`, `cart.component.ts:73-74`, `brand-detail.component.ts:97-100`). Seed data proves impact: DualZone Pro basePrice 899.99 / compareAt 1099.99 (`DataSeeder.cs:162`) displays "€1099.99" big with "€899.99" struck out, while checkout charges 899.99. `GetBrandBySlugQuery.cs:86` alone uses the opposite mapping (`SalePrice = IsOnSale ? BasePrice : null`), confirming the semantics are internally inconsistent.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetProductsQueryHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Brands/Queries/GetBrandBySlugQuery.cs`
- **Why it matters:** Every on-sale product shows the wrong (higher) price as the purchase price across catalog, search, detail, cart, wishlist, and recommendations. Customers see €1099.99 but pay €899.99 — confusing at best, a consumer-protection/price-indication problem at worst, and the brand page disagrees with the rest of the site.
- **Recommended fix:** Pick one contract and apply it everywhere: BasePrice = original (CompareAtPrice), SalePrice = current (BasePrice) when on sale — matching the FE templates — or flip the templates. Centralize the mapping in one helper instead of 12 copy-pasted projections. Estimated effort: Medium.
- **Acceptance criteria:** For a seeded on-sale product, card/detail/cart/wishlist all show 899.99 as the active price and 1099.99 struck through, and the charged amount equals the displayed active price. Snapshot/unit test on the shared mapper.
- **Dependencies or follow-up:** Coordinate with the wishlist mapping (new code) and brand query so all converge on one semantics.
- **Confidence:** verified. Verifier confirmed: 14 projections map `SalePrice = CompareAtPrice`, 'compareAtPrice' never appears in the frontend, and no remapping layer or fix commit exists. Only caveat: `cart.component.ts:72`'s `salePrice < unitPrice` guard accidentally suppresses the inverted display in the cart, slightly narrowing the blast radius; P1 (not P0) since the charged amount is correct.

### 7. Forgot-password never sends email and logs the reset token at Information level

- **Finding:** The forgot-password handler generates a reset token, logs it (including the token value) at Information level, and never sends the reset email — the send call is commented out — while the endpoint reports success to the user regardless.
- **Category:** broken-flow/security
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** `ForgotPasswordCommandHandler` generates the token and only logs it: `_logger.LogInformation("Password reset token generated for user: {UserId}. Token: {Token}", ...)` with the email send commented out (`ForgotPasswordCommandHandler.cs:34-42`). `EmailService.SendPasswordResetEmailAsync` exists and is implemented (`EmailService.cs:51-58`) but is never injected here. The endpoint returns success to the user either way.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Services/EmailService.cs`
- **Why it matters:** Users who forget their password are dead-ended: the UI reports success but no email ever arrives — permanent account lockout for production users. Additionally, logging live password-reset tokens means anyone with log access (Railway logs, log aggregation) can take over any account that requested a reset.
- **Recommended fix:** Inject `IEmailService`, call `SendPasswordResetEmailAsync` with a reset URL, and remove the token from the log line (log user id only). Estimated effort: Small.
- **Acceptance criteria:** Integration test with a fake `IEmailService` asserting it is called with the token; grep confirms no token appears in logs; manual flow via MailHog delivers a working reset link.
- **Dependencies or follow-up:** SMTP config already exists (env vars documented); none.
- **Confidence:** verified. Verifier confirmed: `SendPasswordResetEmailAsync` has zero callers repo-wide (only the commented-out line and the interface reference it), and the flow is live and user-facing via `AuthController.cs:83-89` plus the Angular forgot-password route. Correctly P1 (complete break of account recovery plus credentials-in-logs) rather than P0, since exploitation requires log access and no data is destroyed.

### 8. Concurrent 401s trigger refresh-storm guard that logs users out instead of queuing requests

- **Finding:** When a token refresh is already in flight, `AuthService.refreshToken()` throws a synthetic error that the interceptor treats as fatal, clearing auth state — so concurrent 401s at token expiry log the user out instead of waiting on the shared refresh.
- **Category:** error-handling
- **Severity/Priority:** P1 (Medium) — verification: confirmed
- **Evidence:** `AuthService.refreshToken()` returns `throwError('Token refresh already in progress')` when `_isRefreshing` is true (`auth.service.ts:202-208`). The interceptor treats any refresh error as fatal: `catchError -> authSvc.clearAuthState()` (`auth.interceptor.ts:49-55`). A page load with an expired access token fires several API calls in parallel; the first 401 starts a refresh, every other 401 hits the guard, gets the synthetic error, and clears auth state — racing the in-flight refresh and logging the user out while the refresh may still succeed. There is no shared in-flight observable (contrast: `WishlistService.fetchInFlight$` does this correctly, `wishlist.service.ts:242-278`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/services/auth.service.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts`
- **Why it matters:** Random session loss exactly when the token expires (every ~15 minutes per the documented expiry) and multiple requests are in flight — e.g., mid-checkout, which then surfaces as unexplained "session expired" failures. The circular-dependency fix itself (lazy Injector) is sound; the refresh coordination is not.
- **Recommended fix:** Cache and share a single refresh observable (`shareReplay`) so concurrent callers all wait on the same refresh and retry their requests with the new token; only `clearAuthState` when the shared refresh itself fails. Estimated effort: Small.
- **Acceptance criteria:** Unit test: two simultaneous 401s cause exactly one POST `/auth/refresh` and both original requests are retried successfully; no clearAuth on the second caller.
- **Dependencies or follow-up:** None.
- **Confidence:** verified. Verifier confirmed: the boolean flag is the only refresh coordination — no shared in-flight observable exists, with `restoreCurrentUser` an additional uncited racer on startup. Minor nuance: a succeeding in-flight refresh restores auth state via its `tap`, so the logout can be transient — but concurrent requests still fail unretried and route guards redirect during the cleared window; the deterministic ~15-minute trigger on any page with parallel authenticated calls (including checkout) justifies P1.

### 9. Wishlist concurrent-add protection is per-process only; multi-instance deployments fall through to unhandled 500s

- **Finding:** The new wishlist mutation lock is a static in-process `SemaphoreSlim` dictionary; under multiple instances, races fall through to the DB unique index, which throws an unhandled `DbUpdateException` (500) instead of the idempotent success the single-instance path returns.
- **Category:** concurrency
- **Severity/Priority:** P2 (Medium) — verification: unverified (P2/P3)
- **Evidence:** New `WishlistApplicationService` serializes mutations with a static `ConcurrentDictionary<Guid, SemaphoreSlim>` (`WishlistApplicationService.cs:10,24-37`) — effective only within one process. The DB does have a unique index on `(wishlist_id, product_id)` (`WishlistConfiguration.cs:107`; created in InitialCreate migration `:925-929`), so cross-instance races are caught — but `AddToWishlistCommand`'s check-then-insert (`AddToWishlistCommand.cs:66-73`) would then throw `DbUpdateException`, and there is no handler for it (no global exception mapping for unique violations in Program.cs), producing a 500 instead of the idempotent 200 the single-instance path returns. The semaphore dictionary also never evicts entries (unbounded growth with user count). Deployment uses Railway (railway.toml) where replicas >1 is one toggle away.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Commands/AddToWishlistCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs`
- **Why it matters:** The feature is marked "concurrent add protection" complete, but the guarantee silently weakens to "500 on race" the moment a second instance runs. Single-instance behavior is correct today (verified: 10/10 new unit tests pass).
- **Recommended fix:** Catch `DbUpdateException` for the unique violation in the add handler and return the existing wishlist DTO (idempotent), making the DB constraint the real guard; optionally drop the semaphore or keep it as a fast-path. Evict idle semaphores if kept. Estimated effort: Small.
- **Acceptance criteria:** Unit/integration test simulating a unique-violation on SaveChanges returns success with the existing item rather than throwing.
- **Dependencies or follow-up:** None.
- **Confidence:** verified.

### 10. Cart and wishlist product names ignore translations; BG/DE users see English names

- **Finding:** Cart and wishlist DTO mappers use raw `product.Name` and accept no language code, while catalog queries translate via `GetTranslatedContent` — so BG/DE users see English names in cart/wishlist for products that are translated elsewhere.
- **Category:** i18n/data-correctness
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Catalog queries translate via `p.GetTranslatedContent(request.LanguageCode)` (`GetProductsQueryHandler.cs:100`), but cart mapping uses raw `product.Name` (`AddToCartCommand.cs:171`, `MergeGuestCartCommand.cs:152`, `GetCartQuery.cs:~100`) and the new wishlist mapper does too (`WishlistApplicationService.cs:169`: `ProductName = product.Name`) — neither accepts a language code. Language is passed as a `?lang=` query param by `product.service.ts:40-42`; cart/wishlist services send none.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs`
- **Why it matters:** Violates the project's own Definition of Done ("works in all languages"); a BG user sees translated names in the catalog and English names in cart/wishlist for the same product.
- **Recommended fix:** Thread a LanguageCode into cart/wishlist queries (query param like products) and use `GetTranslatedContent` in the mappers. Estimated effort: Medium.
- **Acceptance criteria:** With `lang=bg`, GET `/api/cart` and `/api/wishlist` return Bulgarian product names for translated products.
- **Dependencies or follow-up:** Wishlist mapper is new uncommitted code — cheapest to fix now before merge.
- **Confidence:** verified.

### 11. Cart/checkout totals render in USD ($) via bare `| currency` while products show EUR and Stripe charges BGN

- **Finding:** Cart and checkout templates use the bare `| currency` pipe (Angular defaults to USD for the default en-US LOCALE_ID), while product pages display EUR explicitly and the actual Stripe charge is BGN — three currencies in one purchase journey.
- **Category:** money-correctness/ui
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `checkout.component.ts:294,326,334,342,348,352` and `cart.component.ts:73-76,116,136-157` use `| currency` with no currency code — Angular defaults to USD for the default en-US LOCALE_ID. Product card/detail use `currency:'EUR'` explicitly (`product-card.component.ts:155-158`); order confirmation uses `order.currency` (EUR); the actual Stripe charge is BGN (`checkout.component.ts:1149`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/cart/cart.component.ts`
- **Why it matters:** A single purchase journey shows €899.99 on the product page, $899.99 in cart and checkout, charges 899.99 BGN, then confirms €899.99 — three currencies for one number.
- **Recommended fix:** Standardize on the store currency: pass 'EUR' (or a CurrencyService value) to every currency pipe, or set `DEFAULT_CURRENCY_CODE` in app config. Estimated effort: Small.
- **Acceptance criteria:** Grep shows no bare `| currency` usages; cart/checkout/product/confirmation all display the same currency symbol.
- **Dependencies or follow-up:** Depends on the currency decision from the payment P0 fix.
- **Confidence:** verified.

### 12. AverageRating/ReviewCount hardcoded to 0 in every product DTO — star ratings are always empty sitewide

- **Finding:** Every product list/search/featured/wishlist projection hardcodes `AverageRating = 0` and `ReviewCount = 0`, so star ratings render empty everywhere regardless of actual reviews; real aggregates exist only in the reviews section query.
- **Category:** data-correctness
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `AverageRating = 0` with comment "Will be calculated from reviews" in `GetProductsQueryHandler.cs:113-114`, `GetProductBySlugQuery.cs:67`, `SearchProductsQuery.cs:119`, `GetFeaturedProductsQuery.cs:57`, and the new wishlist mapper (`WishlistApplicationService.cs:179-180`). Product cards render stars from `product.averageRating` (`product-card.component.ts:140-141`). Real aggregates exist only inside `GetProductReviewsQuery.cs:108` (reviews section of the detail page).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetProductsQueryHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts`
- **Why it matters:** Reviews & Ratings is documented "Complete", but every product card, search result, featured slot, and wishlist item shows zero stars regardless of actual reviews — undermining social proof and the verified-purchase feature.
- **Recommended fix:** Project approved-review aggregates into the product queries (subquery or denormalized columns updated on review approval). Estimated effort: Medium.
- **Acceptance criteria:** A product with approved 5-star reviews shows non-zero stars on cards, search, featured, and wishlist.
- **Dependencies or follow-up:** Consider denormalizing to avoid N+1 in list queries.
- **Confidence:** verified.

### 13. Flat 20% VAT plus tax-base inconsistencies; DE rate is wrong by law

- **Finding:** Tax is a flat 20% of subtotal everywhere (cart and order), shipping is added untaxed, and German customers are charged 20% VAT against the legal 19% rate despite DE being a target market.
- **Category:** money-correctness
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Cart tax: `Math.Round(subtotal * 0.20m)` with a TODO acknowledging DE=19% (`AddToCartCommand.cs:185-186`, `MergeGuestCartCommand.cs:168`); order tax also 20% of subtotal only (`CreateOrderCommand.cs:200-202`) while shipping is added untaxed. Currency hardcoded EUR (`CreateOrderCommand.cs:163`) and shipping costs hardcoded (`CreateOrderCommand.cs:190-196`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs`
- **Why it matters:** Charging 20% VAT to German customers (legal rate 19%) is a compliance issue for a platform explicitly targeting EN/BG/DE markets; EU rules also generally require VAT on shipping.
- **Recommended fix:** Country-based VAT table applied to (subtotal + shipping) at order time, driven by shipping address; keep the cart estimate but label it as estimated. Estimated effort: Medium.
- **Acceptance criteria:** Order to a DE address computes 19% on goods+shipping; BG computes 20%; unit tests per country.
- **Dependencies or follow-up:** Store-configuration work shared with shipping/currency TODOs (API-014).
- **Confidence:** verified.

### 14. Admin related-products search is a stub that always returns empty results

- **Finding:** The admin related-products manager's `searchProducts()` is an unimplemented stub that unconditionally sets empty results, so admins can never find products to attach as related items through this UI.
- **Category:** broken-flow (admin)
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `searchProducts()` short-circuits and ends with "TODO: Implement actual product search" followed by `this.searchResults.set([])` unconditionally (`related-products-manager.component.ts:515-526`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/products/components/related-products-manager/related-products-manager.component.ts`
- **Why it matters:** Admins cannot find products to attach as related items through this UI, so the related-products feature (Plan 17, documented Complete) cannot be curated.
- **Recommended fix:** Wire the existing GET `/api/products` search (`SearchProductsQuery`) into `searchProducts()` with debounce. Estimated effort: Small.
- **Acceptance criteria:** Typing 2+ chars in the manager lists matching products and they can be added as relations.
- **Dependencies or follow-up:** None — public search endpoint already exists.
- **Confidence:** verified.

### 15. Guest checkout is unreachable: checkout route is auth-guarded while backend and docs support guest orders

- **Finding:** The checkout route is gated by `authGuard` so guests are redirected to login, while the backend explicitly supports anonymous guest orders and CLAUDE.md documents guest checkout as working — and the payments endpoint would 401 for guests anyway.
- **Category:** broken-flow
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Route 'checkout' has `canActivate: [authGuard]` (`app.routes.ts:82-89`), so guests are redirected to /login. Backend explicitly supports guest orders (`[AllowAnonymous]` CreateOrder with GuestSessionId, `OrdersController.cs:28-39`; `CreateOrderCommand.cs:85-88`) and CLAUDE.md states "guest checkout stores email". Card payment would also 401 for guests since `PaymentsController` is `[Authorize]` (`PaymentsController.cs:10`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.routes.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/OrdersController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`
- **Why it matters:** Either the docs/business rules are wrong or a revenue-relevant flow is blocked; the half-wired backend guest path is also untested dead code that complicates the order pipeline (cart lookup by session, anonymous create).
- **Recommended fix:** Decide: enable guest checkout (remove the guard, allow anonymous create-intent tied to a guest cart) or remove the guest path from the backend and update CLAUDE.md. Estimated effort: Medium.
- **Acceptance criteria:** Documented behavior matches reality; if enabled, an E2E guest completes checkout without an account.
- **Dependencies or follow-up:** If enabling, must follow the payment P0 fixes (intent tied to server-side cart total).
- **Confidence:** verified.

### 16. Admin "notify customer" on order status update is a silent no-op

- **Finding:** `UpdateOrderStatusCommand` accepts a `NotifyCustomer` flag but the handler contains only a TODO — no email or notification is ever sent when the admin checks the box.
- **Category:** broken-flow (admin)
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `UpdateOrderStatusCommand` accepts `NotifyCustomer` but the handler only has `// TODO: If NotifyCustomer is true, send email notification` (`UpdateOrderStatusCommand.cs:73`); no `IEmailService` usage in the file. The notifications system is documented "Partial — email notifications implemented" and `EmailService.SendEmailAsync` exists (`EmailService.cs:20`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Services/EmailService.cs`
- **Why it matters:** Admins believe customers were emailed about shipping/cancellation when nothing was sent — customer-trust damage that is invisible to the admin.
- **Recommended fix:** Invoke `IEmailService` (and the in-app Notification entity) when `NotifyCustomer` is true; until then, hide or disable the checkbox in the admin UI. Estimated effort: Small.
- **Acceptance criteria:** Status change with `NotifyCustomer=true` sends an email (assert via fake/MailHog) and creates a notification row.
- **Dependencies or follow-up:** Plan 12 (notifications) remaining work.
- **Confidence:** verified.

### 17. Latent crash/constraint traps if a product row is ever hard-deleted

- **Finding:** Cart DTO mapping crashes (`First()` throws) and order-item FK configuration violates NOT NULL (`SetNull` on non-nullable Guid columns) if a product row is ever hard-deleted; deletion is soft today, so the traps are latent.
- **Category:** data-integrity (latent)
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** Product deletion is soft today (`DeleteProductCommand.cs:42-44`), but: (1) cart DTO mapping uses `products.First(p => p.Id == item.ProductId)` which throws `InvalidOperationException` if the row is gone (`AddToCartCommand.cs:160`, `MergeGuestCartCommand.cs:143`); (2) order_items FKs to products/variants are configured `DeleteBehavior.SetNull` on NON-nullable Guid columns (`OrderConfiguration.cs:214-222` vs `OrderItem.cs:6-7`), so any future hard delete violates NOT NULL at the database. CartItem->Product is Cascade (`CartConfiguration.cs:96-106`), silently emptying carts on hard delete.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/Configurations/OrderConfiguration.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Core/Entities/OrderItem.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs`
- **Why it matters:** Anyone running a manual cleanup or future purge job will hit 500s on cart reads and DB errors on order history; the SetNull-on-required-FK config is simply wrong even if currently dormant.
- **Recommended fix:** Make `OrderItem.ProductId`/`VariantId` nullable `Guid?` (order rows already snapshot name/sku/price) and use `FirstOrDefault` with a graceful "unavailable" item in cart mapping. Estimated effort: Small.
- **Acceptance criteria:** Hard-deleting a test product leaves order history readable and cart endpoints returning 200 with the item flagged unavailable.
- **Dependencies or follow-up:** Requires a migration (FK nullability).
- **Confidence:** verified.

### 18. Documentation drift: Accept-Language header, TypeScript Playwright suite, and stock reservations are all documented but not real

- **Finding:** CLAUDE.md documents an Accept-Language header convention, a TypeScript Playwright suite (`npx playwright test` + TS test-data-factory), and complete stock reservations — none of which exist in the codebase.
- **Category:** doc-mismatch
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** (1) CLAUDE.md documents "Accept-Language: en|bg|de" but grep finds zero Accept-Language handling in API or frontend; language travels as `?lang=` (`ProductsController.cs:57`, `product.service.ts:40-42`). (2) CLAUDE.md prescribes `npx playwright test` and a TS test-data-factory, but `tests/ClimaSite.E2E` is Playwright for .NET (C# files, `dotnet test`, .trx outputs). (3) "Stock reservations" claimed complete — none exist (see P1 stock finding).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/ProductsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E`
- **Why it matters:** Agents and developers follow CLAUDE.md verbatim per project rules; wrong commands and phantom mechanisms cause wasted work and false confidence (e.g., trusting reservations during the oversell discussion).
- **Recommended fix:** Update CLAUDE.md: lang query param convention, dotnet-test E2E commands, and honest inventory status. Estimated effort: Small.
- **Acceptance criteria:** CLAUDE.md commands run as written; status table matches code.
- **Dependencies or follow-up:** None.
- **Confidence:** verified.

## Dimension data

### TODO/FIXME Triage (all 24 hits)

| # | Location | Text (abridged) | Classification |
|---|----------|-----------------|----------------|
| 1 | related-products-manager.component.ts:524 | Implement actual product search | **Hidden broken feature** — search always returns [] (P2 finding) |
| 2 | auth.service.ts:72 | AUTH-015 session expiry warnings | Harmless UX note (related P1 refresh-storm bug found independently) |
| 3 | profile.component.ts:69 | phone validation pattern | Harmless note |
| 4 | ForgotPasswordCommandHandler.cs:14 | Inject email service | **Hidden broken flow** — reset email never sent + token logged (P1 finding) |
| 5 | ForgotPasswordCommandHandler.cs:36 | Send email with reset link | Same as #4 |
| 6 | UpdateOrderStatusCommand.cs:73 | NotifyCustomer email | **Hidden no-op** — admin checkbox does nothing (P2 finding) |
| 7 | CreateOrderCommand.cs:63 | Add ILogger | Harmless note |
| 8 | CreateOrderCommand.cs:162 | Currency from store config | **Masks real bug** — EUR order vs BGN charge (part of P0 #2) |
| 9 | CreateOrderCommand.cs:185 | API-014 hardcoded shipping | Known debt; interacts with charge-excludes-shipping (P0 #2) |
| 10 | CreateOrderCommand.cs:200 | VAT per country | **Money/legal gap** — DE 19% (P2 finding) |
| 11 | AddToCartCommand.cs:139 | Guest cart cleanup job | DB bloat only; expiry already filtered at read (GetCartQuery.cs:37). P3 |
| 12 | AddToCartCommand.cs:185 | VAT configurable | Same as #10 |
| 13 | Program.cs:108 | API-012 RFC7807 | Harmless consistency note |
| 14 | CreateReviewCommand.cs:82 | single query optimization | Harmless perf note |
| 15-16 | AdminCategoriesController.cs:104,113 | API-008 N+1 reorder | Perf debt, admin-only, P3 |
| 17 | QuestionsController.cs:47 | rate limiting for questions | **Real gap** — anonymous spam vector on public endpoint, P2/P3 |
| 18-19 | AdminReviewsController.cs:132,144 | API-009 N+1 bulk moderate | Perf debt, P3 |
| 20 | AdminProductsController.cs:72 | slug duplication | Harmless note |
| 21-24 | AdminQuestionsController.cs:201,212,235,246 | API-010 N+1 moderation | Perf debt, P3 |

### FE/BE Contract Drift Table

| Contract | Frontend | Backend | Verdict |
|----------|----------|---------|---------|
| POST /api/cart/merge | body `{userId}` + X-Session-Id header (cart.service.ts:193) | `[FromQuery] string guestSessionId` required (CartController.cs:111) | **ALWAYS 400 — broken merge (P0)** |
| POST /api/orders | sends `paymentIntentId`, `paymentMethod` (checkout.service.ts:99-107) | CreateOrderCommand lacks both fields | **Silently dropped — webhook dead (P0)** |
| ProductBriefDto.SalePrice | FE renders salePrice as discounted current price (product-card :154-158) | Most queries: SalePrice=CompareAtPrice (original/higher); GetBrandBySlugQuery: SalePrice=BasePrice | **Inverted display (P1); internally inconsistent** |
| WishlistDto / WishlistItemDto | wishlist.service.ts:10-41 | WishlistDto.cs | Matches 1:1 (verified) |
| CartItemDto | TS `sku: string` (non-optional), `subtotal`, `maxQuantity` | C# `Sku` nullable (omitted when null due to WhenWritingNull, Program.cs:116); Subtotal/MaxQuantity are computed aliases | Minor: sku can be undefined at runtime vs typed string |
| Cart.shipping | TS expects number | Always 0 from backend; real shipping only added at order time | Misleading 'free shipping' in cart UI |
| AverageRating/ReviewCount | FE renders stars on cards | Hardcoded 0 in ALL product/wishlist queries | Always-empty stars (P2) |
| Language | `?lang=` query param (product.service.ts:41) | `[FromQuery] lang` (ProductsController) | Consistent with each other; CLAUDE.md's Accept-Language claim is wrong; cart/wishlist send no lang at all |

### Latest Test Results (tests/*/TestResults/*.trx)

| File | Date | Result |
|------|------|--------|
| e2e-final-testing.trx (newest) | 2026-06-06 16:10 | 206/207 passed, 0 failed, 1 NotExecuted (Checkout_SavedAddress_CanBeUsed — plain [Fact], 1ms duration, likely runtime-skipped/filtered; worth confirming it actually runs) |
| e2e-full / e2e-full-after-installation-url | 06-06 15:17/15:27 | 206/206 passed |
| e2e-current.trx (intermediate) | 06-06 14:17 | **41 failures** concentrated in Orders/Reviews/Journeys/Admin — all green by the final run, indicating env/app-state flakiness during the session rather than current breakage |
| application-tests.trx | 06-06 15:05 | 161/161 — **predates the new wishlist tests** (0 'Wishlist' entries) |
| api-tests.trx / core-tests.trx | 06-06 15:04/15:03 | 61/61, 197/197 |
| Fresh runs by this review | 2026-06-11 | `dotnet build ClimaSite.sln`: 0 warnings/0 errors. `dotnet test ClimaSite.Application.Tests --filter Wishlist`: 10/10 passed |

### Additional minor observations (not promoted to findings)

- **WishlistItem.PriceWhenAdded is never populated** — AddToWishlistCommand calls `wishlist.AddItem(productId)` without price (AddToWishlistCommand.cs:72); `SetPriceWhenAdded` has no callers. The NotifyOnSale/price-drop UX has no data to work with. (P3)
- **clearWishlist drift**: local state cleared optimistically; if the DELETE fails it is swallowed (`catchError(() => of(null))`, wishlist.service.ts:159-162) and server items reappear on next fetch. Self-healing but surprising. (P3)
- **Wishlist guest→login merge aborts on first bad item**: fetchFromApi posts local-only items via concatMap (wishlist.service.ts:259-262); one 400 (e.g., product deactivated) errors the stream and skips the remaining items for that cycle. (P3)
- **EF retry-strategy closures**: both CreateOrderCommandHandler and WishlistApplicationService run mutation closures under `strategy.ExecuteAsync` without resetting DbContext state — a transient retry could re-Add already-tracked entities (duplicate wishlist/order rows). Theoretical unless a retrying strategy is enabled. The `strategy is null` checks are dead code. (P3)
- **Order number collision**: ORD-yyyyMMdd-HHmmss + 4 random chars (CreateOrderCommand.cs:221-226) — ~1/65k collision chance within the same second; would surface as a 500 if order_number is unique-indexed. (P3)
- **TestController** (api/test/elevate-admin, etc.) is gated by `IsDevelopment() || IsEnvironment("Testing")` with a configurable secret and falls back to a hardcoded `"test-admin-secret"` only in Development — acceptable, but the security dimension should confirm ASPNETCORE_ENVIRONMENT in railway.toml is Production.
- **MergeGuestCartCommand authorization**: any authenticated user can merge any guess-able guestSessionId; session ids are client-generated (cart.service.ts:47) — check generation entropy in the security dimension.
- **Stripe webhook ordering race** (webhook before order row exists) is currently unobservable because of the P0 reconciliation bug, but after fixing it, note WebhooksController returns 200 even when no order matched (WebhooksController.cs:91-93) — Stripe will NOT retry, so an early-arriving webhook is permanently lost. Recommend returning 404/500 for 'order not yet created' or storing unmatched events.

### Open Questions

1. What is the intended store currency — EUR (display/orders) or BGN (Stripe)? Every money fix depends on this decision.
2. Is multi-replica deployment planned on Railway? Determines urgency of the wishlist per-process lock finding.
3. Is guest checkout in scope? Backend half-supports it; the route guard blocks it; docs claim it works.
4. Why is Checkout_SavedAddress_CanBeUsed NotExecuted in every recorded run despite being a plain [Fact]?

## Refuted during verification

None.
