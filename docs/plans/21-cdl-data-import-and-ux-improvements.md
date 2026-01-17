# Plan 21: CDL.bg Data Import & UX Improvements

## Overview

This plan covers importing realistic product data from CDL.bg, fixing navigation issues, redesigning the accessories section, fixing Q&A and Reviews functionality, removing price history, and auditing theme colors across the application.

**Estimated Effort:** Major (multi-day implementation)

---

## Table of Contents

1. [Data Import from CDL.bg](#phase-1-data-import-from-cdlbg)
2. [Navigation Fixes](#phase-2-navigation-fixes)
3. [Accessories Redesign](#phase-3-accessories-redesign)
4. [Q&A Section Fixes](#phase-4-qa-section-fixes)
5. [Reviews Section Fixes](#phase-5-reviews-section-fixes)
6. [Remove Price History](#phase-6-remove-price-history)
7. [Newsletter & Theme Colors](#phase-7-newsletter--theme-color-audit)
8. [Testing Plan](#phase-8-testing-plan)

---

## Phase 1: Data Import from CDL.bg

### 1.1 Category Structure Import

**Task ID:** CDL-001

Import all main categories from CDL.bg with their descriptions:

| CDL.bg Category (Bulgarian) | English Name | Description |
|----------------------------|--------------|-------------|
| Климатизация | Air Conditioning | Cooling, heating, ventilation systems |
| Вентилация | Ventilation | Fans, recuperators, air curtains |
| Отопление | Heating | Convectors, radiators, boilers |
| Бойлери | Water Heaters | Electric, gas, solar water heaters |
| Термопомпи | Heat Pumps | Air-to-air, air-to-water heat pumps |
| Пречистване на вода | Water Purification | Filters, osmosis systems |
| Омекотяване на вода | Water Softening | Softeners, anti-scale systems |
| Слънчеви инсталации | Solar Installations | Thermal collectors, panels |

**Files to Modify:**
- `src/ClimaSite.Infrastructure/Data/DataSeeder.cs` - Add category seeding
- Create new migration for category descriptions field if needed

### 1.2 Subcategory Import

**Task ID:** CDL-002

Import subcategories with descriptions. Example for Air Conditioning:

| Subcategory (Bulgarian) | English Name | Parent |
|------------------------|--------------|--------|
| Високостенни климатици | Wall-Mounted ACs | Air Conditioning |
| Колонни климатици | Floor-Standing ACs | Air Conditioning |
| Касетъчни климатици | Cassette ACs | Air Conditioning |
| Канални климатици | Ducted ACs | Air Conditioning |
| Мултисплит системи | Multi-Split Systems | Air Conditioning |
| Въздухопречистватели | Air Purifiers | Air Conditioning |
| Изсушители за въздух | Dehumidifiers | Air Conditioning |
| VRV/VRF системи | VRV/VRF Systems | Air Conditioning |
| Чилъри | Chillers | Air Conditioning |

**Database Changes:**
- Add `description` column to `categories` table if not exists
- Add `description_bg` and `description_de` for translations

### 1.3 Category Page Description Component

**Task ID:** CDL-003

Create a component to display category/subcategory descriptions at the top of listing pages (similar to CDL.bg).

**Files to Create:**
- `src/ClimaSite.Web/src/app/shared/components/category-header/category-header.component.ts`

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`

**Implementation:**
```typescript
// category-header.component.ts
@Component({
  selector: 'app-category-header',
  template: `
    @if (category()) {
      <div class="category-header">
        <h1>{{ category()?.name }}</h1>
        @if (category()?.description) {
          <div class="category-description">
            {{ category()?.description }}
          </div>
        }
        @if (subcategories().length > 0) {
          <div class="subcategories-grid">
            @for (sub of subcategories(); track sub.id) {
              <a [routerLink]="['/products']" [queryParams]="{category: sub.slug}" class="subcategory-card">
                <img [src]="sub.imageUrl" [alt]="sub.name" />
                <span>{{ sub.name }}</span>
              </a>
            }
          </div>
        }
      </div>
    }
  `
})
```

### 1.4 Product Import Script with Multi-Language Support

**Task ID:** CDL-004

Create a data import script/seeder to import products from CDL.bg structure with translations to all 3 languages (EN, BG, DE).

**Translation Approach:**
- Product names: Translate brand names stay same, model numbers stay same
- Descriptions: Full translation to EN, BG, DE
- Specifications: Keys translated, values stay technical (numbers, units)
- Features: Full translation

**Database Schema for Translations:**
```sql
-- Add translation columns to products table
ALTER TABLE products ADD COLUMN name_bg VARCHAR(500);
ALTER TABLE products ADD COLUMN name_de VARCHAR(500);
ALTER TABLE products ADD COLUMN description_bg TEXT;
ALTER TABLE products ADD COLUMN description_de TEXT;
ALTER TABLE products ADD COLUMN short_description_bg TEXT;
ALTER TABLE products ADD COLUMN short_description_de TEXT;

-- Or use a separate translations table
CREATE TABLE product_translations (
    id UUID PRIMARY KEY,
    product_id UUID REFERENCES products(id),
    language_code VARCHAR(5), -- 'en', 'bg', 'de'
    name VARCHAR(500),
    description TEXT,
    short_description TEXT,
    UNIQUE(product_id, language_code)
);
```

**Products to Import (10-15 per subcategory):**

For **Wall-Mounted ACs**:
- Mitsubishi Electric MSZ-EF Series (Black, White, Silver)
- Daikin Emura, Stylish, Perfera series
- Carrier SensatION, CoolEasy series
- Gree U-Crown, Fairy series
- LG Artcool, Dualcool series
- Samsung WindFree series
- Toshiba Shorai Edge series

For **Heat Pumps**:
- Daikin Altherma 3
- Mitsubishi Ecodan
- Bosch Compress series
- Viessmann Vitocal series
- LG Therma V
- Samsung EHS series

**Product Data Structure:**
```typescript
interface ProductImport {
  name: string;
  slug: string;
  description: string;
  shortDescription: string;
  brand: string;
  model: string;
  basePrice: number;
  categorySlug: string;
  specifications: {
    btu?: number;
    energyClass?: string;
    seer?: number;
    scop?: number;
    noiseLevel?: string;
    refrigerant?: string;
    roomSize?: string;
    warranty?: number;
  };
  features: string[];
  images: string[];
  relatedProductSlugs?: string[];
  accessorySlugs?: string[];
}
```

### 1.5 Product Images with MinIO Storage

**Task ID:** CDL-005

**Solution:** Self-hosted MinIO object storage (S3-compatible)

**MinIO Setup:**

1. **Deploy MinIO on Railway:**
   ```yaml
   # railway.toml
   [deploy]
   image = "minio/minio:latest"
   command = ["server", "/data", "--console-address", ":9001"]

   [env]
   MINIO_ROOT_USER = "${MINIO_ROOT_USER}"
   MINIO_ROOT_PASSWORD = "${MINIO_ROOT_PASSWORD}"
   ```

2. **Create .NET Service for Image Upload:**
   ```csharp
   // src/ClimaSite.Infrastructure/Services/ImageStorageService.cs
   public class MinioImageStorageService : IImageStorageService
   {
       private readonly IMinioClient _minioClient;
       private readonly string _bucketName = "climasite-images";

       public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
       {
           await _minioClient.PutObjectAsync(new PutObjectArgs()
               .WithBucket(_bucketName)
               .WithObject(fileName)
               .WithStreamData(imageStream));

           return $"{_minioEndpoint}/{_bucketName}/{fileName}";
       }
   }
   ```

3. **Image Processing Pipeline:**
   - Upload original image
   - Generate thumbnails (150x150, 300x300, 600x600)
   - Store all versions in MinIO
   - Return URLs for different sizes

**Files to Create:**
- `src/ClimaSite.Infrastructure/Services/MinioImageStorageService.cs`
- `src/ClimaSite.Core/Interfaces/IImageStorageService.cs`
- `src/ClimaSite.Api/Controllers/Admin/ImageUploadController.cs`

**Environment Variables:**
```
MINIO_ENDPOINT=minio.railway.internal:9000
MINIO_ACCESS_KEY=your-access-key
MINIO_SECRET_KEY=your-secret-key
MINIO_BUCKET=climasite-images
MINIO_PUBLIC_URL=https://your-minio-domain.up.railway.app
```

### 1.6 Product Filters/Facets

**Task ID:** CDL-006

Add filter options matching CDL.bg structure:

| Filter | Type | Values |
|--------|------|--------|
| Brand | Multi-select | Daikin, Mitsubishi, LG, Samsung, etc. |
| Price Range | Range slider | Min-Max EUR |
| BTU/Capacity | Multi-select | 9000, 12000, 18000, 24000+ |
| Energy Class | Multi-select | A+++, A++, A+, A, B |
| Room Size | Multi-select | Up to 20m², 20-35m², 35-50m², 50m²+ |
| Features | Multi-select | WiFi, Inverter, Heat Pump, etc. |

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`
- `src/ClimaSite.Api/Controllers/ProductsController.cs` - Add filter parameters

---

## Phase 2: Navigation Fixes

### 2.1 Breadcrumb Category Links

**Task ID:** NAV-001

**Current Issue:** Clicking category in breadcrumb (e.g., "Heat Pumps") doesn't navigate to the category page.

**Current Code (product-detail.component.ts:45-47):**
```html
<a [routerLink]="['/products']" [queryParams]="{category: product()?.category?.slug}">
  {{ product()?.category?.name }}
</a>
```

**Problem:** The link is correct but may not be working due to route configuration or Angular change detection.

**Fix Steps:**
1. Verify route configuration in `app.routes.ts`
2. Ensure `ProductListComponent` properly reads `category` query param
3. Add `data-testid` for E2E testing

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts`
- `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts`

### 2.2 Parent Category Navigation

**Task ID:** NAV-002

Add support for nested category navigation in breadcrumb:

```
Home / Products / Air Conditioning / Wall-Mounted ACs / Product Name
```

Each segment should be clickable and navigate to the appropriate category/subcategory view.

**Implementation:**
```typescript
// Build full breadcrumb path
breadcrumbPath = computed(() => {
  const prod = this.product();
  if (!prod?.category) return [];

  const path = [];
  let current = prod.category;
  while (current) {
    path.unshift(current);
    current = current.parent;
  }
  return path;
});
```

---

## Phase 3: Accessories Redesign

### 3.1 Remove "Add All" Functionality

**Task ID:** ACC-001

**Current Issues:**
- "Add All to Cart" functionality is problematic when there are 10-15 accessories
- Users don't want to buy all accessories together
- Grid layout doesn't work well with many items

**Changes:**
1. Remove the "Add All" section entirely
2. Convert grid to carousel (like Similar Products)
3. Show 5-6 items at a time with arrow navigation

**Files to Modify:**
- `src/ClimaSite.Web/src/app/shared/components/product-consumables/product-consumables.component.ts`

### 3.2 Implement Carousel for Accessories

**Task ID:** ACC-002

Redesign the consumables component to match similar-products carousel style.

**New Template Structure:**
```html
<section class="accessories-section" data-testid="accessories-section">
  <div class="section-header">
    <h3>{{ 'products.accessories.title' | translate }}</h3>
  </div>

  <div class="carousel-container">
    <button class="nav-btn prev" [disabled]="!canScrollLeft()" (click)="scrollLeft()">
      <!-- Left arrow SVG -->
    </button>

    <div class="products-carousel" #carousel (scroll)="onScroll()">
      @for (product of accessories(); track product.id) {
        <div class="product-card">
          <!-- Product card content like Similar Products -->
        </div>
      }
    </div>

    <button class="nav-btn next" [disabled]="!canScrollRight()" (click)="scrollRight()">
      <!-- Right arrow SVG -->
    </button>
  </div>
</section>
```

**Carousel Specifications:**
- **Visible items:** 5-6 on desktop, 2-3 on tablet, 1-2 on mobile
- **Navigation:** Left/right arrows
- **Card width:** ~200px (matches Similar Products)
- **Scroll amount:** Width of ~2 cards per click
- **Mobile:** Touch swipe enabled, arrows hidden

### 3.3 Update Translations

**Task ID:** ACC-003

Update translation keys:
- Remove: `products.consumables.addAll`, `products.consumables.totalPrice`
- Update: `products.consumables.title` -> `products.accessories.title`
- Update: `products.consumables.subtitle` -> `products.accessories.subtitle`

**Files to Modify:**
- `src/ClimaSite.Web/src/assets/i18n/en.json`
- `src/ClimaSite.Web/src/assets/i18n/bg.json`
- `src/ClimaSite.Web/src/assets/i18n/de.json`

---

## Phase 4: Q&A Section Fixes

### 4.1 Require Login for Questions

**Task ID:** QA-001

**Current Issue:** Q&A allows anyone to submit questions without authentication.

**New Behavior:**
- Show "Login to ask a question" button for unauthenticated users
- After login, redirect back to product page with Q&A tab active
- Pre-fill user's name and email from their profile

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/components/product-qa/product-qa.component.ts`
- `src/ClimaSite.Api/Controllers/QuestionsController.cs` - Add `[Authorize]` attribute

**Implementation:**
```typescript
// Inject AuthService
private readonly authService = inject(AuthService);

isAuthenticated = computed(() => this.authService.isAuthenticated());

// In template
@if (!isAuthenticated()) {
  <div class="login-prompt">
    <p>{{ 'products.qa.loginToAsk' | translate }}</p>
    <a [routerLink]="['/auth/login']" [queryParams]="{returnUrl: currentUrl()}">
      {{ 'auth.login' | translate }}
    </a>
  </div>
} @else {
  <!-- Show question form -->
}
```

### 4.2 Admin Moderation for Questions

**Task ID:** QA-002

**Current Behavior:** Questions appear immediately (unclear from code).

**New Behavior:**
- Questions submitted but not visible until approved
- Add `isApproved` field to Question entity
- Show success message: "Your question has been submitted and will appear after review"
- Admin panel: Dedicated Q&A moderation section

**Backend Changes:**
- `src/ClimaSite.Core/Entities/Question.cs` - Add `IsApproved` property
- `src/ClimaSite.Application/Features/Questions/Queries/GetProductQuestionsQueryHandler.cs` - Filter by `IsApproved = true`
- `src/ClimaSite.Api/Controllers/Admin/AdminQuestionsController.cs` - Create moderation endpoints

### 4.4 Admin Q&A Moderation Page

**Task ID:** QA-004

**Create Dedicated Admin Moderation Page:**

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/admin/moderation/moderation.component.ts`
- `src/ClimaSite.Web/src/app/features/admin/moderation/questions-moderation/questions-moderation.component.ts`
- `src/ClimaSite.Web/src/app/features/admin/moderation/reviews-moderation/reviews-moderation.component.ts`

**Admin Moderation Page Features:**
```typescript
// moderation.component.ts
@Component({
  template: `
    <div class="admin-moderation">
      <h1>Content Moderation</h1>

      <div class="moderation-tabs">
        <button [class.active]="activeTab() === 'questions'" (click)="setTab('questions')">
          Questions ({{ pendingQuestionsCount() }})
        </button>
        <button [class.active]="activeTab() === 'reviews'" (click)="setTab('reviews')">
          Reviews ({{ pendingReviewsCount() }})
        </button>
      </div>

      @if (activeTab() === 'questions') {
        <app-questions-moderation />
      } @else {
        <app-reviews-moderation />
      }
    </div>
  `
})
```

**Questions Moderation Features:**
- List of pending questions with product context
- Preview question content
- Approve / Reject buttons
- Bulk actions (approve all, reject selected)
- Filter by product, date range

**Reviews Moderation Features:**
- List of pending reviews with rating preview
- Full review text and rating display
- Verify purchase status indicator
- Approve / Reject / Flag buttons
- Filter by rating, product, date

### 4.3 Fix Q&A Styling Issues

**Task ID:** QA-003

**Current Issues:**
- Uses CSS variables that don't exist (`--spacing-lg`, `--font-size-xl`, etc.)
- Poor alignment and formatting
- Colors not matching theme

**Fix: Replace custom CSS variables with our standard ones:**

| Current Variable | Replace With |
|-----------------|--------------|
| `--spacing-xs` | `0.25rem` |
| `--spacing-sm` | `0.5rem` |
| `--spacing-md` | `1rem` |
| `--spacing-lg` | `1.5rem` |
| `--spacing-xl` | `2rem` |
| `--font-size-xs` | `0.75rem` |
| `--font-size-sm` | `0.875rem` |
| `--font-size-md` | `1rem` |
| `--font-size-xl` | `1.5rem` |
| `--radius-sm` | `0.25rem` |
| `--radius-md` | `0.5rem` |
| `--radius-lg` | `0.75rem` |
| `--color-surface` | `var(--color-bg-primary)` |
| `--color-background` | `var(--color-bg-secondary)` |
| `--color-primary-light` | `var(--color-primary-100)` |

**Complete Style Rewrite:**
```scss
.product-qa {
  padding: 1.5rem;
  background: var(--color-bg-primary);
  border-radius: 0.75rem;
  border: 1px solid var(--color-border-primary);
}

.qa-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 0.5rem;

  h2 {
    font-size: 1.5rem;
    font-weight: 600;
    color: var(--color-text-primary);
    margin: 0;
  }

  .qa-stats {
    display: flex;
    gap: 1rem;
    font-size: 0.875rem;
    color: var(--color-text-secondary);
  }
}

// ... complete rewrite with standard variables
```

---

## Phase 5: Reviews Section Fixes

### 5.1 Require Login for Reviews

**Task ID:** REV-001

**Current Issue:** Submitting a review logs the user out (likely auth token issue).

**Investigation Needed:**
1. Check if `ProductReviewsComponent` makes API call that invalidates token
2. Verify API endpoint authentication handling
3. Check if refresh token flow is working

**Files to Check:**
- `src/ClimaSite.Web/src/app/shared/components/product-reviews/product-reviews.component.ts`
- `src/ClimaSite.Api/Controllers/ReviewsController.cs`

### 5.2 Admin Moderation for Reviews

**Task ID:** REV-002

**New Behavior:**
- Reviews require admin approval before appearing
- Add `isApproved` field if not exists
- Admin panel: Reviews moderation section

### 5.3 Fix Review Submission Bug

**Task ID:** REV-003

**Debug Steps:**
1. Check network requests when submitting review
2. Verify JWT token is included in request
3. Check if API returns 401 that triggers logout
4. Verify error handling doesn't accidentally call logout

---

## Phase 6: Remove Price History

### 6.1 Remove Price History Component

**Task ID:** PRICE-001

**Files to Delete:**
- `src/ClimaSite.Web/src/app/features/products/components/price-history-chart/price-history-chart.component.ts`

**Files to Modify:**
- `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts`
  - Remove import
  - Remove `<app-price-history-chart>` from template
  - Remove from imports array

### 6.2 Remove Price History API

**Task ID:** PRICE-002

**Files to Remove/Modify:**
- `src/ClimaSite.Api/Controllers/ProductsController.cs` - Remove price history endpoint
- `src/ClimaSite.Application/Features/Products/Queries/GetPriceHistoryQuery.cs` - Delete
- Database: Keep price history table for potential future use, but remove seeded data

### 6.3 Remove Translations

**Task ID:** PRICE-003

Remove from all translation files:
- `products.priceHistory.*` keys

---

## Phase 7: Newsletter & Theme Color Audit

### 7.1 Fix Newsletter Section Colors

**Task ID:** THEME-001

**Current Issue:** White text on white background in light theme.

**Location:** `src/ClimaSite.Web/src/app/features/home/home.component.ts`

**Current Fix Applied:**
```scss
.newsletter-input {
  background-color: white;
  color: #1f2937;

  &::placeholder {
    color: #9ca3af;
  }
}
```

**Better Fix - Use CSS Variables:**
```scss
.newsletter-section {
  background: linear-gradient(135deg, var(--color-primary-600), var(--color-primary-800));

  .newsletter-input {
    background-color: var(--color-bg-primary);
    color: var(--color-text-primary);
    border: 2px solid transparent;

    &::placeholder {
      color: var(--color-text-placeholder);
    }

    &:focus {
      border-color: var(--color-primary-300);
    }
  }

  // Text on gradient background
  .newsletter-title,
  .newsletter-subtitle {
    color: var(--color-text-inverse);
  }
}
```

### 7.2 Theme Color Audit - Components to Review

**Task ID:** THEME-002

Audit all components for hardcoded colors and replace with CSS variables:

| Component | File | Issues to Check |
|-----------|------|-----------------|
| Header | `header.component.ts` | Logo colors, nav links |
| Footer | `footer.component.ts` | Background, text colors |
| Product Cards | `product-card.component.ts` | Badge colors, borders |
| Product Detail | `product-detail.component.ts` | Price colors, tabs |
| Cart | `cart.component.ts` | Totals, remove button |
| Checkout | `checkout.component.ts` | Form inputs, steps |
| Q&A | `product-qa.component.ts` | All styling (confirmed issues) |
| Reviews | `product-reviews.component.ts` | Rating stars, form |
| Admin Panel | All admin components | Dashboard, tables, forms |

### 7.3 Color Variable Compliance Check

**Task ID:** THEME-003

Create a script or manual check to find hardcoded colors:

```bash
# Find hardcoded hex colors in TypeScript/SCSS
grep -r "#[0-9a-fA-F]\{3,6\}" src/ClimaSite.Web/src/app --include="*.ts" --include="*.scss"

# Find hardcoded rgb/rgba
grep -r "rgb\|rgba" src/ClimaSite.Web/src/app --include="*.ts" --include="*.scss"
```

**Allowed Exceptions:**
- `rgba(0, 0, 0, x)` for shadows (should use `--shadow-color`)
- SVG inline colors (consider moving to CSS)

### 7.4 Add Missing CSS Variables

**Task ID:** THEME-004

Add any missing variables to `_colors.scss`:

```scss
:root {
  // Additional semantic colors if needed
  --color-success-bg: #{$color-success-50};
  --color-warning-bg: #{$color-warning-50};
  --color-info-bg: #{$color-info-50};

  // Form specific
  --color-input-border: var(--color-border-primary);
  --color-input-focus-ring: var(--color-primary-100);

  // Card specific
  --color-card-shadow: rgba(0, 0, 0, 0.05);
}

[data-theme="dark"] {
  --color-success-bg: #{$color-success-950};
  --color-warning-bg: #{$color-warning-950};
  --color-info-bg: #{$color-info-950};
  --color-card-shadow: rgba(0, 0, 0, 0.2);
}
```

---

## Phase 8: Testing Plan

### 8.1 Unit Tests

**Backend Unit Tests:**

| Test File | Tests |
|-----------|-------|
| `CategoryQueriesTests.cs` | Get categories with descriptions, subcategories |
| `QuestionCommandsTests.cs` | Submit question requires auth, moderation flow |
| `ReviewCommandsTests.cs` | Submit review requires auth, moderation flow |

**Frontend Unit Tests:**

| Test File | Tests |
|-----------|-------|
| `category-header.component.spec.ts` | Renders description, subcategory links |
| `product-consumables.component.spec.ts` | Carousel navigation, no add-all |
| `product-qa.component.spec.ts` | Login required, form validation |

### 8.2 E2E Tests

**New E2E Tests to Create:**

```typescript
// tests/ClimaSite.E2E/Tests/Navigation/BreadcrumbTests.cs
[Test]
public async Task Breadcrumb_CategoryLink_NavigatesToCategoryPage()
{
    // Navigate to product in a category
    await Page.GotoAsync("/products/mitsubishi-msz-ef25");

    // Click category in breadcrumb
    await Page.ClickAsync("[data-testid='breadcrumb-category']");

    // Verify navigation to category page
    await Expect(Page).ToHaveURLAsync(new Regex(@"/products\?category="));
}

// tests/ClimaSite.E2E/Tests/Products/AccessoriesCarouselTests.cs
[Test]
public async Task AccessoriesCarousel_ArrowsNavigate_ShowsMoreProducts()
{
    await Page.GotoAsync("/products/coolmaster-18000");

    var carousel = Page.Locator("[data-testid='accessories-section']");
    await Expect(carousel).ToBeVisibleAsync();

    // Click next arrow
    await Page.ClickAsync("[data-testid='accessories-next']");

    // Verify scroll position changed
    // ...
}

// tests/ClimaSite.E2E/Tests/Products/QATests.cs
[Test]
public async Task QA_UnauthenticatedUser_ShowsLoginPrompt()
{
    await Page.GotoAsync("/products/coolmaster-18000");
    await Page.ClickAsync("[data-testid='tab-qa']");

    await Expect(Page.Locator("[data-testid='qa-login-prompt']")).ToBeVisibleAsync();
}

[Test]
public async Task QA_AuthenticatedUser_CanSubmitQuestion()
{
    // Login first
    await LoginAsUser(Page);

    await Page.GotoAsync("/products/coolmaster-18000");
    await Page.ClickAsync("[data-testid='tab-qa']");
    await Page.ClickAsync("[data-testid='ask-question-btn']");

    await Page.FillAsync("[data-testid='question-text']", "Does this model work with smart home systems?");
    await Page.ClickAsync("[data-testid='submit-question']");

    await Expect(Page.Locator("[data-testid='question-submitted-success']")).ToBeVisibleAsync();
}

// tests/ClimaSite.E2E/Tests/Theme/ThemeColorTests.cs
[Test]
public async Task Newsletter_LightTheme_TextIsVisible()
{
    await Page.GotoAsync("/");

    // Ensure light theme
    await Page.EvaluateAsync("document.documentElement.removeAttribute('data-theme')");

    var input = Page.Locator("[data-testid='newsletter-input']");
    var styles = await input.EvaluateAsync<Dictionary<string, string>>("el => getComputedStyle(el)");

    // Verify text color has sufficient contrast with background
    // ...
}
```

### 8.3 Visual Regression Tests

Consider adding visual regression tests for theme changes:

```typescript
[Test]
public async Task ProductPage_LightTheme_MatchesSnapshot()
{
    await Page.GotoAsync("/products/coolmaster-18000");
    await Expect(Page).ToHaveScreenshotAsync("product-page-light.png");
}

[Test]
public async Task ProductPage_DarkTheme_MatchesSnapshot()
{
    await Page.GotoAsync("/products/coolmaster-18000");
    await Page.EvaluateAsync("document.documentElement.setAttribute('data-theme', 'dark')");
    await Expect(Page).ToHaveScreenshotAsync("product-page-dark.png");
}
```

### 8.4 Test Data Requirements

Create test data factory methods for new features:

```typescript
// TestDataFactory additions
async createQuestionForModeration(productId: string): Promise<Question> {
    // Create question that needs approval
}

async createApprovedQuestion(productId: string): Promise<Question> {
    // Create approved question
}

async createCategoryWithDescription(name: string, description: string): Promise<Category> {
    // Create category with description for testing
}
```

---

## Implementation Order

### Priority 1 (Critical Fixes)
1. **NAV-001** - Breadcrumb category links (affects navigation)
2. **REV-003** - Review submission logout bug (critical UX bug)
3. **THEME-001** - Newsletter colors (visible on homepage)

### Priority 2 (Feature Changes)
4. **ACC-001, ACC-002** - Accessories carousel redesign
5. **QA-001, QA-002, QA-003** - Q&A login + moderation + styling
6. **REV-001, REV-002** - Reviews login + moderation

### Priority 3 (Cleanup)
7. **PRICE-001, PRICE-002, PRICE-003** - Remove price history
8. **THEME-002, THEME-003, THEME-004** - Theme color audit

### Priority 4 (Data Import)
9. **CDL-001 to CDL-006** - Data import from CDL.bg

---

## Decisions Made

| Question | Decision |
|----------|----------|
| **Product Translations** | All 3 languages (EN, BG, DE) |
| **Image Storage** | MinIO (self-hosted, S3-compatible) on Railway |
| **Admin Moderation** | Dedicated moderation page with tabs for Q&A and Reviews |
| **Price History** | Remove completely |
| **Q&A/Reviews Auth** | Login required + admin moderation before publishing |
| **Carousel Style** | 5-6 items visible with arrow navigation |
| **Currency** | EUR (convert from BGN if needed) |
| **Category Images** | Use representative product images from each category |

---

## Definition of Done

- [ ] All navigation links work correctly (breadcrumbs, category links)
- [ ] Accessories section uses carousel with 5-6 visible items
- [ ] Q&A requires login, questions need moderation
- [ ] Reviews require login, reviews need moderation
- [ ] Review submission does not log user out
- [ ] Price history feature completely removed
- [ ] Newsletter section visible in both themes
- [ ] No hardcoded colors in any component
- [ ] All new features have E2E tests
- [ ] All new backend features have unit tests
- [ ] Build passes with no errors
- [ ] Manual testing passes in both themes
- [ ] Manual testing passes in all 3 languages
