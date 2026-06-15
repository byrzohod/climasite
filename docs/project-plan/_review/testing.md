# Testing — Review Findings (2026-06-11)

## Summary

Testing in ClimaSite is real and substantial — roughly 620 backend/E2E xUnit tests (77 Api integration, 163 Application, 173 Core, 208 E2E attributes) plus 941 Jasmine `it()` blocks in 77 spec files — and the latest persisted runs are green (61/61, 161/161, 197/197, E2E 206 passed / 0 failed / 1 not executed). The E2E suite is xUnit + Microsoft.Playwright for .NET with page objects, an API-backed TestDataFactory, axe-core accessibility checks, and an environment-gated TestController for data setup/cleanup — a solid, no-mocking design. CI (`.github/workflows/test.yml`) genuinely gates unit, integration (Testcontainers), frontend (including an i18n key check), build, and a full-stack E2E job.

However, CLAUDE.md's testing documentation describes a different, nonexistent stack (TypeScript Playwright, `npx` commands, `fixtures/test-data-factory.ts`, `tests/ClimaSite.Web.Tests` with karma.conf.js), and its mandated "Full Test Suite" command cannot work as written because root `dotnet test` includes the E2E project, which needs live servers. Coverage is collected for two projects but never thresholded anywhere, so the repo's own 80%/70% mandates are unmeasured and unenforced.

Depth is very uneven: 20 of 26 controllers have no integration tests and 13 of 18 Application feature areas have zero unit tests — including Orders/Payments/Webhooks/GDPR controllers and the entire Cart, Reviews, Inventory, and Notifications application logic. Most critically, the Stripe card payment path is never exercised end-to-end: the only completed-order E2E deliberately picks bank transfer "to avoid Stripe card iframe", and there are no Payments/Webhooks integration tests. Frontend gaps cluster on the riskiest units: auth.interceptor, auth.guard, payment.service, checkout and cart components all lack specs.

Flake posture is "serialize everything" (all 23 E2E classes share one xUnit collection) with no retries and silently-swallowed cleanup failures; a same-day TRX pair (74 failures at 16:00, full pass at 16:10) shows environment sensitivity. Overall: a strong skeleton with green status, but the money path, the compliance path, and the repo's own coverage policy are the load-bearing gaps.

## Findings

### 1. CLAUDE.md documents a nonexistent TypeScript Playwright suite; mandated test commands cannot work as written
- **Finding:** CLAUDE.md's testing documentation describes a TypeScript Playwright E2E stack that does not exist; the actual suite is Playwright for .NET, and the mandated post-implementation commands fail or misfire as written.
- **Category:** documentation/test-infrastructure
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `tests/ClimaSite.E2E/ClimaSite.E2E.csproj:10-19` references xunit 2.9.2 + Microsoft.Playwright 1.49.0 (pure .NET); no `playwright.config.*` exists outside bin/ and no .ts test files exist (verified via find). CLAUDE.md instructs `cd tests/ClimaSite.E2E && npx playwright test`, lists "Test Factory | tests/ClimaSite.E2E/fixtures/test-data-factory.ts" (actual: `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`), documents `tests/ClimaSite.Web.Tests/` (karma.conf.js), which does not exist (verified ls error), gives a TypeScript test example, and lists "Playwright Report http://localhost:9323". Additionally `ClimaSite.sln` includes ClimaSite.E2E (grep count 1), so CLAUDE.md's mandatory post-implementation command `dotnet test` at repo root runs E2E tests requiring live API+Angular servers, then the workflow runs E2E a second time.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/ClimaSite.E2E.csproj`, `/Users/sarkisharalampiev/Projects/climasite/ClimaSite.sln`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`
- **Why it matters:** This is an agent-driven repo whose agents follow CLAUDE.md literally. Wrong commands (`npx playwright test`) fail outright; root `dotnet test` silently attempts E2E without servers, producing 200+ false failures that mask real regressions and waste sessions.
- **Recommended fix:** Rewrite the Testing sections of CLAUDE.md: E2E is Playwright for .NET run via `dotnet test tests/ClimaSite.E2E` with E2E_BASE_URL/E2E_API_URL env vars and running servers; replace the `fixtures/test-data-factory.ts` and ClimaSite.Web.Tests references; change "Full Test Suite" to exclude E2E from root `dotnet test` (e.g. `dotnet test --filter` per project, or remove E2E from the default solution test set via a solution filter `ClimaSite.NoE2E.slnf`). (Effort: Small)
- **Acceptance criteria:** Every command in CLAUDE.md's Commands/Testing sections executes successfully on a fresh checkout (given Docker + servers where stated); no references to .ts E2E files, npx playwright, or ClimaSite.Web.Tests remain.
- **Dependencies or follow-up:** None
- **Confidence:** verified. Verifier confirmed all citations (CLAUDE.md:521-526, 537, 689, 128, 255-257; ClimaSite.sln:22; PlaywrightFixture.cs:16-17 defaults to localhost:4200/5029); the nested AGENTS.md documents the correct .NET setup but does not mitigate, since CLAUDE.md is the authoritative auto-loaded instruction file. P1 calibrated: not a product defect, but the broken NON-NEGOTIABLE workflow fails or floods every session with false failures; fix is small.

### 2. Stripe card payment path has zero automated coverage above mocked unit tests — the only full-order E2E deliberately avoids it
- **Finding:** The card payment path (Stripe Elements, create-intent endpoint, webhook-to-order transition) is never exercised end-to-end or at integration level; the sole completed-order E2E uses bank transfer specifically to avoid the Stripe card iframe.
- **Category:** e2e-coverage
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `tests/ClimaSite.E2E/Tests/Checkout/CheckoutTests.cs:343-345` — comment "Select bank transfer payment method (to avoid Stripe card iframe)" in `Checkout_CompleteOrder_ShowsConfirmation`, the sole test reaching order confirmation (lines 350-354). All other checkout/journey tests stop at "should proceed to payment step" (CheckoutTests.cs:119, CompletePurchaseTests.cs:150). No integration tests exist for PaymentsController (create-intent at `src/ClimaSite.Api/Controllers/PaymentsController.cs:41`) or WebhooksController (`[HttpPost("stripe")]` at `src/ClimaSite.Api/Controllers/WebhooksController.cs:37`) — `tests/ClimaSite.Api.Tests/Controllers/` contains only Auth, Cart, Products, Recommendations, Test, Wishlist. The only coverage is unit-level `tests/ClimaSite.Application.Tests/Features/Payments/Commands/HandleStripeWebhookCommandTests.cs` (14 tests, mocked).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Tests/Checkout/CheckoutTests.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/WebhooksController.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Application.Tests/Features/Payments/Commands/HandleStripeWebhookCommandTests.cs`
- **Why it matters:** Card payment is the primary revenue path of an e-commerce site. A regression in the Stripe Elements integration, create-intent endpoint, or webhook-to-order state transition would ship undetected; CLAUDE.md marks Checkout & Orders "Complete" with "Stripe integration".
- **Recommended fix:** Three layers: (1) integration tests for PaymentsController create-intent/cancel-intent and WebhooksController signature validation + event dispatch using Stripe's test signing secret and constructed event payloads; (2) one E2E happy path using Stripe test mode card 4242 4242 4242 4242 via FrameLocator for the Elements iframe (CheckoutPage already has card selectors at CheckoutPage.cs:20-22 and `FillPaymentDetailsAsync` at :126 — currently dead code); (3) keep the bank-transfer E2E as the fast path. (Effort: Medium)
- **Acceptance criteria:** CI runs at least one E2E completing a card payment to order confirmation in Stripe test mode, plus integration tests asserting webhook signature rejection and payment_intent.succeeded → order Paid transition.
- **Dependencies or follow-up:** Stripe test-mode keys in CI secrets; e2e environment config
- **Confidence:** verified. Verifier confirmed every evidence point, including the exact "avoid Stripe card iframe" comment, the absence of Payments/Webhooks integration tests (grep for PaymentIntent/webhook across Api.Tests is empty), exactly 14 mocked webhook unit tests, and `FillPaymentDetailsAsync` being dead code with no callers. P1 calibrated: no active defect (so not P0), but the primary revenue path has zero coverage above mocked units despite being marked "Complete".

### 3. 20 of 26 API controllers have no integration tests (Orders, Payments, Webhooks, GDPR, Reviews, Notifications, Inventory, all Admin)
- **Finding:** Only 6 of 26 API controllers have integration test classes; Orders, Payments, Webhooks, GDPR, Reviews, Notifications, Inventory, and all Admin controllers are untested at the HTTP layer despite working Testcontainers infrastructure.
- **Category:** integration-coverage
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Api/Controllers/` contains 22 root + 4 Admin controllers (verified ls). `tests/ClimaSite.Api.Tests/Controllers/` contains exactly 6 test classes: AuthControllerTests, CartControllerTests, ProductsControllerTests, RecommendationsControllerTests (targets ProductsController's /recommendations endpoint per ProductsController.cs:221), TestControllerTests, WishlistControllerTests. Untested: Addresses, AdminCustomers, AdminDashboard, AdminOrders, Brands, Categories, Gdpr, Installation, Inventory, Notifications, Orders, Payments, PriceHistory, Promotions, Questions, Reviews, Webhooks + Admin/{Categories,Products,Questions,Reviews}. Test infra (TestWebApplicationFactory.cs:13-26, Testcontainers Postgres+Redis) already exists and works.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/Controllers/`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/Infrastructure/TestWebApplicationFactory.cs`
- **Why it matters:** CLAUDE.md mandates "Integration Tests | All API endpoints". Auth/authorization wiring, model binding, EF query translation against real Postgres, and role gates (e.g. Admin endpoints) are only validated for 6 of 26 controllers; admin order fulfillment and order placement APIs rely solely on E2E UI tests, which are slower and coarser.
- **Recommended fix:** Prioritized rollout reusing IntegrationTestBase: wave 1 Orders, Payments, Webhooks, Gdpr (money/compliance); wave 2 Reviews, Questions, Notifications, Inventory, Addresses; wave 3 Admin* and catalog read controllers (Brands/Categories/Promotions/PriceHistory/Installation). Include authorization-denied (401/403) cases per controller. (Effort: Large)
- **Acceptance criteria:** Every controller has a test class covering each endpoint's happy path + auth failure; CI integration job duration stays under ~10 min.
- **Dependencies or follow-up:** None — Testcontainers infrastructure already in place
- **Confidence:** verified. Verifier confirmed the counts and noted the gap is actually 21 of 26 — RecommendationsControllerTests targets ProductsController, so only 5 distinct controllers are covered, slightly worse than the title states. Mitigation is only partial (mocked unit tests, E2E UI flows); GDPR has zero coverage at any level and Admin 401/403 role gates are untested at the HTTP layer.

### 4. 13 of 18 Application feature areas have zero unit tests (Cart, Reviews, Questions, Inventory, Notifications, GDPR, Promotions, etc.)
- **Finding:** Unit-test coverage of the Application layer is concentrated in 5 of 18 feature areas; 13 areas — including Cart merge logic, review verified-purchase rules, inventory reservations, promotions pricing, and GDPR data assembly — have no handler-level tests at all.
- **Category:** unit-coverage
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Application/Features/` has 18 feature dirs (verified ls). `tests/ClimaSite.Application.Tests/Features/` covers only 5: Auth (41 tests across 3 files), Orders (73 across 5), Payments (14, webhook only), Products (25, scoring only), Wishlist (10). Zero unit tests for: Addresses, Admin, Brands, Cart, Categories, Gdpr, Installation, Inventory, Notifications, PriceHistory, Promotions, Questions, Reviews. The "suspiciously thin" lead is half-right: 163 test attributes exist but are concentrated — e.g. Cart merge/quantity logic and Review verified-purchase rules have no handler-level tests (Cart only via API integration tests, Reviews only via E2E).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Application.Tests/Features/`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs`
- **Why it matters:** CLAUDE.md mandates unit tests for "every handler and service" at 80% coverage. Business rules like stock reservation (Inventory), review moderation, promotion pricing, and GDPR data assembly are currently testable only through slow integration/E2E layers or not at all.
- **Recommended fix:** Use the existing MockDbContext pattern (TestHelpers/MockDbContext.cs implements IApplicationDbContext over in-memory lists, proven by the Wishlist/Orders tests) to add handler tests, prioritized: Cart (merge, quantity combine), Inventory (reservation/oversell), Reviews (verified-purchase gate, moderation), Gdpr (export completeness, delete cascade), Promotions (price calc), Notifications (read/unread), then the rest. (Effort: Large)
- **Acceptance criteria:** Each Features/* directory has at least one handler test file; coverage report shows Application assembly ≥80% line coverage (see coverage-gate finding).
- **Dependencies or follow-up:** Coverage measurement finding (to verify the 80% target)
- **Confidence:** verified. Verifier matched all per-area counts exactly (163 attributes) and confirmed all 13 uncovered dirs contain real IRequestHandler implementations (e.g., Admin 27, Cart 6, Inventory 4, Gdpr 3) — not a DTO-only artifact; Gdpr, Inventory, and Promotions appear in the test tree solely inside MockDbContext DbSet declarations, i.e. zero tests at any layer. P1 calibrated against the repo's explicit 80%/every-handler mandate.

### 5. GDPR export and account deletion endpoints have zero tests at every layer
- **Finding:** The GDPR data export and irreversible account deletion endpoints have no tests at any layer — no unit, no integration, no E2E.
- **Category:** integration-coverage
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Api/Controllers/GdprController.cs:28-58` exposes GET /export and DELETE /delete (account deletion). Case-insensitive grep for "gdpr" across tests/ returns zero files (verified); `src/ClimaSite.Application/Features/Gdpr` has no corresponding test directory; no E2E folder for privacy/account-deletion exists in `tests/ClimaSite.E2E/Tests/`.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/GdprController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Gdpr/`
- **Why it matters:** Account deletion is destructive and irreversible (data-loss class); export completeness is a legal compliance requirement CLAUDE.md itself lists ("Customer data must comply with GDPR"). A bug that deletes the wrong rows, leaves orphaned PII, or omits data from export ships with no safety net.
- **Recommended fix:** Integration tests: export returns all PII categories for a seeded user (orders, addresses, reviews, wishlist); delete requires correct password/confirmation, anonymizes-or-removes PII while preserving order accounting rows, returns 401 unauthenticated, and is idempotent. Unit tests for the Gdpr handlers' data assembly. One E2E for the account-deletion UI flow if it exists in the frontend. (Effort: Medium)
- **Acceptance criteria:** Integration suite asserts post-delete database state (no PII rows for user) and export payload completeness against seeded fixtures.
- **Dependencies or follow-up:** Controller integration test wave 1 (shares infrastructure)
- **Confidence:** verified. Verifier confirmed DeleteUserDataCommand.cs:88-157 hard-deletes notifications, wishlist, cart, addresses, and review votes plus anonymizes the user — irreversible, data-loss-class behavior; grep for gdpr|ExportUserData|DeleteUserData|DeleteAccount across tests/ source hits only compiled bin/ artifacts, and no frontend code calls these endpoints (only unused i18n keys) — no mitigating coverage anywhere. P1 calibrated (no demonstrated defect, so not P0).

### 6. Coverage mandates (80% backend / 70% frontend) are not measured or enforced anywhere
- **Finding:** The repo's headline coverage requirements are neither measured nor enforced: no thresholds exist in any test configuration, CI's codecov upload cannot fail the build, and Api.Tests/frontend coverage is never collected.
- **Category:** coverage-tooling
- **Severity/Priority:** P2 — verification: adjusted (downgraded from P1)
- **Evidence:** No karma.conf.js exists anywhere (verified find); angular.json test target (projects.ClimaSite.Web.architect.test) has no codeCoverage/threshold options. No `*.runsettings` file in repo (verified find); coverlet.collector is referenced in all 4 test csprojs (e.g. `tests/ClimaSite.Application.Tests/ClimaSite.Application.Tests.csproj:12`) but has no Threshold configuration. CI collects coverage only for Core+Application (`.github/workflows/test.yml` unit-tests job) and uploads to codecov with `continue-on-error: true` — so even a total coverage collapse cannot fail CI. Api.Tests and frontend coverage are never collected in CI.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/angular.json`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Application.Tests/ClimaSite.Application.Tests.csproj`
- **Why it matters:** CLAUDE.md's headline testing policy (80%/70% minimums) is unverifiable and unenforced; the documented quality bar is fiction until measured. This also means the per-area gaps above are invisible to the team.
- **Recommended fix:** Backend: add a coverage.runsettings + `dotnet test --collect:"XPlat Code Coverage"` for all 3 unit/integration projects and gate via reportgenerator + a threshold step (or coverlet.msbuild /p:Threshold=80 on Application/Core). Frontend: create karma.conf.js with coverageReporter check thresholds (statements/lines 70) and run `ng test --code-coverage` in CI. Start with report-only for one sprint to baseline, then enforce. (Effort: Medium)
- **Acceptance criteria:** CI fails when Application/Core line coverage <80% or frontend <70%; a coverage summary is visible in CI artifacts/PR comment.
- **Dependencies or follow-up:** None
- **Confidence:** verified. All evidence confirmed (test.yml lines 33, 36, 43, 79, 102; no codecov.yml or other mitigation exists), but verifier adjusted P1→P2: pre-existing repo-wide tooling debt with no direct correctness/security/user-facing impact, CI still hard-gates on all suites passing (test.yml:254-271), and the fix is itself a baseline-then-enforce rollout — normal-course infrastructure work.

### 7. CI gate gaps: no lint, no coverage gate, E2E runs against dev server, trigger references nonexistent develop branch
- **Finding:** The CI pipeline omits linting and coverage gating, exercises the dev server (not the built bundle) in its E2E job, and its triggers reference a `develop` branch that does not exist, leaving feature-branch pushes without CI until a PR is opened.
- **Category:** ci
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `.github/workflows/test.yml` has 5 jobs (unit, integration, frontend, build, e2e) + summary, but: no `ng lint` or dotnet format/analyzer job anywhere despite CLAUDE.md checklist "No TypeScript/ESLint errors" (lint target exists: `src/ClimaSite.Web/angular.json` architect includes "lint"; package.json:11 defines it); codecov upload is `continue-on-error: true`; the e2e job builds the e2e configuration (`npm run build -- --configuration=e2e`) but then serves via `npm run start` dev server — the production-shaped build artifact is never tested; triggers are push/PR to [main, develop] but no develop branch exists locally or on origin (verified git branch -a), and current work happens on feature/* branches, so CI runs only when a PR is opened. Positive: the frontend job's `npm test` also runs scripts/check-i18n.mjs (package.json:9-10), gating translation keys.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/package.json`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/angular.json`
- **Why it matters:** Lint violations and coverage regressions merge silently; E2E never exercises the production bundle (AOT/budget/optimizer differences can break only in prod builds); feature-branch pushes without PRs get no CI at all.
- **Recommended fix:** Add a lint job (`ng lint` + `dotnet build -warnaserror` or analyzers); make coverage a real gate (see coverage finding); serve the built e2e bundle via a static server (or `ng serve --configuration=e2e --prod-like`) — or at minimum add a production-build smoke E2E; either create develop or trim the trigger and add `workflow_dispatch`/feature-branch push triggers. (Effort: Small)
- **Acceptance criteria:** PRs fail on lint errors or compiler warnings; E2E job tests the compiled bundle; CI triggers match the actual branching model.
- **Dependencies or follow-up:** Coverage-gate finding for the coverage part
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

### 8. Highest-risk frontend units lack specs: auth.interceptor, auth.guard, payment.service, checkout/cart/register components (32/86 components, 8/27 services, 8/10 guards-pipes-directives)
- **Finding:** The most regression-prone frontend units — the auth interceptor's 401-refresh-retry logic, auth guard, payment service, and the checkout, cart, and register components — have no unit specs; the gap is concentrated on the riskiest code, not diffuse.
- **Category:** frontend-unit-coverage
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Verified by spec-file existence scan: missing specs include `src/app/auth/interceptors/auth.interceptor.ts` (token refresh logic, previously the subject of a circular-dependency fix per CLAUDE.md), `src/app/auth/guards/auth.guard.ts`, `src/app/core/services/payment.service.ts`, `src/app/features/checkout/checkout.component.ts`, `src/app/features/cart/cart.component.ts`, `src/app/auth/components/register/register.component.ts`, forgot/reset-password, product-list/product-card, and the 5 admin feature components. 77 spec files contain 941 `it()` blocks, so the tested half is genuinely tested — the gap is concentrated, not diffuse.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/payment.service.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/cart/cart.component.ts`
- **Why it matters:** The interceptor's 401-refresh-retry logic and checkout component's multi-step state are the most regression-prone frontend code; they currently rely entirely on E2E, which can't exercise edge cases (concurrent 401s, refresh failure, expired token mid-checkout).
- **Recommended fix:** Add specs in priority order: auth.interceptor (HttpTestingController: 401→refresh→retry, refresh failure→logout, concurrent request queueing), auth.guard, payment.service, checkout.component (step transitions, validation), cart.component, register. Animation/visual services (confetti, flying-cart) are P3. (Effort: Medium)
- **Acceptance criteria:** Specs exist for all auth + payment + checkout units; frontend coverage threshold (70%) passes including these files.
- **Dependencies or follow-up:** None
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

### 9. Guest-to-login cart merge has no UI-level E2E test (API-only coverage)
- **Finding:** The guest-to-login cart merge flow — a key documented business rule — is covered only at the API level; no E2E test exercises the UI sequence the merge actually depends on.
- **Category:** e2e-coverage
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Grep for "merge" across `tests/ClimaSite.E2E/Tests/` matches only Wishlist/WishlistTests.cs (which does test guest wishlist merge at :137-163). Cart merge is covered only at API level: `tests/ClimaSite.Api.Tests/Controllers/CartControllerTests.cs:414-499` (MergeCart region, 4 tests incl. quantity combining and 401). No E2E adds items as guest, logs in through the UI, and asserts the merged cart — the flow that depends on the frontend sending guestSessionId at the right moment in the login sequence.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/Controllers/CartControllerTests.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Tests/Cart/CartTests.cs`
- **Why it matters:** CLAUDE.md lists "Cart merges when guest user logs in" as a key business rule; the failure mode (frontend never calls /api/cart/merge, or calls it before auth state settles) is invisible to API tests. The wishlist slice just shipped exactly this pattern with an E2E — cart deserves parity.
- **Recommended fix:** Add `Cart_GuestItems_MergeIntoUserCartAfterLogin` to CartTests.cs mirroring WishlistTests.cs:137: add product as guest, login via LoginPage, navigate to cart, assert item present and quantity combined when user already had the item. (Effort: Small)
- **Acceptance criteria:** New E2E passes in CI; covers both empty-user-cart and quantity-combine variants.
- **Dependencies or follow-up:** None
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

### 10. E2E flake posture: fully serialized suite, no retry policy, silently-swallowed cleanup failures; evidence of one 74-failure run
- **Finding:** The E2E suite's reliability posture is fragile: all 23 test classes run strictly sequentially in one xUnit collection, there is no retry policy, cleanup failures are silently swallowed, and TRX history records one same-day 74-failure run followed by a full pass.
- **Category:** test-reliability
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** All 23 E2E test classes use `[Collection("Playwright")]` (grep count 23) sharing one fixture → xUnit runs them strictly sequentially (stable but the CI job needs a 30-min timeout, test.yml e2e-tests). No xunit.runner.json, no retry helper. `TestDataFactory.CleanupAsync` (Infrastructure/TestDataFactory.cs:320-330) wraps DELETE /api/test/cleanup/{correlationId} in `catch { /* ignore */ }` — failed cleanup pollutes the shared database invisibly. TRX history shows e2e-final.trx (Jun 6 16:00) with 132 passed / 74 failed followed by e2e-final-testing.trx (16:10) with 206/206 passed — consistent with environment sensitivity (server not ready / config), not test logic, but unverifiable from artifacts alone. Latest run also has 1 NotExecuted: Checkout_SavedAddress_CanBeUsed.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Infrastructure/PlaywrightFixture.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/TestResults/e2e-final.trx`
- **Why it matters:** As the suite grows past 208 tests, a serial 30-min job becomes the merge bottleneck; silent cleanup failure undermines the "self-contained tests" rule and produces order-dependent flakes that are hard to diagnose; mass-failure runs with no triage trail erode trust in red builds.
- **Recommended fix:** (1) Log cleanup failures (at minimum Console.WriteLine with correlationId) and add a CI post-step that calls a bulk cleanup endpoint. (2) Split into 2-3 xUnit collections by area with separate browser contexts to parallelize safely. (3) Add a small retry helper (or rerun-failed step in CI) for known-network-sensitive assertions only — not blanket retries. (4) Investigate/annotate why Checkout_SavedAddress_CanBeUsed was NotExecuted. (Effort: Medium)
- **Acceptance criteria:** E2E wall time reduced ~40%; cleanup failures surface in CI logs; no NotExecuted tests without an explicit Skip reason.
- **Dependencies or follow-up:** TestController bulk cleanup endpoint (exists: /api/test/cleanup/{correlationId})
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

### 11. Working-tree wishlist tests are newer than every persisted test run; "full local test coverage" claim is partially unevidenced (Application slice re-verified passing)
- **Finding:** The uncommitted wishlist test files postdate every persisted TRX run, so the branch's claimed "unit/API/E2E coverage" is only partially evidenced — the Application slice was re-run and passes, but the new API integration and E2E wishlist tests have no recorded execution.
- **Category:** verification
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** All TRX files date Jun 6 (api-tests.trx 15:04 = 61 tests) while the uncommitted test files date Jun 7 (WishlistControllerTests.cs 09:10, WishlistHandlersTests.cs 08:43, WishlistTests.cs 08:51, wishlist.service.spec.ts 09:11 — verified stat). Api.Tests now has 77 test attributes vs 61 executed in its last recorded run, i.e. the 7 new WishlistControllerTests (+ theory expansion) have no persisted run. Executed during this review: `dotnet test tests/ClimaSite.Application.Tests --filter FullyQualifiedName~Wishlist` — 10/10 passed in 134 ms. The new API integration tests (need Docker) and 6 wishlist E2E tests (need servers) remain unexecuted as far as artifacts show, while CLAUDE.md asserts the slice has "unit/API/E2E coverage" and "full local test coverage".
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/Controllers/WishlistControllerTests.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Tests/Wishlist/WishlistTests.cs`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/TestResults/api-tests.trx`
- **Why it matters:** The branch is positioned as merge-ready ("Wishlist Completion | Complete") but the integration and E2E halves of its test evidence are 5 days stale relative to the code. TRX may simply not have been re-emitted, but a reviewer cannot distinguish "ran without --logger trx" from "never ran".
- **Recommended fix:** Before merging feature/plan18-wishlist-completion: run `dotnet test tests/ClimaSite.Api.Tests` (Docker required) and the E2E wishlist folder against running servers; emit TRX. Longer-term, let CI be the source of truth (open the PR so the full pipeline runs) instead of local TRX snapshots. (Effort: Small)
- **Acceptance criteria:** A green CI run (or fresh TRX) covering WishlistControllerTests and Tests/Wishlist exists for the merge commit.
- **Dependencies or follow-up:** Docker + running app servers, or simply opening the PR to trigger CI
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

### 12. Test hygiene: placeholder UnitTest1, redundant CI Postgres service, dead FillPaymentDetailsAsync page-object code
- **Finding:** Minor hygiene debt: an always-passing placeholder test inflates counts, the CI integration job provisions a Postgres service container that is never used, and the checkout page object carries dead card-payment code.
- **Category:** hygiene
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `tests/ClimaSite.Api.Tests/UnitTest1.cs` contains an empty `[Fact] Test1()` that always passes (inflates counts). The CI integration-tests job provisions a postgres:16-alpine service container and sets ConnectionStrings__DefaultConnection (test.yml integration-tests job), but TestWebApplicationFactory.cs:15-20 spins up its own Testcontainers Postgres and overrides the connection string — the service container is never used. `CheckoutPage.FillPaymentDetailsAsync` (PageObjects/CheckoutPage.cs:126-131) and the card selectors are unused by any test (card path avoided). TRX/test-results are correctly git-ignored (verified git check-ignore).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.Api.Tests/UnitTest1.cs`, `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/PageObjects/CheckoutPage.cs`
- **Why it matters:** Placeholder tests mask real counts; the unused service container wastes CI minutes and misleads readers about how integration tests connect; dead page-object code suggests an abandoned card-payment test attempt (reinforces the Stripe finding).
- **Recommended fix:** Delete UnitTest1.cs; remove the postgres service block + env var from the integration-tests job (Testcontainers needs only Docker); either use FillPaymentDetailsAsync in the new Stripe E2E or remove it. (Effort: Small)
- **Acceptance criteria:** CI integration job green without the service container; no always-pass placeholder tests remain.
- **Dependencies or follow-up:** Stripe E2E finding (for the page-object decision)
- **Confidence:** verified by the reviewer; not independently re-verified (P2/P3 findings were not put through verification)

## Dimension data

### Ground truth: the E2E stack (Task 1)

**Actual:** xUnit 2.9.2 + Microsoft.Playwright 1.49.0 (.NET), FluentAssertions, Bogus, Deque.AxeCore.Playwright (`tests/ClimaSite.E2E/ClimaSite.E2E.csproj`). Structure: `Infrastructure/` (PlaywrightFixture — shared Chromium browser, 30s timeouts; TestDataFactory — creates real users/products/orders via API + env-gated TestController, correlation-ID cleanup), `PageObjects/` (7 pages), `Tests/` (16 areas, 23 classes, 208 [Fact]s, all in one `[Collection("Playwright")]` → serial). Run via `dotnet test tests/ClimaSite.E2E` with `E2E_BASE_URL`/`E2E_API_URL`.

**CLAUDE.md is wrong about:** `npx playwright test` commands; `fixtures/test-data-factory.ts`; `tests/ClimaSite.Web.Tests/ (karma.conf.js)` (doesn't exist — Angular specs are colocated in `src/ClimaSite.Web/src`); TypeScript test example; Playwright Report URL :9323; and root `dotnet test` in the mandatory workflow silently includes the E2E project (it's in ClimaSite.sln). The nested `tests/ClimaSite.E2E/AGENTS.md` is accurate — CLAUDE.md was never updated to match.

### Test counts & latest results (Tasks 3, 5)

| Project | [Fact]/[Theory] attrs | Latest TRX (date) | Result |
|---|---|---|---|
| ClimaSite.Core.Tests | 173 | core-tests.trx (Jun 6 15:03) | 197/197 passed |
| ClimaSite.Application.Tests | 163 | application-tests.trx (Jun 6 15:05) | 161/161 passed |
| ClimaSite.Api.Tests | 77 | api-tests.trx (Jun 6 15:04) | 61/61 passed (predates +16 wishlist tests of Jun 7) |
| ClimaSite.E2E | 208 | e2e-final-testing.trx (Jun 6 16:10) | 206 passed / 0 failed / 1 NotExecuted (Checkout_SavedAddress_CanBeUsed) |
| Frontend (Jasmine) | 941 `it()` in 77 spec files | none persisted | unknown locally; CI job exists |

Earlier same-day E2E run (e2e-final.trx, 16:00): 132 passed / **74 failed** → environment sensitivity. Re-ran during review: Application wishlist tests 10/10 pass (134 ms).

### Coverage matrix (Task 3/6)

| Area | Unit (Application/Core) | Integration (Api.Tests) | E2E | Gap |
|---|---|---|---|---|
| Auth | Yes (41) | Yes (AuthControllerTests, 16) | Yes (Login, UserMenu) | password reset E2E thin |
| Products/catalog | Scoring only (25) | Yes (Products 7 + Recommendations 11) | Yes (3 classes) | query handlers unit-untested |
| Cart | **None** | Yes incl. merge (18) | Yes, but **no guest→login merge UI test** | P2 finding |
| Checkout/Orders | Yes (73) | **None** (OrdersController) | Yes (4 Orders classes + Checkout) | OrdersController integration |
| Payments/Stripe | Webhook unit only (14, mocked) | **None** (Payments, Webhooks) | **Card path avoided** (bank transfer only) | **P1 — money path** |
| Admin (all 7 controllers) | **None** | **None** | Yes (AdminPanelTests incl. order status update, moderation) | API-level role gates untested |
| Reviews/Q&A | **None** | **None** | Yes (ReviewsQATests) | verified-purchase rule unit tests |
| Inventory | **None** | **None** | Stock-validation E2E only (Checkout_StockValidation) | reservation logic untested |
| Notifications | Core entity only | **None** | **None** | matches "Partial" status, still zero API tests |
| Wishlist (new slice) | Yes (10, pass) | Yes (7, **no recorded run**) | Yes (6 incl. share link + guest merge, **no recorded run**) | re-run before merge |
| GDPR | **None** | **None** | **None** | **P1 — compliance/destructive** |
| Addresses/Brands/Categories/Promotions/PriceHistory/Installation | **None** | **None** | partial (Contact, MegaMenu, Pages) | wave-3 backlog |
| i18n / Theming / A11y | check-i18n.mjs script | n/a | Yes (LanguageTests, ThemeAndSettings, AccessibilityTests w/ axe-core) | good |
| Frontend units | 77/115+ files spec'd | n/a | n/a | auth.interceptor, auth.guard, payment.service, checkout, cart, register, 5 admin components |

### CI gate (Task 2) — what runs vs. what's missing

**Runs & gates:** backend unit (Core+Application), integration (Api.Tests, Testcontainers), frontend (i18n key check + ng test headless), Release build (BE+FE), full-stack E2E (Postgres+Redis services, API :5029, ng serve e2e, chromium, artifacts on failure), final summary job requiring all green.

**Missing from the gate:** (1) `ng lint` / dotnet analyzers / -warnaserror; (2) any coverage threshold (codecov `continue-on-error: true`); (3) frontend + Api.Tests coverage never collected; (4) E2E exercises dev server, not the built bundle (built artifact discarded); (5) triggers limited to main/`develop` — develop doesn't exist, so feature branches only get CI via PR; (6) no migration-apply check job; (7) unused Postgres service container in integration job.

### Prioritized test backlog (Task 7)

**P0 (before next release)**
1. Re-run + record Api.Tests and E2E wishlist suites on this branch (or open the PR and let CI run) — closes the evidence gap on the merge candidate.
2. Fix CLAUDE.md test commands so agents/devs can actually run the suites (root `dotnet test` trap).

**P1**
3. Stripe card E2E (test card via FrameLocator) + Payments/Webhooks integration tests (signature rejection, intent→order transition).
4. GDPR integration tests: export completeness, delete anonymization, idempotency, authz.
5. Integration tests wave 1: OrdersController, PaymentsController, WebhooksController, GdprController.
6. Coverage measurement + thresholds (coverlet runsettings + karma.conf check 70%); baseline first, then enforce 80/70.
7. Application unit tests: Cart (merge/quantities), Inventory (reservations/oversell), Reviews (verified purchase).

**P2**
8. Frontend specs: auth.interceptor (401→refresh→retry/queue/logout), auth.guard, payment.service, checkout.component, cart.component, register.
9. Cart guest→login merge E2E (mirror WishlistTests pattern).
10. Integration wave 2: Reviews, Questions, Notifications, Inventory, Addresses (+401/403 cases per endpoint).
11. E2E reliability: log cleanup failures, CI bulk-cleanup post-step, split into 2-3 collections for parallelism, explain the NotExecuted saved-address test.
12. CI: lint job, -warnaserror, serve built e2e bundle.

**P3**
13. Integration wave 3: Admin*, Brands, Categories, Promotions, PriceHistory, Installation.
14. Remaining frontend specs (admin components, product-card/list, animation services).
15. Delete UnitTest1.cs; remove unused CI postgres service; use-or-delete FillPaymentDetailsAsync.
16. Decide develop-branch trigger; add workflow_dispatch.

### Layering strategy (recommended)

- **Unit (Application + MockDbContext):** all business rules/edge cases — cheap, the 13 empty feature areas are the bulk of new work.
- **Integration (Api.Tests + Testcontainers):** per-endpoint happy path + authz (401/403) + DB-state assertions; the only place to test EF→Postgres translation and webhook signatures.
- **E2E (Playwright .NET):** one happy path per user journey + the cross-cutting concerns only a browser sees (auth-state timing, iframes, i18n/theme/a11y). Resist duplicating edge cases here.
- **Flake control:** keep no-mocking + correlation-ID isolation; make cleanup observable; parallelize by collection, not within; retries only as CI rerun-failed, never inside test logic.

### Open questions

1. Why was Checkout_SavedAddress_CanBeUsed NotExecuted in the last full run (filter? runtime skip?) — no [Fact(Skip)] exists.
2. What caused the 74-failure run at Jun 6 16:00 — worth a CI guard (healthcheck-gated test start already exists in CI; the failure run was local).
3. Is codecov actually configured (token/status checks) on the GitHub repo, or is the upload step dead? Cannot verify from the repo.
4. Frontend test pass-rate right now is unverified locally (no persisted karma output; running 941 specs was out of scope for this review).

## Refuted during verification

None.
