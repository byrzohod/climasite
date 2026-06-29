# Changelog

All notable changes to ClimaSite are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **SEC-05 / B-011 — removed the committed JWT signing secret + centralized token issuance (security, auth).** The committed placeholder key in `appsettings.json` is gone; `JwtConfiguration.ResolveSecret` now **requires a real `JWT_SECRET` in every environment except Development/Testing** (was: only literal `Production` — so Staging/QA/preview/typo'd names silently signed with the public repo key = token forgery / admin escalation), **rejects the committed placeholder + whitespace + `<32`-char secrets in all environments**, and returns a labelled non-deployable Dev/Test-only fallback so those envs still boot out-of-the-box. The secret is resolved once at startup into a single `JwtOptions` used by **both** bearer validation and `TokenService`, eliminating a latent issuer/audience divergence. All access-token issuance now flows through `TokenService` (the three duplicated handler-local `GenerateAccessToken` copies in Login/Refresh/GoogleSignIn are deleted); the token's live claim set (`NameIdentifier`/`Email`/`Jti`/`firstName`/`lastName`/`Role`) is preserved (the frontend reads the login response body, not JWT claims). **Proven LIVE**: a `Staging` boot without `JWT_SECRET` now fails fast at startup, while a Development login issues a token that authenticates a `[Authorize]` endpoint and admin authorization works (`/acceptance` PASS at `.planning/acceptance/SEC-05-jwt-secret.md`). 25 `JwtConfigurationTests` cases (incl. casing/`Productionn`/whitespace/placeholder/`<32` fail-fast + a mutation gate) + `TokenServiceTests` + end-to-end `JwtIssuanceValidationTests`. Design + final diff cross-vendor-councilled (Codex `gpt-5.5`@`xhigh` + Claude security) clean. No frontend change; no DB migration. Refresh-token hashing (SEC-09/B-040) remains a tracked follow-up. Unit-plan at `.planning/units/SEC-05-jwt-secret/unit-plan.md`.
- **PAY-IDEM — Stripe idempotency keys on the create/charge path.** `POST /api/payments/create-intent` now accepts a **client-supplied per-attempt idempotency key** (the checkout component generates a fresh `crypto.randomUUID()` per place-order attempt — with a `getRandomValues`/time fallback for non-secure contexts) which is validated server-side (`[A-Za-z0-9_-]{8,200}`), namespaced `ci_<key>`, and forwarded to Stripe as `RequestOptions.IdempotencyKey`; refunds carry a deterministic server-derived `re_v1_<sha256(intentId)>` key. So a network retry of a single create-intent POST dedupes to one PaymentIntent, while a genuine new attempt (a fresh click after a failure) gets a fresh intent — closing a [High] money-loss replay path a cross-vendor council caught in an earlier cart-state-key design (the rejected design replayed a refunded intent for 24h). Also: `StripeConfiguration.MaxNetworkRetries=2` set once at the composition root; shipping-method canonicalized in the Stripe metadata; the idempotency key added to the `LogSanitizer` redaction set; and (B-061) a `CancellationToken` threaded through create-intent for symmetry with refund. **Proven LIVE** against real Stripe test mode: same key → identical `pi_…` (Stripe dedup), different key → different `pi_…` (`/acceptance` PASS at `.planning/acceptance/PAY-IDEM-stripe-keys.md`). Backend + frontend; no DB migration. Design + final diff cross-vendor-councilled (Codex `gpt-5.5`@`xhigh`) clean; unit-plan at `.planning/units/PAY-IDEM-stripe-keys/unit-plan.md`. Implements the DEC-PAYMENT idempotency follow-up.
- **Plan-19 B3 — frontend spec coverage.** Added meaningful unit specs for the 28 remaining untested frontend files (21 components + 6 animation directives + the spec-key pipe): auth forgot/reset-password, about/account/settings, brand/category/promotion pages, shared widgets (glass-card, mini-cart-item, trust badges, category-header, skeletons, product-consumables, similar-products), the reveal/count-up/scroll/magnetic/split-text/animate-on-scroll directives, and `spec-key.pipe`. Real assertions (inputs→DOM, outputs, signals, conditional logic, directive host effects, reduced-motion/SSR guards). Frontend suite 1734 green. Surfaced + fixed a latent null-safety crash in `category-header` (rendering a null category dereferenced null); flagged two `split-text.directive` behavior bugs for a follow-up.
- **UX-16 — transitional dual EUR/BGN price display.** New shared pure pipe `DualPricePipe` (`| dualPrice`) renders `€X.XX / Y.YY лв` from one EUR amount at the fixed peg 1 EUR = 1.95583 BGN (Bulgaria's euro-adoption dual display). Replaced all 78 static `currency:'EUR'` store-price renders (+ 2 missed non-pipe patterns, one of which fixed a latent `$`-symbol bug in installation-service) with it across the catalog/cart/checkout/admin-KPI/shared widgets; dynamic order-currency renders (which show the order's own charged currency) are unchanged. Pipe unit-tested (peg math, grouping, negatives, null/zero); 1393 frontend tests green. Resolves UX-16.
- **SEARCH-01-fts — Postgres full-text product search** (replaces the unindexed multi-ILIKE search on both public paths). A trigger-maintained denormalised `products.search_vector` (base fields + tags + ALL translations in one doc), a single `climasite_search` config (`simple` + `unaccent`, diacritic-folding for EN/BG/DE), GIN + pg_trgm indexes. One shared `IProductSearchService` (parameterized raw query) does match + facets + `ts_rank_cd` relevance + exact-SKU boost + paging + window-count total; a per-term substring branch keeps recall a **strict superset** of the old ILIKE search (description/tags/translations included), and the user-facing header search (`GET /api/products?searchTerm=`, previously name/desc/brand/model only) now gets tags/translations/SKU/ranking. 12 real-Postgres FTS integration tests + a verified rank-reversal mutation gate. Design ratified by owner + cross-vendor council (Codex + plan-critic); unit-plan at `.planning/units/SEARCH-01-fts/unit-plan.md`. Resolves **DEC-SEARCH**.
- Plan 18 — Project Completion master plan (`docs/plans/18-project-completion.md`) consolidating the final 9-phase path to production readiness.
- ADR 001 — Home page v3 concept decision (Configurator-First selected).
- ADR 002 — Home v3 stack, assets, and build order (Three.js latest, procedural geometry, rules-based scoring, backend-first).
- ADR index and template at `docs/adr/README.md` and `docs/adr/000-template.md`.
- Home v3 concept proposals and interactive HTML mock at `docs/concepts/home-v3/`.
- Gap audit report at `docs/audit/2026-04-08-gap-report.md`.
- Home v3 unit and E2E coverage for recommendations, translations, reduced motion, responsive viewports, keyboard navigation, and route behavior. (HOME-008, HOME-009, HOME-011)
- SDLC hardening (PROC-01) Waves 0–1: pinned read-only `reviewer`/`verifier`/`security` + `qa`/`developer` agents and the `verify-work`/`perf-budget`/`accessibility-audit`/`refactor`/`skill-verifier` skills into `.claude/`; added the gated per-feature pipeline (`docs/features/_template/*`, `docs/features/README.md`) with the `/feature-kickoff` and `/verify-plan` skills and the CLAUDE.md front-phase rules; declared `DEV_WORKFLOW.md` canonical and fixed the workflow doc drift.
- Adopted the latest vault AI workflow — Phase B (merge queue): `test.yml` now triggers on `merge_group` and `.github/rulesets/trunk-default-branch-protection.json` defines the trunk ruleset (merge queue + 0-approval PR [AI auto-merge kept] + required `Test Summary` + linear-history/non-fast-forward/no-deletion). **Deferred / not applied** — GitHub's merge queue and `evaluate`-mode rulesets both require a paid plan (Team/Enterprise), which this personal public repo isn't on; the apply returned HTTP 422. The existing classic branch protection remains the gate and the `merge_group` trigger is inert-but-ready. Tracked as OPS-11; apply procedure in `docs/runbooks/merge-queue.md`.
- Adopted the latest vault AI workflow (`/project-adopt`): upgraded `.claude/` from the Wave-0 partial install to the current template — 23 agents (+18), 43 skills (+27 new, 11 updated), 6 orchestration scripts, and 4 new hooks (no-spec-no-code, skill-quarantine, state-prime, pre-compact-checkpoint) merged with the PROC-01 hooks at (event,matcher,command) granularity. Mapped the codebase into a vault Knowledge graph (`Knowledge/climasite/` — 9 components, 8 open risks, 6 open questions) and seeded `.planning/` (STATE.md resume contract, reconstructed DESIGN.md, PATHS.md). Reversible checkpoint `pre-adopt-backup-20260623-180552`. (CI merge-queue restructure + trunk ruleset = follow-up Phase B.)
- SDLC hardening (PROC-01) Wave 6b: real Stripe card-payment E2E (`CardPaymentE2ETests` — 4242 happy path to confirmation + declined card) wired to run in CI once `STRIPE_SECRET_KEY`/`STRIPE_PUBLISHABLE_KEY` (Stripe TEST keys) are added as Actions secrets; it self-skips when Stripe isn't configured (the shipped dummy key), so it never blocks. Closes the P0 "money path never E2E-tested" gap once secrets are set.
- SDLC hardening (PROC-01) Wave 6a: 163 real-infra integration tests (Testcontainers, no mocking) across the 17 previously-0%-covered Api controllers — Admin (Products/Categories/Questions/Reviews/Orders/Customers/Dashboard), Addresses, Notifications, GDPR, Brands, Categories, Promotions, PriceHistory, Questions, and the Stripe webhook signature-rejection path. Api.Tests 109 → 272. These tests surfaced BUG-26 (GDPR deletion).
- SDLC hardening (PROC-01) Wave 5d: ephemeral visual-snapshot capture (`VisualSnapshotTests`) — full-page screenshots of key public pages across light/dark (+ mobile home), uploaded as a 7-day E2E artifact for AI/human review. No committed baselines (owner decision); it's a review aid, not a pixel-diff gate.
- SDLC hardening (PROC-01) Wave 5c: accessibility (axe-core) matrix E2E across 9 public pages × light/dark (`AxeAccessibilityMatrixTests`, via Deque.AxeCore.Playwright). Reporting-first — logs every serious/critical violation to test output without failing yet (flip with `A11Y_ENFORCE=1`). Baseline found 3 serious dark-theme color-contrast violations (`/promotions`, `/brands`, `/about`) → tracked as UX-15; all other audited pages clean.
- SDLC hardening (PROC-01) Wave 5a: Lighthouse CI perf-budget job — builds the production frontend, runs Lighthouse (mobile preset) against the static bundle, and reports performance/accessibility/best-practices scores against a ≥0.90 budget (`src/ClimaSite.Web/lighthouserc.json`). Reporting-only for now (assertions at "warn", not in Test Summary) to absorb CI Lighthouse variance; flips to enforcing once stable.
- SDLC hardening (PROC-01) Wave 4: review hardening — a `.github/pull_request_template.md` (Summary/Testing/checklist/screenshots/migrations/reviews), `.github/CODEOWNERS`, and a `PR Checklist` CI gate (danger.js-equivalent in `scripts/ci/pr-checklist.sh`) that fails a PR with an empty Summary/Testing section or that touches auth/payment/GDPR without mentioning `/security-review` — wired into the required `Test Summary`. No required human approval (AI auto-merge kept).
- SDLC hardening (PROC-01) Wave 3c: the `Coverage Gate` CI job now **enforces** backend line coverage ≥80% and frontend line coverage ≥70% (generated code excluded), wired into the required `Test Summary` check — a PR that drops below either threshold is blocked. Current levels: backend 85.6%, frontend ~72%.
- SDLC hardening (PROC-01) Wave 3b (batch 3): 345 more backend unit tests to cross the 80% line — Application handlers (Cart, Questions, Reviews, installation options, invoice generation), 225 Core domain-entity tests (Core 73.7% → 81.25%), and a `MockDbContext` enhancement (FindAsync + async `.AsQueryable()`) that unlocked the previously-skipped list-query handlers (GetNotifications/GetAdminCustomers/GetAdminOrders) and Category Update/Delete branches. Application.Tests 693 → 813; Core.Tests 199 → 424.
- SDLC hardening (PROC-01) Wave 3b (batch 2): 224 more Application-layer unit tests — Categories, Brands, Promotions, Notifications, **GDPR export/delete** (P0 business rule), **Inventory** stock adjust/threshold (P1), and Products-public queries + Translations + price history. Application.Tests: 469 → 693 passing; backend coverage continues climbing toward the 80% gate.
- SDLC hardening (PROC-01) Wave 3b (batch 1): 181 new Application-layer unit tests across previously 0%-covered handlers — Addresses, Admin Products/Customers/Dashboard/Orders/RelatedProducts, and the remaining Auth handlers (ChangePassword/ConfirmEmail/GetUserById/UpdateProfile). Application.Tests: 288 → 469 passing. Lifts the real backend coverage toward the 80% gate (Wave 3c).
- SDLC hardening (PROC-01) Wave 3a: CI hard gates — `Lint & Format` (root `.editorconfig` + `dotnet format --verify-no-changes` + `ng lint`), `Dependency Audit` (.NET `--vulnerable` + Trivy CRITICAL enforced; gitleaks + `npm audit` informational pending SEC-13/SEC-12), `ADR Gate` (filename + immutable-decision checks), and `Test-Design Coverage Lint` (every `automated` scenario names an existing test) — all enforced through the required `Test Summary` check. Added a non-enforcing `Coverage Report` job (backend incl. integration + frontend) to establish the baseline before the 80/70 gate (Wave 3c).
- SDLC hardening (PROC-01) Wave 2: phase-aware Claude Code hooks — SessionStart phase reminder, a `src/`-edit "approved-plan" gate (escape `CLIMASITE_HOTFIX=1`), a commit-time "tests-with-feature" gate (escape `[no-tests]`), a PostToolUse test-ran marker, and a Stop-hook "refuse to finish untested". Shipped in non-blocking **warn mode** behind `.claude/hooks/gate-mode`; flipping to `block` is a separate owner-gated change. Existing git/secret hooks preserved.

### Fixed

- **Backend quick-wins hardening batch (B-007, B-008, B-034, B-036, B-055) from the external review.** Five
  small, independent backend fixes shipped together:
  - **B-007** — order confirmation/shipped email CTAs built `/account/orders/$<guid>` (a literal `$` in the
    interpolation) → a 404 link whenever real email is enabled. The order URL is now built by a single
    `EmailService.BuildOrderUrl` helper (no `$`), unit-tested.
  - **B-008** — `ExceptionHandlingMiddleware` echoed a raw `ArgumentException.Message` (and put it in the
    response `detail`), which can leak internal parameter names/state. Unhandled `ArgumentException` now maps
    to a generic `BadRequest "Invalid request"` with null detail; FluentValidation/app `ValidationException`
    still surface their intended user-facing messages. Middleware unit-tested.
  - **B-034** — the anonymous installation-lead endpoint (writes PII + enqueues an outbox email) had no rate
    limit beyond the global 100/min/IP. It now carries `[EnableRateLimiting("strict")]` (5/min/IP, same as the
    contact form). **Proven live**: the 6th rapid POST returns 429.
  - **B-036** — public pagination/count params were unbounded: `pageSize=0` divided by zero in
    `PaginatedList.TotalPages`, a huge `pageNumber` overflowed `(pageNumber-1)*pageSize` into a negative `Skip`
    (a 500), and `pageSize`/`count`=100000 fetched the whole table (a cheap DoS). A new `QueryBounds` helper
    clamps at the API edge (PageNumber 1..100000, PageSize 1..100, Count 1..24) on `GetProducts`/`SearchProducts`
    and the 6 public count endpoints, plus a defensive floor in `PaginatedList` for any direct caller. **Proven
    live**: `?pageSize=100000` returns at most 100 items; `?pageSize=0` and `?pageNumber=2147483647` return 200.
  - **B-055** — `CorrelationIdMiddleware` echoed and log-pushed an inbound `X-Correlation-Id` verbatim with no
    bound (log-forging / oversize). It now honours only `^[A-Za-z0-9._-]{1,128}$`, else generates a fresh GUID.
    **Proven live**: a spaces/`!`-bearing id is replaced with a GUID; a valid id is still echoed.

  Backend only; no DB migration; no i18n; no frontend change. Backend suites green (Application 873 / Core 424 /
  Api integration 392). Cross-vendor Codex council on the combined diff; `/acceptance` PASS at
  `.planning/acceptance/QW-backend-batch.md`. Unit-plan at `.planning/units/QW-backend-batch/unit-plan.md`.
- **B-002 / BUG-06 (admin slice) — admin product list inverted current vs compare-at price.** The admin
  Product Management list showed an on-sale product's **higher compare-at price prominently** (as if it were the
  live price) and the **real selling price struck-through** — the exact inverse of reality, so admins made
  merchandising/discount decisions off a wrong screen. Two compounding defects, both fixed: (1) `GetAdminProductsQuery`
  mapped `SalePrice = p.CompareAtPrice` **raw** (non-null even when not on sale → fake sale); it now uses the shared
  `ProductPricing.GetSalePrice(BasePrice, CompareAtPrice)` contract (null unless `CompareAt > Base`), identical to
  the public `GetProductsQueryHandler`; (2) the admin list template rendered `salePrice` (original) prominent and
  `price` (current) struck — now `product.price` (current) is prominent and `product.salePrice` (original) is
  struck-through, matching the convention used by every other price renderer in the app. **Proven LIVE** against the
  running stack (real admin API + freshly-rebuilt `ng serve`): the on-sale row shows €499.99 (current) prominent and
  €599.99 (original) struck in light AND dark; not-on-sale rows show a single price, no fake sale (`/acceptance` PASS
  at `.planning/acceptance/B-002-admin-price-inversion.md`). Backend `GetAdminProductsQueryHandlerTests` (mutation-proven:
  the `CompareAt ≤ Base` Theory is the real guard) + a frontend inversion-guard DOM spec; full FE suite 1742 green.
  Cross-vendor Codex council (`gpt-5.5`@`xhigh`) on the diff: 0 findings. Unit-plan at
  `.planning/units/B-002-admin-price-inversion/unit-plan.md`. No DB migration; no i18n change.
- E2E stability: raised the page-objects' tight 10s Playwright waits to the suite's 30s default (113 call sites) — slow CI runners were intermittently tripping them (e.g. order/overlay pages taking ~12s), causing flaky required-check failures that forced re-runs. Longer patience only; no test-logic change.
- GDPR account deletion (Article 17 right-to-erasure) now works: `DeleteUserDataCommandHandler` ran a manual `BeginTransactionAsync` that threw under the `NpgsqlRetryingExecutionStrategy` (`EnableRetryOnFailure`), so logged-in users could not delete their account (silent 400). The deletion now runs inside `Database.CreateExecutionStrategy().ExecuteAsync(...)`. Found by the new Wave 6 integration tests (BUG-26).

### Changed

- Home page replaced by Home v3 with a configurator-first wizard, live room preview, real product recommendations, translated trust/category sections, and deferred below-fold content. (HOME-004..HOME-012)
- Production frontend API configuration now uses relative `/api` calls so deployed frontend traffic goes through the backend proxy instead of a cross-origin API URL.
- Authentication restore now retries `/me` after refresh when a persisted access token is expired, preventing valid refresh sessions from being dropped.
- Main layout now renders a lightweight global navigation shell immediately, while the heavier header/footer chunks load after the initial paint.
- `main` is now branch-protected (OPS-02): pull requests are required, all six CI checks (Unit, Integration, Frontend, Build, E2E, Test Summary) must pass, force-pushes and deletions are blocked, and conversation resolution is required. CLAUDE.md's post-implementation workflow now mandates the feature-branch → PR → green-CI → merge flow instead of pushing directly to `main`.

### Removed

- Legacy `features/home/` and its scroll-driven v2 implementation; superseded by `features/home-v3/` per ADR 001.

### Fixed

- **Payment money path (BUG-02 / BUG-01 / BUG-18):** the Stripe charge is now computed **server-side in EUR** from the cart + shipping method (a shared `CheckoutPricing` helper keeps displayed == charged == order total); the order persists `paymentIntentId` only after server-side verification that the intent **succeeded** in **EUR** for the **exact order total** (closing the €0.01 / underpayment exploit); a filtered unique index on `payment_intent_id` makes order creation idempotent; and the Stripe webhook returns a retryable 404 for an early `payment_intent.succeeded` with no order yet (other events are acknowledged). A card order without a verified intent is now rejected.
- Guest-cart merge on login no longer 400s (BUG-03): the frontend sends `?guestSessionId=` to match the `[FromQuery]` endpoint, so first-time customers keep their cart after logging in.
- Corrected the agent-facing docs (CLAUDE.md, root + E2E `AGENTS.md`) so every command block runs as written (DOC-01): real per-project test commands, the C# Playwright-for-.NET E2E flow (`dotnet test tests/ClimaSite.E2E`), API port **5029**, a new `ClimaSite.NoE2E.slnf` solution filter, EF Core **10.x**, Tailwind in the stack table, `TestDataFactory.cs`, and `?lang=` instead of `Accept-Language`.
- Admin and root route empty-path redirects now use explicit `pathMatch: 'full'`.
- Home recommendations and product browsing E2E tests now wait on real API responses instead of brittle DOM timing.
- Missing Home v3 translation keys added across EN/BG/DE.

### Security

- Forwarded headers are now honored before the rate limiter (SEC-03): behind the nginx reverse proxy the limiter partitions per real client IP instead of lumping every shopper into one global 100 req/min bucket.
- Password reset now actually sends the reset email and **never logs the token** (BUG-07): the handler dispatches the email best-effort (a delivery failure neither 500s the request nor reveals whether the account exists), and the reset link carries the required `&email=` parameter.
- DataSeeder is now environment-gated (SEC-01): the well-known default admin (`admin@climasite.local` / `Admin123!`) and the demo catalog are seeded **only** in Development/Testing. In Production/Staging the first admin is bootstrapped from the `ADMIN_EMAIL` / `ADMIN_INITIAL_PASSWORD` environment variables, and startup fails fast if no admin exists and no bootstrap credentials are provided.
- Production startup now requires `JWT_SECRET` instead of falling back to the committed development JWT secret.
- Test-only admin setup secret defaults are restricted to Development; Testing must configure `TestSettings:AdminSecret`.

---

## Guidance for future entries

- Group changes under: Added, Changed, Deprecated, Removed, Fixed, Security.
- One bullet per user-visible or developer-visible change. No marketing copy.
- Link to the plan task ID in parens when relevant, e.g. `(HOME-005)`.
- Roll `[Unreleased]` into a dated release section on every tagged release; `/release` skill handles the mechanics.
