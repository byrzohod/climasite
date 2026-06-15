# Product & Feature Completeness — Review Findings (2026-06-11)

## Summary

ClimaSite's customer-facing browse-to-cart experience is genuinely strong: PLP with a full filter sidebar, rich PDP (gallery, specs, reviews, Q&A, installation upsell, similar products), guest cart with merge-on-login, wishlist with the new public-sharing slice, brands/promotions content pages, and a polished account area (order history with cancel, reorder, tracking display, and real PDF invoice download).

However, the money path is critically broken: the Stripe PaymentIntent is never persisted on the order (`Order.SetPaymentInfo` has zero callers, `CreateOrderCommand` has no PaymentIntentId field), so webhooks can never match an order and every order stays Pending forever regardless of payment outcome. On top of that, the charged amount (cart subtotal + tax, in BGN, client-supplied) differs from the recorded order total (plus shipping, in EUR) while the UI displays USD and advertises free standard shipping that the backend bills at 5.99.

The operator side is essentially absent: `/admin/products`, `/admin/orders`, and `/admin/users` are 12-line "Coming Soon" stubs despite complete backend admin APIs, meaning nobody can fulfill an order, set tracking, or edit a product through the UI — directly contradicting CLAUDE.md's "Admin Panel | Complete". A whole tier of flows are silent dead-ends for customers: password-reset emails are never sent (token only logged), no transactional email of any kind is sent (`IEmailService` is consumed only by GDPR delete), the contact form fakes success with `setTimeout`, PayPal/bank "payment methods" create orders nobody pays for, installation requests are stored but unviewable, and all six footer legal/support links (terms, privacy, cookies, FAQ, shipping, returns) 404 — a hard blocker for an EU shop, compounded by no cookie consent.

Finally, there is a large implemented-but-unreachable inventory (comparison, recently-viewed, price history, GDPR export/delete, inventory admin API, in-app notifications backend, admin product translation/relations editors) and several CLAUDE.md status-table claims that do not match the code.

## Findings

### 1. Stripe payments never linked to orders — every order stays Pending forever; webhook status updates and refund tracking are dead

- **Finding:** Stripe payments are never linked to orders, so every order stays Pending forever; webhook status updates and refund tracking are dead.
- **Category:** Broken core flow / Payments
- **Severity/Priority:** P0 (Critical) — verification: confirmed
- **Evidence:** `Order.SetPaymentInfo(paymentIntentId, paymentMethod)` at `src/ClimaSite.Core/Entities/Order.cs:185-190` has ZERO callers (grep across src/ finds none). `CreateOrderCommand` (`src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs:12-21`) has no PaymentIntentId/PaymentMethod fields, so the paymentIntentId the frontend sends (`src/ClimaSite.Web/src/app/core/services/checkout.service.ts:99-108`) is silently dropped by model binding. The webhook handler looks up orders by PaymentIntentId (`src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs:39-48`) and returns success when no order matches — which is always. Therefore `payment_intent.succeeded`/`failed` and `charge.refunded` (`src/ClimaSite.Api/Controllers/WebhooksController.cs:100-119`) never update any order.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Core/Entities/Order.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/checkout.service.ts`
- **Why it matters:** Paid customers' orders show Pending forever in order history; failed payments are indistinguishable from successful ones; refunds are never reflected. The entire payment-reconciliation design (webhooks, OrderStatus.Paid/PaymentFailed/Refunded) is non-functional in production.
- **Recommended fix:** Add PaymentIntentId and PaymentMethod to CreateOrderCommand, call `order.SetPaymentInfo()` in the handler, and (since payment is confirmed client-side before order creation) set status Paid when the intent is verified server-side via the Stripe API rather than trusting the client. Add an integration test asserting webhook → order status transition with a matching PaymentIntentId. (Effort: Medium.)
- **Acceptance criteria:** Placing a card order persists the PaymentIntentId; a simulated `payment_intent.succeeded` webhook flips that order to Paid; `charge.refunded` flips it to Refunded; E2E order history shows the updated status.
- **Dependencies or follow-up:** None; should land before any production deploy.
- **Confidence:** verified. Two verifiers independently confirmed P0: every cited line holds in the working tree; `SetPaymentInfo` is called only from tests (which mask the gap by calling it themselves); the webhook returns 200 on no match so Stripe never retries and payment events are permanently lost, making losses unrecoverable retroactively. Card is the default and only automated payment method, so this is the mainline checkout flow.

### 2. Admin products/orders/users pages are 'Coming Soon' stubs — the shop cannot be operated (no fulfillment, no product CRUD, no tracking numbers)

- **Finding:** The /admin/products, /admin/orders, and /admin/users pages are 'Coming Soon' stubs, so the shop cannot be operated: no fulfillment, no product CRUD, no tracking numbers.
- **Category:** Broken core flow / Admin
- **Severity/Priority:** P0 — verification: confirmed
- **Evidence:** `src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts` (12 lines, template = 'common.comingSoon'), `admin/orders/admin-orders.component.ts` (12 lines) and `admin/users/admin-users.component.ts` (12 lines) are placeholders. Meanwhile full backend APIs exist unused: AdminProductsController (CRUD, status/featured toggles, relations — `Controllers/Admin/AdminProductsController.cs:25-177`), AdminOrdersController (status, shipping/tracking, notes — `Controllers/AdminOrdersController.cs:21-78`), AdminCustomersController (`Controllers/AdminCustomersController.cs:21-40`), AdminDashboardController KPIs/charts (`Controllers/AdminDashboardController.cs:20-56`). Frontend grep for 'api/admin' matches only `moderation.service.ts` and two orphaned product sub-services. The admin dashboard component (`features/admin/admin-dashboard/admin-dashboard.component.ts`, 77 lines) renders only four nav links, no KPIs. CLAUDE.md states 'Admin Panel | Complete | CRUD, dashboard'.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/orders/admin-orders.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/users/admin-users.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/Admin/AdminProductsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/AdminOrdersController.cs`
- **Why it matters:** Orders can be placed but never fulfilled through the product: no UI to mark Shipped, add tracking, manage stock, or create/edit products. Combined with the webhook finding, customer order status is permanently frozen at Pending. This blocks operating the shop at all.
- **Recommended fix:** Build the three admin pages against the existing APIs (orders list + status/shipping update first — that is the fulfillment-critical one), then products CRUD (wiring the already-built product-translation-editor and related-products-manager components), then customers. Surface dashboard KPIs from AdminDashboardController. (Effort: Large.)
- **Acceptance criteria:** Admin can change an order to Shipped with a tracking number and the customer sees it in /account/orders/:id; admin can create/edit/deactivate a product end-to-end; CLAUDE.md status table corrected.
- **Dependencies or follow-up:** Validation doc `docs/validation/areas/08-admin-panel.md:364-393` already flags this as Critical — still true in the working tree.
- **Confidence:** verified. Two verifiers independently confirmed P0: stubs and unused Admin-role-guarded APIs verified; the only functional admin page is moderation; the project's own validation doc rates this Critical, and CLAUDE.md falsely claims 'Complete', making it likely to ship as-is. 'Swagger as workaround' does not reduce severity for a product claiming production-grade completeness.

### 3. Charged amount, displayed amount, and recorded order total all disagree (shipping omitted from charge, BGN vs EUR vs USD, 'free' shipping billed 5.99)

- **Finding:** The charged amount, the displayed amount, and the recorded order total all disagree: shipping is omitted from the charge, currencies mix BGN vs EUR vs USD, and 'free' shipping is billed at 5.99.
- **Category:** Correctness / Pricing & VAT
- **Severity/Priority:** P0 (High) — verification: confirmed
- **Evidence:** Stripe is charged `this.cartService.total()` in 'bgn' (`checkout.component.ts:1147-1149`); cart total = subtotal + 20% tax with NO shipping (`GetCartQuery.cs:115-126`). The order then stores currency EUR (`CreateOrderCommand.cs:163` 'order.SetCurrency("EUR")') and adds shipping server-side: standard=5.99, express=15.99, free=0, default 9.99 (`CreateOrderCommand.cs:191-198`), so Order.Total = Subtotal+Shipping+Tax (`Order.cs:127-130`) exceeds the amount actually charged. The checkout UI advertises 'standard – FREE', 'express – $9.99', 'overnight – $19.99' hardcoded in USD (`checkout.component.ts:180,186,192`); 'overnight' has no backend case and bills the 9.99 default, while the 'free' tier is unreachable from the UI. The summary row shows 'freeShipping' because cart.shipping is always 0 (`checkout.component.ts:338-344`). Payment amount is also fully client-supplied — server validates only amount > 0 (`PaymentsController.cs:42-45`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Cart/Queries/GetCartQuery.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`
- **Why it matters:** Every card order undercharges the customer by the shipping cost, and if catalog prices are EUR the BGN charge is ~49% of the intended value. Customers see USD totals that match neither the charge nor the invoice. For an EU shop this is both a revenue leak and a consumer-law problem (advertised price must equal charged price).
- **Recommended fix:** Compute the authoritative total server-side: create the order (or a draft) first, derive amount+currency from Order.Total on the server when creating the PaymentIntent, and render all prices from one currency source. Align UI shipping methods (standard/express/overnight) with backend cases and the displayed fees with the billed fees. (Effort: Medium.)
- **Acceptance criteria:** For any cart, displayed total == Stripe charge == Order.Total, in one currency; shipping fee shown at checkout equals OrderDto.shippingCost; an automated test compares the three values.
- **Dependencies or follow-up:** Interacts with the PaymentIntent linkage fix (do together).
- **Confidence:** verified. Two verifiers independently confirmed P0: all amounts and currency mismatches verified; no reconciliation exists anywhere (server validates only amount > 0, webhook compares no amounts), so a client could pay 0.01 BGN for an order recorded at full EUR value; affects 100% of card orders on the core revenue path.

### 4. Password reset never emails the token — flow is a silent dead-end

- **Finding:** Password reset never emails the token — the flow is a silent dead-end for users.
- **Category:** Broken core flow / Auth
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** ForgotPasswordCommandHandler generates the token and only logs it: 'TODO: Send email with reset link' and commented-out `_emailService` call (`src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs:34-41`). `EmailService.SendPasswordResetEmailAsync` exists and is fully implemented over SMTP (`Infrastructure/Services/EmailService.cs:51-58`) but is never injected. The frontend forgot-password and reset-password pages exist (`app.routes.ts:24-34`), so users get a success message and then wait for an email that never arrives.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Services/EmailService.cs`
- **Why it matters:** Any customer who forgets their password is permanently locked out and will likely churn or flood support. CLAUDE.md marks Authentication 'Complete'.
- **Recommended fix:** Inject IEmailService into ForgotPasswordCommandHandler and send SendPasswordResetEmailAsync with a link to `/reset-password?token=...&email=...`; add a handler unit test asserting the email call. (Effort: Small.)
- **Acceptance criteria:** Submitting forgot-password sends a real email (verifiable via MailHog in shared infra); the link completes the reset E2E.
- **Dependencies or follow-up:** SMTP config (Email:SmtpHost etc.) must be set per environment.
- **Confidence:** verified. Verifier confirmed P1: token only logged with the email call commented out and the service not even injected; `SendPasswordResetEmailAsync` is DI-registered but never called anywhere; success message shown unconditionally to the user.

### 5. No transactional emails at all: order confirmation, shipped, and welcome emails are implemented but never sent

- **Finding:** No transactional emails are sent at all: order confirmation, shipped, and welcome emails are implemented in EmailService but never called.
- **Category:** Product gap / Notifications (Plan 12)
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** IEmailService's only consumer in the Application layer is the GDPR delete flow (grep 'IEmailService' → only `Features/Gdpr/Commands/DeleteUserDataCommand.cs` and the interface). SendOrderConfirmationEmailAsync / SendOrderShippedEmailAsync / SendWelcomeEmailAsync exist (`Common/Interfaces/IEmailService.cs:5-10`; `Infrastructure/Services/EmailService.cs:44-78`) but CreateOrderCommandHandler, RegisterCommandHandler (`Auth/Handlers/RegisterCommandHandler.cs`) and the UpdateShippingInfo flow never call them. CLAUDE.md claims 'Notifications System | Partial | Email notifications implemented'; plan 12 task checklists are almost entirely unchecked (`docs/plans/12-notifications-system.md:315-356`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Common/Interfaces/IEmailService.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/docs/plans/12-notifications-system.md`
- **Why it matters:** Customers receive no order confirmation, no shipping notice, and no receipt — table stakes for any shop and effectively required for EU distance-selling (durable confirmation of contract).
- **Recommended fix:** Wire SendOrderConfirmationEmailAsync into order creation (or on Paid webhook), SendOrderShippedEmailAsync into UpdateShippingInfoCommand, and the welcome email into registration. Make sends fire-and-forget/outboxed so email failure doesn't fail the order. (Effort: Medium.)
- **Acceptance criteria:** Placing an order produces a confirmation email; admin marking shipped produces a tracking email; failures are logged without breaking the flow.
- **Dependencies or follow-up:** Plan 12 open decision on provider/template engine (`docs/plans/18-project-completion.md:454`).
- **Confidence:** verified. Verifier confirmed P1: zero callers of the three send methods in src/ or tests/, no mitigation (no Stripe receipt_email, no outbox, no domain events); the gap is broader than claimed (EmailService defaults to log-only placeholder mode) — a known roadmap item but a launch-blocking, table-stakes gap.

### 6. All six footer legal/support links 404 (terms, privacy, cookies, FAQ, shipping, returns) and there is no cookie consent — EU legal blocker

- **Finding:** All six footer legal/support links (terms, privacy, cookies, FAQ, shipping, returns) 404, and there is no cookie consent — an EU legal blocker.
- **Category:** Product gap / Legal & compliance
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Footer links to /faq, /shipping, /returns, /terms, /privacy, /cookies (`core/layout/footer/footer.component.ts:43-56`), but `app.routes.ts` defines none of these — they fall through to the '**' NotFoundComponent (`app.routes.ts:135-138`). Repo-wide grep for cookie-consent components returns nothing. Social links are `href="#"` placeholders (`footer.component.ts:93-103`). GDPR backend endpoints exist (`GdprController.cs:28-98`) but no privacy policy page references them.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/layout/footer/footer.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.routes.ts`
- **Why it matters:** An EU (Bulgaria/Germany targeted) e-commerce site legally requires terms, privacy policy, withdrawal/returns policy, imprint, and cookie consent before selling. Today every trust-critical link a customer clicks is a 404.
- **Recommended fix:** Add a lazy 'legal' feature with translated static pages (terms, privacy, cookies, returns/withdrawal, shipping, FAQ, imprint) and a cookie-consent banner; remove or fill the social placeholders. (Effort: Medium.)
- **Acceptance criteria:** All footer links render translated content in EN/BG/DE; consent banner blocks non-essential storage until accepted.
- **Dependencies or follow-up:** Needs real business legal text (placeholder copy acceptable for staging).
- **Confidence:** verified. Verifier confirmed P1: all six links fall through to NotFoundComponent; zero cookie-consent code anywhere. Minor overstatement: no third-party trackers exist (only functional localStorage), so a consent banner may not be strictly mandated today, but terms/privacy/returns pages are legally required for an EU (BG/DE) shop regardless.

### 7. Contact form fakes success — submissions go nowhere

- **Finding:** The contact form fakes success — submissions go nowhere.
- **Category:** Broken flow / Dead-end
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `onSubmit()` contains '// Simulate API call' with `setTimeout(...)` that sets `submitSuccess(true)` and resets the form (`features/contact/contact.component.ts:384-399`). No HTTP call, no backend endpoint, no email.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/contact/contact.component.ts`
- **Why it matters:** Customers (including installation/service leads, complaint and GDPR inquiries) believe they contacted the business; messages are silently discarded — reputational and legal exposure.
- **Recommended fix:** Add POST /api/contact that emails the business via IEmailService (and/or persists a ContactMessage), wire the form to it, keep success/error states real. (Effort: Small.)
- **Acceptance criteria:** Submitting the form results in a stored/emailed message verifiable in MailHog; network failure shows the error state.
- **Dependencies or follow-up:** None.
- **Confidence:** verified. Verifier confirmed: simulated success verified verbatim (component injects only FormBuilder, so no HTTP call is possible); no /api/contact endpoint or ContactMessage entity exists; the page is prominently linked from header, footer, main-layout nav, and the resources CTA, and GdprController directs inquiries to 'contact support'.

### 8. PayPal and bank-transfer options create orders that nobody can pay — fake payment methods

- **Finding:** PayPal and bank-transfer options create orders that nobody can pay — they are fake payment methods.
- **Category:** Broken flow / Payments
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Checkout offers card/paypal/bank radio options (`checkout.component.ts:199-214`). For any non-card method, `placeOrder()` just calls `createOrder(email, phone)` with no payment step, no PayPal integration, and no bank-transfer instructions anywhere (`checkout.component.ts:1210-1232` '// Non-card payment (PayPal, bank transfer, etc.)'). The payment method is not even persisted (CreateOrderCommand has no PaymentMethod field; Order.PaymentMethod stays null).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`
- **Why it matters:** Customers selecting PayPal think they paid; stock is decremented for orders that will never be paid; operations (if the admin UI existed) cannot tell payment method apart because it is never stored.
- **Recommended fix:** Remove PayPal until integrated. For bank transfer, persist the method, show IBAN/reference instructions on the confirmation page and email, and keep status Pending-payment. Persist PaymentMethod on the order. (Effort: Small.)
- **Acceptance criteria:** Only real payment methods are offered; bank-transfer orders display payment instructions and are distinguishable in data; PayPal absent or fully integrated.
- **Dependencies or follow-up:** Payment linkage fix (P0) for consistent status semantics.
- **Confidence:** verified. Verifier confirmed: non-card `placeOrder()` skips payment entirely and navigates to the confirmation page with confetti; `paymentMethod` is silently dropped by model binding; stock is unconditionally decremented at order creation; no IBAN/bank-transfer instructions exist anywhere in API, application layer, or i18n files.

### 9. Guest checkout is blocked in the UI despite full backend support — documented behavior unreachable

- **Finding:** Guest checkout is blocked in the UI despite full backend support — documented behavior is unreachable.
- **Category:** Product gap / Docs mismatch
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Route /checkout has `canActivate:[authGuard]` (`app.routes.ts:82-89`) while the backend explicitly supports guest orders: CreateOrder is [AllowAnonymous] with GuestSessionId (`OrdersController.cs:23-32`). PaymentsController is class-level [Authorize] (`PaymentsController.cs:10`), so guests could not pay by card anyway, and GetOrderByIdQuery denies anonymous callers (`GetOrderByIdQuery.cs:44`), so a guest could never view a confirmation. CLAUDE.md business rules state 'guest checkout stores email' and the E2E suite contains Checkout_GuestUser_CanCheckoutWithEmail (per `docs/validation/areas/03-checkout-payments.md:99`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.routes.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/OrdersController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`
- **Why it matters:** Forcing registration is a major conversion killer for HVAC one-off purchases; the half-built guest path (anonymous order creation + guest session) is dead weight and a doc/code contradiction.
- **Recommended fix:** Decide the product stance: either enable guest checkout end-to-end (drop authGuard, allow anonymous create-intent tied to a server-computed amount, guest order confirmation via order number + email) or remove GuestSessionId support and fix the docs. (Effort: Medium.)
- **Acceptance criteria:** Either a guest completes purchase E2E and views confirmation, or all guest-checkout code/doc references are removed consistently.
- **Dependencies or follow-up:** Depends on payment linkage + amount-server-side fixes.
- **Confidence:** verified. Verifier confirmed: all facts hold; the cited E2E test is tautological (passes whether the guest is blocked or not, which TESTING_STRATEGY.md admits is pending this exact product decision); GetOrderByNumberQuery allows anonymous reads but the UI never uses it, so it does not rescue the guest flow.

### 10. Installation service requests vanish into a black hole — no list endpoint, no admin view, no email alert

- **Finding:** Installation service requests vanish into a black hole — there is no list endpoint, no admin view, and no email alert.
- **Category:** Broken flow / Dead-end
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** InstallationController has only GET options/{productId} and POST requests (`InstallationController.cs:22-34`). CreateInstallationRequestCommand stores the request with customer contact data but sends no email/notification (no IEmailService usage in `Features/Installation/Commands/CreateInstallationRequestCommand.cs`). There is no endpoint anywhere to list installation requests (grep 'installation' across Controllers matches only this file) and no admin UI.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/InstallationController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Installation/Commands/CreateInstallationRequestCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/products/components/installation-service/installation-service.component.ts`
- **Why it matters:** The PDP actively solicits installation bookings (a key HVAC revenue stream and trust signal); every submitted request is unactionable — customers wait for a call that never comes.
- **Recommended fix:** Minimum: email the business on each request via IEmailService. Proper: add GET /api/admin/installation-requests + status field and an admin list view. (Effort: Medium.)
- **Acceptance criteria:** Submitting an installation request triggers a business notification and appears in an admin-visible list with a status lifecycle.
- **Dependencies or follow-up:** Admin panel build-out (can ship email-only first).
- **Confidence:** verified. Verifier confirmed: the POST even returns a Location header for an endpoint that does not exist; no list query, no admin endpoint, zero installation references in the Angular admin area; the flow is live on the PDP and shows a success message promising follow-up the business cannot see except by raw DB access.

### 11. Payment captured before order exists, with no rollback: order-creation failure after successful charge leaves customer charged with no order

- **Finding:** Payment is captured before the order exists, with no rollback: an order-creation failure after a successful charge leaves the customer charged with no order.
- **Category:** Correctness / Payments
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `placeOrder()` confirms the Stripe payment first (`checkout.component.ts:1173-1183`) and only then calls createOrder (1184-1206). CreateOrderCommandHandler can still fail afterwards — e.g., 'Insufficient stock' (`CreateOrderCommand.cs:140-145`) or validation — and the error path only sets an error message; the frontend never calls the existing cancel-intent endpoint (`PaymentsController.cs:101-102`; grep finds no frontend usage of cancel-intent), and there is no server-side reconciliation of orphaned PaymentIntents.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`
- **Why it matters:** A race (stock sold out between cart and order creation) or any server error charges the customer real money with no order record linked to them — chargeback and trust risk.
- **Recommended fix:** Invert the flow: create the order (Pending) first, create the PaymentIntent server-side from the order, confirm client-side, mark Paid via webhook. Until then, at minimum auto-refund/cancel the intent when createOrder fails. (Effort: Medium.)
- **Acceptance criteria:** Forced order-creation failure after charge results in automatic refund/cancel, covered by an integration test.
- **Dependencies or follow-up:** Bundle with the P0 payment linkage refactor.
- **Confidence:** verified. Verifier confirmed: charge confirmed before createOrder with the error path only setting a message; zero frontend usage of the cancel-intent endpoint; webhooks for orphaned PaymentIntents are dropped with a 200 and the intent is created without an orderReference, so the charge is entirely unlinked — severe but window-limited and manually recoverable via the Stripe dashboard, hence P1.

### 12. In-app notifications: full backend API with zero producers and zero UI

- **Finding:** In-app notifications consist of a full backend API with zero producers and zero UI.
- **Category:** Implemented-but-unreachable / Plan 12
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** NotificationsController exposes list/summary/read/read-all/delete (`NotificationsController.cs:21-58`) and CreateNotificationCommand exists, but grep finds no code anywhere that sends CreateNotificationCommand (no producers in order/auth/review flows), and no frontend file references api/notifications (repo grep: zero hits); the header has no notification bell (`header.component.ts`). So the notification list would be empty even if a UI existed.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/NotificationsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Notifications/Commands/CreateNotificationCommand.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/layout/header/header.component.ts`
- **Why it matters:** Plan 12's core promise (order status updates via in-app notifications) is unmet; the API is dead code adding surface area without product value.
- **Recommended fix:** Either complete the loop (emit notifications from order-status changes + a header bell consuming /api/notifications/summary) or descope: remove the controller and mark plan 12 in-app scope as deferred. (Effort: Medium.)
- **Acceptance criteria:** Order status change creates a notification visible in a header dropdown, or the endpoints are removed and plan/docs updated.
- **Dependencies or follow-up:** Requires admin order-status UI or webhook fix to have any status changes to notify about.
- **Confidence:** verified. Verifier adjusted P1 → P2: the dead-code claim is fully verified (zero senders, zero frontend hits, no bell), but the gap is already documented as known remaining work (CLAUDE.md 'Partial', Plan 18), the endpoints are [Authorize]-protected, and no UI misleads users — a wire-up-or-descope cleanup of tracked unfinished work.

### 13. Email verification is a three-quarters-built stub: endpoint and client method exist, but no email is sent and no route exists to land on

- **Finding:** Email verification is a three-quarters-built stub: the endpoint and client method exist, but no email is sent and no frontend route exists to land on.
- **Category:** Implemented-but-unreachable / Auth
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** POST /api/auth/confirm-email exists (`AuthController.cs:101-102`) and ConfirmEmailCommandHandler works via UserManager (`Auth/Handlers/ConfirmEmailCommandHandler.cs:24-42`). `auth.service.confirmEmail()` exists (`auth/services/auth.service.ts:266-268`) but no component/route calls it (app.routes.ts has no confirm-email route; grep finds no other usage). RegisterCommandHandler never generates or emails a confirmation token (`Auth/Handlers/RegisterCommandHandler.cs:24-56`) and LoginCommandHandler never checks EmailConfirmed (only IsActive, `LoginCommandHandler.cs:42`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/AuthController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Auth/Handlers/RegisterCommandHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/services/auth.service.ts`
- **Why it matters:** Unverified emails mean typo'd addresses silently break all future transactional mail (once implemented), and the half-built feature misleads maintainers into thinking verification exists.
- **Recommended fix:** Either complete it (send confirmation email on register, add /confirm-email route + component, optionally gate sensitive actions) or delete the endpoint/client method and document email verification as out of scope. (Effort: Medium.)
- **Acceptance criteria:** Registration sends a confirmation link that lands on a working page and flips EmailConfirmed, or the dead pieces are removed.
- **Dependencies or follow-up:** Email sending infrastructure wiring (same as password-reset fix).
- **Confidence:** verified

### 14. Implemented-but-unreachable feature cluster: comparison, recently-viewed, price history, GDPR export/delete, inventory admin API, admin translation/relations editors

- **Finding:** A cluster of implemented features is unreachable from the UI: comparison, recently-viewed, price history, GDPR export/delete, inventory admin API, and the admin translation/relations editors.
- **Category:** Implemented-but-unreachable / Dead code
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** ComparisonService + CompareButtonComponent exist (`core/services/comparison.service.ts`; `shared/components/compare-button/`) but no component imports CompareButtonComponent and no /compare route exists (grep + app.routes.ts). RecentlyViewedComponent (`shared/components/recently-viewed/`) is likewise imported nowhere. PriceHistoryController (`Controllers/PriceHistoryController.cs:19-20`) has zero frontend callers (grep 'price-history' in Web: none). GdprController export/delete/rights (`GdprController.cs:28-98`) has no UI in the account area (account.routes.ts has only dashboard/profile/orders/addresses). InventoryController admin stock endpoints (`InventoryController.cs:22-65`) have no frontend usage. product-translation-editor and related-products-manager components exist under `features/admin/products/components/` with services calling api/admin/products, but their only possible parent (AdminProductsComponent) is a stub, so they are unmountable.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/comparison.service.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/shared/components/recently-viewed/recently-viewed.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PriceHistoryController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/GdprController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/InventoryController.cs`
- **Why it matters:** Significant built value (HVAC spec comparison is a genuinely strong conversion feature for this vertical) is invisible to users; GDPR self-service is legally relevant; meanwhile the dead code inflates maintenance and the test-coverage picture.
- **Recommended fix:** Triage each: wire compare button into product cards + add /compare page (high product value); embed recently-viewed on PDP/home; add price-history sparkline to PDP (price-drop trust signal); add GDPR export/delete section to /account/profile; surface inventory in the future admin products UI. Remove anything descoped. (Effort: Large.)
- **Acceptance criteria:** Each feature is either reachable from the UI with an E2E test or deleted with plan docs updated.
- **Dependencies or follow-up:** Admin items depend on admin panel build-out.
- **Confidence:** verified

### 15. /categories page is a 'Coming Soon' stub while remaining routable

- **Finding:** The /categories page is a 'Coming Soon' stub while remaining routable.
- **Category:** Broken flow / Stub page
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Route /categories loads CategoryListComponent (`app.routes.ts:58-62`), which is a 29-line stub rendering 'categories.comingSoon' (`features/categories/category-list/category-list.component.ts:9-14`). Category browsing actually works only via /products/category/:slug (`app.routes.ts:45-50`) and footer deep links.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/categories/category-list/category-list.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.routes.ts`
- **Why it matters:** Any user or crawler reaching /categories sees an unfinished shop; an HVAC store's category overview is a primary navigation surface.
- **Recommended fix:** Implement a category grid using the existing CategoriesController/category.service (both already exist), or remove the route and all links to it. (Effort: Small.)
- **Acceptance criteria:** /categories renders real category tiles linking to /products/category/:slug in all three languages.
- **Dependencies or follow-up:** None — backend categories API already exists.
- **Confidence:** verified

### 16. PDP trust badge hardcodes 'In Stock' for every product

- **Finding:** The PDP trust badge hardcodes 'In Stock' for every product.
- **Category:** UX correctness / Inventory
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** app-warranty-badge receives `[inStock]="true"` unconditionally (`product-detail/product-detail.component.ts:119`) and wishlist mapping hardcodes `inStock: true` (line 1020), despite the model exposing stockQuantity/inStock (`core/models/product.model.ts:41,70`). Stock is only enforced when AddToCartCommand rejects (`AddToCartCommand.cs:82-104`), surfacing as an error toast after the page promised availability.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs`
- **Why it matters:** Out-of-stock products display a green 'In Stock' trust badge — misleading advertising and a frustrating add-to-cart failure; no back-in-stock or low-stock UX exists.
- **Recommended fix:** Bind the badge to the selected variant's real stock, disable add-to-cart with an out-of-stock state, show low-stock hints (data already returned by cart/product APIs). (Effort: Small.)
- **Acceptance criteria:** A zero-stock variant shows out-of-stock UI and a disabled CTA; E2E covers it.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 17. CLAUDE.md status table materially misrepresents the product (Admin 'Complete', 'Email notifications implemented', guest checkout, TS Playwright commands)

- **Finding:** The CLAUDE.md status table materially misrepresents the product: Admin 'Complete', 'Email notifications implemented', guest checkout, and TypeScript Playwright commands all contradict the code.
- **Category:** Docs vs implementation mismatch
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** CLAUDE.md claims 'Admin Panel | Complete | CRUD, dashboard' (admin pages are stubs, see P0 finding), 'Notifications System | Partial | Email notifications implemented' (only GDPR-delete email is wired), 'Checkout & Orders | Complete | Stripe integration' (payment linkage broken), and business rule 'guest checkout stores email' (blocked by authGuard). CLAUDE.md's E2E commands ('cd tests/ClimaSite.E2E && npx playwright test') do not match reality: tests/ClimaSite.E2E is a C# Playwright project (ClimaSite.E2E.csproj, PageObjects/, run via `dotnet test`). A duplicate, unused auth stack also persists at `src/ClimaSite.Application/Features/Auth/*` (the wired one is `ClimaSite.Application.Auth.*` per `AuthController.cs:2`), and unit tests in `tests/ClimaSite.Application.Tests/Features/Auth/` exercise the dead handlers.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Auth/Commands/RegisterCommandHandler.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/ClimaSite.E2E.csproj`
- **Why it matters:** Agents and humans plan against this table (per the repo's own workflow); false 'Complete' statuses are how the admin stubs and dead email path survived multiple 'merge ready' commits.
- **Recommended fix:** Correct the status table (Admin: Partial/stub UI; Notifications: stub; Checkout: broken payment linkage), fix E2E commands to `dotnet test`, and delete the duplicate Features/Auth handlers plus their tests after confirming the wired namespace. (Effort: Small.)
- **Acceptance criteria:** CLAUDE.md statuses match a re-audit; duplicate auth code removed; documented commands run as written.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 18. No coupon/discount-code capability and no admin management for promotions — 'Promotions' are content pages only

- **Finding:** There is no coupon/discount-code capability and no admin management for promotions — 'Promotions' are content pages only.
- **Category:** Product gap / Marketing
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** PromotionsController is read-only (GET list/slug/featured, `PromotionsController.cs:18-48`); repo-wide grep for coupon/voucher/discount-code in backend returns nothing; cart/checkout have no code-entry field (grep in cart.component.ts/checkout.component.ts: none). Order.DiscountAmount exists in totals (`Order.cs:130`) but nothing sets it. No Admin promotions controller or UI exists; promotion data comes from DataSeeder.cs only.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PromotionsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Core/Entities/Order.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/DataSeeder.cs`
- **Why it matters:** A nav-level 'Promotions' section raises the expectation of redeemable deals; marketing cannot run discount campaigns, and seeded-only promotions cannot be updated without a deploy.
- **Recommended fix:** Phase 1: admin CRUD for promotion content. Phase 2: coupon entity + validation endpoint + cart code field feeding Order.DiscountAmount. (Effort: Large.)
- **Acceptance criteria:** Admin can create a promotion without a deploy; a coupon code changes the order total end-to-end.
- **Dependencies or follow-up:** Admin panel build-out; pricing-consistency fixes first.
- **Confidence:** verified

## Dimension data

### A. Full Flow Inventory (Flow | Status | Missing pieces | Evidence)

| Flow | Status | Missing pieces | Evidence |
|---|---|---|---|
| Browse / product list (filters, facets, sort, category) | Complete | — | `features/products/product-list/product-list.component.ts:32-110+` (filter sidebar, price/brand filters); routes `app.routes.ts:36-56` |
| Search (header, desktop+mobile) | Complete | No autocomplete/suggestions | `core/layout/header/header.component.ts:68-83,222` |
| PDP (gallery, specs, reviews, Q&A, similar, consumables, installation upsell, energy rating) | Complete (minor) | 'In Stock' badge hardcoded true (`:119`); no OOS/back-in-stock UX; price history not shown | `features/products/product-detail/product-detail.component.ts:17-293` |
| Add to cart (guest session, stock-validated) | Complete | — | `AddToCartCommand.cs:82-104` |
| Cart page | Complete | Shipping always shown 0/free (cart never includes shipping) | `GetCartQuery.cs:115-126`; checkout summary `checkout.component.ts:331-354` |
| Cart merge on login | Complete | — | `auth/services/auth.service.ts:132-150` (cart + wishlist merge) |
| Checkout (auth users, card) | Partial / **broken money path** | Charge=cart total (no shipping) in BGN; order total +shipping in EUR; UI shows USD; shipping tier names mismatch backend; payment-before-order with no rollback | `checkout.component.ts:1140-1232`, `CreateOrderCommand.cs:155-202`, `PaymentsController.cs:41-45` |
| Stripe webhooks → order status | **Stub in effect** | PaymentIntentId never persisted on order → webhook never matches; `Order.SetPaymentInfo` zero callers | `Order.cs:185-190`, `HandleStripeWebhookCommand.cs:39-48` |
| Order confirmation page | Complete (auth users) | Guests denied by `GetOrderByIdQuery.cs:44` | `app.routes.ts:90-94` |
| Guest checkout | Stub / unreachable | authGuard on /checkout; Payments [Authorize]; no guest order viewing | `app.routes.ts:86`, `OrdersController.cs:23-32`, `PaymentsController.cs:10` |
| Order history + details (filter, sort, cancel, reorder, invoice PDF, tracking display) | Complete | Status will always read Pending until P0 fixes land | `features/account/orders/`, `order-details.component.ts:48-91,195-198`, `OrdersController.cs:110-157`, `GenerateInvoiceQuery.cs` (QuestPDF) |
| Order cancel | Complete (restock) | No automatic Stripe refund on cancel of paid order | `CancelOrderCommand.cs:79-102` |
| Registration / login / refresh / logout | Complete | No welcome email | `AuthController.cs:23-70`, `Auth/Handlers/RegisterCommandHandler.cs` |
| Password reset | **Stub** | Email never sent (token logged only) | `ForgotPasswordCommandHandler.cs:34-41` |
| Email verification | **Stub** | No send, no frontend route, not enforced at login | `AuthController.cs:101`, `auth.service.ts:266`, `LoginCommandHandler.cs:42` |
| Profile (update, change password, preferences) | Complete | No GDPR export/delete UI | `features/account/profile/profile.component.ts:159-490` |
| Addresses CRUD | Complete | — | `AddressesController.cs`, `features/account/addresses/` |
| GDPR export/delete | Backend only | No UI anywhere | `GdprController.cs:28-98` |
| Wishlist (incl. new sharing, guest localStorage, merge) | Complete (this branch) | — | `WishlistController.cs:21-86`, `features/wishlist/wishlist.component.ts:34-79`, `wishlist.service.ts:56-236` |
| Reviews & ratings (verified purchase, helpful votes) | Complete | — | `ReviewsController.cs:26-120`, `CreateReviewCommand.cs:95` |
| Product Q&A | Complete | — | `QuestionsController.cs`, `product-qa.component` on PDP |
| Admin dashboard | Stub (nav links only) | KPI/revenue/low-stock endpoints (`AdminDashboardController.cs:20-56`) unused | `admin-dashboard.component.ts` (77 lines) |
| Admin products CRUD | **Stub UI** ('Coming Soon') | Full backend exists (`Admin/AdminProductsController.cs:25-177`); translation-editor & related-products-manager components orphaned | `admin-products.component.ts` (12 lines) |
| Admin orders mgmt | **Stub UI** | Backend status/shipping/notes exists (`AdminOrdersController.cs:21-78`) | `admin-orders.component.ts` (12 lines) |
| Admin customers | **Stub UI** | Backend exists (`AdminCustomersController.cs:21-40`) | `admin-users.component.ts` (12 lines) |
| Admin moderation (reviews + questions) | Complete | Only fully built admin page | `admin-moderation.component.ts` (764 lines), `moderation.service.ts:70` |
| Inventory mgmt | Backend only | No UI for stock adjust/threshold/bulk | `InventoryController.cs:22-65` |
| Promotions | Partial | Content pages only; no coupons; no admin CRUD; seeded data only | `PromotionsController.cs:18-48`, `features/promotions/` |
| Brands (list/detail) | Complete | — | `features/brands/brands.routes.ts`, `BrandsController.cs` |
| Installation service requests | Partial / dead-end | No list endpoint, no admin view, no business email | `InstallationController.cs:22-34` |
| Email notifications | **Stub** (except GDPR-delete) | Order confirm/shipped/welcome/reset implemented in `EmailService.cs` but never called | grep IEmailService → only `Features/Gdpr/Commands/DeleteUserDataCommand.cs` |
| In-app notifications | Backend only, no producers, no UI | No bell, no notification.service, nothing creates notifications | `NotificationsController.cs:21-58` |
| Comparison | Dead code | Service+button built; button imported nowhere; no /compare route | `comparison.service.ts`, `compare-button.component.ts` |
| Recently viewed | Dead code | Component imported nowhere | `recently-viewed.component.ts` |
| Price history | Backend only | No PDP chart | `PriceHistoryController.cs:19` |
| Categories overview page | Stub | 'Coming Soon'; category browsing works via /products/category/:slug | `category-list.component.ts:12` |
| Contact | **Fake** | setTimeout-simulated success, no API | `contact.component.ts:393-398` |
| Home v3 (configurator + real recommendations) | Complete | — | `home-v3.component.html:22-28`, `ProductsController.cs:221-231` |
| Legal/support pages (terms, privacy, cookies, FAQ, shipping, returns) | **Missing (404)** | Footer links exist, routes don't; no cookie consent | `footer.component.ts:43-56` vs `app.routes.ts` |
| Resources page | Complete (static) | — | `features/resources/` |

### B. Backend-without-frontend / Frontend-without-backend map

**Backend with no UI:** AdminProducts/AdminOrders/AdminCustomers/AdminDashboard (UI stubs), GdprController, PriceHistoryController, InventoryController, NotificationsController (also no producers), Orders guest path (GuestSessionId), `Order.SetPaymentInfo`, IEmailService methods (welcome/reset/order-confirm/shipped), `OrdersController.GetOrderByNumber` (no track-order page).

**Frontend with no live backend / wiring:** Contact form (simulated), PayPal payment option, 'overnight'/'free-standard' shipping tiers (mismatch backend switch), CompareButton + ComparisonService, RecentlyViewedComponent, admin product-translation-editor & related-products-manager (orphaned under stub parent), `auth.service.confirmEmail` (no route calls it).

**Duplicate/dead code:** `src/ClimaSite.Application/Features/Auth/*` parallel to the wired `src/ClimaSite.Application/Auth/*` (AuthController imports `ClimaSite.Application.Auth.Commands`, AuthController.cs:2); unit tests at `tests/ClimaSite.Application.Tests/Features/Auth/` cover the dead variant.

### C. Plan-vs-reality dispositions

| Plan/doc claim | Reality | Disposition |
|---|---|---|
| CLAUDE.md 'Admin Panel: Complete' | 3 of 4 admin pages are 12-line stubs | False — fix table |
| CLAUDE.md 'Notifications: Partial — Email notifications implemented' | Only GDPR-delete email wired; zero transactional emails | Overstated |
| CLAUDE.md 'Checkout & Orders: Complete — Stripe integration' | Payment never linked to order; totals/currency inconsistent | Materially false |
| CLAUDE.md 'guest checkout stores email' | Frontend authGuard blocks guests; payments require auth | Unreachable |
| CLAUDE.md E2E commands (`npx playwright test`) | E2E is a C# Playwright project run via `dotnet test` | Docs wrong |
| Plan 12 (NOT-001..020) | Entities/API partially exist; producers, emails, prefs, UI all missing (checklists unchecked at `docs/plans/12-notifications-system.md:315+`) | Honest in plan; CLAUDE.md table is the misleading artifact |
| Plan 18 Phase 2 'Wishlist slice complete' | Verified: share endpoints, share UI, merge-on-login, hydrated DTO service, new test files in working tree | Accurate |
| Plan 18 Phase 1 Home v3 'complete, real recommendations' | Verified: `/api/products/recommendations` + home-v3 wiring | Accurate |
| docs/validation/areas/08-admin-panel.md 'Critical: stub admin components' | Still true in working tree | Re-verified, not yet fixed |
| docs/validation/areas/03-checkout-payments.md | Flags hardcoded shipping rates but NOT the PaymentIntent-never-persisted defect or currency mismatch | Gap in prior audit — new findings here |

### D. Highest-leverage path to a sellable shop (suggested order)

1. Fix payment linkage + server-side amount/currency + order-first flow (P0 cluster) — without this nothing else matters.
2. Admin orders page (status + tracking) — minimum operability; then transactional emails (confirmation/shipped) which depend on status changes.
3. Password-reset email (small, severe).
4. Legal pages + cookie consent + real contact endpoint — EU legal floor; reuse existing GDPR API in a privacy page.
5. Admin products CRUD (wire existing orphaned editor components).
6. Decide guest checkout stance; kill or finish fake payment methods.
7. Surface quick wins: compare page, recently-viewed, price-history sparkline, categories page, real OOS badge.

### E. Open questions for the owner

1. Intended sale currency: BGN, EUR, or per-locale? (Code currently mixes all three plus USD display.)
2. Is guest checkout in scope for v1? Backend half supports it.
3. Should email verification gate anything, or be dropped for v1?
4. Are PayPal / bank transfer real roadmap items or should the options be removed now?
5. Who receives installation-request leads operationally (email inbox vs admin queue)?
6. Is the duplicate `Features/Auth` stack intentionally kept for migration, or deletable?

## Refuted during verification

None.
