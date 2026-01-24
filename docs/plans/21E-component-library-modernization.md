# Plan 21E: Component Library Modernization

## Nordic Tech Design System

**Design Direction:** Consistent, accessible, modern - inspired by Scandinavian design principles of simplicity, functionality, and clarity.

---

## 1. Overview

### 1.1 Goals

| Goal | Description |
|------|-------------|
| **Consistency** | Unified visual language across all components with predictable behavior |
| **Accessibility** | WCAG 2.1 AA compliance for all components, keyboard navigation, screen reader support |
| **Performance** | Tree-shakeable components, minimal CSS footprint, optimized animations |
| **Developer Experience** | Intuitive APIs, comprehensive documentation, TypeScript-first |
| **Maintainability** | Single source of truth for design tokens, centralized styling |

### 1.2 Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Component API Consistency | 100% | All components follow same input/output patterns |
| Accessibility Score | 100% | Lighthouse accessibility audit |
| Bundle Size Impact | < 50KB | Gzipped component library total |
| Design Token Coverage | 100% | No hardcoded values in components |
| Test Coverage | > 90% | Unit tests for all component variants |
| Documentation Coverage | 100% | All components documented with examples |

### 1.3 Estimated Effort

| Phase | Tasks | Est. Hours | Priority |
|-------|-------|------------|----------|
| Design Tokens | 5 tasks | 8h | P0 - Foundation |
| Button System | 6 tasks | 12h | P0 - Foundation |
| Icon System | 4 tasks | 10h | P0 - Foundation |
| Card System | 5 tasks | 14h | P1 - Core |
| Form Components | 8 tasks | 24h | P1 - Core |
| Feedback Components | 5 tasks | 12h | P1 - Core |
| Loading States | 5 tasks | 10h | P2 - Enhanced |
| Modal/Dialog System | 4 tasks | 12h | P2 - Enhanced |
| Navigation Components | 4 tasks | 10h | P2 - Enhanced |
| Documentation | 3 tasks | 8h | P3 - Polish |
| **Total** | **49 tasks** | **~120h** | |

---

## 2. Design Tokens

### 2.1 Spacing Scale

```scss
// Design tokens file: src/ClimaSite.Web/src/styles/_tokens.scss

// Base unit: 4px (0.25rem)
$spacing-0: 0;
$spacing-px: 1px;
$spacing-0-5: 0.125rem;  // 2px
$spacing-1: 0.25rem;     // 4px
$spacing-1-5: 0.375rem;  // 6px
$spacing-2: 0.5rem;      // 8px
$spacing-2-5: 0.625rem;  // 10px
$spacing-3: 0.75rem;     // 12px
$spacing-3-5: 0.875rem;  // 14px
$spacing-4: 1rem;        // 16px
$spacing-5: 1.25rem;     // 20px
$spacing-6: 1.5rem;      // 24px
$spacing-7: 1.75rem;     // 28px
$spacing-8: 2rem;        // 32px
$spacing-9: 2.25rem;     // 36px
$spacing-10: 2.5rem;     // 40px
$spacing-11: 2.75rem;    // 44px
$spacing-12: 3rem;       // 48px
$spacing-14: 3.5rem;     // 56px
$spacing-16: 4rem;       // 64px
$spacing-20: 5rem;       // 80px
$spacing-24: 6rem;       // 96px
$spacing-28: 7rem;       // 112px
$spacing-32: 8rem;       // 128px

:root {
  --space-0: #{$spacing-0};
  --space-px: #{$spacing-px};
  --space-0-5: #{$spacing-0-5};
  --space-1: #{$spacing-1};
  --space-1-5: #{$spacing-1-5};
  --space-2: #{$spacing-2};
  --space-2-5: #{$spacing-2-5};
  --space-3: #{$spacing-3};
  --space-3-5: #{$spacing-3-5};
  --space-4: #{$spacing-4};
  --space-5: #{$spacing-5};
  --space-6: #{$spacing-6};
  --space-7: #{$spacing-7};
  --space-8: #{$spacing-8};
  --space-9: #{$spacing-9};
  --space-10: #{$spacing-10};
  --space-11: #{$spacing-11};
  --space-12: #{$spacing-12};
  --space-14: #{$spacing-14};
  --space-16: #{$spacing-16};
  --space-20: #{$spacing-20};
  --space-24: #{$spacing-24};
  --space-28: #{$spacing-28};
  --space-32: #{$spacing-32};
}
```

### 2.2 Border Radius Scale

```scss
$radius-none: 0;
$radius-sm: 0.125rem;    // 2px - Subtle rounding
$radius-md: 0.25rem;     // 4px - Default small elements
$radius-lg: 0.5rem;      // 8px - Buttons, inputs
$radius-xl: 0.75rem;     // 12px - Cards
$radius-2xl: 1rem;       // 16px - Modals, large cards
$radius-3xl: 1.5rem;     // 24px - Hero sections
$radius-full: 9999px;    // Circular

:root {
  --radius-none: #{$radius-none};
  --radius-sm: #{$radius-sm};
  --radius-md: #{$radius-md};
  --radius-lg: #{$radius-lg};
  --radius-xl: #{$radius-xl};
  --radius-2xl: #{$radius-2xl};
  --radius-3xl: #{$radius-3xl};
  --radius-full: #{$radius-full};
}
```

### 2.3 Shadow Scale

```scss
// Elevation system for depth hierarchy
:root {
  // Light theme shadows
  --shadow-xs: 0 1px 2px 0 rgba(15, 23, 42, 0.05);
  --shadow-sm: 0 1px 3px 0 rgba(15, 23, 42, 0.1), 0 1px 2px -1px rgba(15, 23, 42, 0.1);
  --shadow-md: 0 4px 6px -1px rgba(15, 23, 42, 0.1), 0 2px 4px -2px rgba(15, 23, 42, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(15, 23, 42, 0.1), 0 4px 6px -4px rgba(15, 23, 42, 0.1);
  --shadow-xl: 0 20px 25px -5px rgba(15, 23, 42, 0.1), 0 8px 10px -6px rgba(15, 23, 42, 0.1);
  --shadow-2xl: 0 25px 50px -12px rgba(15, 23, 42, 0.25);
  --shadow-inner: inset 0 2px 4px 0 rgba(15, 23, 42, 0.05);
  
  // Colored shadows for interactive elements
  --shadow-primary: 0 4px 14px 0 rgba(14, 165, 233, 0.25);
  --shadow-success: 0 4px 14px 0 rgba(16, 185, 129, 0.25);
  --shadow-error: 0 4px 14px 0 rgba(239, 68, 68, 0.25);
  --shadow-warning: 0 4px 14px 0 rgba(245, 158, 11, 0.25);
  
  // Button-specific shadows
  --shadow-btn: 0 1px 2px 0 rgba(15, 23, 42, 0.05);
  --shadow-btn-hover: 0 4px 8px -2px rgba(15, 23, 42, 0.15);
  --shadow-btn-active: 0 1px 1px 0 rgba(15, 23, 42, 0.1);
}

[data-theme="dark"] {
  --shadow-xs: 0 1px 2px 0 rgba(0, 0, 0, 0.3);
  --shadow-sm: 0 1px 3px 0 rgba(0, 0, 0, 0.4), 0 1px 2px -1px rgba(0, 0, 0, 0.4);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.4), 0 2px 4px -2px rgba(0, 0, 0, 0.4);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.4), 0 4px 6px -4px rgba(0, 0, 0, 0.4);
  --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.5), 0 8px 10px -6px rgba(0, 0, 0, 0.5);
  --shadow-2xl: 0 25px 50px -12px rgba(0, 0, 0, 0.6);
  --shadow-inner: inset 0 2px 4px 0 rgba(0, 0, 0, 0.3);
}
```

### 2.4 Transition Timing

```scss
// Duration tokens
$duration-instant: 0ms;
$duration-fastest: 50ms;
$duration-fast: 150ms;
$duration-normal: 200ms;
$duration-slow: 300ms;
$duration-slower: 400ms;
$duration-slowest: 500ms;

// Easing functions
$ease-linear: linear;
$ease-in: cubic-bezier(0.4, 0, 1, 1);
$ease-out: cubic-bezier(0, 0, 0.2, 1);
$ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
$ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);     // Bouncy
$ease-smooth: cubic-bezier(0.25, 0.1, 0.25, 1);     // Natural
$ease-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);

:root {
  --duration-instant: #{$duration-instant};
  --duration-fastest: #{$duration-fastest};
  --duration-fast: #{$duration-fast};
  --duration-normal: #{$duration-normal};
  --duration-slow: #{$duration-slow};
  --duration-slower: #{$duration-slower};
  --duration-slowest: #{$duration-slowest};
  
  --ease-linear: #{$ease-linear};
  --ease-in: #{$ease-in};
  --ease-out: #{$ease-out};
  --ease-in-out: #{$ease-in-out};
  --ease-spring: #{$ease-spring};
  --ease-smooth: #{$ease-smooth};
  --ease-bounce: #{$ease-bounce};
}
```

### 2.5 Z-Index Scale

```scss
// Layering system to prevent z-index wars
$z-hide: -1;
$z-base: 0;
$z-docked: 10;          // Fixed navigation, sticky elements
$z-dropdown: 100;       // Dropdowns, select menus
$z-sticky: 200;         // Sticky headers
$z-banner: 300;         // Promo banners
$z-overlay: 400;        // Background overlays
$z-modal: 500;          // Modals, dialogs
$z-popover: 600;        // Popovers, tooltips
$z-toast: 700;          // Toast notifications
$z-tooltip: 800;        // Tooltips (highest priority)
$z-max: 9999;           // Emergency override

:root {
  --z-hide: #{$z-hide};
  --z-base: #{$z-base};
  --z-docked: #{$z-docked};
  --z-dropdown: #{$z-dropdown};
  --z-sticky: #{$z-sticky};
  --z-banner: #{$z-banner};
  --z-overlay: #{$z-overlay};
  --z-modal: #{$z-modal};
  --z-popover: #{$z-popover};
  --z-toast: #{$z-toast};
  --z-tooltip: #{$z-tooltip};
  --z-max: #{$z-max};
}
```

---

## 3. Component Systems

### 3.1 Button System

**Current State:** Existing `ButtonComponent` with good foundation. Needs refinement for consistency and additional variants.

#### Variants

| Variant | Use Case | Visual Style |
|---------|----------|--------------|
| `primary` | Primary actions (CTA, submit) | Gradient fill, white text |
| `secondary` | Secondary actions | Subtle fill, dark text |
| `outline` | Tertiary actions | Border only, transparent bg |
| `ghost` | Inline/subtle actions | No border, transparent bg |
| `destructive` | Delete, remove actions | Red fill, white text |
| `success` | Confirm, complete actions | Green fill, white text |
| `link` | Navigation-style actions | Underline on hover |
| `glass` | Over images/gradients | Glassmorphism effect |

#### Sizes

| Size | Padding | Font Size | Min Height | Use Case |
|------|---------|-----------|------------|----------|
| `xs` | 6px 12px | 12px | 28px | Compact UI, tags |
| `sm` | 8px 16px | 12px | 32px | Inline actions |
| `md` | 10px 20px | 14px | 40px | Default |
| `lg` | 14px 28px | 16px | 48px | Prominent actions |
| `xl` | 16px 32px | 18px | 56px | Hero CTAs |

#### Component API

```typescript
// Inputs
variant: InputSignal<ButtonVariant> = input<ButtonVariant>('primary');
size: InputSignal<ButtonSize> = input<ButtonSize>('md');
type: InputSignal<'button' | 'submit' | 'reset'> = input('button');
disabled: InputSignal<boolean> = input(false);
loading: InputSignal<boolean> = input(false);
fullWidth: InputSignal<boolean> = input(false);
iconOnly: InputSignal<boolean> = input(false);
iconPosition: InputSignal<'left' | 'right'> = input<'left' | 'right'>('left');
magnetic: InputSignal<boolean> = input(false);
testId: InputSignal<string> = input('button');

// Outputs
clicked: OutputEmitterRef<MouseEvent> = output();

// Usage
<app-button variant="primary" size="lg" [loading]="isSubmitting()">
  <lucide-icon name="shopping-cart" icon-left />
  Add to Cart
</app-button>
```

#### Accessibility Requirements

- Focus visible outline (2px solid, 2px offset)
- `aria-disabled` when disabled
- `aria-busy="true"` when loading
- Minimum touch target 44x44px on mobile
- Contrast ratio 4.5:1 for text

---

### 3.2 Icon System (Lucide)

**Decision:** Adopt [Lucide](https://lucide.dev/) as the unified icon library.

#### Why Lucide?

| Factor | Lucide | Material Icons | Heroicons |
|--------|--------|----------------|-----------|
| Size | 24px default | 24px | 24px |
| Style | Consistent stroke | Mixed | Solid/Outline |
| Tree-shaking | Yes | No | Yes |
| Icons Count | 1400+ | 2000+ | 300+ |
| Angular Support | Community | Official | Community |
| Customization | Stroke width | Limited | Limited |

#### Implementation Approach

```bash
npm install lucide-angular
```

#### Icon Component API

```typescript
// Wrapper component for consistent sizing and styling
@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
    <lucide-icon
      [name]="name()"
      [size]="computedSize()"
      [strokeWidth]="strokeWidth()"
      [color]="color()"
      [class]="iconClasses()"
      [attr.aria-hidden]="ariaHidden()"
      [attr.role]="ariaHidden() ? null : 'img'"
      [attr.aria-label]="ariaLabel()"
    />
  `
})
export class IconComponent {
  name = input.required<string>();
  size = input<'xs' | 'sm' | 'md' | 'lg' | 'xl'>('md');
  strokeWidth = input<number>(2);
  color = input<string>('currentColor');
  spin = input<boolean>(false);
  ariaHidden = input<boolean>(true);
  ariaLabel = input<string>('');
}

// Size mapping
const sizeMap = {
  xs: 14,
  sm: 16,
  md: 20,
  lg: 24,
  xl: 32
};
```

#### Icon Guidelines

| Context | Size | Stroke Width |
|---------|------|--------------|
| Inline text | `sm` (16px) | 2 |
| Buttons | `md` (20px) | 2 |
| Navigation | `md` (20px) | 1.5 |
| Feature icons | `lg` (24px) | 1.5 |
| Decorative/Hero | `xl` (32px) | 1.5 |

---

### 3.3 Card System

**Current State:** Basic `CardComponent` exists. Need specialized variants for different use cases.

#### Card Variants

| Variant | Component | Use Case |
|---------|-----------|----------|
| Base Card | `CardComponent` | Generic container |
| Product Card | `ProductCardComponent` | Product listings |
| Stat Card | `StatCardComponent` | Dashboard metrics |
| Info Card | `InfoCardComponent` | Feature highlights |
| Testimonial Card | `TestimonialCardComponent` | Reviews/quotes |
| Pricing Card | `PricingCardComponent` | Pricing plans |

#### Base Card API

```typescript
// Inputs
variant: InputSignal<'elevated' | 'outlined' | 'filled'> = input('elevated');
padding: InputSignal<'none' | 'sm' | 'md' | 'lg'> = input('md');
radius: InputSignal<'sm' | 'md' | 'lg' | 'xl'> = input('lg');
interactive: InputSignal<boolean> = input(false);
selected: InputSignal<boolean> = input(false);
loading: InputSignal<boolean> = input(false);

// Content projection slots
<app-card>
  <ng-container card-header>...</ng-container>
  <ng-container card-media>...</ng-container>
  <ng-container card-body>...</ng-container>
  <ng-container card-footer>...</ng-container>
  <ng-container card-actions>...</ng-container>
</app-card>
```

#### Product Card Specifications

```typescript
// Enhanced product card with all required elements
interface ProductCardProps {
  product: Product;
  variant: 'default' | 'compact' | 'horizontal';
  showQuickActions: boolean;
  showCompare: boolean;
  showWishlist: boolean;
  showRating: boolean;
  showStock: boolean;
  showBadges: boolean;
}

// Structure
<app-product-card [product]="product" variant="default">
  <!-- Media section: Image with badges overlay -->
  <!-- Body: Category, Title, Rating, Price -->
  <!-- Actions: Add to cart, Quick view, Compare, Wishlist -->
</app-product-card>
```

---

### 3.4 Form Components

**Current State:** `InputComponent` exists with floating labels. Need to expand to complete form system.

#### Form Components List

| Component | Status | Priority |
|-----------|--------|----------|
| Input | Exists - Enhance | P1 |
| Textarea | New | P1 |
| Select | New | P1 |
| Checkbox | New | P1 |
| Radio | New | P1 |
| Switch/Toggle | New | P1 |
| Range/Slider | New | P2 |
| File Upload | New | P2 |

#### Floating Label Behavior

```
State 1: Empty, Not Focused
┌─────────────────────────────────┐
│  Label Text                     │
└─────────────────────────────────┘

State 2: Focused or Has Value
┌─────────────────────────────────┐
│ Label Text (small, colored)     │
│ User input here_                │
└─────────────────────────────────┘
```

#### Input Component Enhanced API

```typescript
// Types
type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search' | 'date';
type InputSize = 'sm' | 'md' | 'lg';
type ValidationState = 'default' | 'valid' | 'invalid' | 'warning';

// Inputs
type = input<InputType>('text');
size = input<InputSize>('md');
label = input<string>('');
placeholder = input<string>('');
hint = input<string>('');
error = input<string>('');
warning = input<string>('');
disabled = input<boolean>(false);
readonly = input<boolean>(false);
required = input<boolean>(false);
floatingLabel = input<boolean>(true);
showSuccessState = input<boolean>(true);
clearable = input<boolean>(false);
maxLength = input<number | null>(null);
showCharacterCount = input<boolean>(false);
prefixText = input<string>('');
suffixText = input<string>('');
testId = input<string>('input');

// Outputs
valueChange = output<string>();
focused = output<void>();
blurred = output<void>();
cleared = output<void>();
```

#### Select Component API

```typescript
interface SelectOption<T = unknown> {
  value: T;
  label: string;
  disabled?: boolean;
  group?: string;
  icon?: string;
}

// Inputs
options = input.required<SelectOption[]>();
value = input<unknown>(null);
multiple = input<boolean>(false);
searchable = input<boolean>(false);
clearable = input<boolean>(false);
placeholder = input<string>('Select...');
label = input<string>('');
floatingLabel = input<boolean>(true);
error = input<string>('');
disabled = input<boolean>(false);
loading = input<boolean>(false);
virtualScroll = input<boolean>(false); // For large lists

// Outputs
valueChange = output<unknown>();
opened = output<void>();
closed = output<void>();
searchChange = output<string>();
```

#### Checkbox Component API

```typescript
// Inputs
checked = input<boolean>(false);
indeterminate = input<boolean>(false);
label = input<string>('');
description = input<string>('');
disabled = input<boolean>(false);
error = input<string>('');
size = input<'sm' | 'md' | 'lg'>('md');

// Outputs
checkedChange = output<boolean>();
```

#### Radio Group API

```typescript
interface RadioOption<T = unknown> {
  value: T;
  label: string;
  description?: string;
  disabled?: boolean;
}

// Inputs
options = input.required<RadioOption[]>();
value = input<unknown>(null);
name = input<string>('');
orientation = input<'horizontal' | 'vertical'>('vertical');
disabled = input<boolean>(false);
error = input<string>('');

// Outputs
valueChange = output<unknown>();
```

#### Textarea Component API

```typescript
// Inputs
value = input<string>('');
label = input<string>('');
placeholder = input<string>('');
rows = input<number>(3);
minRows = input<number | null>(null);
maxRows = input<number | null>(null);
autoResize = input<boolean>(false);
maxLength = input<number | null>(null);
showCharacterCount = input<boolean>(false);
floatingLabel = input<boolean>(true);
disabled = input<boolean>(false);
error = input<string>('');

// Outputs
valueChange = output<string>();
```

---

### 3.5 Feedback Components

#### Toast Improvements

**Current State:** Good implementation with progress bar and hover pause. Enhancements needed:

```typescript
// Enhanced Toast Types
type ToastPosition = 
  | 'top-left' | 'top-center' | 'top-right'
  | 'bottom-left' | 'bottom-center' | 'bottom-right';

interface ToastConfig {
  position: ToastPosition;
  maxVisible: number;
  stackDirection: 'up' | 'down';
  pauseOnPageHide: boolean;
}

// New features
- Action buttons in toasts
- Promise-based toasts (loading -> success/error)
- Undo action support
- Stack limit with "View all" link
```

#### Badge Component Enhancement

```typescript
// Additional variants
type BadgeVariant = 
  | 'default' | 'primary' | 'secondary' 
  | 'success' | 'warning' | 'error' | 'info'
  | 'outline-primary' | 'outline-secondary'
  | 'dot';

// New inputs
removable = input<boolean>(false);
dot = input<boolean>(false);
pulse = input<boolean>(false); // For notification badges

// Dot badge for status indicators
<app-badge variant="dot" [pulse]="hasNew()">New messages</app-badge>
```

#### Tag Component (New)

```typescript
// For filterable/removable tags
@Component({
  selector: 'app-tag'
})
export class TagComponent {
  label = input.required<string>();
  variant = input<'default' | 'primary' | 'secondary'>('default');
  size = input<'sm' | 'md'>('sm');
  removable = input<boolean>(false);
  selected = input<boolean>(false);
  disabled = input<boolean>(false);
  icon = input<string>('');
  
  removed = output<void>();
  selectedChange = output<boolean>();
}
```

---

### 3.6 Loading States (Skeleton System)

**Current State:** Basic skeleton card exists. Need comprehensive skeleton library.

#### Skeleton Component Types

| Component | Use Case |
|-----------|----------|
| `SkeletonText` | Text placeholders |
| `SkeletonAvatar` | User avatars |
| `SkeletonImage` | Image placeholders |
| `SkeletonButton` | Button placeholders |
| `SkeletonCard` | Generic card layout |
| `SkeletonProductCard` | Product grid items |
| `SkeletonTable` | Table rows |
| `SkeletonList` | List items |

#### Skeleton Base Component

```typescript
@Component({
  selector: 'app-skeleton',
  template: `
    <div 
      [class]="skeletonClasses()"
      [style.width]="width()"
      [style.height]="height()"
      [style.border-radius]="radius()"
      role="status"
      aria-label="Loading..."
    >
      <span class="sr-only">{{ ariaLabel() }}</span>
    </div>
  `
})
export class SkeletonComponent {
  variant = input<'text' | 'circular' | 'rectangular'>('rectangular');
  width = input<string>('100%');
  height = input<string>('1rem');
  radius = input<string>('var(--radius-md)');
  animation = input<'pulse' | 'wave' | 'none'>('wave');
  ariaLabel = input<string>('Loading content');
}
```

#### Skeleton Text Component

```typescript
@Component({
  selector: 'app-skeleton-text'
})
export class SkeletonTextComponent {
  lines = input<number>(1);
  widths = input<(string | number)[]>([]);  // Custom width per line
  lastLineWidth = input<string>('60%');     // Default for last line
  spacing = input<string>('0.5rem');
  fontSize = input<'sm' | 'md' | 'lg'>('md');
}

// Usage
<app-skeleton-text [lines]="3" lastLineWidth="40%" />
```

#### Skeleton Animation Styles

```scss
.skeleton {
  background: var(--color-bg-tertiary);
  position: relative;
  overflow: hidden;
}

.skeleton-wave::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.3),
    transparent
  );
  animation: skeleton-wave 1.5s infinite;
}

.skeleton-pulse {
  animation: skeleton-pulse 1.5s ease-in-out infinite;
}

@keyframes skeleton-wave {
  0% { transform: translateX(-100%); }
  100% { transform: translateX(100%); }
}

@keyframes skeleton-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

@media (prefers-reduced-motion: reduce) {
  .skeleton-wave::after,
  .skeleton-pulse {
    animation: none;
  }
}
```

---

### 3.7 Modal/Dialog System

**Current State:** Good modal with focus trap. Need specialized dialogs.

#### Dialog Types

| Type | Component | Use Case |
|------|-----------|----------|
| Modal | `ModalComponent` | General purpose |
| Confirmation | `ConfirmDialogComponent` | Yes/No decisions |
| Alert | `AlertDialogComponent` | Important messages |
| Form | `FormDialogComponent` | Inline forms |
| Gallery | `GalleryModalComponent` | Image lightbox |
| Drawer | `DrawerComponent` | Side panel |

#### Confirmation Dialog API

```typescript
interface ConfirmDialogConfig {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'default' | 'destructive' | 'warning';
  confirmButtonVariant?: ButtonVariant;
  icon?: string;
}

// Service-based usage
const result = await this.dialogService.confirm({
  title: 'Delete Product',
  message: 'Are you sure you want to delete this product? This action cannot be undone.',
  confirmText: 'Delete',
  cancelText: 'Cancel',
  variant: 'destructive'
});

if (result) {
  // User confirmed
}
```

#### Dialog Service

```typescript
@Injectable({ providedIn: 'root' })
export class DialogService {
  confirm(config: ConfirmDialogConfig): Promise<boolean>;
  alert(config: AlertDialogConfig): Promise<void>;
  prompt(config: PromptDialogConfig): Promise<string | null>;
  open<T, R>(component: Type<T>, config?: DialogConfig): DialogRef<R>;
  closeAll(): void;
}

interface DialogRef<R> {
  close(result?: R): void;
  afterClosed(): Observable<R | undefined>;
  afterOpened(): Observable<void>;
}
```

#### Drawer Component

```typescript
@Component({
  selector: 'app-drawer'
})
export class DrawerComponent {
  isOpen = input<boolean>(false);
  position = input<'left' | 'right' | 'top' | 'bottom'>('right');
  size = input<'sm' | 'md' | 'lg' | 'full'>('md');
  overlay = input<boolean>(true);
  closeOnOverlayClick = input<boolean>(true);
  closeOnEscape = input<boolean>(true);
  
  closed = output<void>();
}

// Size mapping
const drawerSizes = {
  sm: '320px',
  md: '400px',
  lg: '560px',
  full: '100%'
};
```

---

### 3.8 Navigation Components

#### Tabs Component

```typescript
interface Tab {
  id: string;
  label: string;
  icon?: string;
  disabled?: boolean;
  badge?: string | number;
}

@Component({
  selector: 'app-tabs'
})
export class TabsComponent {
  tabs = input.required<Tab[]>();
  activeTab = input<string>('');
  variant = input<'default' | 'pills' | 'underline'>('default');
  size = input<'sm' | 'md' | 'lg'>('md');
  fullWidth = input<boolean>(false);
  
  tabChange = output<string>();
}

// Usage
<app-tabs [tabs]="tabs" [activeTab]="activeTab()" (tabChange)="onTabChange($event)">
  <ng-template tabContent="details">
    <!-- Tab content -->
  </ng-template>
</app-tabs>
```

#### Breadcrumb Enhancement

```typescript
interface BreadcrumbItem {
  label: string;
  href?: string;
  icon?: string;
  current?: boolean;
}

@Component({
  selector: 'app-breadcrumb'
})
export class BreadcrumbComponent {
  items = input.required<BreadcrumbItem[]>();
  separator = input<'slash' | 'chevron' | 'arrow'>('chevron');
  maxItems = input<number | null>(null);  // Collapse middle items
  homeIcon = input<boolean>(true);
}
```

#### Pagination Component

```typescript
@Component({
  selector: 'app-pagination'
})
export class PaginationComponent {
  currentPage = input.required<number>();
  totalPages = input.required<number>();
  totalItems = input<number>(0);
  pageSize = input<number>(12);
  pageSizeOptions = input<number[]>([12, 24, 48]);
  showPageSize = input<boolean>(false);
  showFirstLast = input<boolean>(true);
  showPageNumbers = input<boolean>(true);
  maxVisiblePages = input<number>(5);
  variant = input<'default' | 'simple' | 'minimal'>('default');
  
  pageChange = output<number>();
  pageSizeChange = output<number>();
}

// Variants
// default: Full pagination with numbers
// simple: Previous/Next only with page indicator
// minimal: Just Previous/Next buttons
```

---

## 4. Task List

### Phase 0: Foundation (Design Tokens)

- [x] **TASK-21E-001**: Create `_tokens.scss` with spacing scale CSS custom properties
- [x] **TASK-21E-002**: Create border radius token system
- [x] **TASK-21E-003**: Create shadow token system with theme support
- [x] **TASK-21E-004**: Create transition timing tokens
- [x] **TASK-21E-005**: Create z-index layering system

### Phase 1: Button System

- [x] **TASK-21E-006**: Refactor ButtonComponent to use design tokens
- [x] **TASK-21E-007**: Add `link` button variant
- [x] **TASK-21E-008**: Add `destructive` button variant (rename from `danger`)
- [x] **TASK-21E-009**: Enhance button loading state with better spinner
- [ ] **TASK-21E-010**: Add button group component (`ButtonGroupComponent`)
- [x] **TASK-21E-011**: Write comprehensive button component tests

### Phase 2: Icon System

- [x] **TASK-21E-012**: Install and configure `lucide-angular`
- [ ] **TASK-21E-013**: Create `IconComponent` wrapper with size/color props
- [ ] **TASK-21E-014**: Replace all inline SVGs with Lucide icons
- [ ] **TASK-21E-015**: Create icon usage documentation and guidelines

### Phase 3: Card System

- [ ] **TASK-21E-016**: Enhance base `CardComponent` with new variants
- [ ] **TASK-21E-017**: Create `StatCardComponent` for dashboard metrics
- [ ] **TASK-21E-018**: Create `InfoCardComponent` for feature highlights
- [ ] **TASK-21E-019**: Refactor `ProductCardComponent` to use card system
- [ ] **TASK-21E-020**: Create `PricingCardComponent` for pricing plans

### Phase 4: Form Components

- [ ] **TASK-21E-021**: Enhance `InputComponent` with clearable, character count
- [ ] **TASK-21E-022**: Create `TextareaComponent` with auto-resize
- [ ] **TASK-21E-023**: Create `SelectComponent` with search and groups
- [ ] **TASK-21E-024**: Create `CheckboxComponent` with indeterminate state
- [ ] **TASK-21E-025**: Create `RadioGroupComponent`
- [ ] **TASK-21E-026**: Create `SwitchComponent` (toggle)
- [ ] **TASK-21E-027**: Create `RangeSliderComponent`
- [ ] **TASK-21E-028**: Create `FileUploadComponent` with drag-drop

### Phase 5: Feedback Components

- [ ] **TASK-21E-029**: Enhance toast with action buttons and promise support
- [ ] **TASK-21E-030**: Add toast position configuration
- [ ] **TASK-21E-031**: Enhance `BadgeComponent` with dot and pulse variants
- [ ] **TASK-21E-032**: Create `TagComponent` for filters
- [ ] **TASK-21E-033**: Enhance `AlertComponent` with more variants

### Phase 6: Loading States

- [ ] **TASK-21E-034**: Create base `SkeletonComponent`
- [ ] **TASK-21E-035**: Create `SkeletonTextComponent`
- [ ] **TASK-21E-036**: Create `SkeletonAvatarComponent`
- [ ] **TASK-21E-037**: Refactor `SkeletonProductCardComponent` to use base skeleton
- [ ] **TASK-21E-038**: Create `SkeletonTableComponent`

### Phase 7: Modal/Dialog System

- [ ] **TASK-21E-039**: Create `DialogService` for programmatic dialogs
- [ ] **TASK-21E-040**: Create `ConfirmDialogComponent`
- [ ] **TASK-21E-041**: Create `DrawerComponent` for side panels
- [ ] **TASK-21E-042**: Enhance `ModalComponent` with size variants

### Phase 8: Navigation Components

- [ ] **TASK-21E-043**: Create `TabsComponent` with multiple variants
- [ ] **TASK-21E-044**: Enhance `BreadcrumbComponent` with collapse behavior
- [ ] **TASK-21E-045**: Create `PaginationComponent` with variants
- [ ] **TASK-21E-046**: Create `StepsComponent` for multi-step flows

### Phase 9: Documentation & Polish

- [ ] **TASK-21E-047**: Create component documentation with examples
- [ ] **TASK-21E-048**: Create component usage guidelines
- [ ] **TASK-21E-049**: Final accessibility audit and fixes

---

## 5. Technical Specifications

### 5.1 Component API Design Principles

| Principle | Implementation |
|-----------|----------------|
| Signal Inputs | Use `input()` for all component inputs |
| Output Emitters | Use `output()` for all events |
| Standalone | All components must be standalone |
| Type Safety | Export types for all variants/options |
| Default Values | Sensible defaults for all optional inputs |
| Testability | All components must have `testId` input |

### 5.2 Standard Input Patterns

```typescript
// All components should follow this pattern
@Component({...})
export class ExampleComponent {
  // Required inputs first
  readonly data = input.required<DataType>();
  
  // Optional inputs with defaults
  readonly variant = input<VariantType>('default');
  readonly size = input<SizeType>('md');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
  
  // Accessibility
  readonly ariaLabel = input<string>('');
  readonly testId = input<string>('example');
  
  // Events
  readonly clicked = output<MouseEvent>();
  readonly changed = output<ValueType>();
}
```

### 5.3 Accessibility Requirements

| Requirement | Implementation |
|-------------|----------------|
| Focus Management | All interactive elements focusable, visible focus ring |
| Keyboard Navigation | Arrow keys for lists, Escape to close, Enter to activate |
| Screen Readers | Proper ARIA roles, labels, and live regions |
| Color Contrast | Minimum 4.5:1 for text, 3:1 for large text |
| Reduced Motion | Respect `prefers-reduced-motion` media query |
| Touch Targets | Minimum 44x44px on mobile |

#### ARIA Patterns by Component

| Component | ARIA Role | Required Attributes |
|-----------|-----------|---------------------|
| Button | button | aria-disabled, aria-busy, aria-pressed (toggle) |
| Modal | dialog | aria-modal, aria-labelledby, aria-describedby |
| Alert | alert | aria-live="polite" |
| Toast | status | aria-live="polite", aria-atomic="true" |
| Tabs | tablist/tab/tabpanel | aria-selected, aria-controls |
| Select | listbox/option | aria-expanded, aria-selected, aria-activedescendant |
| Checkbox | checkbox | aria-checked (true/false/mixed) |
| Radio | radiogroup/radio | aria-checked |

### 5.4 File Structure

```
src/ClimaSite.Web/src/app/shared/
├── components/
│   ├── button/
│   │   ├── button.component.ts
│   │   ├── button.component.spec.ts
│   │   ├── button-group.component.ts
│   │   └── index.ts
│   ├── icon/
│   │   ├── icon.component.ts
│   │   └── index.ts
│   ├── card/
│   │   ├── card.component.ts
│   │   ├── stat-card.component.ts
│   │   ├── info-card.component.ts
│   │   └── index.ts
│   ├── form/
│   │   ├── input.component.ts
│   │   ├── textarea.component.ts
│   │   ├── select.component.ts
│   │   ├── checkbox.component.ts
│   │   ├── radio-group.component.ts
│   │   ├── switch.component.ts
│   │   └── index.ts
│   ├── feedback/
│   │   ├── toast/
│   │   ├── alert.component.ts
│   │   ├── badge.component.ts
│   │   ├── tag.component.ts
│   │   └── index.ts
│   ├── skeleton/
│   │   ├── skeleton.component.ts
│   │   ├── skeleton-text.component.ts
│   │   ├── skeleton-product-card.component.ts
│   │   └── index.ts
│   ├── modal/
│   │   ├── modal.component.ts
│   │   ├── confirm-dialog.component.ts
│   │   ├── drawer.component.ts
│   │   ├── dialog.service.ts
│   │   └── index.ts
│   └── navigation/
│       ├── tabs.component.ts
│       ├── breadcrumb.component.ts
│       ├── pagination.component.ts
│       └── index.ts
├── styles/
│   ├── _tokens.scss
│   └── _components.scss
└── index.ts
```

---

## 6. Dependencies

### 6.1 This Plan Enables

| Plan | Dependency |
|------|------------|
| Plan 21A (Hero) | Button, Icon, Card components |
| Plan 21B (Product Grid) | Card, Skeleton, Pagination components |
| Plan 21C (Navigation) | Icon, Tabs, Breadcrumb components |
| Plan 21D (Checkout) | Form components, Button, Modal |
| Plan 21F (Mobile) | All components with responsive design |

### 6.2 External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `lucide-angular` | ^0.300.0 | Icon library |
| `@angular/cdk` | ^19.0.0 | Overlay, A11y utilities |

### 6.3 Migration Path

1. **Phase 1**: Add design tokens without breaking existing components
2. **Phase 2**: Create new components alongside existing ones
3. **Phase 3**: Migrate features to use new components
4. **Phase 4**: Remove deprecated components

---

## 7. Testing Checklist

### 7.1 Unit Tests (Per Component)

- [ ] Renders correctly with default props
- [ ] Renders all variants correctly
- [ ] Handles all sizes correctly
- [ ] Disabled state prevents interactions
- [ ] Loading state shows indicator
- [ ] Emits correct events
- [ ] Handles edge cases (empty data, null values)

### 7.2 Accessibility Tests

- [ ] Keyboard navigation works
- [ ] Focus is visible and trapped (modals)
- [ ] Screen reader announces correctly
- [ ] Color contrast meets WCAG AA
- [ ] Touch targets are 44x44px minimum

### 7.3 Visual Tests

- [ ] Light theme renders correctly
- [ ] Dark theme renders correctly
- [ ] Responsive at all breakpoints
- [ ] Animations work (and respect reduced motion)
- [ ] Loading states display correctly

### 7.4 Integration Tests

- [ ] Components work together (Button in Card)
- [ ] Form components work with Angular forms
- [ ] Modals trap focus correctly
- [ ] Toast notifications stack properly

---

## 8. Success Criteria

### 8.1 Completion Criteria

| Criteria | Measurement |
|----------|-------------|
| All tasks completed | 49/49 tasks checked |
| Test coverage > 90% | Jest coverage report |
| No accessibility violations | axe-core audit |
| Documentation complete | All components documented |
| No TypeScript errors | `ng build` succeeds |
| Both themes work | Visual verification |

### 8.2 Quality Gates

Before marking this plan complete:

1. [ ] All component unit tests pass
2. [ ] Accessibility audit passes (Lighthouse 100)
3. [ ] Bundle size impact < 50KB
4. [ ] No console errors in dev mode
5. [ ] All components work in light/dark themes
6. [ ] All components work in EN/BG/DE
7. [ ] Documentation reviewed and approved

---

## Appendix A: Component Quick Reference

```typescript
// Import all components
import {
  ButtonComponent,
  IconComponent,
  CardComponent,
  InputComponent,
  SelectComponent,
  CheckboxComponent,
  RadioGroupComponent,
  TextareaComponent,
  SwitchComponent,
  ToastService,
  AlertComponent,
  BadgeComponent,
  TagComponent,
  ModalComponent,
  DialogService,
  DrawerComponent,
  TabsComponent,
  BreadcrumbComponent,
  PaginationComponent,
  SkeletonComponent,
  SkeletonTextComponent
} from '@shared/components';
```

## Appendix B: Design Token Quick Reference

```scss
// Spacing: --space-{0|0-5|1|1-5|2|2-5|3|3-5|4|5|6|7|8|9|10|11|12|14|16|20|24|28|32}
// Radius: --radius-{none|sm|md|lg|xl|2xl|3xl|full}
// Shadow: --shadow-{xs|sm|md|lg|xl|2xl|inner|primary|success|error|warning}
// Duration: --duration-{instant|fastest|fast|normal|slow|slower|slowest}
// Easing: --ease-{linear|in|out|in-out|spring|smooth|bounce}
// Z-Index: --z-{hide|base|docked|dropdown|sticky|banner|overlay|modal|popover|toast|tooltip|max}
```

---

*Last Updated: January 2026*
*Plan Version: 1.0*
*Status: Ready for Implementation*
