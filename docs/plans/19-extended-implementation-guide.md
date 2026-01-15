# Plan 19: Extended Implementation Guide

## Overview

This document provides detailed step-by-step implementation guides for all pending features in ClimaSite. Each section contains granular tasks, file changes, code snippets, and testing requirements.

---

# Part 1: Bug Fixes and Enhancements (Plan 18 Extended)

## Implementation Order and Dependencies

```
AUTH-001 (Auth Fix)          ─┐
                               ├──► NAV-001 (Category Nav) ──► HOME-001 (Home Redesign)
THEME-001 (Theme Fix)        ─┤                              │
                               │                              └──► FILTER-001 (Filters)
I18N-001/002 (Translations)  ─┘

NAV-002 (Wishlist) ────────────► Depends on Plan 13 completion
```

---

## AUTH-001: Fix Orders Page Logout Bug (CRITICAL)

### Problem Analysis

When a logged-in user navigates to `/account/orders`, they get logged out because:
1. `AuthService` constructor calls `loadUserFromToken()` async
2. `AuthGuard` checks `isAuthenticated()` synchronously
3. On page refresh, guard runs before auth state is restored

### Step-by-Step Implementation

#### Step 1: Add Auth Ready Signal to AuthService

**File:** `src/ClimaSite.Web/src/app/auth/services/auth.service.ts`

```typescript
// Add these signals to AuthService class
private readonly _authReady = signal<boolean>(false);
private readonly _hasToken = signal<boolean>(false);
private readonly _user = signal<User | null>(null);

// Public readonly signals
readonly authReady = this._authReady.asReadonly();
readonly isAuthenticated = computed(() => this._hasToken() && this._authReady());
readonly user = this._user.asReadonly();

constructor(
  private http: HttpClient,
  private router: Router,
  private tokenService: TokenService
) {
  this.initializeAuth();
}

private async initializeAuth(): Promise<void> {
  const token = this.tokenService.getAccessToken();

  if (token) {
    // Set hasToken immediately - don't wait for API
    this._hasToken.set(true);

    try {
      // Validate token with API
      const user = await firstValueFrom(
        this.http.get<User>('/api/v1/auth/me')
      );
      this._user.set(user);
    } catch (error) {
      // Token invalid - clear it
      this.tokenService.clearTokens();
      this._hasToken.set(false);
      this._user.set(null);
    }
  }

  // Mark auth as ready regardless of token validity
  this._authReady.set(true);
}
```

#### Step 2: Update AuthGuard to Wait for Auth Ready

**File:** `src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts`

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs/operators';
import { Observable } from 'rxjs';

export const authGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for auth to be ready before checking
  return toObservable(authService.authReady).pipe(
    filter(ready => ready === true),
    take(1),
    map(() => {
      if (authService.isAuthenticated()) {
        return true;
      }

      // Store return URL and redirect to login
      return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl: state.url }
      });
    })
  );
};
```

#### Step 3: Add APP_INITIALIZER for Auth (Optional Enhancement)

**File:** `src/ClimaSite.Web/src/app/app.config.ts`

```typescript
import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { AuthService } from './auth/services/auth.service';

function initializeAuth(authService: AuthService): () => Promise<void> {
  return () => new Promise((resolve) => {
    // Subscribe to authReady and resolve when true
    const subscription = toObservable(authService.authReady).subscribe(ready => {
      if (ready) {
        subscription.unsubscribe();
        resolve();
      }
    });
  });
}

export const appConfig: ApplicationConfig = {
  providers: [
    // ... other providers
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true
    }
  ]
};
```

### Unit Tests

**File:** `src/ClimaSite.Web/src/app/auth/services/auth.service.spec.ts`

```typescript
describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokenService: jasmine.SpyObj<TokenService>;

  beforeEach(() => {
    tokenService = jasmine.createSpyObj('TokenService',
      ['getAccessToken', 'clearTokens', 'setTokens']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: TokenService, useValue: tokenService }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should set hasToken immediately when token exists', fakeAsync(() => {
    tokenService.getAccessToken.and.returnValue('valid-token');

    service = TestBed.inject(AuthService);

    // hasToken should be true immediately
    expect(service['_hasToken']()).toBe(true);

    // But authReady should be false until API responds
    expect(service.authReady()).toBe(false);

    // Mock API response
    const req = httpMock.expectOne('/api/v1/auth/me');
    req.flush({ id: '1', email: 'test@test.com' });

    tick();

    expect(service.authReady()).toBe(true);
    expect(service.isAuthenticated()).toBe(true);
  }));

  it('should clear token if API validation fails', fakeAsync(() => {
    tokenService.getAccessToken.and.returnValue('invalid-token');

    service = TestBed.inject(AuthService);

    const req = httpMock.expectOne('/api/v1/auth/me');
    req.error(new ErrorEvent('401'));

    tick();

    expect(tokenService.clearTokens).toHaveBeenCalled();
    expect(service['_hasToken']()).toBe(false);
    expect(service.authReady()).toBe(true);
  }));
});
```

**File:** `src/ClimaSite.Web/src/app/auth/guards/auth.guard.spec.ts`

```typescript
describe('authGuard', () => {
  it('should wait for authReady before allowing navigation', fakeAsync(() => {
    const authService = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);

    // Set authReady to false initially
    authService['_authReady'].set(false);
    authService['_hasToken'].set(true);

    let result: boolean | UrlTree | null = null;

    TestBed.runInInjectionContext(() => {
      authGuard(mockRoute, mockState).subscribe(r => result = r);
    });

    // Guard should not have resolved yet
    expect(result).toBeNull();

    // Now set authReady to true
    authService['_authReady'].set(true);
    tick();

    // Guard should now allow access
    expect(result).toBe(true);
  }));

  it('should redirect to login when not authenticated', fakeAsync(() => {
    const authService = TestBed.inject(AuthService);

    authService['_authReady'].set(true);
    authService['_hasToken'].set(false);

    let result: boolean | UrlTree | null = null;

    TestBed.runInInjectionContext(() => {
      authGuard(mockRoute, mockState).subscribe(r => result = r);
    });

    tick();

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toContain('/auth/login');
  }));
});
```

### Acceptance Checklist

- [ ] Logged-in user can refresh `/account/orders` without logout
- [ ] Auth state persists correctly across page refreshes
- [ ] AuthGuard waits for auth initialization before redirecting
- [ ] Token validation happens in background
- [ ] Invalid tokens are cleared properly
- [ ] Unit tests pass
- [ ] Works in both light and dark themes
- [ ] Works in all languages (EN, BG, DE)

---

## NAV-001: Fix Category Navigation (CRITICAL)

### Problem Analysis

Mega menu categories use query params `[queryParams]="{category: slug}"` but `ProductListComponent` reads from route params `categorySlug`, not query params.

### Step-by-Step Implementation

#### Step 1: Add Category Route

**File:** `src/ClimaSite.Web/src/app/app.routes.ts`

```typescript
export const routes: Routes = [
  // ... existing routes

  // Add new category route BEFORE the generic products route
  {
    path: 'products/category/:categorySlug',
    loadComponent: () => import('./features/products/product-list/product-list.component')
      .then(m => m.ProductListComponent),
    title: 'Products - ClimaSite'
  },

  // Keep existing products route for non-category browsing
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-list/product-list.component')
      .then(m => m.ProductListComponent),
    title: 'Products - ClimaSite'
  },
];
```

#### Step 2: Update Mega Menu Links

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.html`

```html
<!-- Before -->
<a [routerLink]="['/products']" [queryParams]="{category: category.slug}">

<!-- After -->
<a [routerLink]="['/products/category', category.slug]">
  <span class="category-icon">
    <ng-container *ngIf="category.icon" [innerHTML]="getCategoryIcon(category.icon)"></ng-container>
  </span>
  <span class="category-name">{{ getCategoryName(category) | translate }}</span>
</a>

<!-- Subcategory links -->
<a *ngFor="let subcat of category.children"
   [routerLink]="['/products/category', subcat.slug]"
   class="subcategory-link"
   data-testid="subcategory-link">
  {{ getCategoryName(subcat) | translate }}
</a>
```

#### Step 3: Update ProductListComponent

**File:** `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`

```typescript
import { Component, inject, OnInit, signal, computed, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, distinctUntilChanged, switchMap, combineLatest } from 'rxjs';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
  standalone: true,
  imports: [/* imports */]
})
export class ProductListComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);

  // Get category from route param OR query param (backward compatibility)
  categorySlug = toSignal(
    this.route.paramMap.pipe(
      map(params => params.get('categorySlug')),
      switchMap(routeSlug => {
        if (routeSlug) return of(routeSlug);
        // Fallback to query param for backward compatibility
        return this.route.queryParamMap.pipe(
          map(query => query.get('category'))
        );
      }),
      distinctUntilChanged()
    )
  );

  // Current category details
  currentCategory = computed(() => {
    const slug = this.categorySlug();
    if (!slug) return null;
    return this.categoryService.getCategoryBySlug(slug);
  });

  // Breadcrumb trail
  breadcrumbs = computed(() => {
    const category = this.currentCategory();
    if (!category) return [{ label: 'Products', url: '/products' }];

    const trail = [{ label: 'Home', url: '/' }];

    // Add parent categories if they exist
    if (category.parent) {
      trail.push({
        label: category.parent.name,
        url: `/products/category/${category.parent.slug}`
      });
    }

    trail.push({
      label: category.name,
      url: `/products/category/${category.slug}`
    });

    return trail;
  });

  // Products filtered by category
  products = signal<Product[]>([]);
  loading = signal(false);
  totalCount = signal(0);

  constructor() {
    // React to category changes
    effect(() => {
      const slug = this.categorySlug();
      this.loadProducts(slug);
    });
  }

  private async loadProducts(categorySlug: string | null | undefined): Promise<void> {
    this.loading.set(true);

    try {
      const params: ProductFilterParams = {
        categorySlug: categorySlug || undefined,
        page: 1,
        pageSize: 12
      };

      const result = await firstValueFrom(
        this.productService.getProducts(params)
      );

      this.products.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch (error) {
      console.error('Failed to load products:', error);
    } finally {
      this.loading.set(false);
    }
  }
}
```

#### Step 4: Update Home Page Category Cards

**File:** `src/ClimaSite.Web/src/app/features/home/home.component.html`

```html
<!-- Category Cards Section -->
<section class="category-cards">
  <h2>{{ 'home.shopByCategory' | translate }}</h2>
  <div class="category-grid">
    <a *ngFor="let category of mainCategories()"
       [routerLink]="['/products/category', category.slug]"
       class="category-card"
       data-testid="category-card">
      <div class="category-icon">
        <ng-container [innerHTML]="getCategoryIcon(category.icon)"></ng-container>
      </div>
      <span class="category-name">{{ getCategoryName(category) | translate }}</span>
    </a>
  </div>
</section>
```

### E2E Tests

**File:** `tests/ClimaSite.E2E/tests/navigation/category-navigation.spec.ts`

```typescript
import { test, expect } from '@playwright/test';
import { TestDataFactory } from '../fixtures/test-data-factory';

test.describe('Category Navigation', () => {
  let factory: TestDataFactory;

  test.beforeAll(async ({ request }) => {
    factory = new TestDataFactory(request);

    // Create test categories and products
    const category = await factory.createCategory({
      name: 'Air Conditioning',
      slug: 'air-conditioning'
    });

    await factory.createProduct({
      name: 'Test AC Unit',
      categoryId: category.id,
      price: 999.99
    });
  });

  test.afterAll(async () => {
    await factory.cleanup();
  });

  test('clicking category in mega menu shows filtered products', async ({ page }) => {
    await page.goto('/');

    // Hover over Products nav item
    await page.hover('[data-testid="mega-menu-trigger"]');

    // Wait for mega menu to appear
    await expect(page.locator('[data-testid="mega-menu"]')).toBeVisible();

    // Click Air Conditioning category
    await page.click('text=Air Conditioning');

    // Verify URL
    await expect(page).toHaveURL(/\/products\/category\/air-conditioning/);

    // Verify breadcrumb shows category
    await expect(page.locator('[data-testid="breadcrumb"]')).toContainText('Air Conditioning');

    // Verify products are filtered
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible();
  });

  test('clicking category card on home page navigates correctly', async ({ page }) => {
    await page.goto('/');

    // Click category card
    await page.click('[data-testid="category-card"]:has-text("Air Conditioning")');

    // Verify URL
    await expect(page).toHaveURL(/\/products\/category\/air-conditioning/);
  });

  test('subcategory navigation filters correctly', async ({ page }) => {
    await page.goto('/');

    // Hover mega menu
    await page.hover('[data-testid="mega-menu-trigger"]');
    await page.hover('text=Air Conditioning');

    // Click subcategory
    await page.click('[data-testid="subcategory-link"]:has-text("Wall-Mounted AC")');

    // Verify URL
    await expect(page).toHaveURL(/\/products\/category\/wall-mounted-ac/);
  });
});
```

### Acceptance Checklist

- [ ] Clicking "Air Conditioning" shows only air conditioning products
- [ ] Clicking subcategories filters products correctly
- [ ] Breadcrumb shows correct category hierarchy
- [ ] URL reflects the selected category (`/products/category/slug`)
- [ ] Old query param URLs still work (backward compatibility)
- [ ] Category page title updates correctly
- [ ] E2E tests pass
- [ ] Works in all themes and languages

---

## THEME-001: Fix White Theme Contrast Issues (HIGH)

### Step-by-Step Implementation

#### Step 1: Audit Current Color Variables

**File:** `src/ClimaSite.Web/src/styles/_colors.scss`

```scss
// =============================================================================
// LIGHT THEME (Default)
// =============================================================================
:root {
  // ==========================================================================
  // Background Colors
  // ==========================================================================
  --color-bg-primary: #ffffff;
  --color-bg-secondary: #f9fafb;      // gray-50 - subtle background
  --color-bg-tertiary: #f3f4f6;       // gray-100 - cards, inputs
  --color-bg-hover: #e5e7eb;          // gray-200 - hover states
  --color-bg-active: #d1d5db;         // gray-300 - active states
  --color-bg-card: #ffffff;
  --color-bg-input: #ffffff;

  // ==========================================================================
  // Text Colors - CRITICAL: Ensure sufficient contrast
  // ==========================================================================
  --color-text-primary: #111827;      // gray-900 - main text (15.8:1 ratio)
  --color-text-secondary: #374151;    // gray-700 - secondary text (10.6:1 ratio)
  --color-text-tertiary: #4b5563;     // gray-600 - muted text (7.5:1 ratio)
  --color-text-placeholder: #6b7280;  // gray-500 - placeholders (5.3:1 ratio) ✓ WCAG AA
  --color-text-disabled: #9ca3af;     // gray-400 - disabled text (3.9:1 ratio) - use larger font
  --color-text-inverse: #ffffff;      // white - on dark backgrounds

  // ==========================================================================
  // Border Colors
  // ==========================================================================
  --color-border-primary: #e5e7eb;    // gray-200 - default borders
  --color-border-secondary: #d1d5db;  // gray-300 - emphasized borders
  --color-border-focus: #3b82f6;      // blue-500 - focus rings
  --color-border-input: #d1d5db;      // gray-300 - form inputs

  // ==========================================================================
  // Primary Brand Colors
  // ==========================================================================
  --color-primary: #2563eb;           // blue-600
  --color-primary-hover: #1d4ed8;     // blue-700
  --color-primary-active: #1e40af;    // blue-800
  --color-primary-light: #dbeafe;     // blue-100
  --color-primary-50: #eff6ff;        // blue-50

  // ==========================================================================
  // Secondary/Accent Colors
  // ==========================================================================
  --color-accent: #ea580c;            // orange-600
  --color-accent-hover: #c2410c;      // orange-700
  --color-accent-light: #ffedd5;      // orange-100

  // ==========================================================================
  // Semantic Colors
  // ==========================================================================
  --color-success: #16a34a;           // green-600
  --color-success-light: #dcfce7;     // green-100
  --color-success-text: #15803d;      // green-700 (for text on light bg)

  --color-warning: #d97706;           // amber-600
  --color-warning-light: #fef3c7;     // amber-100
  --color-warning-text: #b45309;      // amber-700

  --color-error: #dc2626;             // red-600
  --color-error-light: #fee2e2;       // red-100
  --color-error-text: #b91c1c;        // red-700

  --color-info: #0284c7;              // sky-600
  --color-info-light: #e0f2fe;        // sky-100
  --color-info-text: #0369a1;         // sky-700

  // ==========================================================================
  // Component-Specific Colors
  // ==========================================================================

  // Buttons
  --btn-primary-bg: var(--color-primary);
  --btn-primary-text: #ffffff;
  --btn-primary-hover-bg: var(--color-primary-hover);

  --btn-secondary-bg: #ffffff;
  --btn-secondary-text: var(--color-text-primary);
  --btn-secondary-border: var(--color-border-secondary);
  --btn-secondary-hover-bg: var(--color-bg-tertiary);

  --btn-ghost-text: var(--color-text-secondary);
  --btn-ghost-hover-bg: var(--color-bg-hover);

  // Cards
  --card-bg: var(--color-bg-card);
  --card-border: var(--color-border-primary);
  --card-shadow: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
  --card-hover-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);

  // Forms
  --input-bg: var(--color-bg-input);
  --input-border: var(--color-border-input);
  --input-text: var(--color-text-primary);
  --input-placeholder: var(--color-text-placeholder);
  --input-focus-border: var(--color-border-focus);
  --input-focus-ring: rgba(59, 130, 246, 0.3);

  // Navigation
  --nav-bg: var(--color-bg-primary);
  --nav-text: var(--color-text-primary);
  --nav-text-hover: var(--color-primary);
  --nav-active-bg: var(--color-primary-50);

  // Badges
  --badge-default-bg: var(--color-bg-tertiary);
  --badge-default-text: var(--color-text-secondary);
  --badge-primary-bg: var(--color-primary-light);
  --badge-primary-text: var(--color-primary);

  // Dropdown/Menu
  --dropdown-bg: var(--color-bg-primary);
  --dropdown-border: var(--color-border-primary);
  --dropdown-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
  --dropdown-item-hover-bg: var(--color-bg-hover);
}

// =============================================================================
// DARK THEME
// =============================================================================
[data-theme="dark"] {
  // Background Colors
  --color-bg-primary: #111827;        // gray-900
  --color-bg-secondary: #1f2937;      // gray-800
  --color-bg-tertiary: #374151;       // gray-700
  --color-bg-hover: #4b5563;          // gray-600
  --color-bg-active: #6b7280;         // gray-500
  --color-bg-card: #1f2937;
  --color-bg-input: #374151;

  // Text Colors
  --color-text-primary: #f9fafb;      // gray-50
  --color-text-secondary: #e5e7eb;    // gray-200
  --color-text-tertiary: #d1d5db;     // gray-300
  --color-text-placeholder: #9ca3af;  // gray-400
  --color-text-disabled: #6b7280;     // gray-500
  --color-text-inverse: #111827;      // gray-900

  // Border Colors
  --color-border-primary: #374151;    // gray-700
  --color-border-secondary: #4b5563;  // gray-600
  --color-border-input: #4b5563;

  // Primary Brand Colors
  --color-primary: #3b82f6;           // blue-500
  --color-primary-hover: #60a5fa;     // blue-400
  --color-primary-active: #2563eb;    // blue-600
  --color-primary-light: #1e3a5f;     // custom dark blue

  // Semantic Colors - adjusted for dark theme
  --color-success: #22c55e;           // green-500
  --color-success-light: #14532d;     // green-900
  --color-success-text: #4ade80;      // green-400

  --color-warning: #f59e0b;           // amber-500
  --color-warning-light: #451a03;     // amber-950
  --color-warning-text: #fbbf24;      // amber-400

  --color-error: #ef4444;             // red-500
  --color-error-light: #450a0a;       // red-950
  --color-error-text: #f87171;        // red-400

  // Cards
  --card-shadow: 0 1px 3px rgba(0, 0, 0, 0.3), 0 1px 2px rgba(0, 0, 0, 0.2);
  --card-hover-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.3), 0 2px 4px -1px rgba(0, 0, 0, 0.2);

  // Dropdown
  --dropdown-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
}
```

#### Step 2: Create Component Color Audit Checklist

Create a file to track all components that need color fixes:

**File:** `docs/theme-audit-checklist.md`

```markdown
# Theme Audit Checklist

## Components to Check

### Header
- [ ] Logo visibility
- [ ] Navigation text color
- [ ] User menu dropdown text
- [ ] Cart badge
- [ ] Search input placeholder

### Mega Menu
- [ ] Category text
- [ ] Subcategory text
- [ ] Hover states
- [ ] Background/border contrast

### Product Cards
- [ ] Product name text
- [ ] Price text
- [ ] Original price strikethrough
- [ ] Stock status badge
- [ ] Rating stars
- [ ] Add to cart button

### Forms
- [ ] Input label text
- [ ] Input text
- [ ] Placeholder text
- [ ] Error messages
- [ ] Help text
- [ ] Disabled state

### Buttons
- [ ] Primary button text
- [ ] Secondary button text
- [ ] Ghost button text
- [ ] Disabled button text

### Badges/Tags
- [ ] Default badge
- [ ] Success badge
- [ ] Warning badge
- [ ] Error badge

### Tables
- [ ] Header text
- [ ] Cell text
- [ ] Alternating row backgrounds

### Modals/Dialogs
- [ ] Title text
- [ ] Body text
- [ ] Close button
```

#### Step 3: Fix Specific Component Issues

**File:** `src/ClimaSite.Web/src/app/shared/components/product-card/product-card.component.scss`

```scss
.product-card {
  background: var(--card-bg);
  border: 1px solid var(--card-border);
  border-radius: 12px;
  box-shadow: var(--card-shadow);
  transition: box-shadow 0.2s, border-color 0.2s;

  &:hover {
    box-shadow: var(--card-hover-shadow);
    border-color: var(--color-border-secondary);
  }

  .product-name {
    color: var(--color-text-primary);  // NOT hardcoded #333 or white
    font-weight: 600;
  }

  .product-price {
    color: var(--color-text-primary);
    font-size: 1.25rem;
    font-weight: 700;
  }

  .original-price {
    color: var(--color-text-tertiary);
    text-decoration: line-through;
  }

  .sale-badge {
    background: var(--color-error-light);
    color: var(--color-error-text);
  }

  .stock-status {
    &.in-stock {
      color: var(--color-success-text);
    }

    &.out-of-stock {
      color: var(--color-error-text);
    }
  }
}
```

### Acceptance Checklist

- [ ] All text is readable in light theme (contrast ratio >= 4.5:1)
- [ ] All text is readable in dark theme
- [ ] No hardcoded colors in components
- [ ] All CSS uses variable references
- [ ] WCAG 2.1 AA compliance verified
- [ ] Visual regression tests pass

---

## I18N-001 & I18N-002: Translation Fixes (HIGH)

### Step-by-Step Implementation

#### Step 1: Add Missing Translation Keys

**File:** `src/ClimaSite.Web/src/assets/i18n/en.json`

```json
{
  "common": {
    "tagline": "Your HVAC Solutions Partner",
    "viewAll": "View All",
    "search": "Search",
    "searchPlaceholder": "Search products...",
    "loading": "Loading...",
    "error": "An error occurred",
    "retry": "Try Again",
    "noResults": "No results found",
    "currency": "EUR"
  },

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
  },

  "nav": {
    "home": "Home",
    "products": "Products",
    "promotions": "Promotions",
    "brands": "Brands",
    "about": "About Us",
    "contact": "Contact",
    "account": "My Account",
    "orders": "My Orders",
    "wishlist": "Wishlist",
    "cart": "Cart"
  },

  "hero": {
    "title": "Premium HVAC Solutions",
    "subtitle": "Air conditioners, heating systems, and climate control",
    "cta": "Shop Now"
  },

  "home": {
    "featuredProducts": "Featured Products",
    "newArrivals": "New Arrivals",
    "bestSellers": "Best Sellers",
    "shopByCategory": "Shop by Category",
    "whyChooseUs": "Why Choose Us",
    "expertSupport": "Expert Support",
    "freeShipping": "Free Shipping",
    "easyReturns": "Easy Returns",
    "securePayment": "Secure Payment"
  }
}
```

**File:** `src/ClimaSite.Web/src/assets/i18n/bg.json`

```json
{
  "common": {
    "tagline": "Вашият партньор за HVAC решения",
    "viewAll": "Виж всички",
    "search": "Търсене",
    "searchPlaceholder": "Търсене на продукти...",
    "loading": "Зареждане...",
    "error": "Възникна грешка",
    "retry": "Опитайте отново",
    "noResults": "Няма намерени резултати",
    "currency": "лв."
  },

  "categories": {
    "airConditioning": "Климатизация",
    "wallMountedAC": "Стенни климатици",
    "multiSplit": "Мултисплит системи",
    "floorAC": "Подови климатици",
    "cassetteAC": "Касетъчни климатици",
    "ductedAC": "Канални климатици",
    "heatPumps": "Термопомпи",
    "airPurifiers": "Въздухопречистватели",
    "dehumidifiers": "Изсушители",
    "vrvVrf": "VRV/VRF системи",
    "heatingSystems": "Отоплителни системи",
    "electricHeaters": "Електрически нагреватели",
    "gasHeaters": "Газови нагреватели",
    "infraredHeaters": "Инфрачервени нагреватели",
    "convectors": "Конвектори",
    "radiators": "Радиатори",
    "underfloorHeating": "Подово отопление",
    "ventilation": "Вентилация",
    "exhaustFans": "Вентилатори за отвеждане",
    "ductFans": "Канални вентилатори",
    "recoveryVentilators": "Рекуператори",
    "airCurtains": "Въздушни завеси",
    "industrialFans": "Индустриални вентилатори",
    "waterPurification": "Пречистване на вода",
    "waterFilters": "Водни филтри",
    "reverseOsmosis": "Обратна осмоза",
    "uvSterilizers": "UV стерилизатори",
    "waterSofteners": "Омекотители за вода",
    "waterConsumables": "Консумативи",
    "accessories": "Аксесоари",
    "remoteControls": "Дистанционни управления",
    "installationKits": "Комплекти за инсталация",
    "copperPipes": "Медни тръби",
    "refrigerants": "Хладилни агенти",
    "bracketsMounts": "Стойки и монтаж",
    "drainPumps": "Дренажни помпи"
  },

  "nav": {
    "home": "Начало",
    "products": "Продукти",
    "promotions": "Промоции",
    "brands": "Марки",
    "about": "За нас",
    "contact": "Контакти",
    "account": "Моят акаунт",
    "orders": "Моите поръчки",
    "wishlist": "Любими",
    "cart": "Количка"
  },

  "hero": {
    "title": "Премиум HVAC решения",
    "subtitle": "Климатици, отоплителни системи и климатичен контрол",
    "cta": "Пазарувай сега"
  },

  "home": {
    "featuredProducts": "Препоръчани продукти",
    "newArrivals": "Нови продукти",
    "bestSellers": "Най-продавани",
    "shopByCategory": "Пазарувай по категория",
    "whyChooseUs": "Защо да изберете нас",
    "expertSupport": "Експертна поддръжка",
    "freeShipping": "Безплатна доставка",
    "easyReturns": "Лесно връщане",
    "securePayment": "Сигурно плащане"
  }
}
```

#### Step 2: Update Mega Menu Component to Use Translations

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.ts`

```typescript
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CategoryService } from '@core/services/category.service';

interface Category {
  id: string;
  slug: string;
  nameKey: string;  // Translation key instead of name
  icon?: string;
  children?: Category[];
}

@Component({
  selector: 'app-mega-menu',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './mega-menu.component.html',
  styleUrl: './mega-menu.component.scss'
})
export class MegaMenuComponent {
  private categoryService = inject(CategoryService);
  private translate = inject(TranslateService);

  categories = signal<Category[]>([]);
  activeCategory = signal<Category | null>(null);

  constructor() {
    this.loadCategories();
  }

  private loadCategories(): void {
    // Map API categories to include translation keys
    const apiCategories = this.categoryService.getCategories();

    this.categories.set(
      apiCategories.map(cat => ({
        ...cat,
        nameKey: `categories.${this.slugToKey(cat.slug)}`,
        children: cat.children?.map(child => ({
          ...child,
          nameKey: `categories.${this.slugToKey(child.slug)}`
        }))
      }))
    );
  }

  private slugToKey(slug: string): string {
    // Convert 'wall-mounted-ac' to 'wallMountedAC'
    return slug.replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
  }

  getCategoryName(category: Category): string {
    // Use translation key, fallback to slug
    return this.translate.instant(category.nameKey) || category.slug;
  }
}
```

**File:** `src/ClimaSite.Web/src/app/shared/components/mega-menu/mega-menu.component.html`

```html
<nav class="mega-menu" data-testid="mega-menu">
  <ul class="menu-list">
    @for (category of categories(); track category.id) {
      <li
        class="menu-item"
        (mouseenter)="activeCategory.set(category)"
        (mouseleave)="activeCategory.set(null)">

        <a [routerLink]="['/products/category', category.slug]">
          @if (category.icon) {
            <span class="category-icon" [innerHTML]="category.icon"></span>
          }
          <span class="category-name">{{ category.nameKey | translate }}</span>
          @if (category.children?.length) {
            <svg class="chevron" viewBox="0 0 24 24">
              <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
            </svg>
          }
        </a>

        @if (activeCategory()?.id === category.id && category.children?.length) {
          <div class="submenu-panel" data-testid="submenu-panel">
            <div class="submenu-header">
              <h3>{{ category.nameKey | translate }}</h3>
              <a [routerLink]="['/products/category', category.slug]">
                {{ 'common.viewAll' | translate }} →
              </a>
            </div>

            <div class="submenu-grid">
              @for (child of category.children; track child.id) {
                <a
                  [routerLink]="['/products/category', child.slug]"
                  class="submenu-item"
                  data-testid="submenu-item">
                  {{ child.nameKey | translate }}
                </a>
              }
            </div>
          </div>
        }
      </li>
    }
  </ul>
</nav>
```

---

# Part 2: Wishlist Feature (Plan 13 Extended)

## Quick Implementation Guide

The wishlist plan (13) is already comprehensive. Here's a condensed execution checklist:

### Phase 1: Backend (Days 1-2)

```bash
# 1. Create migration
cd src/ClimaSite.Infrastructure
dotnet ef migrations add AddWishlistTables --startup-project ../ClimaSite.Api

# 2. Apply migration
dotnet ef database update --startup-project ../ClimaSite.Api
```

**Files to create:**
1. `src/ClimaSite.Core/Entities/Wishlist.cs`
2. `src/ClimaSite.Core/Entities/WishlistItem.cs`
3. `src/ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs`
4. `src/ClimaSite.Infrastructure/Data/Configurations/WishlistItemConfiguration.cs`
5. `src/ClimaSite.Core/Interfaces/IWishlistRepository.cs`
6. `src/ClimaSite.Infrastructure/Repositories/WishlistRepository.cs`
7. `src/ClimaSite.Core/Interfaces/IWishlistService.cs`
8. `src/ClimaSite.Infrastructure/Services/WishlistService.cs`
9. `src/ClimaSite.Api/Controllers/WishlistController.cs`
10. DTOs in `src/ClimaSite.Api/DTOs/Wishlist/`

### Phase 2: Frontend (Days 3-4)

**Files to create:**
1. `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts`
2. `src/ClimaSite.Web/src/app/core/models/wishlist.model.ts`
3. `src/ClimaSite.Web/src/app/shared/components/wishlist-button/`
4. `src/ClimaSite.Web/src/app/features/wishlist/`

### Phase 3: Integration & Testing (Day 5)

1. Integrate wishlist button into product cards
2. Add wishlist link to header
3. Write unit tests
4. Write E2E tests

---

# Part 3: Notifications System (Plan 12 Extended)

## Completion Roadmap

### What's Been Done (Partial)
- Database schema designed
- API endpoints defined
- Email templates created

### What Needs Implementation

#### Backend Tasks (Priority Order)

| Task | Files | Effort |
|------|-------|--------|
| NOT-001 | Entities, Configurations, Migrations | 4h |
| NOT-002 | IEmailService, SmtpEmailService, SendGridEmailService | 8h |
| NOT-003 | EmailTemplateService, TemplateCache | 6h |
| NOT-004 | NotificationService | 8h |
| NOT-005 | EmailProcessingService (Background) | 6h |
| NOT-006 | Order Event Handlers | 6h |
| NOT-009 | NotificationsController | 6h |
| NOT-010 | NotificationPreferencesController | 4h |

#### Frontend Tasks (Priority Order)

| Task | Files | Effort |
|------|-------|--------|
| NOT-012 | NotificationService (Angular) | 4h |
| NOT-013 | NotificationBellComponent | 6h |
| NOT-014 | NotificationDropdownComponent | 6h |
| NOT-015 | NotificationCenterPage | 8h |
| NOT-016 | NotificationPreferencesPage | 6h |

### Implementation Order

```
Week 1: Backend Foundation
├── NOT-001: Database schema
├── NOT-002: Email service infrastructure
├── NOT-003: Template engine
└── NOT-004: Notification service core

Week 2: Backend API & Events
├── NOT-005: Background processing
├── NOT-006: Order event handlers
├── NOT-009: Notifications API
└── NOT-010: Preferences API

Week 3: Frontend
├── NOT-012: Angular service
├── NOT-013: Bell component
├── NOT-014: Dropdown
├── NOT-015: Center page
└── NOT-016: Preferences page

Week 4: Testing & Polish
├── NOT-020: Integration tests
├── E2E tests
└── Bug fixes
```

---

# Part 4: UI/UX Enhancements Phase 2 (Plan 15 Extended)

## Granular Task Breakdown

### User Account Menu (UX2-001 to UX2-004)

#### UX2-001: Fix User Icon Behavior

**Current Issue:** User icon always shows login link

**Implementation Steps:**

1. **Update HeaderComponent template** (30 min)
   - Add conditional rendering based on `authService.isAuthenticated()`
   - Show dropdown trigger when authenticated
   - Show login link when not authenticated

2. **Add user menu state** (15 min)
   - Add `showUserDropdown` signal
   - Add mouse enter/leave handlers

3. **Style dropdown** (45 min)
   - Position absolute below trigger
   - Add shadow and border
   - Theme-aware colors

4. **Add keyboard accessibility** (30 min)
   - Focus trap in dropdown
   - Escape to close
   - Tab navigation

**Estimated Total:** 2 hours

#### UX2-002: User Dropdown Menu

**Implementation Steps:**

1. **Create UserDropdownComponent** (1 hour)
```typescript
// user-dropdown.component.ts
@Component({
  selector: 'app-user-dropdown',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="user-dropdown" data-testid="user-dropdown">
      <div class="dropdown-header">
        <span class="user-name">{{ user()?.firstName }} {{ user()?.lastName }}</span>
        <span class="user-email">{{ user()?.email }}</span>
      </div>

      <nav class="dropdown-nav">
        <a routerLink="/account" (click)="close.emit()" data-testid="account-link">
          <svg class="icon"><!-- user icon --></svg>
          {{ 'nav.account' | translate }}
        </a>
        <a routerLink="/account/orders" (click)="close.emit()" data-testid="orders-link">
          <svg class="icon"><!-- package icon --></svg>
          {{ 'nav.orders' | translate }}
        </a>
        <a routerLink="/account/wishlist" (click)="close.emit()" data-testid="wishlist-link">
          <svg class="icon"><!-- heart icon --></svg>
          {{ 'nav.wishlist' | translate }}
        </a>
        <a routerLink="/account/addresses" (click)="close.emit()">
          <svg class="icon"><!-- map-pin icon --></svg>
          {{ 'nav.addresses' | translate }}
        </a>
      </nav>

      <div class="dropdown-footer">
        <button (click)="onLogout()" class="logout-btn" data-testid="logout-button">
          <svg class="icon"><!-- logout icon --></svg>
          {{ 'auth.logout' | translate }}
        </button>
      </div>
    </div>
  `
})
export class UserDropdownComponent {
  @Input() user = input.required<User | null>();
  @Output() close = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  onLogout(): void {
    this.logout.emit();
    this.close.emit();
  }
}
```

2. **Add styles** (30 min)
3. **Add click-outside directive** (30 min)
4. **Write unit tests** (1 hour)

**Estimated Total:** 3 hours

### Mega Menu (UX2-020 to UX2-025)

#### Detailed Implementation

**Step 1: Category Service Updates** (2 hours)

```typescript
// category.service.ts
@Injectable({ providedIn: 'root' })
export class CategoryService {
  private http = inject(HttpClient);
  private translate = inject(TranslateService);

  private categoriesState = signal<Category[]>([]);
  private loadingState = signal(false);

  readonly categories = this.categoriesState.asReadonly();
  readonly loading = this.loadingState.asReadonly();

  readonly categoryTree = computed(() => {
    return this.buildTree(this.categoriesState());
  });

  constructor() {
    this.loadCategories();
  }

  private async loadCategories(): Promise<void> {
    this.loadingState.set(true);
    try {
      const categories = await firstValueFrom(
        this.http.get<Category[]>('/api/v1/categories')
      );
      this.categoriesState.set(categories);
    } finally {
      this.loadingState.set(false);
    }
  }

  private buildTree(categories: Category[]): CategoryTree[] {
    const map = new Map<string, CategoryTree>();
    const roots: CategoryTree[] = [];

    // First pass: create all nodes
    categories.forEach(cat => {
      map.set(cat.id, { ...cat, children: [] });
    });

    // Second pass: build tree
    categories.forEach(cat => {
      const node = map.get(cat.id)!;
      if (cat.parentId) {
        const parent = map.get(cat.parentId);
        if (parent) {
          parent.children.push(node);
        }
      } else {
        roots.push(node);
      }
    });

    // Sort by sortOrder
    const sortNodes = (nodes: CategoryTree[]): CategoryTree[] => {
      return nodes
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map(node => ({
          ...node,
          children: sortNodes(node.children)
        }));
    };

    return sortNodes(roots);
  }

  getCategoryBySlug(slug: string): Category | null {
    return this.categoriesState().find(c => c.slug === slug) || null;
  }

  getTranslatedName(category: Category): string {
    const lang = this.translate.currentLang;

    if (lang === 'bg' && category.nameBg) return category.nameBg;
    if (lang === 'de' && category.nameDe) return category.nameDe;
    return category.name;
  }
}
```

**Step 2: Mega Menu Component** (4 hours)

Full component with hover logic, keyboard navigation, and responsive behavior.

**Step 3: Mobile Menu** (3 hours)

Accordion-style mobile navigation for categories.

---

# Part 5: Testing Requirements

## Test Coverage Matrix

| Feature | Unit Tests | Integration Tests | E2E Tests |
|---------|------------|-------------------|-----------|
| Auth Fix (AUTH-001) | AuthService, AuthGuard | - | Orders navigation |
| Category Nav (NAV-001) | CategoryService | GetProductsQuery | Mega menu, filtering |
| Theme Fix (THEME-001) | - | - | Visual regression |
| I18N (I18N-001/002) | TranslateService usage | - | Language switching |
| Wishlist (NAV-002) | WishlistService | WishlistController | Full CRUD flow |
| Notifications | NotificationService | All endpoints | Bell, dropdown, center |
| User Menu | UserDropdownComponent | - | Login, logout, navigation |

## E2E Test Data Factory Additions

```typescript
// test-data-factory.ts additions

export class TestDataFactory {
  // ... existing methods

  async createCategory(data?: Partial<CreateCategoryDto>): Promise<Category> {
    const response = await this.request.post('/api/admin/categories', {
      data: {
        name: data?.name ?? `Test Category ${this.generateId()}`,
        slug: data?.slug ?? `test-category-${this.generateId()}`,
        parentId: data?.parentId,
        isActive: true,
        ...data
      },
      headers: { Authorization: `Bearer ${this.adminToken}` }
    });

    const category = await response.json();
    this.createdCategories.push(category.id);
    return category;
  }

  async createCategoryWithProducts(count: number = 3): Promise<{
    category: Category;
    products: Product[];
  }> {
    const category = await this.createCategory();
    const products: Product[] = [];

    for (let i = 0; i < count; i++) {
      const product = await this.createProduct({
        categoryId: category.id,
        name: `Product ${i + 1} in ${category.name}`
      });
      products.push(product);
    }

    return { category, products };
  }

  async createWishlistWithItems(userId: string, itemCount: number = 3): Promise<{
    wishlist: Wishlist;
    items: WishlistItem[];
  }> {
    const products = await Promise.all(
      Array.from({ length: itemCount }, () => this.createProduct())
    );

    const items: WishlistItem[] = [];
    for (const product of products) {
      const response = await this.request.post('/api/v1/wishlist/items', {
        data: { productId: product.id },
        headers: { Authorization: `Bearer ${this.getUserToken(userId)}` }
      });
      items.push(await response.json());
    }

    const wishlistResponse = await this.request.get('/api/v1/wishlist', {
      headers: { Authorization: `Bearer ${this.getUserToken(userId)}` }
    });

    return {
      wishlist: await wishlistResponse.json(),
      items
    };
  }
}
```

---

# Summary: Implementation Timeline

## Phase 1: Critical Bug Fixes (3-4 days)
- AUTH-001: Auth guard fix
- NAV-001: Category navigation
- THEME-001: Theme contrast
- I18N-001/002: Translations

## Phase 2: Wishlist Feature (5 days)
- Backend: Database, API, Service
- Frontend: Components, Integration
- Testing: Unit, Integration, E2E

## Phase 3: Notifications (10 days)
- Backend infrastructure
- Event handlers
- Frontend components
- Testing

## Phase 4: UI/UX Enhancements (7 days)
- User menu
- Mega menu
- Product filtering
- Product details enhancements

## Phase 5: Final Testing & Polish (3 days)
- Full E2E test suite
- Visual regression
- Performance optimization
- Documentation

---

# Appendix: File Change Summary

## New Files to Create

```
src/ClimaSite.Core/
├── Entities/
│   ├── Wishlist.cs
│   ├── WishlistItem.cs
│   ├── Notification.cs
│   ├── NotificationPreference.cs
│   └── EmailLog.cs
├── Interfaces/
│   ├── IWishlistRepository.cs
│   ├── IWishlistService.cs
│   ├── INotificationService.cs
│   └── IEmailService.cs
└── Enums/
    ├── NotificationType.cs
    └── EmailStatus.cs

src/ClimaSite.Infrastructure/
├── Data/Configurations/
│   ├── WishlistConfiguration.cs
│   ├── WishlistItemConfiguration.cs
│   └── NotificationConfiguration.cs
├── Repositories/
│   └── WishlistRepository.cs
├── Services/
│   ├── WishlistService.cs
│   ├── NotificationService.cs
│   └── Email/
│       ├── SmtpEmailService.cs
│       └── SendGridEmailService.cs
└── BackgroundServices/
    └── EmailProcessingService.cs

src/ClimaSite.Api/
├── Controllers/
│   ├── WishlistController.cs
│   └── NotificationsController.cs
└── DTOs/
    ├── Wishlist/
    │   ├── WishlistDto.cs
    │   ├── WishlistItemDto.cs
    │   └── AddWishlistItemRequest.cs
    └── Notifications/
        ├── NotificationDto.cs
        └── NotificationPreferencesDto.cs

src/ClimaSite.Web/src/app/
├── core/services/
│   ├── wishlist.service.ts
│   └── notification.service.ts
├── core/models/
│   ├── wishlist.model.ts
│   └── notification.model.ts
├── shared/components/
│   ├── wishlist-button/
│   ├── notification-bell/
│   ├── notification-dropdown/
│   └── user-dropdown/
└── features/
    ├── wishlist/
    │   ├── wishlist.routes.ts
    │   ├── pages/wishlist-page/
    │   ├── pages/shared-wishlist-page/
    │   └── components/wishlist-item/
    └── account/
        ├── notifications/
        └── notification-preferences/
```

## Files to Modify

```
src/ClimaSite.Web/src/
├── app/
│   ├── app.routes.ts                    # Add wishlist, notification routes
│   ├── app.config.ts                    # Add APP_INITIALIZER for auth
│   ├── auth/
│   │   ├── services/auth.service.ts     # Add authReady signal
│   │   └── guards/auth.guard.ts         # Wait for authReady
│   ├── core/
│   │   └── layout/header/
│   │       ├── header.component.ts      # Add user menu, notification bell
│   │       └── header.component.html
│   ├── shared/components/
│   │   ├── mega-menu/                   # Fix routing, add translations
│   │   └── product-card/                # Add wishlist button
│   └── features/
│       ├── home/home.component.*        # Fix category card routing
│       └── products/product-list/       # Support category route param
├── styles/
│   └── _colors.scss                     # Fix contrast issues
└── assets/i18n/
    ├── en.json                          # Add missing keys
    ├── bg.json                          # Add missing keys
    └── de.json                          # Add missing keys
```
