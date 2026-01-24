# Plan 21I: Mobile Experience Optimization

## Overview

This plan details the comprehensive mobile experience optimization for ClimaSite, transforming it from a responsive desktop-first design to a truly mobile-first, thumb-friendly experience following the "Nordic Tech" design direction.

### Goals

1. **Thumb-Friendly Navigation** - Implement bottom navigation and reachable touch targets
2. **Sheet-Based Interactions** - Replace slide-out panels with bottom sheets for filters, cart, and menus
3. **Gesture Support** - Add swipe gestures for intuitive mobile navigation
4. **Performance First** - Reduce animations and optimize for mobile devices
5. **Streamlined Checkout** - Optimize the 3-step checkout flow for mobile
6. **Full-Screen Search** - Immersive search experience on mobile

### Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Mobile Lighthouse Score | ~70-80 | 90+ |
| Largest Contentful Paint (LCP) | TBD | < 2.5s |
| First Input Delay (FID) | TBD | < 100ms |
| Cumulative Layout Shift (CLS) | TBD | < 0.1 |
| Touch Target Compliance | ~85% | 100% |
| Time to Interactive | TBD | < 3.5s |
| Checkout Completion Rate (Mobile) | Baseline | +25% |

### Estimated Effort

| Phase | Duration | Priority |
|-------|----------|----------|
| Phase 1: Foundation Components | 2 days | Critical |
| Phase 2: Navigation Redesign | 2 days | Critical |
| Phase 3: Product Experience | 1.5 days | High |
| Phase 4: Checkout Optimization | 1.5 days | High |
| Phase 5: Performance & Polish | 1 day | Medium |
| **Total** | **8 days** | |

---

## Current State Analysis

### Header Component Analysis

**File:** `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts`

Current mobile implementation:
- **Three-row structure**: Top bar (hidden on mobile) + Main header + Nav bar (hidden on mobile)
- **Mobile menu**: Slide-in panel from right (300px width)
- **Mobile search**: Expandable bar below header
- **Mobile actions**: Search toggle, wishlist, cart icons (2.75rem / 44px - compliant)
- **Hamburger menu**: Triggers slide-in navigation

**Issues Identified:**
1. Mobile menu slides from right (should consider bottom sheet)
2. No bottom navigation option for quick access
3. Search is expandable row, not full-screen
4. Three-row header takes significant vertical space

### Product List Filter Analysis

**File:** `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`

Current implementation:
- **Filter sidebar**: 280px sticky sidebar on desktop
- **Mobile filters**: Slide-out panel with backdrop (max-width: 320px)
- **Filter toggle**: Button in toolbar
- **Focus trap**: Implemented for accessibility

**Issues Identified:**
1. Slide-out from left, not bottom sheet pattern
2. No gesture support to dismiss
3. Filter chips not prominent on mobile
4. No "Apply" button with result count

### Checkout Analysis

**File:** `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts`

Current implementation:
- **3-step wizard**: Shipping > Payment > Review
- **Progress indicator**: Horizontal steps (wraps on mobile)
- **Two-column layout**: Main + Sidebar (stacks on mobile)
- **Form inputs**: Standard inputs with validation

**Issues Identified:**
1. Step lines hidden on mobile but steps wrap awkwardly
2. Long scrolling forms on mobile
3. Order summary at bottom (should be collapsible)
4. No express checkout shortcut

---

## Mobile-Specific Redesigns

### 1. Navigation System

#### Decision: Hybrid Approach (Bottom Nav + Hamburger)

After analyzing the current header structure and mobile UX best practices, the recommended approach is:

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| **Bottom Nav Only** | Thumb-friendly, always visible | Limited items (4-5 max) | For primary actions |
| **Hamburger Only** | Unlimited items, familiar | Not thumb-friendly | For secondary navigation |
| **Hybrid** | Best of both worlds | Slightly more complex | **Recommended** |

**Bottom Navigation Items (5 max):**
1. Home
2. Categories (opens mega menu sheet)
3. Search (opens full-screen search)
4. Cart (shows badge)
5. Account/Menu (opens account sheet or menu)

#### Task: Bottom Navigation Bar Component

```
- [x] TASK-21I-001: Create BottomNavComponent with 5 navigation items
- [x] TASK-21I-002: Implement safe area insets for notched devices
- [x] TASK-21I-003: Add hide-on-scroll behavior (optional, debounced)
- [x] TASK-21I-004: Create bottom nav animations (item press, badge pulse)
- [ ] TASK-21I-005: Add haptic feedback service for supported devices
```

### 2. Header Simplification

#### Current State (Mobile)
```
[Logo] [Search] [Wishlist] [Cart] [Menu]
       (icons only, no text)
```

#### Target State (Mobile)
```
[Logo] [Promo Banner] [Menu Toggle]
```

The simplified header will:
- Show only logo and hamburger on mobile
- Move search, cart, wishlist to bottom nav
- Optional: Slim promo banner for promotions

```
- [x] TASK-21I-006: Refactor HeaderComponent to hide actions on mobile
- [x] TASK-21I-007: Create compact header variant (56px height on mobile)
- [ ] TASK-21I-008: Add promotional banner slot for mobile header
- [ ] TASK-21I-009: Implement scroll-aware header (shrink on scroll)
```

**Completed (January 24, 2026):**
- Simplified mobile header to single compact row (~56px)
- Removed wishlist and cart icons from mobile header (moved to bottom nav)
- Kept search icon toggle and hamburger menu in mobile header
- Enhanced mobile menu with:
  - Search bar at top
  - Wishlist link with badge count
  - Language selector
  - Theme toggle
  - User info section for authenticated users
  - Admin link for admin users

### 3. Bottom Sheet Pattern

Create a reusable bottom sheet component for consistent mobile interactions.

#### Sheet Use Cases
| Sheet Type | Trigger | Content |
|------------|---------|---------|
| Filter Sheet | Bottom nav "Categories" or filter button | Product filters |
| Cart Sheet | Bottom nav "Cart" | Mini cart with edit |
| Menu Sheet | Bottom nav "Account" | Navigation links |
| Search Sheet | Bottom nav "Search" | Full-screen search |
| Product Actions | Long press on product | Quick actions |

```
- [ ] TASK-21I-010: Create BottomSheetComponent with snap points
- [ ] TASK-21I-011: Implement sheet snap positions (peek, half, full)
- [ ] TASK-21I-012: Add drag-to-dismiss gesture with velocity detection
- [ ] TASK-21I-013: Create sheet backdrop with tap-to-dismiss
- [ ] TASK-21I-014: Implement sheet focus trap for accessibility
- [ ] TASK-21I-015: Add sheet transition animations (spring physics)
```

### 4. Filter Sheets

Replace slide-out filter panel with bottom sheet.

#### Filter Sheet Features
- **Peek state**: Shows active filter count and "Filters" title
- **Half state**: Shows filter categories with chips
- **Full state**: Full filter options with sections

```
- [ ] TASK-21I-016: Refactor ProductListComponent to use BottomSheet for filters
- [ ] TASK-21I-017: Create FilterChipsComponent for active filter display
- [ ] TASK-21I-018: Add "Show X Results" sticky button in filter sheet
- [ ] TASK-21I-019: Implement filter reset with animation
- [ ] TASK-21I-020: Add haptic feedback on filter selection
```

### 5. Product Browse Experience

#### Swipe Gestures
- **Horizontal swipe on product card**: Reveal quick actions (wishlist, compare)
- **Swipe between product images**: Gallery navigation
- **Pull down on product list**: Refresh products

```
- [ ] TASK-21I-021: Create SwipeActionsDirective for horizontal swipe reveal
- [ ] TASK-21I-022: Implement product card swipe actions (wishlist, compare)
- [ ] TASK-21I-023: Add pull-to-refresh on product list (optional)
- [ ] TASK-21I-024: Create product gallery swipe with momentum scrolling
```

#### Quick View Sheet
- Long press on product opens quick view bottom sheet
- Shows image, title, price, variants, add to cart

```
- [ ] TASK-21I-025: Create ProductQuickViewSheet component
- [ ] TASK-21I-026: Implement long press detection directive
- [ ] TASK-21I-027: Add variant selector in quick view
- [ ] TASK-21I-028: Implement add-to-cart from quick view
```

### 6. Cart/Checkout Mobile Optimization

#### Mini Cart Sheet
Bottom sheet triggered from bottom nav cart icon.

```
- [ ] TASK-21I-029: Create MiniCartSheet component
- [ ] TASK-21I-030: Add quantity edit inline in mini cart
- [ ] TASK-21I-031: Implement swipe-to-delete cart items
- [ ] TASK-21I-032: Add "Proceed to Checkout" sticky button
```

#### Checkout Flow Optimization

Current 3-step flow works but can be optimized:

| Step | Current | Optimized |
|------|---------|-----------|
| Progress | Horizontal steps | Vertical stepper with collapse |
| Shipping | Long form | Accordion sections |
| Payment | Radio + form | Large touch targets |
| Review | Full details | Collapsible summary |

```
- [ ] TASK-21I-033: Create MobileCheckoutComponent (or mode)
- [ ] TASK-21I-034: Implement collapsible order summary (sticky bottom)
- [ ] TASK-21I-035: Create accordion-style shipping form
- [ ] TASK-21I-036: Add saved address card carousel (horizontal scroll)
- [ ] TASK-21I-037: Implement payment method cards (larger touch targets)
- [ ] TASK-21I-038: Add express checkout button above fold
```

### 7. Full-Screen Search

Replace expandable search bar with immersive full-screen search.

```
- [ ] TASK-21I-039: Create FullScreenSearchComponent
- [ ] TASK-21I-040: Add recent searches persistence (localStorage)
- [ ] TASK-21I-041: Implement search suggestions with keyboard
- [ ] TASK-21I-042: Add voice search button (Web Speech API)
- [ ] TASK-21I-043: Create search result preview cards
- [ ] TASK-21I-044: Implement "Search as you type" with debounce
```

### 8. Touch Target Audit

Ensure all interactive elements meet 44x44px minimum.

```
- [ ] TASK-21I-045: Audit all buttons for 44px touch target compliance
- [ ] TASK-21I-046: Fix small touch targets (list: review stars, pagination)
- [ ] TASK-21I-047: Add touch target debugging utility (dev mode)
- [ ] TASK-21I-048: Create TouchTargetDirective for automatic sizing
```

---

## Gesture Design

### Swipe Patterns

| Gesture | Location | Action | Feedback |
|---------|----------|--------|----------|
| Swipe left on product card | Product grid | Reveal wishlist/compare | Haptic + visual |
| Swipe right on cart item | Mini cart | Delete item | Haptic + red reveal |
| Swipe down on sheet | Any bottom sheet | Dismiss sheet | Spring animation |
| Swipe left/right on gallery | Product images | Navigate images | Momentum scroll |
| Edge swipe (right edge) | Product detail | Go back | System behavior |

### Implementation Details

```typescript
// SwipeActionsDirective - Horizontal swipe reveal
@Directive({ selector: '[appSwipeActions]' })
export class SwipeActionsDirective {
  @Input() leftActions: SwipeAction[] = [];
  @Input() rightActions: SwipeAction[] = [];
  @Input() threshold = 80; // px to trigger action
  @Input() maxSwipe = 120; // max swipe distance

  // Use Hammer.js or custom touch handling
  // Implement rubber-band effect at boundaries
  // Trigger haptic feedback at threshold
}
```

```
- [ ] TASK-21I-049: Implement SwipeActionsDirective with Hammer.js
- [ ] TASK-21I-050: Add rubber-band physics to swipe boundaries
- [ ] TASK-21I-051: Create swipe action reveal animations
```

### Pull to Refresh

| Location | Behavior |
|----------|----------|
| Product list | Refresh products with current filters |
| Category page | Refresh category products |
| Order history | Refresh order list |

```
- [ ] TASK-21I-052: Create PullToRefreshDirective
- [ ] TASK-21I-053: Add pull indicator animation (spinner + arrow)
- [ ] TASK-21I-054: Implement refresh with loading state
```

### Long Press Actions

| Element | Long Press Action |
|---------|-------------------|
| Product card | Open quick view sheet |
| Cart item | Open item options sheet |
| Order item | Copy order number |
| Image | Save/share options |

```
- [ ] TASK-21I-055: Create LongPressDirective with 500ms threshold
- [ ] TASK-21I-056: Add visual feedback during long press (scale + shadow)
- [ ] TASK-21I-057: Implement context menu sheet for long press actions
```

---

## Performance Optimizations

### Animation Reduction on Mobile

Current heavy animations that should be reduced:

| Animation | Current | Mobile Optimization |
|-----------|---------|---------------------|
| RevealDirective | fade-up, scale | Disable or reduce |
| ParallaxDirective | Mouse/scroll tracking | Disable on mobile |
| FlyingCartAnimation | Arc trajectory | Simple fade to cart |
| Confetti | Full canvas animation | Reduce particle count |
| Page transitions | Slide + fade | Simple fade only |

```
- [ ] TASK-21I-058: Add isMobile detection service
- [ ] TASK-21I-059: Create reduced-motion mode for mobile
- [ ] TASK-21I-060: Disable parallax on mobile devices
- [ ] TASK-21I-061: Simplify flying cart to fade animation on mobile
- [ ] TASK-21I-062: Reduce confetti particles on mobile (50% reduction)
- [ ] TASK-21I-063: Implement simple fade page transitions on mobile
```

### Animation Service Updates

```typescript
// animation.service.ts additions
@Injectable({ providedIn: 'root' })
export class AnimationService {
  readonly isMobile = signal(this.checkMobile());
  readonly prefersReducedMotion = signal(this.checkReducedMotion());
  readonly shouldAnimate = computed(() => 
    !this.prefersReducedMotion() && !this.isMobile()
  );

  // Helper for conditional animation
  getAnimation(full: string, reduced: string): string {
    return this.shouldAnimate() ? full : reduced;
  }
}
```

```
- [ ] TASK-21I-064: Update AnimationService with mobile detection
- [ ] TASK-21I-065: Add shouldAnimate computed signal
- [ ] TASK-21I-066: Update RevealDirective to respect mobile mode
```

### Image Optimization

| Strategy | Implementation |
|----------|----------------|
| Responsive images | srcset with mobile breakpoints |
| Lazy loading | Native loading="lazy" + IntersectionObserver |
| WebP/AVIF | Format negotiation with fallback |
| Blur placeholder | Low-quality image placeholder (LQIP) |

```
- [ ] TASK-21I-067: Implement responsive image service
- [ ] TASK-21I-068: Add srcset generation for product images
- [ ] TASK-21I-069: Create ImagePlaceholderComponent with blur-up
- [ ] TASK-21I-070: Add image format detection and WebP serving
```

### Lazy Loading Strategy

| Component | Strategy |
|-----------|----------|
| Below-fold sections | Intersection Observer |
| Product images | Native lazy + placeholder |
| Reviews | Load on scroll |
| Related products | Load after main content |

```
- [ ] TASK-21I-071: Audit and optimize lazy loading triggers
- [ ] TASK-21I-072: Implement deferred loading for reviews section
- [ ] TASK-21I-073: Add skeleton placeholders for lazy components
```

### Bundle Size Considerations

Target: < 200KB initial bundle (gzipped)

| Optimization | Impact |
|--------------|--------|
| Route-based code splitting | Already implemented |
| Remove unused animations | ~15KB savings |
| Tree-shake icon library | ~10KB savings |
| Lazy load Stripe | ~30KB deferred |

```
- [ ] TASK-21I-074: Analyze bundle with source-map-explorer
- [ ] TASK-21I-075: Remove unused animation utilities
- [ ] TASK-21I-076: Implement dynamic Stripe loading
- [ ] TASK-21I-077: Add bundle size budget to build
```

---

## Responsive Breakpoints

### Breakpoint System

| Breakpoint | Width | Target Devices |
|------------|-------|----------------|
| `xs` | < 375px | Small phones (iPhone SE) |
| `sm` | 375px - 480px | Standard phones |
| `md` | 480px - 768px | Large phones, small tablets |
| `lg` | 768px - 1024px | Tablets |
| `xl` | 1024px - 1280px | Small laptops |
| `2xl` | > 1280px | Desktops |

### SCSS Mixins

```scss
// _breakpoints.scss
$breakpoints: (
  'xs': 375px,
  'sm': 480px,
  'md': 768px,
  'lg': 1024px,
  'xl': 1280px
);

@mixin mobile-only {
  @media (max-width: #{map-get($breakpoints, 'md') - 1}) {
    @content;
  }
}

@mixin tablet-up {
  @media (min-width: #{map-get($breakpoints, 'md')}) {
    @content;
  }
}

@mixin desktop-up {
  @media (min-width: #{map-get($breakpoints, 'lg')}) {
    @content;
  }
}
```

### Implementation Approach

```
- [ ] TASK-21I-078: Create breakpoint SCSS mixins file
- [ ] TASK-21I-079: Add BreakpointService for JS breakpoint detection
- [ ] TASK-21I-080: Update global styles with consistent breakpoints
- [ ] TASK-21I-081: Create responsive utility classes
```

### Specific Breakpoint Changes

#### 375px (Small Mobile)
- Single column everything
- Smaller typography (--text-base: 14px)
- Compact header (36px height)
- Bottom nav with 4 items (hide one)

#### 480px (Large Mobile)
- Standard mobile layout
- Full bottom nav (5 items)
- Product grid 2 columns

#### 768px (Tablet)
- Hide bottom nav
- Show desktop header
- Product grid 3 columns
- Side-by-side forms

```
- [ ] TASK-21I-082: Implement 375px specific styles
- [ ] TASK-21I-083: Implement 480px specific styles
- [ ] TASK-21I-084: Implement 768px tablet transition styles
```

---

## PWA Considerations

While full PWA is out of scope for this plan, prepare the foundation:

### Immediate Optimizations
- App-like full-screen experience
- Home screen installability
- Offline product browsing (cached)

```
- [ ] TASK-21I-085: Add Web App Manifest with mobile icons
- [ ] TASK-21I-086: Configure viewport for standalone mode
- [ ] TASK-21I-087: Add apple-touch-icon and splash screens
- [ ] TASK-21I-088: Implement service worker for static asset caching
```

### Future PWA Features (Out of Scope)
- Push notifications
- Background sync for cart
- Offline checkout queue

---

## Dependencies

### Dependencies on Other Plans

| Plan | Dependency | Type |
|------|------------|------|
| 21C (Navigation) | Header redesign foundation | Must complete first |
| 21D (Cart/Checkout) | Mini cart component | Coordinate together |
| 21E (Components) | Button, Card components | Use new components |
| 21F (Animation) | Reduced motion guidelines | Apply guidelines |

### External Dependencies

| Package | Purpose | Size Impact |
|---------|---------|-------------|
| Hammer.js | Gesture recognition | +7KB |
| (Optional) GSAP | Physics animations | +25KB |
| (None new) | Use native APIs when possible | - |

```
- [ ] TASK-21I-089: Evaluate Hammer.js vs native touch events
- [ ] TASK-21I-090: Decide on animation library (GSAP vs CSS)
```

---

## Testing Checklist

### Device Testing Matrix

| Device | OS | Screen | Priority |
|--------|-----|--------|----------|
| iPhone SE (2020) | iOS 15+ | 375x667 | High |
| iPhone 13/14 | iOS 16+ | 390x844 | Critical |
| iPhone 14 Pro Max | iOS 16+ | 430x932 | High |
| Samsung Galaxy S21 | Android 12+ | 360x800 | Critical |
| Samsung Galaxy S23 Ultra | Android 13+ | 384x824 | High |
| Google Pixel 7 | Android 13+ | 412x915 | High |
| iPad Mini | iPadOS 16+ | 768x1024 | Medium |
| iPad Pro 11" | iPadOS 16+ | 834x1194 | Medium |

### Testing Scenarios

```
- [ ] TASK-21I-091: Test bottom navigation on all target devices
- [ ] TASK-21I-092: Test bottom sheet gestures (drag, snap, dismiss)
- [ ] TASK-21I-093: Test filter sheet with all filter combinations
- [ ] TASK-21I-094: Test checkout flow on small screens (375px)
- [ ] TASK-21I-095: Test full-screen search with keyboard
- [ ] TASK-21I-096: Test swipe gestures on product cards
- [ ] TASK-21I-097: Test with VoiceOver (iOS) and TalkBack (Android)
- [ ] TASK-21I-098: Test with reduced motion preference enabled
- [ ] TASK-21I-099: Test landscape orientation (tablet)
- [ ] TASK-21I-100: Performance testing with DevTools throttling
```

### Automated Tests

```
- [ ] TASK-21I-101: Add Playwright mobile viewport tests
- [ ] TASK-21I-102: Create gesture simulation tests
- [ ] TASK-21I-103: Add visual regression tests for mobile
- [ ] TASK-21I-104: Create performance budget tests
```

---

## Task Summary

### All Tasks by Phase

#### Phase 1: Foundation Components (TASK-21I-010 to TASK-21I-015, TASK-21I-078 to TASK-21I-084)
- [ ] TASK-21I-010: Create BottomSheetComponent with snap points
- [ ] TASK-21I-011: Implement sheet snap positions (peek, half, full)
- [ ] TASK-21I-012: Add drag-to-dismiss gesture with velocity detection
- [ ] TASK-21I-013: Create sheet backdrop with tap-to-dismiss
- [ ] TASK-21I-014: Implement sheet focus trap for accessibility
- [ ] TASK-21I-015: Add sheet transition animations (spring physics)
- [ ] TASK-21I-078: Create breakpoint SCSS mixins file
- [ ] TASK-21I-079: Add BreakpointService for JS breakpoint detection
- [ ] TASK-21I-080: Update global styles with consistent breakpoints
- [ ] TASK-21I-081: Create responsive utility classes
- [ ] TASK-21I-082: Implement 375px specific styles
- [ ] TASK-21I-083: Implement 480px specific styles
- [ ] TASK-21I-084: Implement 768px tablet transition styles

#### Phase 2: Navigation Redesign (TASK-21I-001 to TASK-21I-009, TASK-21I-039 to TASK-21I-044)
- [x] TASK-21I-001: Create BottomNavComponent with 5 navigation items
- [x] TASK-21I-002: Implement safe area insets for notched devices
- [x] TASK-21I-003: Add hide-on-scroll behavior (optional, debounced)
- [x] TASK-21I-004: Create bottom nav animations (item press, badge pulse)
- [ ] TASK-21I-005: Add haptic feedback service for supported devices
- [ ] TASK-21I-006: Refactor HeaderComponent to hide actions on mobile
- [ ] TASK-21I-007: Create compact header variant (40px height on mobile)
- [ ] TASK-21I-008: Add promotional banner slot for mobile header
- [ ] TASK-21I-009: Implement scroll-aware header (shrink on scroll)
- [ ] TASK-21I-039: Create FullScreenSearchComponent
- [ ] TASK-21I-040: Add recent searches persistence (localStorage)
- [ ] TASK-21I-041: Implement search suggestions with keyboard
- [ ] TASK-21I-042: Add voice search button (Web Speech API)
- [ ] TASK-21I-043: Create search result preview cards
- [ ] TASK-21I-044: Implement "Search as you type" with debounce

#### Phase 3: Product Experience (TASK-21I-016 to TASK-21I-028, TASK-21I-045 to TASK-21I-057)
- [ ] TASK-21I-016: Refactor ProductListComponent to use BottomSheet for filters
- [ ] TASK-21I-017: Create FilterChipsComponent for active filter display
- [ ] TASK-21I-018: Add "Show X Results" sticky button in filter sheet
- [ ] TASK-21I-019: Implement filter reset with animation
- [ ] TASK-21I-020: Add haptic feedback on filter selection
- [ ] TASK-21I-021: Create SwipeActionsDirective for horizontal swipe reveal
- [ ] TASK-21I-022: Implement product card swipe actions (wishlist, compare)
- [ ] TASK-21I-023: Add pull-to-refresh on product list (optional)
- [ ] TASK-21I-024: Create product gallery swipe with momentum scrolling
- [ ] TASK-21I-025: Create ProductQuickViewSheet component
- [ ] TASK-21I-026: Implement long press detection directive
- [ ] TASK-21I-027: Add variant selector in quick view
- [ ] TASK-21I-028: Implement add-to-cart from quick view
- [ ] TASK-21I-045: Audit all buttons for 44px touch target compliance
- [ ] TASK-21I-046: Fix small touch targets (list: review stars, pagination)
- [ ] TASK-21I-047: Add touch target debugging utility (dev mode)
- [ ] TASK-21I-048: Create TouchTargetDirective for automatic sizing
- [ ] TASK-21I-049: Implement SwipeActionsDirective with Hammer.js
- [ ] TASK-21I-050: Add rubber-band physics to swipe boundaries
- [ ] TASK-21I-051: Create swipe action reveal animations
- [ ] TASK-21I-052: Create PullToRefreshDirective
- [ ] TASK-21I-053: Add pull indicator animation (spinner + arrow)
- [ ] TASK-21I-054: Implement refresh with loading state
- [ ] TASK-21I-055: Create LongPressDirective with 500ms threshold
- [ ] TASK-21I-056: Add visual feedback during long press (scale + shadow)
- [ ] TASK-21I-057: Implement context menu sheet for long press actions

#### Phase 4: Checkout Optimization (TASK-21I-029 to TASK-21I-038)
- [ ] TASK-21I-029: Create MiniCartSheet component
- [ ] TASK-21I-030: Add quantity edit inline in mini cart
- [ ] TASK-21I-031: Implement swipe-to-delete cart items
- [ ] TASK-21I-032: Add "Proceed to Checkout" sticky button
- [ ] TASK-21I-033: Create MobileCheckoutComponent (or mode)
- [ ] TASK-21I-034: Implement collapsible order summary (sticky bottom)
- [ ] TASK-21I-035: Create accordion-style shipping form
- [ ] TASK-21I-036: Add saved address card carousel (horizontal scroll)
- [ ] TASK-21I-037: Implement payment method cards (larger touch targets)
- [ ] TASK-21I-038: Add express checkout button above fold

#### Phase 5: Performance & Polish (TASK-21I-058 to TASK-21I-090)
- [ ] TASK-21I-058: Add isMobile detection service
- [ ] TASK-21I-059: Create reduced-motion mode for mobile
- [ ] TASK-21I-060: Disable parallax on mobile devices
- [ ] TASK-21I-061: Simplify flying cart to fade animation on mobile
- [ ] TASK-21I-062: Reduce confetti particles on mobile (50% reduction)
- [ ] TASK-21I-063: Implement simple fade page transitions on mobile
- [ ] TASK-21I-064: Update AnimationService with mobile detection
- [ ] TASK-21I-065: Add shouldAnimate computed signal
- [ ] TASK-21I-066: Update RevealDirective to respect mobile mode
- [ ] TASK-21I-067: Implement responsive image service
- [ ] TASK-21I-068: Add srcset generation for product images
- [ ] TASK-21I-069: Create ImagePlaceholderComponent with blur-up
- [ ] TASK-21I-070: Add image format detection and WebP serving
- [ ] TASK-21I-071: Audit and optimize lazy loading triggers
- [ ] TASK-21I-072: Implement deferred loading for reviews section
- [ ] TASK-21I-073: Add skeleton placeholders for lazy components
- [ ] TASK-21I-074: Analyze bundle with source-map-explorer
- [ ] TASK-21I-075: Remove unused animation utilities
- [ ] TASK-21I-076: Implement dynamic Stripe loading
- [ ] TASK-21I-077: Add bundle size budget to build
- [ ] TASK-21I-085: Add Web App Manifest with mobile icons
- [ ] TASK-21I-086: Configure viewport for standalone mode
- [ ] TASK-21I-087: Add apple-touch-icon and splash screens
- [ ] TASK-21I-088: Implement service worker for static asset caching
- [ ] TASK-21I-089: Evaluate Hammer.js vs native touch events
- [ ] TASK-21I-090: Decide on animation library (GSAP vs CSS)

#### Testing Tasks (TASK-21I-091 to TASK-21I-104)
- [ ] TASK-21I-091: Test bottom navigation on all target devices
- [ ] TASK-21I-092: Test bottom sheet gestures (drag, snap, dismiss)
- [ ] TASK-21I-093: Test filter sheet with all filter combinations
- [ ] TASK-21I-094: Test checkout flow on small screens (375px)
- [ ] TASK-21I-095: Test full-screen search with keyboard
- [ ] TASK-21I-096: Test swipe gestures on product cards
- [ ] TASK-21I-097: Test with VoiceOver (iOS) and TalkBack (Android)
- [ ] TASK-21I-098: Test with reduced motion preference enabled
- [ ] TASK-21I-099: Test landscape orientation (tablet)
- [ ] TASK-21I-100: Performance testing with DevTools throttling
- [ ] TASK-21I-101: Add Playwright mobile viewport tests
- [ ] TASK-21I-102: Create gesture simulation tests
- [ ] TASK-21I-103: Add visual regression tests for mobile
- [ ] TASK-21I-104: Create performance budget tests

---

## File Changes Summary

### New Files
| File | Purpose |
|------|---------|
| `shared/components/bottom-nav/bottom-nav.component.ts` | Bottom navigation bar |
| `shared/components/bottom-sheet/bottom-sheet.component.ts` | Reusable bottom sheet |
| `shared/components/full-screen-search/full-screen-search.component.ts` | Mobile search |
| `shared/components/mini-cart-sheet/mini-cart-sheet.component.ts` | Cart bottom sheet |
| `shared/components/filter-chips/filter-chips.component.ts` | Active filter display |
| `shared/components/product-quick-view/product-quick-view.component.ts` | Quick view sheet |
| `shared/directives/swipe-actions.directive.ts` | Swipe gesture handling |
| `shared/directives/long-press.directive.ts` | Long press detection |
| `shared/directives/pull-to-refresh.directive.ts` | Pull to refresh |
| `shared/directives/touch-target.directive.ts` | Touch target sizing |
| `core/services/breakpoint.service.ts` | Responsive breakpoint detection |
| `core/services/haptic.service.ts` | Haptic feedback |
| `styles/_breakpoints.scss` | Breakpoint mixins |
| `styles/_mobile.scss` | Mobile-specific utilities |

### Modified Files
| File | Changes |
|------|---------|
| `core/layout/header/header.component.ts` | Simplify for mobile |
| `core/services/animation.service.ts` | Add mobile detection |
| `features/products/product-list/product-list.component.ts` | Use bottom sheet for filters |
| `features/products/product-card/product-card.component.ts` | Add swipe actions |
| `features/checkout/checkout.component.ts` | Mobile optimizations |
| `shared/directives/reveal.directive.ts` | Respect mobile mode |
| `shared/directives/parallax.directive.ts` | Disable on mobile |
| `core/services/flying-cart.service.ts` | Simplify on mobile |
| `core/services/confetti.service.ts` | Reduce particles on mobile |
| `app.component.ts` | Include bottom nav |
| `styles.scss` | Import new mobile styles |

---

## Definition of Done

This plan is complete when:

- [ ] Bottom navigation component implemented and integrated
- [ ] Bottom sheet component with all snap points working
- [ ] Filter sheet replaces slide-out on mobile
- [ ] Full-screen search implemented
- [ ] Mini cart sheet functional
- [ ] All touch targets meet 44px minimum
- [ ] Swipe gestures working on product cards
- [ ] Checkout optimized for mobile screens
- [ ] Animations reduced on mobile devices
- [ ] All Playwright mobile viewport tests passing
- [ ] Performance metrics meet targets (LCP < 2.5s, CLS < 0.1)
- [ ] Manual testing complete on device matrix
- [ ] Accessibility tested with VoiceOver and TalkBack
- [ ] Works in both light and dark themes
- [ ] Works in all languages (EN, BG, DE)

---

*Document created: January 24, 2026*
*Status: Ready for implementation*
*Estimated completion: 8 days*
