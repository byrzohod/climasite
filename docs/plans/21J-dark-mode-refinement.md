# ClimaSite UI Redesign - Plan 21J: Dark Mode Refinement

## Overview

### Goals

This plan addresses the refinement of ClimaSite's dark mode implementation to achieve:

1. **WCAG AAA Contrast Compliance** - Ensure all text meets 7:1 contrast ratio where possible, 4.5:1 minimum
2. **Zero Hardcoded Colors** - Replace all remaining hex values with CSS custom properties
3. **Theme-Specific Adjustments** - Optimize glass effects, shadows, and accent colors per theme
4. **Image Handling** - Implement subtle brightness/contrast adjustments for dark mode
5. **Consistent Visual Experience** - Ensure all components look polished in both themes

### Design Direction: "Nordic Tech"

The dark mode should embody:
- **Refined**: Sophisticated, not just "inverted light mode"
- **Accessible**: Exceeds WCAG AA, aims for AAA where feasible
- **Consistent**: Every component works seamlessly in both themes
- **Subtle**: Thoughtful adjustments, not dramatic changes

### Success Metrics

| Metric | Current State | Target |
|--------|---------------|--------|
| WCAG AA Compliance | ~90% | 100% |
| WCAG AAA Compliance | ~60% | 90%+ |
| Hardcoded Colors | 50+ instances | 0 |
| Glass Effect Issues | Some transparency issues | None |
| Image Dark Mode Support | None | Full |
| Theme Transition Smoothness | Good | Excellent |

### Estimated Effort

| Phase | Duration | Priority |
|-------|----------|----------|
| Color Audit & Fix | 2-3 hours | Critical |
| Contrast Verification | 1-2 hours | Critical |
| Glass/Shadow Adjustments | 1-2 hours | High |
| Image Handling | 1-2 hours | Medium |
| Testing & Polish | 1 hour | High |
| **Total** | **6-10 hours (1 day)** | |

---

## 1. Color Audit

### 1.1 Files with Hardcoded Colors (Must Fix)

The following files contain hardcoded hex colors that need to be replaced with CSS custom properties:

#### Critical Priority (User-facing components)

| File | Line(s) | Hardcoded Color | Replacement Variable |
|------|---------|-----------------|---------------------|
| `features/promotions/promotion-detail/promotion-detail.component.ts` | 239, 418 | `#22c55e` | `var(--color-success)` |
| `features/brands/brand-detail/brand-detail.component.ts` | 410 | `#22c55e` | `var(--color-success)` |
| `features/promotions/promotions-list/promotions-list.component.ts` | 179 | `#f59e0b` | `var(--color-warning)` |
| `features/account/order-details/order-details.component.ts` | 460-468, 1011 | Multiple | Various semantic colors |
| `features/account/orders/orders.component.ts` | 935 | `#e5e7eb`, `#374151` | `var(--color-bg-tertiary)`, `var(--color-text-secondary)` |
| `features/cart/cart.component.ts` | 440-441 | `#dcfce7`, `#22c55e` | `var(--color-success-light)`, `var(--color-success)` |
| `features/checkout/checkout.component.ts` | 415, 809, 897 | `#22c55e`, `#fee2e2` | `var(--color-success)`, `var(--color-error-bg)` |
| `features/contact/contact.component.ts` | 269-278 | `#d4edda`, `#155724`, `#f8d7da`, `#721c24` | Semantic color vars |
| `features/products/product-list/product-list.component.ts` | 973 | `#fee2e2` | `var(--color-error-bg)` |

#### High Priority (Shared components)

| File | Line(s) | Hardcoded Color | Replacement Variable |
|------|---------|-----------------|---------------------|
| `shared/components/product-reviews/product-reviews.component.ts` | 306, 338, 496, 595 | `#ffc107`, `#22c55e` | `var(--color-rating-star)` (new), `var(--color-success)` |
| `shared/components/testimonials/testimonials.component.ts` | 156 | `#ffc107` | `var(--color-rating-star)` |
| `shared/components/final-cta/final-cta.component.ts` | 227, 235, 242 | Gradient colors | Use CSS gradient variables |
| `shared/components/frequently-bought/frequently-bought.component.ts` | 310 | `#22c55e` | `var(--color-success)` |
| `shared/components/similar-products/similar-products.component.ts` | 312 | `#22c55e` | `var(--color-success)` |
| `shared/components/product-consumables/product-consumables.component.ts` | 313 | `#22c55e` | `var(--color-success)` |
| `shared/components/financing-calculator/financing-calculator.component.ts` | 104 | `#22c55e`, `#16a34a` | Gradient variable |
| `shared/components/share-product/share-product.component.ts` | 157, 203, 209, 215 | Social brand colors | Keep as-is (brand colors) |
| `shared/components/how-it-works/how-it-works.component.ts` | 386, 397, 408, 419 | Step colors | `var(--color-step-1)` through `var(--color-step-4)` (new) |
| `shared/components/glass-card/glass-card.component.ts` | 148-152 | `#fff` | `var(--color-white)` |

#### Medium Priority (Services & Models)

| File | Line(s) | Hardcoded Color | Notes |
|------|---------|-----------------|-------|
| `core/services/confetti.service.ts` | 153-161 | Fallback colors | Acceptable as CSS var fallbacks |
| `core/services/flying-cart.service.ts` | 102 | `#fff` | `var(--color-bg-card)` fallback - OK |
| `core/services/payment.service.ts` | 84-90 | `#1a1a1a`, `#6b7280`, `#dc2626` | Use CSS vars with fallbacks |
| `core/models/order.model.ts` | 130-137 | Status colors | Create `--color-status-*` variables |

#### Special Cases (Keep as-is)

| File | Line(s) | Reason |
|------|---------|--------|
| `shared/components/energy-rating/energy-rating.component.ts` | 131-140 | EU Energy Label official colors - must remain static |
| `shared/components/share-product/share-product.component.ts` | 203-215 | Brand colors (Facebook, Twitter, WhatsApp) |
| `styles.scss` (print styles) | 1266-1391 | Print-specific colors are acceptable |

### 1.2 Task List - Color Audit

```
- [ ] TASK-21J-001: Create new CSS variables for missing semantic colors (rating stars, step indicators, status badges)
- [ ] TASK-21J-002: Fix hardcoded colors in promotion-detail.component.ts
- [ ] TASK-21J-003: Fix hardcoded colors in brand-detail.component.ts
- [ ] TASK-21J-004: Fix hardcoded colors in promotions-list.component.ts
- [ ] TASK-21J-005: Fix hardcoded colors in order-details.component.ts
- [ ] TASK-21J-006: Fix hardcoded colors in orders.component.ts
- [ ] TASK-21J-007: Fix hardcoded colors in cart.component.ts
- [ ] TASK-21J-008: Fix hardcoded colors in checkout.component.ts
- [ ] TASK-21J-009: Fix hardcoded colors in contact.component.ts
- [ ] TASK-21J-010: Fix hardcoded colors in product-list.component.ts
- [ ] TASK-21J-011: Fix hardcoded colors in product-reviews.component.ts (star ratings)
- [ ] TASK-21J-012: Fix hardcoded colors in testimonials.component.ts
- [ ] TASK-21J-013: Fix hardcoded colors in final-cta.component.ts
- [ ] TASK-21J-014: Fix hardcoded colors in frequently-bought.component.ts
- [ ] TASK-21J-015: Fix hardcoded colors in similar-products.component.ts
- [ ] TASK-21J-016: Fix hardcoded colors in product-consumables.component.ts
- [ ] TASK-21J-017: Fix hardcoded colors in financing-calculator.component.ts
- [ ] TASK-21J-018: Fix hardcoded colors in how-it-works.component.ts
- [ ] TASK-21J-019: Fix hardcoded colors in glass-card.component.ts
- [ ] TASK-21J-020: Update order.model.ts status colors to use CSS variables
- [ ] TASK-21J-021: Audit payment.service.ts Stripe styling
- [ ] TASK-21J-022: Run final grep to confirm zero hardcoded colors remain
```

---

## 2. Contrast Ratio Verification

### 2.1 WCAG Requirements

| Level | Normal Text (< 18pt) | Large Text (>= 18pt or 14pt bold) |
|-------|---------------------|-----------------------------------|
| AA | 4.5:1 minimum | 3:1 minimum |
| AAA | 7:1 minimum | 4.5:1 minimum |

### 2.2 Current Color Combinations to Verify

#### Light Theme

| Foreground | Background | Current Ratio | Target | Status |
|------------|------------|---------------|--------|--------|
| `--color-text-primary` (#0f172a) | `--color-bg-primary` (#ffffff) | 16.1:1 | AAA | PASS |
| `--color-text-secondary` (#475569) | `--color-bg-primary` (#ffffff) | 7.0:1 | AAA | PASS |
| `--color-text-tertiary` (#64748b) | `--color-bg-primary` (#ffffff) | 4.6:1 | AA | PASS |
| `--color-text-muted` (#64748b) | `--color-bg-secondary` (#f8fafc) | 4.4:1 | AA | CHECK |
| `--color-text-placeholder` (#94a3b8) | `--color-bg-input` (#ffffff) | 3.0:1 | - | FAIL |
| `--color-primary` (#0ea5e9) | `--color-bg-primary` (#ffffff) | 3.1:1 | - | FAIL (for text) |
| `--color-primary-600` (#0284c7) | `--color-bg-primary` (#ffffff) | 4.5:1 | AA | PASS |

#### Dark Theme

| Foreground | Background | Current Ratio | Target | Status |
|------------|------------|---------------|--------|--------|
| `--color-text-primary` (#f8fafc) | `--color-bg-primary` (#0f172a) | 15.1:1 | AAA | PASS |
| `--color-text-secondary` (#cbd5e1) | `--color-bg-primary` (#0f172a) | 9.4:1 | AAA | PASS |
| `--color-text-tertiary` (#94a3b8) | `--color-bg-primary` (#0f172a) | 5.5:1 | AA | PASS |
| `--color-text-muted` (#64748b) | `--color-bg-card` (#1e293b) | 3.8:1 | - | NEEDS FIX |
| `--color-primary` (#38bdf8) | `--color-bg-primary` (#0f172a) | 8.2:1 | AAA | PASS |
| `--color-border-primary` (#334155) | `--color-bg-card` (#1e293b) | 1.5:1 | - | CHECK |

### 2.3 Task List - Contrast Verification

```
- [ ] TASK-21J-023: Adjust --color-text-placeholder for AA compliance (both themes)
- [ ] TASK-21J-024: Adjust --color-text-muted in dark theme for AA compliance
- [ ] TASK-21J-025: Ensure primary color links use darker variant for text
- [ ] TASK-21J-026: Verify all form input text meets 4.5:1 contrast
- [ ] TASK-21J-027: Verify all button text meets 4.5:1 contrast
- [ ] TASK-21J-028: Verify disabled state text meets 3:1 minimum
- [ ] TASK-21J-029: Test with browser contrast checker tools
- [ ] TASK-21J-030: Document all contrast ratios in _colors.scss comments
```

---

## 3. Glass Effect & Opacity Adjustments

### 3.1 Current Glass Variables

```scss
// Light Theme
--glass-bg: rgba(255, 255, 255, 0.8);
--glass-bg-heavy: rgba(255, 255, 255, 0.9);
--glass-border: rgba(255, 255, 255, 0.3);
--glass-shadow: rgba(15, 23, 42, 0.1);

// Dark Theme
--glass-bg: rgba(30, 41, 59, 0.8);
--glass-bg-heavy: rgba(30, 41, 59, 0.9);
--glass-border: rgba(255, 255, 255, 0.1);
--glass-shadow: rgba(0, 0, 0, 0.3);
```

### 3.2 Recommended Adjustments

| Variable | Light Theme | Dark Theme (Current) | Dark Theme (Proposed) | Reason |
|----------|-------------|---------------------|----------------------|--------|
| `--glass-bg` | `rgba(255,255,255,0.8)` | `rgba(30,41,59,0.8)` | `rgba(30,41,59,0.85)` | Slightly more opaque for readability |
| `--glass-bg-heavy` | `rgba(255,255,255,0.9)` | `rgba(30,41,59,0.9)` | `rgba(30,41,59,0.92)` | Better text contrast |
| `--glass-border` | `rgba(255,255,255,0.3)` | `rgba(255,255,255,0.1)` | `rgba(255,255,255,0.12)` | Slightly more visible border |
| `--glass-shadow` | `rgba(15,23,42,0.1)` | `rgba(0,0,0,0.3)` | `rgba(0,0,0,0.4)` | Deeper shadow for depth |

### 3.3 New Glass Variables Needed

```scss
// Add to both themes
--glass-bg-subtle: rgba(var(--glass-base), 0.5);      // For overlays
--glass-backdrop-blur: 12px;                           // Standardize blur
--glass-backdrop-blur-heavy: 20px;                     // For modals
```

### 3.4 Task List - Glass Effects

```
- [ ] TASK-21J-031: Increase dark mode glass opacity from 0.8 to 0.85
- [ ] TASK-21J-032: Add --glass-bg-subtle variable for both themes
- [ ] TASK-21J-033: Add --glass-backdrop-blur standardization
- [ ] TASK-21J-034: Review glass-card.component.ts for hardcoded values
- [ ] TASK-21J-035: Test glass effects on various background images
- [ ] TASK-21J-036: Verify glass readability in both themes
```

---

## 4. Shadow Adjustments

### 4.1 Current Shadow System

```scss
// Light Theme
--shadow-color: rgba(15, 23, 42, 0.08);
--shadow-color-lg: rgba(15, 23, 42, 0.12);
--shadow-color-xl: rgba(15, 23, 42, 0.16);

// Dark Theme
--shadow-color: rgba(0, 0, 0, 0.25);
--shadow-color-lg: rgba(0, 0, 0, 0.35);
--shadow-color-xl: rgba(0, 0, 0, 0.45);
```

### 4.2 Recommended Shadow Scale (New)

Add standardized shadow utilities:

```scss
:root {
  // Shadow Elevations
  --shadow-sm: 0 1px 2px 0 var(--shadow-color);
  --shadow-md: 0 4px 6px -1px var(--shadow-color), 0 2px 4px -2px var(--shadow-color);
  --shadow-lg: 0 10px 15px -3px var(--shadow-color-lg), 0 4px 6px -4px var(--shadow-color);
  --shadow-xl: 0 20px 25px -5px var(--shadow-color-xl), 0 8px 10px -6px var(--shadow-color-lg);
  --shadow-2xl: 0 25px 50px -12px var(--shadow-color-xl);
  
  // Inset Shadows
  --shadow-inner: inset 0 2px 4px 0 var(--shadow-color);
  
  // Focus Rings
  --ring-shadow: 0 0 0 3px var(--color-ring);
}

[data-theme="dark"],
.dark {
  // Slightly deeper shadows for dark mode depth perception
  --shadow-md: 0 4px 8px -1px var(--shadow-color), 0 2px 6px -2px var(--shadow-color);
  --shadow-lg: 0 12px 20px -3px var(--shadow-color-lg), 0 6px 8px -4px var(--shadow-color);
}
```

### 4.3 Task List - Shadows

```
- [ ] TASK-21J-037: Add standardized shadow scale to _colors.scss
- [ ] TASK-21J-038: Create dark mode specific shadow adjustments
- [ ] TASK-21J-039: Replace inline shadow definitions with variables
- [ ] TASK-21J-040: Add focus ring shadow variable
- [ ] TASK-21J-041: Test shadow visibility on dark backgrounds
```

---

## 5. Border Visibility

### 5.1 Current Border Variables

```scss
// Light Theme
--color-border-primary: #e2e8f0;   // Gray-200
--color-border-secondary: #cbd5e1; // Gray-300

// Dark Theme
--color-border-primary: #334155;   // Gray-700
--color-border-secondary: #475569; // Gray-600
```

### 5.2 Issues to Address

| Issue | Location | Solution |
|-------|----------|----------|
| Low contrast borders | Cards on dark bg | Increase border opacity or use lighter shade |
| Invisible dividers | Tables in dark mode | Use `--color-border-secondary` |
| Focus borders | Form inputs | Ensure 3:1 contrast with background |

### 5.3 Recommended Adjustments

```scss
[data-theme="dark"],
.dark {
  --color-border-primary: #{$color-gray-600};   // Was gray-700
  --color-border-secondary: #{$color-gray-500}; // Was gray-600
  --color-border-subtle: #{$color-gray-700};    // New - for very subtle borders
}
```

### 5.4 Task List - Borders

```
- [ ] TASK-21J-042: Lighten dark mode border-primary for better visibility
- [ ] TASK-21J-043: Add --color-border-subtle variable
- [ ] TASK-21J-044: Audit table borders in dark mode
- [ ] TASK-21J-045: Verify input focus borders meet 3:1 contrast
```

---

## 6. Image Handling for Dark Mode

### 6.1 Strategy

Images can appear harsh against dark backgrounds. Implement subtle adjustments:

#### 6.1.1 CSS Filter Approach

```scss
// New utility class or global style
[data-theme="dark"],
.dark {
  // Product images - subtle brightness reduction
  .product-image,
  .gallery-image,
  img[data-dark-mode="dim"] {
    filter: brightness(0.92) contrast(1.05);
    transition: filter var(--theme-transition);
  }
  
  // Hero/banner images - more noticeable
  .hero-image,
  .banner-image,
  img[data-dark-mode="dim-more"] {
    filter: brightness(0.85) contrast(1.1);
  }
  
  // Icons and logos - invert if needed
  img[data-dark-mode="invert"] {
    filter: invert(1) hue-rotate(180deg);
  }
  
  // No adjustment
  img[data-dark-mode="none"],
  .brand-logo {
    filter: none;
  }
}
```

#### 6.1.2 CSS Variable Approach

```scss
:root {
  --image-brightness: 1;
  --image-contrast: 1;
}

[data-theme="dark"],
.dark {
  --image-brightness: 0.92;
  --image-contrast: 1.05;
}

// Apply to images
.product-image {
  filter: brightness(var(--image-brightness)) contrast(var(--image-contrast));
}
```

### 6.2 Logo Variants

| Logo | Light Theme | Dark Theme |
|------|-------------|------------|
| Main Logo | Standard | May need lighter version |
| Brand Logos | Standard | Consider filter or alternate |
| Payment Icons | Standard | Check visibility |

### 6.3 Task List - Image Handling

```
- [ ] TASK-21J-046: Add --image-brightness and --image-contrast CSS variables
- [ ] TASK-21J-047: Create .dark-mode-dim utility class for images
- [ ] TASK-21J-048: Apply subtle dimming to product images in dark mode
- [ ] TASK-21J-049: Audit hero/banner images for dark mode
- [ ] TASK-21J-050: Check logo visibility in dark mode
- [ ] TASK-21J-051: Verify payment icons are visible in dark mode
- [ ] TASK-21J-052: Add data-dark-mode attribute support for per-image control
```

---

## 7. Accessibility Enhancements

### 7.1 Focus State Visibility

Focus states must be clearly visible in both themes:

```scss
:root {
  --focus-ring-width: 2px;
  --focus-ring-offset: 2px;
  --focus-ring-color: var(--color-primary-500);
}

[data-theme="dark"],
.dark {
  --focus-ring-color: var(--color-primary-400);
}

// Focus visible utility
:focus-visible {
  outline: var(--focus-ring-width) solid var(--focus-ring-color);
  outline-offset: var(--focus-ring-offset);
}

// For elements that need custom focus
.focus-ring {
  &:focus-visible {
    box-shadow: 0 0 0 var(--focus-ring-offset) var(--color-bg-primary),
                0 0 0 calc(var(--focus-ring-offset) + var(--focus-ring-width)) var(--focus-ring-color);
  }
}
```

### 7.2 Color-Blind Friendly Verification

Ensure the palette works for common color vision deficiencies:

| Deficiency | Affected Colors | Mitigation |
|------------|-----------------|------------|
| Protanopia (red-blind) | Red/Green distinction | Use icons + color |
| Deuteranopia (green-blind) | Red/Green distinction | Use icons + color |
| Tritanopia (blue-blind) | Blue/Yellow distinction | Adequate contrast |

**Key Principle**: Never rely on color alone to convey meaning. Always pair with:
- Icons
- Text labels
- Patterns/shapes

### 7.3 Task List - Accessibility

```
- [ ] TASK-21J-053: Standardize focus ring styles across both themes
- [ ] TASK-21J-054: Verify focus visibility on all interactive elements
- [ ] TASK-21J-055: Test with color blindness simulators
- [ ] TASK-21J-056: Ensure success/error states have icons, not just color
- [ ] TASK-21J-057: Verify skip links are visible when focused
- [ ] TASK-21J-058: Test keyboard navigation in dark mode
```

---

## 8. Theme Switching System

### 8.1 Current Implementation Review

The current implementation in `theme.service.ts` is well-designed:

| Feature | Status | Notes |
|---------|--------|-------|
| Light/Dark/System modes | Implemented | Uses signals |
| localStorage persistence | Implemented | Key: `climasite-theme-preference` |
| System preference detection | Implemented | `prefers-color-scheme` media query |
| System preference watching | Implemented | Event listener for changes |
| Theme application | Implemented | `data-theme` attribute + `.dark` class |
| Transition support | Partial | `--theme-transition` variable exists |

### 8.2 Improvements Needed

#### 8.2.1 Flash Prevention (FOUC)

Add inline script to `index.html` to prevent flash of wrong theme:

```html
<script>
  (function() {
    const stored = localStorage.getItem('climasite-theme-preference');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const isDark = stored === 'dark' || (stored === 'system' && prefersDark) || (!stored && prefersDark);
    if (isDark) {
      document.documentElement.setAttribute('data-theme', 'dark');
      document.documentElement.classList.add('dark');
    }
  })();
</script>
```

#### 8.2.2 Transition Smoothness

Add coordinated transitions:

```scss
// Exclude specific elements from theme transition to prevent jarring effects
.no-theme-transition,
.no-theme-transition * {
  transition: none !important;
}

// Smoother color transitions
:root {
  --theme-transition-duration: 200ms;
  --theme-transition: 
    background-color var(--theme-transition-duration) ease,
    color var(--theme-transition-duration) ease,
    border-color var(--theme-transition-duration) ease,
    box-shadow var(--theme-transition-duration) ease,
    fill var(--theme-transition-duration) ease,
    stroke var(--theme-transition-duration) ease;
}

// Apply to key elements
body,
.card,
.button,
.input,
header,
footer {
  transition: var(--theme-transition);
}
```

### 8.3 Task List - Theme Switching

```
- [ ] TASK-21J-059: Add FOUC prevention script to index.html
- [ ] TASK-21J-060: Standardize theme transition timing
- [ ] TASK-21J-061: Add .no-theme-transition utility class
- [ ] TASK-21J-062: Test theme switching performance
- [ ] TASK-21J-063: Verify localStorage persistence across sessions
- [ ] TASK-21J-064: Test system preference change detection
```

---

## 9. Technical Specifications

### 9.1 New CSS Variables to Add

```scss
// Add to :root in _colors.scss

// Rating Stars
--color-rating-star: #{$color-warm-400};
--color-rating-star-empty: #{$color-gray-300};

// Step Indicators (How It Works)
--color-step-1: #{$color-primary-500};
--color-step-2: #8b5cf6; // Purple
--color-step-3: #{$color-accent-500};
--color-step-4: #{$color-success-500};

// Status Badge Colors
--color-status-pending-bg: #{$color-warning-100};
--color-status-pending-text: #{$color-warning-800};
--color-status-success-bg: #{$color-success-100};
--color-status-success-text: #{$color-success-800};
--color-status-error-bg: #{$color-error-100};
--color-status-error-text: #{$color-error-800};
--color-status-info-bg: #{$color-info-100};
--color-status-info-text: #{$color-info-800};
--color-status-neutral-bg: #{$color-gray-100};
--color-status-neutral-text: #{$color-gray-700};

// Image Handling
--image-brightness: 1;
--image-contrast: 1;

// Focus Ring (standardized)
--focus-ring-width: 2px;
--focus-ring-offset: 2px;
--focus-ring-color: #{$color-primary-500};

// Shadows (standardized)
--shadow-sm: 0 1px 2px 0 var(--shadow-color);
--shadow-md: 0 4px 6px -1px var(--shadow-color), 0 2px 4px -2px var(--shadow-color);
--shadow-lg: 0 10px 15px -3px var(--shadow-color-lg), 0 4px 6px -4px var(--shadow-color);
--shadow-xl: 0 20px 25px -5px var(--shadow-color-xl), 0 8px 10px -6px var(--shadow-color-lg);

// Theme Transition
--theme-transition-duration: 200ms;
```

### 9.2 Dark Theme Overrides to Add/Modify

```scss
[data-theme="dark"],
.dark {
  // Rating Stars (brighter for dark mode)
  --color-rating-star: #{$color-warm-300};
  --color-rating-star-empty: #{$color-gray-600};

  // Step Indicators
  --color-step-1: #{$color-primary-400};
  --color-step-2: #a78bfa; // Lighter purple
  --color-step-3: #{$color-accent-400};
  --color-step-4: #{$color-success-400};

  // Status Badge Colors (darker backgrounds, lighter text)
  --color-status-pending-bg: rgba(245, 158, 11, 0.15);
  --color-status-pending-text: #{$color-warning-300};
  --color-status-success-bg: rgba(16, 185, 129, 0.15);
  --color-status-success-text: #{$color-success-300};
  --color-status-error-bg: rgba(239, 68, 68, 0.15);
  --color-status-error-text: #{$color-error-300};
  --color-status-info-bg: rgba(14, 165, 233, 0.15);
  --color-status-info-text: #{$color-info-300};
  --color-status-neutral-bg: rgba(100, 116, 139, 0.15);
  --color-status-neutral-text: #{$color-gray-300};

  // Image Handling
  --image-brightness: 0.92;
  --image-contrast: 1.05;

  // Focus Ring (lighter for dark mode)
  --focus-ring-color: #{$color-primary-400};

  // Border adjustments
  --color-border-primary: #{$color-gray-600};
  --color-border-secondary: #{$color-gray-500};

  // Glass adjustments
  --glass-bg: rgba(30, 41, 59, 0.85);
  --glass-bg-heavy: rgba(30, 41, 59, 0.92);
  --glass-border: rgba(255, 255, 255, 0.12);

  // Muted text fix
  --color-text-muted: #{$color-gray-400};
}
```

---

## 10. Dependencies

### 10.1 Dependencies on Other Plans

| Plan | Dependency Type | Notes |
|------|----------------|-------|
| **21E Component Library** | Strong | Component library should use these variables |
| **21A Home Page** | Medium | Hero images need dark mode handling |
| **21B Product Experience** | Medium | Product cards need contrast verification |
| **21G Trust & Credibility** | Low | Trust badges should work in both themes |

### 10.2 Order of Implementation

1. **First**: Complete TASK-21J-001 (new CSS variables) - foundation for everything
2. **Second**: Complete contrast fixes (TASK-21J-023 through 030)
3. **Third**: Fix hardcoded colors (TASK-21J-002 through 022)
4. **Fourth**: Glass/shadow adjustments (TASK-21J-031 through 045)
5. **Fifth**: Image handling (TASK-21J-046 through 052)
6. **Sixth**: Accessibility & theme switching (TASK-21J-053 through 064)

---

## 11. Testing Checklist

### 11.1 Visual Testing

```
- [ ] TEST-21J-001: Toggle between light/dark themes on every page
- [ ] TEST-21J-002: Verify no flash of wrong theme on page load
- [ ] TEST-21J-003: Check all text is readable in both themes
- [ ] TEST-21J-004: Verify all interactive elements have visible focus states
- [ ] TEST-21J-005: Check glass cards are readable in both themes
- [ ] TEST-21J-006: Verify product images don't look washed out in dark mode
- [ ] TEST-21J-007: Check form inputs are clearly visible in both themes
- [ ] TEST-21J-008: Verify error/success states are distinguishable
- [ ] TEST-21J-009: Check table borders and dividers are visible
- [ ] TEST-21J-010: Verify shadows provide appropriate depth in both themes
```

### 11.2 Automated Testing

```
- [ ] TEST-21J-011: Run grep to confirm no hardcoded hex colors remain
- [ ] TEST-21J-012: Run axe-core or Lighthouse accessibility audit
- [ ] TEST-21J-013: Run contrast ratio checker on all color combinations
- [ ] TEST-21J-014: Run E2E tests with theme toggle
- [ ] TEST-21J-015: Verify localStorage persistence in E2E tests
```

### 11.3 Browser/Device Testing

```
- [ ] TEST-21J-016: Chrome (Windows/Mac)
- [ ] TEST-21J-017: Firefox (Windows/Mac)
- [ ] TEST-21J-018: Safari (Mac/iOS)
- [ ] TEST-21J-019: Edge (Windows)
- [ ] TEST-21J-020: Mobile browsers (Chrome/Safari)
- [ ] TEST-21J-021: Test system preference detection on all platforms
```

### 11.4 Accessibility Testing

```
- [ ] TEST-21J-022: Screen reader testing (NVDA/VoiceOver)
- [ ] TEST-21J-023: Keyboard navigation in both themes
- [ ] TEST-21J-024: Color blindness simulation (Protanopia, Deuteranopia, Tritanopia)
- [ ] TEST-21J-025: High contrast mode testing
- [ ] TEST-21J-026: Reduced motion preference testing
```

---

## 12. Complete Task Summary

### All Tasks (64 total)

#### Color Audit (22 tasks)
- [ ] TASK-21J-001: Create new CSS variables for missing semantic colors
- [ ] TASK-21J-002: Fix hardcoded colors in promotion-detail.component.ts
- [ ] TASK-21J-003: Fix hardcoded colors in brand-detail.component.ts
- [ ] TASK-21J-004: Fix hardcoded colors in promotions-list.component.ts
- [ ] TASK-21J-005: Fix hardcoded colors in order-details.component.ts
- [ ] TASK-21J-006: Fix hardcoded colors in orders.component.ts
- [ ] TASK-21J-007: Fix hardcoded colors in cart.component.ts
- [ ] TASK-21J-008: Fix hardcoded colors in checkout.component.ts
- [ ] TASK-21J-009: Fix hardcoded colors in contact.component.ts
- [ ] TASK-21J-010: Fix hardcoded colors in product-list.component.ts
- [ ] TASK-21J-011: Fix hardcoded colors in product-reviews.component.ts
- [ ] TASK-21J-012: Fix hardcoded colors in testimonials.component.ts
- [ ] TASK-21J-013: Fix hardcoded colors in final-cta.component.ts
- [ ] TASK-21J-014: Fix hardcoded colors in frequently-bought.component.ts
- [ ] TASK-21J-015: Fix hardcoded colors in similar-products.component.ts
- [ ] TASK-21J-016: Fix hardcoded colors in product-consumables.component.ts
- [ ] TASK-21J-017: Fix hardcoded colors in financing-calculator.component.ts
- [ ] TASK-21J-018: Fix hardcoded colors in how-it-works.component.ts
- [ ] TASK-21J-019: Fix hardcoded colors in glass-card.component.ts
- [ ] TASK-21J-020: Update order.model.ts status colors to use CSS variables
- [ ] TASK-21J-021: Audit payment.service.ts Stripe styling
- [ ] TASK-21J-022: Run final grep to confirm zero hardcoded colors remain

#### Contrast Verification (8 tasks)
- [ ] TASK-21J-023: Adjust --color-text-placeholder for AA compliance
- [ ] TASK-21J-024: Adjust --color-text-muted in dark theme for AA compliance
- [ ] TASK-21J-025: Ensure primary color links use darker variant for text
- [ ] TASK-21J-026: Verify all form input text meets 4.5:1 contrast
- [ ] TASK-21J-027: Verify all button text meets 4.5:1 contrast
- [ ] TASK-21J-028: Verify disabled state text meets 3:1 minimum
- [ ] TASK-21J-029: Test with browser contrast checker tools
- [ ] TASK-21J-030: Document all contrast ratios in _colors.scss comments

#### Glass Effects (6 tasks)
- [ ] TASK-21J-031: Increase dark mode glass opacity from 0.8 to 0.85
- [ ] TASK-21J-032: Add --glass-bg-subtle variable for both themes
- [ ] TASK-21J-033: Add --glass-backdrop-blur standardization
- [ ] TASK-21J-034: Review glass-card.component.ts for hardcoded values
- [ ] TASK-21J-035: Test glass effects on various background images
- [ ] TASK-21J-036: Verify glass readability in both themes

#### Shadows (5 tasks)
- [ ] TASK-21J-037: Add standardized shadow scale to _colors.scss
- [ ] TASK-21J-038: Create dark mode specific shadow adjustments
- [ ] TASK-21J-039: Replace inline shadow definitions with variables
- [ ] TASK-21J-040: Add focus ring shadow variable
- [ ] TASK-21J-041: Test shadow visibility on dark backgrounds

#### Borders (4 tasks)
- [ ] TASK-21J-042: Lighten dark mode border-primary for better visibility
- [ ] TASK-21J-043: Add --color-border-subtle variable
- [ ] TASK-21J-044: Audit table borders in dark mode
- [ ] TASK-21J-045: Verify input focus borders meet 3:1 contrast

#### Image Handling (7 tasks)
- [ ] TASK-21J-046: Add --image-brightness and --image-contrast CSS variables
- [ ] TASK-21J-047: Create .dark-mode-dim utility class for images
- [ ] TASK-21J-048: Apply subtle dimming to product images in dark mode
- [ ] TASK-21J-049: Audit hero/banner images for dark mode
- [ ] TASK-21J-050: Check logo visibility in dark mode
- [ ] TASK-21J-051: Verify payment icons are visible in dark mode
- [ ] TASK-21J-052: Add data-dark-mode attribute support for per-image control

#### Accessibility (6 tasks)
- [ ] TASK-21J-053: Standardize focus ring styles across both themes
- [ ] TASK-21J-054: Verify focus visibility on all interactive elements
- [ ] TASK-21J-055: Test with color blindness simulators
- [ ] TASK-21J-056: Ensure success/error states have icons, not just color
- [ ] TASK-21J-057: Verify skip links are visible when focused
- [ ] TASK-21J-058: Test keyboard navigation in dark mode

#### Theme Switching (6 tasks)
- [ ] TASK-21J-059: Add FOUC prevention script to index.html
- [ ] TASK-21J-060: Standardize theme transition timing
- [ ] TASK-21J-061: Add .no-theme-transition utility class
- [ ] TASK-21J-062: Test theme switching performance
- [ ] TASK-21J-063: Verify localStorage persistence across sessions
- [ ] TASK-21J-064: Test system preference change detection

---

## 13. Files to Modify

### Primary Files

| File | Changes |
|------|---------|
| `src/styles/_colors.scss` | Add new variables, adjust dark theme values |
| `src/index.html` | Add FOUC prevention script |

### Component Files (Hardcoded Colors)

| File | Type of Change |
|------|---------------|
| `promotion-detail.component.ts` | Replace hex with CSS vars |
| `brand-detail.component.ts` | Replace hex with CSS vars |
| `promotions-list.component.ts` | Replace hex with CSS vars |
| `order-details.component.ts` | Replace hex with CSS vars |
| `orders.component.ts` | Replace hex with CSS vars |
| `cart.component.ts` | Replace hex with CSS vars |
| `checkout.component.ts` | Replace hex with CSS vars |
| `contact.component.ts` | Replace hex with CSS vars |
| `product-list.component.ts` | Replace hex with CSS vars |
| `product-reviews.component.ts` | Replace hex with CSS vars |
| `testimonials.component.ts` | Replace hex with CSS vars |
| `final-cta.component.ts` | Replace hex with CSS vars |
| `frequently-bought.component.ts` | Replace hex with CSS vars |
| `similar-products.component.ts` | Replace hex with CSS vars |
| `product-consumables.component.ts` | Replace hex with CSS vars |
| `financing-calculator.component.ts` | Replace hex with CSS vars |
| `how-it-works.component.ts` | Replace hex with CSS vars |
| `glass-card.component.ts` | Replace hex with CSS vars |
| `order.model.ts` | Update status color config |
| `payment.service.ts` | Update Stripe element styles |

---

*Document created: January 24, 2026*
*Plan Status: Ready for Implementation*
*Estimated Duration: 1 day (6-10 hours)*
*Priority: Low (per master plan), but foundational for quality*
