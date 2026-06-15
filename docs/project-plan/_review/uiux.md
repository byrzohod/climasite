# UI & UX — Review Findings (2026-06-11)

## Summary

The ClimaSite frontend shows strong UI/UX fundamentals in most customer-facing areas: i18n is in perfect parity (1006 identical keys across en/bg/de, with essentially zero hardcoded user-facing strings), accessibility plumbing is genuinely good (full focus traps with focus-restore in Modal and mini-cart drawer, `role=alert` toasts, `prefers-reduced-motion` handled in 30+ files, 44px touch targets in bottom-nav, returnUrl-aware guards that wait for auth hydration), and `data-testid` coverage is high (~469 occurrences). Product list, cart, account orders/profile, contact, and the new home-v3 recommendations all implement loading/empty/error states, several with skeletons and retry.

However, there are real gaps in the money path: the checkout shipping form has validators but renders no field-level errors anywhere (the `.field-error` CSS is dead code) while its submit button is disabled-when-invalid, leaving users with zero guidance; and currency presentation is inconsistent — product pages format prices as EUR while cart/checkout/order summary use the bare currency pipe (USD default, no `DEFAULT_CURRENCY_CODE`) plus hardcoded "$9.99/$19.99" shipping labels. The admin panel is largely "Coming Soon" stubs (products/orders/users) despite CLAUDE.md claiming it complete. The new wishlist slice is solid on states and theming but silently swallows all API errors (share toggle, regenerate, clear) and performs an unconfirmed, non-undoable "Clear all".

Cross-cutting issues: no router scroll restoration, header/footer deferred behind a fixed 3.2s timer (no search/theme/language/cart-badge for the first 3.2s of every cold load), an unused shared BreadcrumbComponent with six hand-rolled duplicates, fragmented breakpoints (767px vs 768px and eight other ad-hoc values), and a light-theme error color (#ef4444 on white, ~3.8:1) that fails WCAG AA for the small error text it is used on.

## Findings

### 1. Currency inconsistency: product pages show EUR, cart/checkout show USD
- **Finding:** Product surfaces format prices explicitly as EUR while cart, checkout, and order summary use the bare currency pipe (USD default), with hardcoded `$9.99`/`$19.99` shipping labels in checkout.
- **Category:** Forms & purchase flow
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Product surfaces explicitly use EUR: product-card.component.ts, product-detail.component.ts, recommendations.component.html (e.g. `{{ product.basePrice | currency:'EUR' }}`) — 13 files confirmed via grep. Cart and checkout use the bare pipe which defaults to USD: src/ClimaSite.Web/src/app/features/cart/cart.component.ts:73-157 (`{{ item.unitPrice | currency }}`, cart-subtotal/tax/total) and src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts:294,326,334,342,348,352. No `DEFAULT_CURRENCY_CODE` provider exists anywhere (grep over src returned nothing; app.config.ts has none). Checkout also hardcodes shipping prices as literal strings: checkout.component.ts:186 `$9.99`, :192 `$19.99`.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/cart/cart.component.ts; src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts; src/ClimaSite.Web/src/app/app.config.ts; src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts
- **Why it matters:** A product priced €599.99 appears as $599.99 the moment it enters the cart — same number, different currency symbol — through cart, checkout review, and order total. For a real shop this is misleading pricing in the core purchase flow and a likely legal/UX problem; the hardcoded $ shipping fees also bypass i18n/locale formatting.
- **Recommended fix:** Provide `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' }` in app.config.ts, remove all explicit `:'EUR'` args or keep them consistently, and replace the hardcoded `$9.99`/`$19.99` strings with values formatted via the currency pipe sourced from the shipping-method model. (Effort: Small)
- **Acceptance criteria:** All price displays (product card/detail, cart, checkout summary, order confirmation, account orders) render the same currency; shipping method prices come through the currency pipe; grep for `\$9.99|\$19.99` and bare `| currency` returns no inconsistent usages.
- **Dependencies or follow-up:** Confirm intended display currency (EUR vs BGN) with product owner since the shop is Bulgarian (cdl.bg branding).
- **Confidence:** Verified. Verifier fully confirmed the evidence (including the mini-cart drawer also using the bare pipe and no `DEFAULT_CURRENCY_CODE`/`LOCALE_ID` provider anywhere); the bug is slightly broader than claimed — account order history shows the same order total in EUR (orders.component.ts:210) that checkout displayed in USD. P1 correctly calibrated: serious and user-visible, but checkout still functions and the actual charge currency is server-determined.

### 2. Checkout shipping form gives zero field-level validation feedback; error CSS is dead code
- **Finding:** The checkout shipping form declares validators for 8 required fields but renders no field-level error messages anywhere; the `.field-error` CSS is dead code and the submit button is simply disabled while invalid.
- **Category:** Forms UX
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** Validators declared at checkout.component.ts:1051-1060 (8 required fields + email). The template (checkout.component.ts:104-167) contains no error message elements and no `[class.invalid]` bindings — grep for `field-error` in the template finds only the unused style rules at :594, :990, :1002. The submit button is disabled while invalid (:164 `[disabled]="shippingForm.invalid"`), and submitShipping() (:1104-1105) returns early without markAllAsTouched. Contrast: login (login.component.ts:267-297), register (:90), profile (:41-52, :181-194) and contact (contact.component.ts:38-55) all show touched-based field errors.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts
- **Why it matters:** A user who misses or mistypes any of 8 required fields sees only a permanently disabled "Next: Payment" button with no indication of which field is wrong or that anything is wrong at all. This is the highest-value form in the app (checkout step 1) and is the weakest form in the codebase; it directly causes checkout abandonment.
- **Recommended fix:** Add per-field error messages and `[class.invalid]` bindings following the existing pattern in profile.component.ts; either enable the submit button and call markAllAsTouched on submit, or keep it disabled but show inline errors on blur. Reuse the already-styled `.field-error` class and existing `errors.required` translation keys. (Effort: Small)
- **Acceptance criteria:** Submitting/blurring with empty required fields shows a translated error under each invalid field in all 3 languages; the invalid input gets the error border; E2E test covers attempting to proceed with an incomplete shipping form.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified. Verifier confirmed all claims: the `.invalid` input style (:574-581) and `.field-error` rules are dead code, no global SCSS styles ng-invalid/ng-touched as a fallback, and login/contact implement the touched-based pattern checkout lacks. P1 calibrated — not a hard blocker, but zero-feedback validation on checkout step 1 is a genuine abandonment risk with a small, pattern-following fix.

### 3. Admin products/orders/users pages are "Coming Soon" stubs; docs claim Admin Panel complete
- **Finding:** Three of the four admin areas (products, orders, users) are 12-line "Coming Soon" stubs while CLAUDE.md claims the Admin Panel is complete with CRUD.
- **Category:** Documentation mismatch / missing feature
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** admin-orders.component.ts, admin-users.component.ts and admin-products.component.ts are each 12-line stubs rendering `{{ 'common.comingSoon' | translate }}` (verified by reading all three files in full). admin.routes.ts:10-20 routes to them, and the dashboard (admin-dashboard.component.ts:15-26) links to all three as primary tiles. Only the dashboard link grid and moderation (admin-moderation.component.ts, 764 lines) are real. CLAUDE.md status table states "Admin Panel | Complete | CRUD, dashboard". (Sub-components exist under admin/products/components/ — related-products-manager, product-translation-editor — but nothing routes to them.)
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/admin/orders/admin-orders.component.ts; src/ClimaSite.Web/src/app/features/admin/users/admin-users.component.ts; src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts; src/ClimaSite.Web/src/app/features/admin/admin.routes.ts; CLAUDE.md
- **Why it matters:** Three of four admin dashboard tiles are dead ends. Shop staff cannot manage products, orders, or users from the UI at all, while project documentation (and presumably stakeholder expectations) claim this is done. The orphaned admin product sub-components suggest a partially deleted or never-wired implementation.
- **Recommended fix:** Either implement the three admin CRUD pages (wiring in the existing related-products-manager / product-translation-editor components) or, short term, correct CLAUDE.md to "Partial", hide the dead dashboard tiles, and track the gap in docs/plans. (Effort: Large)
- **Acceptance criteria:** Admin dashboard tiles lead to functional pages (or are removed), and the CLAUDE.md status table matches reality.
- **Dependencies or follow-up:** Backend admin endpoints exist (src/ClimaSite.Api/Controllers/Admin/) — frontend wiring is the gap; confirm scope with owner.
- **Confidence:** Verified. Verifier fully confirmed the stubs, routes, and dashboard tiles; no mitigation exists — the orphaned sub-components are imported nowhere, and git history (commit a065dde) shows the stubs were never implemented (only the hard-coded "Coming soon..." text was translated). Backend admin controllers exist, confirming a frontend-only gap. P1 calibrated: major admin-facing gap with false completion claims, but pre-existing on main and the customer-facing shop is unaffected.

### 4. Wishlist destructive and sharing actions fail silently; Clear All has no confirmation or undo
- **Finding:** Wishlist Clear All wipes UI state with no confirmation or undo, and all wishlist API failures (clear, sync, share toggle, regenerate token) are silently swallowed with no user feedback.
- **Category:** States & error handling
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** wishlist.component.ts:402-405 clearWishlist() immediately wipes UI state with no confirm dialog and no undo (cart removal, by contrast, shows an undo toast — cart.component.ts:762). wishlist.service.ts swallows every API failure: clearWishlist :161 `catchError(() => of(null))`, syncAddToApi :286, syncRemoveFromApi :296, fetchFromApi :267. Share actions in the component also swallow errors with no user feedback: toggleSharing :409-412 and regenerateShareToken :417-420 only reset shareLoading on error. ToastService is injected in cart but not used in wishlist.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts; src/ClimaSite.Web/src/app/core/services/wishlist.service.ts
- **Why it matters:** If the DELETE call fails, the user sees an empty wishlist that silently resurrects on next refresh (server still has the items) — confusing data "ghosting". If enabling sharing fails, the button simply un-disables and the user has no idea the link was never created. This is in the feature branch under review (plan18-wishlist-completion) and contradicts its "Complete" status.
- **Recommended fix:** Add a confirm step (shared Modal) or undo toast for Clear All; surface share/regenerate/clear failures via ToastService with translated messages; on API failure for clear, restore the previous items (keep a snapshot) instead of leaving divergent state. (Effort: Small)
- **Acceptance criteria:** Simulated 500 on PUT /api/wishlist/share and DELETE /api/wishlist shows a translated error toast and leaves UI state consistent with the server; Clear All requires confirmation or offers undo; covered by component spec.
- **Dependencies or follow-up:** ToastService and ModalComponent already exist and are translation-ready.
- **Confidence:** Verified.

### 5. No router scroll restoration — scroll position persists across page navigations
- **Finding:** The router is configured without scroll position restoration, so scroll offset persists across every navigation and back-navigation position is not restored.
- **Category:** Navigation
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** app.config.ts provides `provideRouter(routes)` with no `withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })` (read in full). Repo-wide grep for scrollPositionRestoration/withInMemoryScrolling returns nothing. No NavigationEnd scroll handler exists in app.component.ts or main-layout.component.ts (both read in full). The only scroll resets are page-local: product-list.component.ts:1364 (pagination) and animation.service.ts:229 (helper, unused for routing).
- **Affected files/areas:** src/ClimaSite.Web/src/app/app.config.ts; src/ClimaSite.Web/src/app/app.component.ts
- **Why it matters:** Scrolling halfway down the product list and clicking a product opens the detail page already scrolled to the same offset; same for cart → checkout, footer links, etc. This is a classic SPA defect affecting almost every navigation, and back-navigation position is also not restored.
- **Recommended fix:** Add `withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' })` to provideRouter in app.config.ts. (Effort: Small)
- **Acceptance criteria:** Navigating from a scrolled list to a detail page starts at the top; browser Back returns to the previous scroll offset; verified in an E2E test or manual QA.
- **Dependencies or follow-up:** Verify interaction with the route fade/slide transition animations (data.animation) after enabling.
- **Confidence:** Verified.

### 6. Header and footer deferred behind a fixed 3.2s timer; placeholder lacks core functionality
- **Finding:** Header and footer are deferred behind a fixed `timer(3200ms)`, and the placeholder shell lacks search, language/theme controls, cart/wishlist badges, and the user menu for the first 3.2 seconds of every cold load.
- **Category:** Navigation & perceived performance
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** main-layout.component.ts: `@defer (on timer(3200ms)) { <app-header /> }` and the same for `<app-footer />` (footer placeholder is a 1px div). The placeholder shell renders static links only: its "search" is a link to /products (header-shell__search), and it contains no language selector, no theme toggle, no cart/wishlist count badges, no user menu, no mega menu — all of which exist only in the real header (header.component.ts:48-49, :256, :94, :111). On mobile the placeholder additionally hides top bar, nav and action icons entirely (main-layout styles `@media (max-width: 767px) { .header-shell__top, .header-shell__nav, .header-shell__actions { display: none; } }`). home-v3.component.html similarly defers room-preview, recommendations and secondary content behind timer(2200ms).
- **Affected files/areas:** src/ClimaSite.Web/src/app/core/layout/main-layout/main-layout.component.ts; src/ClimaSite.Web/src/app/core/layout/header/header.component.ts; src/ClimaSite.Web/src/app/features/home-v3/home-v3.component.html
- **Why it matters:** For the first 3.2 seconds of every cold load (not just the home page), users cannot type a search, see how many items are in their cart, open the account menu, or switch language/theme; fast users clicking "search" get bounced to /products. A fixed wall-clock timer also penalizes fast devices — this looks tuned for the Lighthouse score documented in CLAUDE.md rather than real UX. Footer popping in after 3.2s can shift layout for users who scroll immediately.
- **Recommended fix:** Replace `on timer(3200ms)` with `on idle` (or `on idle; on interaction`) so hydration happens as soon as the main thread is free, and make the placeholder search link focus/open the real search when it loads. Keep timers only if a measured regression justifies them. (Effort: Small)
- **Acceptance criteria:** Header becomes interactive as soon as the browser is idle (typically <1s on desktop); Lighthouse mobile score stays within agreed budget; cart badge appears without a fixed 3.2s wait.
- **Dependencies or follow-up:** Re-run the Lighthouse checks referenced in CLAUDE.md (mobile 0.97/desktop 1.00) after the change.
- **Confidence:** Verified.

### 7. Account orders API failure renders the "no orders" empty state instead of an error state
- **Finding:** When the account orders API fails, the page falls back to the "you have no orders yet" empty state rather than showing an error with retry.
- **Category:** States & error handling
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** orders.component.ts:835-838 — on error, loadOrders() sets `paginatedOrders` to an empty result (`items: [], totalCount: 0 ...`) and clears loading, which makes the template fall into the `orders-empty` empty state (:161-167, 'emptyState.orders.title' = "you have no orders yet" messaging with a "shop now" CTA). No error signal, no retry affordance.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts
- **Why it matters:** A customer with existing orders who hits a transient network/server error is told they have no orders — factually wrong and alarming for an e-commerce account page. Product-list already has the correct pattern (error state + retry button, product-list.component.ts:252-258).
- **Recommended fix:** Add an `error` signal set in the error callback and render an error block with a retry button (mirroring product-list), keeping the empty state only for genuine zero-result responses. (Effort: Small)
- **Acceptance criteria:** With the API stubbed to return 500, /account/orders shows a translated error with a working Retry button instead of the empty state.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 8. Product detail loading/error states are bare text — no skeleton, no retry, no 404 handling
- **Finding:** The product detail page renders loading and error states as single lines of plain text with no skeleton, no retry action, and no 404 handling for bad slugs.
- **Category:** States
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** product-detail.component.ts:29-36 — loading is a single text line `{{ 'common.loading' | translate }}` and error a single text div with no retry action or back link; :897 sets 'products.details.notFound' as an in-page error string rather than redirecting/rendering the NotFoundComponent. Compare product-list.component.ts:235-258 which has full card skeletons, an error state with icon, and a retry button.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts; src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts
- **Why it matters:** Product detail is the highest-traffic, highest-intent page; a flash of unstyled centered text on every visit (and a dead-end error on bad slugs) is markedly below the quality bar set by the adjacent list page, and stale/bad product links from SEO get a generic message instead of a 404.
- **Recommended fix:** Add a detail-page skeleton (gallery block + title/price/button lines, components already exist in shared/skeleton), an error state with retry + "back to products" actions, and route genuine 404 responses to the not-found page or an equivalent in-page 404 with status semantics. (Effort: Medium)
- **Acceptance criteria:** Loading shows a skeleton matching the final layout (verified in light and dark themes); API 500 shows retry; unknown slug shows 404 UX.
- **Dependencies or follow-up:** SkeletonComponent / skeleton-product-card already exist for reuse.
- **Confidence:** Verified.

### 9. Light-theme error color #ef4444 fails WCAG AA contrast for the small error text it styles
- **Finding:** The light-theme `--color-error` token maps to #ef4444 (~3.76:1 on white), below the 4.5:1 AA threshold for the small error text it is used on — and it is the only semantic token using the 500 shade instead of 700.
- **Category:** Accessibility
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** _colors.scss:295 maps `--color-error: #{$color-error-500}` where $color-error-500 = #ef4444 (:137). #ef4444 on white is ~3.76:1, below the 4.5:1 AA threshold for normal-size text, and error text is rendered small: checkout .field-error 0.875rem (checkout.component.ts:594-596), cart .item-error (cart.component.ts:451-453), stripe-error (:691-692), wishlist hover states. Notably every other semantic token uses the 700 shade (--color-success: success-700, --color-warning: warning-700, --color-info: info-700, _colors.scss:287-300) — error is the lone 500.
- **Affected files/areas:** src/ClimaSite.Web/src/styles/_colors.scss
- **Why it matters:** Validation and failure messages — exactly the text low-vision users must be able to read — are the least readable semantic color in the light theme. The fix is a one-line token change since error-700 (#b91c1c, ~6.9:1) already exists and matches the pattern used by the other semantic colors.
- **Recommended fix:** Change light-theme `--color-error` to $color-error-700 (or -600 at minimum) in _colors.scss; keep error-500 for borders/icons where 3:1 suffices. (Effort: Small)
- **Acceptance criteria:** All error text in light theme measures ≥4.5:1 against its background (axe/Lighthouse a11y pass); dark theme unaffected.
- **Dependencies or follow-up:** Visual spot-check that buttons/badges using --color-error as background still look right (needs-confirmation visually).
- **Confidence:** Verified.

### 10. Shared BreadcrumbComponent is never used; six pages hand-roll inconsistent breadcrumbs
- **Finding:** The shared BreadcrumbComponent has zero usages while six pages hand-roll their own breadcrumbs with divergent semantics (some accessible `<nav>` landmarks, some plain `<div>`s).
- **Category:** Consistency / component reuse
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Grep for `app-breadcrumb` across src/app finds zero usages outside the component itself, while ad-hoc `class="breadcrumb"` markup exists in product-detail.component.ts:40 (proper `<nav>` with aria-label), product-list.component.ts, brand-detail.component.ts, promotion-detail.component.ts, category-header.component.ts, and the new wishlist.component.ts:30-36 — which uses a plain `<div>` with no nav landmark or aria-label, unlike product-detail.
- **Affected files/areas:** src/ClimaSite.Web/src/app/shared/components/breadcrumb; src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts; src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts
- **Why it matters:** Duplication has already produced divergent semantics (some breadcrumbs are accessible nav landmarks, some are plain divs) and divergent styling. CLAUDE.md explicitly lists Breadcrumb among the "Shared Components Complete" deliverables, so this is also a docs/claims mismatch.
- **Recommended fix:** Migrate the six ad-hoc implementations to the shared BreadcrumbComponent (or delete the shared component if it is not fit for purpose and standardize the inline pattern with `<nav aria-label>` semantics). (Effort: Medium)
- **Acceptance criteria:** One breadcrumb implementation repo-wide; all instances are `<nav>` landmarks with translated aria-labels and consistent styling in both themes.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 11. Breakpoint fragmentation: ten ad-hoc max-width values with a 767px/768px boundary clash
- **Finding:** Ten different ad-hoc max-width breakpoints exist with no shared tokens/mixins, including a concrete 767px vs 768px clash that reserves dead bottom-nav space on the wishlist page at exactly 768px wide.
- **Category:** Responsiveness
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Grep across component styles: 27× `max-width: 768px`, 13× 1024px, 8× 640px, 4× 767px, 4× 480px, 3× 980px, 3× 600px, 2× 720px, 1× 540px, 1× 374px. No breakpoint variables/mixins exist in src/styles/ (checked _tokens.scss, _spacing.scss; grep 'breakpoint' returns nothing). Concrete clash: bottom-nav shows at ≤767px (bottom-nav.component.ts:105) and main-layout pads content at ≤767px, but wishlist.component.ts:307-309 reserves bottom-nav space at ≤768px — at exactly 768px wide (iPad Mini portrait CSS width) the wishlist page reserves 5rem for a nav that is not rendered.
- **Affected files/areas:** src/ClimaSite.Web/src/styles/_tokens.scss; src/ClimaSite.Web/src/app/shared/components/bottom-nav/bottom-nav.component.ts; src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts; src/ClimaSite.Web/src/app/core/layout/main-layout/main-layout.component.ts
- **Why it matters:** Without shared tokens, every new component invents its own breakpoint, producing off-by-one gaps (dead padding at 768px) and inconsistent collapse points (980 vs 1024, 600 vs 640) that make tablet behavior unpredictable and refactors risky.
- **Recommended fix:** Define SCSS breakpoint variables/mixins (e.g. $bp-mobile: 767px, $bp-tablet: 1024px) in styles/_tokens.scss, document them, and migrate components incrementally starting with the 767/768 conflict. (Effort: Medium)
- **Acceptance criteria:** Bottom-nav visibility and content padding share one breakpoint constant; new lint/review rule references the tokens; no dead reserved space at 768px.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 12. Checkout order-failure banner is not announced to screen readers
- **Finding:** The checkout order-failure banner and stripe-error message have no `role="alert"` or aria-live, so screen-reader users get no announcement when placing an order fails.
- **Category:** Accessibility
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** checkout.component.ts:252-256 — the review-step error `<div class="error-message" data-testid="checkout-error">` has no `role="alert"` or aria-live; same for the stripe-error `<p>` at :228-230 and cart's item-error (cart.component.ts:109-111). The codebase already does this correctly elsewhere: recommendations error has `role="alert"` (recommendations.component.html:22) and toasts use `role="alert"` (toast.component.ts:12).
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts; src/ClimaSite.Web/src/app/features/cart/cart.component.ts
- **Why it matters:** When placing an order fails — the most critical error in the app — screen-reader users get no announcement; the message appears above the fold while focus stays on the Place Order button.
- **Recommended fix:** Add `role="alert"` (or `aria-live="assertive"`) to the checkout error banner and stripe-error, and consider moving focus to the banner on failure. (Effort: Small)
- **Acceptance criteria:** VoiceOver/NVDA announces the order-failure message immediately when placeOrder fails (axe rule check + manual SR test).
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 13. Header search input has no accessible name (placeholder-only labeling)
- **Finding:** Both desktop and mobile header search inputs rely on placeholder text only — no aria-label, no associated label, no `role="search"` on the form, and the decorative search SVG lacks `aria-hidden`.
- **Category:** Accessibility
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** header.component.ts:73-80 — the desktop search `<input>` has placeholder and data-testid but no aria-label, no associated `<label>`, and the decorative search SVG (:70-72) lacks `aria-hidden="true"`; the form has no `role="search"`. The mobile search input (:222-231) has the same pattern. The submit buttons do have aria-labels (:81, :232).
- **Affected files/areas:** src/ClimaSite.Web/src/app/core/layout/header/header.component.ts
- **Why it matters:** Placeholder text disappears on input and is not reliably exposed as the accessible name; screen-reader users tabbing into the site-wide search hear an unnamed text field. Minor but on every page.
- **Recommended fix:** Add `[attr.aria-label]="'common.aria.search' | translate"` to both inputs, aria-hidden on the decorative SVG, and `role="search"` on the form. (Effort: Small)
- **Acceptance criteria:** axe reports no "form elements must have labels" violation on the header in any language.
- **Dependencies or follow-up:** Translation key common.aria.search already exists (used on the button).
- **Confidence:** Verified.

### 14. /categories route is a "Coming Soon" stub (currently unlinked dead route)
- **Finding:** The /categories route renders only a "Coming Soon" message; nothing links to it internally, but it remains routed and reachable by direct URL.
- **Category:** Navigation
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** category-list.component.ts (29 lines, read in full) renders only `{{ 'categories.comingSoon' | translate }}`. It is routed at app.routes.ts:59-62. Grep found no internal links to /categories outside the route definition itself, so it is reachable only by direct URL — but it is indexed in the route table and previously-shared URLs would land on a stub.
- **Affected files/areas:** src/ClimaSite.Web/src/app/features/categories/category-list/category-list.component.ts; src/ClimaSite.Web/src/app/app.routes.ts
- **Why it matters:** Category browsing exists via /products/category/:slug and the mega-menu, so this stub adds confusion; users or crawlers hitting /categories get a near-empty page instead of being redirected to the working category UX.
- **Recommended fix:** Either redirect /categories to /products (redirectTo) or build a real category index page using the existing CategoryService. (Effort: Small)
- **Acceptance criteria:** /categories shows a functional category index or 301-style redirect to /products.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 15. Energy-rating colors hardcoded and duplicated between two components, bypassing _colors.scss
- **Finding:** Two components encode two different hardcoded palettes for the same EU energy-class concept, so the product card badge and the detail-page rating bar can disagree on the color for the same class.
- **Category:** Theming
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** energy-rating.component.ts:131-140 hardcodes the EU energy label hex palette ('A+++': '#00A651' … 'G': '#A11131', fallback '#999' :158, text '#1a1a1a'/'#ffffff' :169) and product-card.component.ts:338-358 hardcodes a second, different gradient palette for energy classes (#10b981, #22c55e, #eab308, #f97316, #ef4444). All other hex hits from the theming grep were benign (CSS var fallbacks in button.component.ts:213, social brand colors in share-product.component.ts:203-221, mask trick #fff in glass-card.component.ts:148-152, confetti palette, HTML entities).
- **Affected files/areas:** src/ClimaSite.Web/src/app/shared/components/energy-rating/energy-rating.component.ts; src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts
- **Why it matters:** EU energy-label colors are a regulated palette so hardcoding is defensible — but two components encode two different palettes for the same concept, so a product card badge and the detail-page rating bar can disagree on the color for class "A". This is the only substantive deviation from the otherwise well-followed single-source-of-truth color rule.
- **Recommended fix:** Move the official EU label palette into a single exported constant (or CSS vars in _colors.scss with a documented "fixed, non-themed" section) and consume it from both components. (Effort: Small)
- **Acceptance criteria:** One palette definition; energy class colors identical on product card and product detail in both themes.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified.

### 16. Documented guest checkout does not exist — checkout route requires authentication
- **Finding:** CLAUDE.md documents guest checkout (orders store guest email), but the checkout route is guarded by authGuard and the checkout UI has no guest email capture path at all.
- **Category:** Documentation mismatch
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** CLAUDE.md Key Business Rules states "Orders require user authentication (guest checkout stores email)". app.routes.ts:82-89 puts authGuard on the checkout root, and grep for "guest" in checkout.component.ts returns nothing — there is no guest email capture path in the checkout UI. Guests are redirected to /login?returnUrl=/checkout (auth.guard.ts:27-30).
- **Affected files/areas:** src/ClimaSite.Web/src/app/app.routes.ts; CLAUDE.md; src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts
- **Why it matters:** Forced registration before checkout is one of the best-documented cart-abandonment causes; if guest checkout was a product requirement (as the doc implies), it is silently missing, and if it was descoped the doc is stale. Either way the discrepancy should be resolved deliberately, not discovered in production.
- **Recommended fix:** Decide with the owner: implement guest checkout (email capture step, backend already stores email per docs) or update CLAUDE.md business rules to "authenticated checkout only". (Effort: Large)
- **Acceptance criteria:** Docs and route behavior agree; if guest checkout is implemented, an E2E covers the unauthenticated purchase flow.
- **Dependencies or follow-up:** Backend support for guest orders must be confirmed (orders API may require a user id).
- **Confidence:** Verified.

## Dimension data

### Missing-states matrix (main pages)

| Page (component) | Loading | Empty | Error | Retry | Notes |
|---|---|---|---|---|---|
| Home v3 (`home-v3.component.html` + recommendations) | Skeleton cards (rec component) | `rec-empty` text | `role=alert` error | No retry button | Sections deferred behind `timer(2200ms)`; wizard itself is static/instant. |
| Products list (`product-list.component.ts:81-258`) | Filter + card skeletons | shared `app-empty-state` | Icon + message | Yes (`retry-button`) | Best-in-repo pattern. |
| Product detail (`product-detail.component.ts:29-36`) | Plain text "Loading..." | n/a | Plain text, no action | **No** | No skeleton; bad slug shows in-page error, not 404 page (`:897`). |
| Cart (`cart.component.ts:20-36,105-111`) | Text "Loading..." | shared `app-empty-state` | Banner + per-item errors | Implicit (cart reload) | Remove has **undo toast** (`:762`) — gold standard in repo. |
| Checkout (`checkout.component.ts`) | Stripe loading text; processing state on Place Order | n/a (guarded by cart) | Banner (`checkout-error`), stripe-error | Re-click Place Order | **No field-level validation errors** (P1). Error banner not aria-live. |
| Order confirmation (`order-confirmation.component.ts:32-44`) | Spinner | n/a | Error state + actions | Yes | Good. |
| Wishlist (`wishlist.component.ts:43-62`) | shared `app-loading` | shared `app-empty-state` (+ shared-not-found variant) | **None** — all API errors swallowed | No | Share/clear failures invisible (P2). |
| Shared wishlist view (same component) | yes | not-found empty state | covered by notFound | n/a | Good. |
| Account orders (`orders.component.ts:132-167,835`) | Skeleton list | shared empty states (filtered/unfiltered) | **Masquerades as empty** | No | P2 finding. |
| Account profile (`profile.component.ts`) | per-form loading | n/a | success+error banners, field errors | n/a | Good forms pattern. |
| Account addresses (`addresses.component.ts:231`) | service loading | (not fully audited) | disabled-while-loading | — | Disabled-until-valid submit, similar to checkout but with shared form component. |
| Admin products/orders/users | — | — | — | — | **"Coming Soon" stubs** (P1). |
| Admin moderation (`admin-moderation.component.ts`, 764 lines) | present | present | present | — | Real implementation. |
| Categories (`category-list.component.ts`) | — | — | — | — | "Coming Soon" stub, unlinked route (P3). |
| Contact (`contact.component.ts:21-55`) | submit disabled | n/a | success + error banners, field errors | n/a | Good. |
| 404 (`not-found.component.ts`) | n/a | n/a | n/a | n/a | Exists, translated, wildcard-routed (`app.routes.ts:135-138`). |
| Offline handling | — | `empty-state` has an unused `offline` variant | No global offline/retry interceptor (`navigator.onLine` unused) | — | Gap, low priority. |

### i18n key-diff results (read-only node script)

```
en: 1006 keys
bg: 1006 keys
de: 1006 keys
Missing/extra keys per language pair: NONE (all 6 pairwise diffs empty)
```

Perfect parity. Template scan for hardcoded user-facing strings found none in .html templates and only doc-comments in .ts inline templates. The only literal user-visible strings found: `$9.99` / `$19.99` shipping prices (checkout.component.ts:186,192) and the placeholder-header phone `0898 567 504` / brand `CDL` (main-layout.component.ts, arguably brand constants).

### Theming audit summary

- 97 raw hex grep hits in app/ → after triage: ~40 HTML entities (false positives), CSS `var(..., #fff)` fallbacks (button.component.ts), `#fff` mask trick (glass-card), social brand colors (share-product), confetti palette (decorative, has reduced-motion guard), test specs. Substantive: dual energy-label palettes (finding above). `_colors.scss` discipline is otherwise excellent; new wishlist sharing UI and home-v3 use CSS vars throughout (wishlist.component.ts styles, room-preview takes a `[theme]` input).
- Dark theme: tokens fully mirrored (`_colors.scss:429-434` etc.); no component-level theme gaps spotted in new features (visual verification needs-confirmation — cannot run browser).

### Accessibility highlights (verified strengths)

- ModalComponent: role=dialog, aria-modal, labelledby, Tab trap, Escape, focus restore (`modal.component.ts:28-30,281-370`).
- Mini-cart drawer: full focus trap + Escape + restore (`mini-cart-drawer.component.ts:217-310`).
- Toasts: role=alert + aria-live polite container (`toast.component.ts:12,440-442`).
- prefers-reduced-motion handled in 34 files incl. animation/confetti/flying-cart services, reveal/count-up/split-text directives, `_animations.scss`.
- Home-v3 wizard: labeled slider with aria-valuenow/min/max, radiogroup/radio + aria-checked segmented controls, aria-live estimate card (`wizard.component.html:7-68`).
- Bottom nav: 44px min touch targets + safe-area insets (`bottom-nav.component.ts:152-153,125`).
- Guards: authGuard/adminGuard/guestGuard all wait for `authReady` and preserve returnUrl (`auth.guard.ts`).

### data-testid coverage

469 occurrences across non-spec app code. Strong on: header (36), checkout (40), cart (21), wishlist (new code fully covered incl. share panel), home-v3 (wizard/rec cards indexed). Gaps: admin-dashboard tiles have none (stub pages anyway); some account sub-forms rely on container-level testids only. Convention is healthy overall.

### Open questions for the team

1. Intended currency: EUR or BGN? (Affects P1 fix direction; site branding is Bulgarian.)
2. Is guest checkout in or out of scope? CLAUDE.md says orders store guest email but the route is auth-gated.
3. Were the 3.2s/2.2s `@defer` timers a deliberate Lighthouse trade-off with measured data, or copy-pasted tuning? `on idle` likely achieves the same score without the fixed delay.
4. Are the orphaned admin product sub-components (related-products-manager, product-translation-editor) awaiting wiring, or leftovers from a removed admin products page?
5. The shared BreadcrumbComponent — keep and migrate, or delete?

## Refuted during verification

None.
