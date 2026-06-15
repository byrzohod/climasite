# ClimaSite — Project Status (Single Source of Truth)

**Date:** 2026-06-11
**Sources:** Consolidated from the verified review findings in `docs/project-plan/_review/` (status, product, uiux, security, architecture, bugs, testing, performance, docs, devops). All file/line evidence below was verified against the working tree on 2026-06-11.

**How to use this document:** This is the authoritative snapshot of what ClimaSite actually is, what works, what is claimed-but-not-real, and what blocks launch. Read it before trusting any other status source — the `CLAUDE.md` status table, `docs/plans/00-master-overview.md`, and `docs/plans/20-issue-registry.md` are all materially stale or wrong in places identified below (with `docs/plans/18-project-completion.md` being the one largely-honest active plan). When this document and an older doc disagree, this document wins; for per-finding evidence and recommended fixes, follow the pointers into `docs/project-plan/_review/*.md`, and for actionable work see the sibling docs in this folder (`PRIORITIZED_BACKLOG.md`, `SECURITY_REVIEW.md`, `BUGS_AND_TECH_DEBT.md`, `TESTING_STRATEGY.md`, `PRODUCTION_READINESS_CHECKLIST.md`, `UI_UX_REVIEW.md`, `ARCHITECTURE_REVIEW.md`, `DEV_WORKFLOW.md`, `DECISIONS.md`). A newcomer should be able to understand the whole project state from this file in ~10 minutes.

---

## 1. Project overview

ClimaSite is a production-grade e-commerce platform for HVAC products (air conditioners, heating, cooling) targeting EN/BG/DE markets. The stack is ASP.NET Core (.NET 10) with Clean Architecture + CQRS/MediatR (`src/ClimaSite.Api`, `src/ClimaSite.Application`, `src/ClimaSite.Core`, `src/ClimaSite.Infrastructure`), PostgreSQL 16 via EF Core 10 (note: `CLAUDE.md` wrongly says 9.x), Redis, Stripe payments, and an Angular 19 standalone-components frontend (`src/ClimaSite.Web`) using signals, Tailwind, ngx-translate (perfect 1006-key EN/BG/DE parity), and light/dark theming. Testing is xUnit (backend), Jasmine/Karma (frontend), and **C# Playwright-for-.NET E2E** run via `dotnet test tests/ClimaSite.E2E` — not the TypeScript Playwright suite the docs describe. CI (`.github/workflows/test.yml`) genuinely gates unit, integration (Testcontainers), frontend, build, and full-stack E2E. The API runs locally on port **5029** (not the documented 5000). The GitHub repo (`byrzohod/climasite`) is **public**.

The customer-facing browse-to-cart experience is genuinely strong: product list with full filtering, a rich PDP (gallery, specs, reviews, Q&A, installation upsell, similar products), guest cart, wishlist with public sharing (new, uncommitted — see §9), account area with order history/cancel/reorder/PDF invoices, brands/promotions pages, and a polished configurator-first home page (home-v3). However, the project is **not launchable**: the Stripe money path is broken end-to-end (payments never reconciled to orders, wrong amount/currency charged), the shop cannot be operated (admin products/orders/users pages are "Coming Soon" stubs), no transactional emails are ever sent, every footer legal page 404s (an EU blocker), and production startup would seed a publicly-known admin password. Plan 18 (`docs/plans/18-project-completion.md`) is the active master plan driving completion work; Phases 0–2 (Home v3 + Wishlist) are done, Notifications/Animation/security/test/prod-readiness phases remain open.

---

## 2. Overall health assessment

**Verdict: structurally healthy, functionally pre-production, documentation untrustworthy.** The architecture is consistent (correct Clean Architecture dependency directions, thin controllers, signal-based frontend), builds are clean, and the test skeleton is real and green — this is not vaporware. But the money path has compounding P0 defects the test suites do not catch, the operator side is absent, several "Complete" claims in `CLAUDE.md` and the master overview are false, and a cluster of silent dead wiring (caching, repositories, emails, a duplicate Auth tree) means the code often reads as if features work when they do not.

### Build & test status (verified 2026-06-11, from `_review/status.md` and `_review/testing.md`)

| Check | Result |
|---|---|
| `dotnet build ClimaSite.sln --no-incremental` | **Succeeded — 0 warnings, 0 errors** (all 8 projects) |
| `ng build --configuration=development` (with uncommitted wishlist changes) | **Succeeded** |
| ClimaSite.Core.Tests | 197/197 passed (last TRX 2026-06-06) |
| ClimaSite.Application.Tests | 161/161 passed (2026-06-06); new Wishlist slice **10/10 re-run passing 2026-06-11** |
| ClimaSite.Api.Tests | 61/61 passed (2026-06-06) — **predates** the 16 new wishlist tests of 2026-06-07; no recorded run for them |
| ClimaSite.E2E (208 tests) | 206 passed / 0 failed / 1 NotExecuted (`Checkout_SavedAddress_CanBeUsed`, unexplained) — 2026-06-06 |
| Frontend (941 Jasmine `it()` in 77 specs) | No persisted local run; CI job exists and was green on last main run |
| CI latest main run | Green (~14.5 min, all 5 jobs + summary) — but **8 of the 10 most recent push-to-main runs failed** (`main` is unprotected; red commits demonstrably land on it) |

**Caveats:** coverage mandates (80% backend / 70% frontend in `CLAUDE.md`) are measured and enforced **nowhere** (codecov upload is `continue-on-error`); 20 of 26 API controllers have zero integration tests; 13 of 18 Application feature areas have zero unit tests; the only completed-order E2E deliberately avoids the Stripe card iframe — so green status does not cover the money path, GDPR, or admin role gates. The mandated "full test suite" command in `CLAUDE.md` cannot run as written (root `dotnet test` includes the server-dependent E2E project; `npx playwright test` targets a suite that doesn't exist).

**Needs confirmation (could not be verified from the repo):**
- Whether anything is deployed to Railway today (determines whether the seeded `admin@climasite.local` / `Admin123!` account is an *active* P0 incident rather than a latent one) and which Railway service consumes which of the three `railway.toml` files.
- Railway replica count (affects the wishlist in-process lock and per-instance OutputCache findings).
- The Lighthouse 0.97 / 1.00 claim — plausible against the current build (~73 KB gzip initial payload) but measured only on `/`, never re-run.
- Local frontend test pass rate (941 specs never executed during the review) and whether codecov is actually configured on the GitHub repo.

---

## 3. Completed features (verified real)

| Feature | Evidence |
|---|---|
| Design system & theming (light/dark, CSS vars) | `src/ClimaSite.Web/src/styles/_colors.scss`; near-total discipline (only energy-label palettes hardcoded, and the light-theme error color fails WCAG AA contrast — `_review/uiux.md` #9). 104 hardcoded-color violations from the Jan gap report are tracked in Plan 18 Phase 3 |
| i18n EN/BG/DE | 1006 identical keys across all three languages (verified key-diff); essentially zero hardcoded user-facing strings; CI runs `scripts/check-i18n.mjs`. (Cart/wishlist API responses ignore translations — see §4) |
| Auth core (register/login/JWT/refresh/lockout) | `AuthController.cs`, live handlers in `src/ClimaSite.Application/Auth/`; Argon2 hashing, refresh-token rotation, lockout 5/15min, 22 integration tests. (Forgot-password and email verification are NOT complete — see §4) |
| Product catalog (PLP/PDP, variants, gallery, facets) | `ProductsController.cs`, `features/products/`; routed and E2E-covered |
| Shopping cart (guest session, stock-validated add) | `CartController.cs`, `features/cart/`; 18 integration tests incl. merge endpoint. (The UI→API merge call is broken — see §4) |
| Reviews & ratings + Q&A (verified purchase, votes, moderation) | `ReviewsController.cs`, `QuestionsController.cs`, `admin-moderation.component.ts` (764 lines — the one real admin page) |
| Search & navigation UI (header search, facets, mega-menu) | `header.component.ts`, `product.service.ts`. (Backend is naive ILIKE, not the claimed "full-text search" — see §6) |
| Order history & account area (cancel, reorder, tracking display, PDF invoice) | `features/account/orders/`, `GenerateInvoiceQuery.cs` (QuestPDF) |
| Addresses CRUD, profile, preferences | `AddressesController.cs`, `features/account/` |
| Brands, promotions content pages, About/Contact/Resources pages | `features/brands/`, `features/promotions/`, routed. (Contact form submission is fake — see §5) |
| Home v3 (configurator-first, real recommendations endpoint) | `features/home-v3/` committed on main (37 files); `/api/products/recommendations`; E2E in `Tests/Home`; Lighthouse mobile 0.97 / desktop 1.00 measured on `/` only (needs confirmation — never re-run) |
| Wishlist incl. public sharing, guest→login merge | `WishlistController.cs` (7 endpoints), `WishlistApplicationService.cs`, share routes, EN/BG/DE keys, 10 unit + 7 API + 5–6 E2E tests. **Entirely uncommitted — see §9** |
| Accessibility plumbing | Focus traps + restore (Modal, mini-cart), `role=alert` toasts, reduced-motion in 34 files, 44px touch targets, ~469 `data-testid`s, axe-core E2E checks |
| Frontend performance baseline | All routes lazy; initial bundle ~259 KB raw / ~73 KB gzip (rebuilt 2026-06-11), inside 650 KB budget |
| Dependency hygiene | `dotnet list package --vulnerable` and `npm audit --omit=dev` both clean (verified 2026-06-11) |

---

## 4. Partially completed features

| Feature | What exists | What's missing / broken |
|---|---|---|
| **Checkout & payments (Stripe)** | Stripe Elements UI, create-intent/cancel-intent endpoints, signature-verified webhook handler, order creation with server-side pricing | **P0:** `paymentIntentId` silently dropped (`CreateOrderCommand` has no field; `Order.SetPaymentInfo` has zero callers) → webhook never matches → every order stuck Pending forever. **P0:** charge amount is client-supplied, in BGN, excluding shipping, while orders are EUR and UI shows USD. Payment captured before order with no rollback; double-submit window; no idempotency key. (`_review/product.md` #1/#3/#11, `_review/bugs.md` #1/#2/#4) |
| **Admin panel** | Full backend admin API (AdminProducts/AdminOrders/AdminCustomers/AdminDashboard controllers); moderation UI (real); orphaned product-translation-editor + related-products-manager components | `/admin/products`, `/admin/orders`, `/admin/users` are **12-line "Coming Soon" stubs** (since 2026-01-12); dashboard renders nav tiles only (no KPIs). No UI to fulfill orders, set tracking, or edit products. Admin E2E tests work around it "via API (since UI may be placeholder)" |
| **Authentication (recovery flows)** | Reset-token generation, `/forgot-password` + `/reset-password` routes, `EmailService.SendPasswordResetEmailAsync` fully implemented | The send call is **commented out** (`ForgotPasswordCommandHandler.cs:36-40`); raw token logged at Information level (credential leak); UI shows success either way → users permanently locked out. Email verification is a ¾-built stub (no send, no frontend route, not enforced) |
| **Notifications (Plan 12)** | In-app backend CRUD (NotificationsController + commands/queries); `EmailService` with welcome/order-confirmation/shipped/reset methods | Zero email call sites except GDPR delete; zero notification producers; zero frontend (no bell, no service, no route). Admin "notify customer" checkbox is a silent no-op TODO. No background-job infrastructure to build on |
| **Cart merge on login** | Backend merge endpoint + 4 integration tests | **FE/BE contract mismatch:** frontend posts a body, backend requires `[FromQuery] guestSessionId` → every merge **400s**, swallowed by `console.warn`; guest items vanish from UI on login (recoverable server-side, but invisible to the user). No E2E covers the UI flow (`_review/bugs.md` #3, P1) |
| **Inventory management** | Backend stock tracking, cart/checkout validation, admin Inventory API endpoints | No admin/frontend inventory UI anywhere; the documented "stock reservations" **do not exist** in code (only a hardcoded `ReservedQuantity = 0` DTO field); stock decrement is read-then-write with no concurrency control → oversell under concurrent checkout (`_review/bugs.md` #5) |
| **GDPR** | Real export/delete/rights endpoints (`GdprController.cs`), password-confirmed anonymizing delete | No frontend consumer (account area has no privacy section); **zero tests at any layer** for an irreversible delete; the validation summary stale-claims the backend is missing |
| **Guest checkout** | Backend supports anonymous orders (`[AllowAnonymous]` CreateOrder + GuestSessionId) | Frontend `/checkout` route is `authGuard`-blocked; PaymentsController is `[Authorize]`; guests can't view confirmations. Docs claim it works. Needs a product decision |
| **Promotions** | Read-only content pages, seeded data | No admin CRUD, no coupons/discount codes anywhere (`Order.DiscountAmount` is never set) |
| **Installation service requests** | PDP form + POST endpoint that persists requests | No list endpoint, no admin view, no business email — every lead goes into a black hole while the UI promises "we will contact you" |
| **Performance optimizations** | Lazy routes, `@defer`, lean bundle, good DB indexes | MediatR `CachingBehavior` **never registered** — all 14 `ICacheableQuery` caches and Redis are dead code (Redis still gates `/health`); blanket 5-min OutputCache with zero invalidation; facets endpoint hydrates the entire product table per request; OnPush at ~12% vs the 70% Plan 18 target |
| **Reviews display** | Real aggregates inside the reviews section | `AverageRating`/`ReviewCount` hardcoded to 0 in every product list/search/featured/wishlist DTO → star ratings render empty sitewide |
| **Sale pricing** | Domain model is correct (`BasePrice` current, `CompareAtPrice` original) | 14 DTO projections map `SalePrice = CompareAtPrice` (inverted) while one (brand page) does the opposite → the **higher** price renders as the deal price sitewide (`_review/bugs.md` #6) |
| **Cart/wishlist i18n** | Catalog queries translate via `GetTranslatedContent(lang)` | Cart and wishlist mappers use raw `product.Name` and accept no language code → BG/DE users see English names in cart/wishlist for products translated elsewhere |

---

## 5. Missing features (do not exist at all)

- **Legal/compliance pages:** all six footer links (terms, privacy, cookies, FAQ, shipping, returns) **404** via the wildcard route; no cookie-consent banner; no German Impressum — hard EU legal floor for a BG/DE shop (`footer.component.ts:43-56` vs `app.routes.ts`).
- **Working contact form:** `contact.component.ts:384-399` fakes success with `setTimeout`; no `/api/contact` endpoint exists.
- **Real PayPal / bank-transfer payment:** offered in checkout UI but create unpaid orders with no instructions; payment method is never persisted.
- **Transactional email dispatch:** order confirmation, shipped, welcome — implemented in `EmailService.cs`, called from nowhere.
- **Background-job infrastructure:** zero `IHostedService`/Hangfire/Quartz — hard blocker for Plan 12 notifications, price-drop alerts (`NotifyOnSale` data is stored but unprocessed), guest-cart cleanup.
- **Deployment pipeline & runbook:** no `deploy.yml`, no tagged release ever, three disagreeing `railway.toml` files, drifting duplicate Dockerfiles, no migration/rollback story.
- **Observability:** console-only Serilog; the `X-Correlation-Id` convention in `CLAUDE.md` is implemented nowhere; no error tracker, metrics, tracing, alerting, or runbooks.
- **README.md / onboarding guide:** repo root has none; the only setup docs are agent files containing wrong ports and wrong test commands (`DEV_WORKFLOW.md` in this folder now fills the gap).
- **Coupons/discount codes**, **/categories index page** (routable "Coming Soon" stub), **SSR/prerender** (pure SPA; generic meta for all product URLs), **image optimization pipeline** (zero srcset/NgOptimizedImage; MinIO originals served raw), **router scroll restoration**.
- **Implemented-but-unreachable cluster** (built, wired to nothing): product comparison (`comparison.service.ts` + CompareButton), recently-viewed component, price-history (controller + recording, no UI), six dead animation directives, dead repository/UnitOfWork layer (~700 lines, zero consumers), dead duplicate Auth tree (`src/ClimaSite.Application/Features/Auth/` — and the unit tests test the dead copy), unused Mapster registration, never-used shared BreadcrumbComponent (six hand-rolled duplicates instead).

---

## 6. Claimed-vs-actual discrepancies (headline table)

From `_review/status.md` — these are the claims a newcomer must NOT trust:

| Feature | Claimed (CLAUDE.md / master overview) | Actual | Gap |
|---|---|---|---|
| **Admin panel** | "Complete — CRUD, dashboard" / "100% Full CRUD" | Products/Orders/Users pages = 12-line "Coming Soon" stubs; dashboard = links only; only moderation real. Backend API complete | **Largest claimed-vs-actual gap in the project** (P1 doc fix; P0 for product operability) |
| **Checkout & Orders** | "Complete — Stripe integration" | Payment never linked to orders; client-supplied BGN charge vs EUR order vs USD display | **Materially false — P0 cluster** |
| **Authentication** | "Complete" | Forgot-password never emails the token (logged instead); email verification stub | P1 broken flow |
| **Notifications** | Master overview: "Complete"; CLAUDE.md: "Partial — email notifications implemented" | Backend in-app CRUD only; zero producers; zero frontend; only GDPR-delete email wired | P1 overstated + three docs give three different answers |
| **Inventory** | "Complete — stock tracking, reservations" | Reservations don't exist; no concurrency control; no admin UI | P1 (oversell) |
| **Guest checkout** | "guest checkout stores email" (business rule) | Route auth-guarded; unreachable | Doc/code contradiction |
| **Search** | "Full-text search" | Naive multi-ILIKE sequential scan; no tsvector/pg_trgm/Meilisearch | P2 scalability + doc mismatch |
| **Performance** | "Complete" | CachingBehavior unregistered (Redis unused); OnPush ~12% vs 70% target; PERF-100/104 open | Overstated |
| **E2E tooling** | `npx playwright test`, `fixtures/test-data-factory.ts`, `tests/ClimaSite.Web.Tests` | C# xUnit + Microsoft.Playwright, `dotnet test`, `Infrastructure/TestDataFactory.cs`; Web.Tests doesn't exist | Commands fail as written in 3 authority docs |
| API port | localhost:5000 (CLAUDE.md) | localhost:5029 (`launchSettings.json`, CI) | Dead port in docs |
| Master overview "All Features Complete / 100%" | `00-master-overview.md:1136-1169` | Contradicted by Plan 18's own existence and the 2026-04-08 gap report; 13 of 16 plan-index links are dead | Treat 00-master-overview status sections as **false** |
| Issue registry "129 open / 0 fixed" | `20-issue-registry.md` | Several CRITICALs verified fixed (e.g. PaymentsController `[Authorize]`) or target deleted code | Frozen since 2026-01-24; unowned |
| Wishlist "Complete / DONE 2026-06-07" | CLAUDE.md, Plan 18, PROJECT_MEMORY | Genuinely implemented — but **100% uncommitted** (see §9) | Process risk |
| `X-Correlation-Id`, repository pattern, ProblemDetails, Accept-Language header, stock reservations | CLAUDE.md conventions | None implemented (handlers use `IApplicationDbContext` directly; custom `{status,message,detail}` error JSON; `?lang=` query param) | Docs describe an imagined codebase |

**Honest sources:** `docs/plans/18-project-completion.md` (open work verified accurate), `docs/audit/2026-04-08-gap-report.md` (current baseline), `.codex/PROJECT_MEMORY.md` (most accurate operational doc), `docs/validation/areas/07-wishlist.md` (refreshed 2026-06-07). The rest of `docs/validation/` is frozen at 2026-01-24 with claims now false in both directions (e.g. it says auth has zero unit tests and CI lacks E2E — both untrue now — while still listing GDPR endpoints as missing when they exist).

---

## 7. Known risks (top items across all dimensions)

| # | Risk | Dimension | Priority |
|---|---|---|---|
| 1 | Money path: unreconciled payments, tamperable client-supplied amount (€0.01 intent possible), BGN/EUR/USD mix, charge-before-order with no refund compensation | Product/Bugs/Security | **P0** |
| 2 | `DataSeeder` runs unconditionally at startup, seeding `admin@climasite.local` / `Admin123!` in every environment — and the repo is **public**, so the credential is published (`Program.cs:31-44`, `DataSeeder.cs:64-65`; `Dockerfile.api` sets `ASPNETCORE_ENVIRONMENT=Production`) | DevOps/Security | **P0** |
| 3 | No `UseForwardedHeaders` while nginx proxies all `/api` traffic → all production users share one 100 req/min rate-limit bucket (site-wide 429s at ~5–15 concurrent shoppers) and one 10/min auth bucket (trivial site-wide login lockout) | Performance/DevOps/Security | **P0** |
| 4 | Guest cart merge always 400s → first-time buyers lose their visible cart on login, silently (one-line frontend fix) | Bugs | P1 |
| 5 | IDOR: anonymous `GET /api/orders/by-number/{orderNumber}` returns full customer PII (email, phone, address, items); order numbers are semi-predictable (`GetOrderByNumberQuery.cs:43`) | Security | P1 |
| 6 | Password-reset tokens logged in plaintext + reset email never sent — account-takeover via log access plus permanent user lockout | Security/Bugs | P1 |
| 7 | Stock oversell: no concurrency token / atomic decrement on `product_variants`; "reservations" are fictional | Bugs | P1 |
| 8 | Sale-price inversion sitewide (higher price shown as deal price) — consumer-protection exposure; also empty star ratings everywhere | Bugs | P1 |
| 9 | Stripe card path has **zero** automated coverage above mocked unit tests; Payments/Webhooks/Orders/GDPR controllers have no integration tests; GDPR delete (irreversible) untested at every layer | Testing | P1 |
| 10 | EU legal floor absent: legal pages 404, no cookie consent, no order-confirmation email (distance-selling requirement), flat 20% VAT charged to DE customers (legal rate 19%), shipping untaxed | Product/Bugs | P1 |
| 11 | EF migrations run at every boot (crash-loop risk on failure); no pre-deploy migration step, no down-tests, no tags to roll back to; `main` is unprotected while CLAUDE.md mandates direct pushes (and 8/10 recent main pushes had red CI) | DevOps | P1 |
| 12 | Unit tests for login/register/refresh test the **dead** duplicate `Features/Auth` tree — the live auth handlers (`Application/Auth/`) have zero unit coverage; both trees auto-register in MediatR | Architecture/Testing | P1 |
| 13 | TestController (DB wipe via substring match + admin elevation with a hardcoded default secret) is compiled into the production image, gated only by environment-name string | Security | P1 |
| 14 | Auth refresh storm: concurrent 401s at token expiry log the user out (~every 15 min under parallel requests, incl. mid-checkout); `auth.interceptor`/`auth.guard`/`payment.service`/checkout/cart components have no unit specs | Bugs/Testing | P1/P2 |
| 15 | Checkout shipping form gives zero field-level validation feedback (dead `.field-error` CSS, disabled-while-invalid button) — direct abandonment risk on the highest-value form; cart/checkout also display USD via the bare `currency` pipe while products show EUR | UI/UX | P1 |
| 16 | Documentation actively misleads agents (TS Playwright fiction, port 5000, phantom patterns) in an agent-driven repo — every session inherits the errors | Docs | P1→P2 (fix is small) |
| 17 | Silent dead wiring invites false fixes: CachingBehavior unregistered, repository layer orphaned, Mapster unused, 5-min blanket OutputCache with no eviction (revoked wishlist share links keep serving up to 5 min) | Architecture/Performance | P2 |
| 18 | Wishlist concurrency uses a leaking static in-process semaphore — degrades to 500s under multi-instance deploys; DTO hardcodes rating fields to 0; share DTO leaks owner `UserId` to anonymous viewers (fix before merge is cheapest) | Architecture/Security | P2 |
| 19 | Secret-injection mismatch: docs promise `STRIPE_SECRET_KEY`/`SMTP_HOST` flat env vars but code reads `Stripe:SecretKey`-style section keys; non-empty dummy Stripe keys defeat fail-fast → first deploy silently runs with dummy payments | DevOps | P2 |
| 20 | Weak default JWT secret committed; env-var requirement enforced only for the literal "Production" environment name; Swagger UI public in production; refresh tokens stored plaintext | Security | P2 |
| 21 | Tracking-doc rot: validation suite frozen Jan 2026, issue registry unowned, four mutually inconsistent trackers, plan checkboxes abandoned (Plan 12: 0/163 checked despite shipped code; "complete" plans 21H/21I carry 100+ unchecked boxes) | Docs | P2 |

---

## 8. Major blockers (must clear before launch)

1. **Fix the payment pipeline as one refactor:** persist `PaymentIntentId`/`PaymentMethod` on orders; compute amount + currency server-side from the order total (decide store currency: EUR vs BGN — open owner question); create order before capture or auto-refund on failure; verify webhook→Paid transition with integration tests. Without this, nothing else matters (`_review/product.md` §D).
2. **Make the shop operable:** build admin orders page (status + tracking) first, then products CRUD (wire the orphaned editor components), then customers — or there is no fulfillment path at all.
3. **Environment-gate the DataSeeder** and bootstrap prod admin from env vars; if anything was ever deployed to Railway, rotate/delete `admin@climasite.local` immediately (needs confirmation: is Railway live?).
4. **Add `UseForwardedHeaders`** before the rate limiter (~10 lines) — prevents a guaranteed launch-day outage.
5. **Fix the cart-merge contract** (one-line frontend fix: `POST /api/cart/merge?guestSessionId=...`) + add the guest→login merge E2E.
6. **EU legal floor:** legal/consent pages, real contact endpoint, order-confirmation email, password-reset email, correct DE VAT.
7. **Ship the wishlist branch** (commit → PR → CI) — see §9.
8. **Deploy pipeline:** one Dockerfile + railway config per service, `deploy.yml` gated on tests, explicit migration step, branch protection on `main`, first version tag.

---

## 9. Uncommitted work status — `feature/plan18-wishlist-completion`

**The entire Plan 18 Phase 2 wishlist slice, marked "DONE 2026-06-07" in three documents, exists only as uncommitted working-tree changes.** Verified 2026-06-11:

- Branch HEAD == `main` == `5166b6d` — **zero commits on the branch**; origin has no such branch; no stash or commit anywhere contains the work. The ~10 untracked files (incl. `WishlistApplicationService.cs`, `ClearWishlistCommand.cs`, `SetWishlistSharingCommand.cs`, `RegenerateWishlistShareTokenCommand.cs`, `GetSharedWishlistQuery.cs`, `WishlistControllerTests.cs`, `WishlistHandlersTests.cs`, two frontend specs, `docs/plans/archive/13-wishlist.md`) are **unrecoverable from git** — one `git checkout .` from loss.
- Working tree: 23 modified files (+1,101 / −3,450) + the untracked files. The slice is a complete, coherent implementation (7 controller endpoints, app service, FE service/component with share + guest-merge, header badge, `/wishlist` + `/wishlist/shared/:shareToken` routes, 8 i18n keys × 3 languages, doc updates), **not WIP**. Both builds pass with it.
- Test evidence is partial: Application wishlist tests re-run 10/10 on 2026-06-11, but the 7 new API integration tests and 5–6 wishlist E2E tests have **no recorded execution** (all TRX files predate them). CLAUDE.md's "full local test coverage" claim is therefore partially unevidenced — opening the PR lets CI close that gap.
- Plan 18's own Definition of Done ("Branch merged to main via PR") is unmet; the DONE markers, CLAUDE.md "Wishlist | Complete" row, and `.codex/PROJECT_MEMORY.md` claims are themselves part of the uncommitted diff (HEAD's CLAUDE.md still says "Wishlist | Partial").
- **Pre-merge fix-it-cheap-now items flagged by review:** surface wishlist API errors + Clear All confirmation in `wishlist.component.ts`/`wishlist.service.ts` (`_review/uiux.md` #4), drop owner `UserId` from the anonymous shared DTO (`_review/security.md` #10), populate or remove the hardcoded `AverageRating`/`ReviewCount` (`_review/architecture.md` #9), add a CHANGELOG entry (`_review/docs.md` #14), and ideally replace the static semaphore with the existing DB unique-index guard (`_review/bugs.md` #9).

**Action:** commit on the branch, push, open the PR, let CI validate (that also closes the test-evidence gap), then merge. Effort: small; risk of inaction: total loss of ~4 days of multi-layer work that every status doc already asserts as done.
