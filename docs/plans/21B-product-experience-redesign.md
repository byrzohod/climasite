# Plan 21B: Product Experience Redesign

> **Design Direction:** "Nordic Tech" - Clean, professional, trust-building
> **Estimated Effort:** 8-10 sprints (160-200 hours)
> **Priority:** High
> **Dependencies:** Plan 21A (Global UI Redesign) for design tokens and base components

---

## Table of Contents

1. [Overview](#1-overview)
2. [Component Redesigns](#2-component-redesigns)
3. [Task List](#3-task-list)
4. [Technical Specifications](#4-technical-specifications)
5. [HVAC-Specific Features](#5-hvac-specific-features)
6. [Dependencies](#6-dependencies)
7. [Testing Checklist](#7-testing-checklist)

---

## 1. Overview

### 1.1 Goals

| Goal | Description | Success Metric |
|------|-------------|----------------|
| **Enhanced Product Discovery** | Make it easier for customers to find and compare HVAC products | +25% product page engagement |
| **Trust Building** | Display energy ratings, certifications, and specs prominently | +15% conversion rate |
| **HVAC-Specific UX** | Room size calculators, BTU recommendations | Reduced support inquiries |
| **Visual Excellence** | Premium Nordic Tech aesthetic | NPS score improvement |
| **Mobile-First** | Optimized touch interactions and swipe gestures | Mobile conversion parity |
| **Comparison Shopping** | Enable side-by-side product comparison | Feature adoption >30% |

### 1.2 Current State Analysis

| Component | Current State | Issues |
|-----------|---------------|--------|
| Product Card | 220px image height, basic layout | Cramped, no energy badge, no comparison |
| Product Grid | Simple auto-fill grid | No featured spotlight, no bento variations |
| Product Detail | Functional but basic | No sticky cart, basic gallery, no immersive experience |
| Filters | Text-based checkboxes | No visual BTU/room calculator, not HVAC-optimized |
| Category Pages | Placeholder only | Not implemented |
| Similar Products | Has price label bug | Lines 52-54 show swapped labels (original/sale) |
| Energy Rating | Exists but underutilized | Not on product cards |

### 1.3 Design Direction: Nordic Tech

**Core Principles:**
- **Clean Lines:** Generous whitespace, minimal borders, subtle shadows
- **Trust Signals:** Energy ratings, certifications, warranty prominently displayed
- **Functional Beauty:** Every element serves a purpose
- **Cool Palette:** Blues, grays, white with accent colors for CTAs
- **Professional Feel:** Targeted at homeowners making significant purchases

**Visual Characteristics:**
- Bento grid layouts for dynamic product showcases
- Glass morphism accents for premium feel
- Micro-interactions that feel responsive but not playful
- Technical specifications presented beautifully
- Energy efficiency as a first-class visual element

### 1.4 Success Metrics

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Product Page Bounce Rate | TBD | -20% | Analytics |
| Add to Cart Rate | TBD | +25% | Analytics |
| Product Comparison Usage | 0% | 30%+ | Feature tracking |
| Mobile Conversion | TBD | Parity with desktop | Analytics |
| Time to Purchase Decision | TBD | -15% | Session analysis |
| Customer Satisfaction | TBD | +10pts NPS | Surveys |

---

## 2. Component Redesigns

### 2.1 Product Card Enhancement

**Current State:**
```
src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts
```

**Issues Identified:**
- Image height fixed at 220px - cramped for HVAC products
- No energy efficiency badge
- No quick specs preview (BTU, noise level)
- No comparison checkbox
- Wishlist button only visible on hover

**Proposed Changes:**

```
+------------------------------------------+
|  [ ] Compare                    [Heart]  |  <- Comparison checkbox + Wishlist
+------------------------------------------+
|                                          |
|     [Product Image - 280px height]       |
|                                          |
|  +--------+                              |
|  | A++    |  <- Energy Badge             |
|  +--------+                              |
+------------------------------------------+
|  BRAND NAME                              |
|  Product Title (2 lines max)             |
|                                          |
|  BTU: 12,000 | Noise: 42dB | Room: 40m2  |  <- Quick specs
|                                          |
|  [Stars] 4.5 (123 reviews)               |
|                                          |
|  EUR 899.00  <strike>1,199</strike> -25% |
|                                          |
|  [=====  Add to Cart  =====]             |
+------------------------------------------+
```

**New Features:**
1. **Energy Badge:** Prominent A+++ to G rating badge on image
2. **Quick Specs Row:** BTU, noise level, room size coverage
3. **Comparison Checkbox:** Toggle to add product to compare list
4. **Expanded Image:** 280px minimum height for better product visibility
5. **Always-Visible Wishlist:** No hover required on mobile
6. **Stock Indicator:** Visual low stock/in stock badge

### 2.2 Product Grid Layouts

**Current State:**
```
src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts
```

**Issues:**
- Single grid layout only
- No featured product spotlight
- No promotional bento variations

**Proposed Layouts:**

#### Layout A: Standard Grid (Default)
```
+-------+-------+-------+-------+
| Card  | Card  | Card  | Card  |
+-------+-------+-------+-------+
| Card  | Card  | Card  | Card  |
+-------+-------+-------+-------+
```

#### Layout B: Featured Spotlight
```
+---------------+-------+-------+
|               | Card  | Card  |
|   Featured    +-------+-------+
|    Product    | Card  | Card  |
+---------------+-------+-------+
| Card  | Card  | Card  | Card  |
+-------+-------+-------+-------+
```

#### Layout C: Bento Mixed
```
+-------+---------------+-------+
| Card  |               | Card  |
+-------+   Large Card  +-------+
| Card  |               | Card  |
+-------+---------------+-------+
| Card  | Card  | Card  | Card  |
+-------+-------+-------+-------+
```

#### Layout D: Category Showcase
```
+-----------------------------------+
|     Category Hero Banner          |
+-------+-------+-------+-------+---+
| Card  | Card  | Card  | Card  |   |
+-------+-------+-------+-------+   |
| Card  | Card  | Card  | Card  | F |
+-------+-------+-------+-------+ i |
|      [Load More / Infinite]    | l |
+--------------------------------+ t |
```

### 2.3 Product Detail Page Redesign

**Current State:**
```
src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts
```

**Issues:**
- Gallery is functional but not immersive
- No sticky add-to-cart on scroll
- Tabs work but could be accordion on mobile
- Specs table is basic

**Proposed Layout:**

```
+--------------------------------------------------+
| Breadcrumb: Home / AC Units / Split Systems      |
+--------------------------------------------------+

+----------------------+---------------------------+
|                      |  BRAND                    |
|  [Main Gallery]      |  Product Title            |
|                      |  [Stars] 4.8 (256 reviews)|
|  +--+--+--+--+--+    |                           |
|  |T1|T2|T3|T4|360|   |  +---+ A++ +---+          |
|  +--+--+--+--+--+    |  Energy Rating            |
|                      |                           |
|  [Lifestyle Images]  |  EUR 1,299.00             |
|                      |  <s>1,599</s> Save 19%    |
|                      |                           |
|                      |  +---------------------+  |
|                      |  | Key Specs Grid     |  |
|                      |  | BTU: 18,000        |  |
|                      |  | Room: 50-70m2      |  |
|                      |  | Noise: 38dB        |  |
|                      |  | Refrigerant: R32   |  |
|                      |  +---------------------+  |
|                      |                           |
|                      |  Variant: [12K][18K][24K] |
|                      |                           |
|                      |  Qty: [-] 1 [+]           |
|                      |                           |
|                      |  [=== Add to Cart ===]    |
|                      |  [Compare] [Share] [Save] |
|                      |                           |
|                      |  +---------------------+  |
|                      |  | Trust Badges        |  |
|                      |  | [2yr Warranty]      |  |
|                      |  | [Free Delivery]     |  |
|                      |  | [Pro Installation]  |  |
|                      |  +---------------------+  |
+----------------------+---------------------------+

+--------------------------------------------------+
| [Description] [Specs] [Reviews] [Q&A] [Install]  |
+--------------------------------------------------+
|                                                  |
| Tab Content with smooth transitions              |
|                                                  |
+--------------------------------------------------+

+--------------------------------------------------+
| Room Size Calculator                             |
| [Visual room selector with BTU recommendation]   |
+--------------------------------------------------+

+--------------------------------------------------+
| Installation Services                            |
+--------------------------------------------------+

+--------------------------------------------------+
| Similar Products Carousel                        |
+--------------------------------------------------+

+--------------------------------------------------+
| Recently Viewed                                  |
+--------------------------------------------------+
```

**New Features:**
1. **Sticky Cart Bar:** Appears on scroll with product name, price, add to cart
2. **360 View Placeholder:** Ready for future 360 product views
3. **Lifestyle Image Section:** Installation photos, room context
4. **Key Specs Grid:** Visual HVAC specs at a glance
5. **Variant Selector:** Visual size/capacity selector
6. **Trust Badge Section:** Enhanced warranty, delivery, installation badges
7. **Room Calculator Integration:** Embedded BTU calculator

### 2.4 Product Gallery Enhancement

**Current State:**
```
src/ClimaSite.Web/src/app/shared/components/product-gallery/product-gallery.component.ts
```

**Current Features:**
- Zoom on hover (desktop)
- Lightbox with pinch zoom (mobile)
- Thumbnail navigation
- Keyboard navigation

**Proposed Additions:**

1. **360 View Tab:**
   - Placeholder for 360-degree product rotation
   - Fallback to image carousel if no 360 assets

2. **Lifestyle Images Section:**
   - Separate section for installation/context photos
   - Grid layout below main gallery

3. **Video Support:**
   - Product video thumbnails
   - Inline video player in lightbox

4. **AR Preview (Future):**
   - "View in Your Room" AR button (future phase)

```
+------------------------------------------+
|                                          |
|         [Main Product Image]             |
|                                          |
+------------------------------------------+
| [T1] [T2] [T3] [T4] [360] [Video]        |
+------------------------------------------+
|                                          |
| Lifestyle Gallery:                       |
| +--------+ +--------+ +--------+         |
| |Install | |Room    | |Detail  |         |
| |Photo   | |Context | |Shot    |         |
| +--------+ +--------+ +--------+         |
+------------------------------------------+
```

### 2.5 Filter System Redesign

**Current State:**
```
src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts
(Filter sidebar section - lines 59-188)
```

**Issues:**
- Text-only filters
- No HVAC-specific filtering (BTU, room size)
- No visual price range slider
- Filter chips not prominent

**Proposed Filter System:**

```
+------------------------------------------+
| Filters                          [Clear] |
+------------------------------------------+
|                                          |
| Active Filters:                          |
| [A++ Energy] [x] [18,000 BTU] [x]        |
|                                          |
+------------------------------------------+
| Room Size Calculator        [?]          |
| +--------------------------------------+ |
| |  [Icon] Select Your Room             | |
| |  +--------------------------------+  | |
| |  | [Bedroom] [Living] [Office]   |  | |
| |  +--------------------------------+  | |
| |  Room Size: [====O====] 45 m2        | |
| |                                      | |
| |  Recommended: 15,000 - 18,000 BTU    | |
| |  [Apply Recommendation]              | |
| +--------------------------------------+ |
+------------------------------------------+
| Price Range                              |
| EUR 0 [========O=====] EUR 5,000         |
| [    500    ] - [   2,500   ]            |
+------------------------------------------+
| Energy Rating                            |
| +--------------------------------------+ |
| | [A+++] [A++] [A+] [A] [B] [C+]       | |
| +--------------------------------------+ |
+------------------------------------------+
| Cooling Capacity (BTU)                   |
| [ ] 9,000 BTU (12-20 m2)          (24)  |
| [x] 12,000 BTU (20-35 m2)         (56)  |
| [x] 18,000 BTU (35-50 m2)         (38)  |
| [ ] 24,000 BTU (50-70 m2)         (15)  |
+------------------------------------------+
| Brand                                    |
| [ ] Daikin                        (45)  |
| [ ] Mitsubishi                    (38)  |
| [ ] LG                            (32)  |
| [Show more...]                           |
+------------------------------------------+
| Noise Level                              |
| [ ] Ultra Quiet (<40 dB)          (28)  |
| [ ] Quiet (40-50 dB)              (65)  |
| [ ] Standard (>50 dB)             (22)  |
+------------------------------------------+
| Features                                 |
| [ ] WiFi Control                  (78)  |
| [ ] Inverter Technology           (95)  |
| [ ] Heat Pump                     (45)  |
+------------------------------------------+
| Availability                             |
| [ ] In Stock                      (89)  |
| [ ] On Sale                       (23)  |
+------------------------------------------+
```

**New Features:**
1. **Room Size Calculator:** Visual room type selector + size slider
2. **BTU Recommendation:** Based on room selection
3. **Price Range Slider:** Dual-handle visual slider
4. **Energy Rating Chips:** Visual EU energy label style
5. **Active Filter Chips:** Prominent chips with easy removal
6. **Noise Level Filter:** HVAC-specific with descriptions
7. **Feature Filters:** WiFi, inverter, heat pump toggles

### 2.6 Category Pages Implementation

**Current State:**
```
src/ClimaSite.Web/src/app/features/categories/category-list/category-list.component.ts
```

**Status:** Placeholder only - shows "Category list coming soon..."

**Proposed Implementation:**

```
+--------------------------------------------------+
| Browse Categories                                 |
+--------------------------------------------------+

+--------------------------------------------------+
|                                                  |
|  [Hero: Air Conditioning Systems]                |
|  Browse our range of cooling solutions           |
|                                                  |
+--------------------------------------------------+

+--------+--------+--------+--------+
|        |        |        |        |
| [Icon] | [Icon] | [Icon] | [Icon] |
| Split  | Multi  | Portab | Casset |
| System | Split  | le AC  | te     |
|  (156) |  (45)  |  (28)  |  (12)  |
+--------+--------+--------+--------+
|        |        |        |        |
| [Icon] | [Icon] | [Icon] | [Icon] |
| Window | Ducted | VRF    | Access |
| Units  | System | System | ories  |
|  (8)   |  (23)  |  (15)  |  (89)  |
+--------+--------+--------+--------+

+--------------------------------------------------+
| Featured in Air Conditioning                      |
| [Product Carousel with top sellers]               |
+--------------------------------------------------+

+--------------------------------------------------+
| Heating Systems                                   |
+--------------------------------------------------+
| ... similar category cards ...                    |
+--------------------------------------------------+
```

**Features:**
1. **Category Hero:** Lifestyle image with category name
2. **Subcategory Grid:** Icon + name + product count
3. **Featured Products:** Top sellers per category
4. **Quick Stats:** Product count per category
5. **Visual Icons:** Category-specific icons
6. **SEO Content:** Category descriptions for SEO

### 2.7 Similar/Related Products Fix & Enhancement

**Current State:**
```
src/ClimaSite.Web/src/app/shared/components/similar-products/similar-products.component.ts
```

**Bug Identified (Lines 52-54):**
```typescript
@if (product.isOnSale && product.salePrice) {
  <span class="original-price">{{ product.salePrice | currency:'EUR' }}</span>
  <span class="sale-price">{{ product.basePrice | currency:'EUR' }}</span>
```
**BUG:** Labels are SWAPPED! `original-price` shows `salePrice`, `sale-price` shows `basePrice`

**Correct Code Should Be:**
```typescript
@if (product.isOnSale && product.salePrice) {
  <span class="original-price">{{ product.basePrice | currency:'EUR' }}</span>
  <span class="sale-price">{{ product.salePrice | currency:'EUR' }}</span>
```

**Proposed Enhancements:**

1. **Fix Price Label Bug:** Swap the price labels correctly
2. **Add Energy Badge:** Show energy rating on carousel cards
3. **Add Quick Specs:** BTU and noise level
4. **Comparison Checkbox:** Add to compare from carousel
5. **Improved Navigation:** Better touch gestures on mobile
6. **"Why Similar" Tag:** "Same Brand", "Same Capacity", etc.

---

## 3. Task List

### Phase 1: Foundation & Bug Fixes (Sprint 1-2)

#### Critical Bug Fixes
- [x] **TASK-21B-001:** Fix similar products price label bug (swap basePrice/salePrice) ✅ *Completed 2026-01-24*
- [ ] **TASK-21B-002:** Audit all product price displays for consistency

#### Product Card Foundation
- [ ] **TASK-21B-003:** Increase product card image height from 220px to 280px
- [x] **TASK-21B-004:** Create EnergyBadge component for product cards ✅ *Completed 2026-01-24 - Added inline to ProductCard*
- [x] **TASK-21B-005:** Add energy badge overlay to product card image ✅ *Completed 2026-01-24*
- [x] **TASK-21B-006:** Create QuickSpecsRow component (BTU, noise, room size) ✅ *Completed 2026-01-24 - Added inline to ProductCard*
- [x] **TASK-21B-007:** Add quick specs row to product card below title ✅ *Completed 2026-01-24*
- [ ] **TASK-21B-008:** Make wishlist button always visible (not hover-only)
- [ ] **TASK-21B-009:** Add stock indicator badge (In Stock/Low Stock/Out of Stock)

### Phase 2: Comparison Feature (Sprint 2-3)

#### Comparison Infrastructure
- [ ] **TASK-21B-010:** Create CompareService for managing comparison list
- [ ] **TASK-21B-011:** Create CompareStore with Angular Signals (max 4 products)
- [ ] **TASK-21B-012:** Add comparison checkbox to product card component
- [ ] **TASK-21B-013:** Create floating CompareBar component (shows selected products)
- [ ] **TASK-21B-014:** Create full ComparisonPage with side-by-side view
- [ ] **TASK-21B-015:** Add specs comparison table to ComparisonPage
- [ ] **TASK-21B-016:** Add comparison checkbox to similar products carousel
- [ ] **TASK-21B-017:** Add comparison checkbox to product detail page
- [ ] **TASK-21B-018:** Add i18n translations for comparison feature (EN, BG, DE)

### Phase 3: Product Grid Layouts (Sprint 3-4)

#### Layout Components
- [ ] **TASK-21B-019:** Create ProductGridLayoutService for layout management
- [ ] **TASK-21B-020:** Implement FeaturedSpotlightLayout component
- [ ] **TASK-21B-021:** Implement BentoMixedLayout component
- [ ] **TASK-21B-022:** Create FeaturedProductCard (large format) component
- [ ] **TASK-21B-023:** Add layout toggle to product list toolbar
- [ ] **TASK-21B-024:** Implement responsive breakpoints for all layouts
- [ ] **TASK-21B-025:** Add layout preference persistence (localStorage)

### Phase 4: Filter System Redesign (Sprint 4-5)

#### Visual Filters
- [ ] **TASK-21B-026:** Create PriceRangeSlider component (dual-handle)
- [ ] **TASK-21B-027:** Create EnergyRatingFilter component (chip-based)
- [ ] **TASK-21B-028:** Create ActiveFilterChips component
- [ ] **TASK-21B-029:** Create BTUFilter component with room size descriptions
- [ ] **TASK-21B-030:** Create NoiseLevelFilter component
- [ ] **TASK-21B-031:** Create FeatureFilters component (WiFi, inverter, etc.)

#### Room Size Calculator
- [ ] **TASK-21B-032:** Create RoomSizeCalculator component
- [ ] **TASK-21B-033:** Create RoomTypeSelector (bedroom, living, office, etc.)
- [ ] **TASK-21B-034:** Implement BTU recommendation algorithm
- [ ] **TASK-21B-035:** Add "Apply Recommendation" filter integration
- [ ] **TASK-21B-036:** Create room size visualization (optional SVG)

#### Filter Integration
- [ ] **TASK-21B-037:** Update ProductFilter interface for new filter types
- [ ] **TASK-21B-038:** Update product list to use new filter components
- [ ] **TASK-21B-039:** Update API/backend for new filter parameters
- [ ] **TASK-21B-040:** Add filter analytics tracking

### Phase 5: Product Detail Page Enhancement (Sprint 5-6)

#### Sticky Cart
- [ ] **TASK-21B-041:** Create StickyCartBar component
- [ ] **TASK-21B-042:** Implement scroll-triggered visibility
- [ ] **TASK-21B-043:** Add mobile-optimized sticky cart (bottom bar)
- [ ] **TASK-21B-044:** Sync sticky cart with main add-to-cart state

#### Gallery Enhancements
- [ ] **TASK-21B-045:** Add 360 view placeholder/tab to gallery
- [ ] **TASK-21B-046:** Create LifestyleGallery section component
- [ ] **TASK-21B-047:** Add video thumbnail support to gallery
- [ ] **TASK-21B-048:** Create VideoPlayer component for lightbox

#### Specs & Info Redesign
- [ ] **TASK-21B-049:** Create KeySpecsGrid component (visual HVAC specs)
- [ ] **TASK-21B-050:** Create VariantSelector component (size/capacity pills)
- [ ] **TASK-21B-051:** Enhance TrustBadges component with installation badge
- [ ] **TASK-21B-052:** Create tabbed accordion for mobile specs view
- [ ] **TASK-21B-053:** Add share functionality (share product link)

### Phase 6: Category Pages (Sprint 6-7)

#### Category List Page
- [ ] **TASK-21B-054:** Implement full CategoryListComponent
- [ ] **TASK-21B-055:** Create CategoryCard component with icon and count
- [ ] **TASK-21B-056:** Create CategoryHero component
- [ ] **TASK-21B-057:** Add featured products section per category
- [ ] **TASK-21B-058:** Create category icon set (SVG sprites)
- [ ] **TASK-21B-059:** Add SEO-friendly category descriptions
- [ ] **TASK-21B-060:** Implement subcategory navigation

### Phase 7: HVAC-Specific Features (Sprint 7-8)

#### BTU Calculator
- [ ] **TASK-21B-061:** Create standalone BTUCalculatorPage
- [ ] **TASK-21B-062:** Implement room dimension inputs
- [ ] **TASK-21B-063:** Add insulation quality factor
- [ ] **TASK-21B-064:** Add sun exposure factor
- [ ] **TASK-21B-065:** Add ceiling height factor
- [ ] **TASK-21B-066:** Show product recommendations based on result
- [ ] **TASK-21B-067:** Add "Find Products" CTA with pre-filtered results

#### Installation Requirements Display
- [ ] **TASK-21B-068:** Create InstallationRequirements component
- [ ] **TASK-21B-069:** Display electrical requirements (voltage, amperage)
- [ ] **TASK-21B-070:** Display mounting requirements
- [ ] **TASK-21B-071:** Display drainage requirements
- [ ] **TASK-21B-072:** Add professional installation CTA

#### Energy Efficiency Section
- [ ] **TASK-21B-073:** Enhance EnergyRating component with annual cost estimate
- [ ] **TASK-21B-074:** Add SEER/SCOP ratings display
- [ ] **TASK-21B-075:** Add energy savings calculator
- [ ] **TASK-21B-076:** Add comparison with lower-rated alternatives

### Phase 8: Performance & Polish (Sprint 8)

#### Performance
- [ ] **TASK-21B-077:** Implement virtual scrolling for large product lists
- [ ] **TASK-21B-078:** Optimize product card rendering (OnPush strategy)
- [ ] **TASK-21B-079:** Add skeleton loaders for new components
- [ ] **TASK-21B-080:** Implement image lazy loading for galleries

#### Accessibility
- [ ] **TASK-21B-081:** Add ARIA labels for all new interactive elements
- [ ] **TASK-21B-082:** Ensure keyboard navigation for comparison feature
- [ ] **TASK-21B-083:** Add focus management for sticky cart
- [ ] **TASK-21B-084:** Screen reader announcements for filter changes

#### Animations
- [ ] **TASK-21B-085:** Add micro-interactions for comparison toggle
- [ ] **TASK-21B-086:** Add filter chip entry/exit animations
- [ ] **TASK-21B-087:** Add smooth transitions for layout changes
- [ ] **TASK-21B-088:** Ensure reduced motion support for all animations

### Phase 9: Testing & Documentation (Sprint 9-10)

#### Unit Tests
- [ ] **TASK-21B-089:** Unit tests for CompareService
- [ ] **TASK-21B-090:** Unit tests for BTU calculation algorithm
- [ ] **TASK-21B-091:** Unit tests for new filter components
- [ ] **TASK-21B-092:** Unit tests for product card enhancements

#### Integration Tests
- [ ] **TASK-21B-093:** Integration tests for comparison flow
- [ ] **TASK-21B-094:** Integration tests for filter combinations
- [ ] **TASK-21B-095:** Integration tests for sticky cart

#### E2E Tests
- [ ] **TASK-21B-096:** E2E test: Complete comparison flow
- [ ] **TASK-21B-097:** E2E test: Room calculator to product selection
- [ ] **TASK-21B-098:** E2E test: Category navigation flow
- [ ] **TASK-21B-099:** E2E test: Mobile product detail page
- [ ] **TASK-21B-100:** E2E test: Filter and sort combinations

---

## 4. Technical Specifications

### 4.1 New Components

| Component | Path | Description |
|-----------|------|-------------|
| `EnergyBadgeComponent` | `shared/components/energy-badge/` | Small badge for product cards |
| `QuickSpecsRowComponent` | `shared/components/quick-specs-row/` | BTU, noise, room size row |
| `CompareCheckboxComponent` | `shared/components/compare-checkbox/` | Toggle for comparison |
| `CompareBarComponent` | `shared/components/compare-bar/` | Floating comparison bar |
| `ComparisonPageComponent` | `features/compare/` | Full comparison page |
| `PriceRangeSliderComponent` | `shared/components/price-range-slider/` | Dual-handle slider |
| `RoomSizeCalculatorComponent` | `shared/components/room-calculator/` | Room type + size selector |
| `BTUCalculatorComponent` | `features/tools/btu-calculator/` | Full BTU calculator page |
| `StickyCartBarComponent` | `shared/components/sticky-cart-bar/` | Scroll-triggered cart |
| `KeySpecsGridComponent` | `shared/components/key-specs-grid/` | Visual HVAC specs grid |
| `VariantSelectorComponent` | `shared/components/variant-selector/` | Capacity/size pills |
| `LifestyleGalleryComponent` | `shared/components/lifestyle-gallery/` | Installation photos |
| `CategoryCardComponent` | `shared/components/category-card/` | Category with icon |
| `FeaturedProductCardComponent` | `shared/components/featured-product-card/` | Large format card |
| `ActiveFilterChipsComponent` | `shared/components/active-filter-chips/` | Removable filter chips |

### 4.2 New Services

| Service | Path | Description |
|---------|------|-------------|
| `CompareService` | `core/services/compare.service.ts` | Manage comparison list |
| `BTUCalculatorService` | `core/services/btu-calculator.service.ts` | BTU calculations |
| `ProductLayoutService` | `core/services/product-layout.service.ts` | Grid layout management |

### 4.3 Model Updates

#### ProductBrief Interface Extensions ✅ *Implemented 2026-01-24*

```typescript
// src/ClimaSite.Web/src/app/core/models/product.model.ts

export interface ProductBrief {
  // Existing fields...
  id: string;
  name: string;
  slug: string;
  shortDescription?: string;
  basePrice: number;
  salePrice?: number;
  isOnSale: boolean;
  discountPercentage: number;
  brand?: string;
  averageRating: number;
  reviewCount: number;
  primaryImageUrl?: string;
  inStock: boolean;
  
  // NEW: HVAC-specific quick specs (IMPLEMENTED)
  energyRating?: EnergyRatingLevel;
  btuCapacity?: number;
  noiseLevel?: number;        // in dB
  roomSizeMin?: number;       // in m2
  roomSizeMax?: number;       // in m2
  hasWifi?: boolean;
  hasInverter?: boolean;
  isHeatPump?: boolean;
}

// Also added:
export type EnergyRatingLevel = 'A+++' | 'A++' | 'A+' | 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G';
export type ProductFeature = 'wifi' | 'inverter' | 'heat_pump' | 'smart_home';

export interface ProductFilter {
  // Existing fields...
  
  // NEW: HVAC-specific filters
  energyRatings?: EnergyRatingLevel[];
  btuMin?: number;
  btuMax?: number;
  noiseLevelMax?: number;
  roomSize?: number;
  features?: ProductFeature[];
}

export type ProductFeature = 'wifi' | 'inverter' | 'heat_pump' | 'smart_home';
```

#### New Comparison Model

```typescript
// src/ClimaSite.Web/src/app/core/models/compare.model.ts

export interface CompareItem {
  productId: string;
  product: ProductBrief;
  addedAt: Date;
}

export interface CompareState {
  items: CompareItem[];
  maxItems: number;  // Default 4
}

export interface CompareSpecs {
  category: string;
  specs: {
    key: string;
    label: string;
    values: (string | number | boolean | null)[];
  }[];
}
```

### 4.4 API Changes

#### New Endpoints Required

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products/compare?ids=id1,id2,id3` | Get products for comparison |
| GET | `/api/products/recommendations?btu=18000` | Get products by BTU |
| GET | `/api/categories/all` | Get all categories with counts |
| GET | `/api/categories/{id}/featured` | Get featured products for category |

#### Filter Parameter Updates

```
GET /api/products?
  energyRatings=A+++,A++,A+&
  btuMin=12000&
  btuMax=18000&
  noiseLevelMax=45&
  roomSize=40&
  features=wifi,inverter
```

### 4.5 State Management

#### Compare Store (Angular Signals)

```typescript
// src/ClimaSite.Web/src/app/core/services/compare.service.ts

@Injectable({ providedIn: 'root' })
export class CompareService {
  private readonly MAX_ITEMS = 4;
  
  // State
  private _items = signal<CompareItem[]>([]);
  
  // Public accessors
  items = this._items.asReadonly();
  count = computed(() => this._items().length);
  isFull = computed(() => this._items().length >= this.MAX_ITEMS);
  
  // Check if product is in compare list
  isComparing(productId: string) {
    return computed(() => 
      this._items().some(item => item.productId === productId)
    );
  }
  
  // Add to compare
  add(product: ProductBrief): boolean {
    if (this.isFull()) return false;
    if (this.isComparing(product.id)()) return false;
    
    this._items.update(items => [
      ...items,
      { productId: product.id, product, addedAt: new Date() }
    ]);
    return true;
  }
  
  // Remove from compare
  remove(productId: string): void {
    this._items.update(items => 
      items.filter(item => item.productId !== productId)
    );
  }
  
  // Clear all
  clear(): void {
    this._items.set([]);
  }
}
```

---

## 5. HVAC-Specific Features

### 5.1 Energy Rating Display Component

**Purpose:** Prominently display EU energy efficiency ratings across the platform.

**Locations:**
- Product card (badge overlay)
- Product detail page (prominent display)
- Comparison table
- Filter sidebar
- Search results

**Design:**
```
+-------+
| A++   |  <- Color-coded (green to red gradient)
+-------+
```

**Implementation:**
```typescript
// Enhanced energy rating with additional context

export interface EnergyRatingConfig {
  rating: EnergyRatingLevel;
  showLabel?: boolean;
  showSavings?: boolean;   // "Save up to X% on electricity"
  size?: 'sm' | 'md' | 'lg';
  variant?: 'badge' | 'full';
}
```

### 5.2 BTU Calculator UI

**Purpose:** Help customers determine the correct AC capacity for their space.

**Factors Considered:**
1. Room dimensions (length x width x height)
2. Room type (bedroom, living room, kitchen, office)
3. Sun exposure (north-facing, south-facing, skylights)
4. Insulation quality (old building, modern, well-insulated)
5. Number of occupants
6. Heat-generating equipment (computers, kitchen appliances)
7. Climate zone

**Formula:**
```
Base BTU = Room Volume (m3) x 35
Adjustments:
  - Poor insulation: +15%
  - South-facing windows: +10%
  - Kitchen: +4000 BTU
  - Per occupant beyond 2: +600 BTU
  - Per major appliance: +1000 BTU
```

**UI Flow:**
```
Step 1: Room Type
+----------------------------------+
| What type of room?               |
|                                  |
| [Bedroom] [Living Room] [Office] |
| [Kitchen] [Basement] [Other]     |
+----------------------------------+

Step 2: Room Size
+----------------------------------+
| Room Dimensions                  |
|                                  |
| Length: [____] m                 |
| Width:  [____] m                 |
| Height: [____] m (default 2.5)   |
|                                  |
| Or use slider:                   |
| [=======O========] 45 m2         |
+----------------------------------+

Step 3: Conditions
+----------------------------------+
| Sun Exposure                     |
| [Low] [Medium] [High]            |
|                                  |
| Insulation                       |
| [Poor] [Average] [Good]          |
|                                  |
| Occupants: [2] [+][-]            |
+----------------------------------+

Result:
+----------------------------------+
| Recommended Capacity             |
|                                  |
|     18,000 BTU                   |
|     (5.3 kW)                     |
|                                  |
| This will comfortably cool       |
| your 45m2 living room            |
|                                  |
| [View 18,000 BTU Units]          |
+----------------------------------+
```

### 5.3 Room Size Visualizer

**Purpose:** Visual representation of room types with sizing guidance.

**Design:**
```
+------------------------------------------+
|  Select Room Type                         |
+------------------------------------------+
|                                          |
|  +--------+  +--------+  +--------+      |
|  |  [===] |  |  [^^^] |  |  [###] |      |
|  | Bedroom|  | Living |  | Office |      |
|  | 15-25m2|  | 25-50m2|  | 10-20m2|      |
|  +--------+  +--------+  +--------+      |
|                                          |
|  Selected: Living Room                   |
|                                          |
|  +--------------------------------------+|
|  |                                      ||
|  |    [Visual room diagram with        ||
|  |     furniture silhouettes]          ||
|  |                                      ||
|  +--------------------------------------+|
|                                          |
|  Adjust Size: [===========O===] 35 m2    |
|                                          |
|  Recommended: 12,000 - 15,000 BTU        |
+------------------------------------------+
```

### 5.4 Installation Requirements Display

**Purpose:** Clearly communicate what's needed to install the product.

**Information Displayed:**
- Electrical requirements (voltage, amperage, outlet type)
- Mounting requirements (wall strength, bracket included)
- Drainage requirements (condensate line)
- Minimum clearances
- Professional installation recommended/required

**Component Design:**
```
+------------------------------------------+
| Installation Requirements                 |
+------------------------------------------+
|                                          |
| Electrical                               |
| +--------------------------------------+ |
| | [Icon] 220-240V / 16A                | |
| | Dedicated circuit recommended        | |
| +--------------------------------------+ |
|                                          |
| Mounting                                 |
| +--------------------------------------+ |
| | [Icon] Wall-mounted indoor unit      | |
| | Min. wall load capacity: 30kg        | |
| | Mounting bracket included            | |
| +--------------------------------------+ |
|                                          |
| Drainage                                 |
| +--------------------------------------+ |
| | [Icon] Condensate drain required     | |
| | 16mm drain line (not included)       | |
| +--------------------------------------+ |
|                                          |
| Clearances                               |
| +--------------------------------------+ |
| | [Diagram showing required spacing]   | |
| | Top: 15cm | Sides: 10cm | Front: 2m  | |
| +--------------------------------------+ |
|                                          |
| [Schedule Professional Installation]     |
+------------------------------------------+
```

---

## 6. Dependencies

### 6.1 Plan Dependencies

| Plan | Dependency Type | Reason |
|------|-----------------|--------|
| **Plan 21A: Global UI Redesign** | Hard | Design tokens, color system, typography |
| **Plan 12: Notifications** | Soft | Toast notifications for comparison actions |
| **Plan 17: Future Enhancements** | Soft | Related products API |

### 6.2 External Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| Angular CDK | ^19.x | Drag-drop for comparison, overlay for sticky cart |
| ngx-slider | Latest | Price range slider component |
| Chart.js (optional) | ^4.x | BTU calculator visualizations |

### 6.3 Backend Dependencies

| Requirement | Status | Notes |
|-------------|--------|-------|
| Product specs in API response | Partial | Need BTU, noise level fields |
| Filter API updates | Required | New filter parameters needed |
| Comparison endpoint | Required | New endpoint for bulk product fetch |
| Category counts | Required | Product counts per category |

---

## 7. Testing Checklist

### 7.1 Unit Testing

- [ ] CompareService: add, remove, clear, isComparing, isFull
- [ ] BTUCalculatorService: calculation with all factor combinations
- [ ] ProductLayoutService: layout switching, persistence
- [ ] EnergyBadgeComponent: all rating levels render correctly
- [ ] PriceRangeSlider: min/max constraints, value changes
- [ ] RoomSizeCalculator: room type selection, size slider

### 7.2 Integration Testing

- [ ] Compare flow: add from card -> compare bar -> comparison page
- [ ] Filter flow: select filters -> API call -> results update
- [ ] Calculator flow: input values -> recommendation -> product list
- [ ] Sticky cart: scroll trigger -> add to cart -> cart update

### 7.3 E2E Testing

- [ ] Complete comparison journey (add 4 products, compare, remove)
- [ ] BTU calculator to purchase (calculate -> filter -> add to cart)
- [ ] Category browsing (categories -> subcategory -> product)
- [ ] Mobile product detail (swipe gallery, sticky cart, tabs)
- [ ] Filter combinations (multiple filters, clear, URL state)

### 7.4 Cross-Browser Testing

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

### 7.5 Accessibility Testing

- [ ] Screen reader navigation for comparison feature
- [ ] Keyboard-only navigation for all new components
- [ ] Focus management for modal dialogs (lightbox, compare bar)
- [ ] Color contrast for energy badges (all rating levels)
- [ ] ARIA labels for interactive elements

### 7.6 Performance Testing

- [ ] Product list with 100+ products: smooth scrolling
- [ ] Comparison page with 4 products: no layout shift
- [ ] Filter changes: <200ms response
- [ ] Image gallery: <100ms thumbnail switch
- [ ] Sticky cart: no jank on scroll

### 7.7 Theme Testing

- [ ] All components work in light theme
- [ ] All components work in dark theme
- [ ] Energy badges have sufficient contrast in both themes
- [ ] Filter chips are readable in both themes

### 7.8 i18n Testing

- [ ] All new strings have translations (EN, BG, DE)
- [ ] BTU calculator labels translate correctly
- [ ] Room types translate correctly
- [ ] Filter labels translate correctly
- [ ] Comparison page translates correctly

---

## Appendix A: File Locations Reference

### Existing Files to Modify

| File | Purpose |
|------|---------|
| `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts` | Add energy badge, quick specs, comparison |
| `src/ClimaSite.Web/src/app/features/products/product-list/product-list.component.ts` | Add layouts, enhanced filters |
| `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` | Sticky cart, enhanced gallery |
| `src/ClimaSite.Web/src/app/features/categories/category-list/category-list.component.ts` | Full implementation |
| `src/ClimaSite.Web/src/app/shared/components/similar-products/similar-products.component.ts` | Fix price bug, add features |
| `src/ClimaSite.Web/src/app/shared/components/energy-rating/energy-rating.component.ts` | Enhance for cards |
| `src/ClimaSite.Web/src/app/shared/components/product-gallery/product-gallery.component.ts` | 360 view, lifestyle |
| `src/ClimaSite.Web/src/app/core/models/product.model.ts` | Add HVAC fields |

### New Files to Create

| Directory | Files |
|-----------|-------|
| `shared/components/energy-badge/` | `energy-badge.component.ts` |
| `shared/components/quick-specs-row/` | `quick-specs-row.component.ts` |
| `shared/components/compare-checkbox/` | `compare-checkbox.component.ts` |
| `shared/components/compare-bar/` | `compare-bar.component.ts` |
| `shared/components/price-range-slider/` | `price-range-slider.component.ts` |
| `shared/components/room-calculator/` | `room-calculator.component.ts` |
| `shared/components/sticky-cart-bar/` | `sticky-cart-bar.component.ts` |
| `shared/components/key-specs-grid/` | `key-specs-grid.component.ts` |
| `shared/components/variant-selector/` | `variant-selector.component.ts` |
| `shared/components/lifestyle-gallery/` | `lifestyle-gallery.component.ts` |
| `shared/components/category-card/` | `category-card.component.ts` |
| `shared/components/featured-product-card/` | `featured-product-card.component.ts` |
| `shared/components/active-filter-chips/` | `active-filter-chips.component.ts` |
| `features/compare/` | `compare.component.ts`, `compare.routes.ts` |
| `features/tools/btu-calculator/` | `btu-calculator.component.ts` |
| `core/services/` | `compare.service.ts`, `btu-calculator.service.ts`, `product-layout.service.ts` |
| `core/models/` | `compare.model.ts` |

---

## Appendix B: Translation Keys Required

```json
{
  "compare": {
    "title": "Compare Products",
    "add": "Add to Compare",
    "remove": "Remove from Compare",
    "clear": "Clear All",
    "viewComparison": "View Comparison",
    "maxReached": "Maximum 4 products can be compared",
    "empty": "No products to compare",
    "addMore": "Add more products to compare"
  },
  "filters": {
    "roomCalculator": {
      "title": "Room Size Calculator",
      "roomType": "Room Type",
      "bedroom": "Bedroom",
      "livingRoom": "Living Room",
      "office": "Office",
      "kitchen": "Kitchen",
      "roomSize": "Room Size",
      "recommended": "Recommended",
      "applyRecommendation": "Apply Recommendation"
    },
    "energyRating": "Energy Rating",
    "btuCapacity": "Cooling Capacity (BTU)",
    "noiseLevel": {
      "title": "Noise Level",
      "ultraQuiet": "Ultra Quiet (<40 dB)",
      "quiet": "Quiet (40-50 dB)",
      "standard": "Standard (>50 dB)"
    },
    "features": {
      "title": "Features",
      "wifi": "WiFi Control",
      "inverter": "Inverter Technology",
      "heatPump": "Heat Pump"
    },
    "priceRange": "Price Range"
  },
  "btuCalculator": {
    "title": "BTU Calculator",
    "subtitle": "Find the right capacity for your room",
    "roomDimensions": "Room Dimensions",
    "length": "Length",
    "width": "Width",
    "height": "Height",
    "sunExposure": "Sun Exposure",
    "insulation": "Insulation Quality",
    "occupants": "Number of Occupants",
    "result": "Recommended Capacity",
    "findProducts": "Find Products"
  },
  "installation": {
    "requirements": "Installation Requirements",
    "electrical": "Electrical",
    "mounting": "Mounting",
    "drainage": "Drainage",
    "clearances": "Clearances",
    "scheduleInstallation": "Schedule Professional Installation"
  }
}
```

---

*Document Version: 1.0*
*Created: January 2026*
*Last Updated: January 2026*
