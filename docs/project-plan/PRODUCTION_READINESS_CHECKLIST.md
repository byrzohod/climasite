# ClimaSite — Production Readiness Checklist

> ⚠️ STALE SNAPSHOT (2026-06-11). Most Section-0 P0 launch blockers listed below as open are FIXED — Stripe money path (BUG-01/02/18), admin CRUD (GAP-01/02), env-gated DataSeeder + removed committed JWT secret (SEC-01/SEC-05), forwarded-headers/rate-limiter (SEC-03), guest cart-merge (BUG-03), inventory reservations (INV-01, #98–#102). See `PROJECT_STATUS.md` + `CHANGELOG.md` for current status. Remaining TRUE launch blockers: OPS-08 deploy, OPS-05 observability, SEC-11 pen-test.

**Date:** 2026-06-11
**Sources:** Verified review findings in `docs/project-plan/_review/` (status, product, uiux, security, architecture, bugs, testing, performance, docs, devops). Companion documents: `docs/project-plan/PRIORITIZED_BACKLOG.md` (task-level backlog), `SECURITY_REVIEW.md`, `TESTING_STRATEGY.md`, `BUGS_AND_TECH_DEBT.md`, `DEV_WORKFLOW.md`, `DECISIONS.md` (owner decisions referenced below). This checklist supersedes the inline security checklist in `_review/security.md` and complements (does not replace) `docs/plans/18-project-completion.md` Phase 5 (SEC-100..107) and Phase 7 (PROD-100..107) — Plan 18's open items verified accurate against code; its task IDs are cited where they overlap. Note that Plan 18 does NOT track the production seeding blocker (#4 below) — it is tracked here and in the backlog only.

**How to use this document:** This is the gate for launching ClimaSite to production. Every item is a concrete, verifiable condition with current status — `[x]` verified done, `[~]` partially done, `[ ]` open, tagged with its backlog priority (P0 = launch blocker/critical, P1 = must-fix before launch, P2 = should-fix, P3 = polish). Work section 0 (launch blockers) first, then the sections in order; do not check an item without meeting its cited acceptance condition. File paths and task IDs point at the exact code/config to change. Items marked "Needs confirmation" require the owner to verify something outside the repo (Railway dashboard, Stripe account) before they can be planned. When an item is fixed, check it here AND update the corresponding entry in `docs/project-plan/BUGS_AND_TECH_DEBT.md`.

---

## 0. Launch blockers at a glance

The shop **cannot launch** until all of these are closed. Details in the cited sections below.

- [ ] **P0 — Stripe payments never linked to orders; every order stays Pending forever.** `CreateOrderCommand` has no `PaymentIntentId`/`PaymentMethod` fields (the frontend's values are silently dropped by model binding), `Order.SetPaymentInfo` has zero callers, and `HandleStripeWebhookCommand.cs:39-49` matches on an always-null column — webhooks 200-ack and Stripe never retries, so payment events are permanently lost. Fix: persist the intent on the order, verify it server-side via the Stripe API. (`_review/bugs.md` #1, `_review/product.md` #1)
- [ ] **P0 — Charged amount ≠ recorded total ≠ displayed total.** Stripe is charged the client-supplied cart total (no shipping) in **BGN** (`checkout.component.ts:1147-1150`, `PaymentsController.cs:42-58` validates only `Amount > 0`); the order records **EUR** + server-side shipping (`CreateOrderCommand.cs:163,190-198`); cart/checkout display **USD**. A client can pay €0.01 for a full-value order. Fix: order-first flow, server-computed amount/currency, one store currency (owner decision — see `DECISIONS.md`). (`_review/bugs.md` #2, `_review/product.md` #3)
- [ ] **P0 — Production startup seeds `admin@climasite.local` / `Admin123!` + demo catalog unconditionally** — and the repo is public, so the credential is world-readable. `Program.cs:32` → `DataSeeder.cs:27-46,64-65`; `Dockerfile.api:37` sets Production. Fix: env-gate seeding; bootstrap prod admin from required env vars. If anything is already deployed, rotate/delete this account immediately. (`_review/devops.md` #1)
- [ ] **P0 — Rate limiter shares one bucket for all users behind the proxy.** No `UseForwardedHeaders` anywhere; all three limiters key on `Connection.RemoteIpAddress` (`Program.cs:208-241`), which behind nginx/Railway is the proxy IP → site-wide 429s at ~5-15 concurrent shoppers, and one user can lock out all logins. Fix: ~10 lines of `ForwardedHeadersOptions` before `UseRateLimiter`. (`_review/performance.md` #1, `_review/devops.md` #4)
- [ ] **P0 — Admin products/orders/users pages are 12-line "Coming Soon" stubs** — nobody can fulfill an order, set tracking, or edit a product through the UI, while complete backend admin APIs sit unused (`features/admin/{products,orders,users}/*.component.ts`; `AdminOrdersController.cs`, `Admin/AdminProductsController.cs`). Orders page first (fulfillment-critical). (`_review/product.md` #2, `_review/status.md` #1)
- [ ] **P1 — Guest cart merge always returns 400; guest carts vanish from the UI on login.** FE posts a body + `X-Session-Id` header (`cart.service.ts:190-205`); BE requires `[FromQuery] string guestSessionId` (`CartController.cs:110-116`); failure swallowed with `console.warn`. One-line FE fix + a UI-level E2E. Deterministic conversion killer, downgraded from P0 only because data is recoverable server-side. (`_review/bugs.md` #3)

---

## 1. Security checklist

Cross-reference: `docs/project-plan/SECURITY_REVIEW.md` for full analysis; Plan 18 Phase 5 (SEC-100..107) tracks part of this scope.

### Access control & data exposure
- [ ] **P0** Gate `DataSeeder`: no admin/demo seeding outside Development/Testing; production admin bootstrapped from required env vars (`ADMIN_EMAIL`/`ADMIN_INITIAL_PASSWORD`) with fail-fast. Acceptance: prod startup against an empty DB creates no `admin@climasite.local`; login with `Admin123!` fails; an integration test asserts seeding is env-gated. (`Program.cs:32`, `DataSeeder.cs:64-65`; launch blocker)
- [ ] **P1** Fix IDOR on `GET /api/orders/by-number/{orderNumber}` — anonymous callers currently get full customer PII (email, phone, address, items) for guessable order numbers (`OrdersController.cs:98-108`; `GetOrderByNumberQuery.cs:43` skips the ownership check when userId is null; order numbers are `ORD-yyyyMMdd-HHmmss-XXXX`, only 4 random hex chars). Mirror the anonymous rejection in `GetOrderByIdQuery.cs:44-46` or require a per-order confirmation token. Add cross-user/anonymous-denial integration tests. (`_review/security.md` #1)
- [ ] **P1** Stop logging the raw password-reset token at Information level and wire the reset email (`ForgotPasswordCommandHandler.cs:34-42` — send call commented out; `EmailService.SendPasswordResetEmailAsync` implemented, zero callers). Anyone with Railway/console log access can mint an account-takeover token today. (`_review/security.md` #2)
- [ ] **P1** Validate the payment-intent amount server-side against the server-computed order total before capture — `PaymentsController.cs:42-45` validates only `Amount > 0`, and the webhook verifies no amounts. Part of launch blocker #2.
- [ ] **P2** Exclude `TestController` (DB wipe via substring match, admin elevation) from Release builds (`#if DEBUG` or conditional registration); require a strong non-default secret on ALL test endpoints — `DELETE /api/test/cleanup/{correlationId}` has no secret, only an env-name gate, and the elevate-admin default secret `"test-admin-secret"` is committed (`TestController.cs:34-35,56,98-201`). Mitigated today only by `ASPNETCORE_ENVIRONMENT=Production` discipline. (`_review/security.md` #3)
- [ ] **P2** Gate Swagger UI/JSON to Development or behind auth — exposed unconditionally in production (`Program.cs:271-277`). Plan 18 SEC-105 scope.
- [ ] **P3** Omit owner `UserId` from the anonymous shared-wishlist response (`WishlistDto.cs:6`, `WishlistApplicationService.cs:129`); apply a named rate-limit policy to `GET /api/wishlist/shared/{shareToken}`. Share-token entropy itself is sound (122-bit GUID).

### Secrets & configuration
- [ ] **P2** Remove the committed placeholder JWT secret from `appsettings.json:21-22`; require `JWT_SECRET` in **all** non-Development environments (currently only the literal "Production" env name triggers the check, `JwtConfiguration.cs:21-24` — a "Staging" deploy would sign tokens with the public key); fail fast on the known placeholder value. (`_review/security.md` #6)
- [ ] **P2** Standardize secret injection: Stripe/SMTP/MinIO read only config-section keys (`Stripe:SecretKey`, `Email:SmtpHost`, `Minio:*`) while CLAUDE.md documents flat env names (`STRIPE_SECRET_KEY`, `SMTP_HOST`) that .NET will NOT map — the real env-var names need the `Stripe__SecretKey` double-underscore form. Remove the non-empty dummy Stripe keys from `appsettings.json:16-20` that defeat the fail-fast null check; add a Production startup check that throws on placeholder/missing Stripe/SMTP/MinIO config. (`_review/devops.md` #10)
- [ ] **P2** Set explicit `AllowedHosts` (not `"*"`) and confirm `AllowedOrigins` contains the real production origin (currently localhost-only, `appsettings.json:28-33`; mitigated by the same-origin `/api` nginx proxy).
- [ ] **P2** Verify MinIO prod credentials are injected via env — `MinioStorageService.cs:11-15` falls back to hardcoded `climasite`/`climasite_minio_secret`.
- [ ] **P3** Tune HSTS for prod (max-age, includeSubDomains; `Program.cs:268` uses defaults).

### Auth hardening
- [ ] **P2** Add FluentValidation validators to the **live** auth commands (`ClimaSite.Application.Auth.Commands.*` — currently none have any); delete the dead duplicate `Features/Auth` tree whose validators never run and whose unit tests cover the dead copy. (`_review/security.md` #7, `_review/architecture.md` #2)
- [ ] **P3** Hash refresh tokens at rest (currently plaintext on `ApplicationUser.RefreshToken`, looked up by equality — `ApplicationUser.cs:13,38-43`); consider per-device tokens + revocation.
- [ ] **P3** Decide and document the email-verification policy: `RequireConfirmedEmail = false` and login never checks `EmailConfirmed` (`Infrastructure/DependencyInjection.cs:46`); the confirm-email endpoint exists but no email is ever sent and no frontend route lands on it (`_review/product.md` #13). Complete it or delete the dead pieces.

### Verified-good baseline (keep it that way)
- [x] JWT validation correctly configured (issuer/audience/lifetime/signing key, zero clock skew, HMAC-SHA256, 15-min access tokens).
- [x] Refresh tokens: 64 crypto-random bytes, rotated on login/refresh, revoked on logout/password-reset.
- [x] Identity lockout (5 attempts / 15 min); login does not reveal user existence; forgot-password returns a generic message.
- [x] Stripe webhook signature verification (`WebhooksController.cs:58-71`).
- [x] All admin controllers carry `[Authorize(Roles="Admin")]`; resource-scoped handlers (orders-by-id, addresses, notifications, wishlist, GDPR) filter by userId.
- [x] No raw SQL anywhere (EF Core parameterized); no user-controlled outbound fetching (SSRF N/A).
- [x] Dependency scans clean: `dotnet list package --vulnerable --include-transitive` and `npm audit --omit=dev` both zero findings (verified 2026-06-11). Add to CI so it stays true (see Testing).
- [ ] **Needs confirmation** Stripe webhook idempotency / duplicate-event handling (Stripe retries) — verify `HandleStripeWebhookCommand` after the linkage fix; note the webhook currently returns 200 on no-match so early-arriving events are permanently lost (`WebhooksController.cs:91-93`).
- [ ] **P2** Run the manual pen-test list in `_review/security.md` ("Areas requiring manual penetration testing": guest-cart session enumeration, amount tampering, JWT forgery on non-Production env names, rate-limit behavior behind the real topology, `/api/test/*` reachability, authz matrix sweep on every `{id:guid}` endpoint) against a staging deploy before launch.

## 2. Testing checklist

Cross-reference: `docs/project-plan/TESTING_STRATEGY.md`; prioritized test backlog in `_review/testing.md` Dimension data.

### Process blockers
- [ ] **P0** Commit and PR the uncommitted wishlist slice — the entire "DONE 2026-06-07" feature (~33 files) exists only in the working tree on a branch with zero commits (HEAD == main); the new API integration tests (7) and E2E tests (6) have no recorded run. One `git checkout .` from loss. Acceptance: green CI on the PR. (`_review/status.md` #4, `_review/testing.md` #11)
- [ ] **P1** Fix CLAUDE.md/AGENTS.md test documentation: the E2E suite is C# xUnit + Microsoft.Playwright run via `dotnet test tests/ClimaSite.E2E` (not `npx playwright test`); root `dotnet test` silently includes the E2E project which needs live servers (API on 5029, ng serve on 4200) — provide a solution filter (e.g. `ClimaSite.NoE2E.slnf`) or `--filter` guidance. The mandated "Full Test Suite" command cannot work as written. (`_review/testing.md` #1)

### Coverage gaps that gate launch
- [ ] **P1** Stripe card-payment path has zero coverage above mocked unit tests: add (a) integration tests for `PaymentsController` create-intent/cancel and `WebhooksController` signature rejection + `payment_intent.succeeded` → order Paid transition; (b) one E2E completing a card payment via Stripe test card 4242… using FrameLocator (`CheckoutPage.FillPaymentDetailsAsync` already exists as dead code at `CheckoutPage.cs:126`). The only completed-order E2E deliberately uses bank transfer "to avoid Stripe card iframe". (`_review/testing.md` #2)
- [ ] **P1** GDPR export/delete endpoints have **zero tests at any layer** while delete is irreversible and multi-table (`GdprController.cs:28-58`, `DeleteUserDataCommand.cs:88-157`). Add integration tests: export completeness, delete anonymization, idempotency, authz. (`_review/testing.md` #5)
- [ ] **P1** Integration-test wave 1 for money/compliance controllers: Orders, Payments, Webhooks, Gdpr — only 5 of 26 controllers currently have integration coverage; Testcontainers infra already works (`TestWebApplicationFactory.cs`). Wave 2: Reviews, Questions, Notifications, Inventory, Addresses (+ 401/403 cases per endpoint). (`_review/testing.md` #3)
- [ ] **P1** Application unit tests for the 13 untested feature areas, prioritized: Cart (merge/quantity combine), Inventory (reservation/oversell), Reviews (verified-purchase gate), Promotions (price calc), Notifications, Gdpr. Use the existing `MockDbContext` pattern proven by the Wishlist/Orders tests. (`_review/testing.md` #4)
- [ ] **P1** Concurrency test for stock decrement: two parallel CreateOrder calls for the last unit — exactly one succeeds, stock never negative. Backs the oversell fix (`_review/bugs.md` #5; no concurrency token or atomic update exists; the documented "stock reservations" are fictional — `ReservedQuantity` is hardcoded 0).
- [ ] **P2** Cart guest→login merge E2E through the UI (mirror `WishlistTests.cs:137`) — would have caught launch blocker #6; current API tests pass because they call the endpoint with the query param directly.
- [ ] **P2** Frontend specs for the riskiest units, all currently spec-less: `auth.interceptor` (401→refresh→retry/queue/logout — also has a real refresh-storm logout bug, `_review/bugs.md` #8), `auth.guard`, `payment.service`, `checkout.component`, `cart.component`, `register.component`.

### Tooling & CI gates
- [ ] **P2** Coverage measurement + thresholds: the mandated 80% backend / 70% frontend minimums are never collected for Api.Tests or the frontend and never enforced anywhere (codecov upload is `continue-on-error`; no karma.conf.js, no runsettings). Baseline first, then gate. (`_review/testing.md` #6)
- [ ] **P2** CI additions: `ng lint` job (script exists, never run in CI), `dotnet format`/`-warnaserror`, Docker image build check, `npm audit` + `dotnet list package --vulnerable` jobs, `dependabot.yml`, `concurrency:` cancellation, NuGet caching; E2E should exercise the built bundle rather than the dev server; remove the nonexistent `develop` branch from triggers. (`_review/testing.md` #7, `_review/devops.md` #9)
- [ ] **P2** E2E reliability: log cleanup failures (currently `catch { /* ignore */ }` in `TestDataFactory.cs:320-330`), split the single serial xUnit collection into 2-3 to parallelize, investigate the perpetually NotExecuted `Checkout_SavedAddress_CanBeUsed`.
- [ ] **P3** Hygiene: delete placeholder `UnitTest1.cs`; remove the unused Postgres service container from the CI integration job (Testcontainers provisions its own); use-or-delete `FillPaymentDetailsAsync`.

### Verified-good baseline
- [x] CI genuinely gates: backend unit, integration (Testcontainers), frontend (incl. i18n key parity check via `check-i18n.mjs`), Release build, full-stack E2E; latest main run green (~14.5 min).
- [x] ~620 backend/E2E tests + 941 frontend `it()` blocks; latest persisted runs green (E2E 206 passed / 1 NotExecuted).
- [x] E2E design is sound: no mocking, API-backed TestDataFactory, correlation-ID cleanup, axe-core a11y checks, page objects.

## 3. Performance checklist

Full analysis in `_review/performance.md`; its "Quick wins" list is the recommended execution order.

- [ ] **P0** Add `UseForwardedHeaders` (XForwardedFor | XForwardedProto, KnownProxies/KnownNetworks for the nginx/Railway hop) **before** `UseRateLimiter` and `UseHttpsRedirection` in `Program.cs`. Acceptance: two clients with different X-Forwarded-For get independent rate-limit buckets through the nginx container. (Launch blocker #4; ~10 lines. Restrict trusted proxies to prevent IP spoofing of the limiter.)
- [ ] **P1** Resolve the dead caching layer: `CachingBehavior` is never registered, so all 14 `ICacheableQuery` caches are inert and Redis serves nothing but health checks (a Redis outage fails `/health` and restarts an app that doesn't use it). Either `cfg.AddOpenBehavior(typeof(CachingBehavior<,>))` in `Application/DependencyInjection.cs` + an invalidation story on product/category mutations, or delete the plumbing and Redis consciously. (`_review/performance.md` #2)
- [ ] **P1** `GetFilterOptionsQuery.cs:57` hydrates the **entire active product table** (incl. Description + jsonb Specifications) per facet request on a hot public endpoint — project only the 4 needed columns; push aggregation into SQL. Worse than it looks: logged-in users bypass the output cache entirely (Authorization header). (`_review/performance.md` #4)
- [ ] **P2** Replace the accidental blanket OutputCache base policy (every anonymous 200 GET cached 5 min in per-instance memory, zero eviction — `EvictByTagAsync` never called; the named "Products"/"Categories" policies are never applied; revoked wishlist share links keep serving from cache) with explicit `[OutputCache(PolicyName=...)]` on intended endpoints + tag eviction on mutations (`Program.cs:245-250`). (`_review/performance.md` #3)
- [ ] **P2** Clamp `pageNumber`/`pageSize` in `PaginatedList.CreateAsync` (backstop: 1 ≤ pageSize ≤ 100) + FluentValidation validators — `?pageSize=100000` currently dumps the catalog with three Includes; negative paging 500s. (`_review/performance.md` #6)
- [ ] **P2** Cache or pre-filter the recommendations handler (`GetRecommendationsQueryHandler.cs:35-39` loads and scores the whole in-stock catalog in C# per homepage wizard call; inputs are low-cardinality and beg for a 5-15 min cache once CachingBehavior is registered).
- [ ] **P2** PERF-100 (Plan 18, open): OnPush adoption ≥70% (currently 10/85 components) and wrap rAF/scroll loops in `NgZone.runOutsideAngular` (AnimationService, confetti, flying-cart, header scroll listener — zero `runOutsideAngular` usage today, so every animation frame triggers app-wide change detection). (`_review/performance.md` #8)
- [ ] **P2** Image pipeline: zero srcset/NgOptimizedImage, no resize/WebP — admin-uploaded originals serve at full resolution to ~300px cards straight from MinIO, no CDN. Generate sized variants at upload in `MinioStorageService` or front MinIO with an image proxy. Currently masked by small seed images. (`_review/performance.md` #9)
- [ ] **P2** PERF/A11Y-104 (Plan 18, open): Lighthouse ≥90 on 5 routes (only `/` has ever been measured: mobile 0.97 / desktop 1.00 — plausible and consistent with the current build).
- [ ] **P2** SSR/prerender decision for a public shop (no @angular/ssr, `prerendered-routes.json` empty; crawlers/link-unfurlers see generic meta for every product URL). **Owner decision required** — record in `docs/adr/` either way; at minimum add per-route Title/Meta now. (`_review/performance.md` #10)
- [ ] **P2** Replace the fixed 3.2s header/footer `@defer` timer with `on idle` — no search, cart badge, or language/theme switch for the first 3.2 s of every cold load (`main-layout.component.ts`; `_review/uiux.md` #6).
- [ ] **P3** Router preloading (`withPreloading(PreloadAllModules)`); batch the recursive category-descendant N+1 (one query per tree node, duplicated in `SearchProductsQuery.cs:150-163` and `GetFilterOptionsQuery.cs:106-119`); collapse the 13 sequential admin-dashboard KPI queries; rewrite brief-DTO list/search handlers as SQL projections (pattern exists in `GetUserOrdersQuery.cs:88-103`).
- [ ] **P3** Search is naive multi-ILIKE over ~10 text columns, not the "full-text search" CLAUDE.md claims — no index can serve it; fine at seed-catalog size, linear degradation after. Pick Postgres FTS / pg_trgm / Meilisearch when the catalog grows; fix the docs claim now. (`_review/performance.md` #5)
- [ ] **Needs confirmation** No RUM exists (web-vitals not installed) — lab scores are the only performance signal; decide whether to add before launch.

### Verified-good baseline
- [x] Initial bundle ~259 KB raw / ~73 KB gzip (budget 650 KB); all 20+ routes lazy; `@defer` below the fold; fonts preconnected/swapped; critical CSS inlined; `@for track` everywhere (zero untracked `*ngFor`); `loading="lazy"` coverage good.
- [x] DB indexing strong (GIN on tags/specifications/variant attributes, b-trees on all hot paths incl. unique `wishlists.share_token`); no pending-migration gap for the wishlist work.

## 4. Monitoring / logging / observability checklist

Today: console-only Serilog, nothing else — incidents would be detected by customers. Plan 18 PROD-103 acknowledges OpenTelemetry as future work. (`_review/devops.md` #5)

- [ ] **P1** Correlation-ID middleware: read/generate `X-Correlation-Id`, push to Serilog `LogContext`, echo on responses. CLAUDE.md documents this header as an existing convention — it is implemented nowhere (grep matches only `TestController`); either implement it or delete the claim.
- [ ] **P1** Error tracker (Sentry is the lowest-effort fit) — **vendor choice is an owner decision** per global rules (see `DECISIONS.md`). Acceptance: a thrown test exception appears in the tracker with release/environment tags.
- [ ] **P1** Alerting + uptime monitoring: at minimum Railway healthcheck-based alerts on `/health` plus an external uptime check.
- [ ] **P1** Remove the password-reset token from logs (duplicated from Security #2 — it is also an observability hygiene item: console logs ship to Railway).
- [ ] **P2** Production log shape: JSON console output in Production (no `appsettings.Production.json` exists to override the dev template); add `Log.CloseAndFlush()` in a try/finally in `Program.cs` (logs currently lost on crash); include the correlation ID in `ExceptionHandlingMiddleware` error responses.
- [ ] **P2** OpenTelemetry traces + RED metrics (PROD-103).
- [ ] **P2** Runbooks: `docs/runbooks/` does not exist. Minimum set: payment-webhook failure triage, DB restore, deploy rollback, rate-limit/429 storm. The repo's own `.claude/skills/deploy-checklist/SKILL.md` defines the bar (correlation IDs, RED metrics, error tracker, dashboards, actionable alerts — all currently absent).
- [~] Health endpoints wired (`/health` with Npgsql+Redis checks, `/health/live`, `/health/ready`, Railway healthcheckPath configured) — but Redis gates `/health` while being functionally unused (see Performance caching item), and the web container reports healthy even when `API_URL` is unset and every `/api` call fails (see Deployment).

## 5. Deployment checklist (Railway)

Cross-reference: `docs/project-plan/DEV_WORKFLOW.md`; Plan 18 Phase 7 (PROD-100..107); the partly-stale `docs/validation/areas/18-build-cicd-deployment.md` (re-baselined by `_review/devops.md` #14 — its "no E2E in CI / no ESLint / Application tests missing" claims are obsolete).

### Pipeline & artifacts
- [ ] **P1** Create `.github/workflows/deploy.yml` (PROD-104): build + tag images, run the migration step, deploy to Railway via CLI/API, gated on the green Test Suite. No CD of any kind exists today (`.github/` contains only `test.yml`).
- [~] **P1** Consolidate deployment config — **duplicates DELETED (OPS-03, 2026-06-27):** the dead `src/ClimaSite.Api/Dockerfile`, `src/ClimaSite.Web/Dockerfile`, `src/ClimaSite.Api/railway.toml`, and stale `src/ClimaSite.Web/nginx.conf` are gone; root `Dockerfile.api`/`Dockerfile.web` + `railway.toml`/`railway.api.toml` are the single canonical set. **Remaining:** add `.github/workflows/deploy.yml` + document the service→config mapping (owner-gated — needs the Railway project + `RAILWAY_TOKEN`). (`_review/devops.md` #2)
- [ ] **P1** Enable branch protection on `main` (verified unprotected via GitHub API; 8 of the 10 most recent push-to-main CI runs failed) requiring the five CI checks; fix CLAUDE.md Post-Implementation step 4 which still mandates direct `git push origin main`. Must land **before** CD wiring. (`_review/devops.md` #6)
- [ ] **P1** Cut a first annotated tag (v0.x) and wire image tagging to git tags — `git tag` is empty and CHANGELOG has only `[Unreleased]`, so there is currently **no rollback target of any kind**. Use the existing `/release` skill. (`_review/devops.md` #12)

### Migrations on deploy & rollback
- [ ] **P1** Stop `MigrateAsync()` at startup in production (PROD-102; `DataSeeder.cs:31` runs on every boot, crash-loops up to 3× on a failed migration per railway.toml restart policy). Move to a `scripts/migrate.sh` / EF migration-bundle pre-deploy step in deploy.yml; env-gate startup migration to Development/Testing. (`_review/devops.md` #3)
- [ ] **P2** Test down-migrations in CI (apply all to a scratch Postgres, roll the latest back); adopt expand-contract for destructive changes per the `db-migrate` skill.
- [ ] **P2** Consolidate the split migration folders (`Infrastructure/Migrations/` holds 2 migrations + the only snapshot; `Infrastructure/Data/Migrations/` holds the other 6) — pure move + namespace edit, never touch migration IDs. (`_review/architecture.md` #5)
- [ ] **P1** Rollback procedure documented and rehearsed: redeploy previous image tag + tested down-migration if needed; previous artifacts retained. (Blocked on tagging + deploy.yml above.)

### Container & runtime config
- [ ] **P0** Production startup must not seed admin/demo data (launch blocker #3 — same code path as the migration fix; coordinate the two).
- [ ] **P2** PROD-100: non-root users in both containers (no `USER` directive anywhere today); web entrypoint must fail hard when `API_URL` is unset instead of silently defaulting to `http://localhost:8080` while nginx `/health` stays green (`docker-entrypoint.sh:8`) — a one-variable misconfiguration currently ships a fully "healthy" but completely broken storefront. (`_review/devops.md` #11)
- [ ] **P2** Verify env-var resolution for every setting against the matrix in `_review/devops.md` Dimension data §4 — notably: Redis silently falls back to localhost; JWT issuer/audience default to localhost; Stripe/SMTP/MinIO read config-section keys only (need `__` env names); the default DB connection string points at project-local Postgres on 5433.
- [ ] **P1** Verify `ASPNETCORE_ENVIRONMENT` is exactly `Production` in the deployed API service — gates TestController exposure (`/api/test/*` must 404), JWT-secret enforcement, and rate-limiter activation.
- [ ] **P2** Verify HTTPS termination/`X-Forwarded-Proto` behaves correctly behind the proxy once ForwardedHeaders lands (`UseHttpsRedirection` currently never sees the forwarded scheme).
- [ ] **P3** Delete dead `environment.prod.ts` (no angular.json configuration references it; production builds use `environment.production.ts` — flagged once already in the Jan-2026 validation report and still unfixed).
- [ ] **Needs confirmation** Is Railway auto-deploy currently connected, and which services consume which config file? **If anything is already deployed, rotate/delete `admin@climasite.local` immediately** — the credential is published in a public repo.
- [ ] **Needs confirmation** What is production object storage (Railway MinIO service / external S3 / none)? MinIO config defaults to localhost:9000 with hardcoded dev creds.

## 6. Data & backups

- [ ] **Needs confirmation / P1** Database backups: nothing in the repo configures or documents backup/restore. Confirm Railway Postgres snapshots are enabled, document RPO/RTO, and run one restore drill before launch. (`_review/devops.md` Dimension data §2 item 9)
- [ ] **P1** Stock integrity: add a concurrency guard to stock decrement (Postgres xmin token + retry on `DbUpdateConcurrencyException`, or atomic conditional `ExecuteUpdateAsync` with `stock >= qty`) — concurrent checkouts can currently oversell high-value units via classic read-then-write lost updates (`CreateOrderCommand.cs:128-146`; admin `AdjustStockCommand` has the same pattern); documented "stock reservations" do not exist. (`_review/bugs.md` #5)
- [ ] **P2** Wishlist concurrency: the static in-process `SemaphoreSlim` dictionary leaks (never evicts) and silently assumes a single API instance; on scale-out, races hit the DB unique index and surface as unhandled 500s instead of idempotent success. Catch the unique-violation in `AddToWishlistCommand` and return the existing DTO; then the DB constraint is the real guard. Cheapest to fix before the wishlist branch merges. (`_review/bugs.md` #9, `_review/architecture.md` #8)
- [ ] **P2** Decide the Stripe webhook no-match behavior after the linkage fix: returning 200 means Stripe never retries, so an early-arriving webhook (order row not yet committed) is permanently lost — return a retryable status or store unmatched events. (`_review/bugs.md` Dimension data)
- [ ] **P3** Latent hard-delete traps: `order_items` FKs configured `DeleteBehavior.SetNull` on non-nullable Guid columns (violates NOT NULL on any future hard delete); cart mapping uses `.First()` and throws if a product row vanishes (`OrderConfiguration.cs:214-222`, `AddToCartCommand.cs:160`). Make `OrderItem.ProductId/VariantId` nullable; use `FirstOrDefault` with an "unavailable" fallback. Requires a migration.
- [ ] **P3** Guest-cart cleanup job (TODO at `AddToCartCommand.cs:139`) — DB bloat only; expired carts are filtered at read. Blocked on background-job infrastructure (none exists — `_review/architecture.md` #12; mechanism choice is an owner decision).
- [x] GDPR delete implementation is sound (password re-confirmation, anonymization, SecurityStamp rotation) — but untested (see Testing) and unreachable from the UI (see Legal).

## 7. Legal & compliance (EU shop — BG/DE markets)

Primarily from `_review/product.md`; this is a hard launch gate for an EU-targeted store.

- [ ] **P1** Legal/support pages: **all six footer links 404** (`/terms`, `/privacy`, `/cookies`, `/faq`, `/shipping`, `/returns` — `footer.component.ts:43-56` link to routes that fall through to `NotFoundComponent`). Add a lazy `legal` feature with translated pages: terms, privacy policy, cookie policy, returns/withdrawal (EU 14-day right), shipping, FAQ, **and a German Impressum**. Needs real business legal text; placeholder copy acceptable for staging only. Also fill or remove the `href="#"` social placeholders. (`_review/product.md` #6)
- [ ] **P1** Cookie consent banner: none exists. Mitigation: no analytics/tracking scripts are currently present (only functional localStorage), so the missing legal pages are the more acute gap — but consent must block any non-essential storage added later.
- [ ] **P1** Transactional emails: order confirmation (a durable confirmation of contract is effectively required for EU distance selling), shipped notice, welcome, and password reset are all implemented in `EmailService.cs` but have **zero call sites** (the only `IEmailService` consumer in the entire codebase is GDPR delete). Wire confirmation into order creation/Paid webhook, shipped into `UpdateShippingInfoCommand` + `UpdateOrderStatusCommand.cs:73` (the admin "NotifyCustomer" checkbox is a silent no-op). Make sends fire-and-forget/outboxed so email failure doesn't fail the order. Plan 18 NOT-100..111. (`_review/product.md` #5, `_review/architecture.md` #1)
- [ ] **P0** Price-display integrity (consumer law: advertised price must equal charged price): the currency/shipping mess (launch blocker #2), plus — **P1** sale-price inversion sitewide: 14 DTO projections map `SalePrice = CompareAtPrice` (the HIGHER original price), which the UI renders as the deal price across catalog, search, detail, wishlist, and recommendations; the brand page alone maps it the other way (`_review/bugs.md` #6). And **P2** the cart/checkout bare `| currency` pipe rendering USD (`_review/uiux.md` #1; set `DEFAULT_CURRENCY_CODE` or pass the store currency explicitly). Decide store currency (EUR vs BGN) first — owner decision.
- [ ] **P2** VAT correctness: flat 20% on subtotal everywhere; German legal rate is 19%; shipping is added untaxed (EU rules generally require VAT on shipping). Country-based VAT table driven by shipping address (`CreateOrderCommand.cs:200-202`, `AddToCartCommand.cs:185-186`). (`_review/bugs.md` #13)
- [ ] **P2** Invoices: PDF invoice download exists (`GenerateInvoiceQuery.cs`, QuestPDF) — verify VAT lines, seller details, and currency are correct **after** the pricing fixes land.
- [ ] **P2** GDPR self-service UI: backend export/delete/rights endpoints exist (`GdprController.cs:28-98`) but no frontend consumer — users cannot exercise GDPR rights via the UI. Add a privacy section to `/account/profile`. Note `docs/validation/00-validation-summary.md` is stale in the opposite direction (claims the endpoints don't exist).
- [ ] **P1** Contact form fakes success via `setTimeout` and discards every message — including GDPR inquiries and complaints (legal exposure; the page is linked from header, footer, and GdprController directs users to "contact support"). Add `POST /api/contact` → IEmailService. (`contact.component.ts:384-399`; `_review/product.md` #7)
- [ ] **P1** Remove or finish fake payment methods: PayPal and bank-transfer options create orders nobody can pay (stock decremented, no payment step, no instructions), and the payment method is never persisted. Remove PayPal until integrated; for bank transfer persist the method and show IBAN/reference instructions. (`_review/product.md` #8)
- [ ] **P1** Installation requests vanish into a black hole: stored with customer contact data but no list endpoint, no admin view, no business email — customers are promised follow-up the business cannot see. Minimum: email the business per request. (`_review/product.md` #10)
- [ ] **P2** Decide the guest-checkout stance (owner decision): `/checkout` is auth-guarded while the backend supports anonymous orders and CLAUDE.md documents guest checkout as a business rule — enable it end-to-end or remove the dead guest path consistently. (`_review/product.md` #9)
- [ ] **P2** PDP "In Stock" badge is hardcoded `true` for every product — misleading availability advertising; bind to real variant stock and add an out-of-stock CTA state (`product-detail.component.ts:119,1020`). (`_review/product.md` #16)
- [ ] **P3** Footer "GDPR Compliant" badge sits next to a privacy link that 404s — remove or make true.

## 8. Documentation checklist

Full disposition tables in `_review/docs.md`. Companion: `docs/project-plan/DEV_WORKFLOW.md` (current, correct commands).

- [ ] **P1** Fix the three command references (CLAUDE.md, root AGENTS.md, `tests/ClimaSite.E2E/AGENTS.md`): E2E is `dotnet test tests/ClimaSite.E2E` (not `npx playwright test`); API port is **5029** not 5000; the factory is `Infrastructure/TestDataFactory.cs` not `fixtures/test-data-factory.ts`; `tests/ClimaSite.Web.Tests` does not exist (Angular specs are colocated in `src/ClimaSite.Web/src`). Acceptance: every documented command runs on a fresh clone.
- [ ] **P1** Correct the CLAUDE.md status table to match code: Admin Panel → "backend complete, UI stubs except moderation"; Checkout & Orders → "payment linkage broken"; Notifications → "backend in-app CRUD only, no emails wired, no frontend"; Inventory → "backend only, reservations do not exist"; Search → "ILIKE, not full-text"; Performance → "Partial, PERF-100/104 open"; guest checkout → blocked by authGuard; drop the Accept-Language header claim (language travels as `?lang=`).
- [ ] **P1** Fix `docs/plans/00-master-overview.md`: "100% Complete" claims are false; 13 of 16 plan-index links are dead (archived or nonexistent files); Notifications wrongly "Complete"; omits plans 18-22. Demote to historical or repoint at Plan 18 + `docs/project-plan/` as the live source of truth.
- [ ] **P2** Create a root `README.md` (project intro, prerequisites, shared-infra setup per AGENTS.md convention, verified run/test commands, doc map) and `docs/operations/deployment.md` (Railway topology, which service uses which Dockerfile/toml, env vars with the **real** double-underscore names, migration strategy, rollback). Write these AFTER the command/port fixes so they copy correct commands.
- [ ] **P2** Resolve the local-dev contradiction: `appsettings.json` defaults to project-local compose Postgres on 5433 while AGENTS.md and the user's global convention mandate shared-infra on 5432; decide one (convention says shared-infra), fix the default, delete or label `docker-compose.yml`.
- [ ] **P2** Archive the stale January-2026 layer with supersession banners: 17 of 18 `docs/validation/areas/*` docs, `docs/plans/20-issue-registry.md` (claims 129 open / 0 fixed; several CRITICALs verified fixed; some rows target deleted code), and the competing 19/21-series redesign plans — one master plan per number; update Plan 18's supersession header to name them.
- [ ] **P2** ADRs: only 2 exist (both home-v3) vs Plan 18's ≥8 target. Needed: wishlist share-token design (before merge), store currency decision, background-job mechanism, SSR decision, error-tracker vendor, guest-checkout stance — see `DECISIONS.md` for the owner-decision queue.
- [ ] **P3** Add the wishlist slice to `CHANGELOG.md` `[Unreleased]` before merging; reconcile Plan 12's dead checkboxes (0/163 checked despite shipped code) into an accurate remaining-scope header; fix CLAUDE.md misc (EF Core version, plans index); regenerate stale AGENTS.md snapshots.
- [ ] **P3** Refresh `docs/performance/performance-audit.md` (stale budgets 500kB vs actual 650kB, false "no fetchpriority" claim, resolved lazy-loading table) or mark it superseded by `_review/performance.md`.

## 9. Final pre-launch QA checklist

Run on a production-shaped staging deploy (Railway, both containers, real env vars), after all P0/P1 items above are closed. All items must pass.

### Money path (highest priority)
- [ ] Place a card order with Stripe test card 4242…: displayed total == Stripe charge == `Order.Total`, in one currency, including shipping; order leaves Pending → Paid via webhook; a refund via the Stripe dashboard flips status to Refunded.
- [ ] `POST /api/payments/create-intent` with a tampered amount is rejected or ignored.
- [ ] Rapid double-click on Place Order produces exactly one intent and one order; a forced order-creation failure after charge results in automatic cancel/refund — today payment is captured before the order exists with no compensation (`_review/bugs.md` #4).
- [ ] Two parallel checkouts for the last unit: exactly one succeeds, stock never negative.
- [ ] An on-sale product shows the lower price as the active price on card/detail/cart/wishlist, and the charged amount equals it.

### Operations
- [ ] Admin can log in (with the env-bootstrapped account — `Admin123!` must fail), change an order to Shipped with a tracking number, and the customer sees it in `/account/orders/:id` and receives the shipped email.
- [ ] Admin can create/edit/deactivate a product end-to-end through the UI.
- [ ] Order confirmation email arrives on purchase; password-reset email arrives and the link completes the reset (MailHog on staging; real SMTP verified once in prod).
- [ ] Installation request and contact-form submission reach the business (email or admin queue).

### Customer flows
- [ ] Guest adds items to cart, logs in mid-shopping → cart items survive (merge fix verified through the UI, not just the API).
- [ ] Guest-checkout stance implemented per the owner decision (enabled end-to-end or consistently removed from backend + docs).
- [ ] Wishlist share link works; a revoked link stops working within the cache TTL; share/clear failures show a toast instead of failing silently (`_review/uiux.md` #4).
- [ ] With an expired access token and several API calls in flight, the session refreshes once and survives — no spurious logout (refresh-storm bug, `_review/bugs.md` #8).
- [ ] Checkout shipping form shows per-field validation errors in all three languages (`_review/uiux.md` #2).
- [ ] BG/DE users see translated product names in cart and wishlist, not English (`_review/bugs.md` #10).

### Platform
- [ ] Behind the deployed proxy: two distinct client IPs get independent rate-limit buckets; a single client still gets limited; one user cannot lock out all logins.
- [ ] `/api/test/*` returns 404; `/swagger` is gated; security headers present; HSTS active.
- [ ] Every API response carries `X-Correlation-Id`; a forced test exception appears in the error tracker; a downed `/health` fires an alert.
- [ ] Deploy → rollback rehearsal: deploy a tagged release, then roll back to the previous image tag successfully; DB restore drill completed once.
- [ ] Web container with missing `API_URL` fails to start (does not serve a green-but-broken storefront).

### Compliance & quality bar
- [ ] All footer legal links render translated content in EN/BG/DE; cookie consent blocks non-essential storage; Impressum present.
- [ ] A DE-address order computes 19% VAT on goods+shipping; BG computes 20%; the invoice PDF shows correct VAT lines.
- [ ] GDPR export and account deletion work from `/account` and pass their integration tests.
- [ ] Full visual pass in light AND dark themes, EN/BG/DE, mobile/tablet/desktop (Definition of Done in CLAUDE.md).
- [ ] Lighthouse ≥90 (performance + a11y) on `/`, `/products`, a product detail, `/cart`, `/checkout`; axe clean on checkout (order-failure banner has `role="alert"` — `_review/uiux.md` #12; header search has an accessible name — `_review/uiux.md` #13).
- [ ] CI fully green on the release commit, including the new Stripe-card E2E and GDPR integration tests; release tagged with a matching CHANGELOG section.
