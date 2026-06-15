# Project Status & Docs-vs-Code Truth — Review Findings (2026-06-11)

## Summary

The repo is in better shape than a typical "claims Complete everywhere" project — both builds are genuinely green (`dotnet build`: 0 errors / 0 warnings; `ng build` development: succeeds with the uncommitted wishlist changes), the E2E suite is real C#/Playwright with a TestDataFactory, CI runs unit + integration + frontend + E2E, and the recently "DONE" Home v3 and Wishlist slices are substantively implemented with tests at every level.

However, the status documentation materially overstates completeness in several places:

1. **The single biggest mismatch:** the Admin Panel is claimed "Complete — CRUD, dashboard" in CLAUDE.md and "Full CRUD operations" in the master overview, but `/admin/products`, `/admin/orders`, and `/admin/users` are literal 12-line "Coming Soon" stub components (only moderation is real), and the admin E2E tests work around this by doing CRUD "via API (since UI may be placeholder)".
2. The forgot-password flow generates a reset token but the email send is commented out, so the claimed-Complete auth feature has a dead end-user flow.
3. Notifications status is contradictory across docs (master overview says Complete, CLAUDE.md says Partial; reality is backend-only with zero frontend and transactional emails wired only to GDPR deletion).
4. The entire wishlist completion slice marked "DONE 2026-06-07" exists only as uncommitted working-tree changes on a branch with zero commits (branch HEAD == main), violating Plan 18's own Definition of Done and risking loss.

There are also several orphaned/backend-only areas (PriceHistory, GDPR, Inventory admin endpoints, unreachable admin sub-components), an issue registry frozen at "129 open / 0 fixed" that no longer reflects code, and CLAUDE.md documenting a TypeScript Playwright E2E suite that does not exist. Plan 18 itself is honest about what remains (Notifications, Animation 21F, phases 3–8), and those open claims verified accurately against code.

## Findings

### 1. Admin Panel claimed Complete but products/orders/users management UIs are "Coming Soon" stubs

- **Finding:** The Admin Panel is claimed Complete ("CRUD, dashboard" / "Full CRUD operations"), but the products, orders, and users management pages are 12-line "Coming Soon" stub components; only moderation is a real UI.
- **Category:** claimed-vs-actual mismatch
- **Severity/Priority:** P1 — verification: adjusted
- **Evidence:** `src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts:9` renders `{{ 'common.comingSoon' | translate }}` (whole component is 12 lines); identical stubs at `features/admin/users/admin-users.component.ts:9` and `features/admin/orders/admin-orders.component.ts:9`. `admin-dashboard.component.ts` is 77 lines of nav tiles only — no stats despite AdminDashboardController existing. Only `admin/moderation/admin-moderation.component.ts` (764 lines) is real. Claims: CLAUDE.md status table "Admin Panel | Complete | CRUD, dashboard"; `docs/plans/00-master-overview.md:1142` "Admin Panel: Full CRUD operations for products, orders, customers". The E2E test itself admits the gap: `tests/ClimaSite.E2E/Tests/Admin/AdminPanelTests.cs:113` comment "Create product via API (since UI may be placeholder)", and AdminPanelTests.cs:102-103 only asserts the stub H1 contains "Product". Git history shows the stubs have existed since cbfb6e8 (2026-01-12); commit a065dde only i18n-ified the stub text.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/orders/admin-orders.component.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/users/admin-users.component.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/00-master-overview.md`
  - `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Tests/Admin/AdminPanelTests.cs`
- **Why it matters:** A "production-grade online shop" cannot be operated without an admin UI for products, orders, and users; the backend admin API exists (AdminProductsController, AdminOrdersController, AdminCustomersController) but is only drivable via raw HTTP. Every status document asserts this is done, so planning built on those documents (e.g., Plan 18 heading to v1.0.0) is built on a false premise. This is the largest claimed-vs-actual gap in the project.
- **Recommended fix:** Correct CLAUDE.md and 00-master-overview.md immediately (Admin Panel → "Backend complete; frontend: moderation only — products/orders/users UI missing"). Add an explicit Plan 18 phase (or new plan) for admin frontend CRUD: product list/create/edit (reusing the orphaned RelatedProductsManagerComponent and ProductTranslationEditorComponent), order management, and customer management, with real E2E tests replacing the API-workaround tests. (Effort: Large)
- **Acceptance criteria:** CLAUDE.md/master-overview no longer claim Admin Panel complete; `/admin/products`, `/admin/orders`, `/admin/users` render functional CRUD UIs backed by the existing admin API; AdminPanelTests exercise the UI (no "via API since UI may be placeholder" workarounds).
- **Dependencies or follow-up:** Backend admin API already exists; depends on prioritization against Plan 18 phases 3–8.
- **Confidence:** verified. Verifier confirmed every evidence point (the stubs are the only routed UIs via admin.routes.ts:10-20; no alternative admin UI exists; RelatedProductsManagerComponent/ProductTranslationEditorComponent are orphaned) and one pass rated it P0 as the largest docs-vs-code gap; adjusted to P1 because the stubs sit behind adminGuard, are customer-invisible and self-announcing, the app is pre-launch (admin API drivable via Swagger), and the urgent doc correction is a minutes-long edit — release-blocking, not an active emergency.

### 2. Forgot-password flow never sends the reset email — token is only written to server logs

- **Finding:** The forgot-password flow generates a reset token but never sends the email — the send call is commented out and the token is only written to server logs, leaving the claimed-Complete auth feature with a dead end-user flow.
- **Category:** broken core flow / claimed-vs-actual
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs:14` "// TODO: Inject email service when implemented"; line 36 "// TODO: Send email with reset link"; line 37 logs the reset token; line 40 the actual send call is commented out: `// await _emailService.SendPasswordResetEmailAsync(...)`. Yet `IEmailService.SendPasswordResetEmailAsync` exists (`src/ClimaSite.Application/Common/Interfaces/IEmailService.cs:8`) and EmailService is registered (`src/ClimaSite.Infrastructure/DependencyInjection.cs:69`). CLAUDE.md claims "Authentication | Complete"; frontend routes `/forgot-password` and `/reset-password` are wired in `app.routes.ts:23-33`.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Common/Interfaces/IEmailService.cs`
- **Why it matters:** A real user who forgets their password hits a dead end: the UI says the email was sent, but nothing is delivered in any environment. The feature is claimed Complete in CLAUDE.md and plan 03 is archived as done. Logging reset tokens to application logs is also a credential-exposure smell (matches open SEC-006 in the validation summary).
- **Recommended fix:** Inject IEmailService into ForgotPasswordCommandHandler, call SendPasswordResetEmailAsync with a frontend reset link, stop logging the raw token, and add a handler unit test plus an E2E covering the full reset flow (can read email via MailHog in dev/shared-infra). (Effort: Small)
- **Acceptance criteria:** Forgot-password request produces a delivered email containing a working `/reset-password` link; raw token no longer appears in logs; unit + E2E tests cover the flow.
- **Dependencies or follow-up:** Email provider decision is tracked as Plan 18 NOT-100, but the existing SMTP EmailService is sufficient for an interim fix.
- **Confidence:** verified. Verifier confirmed all evidence: a complete EmailService implementation exists (Infrastructure/Services/EmailService.cs:51-57), is DI-registered, and has zero production callers; AuthController.cs:88 even returns "a password reset link has been sent" to the client, with no alternative send path anywhere. P1 correctly calibrated — broken core flow plus token-in-logs exposure, but pre-production with a small fix, so not P0.

### 3. Notifications status contradictory across docs and overstated everywhere: zero frontend, transactional emails wired only to GDPR deletion

- **Finding:** Notifications status is contradictory across docs (master overview: Complete; CLAUDE.md: Partial) and overstated everywhere — reality is backend-only with zero frontend, and transactional emails are wired only to GDPR deletion.
- **Category:** claimed-vs-actual mismatch
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `docs/plans/00-master-overview.md:183` lists plan 12 Notifications as "Complete" and :1153 says "Notifications: Complete". CLAUDE.md says "Notifications System | Partial | Email notifications implemented". Reality: the only live `_emailService` call in the codebase is `src/ClimaSite.Application/Features/Gdpr/Commands/DeleteUserDataCommand.cs:170`; SendWelcomeEmailAsync/SendOrderConfirmationEmailAsync/SendOrderShippedEmailAsync have zero call sites (grep across src/ confirmed); `src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs:73` still has "TODO: If NotifyCustomer is true, send email notification". Frontend: no NotificationService, bell, dropdown, center page, or preferences UI exists anywhere (`grep -ril notification` in src/ClimaSite.Web/src/app matched only toast/product-detail/qa files); account.routes.ts has no notifications route. Backend in-app pieces do exist: NotificationsController + 4 commands + 2 queries (`src/ClimaSite.Application/Features/Notifications/`).
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/00-master-overview.md`
  - `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Gdpr/Commands/DeleteUserDataCommand.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Notifications`
- **Why it matters:** Three documents give three different statuses (Complete / Partial-with-emails / ~15% per the April gap report). The CLAUDE.md note "Email notifications implemented" is misleading — no order/welcome/shipped email is ever sent. Anyone trusting the master overview would skip Plan 18 Phase 2 NOT-* work entirely. Customers receive no order confirmation emails on a live shop.
- **Recommended fix:** Fix 00-master-overview.md (Notifications → Partial) and reword CLAUDE.md to "backend in-app CRUD + email service exist; no email dispatch from order/auth flows; no frontend". Then execute Plan 18 NOT-100..111 (provider decision, event handlers, preferences API, frontend bell/center, tests). (Effort: Large)
- **Acceptance criteria:** All status docs agree Notifications is Partial with an accurate scope note; order placement sends a confirmation email in an E2E test; frontend notification UI exists per NOT-106.
- **Dependencies or follow-up:** Plan 18 NOT-100 batched decisions (email provider, template engine, background worker) must be answered by the user first.
- **Confidence:** verified. Verifier confirmed every cited fact and noted the finding is actually slightly understated — CreateNotificationCommand is never invoked outside the Notifications feature, so no business flow creates in-app notifications either. Plan 18 accurately tracking the gap prevents P0, but P1 stands because the canonical status table and CLAUDE.md (loaded every session) misrepresent a customer-facing transactional-email gap.

### 4. Entire "DONE 2026-06-07" wishlist completion slice exists only as uncommitted working-tree changes; branch has zero commits

- **Finding:** The entire wishlist completion slice marked "DONE 2026-06-07" (and Complete in CLAUDE.md and PROJECT_MEMORY) exists only as uncommitted working-tree changes; the feature branch has zero commits (HEAD == main), no push, no PR, no CI.
- **Category:** status risk / process
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `git rev-parse` shows feature/plan18-wishlist-completion HEAD == main == 5166b6d (no commits on branch). `git status`: 23 modified files (+1,101/−3,450) plus ~10 untracked files (WishlistApplicationService.cs, ClearWishlistCommand.cs, SetWishlistSharingCommand.cs, RegenerateWishlistShareTokenCommand.cs, GetSharedWishlistQuery.cs, WishlistControllerTests.cs, WishlistHandlersTests.cs, wishlist.service.spec.ts, wishlist.component.spec.ts, docs/plans/archive/13-wishlist.md). Yet `docs/plans/18-project-completion.md:184-194` marks WISH-100..108 "DONE 2026-06-07", CLAUDE.md marks Wishlist "Complete", and `.codex/PROJECT_MEMORY.md:34` says the slice is complete — all of those doc edits are themselves uncommitted. Plan 18's own DoD (`18-project-completion.md:440` "Branch merged to main via PR") is unmet. The slice itself is substantive and coherent, not WIP: 7 controller endpoints (WishlistController.cs:21-86), application service, 7 API integration tests, 10 handler tests, 5 E2E tests (WishlistTests.cs:37-137), FE service with share/merge (wishlist.service.ts:175-303), header badge (header.component.ts:89-94), `/wishlist` + `/wishlist/shared/:shareToken` routes (app.routes.ts), and 8 wishlist i18n keys in each of EN/BG/DE. Both `dotnet build` and `ng build` pass with these changes.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/WishlistController.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/wishlist.service.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/.codex/PROJECT_MEMORY.md`
- **Why it matters:** Four days of completed, multi-layer work (working tree dated ~2026-06-07, today 2026-06-11) is one `git checkout .` away from loss, and every status document already asserts it as done — if the tree were reset, all docs would be wrong with no trace. It also can't be code-reviewed, CI-validated, or merged in its current state.
- **Recommended fix:** Commit the slice on feature/plan18-wishlist-completion (conventional commit), push, open a PR to main, and let CI validate. Only then should the DONE markers be considered accurate. (Effort: Small)
- **Acceptance criteria:** Branch contains the wishlist commits, CI green on the PR, slice merged to main; plan 18 Phase 2 wishlist DoD item "merged via PR" actually satisfied.
- **Dependencies or follow-up:** None — work is finished; claimed local test runs (Karma 970, E2E 213/213) should be re-confirmed by CI.
- **Confidence:** verified. Verifier confirmed all evidence: the branch is not on origin, all 10 DONE lines exist only in the uncommitted diff (HEAD's CLAUDE.md still says "Wishlist | Partial"), and no stash or commit anywhere contains the work. One minor flaw noted — a tree reset would also revert the uncommitted doc edits, so docs wouldn't be left "wrong with no trace" — but the core exposure (finished work one reset from loss, unreviewable/unmergeable, violating CLAUDE.md's own "NON-NEGOTIABLE" commit-and-push workflow) stands; P1 with a small fix is correctly calibrated.

### 5. DataSeeder runs unconditionally at startup in every environment, seeding hardcoded admin credentials (Admin123!) and demo data — not tracked by Plan 18

- **Finding:** DataSeeder runs unconditionally at startup in every environment, seeding a hardcoded admin account (`admin@climasite.local` / `Admin123!`) and demo catalog data, and no Plan 18 task tracks fixing this before production.
- **Category:** status risk / untracked production blocker
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** `src/ClimaSite.Api/Program.cs:31-44`: `await SeedDatabaseAsync(app)` runs before pipeline configuration with no environment guard; `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:27-38` SeedAsync() does MigrateAsync + SeedAdminUserAsync + SeedBrands/Categories/Products/Promotions in all environments; DataSeeder.cs:64-65 hardcodes adminEmail `admin@climasite.local` / adminPassword `Admin123!`. Plan 18 Phase 5 (SEC-100..107, 18-project-completion.md:264-294) covers JWT/Stripe secrets, headers, CORS, Swagger, rate limits — but not the seeded admin account or prod demo data; PROD-102 only covers the MigrateAsync-at-startup part. Validation summary lists it merely as "SEC-007 Admin password hardcoded in seeder | Low | Open".
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/DataSeeder.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md`
- **Why it matters:** If the existing railway.toml deploy ever goes live as-is, production launches with a publicly-guessable admin account and demo catalog data. Because no plan task tracks it, Plan 18 could complete all listed phases and still ship this. Rated Low in the validation summary, which understates it for a production target.
- **Recommended fix:** Gate demo seeding behind Development/Testing; in production require admin bootstrap credentials from environment variables (fail startup if absent) or a one-time setup flow; add this as an explicit SEC task in Plan 18 Phase 5. (Effort: Small)
- **Acceptance criteria:** Production startup performs no demo seeding and creates no admin user with a hardcoded password; a Plan 18 task tracks and closes this; integration test asserts seeder behavior per environment.
- **Dependencies or follow-up:** Plan 18 Phase 5; PROD-102 migration-strategy work touches the same code path.
- **Confidence:** verified. Verifier confirmed every cited fact: no env checks or config flags anywhere (DataSeeder registered unconditionally at Infrastructure/DependencyInjection.cs:89), the seeded admin has EmailConfirmed=true, and railway.toml is a real deploy target. Caveat: the issue is already tracked as SEC-01 (P0) in docs/project-plan/PRIORITIZED_BACKLOG.md and SR-01 in SECURITY_REVIEW.md, so it is known outside Plan 18 — but the code remains unfixed, the Plan 18 gap is real, and P1 is correct since deployment status is unconfirmed (backlog open question O-7).

### 6. GDPR backend endpoints (export/delete) have no frontend consumer — compliance claim is half-implemented

- **Finding:** GDPR export/delete/data-categories/rights endpoints are fully implemented in the backend but have no frontend consumer, so the compliance claim is half-implemented and users cannot exercise their GDPR rights via the UI.
- **Category:** partially implemented / orphaned
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `src/ClimaSite.Api/Controllers/GdprController.cs:28,53,85,98` implements GET /api/gdpr/export, DELETE /api/gdpr/delete, data-categories, rights — real handlers exist (DeleteUserDataCommand even sends a confirmation email). But grep for `gdpr|data-export|deleteAccount|delete-account` across src/ClimaSite.Web/src/app returns zero hits; account.routes.ts has only profile/orders/addresses. CLAUDE.md "Key Business Rules" states "Customer data must comply with GDPR". Note the validation summary is stale in the opposite direction: SEC-003 "No GDPR endpoints (data export/delete)" is still listed Open (00-validation-summary.md:111) though the endpoints exist.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/GdprController.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/account/account.routes.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/docs/validation/00-validation-summary.md`
- **Why it matters:** GDPR rights (export, erasure) must be user-exercisable; an API endpoint users can't reach via UI does not satisfy the business rule. Meanwhile the validation summary mis-reports the backend as missing, so the actual remaining work (frontend) is invisible in tracking docs.
- **Recommended fix:** Add a privacy section to the account area (export my data, delete my account) calling the existing endpoints, with confirmation UX and i18n; update validation summary SEC-003 to "backend done, frontend missing" until then. (Effort: Medium)
- **Acceptance criteria:** Authenticated user can export data and request deletion from /account; E2E covers the deletion flow; validation summary updated.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 7. Orphaned backend features: PriceHistory has zero frontend consumers; Inventory admin endpoints have no admin UI anywhere

- **Finding:** PriceHistory has zero frontend consumers and the Inventory admin endpoints have no admin UI anywhere — both are orphaned backend features despite "Complete" claims.
- **Category:** abandoned / orphaned areas
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** PriceHistoryController.cs exists and UpdateProductCommand.cs:120-126 records ProductPriceHistory on price changes, but grep `price-history|priceHistory|PriceHistory` across src/ClimaSite.Web/src/app returns nothing. InventoryController.cs and Features/Inventory handlers exist, but grep `api/inventory|InventoryService` across the whole Angular app returns nothing, and features/admin contains no inventory module — yet CLAUDE.md claims "Inventory Management | Complete | Stock tracking, reservations" and archived plan 09 (`docs/plans/archive/09-inventory-management.md:484-496`) specified 14 admin inventory endpoints implying an admin workflow. Plan 18 TST-103 also lists "Inventory low-stock → reorder admin flow" as a missing E2E — there is no UI to E2E-test.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PriceHistoryController.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/InventoryController.cs`
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/archive/09-inventory-management.md`
- **Why it matters:** These are dead API surface from the user's perspective: stock adjustments, low-stock alerts, and price-history audit data are recorded but unviewable and unmanageable. "Complete" for Inventory is only true for the transactional backend (cart/checkout stock validation), not the management feature the plan described.
- **Recommended fix:** Decide explicitly: either descope (mark "backend-only by design" in CLAUDE.md and archive the admin-UI ambitions) or schedule admin inventory + price-history views as part of the admin-panel completion work (see finding 1). Don't leave them unlabeled. (Effort: Medium)
- **Acceptance criteria:** Each orphaned controller is either consumed by a UI or documented as intentionally headless; CLAUDE.md status table reflects the decision.
- **Dependencies or follow-up:** Admin panel UI work (finding 1).
- **Confidence:** verified

### 8. Unreachable admin components: RelatedProductsManagerComponent and ProductTranslationEditorComponent are imported nowhere; admin product search is a deliberate no-op

- **Finding:** RelatedProductsManagerComponent and ProductTranslationEditorComponent are imported nowhere (their intended host /admin/products is a stub), and the related-products manager's product search is a deliberate no-op.
- **Category:** abandoned / dead code
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** grep for `RelatedProductsManagerComponent|ProductTranslationEditorComponent` across src/ClimaSite.Web/src/app (excluding their own files/specs) returns zero usages — they are not routed or embedded anywhere, because their intended host (/admin/products) is a stub. Both have full services + specs (~6 files under features/admin/products/). Additionally `related-products-manager.component.ts:515-527` searchProducts() is a stub: "// TODO: Implement actual product search ... this.searchResults.set([])". Plan 17 marks "Phase 4: Related Products ✅ COMPLETE" (`docs/plans/17-future-enhancements.md:14`) — true for the shopper-facing side (product-detail.component.ts imports SimilarProducts/ProductConsumables, 3 refs) but the admin management side is unreachable dead code.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/products/components/related-products-manager/related-products-manager.component.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/admin/products/components/product-translation-editor/product-translation-editor.component.ts`
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/17-future-enhancements.md`
- **Why it matters:** Maintained, tested components that no user can reach are silent debt: their specs run in CI and create the illusion of coverage for features that are effectively absent. The no-op search would also be a confusing broken control if the components ever get wired in.
- **Recommended fix:** Wire both components into the future admin products page (natural fit with the finding-1 admin work) and implement searchProducts() against the existing admin products list endpoint; or delete them if descoped. (Effort: Medium)
- **Acceptance criteria:** Components reachable from /admin/products with functional search, or removed; no orphaned admin components remain.
- **Dependencies or follow-up:** Admin products page implementation.
- **Confidence:** verified

### 9. 00-master-overview.md "Current Project Status (January 2026)" section is materially false and contradicts CLAUDE.md

- **Finding:** The "Current Project Status (January 2026)" section of 00-master-overview.md is materially false ("100% Complete", "All Features Complete", stale test counts) and contradicts CLAUDE.md and Plan 18.
- **Category:** documentation mismatch
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `docs/plans/00-master-overview.md:1136` "Frontend Implementation: 100% Complete", :1144 "Backend Implementation: 100% Complete", :1153 "Notifications: Complete", :1168-1169 "All Features Complete"; :1141/:1161 stale counts ("213/213" frontend tests vs ~970 claimed in `.codex/PROJECT_MEMORY.md:41`; "167 Core tests" vs 199). The same file's index (:183) marks plan 12 "Complete" while CLAUDE.md marks it "Partial" and Plan 18 lists all NOT-* tasks open. The index also links plans 01-11/15/16 at ./NN-*.md paths though those files now live in docs/plans/archive/.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/00-master-overview.md`
  - `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`
- **Why it matters:** This is the named "master" overview; agents and reviewers are pointed at it first. Its "All Features Complete" verdict directly contradicts the active Plan 18 (which exists precisely because features are incomplete) and helped produce the inflated Admin/Notifications claims.
- **Recommended fix:** Replace the January 2026 status section with a pointer to CLAUDE.md's status table + Plan 18 as the single source of truth; fix the plan-index statuses (12 → Partial) and archive links. (Effort: Small)
- **Acceptance criteria:** Master overview contains no status claims that contradict CLAUDE.md/Plan 18; links resolve.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 10. Issue registry (20-issue-registry.md) frozen at "129 Open / 0 Fixed" and no longer reflects code

- **Finding:** The issue registry (20-issue-registry.md) is frozen at "129 Open / 0 Fixed" (9 CRITICAL) and no longer reflects the code — spot-checked CRITICALs are fixed or target deleted code.
- **Category:** stale tracking
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `docs/plans/20-issue-registry.md:10-16` claims 129 total issues, 129 Open, 0 Fixed (9 CRITICAL). Spot-checks contradict it: CRITICAL API-002 "PaymentsController missing authorization" is fixed — `src/ClimaSite.Api/Controllers/PaymentsController.cs:10` has class-level `[Authorize]`; CRITICAL HOME-003 "Hardcoded Bulgarian testimonials" targets the old home page that was deleted in Plan 18 FND-004 (features/home/ no longer exists). The registry has had zero status updates since creation while at least two waves of fixes (validation-summary fixes 2026-01-24, Plan 18 work) landed.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/docs/plans/20-issue-registry.md`
  - `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs`
- **Why it matters:** A registry that says 9 CRITICALs are open when some are fixed/obsolete makes real risk triage impossible and erodes trust in all tracking docs. The project now has four overlapping, mutually inconsistent trackers (validation areas, gap report, issue registry, Plan 18).
- **Recommended fix:** Re-triage the registry once: mark fixed/obsolete items (verify each against code), or formally supersede the registry with Plan 18 and stamp it "archived — superseded by docs/plans/18-project-completion.md". (Effort: Medium)
- **Acceptance criteria:** Registry statistics match a fresh code audit, or the file is archived with a supersession note; one authoritative tracker remains.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 11. CLAUDE.md documents a TypeScript Playwright E2E suite that does not exist (actual suite is C# xUnit + Microsoft.Playwright)

- **Finding:** CLAUDE.md documents a TypeScript Playwright E2E suite (npx playwright commands, fixtures/test-data-factory.ts, TS example code) that does not exist; the actual suite is C# xUnit + Microsoft.Playwright run via `dotnet test`.
- **Category:** documentation mismatch
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** CLAUDE.md Commands section instructs `cd tests/ClimaSite.E2E && npx playwright install / npx playwright test` and the Quick Reference table lists "Test Factory | tests/ClimaSite.E2E/fixtures/test-data-factory.ts". Reality: tests/ClimaSite.E2E contains ClimaSite.E2E.csproj referencing Microsoft.Playwright 1.49.0 + xunit 2.9.2 (csproj lines 16-17), Infrastructure/TestDataFactory.cs and Infrastructure/PlaywrightFixture.cs; there is no package.json, playwright.config.ts, or fixtures/ directory. CLAUDE.md's own "Post-Implementation Workflow" contradicts its Commands section by using `dotnet test` for the same directory. The TypeScript example test code in CLAUDE.md ("Test Data Factory Pattern") is likewise fictional for this repo.
- **Affected files/areas:**
  - `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`
  - `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/ClimaSite.E2E.csproj`
  - `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`
- **Why it matters:** CLAUDE.md is the operating manual for every agent session; the documented E2E commands fail outright (`npx playwright test` finds no config), wasting agent cycles and risking wrong conclusions ("E2E suite broken"). The root AGENTS.md gets it right ("Playwright (NO MOCKING)" under a dotnet test layout), deepening the inconsistency.
- **Recommended fix:** Rewrite CLAUDE.md E2E sections: commands (`dotnet test tests/ClimaSite.E2E`), factory path (Infrastructure/TestDataFactory.cs), and replace the TypeScript example with the actual C# pattern; mention the Testing-environment env vars documented in `.codex/PROJECT_MEMORY.md:13`. (Effort: Small)
- **Acceptance criteria:** Every command in CLAUDE.md's E2E sections executes successfully as written; file paths in Quick Reference exist.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

## Dimension data

### Build & Working-Tree State (verified 2026-06-11)

| Check | Result |
|---|---|
| `dotnet build ClimaSite.sln --no-incremental` | **Succeeded — 0 warnings, 0 errors** (all 8 projects) |
| `ng build --configuration=development` (with uncommitted changes) | **Succeeded** |
| Branch | `feature/plan18-wishlist-completion`, HEAD == `main` == 5166b6d (**zero commits on branch**) |
| Uncommitted | 23 modified files (+1,101 / −3,450) + ~10 untracked (wishlist commands/service, 4 test files, archived plan 13) |
| Characterization | **Complete, coherent slice — not WIP** (controller + app service + FE service/component + 7 API tests + 10 handler tests + 5 E2E tests + i18n parity + doc updates). But unmerged, un-reviewed, un-CI'd, and 4 days unpushed. |

### Feature Status Matrix (Claimed vs Actual)

| Feature | Claimed (CLAUDE.md) | Actual | Evidence | Gap |
|---|---|---|---|---|
| Design system / theming | Complete | Real (light/dark, _colors.scss) but 104 hardcoded-color violations per gap report; Plan 18 Phase 3 open | docs/audit/2026-04-08-gap-report.md §4 | Hardening pending (tracked) |
| i18n EN/BG/DE | Complete | Real; key parity passes; 25+8 hardcoded strings remain (tracked Phase 3) | gap report §3 | Tracked |
| Authentication | Complete | Login/register/JWT/refresh real; **forgot-password never emails token** | ForgotPasswordCommandHandler.cs:36-40 | **P1 broken flow** |
| Product catalog | Complete | Real: ProductsController, product-list/detail, variants, gallery | routes + components verified | — |
| Shopping cart | Complete | Real: CartController, cart component, merge wired (validation fix 2026-01-24) | controllers/routes | — |
| Checkout & orders (Stripe) | Complete | Real: PaymentsController ([Authorize]), WebhooksController, checkout flow, E2E folder | PaymentsController.cs:10 | TODOs: hardcoded shipping/tax/currency (CreateOrderCommand.cs:162-200) |
| **Admin panel** | **Complete (CRUD, dashboard)** | **Products/Orders/Users pages = 12-line "Coming Soon" stubs; dashboard = links only; moderation real (764 ln). Backend admin API real.** | admin-*.component.ts:9; AdminPanelTests.cs:113 | **P0 mismatch** |
| Reviews & ratings + Q&A | Complete | Real: ReviewsController/QuestionsController + product-reviews/product-qa components; voting implemented | controllers + shared components | — |
| Search & navigation | Complete | Real: header search box, searchProducts API, facets in product service | header.component.ts:69; product.service.ts:66-73 | — |
| Inventory management | Complete | Backend stock tracking/reservation real; **zero frontend/admin inventory UI** (`grep api/inventory` in web = 0 hits) | InventoryController.cs vs web grep | **P2 backend-only** |
| **Notifications** | Partial ("email notifications implemented") | Backend in-app CRUD only; EmailService exists but called solely by GDPR delete; **no FE at all**; master overview falsely says "Complete" | Features/Notifications/; grep results | **P1 overstated + doc conflict** |
| Wishlist | Complete | Genuinely complete in working tree — **but 100% uncommitted** | git status; WishlistController.cs:21-86 | **P1 process risk** |
| Motion/animation | Complete (21F Partial) | Directives removed as claimed (Floating/Tilt/Parallax gone from shared/directives/); ANIM-100..106 open; no docs/design/motion.md | directives dir listing | Consistent |
| Home v3 | Complete | Real: features/home-v3 committed on main (37 files), recommendations endpoint, E2E Tests/Home | git ls-files; app.routes.ts:6-9 | Lighthouse/test-pass claims not re-runnable here |
| GDPR | (business rule) | Backend export/delete/rights endpoints real; **no frontend consumer** | GdprController.cs:28-98 | **P2** |
| PriceHistory | (untracked) | Recorded on product update (UpdateProductCommand.cs:120-126); controller exists; **zero consumers** | grep web = 0 hits | **P2 orphan** |
| Promotions | (17 Complete) | Real: PromotionsController + features/promotions routed | app.routes.ts | — |
| Installation | (untracked) | Real + wired: installation.service.ts consumed by product-detail | features/products/services | — |
| About / Contact / Resources | (17 Complete) | Real pages, routed (343/400/488 lines) | app.routes.ts | Hardcoded strings in about (gap report) |

### Plan 18 — Remaining Work, Verified Against Code

| Item | Plan says | Code check |
|---|---|---|
| Phase 0/1 (Home v3) | Done | Verified committed on main; routes/components/E2E exist |
| Phase 2 Wishlist | Done 2026-06-07 | Implemented but **uncommitted** (see finding) |
| Phase 2 Notifications NOT-100..111 | Open | Confirmed open: no email dispatch from flows, no FE |
| Phase 2 Animation ANIM-100..106 | Open | Confirmed: no docs/design/motion.md; phases 1-2 deletions verified |
| Phase 5 Security | Open | Confirmed: JWT secret still at appsettings.json:22, Stripe test keys :17-19, Swagger unconditional (Program.cs:272), CORS `.AllowAnyHeader()` (Program.cs:~103), no security-headers middleware. CHANGELOG's claim that prod requires JWT_SECRET is implemented via `JwtConfiguration.ResolveSecret` (Program.cs:56) |
| Phase 7 Prod readiness | Open | Confirmed: no deploy.yml; seeding+MigrateAsync at startup (Program.cs:31-44); **untracked: hardcoded admin Admin123! seeds in all envs (DataSeeder.cs:64-65)** |

### Doc-Tracker Conflict Table

| Topic | 00-master-overview | CLAUDE.md | 20-issue-registry | validation summary | Reality |
|---|---|---|---|---|---|
| Notifications | Complete | Partial (emails impl.) | — | Email FIXED | Backend-only, emails unwired |
| Admin panel | 100% CRUD | Complete | — | "No admin E2E tests" | UI stubs except moderation |
| GDPR endpoints | — | — | — | SEC-003 "missing", Open | Exist (backend), no FE |
| PaymentsController auth | — | — | API-002 CRITICAL Open | — | Fixed ([Authorize]) |
| E2E tooling | Playwright TS-ish | npx playwright (TS) | — | — | C# xUnit + Microsoft.Playwright |

### Mystery Dirs (checked)

- `.shared/` → contains only `ui-ux-pro-max` (skill assets for the ui-ux-pro-max plugin). Benign; consider gitignore review.
- `.config/` → `dotnet-tools.json` (standard dotnet local tool manifest). Benign.
- `.opencode/`, `.codex/`, `.claude/` → agent-tooling configs/memory; `.codex/PROJECT_MEMORY.md` is a tracked status doc (modified, uncommitted).
- `TestController.cs` → properly environment-gated: returns 404 outside Development/Testing (TestController.cs:34-46); requires `TestSettings:AdminSecret` outside Development. Not a prod hole as written, but depends on env config discipline.

### Unverifiable Claims (needs-confirmation)

- PROJECT_MEMORY.md:40-43 claims Core 199 / Application 162 / API 70 backend tests passed, Karma 970 passed, E2E 213/213 passed, Lighthouse mobile 0.97 / desktop 1.00. I verified builds only (running full suites/E2E was out of scope per review rules). CI on the eventual wishlist PR would confirm.
- Whether Railway deployments are currently live (railway.toml exists, no CD workflow) — if anything is already deployed, the seeded `admin@climasite.local`/`Admin123!` account escalates the seeding finding to an active P0.

### Open Questions for the Owner

1. Is the admin UI (products/orders/users) intentionally deferred, or was it believed complete? No plan task currently tracks building it.
2. Should Inventory/PriceHistory admin surfaces be descoped formally or scheduled?
3. Is anything deployed to Railway today?
4. Which tracker is authoritative going forward — Plan 18, or the issue registry? (Recommend archiving the registry.)

## Refuted during verification

None.
