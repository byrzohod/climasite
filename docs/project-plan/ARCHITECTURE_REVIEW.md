# ClimaSite — Architecture Review

**Date:** 2026-06-11
**Sources:** verified findings in `docs/project-plan/_review/architecture.md` (primary) and `docs/project-plan/_review/performance.md` (architectural performance issues). All file/line citations below were verified during the 2026-06-11 review unless marked "Needs confirmation".

**How to use this document:** This is the authoritative description of how ClimaSite is *actually* architected as of 2026-06-11 — including where it diverges from what `CLAUDE.md` and the nested `AGENTS.md` files claim. If you are continuing this project (human or AI), trust this document over `CLAUDE.md` where they conflict (the specific stale claims are listed in [Weaknesses](#weaknesses)); follow the "Backend structure review" and "Frontend structure review" sections to learn the real conventions before writing code; pick improvements from "Suggested architectural improvements" in the order given; and do not touch anything in "Areas to AVOID changing" without reading the warning attached to it.

---

## Current architecture summary

### Solution layout (4 backend projects + Angular app)

| Project | Role | Depends on |
|---|---|---|
| `src/ClimaSite.Core` | Domain entities, value objects, exceptions | nothing (except ASP.NET Identity packages — see Weakness W9) |
| `src/ClimaSite.Application` | CQRS commands/queries/handlers (MediatR), validators (FluentValidation), pipeline behaviors, service interfaces | Core |
| `src/ClimaSite.Infrastructure` | EF Core `ApplicationDbContext`, migrations, service implementations (email, Stripe, MinIO storage, Redis cache, tokens) | Core + Application |
| `src/ClimaSite.Api` | Thin controllers, middleware, `Program.cs` composition root | all three |
| `src/ClimaSite.Web` | Angular 19+ standalone SPA, signals-based state | — |

Dependency directions were verified correct via csproj references: Core → nothing, Application → Core, Infrastructure → Core+Application, Api → all. No `using ClimaSite.Infrastructure` appears anywhere in Application or Core; in Api only `Program.cs` and `TestController` reference Infrastructure directly.

### Backend request flow (the ACTUAL pattern, ~95% consistent)

```
Browser
  → nginx (src/ClimaSite.Web/nginx.conf.template proxies /api/ to the API container)
  → ASP.NET Core pipeline (Program.cs: rate limiter, output cache, ExceptionHandlingMiddleware)
  → Controller (thin, IMediator only — 24/26 controllers; TestController and WebhooksController are deliberate exceptions)
  → MediatR pipeline: ValidationBehavior → LoggingBehavior
      (CachingBehavior EXISTS at Application/Common/Behaviors/CachingBehavior.cs but is NOT registered — see W3)
  → single-file Command/Query + Validator + Handler in Application/Features/<Feature>/
  → IApplicationDbContext (EF Core DbSets exposed to Application; 102 of 117 handler files)
  → PostgreSQL
```

Cross-cutting services are interfaces in `Application/Common/Interfaces` (`ICurrentUserService` — used by 33 handlers, `ITokenService`, `IEmailService`, `ICacheService`, `IStorageService`) implemented in Infrastructure.

**Error flow (three incompatible shapes — see W5):** 41 feature files return `Result<T>` which controllers translate to `BadRequest(new { message = result.Error })`; 6 files throw `NotFoundException`/`ValidationException` caught by `ExceptionHandlingMiddleware` (`{status, message, detail}` JSON); `[ApiController]` model-binding failures emit default ASP.NET `ValidationProblemDetails`.

### Frontend state flow

```
Standalone components (78 files use inject(); only 3 legacy constructor-DI)
  → root-provided signal stores: cart.service, auth.service, wishlist.service
      (state via signal()/computed(); ZERO BehaviorSubjects project-wide)
  → HttpClient with per-service `${environment.apiUrl}/api/...` URL building (17 services, uncentralized)
  → auth.interceptor for JWT attach + refresh
```

The wishlist service additionally owns a guest `localStorage` branch with a `concatMap`-based login-merge sync. All TypeScript models are hand-mirrored interfaces; there is no OpenAPI codegen despite Swagger being enabled.

### Caching layers (what is configured vs. what actually runs)

| Layer | Configured | Actually running |
|---|---|---|
| Redis `IDistributedCache` | `Infrastructure/DependencyInjection.cs:59` | **Never used** — only serves health checks (`Program.cs:188-190`); a Redis outage fails `/health` for zero benefit |
| MediatR `ICacheableQuery` (14 queries with keys/TTLs) | keys + durations defined | **Dead code** — CachingBehavior never registered |
| OutputCache named policies "Products"/"Categories" + tags | `Program.cs:248-249` | **Never referenced by any endpoint**; `EvictByTagAsync` never called |
| OutputCache base policy (5 min) | `Program.cs:245-250`, middleware at `:302` | **Active for ALL anonymous 200 GETs**, per-instance memory, zero invalidation |
| ResponseCaching middleware | `Program.cs:193/301` | Inert (no `[ResponseCache]` anywhere) |

---

## Strengths

1. **Clean Architecture dependency directions are correct and enforced in practice.** No layering violations were found across the entire solution.
2. **CQRS is applied uniformly.** All controllers except two deliberate exceptions (`TestController` — direct `ApplicationDbContext` for E2E seeding; `WebhooksController` — Stripe signature verification) are thin `IMediator`-only pass-throughs; the single-file `Command + Validator + Handler` convention in `Application/Features/<Feature>/` is followed by all 17 feature folders.
3. **Pipeline behaviors are wired for validation and logging** (`Application/DependencyInjection.cs:21-22`), so every request gets FluentValidation and structured logging for free.
4. **The frontend is modern and uniform:** 100% standalone components, signals/computed for all shared state, zero BehaviorSubjects, lazy loading on every route (20+), `@defer` for below-fold content, consistent `@for ... track` (46 files, zero untracked `*ngFor`). Initial bundle is ~259 KB raw / ~73 KB gzip — well inside the 650 KB budget.
5. **The `IApplicationDbContext` data-access pattern is consistent** (102/117 handlers) and EF-translatable; `GetUserOrdersQuery.cs:88-103` shows the best-in-repo server-side projection pattern.
6. **DB indexing is strong:** unique b-tree on `products.slug`, GIN on `tags`/`specifications` jsonb, indexes on all order/cart/wishlist hot paths including `wishlists.share_token` (see the index coverage map in `_review/performance.md`).
7. **Wishlist hydration (new on this branch) is well batched** — one wishlist+items query plus one batched product query, no N+1.
8. **Stripe/payment and auth-refresh paths are isolated and test-covered** (22 AuthController integration tests exercise the live auth path).

## Weaknesses

Each weakness carries its review priority. Full evidence lives in `_review/architecture.md` (A#) and `_review/performance.md` (P#).

| # | Weakness | Priority | Source |
|---|---|---|---|
| W1 | **Transactional email layer fully orphaned** — `EmailService.cs` implements welcome/password-reset/order-confirmation emails but has zero call sites; `ForgotPasswordCommandHandler.cs:36-41` logs the reset token (secrets-in-logs) and returns success without sending. Also `EmailService.cs:23` defaults `Email:UsePlaceholder` to true. | P1 | A1 |
| W2 | **Dead duplicate Auth tree** — `Application/Features/Auth/` is unused; `Application/Auth/` is live (imported by `AuthController.cs:2-3`). The unit tests in `tests/ClimaSite.Application.Tests/Features/Auth/` test the DEAD tree, so live auth handlers have zero unit coverage. Worse, `src/ClimaSite.Application/Features/AGENTS.md:70` wrongly declares the dead tree "canonical". | P1 | A2 |
| W3 | **CachingBehavior never registered** — 14 `ICacheableQuery` queries are inert; Redis is provisioned, health-checked, and unused. Partially mitigated by the accidental 5-min output cache on anonymous GETs. | P1/P2 | A3, P2 |
| W4 | **Rate limiter keys on `RemoteIpAddress` behind nginx with no `UseForwardedHeaders`** (`Program.cs:208-217`) — in the deployed topology every shopper shares one 100 req/min bucket; the 10/min auth bucket becomes a site-wide login DoS. Invisible in dev. | **P0** | P1 |
| W5 | **Three incompatible API error shapes** (custom middleware JSON, ad-hoc `{ message }` bodies, default ProblemDetails); `Program.cs:108-110` TODO API-012 admits ProblemDetails is unimplemented while CLAUDE.md claims it. | P2 | A6 |
| W6 | **Repository/UnitOfWork layer is 100% dead scaffolding** (~700 lines: `Core/Interfaces/I*Repository.cs`, `IUnitOfWork` with NO implementation, `Infrastructure/Repositories/*`, DI regs at `Infrastructure/DependencyInjection.cs:84-86`) — zero consumers, yet CLAUDE.md documents it as the pattern. | P2 | A4 |
| W7 | **Wishlist concurrency via static in-process `ConcurrentDictionary<Guid, SemaphoreSlim>`** (`WishlistApplicationService.cs:10`) — leaks entries forever, single-instance assumption baked into the Application layer, plus a `strategy is null` branch (`:29-32`) that exists only to satisfy the `MockDbContext` test fake. | P2 | A8, P15 |
| W8 | **No background-job infrastructure at all** (zero `IHostedService`/`BackgroundService`/Hangfire/Quartz hits) — hard blocker for Plan 12 notifications (`docs/plans/12-notifications-system.md`), wishlist `NotifyOnSale` (stored but never scanned), and the guest-cart cleanup TODO at `AddToCartCommand.cs:139`. | P2 | A12 |
| W9 | **Core depends on ASP.NET Identity** (`ApplicationUser : IdentityUser<Guid>`) — the only framework leak into the domain; pragmatic, accepted (see "Avoid changing"). | P3 | A14 |
| W10 | **Dead code beyond the repositories:** Mapster registered with zero `IRegister`/`.Adapt` usage (A13); 6 of 8 shared Angular directives unused (`magnetic-hover`, `split-text`, `scroll-progress`, `animate-on-scroll`, `count-up`, `optimized-image`) while CLAUDE.md still lists CountUpDirective as a key file (A10). | P2/P3 | A10, A13 |
| W11 | **Documentation actively lies to agents:** CLAUDE.md/AGENTS.md describe a TypeScript Playwright suite (`npx playwright test`, `fixtures/test-data-factory.ts`) and a `tests/ClimaSite.Web.Tests` project — none exist; E2E is C# xUnit + Microsoft.Playwright run via `dotnet test tests/ClimaSite.E2E`. Plus the repository-pattern, ProblemDetails, "full-text search", and "Performance Optimizations: Complete" claims are all false or overstated. | P2 | A11, P5, P14 |
| W12 | **Search is naive multi-ILIKE** (sequential scan; the existing GIN indexes cannot serve it) despite the "full-text search" claim; facets (`GetFilterOptionsQuery.cs:57`) hydrate the entire active product table per request; recommendations score the whole in-stock catalog per call; public pagination has no pageSize clamp. | P1/P2 | P3, P5, P6, P7 |
| W13 | **Hand-mirrored DTOs drift:** wishlist contract hardcodes `AverageRating`/`ReviewCount` to 0 (`WishlistApplicationService.cs:179-180`) and duplicates `ImageUrl`/`PrimaryImageUrl`; product list/search handlers do the same rating hardcoding. No OpenAPI→TS codegen. | P2 | A9, P11 |
| W14 | **No SSR/prerender** for a public storefront (no `@angular/ssr`, `prerendered-routes.json` is empty, static generic meta in `index.html`) — social unfurls and non-Google crawlers see generic meta for every product URL. Architecture decision required (user decides per global rules). | P2 | P10 |

---

## Backend structure review

### CQRS hygiene — verdict: GOOD, with two taxonomy defects

- The single-file `record Command + AbstractValidator + IRequestHandler` co-location convention is followed by all 17 `Features/*` folders and is the convention new code should follow (documented in `src/ClimaSite.Application/Features/AGENTS.md`, modulo the false "canonical Auth" line).
- Controllers are correctly thin: 24/26 are `IMediator`-only. Deliberate exceptions: `TestController` (uses `ApplicationDbContext` directly, by design for E2E seeding) and `WebhooksController` (talks to the Stripe service for signature verification).
- **Defect 1 (P1):** the dead `Features/Auth` duplicate tree (W2). Both trees' handlers are auto-registered by the MediatR assembly scan (`Application/DependencyInjection.cs:20`), so the dead handlers remain resolvable — a genuine trap. Delete `Application/Features/Auth/` first, retarget the three test files, then (separate commit) optionally move live `Application/Auth/` under `Features/`.
- **Defect 2 (P3):** two organizational dialects — top-level `Auth/` uses `Commands/ + Handlers/ + Queries/ + DTOs/` subfolders with handler-per-file, while everything else uses the Features single-file style (A15). Unify after the dead-tree deletion, or document the exception.
- Handler error style is split 41 `Result<T>` vs 6 exception-throwing files — pick `Result<T>` as the convention when standardizing errors (W5).

### Layering — verdict: SOUND, one accepted leak, one dead second pattern

- Dependency directions are correct and there are no Infrastructure leaks into Application/Core.
- The **repository/UnitOfWork layer is dead** (W6). The real, sanctioned pattern is *CQRS handlers query `IApplicationDbContext` (EF Core) directly with manual `Select` projections*. This pattern is fine — the action item is deleting the vestigial layer and fixing the docs, NOT migrating handlers to repositories.
- **Mapster is dead** (W10): the real mapping convention is hand-written `Select(... => new XDto)` projections (26+ handler files), which is consistent and EF-translatable. Keep it; remove the package.
- **Core→Identity dependency (W9):** accepted. Record as an ADR in `docs/adr/`; do not unwind unless multi-tenancy or a non-Identity provider lands on the roadmap.

### Migrations-folder duplication — verdict: BOTH FOLDERS ARE LIVE; one logical chain split in two

This has confused two prior reviewers, so to be explicit:

- `src/ClimaSite.Infrastructure/Migrations/` holds `20260111071552_InitialCreate`, `20260111094355_AddNotifications`, **and the ONLY `ApplicationDbContextModelSnapshot.cs`** (namespace `ClimaSite.Infrastructure.Migrations`).
- `src/ClimaSite.Infrastructure/Data/Migrations/` holds the 6 later migrations (`20260111164205_AddProductTranslations` … `20260124103919_AddReviewVotes`, namespace `ClimaSite.Infrastructure.Data.Migrations`) with NO snapshot.
- Both compile (no `Compile Remove` in the csproj); EF discovers migrations by assembly scan, so the chain is intact and the snapshot DOES include the latest entities (ReviewVote appears in it). **Neither folder is dead. Do not delete either one.**
- Consolidation recipe (P2, small, do in isolation via the project's `db-migrate` skill): move the two old files + snapshot into `Data/Migrations/`, update their namespace to `ClimaSite.Infrastructure.Data.Migrations`, and do NOT touch migration IDs or `[Migration("...")]` attributes (history matching is by ID). Verify with `dotnet ef migrations list` and a probe `dotnet ef migrations add Probe` (then remove it).
- Note: `_review/performance.md` says "only 2 migrations exist"; that count covers only the older folder — the full chain is 8 migrations across both folders per the verified architecture finding.

### Error handling — verdict: NEEDS ONE COORDINATED DECISION

Three client-visible error shapes (W5) force the frontend to special-case parsing. Adopt RFC 7807 ProblemDetails once, in a single coordinated change covering `ExceptionHandlingMiddleware`, a base-controller `Result<T>`→ProblemDetails helper, and the frontend error interceptor. Until that lands, document the real `{status, message, detail}` contract and do NOT change the middleware shape piecemeal — frontend services parse it.

### Cross-cutting config defects (from performance review, architectural in nature)

- **W4 (P0):** add `app.UseForwardedHeaders()` with `KnownNetworks`/`KnownProxies` restricted to the internal network, *before* `UseRateLimiter` in `Program.cs`. Coordinate with the security review — unrestricted trusted proxies allow IP spoofing of the limiter.
- **Caching ownership:** there are currently *two* intended caching layers (MediatR/Redis and OutputCache) and *one* accidental one (the blanket 5-min base policy). Decide a single owner: either register `CachingBehavior` (`cfg.AddOpenBehavior(typeof(CachingBehavior<,>))`) with an invalidation story, or commit to OutputCache with explicit named policies + `EvictByTagAsync` on mutations and delete the MediatR plumbing (and possibly Redis). Do not leave it half-wired. Note the new anonymous `GET /api/wishlist/shared/{shareToken}` inherits the accidental cache, so revoked share links serve for up to 5 minutes.

---

## Frontend structure review

### Signals & state ownership — verdict: GOOD

- State lives in root-provided services (`cart.service`, `auth.service`, `wishlist.service`) using `signal()`/`computed()`; zero BehaviorSubjects project-wide. This is the convention — keep it.
- `inject()` adoption is near-total (78 files; 3 legacy constructor-DI stragglers).
- The wishlist guest-localStorage branch with `concatMap` login-merge is the most complex client state machine; it is decently engineered and now has specs (`wishlist.service.spec.ts` on this branch).

### Standalone & lazy loading — verdict: GOOD

100% standalone components, every route lazy (`app.routes.ts`), `@defer` on home-v3 and main-layout below-fold content. Gap: no `withPreloading(...)` strategy in `app.config.ts`, so the dominant home→products transition pays a chunk fetch on click (P16 — cheap win).

### Change detection — verdict: WEAKEST ARCHITECTURAL POINT

Zone-based (`provideZoneChangeDetection({ eventCoalescing: true })`), only 10/85 components OnPush (~12%), and **zero `runOutsideAngular`** despite rAF/scroll loops in `animation.service.ts:72-103`, `confetti.service.ts` (~:198-248), `flying-cart.service.ts`, and the header scroll `@HostListener` (`header.component.ts:1437-1452`). Every scroll/confetti frame triggers app-wide change detection across ~75 Default-strategy components. Plan 18 PERF-100 (OnPush ≥ 70%, `docs/plans/18-project-completion.md:300-301`) is still open even though CLAUDE.md says performance is "Complete" — the plan-18 task list is the still-valid source here.

### Component size — verdict: FIVE GOD FILES

Five single-file components exceed 1,000 lines with inline templates and ~800–1,000 lines of inline SCSS (defeating stylelint/IDE tooling): `header.component.ts` (1,566), `product-list.component.ts` (1,435), `checkout.component.ts` (1,231 — **money path**), `order-details.component.ts` (1,082), `product-detail.component.ts` (1,030). Logic is modest (150–330 lines each); the fix is mechanical splits to `templateUrl`/`styleUrls` plus extracting obvious children (header: mega-menu/search overlay/account dropdown; checkout: payment step/address step), AFTER the wishlist branch merges.

### Contract management — verdict: DRIFT-PRONE

All TS models hand-mirrored from C# DTOs; 17 services each rebuild `${environment.apiUrl}/api/...`. The wishlist DTO already shipped never-populated fields (W13). Medium-term: generate TS models from the Swagger spec (`openapi-typescript`) in CI and diff against committed models.

---

## Suggested architectural improvements (incremental, in order)

Each step is independently shippable; ordering minimizes rework.

1. **P0 — Forwarded headers before the rate limiter** (`Program.cs`): ~10 lines + trusted-proxy config. Test through the nginx container with two distinct `X-Forwarded-For` values. (W4)
2. **P1 — Wire the email layer**: inject `IEmailService` into `ForgotPasswordCommandHandler` (intended call is already present, commented, at `:40`), remove the token from logs, wire order-confirmation into `CreateOrderCommand` (or the Stripe webhook success path) and welcome into the live `RegisterCommandHandler`; flip `Email:UsePlaceholder` default. (W1)
3. **P1 — Delete `Application/Features/Auth/` and retarget its three test files** at `ClimaSite.Application.Auth.Handlers`; fix `Features/AGENTS.md:70`. Do this BEFORE any further auth work. (W2)
4. **P1/P2 — Decide the caching owner** (one ADR): register `CachingBehavior` with mutation invalidation, OR go all-in on OutputCache named policies + `EvictByTagAsync` and delete the MediatR/Redis plumbing. Either way, remove the blanket 5-min base policy and the inert ResponseCaching middleware. (W3 + P4)
5. **P2 — Delete dead scaffolding in one sweep**: repository layer (~700 lines), Mapster (package + `MappingConfig.cs` + DI lines 28-30), 6 unused directives + `shared/directives/index.ts` entries. Pure deletions, zero consumers proven. (W6, W10)
6. **P2 — Replace the wishlist semaphore with DB constraints**: catch the 23505 unique violation and re-read, delete the static lock dictionary and the `strategy is null` branch; add `AsNoTracking` to wishlist read paths; populate or remove `AverageRating`/`ReviewCount`. Cheapest done before the branch merges. Note: **no new migration is needed** — `_review/architecture.md` finding 8 recommends adding unique indexes, but they already exist (codebase-verified 2026-06-11: `WishlistConfiguration.cs:46` unique `user_id`, `:107` unique `(wishlist_id, product_id)`); only the violation-handling code change remains. (W7, W13)
7. **P2 — One documentation truth pass** over CLAUDE.md + AGENTS.md after items 4–5 settle direction: real E2E commands (`dotnet test tests/ClimaSite.E2E`), real data-access pattern, real error contract, remove `ClimaSite.Web.Tests` and CountUpDirective references, downgrade "Performance Optimizations" and "Search: full-text" claims. (W11)
8. **P2 — ADR + minimal background-job mechanism** (user decides infra per global rules: simplest viable is a `BackgroundService` + DB email-outbox table; RabbitMQ exists in shared-infra if queuing preferred). Email outbox first, since item 2 needs it for reliability; prerequisite for Plan 12. (W8)
9. **P2 — Query-shape fixes on hot endpoints**: projection in `GetFilterOptionsQuery` (4 columns instead of full entities), pageSize clamp in `PaginatedList.CreateAsync` + validators, brief-DTO projections in list/search handlers, batch the recursive category-descendant lookup into one query (shared helper). (W12, P3/P6/P11/P12)
10. **P2 — Frontend CD hardening**: `NgZone.runOutsideAngular` around the four rAF/scroll loops; continue PERF-100 OnPush conversion (signal inputs make it low-risk); add `withPreloading(PreloadAllModules)`. (Frontend review above)
11. **P2 — ProblemDetails migration** (Large, coordinated): middleware + base-controller helper + frontend interceptor in one change. (W5)
12. **P2/P3 — Decisions requiring the user** (do not decide unilaterally): SSR/prerender vs. client-only meta (W14); real search backend — Postgres FTS vs. pg_trgm vs. shared-infra Meilisearch (W12); deployment replica count, which determines how much W7/OutputCache coherence matters (Needs confirmation — railway.toml suggests single-instance but replica count is not verifiable from the repo).
13. **P3 — Component splits** of the five god files (after wishlist merge, coordinate with any Stitch redesign); ESLint `max-lines` warning ~600 for new files. Folder-taxonomy unification (`Auth/` → `Features/Auth/`). MockDbContext: add a real-provider (SQLite in-memory or testcontainers) fixture for transaction/constraint-dependent handlers.

## Areas to AVOID changing unless necessary

High-blast-radius, currently-stable code. (Verbatim-verified list from `_review/architecture.md`.)

1. **Stripe payment path** — `WebhooksController.cs`, `PaymentsController.cs`, `Infrastructure/Services/StripePaymentService.cs`, `CreateOrderCommand.cs` (transactional order + stock reservation). Money + idempotency + webhook signatures; covered by integration/E2E tests. Touch only with the `deploy-checklist`/`security-review` skills.
2. **Auth token chain** — `Infrastructure/Services/TokenService.cs`, `Application/Auth/Handlers/RefreshTokenCommandHandler.cs`, `src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts` + `auth.service.ts`. Recently refactored to break a circular dependency; refresh races are easy to reintroduce and the live handlers currently lack unit tests (W2) — **fix the tests before touching the code**.
3. **EF migration chain** — BOTH `Infrastructure/Migrations/` and `Infrastructure/Data/Migrations/` are live (see verdict above). Never rename IDs, delete files, or "tidy" these except via the dedicated consolidation recipe.
4. **ExceptionHandlingMiddleware error contract** — `{status, message, detail}` is parsed by frontend services; changing the shape outside the coordinated ProblemDetails migration (improvement 11) breaks error UX silently.
5. **Guest-cart merge + DataSeeder** — `Features/Cart/Commands/MergeGuestCartCommand.cs` and `Infrastructure/Data/DataSeeder.cs` (833 lines). Core business rule with subtle session-cookie edge cases; the seeder is an implicit contract for E2E tests and dev environments.

Also stable and fine as-is: the `IApplicationDbContext` direct-EF pattern (do NOT "fix" it by resurrecting repositories), the manual `Select`-projection mapping convention (do NOT introduce a mapper), and the Core→Identity dependency (record the ADR and move on).

---

## Open items / Needs confirmation

- **Deployment replica count** (affects W7 wishlist lock, in-memory OutputCache coherence, and `KnownProxies` for the W4 fix) — not verifiable from the repo.
- **Whether `Features/Notifications` (7 files) + `NotificationsController` are exercised by the frontend** — not traced; if unused they join the dead-wiring list for Plan 12 rework.
- **Whether the E2E suite currently passes** — not run during the review.
- **CI coverage gates**: `.github/workflows/test.yml` uploads to codecov but no gate enforcing the documented 80%/70% minimums was visible — coverage policy appears aspirational.
- **Agent-config tree drift**: `.claude/skills/`, `.codex/skills/climasite-*`, and `.opencode/skills/` have diverging copies of the same procedures (e.g., db-migrate). P3; pick one canonical source with a sync script or symlinks.
