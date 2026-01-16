# Plan 20: Comprehensive Fixes and Enhancements

## Overview

This plan addresses all outstanding bugs, UI/UX issues, and missing features identified in the production review. The issues span navigation, authentication, translations, page implementations, and data seeding.

**Priority Levels:**
- **CRITICAL**: Breaks core functionality, must fix immediately
- **HIGH**: Significant user impact, fix within sprint
- **MEDIUM**: Noticeable issues, fix as capacity allows
- **LOW**: Polish/enhancement, nice to have

---

## Issue Summary Table

| ID | Issue | Priority | Category | Estimated Effort |
|----|-------|----------|----------|------------------|
| HOME-002 | Home page needs professional polish | HIGH | UI/UX | Large |
| PROMO-001 | Promotions page shows no products | CRITICAL | Data | Medium |
| NAV-003 | Mega-menu goes off-page on left | CRITICAL | Bug | Medium |
| NAV-004 | Parent categories not clickable | HIGH | Bug | Small |
| NAV-005 | Subcategory translations showing keys | HIGH | Bug | Small |
| AUTH-002 | Orders page logs user out | CRITICAL | Bug | Medium |
| AUTH-003 | Resources links redirect to login | HIGH | Bug | Small |
| BRAND-001 | Brands page shows no brands | CRITICAL | Data | Medium |
| ABOUT-001 | About page spacing/UX issues | MEDIUM | UI/UX | Small |
| RES-001 | Resources page missing URLs | HIGH | Content | Medium |
| CONTACT-001 | Contact page missing map | HIGH | Feature | Medium |
| PRICE-001 | Discounted price higher than regular | CRITICAL | Bug | Small |
| REVIEW-001 | Reviews tab not implemented | HIGH | Feature | Large |
| RELATED-001 | Related products not showing | MEDIUM | Bug | Small |
| WISH-001 | Wishlist navigation inconsistency | MEDIUM | Bug | Small |
| PROFILE-001 | Profile settings page not implemented | HIGH | Feature | Medium |

---

## Section 1: Critical Bugs

### PROMO-001: Promotions Page Shows No Products

**Problem:** Promotions feature is fully implemented but database has no promotion seed data.

**Root Cause:** `DataSeeder.cs` lacks a `SeedPromotionsAsync()` method.

**Files to Modify:**
- `src/ClimaSite.Infrastructure/Data/DataSeeder.cs`

**Implementation:**
1. Create `SeedPromotionsAsync()` method
2. Add 3-5 sample promotions:
   - Winter Sale (20% off heating systems)
   - Summer Cool (15% off air conditioners)
   - Free Shipping Weekend
   - Bundle & Save (BOGO accessories)
3. Link promotions to existing products via `PromotionProduct` join table
4. Set valid date ranges (StartDate <= now <= EndDate)
5. Call method from `SeedAsync()`

**Acceptance Criteria:**
- [ ] Promotions list page shows 3+ active promotions
- [ ] Promotion detail page shows linked products
- [ ] Promo codes can be applied at checkout

---

### NAV-003: Mega-Menu Dropdown Goes Off-Page

**Problem:** Dropdown uses `left: 50%; transform: translateX(-50%)` without viewport bounds checking. On smaller viewports, the 800px-wide menu extends beyond the left edge.

**Root Cause:** No JavaScript boundary detection or CSS clamp() fallback.

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`

**Lines Affected:** 167-179 (mega-menu positioning CSS)

**Implementation:**
1. Add viewport boundary detection in TypeScript:
   ```typescript
   @HostListener('window:resize')
   private calculateMenuPosition(): void {
     const trigger = this.menuTrigger?.nativeElement;
     if (!trigger) return;
     const rect = trigger.getBoundingClientRect();
     const menuWidth = 800;
     const viewportWidth = window.innerWidth;

     // Calculate optimal left position
     let left = rect.left + rect.width / 2 - menuWidth / 2;

     // Clamp to viewport bounds
     left = Math.max(16, Math.min(left, viewportWidth - menuWidth - 16));
     this.menuPosition.set({ left: `${left}px` });
   }
   ```
2. Apply dynamic positioning via `[style.left]`
3. Remove fixed `left: 50%; transform: translateX(-50%)`

**Acceptance Criteria:**
- [ ] Menu stays within viewport on all screen sizes
- [ ] Menu aligns properly when trigger is near edges
- [ ] No horizontal scrollbar appears

---

### NAV-004: Parent Categories Not Clickable

**Problem:** Category buttons have `(click)="navigateToCategory()"` but the handler only closes the menu without navigating.

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`

**Lines Affected:** 41-48 (button template), 471-474 (handler)

**Implementation:**
1. Update `navigateToCategory()` method:
   ```typescript
   navigateToCategory(category: CategoryTree): void {
     this.router.navigate(['/products/category', category.slug]);
     this.closeMenu();
   }
   ```
2. Inject Router in constructor
3. Add proper navigation logic before closing menu

**Acceptance Criteria:**
- [ ] Clicking parent category navigates to category page
- [ ] Category page shows filtered products
- [ ] Menu closes after navigation

---

### NAV-005: Subcategory Translations Showing Keys

**Problem:** `addTranslationKeys()` mapping doesn't match all API slugs. Fallback shows raw category names instead of translation keys.

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`

**Lines Affected:** 417-438

**Implementation:**
1. Update slug-to-key mapping to include ALL category slugs from database:
   ```typescript
   const slugToTranslationKey: Record<string, string> = {
     'air-conditioners': 'categories.airConditioning',
     'split-systems': 'categories.splitSystems',
     'multi-split-systems': 'categories.multiSplit',
     'portable-ac': 'categories.portableAC',
     'window-units': 'categories.windowUnits',
     'heating-systems': 'categories.heating',
     'heat-pumps': 'categories.heatPumps',
     'electric-heaters': 'categories.electricHeaters',
     'gas-heaters': 'categories.gasHeaters',
     'ventilation': 'categories.ventilation',
     'accessories': 'categories.accessories',
     // ... add all from database
   };
   ```
2. Add missing translation keys to en.json, bg.json, de.json
3. Ensure recursive application to all nesting levels

**Acceptance Criteria:**
- [ ] All category names display translated text
- [ ] No raw translation keys visible in any language
- [ ] Works in EN, BG, and DE

---

### AUTH-002: Orders Page Logs User Out

**Problem:** When token expires during orders page load, the auth interceptor triggers logout without proper recovery.

**Files:**
- `src/ClimaSite.Web/src/app/auth/interceptors/auth.interceptor.ts`
- `src/ClimaSite.Web/src/app/auth/services/auth.service.ts`

**Lines Affected:**
- Interceptor: 44 (logout without error handling)
- Service: 149-172 (refreshToken clears auth immediately on error)

**Implementation:**
1. Add retry logic with exponential backoff for token refresh
2. Add proper error handling in interceptor:
   ```typescript
   catchError((refreshError) => {
     // Only logout on definitive 401, not network errors
     if (refreshError.status === 401) {
       return authService.logout().pipe(
         tap(() => router.navigate(['/login'], {
           queryParams: { returnUrl: router.url, reason: 'session_expired' }
         }))
       );
     }
     return throwError(() => refreshError);
   })
   ```
3. Show user-friendly notification when session expires
4. Preserve return URL for post-login redirect

**Acceptance Criteria:**
- [ ] Orders page loads without unexpected logout
- [ ] Network errors don't trigger logout
- [ ] User sees "Session expired" message when token truly expires
- [ ] User can log back in and return to orders page

---

### AUTH-003: Resources Links Redirect to Login

**Problem:** Some resource items have protected routes or broken links that redirect to login.

**File:** `src/ClimaSite.Web/src/app/features/resources/resources.component.ts`

**Lines Affected:** 363-434 (resource definitions)

**Implementation:**
1. Audit all resource URLs for protected routes
2. Remove authentication requirements from public resources
3. For resources requiring login, show clear indication:
   ```typescript
   {
     id: 'warranty-claim',
     titleKey: 'resources.warranty.title',
     type: 'link',
     url: '/account/warranty',
     requiresAuth: true  // Show login prompt instead of silent redirect
   }
   ```
4. Add visual indicator for auth-required resources

**Acceptance Criteria:**
- [ ] Public resources accessible without login
- [ ] Auth-required resources show clear indication
- [ ] No unexpected redirects to login page

---

### BRAND-001: Brands Page Shows No Brands

**Problem:** Brands feature is fully implemented but database has no brand seed data.

**Root Cause:** `DataSeeder.cs` lacks a `SeedBrandsAsync()` method. Products reference brands by name string, not entity relationships.

**Files to Modify:**
- `src/ClimaSite.Infrastructure/Data/DataSeeder.cs`

**Implementation:**
1. Create `SeedBrandsAsync()` method based on existing product brand names:
   ```csharp
   private async Task SeedBrandsAsync()
   {
     var brands = new[]
     {
       new Brand {
         Name = "DualZone",
         Slug = "dualzone",
         Description = "Premium air conditioning solutions",
         CountryOfOrigin = "Germany",
         FoundedYear = 1995,
         LogoUrl = "/images/brands/dualzone.png",
         IsActive = true,
         IsFeatured = true
       },
       // Add: CoolMaster, ArcticBreeze, HeatFlow, VentAir, etc.
     };
     // ... save to database
   }
   ```
2. Create brand logo placeholders or use text-based logos
3. Link products to brand entities (update Product.BrandId foreign key)
4. Add brand translations for BG and DE

**Acceptance Criteria:**
- [ ] Brands list page shows 5+ brands with logos
- [ ] Brand detail page shows brand info and products
- [ ] "Featured Brands" section can be added to home page

---

### PRICE-001: Discounted Price Higher Than Regular Price

**Problem:** Price display logic is inverted. `salePrice` is mapped to `CompareAtPrice` (the higher original price).

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` (lines 77-84)
- `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts` (lines 73-79)
- `src/ClimaSite.Web/src/app/shared/components/similar-products/similar-products.component.ts` (lines 51-57)
- `src/ClimaSite.Web/src/app/shared/components/product-consumables/product-consumables.component.ts` (lines 38-44)

**Implementation:**
1. Correct the display logic in all components:
   ```html
   <!-- BEFORE (Wrong) -->
   <span class="original-price">{{ product.basePrice | currency }}</span>
   <span class="sale-price">{{ product.salePrice | currency }}</span>

   <!-- AFTER (Correct) -->
   <span class="original-price">{{ product.salePrice | currency }}</span>
   <span class="sale-price">{{ product.basePrice | currency }}</span>
   ```
2. Alternative: Fix the DTO mapping in backend to swap values
3. Update labels: "Was: [original]" → "Now: [sale]"

**Acceptance Criteria:**
- [ ] Original/strikethrough price is higher than sale price
- [ ] Discount percentage calculated correctly
- [ ] All product displays (cards, details, similar) show correct pricing

---

## Section 2: High Priority Issues

### HOME-002: Home Page Professional Polish

**Problem:** Home page lacks professional e-commerce feel - missing hero images, brand showcase, testimonials, and proper visual hierarchy.

**File:** `src/ClimaSite.Web/src/app/features/home/home.component.ts`

**Implementation:**
1. **Hero Section Enhancements:**
   - Add background images with overlay text (not just gradients)
   - Implement pause-on-hover for carousel
   - Add keyboard navigation (arrow keys)
   - Add thumbnail indicators instead of dots

2. **Add Missing Sections:**
   - Brands showcase (horizontal scrolling logos)
   - Customer testimonials with photos
   - Seasonal/promotional banners with countdown
   - "Shop by Room" or "Shop by Need" visual cards

3. **Visual Improvements:**
   - Alternate background colors between sections
   - Add subtle shadows and elevation
   - Improve category cards with actual product images
   - Replace emoji icons with proper SVG/icons

4. **Newsletter Form:**
   - Replace `alert()` with proper toast notification
   - Add loading state during submission
   - Add discount incentive ("Get 10% off your first order")

5. **Dark Theme Support:**
   - Replace hardcoded gradient colors with CSS variables
   - Test all sections in dark mode

**Acceptance Criteria:**
- [ ] Hero has background images with proper overlays
- [ ] Brands section shows partner logos
- [ ] Testimonials section with 3+ reviews
- [ ] Newsletter uses toast notifications
- [ ] Looks professional in both light and dark themes
- [ ] All text translated in EN, BG, DE

---

### CONTACT-001: Contact Page Missing Map

**Problem:** Contact page has translation key for map section but no map is rendered.

**File:** `src/ClimaSite.Web/src/app/features/contact/contact.component.ts`

**Implementation:**
1. Add Google Maps or Leaflet integration:
   ```typescript
   // Option A: Google Maps (requires API key)
   // Option B: Leaflet with OpenStreetMap (free, no API key)
   ```
2. Create map section in template:
   ```html
   <section class="map-section">
     <h2>{{ 'contact.map.title' | translate }}</h2>
     <div id="map" class="map-container"></div>
   </section>
   ```
3. Add map initialization in `ngAfterViewInit()`
4. Add location marker at business address
5. Make map responsive

**Dependencies:**
- Leaflet: `npm install leaflet @types/leaflet`
- OR Google Maps: `npm install @angular/google-maps`

**Acceptance Criteria:**
- [ ] Map displays on contact page
- [ ] Business location marked on map
- [ ] Map is responsive on all screen sizes
- [ ] Works in both light and dark themes

---

### REVIEW-001: Reviews Tab Not Implemented

**Problem:** Product detail reviews tab shows "Coming soon" placeholder.

**File:** `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` (lines 235-239)

**Implementation:**
1. Create `ProductReviewsComponent`:
   - Star rating display
   - Review list with pagination
   - "Write a Review" form (authenticated users only)
   - Verified purchase badge
   - Helpful votes

2. Backend endpoints (if not existing):
   - `GET /api/products/{id}/reviews`
   - `POST /api/products/{id}/reviews`
   - `POST /api/reviews/{id}/helpful`

3. Integration:
   - Replace placeholder with component
   - Load reviews when tab activated
   - Show average rating in product header

**Acceptance Criteria:**
- [ ] Reviews tab shows actual reviews
- [ ] Users can write reviews (when authenticated)
- [ ] Star ratings display correctly
- [ ] Review form validates input
- [ ] All text translated

---

### PROFILE-001: Profile Settings Page Not Implemented

**Problem:** Profile page exists as route but only shows "Coming soon" placeholder.

**File:** `src/ClimaSite.Web/src/app/features/account/profile/profile.component.ts`

**Implementation:**
1. Build complete profile management form:
   - Personal information (name, email, phone)
   - Password change section
   - Email preferences/notifications
   - Language preference
   - Theme preference (light/dark)
   - Account deletion option

2. Backend endpoints:
   - `GET /api/account/profile`
   - `PUT /api/account/profile`
   - `PUT /api/account/password`
   - `DELETE /api/account` (with confirmation)

3. Form validation:
   - Email format validation
   - Password strength requirements
   - Confirm password match

**Acceptance Criteria:**
- [ ] User can view their profile
- [ ] User can update personal information
- [ ] User can change password
- [ ] User can set preferences
- [ ] All changes persist correctly

---

### RES-001: Resources Page Missing URLs

**Problem:** All resource items have no `url` property, making them non-clickable.

**File:** `src/ClimaSite.Web/src/app/features/resources/resources.component.ts` (lines 363-434)

**Implementation:**
1. Option A: Add actual resource URLs:
   ```typescript
   {
     id: 'ac-install',
     titleKey: 'resources.categories.installation.items.acInstall.title',
     type: 'pdf',
     url: '/assets/resources/ac-installation-guide.pdf'
   }
   ```

2. Option B: Mark unavailable resources:
   ```typescript
   {
     id: 'ac-install',
     titleKey: '...',
     type: 'pdf',
     status: 'coming_soon'  // Shows badge instead of link
   }
   ```

3. Create placeholder PDFs or link to manufacturer resources

4. Add visual distinction for unavailable resources

**Acceptance Criteria:**
- [ ] Available resources are clickable
- [ ] Unavailable resources show "Coming Soon" badge
- [ ] PDFs open in new tab
- [ ] Videos play or link to video page
- [ ] External links open in new tab

---

## Section 3: Medium Priority Issues

### ABOUT-001: About Page Spacing/UX Issues

**Problem:** Duplicate padding declarations, hardcoded CTA text not translatable.

**File:** `src/ClimaSite.Web/src/app/features/about/about.component.ts`

**Implementation:**
1. Fix duplicate padding (lines 123, 127)
2. Add translation keys for CTA section:
   ```html
   <h2>{{ 'about.cta.title' | translate }}</h2>
   <p>{{ 'about.cta.description' | translate }}</p>
   ```
3. Improve hero section visual impact
4. Add team photos or company imagery

**Acceptance Criteria:**
- [ ] No duplicate CSS declarations
- [ ] All text uses translation keys
- [ ] Page looks balanced and professional
- [ ] Works in all languages

---

### WISH-001: Wishlist Navigation Inconsistency

**Problem:** Header dropdown links to `/account/wishlist` but route is at `/wishlist`.

**File:** `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts` (line 143)

**Implementation:**
1. Change header dropdown link from `/account/wishlist` to `/wishlist`
2. Add wishlist link to mobile navigation menu
3. Ensure consistent navigation across all entry points

**Acceptance Criteria:**
- [ ] All wishlist links go to `/wishlist`
- [ ] Wishlist accessible from mobile menu
- [ ] No 404 errors when clicking wishlist

---

### RELATED-001: Related Products Not Showing

**Problem:** Similar products and consumables components exist but may not be rendering on product detail page.

**Files:**
- `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/similar-products/similar-products.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/product-consumables/product-consumables.component.ts`

**Implementation:**
1. Verify components are imported and rendered in product detail
2. Check API calls are being made
3. Verify database has related product data
4. Add seed data for related products if missing

**Acceptance Criteria:**
- [ ] Similar products section shows on product pages
- [ ] Consumables/accessories section shows where applicable
- [ ] Carousel navigation works
- [ ] Add to cart works from related products

---

## Section 4: Low Priority / Polish

### HOME-003: Hero Carousel Keyboard Navigation

Add arrow key support for hero slider.

### HOME-004: Newsletter Form Validation UI

Show real-time validation feedback, not just on submit.

### CONTACT-002: Form Backend Integration

Connect contact form to actual email sending service.

### ABOUT-002: Add Team Section

Add team member cards with photos and bios.

---

## Implementation Order

### Phase 1: Critical Data Issues (Day 1-2)
1. PROMO-001: Seed promotions data
2. BRAND-001: Seed brands data
3. PRICE-001: Fix price display logic

### Phase 2: Critical Navigation Bugs (Day 3-4)
4. NAV-003: Fix mega-menu positioning
5. NAV-004: Make parent categories clickable
6. NAV-005: Fix subcategory translations
7. AUTH-002: Fix orders page logout
8. AUTH-003: Fix resources redirects

### Phase 3: High Priority Features (Day 5-7)
9. HOME-002: Home page polish
10. CONTACT-001: Add map
11. PROFILE-001: Implement profile page
12. REVIEW-001: Implement reviews tab
13. RES-001: Add resource URLs

### Phase 4: Medium/Low Priority (Day 8-10)
14. ABOUT-001: About page fixes
15. WISH-001: Wishlist nav consistency
16. RELATED-001: Related products
17. Remaining polish items

---

## Testing Requirements

### For Each Fix:
- [ ] Unit tests for new/modified services
- [ ] Component tests for UI changes
- [ ] E2E tests for user flows
- [ ] Manual testing in all 3 languages
- [ ] Manual testing in light and dark themes
- [ ] Mobile responsive testing

### Regression Testing:
- [ ] Existing E2E test suite passes
- [ ] No new console errors
- [ ] No accessibility regressions

---

## Dependencies

### NPM Packages (if needed):
- Leaflet for maps: `npm install leaflet @types/leaflet`
- Swiper for carousels: `npm install swiper` (optional, for better hero slider)

### Assets Needed:
- Hero background images (3+)
- Brand logos (5+)
- Resource PDFs or placeholder documents
- Team photos (optional)

---

## Notes

- All UI changes must use CSS variables from `_colors.scss`
- All text must use translation keys
- All interactive elements must have `data-testid` attributes
- Follow existing code patterns and conventions
