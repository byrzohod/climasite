# ClimaSite — UI/UX Review

**Date:** 2026-06-11
**Sources:** `docs/project-plan/_review/uiux.md` (primary, verified findings), `docs/project-plan/_review/product.md` (flow dead-ends that manifest in the UI), `docs/project-plan/_review/bugs.md` (UI-visible data-correctness bugs, cross-referenced). Branch under review: `feature/plan18-wishlist-completion`.

**How to use this document:** This is the canonical UI/UX state of the ClimaSite frontend (`src/ClimaSite.Web/`) as of 2026-06-11, intended for a developer or AI assistant continuing from documentation alone. Every finding names exact files/lines and carries a priority (P0 critical blocker, P1 important gap, P2 useful improvement, P3 polish). Start with the "Prioritized UI work list" at the bottom for sequencing; use the Findings sections for evidence, recommended fixes, and acceptance criteria. Items the review could verify only by code reading (no browser run) are tagged **Needs visual/browser confirmation**. Where this document contradicts `CLAUDE.md`'s status table ("Admin Panel | Complete", "Shared Components Complete", guest checkout business rule), this document is correct and CLAUDE.md is stale — see Finding F3 and the docs review (`docs/project-plan/_review/docs.md`).

---

## Overview

The frontend is strong where most projects are weak and weak where it hurts most:

**Strengths (verified):**
- **i18n is in perfect parity** — 1006 identical keys across `en.json`/`bg.json`/`de.json`, essentially zero hardcoded user-facing strings.
- **Accessibility plumbing is genuinely good** — full focus traps with focus-restore in `ModalComponent` and the mini-cart drawer, `role=alert` toasts, `prefers-reduced-motion` handled in 34 files, 44px touch targets in bottom-nav, auth guards that wait for hydration and preserve `returnUrl`.
- **`data-testid` coverage is high** (~469 occurrences) and the new wishlist/home-v3 code follows conventions (CSS vars, shared empty/loading states, testids).
- Product list, cart, account orders/profile, contact, order confirmation, and home-v3 recommendations all implement loading/empty/error states — `product-list.component.ts` is the best-in-repo pattern (skeletons + error + retry).

**Weaknesses (verified):**
- **The money path has the worst UX in the app**: the checkout shipping form shows zero field-level validation feedback (F2), prices switch from EUR on product pages to USD in cart/checkout (F1), and shipping labels are hardcoded `$9.99`/`$19.99` strings that don't even match what the backend bills (see product review P0 #3).
- **Three of four admin pages are 12-line "Coming Soon" stubs** (products/orders/users) while CLAUDE.md claims the Admin Panel complete (F3).
- **A whole tier of UI flows are silent dead-ends**: the contact form fakes success with `setTimeout`, all six footer legal links 404, PayPal/bank payment options create unpayable orders, the PDP "In Stock" badge is hardcoded `true` (F15–F18, F20, from product.md).
- **UI-visible data-correctness bugs rooted in the backend contract** (F21–F24, from `_review/bugs.md`): sale-price display is inverted sitewide (the HIGHER original price renders as the deal price), star ratings are always empty (aggregates hardcoded to 0), guest cart items silently vanish on login (the merge call always 400s), and cart/wishlist product names ignore BG/DE translations.
- Cross-cutting: no router scroll restoration (F5), header/footer deferred behind a fixed 3.2s timer (F6), light-theme error color fails WCAG AA contrast (A1), fragmented breakpoints with a concrete 767/768px clash (R1).

---

## Findings

Standard format per finding: what/where → why it matters → fix → acceptance. Line numbers reference the working tree on 2026-06-11.

### Missing loading / empty / error states

#### F7 — Account orders API failure masquerades as "no orders" empty state (P2)
- **Where:** `src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts:835-838` — on error, `loadOrders()` sets an empty result and the template falls into the `orders-empty` state (`:161-167`, "you have no orders yet" + "shop now" CTA). No error signal, no retry.
- **Why:** A customer with existing orders hitting a transient 500 is told they have no orders — factually wrong and alarming.
- **Fix:** Add an `error` signal + error block with retry button, mirroring `product-list.component.ts:252-258`. (Small)
- **Acceptance:** API stubbed to 500 → `/account/orders` shows a translated error with working Retry, not the empty state.

#### F8 — Product detail loading/error states are bare text; no skeleton, no retry, no 404 (P2)
- **Where:** `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts:29-36` (single-line loading/error text); `:897` sets `products.details.notFound` as an in-page string instead of routing to `NotFoundComponent`.
- **Why:** Highest-traffic, highest-intent page; markedly below the bar set by the adjacent list page. Stale SEO links to bad slugs get a generic message, not a 404.
- **Fix:** Detail-page skeleton (reuse `shared/skeleton` components), error state with retry + "back to products", route genuine 404s to the not-found page. (Medium)
- **Acceptance:** Skeleton matches final layout in both themes (**needs visual confirmation**); API 500 shows retry; unknown slug shows 404 UX.

#### F4 — Wishlist destructive/sharing actions fail silently; Clear All has no confirmation or undo (P2)
- **Where:** `src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts:402-405` (`clearWishlist()` wipes UI state with no confirm/undo), `:409-412` / `:417-420` (share toggle / regenerate only reset `shareLoading` on error); `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts:161,267,286,296` — every API failure swallowed via `catchError(() => of(null))`.
- **Why:** A failed DELETE shows an empty wishlist that silently resurrects on next refresh; a failed share-enable leaves the user thinking a link was created. This is in the very feature branch under review and contradicts the Wishlist "Complete" status in CLAUDE.md / `docs/validation/areas/07-wishlist.md`.
- **Fix:** Confirm step (shared `ModalComponent`) or undo toast for Clear All (cart already has the undo-toast gold standard, `cart.component.ts:762`); surface failures via `ToastService` with translated messages; snapshot-and-restore items if the clear call fails. (Small)
- **Acceptance:** Simulated 500 on `PUT /api/wishlist/share` and `DELETE /api/wishlist` shows a translated error toast and leaves UI consistent with server; covered by component spec.

#### F19 — No global offline handling (P3)
- **Where:** `empty-state` component has an unused `offline` variant; no global offline/retry interceptor (`navigator.onLine` unused anywhere).
- **Fix:** Low priority; wire the existing variant into a global error path if/when desired.

State coverage matrix for all main pages (loading/empty/error/retry per component, with file:line evidence) is in `docs/project-plan/_review/uiux.md` § "Missing-states matrix" — keep that as the reference table rather than duplicating it here.

### Inconsistent patterns

#### F1 — Currency inconsistency: product pages show EUR, cart/checkout/mini-cart show USD (P1; feeds the P0 pricing cluster)
- **Where:** Product surfaces explicitly format EUR (`product-card.component.ts`, `product-detail.component.ts`, `recommendations.component.html` — 13 files). Cart/checkout use the bare `| currency` pipe (USD default): `features/cart/cart.component.ts:73-157`, `features/checkout/checkout.component.ts:294,326,334,342,348,352`, plus `mini-cart-item.component.ts:71-74` and `mini-cart-drawer.component.html:91`. No `DEFAULT_CURRENCY_CODE` provider exists (`app.config.ts` checked). Checkout hardcodes `$9.99` (`:186`) and `$19.99` (`:192`) shipping labels.
- **Why:** A €599.99 product becomes $599.99 the moment it enters the cart — misleading pricing through the entire purchase flow. **This is the UI face of the product review's P0 #3** (charged amount in BGN, recorded total in EUR, displayed in USD, "free" shipping billed 5.99 — see `_review/product.md` Finding 3); fix the display layer as part of that cluster, not in isolation.
- **Fix:** `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' }` in `app.config.ts`; remove or standardize explicit `:'EUR'` args; shipping labels formatted via the currency pipe from the shipping-method model. (Small for display; the authoritative-amount fix is backend.)
- **Acceptance:** All price displays render one currency; `grep '\$9.99\|\$19.99'` and bare `| currency` return no inconsistent usages.
- **Open question (owner):** intended display currency EUR vs BGN — site branding is Bulgarian (cdl.bg).

#### F10 — Shared BreadcrumbComponent has zero usages; six pages hand-roll divergent breadcrumbs (P2)
- **Where:** `shared/components/breadcrumb` is never imported. Ad-hoc `class="breadcrumb"` markup in `product-detail.component.ts:40` (proper `<nav aria-label>`), `product-list.component.ts`, `brand-detail.component.ts`, `promotion-detail.component.ts`, `category-header.component.ts`, and `wishlist.component.ts:30-36` (plain `<div>`, no landmark).
- **Why:** Divergent semantics (some accessible landmarks, some not) and styling; CLAUDE.md lists Breadcrumb among "Shared Components Complete" — a claims mismatch.
- **Fix:** Migrate all six to the shared component, or delete it and standardize the inline `<nav aria-label>` pattern. (Medium)

#### F14 — Energy-rating colors: two different hardcoded palettes for the same concept (P3)
- **Where:** `shared/components/energy-rating/energy-rating.component.ts:131-140` (official EU hex palette) vs `features/products/product-card/product-card.component.ts:338-358` (a second, different gradient palette). The card badge and detail-page bar can disagree on the color for class "A".
- **Fix:** One exported constant (or a documented "fixed, non-themed" section in `_colors.scss`) consumed by both. (Small)

### Form UX

#### F2 — Checkout shipping form gives ZERO field-level validation feedback (P1)
- **Where:** `features/checkout/checkout.component.ts` — validators declared at `:1051-1060` (8 required fields + email), but the template (`:104-167`) renders no error elements and no `[class.invalid]` bindings; the `.field-error` CSS at `:594`, `:990`, `:1002` is dead code; submit is `[disabled]="shippingForm.invalid"` (`:164`); `submitShipping()` (`:1104-1105`) returns early without `markAllAsTouched`.
- **Why:** A user who mistypes any of 8 required fields sees only a permanently disabled "Next: Payment" button with no indication of what's wrong. Highest-value form in the app; weakest form in the codebase; direct abandonment risk. Contrast: login (`login.component.ts:267-297`), register (`:90`), profile (`:41-52,:181-194`), contact (`contact.component.ts:38-55`) all implement touched-based field errors.
- **Fix:** Per-field error messages + `[class.invalid]` following the `profile.component.ts` pattern; reuse the already-styled `.field-error` class and existing `errors.required` translation keys; either enable submit and `markAllAsTouched` on click, or keep disabled but show errors on blur. (Small)
- **Acceptance:** Blurring/submitting empty required fields shows a translated error under each invalid field in all 3 languages; E2E covers attempting to proceed with an incomplete shipping form.

#### F15 — Contact form fakes success; submissions go nowhere (P1, from product.md Finding 7)
- **Where:** `features/contact/contact.component.ts:384-399` — `onSubmit()` contains `// Simulate API call` with `setTimeout(...)` setting `submitSuccess(true)`. No HTTP call; no backend endpoint exists.
- **Why:** The form's UX states are well-built (field errors, success/error banners) but the success is a lie — leads and complaints silently discarded. Reachable from header nav, footer, and resources CTA.
- **Fix:** Backend `POST /api/contact` (email via `IEmailService` and/or persist), wire the form, keep the existing states real. (Small frontend; backend endpoint needed.)

### Navigation issues

#### F5 — No router scroll restoration (P2)
- **Where:** `app.config.ts` provides `provideRouter(routes)` with no `withInMemoryScrolling`. No `NavigationEnd` scroll handler in `app.component.ts` or `main-layout.component.ts`. Only page-local resets exist (`product-list.component.ts:1364`).
- **Why:** Scrolling down the product list and opening a product lands on the detail page at the same scroll offset; back-navigation position is also not restored. Affects nearly every navigation.
- **Fix:** `withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' })`. (Small) **Needs browser confirmation** of interaction with route fade/slide transitions (`data.animation`) after enabling.

#### F6 — Header/footer deferred behind a fixed 3.2s timer; placeholder lacks core functionality (P2)
- **Where:** `core/layout/main-layout/main-layout.component.ts` — `@defer (on timer(3200ms))` for both `<app-header />` and `<app-footer />` (footer placeholder is a 1px div). The placeholder shell has no search input (its "search" links to `/products`), no language selector, no theme toggle, no cart/wishlist badges, no user menu; on mobile (`@media (max-width: 767px)`) it hides top bar, nav, and action icons entirely. `home-v3.component.html` similarly defers below-fold content behind `timer(2200ms)`.
- **Why:** For the first 3.2s of every cold load (not just home), users can't search, see cart count, or switch language/theme. The fixed wall-clock timer penalizes fast devices and looks tuned for the Lighthouse score in CLAUDE.md (mobile 0.97/desktop 1.00) rather than real UX.
- **Fix:** Replace `on timer(3200ms)` with `on idle` (or `on idle; on interaction`); make the placeholder search focus/open the real search once loaded. (Small) **Needs browser confirmation:** re-run Lighthouse after the change to keep the documented budget.

#### F13 — `/categories` is a routed "Coming Soon" stub (P3 as nav issue; product review rates the stub P2)
- **Where:** `features/categories/category-list/category-list.component.ts` (29 lines, renders `categories.comingSoon`), routed at `app.routes.ts:59-62`. Nothing links to it internally; reachable by direct URL/crawlers. Category browsing actually works via `/products/category/:slug` and the mega-menu.
- **Fix:** Redirect to `/products`, or build a real category grid (backend `CategoriesController` + `category.service` already exist). (Small)

#### F16 — All six footer legal/support links 404; no cookie consent (P1, from product.md Finding 6)
- **Where:** `core/layout/footer/footer.component.ts:43-56` links `/faq`, `/shipping`, `/returns`, `/terms`, `/privacy`, `/cookies`; none exist in `app.routes.ts` — all fall through to the `**` `NotFoundComponent` (`app.routes.ts:135-138`). Social links are `href="#"` placeholders (`:93-103`). No cookie-consent component exists repo-wide; a "GDPR Compliant" footer badge displays while the privacy policy 404s.
- **Why:** EU legal blocker (terms, privacy, withdrawal/returns, German Impressum) and every trust-critical link a customer clicks is a 404.
- **Fix:** Lazy `legal` feature with translated static pages + cookie-consent banner; remove or fill the social placeholders. (Medium; needs real legal copy — placeholder acceptable for staging.)

#### F3 — Admin products/orders/users pages are "Coming Soon" stubs (P1 in the UI review; **escalated to P0 in product.md** because the shop cannot be operated)
- **Where:** `features/admin/products/admin-products.component.ts`, `admin/orders/admin-orders.component.ts`, `admin/users/admin-users.component.ts` — each a 12-line stub rendering `common.comingSoon`; routed at `admin.routes.ts:10-20`; the dashboard (`admin-dashboard.component.ts:15-26`) links to all three as primary tiles and renders no KPIs. Full backend APIs exist unused (`Controllers/Admin/AdminProductsController.cs`, `Controllers/AdminOrdersController.cs`, `Controllers/AdminCustomersController.cs`, `AdminDashboardController.cs`). Orphaned sub-components `related-products-manager` and `product-translation-editor` exist under `features/admin/products/components/` but nothing routes to them.
- **Why:** No UI to fulfill orders, set tracking, or edit products; CLAUDE.md claims "Admin Panel | Complete | CRUD, dashboard". `docs/validation/areas/08-admin-panel.md:364-393` already flags this as Critical — still true.
- **Fix:** Build admin orders (status + tracking) first — fulfillment-critical — then products CRUD wiring the orphaned editors, then customers; surface dashboard KPIs. Short-term: correct CLAUDE.md and hide dead tiles. (Large)

#### F17 — Guest checkout blocked in UI despite documented behavior and backend support (P1, from product.md Finding 9)
- **Where:** `app.routes.ts:82-89` puts `authGuard` on `/checkout`; no guest email capture exists in `checkout.component.ts`; guests redirect to `/login?returnUrl=/checkout`. Backend `OrdersController.cs:23-32` is `[AllowAnonymous]` with `GuestSessionId`, but `PaymentsController.cs:10` is `[Authorize]`, so the half-built path is unreachable either way. CLAUDE.md business rules state "guest checkout stores email".
- **Fix:** Owner decision required — implement end-to-end or update docs to "authenticated checkout only". (Large if implemented.)

#### F18 — PDP trust badge hardcodes "In Stock" for every product (P2, from product.md Finding 16)
- **Where:** `product-detail.component.ts:119` passes `[inStock]="true"` unconditionally to `app-warranty-badge`; wishlist mapping hardcodes `inStock: true` (`:1020`), despite `product.model.ts:41,70` exposing real `stockQuantity`/`inStock`. Stock is only enforced server-side at add-to-cart, surfacing as an error toast after the page promised availability.
- **Fix:** Bind to the selected variant's real stock; disable add-to-cart with an out-of-stock state; show low-stock hints. (Small)

#### F20 — Fake payment method options in checkout (P1, from product.md Finding 8)
- **Where:** `checkout.component.ts:199-214` offers card/paypal/bank radios; for non-card, `placeOrder()` (`:1210-1232`) creates the order with no payment step, no PayPal integration, no bank-transfer instructions; the method isn't even persisted.
- **Why (UI angle):** Customers selecting PayPal believe they paid. The UI must not offer methods that don't exist.
- **Fix:** Remove PayPal until integrated; for bank transfer, show IBAN/reference instructions on confirmation (requires backend persisting the method). (Small UI change; coordinate with the payments P0 cluster in `_review/product.md`.)

### UI-visible data-correctness issues (root cause backend/contract — authoritative entries in `_review/bugs.md`)

These render wrong in the UI but are fixed mostly outside component templates. They are listed here because a frontend developer will otherwise rediscover them as "display bugs"; full evidence and verifier notes are in `docs/project-plan/_review/bugs.md` (and the consolidated `docs/project-plan/BUGS_AND_TECH_DEBT.md`).

#### F21 — Sale-price display inverted sitewide: the HIGHER original price renders as the deal price (P1, bugs.md #6)
- **Where:** Nearly every backend DTO maps `SalePrice = CompareAtPrice` (the higher original price) while the templates render `salePrice` as the prominent deal price and `basePrice` struck through: `product-card.component.ts:154-158`, `product-detail.component.ts:87-91`, `brand-detail.component.ts:97-100`. Seeded example: DualZone Pro displays "€1099.99" big with "€899.99" struck out while checkout charges 899.99. The cart accidentally suppresses it via a `salePrice < unitPrice` guard (`cart.component.ts:72`).
- **Fix:** One shared mapping contract backend-side (the FE templates' semantics — `salePrice` = current, `basePrice` = original — are the sensible target); afterwards visually re-verify card/detail/cart/wishlist/brand. UI effort is verification only.

#### F22 — Guest cart silently vanishes on login: merge call always 400s (P1, bugs.md #3)
- **Where:** `core/services/cart.service.ts:190-205` posts `guestSessionId` only in the `X-Session-Id` header while `CartController.cs:110-116` requires it as `[FromQuery]` — every merge request 400s before the handler runs, and `auth.service.ts:133-142` swallows the failure with `console.warn`. The UI then switches to the empty user cart.
- **Fix:** One-line frontend change: `POST /api/cart/merge?guestSessionId=${this.getSessionId()}`. Add the missing E2E (guest adds 2 items → logs in → both items still visible). CLAUDE.md's "Cart merges when guest user logs in" business rule is currently false in practice.

#### F23 — Star ratings always empty sitewide (P2, bugs.md #12)
- **Where:** Product list/search/featured/wishlist projections hardcode `AverageRating = 0` / `ReviewCount = 0`, so `product-card.component.ts:140-141` renders zero stars on every card regardless of real reviews. Backend projection fix; no FE change needed.

#### F24 — Cart/wishlist product names ignore BG/DE translations (P2, bugs.md #10)
- **Where:** Cart and wishlist mappers use raw `product.Name` and accept no language code, and the FE cart/wishlist services send no `lang` param (the catalog does: `product.service.ts:40-42`). A BG user sees translated names in the catalog and English names in cart/wishlist for the same product. FE share of the fix: send the language param once the backend accepts it.

Also UX-relevant from the bugs review: concurrent 401s at token expiry can log users out instead of queuing on the in-flight refresh (`auth/services/auth.service.ts:202-208` + `auth/interceptors/auth.interceptor.ts:49-55`; bugs.md #8, P1) — surfaces as unexplained "session expired" failures, including mid-checkout.

---

## Accessibility concerns

**Verified strengths** (keep these patterns; they are the house style):
- `ModalComponent`: `role=dialog`, `aria-modal`, `labelledby`, Tab trap, Escape, focus restore (`modal.component.ts:28-30,281-370`).
- Mini-cart drawer: full focus trap + Escape + restore (`mini-cart-drawer.component.ts:217-310`).
- Toasts: `role=alert` + polite live container (`toast.component.ts:12,440-442`).
- `prefers-reduced-motion` in 34 files (animation/confetti/flying-cart services, reveal/count-up/split-text directives, `_animations.scss`).
- Home-v3 wizard: labeled slider with `aria-valuenow/min/max`, radiogroup semantics, aria-live estimate card (`wizard.component.html:7-68`).
- Bottom nav: 44px touch targets + safe-area insets (`bottom-nav.component.ts:152-153,125`).

**Concerns:**

| ID | Issue | Where | Priority |
|---|---|---|---|
| A1 | Light-theme `--color-error` = `#ef4444` (~3.76:1 on white) fails WCAG AA 4.5:1 for the small error text it styles (checkout `.field-error` 0.875rem, cart `.item-error`, stripe-error, wishlist hovers). It is the lone semantic token using the 500 shade — success/warning/info all use 700 (`_colors.scss:287-300`). Fix: one-line change to `$color-error-700` (`#b91c1c`, ~6.9:1) at `_colors.scss:295`; keep error-500 for borders/icons. **Needs visual confirmation** that buttons/badges using `--color-error` as background still look right. | `src/ClimaSite.Web/src/styles/_colors.scss:295,137` | P2 |
| A2 | Checkout order-failure banner and stripe-error have no `role="alert"`/aria-live — screen readers get no announcement when placing an order fails (the most critical error in the app). Cart's `item-error` same. Correct pattern already exists at `recommendations.component.html:22` and `toast.component.ts:12`. Consider moving focus to the banner on failure. **Needs SR confirmation** (VoiceOver/NVDA). | `checkout.component.ts:252-256,228-230`; `cart.component.ts:109-111` | P3 |
| A3 | Header search inputs (desktop `:73-80`, mobile `:222-231`) are placeholder-only labeled — no `aria-label`, no `<label>`, no `role="search"` on the form, decorative SVG lacks `aria-hidden`. On every page. Key `common.aria.search` already exists (used on the buttons). | `core/layout/header/header.component.ts` | P3 |
| A4 | Breadcrumb semantics divergent: wishlist uses a plain `<div>` with no nav landmark while product-detail uses proper `<nav aria-label>` (see F10). | `wishlist.component.ts:30-36` | P2 (part of F10) |

---

## Responsive design concerns

| ID | Issue | Where | Priority |
|---|---|---|---|
| R1 | **Breakpoint fragmentation** — ten ad-hoc max-width values (27× 768px, 13× 1024px, 8× 640px, 4× 767px, 4× 480px, 3× 980px, 3× 600px, 2× 720px, 1× 540px, 1× 374px); no breakpoint variables/mixins anywhere in `src/styles/` (`_tokens.scss`, `_spacing.scss` checked). | repo-wide component styles | P2 |
| R2 | **Concrete 767/768 clash:** bottom-nav shows at ≤767px (`bottom-nav.component.ts:105`) and main-layout pads at ≤767px, but `wishlist.component.ts:307-309` reserves bottom-nav space at ≤768px — at exactly 768px CSS width (iPad Mini portrait) the wishlist reserves 5rem for a nav that isn't rendered. **Needs visual confirmation** at 768px viewport. | `wishlist.component.ts:307-309`, `bottom-nav.component.ts:105` | P2 (fix first, as part of R1) |
| R3 | Mobile placeholder header (first 3.2s) hides top bar, nav, and action icons entirely (`@media (max-width: 767px)` in main-layout styles) — mobile users get even less than desktop during the defer window. See F6. | `main-layout.component.ts` | P2 (part of F6) |

**Fix for R1/R2:** define `$bp-mobile: 767px`, `$bp-tablet: 1024px` (etc.) as SCSS variables/mixins in `styles/_tokens.scss`, migrate incrementally starting with the 767/768 conflict, and reference the tokens in review guidelines.

---

## i18n status

Key-diff results (read-only node script over `src/ClimaSite.Web/src/assets/i18n/`):

```
en: 1006 keys
bg: 1006 keys
de: 1006 keys
Missing/extra keys per language pair: NONE (all 6 pairwise diffs empty)
```

**Perfect parity.** Template scan found no hardcoded user-facing strings in `.html` templates and only doc-comments in `.ts` inline templates. The only literal user-visible strings:

- `$9.99` / `$19.99` shipping prices — `checkout.component.ts:186,192` (covered by F1; these also bypass locale formatting).
- Placeholder-header phone `0898 567 504` / brand `CDL` — `main-layout.component.ts` (arguably brand constants; acceptable).

**Caveat — key parity is not full i18n:** cart and wishlist API responses return untranslated (English) product names in BG/DE because the backend mappers ignore the language and the FE cart/wishlist services send no `lang` param (F24, `_review/bugs.md` #10). This violates the "works in all languages" Definition of Done even though the translation files themselves are complete. Beyond F1 (hardcoded `$` shipping labels) and F24, no i18n remediation is required. Per-language rendering in BG/DE **needs visual confirmation** for layout overflow (German strings are typically longest), but no key gaps exist.

---

## Theming compliance

97 raw hex hits in `app/` triaged down to **one substantive violation**:

- **Violation:** dual hardcoded energy-label palettes (F14) — `energy-rating.component.ts:131-140` vs `product-card.component.ts:338-358`.
- **Benign (no action):** ~40 HTML entities (false positives), `var(..., #fff)` CSS fallbacks (`button.component.ts:213`), the `#fff` mask trick (`glass-card.component.ts:148-152`), social brand colors (`share-product.component.ts:203-221`), the confetti palette (decorative, reduced-motion guarded), test specs.

`_colors.scss` single-source-of-truth discipline is otherwise excellent; the new wishlist sharing UI and home-v3 use CSS vars throughout (room-preview takes a `[theme]` input). Dark theme tokens are fully mirrored (`_colors.scss:429-434` etc.) with no component-level gaps spotted in code — **dark-theme rendering of new features needs visual confirmation** (the review could not run a browser).

One token-level issue: the light-theme error color contrast (A1) — a theming fix as much as an accessibility one.

---

## Suggested improvements

Beyond the defect list, highest-leverage UX upgrades (cross-referenced with `_review/product.md` § D "Highest-leverage path"):

1. **Make checkout the best form in the app, not the worst** — F2 (field errors) + F1 (currency) + F20 (remove fake payment methods) + A2 (announce failures). All small individually; together they transform the money path.
2. **Surface the implemented-but-unreachable conversion features** (product.md Finding 14): wire `CompareButtonComponent` into product cards + a `/compare` page (HVAC spec comparison is high-value for this vertical); embed `RecentlyViewedComponent` on PDP/home; add a price-history sparkline to the PDP (`PriceHistoryController` already exists); add GDPR export/delete UI to `/account/profile` (`GdprController` exists).
3. **Replace defer timers with `on idle`** (F6) — likely the single biggest perceived-performance win for real users, at near-zero cost.
4. **Adopt the repo's own best patterns everywhere**: `product-list`'s skeleton+error+retry for product-detail (F8) and account orders (F7); cart's undo-toast for wishlist Clear All (F4).
5. **Standardize primitives**: breadcrumbs (F10), breakpoints (R1), energy palette (F14) — cheap consistency debt to clear before more features pile on.
6. **Search autocomplete/suggestions** — header search currently submits only (product.md flow inventory notes no suggestions). P3, but a common e-commerce expectation.

Open questions for the owner (blocking some fixes): intended currency (EUR vs BGN); guest checkout in/out of scope for v1; were the 3.2s/2.2s defer timers a measured Lighthouse trade-off; keep-or-delete for the shared BreadcrumbComponent and the orphaned admin product editors.

---

## Prioritized UI work list

UI/frontend-scoped items only; backend-coupled items note their dependency. "Visual?" = needs browser/visual/screen-reader confirmation after (or before) the fix.

### P0 — blockers (UI share of launch-blocking clusters)
| # | Item | Files | Effort | Visual? |
|---|---|---|---|---|
| 1 | Checkout pricing display alignment — part of the payments/pricing P0 cluster (`_review/product.md` Findings 1, 3, 11): one currency end-to-end (`DEFAULT_CURRENCY_CODE`), shipping labels from the model via currency pipe, shipping tier names matching backend cases | `app.config.ts`, `checkout.component.ts:180-192,294-352`, `cart.component.ts:73-157`, mini-cart files | S (UI) — backend amount fix required in same cluster | No |
| 2 | Admin orders page (status + tracking number against existing `AdminOrdersController`) — minimum shop operability; product review rates the admin-stub gap P0 | `features/admin/orders/admin-orders.component.ts`, new admin order service | L | Yes (new UI) |

### P1 — important gaps
| # | Item | Finding | Effort | Visual? |
|---|---|---|---|---|
| 3 | Guest cart merge: one-line FE fix (`POST /api/cart/merge?guestSessionId=...`) + E2E for add-as-guest → login → items persist | F22 | XS | No (E2E) |
| 4 | Checkout shipping form field-level validation errors | F2 | S | Yes (3 languages) |
| 5 | Sale-price contract: one mapping sitewide (`salePrice` = current, `basePrice` = original); re-verify card/detail/cart/wishlist/brand after backend fix | F21 | XS UI (backend M) | Yes |
| 6 | Footer legal pages (terms/privacy/cookies/FAQ/shipping/returns) + cookie consent; fix `href="#"` socials | F16 | M (needs legal copy) | Yes |
| 7 | Contact form: wire to real `POST /api/contact` | F15 | S (UI) + backend endpoint | No |
| 8 | Remove/replace fake PayPal & bank-transfer options | F20 | S (UI; coordinate with payments cluster) | No |
| 9 | Admin products CRUD (wire orphaned `product-translation-editor`, `related-products-manager`) + admin users page + dashboard KPIs | F3 | L | Yes |
| 10 | Guest checkout decision: implement or update docs/route | F17 | L if implemented / XS if descoped | Yes if implemented |

### P2 — useful improvements
| # | Item | Finding | Effort | Visual? |
|---|---|---|---|---|
| 11 | Wishlist: confirm/undo for Clear All; toast all API failures; restore-on-failure | F4 | S | No |
| 12 | Router scroll restoration (`withInMemoryScrolling`) | F5 | S | Yes (route-transition interaction) |
| 13 | Header/footer defer: `timer(3200ms)` → `on idle`; functional placeholder search | F6 | S | Yes (re-run Lighthouse) |
| 14 | Account orders: real error state + retry (stop masquerading as empty) | F7 | S | No |
| 15 | Product detail: skeleton, error+retry, 404 routing for bad slugs | F8 | M | Yes (both themes) |
| 16 | Light-theme `--color-error` → error-700 | A1 | XS | Yes (error backgrounds spot-check) |
| 17 | Cart/wishlist names in BG/DE: send `lang` param from cart/wishlist services (backend mapper change required) | F24 | XS UI (backend M) | Yes (BG/DE) |
| 18 | Star ratings: render real aggregates once backend projects them (no FE change expected — verify) | F23 | XS (verify) | Yes |
| 19 | Breadcrumb consolidation (one implementation, `<nav aria-label>` everywhere) | F10/A4 | M | Yes |
| 20 | Breakpoint tokens in `_tokens.scss`; fix 767/768 wishlist clash first | R1/R2 | M (incremental) | Yes (768px viewport) |
| 21 | PDP: bind "In Stock" badge to real stock; OOS add-to-cart state | F18 | S | Yes |
| 22 | Surface unreachable features: compare button + `/compare`, recently-viewed, price-history sparkline, GDPR account section | product.md F14 | L (triage per feature) | Yes |

### P3 — polish
| # | Item | Finding | Effort | Visual? |
|---|---|---|---|---|
| 23 | `role="alert"` on checkout/stripe/cart error banners; focus management on order failure | A2 | XS | SR test |
| 24 | Header search: `aria-label`, `role="search"`, `aria-hidden` SVG | A3 | XS | axe check |
| 25 | `/categories`: redirect to `/products` or build category grid | F13 | S | Yes if built |
| 26 | Single energy-label palette shared by both components | F14 | S | Yes |
| 27 | Wire `empty-state` offline variant / global offline handling | F19 | S | No |

### Items requiring visual/browser confirmation (consolidated)
The review was code-reading only — no browser was run. Before closing the following, verify in a real browser: dark-theme rendering of wishlist sharing UI and home-v3 (no code gaps found, but unconfirmed); A1 error-color change against `--color-error` backgrounds; scroll-restoration interplay with route animations; Lighthouse budget after removing defer timers; 768px wishlist dead-padding; BG/DE text overflow; sale-price rendering on card/detail/cart/wishlist/brand after the F21 contract fix; translated product names in cart/wishlist under BG/DE (F24); screen-reader announcement of checkout failures (A2); all new admin/legal pages in both themes and all three languages per the Definition of Done in `CLAUDE.md`.

---

## Relationship to existing docs

- `docs/plans/18-project-completion.md` — Plan 18's Phase 1 (home v3) and Phase 2 wishlist-slice claims were **verified accurate** by the product review; however, Plan 18 contains **no admin-UI task** on its path to v1.0.0 — that gap must be added (items #2, #9 above). Wishlist "Complete" status should be qualified by F4 (silent error swallowing) and F24 (untranslated names).
- `docs/validation/areas/08-admin-panel.md:364-393` — already flags admin stubs as Critical; still true; this document supersedes nothing there, it confirms it.
- `docs/validation/areas/07-wishlist.md` — wishlist validation predates F4; the silent-failure finding is new.
- `CLAUDE.md` — status table is **stale** on: Admin Panel ("Complete" → 3 of 4 pages are stubs), Shared Components (BreadcrumbComponent unused), guest checkout business rule (blocked by `authGuard`), the "Cart merges when guest user logs in" rule (broken in practice, F22), and Checkout "Complete" (see payments P0 in `_review/product.md`). Correct it as part of item #2/#9 work (tracked as P2 docs work in `_review/docs.md`).
- Full evidence trail, verifier notes, and the per-page state matrix: `docs/project-plan/_review/uiux.md`; flow inventory and backend/frontend wiring map: `docs/project-plan/_review/product.md`; full bug entries for F21–F24: `docs/project-plan/_review/bugs.md` and `docs/project-plan/BUGS_AND_TECH_DEBT.md`.
