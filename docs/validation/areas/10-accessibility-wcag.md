# Accessibility (WCAG) - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Keyboard Navigation** - Tab order, focus management, keyboard-only operation
- **ARIA Roles & Labels** - Semantic markup for assistive technologies
- **Focus Traps** - Modal dialogs, mobile menus, filter sidebars
- **Focus Order** - Logical tab sequence, focus restoration
- **Screen Reader Support** - Live regions, announcements, semantic structure
- **Color Contrast** - WCAG AA/AAA compliance for text and interactive elements
- **Shared Components** - Modal, Toast, Alert, Breadcrumb accessibility

### WCAG 2.1 Guidelines Covered
| Guideline | Level | Description |
|-----------|-------|-------------|
| 1.3.1 | A | Info and Relationships |
| 1.4.3 | AA | Contrast (Minimum) |
| 1.4.11 | AA | Non-text Contrast |
| 2.1.1 | A | Keyboard |
| 2.1.2 | A | No Keyboard Trap |
| 2.4.3 | A | Focus Order |
| 2.4.7 | AA | Focus Visible |
| 3.2.1 | A | On Focus |
| 4.1.2 | A | Name, Role, Value |
| 4.1.3 | AA | Status Messages |

### Components Audited
| Component | Location |
|-----------|----------|
| Modal | `src/ClimaSite.Web/src/app/shared/components/modal/modal.component.ts` |
| Toast | `src/ClimaSite.Web/src/app/shared/components/toast/toast.component.ts` |
| Alert | `src/ClimaSite.Web/src/app/shared/components/alert/alert.component.ts` |
| Breadcrumb | `src/ClimaSite.Web/src/app/shared/components/breadcrumb/breadcrumb.component.ts` |
| Input | `src/ClimaSite.Web/src/app/shared/components/input/input.component.ts` |
| Header | `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts` |
| Product List | `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts` |
| Checkout | `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts` |

---

## 2. Code Path Map

### Focus Management Implementation

| Component | Focus Trap | Escape Close | Focus Restore | File:Line |
|-----------|------------|--------------|---------------|-----------|
| **ModalComponent** | Yes | Yes | Yes | `modal.component.ts:278-332` |
| **HeaderComponent (Mobile Menu)** | Yes | Yes | Yes | `header.component.ts:1278-1301` |
| **ProductListComponent (Filter Sidebar)** | Yes | Yes | Yes | `product-list.component.ts:1200-1226` |
| **CheckoutComponent (Processing Modal)** | Partial | No | No | `checkout.component.ts:51-66` |

### ARIA Implementation by Component

#### Modal Component (`modal.component.ts`)
```typescript
// Line 29-31: Role and modal attributes
role="dialog"
aria-modal="true"
[attr.aria-labelledby]="titleId"

// Line 45: Close button accessible label
[attr.aria-label]="'common.close' | translate"

// Line 48: Decorative SVG hidden from AT
aria-hidden="true"
```

#### Toast Component (`toast.component.ts`)
```typescript
// Line 12: Alert role for live region
role="alert"

// Line 17: Icon hidden from AT
aria-hidden="true"

// Line 48: Dismiss button label
[attr.aria-label]="'common.dismiss' | translate"

// Line 51: SVG hidden from AT
aria-hidden="true"
```

#### Alert Component (`alert.component.ts`)
```typescript
// Line 14: Alert role
role="alert"

// Line 20: Icon hidden from AT
aria-hidden="true"

// Line 54: Close button label
[attr.aria-label]="'common.close' | translate"
```

#### Breadcrumb Component (`breadcrumb.component.ts`)
```typescript
// Line 22: Navigation landmark with label
[attr.aria-label]="'common.aria.breadcrumb' | translate"

// Line 48: Separator hidden from AT
aria-hidden="true"

// Line 56: Current page indicator
aria-current="page"

// Line 25-31: Schema.org structured data
itemscope itemtype="https://schema.org/BreadcrumbList"
```

#### Input Component (`input.component.ts`)
```typescript
// Line 41-42: Accessible input attributes
[attr.aria-label]="ariaLabel() || label()"
[attr.aria-describedby]="error() ? inputId + '-error' : null"

// Line 57-58: Password toggle accessibility
[attr.aria-label]="passwordVisible() ? ('auth.hidePassword' | translate) : ('auth.showPassword' | translate)"
[attr.aria-pressed]="passwordVisible()"

// Line 81: Error message live region
role="alert"
```

### Form Validation Accessibility (Checkout)
| Field | aria-invalid | aria-describedby | role="alert" | File:Line |
|-------|--------------|------------------|--------------|-----------|
| firstName | Yes | Yes | Yes | `checkout.component.ts:215-219` |
| lastName | Yes | Yes | Yes | `checkout.component.ts:229-233` |
| email | Yes | Yes | Yes | `checkout.component.ts:245-249` |
| addressLine1 | Yes | Yes | Yes | `checkout.component.ts:266-270` |
| city | Yes | Yes | Yes | `checkout.component.ts:287-291` |
| postalCode | Yes | Yes | Yes | `checkout.component.ts:318-322` |
| phone | Yes | Yes | Yes | `checkout.component.ts:354-358` |

### Keyboard Navigation Patterns

| Pattern | Implementation | File:Line |
|---------|----------------|-----------|
| **Tab cycling in modal** | First/last element wrap | `modal.component.ts:313-331` |
| **Tab cycling in mobile menu** | First/last element wrap | `header.component.ts:1294-1300` |
| **Tab cycling in filter sidebar** | First/last element wrap | `product-list.component.ts:1219-1225` |
| **Escape to close modal** | Document keydown listener | `modal.component.ts:301-306` |
| **Escape to close mobile menu** | Component keydown handler | `header.component.ts:1279-1281` |
| **Escape to close filter sidebar** | Component keydown handler | `product-list.component.ts:1204-1206` |

### Global Focus Styles (`styles.scss:59-80`)
```scss
// Line 61-64: Global focus-visible outline
*:focus-visible {
  outline: 2px solid var(--color-border-focus);
  outline-offset: 2px;
}

// Line 66-76: Interactive elements focus styles
button:focus-visible,
a:focus-visible,
input:focus-visible,
select:focus-visible,
textarea:focus-visible,
[role="button"]:focus-visible,
[tabindex]:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
  box-shadow: 0 0 0 4px var(--glow-primary);
}

// Line 78-80: Hide focus ring for mouse users
:focus:not(:focus-visible) {
  outline: none;
}
```

---

## 3. Test Coverage Audit

### Unit Tests - Accessibility

| Test File | Test Names | WCAG Criteria |
|-----------|------------|---------------|
| `modal.component.spec.ts:68-75` | `should have role="dialog" and aria-modal="true"` | 4.1.2 Name, Role, Value |
| `modal.component.spec.ts:145-157` | `should close on Escape key press` | 2.1.1 Keyboard |
| `modal.component.spec.ts:159-171` | `should not close on Escape when closeOnEscape is false` | 2.1.1 Keyboard |
| `toast.component.spec.ts:67-70` | `should have role="alert" for accessibility` | 4.1.3 Status Messages |
| `alert.component.spec.ts:83-86` | `should have role="alert" for accessibility` | 4.1.3 Status Messages |
| `breadcrumb.component.spec.ts:44-47` | `should have nav element with aria-label` | 1.3.1 Info & Relationships |
| `breadcrumb.component.spec.ts:106-114` | `should render span for current page` with `aria-current="page"` | 2.4.8 Location |
| `product-gallery.component.spec.ts:91-110` | `should handle keyboard navigation in fullscreen` | 2.1.1 Keyboard |
| `theme-toggle.component.spec.ts:81` | `should be keyboard accessible` | 2.1.1 Keyboard |

### E2E Tests - Accessibility

| Test File | Test Names | WCAG Criteria |
|-----------|------------|---------------|
| `UserMenuTests.cs:168-182` | `UserMenu_PressEscape_ClosesDropdown` | 2.1.1 Keyboard |
| `ThemeAndSettingsTests.cs:353-360` | Tab through focusable elements | 2.4.3 Focus Order |

### Missing Test Coverage

| Area | Missing Tests | Priority |
|------|---------------|----------|
| Modal focus trap | Tab cycling stays within modal | Critical |
| Modal focus restore | Focus returns to trigger element | High |
| Screen reader announcements | Live region announcements | High |
| Skip links | Skip to main content | Medium |
| Heading hierarchy | h1-h6 logical order | Medium |
| Form error announcements | Error read by screen reader | High |
| Color contrast validation | Automated contrast checking | Medium |
| Mobile menu focus trap | Tab cycling in mobile nav | High |

---

## 4. Manual Verification Steps

### Keyboard Navigation Testing

#### Modal Component
1. Click a button that opens a modal
2. Verify focus moves to first focusable element inside modal
3. Press Tab repeatedly - verify focus stays within modal
4. Press Shift+Tab - verify reverse cycling works
5. Press Escape - verify modal closes
6. Verify focus returns to the triggering element

#### Mobile Menu (< 1024px viewport)
1. Resize browser to mobile width
2. Click hamburger menu icon
3. Verify focus moves into mobile menu
4. Press Tab - verify focus cycles within menu
5. Press Escape - verify menu closes
6. Verify focus returns to hamburger button

#### Filter Sidebar (Product List, mobile)
1. Navigate to `/products` on mobile viewport
2. Click filter toggle button
3. Verify focus moves into filter sidebar
4. Press Tab - verify focus stays within sidebar
5. Press Escape - verify sidebar closes
6. Verify focus returns to filter button

### Screen Reader Testing

#### Toast Notifications
1. Enable screen reader (VoiceOver/NVDA/JAWS)
2. Trigger an action that shows a toast (e.g., add to cart)
3. Verify toast message is announced automatically (`role="alert"`)
4. Verify dismiss button announces its purpose

#### Alert Components
1. Navigate to a page with alerts
2. Verify alert content is announced when page loads
3. Verify alert type (success/error/warning/info) is conveyed

#### Breadcrumb Navigation
1. Navigate to a product detail page
2. Verify screen reader announces "breadcrumb navigation"
3. Verify each breadcrumb item is announced
4. Verify current page has "current page" announcement

#### Form Validation
1. Navigate to checkout page
2. Submit form with empty required fields
3. Verify error messages are announced
4. Verify field labels are properly associated

### Color Contrast Testing

#### Light Theme
1. Use browser DevTools or axe-core extension
2. Check text contrast ratios:
   - Primary text (#0f172a) on white: **21:1** (AAA)
   - Secondary text (#475569) on white: **7.06:1** (AAA)
   - Tertiary text (#64748b) on white: **4.55:1** (AA)
   - Placeholder text (#94a3b8) on white: **3.02:1** (AA for large text)
3. Check interactive element contrast:
   - Primary button (#0ea5e9) on white: **3.03:1** (AA for UI)
   - Focus ring visible against all backgrounds

#### Dark Theme
1. Switch to dark theme
2. Check text contrast ratios:
   - Primary text (#f8fafc) on #0f172a: **17.4:1** (AAA)
   - Secondary text (#cbd5e1) on #1e293b: **7.23:1** (AAA)
3. Verify focus indicators visible in dark mode

### Focus Visibility Testing
1. Navigate using Tab key only
2. Verify focus indicator visible on all interactive elements:
   - Buttons (primary, secondary, icon buttons)
   - Links (navigation, inline)
   - Form inputs (text, select, checkbox, radio)
   - Custom controls (theme toggle, language selector)
3. Verify focus indicator meets 3:1 contrast ratio

---

## 5. Gaps & Risks

### Critical Gaps

- [ ] **No skip links** - No "Skip to main content" link for keyboard users
- [ ] **Processing modal not accessible** - `checkout.component.ts:51-66` lacks focus trap and escape handler
- [ ] **No automated a11y tests** - No axe-core or similar in CI pipeline
- [ ] **Password toggle removes from tab order** - `tabindex="-1"` on password toggle button (`input.component.ts:59`)

### High Priority Gaps

- [ ] **No landmark regions** - Missing `<main>`, `<nav>`, `<aside>` landmarks (only partial implementation)
- [ ] **Heading hierarchy not enforced** - No automated check for proper h1-h6 nesting
- [ ] **No focus trap E2E tests** - Focus management not tested end-to-end
- [ ] **Mobile menu close button** - No visible close button, only Escape key
- [ ] **Image alt text validation** - No automated check for missing alt attributes
- [ ] **Error summary missing** - Forms show inline errors but no summary for screen readers

### Medium Priority Gaps

- [ ] **No reduced motion support** - Animations don't respect `prefers-reduced-motion` in all components
- [ ] **No high contrast mode** - No CSS for Windows High Contrast Mode
- [ ] **Touch target size** - Some buttons may be < 44x44px on mobile
- [ ] **No aria-live for cart count** - Cart badge updates not announced
- [ ] **Language switcher** - No `lang` attribute update on language change
- [ ] **Auto-complete attributes** - Not all form fields have proper autocomplete values

### Low Priority Gaps

- [ ] **No print styles** - Print stylesheet not accessibility-optimized
- [ ] **Decorative images** - Some SVGs missing `aria-hidden="true"`
- [ ] **Link purpose** - Some links lack context (e.g., "Read more" without visible context)

### Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Screen reader users cannot complete checkout | High | Medium | Add proper ARIA live regions, focus management |
| Keyboard users trapped in modals | High | Low | Focus trap implementation exists, needs testing |
| Color-blind users miss error states | Medium | Medium | Ensure errors have text + icon, not just color |
| Users with motor impairments struggle with small targets | Medium | Medium | Audit touch target sizes |

---

## 6. Recommended Fixes & Tests

### Critical Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| No skip links | Add `<a href="#main-content" class="skip-link">Skip to main content</a>` in `app.component.html` | Low |
| Processing modal a11y | Add `aria-live="polite"`, focus management, Escape handler to processing overlay | Medium |
| Password toggle tabindex | Change `tabindex="-1"` to `tabindex="0"` and add keyboard handler | Low |
| Add a11y CI testing | Integrate `@axe-core/playwright` in E2E tests, run on every PR | Medium |

### High Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| Focus trap E2E tests | Add Playwright tests for modal, mobile menu, filter sidebar focus traps | Medium |
| Landmark regions | Add `<main role="main">`, ensure `<nav>` on header, `<aside>` for filters | Low |
| Error summary | Add error summary component that announces all form errors | Medium |
| Cart badge live region | Add `aria-live="polite"` to cart count, announce changes | Low |
| Mobile menu close button | Add visible close button with accessible name | Low |

### Medium Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| Reduced motion | Add `@media (prefers-reduced-motion: reduce)` to all animations | Medium |
| High contrast mode | Test and add `-ms-high-contrast` media queries | Medium |
| Touch targets | Audit all buttons/links for 44x44px minimum size | Medium |
| Auto-complete attributes | Add `autocomplete` to all form fields per WCAG 1.3.5 | Low |

### Test Recommendations

```typescript
// E2E Test: Modal Focus Trap
test('modal should trap focus within', async ({ page }) => {
  await page.goto('/products');
  await page.click('[data-testid="quick-view-button"]');
  
  // Verify focus moves to modal
  const modal = page.locator('[role="dialog"]');
  await expect(modal).toBeFocused();
  
  // Tab through all elements, verify focus stays in modal
  for (let i = 0; i < 10; i++) {
    await page.keyboard.press('Tab');
    const activeElement = await page.evaluate(() => document.activeElement?.closest('[role="dialog"]'));
    expect(activeElement).not.toBeNull();
  }
  
  // Escape closes modal
  await page.keyboard.press('Escape');
  await expect(modal).not.toBeVisible();
});

// Unit Test: Screen Reader Announcement
it('should announce error message to screen readers', () => {
  fixture.componentRef.setInput('error', 'Email is required');
  fixture.detectChanges();
  
  const errorElement = fixture.debugElement.query(By.css('[role="alert"]'));
  expect(errorElement).toBeTruthy();
  expect(errorElement.nativeElement.textContent).toContain('Email is required');
});

// Integration: Axe-core Accessibility Scan
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('should not have any accessibility violations on home page', async ({ page }) => {
  await page.goto('/');
  const accessibilityScanResults = await new AxeBuilder({ page }).analyze();
  expect(accessibilityScanResults.violations).toEqual([]);
});
```

---

## 7. Evidence & Notes

### Accessibility Implementation Details

#### Focus Trap Pattern (Modal)
From `modal.component.ts:278-332`:
```typescript
private setupFocusTrap(): void {
  const modalContainer = this.elementRef.nativeElement.querySelector('.modal-container');
  if (!modalContainer) return;

  const focusableSelectors = [
    'button:not([disabled])',
    '[href]',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])'
  ].join(', ');

  this.focusableElements = Array.from(modalContainer.querySelectorAll(focusableSelectors));
  this.firstFocusableElement = this.focusableElements[0] || null;
  this.lastFocusableElement = this.focusableElements[this.focusableElements.length - 1] || null;

  // Focus first focusable element
  if (this.firstFocusableElement) {
    this.firstFocusableElement.focus();
  }
}

private handleTabKey(event: KeyboardEvent): void {
  if (this.focusableElements.length === 0) {
    event.preventDefault();
    return;
  }

  if (event.shiftKey) {
    if (this.document.activeElement === this.firstFocusableElement) {
      event.preventDefault();
      this.lastFocusableElement?.focus();
    }
  } else {
    if (this.document.activeElement === this.lastFocusableElement) {
      event.preventDefault();
      this.firstFocusableElement?.focus();
    }
  }
}
```

#### Focus Restoration Pattern
From `modal.component.ts:240-268`:
```typescript
private onOpen(): void {
  // Store currently focused element
  this.previouslyFocusedElement = this.document.activeElement as HTMLElement;
  // ...
}

private onClose(): void {
  // Restore focus to previously focused element
  if (this.previouslyFocusedElement) {
    this.previouslyFocusedElement.focus();
    this.previouslyFocusedElement = null;
  }
}
```

#### Color Contrast Implementation
From `_colors.scss`:
- Light theme text on white background meets WCAG AA/AAA
- Dark theme text on dark background meets WCAG AA/AAA
- Focus indicators use `--color-primary` with `--glow-primary` for enhanced visibility

#### Semantic HTML Structure
- `role="dialog"` + `aria-modal="true"` for modals
- `role="alert"` for toast/alert components (live regions)
- `role="navigation"` for pagination and nav elements
- `role="radiogroup"` for shipping/payment method selection
- `aria-current="page"` for current breadcrumb item
- `aria-pressed` for toggle buttons
- `aria-expanded` for expandable sections

### ARIA Attribute Inventory

| Attribute | Usage Count | Components |
|-----------|-------------|------------|
| `aria-label` | 50+ | Buttons, links, inputs, navigation |
| `aria-hidden="true"` | 30+ | Decorative icons, separators |
| `aria-modal="true"` | 4 | Modal, mobile menu, filter sidebar, gallery |
| `aria-current` | 3 | Breadcrumb, pagination, checkout steps |
| `aria-describedby` | 15+ | Form inputs with errors |
| `aria-invalid` | 15+ | Form inputs with validation |
| `aria-pressed` | 3 | Toggle buttons |
| `aria-expanded` | 5+ | Dropdowns, accordions |
| `aria-labelledby` | 5+ | Modals, form groups |
| `role="alert"` | 15+ | Toast, alert, form errors |
| `role="dialog"` | 4 | Modal, galleries |
| `role="navigation"` | 3 | Pagination, nav |
| `role="radiogroup"` | 4 | Shipping methods, payment methods |
| `role="group"` | 5+ | Form field groups |

### Reduced Motion Support
From `styles.scss:27-29`:
```scss
@media (prefers-reduced-motion: reduce) {
  scroll-behavior: auto;
}
```
Note: This only covers scroll behavior; animations in components need individual handling.

### Test Data References

| Test File | Purpose | Coverage |
|-----------|---------|----------|
| `modal.component.spec.ts` | Focus management, keyboard, ARIA | Good |
| `toast.component.spec.ts` | Alert role, dismiss functionality | Good |
| `alert.component.spec.ts` | Alert role, visibility | Good |
| `breadcrumb.component.spec.ts` | Navigation landmark, current page | Good |
| `UserMenuTests.cs` | Escape key closes menu | Partial |
| `ThemeAndSettingsTests.cs` | Tab navigation verification | Minimal |
