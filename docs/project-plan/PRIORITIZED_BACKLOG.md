# ClimaSite — Prioritized Backlog

**Date:** 2026-06-11
**Sources:** All ten verified review files in `docs/project-plan/_review/` plus the wave-1 documents (`SECURITY_REVIEW.md`, `TESTING_STRATEGY.md`, `UI_UX_REVIEW.md`, `BUGS_AND_TECH_DEBT.md`, `ARCHITECTURE_REVIEW.md`, `DECISIONS.md`, `DEV_WORKFLOW.md`).

**How to use this document:** This is THE actionable, deduplicated task list for ClimaSite — one task may close findings from several review dimensions, and every task lists the findings it closes so nothing is double-worked. Categories are ordered by the urgency of their highest-priority items, and tasks within each category are ordered P0→P3, but the **"Next 10 tasks"** section at the end is the strict execution order — start there. Each task carries ID, priority (P0 critical blocker / P1 important / P2 useful / P3 polish), complexity (Small/Medium/Large), affected files, acceptance criteria, and dependencies; full evidence and `file:line` proof live in the cited `_review/*.md` finding and wave-1 docs — do not re-derive analysis, just execute. `BUG-nn` and `TS-nn` IDs deliberately match `BUGS_AND_TECH_DEBT.md` and `TESTING_STRATEGY.md`; `SR-nn` refers to findings in `SECURITY_REVIEW.md`; `D-nnn`/`O-n` refer to `DECISIONS.md`. Anything marked **Needs confirmation** requires owner input before (or while) starting.

**Relationship to `docs/plans/18-project-completion.md`:** Plan 18 remains the active master plan and its Phase 5 (SEC-100..107) and Phase 7 (PROD-100..107) tasks are still valid — they are cited inline below. However, Plan 18 **does not track** several launch blockers found by this review (admin UI build-out, payment reconciliation, DataSeeder credential gating, forwarded headers, order-by-number IDOR); those are new tasks here and should be added to Plan 18's phases. Where this backlog and older trackers conflict (`docs/plans/00-master-overview.md` "100% Complete" claims, `docs/plans/20-issue-registry.md` "129 open / 0 fixed"), **this backlog wins** — those docs are stale (see DOC-02/DOC-04).

---

> **External multi-agent review (2026-06-28) — triaged + council-verified.** A 12-dimension external code review (`/Users/sarkisharalampiev/Projects/climasite-review-20260628-211745`) was re-checked against the live code by 14 skeptical verifiers, with a Codex `gpt-5.5` cross-check of the high-stakes cluster. See **[`EXTERNAL_REVIEW_TRIAGE.md`](EXTERNAL_REVIEW_TRIAGE.md)** for the full 61-item register (57 CONFIRMED / 3 PARTIAL / 1 ALREADY_FIXED; post-council **2 High, 32 Medium, 25 Low, 2 None**). It largely **validates this backlog and attaches `file:line` evidence** to existing items (e.g. B-011→SEC-05, B-002→BUG-06 admin slice, B-040→SEC-09, B-046→SEC-04, B-006→BUG-08, B-043→BUG-12, B-013/B-019→PERF-01, B-027/B-028→SEC-14, B-004/B-017→OPS-07, B-045→OPS-04, B-024→SEC-07, B-022→SEC-08) — it is **not a new parallel backlog**. The genuinely-new confirmed items are registered in [§ External review — newly-tracked items](#external-review--newly-tracked-items-2026-06-28-triaged) below. Only **two** items are post-council High: **B-011** (committed JWT fallback secret = SEC-05) and **B-002** (admin price inversion = BUG-06 admin slice); council fix-first = **B-011**.

---

## Blocking decisions (resolve early — they gate the tasks that cite them)

Per the owner's standing convention, these are **owner decisions**, recorded as ADRs (`docs/adr/`). Full context in `DECISIONS.md` §3.

| Ref | Decision | Gates |
|---|---|---|
| DEC-CURRENCY | **Store currency: EUR or BGN?** Code currently mixes EUR (orders/display), BGN (Stripe charge), USD (cart/checkout pipes). Single most blocking decision in the repo (`_review/bugs.md` open question 1). | BUG-01, BUG-02, BUG-11, BUG-13 |
| DEC-GUEST | Guest checkout in scope for v1? Backend half-supports it; route guard blocks it; docs claim it works. | GAP-07, TS-13 |
| DEC-SHIPPING | ✅ RESOLVED 2026-06-27: **free standard shipping over a €50 subtotal**, €5.99 below (express €15.99 / overnight €19.99 unchanged). Implemented server-side (`CheckoutPricing.GetShippingCost(method, subtotal)`, threshold €50) + UI mirror; displayed==charged verified (cross-vendor council). | BUG-11 (done) |
| O-1 | Background-job mechanism (BackgroundService + DB outbox vs Hangfire vs RabbitMQ from shared-infra) | ARCH-05, GAP-09, GAP-03 (reliability) |
| O-2 | API error contract: RFC 7807 ProblemDetails vs ratify current `{status,message,detail}` | ARCH-04 |
| O-3 | Query-caching owner: register `CachingBehavior` + invalidation vs delete it; vs OutputCache named policies | PERF-01 |
| O-4 | Observability vendor (Sentry suggested) + OpenTelemetry scope | OPS-05 |
| O-5 | ADR 003 (Canvas 2D) + ADR for wishlist share-token design | DOC-05, OPS-01 (ideally before merge) |
| O-6 | Shared-infra vs project-local docker-compose for local dev | OPS-10 |
| O-7 | **Railway topology: is anything deployed today?** Which services/config files, auto-deploy status, backups. Determines whether SEC-01 is an *active* P0 and the KnownProxies config for SEC-03. | SEC-01, SEC-03, OPS-03, OPS-08, BUG-09 |
| ~~DEC-SEARCH~~ | ✅ **DECIDED + DONE 2026-06-28 (SEARCH-01-fts):** Postgres FTS (trigger-maintained `search_vector`, `ts_rank_cd`) + pg_trgm substring fallback hybrid — chosen over Meilisearch (no extra infra at this catalog scale). | PERF-05 |
| DEC-SSR | SSR/prerender for the public storefront vs client-only meta | PERF-07 |

---

## 1. Security (SEC)

Detail and evidence: `docs/project-plan/SECURITY_REVIEW.md` (SR-01..SR-20) and `_review/security.md`, `_review/devops.md`, `_review/performance.md`.

### SEC-01 — Gate DataSeeder: no hardcoded admin credentials or demo data in production (P0, Small)
- **Status:** ✅ DONE (2026-06-15, branch `fix/sec-01-gate-data-seeder`). DataSeeder env-gated; prod admin bootstrapped from `ADMIN_EMAIL`/`ADMIN_INITIAL_PASSWORD`; 4 Testcontainers regression tests in `DataSeederTests.cs`. OPS-08 confirmed nothing is deployed → latent, no credential rotation needed.
- **Description:** `Program.cs:31-44` runs `DataSeeder` unconditionally at startup; `DataSeeder.cs:64-65` hardcodes `admin@climasite.local` / `Admin123!` (EmailConfirmed=true) and seeds a demo catalog. The repo is **public** (byrzohod/climasite), so the credential is published. Gate role seeding as always-on; run admin/product/promotion seeding only in Development/Testing; bootstrap the production admin from required env vars (`ADMIN_EMAIL`/`ADMIN_INITIAL_PASSWORD`) and fail startup if absent.
- **Closes:** SR-01; `_review/devops.md` #1 (P0 confirmed); `_review/status.md` #5. **Not tracked by Plan 18** — add to Phase 5.
- **Affected:** `src/ClimaSite.Api/Program.cs`, `src/ClimaSite.Infrastructure/Data/DataSeeder.cs`, `Dockerfile.api`.
- **Acceptance:** Production startup against an empty DB creates no `admin@climasite.local` and no demo products; login with `Admin123!` fails in prod; integration test asserts env-gated seeding. If OPS-08 confirms anything is already deployed, rotate/delete the seeded admin **immediately**.
- **Depends on:** Coordinate with OPS-04 (same startup code path); OPS-08 (Needs confirmation: is it live?).

### SEC-03 — `UseForwardedHeaders` before the rate limiter (P0, Small)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** No forwarded-headers handling anywhere; nginx (`nginx.conf.template`) proxies all `/api` traffic, so every shopper shares one `RemoteIpAddress` → one global 100 req/min bucket (site-wide 429s at ~5-15 concurrent users) and one shared 10/min auth bucket (one user can lock out all logins). Add `ForwardedHeadersOptions` (XForwardedFor | XForwardedProto, KnownNetworks/KnownProxies for the Railway/nginx hop) early in the pipeline, before `UseRateLimiter` and `UseHttpsRedirection`.
- **Closes:** SR-06; `_review/performance.md` #1 (P0 confirmed); `_review/devops.md` #4; `_review/security.md` #5. Complements Plan 18 SEC-104 (pointless until this lands). **Not tracked by Plan 18.**
- **Affected:** `src/ClimaSite.Api/Program.cs:208-241`, `src/ClimaSite.Web/nginx.conf.template`.
- **Acceptance:** Through the nginx container, two clients with distinct `X-Forwarded-For` get independent rate-limit buckets; logged request IPs match the real client; KnownProxies restricted (no IP spoofing of the limiter).
- **Depends on:** O-7 for the exact trusted-proxy config (start with the nginx hop regardless).

### SEC-02 — Fix order-by-number IDOR / PII leak (P1, Small)
- **Description:** Anonymous `GET /api/orders/by-number/{orderNumber}` skips the ownership check when userId is null (`GetOrderByNumberQuery.cs:43`), returning full customer email/phone/address/items; order numbers have only 4 hex chars of randomness. Reject anonymous callers (mirror `GetOrderByIdQuery`) or require a per-order opaque confirmation token for guest lookups.
- **Closes:** SR-04; `_review/security.md` #1 (P1 confirmed). **Not tracked by Plan 18.**
- **Affected:** `src/ClimaSite.Api/Controllers/OrdersController.cs:98-108`, `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByNumberQuery.cs`.
- **Acceptance:** Anonymous request returns 401/404 with no PII; cross-user access denied; admin still allowed; integration test for all three cases.
- **Depends on:** None. Coordinate with GAP-07 if guest order confirmation is enabled.

### SEC-11 — Pre-launch penetration checklist run (P1, Medium)
- **Description:** Execute the manual pen-test list in `SECURITY_REVIEW.md` ("Areas requiring manual penetration testing"): guest-cart session enumeration, payment amount reconciliation, JWT forgery on non-Production env names, rate-limit behavior behind real proxy, TestController reachability, login brute-force/lockout, order-number enumeration, cross-user authorization sweep over every `{id:guid}` endpoint.
- **Closes:** SECURITY_REVIEW.md pen-test section; gates the production checklist there.
- **Affected:** Deployed staging environment.
- **Acceptance:** Each checklist item has a recorded pass/fail + remediation ticket.
- **Depends on:** SEC-01..SEC-08, BUG-01/02 fixed first; a deployed environment (OPS-03).

### SEC-04 — Exclude TestController from Release builds; strong secrets on all test endpoints (P2, Small)
- **Description:** TestController (DB wipe, admin elevation) ships in the prod image gated only by environment-name string; cleanup has no secret; Development falls back to hardcoded `"test-admin-secret"`. Exclude via `#if DEBUG` or conditional registration; require non-default secrets for all `/api/test/*` endpoints.
- **Closes:** SR-08; `_review/security.md` #3 (P2 adjusted — Dockerfile pins env to Production, so defense-in-depth).
- **Affected:** `src/ClimaSite.Api/Controllers/TestController.cs`, `.github/workflows/test.yml`.
- **Acceptance:** Release/Production build returns 404 for all `/api/test/*`; no hardcoded default secret in source; CI E2E still passes with configured secret.
- **Depends on:** None.

### SEC-05 — Remove committed JWT secret; require `JWT_SECRET` in all non-Development environments (P2, Small)
- **Description:** `appsettings.json:22` ships a usable placeholder signing key; `JwtConfiguration.ResolveSecret` only enforces env-var for the literal "Production" name — Staging/QA silently sign with the public key (full token forgery). Fail fast on missing or placeholder secret in every non-Development environment.
- **Closes:** SR-09; `_review/security.md` #6. Extends Plan 18 **SEC-100** (its "prod is safe" note is superseded — see SECURITY_REVIEW.md stale-doc note).
- **Affected:** `src/ClimaSite.Api/appsettings.json`, `src/ClimaSite.Api/Configuration/JwtConfiguration.cs`, `src/ClimaSite.Infrastructure/Services/TokenService.cs`.
- **Acceptance:** App refuses to start in any non-Development env without an external `JWT_SECRET`; committed config contains no usable key.
- **Depends on:** None.

### SEC-06 — Gate Swagger out of production (P2, Small)
- **Status:** ✅ DONE (2026-06-27). Wrapped `UseSwagger`/`UseSwaggerUI` in `if (app.Environment.IsDevelopment())` — the API schema + UI are now served **only in Development** (off in Production/Staging/Testing). Integration-tested: `/swagger/index.html` + `/swagger/v1/swagger.json` → 404 in the (non-Dev) Testing factory; the `/swagger` path still carries the security headers + no CSP. Council-reviewed.
- **Description:** `UseSwagger`/`UseSwaggerUI` run unconditionally (`Program.cs:271-277`). Wrap in `IsDevelopment()` or protect behind auth/flag per Plan 18 **SEC-102**.
- **Closes:** SR-10; `_review/security.md` #4.
- **Affected:** `src/ClimaSite.Api/Program.cs`.
- **Acceptance:** `/swagger` and `/swagger/v1/swagger.json` 404 (or require auth) in Production; available in Development.
- **Depends on:** None.

### SEC-07 — Standardize secret env-var names; remove dummy Stripe keys; production fail-fast (P2, Small)
- **Status:** ✅ DONE (Stripe) 2026-06-27. Emptied the committed dummy Stripe keys in `appsettings.json` (0 dummy keys in tracked source); added `StripeConfiguration.ValidateProductionConfiguration` (mirrors `JwtConfiguration`) wired in `ConfigureServices` → **Production fail-fasts at startup** on missing/placeholder Stripe SecretKey/WebhookSecret/PublishableKey; safe in Dev/Testing (scoped `StripePaymentService` + `FakePaymentService` swap). `CLAUDE.md` env table corrected to `Stripe__*` (the names the code reads) + points to `docs/runbooks/deploy.md`. JWT was **already** prod-guarded (`JwtConfiguration`). Tests: `StripeConfigurationTests` (5) + startup integration green. **Remaining (follow-up):** same prod-validation pattern for SMTP (`Email__*`) + MinIO (`Minio__*`) — out of this unit's scope.
- **Closes:** SR-11; `_review/devops.md` #10. Overlaps Plan 18 SEC-100.
- **Affected:** `src/ClimaSite.Infrastructure/Services/StripePaymentService.cs`, `EmailService.cs`, `Infrastructure/DependencyInjection.cs` (MinIO), `src/ClimaSite.Api/appsettings.json`, `CLAUDE.md` env table.
- **Acceptance:** Booting Production without real Stripe config throws at startup; documented variable names proven by a startup-validation test.
- **Depends on:** None.

### SEC-08 — Security headers middleware + CORS/HSTS/AllowedHosts tightening (P2, Medium)
- **Status:** 🚧 HEADERS DONE (2026-06-26). Added `SecurityHeadersMiddleware` (unconditional, early in the pipeline): `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `X-XSS-Protection: 0`, `Permissions-Policy`, and a strict `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; base-uri 'none'` for the JSON API (skipped for `/swagger`). Integration-tested (`SecurityHeadersTests`, 2/2). Note: the API does NOT serve the SPA, so its CSP is safely strict. **Remaining (deploy-time, OPS-08):** the Stripe-compatible **frontend** CSP (where the SPA is served), the CORS header allowlist (`AllowAnyHeader` → explicit, SEC-103), and `AllowedHosts` tightening — these depend on the deploy topology.
- **Description:** No security-headers middleware exists; CORS uses `.AllowAnyHeader()`; `AllowedHosts="*"`. Implement Plan 18 **SEC-101** (CSP compatible with Stripe, X-Frame-Options DENY, nosniff, Referrer-Policy, Permissions-Policy + integration test) and **SEC-103** (explicit CORS allowlist); set explicit AllowedHosts and tune HSTS.
- **Closes:** SR-12; Plan 18 SEC-101/SEC-103.
- **Affected:** `src/ClimaSite.Api/Program.cs`, new middleware.
- **Acceptance:** Integration test asserts all headers on API responses; checkout (Stripe iframe) still works under the CSP.
- **Depends on:** None.

### SEC-09 — Hash refresh tokens at rest (P3, Small)
- **Description:** Plaintext refresh tokens stored on `ApplicationUser` and matched by equality; store SHA-256 hashes, compare hashed; optionally move to a per-device RefreshTokens table.
- **Closes:** SR-17; `_review/security.md` #8.
- **Affected:** `src/ClimaSite.Core/Entities/ApplicationUser.cs`, `src/ClimaSite.Application/Auth/Handlers/RefreshTokenCommandHandler.cs`, migration.
- **Acceptance:** DB stores only hashes; refresh flow still works; existing sessions invalidated knowingly (release note).
- **Depends on:** ARCH-01 first (don't touch the auth chain while its unit tests target the dead tree).

### SEC-10 — Remove owner `UserId` from the anonymous shared-wishlist response (P3, Small)
- **Description:** `GET /api/wishlist/shared/{shareToken}` returns the owner's internal UserId GUID (`WishlistDto.cs:6`); omit it (and any non-essential owner identifiers) from the anonymous path; consider a named rate-limit policy on the endpoint. Note: the anonymous GET also inherits the 5-minute blanket output cache, so revoked share links keep serving — fixed by PERF-01.
- **Closes:** SR-19; `_review/security.md` #10.
- **Affected:** `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `WishlistDto.cs`, `src/ClimaSite.Api/Controllers/WishlistController.cs`.
- **Acceptance:** Shared-wishlist response contains no `userId`; test asserts absence.
- **Depends on:** Best done as a rider on OPS-01 (the wishlist branch is uncommitted — cheapest moment is now).

### SEC-12 — Resolve the high-severity Angular advisories (upgrade Angular 19 → current) (P1, Large)
- **Status:** OPEN (surfaced 2026-06-21 by the Wave 3a dependency-audit). `npm audit` reports 8 vulns (7 high, 1 moderate), incl. `@angular/core <=19.2.25` (DOM-clobbering / response-cache-poisoning, GHSA-rgjc-h3x7-9mwg). The only fix is a major Angular upgrade (19 → 20/21/22), a breaking migration out of scope for the Wave 3 gates PR — so the CI `npm audit` step is **informational** until this lands.
- **Description:** Plan and execute the Angular major upgrade (and the other 6 high-sev transitive deps it carries), via the gated per-feature pipeline (`/feature-kickoff SEC-12`). Re-run `npm audit`; once clean, flip the CI npm-audit step to blocking.
- **Affected:** `src/ClimaSite.Web/package.json`, `package-lock.json`, Angular API surface across the app.
- **Acceptance:** `npm audit --omit=dev` reports 0 high/critical; frontend builds + all specs/E2E green; CI npm-audit step enforced (non-informational).
- **Depends on:** None, but large — own feature folder + plan + verify-plan.

### SEC-13 — Allowlist + enforce the gitleaks secret-scan gate (P2, Small)
- **Status:** OPEN (2026-06-21). The Wave 3a `dependency-audit` job runs gitleaks but `continue-on-error: true` (informational) to avoid false-positive blocks on the repo's dummy/test secrets (appsettings dummy keys, test JWT secrets) before an allowlist exists.
- **Description:** Add a `.gitleaks.toml` allowlist for the known non-secret test/dummy values, confirm a clean run in CI, then remove `continue-on-error` so gitleaks is a hard gate.
- **Affected:** `.gitleaks.toml` (new), `.github/workflows/test.yml` (dependency-audit job).
- **Acceptance:** gitleaks passes clean on the repo; the step is blocking (no `continue-on-error`); a planted fake secret fails CI.
- **Depends on:** Pairs with SEC-07 (remove dummy Stripe keys) — fewer allowlist entries needed once those are gone.

### SEC-14 — GDPR erasure leaves Orders PII intact (Article 17 minimization) (P1, Medium)
- **Status:** ✅ DONE (2026-06-27, ADR-0004). `Order.AnonymizePersonalData()` scrubs email/phone/shipping+billing address (the dict held the name) on account deletion; `DeleteUserDataCommandHandler` now anonymizes the user's orders (by `UserId`) inside the execution-strategy transaction, while RETAINING the invoice record for the legal accounting-retention period (Art. 17(3)(b)). The confirmation email's "order history (anonymized)" claim is now true. Integration-tested (`GdprControllerTests.DeleteAccount_AnonymizesOrderPii_ButRetainsTheOrderRecord`; 10/10 GDPR tests green). Follow-ups: a scheduled retention-sweep to hard-purge past 7y; guest-order (no UserId) policy.
- **Description:** Decide the legal-basis-to-retain (tax/invoice records may be retained, but personal data within them should be minimized/pseudonymized) — **write an ADR** — then anonymize the order-level PII for a deleted user (or document the retention decision so the email copy matches reality). Also strengthen `GdprControllerTests` to assert `PasswordHash == null`, security-stamp rotation, and that child rows (addresses/notifications) are gone.
- **Affected:** `src/ClimaSite.Application/Features/Gdpr/Commands/DeleteUserDataCommand.cs`, `src/ClimaSite.Core/Entities/Order.cs`, `docs/adr/`, `tests/ClimaSite.Api.Tests/Controllers/GdprControllerTests.cs`.
- **Acceptance:** A deleted user's orders retain no readable email/phone/name/address (or an ADR records the lawful-basis exception); the confirmation email matches reality; tests assert it. Run `/security-review`.
- **Depends on:** none. Through the gated pipeline (GDPR → ADR + security-review).

---

## 2. Bugs (BUG)

IDs match `docs/project-plan/BUGS_AND_TECH_DEBT.md` exactly; full evidence there and in `_review/bugs.md`. The money-path bugs compound: BUG-02 charges the wrong amount, BUG-04 charges at the wrong time, BUG-01 loses the link between charge and order, BUG-18 throws away the webhook evidence. Fix BUG-02 + BUG-01 together, then BUG-04, then BUG-18 — all gated on **DEC-CURRENCY**.

> **Folded elsewhere (do not double-track):** BUG-14 (admin related-products search stub) → GAP-02; BUG-15 (guest checkout unreachable) → GAP-07; BUG-16 (admin "notify customer" no-op) → GAP-03.

### BUG-01 — Persist `paymentIntentId`; make Stripe webhooks reconcile orders (P0, Small)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** Frontend sends `paymentIntentId`/`paymentMethod` on order creation but `CreateOrderCommand` has no such fields — silently dropped; `Order.SetPaymentInfo` has zero callers; the webhook matches on always-null `PaymentIntentId`, so **every card order stays Pending forever** and refunds/failures are no-ops. Add both fields to the command, call `order.SetPaymentInfo(...)`, verify the intent's amount/currency/status server-side via `IPaymentService` before accepting, add a unique index on `orders.payment_intent_id` (idempotency).
- **Closes:** `_review/bugs.md` #1 (P0 confirmed); `_review/product.md` #1; SR-03.
- **Affected:** `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `src/ClimaSite.Api/Controllers/OrdersController.cs`, `src/ClimaSite.Application/Features/Payments/Commands/HandleStripeWebhookCommand.cs`, `src/ClimaSite.Web/src/app/core/services/checkout.service.ts`.
- **Acceptance:** TS-03 regression tests — order create persists the intent ID; simulated `payment_intent.succeeded` flips the order to Paid; `charge.refunded` flips to Refunded; E2E order history leaves Pending.
- **Depends on:** DEC-CURRENCY; pair with BUG-02; BUG-18 follows.

### BUG-02 — Compute the charge server-side: correct amount, one currency, shipping included (P0, Medium)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** Stripe is charged a **client-supplied** amount (any authenticated user can pay €0.01), hardcoded to BGN while orders are EUR (~49% underpayment), and the cart total omits the 5.99–15.99 shipping the order records. Create the payment intent from the server-calculated order total (subtotal + tax + shipping) in the store currency; never trust the client amount; reject mismatched intents in the webhook.
- **Closes:** `_review/bugs.md` #2 (P0 confirmed); `_review/product.md` #3; SR-02; SECURITY_REVIEW payments note.
- **Affected:** `src/ClimaSite.Api/Controllers/PaymentsController.cs:42-58`, `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`, `src/ClimaSite.Infrastructure/Services/StripePaymentService.cs`.
- **Acceptance:** Arbitrary client amount rejected/ignored; for any cart: displayed total == Stripe charge == `Order.Total`, one currency, shipping included; automated test compares all three.
- **Depends on:** **DEC-CURRENCY (blocking)**; pair with BUG-01.

### BUG-03 — Fix guest-cart merge contract: merge no longer 400s, guest items survive login (P1, Small)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** Frontend POSTs `/api/cart/merge` with a body + `X-Session-Id` header; backend requires `[FromQuery] string guestSessionId` → deterministic 400, swallowed by `console.warn` — **every first-time customer loses their visible cart at login** (and checkout is auth-gated, so this hits everyone). Change `CartService.mergeCart` to pass `?guestSessionId=` (or make the endpoint read the header); surface failures.
- **Closes:** `_review/bugs.md` #3 (verifier adjusted P0→P1: 100% reproducible but the guest cart row is never deleted server-side, so items are recoverable; treat as a first-wave launch blocker regardless — the fix is one line).
- **Affected:** `src/ClimaSite.Web/src/app/core/services/cart.service.ts:190-205`, `src/ClimaSite.Api/Controllers/CartController.cs:110-116`, `src/ClimaSite.Web/src/app/auth/services/auth.service.ts:133-142`.
- **Acceptance:** E2E (TS-03): guest adds 2 items, logs in, both items present, quantities combine when overlapping, guest cart row removed server-side.
- **Depends on:** None.

### BUG-04 — Order-before-charge (or compensation) + double-submit guard (P1, Medium)
- **Description:** `placeOrder()` confirms the Stripe payment before creating the order; order-creation failure (stock-out, validation) leaves a captured charge with no order and no refund; the processing flag is set too late, allowing double-charges from double-clicks. Create the order (Pending) first and derive the intent from it, or auto-cancel/refund on `createOrder` failure (`CancelPaymentIntentAsync` exists server-side, never called); set the processing flag at the top of `placeOrder()`.
- **Closes:** `_review/bugs.md` #4; `_review/product.md` #11; SR-07.
- **Affected:** `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `checkout.service.ts`, `CreateOrderCommand.cs`, `PaymentsController.cs`.
- **Acceptance:** Rapid double-click yields exactly one intent and one order; forced order failure after charge leaves no uncompensated captured intent (integration test).
- **Depends on:** BUG-01, BUG-02.

### BUG-05 — Stock decrement concurrency control (oversell) (P1, Medium)
- **Description:** Order creation validates and decrements stock with read-then-write under ReadCommitted — concurrent checkouts oversell the last unit; the documented "stock reservations" do not exist anywhere. Add a Postgres `xmin` concurrency token on `product_variants` with retry, or decrement atomically (`ExecuteUpdateAsync` with `stock >= qty` guard). Same pattern exists in the admin path (`AdjustStockCommand.cs:47-62`) — fix both.
- **Closes:** `_review/bugs.md` #5; CLAUDE.md "reservations" doc-drift (with DOC-02).
- **Affected:** `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs:128-146`, `src/ClimaSite.Core/Entities/ProductVariant.cs`, `src/ClimaSite.Api/Controllers/InventoryController.cs`.
- **Acceptance:** Concurrency test (TS-08): two parallel orders for the last unit — exactly one succeeds; stock never negative or double-decremented.
- **Depends on:** None (independent of payment fixes).

### BUG-06 — Un-invert sale-price mapping sitewide (P1, Medium) — ✅ public surfaces DONE; admin slice (B-002) DONE 2026-06-29
- **Status:** The shared `ProductPricing` contract + centralized mapper landed and all public projections/templates were un-inverted (see the `ProductPricing.GetSalePrice` usages across catalog/search/detail/cart/wishlist/recommendations/brand/promotion). The remaining **admin product-list slice** (the one projection still on the raw mapping, `GetAdminProductsQuery` + its template) was fixed as **B-002** on 2026-06-29 (`/acceptance` PASS, Codex council clean). The only residual is the **dead-code** orphan components tracked as FOUND-B002-orphans above.
- **Description:** ~14 DTO projections map `SalePrice = CompareAtPrice` (the **higher** original price) while the UI renders `salePrice` as the deal price — every on-sale product shows the wrong price as the purchase price across catalog/search/detail/cart/wishlist/recommendations and schema.org markup; `GetBrandBySlugQuery` alone uses the opposite mapping. Pick one contract, centralize in a single shared mapper, fix all projections and/or templates.
- **Closes:** `_review/bugs.md` #6 (P1 confirmed, 14 inverted projections).
- **Affected:** `GetProductsQueryHandler.cs`, `GetFeaturedProductsQuery.cs`, `SearchProductsQuery.cs`, `GetRelatedProductsQuery.cs`, `GetRecommendationsQueryHandler.cs`, `GetProductBySlugQuery.cs`, cart/wishlist mappers, `GetBrandBySlugQuery.cs`, `product-card.component.ts`, `structured-data.service.ts`.
- **Acceptance:** Seeded on-sale product (DualZone Pro) shows 899.99 active / 1099.99 struck-through everywhere; charged amount equals displayed active price; unit test on the shared mapper.
- **Depends on:** None; coordinate with PERF-03 (same projections get rewritten).

### BUG-07 — Forgot-password: send the email, stop logging the token (P1, Small)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** `ForgotPasswordCommandHandler.cs:34-42` logs the raw reset token at Information level (account takeover via log access) and the email send is commented out — users are silently dead-ended while the UI claims success. Inject `IEmailService`, call `SendPasswordResetEmailAsync` with a `/reset-password` link, remove the token from the log line, and fix the forgot-password page showing success on error. Note `EmailService` defaults to placeholder mode (`Email:UsePlaceholder=true`) — flip for real environments (GAP-03/SEC-07).
- **Closes:** `_review/bugs.md` #7; `_review/security.md` #2 (SR-05); `_review/product.md` #4; `_review/architecture.md` #1 (reset slice); `_review/status.md` #2.
- **Affected:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`, `src/ClimaSite.Infrastructure/Services/EmailService.cs`, `forgot-password.component.ts`.
- **Acceptance:** Handler unit test asserts `IEmailService` invoked and token never logged; manual flow via MailHog delivers a working reset link; E2E covers the full reset.
- **Depends on:** None (SMTP/MailHog config exists in shared-infra).

### BUG-08 — Share a single in-flight token refresh; stop logging users out on concurrent 401s (P1, Small)
- **Description:** When refresh is in flight, `AuthService.refreshToken()` throws a synthetic error that the interceptor treats as fatal (`clearAuthState`) — page loads with several parallel calls at token expiry randomly log the user out (every ~15 min). Cache one shared refresh observable (`shareReplay`); only clear auth when the shared refresh itself fails. (`WishlistService.fetchInFlight$` shows the correct pattern in-repo.)
- **Closes:** `_review/bugs.md` #8.
- **Affected:** `src/ClimaSite.Web/src/app/auth/services/auth.service.ts:202-208`, `src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts:49-55`.
- **Acceptance:** Unit test (TS-09): two simultaneous 401s → exactly one POST `/auth/refresh`, both requests retried, no clearAuth on the second caller.
- **Depends on:** Write the interceptor spec first (TS-09) — the auth chain is a "do not touch without tests" area per ARCHITECTURE_REVIEW.md.

### BUG-09 — Replace wishlist in-process semaphore with DB-constraint-based idempotency (P2, Small)
- **Description:** The new `WishlistApplicationService` serializes mutations with a static, never-evicted per-user `SemaphoreSlim` dictionary — single-instance only; under multi-instance Railway, races fall through to the existing unique index and surface as unhandled 500s. Catch the unique-violation `DbUpdateException` (23505) and return the existing DTO (idempotent), delete the lock dictionary and the test-fake-driven `strategy is null` branch, add `AsNoTracking` to read paths.
- **Closes:** `_review/bugs.md` #9; `_review/architecture.md` #8; `_review/performance.md` #15; ARCHITECTURE_REVIEW improvement 6.
- **Affected:** `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `AddToWishlistCommand.cs`, `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs`.
- **Acceptance:** Test simulating unique-violation on SaveChanges returns success with the existing item; no static lock dictionary remains.
- **Depends on:** Cheapest as a rider on OPS-01 (branch still uncommitted); real-provider test fixture (ARCH-07) ideal but not blocking.

### BUG-10 — Translate product names in cart and wishlist DTOs (P2, Medium)
- **Description:** Catalog queries translate via `GetTranslatedContent(lang)`, but cart and wishlist mappers use raw `product.Name` and accept no language — BG/DE users see English names for translated products, violating the project's own DoD. Thread `?lang=` through cart/wishlist endpoints and mappers.
- **Closes:** `_review/bugs.md` #10.
- **Affected:** `AddToCartCommand.cs`, `MergeGuestCartCommand.cs`, `GetCartQuery.cs`, `WishlistApplicationService.cs`, `cart.service.ts`, `wishlist.service.ts`.
- **Acceptance:** With `lang=bg`, GET `/api/cart` and `/api/wishlist` return Bulgarian names for translated products (integration test).
- **Depends on:** Wishlist part cheapest before OPS-01 merge.

### BUG-11 — One display currency: `DEFAULT_CURRENCY_CODE` + no bare `| currency` pipes (P2, Small)
- **Status:** ✅ DONE (2026-06-26). Set `DEFAULT_CURRENCY_CODE='EUR'` in `app.config.ts`; converted all 18 bare `| currency` → `| currency:'EUR'` (checkout/cart/mini-cart×2); replaced the wrong/`$` shipping-option labels with a `shippingCost` map (standard €5.99 / express €15.99 / overnight €19.99) mirroring `CheckoutPricing.cs`, rendered via `:'EUR'` — so displayed shipping == charged. Added a checkout spec that fails if the map drifts from the server. Grep acceptance: 0 bare pipes, 0 `$` literals; 1246 frontend tests green. **Surfaced:** standard showed "free" but the server charges €5.99 — UI now matches the server; **see new question DEC-SHIPPING** (should standard be free?). Dual EUR/BGN transitional display = follow-up **UX-16** below.
- **Description:** Product pages show EUR, cart/checkout show USD (bare pipe defaults), Stripe charges BGN, and checkout hardcodes `$9.99`/`$19.99` shipping labels that don't match the backend's tiers. Provide `{ provide: DEFAULT_CURRENCY_CODE, useValue: <store currency> }`, fix all bare pipes (incl. `mini-cart-item.component.ts`, `mini-cart-drawer.component.html`), source shipping labels from the model via the pipe, and align tier names with backend cases.
- **Closes:** `_review/bugs.md` #11; `_review/uiux.md` #1 (P1 confirmed); UI_UX_REVIEW work item #1; display slice of `_review/product.md` #3.
- **Affected:** `app.config.ts`, `checkout.component.ts`, `cart.component.ts`, mini-cart components.
- **Acceptance:** grep shows no bare `| currency` and no `$9.99|$19.99` literals; product/cart/checkout/confirmation all render one currency.
- **Depends on:** DEC-CURRENCY; ride with BUG-02.

### BUG-12 — Real `AverageRating`/`ReviewCount` in product DTOs (stars are always empty) (P2, Medium)
- **Description:** Every list/search/featured/wishlist projection hardcodes rating 0 — all star ratings render empty sitewide despite real reviews. Project approved-review aggregates (subquery or denormalized columns updated on review approval).
- **Closes:** `_review/bugs.md` #12; `_review/architecture.md` #9 (wishlist DTO fields); part of `_review/performance.md` #11.
- **Affected:** `GetProductsQueryHandler.cs`, `GetProductBySlugQuery.cs`, `SearchProductsQuery.cs`, `GetFeaturedProductsQuery.cs`, `WishlistApplicationService.cs`.
- **Acceptance:** A product with approved 5-star reviews shows non-zero stars on cards, search, featured, wishlist; consider denormalizing to avoid N+1.
- **Depends on:** Coordinate with PERF-03 (same projections).

### BUG-13 — Country-based VAT (DE = 19%) on goods + shipping (P2, Medium)
- **Description:** Flat 20% VAT on subtotal everywhere; Germany's legal rate is 19% and EU rules generally require VAT on shipping. Country-based VAT table applied to (subtotal + shipping) at order time, driven by shipping address; cart keeps a labeled estimate.
- **Closes:** `_review/bugs.md` #13.
- **Affected:** `CreateOrderCommand.cs:200-202`, `AddToCartCommand.cs:185-186`, `MergeGuestCartCommand.cs`.
- **Acceptance:** DE address → 19% on goods+shipping; BG → 20%; unit tests per country.
- **Depends on:** DEC-CURRENCY/store-config decisions (shares the hardcoded shipping/currency TODO cluster, API-014).

### BUG-17 — Defuse hard-delete traps (cart `First()` crash, `SetNull` on non-nullable FKs) (P3, Small)
- **Description:** If a product row is ever hard-deleted: cart mapping throws (`products.First(...)`), and `order_items` FKs configured `DeleteBehavior.SetNull` on non-nullable Guid columns violate NOT NULL. Make `OrderItem.ProductId`/`VariantId` nullable (rows already snapshot name/sku/price); use `FirstOrDefault` + "unavailable" item in cart mapping.
- **Closes:** `_review/bugs.md` #17.
- **Affected:** `OrderConfiguration.cs:214-222`, `OrderItem.cs`, `AddToCartCommand.cs:160`, `MergeGuestCartCommand.cs:143`, migration.
- **Acceptance:** Hard-deleting a test product leaves order history readable and cart endpoints returning 200 with the item flagged unavailable.
- **Depends on:** Migration via the db-migrate skill (see ARCH-03 — don't entangle with the folder consolidation).

### BUG-18 — Stop returning 200 for unmatched Stripe webhook events (P2, Small)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** `WebhooksController.cs:91-93` ACKs events with "no matching order", so Stripe never retries — an early-arriving webhook (before the order row commits) is permanently lost. Return non-2xx for "order not yet created" or persist unmatched events for replay.
- **Closes:** `_review/bugs.md` #18 / additional observation; rider on SR-03 fix.
- **Affected:** `src/ClimaSite.Api/Controllers/WebhooksController.cs`, `HandleStripeWebhookCommand.cs`.
- **Acceptance:** Integration test: webhook for an unknown intent returns retryable status (or is stored and reconciled); duplicate events are idempotent.
- **Depends on:** BUG-01.

### BUG-26 — GDPR account deletion fails in production (transaction vs retry strategy) (P0, Small)
- **Status:** ✅ FIXED (2026-06-22, Wave 6a). Surfaced by the new `GdprControllerTests` integration tests.
- **Description:** `DeleteUserDataCommandHandler` opened a manual `BeginTransactionAsync`, which throws `InvalidOperationException` under the `NpgsqlRetryingExecutionStrategy` (`EnableRetryOnFailure(3)`, `Infrastructure/DependencyInjection.cs`). The handler swallowed it → generic 400, so a logged-in user with the correct password **could not delete their account (GDPR Article 17 right-to-erasure broken)**. Fix: run the deletion inside `_context.Database.CreateExecutionStrategy().ExecuteAsync(...)` so the transaction is retry-compatible.
- **Affected:** `src/ClimaSite.Application/Features/Gdpr/Commands/DeleteUserDataCommand.cs`; test `tests/ClimaSite.Api.Tests/Controllers/GdprControllerTests.cs` flipped from pinning the broken 400 to asserting success/anonymize/blocked-login.
- **Acceptance:** Integration test: valid password + confirmation → 200, account anonymized (deactivated, email scrubbed, password cleared), old credentials rejected. ✅
- **Depends on:** none. Security-review recommended (GDPR + auth surface).

---

## 3. Product gaps (GAP)

Detail: `_review/product.md` (flow inventory + "highest-leverage path"), `_review/status.md`, `UI_UX_REVIEW.md`.

### GAP-01 — Admin orders page: list, status update, tracking number (P0, Large)
- **Description:** `/admin/orders` is a 12-line "Coming Soon" stub while `AdminOrdersController` (status, shipping/tracking, notes) is fully built — **orders can be placed but never fulfilled through the product**. Build the orders list + detail with status transitions and tracking-number entry against the existing API; this is the minimum-operability slice of the admin build-out and the precondition for shipped-email notifications.
- **Closes:** Fulfillment slice of `_review/product.md` #2 (P0 confirmed) and `_review/status.md` #1; UI_UX_REVIEW P0 item #2; re-confirms `docs/validation/areas/08-admin-panel.md`. **No Plan 18 task tracks admin UI — add one.**
- **Affected:** `src/ClimaSite.Web/src/app/features/admin/orders/admin-orders.component.ts` (replace stub), new admin order service, `src/ClimaSite.Api/Controllers/AdminOrdersController.cs` (exists).
- **Acceptance:** Admin marks an order Shipped with a tracking number and the customer sees it in `/account/orders/:id`; real E2E replaces the "via API since UI may be placeholder" workarounds in `AdminPanelTests.cs`; works in both themes / all three languages.
- **Depends on:** None (backend exists). Pairs with GAP-03 (shipped email) and BUG-01 (status changes only meaningful once orders leave Pending).

### GAP-02 — Admin products CRUD + customers page + dashboard KPIs (P1, Large)
- **Status:** ✅ DONE (2026-06-17). Admin products list/create/edit/deactivate + translations & related-products editors wired (BUG-14 `searchProducts()` implemented); admin customers list/detail/status; dashboard KPIs/charts/recent-orders/low-stock/top-products. Backend gap closed too: `AdminProductsController` now uses `GetAdminProductsQuery`/`GetAdminProductByIdQuery` (admin DTOs) instead of the public catalog projection. EN/BG/DE i18n, unit + component + service specs, and admin products/customers E2E added.
- **Description:** Replace the `/admin/products` and `/admin/users` stubs with CRUD UIs against `AdminProductsController`/`AdminCustomersController`; wire the orphaned `product-translation-editor` and `related-products-manager` components (implementing the no-op `searchProducts()` against the existing products search — closes BUG-14); surface `AdminDashboardController` KPIs on the dashboard instead of bare nav tiles.
- **Closes:** Remainder of `_review/product.md` #2 / `_review/status.md` #1; `_review/status.md` #8 (orphaned components); `_review/uiux.md` #3; `_review/bugs.md` #14 (BUG-14); UI_UX_REVIEW item #7.
- **Affected:** `features/admin/products/`, `features/admin/users/`, `features/admin/admin-dashboard/`, `features/admin/products/components/*` (orphans), backend already exists.
- **Acceptance:** Create/edit/deactivate a product end-to-end incl. translations and related products; customer list/detail works; dashboard shows real KPIs; E2E coverage; CLAUDE.md admin row corrected (DOC-02).
- **Depends on:** GAP-01 first (fulfillment beats catalog editing); inventory surfacing decision in GAP-11.

### GAP-03 — Wire transactional emails: order confirmation, shipped, welcome, admin notify-customer (P1, Medium)
- **Status:** ✅ DONE (2026-06-17, merged in #21 on the ARCH-05 outbox from #20). Order confirmation enqueued transactionally in `CreateOrderCommand`; order-shipped on the admin ship transition; welcome on registration (ARCH-05); password reset moved to the outbox; admin `NotifyCustomer` honored in `UpdateOrderStatusCommand` (BUG-16). Delivery is async via the DB outbox + `BackgroundService`, so email failure never fails the request. (In-app notification row for status changes deferred to GAP-09.)
- **Description:** The entire email layer is implemented but orphaned — the only `IEmailService` caller is GDPR delete; no order confirmation, no shipped notice, no welcome email; the admin "notify customer" checkbox is a silent no-op (BUG-16). Wire `SendOrderConfirmationEmailAsync` into order creation (or the Paid webhook), `SendOrderShippedEmailAsync` into the shipping-update flow, welcome into registration, and honor `NotifyCustomer`; flip the `Email:UsePlaceholder=true` default for real environments; make sends fire-and-forget/outboxed so email failure never fails the order.
- **Closes:** `_review/product.md` #5; `_review/architecture.md` #1 (order/welcome slices); `_review/bugs.md` #16 (BUG-16); `_review/status.md` #3 (email slice). Plan 18 NOT-10x territory.
- **Affected:** `CreateOrderCommand.cs`, `Auth/Handlers/RegisterCommandHandler.cs`, `Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs:73`, `EmailService.cs`.
- **Acceptance:** Placing an order produces a confirmation email (MailHog assert in E2E); admin ship action sends tracking email + creates a notification row; failures logged without breaking the flow; handler unit tests assert the calls.
- **Depends on:** BUG-07 (same wiring pattern, do first); O-1/ARCH-05 for outbox reliability (synchronous fire-and-forget acceptable as v1); SEC-07 for SMTP config names.

### GAP-04 — Legal pages (terms/privacy/cookies/returns/shipping/FAQ/Impressum) + cookie consent (P1, Medium)
- **Status:** ✅ DONE (2026-06-17, #25). Lazy `features/legal/` with a shared `LegalPageComponent` (terms/privacy/cookies/returns/shipping/Impressum) + accessible FAQ accordion; root routes wired; footer 404s closed and Impressum link added; social `href="#"` replaced. `ConsentService` + `CookieConsentComponent` banner (non-essential storage withheld until accepted). EN/BG/DE placeholder legal prose (flagged) + unit specs + E2E. Built functional/direct per owner (no Stitch for utility pages).
- **Description:** All six footer legal/support links 404 (routes don't exist) while the footer shows a "GDPR Compliant" badge; no cookie-consent component exists; social links are `href="#"`. EU (BG/DE) distance-selling legal floor: add a lazy `legal` feature with translated static pages incl. German Impressum, a consent banner (no analytics scripts found, so banner is less acute than the mandatory pages), and fix/remove socials.
- **Closes:** `_review/product.md` #6 (P1 confirmed); UI_UX_REVIEW item #4.
- **Affected:** `core/layout/footer/footer.component.ts:43-56,93-103`, `app.routes.ts`, new `features/legal/`.
- **Acceptance:** All footer links render translated content in EN/BG/DE in both themes; consent banner blocks non-essential storage until accepted; placeholder copy acceptable pre-launch but flagged.
- **Depends on:** Real legal text from the owner (placeholder first); GAP-10 (privacy page should link the GDPR endpoints).

### GAP-05 — Real contact endpoint (form currently fakes success via setTimeout) (P1, Small)
- **Status:** ✅ DONE (2026-06-17, #23). `ContactMessage` entity + `contact_messages` table; `CreateContactMessageCommand` persists the enquiry AND queues a business-notification email via the ARCH-05 outbox (atomic); public `POST /api/contact`; the form now issues a real request with success/error states. Handler unit + Api integration + frontend specs.
- **Description:** `contact.component.ts:384-399` simulates success with `setTimeout` — every submission (sales leads, complaints, GDPR inquiries) is silently discarded. Add `POST /api/contact` that emails the business via `IEmailService` and/or persists a ContactMessage; wire the form with real success/error states.
- **Closes:** `_review/product.md` #7 (P1 confirmed); UI_UX_REVIEW item #5.
- **Affected:** `features/contact/contact.component.ts`, new controller/command in `ClimaSite.Api`/`ClimaSite.Application`.
- **Acceptance:** Submission produces a stored/emailed message verifiable in MailHog; network failure shows the error state; integration + E2E test.
- **Depends on:** Email wiring base (BUG-07/GAP-03).

### GAP-06 — Remove or finish fake payment methods (PayPal, bank transfer) (P1, Small)
- **Status:** ✅ DONE (2026-06-17, #28). PayPal removed from checkout/review/confirmation. Bank transfer made real: `Order.SetPaymentMethod` persists the method for offline orders (stays Pending), a bank-instructions email (amount, reference = order number, IBAN/account/bank from `BankTransferOptions`) is staged via the outbox in the order's transaction, and `GET /api/payments/config` now returns bank details for the UI (instructions shown on the bank option + confirmation, `data-testid bank-transfer-instructions`). Validator restricts PaymentMethod to card|bank. Unit + Api integration + frontend specs + E2E.
- **Description:** Non-card options create orders nobody can pay: no PayPal integration, no bank-transfer instructions, payment method not even persisted, stock decremented unconditionally. Remove PayPal until integrated; for bank transfer persist the method, show IBAN/reference instructions on confirmation + email, keep status Pending-payment.
- **Closes:** `_review/product.md` #8 (P1 confirmed); UI_UX_REVIEW item #6.
- **Affected:** `checkout.component.ts:199-214,1210-1232`, `CreateOrderCommand.cs` (PaymentMethod field — shared with BUG-01).
- **Acceptance:** Only payable methods offered; bank-transfer orders display instructions and are distinguishable in data; E2E covers the bank path.
- **Depends on:** BUG-01 (PaymentMethod persistence lands there).

### GAP-07 — Guest checkout: decide, then implement or descope (P1, decision + Medium/Large)
- **Status:** ✅ DONE (2026-06-17, #24; DEC-GUEST = enable). `Order.GuestAccessToken` (256-bit opaque, guest orders only, returned only on creation) + anonymous token-gated `GET /api/orders/{id}/guest?token=` (constant-time compare, generic 404 — SEC-02 owner-only by-id/by-number left intact). `create-intent` is `[AllowAnonymous]` (amount is server-computed). `/checkout` guard dropped; confirmation is token-gated; TS-13 tautological E2E rewritten. Unit + integration + frontend specs.
- **Description:** `/checkout` is auth-guarded while the backend explicitly supports anonymous guest orders and CLAUDE.md documents guest checkout; `PaymentsController` is `[Authorize]` so guests couldn't pay anyway; the existing guest E2E is tautological (passes either way). Either enable end-to-end (drop guard, anonymous create-intent tied to server-computed totals, guest confirmation via order number + email — interacts with SEC-02) or remove the backend guest path and fix docs.
- **Closes:** `_review/product.md` #9; `_review/bugs.md` #15 (BUG-15); `_review/uiux.md` #16; TS-13.
- **Affected:** `app.routes.ts:82-89`, `OrdersController.cs`, `PaymentsController.cs`, `CheckoutTests.cs`, CLAUDE.md business rules.
- **Acceptance:** Docs and behavior agree; if enabled, a guest completes purchase E2E and views confirmation; the tautological E2E is fixed (TS-13).
- **Depends on:** **DEC-GUEST (blocking)**; BUG-01/BUG-02 must land first if enabling.

### GAP-08 — Installation requests: notify the business and make them viewable (P1, Medium)
- **Status:** ✅ DONE (2026-06-17, #27). `CreateInstallationRequestCommand` queues a business-notification email via the outbox (to `ContactOptions.RecipientEmail`), transactionally with the persist. Admin: `GetAdminInstallationRequestsQuery` (paged + status filter), `UpdateInstallationRequestStatusCommand` (Confirm/Schedule/InProgress/Complete/Cancel), `AdminInstallationController` (GET list, PUT {id}/status), + admin list UI with status management, dashboard tile, EN/BG/DE. No migration (Status already on the entity). Unit + Api integration + frontend specs + E2E.
- **Description:** The PDP solicits installation bookings; requests are stored but there is no list endpoint, no admin view, no email — i18n copy promises "We will contact you" and nothing can fulfill it. Minimum: business email per request via `IEmailService`. Proper: `GET /api/admin/installation-requests` + status field + admin list view.
- **Closes:** `_review/product.md` #10 (P1 confirmed).
- **Affected:** `InstallationController.cs`, `CreateInstallationRequestCommand.cs`, admin UI (rides on GAP-02 shell).
- **Acceptance:** Submitting a request triggers a business notification and appears in an admin-visible list with a status lifecycle.
- **Depends on:** Email wiring (GAP-03); admin shell (GAP-01/02) for the list view — email-only can ship first.

### GAP-09 — Notifications system: complete Plan 18 NOT-100..111 or formally descope (P2, Large)
- **Status:** ✅ DONE (2026-06-17, #29 — completed the loop). Producers: `UpdateOrderStatusCommand` + `UpdateShippingInfoCommand` create a `Notification` (transactionally, authenticated orders only — guests skipped) on status changes, mapped to type + linked to the order. Frontend `NotificationService` (unreadCount + recent signals; summary/list/mark-read/mark-all/delete) + header bell with unread badge + dropdown (authenticated-only, accessible). EN/BG/DE i18n; service + header specs; cross-user E2E. No migration (table existed). Preferences (NOT-106) not built — basic loop only.
- **Description:** In-app notifications have a full backend API with **zero producers and zero UI** (no bell, no `NotificationService`, nothing creates notifications); master overview falsely says "Complete". Either complete the loop (emit from order-status changes, header bell + dropdown + preferences per Plan 18 NOT-106) or remove the controller and mark Plan 12 in-app scope deferred.
- **Closes:** `_review/product.md` #12 (P2 adjusted); `_review/status.md` #3 (in-app slice); Plan 18 Phase 2 NOT-* (or its descope).
- **Affected:** `NotificationsController.cs`, `Features/Notifications/`, `header.component.ts`, new frontend feature.
- **Acceptance:** Order status change creates a notification visible in a header dropdown (with E2E), or endpoints removed and Plan 12/18 + CLAUDE.md updated.
- **Depends on:** **O-1** (background jobs) per Plan 18 NOT-100 batched decisions; GAP-01 (status changes must exist to notify about).

### GAP-10 — GDPR self-service UI in the account area (P2, Medium)
- **Description:** Backend export/delete/rights endpoints are real but have no frontend consumer — GDPR rights are not user-exercisable, and the old validation summary mis-reports the backend as missing. Add a privacy section under `/account` (export my data, delete my account with confirmation UX), i18n'd.
- **Closes:** SR-14; `_review/status.md` #6; GDPR slice of `_review/product.md` #14; corrects `docs/validation/00-validation-summary.md` SEC-003 (with DOC-04).
- **Affected:** `features/account/account.routes.ts`, new privacy component, `GdprController.cs` (exists).
- **Acceptance:** Authenticated user exports data and requests deletion from `/account`; E2E covers deletion; GDPR integration tests (TS-05) land alongside.
- **Depends on:** TS-05 strongly recommended first (deletion is destructive and currently untested at every layer).

### GAP-11 — Triage implemented-but-unreachable features: compare, recently-viewed, price history, inventory admin (P2, Large)
- **Description:** Built-but-invisible value: `ComparisonService` + CompareButton (no route/usage), `RecentlyViewedComponent` (imported nowhere), `PriceHistoryController` (zero frontend callers), `InventoryController` admin endpoints (no UI anywhere, yet CLAUDE.md claims Inventory "Complete"). Per feature: wire it (compare button + `/compare` page is genuinely high-value for HVAC spec shopping; price-history sparkline on PDP; recently-viewed on PDP/home; inventory views inside GAP-02) or formally descope and document "backend-only by design".
- **Closes:** `_review/product.md` #14; `_review/status.md` #7; UI_UX_REVIEW item #18.
- **Affected:** `comparison.service.ts`, `compare-button.component.ts`, `recently-viewed.component.ts`, `PriceHistoryController.cs`, `InventoryController.cs`.
- **Acceptance:** Each feature reachable from the UI with an E2E test, or deleted/documented as headless; CLAUDE.md status reflects each decision.
- **Depends on:** Owner triage; GAP-02 for the admin-side pieces.

### GAP-12 — Email verification: complete or remove; decide the policy (P2, Medium)
- **Description:** Three-quarters-built stub: confirm-email endpoint + client method exist, but registration never sends a token, no `/confirm-email` route exists, and login ignores `EmailConfirmed` (`RequireConfirmedEmail=false`). Decide policy (SR-20): enforce verification (send on register, landing route, optionally gate sensitive actions) or delete the dead pieces and document it as out of scope.
- **Closes:** `_review/product.md` #13; SR-20 / `_review/security.md` #11.
- **Affected:** `AuthController.cs`, `Auth/Handlers/RegisterCommandHandler.cs`, `auth.service.ts:266-268`, `app.routes.ts`.
- **Acceptance:** Registration sends a working confirmation link that flips `EmailConfirmed`, or the endpoint/client method are removed; policy recorded.
- **Depends on:** Email wiring (GAP-03).

### GAP-13 — Coupons/discount codes + admin promotions management (P3, Large)
- **Description:** "Promotions" are seeded, read-only content pages; no coupon entity, no code field in cart/checkout, `Order.DiscountAmount` is never set, and promotions can't be updated without a deploy. Phase 1: admin CRUD for promotion content; Phase 2: coupon entity + validation endpoint + cart code field.
- **Closes:** `_review/product.md` #18.
- **Affected:** `PromotionsController.cs`, new admin controller/UI, cart/checkout components.
- **Acceptance:** Admin creates a promotion without a deploy; a coupon code changes the order total end-to-end.
- **Depends on:** GAP-02 (admin shell); pricing fixes (BUG-02/06) first.

---

## 4. Deployment / Ops (OPS)

Detail: `_review/devops.md`, `DEV_WORKFLOW.md`. The devops launch-blocker list (devops Dimension data §2) maps to SEC-01, SEC-03, OPS-03/04/05, SEC-07, OPS-07, OPS-02.

### OPS-01 — Commit, push, and PR the wishlist slice (P1, Small) — **do first**
- **Description:** The entire "DONE 2026-06-07" wishlist completion slice (23 modified + ~10 untracked files, incl. all its tests and doc updates) exists **only as uncommitted working-tree changes** on `feature/plan18-wishlist-completion` (branch HEAD == main, nothing recoverable from git). One `git checkout .` from total loss. Commit (conventional commits), push, open the PR, let CI validate. Recommended riders while the branch is open: SEC-10 (drop UserId from shared DTO), BUG-09 (semaphore → constraint), BUG-10 (wishlist lang), UX-02 (error toasts), CHANGELOG bullets, ADR for the share-token design (O-5).
- **Closes:** `_review/status.md` #4 (P1 confirmed); TS-01 (CI run = the missing test evidence); `_review/docs.md` #14 (CHANGELOG).
- **Affected:** whole working tree; `CHANGELOG.md`.
- **Acceptance:** Branch contains the commits; CI green on the PR; merged to main; Plan 18 Phase 2 wishlist DoD ("merged via PR") actually satisfied.
- **Depends on:** Nothing. Riders are optional but this is their cheapest moment.

### OPS-02 — Protect `main`; fix CLAUDE.md's direct-push mandate (P1, Small)
- **Status:** ✅ DONE (2026-06-15, branch `chore/ops-02-protect-main`). `main` protected via GitHub API: 6 required status checks, `enforce_admins=true`, PR required (0 approvals), no force-push/deletion, conversation-resolution required. CLAUDE.md post-implementation workflow rewritten to the PR flow with correct per-project test commands.
- **Description:** `main` has no protection rules (verified via GitHub API) while CLAUDE.md's "NON-NEGOTIABLE" workflow step 4 mandates `git push origin main` — red commits have already landed on main (run 27071713464). Require the five Test Suite checks + PRs, forbid force-push; rewrite CLAUDE.md step 4 to "push feature branch → PR → merge on green" (matches `DEV_WORKFLOW.md` §2-3).
- **Closes:** SR-15; `_review/devops.md` #6 (P1 confirmed).
- **Affected:** GitHub repo settings, `CLAUDE.md`, `AGENTS.md`.
- **Acceptance:** Direct push to main rejected; CLAUDE.md and AGENTS.md describe the same PR flow.
- **Depends on:** Repo admin (owner has it). Land before any CD wiring (OPS-03).

### OPS-03 — Deploy workflow + one canonical Dockerfile/Railway config per service (P1, Medium)
- **Status:** 🚧 CLEANUP DONE (2026-06-27). Deleted the four dead/stale duplicates — `src/ClimaSite.Api/Dockerfile` (byte-identical to root), `src/ClimaSite.Web/Dockerfile` (wrong build context), `src/ClimaSite.Api/railway.toml` (third config), `src/ClimaSite.Web/nginx.conf` (stale, never copied) — so the **root** `Dockerfile.api`/`Dockerfile.web` + `railway.toml`/`railway.api.toml` are the single canonical set (no edit-traps). Verified nothing in code/CI/compose referenced the deleted paths; CI builds via dotnet/ng (not these Dockerfiles). **Remaining (owner-gated):** the `.github/workflows/deploy.yml` CD workflow + service→config mapping doc need the owner's Railway project + a `RAILWAY_TOKEN` secret — deferred to the OPS-08 owner setup.
- **Description:** No CD workflow exists; three disagreeing `railway.toml` files, drifting duplicate Dockerfiles, and a stale `nginx.conf` copy make deploys guesswork. Pick root `Dockerfile.api`/`Dockerfile.web` as canonical, delete `src/ClimaSite.Api/railway.toml`+Dockerfile, `src/ClimaSite.Web/Dockerfile`, stale `nginx.conf`; add `.github/workflows/deploy.yml` (build images → migrate → Railway deploy on main/tag, gated on the test suite); document the service-to-config mapping.
- **Closes:** `_review/devops.md` #2 (P1 confirmed); Plan 18 PROD-104.
- **Affected:** root deploy artifacts, `.github/workflows/`, `docs/operations/` (DOC-03).
- **Acceptance:** Exactly one Dockerfile + one Railway config per service; deploy workflow runs only after tests pass; mapping documented.
- **Depends on:** **O-7 / OPS-08** (which Railway services exist); OPS-02 first; OPS-04 supplies the migration step.

### OPS-04 — Stop `MigrateAsync` at startup in prod; dedicated pre-deploy migration step (P1, Medium)
- **Description:** Migrations run on every boot (crash-loop ×3 against prod data on a bad migration; replica race risk; no rollback target — zero git tags). Gate `MigrateAsync` to Development/Testing; add `scripts/migrate.sh` (or EF migration bundle) as the CD pre-deploy step; adopt expand-contract per the repo's own `deploy-checklist` skill.
- **Closes:** `_review/devops.md` #3 (P1 confirmed); Plan 18 PROD-102. Pairs with TS-17 (CI migration apply/rollback job).
- **Affected:** `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:31`, `Program.cs`, new `scripts/migrate.sh`, deploy workflow.
- **Acceptance:** Production startup performs zero schema changes; CD logs an explicit migration step; CI job applies all migrations to a clean Postgres and rolls back the latest.
- **Depends on:** Same code path as SEC-01 — do together; OPS-03 hosts the step.

### OPS-05 — Observability floor: correlation IDs, structured logs, error tracker, alerts (P1, Medium)
- **Status:** 🚧 FLOOR DONE (2026-06-27). `CorrelationIdMiddleware` (read/generate `X-Correlation-Id` → Serilog `LogContext` → echo on response) wired early in the pipeline; `ExceptionHandlingMiddleware` now returns a `traceId`; `Log.CloseAndFlush` registered on `ApplicationStopped`. Integration-tested (`CorrelationIdTests`, 3/3 — generated id, echoed id, traceId-in-error). **Remaining (deploy-time / needs O-4):** the error tracker (Sentry — O-4 vendor choice), OpenTelemetry (PROD-103), JSON-console-in-Production, healthcheck-based uptime alerting.
- **Description:** Console-only Serilog; the `X-Correlation-Id` convention documented in CLAUDE.md is implemented nowhere; no Sentry/OTel/metrics/alerts/runbooks; no `Log.CloseAndFlush`. Add correlation middleware (read/generate header → Serilog LogContext → echo on response), JSON console output in Production, an error tracker (O-4 — Sentry suggested), OTel per Plan 18 PROD-103, healthcheck-based uptime alerting, `CloseAndFlush` in try/finally.
- **Closes:** `_review/devops.md` #5 (P1 confirmed); Plan 18 PROD-103; CLAUDE.md correlation-ID doc-drift.
- **Affected:** `Program.cs`, `src/ClimaSite.Api/Middleware/`, `appsettings.json`, new packages.
- **Acceptance:** Every API response carries `X-Correlation-Id`; one request's logs share the ID; a thrown test exception appears in the tracker; ExceptionHandlingMiddleware includes a traceId.
- **Depends on:** **O-4** (vendor choice — owner).

### OPS-08 — Confirm Railway topology, live-deploy status, and backups (P1, Small, **Needs confirmation**)
- **Status:** 🚧 READINESS ARTIFACTS PREPARED (2026-06-27, branch `ops/ops-08-deploy-readiness`). Canonical deploy runbook written: [`docs/runbooks/deploy.md`](../runbooks/deploy.md) — topology diagram, full env-var matrix (code-actual names, incl. discovered `ADMIN_EMAIL`/`ADMIN_INITIAL_PASSWORD`, `Minio__*`, `AllowedOrigins__*`, `API_URL`), deploy/migration/smoke procedure, rollback (expand-contract), and the deploy-artifact config-review (F1–F9). **Canonical artifacts confirmed:** root `railway.toml`→`Dockerfile.web` (web) + `railway.api.toml`→`Dockerfile.api` (api); the `src/ClimaSite.Api/Dockerfile`, `src/ClimaSite.Web/Dockerfile`, `src/ClimaSite.Api/railway.toml`, and `src/ClimaSite.Web/nginx.conf` are dead/stale duplicates (delete under OPS-03 — not deleted here, review-only). The remaining OPS-08 items are **owner-gated**: see the [Owner-action checklist](../runbooks/deploy.md#5-owner-action-checklist--the-5-ops-08-confirmation-questions) which frames the 5 confirmation questions to answer in one pass.
- **Description:** Owner questions that gate several tasks: Is auto-deploy connected, and which services/config files? Has a production DB ever been seeded (if yes — rotate `admin@climasite.local` **now**)? What is production object storage? Are Postgres backups enabled / restore tested? Replica count (gates BUG-09 urgency, OutputCache coherence, KnownProxies for SEC-03)?
- **Closes:** O-7; `_review/devops.md` open questions 1–5; `_review/architecture.md` + `_review/performance.md` topology open items.
- **Affected:** Railway dashboard (owner); outcomes recorded in [`docs/runbooks/deploy.md`](../runbooks/deploy.md) (DOC-03 runbook) + DECISIONS.md.
- **Acceptance:** All five questions answered and recorded; any live exposure remediated same-day.
- **Depends on:** Owner access. (Readiness artifacts no longer blocking — done.)

### OPS-06 — CI hardening: lint, analyzers, docker-build, dependency scanning, concurrency, real-bundle E2E (P2, Medium)
- **Description:** CI runs tests only — no `ng lint`, no `dotnet format`/`-warnaserror`, Dockerfiles never built, no `npm audit`/`dotnet list package --vulnerable`/dependabot/CodeQL, codecov upload is `continue-on-error`, no `concurrency:` group or NuGet cache, E2E tests the dev server not the built bundle, triggers reference a nonexistent `develop` branch.
- **Closes:** `_review/devops.md` #9; TS-12; `_review/testing.md` #7; SECURITY_REVIEW dependency-scanning row.
- **Affected:** `.github/workflows/test.yml`, new `dependabot.yml`, `angular.json`.
- **Acceptance:** A PR with a lint error, vulnerable package, or broken Dockerfile fails CI; E2E exercises the compiled e2e bundle; triggers match the real branching model (+`workflow_dispatch`); new checks added to OPS-02's required set.
- **Depends on:** OPS-02 (to make the new checks required).

### OPS-07 — Non-root containers; web entrypoint fails hard on unset `API_URL` (P2, Small)
- **Description:** No `USER` directive in any Dockerfile (API and nginx run as root); `docker-entrypoint.sh` defaults `API_URL` to `http://localhost:8080`, so a misconfigured web service passes health checks while every `/api` call fails.
- **Closes:** SR-13; `_review/devops.md` #11; Plan 18 PROD-100.
- **Affected:** `Dockerfile.api`, `Dockerfile.web`, `src/ClimaSite.Web/docker-entrypoint.sh`, `nginx.conf.template`.
- **Acceptance:** `whoami` != root as PID 1 in both images; web image without `API_URL` exits with a clear error.
- **Depends on:** OPS-03 (canonical Dockerfiles first).

### OPS-09 — Cut the first version tag; exercise the release skill (P2, Small)
- **Description:** Zero git tags ever; CHANGELOG perpetually "[Unreleased]"; no rollback target exists. Cut a v0.x pre-release from main via the `/release` skill, roll the changelog section, and wire image tagging to git tags in OPS-03's workflow.
- **Closes:** `_review/devops.md` #12.
- **Affected:** `CHANGELOG.md`, git tags, deploy workflow.
- **Acceptance:** Annotated tag + matching CHANGELOG section exist; deploy workflow publishes an image labeled with the tag.
- **Depends on:** OPS-01 merged; OPS-03 for image tagging.

### OPS-10 — Resolve local-dev bootstrap contradiction (shared-infra vs project compose) (P2, Small)
- **Description:** `appsettings.json` defaults to project-local compose Postgres on 5433 while AGENTS.md and the owner's global convention mandate shared-infra on 5432; the compose file is referenced by no doc. Decide (O-6 — convention says shared-infra), point the Development connection at `localhost:5432/climasite`, and delete or clearly document `docker-compose.yml`.
- **Closes:** `_review/devops.md` #8; `_review/docs.md` #12; O-6.
- **Affected:** `src/ClimaSite.Api/appsettings.json`, `docker-compose.yml`, README (DOC-03).
- **Acceptance:** Fresh clone + documented steps yields a running app without editing config; compose file removed or its purpose documented.
- **Depends on:** **O-6** (owner); DOC-03 captures the result.

### OPS-11 — Enable the trunk merge queue (needs a paid GitHub plan) (P3, Small)
- **Status:** DEFERRED / plan-blocked (2026-06-24). Surfaced during the `/project-adopt` Phase B. GitHub's `merge_queue` ruleset rule **and** `evaluate`-mode rulesets both require a paid plan (merge queue → Team/Enterprise; evaluate → Enterprise); `byrzohod/climasite` is a personal **public repo on Free**, so the apply returns HTTP 422.
- **Description:** The trunk merge queue is fully prepared but not applied: `test.yml` already has the `merge_group: types: [checks_requested]` trigger (inert) and `.github/rulesets/trunk-default-branch-protection.json` is the ready-to-apply spec (reconciled to require the `Test Summary` aggregator). Current gate = the existing **classic branch protection** (OPS-02). When the repo moves to a supporting plan, apply per `docs/runbooks/merge-queue.md` (evaluate → validate a green `merge_group` run → flip to active), then remove the classic protection to avoid double-gating.
- **Affected:** `.github/rulesets/trunk-default-branch-protection.json`, repo plan, `docs/runbooks/merge-queue.md`.
- **Acceptance:** A queued PR merges via `merge_group` with `Test Summary` green; classic protection retired; runbook updated to "active".
- **Depends on:** a GitHub Team/Enterprise plan for `byrzohod/climasite` (owner). Low priority on a solo repo — the queue mainly serializes concurrent merges.

---

## 5. Testing (TS)

IDs match `docs/project-plan/TESTING_STRATEGY.md` §7; use its §1.3 corrected commands. **Folded elsewhere:** TS-01 → OPS-01 (PR/CI run), TS-02 → DOC-01 (command fixes + slnf), TS-12 → OPS-06 (CI gates), TS-13 → GAP-07 (guest E2E).

### TS-03 — Regression tests bundled with the P0 bug fixes (P0, Medium)
- **Status:** ✅ DONE (2026-06-16, merged to main; see §10 of PROJECT_STATUS.md and CHANGELOG).
- **Description:** Land with BUG-01/02/03, not after: integration tests for order-create persisting `paymentIntentId` and webhook → Paid transition; cart-merge test using the **real frontend request shape** plus a guest→login merge E2E (mirror `WishlistTests.cs:137` pattern); an automated displayed==charged==recorded total assertion.
- **Closes:** TS-03; `_review/testing.md` #9 (merge E2E); the "why green tests missed the P0s" gap (TESTING_STRATEGY §2).
- **Affected:** `tests/ClimaSite.Api.Tests/Controllers/` (new Orders/Webhooks tests), `tests/ClimaSite.E2E/Tests/Cart/CartTests.cs`.
- **Acceptance:** Each P0 fix PR includes its regression test; reverting the fix fails CI.
- **Depends on:** BUG-01/02/03 (same PRs).

### TS-04 — Stripe card-path coverage: E2E with test card + Payments/Webhooks integration tests (P1, Medium)
- **Description:** The primary revenue path has zero coverage above mocked unit tests — the only completed-order E2E deliberately avoids the Stripe iframe. Add one E2E using test card 4242… via FrameLocator (`CheckoutPage.FillPaymentDetailsAsync` exists as dead code), plus integration tests for create-intent/cancel-intent and webhook signature rejection + event dispatch.
- **Closes:** TS-04; `_review/testing.md` #2 (P1 confirmed).
- **Affected:** `tests/ClimaSite.E2E/Tests/Checkout/CheckoutTests.cs`, `PageObjects/CheckoutPage.cs`, new `tests/ClimaSite.Api.Tests/Controllers/{Payments,Webhooks}ControllerTests.cs`; CI Stripe test-mode keys.
- **Acceptance:** CI completes a card payment to order confirmation in Stripe test mode; webhook tests assert signature rejection and intent→Paid transition.
- **Depends on:** BUG-01/02 (the path must work first); Stripe test keys in CI secrets (owner).

### TS-05 — GDPR integration tests: export completeness, delete anonymization, idempotency, authz (P1, Medium)
- **Description:** Irreversible account deletion and legally-required export have **zero tests at every layer**. Integration tests asserting post-delete DB state (no PII rows; order accounting preserved), export payload completeness against seeded fixtures, 401 unauthenticated, idempotency.
- **Closes:** TS-05; `_review/testing.md` #5 (P1 confirmed).
- **Affected:** new `tests/ClimaSite.Api.Tests/Controllers/GdprControllerTests.cs`, `Features/Gdpr` handler tests.
- **Acceptance:** As described; reuses `TestWebApplicationFactory`.
- **Depends on:** None; do before GAP-10 exposes the delete button to users.

### TS-06 — Integration tests wave 1: Orders, Payments, Webhooks, Gdpr controllers (P1, Medium)
- **Description:** 20 of 26 controllers have no integration tests; wave 1 covers the money/compliance set (happy path + 401/403 per endpoint) reusing the existing Testcontainers infrastructure.
- **Closes:** TS-06; money/compliance slice of `_review/testing.md` #3.
- **Affected:** `tests/ClimaSite.Api.Tests/Controllers/`.
- **Acceptance:** Each of the four controllers has a test class covering every endpoint's happy path + auth failure; CI integration job stays <10 min.
- **Depends on:** Subsumes TS-04 (integration half) and TS-05 — coordinate to avoid duplication.

### TS-08 — Application unit tests: Cart, Inventory, Reviews + sale-price mapper (P1, Large)
- **Description:** 13 of 18 Application feature areas have zero unit tests. Priority order using the proven `MockDbContext` pattern: Cart (merge/quantity-combine/stock-cap), Inventory (oversell — pairs with BUG-05's concurrency fix), Reviews (verified-purchase gate, moderation), plus a unit test on the shared sale-price mapper from BUG-06; then Gdpr, Promotions, Notifications, remainder.
- **Closes:** TS-08; `_review/testing.md` #4 (P1 confirmed).
- **Affected:** `tests/ClimaSite.Application.Tests/Features/`.
- **Acceptance:** Each prioritized Features/* dir has handler tests; coverage measured by TS-07.
- **Depends on:** BUG-05/BUG-06 land their specific tests first; ARCH-07 for transaction-dependent handlers.

### TS-07 — Coverage measurement + thresholds (backend 80 / frontend 70) (P2, Medium)
- **Description:** The repo's headline coverage mandates are measured and enforced nowhere (no karma.conf, no runsettings, codecov `continue-on-error`). Coverlet runsettings + threshold gate for backend; new `karma.conf.js` with 70% check + `ng test --code-coverage` in CI. Baseline report-only for one sprint, then enforce. (TESTING_STRATEGY lists this P1; the verifier calibrated the finding P2 — treat as the first P2.)
- **Closes:** TS-07; `_review/testing.md` #6 (P2 adjusted).
- **Affected:** `.github/workflows/test.yml`, `tests/*/­*.csproj`, `src/ClimaSite.Web/angular.json` + new karma.conf.js.
- **Acceptance:** CI fails under 80%/70% after the baseline sprint; coverage summary visible per PR.
- **Depends on:** OPS-06 (same workflow edits).

### TS-09 — Frontend specs for the riskiest units (P2, Medium)
- **Description:** auth.interceptor (401→refresh→retry, concurrent-401 queueing — pairs with BUG-08), auth.guard, payment.service, checkout.component (step transitions, double-submit), cart.component, register.
- **Closes:** TS-09; `_review/testing.md` #8.
- **Affected:** colocated `*.spec.ts` under `src/ClimaSite.Web/src/app/`.
- **Acceptance:** Specs exist and pass for all six units; interceptor spec asserts single shared refresh.
- **Depends on:** Write interceptor spec before/with BUG-08.

### TS-10 — Integration tests wave 2: Reviews, Questions, Notifications, Inventory, Addresses (P2, Medium)
- **Closes:** TS-10; remainder of `_review/testing.md` #3.
- **Affected:** `tests/ClimaSite.Api.Tests/Controllers/`. **Acceptance:** happy + 401/403 per endpoint. **Depends on:** TS-06 pattern established.

### TS-11 — E2E reliability: observable cleanup, parallel collections, NotExecuted mystery (P2, Medium)
- **Description:** All 23 E2E classes share one serial collection (30-min job); `TestDataFactory.CleanupAsync` swallows failures silently; one same-day 74-failure run shows env sensitivity; `Checkout_SavedAddress_CanBeUsed` is NotExecuted in every recorded run with no Skip attribute. Log cleanup failures + CI bulk-cleanup post-step; split into 2-3 collections; rerun-failed in CI only; explain/annotate the NotExecuted test.
- **Closes:** TS-11; `_review/testing.md` #10.
- **Affected:** `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`, `PlaywrightFixture.cs`, CI e2e job.
- **Acceptance:** E2E wall time ~-40%; cleanup failures visible in CI logs; no NotExecuted tests without explicit Skip reason.
- **Depends on:** None.

### TS-14 — Integration wave 3: Admin*, Brands, Categories, Promotions, PriceHistory, Installation (P3, Large)
- **Closes:** TS-14. **Depends on:** GAP-01/02 (admin endpoints get UI consumers worth testing).

### TS-15 — Remaining frontend specs (admin components, product-card/list, animation services) (P3, Medium)
- **Closes:** TS-15. **Depends on:** GAP-01/02 (admin components must exist first).

### TS-16 — Test hygiene: delete `UnitTest1.cs`, unused CI postgres service, dead-or-used `FillPaymentDetailsAsync` (P3, Small)
- **Closes:** TS-16; `_review/testing.md` #12. Note: the dead `Features/Auth` test retargeting is in ARCH-01, not here.
- **Acceptance:** No always-pass placeholder tests; integration CI green without the unused service container.

### TS-17 — CI migration job: apply all migrations to clean Postgres + roll back latest (P3, Small)
- **Closes:** TS-17. **Depends on:** OPS-04 (migration script), ARCH-03 (folder consolidation first is cleaner).

---

## 6. UI/UX (UX)

Detail: `docs/project-plan/UI_UX_REVIEW.md` (work-list #1-23) and `_review/uiux.md`. Items #1 (currency) → BUG-11; #2 (admin orders) → GAP-01; #4-#8 → GAP-04..07/GAP-02; #18 → GAP-11. All UI work must pass the DoD: both themes, EN/BG/DE, keyboard/SR.

### UX-01 — Checkout shipping form: field-level validation errors (P1, Small)
- **Description:** 8 required validators but **zero rendered error messages** (`.field-error` CSS is dead code) and a disabled-while-invalid submit — users get no clue which field is wrong on the highest-value form in the app. Follow the existing touched-based pattern from `profile.component.ts`.
- **Closes:** `_review/uiux.md` #2 (P1 confirmed); UI_UX_REVIEW item #3.
- **Affected:** `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts:104-167,1051-1105`.
- **Acceptance:** Blur/submit with empty required fields shows translated errors under each field in all 3 languages; E2E covers incomplete-form attempt.
- **Depends on:** None.

### UX-02 — Wishlist: surface API errors; confirm/undo for Clear All (P2, Small)
- **Description:** All wishlist API failures (clear, sync, share toggle, regenerate) are silently swallowed; Clear All wipes UI state with no confirmation/undo and resurrects on refresh if the DELETE failed. Toast failures (ToastService exists), add confirm modal or undo (cart's undo toast is the in-repo gold standard), restore snapshot on failed clear.
- **Closes:** `_review/uiux.md` #4; UI_UX_REVIEW item #9.
- **Affected:** `features/wishlist/wishlist.component.ts:402-420`, `core/services/wishlist.service.ts`.
- **Acceptance:** Simulated 500 on share/clear shows a translated toast and leaves state consistent; component spec covers it.
- **Depends on:** Ideal rider on OPS-01 (uncommitted branch).

### UX-03 — Router scroll restoration (P2, Small)
- **Closes:** `_review/uiux.md` #5; item #10. **Affected:** `app.config.ts` (`withInMemoryScrolling`). **Acceptance:** Detail pages open at top; Back restores offset; verify interaction with route transition animations. 

### UX-04 — Header/footer: replace `timer(3200ms)` defer with `on idle` (P2, Small)
- **Description:** For the first 3.2s of **every cold load** there is no search, cart badge, language/theme switch, or user menu. Use `@defer (on idle)` (same for home-v3's `timer(2200ms)` sections); make the placeholder search focus the real search when loaded.
- **Closes:** `_review/uiux.md` #6; item #11.
- **Affected:** `core/layout/main-layout/main-layout.component.ts`, `features/home-v3/home-v3.component.html`.
- **Acceptance:** Header interactive when browser is idle (<1s desktop); Lighthouse stays within budget (re-run the PERF/A11Y-104 checks).
- **Depends on:** None.

### UX-05 — Account orders: real error state instead of fake "no orders" (P2, Small)
- **Closes:** `_review/uiux.md` #7; item #12. **Affected:** `features/account/orders/orders.component.ts:835-838`. **Acceptance:** Stubbed 500 shows translated error + working Retry (mirror product-list's pattern), not the empty state.

### UX-06 — Product detail: skeleton, error + retry, real 404 for bad slugs (P2, Medium)
- **Closes:** `_review/uiux.md` #8; item #13. **Affected:** `features/products/product-detail/product-detail.component.ts:29-36,897`; reuse `shared/skeleton`. **Acceptance:** Skeleton matches final layout in both themes; API 500 shows retry; unknown slug shows 404 UX.

### UX-07 — Light-theme `--color-error` → error-700 (WCAG AA) (P2, Small)
- **Description:** `#ef4444` on white is ~3.76:1 — fails AA for the small error text it styles; every other semantic token already uses the 700 shade. One-line token change.
- **Closes:** `_review/uiux.md` #9; item #14.
- **Affected:** `src/ClimaSite.Web/src/styles/_colors.scss:295`.
- **Acceptance:** Error text ≥4.5:1 in light theme (axe pass); visual spot-check of error-background components.

### UX-08 — One breadcrumb implementation (P2, Medium)
- **Closes:** `_review/uiux.md` #10; item #15. **Description:** Shared BreadcrumbComponent has zero usages; six pages hand-roll divergent breadcrumbs (some not `<nav>` landmarks). Migrate all six or delete the shared component and standardize the inline `<nav aria-label>` pattern. **Acceptance:** One implementation; all instances are landmarks, both themes.

### UX-09 — Breakpoint tokens; fix the 767/768 clash (P2, Medium)
- **Closes:** `_review/uiux.md` #11; item #16. **Affected:** `styles/_tokens.scss` (new `$bp-*` mixins), `bottom-nav.component.ts`, `wishlist.component.ts:307-309`. **Acceptance:** Bottom-nav visibility and content padding share one constant; no dead reserved space at 768px; migrate others incrementally.

### UX-10 — Real stock badge + out-of-stock CTA on PDP (P2, Small)
- **Closes:** `_review/product.md` #16; item #17. **Affected:** `product-detail.component.ts:119,1020`. **Acceptance:** Zero-stock variant shows OOS UI with disabled add-to-cart; E2E covers it.

### UX-11 — A11y polish: `role="alert"` on checkout/cart error banners; header search labels (P3, Small)
- **Closes:** `_review/uiux.md` #12, #13; items #19, #20. **Affected:** `checkout.component.ts:228-256`, `cart.component.ts:109-111`, `header.component.ts:70-83,222-232`. **Acceptance:** SR announces order failure; axe reports no unlabeled-form-element violation on the header.

### UX-12 — `/categories`: redirect to `/products` or build the category grid (P3, Small)
- **Closes:** `_review/uiux.md` #14; `_review/product.md` #15; item #21. **Affected:** `features/categories/category-list/`, `app.routes.ts:59-62`. **Acceptance:** `/categories` is functional or redirects; no "Coming Soon" reachable.

### UX-13 — Single EU energy-label palette (P3, Small)
- **Closes:** `_review/uiux.md` #15; item #22. **Affected:** `energy-rating.component.ts:131-140`, `product-card.component.ts:338-358`, `_colors.scss` (documented fixed-palette section). **Acceptance:** Identical class colors on card and detail.

### UX-14 — Offline/empty-state wiring (P3, Small)
- **Closes:** item #23. **Affected:** `shared/components/empty-state` (unused `offline` variant). **Acceptance:** Global offline detection shows the variant with retry.

### UX-15 — Color-contrast violations (WCAG AA), light + dark theme (P2, Small→Medium)
- **Status:** ✅ **DONE (PR #60, 2026-06-24).** Resolution: the dark-theme violations (`/promotions`,`/brands`,`/about`) were white text on `var(--color-primary)` (= light link-blue `#38bdf8` in dark = 2.14:1) → added a dedicated **`--color-primary-surface`** (primary-700 `#0369a1`, 5.93:1) and repointed the 3 CTA fills. The apparent **light-theme** violations were NOT color defects — axe was scanning *mid-`appReveal` opacity-fade*; fixed by running a11y scans under **reduced-motion** (`CreatePageAsync(reducedMotion: true)`). Both axe suites are now clean and **`A11Y_ENFORCE=1` is live in CI** (hard gate), with `[RetryFact]` on the data-driven `AccessibilityTests`. Unit-plan: `.planning/units/UX-15-contrast/`.
- **Description:** Fix the offending light + dark foreground/background pairs to meet WCAG AA (muted-text/badge tokens in `_colors.scss`). Then set `A11Y_ENFORCE=1` in the E2E job (Plan 19 C2) to make **both** axe suites hard gates.
- **Affected:** `src/ClimaSite.Web/src/styles/_colors.scss` (muted-text tokens, light + dark), product-list/detail/cart + promotions/brands/about styles.
- **Acceptance:** both `AxeAccessibilityMatrixTests` and `AccessibilityTests` report 0 serious/critical with `A11Y_ENFORCE=1`; then enforce in CI.
- **Depends on:** none. Gated `src/` change → via `/plan-tree` unit-plan. Pairs with UX-07 (light-theme error color) and UX-13 (energy-label palette).

### UX-16 — Transitional dual EUR/BGN price display (DEC-CURRENCY) (P3, Medium)
- **Status:** ✅ **DONE 2026-06-28.** Shared pure `DualPricePipe` (`| dualPrice`) renders `€X.XX / Y.YY лв` at the 1.95583 peg; replaced all 78 static `currency:'EUR'` store-price renders + 2 missed non-pipe patterns (fixed a latent `$`-symbol bug in installation-service); dynamic order-currency renders left intact. Inline format chosen. Pipe unit-tested (peg/grouping/negatives/null); council-reviewed (no High); /acceptance PASS. Follow-ups: summary-DTO currency field (only if multi-currency orders ever), promo-badge dual, aria-label on critical totals.
- **Description:** Introduce a shared `PricePipe`/`DualPriceComponent` that renders `€X.XX / Y.YY лв` from a single EUR amount + a peg constant, and replace the ~36 `| currency:'EUR'` renders with it (single source for price formatting). Decide placement/format (inline vs tooltip) — a small UX-design call.
- **Affected:** new shared pipe/component + all price renders across `src/ClimaSite.Web`.
- **Acceptance:** every price shows EUR + BGN at the 1.95583 peg; one shared formatter (no scattered `currency:'EUR'`); pipe unit-tested.
- **Depends on:** none (gated `src/` → unit-plan). Only needed before a BG launch (OPS-08).

---

## 7. Architecture (ARCH)

Detail: `docs/project-plan/ARCHITECTURE_REVIEW.md` (improvements 1-13) and `_review/architecture.md`. Respect its **"Areas to AVOID changing"** list (Stripe path, auth token chain, migration chain, error contract, guest-merge/DataSeeder) — touch those only via the tasks that own them.

### ARCH-01 — Delete dead `Features/Auth` tree; retarget its tests; add validators to the live auth commands (P1, Medium)
- **Description:** The auth unit tests test a dead duplicate command tree — the live handlers (token rotation, lockout) have **zero unit coverage**, and `Features/AGENTS.md:70` wrongly declares the dead tree canonical. Delete `src/ClimaSite.Application/Features/Auth/`, retarget the three test files at `ClimaSite.Application.Auth.Handlers`, add FluentValidation validators (email format/length, name length) to the commands `AuthController` actually dispatches, fix the AGENTS.md line.
- **Closes:** `_review/architecture.md` #2 (P1 confirmed); SR-16 / `_review/security.md` #7; auth slice of TS-16.
- **Affected:** `src/ClimaSite.Application/Features/Auth/` (delete), `src/ClimaSite.Application/Auth/`, `tests/ClimaSite.Application.Tests/Features/Auth/`, `src/ClimaSite.Application/Features/AGENTS.md`.
- **Acceptance:** grep `Application.Features.Auth` returns zero hits; Application tests pass against the live handlers; every dispatched auth command has a registered validator.
- **Depends on:** Do **before** any further auth work (incl. SEC-09, BUG-08 backend side).

### ARCH-02 — Dead-scaffolding sweep: repositories, Mapster, six unused directives (P2, Small)
- **Description:** Pure deletions, zero consumers proven: repository/UnitOfWork layer (~700 lines, incl. DI registrations), Mapster (package + `MappingConfig.cs` + DI lines), six unused directives (magnetic-hover, split-text, scroll-progress, animate-on-scroll, count-up, optimized-image) + index entries.
- **Closes:** `_review/architecture.md` #4, #13, #10; ARCHITECTURE_REVIEW improvement 5.
- **Affected:** `Core/Interfaces/I*Repository.cs`, `IUnitOfWork.cs`, `Infrastructure/Repositories/`, `Common/Mappings/MappingConfig.cs`, `shared/directives/`.
- **Acceptance:** Solution + ng build green; greps for the deleted symbols return nothing; CLAUDE.md/AGENTS.md updated via DOC-01/02 (repository pattern, CountUpDirective rows).
- **Depends on:** None.

### ARCH-03 — Consolidate the EF migrations into one folder (P2, Small)
- **Description:** One logical chain split across `Infrastructure/Migrations/` (2 migrations + the only snapshot) and `Infrastructure/Data/Migrations/` (6 migrations) — a standing "cleanup" trap. Move the two files + snapshot into `Data/Migrations/`, update namespaces only (never the `[Migration("...")]` IDs); verify with `dotnet ef migrations list` on a clean DB. Use the `db-migrate` skill; do it in isolation.
- **Closes:** `_review/architecture.md` #5.
- **Affected:** `src/ClimaSite.Infrastructure/Migrations/`, `src/ClimaSite.Infrastructure/Data/Migrations/`.
- **Acceptance:** One folder; clean DB applies all 8 migrations; a probe `migrations add` generates into the consolidated folder.
- **Depends on:** Do before TS-17 and before any new migration-bearing task (BUG-17, SEC-09).

### ARCH-04 — One API error contract (O-2): ProblemDetails or ratified custom shape (P2, Large)
- **Description:** Three incompatible error shapes (custom middleware JSON, ad-hoc `BadRequest(new { message })`, default ValidationProblemDetails) while CLAUDE.md claims ProblemDetails. One coordinated change: middleware (or `IExceptionHandler` + `AddProblemDetails`), a base-controller helper for `Result<T>` failures, and the frontend error interceptor — together. Stop echoing raw `ArgumentException` messages (SR-18) as part of the same pass.
- **Closes:** `_review/architecture.md` #6; SR-18 / `_review/security.md` #9; Program.cs TODO API-012.
- **Affected:** `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs`, all controllers' failure paths, frontend error handling.
- **Acceptance:** Contract-test sweep shows one schema for all 4xx/5xx; frontend parses a single shape; CLAUDE.md matches (DOC-01).
- **Depends on:** **O-2** (owner). Do NOT change the middleware shape piecemeal.

### ARCH-05 — Background-job mechanism (O-1) + email outbox processor (P2, Large)
- **Description:** Zero job infrastructure exists (no IHostedService/Hangfire/Quartz) — a hard blocker for Plan 12 notifications, wishlist `NotifyOnSale` (data is stored, nothing scans it), and guest-cart cleanup. ADR first (simplest viable: `BackgroundService` + DB email-outbox table; RabbitMQ exists in shared-infra if queuing preferred — **owner decides**), then implement the outbox dispatch as the first job since GAP-03 needs it for reliability.
- **Closes:** `_review/architecture.md` #12; O-1.
- **Affected:** `src/ClimaSite.Api/Program.cs`, new Infrastructure worker + outbox table/migration.
- **Acceptance:** ADR merged; one recurring job runs in Development with a retry-on-failure test.
- **Depends on:** **O-1**; precedes GAP-09; upgrades GAP-03 from fire-and-forget.

### ARCH-06 — Split the five 1,000–1,600-line god components (P3, Large)
- **Description:** header (1,566), product-list (1,435), checkout (1,231 — money path), order-details (1,082), product-detail (1,030): split to templateUrl/styleUrls (mechanical) and extract obvious children (mega-menu, search overlay, payment step, address step); add ESLint `max-lines` warning ~600 for new files; unify the `Auth/` vs `Features/*` folder taxonomy after ARCH-01.
- **Closes:** `_review/architecture.md` #7, #15.
- **Affected:** the five components; `eslint` config.
- **Acceptance:** Each file <~600 lines or split; ng test + E2E green; no visual regressions in either theme.
- **Depends on:** After OPS-01 merges (avoid conflicts); coordinate with any Stitch redesign work.

### ARCH-07 — Real-provider test fixture for transaction/constraint-dependent handlers (P3, Medium)
- **Description:** `MockDbContext` has no transaction/constraint fidelity and already warped production code (the `strategy is null` branch). Add an EF Core SQLite-in-memory (or Testcontainers) fixture for handlers using transactions/constraints (wishlist mutations, CreateOrderCommand); delete the null-strategy branch once covered; document when each fixture applies.
- **Closes:** `_review/architecture.md` #16; pairs with BUG-09.
- **Affected:** `tests/ClimaSite.Application.Tests/TestHelpers/`, `WishlistApplicationService.cs`.
- **Acceptance:** Transaction-using handlers have at least one real-provider test; the null branch is gone.
- **Depends on:** BUG-09 (same code).

---

## 8. Performance (PERF)

Detail: `_review/performance.md`. Its P0 (forwarded headers) lives here as **SEC-03**. The frontend bundle/lazy-loading story is verified good — don't re-fix it.

### PERF-01 — One caching owner (O-3): activate or delete the dead layers; kill the blanket 5-min output cache (P1, Medium)
- **Description:** `CachingBehavior` + 14 `ICacheableQuery` opt-ins are dead (never registered) — Redis is provisioned, health-checked (an availability failure mode), and unused; meanwhile a blanket OutputCache base policy silently caches **all anonymous GETs for 5 min with zero invalidation** (named policies/tags never referenced, `EvictByTagAsync` never called; revoked wishlist share links keep serving — SEC-10 note), and the ResponseCaching middleware is inert. Decide O-3, then: register `AddOpenBehavior(typeof(CachingBehavior<,>))` + mutation invalidation **or** delete the MediatR/Redis plumbing; remove the blanket base policy; apply named policies explicitly with `EvictByTagAsync` on product/category mutations; drop the inert middleware.
- **Closes:** `_review/performance.md` #2 (P1 confirmed), #4; `_review/architecture.md` #3; O-3.
- **Affected:** `Application/DependencyInjection.cs`, `Common/Behaviors/CachingBehavior.cs`, `Program.cs:245-250,302`, mutation handlers.
- **Acceptance:** Admin price update is visible to anonymous users immediately after tag eviction (integration test); either Redis keys appear and a second identical query skips the DB, or all dead caching code is gone; Redis removed from /health if unused.
- **Depends on:** **O-3** (owner). Do not enable caching without the invalidation story.

### PERF-02 — Facet query projection + pagination clamp (P1, Small)
- **Description:** `GetFilterOptionsQuery` hydrates the **entire active product table** (incl. Description + jsonb Specifications) per facet request; public list endpoints accept `?pageSize=100000` and negative paging (DoS amplification, 500s). Project only the 4 needed columns (then push aggregation into SQL); clamp in `PaginatedList.CreateAsync` (page ≥1, size 1..100) + FluentValidation validators on public paginated queries.
- **Closes:** `_review/performance.md` #3 (P1 confirmed), #6.
- **Affected:** `GetFilterOptionsQuery.cs:57`, `Common/Models/PaginatedList.cs:23-36`, `ProductsController.cs`, `SearchProductsQuery.cs`.
- **Acceptance:** EF-logged SQL transfers only needed columns; `pageSize=100000` returns ≤ max; `pageNumber=0/-1` → 400; unit tests cover the clamp.
- **Depends on:** None.

### PERF-03 — Query-shape fixes: brief-DTO projections, recommendations pre-filter, category-descendant batching, dashboard KPIs (P2, Medium)
- **Description:** List/search handlers materialize full entities with 3 Includes for 10-field DTOs; the homepage recommendations handler scores the whole in-stock catalog in C# per call (low-cardinality inputs begging for cache/SQL pre-filter on the existing jsonb GIN index); `GetDescendantIdsAsync` is an N+1-by-depth duplicated in two hot handlers; the admin dashboard fires 13 sequential aggregates. Rewrite as `.Select()` projections (do together with BUG-06/BUG-12 — same code), pre-filter/cache recommendations, batch descendants in one query, collapse KPIs to 2-3 grouped queries.
- **Closes:** `_review/performance.md` #7, #11, #12, #13; ARCHITECTURE_REVIEW improvement 9.
- **Affected:** `GetProductsQueryHandler.cs`, `SearchProductsQuery.cs`, `GetRecommendationsQueryHandler.cs`, `GetFilterOptionsQuery.cs`, `GetDashboardKpisQuery.cs`.
- **Acceptance:** EF logs show projected columns only; recommendation DB rows bounded; one query per descendant resolution; KPI handler ≤4 queries with identical output.
- **Depends on:** Bundle with BUG-06 + BUG-12.

### PERF-04 — Frontend change-detection hygiene + route preloading (P2, Medium)
- **Description:** Zone-based CD with 10/85 OnPush and zero `runOutsideAngular` — every scroll/rAF/confetti frame runs app-wide change detection. Wrap the four rAF/scroll loops (`animation.service.ts`, `confetti.service.ts`, `flying-cart.service.ts`, header scroll listener) in `NgZone.runOutsideAngular`; continue Plan 18 **PERF-100** (OnPush ≥70%, currently ~12%); add `withPreloading(PreloadAllModules)` and a preload hint for the persisted language's i18n JSON.
- **Closes:** `_review/performance.md` #8, #16; Plan 18 PERF-100.
- **Affected:** the four services/components, `app.config.ts`, `index.html`.
- **Acceptance:** DevTools trace while scrolling /products shows no per-frame app-wide CD; OnPush ≥70%; product/cart chunks fetched during idle; Lighthouse unchanged or better.
- **Depends on:** None.

### PERF-05 — Real search backend or corrected claim (P2, Large, decision)
- **Description:** "Full-text search" is actually ~10 ILIKE predicates per term (sequential scan; existing GIN indexes unusable). Pick per **DEC-SEARCH**: Postgres tsvector + GIN, pg_trgm, or shared-infra Meilisearch; until then correct the CLAUDE.md claim (DOC-02).
- **Closes:** `_review/performance.md` #5 (P2 adjusted).
- **Affected:** `SearchProductsQuery.cs`, `ProductConfiguration.cs`, migration.
- **Acceptance:** EXPLAIN ANALYZE shows index usage; p95 flat to 10k products; docs match implementation.
- **Depends on:** **DEC-SEARCH** (owner).

### PERF-06 — Image pipeline: sized variants + WebP + NgOptimizedImage/srcset (P2, Large)
- **Description:** Zero srcset/NgOptimizedImage; admin-uploaded originals served at full resolution from MinIO for ~300px cards. Generate thumb/card/detail WebP variants at upload in `MinioStorageService`, expose in `ProductImageDto`, adopt `ngSrcset` in card/gallery (or front MinIO with imgproxy).
- **Closes:** `_review/performance.md` #9.
- **Affected:** `Infrastructure/Services/MinioStorageService.cs`, `ProductImageDto`, `product-card`/`product-gallery` components.
- **Acceptance:** Card images ≤2× display width in WebP/AVIF (network panel); PDP LCP image appropriately sized with `fetchpriority=high`.
- **Depends on:** Production storage decision (OPS-08 Q3).

### PERF-07 — SSR/prerender decision; per-route meta now (P2, Large, decision)
- **Description:** Pure client SPA — crawlers/unfurlers see one generic title for every product URL. **DEC-SSR** options: Angular SSR + hydration for product/category/home; prerender + edge meta; or accept and document. Regardless, implement per-route Title/Meta service usage now (cheap, partial win).
- **Closes:** `_review/performance.md` #10.
- **Affected:** `package.json`/`angular.json` (if SSR), route components, `index.html`.
- **Acceptance:** Decision in `docs/adr/`; curl of `/products/{slug}` returns product-specific meta (if SSR/prerender chosen); meta service usage shipped either way.
- **Depends on:** **DEC-SSR** (owner — affects deploy topology, OPS-03).

---

## 9. Documentation (DOC)

Detail: `_review/docs.md` (incl. the full per-file Disposition Table) and `_review/status.md`. DOC-01/DOC-02 are P1 and sit in the Next 10 — this category is last only because the rest of it is P2/P3.

### DOC-01 — Fix the executable facts: E2E commands, ports, paths, stack versions (P1, Small)
- **Status:** ✅ DONE (2026-06-16, branch `docs/doc-01-fix-test-commands`). Fixed CLAUDE.md + root/E2E AGENTS.md (per-project test commands, `dotnet test tests/ClimaSite.E2E`, port 5029, EF 10.x, Tailwind, `TestDataFactory.cs`, `?lang=`, dropped CountUpDirective row); added `ClimaSite.NoE2E.slnf` (builds green, E2E excluded). grep confirms no `npx playwright`/`localhost:5000`/`test-data-factory.ts` remain.
- **Description:** CLAUDE.md + root AGENTS.md + `tests/ClimaSite.E2E/AGENTS.md` document a **TypeScript Playwright suite that has never existed** (`npx playwright test`, `fixtures/test-data-factory.ts`, port 5000, `tests/ClimaSite.Web.Tests`, Playwright report :9323) — the mandatory post-implementation workflow is unexecutable as written, and bare root `dotnet test` silently runs E2E without servers. Replace with the corrected commands from `TESTING_STRATEGY.md` §1.3 / `DEV_WORKFLOW.md` §2.3 (`dotnet test tests/ClimaSite.E2E`, port 5029, `Infrastructure/TestDataFactory.cs`, env vars); add `ClimaSite.NoE2E.slnf`; fix EF "9.x" → 10.x, add Tailwind to the stack table, fix `docs/skills.md` → `docs/skills/`, drop the dead CountUpDirective Quick-Reference row, fix the Accept-Language claim (reality: `?lang=`).
- **Closes:** `_review/docs.md` #1, #2, #9, #13; `_review/testing.md` #1 (TS-02); `_review/devops.md` #7; `_review/architecture.md` #11; `_review/status.md` #11; `_review/bugs.md` #18 (doc part).
- **Affected:** `CLAUDE.md`, `AGENTS.md`, `tests/ClimaSite.E2E/AGENTS.md`, new `.slnf`.
- **Acceptance:** Every command block in all three docs executes successfully on a fresh checkout (with documented prerequisites); grep for `npx playwright`, `localhost:5000`, `test-data-factory.ts` returns nothing outside archives.
- **Depends on:** None — do before the bug-fix wave so agents can run tests.

### DOC-02 — Status truth pass: CLAUDE.md table + master overview (P1, Small)
- **Status:** ✅ DONE (2026-06-26). Verified per-feature status against the code (4-cluster parallel read + hand spot-checks); rewrote `PROJECT_STATUS.md` as a dated, evidence-backed SSOT. REFUTED the stale "broken/stub" claims (admin/notifications/contact/legal/installation/GDPR/payments verified complete); corrected 2 verifier errors (BUG-03 fixed, wishlist-merge exists); confirmed the real open gaps map to existing rows (BUG-11, search-FTS, inventory-reservations, SEC-08/12/13/14, Lighthouse-enforce, OPS-05/08/11). CLAUDE.md status tables already removed → pointer; `00-master-overview` bannered (DOC-04). No status claim now contradicts code.
- **Description:** Correct the claims that drove this review's biggest gaps: Admin Panel → "Backend complete; frontend: moderation only"; Notifications → accurate Partial scope ("no email dispatch from flows, no frontend"); Checkout → "payment linkage broken (BUG-01/02)"; Inventory → "backend-only, no reservations"; Performance → "Partial — PERF-100/104 open"; Search → "ILIKE, not FTS"; guest-checkout business rule → match GAP-07's outcome; drop the parallax row. In `docs/plans/00-master-overview.md`: delete/date-stamp the "100% Complete" section, fix the plan index (dead links to 15/16 + archived 01-11, Notifications "Complete", missing 18-22) or demote the file to archive with a pointer to this folder.
- **Closes:** `_review/status.md` #9 + doc slices of #1/#3; `_review/docs.md` #3; `_review/product.md` #17; `_review/performance.md` #14.
- **Affected:** `CLAUDE.md`, `docs/plans/00-master-overview.md`.
- **Acceptance:** No status claim in either file contradicts code or Plan 18; all index links resolve.
- **Depends on:** None; refresh again as GAP/BUG tasks land.

### DOC-03 — README + deployment runbook (P2, Medium)
- **Description:** No repo README, no human onboarding path, no deployment doc for the Railway/Docker artifacts. README: what/stack/prerequisites/shared-infra setup/verified commands/doc map (source: `DEV_WORKFLOW.md`). `docs/operations/deployment.md`: Railway service-to-config mapping (from OPS-08), Dockerfiles, env-var matrix (devops Dimension data §4), migration + rollback story.
- **Closes:** `_review/docs.md` #5 (P2 adjusted); devops onboarding slice of #8.
- **Affected:** new `README.md`, new `docs/operations/deployment.md`.
- **Acceptance:** Fresh-clone smoke test: clone → running app + one passing E2E using only README; every root-level deploy artifact explained.
- **Depends on:** DOC-01 first (README copies correct commands); OPS-08 answers.

### DOC-04 — Tracker consolidation: issue registry, validation suite, plan numbering, Plan 12 reconciliation (P2, Medium)
- **Status:** 🚧 LARGELY DONE (2026-06-25). Done: added `docs/README.md` (hierarchy map) + `docs/plans/README.md` (active-vs-archived index, numbering collisions labelled); superseded-bannered `docs/plans/00-master-overview.md`, `20-issue-registry.md`, and the 2026-01-24 `docs/validation/` suite (17 area docs; kept the refreshed `07-wishlist.md`); fixed 13 broken links → 0; retired the `docs/features/` PROC-01 pipeline (banner → vault flow). Remaining: optionally MOVE superseded plans into `docs/plans/archive/`; the 21-series ~700-task extraction (owner call); Plan 12 (0/163) reconciliation; Plan 18 `.auto-memory`→`.codex/PROJECT_MEMORY.md` guardrail fix; archive `docs/skills/` motion research.
- **Description:** Four mutually inconsistent trackers exist. (a) Triage-or-archive `docs/plans/20-issue-registry.md` (claims 129 open/0 fixed; verified fixes + deleted-code targets exist) — recommended cheap path: banner "superseded by Plan 18 + docs/project-plan/PRIORITIZED_BACKLOG.md". (b) Banner the 2026-01-24 validation suite as superseded by the 2026-04-08 gap report + this review (fix the inverted-stale SEC-003 GDPR row). (c) Archive superseded plans (19-ui-ux-redesign-masterplan, 20-quality-audit, 21-ui-improvement, 22) with successor banners; resolve the two-Plan-19/four-Plan-21 numbering collisions; extract surviving 21B-21J tasks into Plan 18 or drop (owner call, ~700 boxes). (d) Reconcile Plan 12's 0/163 checkboxes into an accurate implemented-vs-remaining header (input for GAP-09). (e) Fix Plan 18's `.auto-memory/` guardrail → `.codex/PROJECT_MEMORY.md`; archive `docs/skills/` motion research that contradicts the adopted 21F direction.
- **Closes:** `_review/docs.md` #4, #6, #7, #8, #15, #16; `_review/status.md` #10.
- **Affected:** `docs/plans/`, `docs/validation/`, `docs/skills/`.
- **Acceptance:** One authoritative tracker chain remains (Plan 18 + this backlog); every archived doc carries a successor banner; registry counts true or file archived.
- **Depends on:** Owner answers on 21-series task survival and registry triage depth (docs.md open questions).

### DOC-05 — ADR backfill + repair ADR process integrity (P2, Medium)
- **Status:** 🚧 PARTIAL (2026-06-25). Done: created **ADR-0003** (Canvas-2D renderer) superseding ADR-002's in-place renderer amendment; restored ADR-002 immutability (status-line pointer only, body untouched); fixed the `docs/adr/README.md` index (added ADR-0001 + 0003 rows); promoted DECISIONS.md D-014. Remaining: the wishlist share-token ADR (O-5 / D-015) and backfilling short ADRs from DECISIONS.md §2 toward the ≥8 target.
- **Description:** Only 2 ADRs exist and ADR 002 was amended in-place against its own immutability rule (Canvas 2D vs the decided Three.js). Write ADR 003 superseding the renderer decision; ADR for the wishlist share-token design (O-5 — ideally before OPS-01 merges); backfill short ADRs from `DECISIONS.md` §2 (Argon2, rate limiting, Tailwind-only, shared-infra, EF 10, Core→Identity exception) toward Plan 18 Phase 8's ≥8 target; record each blocking decision from this backlog as it's made.
- **Closes:** `_review/docs.md` #10; `_review/architecture.md` #14; O-5.
- **Affected:** `docs/adr/`.
- **Acceptance:** ADR index covers the Canvas amendment + wishlist sharing; no accepted ADR body contains decision-reversing edits; ≥8 ADRs exist.
- **Depends on:** O-5; DECISIONS.md is the source list.

### DOC-06 — Agent-context sync: AGENTS.md regeneration, skills trees, memory-file roles (P3, Medium)
- **Description:** Root AGENTS.md is a stale 2026-01-25 snapshot (wrong counts: "9 directives" vs 7, "20 feature folders" vs 18, "28 services" vs 19); skills exist in three diverging trees (`.claude/`, `.codex/`, `.opencode/`). Declare the hierarchy in each file's header (CLAUDE.md canonical; AGENTS.md generated snapshot + regeneration rule; PROJECT_MEMORY.md session state that links rather than restates); regenerate nested AGENTS.md; keep `.claude/skills` as source, sync/derive `.codex`, delete or sync the `.opencode` orphan.
- **Closes:** `_review/docs.md` #11; `_review/architecture.md` agent-config drift note.
- **Affected:** `AGENTS.md` (root + nested), `.codex/PROJECT_MEMORY.md`, `.opencode/skills/`.
- **Acceptance:** Each context file states its role and update trigger; counts match code; one skills source of truth.
- **Depends on:** DOC-01 (same edit pass for command blocks).

---

## 10. Process (PROC)

### PROC-01 — SDLC hardening: gated phases, agents/skills, and CI/hook enforcement (P1, Large)
- **Status:** 🚧 IN PROGRESS (2026-06-21). Plan owner-approved; canonical detail in `docs/project-plan/SDLC_HARDENING_PLAN.md`; live execution tracker in `docs/features/PROC-01/STATE.md`. Wave 0 merged (#40); Wave 1 in progress.
- **Description:** Build the development process "like a real company": eight discrete, gated phases (research → plan → verify-plan → track → implement → test → review → merge) that can't be skipped, enforced via pinned agents/skills + phase-aware hooks + CI required-checks + branch protection. Strongest gates are TEST (real-infra E2E/UI/visual/a11y/perf, no mocking) and REVIEW (independent read-only reviewer/verifier agents).
- **Closes:** the process/governance gaps in `_review/devops.md`, `_review/testing.md`, `_review/docs.md` (doc drift, advisory-only coverage, no visual/perf gate, untested money path).
- **Affected:** `.claude/agents/*`, `.claude/skills/*`, `.claude/settings.json` + `hooks/`, `.github/workflows/*`, `docs/features/*`, `CLAUDE.md`, `DEV_WORKFLOW.md`.
- **Acceptance:** Each of the 7 waves lands as its own green-CI PR; the eight-phase pipeline is documented (`docs/features/README.md`) and dogfooded; hard gates (Wave 2 hooks, Wave 3 coverage, Wave 6 real-card E2E) enforce the phases. Owner-approved pauses before flipping any gate that could block all future PRs.
- **Depends on:** None (foundational). Waves are sequential (each builds on the prior).

---

## External review — newly-tracked items (2026-06-28, triaged)

These are the **genuinely-new, CONFIRMED-real** findings from the 2026-06-28 external review that **no existing `SEC/BUG/GAP/OPS/UX/PERF/ARCH/TS/DOC` task already tracks** — full triage + council notes in [`EXTERNAL_REVIEW_TRIAGE.md`](EXTERNAL_REVIEW_TRIAGE.md). IDs keep their review `B-nnn` and carry a suggested home. **All B-IDs that map onto an existing task are NOT re-listed here** — the register cross-references them (e.g. B-005→UX-01, B-036→PERF-02, B-041→UX-03, B-018→UX-05, B-060→UX-06, B-008→ARCH-04/SR-18, B-003→SEC-12, B-058→OPS-06, B-059→TS-16, B-051→ARCH-03). Nothing is deployed yet (OPS-08), so deploy-conditional items are latent.

### NEW-BUG (functional defects)
- **FOUND-B002-orphans (Low, S — latent/dead-code) ✅ DONE 2026-06-30:** Confirmed dead (zero `<app-*>` usages, zero imports, no routes/barrels) → **deleted** `frequently-bought` + `product-variants` components + specs, removing the inverted-convention landmine outright. (frontend-cleanups.)
- **FOUND-B002-noimage (Low, XS — cosmetic) ✅ DONE 2026-06-30:** Repointed the 3 `assets/images/no-image.svg` references to the existing canonical `assets/images/fallbacks/no-product-image.svg` (served 200, verified live). (frontend-cleanups.)
- **B-007 (Medium, XS) ✅ DONE 2026-06-29:** Order email CTAs built `/account/orders/$<guid>` (literal `$`) → 404. Fixed via a single `EmailService.BuildOrderUrl` helper (no `$`) used by both order emails; unit-tested. (QW-backend-batch.)
- **B-016 (Medium, M):** Recommendation scoring reads canonical spec keys (`btu`,`isInverter`,`noiseLevel`,…) that `DataSeeder` never emits (it writes `"BTU"` int / `"Noise Level"` string), so HVAC fit-scoring silently collapses to fallback for display-key products (`RecommendationScoringService.cs:41` vs `DataSeeder.cs:247`; **council: seeding is Dev/Test-gated, not "100% of prod"**). **Fix:** enforce a canonical HVAC spec schema at the seed/import/admin boundary, or make the `Extract*` helpers alias-aware; add a recommendation test seeded via `DataSeeder`.
- **B-038 (Medium, S):** `AnswerQuestionCommand.cs:74` calls `MarkAsAnswered()` on submission of a still-`Pending` public answer, so a question is flagged answered (hidden from the unanswered queue) while `AnswerCount` renders 0 (`GetProductQuestionsQuery.cs:32-41`). **Fix:** set `AnsweredAt` only on first approval, or recompute answered-state from `Answers.Any(a => a.Status == Approved)`.

### NEW-UX / a11y (Definition-of-Done: keyboard + SR, both themes, EN/BG/DE)
- **B-001 (Medium, S) ✅ DONE 2026-07-01 (a11y batch):** the address add/edit/delete dialogs now render via the shared `<app-modal>` (`role=dialog`+`aria-modal`+`aria-labelledby`+Escape/backdrop close+focus-trap+focus-restore). Surfaced+fixed a shared-modal gap: a modal a parent `@if`-renders is DESTROYED on close before the `isOpen→false` focus-restore runs → `ModalComponent.ngOnDestroy`/`cleanup()` now restores focus too (null-guarded, no double-restore) — helps every `@if`'d consumer. **Keyboard-driven `/acceptance` PASS** (open→Tab-trap→Escape→focus back on the trigger). (a11y-dialogs-radios.)
- **B-014 (Medium, S) ✅ DONE 2026-07-01 (a11y batch):** the checkout payment+shipping `<input type=radio>` are no longer `display:none` — sr-only-but-**focusable** + a visible `:focus-visible` ring; each set wrapped in `role="radiogroup"` named by its heading (`aria-labelledby`); selected-card state tied to the native radio via `:has(input:checked)`. Native radios kept (native arrow-key selection). Council confirmed real focusable radios. (a11y-dialogs-radios.)
- **B-020 (Medium, S) ✅ DONE 2026-06-29:** Cart load failure no longer renders as a fake empty cart. `loadCart` now sets a dedicated `_loadFailed` (separate from transient mutation `_error`) + preserves the loaded cart; the cart page, **checkout, and the mini-cart drawer** (council caught the latter two) show an accessible error + Retry before their empty state. Live-proven; specs added. (B-018-B-020-load-error-states.)
- **FOUND-loaderr-race (Low, S) ✅ DONE 2026-06-30:** `loadOrders()`/`loadCart()` now carry a monotonic `loadSeq` token — only the most recent load writes state, so a stale request resolving after a newer one can't re-show an error or overwrite fresh data. Deterministic out-of-order tests (cart + orders). (frontend-cleanups.)
- **B-042 (Medium, S) ✅ DONE 2026-07-01 (a11y batch):** the checkout saved-address cards are now a `role="radiogroup"` of `role="radio"` (accessible name = the visible heading via `aria-labelledby`) with `aria-checked`, roving `tabindex` (selected=0/else -1; first=0 when none selected), and Enter/Space + Arrow-key selection with wrap-around (WAI-ARIA APG). Visible `:focus-visible` ring. (a11y-dialogs-radios.)
- **B-049 (Low, XS):** Cart removal toast renders `"… - Undo"` text but passes no action callback; `undoRemoval()` exists but is never wired (`cart.component.ts:775,794`). **Fix:** wire the toast action to `undoRemoval(item.id)`, or remove the "Undo" copy until the action exists.
- **B-033 (Low, XS) ✅ DONE 2026-06-30 (with B-044 Wave A):** `LanguageService.initializeTranslations()` now sets `document.documentElement.lang = initialLang` on first paint (browser-guarded) so a returning bg/de user isn't `lang="en"`; `/acceptance` confirmed `<html lang="de">` after an EN→DE switch.

### NEW-SEC (hardening — low individual stakes)
- **B-034 (Medium, XS) ✅ DONE 2026-06-29:** `POST api/installation/requests` (anonymous PII lead + outbox email) now carries `[EnableRateLimiting("strict")]` (5/min/IP, like contact); live-proven 429 after the budget. (QW-backend-batch.)
- **B-039 (Medium, M) ✅ DONE 2026-07-01:** per-voter Q&A vote ledger shipped, mirroring `ReviewVote`. New `ProductQuestionVote`/`ProductAnswerVote` entities + `UNIQUE(target,user)` (expand migration `AddQaVoteLedgers`, no count zeroing); voting now `[Authorize]`+strict-rate-limited, self-vote→400, non-approved→404, **toggle** (re-click un-votes, answer flip). DTO exposes `hasVotedHelpful`/`userVoteHelpful` (batched, no N+1) so the UI uses authoritative server state (localStorage tracking removed; active/pressed + anon-disabled + per-id overlap guard). Concurrency: atomic `ExecuteUpdate` count deltas gated on rows-affected via `ON CONFLICT`/conditional `DELETE`/`UPDATE WHERE is_helpful=@from`, with the read+decision **outside** the retrying execution-strategy tx (idempotent under `EnableRetryOnFailure` — break-probe-proven). GDPR hard-deletes the votes (counts stay as anonymized aggregates). Chose authenticated-only over anon-token (council). 3 design council rounds + backend REWORK (retry idempotency)→CLEAN + combined APPROVE-WITH-CHANGES→fixed; `/acceptance` PASS (`.planning/acceptance/B-039-qa-vote-ledger.md`). Follow-up: `"strict"` rate-limit is IP- not user-partitioned.
- **B-029 (Low, S):** Generic contact/installation email bodies concatenate raw user `Name/Email/Message` and are sent with `IsBodyHtml=true` unescaped (`CreateContactMessageCommand.cs:59` / `EmailService.cs:127`), unlike the order/welcome templates which `EscapeHtml`. Recipient is the internal business address (no JS exec) → Low. **Fix:** send Generic bodies as `text/plain`, or `HtmlEncode` the user fields.
- **FOUND-QW-admin-pagination (Low, S) ✅ DONE 2026-06-30:** Every auth-gated paginated list (admin products/orders/customers/questions/reviews/**inventory**/**installation-requests** + the authenticated **notifications** list) now clamps `pageNumber`/`pageSize` via `QueryBounds` at the controller — `pageSize=0` div-by-zero / huge-`pageNumber` overflow can no longer 500, and `pageSize=100000` caps at 100 (live-proven on the 2260-product DB). Integration `AdminPaginationBoundsTests` (16 cases). Its council caught inventory + installation-requests + the notifications list that the first pass missed. AdminDashboard `count` + Notifications `recentCount` left (different "recent N" semantics). (FOUND-QW-admin-pagination.)
- **B-055 (Low, XS) ✅ DONE 2026-06-29:** `CorrelationIdMiddleware` now honours an inbound `X-Correlation-Id` only if it matches `\A[A-Za-z0-9._-]{1,128}\z` (absolute anchors), else a fresh GUID — blocks log-forging/oversize. Live-proven + middleware unit-tested. (QW-backend-batch.)

### NEW-SEO
- **B-044 (Medium, M/L) ✅ DONE 2026-06-30 (Wave A #92 + Wave B PR):** SEO foundation shipped. **Wave A:** `SeoService` (per-route `<title>`+description+canonical+OG/Twitter+robots, re-applied on lang change) + a `TitleStrategy`; `StructuredDataService` wired into home/product/list/FAQ/breadcrumb (stable ids, cleared on NavigationStart); `index.html` brand title + raster `og-default.png`; out-of-order `loadSeq`/`fetchSeq` guards; **a fatal NG0200 circular-DI that broke ALL runtime SEO was caught only by `/acceptance`** (Router removed from SeoService/TitleStrategy; reset moved to an app-initializer). **Wave B:** dynamic `~/robots.txt` + `~/sitemap.xml` (2285 URLs, `/products/category/{slug}`, `IsCurrentlyActive` promos, `Uri.EscapeDataString` slugs), host-injection-safe base (`Seo:SiteBaseUrl` first, **fail-closed 503 in Staging/Prod when unset**), `X-Robots-Tag: noindex` on `/api`, nginx `=`-locations + private-prefix noindex blocks, `[OutputCache(NoStore=true)]` (cross-host poisoning fix), M8 `MetaTitle/MetaDescription` passthrough. 3 design council rounds + both diffs councilled; `/acceptance` PASS (EN+DE) at `.planning/acceptance/B-044-seo-wave{A,B}.md`. *Full DEC-SSR crawlability (SSR/prerender, non-JS-scraper OG, hreflang) remains a separate owner decision.*
- **B-048 (Low, XS) ✅ DONE 2026-06-30 (with B-044 Wave A):** breadcrumb JSON-LD de-duped — the dead component-template `<script>` is gone; the pages with visible breadcrumbs emit a single `BreadcrumbList` via the head-service `setBreadcrumbData` (stable `breadcrumb` id, cleared on NavigationStart). *(The `BreadcrumbComponent` was actually unused, so the original "move to the service" was moot — the real fix was wiring the pages.)*

### NEW-CHORE (tech-debt / tooling)
- **B-053 (Low, S):** Guest cart expiry is enforced on read only (`GetCartQuery.cs:41`); no cleanup job exists, so expired guest carts persist indefinitely (bloat + retention angle). **Fix:** add a `BackgroundService` batch-deleting carts where `UserId IS NULL AND ExpiresAt<now`; **council: also enforce `ExpiresAt` on cart writes** (`AddToCartCommand.cs:134` currently resurrects expired carts).
- **B-047 (Low, XS):** `src/assets/config.json` (`{ "apiUrl":"/api" }`) advertises runtime API config but is referenced nowhere (services read `environment.apiUrl`) — an operator edit-trap. **Fix:** delete it (+ the dist copy), or wire a real runtime-config app-initializer; document the single config path.
- **B-056 (Low, S):** `specs-table.component.ts:8` and `share-product.component.ts:64` accept/render icon strings via `[innerHTML]` — inputs are static today (Angular auto-sanitizes; no live XSS), but it's a data-driven-HTML API smell. **Fix:** accept a typed icon-name (or `ng-template` outlet) + a lint rule forbidding new data-driven `[innerHTML]`.
- **B-026 (Low, S):** No single-command local E2E orchestrator (start API+web → healthcheck → run → teardown) and the card-E2E self-skip (`CardPaymentE2ETests.cs:83,117`) isn't surfaced in the CI Test Summary. **Fix:** add `scripts/e2e-local.sh`; emit a CI step-summary line when card E2E self-skips. *(The PAY-IDEM acceptance report is the in-flight unit's own pre-merge gate, not tracked here.)*

---

## Next 10 tasks — the strict execution order

1. **OPS-01 — Commit, push, and PR the wishlist slice.** Four days of finished multi-layer work is one `git checkout .` from non-recoverable loss, and every status doc already claims it as done; the PR's CI run also closes the missing test evidence (TS-01). Take SEC-10/BUG-09/BUG-10/UX-02 as riders if cheap.
2. **SEC-01 — Gate DataSeeder admin credentials and demo data** (with OPS-08's "is anything live?" check). The repo is public, so `admin@climasite.local` / `Admin123!` is a published backdoor the moment any deploy happens; not tracked by Plan 18 at all.
3. **OPS-02 — Protect `main` and fix CLAUDE.md's direct-push step.** Five-minute change that turns the entire CI investment from advisory into a gate before the wave of fixes below starts landing.
4. **DOC-01 — Fix the test commands/ports/paths docs.** Every subsequent task (human or agent) needs runnable commands; today the mandatory workflow fails as written and bare `dotnet test` produces 200+ false failures.
5. **BUG-02 — Server-side payment amount/currency (ask DEC-CURRENCY first).** Every honest card order currently underpays ~49% and omits shipping, and €0.01 manipulation is trivially exploitable — nothing about the shop is sellable until the charge is right.
6. **BUG-01 — Persist `paymentIntentId` and make webhooks reconcile orders** (BUG-18 rider). Without it 100% of card orders are charged-but-stuck-Pending with no recovery path; pairs with BUG-02 in the same PR plus TS-03 regression tests.
7. **BUG-03 — Fix the guest-cart merge 400.** Deterministic, silent cart loss for every first-time customer who logs in — the single worst conversion bug in the product, and a Small fix.
8. **SEC-03 — `UseForwardedHeaders` before the rate limiter.** In the deployed topology the whole store shares one 100 req/min bucket — a guaranteed self-inflicted outage at the first modest traffic, invisible in local testing.
9. **BUG-07 — Send the password-reset email and stop logging the token.** Users are silently locked out forever while live reset tokens sit in the logs; Small fix that also closes security finding SR-05.
10. **GAP-01 — Admin orders page (status + tracking).** After the money path works, the shop still cannot be operated: no UI exists to fulfill an order. This is the minimum-operability slice and unblocks shipped-email notifications (GAP-03).

**Immediately after the ten:** SEC-02 (order-by-number IDOR), TS-04 (Stripe card-path coverage), GAP-03 (transactional emails), GAP-04/GAP-05 (EU legal pages + real contact endpoint), ARCH-01 (dead auth tree), UX-01 (checkout form errors), then OPS-03/04/05 to make the first deploy safe.
