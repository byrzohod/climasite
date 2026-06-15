# ClimaSite — Security Review

**Date:** 2026-06-11
**Sources:** `docs/project-plan/_review/security.md` (primary), plus security-relevant findings from `_review/bugs.md`, `_review/devops.md`, `_review/status.md`. Verification status is carried over from those reviews: P0/P1 items were independently re-verified against the working tree; items marked "reviewer-verified" were verified once by the dimension reviewer but not re-checked.

**How to use this document:** This is the authoritative security state of ClimaSite as of 2026-06-11. Work the Findings section top-down — the three Critical items are launch blockers and must be fixed (and re-tested) before any production deployment; High items must be fixed before launch; Medium items before or immediately after; Low items are scheduled hardening. Each finding gives exact `file:line` evidence, a concrete fix, and acceptance criteria, so a developer or AI agent can act on it without re-deriving the analysis. The Production-readiness checklist at the end is the gate for any deploy. Where Plan 18 Phase 5 (`docs/plans/18-project-completion.md`, SEC-100..107) already tracks an item it is cited; items NOT covered by Plan 18 are explicitly flagged so they don't fall through.

---

## Executive security posture

The security **baseline is solid**: JWT validation is correctly configured (issuer/audience/lifetime/signing-key validation, zero clock skew, HMAC-SHA256, 15-minute access tokens); refresh tokens are 64 crypto-random bytes, rotated on login/refresh and revoked on logout/password-reset; ASP.NET Identity enforces password policy plus lockout (5 attempts / 15 min); CORS is origin-restricted with credentials; the Stripe webhook verifies signatures (`WebhooksController.cs:58-71`); all admin controllers carry `[Authorize(Roles="Admin")]`; most resource-scoped handlers (orders-by-id, addresses, notifications, wishlist, GDPR delete) correctly filter by the authenticated user; EF Core parameterizes everything (no raw SQL); and **dependency scans are clean** (NuGet + npm prod, verified). GDPR deletion is well implemented (password re-confirmation, anonymization, SecurityStamp rotation).

The project is nevertheless **not production-launchable** from a security standpoint. Three Critical issues stand out:

1. **Hardcoded admin credentials seeded at production startup** — `DataSeeder` runs unconditionally and creates `admin@climasite.local` / `Admin123!` (the repo is public on GitHub, so the credential is published).
2. **Client-controlled payment amounts** — Stripe intents are created from a client-supplied amount, in the wrong currency (BGN vs EUR orders), excluding shipping; trivially exploitable for €0.01 checkouts.
3. **Payments are never reconciled to orders** — `paymentIntentId` is silently dropped on order creation, so the webhook (the only server-side payment integrity check) can never match an order.

Beyond those: an anonymous IDOR leaks customer PII via order-number lookup; the forgot-password flow logs reset tokens in plaintext and never sends the email; and the per-IP rate limiter collapses to one shared bucket behind the Railway/nginx proxy, neutralizing brute-force protection and creating a mass-lockout vector. Plan 18 Phase 5 (SEC-100..107) covers committed secrets, security headers, Swagger gating, CORS tightening, rate-limit extension, and dependency auditing — but it does **not** track the DataSeeder credentials, the order-by-number IDOR, forwarded-headers handling, TestController exclusion, payment reconciliation, or refresh-token hashing. Those need to be added as explicit tasks.

> **Stale-doc note:** Plan 18 SEC-100 states "prod env values were already env-var, so production is safe". That is too optimistic — the JWT env-var requirement only triggers when the environment name is literally `Production` (`JwtConfiguration.cs:21-24`), and Stripe/SMTP/MinIO env names documented in `CLAUDE.md` don't map to the config-section keys the code actually reads (see SR-11). Treat SEC-100's safety claim as superseded by this review.

---

## Findings

Format: severity (Critical/High/Medium/Low) and priority (P0–P3) per the project priority model. Ordered Critical → Low.

### Critical

#### SR-01 — Production startup seeds hardcoded admin credentials (`Admin123!`) and demo catalog unconditionally
- **Severity:** Critical | **Priority:** P0 | **Verification:** confirmed (devops review, independently re-verified)
- **Evidence:** `src/ClimaSite.Api/Program.cs:32` calls `SeedDatabaseAsync(app)` with no environment gate; `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:27-46` runs migrate + seed in all environments; `DataSeeder.cs:64-65` hardcodes `admin@climasite.local` / `Admin123!`, with `EmailConfirmed = true` at `DataSeeder.cs:74`; `Dockerfile.api:37` sets `ENV ASPNETCORE_ENVIRONMENT=Production`, so the production container executes this on first boot. The GitHub repo (`byrzohod/climasite`) is **public**, so the credential is openly published (CWE-798).
- **Why it matters:** Anyone reading the repo gets full Admin on a deployed store (orders, customer PII, prices). Demo HVAC products also pollute the live catalog. **Not tracked by any Plan 18 task** (Phase 5 covers JWT/Stripe secrets but not this; PROD-102 only covers migrate-at-startup). The old validation summary rated it "SEC-007, Low" — that rating is superseded.
- **Fix:** Keep role seeding always-on; gate admin/product/promotion seeding to Development/Testing via `IWebHostEnvironment`. Bootstrap the production admin from required env vars (`ADMIN_EMAIL` / `ADMIN_INITIAL_PASSWORD`) with forced rotation, or a one-time CLI command. Fail startup in Production if seed credentials would be used. Coordinate with the migrations-at-startup fix (same code path, `DataSeeder.cs:31`).
- **Acceptance criteria:** Starting the API with `ASPNETCORE_ENVIRONMENT=Production` against an empty DB creates no `admin@climasite.local` user and no demo products; login with `Admin123!` fails in prod; an integration test asserts seeding is env-gated; an explicit SEC task is added to Plan 18 Phase 5.
- **Needs confirmation:** Whether anything is deployed to Railway today. If a production DB was ever seeded, **rotate/delete `admin@climasite.local` immediately** — this becomes an active incident, not a latent finding.

#### SR-02 — Payment amount is client-supplied, charged in BGN against EUR orders, and excludes shipping
- **Severity:** Critical | **Priority:** P0 | **Verification:** confirmed (bugs review, two verifier passes)
- **Evidence:** `src/ClimaSite.Api/Controllers/PaymentsController.cs:42-58` charges `request.Amount` with currency defaulting to `"bgn"`, no server-side validation against any cart/order. Frontend passes the client-computed cart total and hardcodes `'bgn'` (`src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts:1147-1150`). The cart total excludes shipping (`cart.service.ts:31`), while the order adds shipping server-side and is hardcoded to EUR (`CreateOrderCommand.cs:163,190-197`). `StripePaymentService.cs:35` truncates with `(long)(amount * 100)`.
- **Why it matters:** (1) Any authenticated user can create and confirm an intent for €0.01 and place the order — classic price manipulation; (2) honest users underpay ~49% (BGN pegged 1.95583/EUR); (3) shipping is never charged. The webhook performs **no amount/currency check** before marking orders Paid.
- **Fix:** Compute the charge server-side from the server-calculated order total (subtotal + tax + shipping) in the store currency — never from a client-supplied amount. Create the order (Pending) first and derive the intent from it, or recompute the cart total in create-intent and verify amount/currency in the webhook handler. Decide store currency (EUR vs BGN) first — a prerequisite open question in `_review/bugs.md`.
- **Acceptance criteria:** POST `/api/payments/create-intent` with an arbitrary amount is rejected or ignored; charge equals server order total including shipping in the configured currency; webhook rejects intents whose amount/currency mismatch the order.

#### SR-03 — Stripe payments never reconciled to orders: `paymentIntentId` silently dropped, webhook can never match
- **Severity:** Critical | **Priority:** P0 | **Verification:** confirmed (bugs review, two verifier passes)
- **Evidence:** Frontend sends `paymentIntentId` on order creation (`checkout.service.ts:87-107`), but `CreateOrderCommand` (`src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs:12-21`) has no such field — System.Text.Json silently drops it. `Order.SetPaymentInfo` is never called; `HandleStripeWebhookCommand.cs:39-49` matches on an always-null `PaymentIntentId` and acknowledges with 200 ("no matching order"), so Stripe never retries.
- **Why it matters (security angle):** The webhook is the only server-side payment-integrity checkpoint; with reconciliation dead, refund and payment-failure events are no-ops and a tampered/underpaid intent (SR-02) is never caught. 100% of card orders are charged-but-stuck-Pending. Full functional detail in `_review/bugs.md` finding 1.
- **Fix:** Add `PaymentIntentId` (and `PaymentMethod`) to `CreateOrderCommand`, persist via `order.SetPaymentInfo(...)`, verify the intent's amount/currency/status against the order before accepting, and add a unique index on `PaymentIntentId` as an idempotency key. Fix together with SR-02. Also change `WebhooksController.cs:91-93` to not return 200 for unmatched events (or store them) so Stripe retries.
- **Acceptance criteria:** Integration test: create order with `paymentIntentId` → persisted; simulated `payment_intent.succeeded` transitions the order to Paid; mismatched amount/currency is rejected.

### High

#### SR-04 — IDOR / PII disclosure: anonymous `GET /api/orders/by-number/{orderNumber}` skips ownership check
- **Severity:** High | **Priority:** P1 | **Verification:** confirmed end-to-end
- **Evidence:** `src/ClimaSite.Api/Controllers/OrdersController.cs:98-108` — no `[Authorize]`, no class-level auth, no FallbackPolicy in `Program.cs:84-88`. Handler `GetOrderByNumberQuery.cs:43` only guards `if (userId.HasValue && ...)` — anonymous callers bypass the check and receive full `OrderDto` (CustomerEmail, CustomerPhone, full ShippingAddress, items — `GetOrderByNumberQuery.cs:82-118`). `GetOrderByIdQuery.cs:44-46` rejects anonymous callers, proving the asymmetry. Order numbers are predictable: `ORD-yyyyMMdd-HHmmss-XXXX` with only 4 hex chars of randomness (`CreateOrderCommand.cs:221-226`); a single IP can exhaust the 65,536-suffix space for a known timestamp in ~11 hours under the global limiter.
- **Why it matters:** OWASP A01 Broken Access Control; GDPR-relevant personal-data disclosure to unauthenticated callers.
- **Fix:** In `GetOrderByNumberQueryHandler`, reject when `!userId.HasValue && !IsAdmin` (mirror `GetOrderByIdQuery`), OR require a per-order opaque confirmation token for guest lookups. Never return PII anonymously. **Not tracked by Plan 18 — add a task.**
- **Acceptance criteria:** Anonymous request returns 401/404 with no PII; users see only their own orders; admins see all; integration test asserts anonymous and cross-user access denied.
- **Open question (needs confirmation):** Is this endpoint used by a guest order-confirmation page? If yes, design the guest token before removing access.

#### SR-05 — Password-reset token logged in plaintext; reset email never sent
- **Severity:** High | **Priority:** P1 | **Verification:** confirmed
- **Evidence:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs:37` logs the raw token at Information level (`"...Token: {Token}"`); line 40 has the email send commented out. `EmailService.SendPasswordResetEmailAsync` (`EmailService.cs:51-57`) is implemented, registered (`Infrastructure/DependencyInjection.cs:69`), and has zero callers. Serilog writes to Console at Information (`Program.cs:18-22`). The frontend (`forgot-password.component.ts:82-89`) shows success either way.
- **Why it matters:** Anyone with log access (Railway console, aggregation) can take over any account that requested a reset; simultaneously the user-facing reset flow is silently dead (permanent lockout for users who forget passwords) while docs claim Authentication "Complete".
- **Fix:** Remove the token from the log line (log user id only); inject `IEmailService` and call `SendPasswordResetEmailAsync` with the reset link.
- **Acceptance criteria:** No reset token in logs (unit test asserts); forgot-password delivers a working email (MailHog in dev); E2E covers the full reset flow.

#### SR-06 — No `UseForwardedHeaders`: per-IP rate limiting collapses to one shared bucket behind nginx/Railway
- **Severity:** High | **Priority:** P1 | **Verification:** confirmed (devops review verified zero forwarded-headers handling anywhere; security review rated the auth-limiter aspect P2 — the devops P1 calibration is adopted here because the availability impact is site-wide)
- **Evidence:** All rate-limit partitions key on `context.Connection.RemoteIpAddress` (`Program.cs:210, 222, 235` — global 100/min, auth 10/min, strict 5/min). No `UseForwardedHeaders`/`KnownProxies` anywhere in code, Dockerfiles, or configs; `ASPNETCORE_FORWARDEDHEADERS_ENABLED` also absent. `environment.prod.ts` uses a relative apiUrl, so all browser traffic goes through the nginx proxy (`nginx.conf.template:30-46` sets X-Forwarded-For, which ASP.NET ignores). `AuthController.GetIpAddress()` (`AuthController.cs:176-181`) reads X-Forwarded-For manually (spoofable) for audit only — the limiter does not.
- **Why it matters:** In production, ~100 total req/min across ALL customers triggers site-wide 429s; the 10/min auth bucket is shared so brute-force isolation is meaningless and one attacker can lock out all logins (DoS). Logged client IPs are also wrong, hampering forensics. OWASP A07.
- **Fix:** Add `ForwardedHeadersOptions` (XForwardedFor | XForwardedProto, KnownNetworks/KnownProxies for the Railway/nginx hop) and `app.UseForwardedHeaders()` before rate limiting and HTTPS redirection. Complements Plan 18 SEC-104 (rate-limit extension) — SEC-104 is pointless until this lands. **Forwarded-headers itself is not tracked by Plan 18 — add it.**
- **Acceptance criteria:** Two distinct client IPs behind the proxy get independent buckets (test with distinct X-Forwarded-For values against `/api/auth/login`); logged request IPs match the real client.

#### SR-07 — Card charged before order exists; no compensation on failure; double-submit window; no idempotency key
- **Severity:** High | **Priority:** P1 | **Verification:** confirmed
- **Evidence:** `checkout.component.ts:1174-1177` confirms the Stripe payment, then creates the order (`:1185`); on order failure only an error message is set (`:1199-1201`) — `CancelPaymentIntentAsync` exists server-side but is never called from the frontend. The processing guard is set only inside `createOrder()` (`checkout.service.ts:88`), after the multi-second confirm phase, so double-clicks can charge twice. No idempotency key anywhere in the order pipeline.
- **Why it matters:** Customer money is captured with no order created (and per SR-03, no persisted intent ID to find the orphaned charge); double-charge is possible. Money-handling integrity issue.
- **Fix:** Set the processing flag at the top of `placeOrder()`; create the order (Pending) before confirming payment, or auto-cancel/refund the intent on `createOrder` failure; add an idempotency key (unique index on `PaymentIntentId`). Depends on SR-02/SR-03.
- **Acceptance criteria:** Rapid double-click produces exactly one intent and one order; forced order failure after payment leaves no uncancelled captured intent.

### Medium

#### SR-08 — TestController (DB wipe, admin elevation) compiled into the production image; cleanup gated only by environment-name string
- **Severity:** High if env misconfigured, otherwise latent | **Priority:** P2 | **Verification:** verified; verifier downgraded P1→P2 because `Dockerfile.api:37` hardcodes `ASPNETCORE_ENVIRONMENT=Production`, so `/api/test/*` 404s in prod today
- **Evidence:** `src/ClimaSite.Api/Controllers/TestController.cs` has no `#if DEBUG` or conditional registration. `DELETE /api/test/cleanup/{correlationId}` (`TestController.cs:98-201`) mass-deletes data guarded ONLY by `IsDevelopment() || IsEnvironment("Testing")` (`:34-35`), no secret. `POST /api/test/elevate-admin` (`:40-90`) defaults to the hardcoded secret `"test-admin-secret"` in Development (`:56`), the same value used in CI (`.github/workflows/test.yml`).
- **Fix:** Exclude TestController from Release builds (`#if DEBUG`) or register conditionally at startup; require a strong, non-default secret for ALL test endpoints including cleanup. **Not tracked by Plan 18.**
- **Acceptance criteria:** Release/Production build exposes no `/api/test/*` route; cleanup/seed require a configured strong secret; no hardcoded default secret in source.

#### SR-09 — Weak default JWT secret committed; strong-secret requirement only enforced for the literal "Production" environment
- **Severity:** Medium | **Priority:** P2 | **Verification:** reviewer-verified
- **Evidence:** `src/ClimaSite.Api/appsettings.json:21-22` ships `"Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"`. `JwtConfiguration.ResolveSecret` (`JwtConfiguration.cs:21-24`) only throws when `environment.IsProduction()` and no env secret — any other name (Staging, QA, typo'd "production") silently signs tokens with the public key, enabling full JWT forgery / privilege escalation (OWASP A02). Both validation (`Program.cs:79`) and issuance (`TokenService.cs:24-26`) use the fallback.
- **Fix:** Remove the secret from committed `appsettings.json` (Plan 18 **SEC-100** tracks this — but extend it); require `JWT_SECRET` in all non-Development environments; fail fast if the configured secret equals the known placeholder.
- **Acceptance criteria:** App refuses to start in any non-Development env without an external `JWT_SECRET`; committed config contains no usable signing key.

#### SR-10 — Swagger UI and OpenAPI JSON exposed unconditionally in production
- **Severity:** Medium | **Priority:** P2 | **Verification:** reviewer-verified
- **Evidence:** `Program.cs:271-277` calls `UseSwagger`/`UseSwaggerUI` outside the `IsDevelopment()` block (which ends at line 265). OWASP A05.
- **Fix:** Tracked as Plan 18 **SEC-102** (dev-only, or `Features:SwaggerInProd=true` flag with basic auth). Implement as specified there.
- **Acceptance criteria:** `/swagger` and `/swagger/v1/swagger.json` return 404 (or require auth) in Production, remain available in Development.

#### SR-11 — Secret-injection conventions inconsistent: Stripe/SMTP/MinIO read config-section keys while docs promise flat env vars; dummy keys defeat fail-fast
- **Severity:** Medium | **Priority:** P2 | **Verification:** reviewer-verified (devops review)
- **Evidence:** JWT/DB/Redis read flat env vars (`JwtConfiguration.cs:13`, `DependencyInjection.cs:21,54`), but `StripePaymentService.cs:16` reads only `Stripe:SecretKey`, `WebhooksController.cs:51` `Stripe:WebhookSecret`, `EmailService.cs:83` `Email:SmtpHost`; MinIO defaults to localhost:9000 with hardcoded creds `climasite`/`climasite_minio_secret` (`DependencyInjection.cs:74-81`, also `MinioStorageService.cs:11-15`). CLAUDE.md documents flat names (`STRIPE_SECRET_KEY`, `SMTP_HOST`) that .NET will NOT map to section keys (needs `Stripe__SecretKey`). `appsettings.json:16-20` ships non-empty dummy Stripe keys, so the fail-fast at `StripePaymentService.cs:18-21` never fires — a deploy configured per the docs silently runs with dummy keys until the first checkout fails.
- **Fix:** Standardize (read flat names in code, or document `__` names); remove dummy Stripe keys from `appsettings.json` (Plan 18 SEC-100 covers the removal); add a Production startup check that fails when Stripe/SMTP/MinIO config is placeholder/missing.
- **Acceptance criteria:** Booting Production without real Stripe config throws at startup; the documented env-var table matches names proven by startup validation.

#### SR-12 — No security-headers middleware; CORS uses `.AllowAnyHeader()`
- **Severity:** Medium | **Priority:** P2 | **Verification:** confirmed by status review's Plan 18 Phase 5 code check (no CSP/X-Frame-Options/nosniff middleware exists; CORS `.AllowAnyHeader()` at `Program.cs:~103`; HSTS enabled non-dev with default max-age at `Program.cs:268`; `AllowedHosts: "*"`)
- **Fix:** Already specified in Plan 18 **SEC-101** (CSP compatible with Stripe, X-Frame-Options DENY, nosniff, Referrer-Policy, Permissions-Policy + integration test) and **SEC-103** (explicit CORS header allowlist). Implement as written; also set explicit `AllowedHosts` and tune HSTS (max-age, includeSubDomains).
- **Acceptance criteria:** Integration test asserts the headers on every response; CORS rejects non-allowlisted headers; `AllowedHosts` is explicit.

#### SR-13 — Docker hardening gaps: containers run as root; web container silently defaults API_URL to localhost
- **Severity:** Medium | **Priority:** P2 | **Verification:** reviewer-verified (devops review)
- **Evidence:** No `USER` directive in `Dockerfile.api`, `Dockerfile.web`, or `src/ClimaSite.Web/Dockerfile`. `src/ClimaSite.Web/docker-entrypoint.sh:8` defaults `API_URL` to `http://localhost:8080`, so a misconfigured web service passes health checks (`nginx.conf.template:61-65`) while every `/api` call fails.
- **Fix:** Non-root users in final stages (nginx-unprivileged for web); fail hard in entrypoint when `API_URL` is unset outside local dev. Tracked as Plan 18 **PROD-100**.
- **Acceptance criteria:** PID 1 is non-root in both images; web image without `API_URL` exits with a clear error.

#### SR-14 — GDPR rights not user-exercisable: export/delete endpoints have no frontend consumer
- **Severity:** Medium | **Priority:** P2 | **Verification:** reviewer-verified (status review)
- **Evidence:** `src/ClimaSite.Api/Controllers/GdprController.cs:28,53,85,98` implements export/delete/data-categories/rights with real handlers, but grep across `src/ClimaSite.Web/src/app` finds zero consumers; `account.routes.ts` has only profile/orders/addresses. Note: `docs/validation/00-validation-summary.md` SEC-003 ("No GDPR endpoints") is stale in the opposite direction — the backend exists.
- **Fix:** Add a privacy section to `/account` (export my data, delete my account) calling the existing endpoints, with confirmation UX and i18n; update the validation summary to "backend done, frontend missing".
- **Acceptance criteria:** Authenticated user can export and request deletion from the UI; E2E covers deletion.

#### SR-15 — `main` branch unprotected while CLAUDE.md mandates direct push to main
- **Severity:** Medium (change-control / supply-chain) | **Priority:** P1 per devops review | **Verification:** confirmed live (`gh api .../branches/main/protection` → 404; Rulesets empty; a red CI run has already landed on main)
- **Fix:** Enable branch protection (require the five Test Suite checks, require PRs, forbid force-push); update CLAUDE.md step 4 to branch→PR→merge-on-green. Must land before any CD wiring to main.
- **Acceptance criteria:** Direct push to main rejected by GitHub; CLAUDE.md and AGENTS.md describe the same PR flow.

#### SR-16 — Active login/register commands have no FluentValidation; duplicate dead handler set exists
- **Severity:** Low | **Priority:** P2 | **Verification:** reviewer-verified
- **Evidence:** `AuthController` dispatches `ClimaSite.Application.Auth.Commands.*` which have no validators; a parallel validator-bearing set at `Features/Auth/Commands` (`LoginCommand.cs:14`) is unused dead code targeting different command types. `ValidationBehavior` only runs when a validator exists (`Application/DependencyInjection.cs:21,26`).
- **Fix:** Delete the unused `Features/Auth` duplicates (or consolidate onto them); add validators (email format/length, name length) for the commands actually dispatched.
- **Acceptance criteria:** Every auth command dispatched by `AuthController` has a registered validator; no duplicate handlers for the same command remain.

### Low

#### SR-17 — Refresh tokens stored in plaintext in the database
- **Severity:** Low | **Priority:** P3 | **Verification:** reviewer-verified
- **Evidence:** `ApplicationUser.RefreshToken` is a plaintext column (`ApplicationUser.cs:13,38-43`); `RefreshTokenCommandHandler.cs:35-36` compares by equality. Generation itself is strong (64 RNG bytes).
- **Fix:** Store SHA-256 hashes and compare hashes on refresh; optionally a `RefreshTokens` table for per-device tokens/revocation.
- **Acceptance criteria:** DB stores only hashed tokens; refresh still works; a DB dump reveals no usable tokens.

#### SR-18 — Exception middleware echoes raw `ArgumentException` messages to clients
- **Severity:** Low | **Priority:** P3 | **Verification:** reviewer-verified
- **Evidence:** `ExceptionHandlingMiddleware.cs:38,47-57` puts raw `arg.Message` into the response `detail`. No stack traces leak; unexpected exceptions correctly return generic messages.
- **Fix:** Generic 400 for `ArgumentException`; curated text only for explicit ValidationException; adopt RFC 7807 ProblemDetails (TODO already at `Program.cs:108`).

#### SR-19 — Public shared-wishlist response leaks owner `UserId`
- **Severity:** Low | **Priority:** P3 | **Verification:** reviewer-verified
- **Evidence:** `WishlistController.cs:28-34` (`[AllowAnonymous]` `shared/{shareToken}`) returns `WishlistDto` including `UserId` (`WishlistDto.cs:6`, populated at `WishlistApplicationService.cs:129`). Share-token entropy is strong (122-bit GUID base64url, `Wishlist.cs:36-41`); only `IsPublic` lists are returned. Only the global 100/min limiter applies.
- **Fix:** Omit `UserId` from the anonymous share DTO; consider a named rate-limit policy on the endpoint. Note: this code is part of the **uncommitted** wishlist slice (see `_review/status.md` finding 4) — cheapest to fix before the branch is committed/merged.

#### SR-20 — Email confirmation not required or enforced
- **Severity:** Low | **Priority:** P3 | **Verification:** reviewer-verified
- **Evidence:** `Infrastructure/DependencyInjection.cs:46` sets `RequireConfirmedEmail = false`; `LoginCommandHandler.cs:33-91` never checks `EmailConfirmed`. The confirm endpoint exists (`AuthController.cs:101-109`) but is optional.
- **Fix:** Make the policy deliberate: enforce confirmation (gate sensitive actions) or document it as intentionally optional.

---

## Areas requiring manual penetration testing

From `_review/security.md` (with additions from bugs/devops reviews). None of these can be closed by code reading alone.

1. **Guest-cart access model** — `GetCart`/`UpdateCartItem`/`RemoveFromCart` accept a client-supplied `guestSessionId` (`CartController.cs:29,57-92`), and session IDs are client-generated (`cart.service.ts:47`). Test guessability/enumeration, cross-guest cart manipulation, and whether an authenticated user's cart is reachable via `guestSessionId`. Also: `MergeGuestCartCommand` lets any authenticated user merge any guessable `guestSessionId`.
2. **Payment amount reconciliation & webhook idempotency/replay** — can a client pay less than owed (SR-02)? Does `HandleStripeWebhookCommand` survive duplicate events (Stripe retries)? *Needs confirmation — not verified at the webhook layer.*
3. **JWT forgery against a non-"Production"-named deployed environment** — verify the committed placeholder key (SR-09) is not in use.
4. **Rate-limit effectiveness behind the actual Railway/nginx topology** — single-bucket / mass-lockout behavior (SR-06).
5. **TestController reachability in the deployed environment** — confirm `/api/test/*` returns 404 in prod (SR-08).
6. **Brute-force / credential-stuffing against `/api/auth/login`** given the proxy IP issue; verify per-account lockout actually triggers.
7. **Order-number enumeration** feasibility for the IDOR endpoint (SR-04): timing, format, and broader PII surface.
8. **Authorization matrix sweep** — attempt cross-user access to every `{id:guid}` resource endpoint (addresses, orders, notifications, review edit/delete) using a second user's IDs.

---

## Production-readiness security checklist

Gate for any production deploy. Merges the checklist from `_review/security.md` with devops/bugs security items. Plan 18 task IDs cited where tracked.

**Critical (block deploy):**
- [ ] Gate `DataSeeder` admin/demo seeding to Development/Testing; bootstrap prod admin from env vars (SR-01 — *add to Plan 18*)
- [ ] If anything is already deployed: rotate/delete `admin@climasite.local` and audit for misuse (SR-01)
- [ ] Server-side payment amount/currency incl. shipping; reject client-supplied amounts (SR-02)
- [ ] Persist `PaymentIntentId` on orders; webhook verifies amount/currency; non-200 on unmatched events (SR-03)

**High:**
- [ ] Fix order-by-number IDOR; add anonymous + cross-user access tests (SR-04 — *add to Plan 18*)
- [ ] Remove reset-token logging; wire the reset email (SR-05)
- [ ] Add `UseForwardedHeaders`; verify per-client rate limiting behind Railway/nginx (SR-06 — *add to Plan 18*)
- [ ] Order-before-charge or auto-cancel intent on order failure; double-submit guard; idempotency key (SR-07)
- [ ] Enable branch protection on `main`; fix CLAUDE.md push-to-main instruction (SR-15)

**Medium:**
- [ ] Exclude TestController from Release builds; strong non-default secret on all test endpoints (SR-08)
- [ ] Remove committed JWT secret; require `JWT_SECRET` in all non-dev envs; fail on placeholder (SR-09, Plan 18 SEC-100)
- [ ] Gate Swagger to non-production or behind auth (SR-10, SEC-102)
- [ ] Standardize secret env-var names; remove dummy Stripe keys; Production fail-fast for Stripe/SMTP/MinIO (SR-11, SEC-100)
- [ ] Security headers middleware + integration test (SR-12, SEC-101); tighten CORS header allowlist (SEC-103)
- [ ] Explicit `AllowedHosts` (not `"*"`); confirm `AllowedOrigins` is the real prod origin; tune HSTS; verify HTTPS redirection behind the proxy (SR-12)
- [ ] Extend rate limiting to payments/password-reset/admin writes/webhooks (SEC-104)
- [ ] Non-root containers; web entrypoint fails on unset `API_URL` (SR-13, PROD-100)
- [ ] GDPR export/delete reachable from the account UI (SR-14)
- [ ] Verify MinIO/SMTP/Stripe/DB/Redis secrets injected via env in prod (not committed defaults) (SR-11)
- [ ] Stop running migrations + seeding at app startup in prod; dedicated pre-deploy migration step (PROD-102; see `_review/devops.md` finding 3)
- [ ] Correlation IDs + error tracking so security incidents are detectable (see `_review/devops.md` finding 5)

**Scheduled hardening:**
- [ ] Hash refresh tokens at rest; consider per-device tokens + revocation (SR-17)
- [ ] Generic messages for `ArgumentException`; RFC 7807 ProblemDetails (SR-18)
- [ ] Remove `UserId` from anonymous shared-wishlist DTO (SR-19 — fix in the uncommitted wishlist branch before merge)
- [ ] Decide and enforce email-verification policy (SR-20)
- [ ] Add validators for live auth commands; delete dead duplicate handlers (SR-16)
- [ ] Run Plan 18 SEC-105 (`/security-review` OWASP walkthrough) and SEC-107 (security ADR) after the above land

---

## Dependency risk status

**Current status: CLEAN (verified 2026-06-11).**

- `dotnet list ClimaSite.sln package --vulnerable --include-transitive` → no vulnerable packages across all 8 projects (Core, Application, Infrastructure, Api, test projects).
- `npm audit --omit=dev` in `src/ClimaSite.Web` → 0 vulnerabilities (all severities).

**Gaps / follow-ups:**

| Item | Status | Action |
|---|---|---|
| Automated scanning in CI | **Missing** — `.github/workflows/test.yml` has no audit step, no `dependabot.yml`, no CodeQL/Trivy (`_review/devops.md` finding 9) | Add `npm audit --audit-level=high` + `dotnet list package --vulnerable` jobs failing on high/critical; add dependabot.yml. Tracked partially as Plan 18 SEC-106 (one-shot audit) — make it continuous. |
| Docker image scanning | Missing (images never built in CI) | Add docker-build job, optionally Trivy scan |
| MinIO fallback credentials | Hardcoded dev defaults `climasite`/`climasite_minio_secret` (`MinioStorageService.cs:11-15`, `DependencyInjection.cs:74-81`) | Covered by SR-11 fail-fast |
| Committed dev secrets | JWT placeholder + dummy Stripe test keys + dev DB password in `appsettings.json`; dev-only creds in `docker-compose.yml`; CI test secrets in `test.yml` (the CI `test-admin-secret` matches TestController's accepted value — relevant to SR-08). No real production secrets found in the repo. | SEC-100 + SR-08/SR-09/SR-11 |

Re-run both scans before each release; the clean result is a point-in-time baseline, not a guarantee.
