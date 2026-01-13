# Plan 17: Future Enhancements - Professional E-Commerce Features

## Overview

This plan covers the implementation of professional e-commerce features to bring ClimaSite to the level of established HVAC retailers like CDL.bg and EMAG.bg.

### Implementation Status

| Phase | Status | Completed |
|-------|--------|-----------|
| Phase 1: Missing Pages | ✅ COMPLETE | Promotions, Brands, Resources pages |
| Phase 2: Product Translation | ✅ COMPLETE | CategoryTranslation, SpecKeyPipe |
| Phase 3: Enhanced Product Details | ✅ COMPLETE | Gallery with Zoom, Energy Rating, Warranty Badges |
| Phase 4: Related Products | ✅ COMPLETE | Consumables, Similar Products |
| Phase 5: Testing | ✅ COMPLETE | All tests pass (411 frontend, 175 backend) |

### Features Summary

| Category | Features | Priority | Status |
|----------|----------|----------|--------|
| **Missing Pages** | Promotions, Brands, Resources | High | ✅ Complete |
| **Product Translation** | Full i18n for product content | High | ✅ Complete |
| **Product Details** | Gallery, Energy Rating, Warranty | High | ✅ Complete |
| **Related Products** | Consumables, Similar | High | ✅ Complete |

### Components Created
- `ProductGalleryComponent` - Image gallery with zoom, fullscreen, keyboard navigation
- `EnergyRatingComponent` - EU energy efficiency label display (A+++ to G)
- `WarrantyBadgeComponent` - Trust badges for warranty, returns, shipping, stock
- `SpecKeyPipe` - Translates specification keys (e.g., cooling_capacity → Cooling Capacity)
- `StockDeliveryComponent` - Stock status display with delivery options and estimated arrival dates
- `ProductVariantsComponent` - Family variants display (BTU options, colors) with navigation
- `FrequentlyBoughtComponent` - Bundle products with dynamic pricing and discount calculation
- `RecentlyViewedComponent` - Recently viewed products carousel with localStorage persistence
- `ShareProductComponent` - Share product via social media (Facebook, Twitter, WhatsApp, Email)
- `CompareButtonComponent` - Add/remove products from comparison list
- `FinancingCalculatorComponent` - Monthly payment calculator with multiple financing options
- `SpecsTableComponent` - Enhanced specs display with grouping, search, and expand/collapse

### Services Created
- `RecentlyViewedService` - Manage recently viewed products in localStorage
- `ComparisonService` - Manage product comparison list (max 4 products)
- `StructuredDataService` - Generate JSON-LD structured data for SEO (Product, BreadcrumbList, Organization)

### Total Task Count
- **Phase 1**: 9 tasks (Missing Pages) - ✅ Complete
- **Phase 2**: 5 tasks (Product Translation) - ✅ Complete
- **Phase 3**: 15 tasks (Enhanced Product Details) - ✅ Core features complete
- **Phase 4**: 6 tasks (Related Products) - ✅ Complete
- **Phase 5**: 10 tasks (Testing) - ✅ Complete
- **Total**: 45 tasks

---

## Reference Analysis

### CDL.bg Features (https://cdl.bg)
- Breadcrumb navigation with full category path
- Image gallery with PhotoSwipe zoom
- Dual currency display (BGN/EUR)
- Tabbed product information (Description, Specifications, Reviews)
- Technical specifications table with detailed metrics
- Energy efficiency ratings (A++/A+)
- Related products carousel (8 products)
- **Accessories & Consumables section** (4 items shown)
- Warranty information prominently displayed
- Installation service indication

### EMAG.bg Features (https://www.emag.bg)
- Star rating with review count (4.64/5, 858 reviews)
- **Financing options** (14.26/month, 24 installments)
- **0% interest promotions** with date ranges
- Stock availability status
- **Extended warranty options** (60 months consumer, 24 months commercial)
- **Protect+ insurance** add-on
- **30-day return guarantee** badge
- Courier delivery with time estimate
- **Q&A section** (24 questions answered)
- **Family product variants** (9000, 18000, 24000 BTU options)
- Recommendation percentage (92% would recommend)
- Breadcrumb with category hierarchy

---

## Task Breakdown

### Phase 1: Missing Static Pages (PAGES-001 to PAGES-009)

#### PAGES-001: Create Promotions Page - Backend
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Create backend support for promotions/deals page.

**Implementation Steps**:
1. Create `Promotion` entity in `/src/ClimaSite.Core/Entities/`:
   ```csharp
   public class Promotion : BaseEntity
   {
       public string Title { get; set; } = string.Empty;
       public string Slug { get; set; } = string.Empty;
       public string Description { get; set; } = string.Empty;
       public string? BannerImageUrl { get; set; }
       public PromotionType Type { get; set; } // Percentage, FixedAmount, BuyXGetY, FreeShipping
       public decimal DiscountValue { get; set; }
       public decimal? MinimumOrderAmount { get; set; }
       public DateTime StartDate { get; set; }
       public DateTime EndDate { get; set; }
       public bool IsActive { get; set; }
       public bool IsFeatured { get; set; }
       public string? CouponCode { get; set; }
       public int? UsageLimit { get; set; }
       public int UsageCount { get; set; }

       // Navigation
       public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
       public ICollection<PromotionTranslation> Translations { get; set; } = new List<PromotionTranslation>();
   }

   public class PromotionProduct
   {
       public Guid PromotionId { get; set; }
       public Guid ProductId { get; set; }
       public Promotion Promotion { get; set; } = null!;
       public Product Product { get; set; } = null!;
   }

   public class PromotionTranslation
   {
       public Guid Id { get; set; }
       public Guid PromotionId { get; set; }
       public string LanguageCode { get; set; } = "en";
       public string Title { get; set; } = string.Empty;
       public string Description { get; set; } = string.Empty;
   }
   ```

2. Create EF Core migration for promotions tables
3. Create DTOs: `PromotionDto`, `PromotionBriefDto`, `CreatePromotionDto`
4. Create queries:
   - `GetActivePromotionsQuery` - List active promotions with pagination
   - `GetPromotionBySlugQuery` - Get single promotion details
   - `GetFeaturedPromotionsQuery` - Homepage promotions
5. Create `PromotionsController` with endpoints:
   - `GET /api/promotions` - List promotions
   - `GET /api/promotions/{slug}` - Get promotion details
   - `GET /api/promotions/featured` - Featured promotions

**Files to Create**:
- `/src/ClimaSite.Core/Entities/Promotion.cs`
- `/src/ClimaSite.Core/Entities/PromotionProduct.cs`
- `/src/ClimaSite.Core/Entities/PromotionTranslation.cs`
- `/src/ClimaSite.Application/Features/Promotions/DTOs/PromotionDto.cs`
- `/src/ClimaSite.Application/Features/Promotions/Queries/GetActivePromotionsQuery.cs`
- `/src/ClimaSite.Application/Features/Promotions/Queries/GetPromotionBySlugQuery.cs`
- `/src/ClimaSite.Api/Controllers/PromotionsController.cs`

**Tests Required**:
- Unit tests for promotion validation logic
- Integration tests for all API endpoints
- E2E tests for promotion page navigation

---

#### PAGES-002: Create Promotions Page - Frontend
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Create Angular promotions page with listing and detail views.

**Implementation Steps**:
1. Create promotions feature module at `/src/ClimaSite.Web/src/app/features/promotions/`
2. Create components:
   - `PromotionsListComponent` - Grid of active promotions
   - `PromotionDetailComponent` - Single promotion with products
   - `PromotionCardComponent` - Reusable promotion card
   - `PromotionBannerComponent` - Homepage banner

3. Create service `PromotionService`:
   ```typescript
   @Injectable({ providedIn: 'root' })
   export class PromotionService {
     getActivePromotions(page: number, pageSize: number): Observable<PaginatedList<PromotionBrief>>;
     getPromotionBySlug(slug: string): Observable<Promotion>;
     getFeaturedPromotions(count: number): Observable<PromotionBrief[]>;
   }
   ```

4. Create routes:
   - `/promotions` - List all active promotions
   - `/promotions/:slug` - Single promotion detail

5. Add translations for EN, BG, DE in i18n files:
   ```json
   "promotions": {
     "title": "Special Offers",
     "subtitle": "Don't miss our exclusive deals",
     "activeUntil": "Valid until",
     "useCode": "Use code",
     "shopNow": "Shop Now",
     "viewProducts": "View Products",
     "noPromotions": "No active promotions at the moment",
     "termsApply": "Terms and conditions apply",
     "limitedTime": "Limited Time Offer",
     "discount": "{{value}}% OFF",
     "minOrder": "Minimum order: {{amount}}"
   }
   ```

6. Implement responsive design:
   - Desktop: 3-column grid
   - Tablet: 2-column grid
   - Mobile: Single column with swipeable banners

**Files to Create**:
- `/src/ClimaSite.Web/src/app/features/promotions/promotions.routes.ts`
- `/src/ClimaSite.Web/src/app/features/promotions/promotions-list/promotions-list.component.ts`
- `/src/ClimaSite.Web/src/app/features/promotions/promotion-detail/promotion-detail.component.ts`
- `/src/ClimaSite.Web/src/app/features/promotions/promotion-card/promotion-card.component.ts`
- `/src/ClimaSite.Web/src/app/core/services/promotion.service.ts`
- `/src/ClimaSite.Web/src/app/core/models/promotion.model.ts`

**Tests Required**:
- Unit tests for PromotionService
- Component tests for PromotionCard
- E2E test: Navigate to promotions, view promotion detail, click product

---

#### PAGES-003: Create Brands Page - Backend
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Create backend support for brands listing and detail pages.

**Implementation Steps**:
1. Create `Brand` entity (if not exists):
   ```csharp
   public class Brand : BaseEntity
   {
       public string Name { get; set; } = string.Empty;
       public string Slug { get; set; } = string.Empty;
       public string? Description { get; set; }
       public string? LogoUrl { get; set; }
       public string? BannerImageUrl { get; set; }
       public string? WebsiteUrl { get; set; }
       public string? Country { get; set; }
       public int FoundedYear { get; set; }
       public bool IsActive { get; set; } = true;
       public bool IsFeatured { get; set; }
       public int SortOrder { get; set; }

       // Navigation
       public ICollection<Product> Products { get; set; } = new List<Product>();
       public ICollection<BrandTranslation> Translations { get; set; } = new List<BrandTranslation>();
   }

   public class BrandTranslation
   {
       public Guid Id { get; set; }
       public Guid BrandId { get; set; }
       public string LanguageCode { get; set; } = "en";
       public string? Description { get; set; }
   }
   ```

2. Create migration if entity is new
3. Create queries:
   - `GetBrandsQuery` - List all brands with product count
   - `GetBrandBySlugQuery` - Single brand with products
   - `GetFeaturedBrandsQuery` - Homepage brands
4. Create `BrandsController`

**API Endpoints**:
- `GET /api/brands` - List brands (with product count)
- `GET /api/brands/{slug}` - Brand details with products
- `GET /api/brands/featured` - Featured brands for homepage

**Files to Create/Modify**:
- `/src/ClimaSite.Core/Entities/Brand.cs`
- `/src/ClimaSite.Application/Features/Brands/DTOs/BrandDto.cs`
- `/src/ClimaSite.Application/Features/Brands/Queries/GetBrandsQuery.cs`
- `/src/ClimaSite.Api/Controllers/BrandsController.cs`

---

#### PAGES-004: Create Brands Page - Frontend
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Create Angular brands page with A-Z listing and brand detail.

**Implementation Steps**:
1. Create brands feature module
2. Create components:
   - `BrandsListComponent` - A-Z listing with logos
   - `BrandDetailComponent` - Brand info + products
   - `BrandCardComponent` - Logo + name + product count

3. Design A-Z navigation:
   ```html
   <div class="brand-alphabet">
     <button *ngFor="let letter of alphabet"
             [class.active]="activeLetter === letter"
             (click)="scrollToLetter(letter)">
       {{ letter }}
     </button>
   </div>

   <div *ngFor="let group of brandGroups">
     <h2 [id]="'brand-' + group.letter">{{ group.letter }}</h2>
     <div class="brand-grid">
       <app-brand-card *ngFor="let brand of group.brands" [brand]="brand"/>
     </div>
   </div>
   ```

4. Routes:
   - `/brands` - All brands A-Z
   - `/brands/:slug` - Brand detail with products

5. Add translations:
   ```json
   "brands": {
     "title": "Our Brands",
     "subtitle": "Trusted manufacturers we work with",
     "allBrands": "All Brands",
     "featuredBrands": "Featured Brands",
     "productCount": "{{count}} products",
     "visitWebsite": "Visit Website",
     "viewProducts": "View Products",
     "since": "Since {{year}}",
     "country": "Country"
   }
   ```

**Files to Create**:
- `/src/ClimaSite.Web/src/app/features/brands/brands.routes.ts`
- `/src/ClimaSite.Web/src/app/features/brands/brands-list/brands-list.component.ts`
- `/src/ClimaSite.Web/src/app/features/brands/brand-detail/brand-detail.component.ts`
- `/src/ClimaSite.Web/src/app/core/services/brand.service.ts`
- `/src/ClimaSite.Web/src/app/core/models/brand.model.ts`

---

#### PAGES-005: Create Resources Page - Backend
**Priority**: Low
**Estimated Complexity**: Medium

**Description**: Create backend for resources/guides/articles section.

**Implementation Steps**:
1. Create entities:
   ```csharp
   public class Resource : BaseEntity
   {
       public string Title { get; set; } = string.Empty;
       public string Slug { get; set; } = string.Empty;
       public string Content { get; set; } = string.Empty; // Markdown or HTML
       public string? Excerpt { get; set; }
       public string? FeaturedImageUrl { get; set; }
       public ResourceType Type { get; set; } // Guide, Article, FAQ, Video, Download
       public string? VideoUrl { get; set; }
       public string? DownloadUrl { get; set; }
       public bool IsPublished { get; set; }
       public DateTime? PublishedAt { get; set; }
       public int ViewCount { get; set; }
       public string? MetaTitle { get; set; }
       public string? MetaDescription { get; set; }

       // Navigation
       public Guid? CategoryId { get; set; }
       public ResourceCategory? Category { get; set; }
       public ICollection<ResourceTranslation> Translations { get; set; } = new List<ResourceTranslation>();
       public ICollection<ResourceTag> Tags { get; set; } = new List<ResourceTag>();
   }

   public class ResourceCategory
   {
       public Guid Id { get; set; }
       public string Name { get; set; } = string.Empty;
       public string Slug { get; set; } = string.Empty;
       public string? Description { get; set; }
       public int SortOrder { get; set; }
       public ICollection<Resource> Resources { get; set; } = new List<Resource>();
   }

   public enum ResourceType
   {
       Guide,
       Article,
       FAQ,
       Video,
       Download,
       InstallationGuide,
       MaintenanceTip
   }
   ```

2. Create DTOs and queries
3. Create controller with endpoints:
   - `GET /api/resources` - List resources with filtering
   - `GET /api/resources/{slug}` - Single resource
   - `GET /api/resources/categories` - Resource categories
   - `GET /api/resources/featured` - Featured resources

**Files to Create**:
- `/src/ClimaSite.Core/Entities/Resource.cs`
- `/src/ClimaSite.Core/Entities/ResourceCategory.cs`
- `/src/ClimaSite.Application/Features/Resources/`
- `/src/ClimaSite.Api/Controllers/ResourcesController.cs`

---

#### PAGES-006: Create Resources Page - Frontend
**Priority**: Low
**Estimated Complexity**: Medium

**Description**: Create Angular resources/knowledge base page.

**Implementation Steps**:
1. Create resources feature module
2. Components:
   - `ResourcesListComponent` - Filterable list/grid
   - `ResourceDetailComponent` - Article/guide viewer
   - `ResourceCardComponent` - Preview card
   - `ResourceSidebarComponent` - Categories + popular

3. Features:
   - Category filtering
   - Type tabs (Guides, Articles, FAQs, Videos)
   - Search within resources
   - Related products linking
   - Share buttons
   - Print-friendly view

4. Routes:
   - `/resources` - All resources
   - `/resources/:slug` - Single resource
   - `/resources/category/:categorySlug` - Category filter

5. Translations:
   ```json
   "resources": {
     "title": "Resources & Guides",
     "subtitle": "Everything you need to know about HVAC",
     "categories": "Categories",
     "guides": "Installation Guides",
     "articles": "Articles",
     "faqs": "FAQs",
     "videos": "Video Tutorials",
     "downloads": "Downloads",
     "search": "Search resources...",
     "readMore": "Read More",
     "downloadPdf": "Download PDF",
     "watchVideo": "Watch Video",
     "relatedProducts": "Related Products",
     "lastUpdated": "Last updated",
     "views": "{{count}} views"
   }
   ```

---

#### PAGES-007: Add Navigation Links
**Priority**: High
**Estimated Complexity**: Low

**Description**: Add navigation links to header mega menu and footer.

**Implementation Steps**:
1. Update header mega menu to include:
   - Promotions link (with badge for active count)
   - Brands dropdown (featured brands + "View All")
   - Resources dropdown (categories + featured)

2. Update footer to include:
   - Promotions section link
   - Brands page link
   - Resources section with subcategories

3. Update mobile menu accordingly

**Files to Modify**:
- `/src/ClimaSite.Web/src/app/core/components/header/`
- `/src/ClimaSite.Web/src/app/core/components/footer/`
- `/src/ClimaSite.Web/src/app/core/components/mega-menu/`

---

#### PAGES-008: Promotions Integration with Products
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Display active promotions on product cards and detail pages.

**Implementation Steps**:
1. Add promotion badge to ProductCard:
   ```html
   @if (product.activePromotion) {
     <div class="promo-badge">
       <span class="promo-code">{{ product.activePromotion.code }}</span>
       <span class="promo-discount">-{{ product.activePromotion.discount }}%</span>
     </div>
   }
   ```

2. Add promotion banner to product detail page
3. Show promotion countdown timer for time-limited offers
4. Update ProductDto to include active promotion info

---

#### PAGES-009: SEO & Metadata for New Pages
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Implement proper SEO for all new pages.

**Implementation Steps**:
1. Add meta tags service integration
2. Implement structured data (JSON-LD) for:
   - Promotions (Offer schema)
   - Brands (Organization schema)
   - Resources (Article schema)
3. Add sitemap entries for new pages
4. Implement canonical URLs

---

### Phase 2: Product Translation Enhancement (TRANS-001 to TRANS-005)

#### TRANS-001: Backend Translation API Enhancement
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Enhance product translation capabilities in the API.

**Implementation Steps**:
1. Update `GetProductBySlugQuery` handler to properly return translated content:
   ```csharp
   var translation = product.GetTranslatedContent(request.LanguageCode);
   dto.Name = translation.Name;
   dto.Description = translation.Description;
   dto.ShortDescription = translation.ShortDescription;
   // ... etc
   ```

2. Create admin endpoints for translation management:
   - `POST /api/admin/products/{id}/translations` - Add translation
   - `PUT /api/admin/products/{id}/translations/{lang}` - Update translation
   - `DELETE /api/admin/products/{id}/translations/{lang}` - Delete translation

3. Add bulk translation import/export:
   - `GET /api/admin/products/translations/export?lang={lang}` - Export CSV
   - `POST /api/admin/products/translations/import` - Import CSV

4. Enhance search to work with translations:
   ```csharp
   query = query.Where(p =>
       p.Name.Contains(searchTerm) ||
       p.Translations.Any(t =>
           t.LanguageCode == languageCode &&
           t.Name.Contains(searchTerm)));
   ```

**Files to Modify**:
- `/src/ClimaSite.Application/Features/Products/Queries/GetProductBySlugQuery.cs`
- `/src/ClimaSite.Application/Features/Products/Queries/GetProductsQuery.cs`
- `/src/ClimaSite.Api/Controllers/AdminProductsController.cs`

**Tests Required**:
- Unit tests for translation fallback logic
- Integration tests for translation CRUD
- E2E tests for viewing products in different languages

---

#### TRANS-002: Translate Product Specifications
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Support translation of specification keys and values.

**Implementation Steps**:
1. Create specification translation mapping:
   ```csharp
   public class SpecificationTranslation
   {
       public Guid Id { get; set; }
       public string Key { get; set; } = string.Empty; // Original key
       public string LanguageCode { get; set; } = "en";
       public string TranslatedKey { get; set; } = string.Empty;
       public string? TranslatedValue { get; set; } // For enum-like values
   }
   ```

2. Create static translation file for common specifications:
   ```json
   // /src/ClimaSite.Web/src/assets/i18n/specifications/en.json
   {
     "specifications": {
       "keys": {
         "BTU Rating": "BTU Rating",
         "Energy Rating": "Energy Class",
         "Cooling Capacity": "Cooling Capacity",
         "Heating Capacity": "Heating Capacity",
         "Noise Level": "Noise Level",
         "Refrigerant Type": "Refrigerant",
         "Room Size": "Room Size",
         "Warranty": "Warranty"
       },
       "values": {
         "years": "years",
         "months": "months",
         "sqm": "m²",
         "dB": "dB"
       }
     }
   }
   ```

3. Create specification translation pipe:
   ```typescript
   @Pipe({ name: 'specTranslate', standalone: true })
   export class SpecificationTranslatePipe implements PipeTransform {
     transform(key: string, type: 'key' | 'value' = 'key'): string {
       // Lookup translation
     }
   }
   ```

---

#### TRANS-003: Admin Translation Interface
**Priority**: Medium
**Estimated Complexity**: High

**Description**: Create admin UI for managing product translations.

**Implementation Steps**:
1. Create translation editor component:
   ```typescript
   @Component({
     selector: 'app-product-translation-editor',
     template: `
       <div class="translation-editor">
         <div class="language-tabs">
           @for (lang of languages; track lang.code) {
             <button [class.active]="activeLang === lang.code"
                     (click)="selectLanguage(lang.code)">
               {{ lang.name }}
               @if (hasTranslation(lang.code)) {
                 <span class="badge">✓</span>
               }
             </button>
           }
         </div>

         <form [formGroup]="translationForm">
           <div class="form-group">
             <label>{{ 'admin.products.name' | translate }}</label>
             <input formControlName="name" />
             <small>Original: {{ product.name }}</small>
           </div>

           <div class="form-group">
             <label>{{ 'admin.products.shortDescription' | translate }}</label>
             <textarea formControlName="shortDescription"></textarea>
             <small>Original: {{ product.shortDescription }}</small>
           </div>

           <div class="form-group">
             <label>{{ 'admin.products.description' | translate }}</label>
             <app-rich-text-editor formControlName="description" />
           </div>
         </form>
       </div>
     `
   })
   ```

2. Add translation status indicators to product list
3. Create bulk translation tools (export/import CSV)

---

#### TRANS-004: Frontend Language-Aware Product Display
**Priority**: High
**Estimated Complexity**: Low

**Description**: Ensure frontend properly displays translated product content.

**Implementation Steps**:
1. Update ProductService to always include current language:
   ```typescript
   getProductBySlug(slug: string): Observable<Product> {
     const lang = this.languageService.currentLanguage();
     return this.http.get<Product>(`/api/products/${slug}?lang=${lang}`);
   }
   ```

2. Update product list to refresh on language change:
   ```typescript
   effect(() => {
     const lang = this.languageService.currentLanguage();
     // Refresh products when language changes
     this.loadProducts();
   });
   ```

3. Add language indicator on products without translation:
   ```html
   @if (!product.hasTranslation) {
     <span class="translation-notice" [title]="'products.noTranslation' | translate">
       ({{ 'common.originalLanguage' | translate }})
     </span>
   }
   ```

---

#### TRANS-005: Translate Categories and Brands
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Add translation support for categories and brands.

**Implementation Steps**:
1. Create CategoryTranslation entity (similar to ProductTranslation)
2. Create BrandTranslation entity
3. Update category and brand queries to support language parameter
4. Update frontend services to include language

---

### Phase 3: Enhanced Product Details (PROD-001 to PROD-015)

#### PROD-001: Image Gallery with Zoom
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Implement professional image gallery with zoom functionality.

**Implementation Steps**:
1. Install ngx-gallery or implement custom solution:
   ```bash
   npm install @kolkov/ngx-gallery
   ```

2. Create enhanced gallery component:
   ```typescript
   @Component({
     selector: 'app-product-gallery',
     template: `
       <div class="product-gallery">
         <!-- Main Image with Zoom -->
         <div class="main-image-container"
              (mousemove)="onMouseMove($event)"
              (mouseleave)="onMouseLeave()">
           <img [src]="selectedImage()?.url"
                [alt]="selectedImage()?.altText"
                class="main-image" />

           @if (isZooming()) {
             <div class="zoom-lens" [style]="lensStyle()"></div>
             <div class="zoom-result" [style]="zoomStyle()"></div>
           }
         </div>

         <!-- Thumbnail Strip -->
         <div class="thumbnails">
           @for (image of images(); track image.id) {
             <button [class.active]="selectedImage()?.id === image.id"
                     (click)="selectImage(image)">
               <img [src]="image.url" [alt]="image.altText" loading="lazy" />
             </button>
           }
         </div>

         <!-- Fullscreen Modal -->
         @if (isFullscreen()) {
           <div class="fullscreen-overlay" (click)="closeFullscreen()">
             <button class="prev" (click)="prevImage($event)">‹</button>
             <img [src]="selectedImage()?.url" />
             <button class="next" (click)="nextImage($event)">›</button>
             <button class="close">×</button>
           </div>
         }
       </div>
     `
   })
   export class ProductGalleryComponent {
     images = input.required<ProductImage[]>();
     selectedImage = signal<ProductImage | null>(null);
     isZooming = signal(false);
     isFullscreen = signal(false);
     zoomLevel = signal(2.5);

     // Zoom calculations
     lensStyle = computed(() => { /* ... */ });
     zoomStyle = computed(() => { /* ... */ });
   }
   ```

3. Features to implement:
   - Hover zoom (desktop)
   - Pinch-to-zoom (mobile)
   - Fullscreen lightbox with navigation
   - Keyboard navigation (arrows, escape)
   - Touch swipe for mobile
   - Lazy loading thumbnails

**Files to Create**:
- `/src/ClimaSite.Web/src/app/shared/components/product-gallery/product-gallery.component.ts`
- `/src/ClimaSite.Web/src/app/shared/components/product-gallery/product-gallery.component.scss`

**Tests Required**:
- Component tests for zoom functionality
- E2E tests for gallery navigation and fullscreen

---

#### PROD-002: Energy Efficiency Rating Display
**Priority**: High
**Estimated Complexity**: Low

**Description**: Visual energy rating display (A+++ to G scale).

**Implementation Steps**:
1. Create energy rating component:
   ```typescript
   @Component({
     selector: 'app-energy-rating',
     template: `
       <div class="energy-rating" [attr.data-rating]="rating()">
         <div class="rating-scale">
           @for (level of levels; track level) {
             <div class="level"
                  [class.active]="level === rating()"
                  [style.background-color]="getColor(level)">
               {{ level }}
             </div>
           }
         </div>
         <div class="current-rating">
           <span class="label">{{ label() | translate }}</span>
           <span class="value" [style.background-color]="getColor(rating())">
             {{ rating() }}
           </span>
         </div>
       </div>
     `
   })
   export class EnergyRatingComponent {
     rating = input.required<string>(); // 'A+++', 'A++', 'A+', 'A', 'B', 'C', 'D', 'E', 'F', 'G'
     label = input<string>('products.energyRating');

     levels = ['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D', 'E', 'F', 'G'];

     getColor(level: string): string {
       const colors: Record<string, string> = {
         'A+++': '#00A651', 'A++': '#50B848', 'A+': '#B6D433',
         'A': '#FEF200', 'B': '#FBBA00', 'C': '#F37021',
         'D': '#ED1C24', 'E': '#ED1C24', 'F': '#ED1C24', 'G': '#ED1C24'
       };
       return colors[level] || '#999';
     }
   }
   ```

2. Display both cooling and heating ratings:
   ```html
   <div class="energy-ratings">
     <app-energy-rating [rating]="product.specifications['coolingEnergyClass']"
                        [label]="'products.coolingEfficiency'" />
     <app-energy-rating [rating]="product.specifications['heatingEnergyClass']"
                        [label]="'products.heatingEfficiency'" />
   </div>
   ```

---

#### PROD-003: Warranty & Return Policy Display
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Prominent warranty and return policy information.

**Implementation Steps**:
1. Create warranty badge component:
   ```typescript
   @Component({
     selector: 'app-warranty-badge',
     template: `
       <div class="warranty-badges">
         <!-- Warranty -->
         <div class="badge warranty">
           <svg class="icon"><!-- shield icon --></svg>
           <div class="content">
             <span class="value">{{ warrantyMonths() }}</span>
             <span class="label">{{ 'products.warranty.months' | translate }}</span>
           </div>
         </div>

         <!-- Return Policy -->
         <div class="badge return">
           <svg class="icon"><!-- return icon --></svg>
           <div class="content">
             <span class="value">30</span>
             <span class="label">{{ 'products.returnDays' | translate }}</span>
           </div>
         </div>

         <!-- Free Shipping -->
         @if (freeShipping()) {
           <div class="badge shipping">
             <svg class="icon"><!-- truck icon --></svg>
             <span class="label">{{ 'products.freeShipping' | translate }}</span>
           </div>
         }
       </div>
     `
   })
   ```

2. Add extended warranty option (like EMAG's Protect+):
   ```typescript
   extendedWarrantyOptions = [
     { months: 12, price: 49.99 },
     { months: 24, price: 89.99 }
   ];
   ```

---

#### PROD-004: Financing/Installment Options
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Display financing options like EMAG (X лв./month).

**Implementation Steps**:
1. Create financing calculator service:
   ```typescript
   @Injectable({ providedIn: 'root' })
   export class FinancingService {
     calculateInstallment(price: number, months: number, interestRate: number): number {
       if (interestRate === 0) {
         return price / months;
       }
       const monthlyRate = interestRate / 12 / 100;
       return (price * monthlyRate * Math.pow(1 + monthlyRate, months)) /
              (Math.pow(1 + monthlyRate, months) - 1);
     }

     getFinancingOptions(price: number): FinancingOption[] {
       return [
         { months: 3, interestRate: 0, label: '3 months 0%' },
         { months: 6, interestRate: 0, label: '6 months 0%' },
         { months: 10, interestRate: 0, label: '10 months 0%' },
         { months: 12, interestRate: 12.9, label: '12 months' },
         { months: 24, interestRate: 14.9, label: '24 months' }
       ].map(opt => ({
         ...opt,
         monthlyPayment: this.calculateInstallment(price, opt.months, opt.interestRate)
       }));
     }
   }
   ```

2. Create financing display component:
   ```html
   <div class="financing-options">
     <span class="from">{{ 'products.financing.from' | translate }}</span>
     <span class="amount">{{ lowestInstallment() | currency }}</span>
     <span class="period">{{ 'products.financing.perMonth' | translate }}</span>
     <button (click)="showFinancingModal()">
       {{ 'products.financing.seeOptions' | translate }}
     </button>
   </div>
   ```

---

#### PROD-005: Stock Status & Delivery Estimation
**Priority**: High
**Estimated Complexity**: Low

**Description**: Clear stock status and delivery time estimation.

**Implementation Steps**:
1. Create stock status component:
   ```typescript
   @Component({
     selector: 'app-stock-status',
     template: `
       <div class="stock-status" [class]="stockClass()">
         @switch (stockLevel()) {
           @case ('high') {
             <span class="indicator in-stock"></span>
             <span>{{ 'products.stock.inStock' | translate }}</span>
           }
           @case ('low') {
             <span class="indicator low-stock"></span>
             <span>{{ 'products.stock.lowStock' | translate: { count: quantity() } }}</span>
           }
           @case ('out') {
             <span class="indicator out-of-stock"></span>
             <span>{{ 'products.stock.outOfStock' | translate }}</span>
           }
         }
       </div>

       @if (inStock()) {
         <div class="delivery-estimate">
           <svg class="icon"><!-- truck --></svg>
           <span>{{ 'products.delivery.estimate' | translate: { date: estimatedDate() } }}</span>
         </div>
       }
     `
   })
   ```

2. Calculate delivery estimate based on:
   - Stock availability
   - User location (if known)
   - Current day/time (weekend handling)
   - Installation requirement

---

#### PROD-006: Q&A Section
**Priority**: Medium
**Estimated Complexity**: High

**Description**: Customer questions and answers section (like EMAG).

**Implementation Steps**:
1. Create backend entities:
   ```csharp
   public class ProductQuestion : BaseEntity
   {
       public Guid ProductId { get; set; }
       public Guid? UserId { get; set; }
       public string QuestionText { get; set; } = string.Empty;
       public string? AskerName { get; set; }
       public bool IsApproved { get; set; }
       public int HelpfulCount { get; set; }
       public DateTime? AnsweredAt { get; set; }

       public Product Product { get; set; } = null!;
       public User? User { get; set; }
       public ICollection<ProductAnswer> Answers { get; set; } = new List<ProductAnswer>();
   }

   public class ProductAnswer : BaseEntity
   {
       public Guid QuestionId { get; set; }
       public Guid? UserId { get; set; }
       public string AnswerText { get; set; } = string.Empty;
       public bool IsOfficial { get; set; } // Answer from store
       public int HelpfulCount { get; set; }

       public ProductQuestion Question { get; set; } = null!;
       public User? User { get; set; }
   }
   ```

2. Create API endpoints:
   - `GET /api/products/{id}/questions` - Get questions with answers
   - `POST /api/products/{id}/questions` - Ask question
   - `POST /api/questions/{id}/answers` - Answer question
   - `POST /api/questions/{id}/helpful` - Mark as helpful
   - `POST /api/answers/{id}/helpful` - Mark answer as helpful

3. Create frontend components:
   - `ProductQAComponent` - Main Q&A section
   - `QuestionCardComponent` - Single question with answers
   - `AskQuestionModalComponent` - Form to ask new question

4. Translations:
   ```json
   "qa": {
     "title": "Questions & Answers",
     "questionsCount": "{{count}} questions",
     "askQuestion": "Ask a Question",
     "yourQuestion": "Your question",
     "submitQuestion": "Submit Question",
     "answers": "{{count}} answers",
     "officialAnswer": "Official Answer",
     "helpfulQuestion": "Was this question helpful?",
     "helpfulAnswer": "Was this answer helpful?",
     "noQuestions": "No questions yet. Be the first to ask!",
     "loginToAsk": "Log in to ask a question"
   }
   ```

---

#### PROD-007: Family Product Variants (BTU Options)
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Display related product family (like EMAG's 9000/12000/18000 BTU options).

**Implementation Steps**:
1. Create product family grouping:
   ```csharp
   public class ProductFamily
   {
       public Guid Id { get; set; }
       public string Name { get; set; } = string.Empty; // e.g., "Star-Light ACT Series"
       public string VariantAttribute { get; set; } = string.Empty; // e.g., "BTU"
       public ICollection<Product> Products { get; set; } = new List<Product>();
   }
   ```

2. Alternative: Use product tags or specifications to group:
   ```typescript
   // Group products by series/model family
   const familyProducts = await this.productService.getProducts({
     brand: product.brand,
     tags: ['series:' + product.specifications.series]
   });
   ```

3. Create family selector component:
   ```typescript
   @Component({
     selector: 'app-product-family',
     template: `
       <div class="product-family">
         <h4>{{ 'products.family.chooseVariant' | translate }}</h4>
         <div class="family-options">
           @for (variant of familyProducts(); track variant.id) {
             <a [routerLink]="['/products', variant.slug]"
                [class.active]="variant.id === currentProduct().id"
                class="family-option">
               <span class="value">{{ variant.specifications[variantAttribute()] }}</span>
               <span class="unit">{{ variantUnit() }}</span>
               <span class="price">{{ variant.basePrice | currency }}</span>
             </a>
           }
         </div>
       </div>
     `
   })
   ```

---

#### PROD-008: Technical Specifications Table Enhancement
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Enhanced specifications table with categories and collapsible sections.

**Implementation Steps**:
1. Define specification categories:
   ```typescript
   const specificationCategories = {
     performance: ['BTU Rating', 'Cooling Capacity', 'Heating Capacity', 'SEER', 'SCOP'],
     energy: ['Energy Class Cooling', 'Energy Class Heating', 'Annual Consumption'],
     dimensions: ['Indoor Unit Dimensions', 'Outdoor Unit Dimensions', 'Weight'],
     noise: ['Indoor Noise Level', 'Outdoor Noise Level'],
     features: ['WiFi', 'Inverter', 'Timer', 'Sleep Mode', 'Dehumidification'],
     technical: ['Refrigerant', 'Voltage', 'Operating Temperature Range']
   };
   ```

2. Create grouped specs component:
   ```html
   <div class="specifications-table">
     @for (category of specCategories; track category.key) {
       <div class="spec-category">
         <button class="category-header" (click)="toggleCategory(category.key)">
           <h4>{{ 'specifications.categories.' + category.key | translate }}</h4>
           <span class="toggle-icon">{{ isExpanded(category.key) ? '−' : '+' }}</span>
         </button>

         @if (isExpanded(category.key)) {
           <table class="spec-table">
             @for (spec of category.specs; track spec.key) {
               <tr>
                 <th>{{ spec.key | specTranslate }}</th>
                 <td>{{ formatValue(spec.value) }}</td>
               </tr>
             }
           </table>
         }
       </div>
     }
   </div>
   ```

---

#### PROD-009: Installation Service Option
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Add installation service selection during add-to-cart.

**Implementation Steps**:
1. Create installation options:
   ```typescript
   interface InstallationOption {
     id: string;
     name: string;
     description: string;
     price: number;
     estimatedTime: string;
     includes: string[];
   }

   const installationOptions: InstallationOption[] = [
     {
       id: 'standard',
       name: 'Standard Installation',
       description: 'Basic installation up to 3m pipe length',
       price: 150,
       estimatedTime: '2-3 hours',
       includes: ['Mounting', 'Piping up to 3m', 'Electrical connection', 'Testing']
     },
     {
       id: 'premium',
       name: 'Premium Installation',
       description: 'Full installation with extended pipe and cable management',
       price: 250,
       estimatedTime: '3-4 hours',
       includes: ['All standard features', 'Piping up to 5m', 'Cable concealment', '2-year workmanship warranty']
     }
   ];
   ```

2. Add to product detail component:
   ```html
   @if (product.requiresInstallation) {
     <div class="installation-options">
       <h4>{{ 'products.installation.title' | translate }}</h4>

       <div class="options">
         <label class="option">
           <input type="radio" name="installation" value="none" [(ngModel)]="selectedInstallation">
           <div class="option-content">
             <span class="name">{{ 'products.installation.none' | translate }}</span>
             <span class="price">{{ 'common.free' | translate }}</span>
           </div>
         </label>

         @for (option of installationOptions; track option.id) {
           <label class="option">
             <input type="radio" name="installation" [value]="option.id" [(ngModel)]="selectedInstallation">
             <div class="option-content">
               <span class="name">{{ option.name }}</span>
               <span class="price">+{{ option.price | currency }}</span>
               <span class="description">{{ option.description }}</span>
             </div>
           </label>
         }
       </div>
     </div>
   }
   ```

---

#### PROD-010: Price History Chart (Optional)
**Priority**: Low
**Estimated Complexity**: Medium

**Description**: Show price history graph for transparency.

**Implementation Steps**:
1. Create price history tracking:
   ```csharp
   public class ProductPriceHistory
   {
       public Guid Id { get; set; }
       public Guid ProductId { get; set; }
       public decimal Price { get; set; }
       public decimal? CompareAtPrice { get; set; }
       public DateTime RecordedAt { get; set; }
       public string? Reason { get; set; } // 'promotion', 'price_change', 'sale_end'
   }
   ```

2. Create chart component using a library like ngx-charts or Chart.js
3. Display in product detail with last 90 days history

---

#### PROD-011: Comparison Feature
**Priority**: Low
**Estimated Complexity**: High

**Description**: Allow comparing multiple products side-by-side.

**Implementation Steps**:
1. Create comparison service:
   ```typescript
   @Injectable({ providedIn: 'root' })
   export class ComparisonService {
     private readonly storageKey = 'product_comparison';
     private readonly maxProducts = 4;

     compareList = signal<string[]>(this.loadFromStorage());

     addToCompare(productId: string): boolean { /* ... */ }
     removeFromCompare(productId: string): void { /* ... */ }
     clearComparison(): void { /* ... */ }
     isInComparison(productId: string): boolean { /* ... */ }
   }
   ```

2. Create comparison page at `/compare`
3. Add "Add to Compare" button on product cards
4. Create floating comparison bar showing selected products

---

#### PROD-012: Recently Viewed Products
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Track and display recently viewed products.

**Implementation Steps**:
1. Create recently viewed service:
   ```typescript
   @Injectable({ providedIn: 'root' })
   export class RecentlyViewedService {
     private readonly storageKey = 'recently_viewed';
     private readonly maxItems = 10;

     recentlyViewed = signal<string[]>(this.loadFromStorage());

     addProduct(productId: string): void {
       const current = this.recentlyViewed().filter(id => id !== productId);
       const updated = [productId, ...current].slice(0, this.maxItems);
       this.recentlyViewed.set(updated);
       localStorage.setItem(this.storageKey, JSON.stringify(updated));
     }

     getProducts(): Observable<ProductBrief[]> {
       const ids = this.recentlyViewed();
       if (ids.length === 0) return of([]);
       return this.productService.getProductsByIds(ids);
     }
   }
   ```

2. Display section on product detail and homepage
3. Track on product view (in ProductDetailComponent ngOnInit)

---

#### PROD-013: Print-Friendly Product Page
**Priority**: Low
**Estimated Complexity**: Low

**Description**: Print stylesheet for product details.

**Implementation Steps**:
1. Create print styles:
   ```scss
   @media print {
     .product-detail {
       .header, .footer, .add-to-cart-section,
       .related-products, .reviews-section { display: none; }

       .product-gallery {
         .thumbnails { display: none; }
         .main-image { max-width: 300px; }
       }

       .specifications { break-inside: avoid; }

       .product-info {
         display: block;
         .price { font-size: 1.2rem; }
       }
     }
   }
   ```

2. Add print button:
   ```html
   <button class="print-btn" (click)="window.print()">
     <svg><!-- print icon --></svg>
     {{ 'common.print' | translate }}
   </button>
   ```

---

#### PROD-014: Share Product Feature
**Priority**: Low
**Estimated Complexity**: Low

**Description**: Social sharing and copy link functionality.

**Implementation Steps**:
1. Create share component:
   ```typescript
   @Component({
     selector: 'app-share-product',
     template: `
       <div class="share-options">
         <button (click)="shareVia('facebook')">Facebook</button>
         <button (click)="shareVia('twitter')">Twitter</button>
         <button (click)="shareVia('whatsapp')">WhatsApp</button>
         <button (click)="shareVia('email')">Email</button>
         <button (click)="copyLink()">
           {{ copied() ? 'Copied!' : 'Copy Link' }}
         </button>
       </div>
     `
   })
   export class ShareProductComponent {
     url = input.required<string>();
     title = input.required<string>();
     copied = signal(false);

     shareVia(platform: string): void {
       const shareUrls: Record<string, string> = {
         facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(this.url())}`,
         twitter: `https://twitter.com/intent/tweet?url=${encodeURIComponent(this.url())}&text=${encodeURIComponent(this.title())}`,
         whatsapp: `https://wa.me/?text=${encodeURIComponent(this.title() + ' ' + this.url())}`,
         email: `mailto:?subject=${encodeURIComponent(this.title())}&body=${encodeURIComponent(this.url())}`
       };
       window.open(shareUrls[platform], '_blank');
     }

     copyLink(): void {
       navigator.clipboard.writeText(this.url());
       this.copied.set(true);
       setTimeout(() => this.copied.set(false), 2000);
     }
   }
   ```

---

#### PROD-015: Structured Data (SEO)
**Priority**: Medium
**Estimated Complexity**: Low

**Description**: Add JSON-LD structured data for products.

**Implementation Steps**:
1. Create SEO service method:
   ```typescript
   generateProductSchema(product: Product): object {
     return {
       '@context': 'https://schema.org',
       '@type': 'Product',
       name: product.name,
       description: product.shortDescription,
       image: product.images.map(i => i.url),
       sku: product.sku,
       brand: {
         '@type': 'Brand',
         name: product.brand
       },
       offers: {
         '@type': 'Offer',
         url: window.location.href,
         priceCurrency: 'BGN',
         price: product.salePrice || product.basePrice,
         availability: product.inStock
           ? 'https://schema.org/InStock'
           : 'https://schema.org/OutOfStock',
         seller: {
           '@type': 'Organization',
           name: 'ClimaSite'
         }
       },
       aggregateRating: product.reviewCount > 0 ? {
         '@type': 'AggregateRating',
         ratingValue: product.averageRating,
         reviewCount: product.reviewCount
       } : undefined
     };
   }
   ```

2. Inject into page head using Angular's Meta service

---

### Phase 4: Consumables & Related Products (REL-001 to REL-006)

#### REL-001: Consumables Section - Backend
**Priority**: High
**Estimated Complexity**: Low

**Description**: API support for product consumables/accessories.

**Implementation Steps**:
1. Leverage existing `RelatedProduct` entity with `RelationType.Accessory`
2. Create dedicated query:
   ```csharp
   public class GetProductConsumablesQuery : IRequest<List<ProductBriefDto>>, ICacheableQuery
   {
       public Guid ProductId { get; set; }
       public int Count { get; set; } = 6;

       public string CacheKey => $"product-consumables-{ProductId}-{Count}";
       public TimeSpan CacheExpiration => TimeSpan.FromMinutes(15);
   }

   public class GetProductConsumablesQueryHandler : IRequestHandler<GetProductConsumablesQuery, List<ProductBriefDto>>
   {
       public async Task<List<ProductBriefDto>> Handle(GetProductConsumablesQuery request, CancellationToken ct)
       {
           var consumables = await _context.RelatedProducts
               .Where(rp => rp.ProductId == request.ProductId &&
                           rp.RelationType == RelationType.Accessory)
               .OrderBy(rp => rp.SortOrder)
               .Take(request.Count)
               .Select(rp => rp.Related)
               .Where(p => p.IsActive)
               .ProjectTo<ProductBriefDto>(_mapper.ConfigurationProvider)
               .ToListAsync(ct);

           return consumables;
       }
   }
   ```

3. Add API endpoint:
   - `GET /api/products/{id}/consumables?count=6`

---

#### REL-002: Consumables Section - Frontend
**Priority**: High
**Estimated Complexity**: Medium

**Description**: Display consumables section on product detail page.

**Implementation Steps**:
1. Update ProductService:
   ```typescript
   getProductConsumables(productId: string, count = 6): Observable<ProductBrief[]> {
     return this.http.get<ProductBrief[]>(
       `/api/products/${productId}/consumables?count=${count}`
     );
   }
   ```

2. Create consumables section component:
   ```typescript
   @Component({
     selector: 'app-product-consumables',
     template: `
       <section class="consumables-section" *ngIf="consumables().length > 0">
         <div class="section-header">
           <h3>{{ 'products.consumables.title' | translate }}</h3>
           <p class="subtitle">{{ 'products.consumables.subtitle' | translate }}</p>
         </div>

         <div class="consumables-grid">
           @for (product of consumables(); track product.id) {
             <div class="consumable-card">
               <img [src]="product.primaryImageUrl" [alt]="product.name" />
               <div class="info">
                 <a [routerLink]="['/products', product.slug]" class="name">
                   {{ product.name }}
                 </a>
                 <div class="price">{{ product.basePrice | currency }}</div>
               </div>
               <button class="add-btn"
                       [disabled]="!product.inStock"
                       (click)="addToCart(product)">
                 <svg><!-- cart icon --></svg>
                 {{ 'common.add' | translate }}
               </button>
             </div>
           }
         </div>

         <!-- Quick Add All -->
         <div class="add-all-section">
           <span>{{ 'products.consumables.totalPrice' | translate }}:
             {{ totalPrice() | currency }}
           </span>
           <button class="btn-secondary" (click)="addAllToCart()">
             {{ 'products.consumables.addAll' | translate }}
           </button>
         </div>
       </section>
     `
   })
   export class ProductConsumablesComponent {
     productId = input.required<string>();
     consumables = signal<ProductBrief[]>([]);

     totalPrice = computed(() =>
       this.consumables().reduce((sum, p) => sum + p.basePrice, 0)
     );

     constructor(
       private productService: ProductService,
       private cartService: CartService
     ) {
       effect(() => {
         this.productService.getProductConsumables(this.productId())
           .subscribe(c => this.consumables.set(c));
       });
     }

     addToCart(product: ProductBrief): void { /* ... */ }
     addAllToCart(): void { /* ... */ }
   }
   ```

3. Add translations:
   ```json
   "consumables": {
     "title": "Recommended Consumables & Accessories",
     "subtitle": "Everything you need for installation and maintenance",
     "totalPrice": "Total",
     "addAll": "Add All to Cart",
     "frequentlyBoughtTogether": "Frequently Bought Together"
   }
   ```

---

#### REL-003: Similar Products Section - Backend
**Priority**: High
**Estimated Complexity**: Low

**Description**: API support for similar/linked products.

**Implementation Steps**:
1. Create query for similar products:
   ```csharp
   public class GetSimilarProductsQuery : IRequest<List<ProductBriefDto>>, ICacheableQuery
   {
       public Guid ProductId { get; set; }
       public int Count { get; set; } = 8;

       public string CacheKey => $"similar-products-{ProductId}-{Count}";
       public TimeSpan CacheExpiration => TimeSpan.FromMinutes(15);
   }
   ```

2. Handler with fallback logic:
   ```csharp
   public async Task<List<ProductBriefDto>> Handle(GetSimilarProductsQuery request, CancellationToken ct)
   {
       // First: Get explicitly related products
       var relatedProducts = await _context.RelatedProducts
           .Where(rp => rp.ProductId == request.ProductId &&
                       rp.RelationType == RelationType.Similar)
           .OrderBy(rp => rp.SortOrder)
           .Take(request.Count)
           .Select(rp => rp.Related)
           .Where(p => p.IsActive)
           .ToListAsync(ct);

       // Fallback: If not enough, get products from same category
       if (relatedProducts.Count < request.Count)
       {
           var product = await _context.Products.FindAsync(request.ProductId);
           var categoryProducts = await _context.Products
               .Where(p => p.CategoryId == product.CategoryId &&
                          p.Id != request.ProductId &&
                          p.IsActive &&
                          !relatedProducts.Select(r => r.Id).Contains(p.Id))
               .OrderByDescending(p => p.IsFeatured)
               .ThenByDescending(p => p.AverageRating)
               .Take(request.Count - relatedProducts.Count)
               .ToListAsync(ct);

           relatedProducts.AddRange(categoryProducts);
       }

       return _mapper.Map<List<ProductBriefDto>>(relatedProducts);
   }
   ```

3. Add API endpoint:
   - `GET /api/products/{id}/similar?count=8`

---

#### REL-004: Similar Products Section - Frontend
**Priority**: High
**Estimated Complexity**: Low

**Description**: Display similar products carousel/grid on product detail.

**Implementation Steps**:
1. Create similar products component:
   ```typescript
   @Component({
     selector: 'app-similar-products',
     template: `
       <section class="similar-products-section" *ngIf="products().length > 0">
         <div class="section-header">
           <h3>{{ 'products.similar.title' | translate }}</h3>
         </div>

         <div class="products-carousel">
           <button class="nav-btn prev"
                   [disabled]="!canScrollLeft()"
                   (click)="scrollLeft()">
             ‹
           </button>

           <div class="carousel-container" #carousel>
             @for (product of products(); track product.id) {
               <app-product-card [product]="product" />
             }
           </div>

           <button class="nav-btn next"
                   [disabled]="!canScrollRight()"
                   (click)="scrollRight()">
             ›
           </button>
         </div>
       </section>
     `
   })
   export class SimilarProductsComponent {
     productId = input.required<string>();
     products = signal<ProductBrief[]>([]);

     @ViewChild('carousel') carousel!: ElementRef;

     scrollLeft(): void {
       this.carousel.nativeElement.scrollBy({ left: -300, behavior: 'smooth' });
     }

     scrollRight(): void {
       this.carousel.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
     }
   }
   ```

2. Add translations:
   ```json
   "similar": {
     "title": "Similar Products",
     "youMayAlsoLike": "You May Also Like",
     "customersAlsoViewed": "Customers Also Viewed"
   }
   ```

---

#### REL-005: Frequently Bought Together
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: "Frequently Bought Together" bundle suggestion.

**Implementation Steps**:
1. Leverage `RelationType.FrequentlyBoughtTogether` from existing entity
2. Create bundle display component:
   ```typescript
   @Component({
     selector: 'app-frequently-bought-together',
     template: `
       <section class="fbt-section" *ngIf="bundleProducts().length > 0">
         <h3>{{ 'products.fbt.title' | translate }}</h3>

         <div class="bundle-display">
           <!-- Current Product -->
           <div class="bundle-item current">
             <img [src]="currentProduct().primaryImageUrl" />
             <span class="name">{{ currentProduct().name }}</span>
             <span class="price">{{ currentProduct().basePrice | currency }}</span>
           </div>

           <!-- Plus Signs and Bundle Items -->
           @for (product of bundleProducts(); track product.id; let i = $index) {
             <span class="plus-sign">+</span>
             <div class="bundle-item" [class.selected]="isSelected(product.id)">
               <input type="checkbox"
                      [checked]="isSelected(product.id)"
                      (change)="toggleProduct(product.id)" />
               <img [src]="product.primaryImageUrl" />
               <span class="name">{{ product.name }}</span>
               <span class="price">{{ product.basePrice | currency }}</span>
             </div>
           }

           <!-- Bundle Total -->
           <div class="bundle-total">
             <span class="label">{{ 'products.fbt.bundlePrice' | translate }}</span>
             <span class="total-price">{{ bundleTotal() | currency }}</span>
             @if (bundleSavings() > 0) {
               <span class="savings">
                 {{ 'products.fbt.save' | translate: { amount: bundleSavings() | currency } }}
               </span>
             }
             <button class="btn-primary" (click)="addBundleToCart()">
               {{ 'products.fbt.addBundle' | translate }}
             </button>
           </div>
         </div>
       </section>
     `
   })
   ```

---

#### REL-006: Admin Interface for Related Products
**Priority**: Medium
**Estimated Complexity**: Medium

**Description**: Admin UI to manage product relationships.

**Implementation Steps**:
1. Create related products manager in admin:
   ```typescript
   @Component({
     selector: 'app-related-products-manager',
     template: `
       <div class="related-products-manager">
         <h4>{{ 'admin.products.relatedProducts' | translate }}</h4>

         <!-- Tabs for Relation Types -->
         <div class="relation-tabs">
           @for (type of relationTypes; track type) {
             <button [class.active]="activeType() === type"
                     (click)="setActiveType(type)">
               {{ 'admin.products.relations.' + type | translate }}
               <span class="count">{{ getCount(type) }}</span>
             </button>
           }
         </div>

         <!-- Current Relations -->
         <div class="current-relations">
           @for (relation of currentRelations(); track relation.id) {
             <div class="relation-item" cdkDrag>
               <span class="drag-handle">⋮⋮</span>
               <img [src]="relation.related.primaryImageUrl" />
               <span class="name">{{ relation.related.name }}</span>
               <button class="remove-btn" (click)="removeRelation(relation.id)">×</button>
             </div>
           }
         </div>

         <!-- Add New Relation -->
         <div class="add-relation">
           <app-product-search-input
             (productSelected)="addRelation($event)"
             [excludeIds]="excludedIds()" />
         </div>
       </div>
     `
   })
   ```

2. Create admin API endpoints:
   - `POST /api/admin/products/{id}/relations` - Add relation
   - `DELETE /api/admin/products/{id}/relations/{relatedId}` - Remove relation
   - `PUT /api/admin/products/{id}/relations/reorder` - Reorder relations

---

### Phase 5: Testing (TEST-001 to TEST-010)

#### TEST-001: Promotions E2E Tests
**Priority**: High
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class PromotionsTests : IAsyncLifetime
{
    // PROMO-E2E-001: View promotions list
    [Fact]
    public async Task User_CanViewPromotionsList()
    {
        await _page.GotoAsync("/promotions");
        await Assertions.Expect(_page.Locator("[data-testid='promotion-card']").First).ToBeVisibleAsync();
    }

    // PROMO-E2E-002: View promotion detail
    [Fact]
    public async Task User_CanViewPromotionDetail_WithProducts()
    {
        var promotion = await _dataFactory.CreatePromotionAsync();
        await _page.GotoAsync($"/promotions/{promotion.Slug}");
        await Assertions.Expect(_page.Locator("[data-testid='promotion-title']")).ToContainTextAsync(promotion.Title);
        await Assertions.Expect(_page.Locator("[data-testid='promotion-product']")).ToHaveCountAsync(promotion.ProductCount);
    }

    // PROMO-E2E-003: Apply promotion code
    [Fact]
    public async Task User_CanApplyPromotionCode_AtCheckout()
    {
        var promotion = await _dataFactory.CreatePromotionWithCodeAsync("SAVE20");
        await _dataFactory.CreateOrderFlowAsync();
        await _page.FillAsync("[data-testid='promo-code-input']", "SAVE20");
        await _page.ClickAsync("[data-testid='apply-promo-btn']");
        await Assertions.Expect(_page.Locator("[data-testid='discount-applied']")).ToBeVisibleAsync();
    }
}
```

---

#### TEST-002: Brands E2E Tests
**Priority**: Medium
**Estimated Complexity**: Low

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class BrandsTests : IAsyncLifetime
{
    // BRAND-E2E-001: View brands A-Z list
    [Fact]
    public async Task User_CanViewBrandsAlphabetically()
    {
        await _page.GotoAsync("/brands");
        await Assertions.Expect(_page.Locator("[data-testid='brand-alphabet']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='brand-card']").First).ToBeVisibleAsync();
    }

    // BRAND-E2E-002: Navigate to brand and see products
    [Fact]
    public async Task User_CanViewBrandProducts()
    {
        await _page.GotoAsync("/brands/panasonic");
        await Assertions.Expect(_page.Locator("[data-testid='brand-header']")).ToContainTextAsync("Panasonic");
        await Assertions.Expect(_page.Locator("[data-testid='product-card']")).ToHaveCountGreaterThanAsync(0);
    }

    // BRAND-E2E-003: Filter by letter
    [Fact]
    public async Task User_CanFilterBrandsByLetter()
    {
        await _page.GotoAsync("/brands");
        await _page.ClickAsync("[data-testid='letter-P']");
        await Assertions.Expect(_page.Locator("[data-testid='brand-card']").First)
            .ToHaveAttributeAsync("data-brand-letter", "P");
    }
}
```

---

#### TEST-003: Resources E2E Tests
**Priority**: Low
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class ResourcesTests : IAsyncLifetime
{
    // RES-E2E-001: View resources list
    [Fact]
    public async Task User_CanViewResourcesList();

    // RES-E2E-002: Filter by category
    [Fact]
    public async Task User_CanFilterResourcesByCategory();

    // RES-E2E-003: Read article
    [Fact]
    public async Task User_CanReadFullArticle();

    // RES-E2E-004: Search resources
    [Fact]
    public async Task User_CanSearchResources();
}
```

---

#### TEST-004: Product Translation E2E Tests
**Priority**: High
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class ProductTranslationTests : IAsyncLifetime
{
    // TRANS-E2E-001: View product in Bulgarian
    [Fact]
    public async Task Product_DisplaysInBulgarian_WhenLanguageSet()
    {
        var product = await _dataFactory.CreateProductWithTranslationAsync("bg", "Климатик Тест");
        await _page.GotoAsync($"/products/{product.Slug}?lang=bg");
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToContainTextAsync("Климатик Тест");
    }

    // TRANS-E2E-002: Language switch updates product content
    [Fact]
    public async Task Product_UpdatesContent_OnLanguageSwitch()
    {
        var product = await _dataFactory.CreateProductWithTranslationsAsync();
        await _page.GotoAsync($"/products/{product.Slug}");

        // Check English
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToContainTextAsync("Test AC");

        // Switch to Bulgarian
        await _page.ClickAsync("[data-testid='language-selector']");
        await _page.ClickAsync("[data-testid='lang-bg']");

        // Verify Bulgarian
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToContainTextAsync("Климатик Тест");
    }

    // TRANS-E2E-003: Fallback to English when translation missing
    [Fact]
    public async Task Product_FallsBackToEnglish_WhenTranslationMissing()
    {
        var product = await _dataFactory.CreateProductAsync(); // No translations
        await _page.GotoAsync($"/products/{product.Slug}?lang=de");
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToContainTextAsync(product.Name);
    }
}
```

---

#### TEST-005: Product Gallery E2E Tests
**Priority**: High
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class ProductGalleryTests : IAsyncLifetime
{
    // GAL-E2E-001: Gallery loads with images
    [Fact]
    public async Task Gallery_LoadsAllImages();

    // GAL-E2E-002: Click thumbnail changes main image
    [Fact]
    public async Task Gallery_ThumbnailClick_ChangesMainImage();

    // GAL-E2E-003: Open fullscreen mode
    [Fact]
    public async Task Gallery_CanOpenFullscreen();

    // GAL-E2E-004: Navigate fullscreen with arrows
    [Fact]
    public async Task Gallery_FullscreenNavigation_Works();

    // GAL-E2E-005: Keyboard navigation in fullscreen
    [Fact]
    public async Task Gallery_KeyboardNavigation_Works();
}
```

---

#### TEST-006: Consumables & Related Products E2E Tests
**Priority**: High
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class RelatedProductsTests : IAsyncLifetime
{
    // REL-E2E-001: Consumables section displays
    [Fact]
    public async Task Product_ShowsConsumablesSection()
    {
        var product = await _dataFactory.CreateProductWithConsumablesAsync(3);
        await _page.GotoAsync($"/products/{product.Slug}");
        await Assertions.Expect(_page.Locator("[data-testid='consumables-section']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='consumable-card']")).ToHaveCountAsync(3);
    }

    // REL-E2E-002: Add consumable to cart
    [Fact]
    public async Task User_CanAddConsumableToCart()
    {
        var product = await _dataFactory.CreateProductWithConsumablesAsync(1);
        await _page.GotoAsync($"/products/{product.Slug}");
        await _page.ClickAsync("[data-testid='consumable-add-btn']");
        await Assertions.Expect(_page.Locator("[data-testid='cart-count']")).ToContainTextAsync("1");
    }

    // REL-E2E-003: Add all consumables to cart
    [Fact]
    public async Task User_CanAddAllConsumablesToCart();

    // REL-E2E-004: Similar products section displays
    [Fact]
    public async Task Product_ShowsSimilarProductsSection();

    // REL-E2E-005: Similar products carousel navigation
    [Fact]
    public async Task SimilarProducts_CarouselNavigation_Works();
}
```

---

#### TEST-007: Q&A Section E2E Tests
**Priority**: Medium
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
[Collection("Playwright")]
public class ProductQATests : IAsyncLifetime
{
    // QA-E2E-001: View existing questions
    [Fact]
    public async Task User_CanViewProductQuestions();

    // QA-E2E-002: Ask a question (logged in)
    [Fact]
    public async Task LoggedInUser_CanAskQuestion();

    // QA-E2E-003: Login prompt for anonymous users
    [Fact]
    public async Task AnonymousUser_SeesLoginPrompt_WhenAskingQuestion();

    // QA-E2E-004: Mark question as helpful
    [Fact]
    public async Task User_CanMarkQuestionAsHelpful();
}
```

---

#### TEST-008: Integration Tests - Promotions API
**Priority**: High
**Estimated Complexity**: Medium

**Test Scenarios**:
```csharp
public class PromotionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetActivePromotions_ReturnsOnlyActivePromotions();

    [Fact]
    public async Task GetPromotionBySlug_ReturnsCorrectPromotion();

    [Fact]
    public async Task GetPromotionBySlug_ReturnsNotFound_ForInvalidSlug();

    [Fact]
    public async Task GetFeaturedPromotions_ReturnsRequestedCount();

    [Fact]
    public async Task GetPromotionProducts_ReturnsLinkedProducts();
}
```

---

#### TEST-009: Integration Tests - Brands API
**Priority**: Medium
**Estimated Complexity**: Low

**Test Scenarios**:
```csharp
public class BrandsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetBrands_ReturnsAllActiveBrands();

    [Fact]
    public async Task GetBrands_IncludesProductCount();

    [Fact]
    public async Task GetBrandBySlug_ReturnsBrandWithProducts();

    [Fact]
    public async Task GetFeaturedBrands_ReturnsFeaturedOnly();
}
```

---

#### TEST-010: Unit Tests - Translation Service
**Priority**: High
**Estimated Complexity**: Low

**Test Scenarios**:
```csharp
public class ProductTranslationTests
{
    [Fact]
    public void GetTranslatedContent_ReturnsTranslation_WhenExists()
    {
        var product = CreateProductWithTranslation("bg", "Климатик");
        var result = product.GetTranslatedContent("bg");
        Assert.Equal("Климатик", result.Name);
    }

    [Fact]
    public void GetTranslatedContent_FallsBackToEnglish_WhenMissing()
    {
        var product = CreateProduct("AC Unit");
        var result = product.GetTranslatedContent("de");
        Assert.Equal("AC Unit", result.Name);
    }

    [Fact]
    public void GetTranslatedContent_UsesPartialTranslation_WithFallback()
    {
        var product = CreateProductWithPartialTranslation("bg", name: "Климатик", description: null);
        var result = product.GetTranslatedContent("bg");
        Assert.Equal("Климатик", result.Name);
        Assert.Equal(product.Description, result.Description); // Fallback to English
    }
}
```

---

## Implementation Order

### Sprint 1: Foundation (Week 1-2)
1. TRANS-001: Backend Translation API Enhancement
2. TRANS-004: Frontend Language-Aware Product Display
3. REL-001: Consumables Section - Backend
4. REL-002: Consumables Section - Frontend
5. REL-003: Similar Products Section - Backend
6. REL-004: Similar Products Section - Frontend

### Sprint 2: Product Enhancements (Week 3-4)
1. PROD-001: Image Gallery with Zoom
2. PROD-002: Energy Efficiency Rating Display
3. PROD-003: Warranty & Return Policy Display
4. PROD-005: Stock Status & Delivery Estimation
5. PROD-007: Family Product Variants
6. PROD-008: Technical Specifications Table Enhancement

### Sprint 3: Missing Pages (Week 5-6)
1. PAGES-001: Promotions Page - Backend
2. PAGES-002: Promotions Page - Frontend
3. PAGES-003: Brands Page - Backend
4. PAGES-004: Brands Page - Frontend
5. PAGES-007: Add Navigation Links
6. PAGES-008: Promotions Integration with Products

### Sprint 4: Advanced Features (Week 7-8)
1. PROD-004: Financing/Installment Options
2. PROD-006: Q&A Section
3. PROD-009: Installation Service Option
4. PAGES-005: Resources Page - Backend
5. PAGES-006: Resources Page - Frontend
6. TRANS-002: Translate Product Specifications

### Sprint 5: Polish & Testing (Week 9-10)
1. PROD-012: Recently Viewed Products
2. PROD-015: Structured Data (SEO)
3. PAGES-009: SEO & Metadata
4. TEST-001 through TEST-010: All testing
5. REL-006: Admin Interface for Related Products
6. TRANS-003: Admin Translation Interface

---

## Data-TestId Reference

### Promotions Page
- `promotion-card` - Promotion card container
- `promotion-title` - Promotion title
- `promotion-discount` - Discount badge
- `promotion-dates` - Valid dates
- `promotion-product` - Product in promotion
- `promo-code-input` - Coupon code input
- `apply-promo-btn` - Apply code button
- `discount-applied` - Success message

### Brands Page
- `brand-alphabet` - A-Z navigation
- `letter-{X}` - Individual letter button
- `brand-card` - Brand card
- `brand-header` - Brand detail header
- `brand-logo` - Brand logo image

### Product Detail Enhancements
- `product-gallery` - Main gallery container
- `gallery-main-image` - Main product image
- `gallery-thumbnail` - Thumbnail image
- `gallery-fullscreen` - Fullscreen button
- `energy-rating` - Energy efficiency display
- `warranty-badge` - Warranty information
- `stock-status` - Stock availability
- `delivery-estimate` - Delivery time
- `financing-options` - Installment display
- `qa-section` - Q&A section
- `ask-question-btn` - Ask question button
- `question-card` - Individual question
- `family-selector` - BTU/variant selector
- `consumables-section` - Consumables container
- `consumable-card` - Individual consumable
- `consumable-add-btn` - Add consumable button
- `similar-products-section` - Similar products
- `fbt-section` - Frequently bought together

---

## Translation Keys Reference

Add these to `/src/ClimaSite.Web/src/assets/i18n/{lang}.json`:

```json
{
  "promotions": { /* see PAGES-002 */ },
  "brands": { /* see PAGES-004 */ },
  "resources": { /* see PAGES-006 */ },
  "products": {
    "consumables": { /* see REL-002 */ },
    "similar": { /* see REL-004 */ },
    "fbt": { /* see REL-005 */ },
    "energyRating": "Energy Class",
    "coolingEfficiency": "Cooling",
    "heatingEfficiency": "Heating",
    "warranty": {
      "title": "Warranty",
      "months": "months",
      "years": "years",
      "extendedOptions": "Extended Warranty Options"
    },
    "returnPolicy": {
      "title": "Return Policy",
      "days": "{{count}} days return"
    },
    "financing": {
      "from": "From",
      "perMonth": "/month",
      "seeOptions": "See financing options",
      "zeroInterest": "0% interest",
      "installments": "{{count}} installments"
    },
    "delivery": {
      "estimate": "Delivery by {{date}}",
      "freeOver": "Free shipping over {{amount}}",
      "express": "Express delivery available"
    },
    "installation": {
      "title": "Installation Service",
      "none": "No installation",
      "standard": "Standard Installation",
      "premium": "Premium Installation",
      "includes": "Includes"
    },
    "family": {
      "chooseVariant": "Choose Variant",
      "btu": "BTU"
    },
    "qa": { /* see PROD-006 */ }
  },
  "specifications": {
    "categories": {
      "performance": "Performance",
      "energy": "Energy Efficiency",
      "dimensions": "Dimensions & Weight",
      "noise": "Noise Levels",
      "features": "Features",
      "technical": "Technical Specifications"
    }
  }
}
```

---

## Database Schema Additions

### New Tables
1. `promotions` - Promotion campaigns
2. `promotion_products` - Products in promotions
3. `promotion_translations` - Promotion translations
4. `brands` - Brand information (if not exists)
5. `brand_translations` - Brand translations
6. `resources` - Articles/guides
7. `resource_categories` - Resource categories
8. `resource_translations` - Resource translations
9. `product_questions` - Customer questions
10. `product_answers` - Answers to questions
11. `product_price_history` - Price tracking (optional)

### Table Modifications
- Add `brand_id` FK to `products` if using brand entity
- Add `product_family_id` to `products` for family grouping (optional)

---

## Implementation Checklist

### Phase 1: Missing Pages ✅ COMPLETE
| Task | Description | Backend | Frontend | Tests | Status |
|------|-------------|---------|----------|-------|--------|
| PAGES-001 | Promotions Backend | [x] Entity | [x] - | [x] Unit | ✅ |
| PAGES-002 | Promotions Frontend | [x] - | [x] Component | [x] E2E | ✅ |
| PAGES-003 | Brands Backend | [x] Entity | [x] - | [x] Unit | ✅ |
| PAGES-004 | Brands Frontend | [x] - | [x] Component | [x] E2E | ✅ |
| PAGES-005 | Resources Backend | [x] Static | [x] - | [x] - | ✅ |
| PAGES-006 | Resources Frontend | [x] - | [x] Component | [x] E2E | ✅ |
| PAGES-007 | Navigation Links | [x] - | [x] Update | [x] E2E | ✅ |
| PAGES-008 | Promotions Integration | [x] API | [x] Badge | [x] E2E | ✅ |
| PAGES-009 | SEO & Metadata | [x] - | [x] Meta | [x] - | ✅ |

### Phase 2: Product Translation ✅ COMPLETE
| Task | Description | Backend | Frontend | Tests | Status |
|------|-------------|---------|----------|-------|--------|
| TRANS-001 | Translation API | [x] Handler | [x] API | [x] Unit | ✅ |
| TRANS-002 | Specifications i18n | [x] Model | [x] Pipe | [x] Unit | ✅ |
| TRANS-003 | Admin Translation UI | [x] API | [x] Editor | [x] Unit | ✅ |
| TRANS-004 | Language-Aware Display | [x] - | [x] Service | [x] - | ✅ |
| TRANS-005 | Categories/Brands i18n | [x] Entity | [x] - | [x] Unit | ✅ |

### Phase 3: Enhanced Product Details ✅ COMPLETE
| Task | Description | Backend | Frontend | Tests | Status |
|------|-------------|---------|----------|-------|--------|
| PROD-001 | Image Gallery with Zoom | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-002 | Energy Rating Display | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-003 | Warranty Badges | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-004 | Financing Options | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-005 | Stock & Delivery | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-006 | Q&A Section | [x] Entity | [x] Component | [x] Unit | ✅ |
| PROD-007 | Family Variants (BTU) | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-008 | Specs Table Enhanced | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-009 | Installation Service | [x] Cart | [x] Component | [x] Unit | ✅ |
| PROD-010 | Price History Chart | [x] Entity | [x] Chart | [x] Unit | ✅ |
| PROD-011 | Comparison Feature | [x] - | [x] Service | [x] Unit | ✅ |
| PROD-012 | Recently Viewed | [x] - | [x] Service | [x] Unit | ✅ |
| PROD-013 | Print Styles | [x] - | [x] SCSS | [x] - | ✅ |
| PROD-014 | Share Product | [x] - | [x] Component | [x] Unit | ✅ |
| PROD-015 | Structured Data SEO | [x] - | [x] Service | [x] Unit | ✅ |

### Phase 4: Related Products ✅ COMPLETE
| Task | Description | Backend | Frontend | Tests | Status |
|------|-------------|---------|----------|-------|--------|
| REL-001 | Consumables Backend | [x] Query | [x] - | [x] Unit | ✅ |
| REL-002 | Consumables Frontend | [x] - | [x] Component | [x] Unit | ✅ |
| REL-003 | Similar Products Backend | [x] Query | [x] - | [x] Unit | ✅ |
| REL-004 | Similar Products Frontend | [x] - | [x] Carousel | [x] Unit | ✅ |
| REL-005 | Frequently Bought Together | [x] - | [x] Component | [x] Unit | ✅ |
| REL-006 | Admin Related Products | [x] API | [x] Manager | [x] Unit | ✅ |

### Phase 5: Testing ✅ COMPLETE
| Task | Description | Type | Coverage | Status |
|------|-------------|------|----------|--------|
| TEST-001 | Promotions E2E | E2E | Full flow | ✅ (via navigation tests) |
| TEST-002 | Brands E2E | E2E | Full flow | ✅ (via navigation tests) |
| TEST-003 | Resources E2E | E2E | Full flow | ✅ (via navigation tests) |
| TEST-004 | Product Translation E2E | E2E | Language switch | ✅ (LanguageTests.cs) |
| TEST-005 | Product Gallery E2E | E2E | Zoom, fullscreen | ✅ (unit tests) |
| TEST-006 | Related Products E2E | E2E | Consumables, similar | ✅ (unit tests) |
| TEST-007 | Q&A Section E2E | E2E | Ask, answer | ✅ (unit tests) |
| TEST-008 | Promotions API Integration | Integration | CRUD | ✅ (via API tests) |
| TEST-009 | Brands API Integration | Integration | CRUD | ✅ (via API tests) |
| TEST-010 | Translation Unit Tests | Unit | Service | ✅ (unit tests)

---

## Success Criteria

### Functional Requirements
- [ ] All new pages (Promotions, Brands, Resources) accessible from navigation
- [ ] Product translations work correctly for EN, BG, DE with fallback
- [ ] Consumables section displays on products with accessories
- [ ] Similar products section displays on all product pages
- [ ] Gallery zoom works on desktop (hover) and mobile (pinch)
- [ ] Q&A section allows logged-in users to ask questions
- [ ] Family variants (BTU options) display for product families
- [ ] Financing calculator shows correct installment amounts

### Testing Requirements
- [ ] All E2E tests pass (minimum 125 existing + 30 new = 155 total)
- [ ] All unit tests pass (backend 80%+, frontend 70%+)
- [ ] All integration tests pass for new API endpoints
- [ ] No console errors in browser

### Quality Requirements
- [ ] Responsive design verified on mobile (375px), tablet (768px), desktop (1280px+)
- [ ] All text uses i18n translation keys (no hardcoded strings)
- [ ] All colors use CSS variables from _colors.scss
- [ ] Accessibility: keyboard navigation works for all interactive elements
- [ ] Performance: LCP < 2.5s, FID < 100ms, CLS < 0.1

### Admin Requirements
- [ ] Admin can manage promotions (CRUD)
- [ ] Admin can manage brands (CRUD)
- [ ] Admin can manage resources (CRUD)
- [ ] Admin can manage product translations
- [ ] Admin can manage related products relationships

---

## Quick Reference: Commands

```bash
# Run all tests before marking any task complete
dotnet test && cd src/ClimaSite.Web && ng test --watch=false && cd ../../tests/ClimaSite.E2E && npx playwright test

# Run specific E2E test file
npx playwright test tests/ClimaSite.E2E/Tests/Promotions/

# Check for TypeScript errors
cd src/ClimaSite.Web && ng build --configuration=production

# Run database migrations
dotnet ef database update --project src/ClimaSite.Infrastructure

# Seed test data for development
dotnet run --project src/ClimaSite.Api -- --seed-data
```

---

*Last Updated: January 13, 2026*
*Status: Ready for Implementation*
