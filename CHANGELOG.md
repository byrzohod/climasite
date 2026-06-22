# Changelog

All notable changes to ClimaSite are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Plan 18 — Project Completion master plan (`docs/plans/18-project-completion.md`) consolidating the final 9-phase path to production readiness.
- ADR 001 — Home page v3 concept decision (Configurator-First selected).
- ADR 002 — Home v3 stack, assets, and build order (Three.js latest, procedural geometry, rules-based scoring, backend-first).
- ADR index and template at `docs/adr/README.md` and `docs/adr/000-template.md`.
- Home v3 concept proposals and interactive HTML mock at `docs/concepts/home-v3/`.
- Gap audit report at `docs/audit/2026-04-08-gap-report.md`.
- Home v3 unit and E2E coverage for recommendations, translations, reduced motion, responsive viewports, keyboard navigation, and route behavior. (HOME-008, HOME-009, HOME-011)
- SDLC hardening (PROC-01) Waves 0–1: pinned read-only `reviewer`/`verifier`/`security` + `qa`/`developer` agents and the `verify-work`/`perf-budget`/`accessibility-audit`/`refactor`/`skill-verifier` skills into `.claude/`; added the gated per-feature pipeline (`docs/features/_template/*`, `docs/features/README.md`) with the `/feature-kickoff` and `/verify-plan` skills and the CLAUDE.md front-phase rules; declared `DEV_WORKFLOW.md` canonical and fixed the workflow doc drift.
- SDLC hardening (PROC-01) Wave 3b (batch 3): 345 more backend unit tests to cross the 80% line — Application handlers (Cart, Questions, Reviews, installation options, invoice generation), 225 Core domain-entity tests (Core 73.7% → 81.25%), and a `MockDbContext` enhancement (FindAsync + async `.AsQueryable()`) that unlocked the previously-skipped list-query handlers (GetNotifications/GetAdminCustomers/GetAdminOrders) and Category Update/Delete branches. Application.Tests 693 → 813; Core.Tests 199 → 424.
- SDLC hardening (PROC-01) Wave 3b (batch 2): 224 more Application-layer unit tests — Categories, Brands, Promotions, Notifications, **GDPR export/delete** (P0 business rule), **Inventory** stock adjust/threshold (P1), and Products-public queries + Translations + price history. Application.Tests: 469 → 693 passing; backend coverage continues climbing toward the 80% gate.
- SDLC hardening (PROC-01) Wave 3b (batch 1): 181 new Application-layer unit tests across previously 0%-covered handlers — Addresses, Admin Products/Customers/Dashboard/Orders/RelatedProducts, and the remaining Auth handlers (ChangePassword/ConfirmEmail/GetUserById/UpdateProfile). Application.Tests: 288 → 469 passing. Lifts the real backend coverage toward the 80% gate (Wave 3c).
- SDLC hardening (PROC-01) Wave 3a: CI hard gates — `Lint & Format` (root `.editorconfig` + `dotnet format --verify-no-changes` + `ng lint`), `Dependency Audit` (.NET `--vulnerable` + Trivy CRITICAL enforced; gitleaks + `npm audit` informational pending SEC-13/SEC-12), `ADR Gate` (filename + immutable-decision checks), and `Test-Design Coverage Lint` (every `automated` scenario names an existing test) — all enforced through the required `Test Summary` check. Added a non-enforcing `Coverage Report` job (backend incl. integration + frontend) to establish the baseline before the 80/70 gate (Wave 3c).
- SDLC hardening (PROC-01) Wave 2: phase-aware Claude Code hooks — SessionStart phase reminder, a `src/`-edit "approved-plan" gate (escape `CLIMASITE_HOTFIX=1`), a commit-time "tests-with-feature" gate (escape `[no-tests]`), a PostToolUse test-ran marker, and a Stop-hook "refuse to finish untested". Shipped in non-blocking **warn mode** behind `.claude/hooks/gate-mode`; flipping to `block` is a separate owner-gated change. Existing git/secret hooks preserved.

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
