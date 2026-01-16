# Implementation Plan: Bug Fixes and Enhancements (Plan 19)

## Overview

This plan addresses 3 issues identified in the current build:
1. **MENU-001**: Mega menu dropdown doesn't always close when mouse leaves
2. **WISH-001**: Wishlist add functionality doesn't work from products page
3. **HOME-001**: Home page design/UX needs improvement

---

## Issue 1: Mega Menu Hover Fix (MENU-001)

### Problem Analysis
- Timer-based closing (200ms) has race conditions
- No hover bridge between trigger and dropdown
- Animation timing conflicts with close timeout
- Mouse can slip between trigger and menu

### Solution

**1.1 Wrap trigger and dropdown in single container**
- Create a parent element that encompasses both trigger and dropdown
- Use CSS `:hover` on the parent for reliable hover detection

**1.2 Add hover bridge with CSS**
- Add invisible bridge element connecting trigger to dropdown
- Prevents "dead zone" when mouse moves between elements

**1.3 Simplify event handling**
- Remove complex timer logic
- Use single `mouseenter`/`mouseleave` on parent container

### Files to Modify
- `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts`
- `src/ClimaSite.Web/src/app/core/layout/header/header.component.scss`

---

## Issue 2: Wishlist API Fix (WISH-001)

### Problem Analysis
- Frontend sends: `POST /api/wishlist/items` with body `{ productId }`
- Backend expects: `POST /api/wishlist/items/{productId}` (route parameter)
- Endpoint mismatch causes 404/400 errors

### Solution

**2.1 Fix frontend API call**
- Update `WishlistService.syncAddToApi()` to use route parameter
- Change from body-based to URL-based parameter

**2.2 Add error feedback**
- Show toast notification on add/remove
- Handle 401 for guests (prompt login or use localStorage)

### Files to Modify
- `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts`

---

## Issue 3: Home Page Redesign (HOME-001)

### Problem Analysis
- Uses emoji icons instead of professional SVG icons
- Hero section lacks imagery and visual impact
- Newsletter form has no validation or feedback
- Inconsistent spacing and typography
- Overall rating: 5.5/10

### Solution

**3.1 Replace emoji with SVG icons**
- Use Heroicons or similar professional icon set
- Create inline SVG components for key icons

**3.2 Improve hero section**
- Add background image with overlay gradient
- Improve typography hierarchy
- Add subtle animation on load

**3.3 Enhance feature cards**
- Add hover effects and shadows
- Improve icon presentation
- Better spacing and alignment

**3.4 Fix newsletter section**
- Add form validation
- Show loading state and success/error feedback
- Improve styling

**3.5 Add professional polish**
- Consistent section spacing
- Better color contrast
- Smooth scroll animations

### Files to Modify
- `src/ClimaSite.Web/src/app/features/home/home.component.ts`
- `src/ClimaSite.Web/src/app/features/home/home.component.scss`
- Translation files for new text

---

## Implementation Order

1. **WISH-001** (15 min) - Simple API fix
2. **MENU-001** (30 min) - Hover behavior fix
3. **HOME-001** (1-2 hours) - Design improvements

---

## Verification

After implementation:
1. Test mega menu hover in Chrome, Firefox, Safari
2. Test wishlist add from product list and product detail
3. Verify home page in light/dark themes
4. Verify home page in EN/BG/DE languages
5. Run E2E tests to ensure no regressions
