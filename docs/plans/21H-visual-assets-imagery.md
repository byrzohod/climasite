# Plan 21H: Visual Assets & Imagery

## Overview

This plan establishes a comprehensive visual asset system for ClimaSite, transforming the current inconsistent imagery (gradient blobs, mixed stock photos, emoji icons) into a cohesive "Nordic Tech" visual identity. The focus is on high-quality, purposeful imagery that builds trust and enhances the shopping experience.

### Current State Analysis

| Asset Type | Current State | Issues |
|------------|---------------|--------|
| Hero Images | Gradient blobs background | No product context, generic feel |
| Category Images | Unsplash stock photos | Inconsistent style, generic |
| Product Images | API-sourced | Inconsistent backgrounds, varying quality |
| Icons | Mixed inline SVG + emoji | No consistency, unprofessional |
| Illustrations | None | Missing empty states, error states |
| Brand Logos | Text only | No visual brand recognition |
| Trust Badges | None | Missing payment/certification badges |

### Goals

1. **Professional Appearance** - Establish "Nordic Tech" visual identity
2. **Consistency** - Unified visual language across all imagery
3. **Performance** - Optimized assets with modern formats and lazy loading
4. **Trust Building** - Professional imagery that conveys quality and reliability
5. **Accessibility** - Proper alt text, contrast, and fallbacks

### Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Largest Contentful Paint (LCP) | < 2.5s | Lighthouse audit |
| Image-related CLS | 0 | No layout shifts from images |
| Visual consistency score | 90%+ | Design review checklist |
| Image accessibility | 100% | All images have proper alt text |
| Format adoption | 95%+ | WebP with fallbacks |
| Placeholder coverage | 100% | All image slots have loading states |

### Estimated Effort

| Phase | Effort | Priority |
|-------|--------|----------|
| Icon Library Setup | 4 hours | P0 - Critical |
| Hero Imagery System | 8 hours | P0 - Critical |
| Product Image Guidelines | 6 hours | P0 - Critical |
| Category Visuals | 6 hours | P1 - High |
| Illustrations | 8 hours | P1 - High |
| Trust Badges | 4 hours | P1 - High |
| Background Patterns | 4 hours | P2 - Medium |
| Brand Logos | 4 hours | P2 - Medium |
| **Total** | **44 hours** | |

---

## Asset Categories

### 1. Hero Imagery

Transform the hero section from abstract gradient blobs to impactful lifestyle photography that showcases HVAC products in real-world contexts.

#### Strategy

- **Primary Approach**: High-quality lifestyle photography showing HVAC products in modern homes/offices
- **Seasonal Variations**: Different hero images for summer (cooling focus) and winter (heating focus)
- **Fallback**: Elegant gradient with subtle product silhouettes

#### Hero Image Requirements

| Attribute | Specification |
|-----------|---------------|
| Primary Dimensions | 1920x800px (desktop) |
| Tablet Dimensions | 1024x600px |
| Mobile Dimensions | 768x500px |
| Format | WebP primary, JPEG fallback |
| Max File Size | 150KB (desktop), 80KB (mobile) |
| Style | Nordic minimal, cool tones, natural light |
| Focal Point | Right side (text overlay on left) |

#### Seasonal Themes

| Season | Theme | Color Accent | Products Featured |
|--------|-------|--------------|-------------------|
| Summer (May-Sep) | Cool comfort | Cool blues, white | Air conditioners, fans |
| Winter (Oct-Apr) | Warm sanctuary | Warm amber, soft white | Heaters, heat pumps |
| Default | Year-round comfort | Neutral, balanced | Mixed HVAC |

#### Tasks

- [ ] TASK-21H-001: Create hero image component with responsive srcset support
- [ ] TASK-21H-002: Source/create summer hero lifestyle image (AC in modern living room)
- [ ] TASK-21H-003: Source/create winter hero lifestyle image (heating in cozy home)
- [ ] TASK-21H-004: Source/create default hero image (year-round comfort theme)
- [ ] TASK-21H-005: Create hero image optimization pipeline (resize, compress, WebP conversion)
- [ ] TASK-21H-006: Implement seasonal hero image rotation based on date
- [ ] TASK-21H-007: Create gradient fallback for slow connections/loading
- [ ] TASK-21H-008: Add hero image preloading with priority hints

---

### 2. Category Visuals

Replace generic Unsplash stock photos with either custom illustrations or carefully curated product photography.

#### Decision: Product Photography over Illustrations

After analysis, **curated product photography** is recommended for categories because:
- Products are tangible items customers want to see
- Photography builds trust for e-commerce
- Easier to maintain consistency with product images
- Professional product shots available from manufacturers

#### Category Image Requirements

| Attribute | Specification |
|-----------|---------------|
| Dimensions | 600x400px (3:2 ratio) |
| Thumbnail | 300x200px |
| Format | WebP primary, JPEG fallback |
| Max File Size | 50KB (full), 20KB (thumb) |
| Background | Neutral light gray (#F5F5F5) or white |
| Style | Clean, centered product, soft shadow |

#### Category Visual Strategy

| Category | Visual Approach | Key Product(s) to Feature |
|----------|-----------------|---------------------------|
| Air Conditioners | Split unit, wall-mounted | Premium split AC unit |
| Portable AC | Standalone unit | Modern portable AC |
| Heating | Radiator or heat pump | Stylish electric heater |
| Heat Pumps | Outdoor/indoor unit | Air-to-air heat pump |
| Ventilation | Ventilation unit | Modern ventilation system |
| Accessories | Grouped accessories | Remotes, filters, mounts |
| Parts | Technical parts | Filters, components |

#### Tasks

- [ ] TASK-21H-009: Create category image component with hover effects
- [ ] TASK-21H-010: Source category images for Air Conditioners
- [ ] TASK-21H-011: Source category images for Portable AC
- [ ] TASK-21H-012: Source category images for Heating
- [ ] TASK-21H-013: Source category images for Heat Pumps
- [ ] TASK-21H-014: Source category images for Ventilation
- [ ] TASK-21H-015: Source category images for Accessories
- [ ] TASK-21H-016: Source category images for Parts
- [ ] TASK-21H-017: Process all category images (resize, optimize, WebP)
- [ ] TASK-21H-018: Create category image hover zoom effect

---

### 3. Product Image Guidelines

Establish standards for product imagery to ensure consistency across all products, regardless of source.

#### Product Image Standards

| Attribute | Primary Image | Gallery Images | Thumbnail |
|-----------|---------------|----------------|-----------|
| Dimensions | 800x800px | 1200x1200px | 300x300px |
| Aspect Ratio | 1:1 (square) | 1:1 (square) | 1:1 (square) |
| Background | Pure white (#FFFFFF) | White or contextual | Pure white |
| Format | WebP + JPEG fallback | WebP + JPEG | WebP + JPEG |
| Max Size | 80KB | 150KB | 25KB |

#### Required Product Angles

| Angle | Purpose | Required |
|-------|---------|----------|
| Front | Primary display image | Yes |
| 45-degree | Show depth and design | Recommended |
| Side | Installation context | Optional |
| Detail | Features, controls, vents | Recommended |
| Scale | Size reference (with object) | Optional |
| Installed | In-context lifestyle | Optional |

#### Background Processing

For products with inconsistent backgrounds, implement automated background removal:

```typescript
// Product image processing pipeline
interface ProductImageConfig {
  removeBackground: boolean;
  targetBackground: string; // '#FFFFFF' for white
  padding: number; // percentage
  shadow: {
    enabled: boolean;
    blur: number;
    opacity: number;
    offsetY: number;
  };
}
```

#### Tasks

- [ ] TASK-21H-019: Document product image guidelines for suppliers/admins
- [ ] TASK-21H-020: Create product image validation service (dimensions, format, size)
- [ ] TASK-21H-021: Implement client-side image preview with guidelines overlay
- [ ] TASK-21H-022: Create product image zoom component for gallery
- [ ] TASK-21H-023: Implement image gallery with lightbox
- [ ] TASK-21H-024: Add product image lazy loading with blur-up placeholders
- [ ] TASK-21H-025: Create consistent product shadow/reflection effect (CSS)

---

### 4. Icon Library

Standardize on Lucide icons for consistency, replacing mixed inline SVGs and emoji.

#### Icon Library Choice: Lucide

| Criteria | Lucide | Heroicons | Feather |
|----------|--------|-----------|---------|
| Style | Clean, minimal | Bold, modern | Thin, elegant |
| Icon Count | 1000+ | 450+ | 280+ |
| Tree-shakeable | Yes | Yes | Yes |
| Angular Support | Yes | Yes | Yes |
| **Selected** | **Yes** | No | No |

Lucide selected for: largest icon set, active development, clean Nordic-compatible style.

#### Icon Size Scale

| Size Name | Pixels | Use Case |
|-----------|--------|----------|
| xs | 12px | Inline with small text |
| sm | 16px | Buttons, form inputs |
| md | 20px | Default, navigation |
| lg | 24px | Feature highlights |
| xl | 32px | Section headers |
| 2xl | 48px | Hero features, empty states |

#### Icon Categories Needed

| Category | Icons Required | Lucide Icons |
|----------|----------------|--------------|
| Navigation | Home, search, menu, close | `home`, `search`, `menu`, `x` |
| E-commerce | Cart, heart, package, truck | `shopping-cart`, `heart`, `package`, `truck` |
| User | User, settings, logout, orders | `user`, `settings`, `log-out`, `clipboard-list` |
| Products | Filter, sort, grid, list | `filter`, `arrow-up-down`, `grid`, `list` |
| Actions | Add, edit, delete, save | `plus`, `pencil`, `trash-2`, `save` |
| Status | Check, warning, error, info | `check-circle`, `alert-triangle`, `x-circle`, `info` |
| Social | Share, facebook, twitter | `share-2`, `facebook`, `twitter` |
| HVAC Specific | Temperature, fan, snowflake | `thermometer`, `fan`, `snowflake` |

#### Custom Icons (if needed)

Some HVAC-specific icons may need custom creation:

| Icon | Description | Approach |
|------|-------------|----------|
| Split AC Unit | Wall-mounted AC | Custom SVG |
| Heat Pump | Outdoor unit | Custom SVG |
| BTU Indicator | Cooling capacity | Custom SVG |
| Energy Rating | A+++ style badge | Custom SVG |

#### Tasks

- [ ] TASK-21H-026: Install and configure lucide-angular package
- [ ] TASK-21H-027: Create IconComponent wrapper with size/color props
- [ ] TASK-21H-028: Replace all emoji icons with Lucide equivalents
- [ ] TASK-21H-029: Replace inline SVG icons with Lucide components
- [ ] TASK-21H-030: Create icon showcase/documentation page (dev only)
- [ ] TASK-21H-031: Design custom HVAC-specific icons (4 icons)
- [ ] TASK-21H-032: Add icon accessibility (aria-label, role="img")

---

### 5. Illustrations

Create custom spot illustrations for empty states, error states, and onboarding to add personality and guide users.

#### Illustration Style Guide

| Attribute | Specification |
|-----------|---------------|
| Style | Flat design with subtle gradients |
| Colors | Brand palette (primary blue, accent colors) |
| Line Weight | 2px consistent stroke |
| Dimensions | 200x200px (spot), 400x300px (full) |
| Format | SVG (scalable), PNG fallback |
| Mood | Friendly, professional, helpful |

#### Required Illustrations

| Context | Illustration | Description |
|---------|--------------|-------------|
| Empty Cart | Shopping cart with breeze | Light, airy feel |
| Empty Wishlist | Heart with sparkles | Encouraging |
| Empty Search | Magnifying glass with "?" | Helpful, not frustrating |
| No Products | Box with question mark | Category empty state |
| Order Success | Checkmark with confetti | Celebratory |
| Order Processing | Package with motion lines | Activity indicator |
| Error 404 | Lost snowflake/AC unit | Playful, on-brand |
| Error 500 | Broken AC with tools | Technical but friendly |
| Offline | Cloud with disconnect | Network issues |
| Maintenance | Wrench with AC | Scheduled downtime |
| Welcome/Onboarding | Home with comfort | First-time user |
| Email Sent | Envelope with checkmark | Confirmation |

#### Tasks

- [ ] TASK-21H-033: Create illustration style guide document
- [ ] TASK-21H-034: Design Empty Cart illustration
- [ ] TASK-21H-035: Design Empty Wishlist illustration
- [ ] TASK-21H-036: Design Empty Search Results illustration
- [ ] TASK-21H-037: Design No Products illustration
- [ ] TASK-21H-038: Design Order Success illustration
- [ ] TASK-21H-039: Design Order Processing illustration
- [ ] TASK-21H-040: Design 404 Error illustration
- [ ] TASK-21H-041: Design 500 Error illustration
- [ ] TASK-21H-042: Design Offline State illustration
- [ ] TASK-21H-043: Design Maintenance illustration
- [ ] TASK-21H-044: Create EmptyStateComponent with illustration slots
- [ ] TASK-21H-045: Implement illustrations in all empty state locations

---

### 6. Brand Logos

Collect and display partner brand logos for products sold on the platform.

#### Brand Logo Requirements

| Attribute | Specification |
|-----------|---------------|
| Format | SVG preferred, PNG with transparency |
| Dimensions | Max 200x80px (horizontal), 100x100px (square) |
| Color Mode | Full color + monochrome versions |
| Background | Transparent |
| Min Clear Space | 10% of logo width on all sides |

#### Expected Brand Partners

| Brand | Category | Logo Status |
|-------|----------|-------------|
| Daikin | AC, Heat Pumps | Needed |
| Mitsubishi Electric | AC, Heat Pumps | Needed |
| LG | AC, Air Purifiers | Needed |
| Samsung | AC | Needed |
| Toshiba | AC | Needed |
| Panasonic | AC, Heating | Needed |
| Carrier | AC, Heating | Needed |
| Bosch | Heating, Heat Pumps | Needed |
| Vaillant | Heating | Needed |
| Viessmann | Heat Pumps | Needed |

#### Brand Display Locations

- Product detail page (brand attribution)
- "Our Brands" section on homepage
- Category filter sidebar
- Search filters

#### Tasks

- [ ] TASK-21H-046: Create brand logo collection directory structure
- [ ] TASK-21H-047: Source and process Daikin logo (color + mono)
- [ ] TASK-21H-048: Source and process Mitsubishi Electric logo
- [ ] TASK-21H-049: Source and process LG logo
- [ ] TASK-21H-050: Source and process Samsung logo
- [ ] TASK-21H-051: Source and process remaining brand logos (6 brands)
- [ ] TASK-21H-052: Create BrandLogoComponent with hover effects
- [ ] TASK-21H-053: Create "Our Brands" carousel/grid for homepage
- [ ] TASK-21H-054: Add brand logo to product cards and detail pages

---

### 7. Trust & Badge Graphics

Create and implement trust indicators to build customer confidence.

#### Payment Method Badges

| Provider | Format | Size |
|----------|--------|------|
| Visa | SVG | 48x30px |
| Mastercard | SVG | 48x30px |
| American Express | SVG | 48x30px |
| PayPal | SVG | 60x30px |
| Apple Pay | SVG | 48x30px |
| Google Pay | SVG | 48x30px |
| Stripe | SVG | 48x30px |

#### Certification Badges

| Badge | Purpose | Placement |
|-------|---------|-----------|
| SSL Secure | Security indicator | Footer, checkout |
| GDPR Compliant | Privacy assurance | Footer |
| Verified Business | Trust indicator | Footer |
| Money-Back Guarantee | Risk reduction | Product page, checkout |
| Free Shipping | Value proposition | Header, product page |
| Warranty Badge | Product protection | Product page |

#### Energy Rating Badges

| Rating | Color | Use |
|--------|-------|-----|
| A+++ | Dark green | Highest efficiency |
| A++ | Green | High efficiency |
| A+ | Light green | Good efficiency |
| A | Yellow-green | Standard |
| B-G | Yellow to red | Lower ratings |

#### Tasks

- [ ] TASK-21H-055: Source/create payment method badge set (SVG)
- [ ] TASK-21H-056: Create SSL/Security badge
- [ ] TASK-21H-057: Create GDPR compliance badge
- [ ] TASK-21H-058: Create Money-Back Guarantee badge
- [ ] TASK-21H-059: Create Free Shipping badge
- [ ] TASK-21H-060: Create Warranty badge variants (1yr, 2yr, 5yr)
- [ ] TASK-21H-061: Create Energy Rating badge component (A+++ to G)
- [ ] TASK-21H-062: Create TrustBadgesComponent for footer
- [ ] TASK-21H-063: Create PaymentMethodsComponent for checkout
- [ ] TASK-21H-064: Add trust badges to checkout flow

---

### 8. Background Patterns & Textures

Subtle backgrounds that add visual interest without distracting from content.

#### Pattern Library

| Pattern | Use Case | Style |
|---------|----------|-------|
| Subtle Grid | Section backgrounds | 1px lines, 5% opacity |
| Dot Matrix | Hero overlay | Small dots, 3% opacity |
| Noise Texture | Cards, panels | Fine grain, 2% opacity |
| Wave Pattern | Footer, dividers | Smooth curves, brand color |
| Geometric | Feature sections | Triangles/hexagons, 5% opacity |

#### Gradient Presets

| Name | Colors | Use Case |
|------|--------|----------|
| Nordic Blue | `#E8F4FC` to `#FFFFFF` | Hero backgrounds |
| Warm Comfort | `#FEF7ED` to `#FFFFFF` | Heating sections |
| Cool Breeze | `#EDF7FE` to `#FFFFFF` | Cooling sections |
| Neutral Fade | `#F8F9FA` to `#FFFFFF` | General sections |
| Dark Gradient | `#1A1A2E` to `#16213E` | Dark mode hero |

#### Tasks

- [ ] TASK-21H-065: Create subtle grid pattern SVG
- [ ] TASK-21H-066: Create dot matrix pattern SVG
- [ ] TASK-21H-067: Create noise texture (tiny PNG, tileable)
- [ ] TASK-21H-068: Create wave pattern SVG for dividers
- [ ] TASK-21H-069: Define gradient presets in _colors.scss
- [ ] TASK-21H-070: Create BackgroundPatternComponent
- [ ] TASK-21H-071: Apply patterns to hero section
- [ ] TASK-21H-072: Apply patterns to feature sections

---

## Image Optimization Strategy

### Format Strategy

```
Primary: WebP (85% quality, lossy)
Fallback: JPEG (85% quality) for images, PNG for transparency
Icons: SVG (inline or sprite)
Illustrations: SVG primary, PNG@2x fallback
```

### Responsive Images Implementation

```html
<!-- Hero Image Example -->
<picture>
  <source
    media="(min-width: 1024px)"
    srcset="hero-desktop.webp 1920w, hero-desktop-1280.webp 1280w"
    sizes="100vw"
    type="image/webp"
  />
  <source
    media="(min-width: 768px)"
    srcset="hero-tablet.webp 1024w"
    sizes="100vw"
    type="image/webp"
  />
  <source
    srcset="hero-mobile.webp 768w"
    sizes="100vw"
    type="image/webp"
  />
  <!-- JPEG fallbacks -->
  <source
    media="(min-width: 1024px)"
    srcset="hero-desktop.jpg 1920w, hero-desktop-1280.jpg 1280w"
    sizes="100vw"
  />
  <img
    src="hero-mobile.jpg"
    alt="Modern home with efficient climate control"
    width="768"
    height="500"
    loading="eager"
    fetchpriority="high"
  />
</picture>
```

### Lazy Loading Strategy

| Image Type | Loading | Priority |
|------------|---------|----------|
| Hero images | eager | high |
| Above-fold products | eager | high |
| Below-fold products | lazy | auto |
| Category images | lazy | auto |
| Gallery thumbnails | lazy | low |
| Brand logos | lazy | low |

### Placeholder Strategy

#### Blur-Up Placeholders

```typescript
// Generate tiny placeholder (20px wide, base64 encoded)
interface ImageWithPlaceholder {
  src: string;
  srcset: string;
  placeholder: string; // base64 tiny image
  width: number;
  height: number;
  alt: string;
}
```

#### Skeleton Placeholders

```scss
.image-skeleton {
  background: linear-gradient(
    90deg,
    var(--color-gray-100) 25%,
    var(--color-gray-200) 50%,
    var(--color-gray-100) 75%
  );
  background-size: 200% 100%;
  animation: skeleton-loading 1.5s infinite;
}

@keyframes skeleton-loading {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

### Tasks

- [ ] TASK-21H-073: Create ImageComponent with WebP fallback support
- [ ] TASK-21H-074: Implement responsive image srcset generation
- [ ] TASK-21H-075: Create blur-up placeholder generation script
- [ ] TASK-21H-076: Implement skeleton loading placeholders
- [ ] TASK-21H-077: Add Intersection Observer for lazy loading
- [ ] TASK-21H-078: Configure image CDN/optimization pipeline
- [ ] TASK-21H-079: Add fetchpriority hints for critical images

---

## Asset Specifications Summary

### Dimension Reference

| Asset Type | Dimensions | Aspect Ratio | Max Size |
|------------|------------|--------------|----------|
| Hero (Desktop) | 1920x800 | 2.4:1 | 150KB |
| Hero (Tablet) | 1024x600 | 1.7:1 | 100KB |
| Hero (Mobile) | 768x500 | 1.5:1 | 80KB |
| Category | 600x400 | 3:2 | 50KB |
| Category Thumb | 300x200 | 3:2 | 20KB |
| Product Primary | 800x800 | 1:1 | 80KB |
| Product Gallery | 1200x1200 | 1:1 | 150KB |
| Product Thumb | 300x300 | 1:1 | 25KB |
| Brand Logo | 200x80 max | varies | 10KB |
| Illustration (Spot) | 200x200 | 1:1 | 15KB |
| Illustration (Full) | 400x300 | 4:3 | 25KB |
| Icon | 12-48px | 1:1 | <2KB |

### Color Profiles

- All images: sRGB color space
- No embedded color profiles (strip on optimization)
- Consistent white balance across product images

---

## Placeholder System

### Loading States

```typescript
// Placeholder configuration by context
const placeholderConfig = {
  hero: {
    type: 'gradient',
    colors: ['var(--color-primary-50)', 'var(--color-gray-100)'],
  },
  product: {
    type: 'skeleton',
    aspectRatio: '1:1',
  },
  category: {
    type: 'blur',
    placeholderSrc: 'data:image/jpeg;base64,...',
  },
  avatar: {
    type: 'initials',
    fallbackIcon: 'user',
  },
};
```

### Error State Images

| Context | Fallback | Icon |
|---------|----------|------|
| Product Image | Gray box + camera icon | `image-off` |
| Category Image | Gray box + folder icon | `folder` |
| User Avatar | Initials or user icon | `user` |
| Brand Logo | Brand name text | none |
| Hero | Gradient background | none |

### No-Image Fallbacks

```typescript
// Default fallback images
const fallbackImages = {
  product: '/assets/images/fallbacks/no-product-image.svg',
  category: '/assets/images/fallbacks/no-category-image.svg',
  user: '/assets/images/fallbacks/default-avatar.svg',
  brand: null, // Use text fallback
};
```

### Tasks

- [ ] TASK-21H-080: Create no-product-image fallback SVG
- [ ] TASK-21H-081: Create no-category-image fallback SVG
- [ ] TASK-21H-082: Create default-avatar fallback SVG
- [ ] TASK-21H-083: Implement ImageFallbackDirective
- [ ] TASK-21H-084: Add error handling to all image components

---

## Dependencies

### Depends On (Prerequisites)

| Plan | Dependency | Reason |
|------|------------|--------|
| 21A | Design Tokens | Color variables for illustrations, patterns |
| 21B | Typography | Font pairing for badges, overlays |

### Depended On By

| Plan | Usage |
|------|-------|
| 21C | Component Library uses icons, images |
| 21D | Homepage uses hero, categories, illustrations |
| 21E | Product Pages use gallery, badges |
| 21F | Checkout uses trust badges, payment icons |
| 21G | Admin uses icons throughout |

### External Dependencies

| Dependency | Purpose | Version |
|------------|---------|---------|
| lucide-angular | Icon library | ^0.300.0 |
| sharp (backend) | Image processing | ^0.33.0 |
| @angular/cdk | Image loading utilities | ^19.0.0 |

---

## Testing Checklist

### Image Loading Tests

- [ ] All hero images load correctly at each breakpoint
- [ ] WebP images load in supported browsers
- [ ] JPEG fallbacks work in Safari < 14
- [ ] Lazy loaded images load on scroll
- [ ] Priority images load immediately
- [ ] No layout shift when images load (CLS = 0)

### Fallback Tests

- [ ] Broken image URLs show fallback image
- [ ] Missing product images show placeholder
- [ ] Network error shows offline illustration
- [ ] Slow connections show skeleton/blur placeholder

### Performance Tests

- [ ] LCP < 2.5s with hero image
- [ ] Total image weight < 500KB on homepage
- [ ] Total image weight < 300KB on product list
- [ ] All images optimized (no images > spec max size)
- [ ] No render-blocking image requests

### Accessibility Tests

- [ ] All images have meaningful alt text
- [ ] Decorative images have empty alt=""
- [ ] Icons have aria-label or aria-hidden
- [ ] Color contrast on overlaid text meets WCAG AA
- [ ] Illustrations have title/desc for screen readers

### Cross-Browser Tests

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Safari iOS
- [ ] Chrome Android

### Theme Tests

- [ ] All assets work in light theme
- [ ] All assets work in dark theme
- [ ] Illustrations adapt to theme (if applicable)
- [ ] Icons have correct colors in both themes

### Responsive Tests

- [ ] Hero images correct at 320px, 768px, 1024px, 1440px, 1920px
- [ ] Product images scale correctly
- [ ] Icons remain crisp at all sizes
- [ ] No horizontal scroll from oversized images

---

## Task Summary

### Total Tasks: 84

| Category | Task Range | Count |
|----------|------------|-------|
| Hero Imagery | TASK-21H-001 to 008 | 8 |
| Category Visuals | TASK-21H-009 to 018 | 10 |
| Product Images | TASK-21H-019 to 025 | 7 |
| Icon Library | TASK-21H-026 to 032 | 7 |
| Illustrations | TASK-21H-033 to 045 | 13 |
| Brand Logos | TASK-21H-046 to 054 | 9 |
| Trust Badges | TASK-21H-055 to 064 | 10 |
| Background Patterns | TASK-21H-065 to 072 | 8 |
| Image Optimization | TASK-21H-073 to 079 | 7 |
| Placeholder System | TASK-21H-080 to 084 | 5 |

### Priority Breakdown

| Priority | Tasks | Description |
|----------|-------|-------------|
| P0 - Critical | 22 | Icon library, hero images, product guidelines |
| P1 - High | 42 | Categories, illustrations, trust badges |
| P2 - Medium | 20 | Brand logos, background patterns |

---

## Implementation Order

### Phase 1: Foundation (Week 1)
1. Icon library setup (TASK-21H-026 to 032)
2. Image component infrastructure (TASK-21H-073 to 079)
3. Placeholder system (TASK-21H-080 to 084)

### Phase 2: Core Assets (Week 2)
1. Hero imagery (TASK-21H-001 to 008)
2. Product image guidelines (TASK-21H-019 to 025)
3. Category visuals (TASK-21H-009 to 018)

### Phase 3: Enhancement (Week 3)
1. Illustrations (TASK-21H-033 to 045)
2. Trust badges (TASK-21H-055 to 064)

### Phase 4: Polish (Week 4)
1. Brand logos (TASK-21H-046 to 054)
2. Background patterns (TASK-21H-065 to 072)

---

## File Structure

```
src/ClimaSite.Web/src/
├── assets/
│   ├── images/
│   │   ├── hero/
│   │   │   ├── summer/
│   │   │   │   ├── hero-desktop.webp
│   │   │   │   ├── hero-desktop.jpg
│   │   │   │   ├── hero-tablet.webp
│   │   │   │   └── hero-mobile.webp
│   │   │   ├── winter/
│   │   │   └── default/
│   │   ├── categories/
│   │   │   ├── air-conditioners.webp
│   │   │   ├── heating.webp
│   │   │   └── ...
│   │   ├── illustrations/
│   │   │   ├── empty-cart.svg
│   │   │   ├── empty-wishlist.svg
│   │   │   ├── error-404.svg
│   │   │   └── ...
│   │   ├── fallbacks/
│   │   │   ├── no-product-image.svg
│   │   │   ├── no-category-image.svg
│   │   │   └── default-avatar.svg
│   │   ├── brands/
│   │   │   ├── daikin.svg
│   │   │   ├── daikin-mono.svg
│   │   │   └── ...
│   │   ├── badges/
│   │   │   ├── payments/
│   │   │   │   ├── visa.svg
│   │   │   │   ├── mastercard.svg
│   │   │   │   └── ...
│   │   │   ├── trust/
│   │   │   │   ├── ssl-secure.svg
│   │   │   │   ├── money-back.svg
│   │   │   │   └── ...
│   │   │   └── energy/
│   │   │       ├── rating-a-plus-plus-plus.svg
│   │   │       └── ...
│   │   └── patterns/
│   │       ├── grid.svg
│   │       ├── dots.svg
│   │       ├── noise.png
│   │       └── wave.svg
│   └── icons/
│       └── custom/
│           ├── split-ac.svg
│           ├── heat-pump.svg
│           └── ...
├── app/
│   └── shared/
│       └── components/
│           ├── image/
│           │   ├── image.component.ts
│           │   └── image.component.scss
│           ├── icon/
│           │   ├── icon.component.ts
│           │   └── icon.component.scss
│           ├── empty-state/
│           │   ├── empty-state.component.ts
│           │   └── empty-state.component.scss
│           ├── trust-badges/
│           │   └── trust-badges.component.ts
│           ├── payment-methods/
│           │   └── payment-methods.component.ts
│           ├── brand-logo/
│           │   └── brand-logo.component.ts
│           └── energy-rating/
│               └── energy-rating.component.ts
└── styles/
    ├── _colors.scss          # Gradient presets added
    └── _patterns.scss        # Background pattern mixins
```

---

## Notes

- All imagery should support reduced motion preferences (no animated GIFs)
- Consider CDN integration for production (Cloudflare Images, Cloudinary, etc.)
- Plan for future: AI-generated product backgrounds for consistency
- Brand logos require permission - verify licensing before use
- Consider seasonal A/B testing different hero images
