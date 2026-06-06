# ClimaSite Gap Audit — 2026-04-08

**Auditor:** Claude (parallel explore agents)
**Branch:** main (pre-plan)
**Scope:** Whole repo — tests, i18n, theming, a11y, performance, security, partial features, production readiness, home page structure.
**Context:** Commissioned as the basis for `docs/plans/18-project-completion.md`. The home page is explicitly out of scope for "fix in place" work — it is being wiped and rewritten from scratch.

---

## 1. Summary Scorecard

| Dimension | Status | Severity | Biggest Offender |
|-----------|--------|----------|------------------|
| Backend unit tests | 69% covered | High | Admin (27 handlers, 1 test), Questions (0), Notifications (0), Addresses (0), Categories (0) |
| Integration tests | 18% of controllers (4 / 22) | **Critical** | 18 controllers with zero integration tests |
| Frontend unit tests | ~49% components, 41% services | High | 13 core services untested |
| E2E tests | Missing Notifications, Q&A, Inventory, Search, Brands/Categories browse | High | Notifications has no E2E at all |
| E2E mocking rule | Compliant | — | No `Mock`/`Moq`/`jest.fn` found in `tests/ClimaSite.E2E` |
| i18n key parity | Pass (1160 keys, all 3 langs) | — | — |
| i18n hardcoded strings | 25 template + 8 TS violations | High | about, admin, checkout, profile, auth.service |
| Theming hardcoded colors | 104 violations across 35+ files | **Critical** | `glass-card` (27), `home/*` (~30), account (`color: white` w/ no dark override) |
| Dark mode missing overrides | 15 components | High | account/orders, account/addresses, brands/*, about, not-found |
| Text-on-media without `text-shadow` | 4+ instances | Medium | scroll-video-section, hero-section, categories-section |
| Accessibility | Partial | Medium | 10+ clickable `<div>` without keyboard, low `role` coverage (50%) |
| Performance | Partial | Medium | 8 / 121 components use `OnPush`; 20+ inline templates > 200 lines; largest 1392 |
| Security | Partial | **High** | JWT secret in committed `appsettings.json`, no CSP/security headers middleware, Swagger exposed in prod, CORS `AllowAnyHeader`, rate-limit only on `/auth` |
| CI pipeline | Strong | — | `.github/workflows/test.yml` covers unit + int + FE + E2E with pg/redis |
| CD pipeline | Missing | High | No deploy workflow; Railway config exists but no auto-deploy |
| Docker hardening | Partial | High | Both Dockerfiles run as root; web container has no HEALTHCHECK |
| Observability | Partial | Medium | Serilog console only; no OpenTelemetry / metrics / tracing / log aggregation |
| Graceful shutdown | Missing | High | No shutdown hooks in `Program.cs` |
| Migrations on startup | Present but risky | Medium | `MigrateAsync()` runs inline with no rollback story |
| ADRs | Missing | Medium | No `docs/adr/` directory; CLAUDE.md mandates ADR-per-decision |
| Release process | Missing | Medium | No `CHANGELOG.md`, no semver tagging, no release notes template |

Legend — **Critical** = blocks production, **High** = ship-blocker for completion, **Medium** = polish.

---

## 2. Tests

### 2.1 Backend unit tests (target: 80%)

27 of 86 Application-layer handlers have **zero** corresponding unit tests (~31% gap).

Top gaps by feature area (handler count vs test files):

- `Features/Admin/` — 27 handlers, 1 partial test
- `Features/Questions/` — 8 handlers, 0 tests
- `Features/Notifications/` — 6 handlers, 0 tests
- `Features/Addresses/` — 6 handlers, 0 tests
- `Features/Categories/` — 5 handlers, 0 tests
- `Features/Inventory/` — 4 handlers, 0 tests
- `Features/Promotions/` — 3 handlers, 0 tests
- `Features/Gdpr/` — 3 handlers, 0 tests
- `Features/Brands/` — 3 handlers, 0 tests
- `Features/Installation/` — 2 handlers, 0 tests

Well-covered areas: `Orders` (9 files), `Auth` (6), `Products` (4), `Cart` (3), `Wishlist` (2), `Payments` (1).

### 2.2 Integration tests (target: every endpoint)

**Only 4 / 22 controllers (18%) have integration tests.** Missing:

Addresses, AdminCustomers, AdminDashboard, AdminOrders, Brands, Categories, Gdpr, Installation, Inventory, Notifications, Orders, Payments, PriceHistory, Promotions, Questions, Reviews, Webhooks, Wishlist.

### 2.3 Frontend unit tests (target: 70%)

- 60 `.spec.ts` files across 92 components (~49%) — below target.
- 9 services covered, 13 not covered: `address`, `animation`, `category`, `confetti`, `flying-cart`, `moderation`, `payment`, `promotion`, `review`, `scroll-trigger`, `scroll-video`, `smooth-scroll`, `wishlist`.

### 2.4 E2E (Playwright)

Covered: Accessibility, Account, Admin, Auth, Cart, Checkout, i18n, Journeys, Navigation, Orders, Pages, Products, Reviews, Settings, Wishlist (15 folders, 22 classes).

Missing: **Notifications**, Q&A, Inventory management, Search/filtering flows, Brands / Categories browsing.

### 2.5 Mocking rule

No violations. `TestDataFactory` is used throughout `tests/ClimaSite.E2E/`. No `Mock`/`Moq`/`jest.fn`/`sinon` references found.

---

## 3. i18n

### 3.1 Key parity — PASS

`en.json`, `bg.json`, `de.json` all 1160 lines, identical key tree.

### 3.2 Hardcoded template strings — 25 violations

Worst offenders:

- `features/home/home.component.ts` — "CDL", "Hot" (will be deleted in home wipe)
- `features/about/about.component.ts` — "Ivan Petrov", "Head of Sales", 4 more team-member strings
- `features/checkout/checkout.component.ts` — "Germany", "Austria", "Romania", "Greece" (country list)
- `features/admin/products/admin-products.component.ts` — "Product Management"
- `features/admin/users/admin-users.component.ts` — "User Management"
- `features/admin/orders/admin-orders.component.ts` — "Order Management"
- `features/admin/moderation/admin-moderation.component.ts` — "By:", "Verified Purchase"
- `features/account/profile/profile.component.ts` — "English", "Deutsch"
- `shared/components/financing-calculator/financing-calculator.component.ts` — "Interest:"
- `shared/components/payment-icons/payment-icon.component.ts` — "AMEX", "Pay", "Pal"

### 3.3 Hardcoded TS messages — 8 violations

- `core/services/checkout.service.ts:96` — `'Shipping address is required'`
- `auth/services/auth.service.ts:174` — `'Logout already in progress'`
- `auth/services/auth.service.ts:192` — `'Logout failed'`
- `core/services/scroll-video.service.ts:104` — `'ScrollVideoService: cannot init after destroy'` *(will be deleted with home)*
- plus 4 more in services

---

## 4. Theming

### 4.1 Hardcoded colors — 104 violations across 35+ files

Top offenders (excluding home, which is being wiped):

- `shared/components/glass-card/glass-card.component.ts` — **27** hardcoded `rgba()` values (lines 53–208)
- `shared/components/product-reviews/product-reviews.component.ts` — 5
- `features/products/product-card/product-card.component.ts` — 5
- `shared/components/product-gallery/product-gallery.component.ts` — 4
- Account area (`orders`, `addresses`, `order-details`) — multiple `color: white` + `border-top-color: white`
- Brands (`brand-detail`, `brands-list`), About, Not-Found — `color: white` with no dark override
- `core/services/flying-cart.service.ts:117` — `var(--color-bg-card, #fff)` — fallback violates policy

### 4.2 Missing dark mode overrides — 15 components

Components with hardcoded light-mode colors and zero `[data-theme="dark"]` overrides:

account/orders, account/addresses, account/order-details, brands/brand-detail, brands/brands-list, about, not-found, plus 8 more admin/feature components.

### 4.3 Text-on-media without `text-shadow` — 4+

All in `features/home/` — resolved by the home wipe.

---

## 5. Accessibility

**Strengths:** 628 `data-testid` attributes, 61 role attributes, proper ARIA on mini-cart drawer (`role="dialog"`, `aria-modal="true"`), semantic landmarks.

**Gaps:**

- 10+ `<div (click)>` without `(keydown.enter)`/`(keydown.space)` or `role="button"`: `header`, admin related-products search results, promotion-detail code span, account addresses modal overlay, +6 more.
- Role coverage only ~50% (61 roles across 121 components).
- Monster inline templates inhibit audit: `header` 1392 lines, `home` 1151, `product-list` 1075, `product-detail` 794.

---

## 6. Performance

**Already done** (see `docs/performance/performance-audit.md`): route-level lazy loading, 25 lazy images, `fetchpriority="high"` on LCP, preconnect hints, bundle budgets, skeleton screens, RAF throttled scroll handlers.

**Gaps:**

- Only 8 / 121 components use `OnPush` (~7%).
- 20+ components with inline templates > 200 lines; largest 1392.
- Missing explicit `width`/`height` on many `<img>` → CLS risk.
- No production CWV monitoring.

---

## 7. Security

**Strengths:** `[Authorize(Roles="Admin")]` on admin endpoints, JWT env-var primary, global rate limit 100/min, auth rate limit 10/min, sensitive rate limit 5/min, HSTS in non-dev, EF Core only (no raw SQL), `ClockSkew = TimeSpan.Zero`, JWT issuer/audience/lifetime all validated.

**Gaps — in priority order:**

1. **JWT secret committed** — `src/ClimaSite.Api/appsettings.json:22` = `"YourSuperSecretKeyThatIsAtLeast32CharactersLong!"`. Must remove from committed file, require env var.
2. **Stripe test keys committed** — `appsettings.json:16-19`. Same fix.
3. **No custom security-headers middleware** — missing CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy. Only HSTS present.
4. **Swagger exposed in all environments** — `Program.cs:273-278`. Gate behind `IsDevelopment()`.
5. **CORS `.AllowAnyHeader()`** — line 103. Whitelist `Content-Type, Authorization, Accept`.
6. **Rate limiting gaps** — only `AuthController` uses `[EnableRateLimiting("auth")]`. Need policies on Payments, password reset, admin write ops (currently only global 100/min).

---

## 8. Partial features

### 8.1 Notifications (plan 12) — ~15% overall

Backend ~20%: `NotificationsController` + 4 commands + 2 queries + DTOs + entity. Missing: email infrastructure (NOT-002), template engine (NOT-003), event handlers (NOT-006/007/008), preferences API, background processing.
Frontend: 0%. No service, no bell, no dropdown, no center page, no preferences UI.

### 8.2 Wishlist (plan 13) — ~25% overall

Backend ~40%: entity + migration + repo + 2 commands + 1 query + controller. Missing: service layer, comprehensive validators, unit/integration tests.
Frontend ~5%: only `wishlist.component.ts`. Missing service, button, page, share, route, header integration.

### 8.3 Animation Audit 21F — Phase 1-2 done, Phase 3-6 remain

Done (8/27): Removed `FloatingDirective`, `TiltEffectDirective`, `ParallaxDirective`; simplified `RevealDirective` to fade/fade-up/fade-down; static backgrounds ("Nordic Tech" philosophy).
Pending: flying-cart/confetti/button/card/modal refinement, reduced-motion audit, Lighthouse verification, style guide.

---

## 9. Production readiness

**CI:** Strong. `.github/workflows/test.yml` runs unit + int + FE + E2E with pg/redis/codecov.
**CD:** Missing. No deploy workflow; Railway config exists (`railway.toml`, `railway.api.toml` → `dockerfile` builder + `/health` 300s timeout + `on_failure` restart) but nothing triggers deploys.

**Docker:**
- `Dockerfile.api`: multi-stage (sdk→build→publish→aspnet:10.0), HEALTHCHECK at `/health`, curl installed, port 8080. **Missing: non-root user**.
- `Dockerfile.web`: multi-stage (node:22-alpine→nginx:alpine) + entrypoint script. **Missing: HEALTHCHECK + non-root user**.
- `docker-compose.yml`: dev only. Plaintext creds, pgAdmin with dev password.

**Health checks:** `/health`, `/health/ready`, `/health/live`. PG + Redis checks. Missing: external dependency checks (Stripe, SMTP).

**Observability:** Serilog console + request logging only. **No OpenTelemetry, tracing, metrics, or log aggregation.**

**Migrations:** `MigrateAsync()` runs at startup in `DataSeeder.SeedAsync()`. Blocks startup on failure; no rollback story documented.

**Graceful shutdown:** Not implemented. No `StopAsync`, no in-flight-request drain.

**ADRs:** `docs/adr/` does not exist. CLAUDE.md rule #7 violated.

**Release:** no `CHANGELOG.md`, no semver tags, no release-notes template, no `/release` skill invocation history.

---

## 10. Home page (context for wipe)

Total: 3,682 lines across 6 component files in `features/home/`. Sections in order: dark hero (100vh) → lifestyle reveal (100vh) → scroll video canvas (6 panels) → category explorer → why choose us → trust bar marquee. Stub directories exist for featured-products, testimonials, newsletter-cta.

**Home-exclusive (safe to delete):**
- `features/home/**`
- `core/services/scroll-video.service.ts`
- `core/services/scroll-trigger.service.ts`
- `shared/directives/scroll-video.directive.ts`
- All `home.*` keys in `assets/i18n/{en,bg,de}.json`
- Home route `''` in `app.routes.ts:6-9`
- `tests/ClimaSite.E2E/PageObjects/HomePage.cs` (or repurpose for v3)

**Must preserve (used by other features):**
- `shared/directives/reveal.directive.ts` (used by cart, product-detail, product-list, why-choose-us, trust-bar)
- `core/services/product.service.ts`
- All standard Angular modules

---

## 11. Recommendation

Wipe the home page and close the gaps in this order: **cleanup & foundation → home v3 rewrite → partial feature completion → compliance hardening (i18n + theme + dark) → test coverage gap closure → security hardening → performance & a11y → production readiness → documentation & ADRs**. Details in `docs/plans/18-project-completion.md`.
