# Plan 21A: Home Page Redesign

## Overview

**Priority:** Critical | **Complexity:** High | **Estimated Effort:** 3-5 days

### Goals

Transform the ClimaSite home page from a generic, template-like experience into a premium "Nordic Tech" design that:

1. Shows products above the fold (currently requires 2+ scrolls)
2. Replaces gradient blobs with purposeful, product-focused imagery
3. Implements Bento grid layouts for modern visual appeal
4. Reduces animation noise while keeping meaningful micro-interactions
5. Builds trust with real testimonials, certifications, and brand logos
6. Drives conversions with strategic CTAs and value propositions

### Success Metrics

| Metric | Current State | Target | Measurement |
|--------|--------------|--------|-------------|
| Products Above Fold | 0 | 3-4 products visible | Visual audit |
| Lighthouse Performance | ~75 | 90+ | Lighthouse audit |
| Animation Count | 15+ simultaneous | 5-7 purposeful | Code audit |
| Time to First Product | 2+ scroll actions | 0 (above fold) | User testing |
| Trust Signals Visible | 0 above fold | 4+ (badges, warranty) | Visual audit |
| Newsletter Conversion | Baseline | +25% | Analytics |

### Design Direction: "Nordic Tech"

- **Style:** Clean Minimalism + Bento Grid layouts
- **Primary Color:** Arctic Blue (`#0ea5e9` / `--color-primary-500`)
- **CTA Color:** Ember Orange (`#f97316` / `--color-ember-500`)
- **Typography:** Inter (body) + Space Grotesk (headlines) - unchanged
- **Animation Philosophy:** Purposeful only - reveal on scroll, subtle hovers

---

## Section-by-Section Redesign

### 1. Hero Section

**Current State:**
- Full-screen gradient blobs (3 animated circles)
- Parallax effects on multiple layers
- Generic "Comfort Engineered" messaging
- No product imagery
- No search functionality
- Floating animations on every text element

**Target State:**

```
+------------------------------------------------------------------+
|  [Logo]     [Search Bar - Prominent]     [Cart] [Account] [Lang] |
+------------------------------------------------------------------+
|                                                                  |
|   SEASONAL PROMO BANNER (Optional - toggleable)                  |
|   "Winter Sale: Up to 30% off heating systems"  [Shop Now ->]    |
|                                                                  |
+------------------------------------------------------------------+
|                                                                  |
|   +------------------------+    +-----------------------------+  |
|   |                        |    |                             |  |
|   |   Hero Product         |    |   Perfect Climate.          |  |
|   |   Image                |    |   Precision Engineered.     |  |
|   |   (Featured AC Unit)   |    |                             |  |
|   |                        |    |   Energy-efficient HVAC     |  |
|   |                        |    |   for every space.          |  |
|   |                        |    |                             |  |
|   |                        |    |   [Shop All Products]       |  |
|   |                        |    |   [Find Your System ->]     |  |
|   +------------------------+    +-----------------------------+  |
|                                                                  |
|   +--------+ +--------+ +--------+ +--------+                    |
|   | Free   | | 5-Year | | Expert | | Pro    |  <- Value Props   |
|   |Shipping| |Warranty| |Support | |Install |     (above fold)  |
|   +--------+ +--------+ +--------+ +--------+                    |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Background:** Clean white/light gray (`--color-bg-primary`) - NO gradient blobs
- **Hero Product Image:** Large, high-quality product photo (60% width on desktop)
- **Search:** Prominent search bar in header, always visible
- **Seasonal Banner:** Optional top banner for promotions (can be disabled via CMS)
- **Value Props:** Moved ABOVE the fold as icon strip
- **Animation:** Single fade-in on load, NO floating/parallax on hero content

**Tasks:**

- [x] TASK-21A-001: Remove gradient blob backgrounds from hero section
- [x] TASK-21A-002: Create HeroProductComponent with product image slot
- [x] TASK-21A-003: Implement seasonal promo banner with dismiss functionality
- [x] TASK-21A-004: Move value propositions strip above the fold (below hero)
- [x] TASK-21A-005: Replace floating animations with single fade-in entrance
- [x] TASK-21A-006: Add prominent search bar to hero (or ensure header search is visible)
- [x] TASK-21A-007: Create dual CTA layout (primary + secondary ghost button)
- [x] TASK-21A-008: Implement hero content API for dynamic seasonal content

---

### 2. Category Navigation (Bento Grid)

**Current State:**
- 4 equal-width panels in a row
- Stock Unsplash images
- Tilt effect on all cards
- Lazy-loaded backgrounds

**Target State:**

```
+------------------------------------------------------------------+
|                                                                  |
|   +---------------------------+  +------------+  +------------+  |
|   |                           |  |            |  |            |  |
|   |   AIR CONDITIONING        |  |  HEATING   |  | VENTILATION|  |
|   |   (Large Featured Panel)  |  |  SYSTEMS   |  |            |  |
|   |                           |  |            |  |            |  |
|   |   "Stay cool with         |  +------------+  +------------+  |
|   |    premium split ACs"     |  |                             |  |
|   |                           |  |   WATER PURIFICATION        |  |
|   |   [Explore ->]            |  |   (Wide Panel)              |  |
|   |                           |  |                             |  |
|   +---------------------------+  +-----------------------------+  |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Layout:** Asymmetric Bento grid (1 large + 3 smaller panels)
- **Images:** Custom category images (product-focused, not lifestyle stock)
- **Hover Effect:** Subtle scale (1.02) + arrow animation - NO tilt effect
- **Overlay:** Minimal gradient overlay (only bottom 30%)
- **Typography:** Bold category names + short tagline

**Tasks:**

- [x] TASK-21A-009: Implement Bento grid layout for categories (CSS Grid)
- [x] TASK-21A-010: Remove TiltEffectDirective from category panels (already removed in 21F)
- [x] TASK-21A-011: Create asymmetric panel sizing (large featured + smaller)
- [x] TASK-21A-012: Update category images to product-focused photography (replaced with gradient backgrounds + icons)
- [x] TASK-21A-013: Simplify hover effects (scale + arrow only)
- [x] TASK-21A-014: Add category taglines to translation files
- [x] TASK-21A-015: Ensure proper aspect ratios on all breakpoints

---

### 3. Featured Products Section

**Current State:**
- Standard 4-column grid
- Shows 4 products
- Basic product cards
- "View All" link in header

**Target State:**

```
+------------------------------------------------------------------+
|   FEATURED PRODUCTS                              [View All ->]   |
+------------------------------------------------------------------+
|                                                                  |
|   +-------------------+  +----------+  +----------+  +----------+|
|   |                   |  |          |  |          |  |          ||
|   |   HERO PRODUCT    |  | Product  |  | Product  |  | Product  ||
|   |   (2x size)       |  |    2     |  |    3     |  |    4     ||
|   |                   |  |          |  |          |  |          ||
|   |   "Best Seller"   |  +----------+  +----------+  +----------+|
|   |   badge           |  |          |  |          |  |          ||
|   |                   |  | Product  |  | Product  |  | Product  ||
|   |   Full specs      |  |    5     |  |    6     |  |    7     ||
|   |   preview         |  |          |  |          |  |          ||
|   +-------------------+  +----------+  +----------+  +----------+|
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Layout:** Hero product (spans 2 rows) + 6 supporting products
- **Hero Product:** Larger image, "Best Seller" or "Editor's Pick" badge, specs preview
- **Supporting Products:** Standard product cards with energy rating badges
- **Hover:** Subtle lift (4px), shadow increase - NO translateY(-4px) constant

**Tasks:**

- [ ] TASK-21A-016: Create FeaturedProductsGridComponent with hero product layout
- [ ] TASK-21A-017: Implement "Best Seller" / "Editor's Pick" badge component
- [ ] TASK-21A-018: Add energy rating display to product cards
- [ ] TASK-21A-019: Increase featured products from 4 to 7 (1 hero + 6 supporting)
- [ ] TASK-21A-020: Update ProductService to support hero product designation
- [ ] TASK-21A-021: Remove excessive hover animations from product items

---

### 4. Trust Indicators Section (NEW)

**Current State:**
- Brand names as text in scrolling ticker
- No brand logos
- No certifications visible
- No warranty badges

**Target State:**

```
+------------------------------------------------------------------+
|                                                                  |
|   TRUSTED BY INDUSTRY LEADERS                                    |
|                                                                  |
|   [Daikin Logo] [Mitsubishi] [LG] [Samsung] [Gree] [Toshiba]    |
|   <- Infinite scroll marquee with actual logos ->                |
|                                                                  |
+------------------------------------------------------------------+
|                                                                  |
|   +----------------+  +----------------+  +----------------+     |
|   | [Energy Star]  |  | [ISO 9001]     |  | [TUV Certified]|     |
|   | Certified      |  | Quality        |  | Safety         |     |
|   +----------------+  +----------------+  +----------------+     |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Brand Logos:** Real SVG logos, grayscale by default, color on hover
- **Certifications:** Energy Star, ISO, industry certifications as badges
- **Layout:** Marquee for brands, static grid for certifications
- **Animation:** Smooth marquee scroll, pause on hover

**Tasks:**

- [ ] TASK-21A-022: Create TrustIndicatorsComponent with brand logos section
- [ ] TASK-21A-023: Source/create SVG logos for partner brands (grayscale)
- [ ] TASK-21A-024: Implement certification badges component
- [ ] TASK-21A-025: Replace text-only brand ticker with logo marquee
- [ ] TASK-21A-026: Add hover color transition for brand logos
- [ ] TASK-21A-027: Create translation keys for certification descriptions

---

### 5. Social Proof / Testimonials Section

**Current State:**
- Single rotating quote
- Initials-only avatars
- No photos
- Auto-rotate every 6 seconds
- Basic dot navigation

**Target State:**

```
+------------------------------------------------------------------+
|                                     Dark background section      |
|   WHAT OUR CUSTOMERS SAY                                         |
|                                                                  |
|   +-----------------------------------------------+              |
|   |                                               |              |
|   |   "The installation was professional and      |              |
|   |    the AC works perfectly. Best purchase      |              |
|   |    I've made this year!"                      |              |
|   |                                               |              |
|   |   [Photo] Maria S.        ★★★★★              |              |
|   |           Sofia, Bulgaria  Verified Purchase  |              |
|   |                                               |              |
|   +-----------------------------------------------+              |
|                                                                  |
|   [Video Testimonial Thumbnail] "Watch Sarah's Story" (optional) |
|                                                                  |
|   ( o ) ( ) ( )  <- Navigation dots                              |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Background:** Dark section for contrast (`--color-bg-inverse`)
- **Photos:** Real customer photos (or realistic placeholders)
- **Verified Badge:** "Verified Purchase" badge for authenticated reviews
- **Star Rating:** 5-star display
- **Video Option:** Optional video testimonial thumbnail
- **Animation:** Crossfade between testimonials (not slide)

**Tasks:**

- [ ] TASK-21A-028: Add photo support to testimonial data structure
- [ ] TASK-21A-029: Create TestimonialCardComponent with photo, rating, verification
- [ ] TASK-21A-030: Implement "Verified Purchase" badge component
- [ ] TASK-21A-031: Replace initials avatar with photo avatar (fallback to initials)
- [ ] TASK-21A-032: Add video testimonial thumbnail option
- [ ] TASK-21A-033: Implement crossfade transition (not slide)
- [ ] TASK-21A-034: Update testimonial translation structure for photos/videos

---

### 6. Value Propositions (Relocated Above Fold)

**Current State:**
- Below categories section
- 4 icon + text items
- SVG icons inline
- Reveal animation on scroll

**Target State:**
- Moved to directly below hero section (ABOVE FOLD)
- Cleaner, more compact design
- Lucide icons (consistent icon library)

```
+------------------------------------------------------------------+
|                                                                  |
|   [Truck]          [Shield]         [Headset]       [Wrench]     |
|   Free Shipping    5-Year           24/7 Expert     Professional |
|   on orders $500+  Warranty         Support         Installation |
|                                                                  |
+------------------------------------------------------------------+
```

**Tasks:**

- [ ] TASK-21A-035: Relocate value props to position directly after hero
- [ ] TASK-21A-036: Compact the value props design (smaller icons, single line)
- [ ] TASK-21A-037: Replace inline SVG icons with Lucide icon component
- [ ] TASK-21A-038: Remove scroll reveal animation (already visible on load)
- [ ] TASK-21A-039: Add "on orders $500+" detail to shipping prop

---

### 7. Newsletter Section

**Current State:**
- Basic two-column layout
- Generic "Stay Updated" messaging
- Email input + arrow button
- No incentive offered

**Target State:**

```
+------------------------------------------------------------------+
|   Gradient background (subtle)                                   |
|                                                                  |
|   +-----------------------------------------------------------+  |
|   |                                                           |  |
|   |   [Gift Icon]                                             |  |
|   |                                                           |  |
|   |   GET 10% OFF YOUR FIRST ORDER                            |  |
|   |                                                           |  |
|   |   Plus exclusive deals, new product alerts, and           |  |
|   |   climate tips delivered to your inbox.                   |  |
|   |                                                           |  |
|   |   +----------------------------------+ +------------+     |  |
|   |   | your@email.com                   | | Subscribe  |     |  |
|   |   +----------------------------------+ +------------+     |  |
|   |                                                           |  |
|   |   No spam. Unsubscribe anytime.                           |  |
|   |                                                           |  |
|   +-----------------------------------------------------------+  |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Incentive:** 10% off first order (requires backend coupon system)
- **Layout:** Centered, card-based design
- **Background:** Subtle gradient or pattern
- **Privacy:** Clear "no spam" assurance
- **Success State:** Animated checkmark + coupon code display

**Tasks:**

- [ ] TASK-21A-040: Redesign newsletter with incentive-based messaging
- [ ] TASK-21A-041: Create NewsletterCardComponent with centered layout
- [ ] TASK-21A-042: Add incentive icon (gift/discount)
- [ ] TASK-21A-043: Implement success state with coupon code reveal
- [ ] TASK-21A-044: Add privacy disclaimer text
- [ ] TASK-21A-045: Update all newsletter translation keys (EN, BG, DE)
- [ ] TASK-21A-046: (Backend) Create newsletter signup coupon generation

---

### 8. Final CTA Section

**Current State:**
- Full-width primary color background
- Parallax gradient animation
- "Ready to upgrade?" messaging
- Single button

**Target State:**

```
+------------------------------------------------------------------+
|   Ember Orange gradient background                               |
|                                                                  |
|   +-----------------------------------------------------------+  |
|   |                                                           |  |
|   |   READY TO EXPERIENCE PERFECT CLIMATE?                    |  |
|   |                                                           |  |
|   |   Browse our collection of premium HVAC systems           |  |
|   |   and find the perfect fit for your space.                |  |
|   |                                                           |  |
|   |   [Shop All Products]    [Get Expert Advice ->]           |  |
|   |                                                           |  |
|   +-----------------------------------------------------------+  |
|                                                                  |
+------------------------------------------------------------------+
```

**Design Specifications:**
- **Background:** Ember Orange gradient (`--color-ember-500` to `--color-ember-600`)
- **Layout:** Centered text with dual CTAs
- **Primary CTA:** "Shop All Products" (white button)
- **Secondary CTA:** "Get Expert Advice" (ghost/text link)
- **Animation:** Subtle gradient shift on scroll (NOT aggressive parallax)

**Tasks:**

- [ ] TASK-21A-047: Update CTA background to Ember Orange gradient
- [ ] TASK-21A-048: Add secondary CTA (ghost button style)
- [ ] TASK-21A-049: Remove aggressive parallax, add subtle gradient shift
- [ ] TASK-21A-050: Update CTA copy for more urgency
- [ ] TASK-21A-051: Ensure proper contrast for accessibility (WCAG AA)

---

### 9. Process Section (Remove or Simplify)

**Current State:**
- 4-step "How it Works" timeline
- Below featured products
- Reveal animations

**Target State:**
- **Option A:** Remove entirely (simplify page)
- **Option B:** Move to footer or dedicated "How It Works" page
- **Recommendation:** Remove from home page, link to dedicated page

**Tasks:**

- [ ] TASK-21A-052: Remove process section from home page
- [ ] TASK-21A-053: Create standalone "How It Works" page (optional)
- [ ] TASK-21A-054: Add "How It Works" link to footer

---

### 10. Stats Section (Remove or Replace)

**Current State:**
- Dark background with 4 stats
- "10K+ customers", "500+ products", etc.
- Count-up animation
- Feels generic/unverifiable

**Target State:**
- **Remove generic stats** - they erode trust
- **Replace with** specific, verifiable metrics OR remove entirely

**Alternative (if keeping):**
```
+------------------------------------------------------------------+
|                                                                  |
|   15+ Years         98%              4.9/5           24/7        |
|   in Business       Satisfaction     Rating          Support     |
|                     Rate             (500+ reviews)              |
|                                                                  |
+------------------------------------------------------------------+
```

**Tasks:**

- [ ] TASK-21A-055: Remove or redesign stats section with verifiable data
- [ ] TASK-21A-056: If keeping, connect to real review/order data
- [ ] TASK-21A-057: Update stats copy to be specific and believable

---

## Task List Summary

### Hero Section (8 tasks) - COMPLETED
- [x] TASK-21A-001: Remove gradient blob backgrounds from hero section
- [x] TASK-21A-002: Create HeroProductComponent with product image slot
- [x] TASK-21A-003: Implement seasonal promo banner with dismiss functionality
- [x] TASK-21A-004: Move value propositions strip above the fold
- [x] TASK-21A-005: Replace floating animations with single fade-in entrance
- [x] TASK-21A-006: Add prominent search bar to hero
- [x] TASK-21A-007: Create dual CTA layout (primary + secondary ghost button)
- [x] TASK-21A-008: Implement hero content API for dynamic seasonal content

### Categories Section (7 tasks) - COMPLETED
- [x] TASK-21A-009: Implement Bento grid layout for categories
- [x] TASK-21A-010: Remove TiltEffectDirective from category panels (already done in 21F)
- [x] TASK-21A-011: Create asymmetric panel sizing
- [x] TASK-21A-012: Update category images to product-focused photography (replaced with gradient backgrounds + icons)
- [x] TASK-21A-013: Simplify hover effects (scale + arrow only)
- [x] TASK-21A-014: Add category taglines to translation files
- [x] TASK-21A-015: Ensure proper aspect ratios on all breakpoints

### Featured Products Section (6 tasks)
- [ ] TASK-21A-016: Create FeaturedProductsGridComponent with hero product layout
- [ ] TASK-21A-017: Implement "Best Seller" / "Editor's Pick" badge component
- [ ] TASK-21A-018: Add energy rating display to product cards
- [ ] TASK-21A-019: Increase featured products from 4 to 7
- [ ] TASK-21A-020: Update ProductService to support hero product designation
- [ ] TASK-21A-021: Remove excessive hover animations from product items

### Trust Indicators Section (6 tasks)
- [ ] TASK-21A-022: Create TrustIndicatorsComponent with brand logos section
- [ ] TASK-21A-023: Source/create SVG logos for partner brands
- [ ] TASK-21A-024: Implement certification badges component
- [ ] TASK-21A-025: Replace text-only brand ticker with logo marquee
- [ ] TASK-21A-026: Add hover color transition for brand logos
- [ ] TASK-21A-027: Create translation keys for certification descriptions

### Testimonials Section (7 tasks)
- [ ] TASK-21A-028: Add photo support to testimonial data structure
- [ ] TASK-21A-029: Create TestimonialCardComponent with photo, rating, verification
- [ ] TASK-21A-030: Implement "Verified Purchase" badge component
- [ ] TASK-21A-031: Replace initials avatar with photo avatar
- [ ] TASK-21A-032: Add video testimonial thumbnail option
- [ ] TASK-21A-033: Implement crossfade transition (not slide)
- [ ] TASK-21A-034: Update testimonial translation structure

### Value Propositions Section (5 tasks)
- [ ] TASK-21A-035: Relocate value props to position directly after hero
- [ ] TASK-21A-036: Compact the value props design
- [ ] TASK-21A-037: Replace inline SVG icons with Lucide icon component
- [ ] TASK-21A-038: Remove scroll reveal animation
- [ ] TASK-21A-039: Add "on orders $500+" detail to shipping prop

### Newsletter Section (7 tasks)
- [ ] TASK-21A-040: Redesign newsletter with incentive-based messaging
- [ ] TASK-21A-041: Create NewsletterCardComponent with centered layout
- [ ] TASK-21A-042: Add incentive icon (gift/discount)
- [ ] TASK-21A-043: Implement success state with coupon code reveal
- [ ] TASK-21A-044: Add privacy disclaimer text
- [ ] TASK-21A-045: Update all newsletter translation keys
- [ ] TASK-21A-046: (Backend) Create newsletter signup coupon generation

### Final CTA Section (5 tasks)
- [ ] TASK-21A-047: Update CTA background to Ember Orange gradient
- [ ] TASK-21A-048: Add secondary CTA (ghost button style)
- [ ] TASK-21A-049: Remove aggressive parallax, add subtle gradient shift
- [ ] TASK-21A-050: Update CTA copy for more urgency
- [ ] TASK-21A-051: Ensure proper contrast for accessibility

### Cleanup Tasks (6 tasks)
- [ ] TASK-21A-052: Remove process section from home page
- [ ] TASK-21A-053: Create standalone "How It Works" page (optional)
- [ ] TASK-21A-054: Add "How It Works" link to footer
- [ ] TASK-21A-055: Remove or redesign stats section
- [ ] TASK-21A-056: Connect stats to real data (if keeping)
- [ ] TASK-21A-057: Update stats copy to be specific

**Total Tasks: 57**

---

## Technical Specifications

### Component Structure

```
src/ClimaSite.Web/src/app/features/home/
├── home.component.ts              # Main home page (refactor)
├── home.component.scss            # Extracted styles (new)
├── components/
│   ├── hero-section/
│   │   ├── hero-section.component.ts
│   │   ├── hero-section.component.scss
│   │   └── hero-section.component.html
│   ├── promo-banner/
│   │   └── promo-banner.component.ts
│   ├── category-grid/
│   │   └── category-grid.component.ts      # Bento grid
│   ├── featured-products/
│   │   └── featured-products.component.ts  # Hero + grid layout
│   ├── trust-indicators/
│   │   ├── trust-indicators.component.ts
│   │   ├── brand-marquee.component.ts
│   │   └── certification-badges.component.ts
│   ├── testimonial-carousel/
│   │   ├── testimonial-carousel.component.ts
│   │   └── testimonial-card.component.ts
│   ├── newsletter-card/
│   │   └── newsletter-card.component.ts
│   └── final-cta/
│       └── final-cta.component.ts
```

### New Components Needed

| Component | Purpose | Priority |
|-----------|---------|----------|
| `HeroSectionComponent` | Product-focused hero with search | High |
| `PromoBannerComponent` | Dismissible seasonal promo | Medium |
| `CategoryGridComponent` | Bento-style category navigation | High |
| `FeaturedProductsComponent` | Hero product + supporting grid | High |
| `TrustIndicatorsComponent` | Brand logos + certifications | High |
| `BrandMarqueeComponent` | Infinite scroll brand logos | Medium |
| `CertificationBadgesComponent` | Industry certification display | Medium |
| `TestimonialCarouselComponent` | Photo-based testimonials | High |
| `TestimonialCardComponent` | Individual testimonial display | High |
| `NewsletterCardComponent` | Incentive-based signup | Medium |
| `FinalCtaComponent` | Ember orange CTA section | Medium |
| `ProductBadgeComponent` | "Best Seller", Energy rating badges | High |

### Animations to Keep

| Animation | Location | Notes |
|-----------|----------|-------|
| Fade-in on load | Hero content | Single entrance, no stagger |
| Scroll reveal | Section entrances | Subtle fade-up, 1 per section |
| Hover lift | Product cards | 4px lift + shadow |
| Marquee scroll | Brand logos | Continuous, pause on hover |
| Crossfade | Testimonials | Between testimonial changes |
| Button hover | All CTAs | Subtle scale + shadow |

### Animations to Remove

| Animation | Location | Reason |
|-----------|----------|--------|
| Gradient blob float | Hero background | Visual noise, dated |
| Multiple parallax layers | Hero | Performance, distraction |
| Floating text | Hero content | Unnecessary motion |
| Tilt effect | Category cards | Over-designed |
| Stagger animations | Multiple sections | Too slow, fatiguing |
| Count-up | Stats | Feels gimmicky |
| Line draw | Process section | Section being removed |

### Responsive Breakpoints

| Breakpoint | Layout Changes |
|------------|----------------|
| `>1200px` | Full desktop: Bento grid, 7 products, horizontal layout |
| `1024px` | Tablet landscape: 2-column categories, 4 products |
| `768px` | Tablet portrait: Single column categories, 2 products |
| `480px` | Mobile: Stacked layout, carousel for products |

### CSS Architecture

```scss
// New design tokens for Plan 21A
:host {
  // Bento grid
  --bento-gap: 1rem;
  --bento-radius: var(--radius-lg);
  
  // Hero
  --hero-min-height: 80vh;
  --hero-product-width: 60%;
  
  // Trust section
  --marquee-speed: 30s;
  --logo-grayscale: 1;
  --logo-opacity: 0.6;
  
  // Newsletter
  --newsletter-bg: var(--gradient-aurora-soft);
  
  // CTA
  --cta-gradient: linear-gradient(135deg, var(--color-ember-500) 0%, var(--color-ember-600) 100%);
}
```

---

## Content Requirements

### Images Needed

| Image | Dimensions | Format | Notes |
|-------|------------|--------|-------|
| Hero product image | 800x800 min | WebP + PNG fallback | Featured AC unit, transparent bg |
| Category: Air Conditioning | 600x800 | WebP | Product-focused, modern interior |
| Category: Heating | 600x400 | WebP | Heating system in use |
| Category: Ventilation | 600x400 | WebP | Clean air/duct visual |
| Category: Water Purification | 600x400 | WebP | Water filter system |
| Brand logos (x10) | 120x60 | SVG | Grayscale versions |
| Certification badges (x3) | 80x80 | SVG | Energy Star, ISO, TUV |
| Testimonial photos (x3) | 100x100 | WebP | Real or realistic headshots |
| Video thumbnail | 400x225 | WebP | Optional video testimonial |

### Copy Changes

| Section | Current | New |
|---------|---------|-----|
| Hero eyebrow | "Premium Climate Solutions" | "Nordic Tech Climate Systems" |
| Hero title | "Comfort Engineered / For Every Space" | "Perfect Climate. / Precision Engineered." |
| Hero subtitle | "Energy-efficient HVAC systems..." | "Premium HVAC solutions for homes and businesses. Energy-efficient. Professionally installed." |
| Hero CTA | "Explore Products" | "Shop All Products" |
| Newsletter title | "Stay Updated" | "Get 10% Off Your First Order" |
| Newsletter subtitle | "Get exclusive deals..." | "Plus exclusive deals, new product alerts, and climate tips." |
| CTA title | "Ready to upgrade your comfort?" | "Ready to Experience Perfect Climate?" |

### Translation Keys (New/Updated)

```json
// EN translations to add/update
{
  "home.hero.eyebrow": "Nordic Tech Climate Systems",
  "home.hero.title1": "Perfect Climate.",
  "home.hero.title2": "Precision Engineered.",
  "home.hero.subtitle": "Premium HVAC solutions for homes and businesses. Energy-efficient. Professionally installed.",
  "home.hero.cta.primary": "Shop All Products",
  "home.hero.cta.secondary": "Find Your System",
  
  "home.promo.winter": "Winter Sale: Up to 30% off heating systems",
  "home.promo.summer": "Summer Sale: Cool savings on air conditioners",
  "home.promo.cta": "Shop Now",
  
  "home.categories.airConditioning.tagline": "Stay cool with premium split ACs",
  "home.categories.heating.tagline": "Efficient warmth for every room",
  "home.categories.ventilation.tagline": "Fresh air, pure comfort",
  "home.categories.waterPurification.tagline": "Crystal clear, always",
  
  "home.products.badge.bestSeller": "Best Seller",
  "home.products.badge.editorsPick": "Editor's Pick",
  "home.products.badge.newArrival": "New",
  
  "home.trust.title": "Trusted by Industry Leaders",
  "home.trust.certifications.energyStar": "Energy Star Certified",
  "home.trust.certifications.iso": "ISO 9001 Quality",
  "home.trust.certifications.tuv": "TUV Safety Certified",
  
  "home.testimonials.verified": "Verified Purchase",
  "home.testimonials.watchVideo": "Watch Video Testimonial",
  
  "home.values.shipping.detail": "on orders over $500",
  
  "home.newsletter.title": "Get 10% Off Your First Order",
  "home.newsletter.subtitle": "Plus exclusive deals, new product alerts, and climate tips delivered to your inbox.",
  "home.newsletter.privacy": "No spam. Unsubscribe anytime.",
  "home.newsletter.success": "Welcome! Your 10% off code:",
  "home.newsletter.couponCode": "WELCOME10",
  
  "home.cta.title": "Ready to Experience Perfect Climate?",
  "home.cta.subtitle": "Browse our collection of premium HVAC systems and find the perfect fit for your space.",
  "home.cta.primary": "Shop All Products",
  "home.cta.secondary": "Get Expert Advice"
}
```

---

## Dependencies

### On Other Plans

| Plan | Dependency | Impact |
|------|------------|--------|
| **Plan 21E** | Button variants, icon system | Required for consistent CTAs |
| **Plan 21G** | Trust badge component | Can share certification badges |
| **Plan 21B** | Product card enhancements | Energy ratings on cards |
| **Plan 21C** | Header search | Hero search may use same component |

### Backend Requirements

| Requirement | API Endpoint | Notes |
|-------------|--------------|-------|
| Hero product designation | `GET /api/products/featured?hero=true` | New query param |
| Seasonal promo content | `GET /api/content/promo-banner` | New endpoint |
| Newsletter coupon | `POST /api/newsletter/subscribe` | Return coupon code |
| Testimonials with photos | `GET /api/reviews/featured` | Include photoUrl |

### Third-Party

| Dependency | Purpose | Status |
|------------|---------|--------|
| Lucide Icons | Consistent icon library | Need to install |
| Brand logo SVGs | Trust indicators | Need to source/create |

---

## Testing Checklist

### Visual Testing

- [ ] Hero section renders correctly without gradient blobs
- [ ] Product image displays at correct size/position
- [ ] Bento grid categories have correct proportions
- [ ] All images have proper aspect ratios
- [ ] Trust badges render with correct colors
- [ ] Testimonial photos display properly (with fallback)
- [ ] Newsletter card is centered and styled correctly
- [ ] CTA section has Ember Orange gradient
- [ ] All sections work in light theme
- [ ] All sections work in dark theme

### Responsive Testing

- [ ] Desktop (1440px): Full layout, all features visible
- [ ] Laptop (1200px): Categories adjust, products visible
- [ ] Tablet landscape (1024px): 2-column grid where appropriate
- [ ] Tablet portrait (768px): Single column, horizontal scroll where needed
- [ ] Mobile (480px): Stacked layout, touch-friendly
- [ ] Mobile (375px): Minimum supported width

### Animation Testing

- [ ] Hero fade-in plays once on load
- [ ] No floating/parallax on hero content
- [ ] Section reveals trigger on scroll
- [ ] Hover effects work on desktop
- [ ] Touch interactions work on mobile
- [ ] `prefers-reduced-motion` respected

### Accessibility Testing

- [ ] All images have alt text
- [ ] Focus states visible on all interactive elements
- [ ] Keyboard navigation works through all sections
- [ ] Screen reader announces sections correctly
- [ ] Color contrast meets WCAG AA (4.5:1)
- [ ] CTA section meets contrast requirements

### i18n Testing

- [ ] All text uses translation keys
- [ ] EN translations complete
- [ ] BG translations complete
- [ ] DE translations complete
- [ ] Dynamic content (testimonials) translates correctly

### Performance Testing

- [ ] Lighthouse Performance score > 90
- [ ] No layout shifts (CLS < 0.1)
- [ ] Images lazy load below fold
- [ ] Animations use GPU-accelerated properties
- [ ] No JavaScript errors in console

### E2E Testing

- [ ] User can click hero CTA and navigate to products
- [ ] User can click category and navigate to category page
- [ ] User can click product card and navigate to product detail
- [ ] Newsletter form submits successfully
- [ ] Promo banner can be dismissed
- [ ] Testimonials cycle automatically
- [ ] Brand marquee scrolls and pauses on hover

---

## File Paths Reference

### Files to Modify

| File | Changes |
|------|---------|
| `src/ClimaSite.Web/src/app/features/home/home.component.ts` | Major refactor, extract sections |
| `src/ClimaSite.Web/src/styles/_colors.scss` | Add Ember gradient variable |
| `src/ClimaSite.Web/src/assets/i18n/en.json` | New translation keys |
| `src/ClimaSite.Web/src/assets/i18n/bg.json` | New translation keys |
| `src/ClimaSite.Web/src/assets/i18n/de.json` | New translation keys |
| `src/ClimaSite.Web/src/app/core/services/product.service.ts` | Add hero product support |

### Files to Create

| File | Purpose |
|------|---------|
| `src/ClimaSite.Web/src/app/features/home/components/hero-section/hero-section.component.ts` | New hero |
| `src/ClimaSite.Web/src/app/features/home/components/promo-banner/promo-banner.component.ts` | Seasonal banner |
| `src/ClimaSite.Web/src/app/features/home/components/category-grid/category-grid.component.ts` | Bento categories |
| `src/ClimaSite.Web/src/app/features/home/components/featured-products/featured-products.component.ts` | Hero + grid |
| `src/ClimaSite.Web/src/app/features/home/components/trust-indicators/trust-indicators.component.ts` | Logos + certs |
| `src/ClimaSite.Web/src/app/features/home/components/testimonial-carousel/testimonial-carousel.component.ts` | New testimonials |
| `src/ClimaSite.Web/src/app/features/home/components/newsletter-card/newsletter-card.component.ts` | New newsletter |
| `src/ClimaSite.Web/src/app/features/home/components/final-cta/final-cta.component.ts` | New CTA |
| `src/ClimaSite.Web/src/app/shared/components/product-badge/product-badge.component.ts` | Badges |
| `src/ClimaSite.Web/src/assets/images/brands/*.svg` | Brand logos |
| `src/ClimaSite.Web/src/assets/images/certifications/*.svg` | Cert badges |

### Files to Remove/Deprecate

| File | Reason |
|------|--------|
| Hero gradient blob styles | Replaced with product focus |
| Process section code | Moved to separate page |

---

## Implementation Order

### Day 1: Foundation
1. TASK-21A-001: Remove gradient blobs
2. TASK-21A-002: Create HeroSectionComponent skeleton
3. TASK-21A-035-039: Relocate and update value props

### Day 2: Hero & Categories
4. TASK-21A-003-008: Complete hero section
5. TASK-21A-009-015: Implement Bento category grid

### Day 3: Products & Trust
6. TASK-21A-016-021: Featured products with hero layout
7. TASK-21A-022-027: Trust indicators section

### Day 4: Social Proof & Conversion
8. TASK-21A-028-034: Testimonials redesign
9. TASK-21A-040-046: Newsletter redesign

### Day 5: Polish & Cleanup
10. TASK-21A-047-051: Final CTA section
11. TASK-21A-052-057: Remove/simplify stats and process
12. Full testing and responsive fixes

---

*Document created: January 24, 2026*
*Parent plan: 21-ui-redesign-master-plan.md*
*Status: Ready for implementation*
