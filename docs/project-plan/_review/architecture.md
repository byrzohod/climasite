# Architecture & Code Quality — Review Findings (2026-06-11)

## Summary

ClimaSite's macro-architecture is sound and consistent: csproj dependency directions are correct Clean Architecture (Core -> nothing, Application -> Core, Infrastructure -> Core+Application, Api -> all), 25 of 26 controllers are thin MediatR pass-throughs, validation/logging pipeline behaviors are wired, and the frontend is uniformly standalone components with signals/computed state and `inject()` (78 files use `inject()`, only 3 legacy constructor-DI, zero BehaviorSubjects).

However, the documented architecture and the actual architecture diverge sharply: the repository pattern is 100% dead scaffolding (zero consumers; all 102 handler files use `IApplicationDbContext` directly), Mapster is registered but never used (manual mapping is the real convention), and error handling is a three-way split of custom middleware JSON, ad-hoc anonymous `BadRequest` bodies, and default ASP.NET ProblemDetails — while CLAUDE.md claims repository pattern + ProblemDetails + a TypeScript Playwright suite that does not exist.

The most damaging concrete defects found: the entire transactional email layer (password reset, welcome, order confirmation) is implemented but has zero call sites, so password reset silently does nothing for users; a dead duplicate Auth command tree (`Application/Features/Auth`) is what the unit tests actually test, leaving the live auth handlers without unit coverage; and the Redis CachingBehavior plus five `ICacheableQuery` product queries are wired on the query side but the behavior is never registered, so caching is silently inactive despite "Performance Optimizations: Complete".

EF migrations are split across two folders (both live, single snapshot in one of them), which works today but is a trap. The frontend's main debt is five 1,000–1,600-line single-file components (header, product-list, checkout, order-details, product-detail) where inline templates and ~800–1,000 lines of inline SCSS defeat style tooling. The new wishlist slice on this branch is decently engineered but introduces an in-process static per-user semaphore that leaks and won't survive horizontal scaling, and its DTO hardcodes AverageRating/ReviewCount to 0. Future work (Plan 12 notifications, price-drop alerts, guest-cart cleanup) is blocked by the total absence of background-job infrastructure.

Overall: a consistent, workable codebase whose biggest risks are silent dead wiring and documentation that actively lies to the agents maintaining it.

## Findings

### 1. Transactional email layer is fully orphaned — password reset, welcome, and order-confirmation emails are never sent

- **Finding:** The entire transactional email layer (password reset, welcome, order confirmation) is implemented but has zero call sites; password reset silently does nothing for users.
- **Category:** broken-flow / dead wiring
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs:14` ('TODO: Inject email service when implemented'), `:36-41` generates a reset token, logs it ('Token: {Token}'), and returns Success without sending anything. `src/ClimaSite.Application/Common/Interfaces/IEmailService.cs:7-9` declares SendWelcomeEmailAsync/SendPasswordResetEmailAsync/SendOrderConfirmationEmailAsync; `src/ClimaSite.Infrastructure/Services/EmailService.cs:44-66` implements all three (414-line service); repo-wide grep shows ZERO call sites for any of them — the only IEmailService consumer is Gdpr `DeleteUserDataCommand.cs:170` (generic SendEmailAsync). CLAUDE.md status table claims 'Notifications System: Partial — Email notifications implemented'.
- **Affected files/areas:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`, `src/ClimaSite.Application/Common/Interfaces/IEmailService.cs`, `src/ClimaSite.Infrastructure/Services/EmailService.cs`, `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`
- **Why it matters:** Forgot-password is a core user flow that currently returns success to the UI while no email ever arrives — users are silently locked out. Order confirmation emails (a basic e-commerce expectation) never go out. Logging the raw reset token is also a secrets-in-logs issue (security dimension overlap).
- **Recommended fix:** Inject IEmailService into ForgotPasswordCommandHandler and call SendPasswordResetEmailAsync (the commented-out line `:40` already shows the intended call); remove the token from the log statement. Wire SendOrderConfirmationEmailAsync into CreateOrderCommand (or the Stripe webhook success path) and SendWelcomeEmailAsync into RegisterCommandHandler. Add unit tests asserting the email service is invoked. (Effort: Small)
- **Acceptance criteria:** Requesting a password reset in a dev environment with MailHog produces an email containing a working reset link; placing an order produces a confirmation email; no reset token appears in logs; handler unit tests verify IEmailService calls.
- **Dependencies or follow-up:** SMTP config already exists (MailHog in shared infra). Should land before any production launch.
- **Confidence:** verified — Verifier fully confirmed: the live forgot-password UI silently succeeds with no email, zero call sites exist for the three email methods, and no mitigation exists anywhere (SmtpClient/MailKit usage confined to EmailService; Notifications code is in-app DB only; no controller or handler sends email). P1 correctly calibrated: a broken core user flow plus token-in-logs leak that must land before production, but not an actively exploitable or data-loss P0.

### 2. Dead duplicate Auth command tree — unit tests cover the dead copy, live auth handlers have zero unit tests

- **Finding:** Two parallel Auth command trees exist; the unit tests exercise the dead one, so the live login/register/refresh handlers have no unit coverage.
- **Category:** dead-code / testing
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Two parallel trees exist: live `src/ClimaSite.Application/Auth/` (Commands/Handlers/Queries/DTOs; `AuthController.cs:2-3` imports ClimaSite.Application.Auth.Commands/Queries) and dead `src/ClimaSite.Application/Features/Auth/` (LoginCommand.cs, LoginCommandHandler.cs, RegisterCommand(Handler).cs, `RefreshTokenCommand.cs:26` with embedded handler, AuthResponseDto.cs) with no production references. `tests/ClimaSite.Application.Tests/Features/Auth/Commands/{LoginCommandHandlerTests,RegisterCommandHandlerTests,RefreshTokenCommandHandlerTests}.cs` all import ClimaSite.Application.Features.Auth (the dead tree); grep for 'ClimaSite.Application.Auth' under tests/ returns nothing. Both trees' handlers are auto-registered by MediatR assembly scan (`Application/DependencyInjection.cs:20`), so dead handlers remain resolvable.
- **Affected files/areas:** `src/ClimaSite.Application/Features/Auth/Commands/LoginCommandHandler.cs`, `src/ClimaSite.Application/Auth/Handlers/LoginCommandHandler.cs`, `src/ClimaSite.Api/Controllers/AuthController.cs`, `tests/ClimaSite.Application.Tests/Features/Auth/Commands/LoginCommandHandlerTests.cs`
- **Why it matters:** Unit-test green checkmarks for login/register/refresh are testing code that never runs in production; the real handlers (token rotation, lockout, etc.) have no unit coverage. Anyone 'fixing a bug' in the Features/Auth tree changes nothing. Partially mitigated by `tests/ClimaSite.Api.Tests/Controllers/AuthControllerTests.cs` integration tests, which do exercise the live path.
- **Recommended fix:** Delete `src/ClimaSite.Application/Features/Auth/` entirely; retarget the three test files at ClimaSite.Application.Auth.Handlers equivalents (or rewrite, since constructor signatures differ slightly); optionally then move the live Auth folder under Features/ to unify the folder taxonomy (separate commit). (Effort: Medium)
- **Acceptance criteria:** grep 'Application.Features.Auth' returns zero hits; solution builds; Application.Tests pass against ClimaSite.Application.Auth.Handlers types; AuthController integration tests still green.
- **Dependencies or follow-up:** None. Do before any further auth work to avoid edits landing in the dead tree.
- **Confidence:** verified — Verifier confirmed: the dead tree's namespace is referenced only by its own files and the three unit-test files; both trees compile and auto-register (the dead RefreshTokenCommandValidator is also picked up by AddValidatorsFromAssembly); the stated mitigation (22 AuthControllerTests integration tests) is real but already accounted for. P1 calibrated: no runtime defect, but security-critical auth logic has zero effective unit coverage while green tests assert dead code — a false-confidence and maintenance trap.

### 3. CachingBehavior never registered — Redis query caching is silently inactive despite five queries opting in

- **Finding:** The Redis CachingBehavior is defined but never registered in the MediatR pipeline, so query caching is silently inactive even though multiple queries implement `ICacheableQuery`.
- **Category:** dead wiring / performance
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** `src/ClimaSite.Application/DependencyInjection.cs:21-22` registers only ValidationBehavior and LoggingBehavior. `src/ClimaSite.Application/Common/Behaviors/CachingBehavior.cs:7-14` defines ICacheableQuery and the behavior (where TRequest : ICacheableQuery), but grep for 'CachingBehavior' in Api/Infrastructure registration code returns nothing. Five queries implement ICacheableQuery expecting caching: `GetFeaturedProductsQuery.cs:9`, `GetFilterOptionsQuery.cs:10`, `SearchProductsQuery.cs:10`, `GetProductsQuery.cs:8`, `GetProductBySlugQuery.cs:10`. ICacheService has no other consumers (grep: only the behavior, interface, DI registration, and CacheService impl), so no caching happens anywhere. CLAUDE.md claims 'Performance Optimizations: Complete'.
- **Affected files/areas:** `src/ClimaSite.Application/DependencyInjection.cs`, `src/ClimaSite.Application/Common/Behaviors/CachingBehavior.cs`, `src/ClimaSite.Application/Features/Products/Queries/GetProductsQuery.cs`
- **Why it matters:** Every product-list/search/detail request hits PostgreSQL on every call; the Redis dependency is provisioned and paid for but does nothing. Worse, the code reads as if caching works, so cache-invalidation bugs will be 'fixed' that never existed and real load problems will surprise. (See verifier note: the uncached-load claim is partly mitigated by output caching.)
- **Recommended fix:** Either register the behavior (`cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>))` — note MediatR open-generic constrained behaviors need AddOpenBehavior or registration-source handling for non-cacheable requests) AND implement invalidation on product/category mutations, or delete CachingBehavior + ICacheableQuery markers and document that caching is deferred. Do not leave it half-wired. (Effort: Medium)
- **Acceptance criteria:** Either (a) integration test proving a second identical GetProducts call is served from cache and admin product update invalidates it, or (b) ICacheableQuery/CachingBehavior removed and grep returns zero hits.
- **Dependencies or follow-up:** Enabling requires an invalidation strategy first — enabling blindly will serve stale product/stock data to customers.
- **Confidence:** verified — Verifier adjusted P1 down to P2: dead wiring confirmed and even understated (14 queries, not 5, implement ICacheableQuery as dead markers; Redis serves only the health check), but the "every request hits PostgreSQL" impact claim is refuted by an output-cache base policy (`Api/Program.cs:245-250`, 5-min expiry, UseOutputCache at `:302`) that shields anonymous GET traffic in memory — so this is a code-hygiene issue, not a major performance defect; the wire-or-delete fix stands.

### 4. Repository/UnitOfWork layer is 100% dead scaffolding; the real data-access pattern is handlers -> IApplicationDbContext

- **Finding:** The repository/UnitOfWork layer has zero consumers anywhere; the actual, consistently applied pattern is handlers injecting `IApplicationDbContext` and using EF Core directly.
- **Category:** architecture / dead-code
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Core/Interfaces/` contains IRepository, IUnitOfWork (IUnitOfWork has NO implementation anywhere — grep 'class UnitOfWork' is empty), ICart/ICategory/IOrder/IProduct/IReview/IWishlistRepository. `src/ClimaSite.Infrastructure/Repositories/` has BaseRepository, CategoryRepository, ProductRepository (416 lines), WishlistRepository, registered at `Infrastructure/DependencyInjection.cs:84-86` — but grep across Api+Application finds ZERO consumers of any repository interface. Actual pattern: 102 of 117 handler files inject IApplicationDbContext and use EF directly (e.g., `AddToWishlistCommand.cs:27, 56-57`); 100 Application files import Microsoft.EntityFrameworkCore. CLAUDE.md ('Implement repository pattern for data access') documents the dead pattern.
- **Affected files/areas:** `src/ClimaSite.Core/Interfaces/IUnitOfWork.cs`, `src/ClimaSite.Infrastructure/Repositories/ProductRepository.cs`, `src/ClimaSite.Infrastructure/DependencyInjection.cs`, `src/ClimaSite.Application/Common/Interfaces/IApplicationDbContext.cs`
- **Why it matters:** ~700+ lines of unmaintained query logic (ProductRepository alone is 416 lines) that silently drifts from the real handler queries; new contributors and agents following CLAUDE.md will add code to the dead layer. The IApplicationDbContext pattern itself is fine and consistently applied — the problem is the vestigial second pattern.
- **Recommended fix:** Delete `Core/Interfaces/I*Repository.cs` + IUnitOfWork.cs, `Infrastructure/Repositories/*`, and the three DI registrations; update CLAUDE.md and AGENTS.md to document 'CQRS handlers query IApplicationDbContext (EF Core) directly' as the sanctioned pattern. (Effort: Small)
- **Acceptance criteria:** Solution builds with the Repositories folder and interfaces removed; grep 'IRepository' returns zero hits; CLAUDE.md data-access section matches reality.
- **Dependencies or follow-up:** None — pure deletion, zero consumers proven.
- **Confidence:** verified

### 5. EF migrations split across two live folders with a single snapshot — works today, but a consolidation trap

- **Finding:** The EF migration chain is one logical sequence split across two folders/namespaces, with the only model snapshot in the older folder.
- **Category:** architecture / maintainability
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Infrastructure/Migrations/` holds InitialCreate (20260111071552) + AddNotifications (20260111094355) + the ONLY ApplicationDbContextModelSnapshot.cs (namespace ClimaSite.Infrastructure.Migrations). `src/ClimaSite.Infrastructure/Data/Migrations/` holds the 6 later migrations (20260111164205_AddProductTranslations ... 20260124103919_AddReviewVotes, namespace ClimaSite.Infrastructure.Data.Migrations) with NO snapshot. Both compile (no Compile Remove in ClimaSite.Infrastructure.csproj) and EF discovers migrations by assembly scan (`DependencyInjection.cs:30` MigrationsAssembly), so the chain is intact and the snapshot DOES include the latest entities (ReviewVote appears 3x in the snapshot). Verdict: neither folder is dead; the set is one logical chain split across two namespaces.
- **Affected files/areas:** `src/ClimaSite.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`, `src/ClimaSite.Infrastructure/Data/Migrations/20260124103919_AddReviewVotes.cs`, `src/ClimaSite.Infrastructure/DependencyInjection.cs`
- **Why it matters:** Where `dotnet ef migrations add` drops the next migration depends on tooling heuristics (it follows the most recent migration's namespace, while the snapshot sits in the other one), so the split self-perpetuates and invites someone to 'clean up' by deleting a folder — which would break the applied-migration chain. Confused two prior reviewers already (this audit was asked to determine 'which is dead').
- **Recommended fix:** Move the two files + snapshot from `Infrastructure/Migrations/` into `Data/Migrations/` and update their namespace to ClimaSite.Infrastructure.Data.Migrations (migration IDs and `[Migration("...")]` attributes must NOT change — history matching is by ID, so a pure move+namespace edit is safe). Verify with `dotnet ef migrations list` against a freshly migrated DB. (Effort: Small)
- **Acceptance criteria:** One migrations folder; `dotnet ef database update` on a clean DB applies all 8 migrations; `dotnet ef migrations add Probe` generates into the consolidated folder (then remove it).
- **Dependencies or follow-up:** Use the project's db-migrate skill; do it in isolation, not bundled with schema changes.
- **Confidence:** verified

### 6. Three incompatible API error-response shapes; CLAUDE.md falsely claims ProblemDetails

- **Finding:** API errors are returned in three incompatible shapes (custom middleware JSON, anonymous BadRequest bodies, default ValidationProblemDetails) while CLAUDE.md claims ProblemDetails is in use.
- **Category:** consistency / API contract
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** (1) `ExceptionHandlingMiddleware.cs:46-58` returns custom JSON `{status, message, detail}`. (2) Controllers translating `Result<T>` failures return `BadRequest(new { message = result.Error })` — e.g., `WishlistController.cs:42-44, 54-56, 65-67`. (3) `[ApiController]` model-binding/annotation failures return default ValidationProblemDetails (no ApiBehaviorOptions customization anywhere in Program.cs). Handler-side error style is also split: 41 feature files use `Result<T>` while 6 throw NotFoundException/ValidationException for the middleware to catch. `Program.cs:108-110` contains TODO API-012 acknowledging ProblemDetails is NOT implemented, yet CLAUDE.md says 'Apply proper exception handling with ProblemDetails'.
- **Affected files/areas:** `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs`, `src/ClimaSite.Api/Controllers/WishlistController.cs`, `src/ClimaSite.Api/Program.cs`
- **Why it matters:** Frontend error handling must special-case three shapes (some parse `message`, ProblemDetails uses `title`/`errors`), making toast/error UX inconsistent and client error parsing fragile. The doc claim means new endpoints get written against an imagined contract.
- **Recommended fix:** Adopt RFC 7807 once: convert the middleware to ProblemDetails (or IExceptionHandler + AddProblemDetails), add a small base-controller helper mapping Result failures to ProblemDetails, and update the frontend error interceptor in one coordinated change. Until then, fix CLAUDE.md to describe the real `{status,message,detail}` contract. (Effort: Large)
- **Acceptance criteria:** All 4xx/5xx responses from a contract-test sweep share one schema; frontend error interceptor parses a single shape; CLAUDE.md matches.
- **Dependencies or follow-up:** Must be coordinated with frontend services that parse `error.error.message`; do NOT change the middleware shape piecemeal.
- **Confidence:** verified

### 7. Five 1,000–1,600-line single-file god components with inline templates and ~800–1,000 lines of inline SCSS

- **Finding:** Five high-churn frontend components are single files of 1,000–1,600 lines, the bulk of which is inline template markup and inline SCSS.
- **Category:** frontend / complexity hotspot
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `header.component.ts`: 1,566 lines (template lines 27-379, inline styles 380-1418, class from 1419 — nav + mega-menu + search + cart + account + language + theme in one file). `product-list.component.ts`: 1,435 (template 30-323, styles 324-1107, class 1108+). `checkout.component.ts`: 1,231 (class at 1014). `order-details.component.ts`: 1,082 (class at 944). `product-detail.component.ts`: 1,030 (class at 824). The single-file inline pattern is the project-wide convention, so these are pattern-consistent — but at this size the SCSS-in-template-string defeats stylelint/IDE SCSS tooling and makes diffs/merge conflicts painful.
- **Affected files/areas:** `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts`, `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`, `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`, `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts`, `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts`
- **Why it matters:** These five files are where most user-facing churn happens (checkout, PDP, PLP, header). Component logic is actually modest (150-330 lines); the bulk is markup+styles, so the cost is tooling blindness and conflict-prone files, not logic complexity — but checkout at 1,231 lines containing payment flow is the riskiest to keep editing in place.
- **Recommended fix:** For files >800 lines, split to templateUrl/styleUrls (mechanical, zero behavior change) and extract obvious children (header: mega-menu, search overlay, account dropdown; checkout: payment step, address step). Set an ESLint max-lines warning (~600) for new files. (Effort: Large)
- **Acceptance criteria:** Each of the five files under ~600 lines or split into .html/.scss; ng test and E2E green; no visual regressions in light/dark.
- **Dependencies or follow-up:** Do AFTER the wishlist branch merges to avoid conflicts; coordinate with any Stitch-driven redesign work.
- **Confidence:** verified

### 8. Wishlist concurrency control uses a leaking static in-process semaphore that breaks under horizontal scaling

- **Finding:** The new wishlist service serializes per-user mutations via a static in-process semaphore dictionary that leaks entries and provides no guarantee across multiple API instances.
- **Category:** scalability / new code on this branch
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs:10` 'private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserMutationLocks' — entries are GetOrAdd'ed (`:24`) and never removed (unbounded growth, one SemaphoreSlim per user forever). The lock only serializes within ONE process; with >1 API instance (docker-compose/railway scale-out) duplicate wishlists or duplicate items can still be created. Also `:29-32` 'strategy is null ? ... : ...' — DatabaseFacade.CreateExecutionStrategy() never returns null in real EF Core; this branch exists solely to accommodate `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs` whose mocked DatabaseFacade (`MockDbContext.cs:170-186`) returns null — production code bent to fit a test fake.
- **Affected files/areas:** `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs`
- **Why it matters:** Works for single-instance deploys, but it is a hidden single-instance assumption baked into the Application layer plus a slow memory leak; the null-check pattern will be copy-pasted into future services. The robust guarantee belongs in the database.
- **Recommended fix:** Add unique indexes: Wishlists(user_id) and WishlistItems(wishlist_id, product_id) via migration, catch DbUpdateException (23505) and re-read — then delete the semaphore dictionary. Replace the strategy null-check by testing with a real provider (SQLite in-memory) or by exposing an ExecuteInTransaction abstraction on IApplicationDbContext. (Effort: Medium)
- **Acceptance criteria:** Parallel-add integration test (two concurrent AddToWishlist for same product) yields one item with constraints in place and no static lock dictionary in the service; migration applied.
- **Dependencies or follow-up:** Migration needed; coordinate with the uncommitted branch before merge — cheapest moment to fix is now.
- **Confidence:** verified

### 9. Hand-mirrored frontend DTOs drift: wishlist contract carries duplicate and never-populated fields

- **Finding:** The wishlist DTO contract includes fields the backend never populates (AverageRating/ReviewCount hardcoded to 0) and a duplicate image field, mirrored by hand into three overlapping TS shapes.
- **Category:** duplication / contract drift
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Backend mapping hardcodes AverageRating = 0 and ReviewCount = 0 (`WishlistApplicationService.cs:179-180`) and sets both ImageUrl and PrimaryImageUrl to the same value (`:173-174`) — duplicate field kept for legacy frontend compatibility. Frontend `wishlist.service.ts:10-52` hand-declares three overlapping shapes (WishlistDto, WishlistApiItem, WishlistItem) mirroring the C# DTO including averageRating/reviewCount, which will always render 0. Project-wide, every TS model is hand-written (no OpenAPI codegen despite Swagger being enabled); 17 services each rebuild `${environment.apiUrl}/api/...` (consistent but uncentralized).
- **Affected files/areas:** `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `src/ClimaSite.Application/Features/Wishlist/DTOs/WishlistDto.cs`, `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts`
- **Why it matters:** Fields that exist in the contract but are never populated are latent UI bugs (a wishlist card showing '0 stars (0 reviews)' for a 5-star product) and erode trust in every other mirrored model. Manual mirroring across 17 services guarantees future drift in both directions.
- **Recommended fix:** Short term: either populate AverageRating/ReviewCount in MapToDtoAsync (one extra grouped query over Reviews) or remove the fields from DTO + TS model + template; drop one of ImageUrl/PrimaryImageUrl. Medium term: generate TS models from the Swagger spec (openapi-typescript) in CI and diff against committed models. (Effort: Small)
- **Acceptance criteria:** Wishlist API returns real rating data (verified in an API test) or the fields are gone from both sides; no TS interface field exists that the backend never populates.
- **Dependencies or follow-up:** Branch is uncommitted — fix before merge.
- **Confidence:** verified

### 10. Six of eight shared directives are dead code; CLAUDE.md still advertises one of them as a key file

- **Finding:** Six shared Angular directives have zero usages outside their own files/specs, and CLAUDE.md's Quick Reference still lists one (CountUpDirective) as a key file.
- **Category:** dead-code / docs drift
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Zero usages outside their own files/specs for: magnetic-hover.directive.ts, split-text.directive.ts, scroll-progress.directive.ts, animate-on-scroll.directive.ts, count-up.directive.ts, optimized-image.directive.ts (verified by grepping both selectors appMagneticHover/appSplitText/appScrollProgress/appAnimateOnScroll/appCountUp/appOptimizedImage and class names in src/ClimaSite.Web/src/app — all 0). Only RevealDirective is used (3 components). Plan 21F removed Floating/Tilt/Parallax cleanly (no dangling refs), but stopped there. CLAUDE.md 'Quick Reference' still lists CountUpDirective as a key file and the status table calls Animation Audit 21F 'Phases 1-2 complete'.
- **Affected files/areas:** `src/ClimaSite.Web/src/app/shared/directives/magnetic-hover.directive.ts`, `src/ClimaSite.Web/src/app/shared/directives/count-up.directive.ts`, `src/ClimaSite.Web/src/app/shared/directives/index.ts`, `CLAUDE.md`
- **Why it matters:** Dead directives get imported by future code (resurrecting deliberately-retired animation patterns 21F was meant to kill), inflate the bundle surface, and the stale CLAUDE.md reference actively steers agents toward dead code.
- **Recommended fix:** Delete the six unused directives + their specs, update `shared/directives/index.ts`, and fix the CLAUDE.md Quick Reference and 21F status. If optimized-image is intended future work, track it in a plan instead of keeping the file. (Effort: Small)
- **Acceptance criteria:** ng build green; grep for the six selectors/class names returns only git history; CLAUDE.md Quick Reference lists only living files.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 11. CLAUDE.md/AGENTS.md describe a test stack and patterns that do not exist (TS Playwright suite, ClimaSite.Web.Tests, repository pattern, ProblemDetails)

- **Finding:** The agent-facing docs describe a TypeScript Playwright suite, a ClimaSite.Web.Tests project, a repository pattern, and ProblemDetails error handling — none of which exist in the codebase.
- **Category:** documentation
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `tests/ClimaSite.E2E` is C# (ClimaSite.E2E.csproj with Microsoft.Playwright 1.49 + xunit 2.9.2; PageObjects/, Infrastructure/, Tests/ — no package.json, no fixtures/ dir), but CLAUDE.md instructs `npx playwright test`, documents `tests/ClimaSite.E2E/fixtures/test-data-factory.ts` (does not exist) and a TypeScript test example; `AGENTS.md:112` repeats `npx playwright test`. CLAUDE.md project tree lists `tests/ClimaSite.Web.Tests` (karma.conf.js) — tests/ contains only Api.Tests, Application.Tests, Core.Tests, E2E. Plus the repository-pattern and ProblemDetails claims contradicted in findings above, and the 'Plans' table pointing at karma/TS conventions.
- **Affected files/areas:** `CLAUDE.md`, `AGENTS.md`, `tests/ClimaSite.E2E/ClimaSite.E2E.csproj`
- **Why it matters:** This project is explicitly agent-driven (CLAUDE.md mandates post-implementation workflows). Every agent session inherits these falsehoods: 'npx playwright test' fails, agents may scaffold a TS test factory or repository classes to match the docs, and the mandatory test commands in the 'NON-NEGOTIABLE' section cannot be executed as written.
- **Recommended fix:** One documentation pass: correct E2E commands to `dotnet test tests/ClimaSite.E2E`, replace the TS example with the PageObjects/xUnit pattern, delete ClimaSite.Web.Tests from the tree, rewrite data-access and error-handling sections to match IApplicationDbContext + custom middleware reality, and sync nested AGENTS.md files. (Effort: Small)
- **Acceptance criteria:** Every command in CLAUDE.md's Commands and Post-Implementation sections runs successfully from a clean checkout; no referenced file path 404s.
- **Dependencies or follow-up:** Update after deciding findings 4 and 6 direction so docs are written once.
- **Confidence:** verified

### 12. No background-job infrastructure — hard blocker for Plan 12 notifications, wishlist NotifyOnSale, and guest-cart cleanup

- **Finding:** There is no background-job/worker infrastructure of any kind, yet several features already persist data assuming a processor exists, and Plan 12 cannot be built without one.
- **Category:** future-blocker
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** grep for IHostedService/BackgroundService/AddHostedService/Hangfire/Quartz across src returns zero hits. Features that already persist data assuming a processor exists: WishlistItem.NotifyOnSale is stored and round-tripped (`WishlistApplicationService.cs:185`) but nothing scans for price drops; `AddToCartCommand.cs:139` 'TODO: Implement guest cart expiration/cleanup job'; Notification entity + Features/Notifications (7 files) + NotificationsController exist but emails would be sent synchronously in-request once wired (finding 1). Plan 12 (`docs/plans/12-notifications-system.md`) is the next major roadmap item per CLAUDE.md.
- **Affected files/areas:** `src/ClimaSite.Api/Program.cs`, `src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs`, `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`
- **Why it matters:** Plan 12's email digests, sale alerts, and cleanup jobs cannot be built without a worker/scheduler; bolting them into request handlers will create timeouts and dropped work. Deciding the mechanism (hosted BackgroundService + RabbitMQ from shared-infra vs Hangfire) shapes the notifications design, so it must precede implementation.
- **Recommended fix:** Make an ADR choosing the job mechanism (simplest viable: .NET BackgroundService with a DB outbox table for emails; RabbitMQ already exists in shared infra if queuing is preferred — per global rules the user decides infra). Implement an email-outbox processor first since finding 1 needs it anyway. (Effort: Large)
- **Acceptance criteria:** ADR merged; one working recurring job (e.g., email outbox dispatch) running in Development with a test proving retry-on-failure.
- **Dependencies or follow-up:** User decision on job tech; precedes Plan 12; interacts with finding 1's email wiring.
- **Confidence:** verified

### 13. Mapster is registered but completely unused — manual projection is the real (and consistent) mapping convention

- **Finding:** Mapster is configured and registered in DI but has zero `IRegister` implementations and zero `.Adapt` calls; hand-written `Select` projections are the actual convention.
- **Category:** dead-code
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `Application/DependencyInjection.cs:29-30` builds and registers MappingConfig.GetConfiguration(); MappingConfig.cs scans the assembly for Mapster IRegister implementations — grep finds ZERO IRegister implementations and ZERO `.Adapt<` / `.Adapt(` calls anywhere in Application. Actual convention: hand-written `Select(... => new XDto)` projections (26+ handler files) — which is consistent and EF-translatable.
- **Affected files/areas:** `src/ClimaSite.Application/DependencyInjection.cs`, `src/ClimaSite.Application/Common/Mappings/MappingConfig.cs`
- **Why it matters:** A dead mapping framework registered in DI invites a second mapping style to creep in, and the package is a supply-chain/upgrade surface for nothing. The manual-projection convention is good for EF query translation and should be the documented standard.
- **Recommended fix:** Remove the Mapster package reference, MappingConfig.cs, and DI lines 28-30; document 'manual Select projections only' in AGENTS.md (Application/Features). (Effort: Small)
- **Acceptance criteria:** Solution builds without the Mapster package; grep Mapster returns zero hits.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 14. Domain layer (Core) depends on ASP.NET Identity

- **Finding:** The domain layer's only framework leak: ApplicationUser inherits from IdentityUser and Core references Identity packages.
- **Category:** architecture
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Core/Entities/ApplicationUser.cs:1,5` — 'using Microsoft.AspNetCore.Identity; public class ApplicationUser : IdentityUser<Guid>'; ClimaSite.Core.csproj references Microsoft.Extensions.Identity.Core/Stores 10.0.1. Otherwise Core is clean (no project references, no EF).
- **Affected files/areas:** `src/ClimaSite.Core/Entities/ApplicationUser.cs`, `src/ClimaSite.Core/ClimaSite.Core.csproj`
- **Why it matters:** The only framework leak into the domain layer. Pragmatic and very common with Identity; the cost (Identity types visible to all of Core) is real but low, and unwinding it (separate identity model + domain User) is expensive relative to benefit at this project size.
- **Recommended fix:** Accept and document as a deliberate exception in an ADR (docs/adr/ exists). Do not refactor unless multi-tenancy or a non-Identity auth provider appears on the roadmap. (Effort: Small)
- **Acceptance criteria:** ADR recorded; no further framework packages added to Core (could be enforced via a simple architecture test).
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 15. Application folder taxonomy split: top-level Auth/ (handler-per-file) vs Features/* (single-file command+validator+handler)

- **Finding:** Two organizational dialects coexist in the Application layer: top-level Auth/ with separate handler files vs Features/* folders co-locating command+validator+handler per file.
- **Category:** consistency
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Application/` top level contains both Auth/ and Features/ (ls output); Auth/ uses Commands/ + Handlers/ + Queries/ + DTOs/ subfolders with handlers in separate files (e.g., `Auth/Handlers/LoginCommandHandler.cs`), while all 17 Features/* folders co-locate command+validator+handler in one file (e.g., `Features/Wishlist/Commands/AddToWishlistCommand.cs` contains record + validator + handler). Features/AGENTS.md documents the Features convention.
- **Affected files/areas:** `src/ClimaSite.Application/Auth/Handlers/LoginCommandHandler.cs`, `src/ClimaSite.Application/Features/Wishlist/Commands/AddToWishlistCommand.cs`, `src/ClimaSite.Application/Features/AGENTS.md`
- **Why it matters:** Two organizational dialects in one layer; combined with the dead Features/Auth tree (finding 2) this makes auth code discovery genuinely confusing — three places where 'LoginCommand' lives.
- **Recommended fix:** After deleting dead Features/Auth, move Application/Auth under Features/Auth keeping its file style, or at minimum note the exception in Features/AGENTS.md. Pure file moves + namespace updates. (Effort: Medium)
- **Acceptance criteria:** Single top-level taxonomy (Common + Features) or a documented exception; AuthController compiles against the new namespaces.
- **Dependencies or follow-up:** Sequence after finding 2 (dead tree deletion).
- **Confidence:** verified

### 16. MockDbContext is a well-built but high-maintenance hand-rolled EF fake with fidelity gaps

- **Finding:** The hand-rolled MockDbContext test fake is solid for pure-logic tests but imposes a per-entity maintenance tax and cannot reproduce real EF transaction/constraint behavior — and has already warped production code.
- **Category:** testing
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs` (312 lines): 29 hand-mirrored DbSet lists (`:18-46`) that must be extended for every new entity; TestAsyncQueryProvider/async enumerator support (`:193-198`) is solid, but transactions are no-op mocks (`:170-186`, CreateExecutionStrategy returns null — which forced the production null-branch in `WishlistApplicationService.cs:30`), SaveChangesAsync always returns 1 with manual nav-prop syncing (`:135-167`), reflection-based navigation fixup (`:103-109`), and no constraint/concurrency semantics. Include() only 'works' because lists hold live object references.
- **Affected files/areas:** `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs`, `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`
- **Why it matters:** Per-entity maintenance tax on every schema change, and tests can pass while real EF behavior (transactions, unique constraints, cascade deletes, query translation) differs — the wishlist concurrency design (finding 8) is exactly the class of bug this fake cannot catch. It has also already warped production code.
- **Recommended fix:** Keep MockDbContext for fast pure-logic tests but add an EF Core + SQLite in-memory (or Npgsql + testcontainers, matching the E2E policy) fixture for handlers that use transactions/constraints; document in tests AGENTS.md when each applies; remove the production null-strategy branch once covered. (Effort: Medium)
- **Acceptance criteria:** Transaction-using handlers (wishlist mutations, CreateOrderCommand) have at least one test on a real EF provider; the `strategy is null` branch is deleted.
- **Dependencies or follow-up:** Pairs with finding 8.
- **Confidence:** verified

## Dimension data

### Dependency / data-flow overview

**Backend flow (the ACTUAL pattern, consistent across ~95% of the surface):**
HTTP -> Controller (thin, IMediator only; 24/26 controllers — TestController uses ApplicationDbContext directly by design, WebhooksController talks to Stripe service) -> MediatR pipeline (ValidationBehavior -> LoggingBehavior; CachingBehavior defined but NOT registered) -> single-file Command/Query+Validator+Handler in `Application/Features/<Feature>/` -> `IApplicationDbContext` (EF Core DbSets exposed to Application; 102/117 handler files) -> PostgreSQL. Cross-cutting services (`ICurrentUserService` in 33 handlers, `ITokenService`, `IEmailService`, `ICacheService`, `IStorageService`) are interfaces in `Application/Common/Interfaces` implemented in Infrastructure — dependency directions verified correct via csproj: Core -> (nothing, except Identity packages), Application -> Core, Infrastructure -> Core+Application, Api -> all three. No `using ClimaSite.Infrastructure` anywhere in Application or Core; in Api only Program.cs + TestController.

**Vestigial second pattern (dead):** Core repository interfaces + Infrastructure/Repositories + DI registrations, zero consumers. IUnitOfWork has no implementation at all.

**Error flow:** handlers return `Result<T>` (41 files) which controllers translate to `BadRequest(new { message })`; a minority (6 files) throw exceptions caught by ExceptionHandlingMiddleware (`{status,message,detail}` JSON); model-binding errors emit default ProblemDetails. Three client-visible shapes.

**Frontend flow:** standalone components -> root-provided signal stores (cart.service, auth.service, wishlist.service own state via `signal`/`computed`; zero BehaviorSubjects project-wide) -> HttpClient with per-service `${environment.apiUrl}/api/...` (17 services) -> auth.interceptor for JWT/refresh. Wishlist additionally owns a guest localStorage branch with login-merge (`concatMap`-based sync). Models are hand-mirrored TS interfaces; no OpenAPI codegen.

### Hotspot table

| File | Size | Risk | Notes |
|---|---|---|---|
| `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts` | 1,566 | High churn | template 27-379, inline SCSS 380-1418, logic ~150 lines |
| `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts` | 1,435 | High churn | filters/facets/sort all in one file |
| `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts` | 1,231 | **Money path** | payment + address + steps in one file |
| `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts` | 1,082 | Medium | |
| `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` | 1,030 | High churn | |
| `src/ClimaSite.Infrastructure/Data/DataSeeder.cs` | 833 | Test-data dependency | E2E/dev seeding contract |
| `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs` | 292 | **Money path** | hardcoded shipping/VAT TODOs (API-014) |
| `src/ClimaSite.Application/Features/Orders/Queries/GenerateInvoiceQuery.cs` | 375 | Medium | |
| Admin controllers (AdminQuestions/AdminReviews/AdminCategories) | ~200-290 | Known N+1 | self-documented TODOs API-008/009/010: per-item MediatR sends in loops |
| `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs` | 312 | Maintenance tax | 29 entity lists to mirror per schema change |

### Five areas to AVOID changing unless necessary (stable, high blast radius)

1. **Stripe payment path** — `WebhooksController.cs` (133), `PaymentsController.cs` (125), `Infrastructure/Services/StripePaymentService.cs`, `CreateOrderCommand.cs` (transactional order + stock reservation). Money + idempotency + webhook signatures; covered by integration/E2E tests. Touch only with the deploy-checklist/security-review skills.
2. **Auth token chain** — `Infrastructure/Services/TokenService.cs`, `Application/Auth/Handlers/RefreshTokenCommandHandler.cs`, `src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts` + `auth.service.ts`. Recently refactored to break a circular dependency (per CLAUDE.md history); refresh races are easy to reintroduce and the live handlers lack unit tests (finding 2) — fix the tests before touching the code.
3. **EF migration chain** — both `Infrastructure/Migrations/` and `Infrastructure/Data/Migrations/`. All 8 migrations + single snapshot are live; never rename IDs, delete, or 'tidy' these except via the dedicated consolidation in finding 5.
4. **ExceptionHandlingMiddleware error contract** — `{status,message,detail}` is parsed by frontend services; changing the shape outside the coordinated ProblemDetails migration (finding 6) breaks error UX silently.
5. **Guest-cart merge + DataSeeder** — `Features/Cart/Commands/MergeGuestCartCommand.cs` and `Data/DataSeeder.cs` (833 lines). Core business rule (CLAUDE.md) with subtle session-cookie edge cases; the seeder is an implicit contract for E2E tests and dev environments.

### Dead-code inventory (all verified zero-consumer)

| Item | Location | Lines (approx) |
|---|---|---|
| Repository layer | `Core/Interfaces/I*Repository.cs`, `IUnitOfWork.cs`, `Infrastructure/Repositories/*` (4 files), DI regs at `Infrastructure/DependencyInjection.cs:84-86` | ~700 |
| Duplicate Auth tree | `Application/Features/Auth/` (6 files) | ~400 |
| CachingBehavior (unregistered) + 5 ICacheableQuery markers | `Application/Common/Behaviors/CachingBehavior.cs` | ~60 + markers |
| Mapster registration | `Application/Common/Mappings/MappingConfig.cs`, DI lines 28-30, package ref | ~20 + dep |
| 6 unused directives | `shared/directives/{magnetic-hover,split-text,scroll-progress,animate-on-scroll,count-up,optimized-image}.directive.ts` | ~600 |
| Orphaned email methods | `EmailService.cs:44-66` (implemented, no callers) | live code, dead wiring |

### Documentation drift table (CLAUDE.md / AGENTS.md vs reality)

| Doc claim | Reality |
|---|---|
| "Implement repository pattern for data access" | Handlers use IApplicationDbContext directly; repositories dead |
| "exception handling with ProblemDetails" | Custom `{status,message,detail}` middleware; TODO API-012 admits it |
| E2E: `npx playwright test`, `fixtures/test-data-factory.ts`, TS example | C# xUnit + Microsoft.Playwright; PageObjects/; run via `dotnet test` |
| `tests/ClimaSite.Web.Tests` (karma.conf.js) | Does not exist; Angular tests live in src/ClimaSite.Web |
| Quick Reference lists CountUpDirective | Unused (0 references) |
| "Notifications: Partial — Email notifications implemented" | Email service exists; zero flows call it |
| "Performance Optimizations: Complete" | Redis query caching wired but inactive (behavior unregistered) |

### Agent-config tree drift (.claude / .codex / .opencode)

Three skill trees with diverging names and content: `.claude/skills/{code-review,db-migrate,deploy-checklist,release,security-review,seo-review,ui-qa,ui-ux-pro-max,agent-workflow}` vs `.codex/skills/climasite-*` (prefixed duplicates) vs `.opencode/skills/ui-ux-pro-max` (only 1 tracked file; SKILL.md differs from .claude's copy; local node_modules/bun.lock untracked). `.codex/PROJECT_MEMORY.md` is modified on this branch and exists only there. Risk: three agents operating from divergent procedures (e.g., db-migrate steps). Suggest one canonical source with a sync script, or symlinks. (P3; folded here rather than a finding since impact is workflow-only.)

### Open questions / needs-confirmation

1. **Deployment topology**: railway.toml suggests single-instance API today — confirms whether the wishlist in-process lock (finding 8) is a latent or active bug. Could not verify instance count from repo.
2. **Is `Application/Features/Notifications` (7 files) + NotificationsController actually exercised by the frontend?** Not deeply traced; if unused, it joins the dead-wiring list for Plan 12 rework.
3. **Whether E2E suite currently passes** — not run (read-only constraint); test-results/ artifacts exist but were not treated as evidence.
4. **CI coverage thresholds**: workflow collects coverage and uploads to codecov but no gate enforcing the documented 80%/70% minimums was visible in `.github/workflows/test.yml:32-43` — coverage policy appears aspirational (likely, not fully verified against codecov config).

## Refuted during verification

None.
