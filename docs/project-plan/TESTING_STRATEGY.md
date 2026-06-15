# ClimaSite Testing Strategy

**Date:** 2026-06-11
**Sources:** `docs/project-plan/_review/testing.md` (primary), cross-checked against `_review/bugs.md`, `_review/product.md`, `_review/devops.md`, the CI workflow `.github/workflows/test.yml`, and the test projects themselves.

**How to use this document:** This is the single authoritative description of how testing actually works in ClimaSite as of 2026-06-11 and what to build next. Read "Ground truth" first — the testing sections of `CLAUDE.md` and both `AGENTS.md` files describe a TypeScript Playwright stack that **does not exist**, and following them will waste your session. Then use the coverage matrix to see where tests exist, the "Missing tests by layer" section for concrete targets (file paths included), and the P0–P3 backlog table to pick work in priority order. A key theme throughout: the suites are green (≈620 backend/E2E tests + 941 frontend specs passing), yet three P0 money-path bugs shipped under them (`_review/bugs.md` findings 1–3) — because the broken paths are exactly the ones with no tests. Green CI currently proves much less than it appears to.

---

## 1. Ground truth: what test stacks actually exist

### 1.1 The four real test projects

| Project | Stack | Count (attrs/specs) | Run command |
|---|---|---|---|
| `tests/ClimaSite.Core.Tests` | xUnit | 173 `[Fact]`/`[Theory]` | `dotnet test tests/ClimaSite.Core.Tests` |
| `tests/ClimaSite.Application.Tests` | xUnit + `TestHelpers/MockDbContext.cs` (in-memory `IApplicationDbContext`) | 163 | `dotnet test tests/ClimaSite.Application.Tests` |
| `tests/ClimaSite.Api.Tests` | xUnit + `Infrastructure/TestWebApplicationFactory.cs` (**Testcontainers** Postgres + Redis — requires Docker) | 77 | `dotnet test tests/ClimaSite.Api.Tests` |
| `tests/ClimaSite.E2E` | **xUnit + Microsoft.Playwright 1.49.0 for .NET** (C#, not TypeScript), FluentAssertions, Bogus, Deque.AxeCore.Playwright | 208 across 23 classes / 16 areas | `dotnet test tests/ClimaSite.E2E` — requires live servers, see below |
| Frontend (no separate project) | Jasmine/Karma specs **colocated** in `src/ClimaSite.Web/src/**` | 941 `it()` in 77 spec files | `cd src/ClimaSite.Web && npm test` (also runs `scripts/check-i18n.mjs`) |

E2E internals: `Infrastructure/PlaywrightFixture.cs` (shared Chromium, 30 s timeouts, defaults to `localhost:4200`/`5029`), `Infrastructure/TestDataFactory.cs` (creates real users/products/orders via the API plus the environment-gated `TestController`; correlation-ID cleanup), `PageObjects/` (7 pages). All 23 classes share one `[Collection("Playwright")]` → fully serial execution. Design is genuinely no-mocking and self-contained, matching the project's stated E2E rules.

### 1.2 The CLAUDE.md discrepancy (P1 — fix before anything else)

`CLAUDE.md` (and root `AGENTS.md`, and the COMMANDS block of `tests/ClimaSite.E2E/AGENTS.md:75-80`) document a stack that was never built:

| CLAUDE.md claims | Reality |
|---|---|
| `npx playwright test` / `--ui` / `-g "checkout"` | No `package.json`, no `playwright.config.*`, no `.ts` test files in `tests/ClimaSite.E2E`. Run via `dotnet test tests/ClimaSite.E2E` |
| `tests/ClimaSite.E2E/fixtures/test-data-factory.ts` | `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs` |
| `tests/ClimaSite.Web.Tests/` with `karma.conf.js` | Directory does not exist; Angular specs are colocated under `src/ClimaSite.Web/src`; **no karma.conf.js exists anywhere** |
| Playwright Report at `http://localhost:9323` | No such report server; results are TRX files |
| API at `http://localhost:5000` | `launchSettings.json` → `http://localhost:5029` |
| Root `dotnet test` as step 1 of the "NON-NEGOTIABLE" post-implementation workflow | `ClimaSite.sln` **includes** the E2E project, so root `dotnet test` attempts 208 browser tests with no servers running → 200+ false failures that mask real regressions |
| "stock reservations", `Accept-Language` header | Neither exists in code (`_review/bugs.md` findings 5/18); language travels as `?lang=` query param |

The nested `tests/ClimaSite.E2E/AGENTS.md` accurately describes the fixture/factory/page-object patterns but its COMMANDS section repeats the wrong `npx` commands — trust its patterns, not its commands.

### 1.3 Corrected commands (use these, not CLAUDE.md's)

```bash
# Backend unit (no Docker needed)
dotnet test tests/ClimaSite.Core.Tests
dotnet test tests/ClimaSite.Application.Tests

# API integration (Docker required — Testcontainers spins up Postgres + Redis itself;
# the Postgres service container in CI is unused legacy, see §3)
dotnet test tests/ClimaSite.Api.Tests

# Frontend unit + i18n key check
cd src/ClimaSite.Web && npm test
# or explicitly: ng test --watch=false --browsers=ChromeHeadless

# E2E — three steps:
# 1. Start servers
dotnet run --project src/ClimaSite.Api            # http://localhost:5029
cd src/ClimaSite.Web && npm run start -- --configuration=e2e --port=4200
# 2. First time only: install browsers (after building the E2E project)
dotnet build tests/ClimaSite.E2E --configuration Release
pwsh tests/ClimaSite.E2E/bin/Release/net10.0/playwright.ps1 install chromium
# 3. Run
E2E_BASE_URL=http://localhost:4200 E2E_API_URL=http://localhost:5029 \
  dotnet test tests/ClimaSite.E2E --configuration Release \
  --logger "trx;LogFileName=e2e-results.trx"

# Filter E2E by name (replaces `npx playwright test -g "checkout"`):
dotnet test tests/ClimaSite.E2E --filter "FullyQualifiedName~Checkout"

# NEVER run bare `dotnet test` at repo root until the sln is filtered (backlog item TS-02).
```

### 1.4 Latest recorded results

| Suite | Latest TRX | Result |
|---|---|---|
| Core | `core-tests.trx` (2026-06-06 15:03) | 197/197 pass |
| Application | `application-tests.trx` (06-06 15:05) | 161/161 pass — predates the new wishlist tests |
| Api | `api-tests.trx` (06-06 15:04) | 61/61 pass — **predates the 16 new wishlist integration tests** (Jun 7 working tree) |
| E2E | `e2e-final-testing.trx` (06-06 16:10) | 206 pass / 0 fail / **1 NotExecuted** (`Checkout_SavedAddress_CanBeUsed` — plain `[Fact]`, reason unknown. Needs confirmation) |
| Frontend | none persisted | Pass rate unverified locally; CI job exists. Needs confirmation |

Caution flags: a same-day E2E run (`e2e-final.trx`, 06-06 16:00) shows **74 failures**, fully green ten minutes later — environment sensitivity (server readiness), not test logic. The uncommitted wishlist test files (`tests/ClimaSite.Api.Tests/Controllers/WishlistControllerTests.cs`, `tests/ClimaSite.E2E/Tests/Wishlist/WishlistTests.cs`) have **no recorded run**; only the Application slice was re-run during review (10/10 pass). CLAUDE.md's "full local test coverage" claim for the wishlist slice is therefore partially unevidenced — re-run or open the PR before merging `feature/plan18-wishlist-completion`.

---

## 2. Current coverage summary

Matrix per area (from `_review/testing.md`, spot-confirmed against `tests/`):

| Area | Unit (Application/Core) | Integration (Api.Tests) | E2E | Verdict |
|---|---|---|---|---|
| Auth | ✅ 41 tests (but some target a **dead duplicate** `Features/Auth/*` stack — see §4.1) | ✅ AuthControllerTests (16) | ✅ Login, UserMenu | OK; password-reset E2E thin (and the flow itself is broken — no email ever sent) |
| Products/catalog | ⚠️ Scoring only (25) | ✅ Products (7) + Recommendations (11) | ✅ 3 classes | Query handlers unit-untested |
| Cart | ❌ none | ✅ incl. merge (18) | ⚠️ no guest→login merge UI test | **API tests pass while the real FE merge call always 400s** (`_review/bugs.md` #3) |
| Checkout/Orders | ✅ 73 | ❌ OrdersController untested | ✅ 4 Orders classes + Checkout | OrdersController integration missing — which is exactly where `paymentIntentId` is silently dropped |
| Payments/Stripe | ⚠️ webhook unit only (14, fully mocked) | ❌ Payments, Webhooks | ❌ **card path deliberately avoided** (bank transfer only, `CheckoutTests.cs:343-345`) | **Worst gap — the money path** |
| Admin (7 controllers) | ❌ | ❌ | ✅ AdminPanelTests | API role gates untested; note 3 of 4 admin UI pages are stubs (`_review/product.md` #2) |
| Reviews/Q&A | ❌ | ❌ | ✅ ReviewsQATests | Verified-purchase rule (`CreateReviewCommand.cs:95`) has no unit test |
| Inventory | ❌ | ❌ | ⚠️ stock-validation E2E only | Reservation logic untested — because **it doesn't exist** (`_review/bugs.md` #5: oversell race) |
| Notifications | ⚠️ Core entity only | ❌ | ❌ | Matches "Partial" status; zero API tests |
| Wishlist (new slice) | ✅ 10 (re-verified passing) | ⚠️ 7, no recorded run | ⚠️ 6, no recorded run | Re-run before merge |
| GDPR | ❌ | ❌ | ❌ | **Zero tests at every layer for an irreversible delete + legal export** (`GdprController.cs:28-58`, `DeleteUserDataCommand.cs:90-157`) |
| Addresses/Brands/Categories/Promotions/PriceHistory/Installation | ❌ | ❌ | partial | Wave-3 backlog |
| i18n / Theming / A11y | ✅ `scripts/check-i18n.mjs` gates keys in CI | n/a | ✅ LanguageTests, ThemeAndSettings, AccessibilityTests (axe-core) | Genuinely good |
| Frontend units | 77 of ~115+ files have specs (941 `it()`) | n/a | n/a | Missing specs cluster on the riskiest code: `auth.interceptor`, `auth.guard`, `payment.service`, `checkout.component`, `cart.component`, `register.component` |

Headline numbers: **20 of 26 controllers** have no integration tests; **13 of 18 Application feature areas** have zero unit tests. The gap is not diffuse — it is concentrated on money (Payments/Orders/Webhooks), compliance (GDPR), and the auth-refresh frontend path.

### Why green tests missed the P0 bugs (cross-check with `_review/bugs.md` / `_review/product.md`)

1. **Cart merge always 400s in production** (FE sends body+header; BE wants `[FromQuery]`) — API tests pass because they call the endpoint with the query param directly (`CartControllerTests.cs:436,473`); no E2E logs in through the UI with a guest cart.
2. **`paymentIntentId` silently dropped on order creation** → webhooks never match an order — no OrdersController integration test posts the real frontend payload shape.
3. **Client-supplied charge amount, BGN-vs-EUR, shipping omitted** — no test compares displayed total vs charged amount vs `Order.Total`.
4. **Guest checkout E2E is tautological** — passes whether the guest reaches checkout *or* is redirected to login (`_review/product.md` #9).

Lesson for all new tests: assert **contracts as the real client sends them** (exact JSON bodies, headers, query params) and assert **cross-system invariants** (displayed == charged == recorded), not just per-endpoint happy paths.

---

## 3. What CI actually gates vs. what it should

Pipeline: `.github/workflows/test.yml`, triggers `push`/`PR` to `[main, develop]`.

### Actually gated (verified in the workflow)

| Job | What it runs |
|---|---|
| Unit Tests | `dotnet test tests/ClimaSite.Core.Tests` + `tests/ClimaSite.Application.Tests`, collects XPlat coverage for these two only |
| Integration Tests | `dotnet test tests/ClimaSite.Api.Tests` (Testcontainers; the workflow's own `postgres:16-alpine` service container is **never used** — legacy) |
| Frontend Unit Tests | `npm test` → `check-i18n.mjs` (translation-key gate) + headless Karma |
| Build Verification | Release builds, backend + frontend |
| E2E Tests | Real full stack: Postgres+Redis services, API on :5029, `ng serve --configuration=e2e` on :4200, chromium, `dotnet test tests/ClimaSite.E2E`, TRX + artifacts on failure |
| Test Summary | Requires all jobs green |

### Not gated (should be)

1. **No coverage threshold anywhere.** Codecov upload is `continue-on-error: true` (test.yml:43) — a total coverage collapse cannot fail CI. Api.Tests and frontend coverage are never even collected. The repo's own 80%/70% mandates are fiction until measured (`_review/testing.md` #6).
2. **No lint job.** `ng lint` exists (`src/ClimaSite.Web/package.json:11`, angular.json lint target) but never runs in CI; no `dotnet format`/`-warnaserror` either — despite CLAUDE.md's checklist requiring both.
3. **E2E runs against the dev server, not the built bundle.** The job builds `--configuration=e2e` then serves via `npm run start` — the production-shaped artifact is never exercised (AOT/optimizer/budget breakages ship undetected).
4. **Trigger mismatch:** `develop` does not exist (verified `git branch -a`); feature branches get CI only when a PR is opened. No `workflow_dispatch`.
5. **No migration job** — migrations are never applied to a clean Postgres in CI (also a deploy risk; see `_review/devops.md` #3).
6. **CI is advisory anyway:** `main` has no branch protection (devops review verified live via `gh api` — 404), and a red commit has already landed on main (run 27071713464). Protecting main with required checks is what makes everything above matter.

---

## 4. Missing tests by layer (concrete targets)

### 4.1 Unit — Application layer (`tests/ClimaSite.Application.Tests`, use the proven `TestHelpers/MockDbContext.cs` pattern)

Priority order, each with the business rule at stake:

| Target | What to test |
|---|---|
| `Features/Cart/Commands/MergeGuestCartCommand.cs` | Merge, quantity combine, **stock-capped merge** (`:102-119`), expired guest cart |
| `Features/Orders/Commands/CreateOrderCommand.cs` (extend existing 73) | After the P0 fix: `PaymentIntentId`/`PaymentMethod` persisted; total = subtotal+shipping+tax; insufficient-stock failure path |
| Inventory (`Features/Inventory/`, `AdjustStockCommand.cs:47-62`) | Reservation/oversell semantics **once the concurrency fix lands** (`_review/bugs.md` #5); until then, at minimum the adjust-stock validation |
| `Features/Reviews/Commands/CreateReviewCommand.cs` | Verified-purchase gate (`:95`), moderation states |
| `Features/Gdpr/Commands/` (`DeleteUserDataCommand`, export) | Export completeness per PII category; delete removes/anonymizes the right rows |
| Promotions | Price calculation, featured/active filtering |
| Notifications | Read/unread/summary logic |
| Then: Addresses, Brands, Categories, Installation, PriceHistory, Admin commands | Happy path + validation each |
| Sale-price mapping helper (new, per `_review/bugs.md` #6 fix) | One shared mapper test asserting `SalePrice`/`BasePrice` semantics — kills the 14-way inverted-projection drift |

**Cleanup:** `tests/ClimaSite.Application.Tests/Features/Auth/` exercises the **dead duplicate** auth stack (`src/ClimaSite.Application/Features/Auth/*`); the wired one is `ClimaSite.Application.Auth.*` (see `AuthController.cs:2`). Delete the dead handlers and retarget or delete these tests (`_review/product.md` #17). Also delete the placeholder `tests/ClimaSite.Api.Tests/UnitTest1.cs` (always-passing `[Fact]`).

### 4.2 Integration — API (`tests/ClimaSite.Api.Tests`, reuse `TestWebApplicationFactory`/IntegrationTestBase; include 401/403 cases per controller)

- **Wave 1 (money/compliance):** `OrdersController` (create with the **exact frontend payload** incl. `paymentIntentId` — would have caught P0 #1), `PaymentsController` (create-intent must reject/ignore client-supplied amounts after the fix; cancel-intent), `WebhooksController` (Stripe signature rejection; `payment_intent.succeeded` → order Paid; unmatched-intent behavior — note `WebhooksController.cs:91-93` currently returns 200 for "no matching order" so Stripe never retries), `GdprController` (export completeness, delete idempotency + post-delete DB state, 401).
- **Wave 2:** Reviews, Questions, Notifications, Inventory, Addresses.
- **Wave 3:** `Admin/*` (role-gate 403s for non-admins), Brands, Categories, Promotions, PriceHistory, Installation.
- **Contract regression for the merge bug:** a test that calls `POST /api/cart/merge` with the *frontend's* current shape (body + `X-Session-Id` header) and asserts success once the contract is fixed.
- **Concurrency:** two parallel `CreateOrder` calls for the last unit — exactly one succeeds, stock never negative (acceptance test for `_review/bugs.md` #5).

### 4.3 Component/unit — Frontend (colocated specs, priority order)

| Target file | Key cases |
|---|---|
| `src/app/auth/interceptors/auth.interceptor.ts` | Via `HttpTestingController`: 401 → refresh → retry; refresh failure → logout; **concurrent 401s produce exactly one `POST /auth/refresh`** (currently they log the user out — `_review/bugs.md` #8; the fix and the spec should land together) |
| `src/app/auth/guards/auth.guard.ts` | Redirect vs. allow |
| `src/app/core/services/payment.service.ts` | Intent creation/error paths |
| `src/app/features/checkout/checkout.component.ts` | Step transitions, validation, **double-submit guard** (processing flag set before the payment phase — `_review/bugs.md` #4), totals rendering |
| `src/app/features/cart/cart.component.ts` | Quantity update, removal, totals |
| `src/app/auth/components/register/register.component.ts` + forgot/reset-password | Form validation, error states |
| Then: product-list/product-card, 5 admin components, animation services (P3) | |

### 4.4 E2E (`tests/ClimaSite.E2E`, mirror existing page-object + TestDataFactory patterns)

- **Stripe card happy path:** test card `4242 4242 4242 4242` via `FrameLocator` for the Elements iframe. `PageObjects/CheckoutPage.cs` already has card selectors (`:20-22`) and `FillPaymentDetailsAsync` (`:126`) — currently dead code; use it or delete it. Requires Stripe test-mode keys in CI secrets.
- **Guest→login cart merge:** mirror the wishlist pattern at `Tests/Wishlist/WishlistTests.cs:137-163` — add as guest, log in through the UI, assert items survive (regression test for P0 bug #3). Variants: empty user cart, quantity combine.
- **Fix the tautological guest-checkout E2E** (`Checkout_GuestUser_CanCheckoutWithEmail`) once the product decision on guest checkout is made (`_review/product.md` #9) — it must assert one specific outcome, not accept both.
- **Order status transition:** order leaves Pending after a (simulated) successful payment — end-to-end proof of the webhook fix.
- **Out-of-stock UX** once the hardcoded "In Stock" badge is fixed (`_review/product.md` #16).
- Investigate/annotate the perpetually NotExecuted `Checkout_SavedAddress_CanBeUsed`. Needs confirmation why it never runs.

---

## 5. Critical user journeys to cover with E2E (ordered)

These are ordered by revenue/compliance impact, informed by the untested-flow findings in `_review/bugs.md` and `_review/product.md`. Items 1–3 are blocked on the P0 bug fixes landing first — write the tests as part of those fixes (they are the acceptance criteria).

1. **Card purchase end-to-end:** browse → add to cart → checkout → Stripe test card in the Elements iframe → confirmation → order appears in `/account/orders` and **transitions out of Pending** via webhook. (Currently zero coverage; the only completed-order E2E deliberately picks bank transfer.)
2. **Price integrity invariant:** for a seeded on-sale product, the PDP/cart/checkout displayed total == Stripe charge amount == `Order.Total`, in one currency. (Guards against regression of `_review/bugs.md` #2/#6/#11.)
3. **Guest cart → login → merged cart → purchase.** (The single most common first-customer path; today it silently loses the cart.)
4. **Order fulfillment loop:** admin sets order Shipped + tracking → customer sees it in order details. (Blocked on the admin orders UI, `_review/product.md` #2 — until then cover at integration level via `AdminOrdersController`.)
5. **Password reset:** request → email (MailHog in shared infra) → reset link → login with new password. (Blocked on the email send being wired, `_review/bugs.md` #7.)
6. **Order cancel with restock** (exists) extended to assert refund behavior once cancel-refund is implemented.
7. **GDPR self-service** export/delete — only if/when a UI is added; until then integration tests are the right layer (no frontend calls these endpoints today).
8. **Wishlist share link + guest merge** — already written on this branch; needs a recorded green run.
9. Existing strong suites to preserve: language switching, theming, axe-core accessibility, search/filtering, reviews/Q&A.

---

## 6. Testing tools & workflow improvements

### Coverage measurement (currently nonexistent)

- **Backend:** add a `coverage.runsettings`; run `dotnet test --collect:"XPlat Code Coverage"` for all three unit/integration projects (coverlet.collector is already referenced in every csproj); aggregate with `reportgenerator` and gate via a threshold step (or `coverlet.msbuild /p:Threshold=80` on Application + Core).
- **Frontend:** create `karma.conf.js` (none exists anywhere) with `coverageReporter.check` thresholds (lines/statements 70) and run `ng test --code-coverage` in CI.
- **Rollout:** report-only for one sprint to baseline, then enforce 80/70. Make the codecov step actually able to fail, or replace it. Whether codecov is configured at all on the GitHub repo is **Needs confirmation**.

### Flake control

- **Cleanup observability:** `TestDataFactory.CleanupAsync` (`Infrastructure/TestDataFactory.cs:320-330`) swallows failures with `catch { /* ignore */ }` — log the correlationId at minimum, and add a CI post-step calling the existing bulk cleanup endpoint `/api/test/cleanup/{correlationId}`.
- **Parallelism:** split the single `[Collection("Playwright")]` into 2–3 collections by area with separate browser contexts (target ~40% wall-time reduction; the serial 30-min job is the future merge bottleneck).
- **Retries:** none inside test logic; use a CI rerun-failed step for known network-sensitive assertions only.
- **Readiness:** the 74-failure → green-in-10-minutes TRX pair shows local runs start before servers are ready; CI already health-gates — add a local helper script that waits on `/health` before invoking `dotnet test tests/ClimaSite.E2E`.
- **No silent skips:** any NotExecuted test must carry an explicit `[Fact(Skip="...")]`.

### Test data

- Keep the `TestDataFactory` + correlation-ID + env-gated `TestController` design — it is the right architecture and is already proven.
- E2E must keep using Testcontainers-style isolation in integration and never the shared dev infra (per the user's global rules; already true today).
- Hygiene: remove the unused `postgres:16-alpine` service container from the CI integration job (Testcontainers only needs Docker); delete `UnitTest1.cs`.

### Workflow / documentation

- **Rewrite CLAUDE.md's testing sections** with the §1.3 commands; same fix in root `AGENTS.md` and `tests/ClimaSite.E2E/AGENTS.md` COMMANDS block. This is the highest-leverage testing fix in the repo because agents follow these docs literally.
- **Solution filter:** add `ClimaSite.NoE2E.slnf` (or per-project `dotnet test` in docs) so a routine "run the tests" never sucks in the E2E project.
- **CI as source of truth:** prefer opening PRs (full pipeline runs) over local TRX snapshots as merge evidence; protect `main` with required checks (devops backlog) so green CI is actually load-bearing.
- Where `docs/plans/18-project-completion.md` lists remaining test work, it remains valid; this document supersedes the *testing commands and stack description* in CLAUDE.md, which are stale.

---

## 7. Prioritized testing backlog

| ID | P | Item | Concrete target | Effort |
|---|---|---|---|---|
| TS-01 | P0 | Re-run + record the wishlist Api integration (Docker) and E2E suites for `feature/plan18-wishlist-completion` — or open the PR and let CI run | `WishlistControllerTests.cs`, `Tests/Wishlist/` | S |
| TS-02 | P0 | Fix testing docs: corrected commands (§1.3) into `CLAUDE.md`, `AGENTS.md`, `tests/ClimaSite.E2E/AGENTS.md`; add solution filter so root `dotnet test` stops including E2E | docs + `ClimaSite.NoE2E.slnf` | S |
| TS-03 | P0 | Regression tests bundled with the three P0 bug fixes: order-create persists `paymentIntentId` (integration), webhook → Paid transition (integration), cart merge with the real FE request shape + guest→login merge E2E, displayed==charged==recorded total assertion | `OrdersController`, `WebhooksController`, `CartTests.cs` | M |
| TS-04 | P1 | Stripe card E2E (FrameLocator + test card) + Payments/Webhooks integration tests (signature rejection, intent→order) | `CheckoutPage.FillPaymentDetailsAsync` (currently dead), CI Stripe test keys | M |
| TS-05 | P1 | GDPR integration tests: export completeness, delete anonymization + idempotency + authz | `GdprController`, `DeleteUserDataCommand` | M |
| TS-06 | P1 | Integration wave 1: Orders, Payments, Webhooks, Gdpr controllers (happy + 401/403) | `tests/ClimaSite.Api.Tests/Controllers/` | M |
| TS-07 | P1 | Coverage measurement + thresholds: coverlet runsettings (backend 80) + new karma.conf.js (frontend 70); baseline first, then enforce; fix `continue-on-error` codecov step | `.github/workflows/test.yml`, `angular.json` | M |
| TS-08 | P1 | Application unit tests: Cart (merge/quantities/stock-cap), Inventory (oversell — pairs with concurrency fix), Reviews (verified purchase), plus shared sale-price mapper test | `tests/ClimaSite.Application.Tests/Features/` | L |
| TS-09 | P2 | Frontend specs: auth.interceptor (concurrent-401 single refresh), auth.guard, payment.service, checkout.component (double-submit), cart.component, register | colocated `*.spec.ts` | M |
| TS-10 | P2 | Integration wave 2: Reviews, Questions, Notifications, Inventory, Addresses | `tests/ClimaSite.Api.Tests/Controllers/` | M |
| TS-11 | P2 | E2E reliability: log cleanup failures, CI bulk-cleanup post-step, split into 2–3 collections, explain/skip-annotate `Checkout_SavedAddress_CanBeUsed`, local server-readiness helper | `TestDataFactory.cs`, `PlaywrightFixture.cs` | M |
| TS-12 | P2 | CI gates: `ng lint` job, `-warnaserror`/analyzers, serve built e2e bundle (not dev server), fix `develop` trigger + add `workflow_dispatch`; (with devops) protect `main` with required checks | `.github/workflows/test.yml` | S |
| TS-13 | P2 | Fix tautological guest-checkout E2E once the guest-checkout product decision lands | `CheckoutTests.cs` | S |
| TS-14 | P3 | Integration wave 3: Admin*, Brands, Categories, Promotions, PriceHistory, Installation | `tests/ClimaSite.Api.Tests/Controllers/` | L |
| TS-15 | P3 | Remaining frontend specs (admin components, product-card/list, animation services) | colocated specs | M |
| TS-16 | P3 | Hygiene: delete `UnitTest1.cs`; remove unused CI postgres service; use-or-delete `FillPaymentDetailsAsync`; delete dead `Features/Auth/*` duplicate + its tests | various | S |
| TS-17 | P3 | CI migration job: apply all migrations to a clean Postgres (+ roll back latest) | new CI job | S |

### Open questions (Needs confirmation)

1. Why is `Checkout_SavedAddress_CanBeUsed` NotExecuted in every recorded run (no `[Fact(Skip)]` exists)?
2. Is codecov actually configured (token/status checks) on `byrzohod/climasite`, or is the upload step dead?
3. Current frontend pass rate — no persisted Karma output; run `npm test` to baseline.
4. Stripe test-mode keys availability for CI (prerequisite for TS-04).
