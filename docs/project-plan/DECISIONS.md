# ClimaSite — Decision Log

**Date:** 2026-06-11
**Source review:** `docs/project-plan/_review/` (architecture.md, devops.md, docs.md, security.md, testing.md)

**How to use this document:** This is the project's decision ledger as of 2026-06-11. Section 1 confirms that `docs/adr/` remains the canonical home for Architecture Decision Records and indexes what exists there today. Section 2 backfills the significant decisions that are clearly embodied in the codebase but were never written down — each in lightweight ADR form so a developer or agent continuing the project can tell what was decided, why, and whether the decision is ratified or merely accidental ("Needs confirmation by owner" = the code does this consistently, but no human ever explicitly chose it, or the docs contradict it). Section 3 lists decisions that have **not** been made yet but are blocking planned work. Section 4 states the rule for recording all future decisions. When a backfilled decision here gets ratified or reversed, promote it to a numbered ADR in `docs/adr/` and update this file to point at it — do not grow this file into a second ADR system.

---

## 1. Canonical ADR home: `docs/adr/`

`docs/adr/` is and remains the single canonical location for decision records. Its process rules live in `docs/adr/README.md` (numbered `NNN-kebab-case-title.md` files, status lifecycle `Proposed → Accepted → Superseded/Deprecated`, and crucially: **never edit the body of an accepted ADR to reverse its decision — write a superseding ADR**). The skeleton is `docs/adr/000-template.md`.

### Existing ADR index (verified 2026-06-11)

| # | Title | Status | Date | Notes |
|---|---|---|---|---|
| [001](../adr/001-home-page-concept.md) | Home page v3 concept (Configurator-First) | Accepted | 2026-04-08 | Chose Concept B over Kinetic Showroom (A) and Comfort Lab (C); supersedes home plans 19, 21A, 23, 24, 25. |
| [002](../adr/002-home-v3-stack-and-assets.md) | Home v3 stack, assets, and build order | Superseded by ADR 0003 (renderer) | 2026-04-08 | Three.js + procedural geometry + rules-based recommendation scoring + backend-first build order. The renderer sub-decision was superseded by [ADR 0003](../adr/0003-home-v3-rendering-canvas2d.md) — production ships Canvas 2D (`three` is absent from `src/ClimaSite.Web/package.json`); scoring + build-order sub-decisions still stand. The earlier in-place "Implementation note — 2026-06-07" violated immutability and is now properly recorded as a superseding ADR (see D-014). |
| [0001](../adr/0001-background-jobs-db-outbox.md) | Background jobs via `BackgroundService` + a DB email outbox | Accepted | 2026-06-16 | Ratifies O-1 / D-001 below; transactional Postgres outbox drained by an in-process `BackgroundService`. |
| [0003](../adr/0003-home-v3-rendering-canvas2d.md) | Home v3 configurator preview renders with Canvas 2D | Accepted | 2026-06-25 | Supersedes ADR 002's renderer sub-decision; ratifies D-014. Three.js deferred (not banned) for future 3D. |

Only 2 ADRs exist despite Plan 18 (`docs/plans/18-project-completion.md`) mandating an ADR per non-obvious decision and targeting ≥8. The backfill below closes that gap on paper; ratifying the "Needs confirmation" entries closes it for real.

---

## 2. Backfilled decisions

Each entry uses a compact ADR shape: Context / Decision / Alternatives considered / Consequences / Date. Status legend:

- **De-facto, ratified by practice** — consistently implemented, no contradicting evidence; safe to treat as binding.
- **Needs confirmation by owner** — implemented, but either never explicitly chosen, contradicted by project docs, or carrying known defects that make ratification a real choice.

### D-001 — Clean Architecture layering (Core / Application / Infrastructure / Api)

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** A production e-commerce backend needs enforced dependency direction so domain logic stays framework-free and testable.
- **Decision:** Four projects with strict dependency direction: `ClimaSite.Core` → nothing, `ClimaSite.Application` → Core, `ClimaSite.Infrastructure` → Core+Application, `ClimaSite.Api` → all three. Cross-cutting services (`ICurrentUserService`, `ITokenService`, `IEmailService`, `ICacheService`, `IStorageService`) are interfaces in `src/ClimaSite.Application/Common/Interfaces/` implemented in Infrastructure.
- **Alternatives considered:** Not recorded (vertical-slice single project, n-tier). The csproj graph is verified correct (`_review/architecture.md` §Dependency overview), so the structure is deliberate even if unrecorded.
- **Consequences:** Clean, verified boundaries; no `using ClimaSite.Infrastructure` anywhere in Application or Core. One sanctioned exception exists (D-013, Identity in Core). The cost is ceremony for small features — accepted.

### D-002 — CQRS with MediatR; thin controllers

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Every API operation needs validation, logging, and a uniform dispatch shape across ~26 controllers.
- **Decision:** All operations are MediatR commands/queries (MediatR 14.0.0, `src/ClimaSite.Application/ClimaSite.Application.csproj`). 25 of 26 controllers are thin `IMediator` pass-throughs. Pipeline behaviors registered in `src/ClimaSite.Application/DependencyInjection.cs:21-22`: `ValidationBehavior` (FluentValidation 12.1.1) then `LoggingBehavior`. The Features convention co-locates command + validator + handler in one file under `Application/Features/<Feature>/` (documented in `src/ClimaSite.Application/Features/AGENTS.md`).
- **Alternatives considered:** Not recorded (plain services, minimal APIs).
- **Consequences:** Uniform request surface; handlers auto-registered by assembly scan — which also keeps the **dead duplicate** `Application/Features/Auth/` tree resolvable (see `_review/architecture.md` finding 2; the live tree is `Application/Auth/` with a different folder dialect — finding 15). A third behavior, `CachingBehavior`, exists but was never registered (Open Decision O-3). Note: `CachingBehavior` deliberately aside, the duplicate-Auth-tree and folder-taxonomy split are accidents to clean up, not decisions.

### D-003 — Data access: handlers query `IApplicationDbContext` (EF Core) directly; no repository layer

- **Status:** **Needs confirmation by owner** (the real pattern contradicts CLAUDE.md)
- **Date:** predates this log, recorded 2026-06-11
- **Context:** CLAUDE.md says "Implement repository pattern for data access," and `src/ClimaSite.Core/Interfaces/` + `src/ClimaSite.Infrastructure/Repositories/` exist — but verified analysis (`_review/architecture.md` finding 4) shows **zero consumers** of any repository interface; `IUnitOfWork` has no implementation at all.
- **Decision (de-facto):** CQRS handlers inject `IApplicationDbContext` (`src/ClimaSite.Application/Common/Interfaces/IApplicationDbContext.cs`) and write EF Core queries directly — 102 of 117 handler files do this. The repository/UnitOfWork layer (~700 lines incl. the 416-line `ProductRepository.cs`) is dead scaffolding.
- **Alternatives considered:** Repository + UnitOfWork (scaffolded, then abandoned in practice — the actual "alternative considered and dropped").
- **Consequences:** Consistent and EF-translatable; but the vestigial second pattern plus the false doc claim steers agents into dead code. **Ratification action:** delete `Core/Interfaces/I*Repository.cs`, `IUnitOfWork.cs`, `Infrastructure/Repositories/*`, the DI registrations at `Infrastructure/DependencyInjection.cs:84-86`, and fix CLAUDE.md — then promote this to an ADR.

### D-004 — DTO mapping: manual `Select` projections; no mapping framework

- **Status:** **Needs confirmation by owner** (Mapster is registered but 100% unused)
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Mapster 7.4.0 is referenced and registered (`Application/DependencyInjection.cs:29-30`, `Common/Mappings/MappingConfig.cs`) but there are zero `IRegister` implementations and zero `.Adapt` calls (`_review/architecture.md` finding 13).
- **Decision (de-facto):** Hand-written `Select(x => new XDto { ... })` projections in 26+ handler files. This is the real, consistent convention and it is EF-query-translatable.
- **Alternatives considered:** Mapster (adopted on paper, never used), AutoMapper (never present).
- **Consequences:** Good for query translation and explicitness; the dead Mapster registration invites a second mapping style and is supply-chain surface for nothing. **Ratification action:** remove the Mapster package + `MappingConfig.cs`, document "manual Select projections only". Related drift risk: every frontend TS model is hand-mirrored (no OpenAPI codegen despite Swagger) — see `_review/architecture.md` finding 9 for the wishlist contract already drifting.

### D-005 — Auth: ASP.NET Identity + JWT access tokens + rotating refresh tokens

- **Status:** De-facto, ratified by practice — **except the password-hashing claim, which Needs confirmation by owner**
- **Date:** predates this log, recorded 2026-06-11
- **Context:** The platform needs stateless API auth with session revocation and brute-force resistance.
- **Decision (de-facto):** ASP.NET Identity (`AddIdentity<ApplicationUser, IdentityRole<Guid>>` at `src/ClimaSite.Infrastructure/DependencyInjection.cs:38`) with lockout (5 attempts / 15 min); JWT access tokens (HMAC-SHA256, 15-minute lifetime, issuer/audience/lifetime/key validation, zero clock skew); refresh tokens of 64 cryptographically random bytes, rotated on login/refresh, revoked on logout/password-reset, stored plaintext as a single column on `ApplicationUser` (`src/ClimaSite.Core/Entities/ApplicationUser.cs:13`).
- **Alternatives considered:** Not recorded (cookie auth, external IdP, IdentityServer/OpenIddict).
- **Consequences:** Solid baseline per `_review/security.md`. Known caveats to carry into ratification: (a) **CLAUDE.md claims "Argon2" password hashing — this is false.** `grep -ri argon2 src/` returns nothing and no custom `IPasswordHasher` is registered; hashing is Identity's default PBKDF2. Owner must decide: adopt Argon2 for real, or fix the doc. (b) Refresh tokens are stored unhashed (`_review/security.md` finding 8, P3). (c) The live auth handlers (`Application/Auth/Handlers/`) have no FluentValidation and no unit tests — the tests target the dead duplicate tree (`_review/architecture.md` finding 2, `_review/security.md` finding 7). (d) Weak default JWT secret in `appsettings.json` is only overridden-or-fail for the literal "Production" env (`_review/security.md` finding 6).

### D-006 — Database: PostgreSQL 16 + EF Core 10 + Npgsql, snake_case naming, JSONB for flexible attributes

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Product specifications vary per HVAC category; relational integrity is needed for orders/inventory.
- **Decision:** PostgreSQL 16 with EF Core (Microsoft.EntityFrameworkCore **10.0.1**, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 — note CLAUDE.md's "EF Core 9.x" is stale, `_review/docs.md` finding 9); snake_case column naming; JSONB columns for product specifications; soft deletes where appropriate; EF migrations for all schema changes.
- **Alternatives considered:** Not recorded.
- **Consequences:** Works well. Two operational traps inherited with it: the migration chain is split across two live folders with a single snapshot (`Infrastructure/Migrations/` + `Infrastructure/Data/Migrations/` — consolidation recipe in `_review/architecture.md` finding 5; never "tidy" these casually), and `MigrateAsync()` currently runs on every app boot with no pre-deploy step or rollback story (`_review/devops.md` finding 3 — a P1 to fix, not part of this decision).

### D-007 — Frontend: Angular 19 standalone components + Signals + `inject()`

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** A modern Angular state model without NgModule/RxJS-subject ceremony.
- **Decision:** Standalone components only (no NgModules); Angular Signals for state (root-provided signal stores in `cart.service`, `auth.service`, `wishlist.service`; **zero** `BehaviorSubject`s project-wide); `inject()` function over constructor DI (78 files use `inject()`, only 3 legacy constructor-DI); lazy-loaded feature routes; `data-testid` on interactive elements.
- **Alternatives considered:** Not recorded (NgRx, RxJS-subject services, NgModules).
- **Consequences:** Uniform and verified (`_review/architecture.md` summary). Cost surfaced by review: the single-file inline-template/inline-SCSS convention produced five 1,000–1,600-line god components (header, product-list, checkout, order-details, product-detail — finding 7); the split-to-`templateUrl`/`styleUrls` refactor is compatible with this decision, not a reversal of it.

### D-008 — i18n: ngx-translate runtime translation, EN/BG/DE

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** The shop serves Bulgarian, English, and German customers and must switch language at runtime without rebuilds.
- **Decision:** `@ngx-translate/core` + `@ngx-translate/http-loader` v17 (`src/ClimaSite.Web/package.json`) with JSON files at `src/ClimaSite.Web/src/assets/i18n/{en,bg,de}.json`; all user-facing text must use translation keys; backend translated content negotiated via `Accept-Language`. CI runs an i18n key check in the frontend test job.
- **Alternatives considered:** Angular built-in `$localize` (compile-time, one bundle per language — incompatible with runtime switching). Not formally recorded.
- **Consequences:** Runtime switching works across three languages; every feature carries a three-language translation obligation (enforced by the CI key check).

### D-009 — Frontend styling: Tailwind CSS + custom components (no Angular Material / PrimeNG) + CSS-variable theming

- **Status:** **Needs confirmation by owner** (tech-stack.md still says "Angular Material or PrimeNG — Options:")
- **Date:** predates this log, recorded 2026-06-11
- **Context:** `docs/tech-stack.md` (last touched 2026-01-12) never recorded the outcome of the component-library decision.
- **Decision (de-facto):** Tailwind CSS 3.4 (`tailwindcss ^3.4.19` + forms/typography plugins) with fully custom components — neither Material nor PrimeNG is in `package.json`. All colors live in `src/ClimaSite.Web/src/styles/_colors.scss` as CSS custom properties supporting light/dark themes; hardcoding colors in components is forbidden (CLAUDE.md, consistently followed).
- **Alternatives considered:** Angular Material, PrimeNG (both listed as options in tech-stack.md, both implicitly rejected).
- **Consequences:** Full design control, no library-upgrade coupling; cost is hand-building primitives (Alert/Modal/Toast/Breadcrumb exist in `shared/components/`). **Ratification action:** rewrite `docs/tech-stack.md` to decided-state and add Tailwind to CLAUDE.md's stack table.

### D-010 — Testing: xUnit everywhere; E2E is Playwright **for .NET** with a no-mocking policy

- **Status:** De-facto, ratified by practice (confirmed by `_review/testing.md`; project docs contradict it and must be fixed)
- **Date:** predates this log, recorded 2026-06-11
- **Context:** CLAUDE.md/AGENTS.md describe a TypeScript Playwright suite (`npx playwright test`, `fixtures/test-data-factory.ts`) that **has never existed**.
- **Decision (de-facto):** Backend/unit/integration tests are xUnit; integration tests use Testcontainers Postgres+Redis via `tests/ClimaSite.Api.Tests/Infrastructure/TestWebApplicationFactory.cs`; E2E is **xUnit + Microsoft.Playwright 1.49 for .NET** (`tests/ClimaSite.E2E/ClimaSite.E2E.csproj`) with PageObjects, an API-backed `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`, axe-core accessibility checks, and an environment-gated `TestController` for data setup/cleanup. E2E runs via `dotnet test tests/ClimaSite.E2E` against a live API (port 5029) + `ng serve` — real data, real DB, no mocking. Frontend unit tests are Jasmine/Karma inside `src/ClimaSite.Web` (there is no `tests/ClimaSite.Web.Tests`).
- **Alternatives considered:** TypeScript Playwright (documented but never built — effectively the rejected alternative).
- **Consequences:** A genuinely solid suite (~620 backend/E2E tests + 941 frontend specs, green as of last persisted runs). The doc lie breaks the project's own "NON-NEGOTIABLE" workflow — fixing CLAUDE.md/AGENTS.md is P1 (`_review/testing.md` finding 1). Unit tests additionally use a hand-rolled `MockDbContext` fake whose limits have already warped production code (`_review/architecture.md` findings 8/16) — supplement with a real-provider fixture for transaction/constraint logic.

### D-011 — Payments: Stripe (Stripe.net backend + @stripe/stripe-js frontend, webhook-driven order transitions)

- **Status:** De-facto, ratified by practice
- **Date:** predates this log, recorded 2026-06-11
- **Context:** The shop needs card payments plus a bank-transfer option for the Bulgarian market.
- **Decision:** Stripe.net 46.2.0 (`src/ClimaSite.Infrastructure/ClimaSite.Infrastructure.csproj:19`) with `StripePaymentService`, `PaymentsController` (create-intent), `WebhooksController` (signature-verified `POST /api/webhooks/stripe`), and `@stripe/stripe-js ^8.6.1` + Stripe Elements on the frontend. Bank transfer exists as a non-Stripe path.
- **Alternatives considered:** Not recorded.
- **Consequences:** Webhook signatures verified (good). Carried risks, not part of the decision: the card path has zero integration/E2E coverage (`_review/testing.md` finding 2 — the only completed-order E2E deliberately avoids the Stripe iframe), and Stripe config is read only from `Stripe:*` section keys while CLAUDE.md documents flat `STRIPE_SECRET_KEY` env vars, with non-empty dummy keys defeating fail-fast (`_review/devops.md` finding 10).

### D-012 — Hosting: Railway, Docker multi-stage images, nginx serving the SPA and proxying `/api`

- **Status:** **Needs confirmation by owner** (intent is clear; the concrete service↔config binding was never recorded and three configs disagree)
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Deployment scaffolding exists but no deploy has been documented and no release ever tagged.
- **Decision (de-facto):** Railway hosts two services built from multi-stage Dockerfiles (`Dockerfile.api`, `Dockerfile.web` at repo root); the web container runs nginx with a PORT-templated config (`src/ClimaSite.Web/nginx.conf.template`) that serves the Angular build and proxies `/api/` to the API service (frontend `environment.production.ts` uses a relative `apiUrl: ''`); `railway.toml` healthchecks hit `/health` (Npgsql+Redis checks wired in `Program.cs`). Config is env-var-first with Railway URL converters (`DATABASE_URL`/`REDIS_URL`).
- **Alternatives considered:** Not recorded.
- **Consequences:** Reasonable topology, but: three railway.toml files exist (root `railway.toml`, `railway.api.toml`, `src/ClimaSite.Api/railway.toml`) with no doc saying which Railway service consumes which; duplicate drifting Dockerfiles and a stale `src/ClimaSite.Web/nginx.conf` are edit-the-wrong-file traps; there is no deploy workflow (`_review/devops.md` finding 2). **Owner must confirm** (devops open questions): which services exist in the Railway dashboard, which config each is bound to, whether auto-deploy from main is enabled, and whether Postgres backups are on. Also note the P0 rider: production startup currently seeds `admin@climasite.local`/`Admin123!` (`_review/devops.md` finding 1) — that was never a decision, it is a defect.

### D-013 — Domain layer depends on ASP.NET Identity (`ApplicationUser : IdentityUser<Guid>`)

- **Status:** **Needs confirmation by owner** (review explicitly recommends ratifying this as a deliberate exception)
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Pure Clean Architecture would keep Identity types out of Core; pragmatically, Identity's user/role model is the auth backbone.
- **Decision (de-facto):** `src/ClimaSite.Core/Entities/ApplicationUser.cs` inherits `IdentityUser<Guid>`; `ClimaSite.Core.csproj` references `Microsoft.Extensions.Identity.Core/Stores`. This is the **only** framework leak into Core (verified — `_review/architecture.md` finding 14).
- **Alternatives considered:** Separate identity model + mapped domain User (rejected implicitly: high cost, low benefit at this project size).
- **Consequences:** Identity types visible throughout Core — acceptable unless multi-tenancy or a non-Identity provider lands on the roadmap (that is the tripwire to reopen). **Ratification action:** accept via a short ADR and optionally enforce "no further framework packages in Core" with an architecture test.

### D-014 — Home v3 ships Canvas 2D rendering; Three.js deferred

- **Status:** **Promoted to [ADR 0003](../adr/0003-home-v3-rendering-canvas2d.md)** (Accepted 2026-06-25) — supersedes ADR 002's renderer sub-decision; ADR 002's Status line was updated to point at 0003 (the allowed immutability-preserving edit), restoring process integrity
- **Date:** 2026-06-07 (implementation note in ADR 002)
- **Context:** ADR 002 chose Three.js + procedural geometry for the configurator's room preview. During Plan 18 Phase 1 the preview shipped as Canvas 2D axonometric rendering instead; `three` was never added to `package.json`. Verified outcomes: Lighthouse mobile 0.97 / desktop 1.00, 213/213 E2E passing.
- **Decision (de-facto):** Canvas 2D is the production renderer for the home configurator preview; Three.js remains the accepted stack for future product-detail 3D when interaction justifies the bundle cost.
- **Alternatives considered:** Three.js (ADR 002's original pick — deferred, not rejected).
- **Consequences:** No WebGL dependency, smaller bundle, reduced-motion fallback is the primary path. **Ratification action (per `_review/docs.md` finding 10):** write ADR 003 superseding ADR 002's renderer sub-decision and restore/annotate 002's original text, so the ADR system's first amendment doesn't normalize body-editing.

### D-015 — Wishlist public sharing via unguessable GUID share token

- **Status:** **Needs confirmation by owner** — new on branch `feature/plan18-wishlist-completion`, security-reviewed as sound, but shipping a security-relevant token design with no ADR
- **Date:** 2026-06 (uncommitted branch), recorded 2026-06-11
- **Context:** Wishlist completion added public sharing (`SetWishlistSharingCommand`, `RegenerateWishlistShareTokenCommand`, `GetSharedWishlistQuery` under `src/ClimaSite.Application/Features/Wishlist/`).
- **Decision (de-facto):** Share links use a GUID share token (~122 bits entropy, not enumerable — `_review/security.md` summary judged the design sound), with owner-controlled enable/disable and token regeneration.
- **Alternatives considered:** Sequential/short IDs (enumerable — correctly avoided); signed expiring URLs (not chosen; no expiry requirement stated).
- **Consequences:** Anyone with the link can read the wishlist until sharing is disabled or the token regenerated — acceptable for wishlists if the owner confirms. **Ratification action:** short ADR before/at merge (per `_review/docs.md` finding 10). Separate non-decision defects on the same slice: in-process static semaphore for concurrency (`_review/architecture.md` finding 8) and hardcoded `AverageRating/ReviewCount = 0` in the DTO (finding 9) — fix, don't ratify.

### D-016 — Local development uses the shared infra stack (`~/Projects/shared-infra`), one database per project

- **Status:** **Needs confirmation by owner** — this is the user's stated global convention, but the repo actively contradicts it
- **Date:** predates this log, recorded 2026-06-11
- **Context:** The user's global rules and `AGENTS.md:120-122` mandate: local dev connects to the shared Postgres/Redis/Meilisearch/MinIO/RabbitMQ/MailHog stack at `~/Projects/shared-infra/docker-compose.yml` (Postgres on 5432, database `climasite`); no project-local compose for these services; integration tests use Testcontainers, never the shared stack.
- **Decision (intended):** As above. Integration tests honoring Testcontainers is verified true.
- **Contradiction in repo:** A root `docker-compose.yml` defines a project-local Postgres on **5433** + Redis + MinIO + pgadmin, and `src/ClimaSite.Api/appsettings.json:3` defaults the connection string to port 5433 — so the code's default follows the forbidden path and a shared-infra dev gets connection-refused without exporting `ConnectionStrings__DefaultConnection` (`_review/devops.md` finding 8, `_review/docs.md` finding 12).
- **Alternatives considered:** Project-local compose (explicitly forbidden by the global convention, yet shipped).
- **Consequences:** Every onboarding re-derives the truth. **Ratification action (owner decides):** point `appsettings.json` Development at `localhost:5432/climasite` and delete the compose file, or explicitly document the compose file as a sanctioned CI-parity/fallback exception.

### D-017 — Logging: Serilog to console

- **Status:** De-facto, ratified by practice — as a *floor*, not an end state
- **Date:** predates this log, recorded 2026-06-11
- **Context:** Structured logging was wired early; Railway captures stdout.
- **Decision (de-facto):** Serilog with a single Console sink (`Program.cs:18-24`, `appsettings.json`). Request logging exists; correlation IDs do **not** — CLAUDE.md's `X-Correlation-Id` convention is documented but unimplemented anywhere (`_review/devops.md` finding 5).
- **Alternatives considered:** Not recorded.
- **Consequences:** Adequate for dev; inadequate for production (no error tracker, metrics, tracing, alerting, runbooks). The observability stack choice is a pending owner decision (Open Decision O-4); Plan 18 PROD-103 targets OpenTelemetry.

---

## 3. Open decisions (not yet made — owner input required)

These are choices the review identified as **blocking or shaping upcoming work**. Per the project convention (Section 4), the user decides; record each outcome as a numbered ADR in `docs/adr/`.

| # | Decision needed | Why it's blocking | Inputs |
|---|---|---|---|
| O-1 | **Background-job mechanism** — .NET `BackgroundService` + DB outbox vs Hangfire/Quartz vs RabbitMQ (already in shared infra) | Hard blocker for Plan 12 notifications, wishlist `NotifyOnSale` price-drop alerts, guest-cart cleanup; the codebase has zero job infrastructure. The email-outbox processor is the natural first job since the orphaned email layer (`_review/architecture.md` finding 1) needs it anyway. | `_review/architecture.md` finding 12 |
| O-2 | **API error contract** — adopt RFC 7807 ProblemDetails everywhere, or ratify the current custom `{status,message,detail}` middleware shape | Three incompatible error shapes exist today; CLAUDE.md falsely claims ProblemDetails (`Program.cs:108` TODO API-012 admits it). Must be one coordinated backend+frontend change. | `_review/architecture.md` finding 6 |
| O-3 | **Query caching** — register `CachingBehavior` + build invalidation, or delete it and the 14 inert `ICacheableQuery` markers | Redis caching is silently inactive despite "Performance: Complete"; enabling blindly would serve stale product/stock data. Do not leave half-wired. | `_review/architecture.md` finding 3 |
| O-4 | **Observability vendor** — error tracker (Sentry suggested as lowest-effort) + OpenTelemetry scope + alerting | Production launch blocker class; correlation-ID middleware should land with it. | `_review/devops.md` finding 5; Plan 18 PROD-103 |
| O-5 | ~~ADR 003 (Canvas 2D)~~ **DONE — [ADR 0003](../adr/0003-home-v3-rendering-canvas2d.md)** formalizes D-014. Still open: an **ADR for wishlist share tokens** (D-015). | Canvas-2D half restores ADR process integrity after the 002 in-place amendment; the wishlist token design (D-015) still needs recording. | `_review/docs.md` finding 10 |
| O-6 | **Shared-infra vs project-local compose** — formalize or kill D-016's contradiction | Onboarding friction every session until decided. | `_review/devops.md` finding 8 |
| O-7 | **Railway topology confirmation** — which services, which config files, auto-deploy status, backups | Determines whether the P0 seeding defect is already live and unblocks the deploy workflow. | `_review/devops.md` open questions 1–4 |

---

## 4. Rule for recording future decisions

1. **Who decides:** Per the project owner's standing convention, the **user (owner) decides all stack-level choices** — tech stack, database, auth approach, hosting, architecture style, UI framework/library, third-party services, and anything with a bill or a migration cost. Agents decide implementation details (file structure, naming, algorithm internals) within those decisions. Never silently choose a stack-level option; ask, with alternatives.
2. **Where:** Every ratified decision becomes a numbered ADR in `docs/adr/` using `docs/adr/000-template.md`, added to the index in `docs/adr/README.md`. This file (DECISIONS.md) is a backfill ledger and pointer — new decisions do **not** accumulate here.
3. **When:** Write an ADR when a decision picks one option among non-obvious alternatives, introduces a cross-cutting convention, is painful to reverse, or amends an earlier decision (criteria in `docs/adr/README.md`).
4. **Immutability:** Never edit the body of an accepted ADR to change its decision — write a superseding ADR and update the old one's Status line only. The ADR 002 in-place amendment (D-014) is the cautionary example.
5. **Backfill promotion:** When a "Needs confirmation by owner" entry above is ratified (or reversed), write the ADR, link it from the entry here, and mark the entry "Promoted to ADR NNN".
