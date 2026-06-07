# Plan 18 — Project Completion & Production Readiness

**Status:** In progress — Phase 1 Home v3 complete
**Owner:** sarkisharalampiev
**Created:** 2026-04-08
**Gap report:** [`docs/audit/2026-04-08-gap-report.md`](../audit/2026-04-08-gap-report.md)
**Supersedes / closes:** partial work in plans 12 (Notifications), 13 (Wishlist), 21F (Animation), and all previous home-page plans (19, 21A, 23, 24, 25).

---

## 0. Guardrails (read before starting any task)

These derive from `CLAUDE.md` and `.auto-memory/` and are non-negotiable for every task in this plan.

1. Tests are written **with** the feature, never deferred. Every phase that ships code ships tests in the same PR.
2. No mocking in UI / E2E tests. Real DB (testcontainers), real backend, real services, human-like UI interaction (`click`, `fill`, no `page.goto` to skip flows, no seeded-DB shortcuts).
3. Conventional commits only. Feature branches only. Never push to main.
4. Visual verification before declaring any UI task done: Playwright screenshots desktop + mobile + dark mode, read them, fix, only then deliver.
5. ADR for every non-obvious decision — `docs/adr/NNN-title.md`.
6. Update `.auto-memory/`, `CLAUDE.md`, and this plan after every iteration.
7. Never run destructive git ops without explicit approval.
8. No silent stack picks. The user decides framework / DB / library. When this plan demands a new library (WebGL, OpenTelemetry exporter, email provider), surface it as a question, don't guess.
9. Color tokens only — no new hardcoded hex/rgba, ever. i18n keys only — no new hardcoded user-facing strings, ever.
10. Batch clarifying questions 5–15 at a time, max 2–3 rounds.

---

## 1. Phase overview

| # | Phase | Goal | Exit criteria |
|---|-------|------|---------------|
| 0 | Foundation & Cleanup | Branch, ADR scaffold, CHANGELOG, wipe old home cleanly | New branch exists; `docs/adr/` + `CHANGELOG.md` exist; `features/home/` and its exclusive services/translations/routes deleted; tests still build |
| 1 | Home v3 — Interactive Product Showcase | Ship a brand-new homepage built from scratch | Picked concept implemented; desktop+mobile+dark screenshots reviewed; E2E tests green; i18n + theme tokens 100% |
| 2 | Complete partial features | Finish Notifications, Wishlist, Animation Audit 21F | All remaining NOT-*, WISH-*, 21F-* tasks done; tests for each; CLAUDE.md status table flips to Complete |
| 3 | i18n / theming / dark-mode hardening | Close the 104 color + 33 i18n violations | Zero hardcoded colors outside `_colors.scss`; zero hardcoded user-facing strings; all 3 languages and both themes verified on every route |
| 4 | Test coverage gap closure | Hit 80% BE / 70% FE / 100% endpoints / all flows E2E | Coverage gates in CI updated and passing; missing E2E flows (Notifications, Q&A, Inventory, Search, Brands/Categories) added |
| 5 | Security hardening | Remove secrets, lock down headers, CORS, rate limits, Swagger | JWT + Stripe secrets env-only; security-headers middleware added; Swagger gated; CORS tight; rate limits on all sensitive endpoints; `/security-review` skill pass clean |
| 6 | Performance & accessibility | OnPush adoption, template extraction, CWV, a11y keyboard + ARIA | Lighthouse ≥ 90 all categories on / , /products, /product/:id, /cart, /checkout; axe-core zero serious violations in E2E |
| 7 | Production readiness | CD pipeline, observability, Docker hardening, graceful shutdown, migrations strategy | Railway auto-deploy on main tag; OpenTelemetry traces/metrics/logs; non-root Docker + healthchecks; graceful shutdown hooks; `/deploy-checklist` passes |
| 8 | Documentation & Release | ADRs for every non-obvious decision, CHANGELOG, release notes, v1.0.0 tag | ≥ 8 ADRs written; CHANGELOG populated; `/release` skill produces v1.0.0 artifacts |

**Target completion ordering rationale:** Cleanup first so no new work inherits debt. Home first because it's the user's top request and the biggest visible change. Then partials (finish what's started) before hardening (don't harden moving targets). Tests before security because security fixes need tests to verify they don't regress. Perf / a11y before prod because they affect how you instrument. Prod last because it depends on everything upstream.

---

## 2. Phase 0 — Foundation & Cleanup

> **Status (2026-04-08):** All Phase 0 tasks complete on branch `feature/project-completion-plan-18`. Old home component (2,219 lines) and 5 home-only shared components deleted; ADR scaffolding, CHANGELOG, and plan archive in place.

### FND-001 — Create working branch — **DONE 2026-04-08**
**Branch:** `feature/project-completion-plan-18` (forked from `main` at `ff48fe4`).

### FND-002 — Scaffold ADR directory & index — **DONE 2026-04-08**
`docs/adr/README.md` + `000-template.md` created; ADRs 001 and 002 both Accepted.

### FND-003 — Create `CHANGELOG.md` (Keep a Changelog format) — **DONE 2026-04-08**
`CHANGELOG.md` at repo root with Unreleased section populated for Plan 18 work.

### FND-004 — Wipe old home page (clean slate) — **DONE 2026-04-08**
Deleted: `features/home/home.component.ts` (2,219 lines), 5 home-only shared components (`stats-counter`, `testimonials`, `how-it-works`, `brand-carousel`, `final-cta`), the entire `home.*` i18n key tree across en/bg/de. Verified no non-home imports of any deleted files. `app.routes.ts` rewired to load `HomeV3Component` from `features/home-v3/` (not a placeholder — the real implementation, see HOME-004 below). `ng build --configuration=development` passes.
Delete:
- `src/ClimaSite.Web/src/app/features/home/**`
- `src/ClimaSite.Web/src/app/core/services/scroll-video.service.ts`
- `src/ClimaSite.Web/src/app/core/services/scroll-trigger.service.ts`
- `src/ClimaSite.Web/src/app/shared/directives/scroll-video.directive.ts`
- Home-exclusive home.* keys in `assets/i18n/{en,bg,de}.json`
- `tests/ClimaSite.E2E/PageObjects/HomePage.cs` (will be rebuilt in Phase 1)

Update `app.routes.ts` to temporarily route `''` to a `<ComingSoonComponent>` placeholder in `features/home-v3/` so the app still builds and runs.

**Safety checks BEFORE deletion:**
- grep whole repo for `ScrollVideoService`, `ScrollTriggerService`, `scroll-video.directive` to confirm zero non-home imports.
- grep whole repo for `home\.` translation keys to confirm no other feature consumes them.
- confirm `RevealDirective`, `ProductService` remain untouched.

**Acceptance:** `ng build` passes, `dotnet build` passes, app starts, `/` serves placeholder, all E2E suites except Home still green.

### FND-005 — Update CLAUDE.md status table — **DONE 2026-04-08**
Status table now lists Home v3 as in-progress and Plan 18 as the active completion plan.

### FND-006 — Archive superseded home plans — **DONE 2026-04-08**
Plans 19 and 21A moved to `docs/plans/archive/`. (Plans 23/24/25 never landed on `main`; they only existed on the abandoned `feature/immersive-landing-page` branch and were discarded with that branch's uncommitted state.)

---

## 3. Phase 1 — Home v3: Interactive Product Showcase

**Direction (user-selected):** Interactive product showcase — 3D/WebGL hero, configurator teaser, parallax product reveals, showroom vibe.

**Three concepts** are mocked in `docs/concepts/home-v3/`:
- `concept-a-kinetic-showroom.md`
- `concept-b-configurator-first.md`
- `concept-c-comfort-lab.md`

### HOME-001 — Concept approval gate — **DONE 2026-04-08**
User delegated the pick; **Concept B (Configurator-First) selected** and recorded in [`docs/adr/001-home-page-concept.md`](../adr/001-home-page-concept.md) (status: Accepted).
**Rationale summary:** solves the HVAC sizing problem directly, avoids the scroll-pinning failures of v1/v2, strongest fallback story, creates a reusable `/api/products/recommendations` backend endpoint, shortest path to commerce, achievable perf budget. Concepts A and C are archived as future patterns for product-detail 3D and category-page exhibits respectively.

### HOME-002 — Stack decision questions (batched) — **DONE 2026-04-08**
ADR 002 records all four decisions (Three.js latest, procedural geometry, rules-based scoring, backend-first build order). Note: the production renderer ships as Canvas 2D (per HOME-007 reduced-motion fallback) which doubles as the primary path; Three.js will be added in a follow-up if/when WebGL adds value beyond the axonometric view.
Before any code: one batched round of questions covering:
- WebGL library: Three.js (already in toolchain) vs OGL vs raw WebGL vs Babylon
- Physics/animation: GSAP (already in repo) vs Motion One vs Framer Motion (not Angular — CSS/GSAP likely)
- 3D model source: real CAD imports vs stylized low-poly vs procedural geometry
- Asset budget: total page JS budget, total model+texture budget
- Fallback strategy: what non-WebGL users see (reduced-motion, low-end devices, SSR first paint)
- Analytics events to emit from hero interactions

**Acceptance:** decisions captured in ADR 002.

### HOME-003 — Visual design pass — **DONE 2026-04-08**
Single-file mock at `docs/concepts/home-v3/mock-chosen.html` (~860 lines) with the Canvas 2D axonometric renderer; Playwright screenshots in `docs/concepts/home-v3/screenshots/` (desktop/mobile × light/dark + bedroom variant). User-approved.

### HOME-004 — Scaffold `features/home-v3/` module — **DONE 2026-04-08**
Module created with the structure:
```
features/home-v3/
├── home-v3.component.{ts,html,scss}     # container, OnPush
├── components/
│   ├── wizard/                          # area slider + room type + zone picker
│   ├── room-preview/                    # Canvas 2D axonometric renderer
│   └── recommendations/                 # 3-card slab consuming the API
├── services/
│   ├── home-wizard-state.service.{ts,spec.ts}
│   └── product-recommendations.service.ts
└── models/home-v3.models.ts
```
All components are standalone, OnPush, under 300 lines each. Every interactive element has a `data-testid`. All color via `var(--color-*)`. No `document`/`window` outside `effect` callbacks (which only run in browser).

### HOME-005 — Section implementation (per concept) — **DONE 2026-04-08**
Sections delivered: hero (wizard + live preview), recommendation slab (3 cards from real `/api/products/recommendations`), category grid (4 categories), trust strip (4 stats), final CTA. **Backend endpoint** built per ADR 002 — `RecommendationScoringService` with 6-factor weighted scoring, `GetRecommendationsQueryHandler`, `RecommendedProductDto`, `ProductsController.GetRecommendations`. Re-verified on 2026-06-07 with the full backend, frontend, and E2E suites.

### HOME-006 — i18n keys for home v3 — **DONE 2026-04-08**
`homeV3.*` key tree added to en/bg/de in full (wizard, roomType, zone, recommendations, categories, trust, cta, matchReason — ~50 keys per locale). Old `home.*` tree removed.

### HOME-007 — Reduced-motion + low-end fallback — **DONE 2026-04-08**
The `RoomPreviewComponent` reads `prefers-reduced-motion: reduce` once at construction; when set, the airflow animation does not request additional frames after the first paint. The renderer is Canvas 2D with no WebGL feature requirement, so the no-WebGL fallback is the renderer itself. Playwright verification of both modes is folded into HOME-010.

### HOME-008 — Unit tests — **DONE 2026-06-07**
Added and verified focused Jasmine coverage for:
- `HomeV3Component` recommendation scheduling and translation behavior.
- `ProductRecommendationsService` request mapping.
- `WizardComponent` keyboard navigation and roving tabindex behavior.
- `RoomPreviewComponent` reduced-motion behavior.
- `RecommendationsComponent` with real translation loader to catch leaked keys.
- Root/admin route configuration, main layout deferral behavior, and auth refresh-on-restore.

### HOME-009 — E2E tests (real data, no mocks) — **DONE 2026-06-07**
Rebuilt the Home page object and tests under `tests/ClimaSite.E2E/Tests/Home/` using real API calls and `TestDataFactory`. Coverage includes EN/BG/DE landing render, light/dark theme render, 375/768/1440 viewports, real product recommendation loading, CTA routing, reduced-motion behavior, and keyboard navigation.

### HOME-010 — Visual verification pass — **DONE 2026-06-07**
Captured and inspected Playwright screenshots for desktop/mobile and light/dark after the final production build. Browser QA confirmed no visible translation keys, `undefined`, `null`, TODO/debug output, console errors, or product recommendation network failures.

### HOME-011 — Accessibility pass — **DONE 2026-06-07**
E2E accessibility coverage verifies the new home route, reduced motion, semantic labels, and keyboard navigation for the wizard/radio groups. Manual browser QA also checked immediate global navigation availability after the performance deferral changes.

### HOME-012 — Performance budget verification — **DONE 2026-06-07**
Final Lighthouse results on `/` after the production build:
- Mobile: Performance 0.97, FCP 1.824s, LCP 2.296s, CLS 0, TBT 0.
- Desktop: Performance 1.00, FCP 0.449s, LCP 0.576s, CLS 0.000057, TBT 0.

### HOME-013 — Documentation — **DONE 2026-06-07**
Updated `CLAUDE.md`, this plan, ADR 002 implementation notes, `CHANGELOG.md`, and `.codex/PROJECT_MEMORY.md`. No `.auto-memory/` directory exists in this checkout; `.codex/PROJECT_MEMORY.md` is the GPT-compatible shared memory file.

---

## 4. Phase 2 — Complete partial features

### Notifications (NOT-*)

- **NOT-100** — Questions round: email provider (SES vs Postmark vs SendGrid vs SMTP), template engine (Razor vs Scriban vs Fluid), background worker (Quartz vs Hangfire vs hosted service).
- **NOT-101** — Email service abstraction + provider implementation.
- **NOT-102** — Template engine + templates for OrderPlaced, OrderShipped, OrderDelivered, PasswordReset, LowStockAlert, WelcomeEmail.
- **NOT-103** — Domain event handlers dispatching to notifications.
- **NOT-104** — Preferences API (`/api/account/notification-preferences`).
- **NOT-105** — Background worker for queued notifications + retry/dead-letter.
- **NOT-106** — Frontend: `NotificationService`, bell component, dropdown, notifications center page, preferences UI.
- **NOT-107** — i18n keys in all 3 languages for all template copy + UI strings.
- **NOT-108** — Unit tests for every handler + service.
- **NOT-109** — Integration tests for `NotificationsController`.
- **NOT-110** — E2E: notification preference change, trigger order → observe email + in-app bell updates.
- **NOT-111** — Update CLAUDE.md, plan 12 → archived.

### Wishlist (WISH-*)

- **WISH-100** — Backend service layer + validators.
- **WISH-101** — Backend unit tests for all handlers.
- **WISH-102** — Integration tests for `WishlistController`.
- **WISH-103** — Frontend: `WishlistService` (already partially present — finish it), `WishlistButtonComponent`, `WishlistPageComponent`, share flow.
- **WISH-104** — Header integration (icon + count badge).
- **WISH-105** — i18n keys in all 3 languages.
- **WISH-106** — Frontend unit tests.
- **WISH-107** — E2E: add to wishlist, remove, share link, guest→login merge.
- **WISH-108** — Update CLAUDE.md, plan 13 → archived.

### Animation Audit 21F — Phase 3–6

- **ANIM-100** — Flying cart polish + reduced-motion compliance.
- **ANIM-101** — Confetti polish + reduced-motion compliance.
- **ANIM-102** — Button/card/modal micro-interaction refinement (align with Nordic Tech philosophy).
- **ANIM-103** — Repo-wide `prefers-reduced-motion` audit.
- **ANIM-104** — Lighthouse performance comparison before/after.
- **ANIM-105** — Motion style guide in `docs/design/motion.md`.
- **ANIM-106** — Plan 21F → archived.

---

## 5. Phase 3 — i18n / Theme / Dark-mode hardening

### I18N-100 — Extract all 25 template + 8 TS hardcoded strings
One PR per feature area. Each PR: add keys to en/bg/de, update template, add regression test.

### I18N-101 — Add i18n lint rule
ESLint rule / custom ng-lint to flag hardcoded non-empty text inside Angular templates. Fail the build on new violations.

### THM-100 — Refactor `glass-card` component
Extract all 27 `rgba()` calls into tokens in `_colors.scss` (e.g. `--glass-bg`, `--glass-border`, `--glass-shadow`).

### THM-101 — Kill all `color: white` and hex/rgb values in feature components
Account, brands, about, not-found, admin pages. Replace with `var(--color-text-*)`.

### THM-102 — Add `[data-theme="dark"]` overrides where still needed
15 flagged components.

### THM-103 — Repo-wide stylelint rule
Block hex, rgb, rgba, and named colors outside `src/styles/_colors.scss`. CI fails on new violations.

### THM-104 — Visual regression suite
Playwright visual regression on every route in light + dark. Baseline snapshots committed.

### I18N/THM-105 — Verification
Run full E2E suite with `Accept-Language=bg` and `Accept-Language=de`, in both themes. Screenshots attached.

---

## 6. Phase 4 — Test coverage gap closure

### TST-100 — Backend unit tests for untested handlers (27 handlers)
Break into per-feature tasks: Admin (biggest, split into sub-batches), Questions, Notifications, Addresses, Categories, Inventory, Promotions, Gdpr, Brands, Installation.

### TST-101 — Integration tests for 18 missing controllers
One PR per 2–3 controllers. Real WebApplicationFactory + testcontainers Postgres. Fixture patterns mirror existing `CartController` tests.

### TST-102 — Frontend unit tests for 13 missing services
`address`, `animation`, `category`, `confetti`, `flying-cart`, `moderation`, `payment`, `promotion`, `review`, `wishlist`, and (if any of the scroll-* services survive after home wipe) those too.

### TST-103 — Missing E2E flows
- Notifications end-to-end
- Q&A thread (ask → answer → mark helpful)
- Inventory low-stock → reorder admin flow
- Search + facet filter flow
- Brand landing + category landing browse

### TST-104 — Coverage gates in CI
Update `.github/workflows/test.yml` to fail under: 80% BE line coverage, 70% FE line coverage. Publish coverage to Codecov.

### TST-105 — Flaky test triage
Run full suite 5× in CI; any test failing ≥ 1× gets quarantined + fixed in the same PR.

---

## 7. Phase 5 — Security hardening

### SEC-100 — Remove committed secrets
- Delete JWT secret from `appsettings.json`; keep only in env + `appsettings.Development.json` (ignored or with dev-only value).
- Delete Stripe test keys from `appsettings.json`; move to `appsettings.Development.json`.
- Rotate the leaked JWT secret (generate new dev value; note that prod env values were already env-var, so production is safe — but rotate anyway as defense in depth).
- Add `scripts/check-committed-secrets.sh` + pre-commit hook.

### SEC-101 — Security headers middleware
Add middleware setting: CSP (tight but compatible with Stripe), X-Frame-Options: DENY, X-Content-Type-Options: nosniff, Referrer-Policy: strict-origin-when-cross-origin, Permissions-Policy minimal.
Write integration test asserting headers are present on every response.

### SEC-102 — Gate Swagger behind dev
Only map Swagger when `IsDevelopment()` OR when a specific `Features:SwaggerInProd=true` config flag is set with basic auth.

### SEC-103 — Tighten CORS
Replace `.AllowAnyHeader()` with explicit allowlist (`Content-Type`, `Authorization`, `Accept`, `X-Correlation-Id`, `Accept-Language`).

### SEC-104 — Extend rate limiting
Policies on:
- `/api/payments/*` — same bucket as `/auth`
- `/api/account/password-reset` — 3/hr per IP
- `/api/admin/*` write verbs — 30/min
- Webhooks signed but also rate-limited per source IP

### SEC-105 — OWASP Top 10 walkthrough
Run `/security-review` skill. File findings as sub-tasks. Close each.

### SEC-106 — Dependency audit
`npm audit --omit=dev`, `dotnet list package --vulnerable`. Fix or document.

### SEC-107 — ADR
`docs/adr/003-security-posture.md` describing headers, rate limits, secrets strategy.

---

## 8. Phase 6 — Performance & Accessibility

### PERF-100 — OnPush adoption
Convert stateless components to `OnPush`. Target: ≥ 70% of components.

### PERF-101 — Extract bloated inline templates to `.html` + `.scss` files
`header` (1392 lines), `product-list` (1075), `product-detail` (794), and any other > 300 lines. Cap 300 lines going forward.

### PERF-102 — Image width/height + responsive `srcset`
Add explicit dimensions + srcset/sizes to all product/hero/brand imagery. Kill layout shifts.

### PERF-103 — CWV RUM
Add `web-vitals` reporter sending LCP/CLS/INP/FID to a `/api/metrics/cwv` endpoint (or OpenTelemetry exporter if that's where Phase 7 lands).

### PERF-104 — Bundle analysis
`ng build --stats-json` + `source-map-explorer`. Document top 10 heaviest deps; lazy-load where possible.

### A11Y-100 — Keyboard handlers on clickable divs
Fix all 10+ flagged `<div (click)>` violations with `role="button"` + `(keydown.enter)` + `(keydown.space)` + `tabindex="0"`.

### A11Y-101 — ARIA pass
Role coverage audit; add roles on custom interactive elements.

### A11Y-102 — Screen reader walkthrough
NVDA / VoiceOver walkthrough of every primary flow. Document issues, fix.

### A11Y-103 — axe-core CI gate
E2E suite with axe; zero serious/critical violations.

### PERF/A11Y-104 — Lighthouse target ≥ 90 on 5 key routes
`/`, `/products`, `/products/:slug`, `/cart`, `/checkout`. Attach reports to PR.

---

## 9. Phase 7 — Production readiness

### PROD-100 — Docker hardening
- Non-root user in both Dockerfiles.
- `HEALTHCHECK` in `Dockerfile.web` (nginx + curl).
- Minimal runtime image layers.
- `.dockerignore` audit.

### PROD-101 — Graceful shutdown
Implement `IHostApplicationLifetime.ApplicationStopping` handler to drain in-flight requests; configure Kestrel shutdown timeout; document behavior.

### PROD-102 — Migrations strategy
Stop running `MigrateAsync()` at app startup in prod. Add a dedicated `scripts/migrate.sh` (or `dotnet ef database update --no-build`) that CD runs as a pre-deploy step. Keep the startup path only for dev. ADR 004.

### PROD-103 — Observability: OpenTelemetry
- Traces: ASP.NET, EF Core, HttpClient, MediatR.
- Metrics: request duration, DB query duration, Stripe + SMTP call counts.
- Logs: Serilog → OTLP exporter.
- Pick backend (user question: Grafana Cloud vs Honeycomb vs Seq vs Axiom).

### PROD-104 — CD pipeline
`.github/workflows/deploy.yml`:
- Triggered on tag push `v*.*.*`
- Runs full test suite
- Builds + pushes Docker images
- Deploys to Railway (or chosen host)
- Runs `/deploy-checklist` skill automation
- Posts deploy notification

### PROD-105 — External dependency healthchecks
Add health checks for Stripe and SMTP reachability (timebox, non-blocking on startup).

### PROD-106 — Rollback plan
Document rollback procedure in `docs/runbooks/rollback.md`. Test it in staging.

### PROD-107 — `/deploy-checklist` first pass
Run skill; close any remaining gaps.

---

## 10. Phase 8 — Documentation & Release

### DOC-100 — ADRs for every non-obvious decision
Minimum expected:
- 001 — Home page concept pick
- 002 — WebGL stack + asset budget
- 003 — Security posture
- 004 — Migration strategy
- 005 — Observability backend
- 006 — Email provider + template engine
- 007 — Stylelint / i18n lint enforcement
- 008 — Deployment topology

### DOC-101 — Update `CLAUDE.md`
Flip every status-table row to Complete. Add lessons-learned entries for each phase.

### DOC-102 — Update `.auto-memory/`
Per-phase feedback + project memories for major decisions.

### DOC-103 — Update Obsidian Projects Catalog
`/Users/sarkisharalampiev/Projects/vault/vault/AI/Projects Catalog.md` — ClimaSite entry reflects completion state.

### DOC-104 — Populate `CHANGELOG.md`
Move Unreleased to v1.0.0 section on release.

### REL-100 — `/release` skill
Run `/release` to bump semver, tag, generate release notes, verify artifacts.

### REL-101 — v1.0.0 tag
Create annotated tag, push, verify CD pipeline runs successfully, verify `/health` returns green on deployed artifact.

---

## 11. Task ID master index

| Prefix | Meaning | Phase |
|--------|---------|-------|
| FND-* | Foundation | 0 |
| HOME-* | Home v3 | 1 |
| NOT-* | Notifications | 2 |
| WISH-* | Wishlist | 2 |
| ANIM-* | Animation 21F wrap | 2 |
| I18N-* | i18n hardening | 3 |
| THM-* | Theme hardening | 3 |
| TST-* | Test coverage | 4 |
| SEC-* | Security | 5 |
| PERF-* | Performance | 6 |
| A11Y-* | Accessibility | 6 |
| PROD-* | Production readiness | 7 |
| DOC-* | Documentation | 8 |
| REL-* | Release | 8 |

---

## 12. Dependencies & critical path

```
FND → HOME ─┐
             ├→ I18N/THM ─→ TST ─→ SEC ─→ PERF/A11Y ─→ PROD ─→ DOC/REL
NOT/WISH/ANIM ┘
```

HOME and partial features can run in parallel after FND. I18N/THM depends on HOME being done (otherwise we fix colors that immediately get deleted). TST depends on partials being done. SEC / PERF / A11Y can start partially in parallel with TST. PROD depends on TST + SEC passing. DOC/REL runs last.

## 13. Definition of Done for each phase

Every phase must satisfy ALL of:

1. Branch merged to main via PR
2. `/code-review` skill passed
3. All existing tests green
4. New tests added + green
5. CLAUDE.md updated
6. `.auto-memory/` updated
7. ADR written if a non-obvious decision was made
8. Screenshots attached (if UI work)
9. Lighthouse attached (if UI work)
10. Phase moves from "In progress" to "Complete" in this file

## 14. Open questions (to resolve at each phase gate)

Phase 1: home concept pick, WebGL stack, asset budget, fallback strategy, analytics events
Phase 2: email provider, template engine, background worker library
Phase 5: whether to keep Swagger behind basic-auth in prod or kill entirely
Phase 7: observability backend, CD target host (confirm Railway)
Phase 8: whether v1.0.0 or v0.1.0 is the right initial public tag
