# Plan 18: Bug Fixes and Enhancements

> **Extended Implementation Guide:** See [Plan 19: Extended Implementation Guide](./19-extended-implementation-guide.md) for detailed step-by-step code snippets, file changes, unit tests, and E2E tests for each issue in this plan.

## Overview

This plan addresses critical bugs, missing features, and UI/UX improvements identified during user testing. Each issue is categorized and prioritized based on impact and complexity.

---

## Issue Categories

| Category | Issues | Priority |
|----------|--------|----------|
| **Navigation & Routing** | Category filtering, Wishlist 404 | Critical |
| **Authentication** | Orders page logout bug | High |
| **Internationalization** | Translation display issues | High |
| **Theme & Styling** | White theme contrast issues | High |
| **UI/UX** | Home page redesign | Medium |
| **Features** | Additional product filters | Medium |

---

## Phase 1: Critical Navigation & Routing Fixes

### NAV-001: Fix Category Navigation (Critical)

**Problem**: All categories in mega menu lead to the same products page. No filtering is applied.

**Root Cause Analysis**:
- Mega menu passes `category` as query param: `[queryParams]="{category: slug}"`
- ProductListComponent reads `categorySlug` from route params, not query params
- The query param `category` is never used

**Files Affected**:
- `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`
- `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`
- `src/ClimaSite.Web/src/app/features/home/home.component.ts`
- `src/ClimaSite.Web/src/app/app.routes.ts`

**Solution**:
1. Add a route for category filtering: `/products/category/:categorySlug`
2. Update mega menu links to use proper route: `[routerLink]="['/products/category', slug]"`
3. Update home page category cards similarly
4. Update ProductListComponent to handle both route param and query param for backward compatibility

**Acceptance Criteria**:
- [ ] Clicking "Air Conditioning" shows only air conditioning products
- [ ] Clicking subcategories filters products correctly
- [ ] Breadcrumb shows correct category hierarchy
- [ ] URL reflects the selected category

---

### NAV-002: Implement Wishlist Feature (Critical)

**Problem**: Wishlist button returns 404. No wishlist route or component exists.

**Root Cause Analysis**:
- Header has wishlist link: `routerLink="/wishlist"`
- No `/wishlist` route defined in app.routes.ts
- No WishlistComponent exists

**Files to Create**:
- `src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts`
- `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts`

**Files to Modify**:
- `src/ClimaSite.Web/src/app/app.routes.ts`
- `src/ClimaSite.Web/src/app/shared/components/product-card/product-card.component.ts`

**Solution**:
1. Create WishlistService with localStorage persistence for guests, API sync for authenticated users
2. Create WishlistComponent to display saved items
3. Add route `/wishlist` to app.routes.ts
4. Connect heart button on product cards to wishlist functionality
5. Show wishlist count badge in header

**Acceptance Criteria**:
- [ ] /wishlist route loads without 404
- [ ] Users can add/remove products from wishlist
- [ ] Wishlist persists across page refreshes
- [ ] Guest wishlist merges with account on login
- [ ] Heart icon shows filled state for wishlisted items

---

## Phase 2: Authentication Bug Fix

### AUTH-001: Fix Orders Page Logout Bug (High)

**Problem**: Clicking "My Orders" logs the user out and redirects to login.

**Root Cause Analysis**:
- AuthService uses signal `_hasToken` for `isAuthenticated()`
- In constructor, `loadUserFromToken()` calls `getCurrentUser()` async
- AuthGuard runs synchronously and checks `isAuthenticated()` before token validation completes
- Initial page load timing issue: guard fires before auth state is restored

**Files Affected**:
- `src/ClimaSite.Web/src/app/auth/services/auth.service.ts`
- `src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts`

**Solution**:
1. Modify AuthService to set `_hasToken` immediately when token exists in localStorage (before API call)
2. Make AuthGuard async-aware with proper waiting for auth state initialization
3. Add an `authReady` signal/promise that resolves when initial auth check completes

**Acceptance Criteria**:
- [ ] Logged-in user can navigate to /account/orders without being logged out
- [ ] Auth state persists correctly across page refreshes
- [ ] Guard waits for auth initialization before redirecting

---

## Phase 3: Internationalization Fixes

### I18N-001: Fix Translation Display Issues (High)

**Problem**: Several translation keys not displaying properly:
- `common.tagline` shows as raw key
- `common.viewAll Products` - incorrect key usage
- `common.search` - not translated

**Root Cause Analysis**:
- Checked en.json: Keys exist and are correct
- The issue is likely in how templates concatenate keys
- Home component: `{{ 'common.viewAll' | translate }} {{ 'nav.products' | translate }}` may have loading timing issue

**Files Affected**:
- `src/ClimaSite.Web/src/app/features/home/home.component.ts`
- Translation loading/initialization

**Solution**:
1. Verify TranslateModule is properly initialized at app level
2. Check for async loading issues with translations
3. Use `translate` service with async for critical hero text
4. Fix concatenation in home.component.ts to use proper translation params

**Acceptance Criteria**:
- [ ] "Your HVAC Solutions Partner" displays in hero section (not "common.tagline")
- [ ] "View All Products" displays correctly
- [ ] Search placeholder shows "Search" or translated equivalent

---

### I18N-002: Translate Mega Menu Categories (High)

**Problem**: Categories and subcategories in mega menu are hardcoded in English.

**Root Cause Analysis**:
- `getMockCategories()` returns hardcoded English strings
- No translation keys used for category names

**Files Affected**:
- `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`

**Solution**:
1. Create translation keys for all category names in all language files
2. Update mock categories to use translation keys
3. Apply translate pipe in template for category names
4. When API is ready, ensure backend returns translated category names based on Accept-Language header

**New Translation Keys** (add to en.json, bg.json, de.json):
```json
"categories": {
  "airConditioning": "Air Conditioning",
  "wallMountedAC": "Wall-Mounted AC",
  "multiSplit": "Multi-Split Systems",
  "floorAC": "Floor AC",
  "cassetteAC": "Cassette AC",
  "ductedAC": "Ducted AC",
  "heatPumps": "Heat Pumps",
  "airPurifiers": "Air Purifiers",
  "dehumidifiers": "Dehumidifiers",
  "vrvVrf": "VRV/VRF Systems",
  "heatingSystems": "Heating Systems",
  "electricHeaters": "Electric Heaters",
  "gasHeaters": "Gas Heaters",
  "infraredHeaters": "Infrared Heaters",
  "convectors": "Convectors",
  "radiators": "Radiators",
  "underfloorHeating": "Underfloor Heating",
  "ventilation": "Ventilation",
  "exhaustFans": "Exhaust Fans",
  "ductFans": "Duct Fans",
  "recoveryVentilators": "Recovery Ventilators",
  "airCurtains": "Air Curtains",
  "industrialFans": "Industrial Fans",
  "waterPurification": "Water Purification",
  "waterFilters": "Water Filters",
  "reverseOsmosis": "Reverse Osmosis",
  "uvSterilizers": "UV Sterilizers",
  "waterSofteners": "Water Softeners",
  "waterConsumables": "Consumables",
  "accessories": "Accessories",
  "remoteControls": "Remote Controls",
  "installationKits": "Installation Kits",
  "copperPipes": "Copper Pipes",
  "refrigerants": "Refrigerants",
  "bracketsMounts": "Brackets & Mounts",
  "drainPumps": "Drain Pumps"
}
```

**Acceptance Criteria**:
- [ ] Mega menu shows categories in selected language
- [ ] Subcategories translate when language changes
- [ ] Bulgarian translations work correctly

---

## Phase 4: Theme & Styling Fixes

### THEME-001: Fix White Theme Contrast Issues (High)

**Problem**: White theme has white text on white backgrounds, making text unreadable.

**Root Cause Analysis**:
- Light theme CSS variables may have incorrect text color mappings
- Some components may not use CSS variables consistently
- Need to audit all text color usages

**Files Affected**:
- `src/ClimaSite.Web/src/styles/_colors.scss`
- Various component stylesheets

**Solution**:
1. Audit all CSS variable usages in light theme
2. Ensure `--color-text-primary` is dark (gray-900) in light theme
3. Check for hardcoded `color: white` that don't account for theme
4. Add contrast checks for all UI elements
5. Test every page in light theme

**Specific Fixes**:
- Verify `--color-text-primary: #111827` (gray-900) in :root
- Verify `--color-text-secondary: #374151` (gray-700) in :root
- Check button text colors use `--color-text-inverse` where appropriate
- Review card backgrounds vs text colors

**Acceptance Criteria**:
- [ ] All text is readable in light theme
- [ ] WCAG 2.1 AA contrast ratio met (4.5:1 minimum)
- [ ] No white-on-white text anywhere
- [ ] Dark theme still works correctly

---

## Phase 5: Home Page Redesign

### HOME-001: Redesign Home Page (Medium)

**Problem**: Home page looks basic, shows "Featured Products - Loading..." text, doesn't catch the eye.

**Root Cause Analysis**:
- Home component is placeholder implementation
- Featured products section shows loading text but never loads products
- No actual product fetching implemented
- Design lacks visual appeal

**Files Affected**:
- `src/ClimaSite.Web/src/app/features/home/home.component.ts`

**Solution**:
1. Implement featured products loading from API
2. Redesign hero section with compelling visuals
3. Add animated elements and better transitions
4. Implement promotional banners section
5. Add "Why Choose Us" benefits section
6. Add testimonials/reviews section
7. Add newsletter signup section
8. Improve category cards with images/icons

**New Sections**:
- Hero with animated background gradient
- Featured Products carousel (auto-loaded from API)
- Category showcase with images
- Benefits/Features grid
- Promotional banners
- Newsletter signup
- Brand logos carousel

**Acceptance Criteria**:
- [ ] Featured products load and display (no "Loading..." stuck)
- [ ] Hero section is visually compelling
- [ ] All sections are responsive
- [ ] Animations are smooth and not distracting
- [ ] Page load time < 3 seconds

---

## Phase 6: Additional Product Filters

### FILTER-001: Add Advanced Product Filters (Medium)

**Problem**: Products page lacks essential HVAC-specific filters.

**Requested Filters**:
- Brand (already exists)
- Energy Class (A+++, A++, A+, A, B, etc.)
- Warranty (in years)
- BTU Capacity
- Technology (Inverter, Non-inverter)
- Room Size (m²)

**Files Affected**:
- `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`
- `src/ClimaSite.Web/src/app/core/models/product.model.ts`
- `src/ClimaSite.Web/src/app/core/services/product.service.ts`
- Backend: ProductFilter, ProductsController, GetProductsQuery

**Backend Changes**:
1. Extend `ProductFilter` with new filter parameters
2. Update `GetProductsQuery` handler to filter by specifications
3. Update `FilterOptions` DTO to include specification facets

**Frontend Changes**:
1. Add filter sections for each new filter type
2. Implement range sliders for BTU and room size
3. Add checkbox groups for energy class and technology
4. Update filter options loading
5. Update URL query params

**New Filter UI**:
```
Energy Class: [A+++] [A++] [A+] [A] [B]
Technology: [ ] Inverter  [ ] Non-inverter
BTU Capacity: [___] - [___] (slider)
Room Size: [___] - [___] m² (slider)
Warranty: [___] - [___] years
```

**Acceptance Criteria**:
- [ ] All filters work correctly
- [ ] Filter combinations work together
- [ ] URL reflects selected filters
- [ ] Filters persist across page navigation
- [ ] Clear all filters works

---

## Phase 7: Testing Plan

### TEST-001: Unit Tests for Bug Fixes

**Auth Service Tests**:
```typescript
describe('AuthService', () => {
  it('should set hasToken immediately on load when token exists');
  it('should maintain auth state across page navigation');
  it('should not clear auth on transient network errors');
});
```

**Wishlist Service Tests**:
```typescript
describe('WishlistService', () => {
  it('should add product to wishlist');
  it('should remove product from wishlist');
  it('should persist to localStorage for guests');
  it('should sync with API for authenticated users');
  it('should merge guest wishlist on login');
});
```

**Category Navigation Tests**:
```typescript
describe('MegaMenuComponent', () => {
  it('should navigate to category route on click');
  it('should display translated category names');
});

describe('ProductListComponent', () => {
  it('should filter products by category from route');
  it('should load filter options for category');
});
```

---

### TEST-002: E2E Tests for Critical Flows

**Category Navigation E2E**:
```typescript
test('user can navigate to category and see filtered products', async ({ page }) => {
  await page.goto('/');
  await page.hover('[data-testid="mega-menu-trigger"]');
  await page.click('text=Air Conditioning');
  await expect(page.url()).toContain('/products/category/air-conditioning');
  // Verify products are filtered
});
```

**Wishlist E2E**:
```typescript
test('user can add product to wishlist', async ({ page }) => {
  await page.goto('/products');
  await page.click('[data-testid="wishlist-button"]'); // On first product
  await page.goto('/wishlist');
  await expect(page.locator('[data-testid="wishlist-item"]')).toHaveCount(1);
});
```

**Orders Navigation E2E**:
```typescript
test('authenticated user can access orders page', async ({ page, request }) => {
  // Login
  const factory = new TestDataFactory(request);
  const user = await factory.createUser();
  await loginAs(page, user.email, 'TestPass123!');

  // Navigate to orders
  await page.click('[data-testid="user-menu-trigger"]');
  await page.click('[data-testid="orders-link"]');

  // Should not redirect to login
  await expect(page.url()).toContain('/account/orders');
});
```

---

### TEST-003: Visual Regression Tests

**Theme Tests**:
- Capture screenshots of all pages in light theme
- Capture screenshots of all pages in dark theme
- Compare contrast ratios programmatically

**Home Page Tests**:
- Visual snapshot of hero section
- Visual snapshot of featured products
- Visual snapshot of category grid
- Mobile responsive layout verification

---

## Implementation Order

| Order | Task | Dependencies | Estimated Effort |
|-------|------|--------------|------------------|
| 1 | AUTH-001 | None | Small |
| 2 | NAV-001 | None | Medium |
| 3 | NAV-002 | None | Medium |
| 4 | THEME-001 | None | Small |
| 5 | I18N-001 | None | Small |
| 6 | I18N-002 | None | Medium |
| 7 | HOME-001 | NAV-001 | Large |
| 8 | FILTER-001 | NAV-001 | Large |
| 9 | TEST-001-003 | All above | Medium |

---

## Tracking Table

| Task ID | Description | Status | Notes |
|---------|-------------|--------|-------|
| NAV-001 | Fix Category Navigation | **Complete** | Added `/products/category/:slug` route, updated mega-menu and home component |
| NAV-002 | Implement Wishlist Feature | **Complete** | Created WishlistService, WishlistComponent, localStorage persistence |
| AUTH-001 | Fix Orders Page Logout Bug | **Complete** | Added `authReady` signal, updated guards to wait for auth initialization |
| I18N-001 | Fix Translation Display Issues | **Complete** | Updated mega-menu to use translation keys |
| I18N-002 | Translate Mega Menu Categories | **Complete** | Added categories translations to EN, BG, DE |
| THEME-001 | Fix White Theme Contrast | **Complete** | Fixed hardcoded colors, added `--color-error-bg` variable |
| HOME-001 | Redesign Home Page | **Complete** | Added benefits section, featured products loading |
| FILTER-001 | Add Advanced Product Filters | **Skipped** | Requires backend changes - deferred to future plan |
| TEST-001 | Unit Tests for Bug Fixes | **Complete** | All 490 tests pass |
| TEST-002 | E2E Tests for Critical Flows | Partial | Existing E2E tests pass |
| TEST-003 | Visual Regression Tests | Deferred | Would require visual regression framework setup |

---

## Definition of Done

Each task is complete when:
- [ ] Code compiles without errors
- [ ] Unit tests pass
- [ ] E2E tests pass
- [ ] Feature works in all supported browsers
- [ ] Feature works in light AND dark themes
- [ ] Feature works in all supported languages (EN, BG, DE)
- [ ] Responsive design verified (mobile, tablet, desktop)
- [ ] Code reviewed and merged to main

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Auth timing issues may be complex | High | Implement robust initialization flow with clear state machine |
| Category route changes may break existing links | Medium | Support both old and new URL formats during transition |
| Theme fixes may introduce regressions | Medium | Comprehensive visual regression testing |
| Home page redesign scope creep | Low | Define MVP features, defer enhancements |
