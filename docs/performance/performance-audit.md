# ClimaSite Performance Audit & Core Web Vitals Optimization

> Generated: 2026-01-24
> Last Updated: 2026-01-24

## Executive Summary

This document provides a comprehensive performance audit of the ClimaSite Angular e-commerce application, with specific focus on Core Web Vitals optimization. The audit covers current state analysis, identified issues, implemented optimizations, and recommendations for future improvements.

---

## 1. Core Web Vitals Targets

| Metric | Target | Description | Current Status |
|--------|--------|-------------|----------------|
| **LCP** (Largest Contentful Paint) | < 2.5s | Time to render the largest visible content | Needs measurement |
| **FID** (First Input Delay) | < 100ms | Time from first interaction to browser response | Needs measurement |
| **CLS** (Cumulative Layout Shift) | < 0.1 | Visual stability during page load | Needs measurement |
| **INP** (Interaction to Next Paint) | < 200ms | Responsiveness to all interactions | Needs measurement |
| **TTFB** (Time to First Byte) | < 800ms | Server response time | Backend dependent |
| **FCP** (First Contentful Paint) | < 1.8s | Time to first visible content | Needs measurement |

### Recommended Measurement Tools
- Chrome DevTools Lighthouse (local testing)
- PageSpeed Insights (field + lab data)
- Web Vitals Chrome Extension (real-time monitoring)
- `web-vitals` npm package (production RUM)

---

## 2. Current State Analysis

### 2.1 Build Configuration (angular.json)

**Bundle Budgets (Configured):**
```json
{
  "type": "initial",
  "maximumWarning": "500kB",
  "maximumError": "1MB"
},
{
  "type": "anyComponentStyle",
  "maximumWarning": "8kB",
  "maximumError": "16kB"
}
```

**Assessment:** Bundle budgets are properly configured and reasonable for an e-commerce SPA.

### 2.2 Lazy Loading Implementation

#### Route-Level Lazy Loading (Excellent)

All routes in `app.routes.ts` use lazy loading via `loadComponent` or `loadChildren`:

| Route | Implementation | Status |
|-------|----------------|--------|
| `/` (Home) | `loadComponent` | Lazy loaded |
| `/products` | `loadComponent` | Lazy loaded |
| `/products/:slug` | `loadComponent` | Lazy loaded |
| `/cart` | `loadComponent` | Lazy loaded |
| `/checkout` | `loadComponent` | Lazy loaded |
| `/account/*` | `loadChildren` | Lazy loaded (route group) |
| `/admin/*` | `loadChildren` | Lazy loaded (route group) |
| `/promotions/*` | `loadChildren` | Lazy loaded |
| `/brands/*` | `loadChildren` | Lazy loaded |

**Assessment:** Excellent route-level code splitting. Each feature loads on demand.

#### Image Lazy Loading (Partial)

| Component | File | Has `loading="lazy"` | Assessment |
|-----------|------|---------------------|------------|
| ProductCardComponent | `product-card.component.ts:28` | Yes | Correct |
| ProductGalleryComponent | `product-gallery.component.ts:80` | Thumbnails only | Main image should be eager |
| SimilarProductsComponent | `similar-products.component.ts:41` | Yes | Correct |
| ProductConsumablesComponent | `product-consumables.component.ts:41` | Yes | Correct |
| BrandDetailComponent | `brand-detail.component.ts:84` | Yes | Correct |
| PromotionDetailComponent | `promotion-detail.component.ts:80` | Yes | Correct |
| ContactComponent | `contact.component.ts:153` | Yes (iframe) | Correct |
| CartComponent | `cart.component.ts:52` | **No** | Needs fix |
| BrandsListComponent | `brands-list.component.ts:42` | **No** | Needs fix |
| RecentlyViewedComponent | `recently-viewed.component.ts:34` | **No** | Needs fix |
| FrequentlyBoughtComponent | `frequently-bought.component.ts:29,62` | **No** | Needs fix |
| OrdersComponent | `orders.component.ts:182` | **No** | Needs fix |
| OrderDetailsComponent | `order-details.component.ts:126` | **No** | Needs fix |
| RelatedProductsManager | `related-products-manager.component.ts:61,125` | **No** | Needs fix (admin) |

### 2.3 LCP Optimization

#### Hero Images (Critical)

The home page hero section does NOT use a traditional `<img>` tag but CSS gradient backgrounds for a modern design. This is intentional for design purposes.

**Category Images:** Use Intersection Observer for lazy loading background images (HOME-P02 pattern). This is a good optimization.

**Missing fetchpriority:** No images in the codebase use `fetchpriority="high"` for LCP optimization.

### 2.4 CLS (Layout Shift) Analysis

| Issue | Component | Impact |
|-------|-----------|--------|
| Images without dimensions | Multiple components | Medium |
| Dynamic content loading | Product cards, lists | Low (skeletons help) |
| Font loading | Google Fonts | Medium |
| Lazy loaded content | Category panels | Low (handled well) |

**Skeleton screens are implemented**, which significantly reduces perceived layout shift.

### 2.5 JavaScript Performance

#### Scroll Handlers (Well Optimized)
- HeaderComponent: Uses RAF throttling (`header.component.ts:1211-1223`)
- AnimationService: Uses RAF throttling + passive listeners

#### Change Detection
- **No components use OnPush change detection strategy**
- All components use default change detection
- This is a significant optimization opportunity

### 2.6 Font Loading

**index.html** loads Google Fonts:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600;700&family=Space+Grotesk:wght@400;500;600;700&display=swap" rel="stylesheet">
```

**Assessment:**
- Preconnect hints are correctly implemented
- `display=swap` is used (good for CLS)
- Multiple font weights may increase load time

### 2.7 External Resource Handling

| Resource | Preconnect | Status |
|----------|------------|--------|
| Google Fonts | Yes | Configured |
| Stripe | Yes | Added in audit |
| OpenStreetMap | No | Loaded lazily in Contact |
| Unsplash Images | Yes | Added in audit |

### 2.8 Memory Management

**Properly Cleaned Up:**
- HomeComponent: Subscription cleanup, Observer disconnect
- AnimationService: Has destroy() method
- Animation directives: Disconnect observers in ngOnDestroy

**Potential Issues:**
- Some components may not unsubscribe from observables
- Long-running intervals should be reviewed

---

## 3. Identified Issues & Severity

### Critical (Impact on Core Web Vitals)

| Issue | Metric Affected | Priority | Status |
|-------|-----------------|----------|--------|
| Missing `loading="lazy"` on 8+ image locations | LCP, Performance | High | FIXED |
| No `fetchpriority="high"` on any LCP images | LCP | High | FIXED |
| No Stripe preconnect hint | TTFB for checkout | High | FIXED |
| No OnPush change detection | FID, INP | Medium-High | Pending |

### High Priority

| Issue | Impact | Status |
|-------|--------|--------|
| Images missing explicit width/height | CLS | Pending |
| No image blur-up/placeholder strategy | Perceived performance | Pending |
| Main gallery image should be eager loaded | LCP | FIXED |
| No route preloading strategy | Navigation speed | Pending |

### Medium Priority

| Issue | Impact |
|-------|--------|
| Multiple Google Font weights | Initial load |
| No service worker / offline support | Repeat visits |
| No virtual scrolling for long lists | Memory, scroll performance |
| Carousel scroll handlers not throttled | Scroll performance |

### Low Priority

| Issue | Impact |
|-------|--------|
| No Core Web Vitals monitoring in production | Visibility |
| Inconsistent skeleton usage | UX consistency |
| No image CDN / optimization | Bandwidth |

---

## 4. Implemented Optimizations

### 4.1 Quick Wins Implemented

#### Images with `loading="lazy"` Added

| Component | File | Line | Change |
|-----------|------|------|--------|
| CartComponent | `cart.component.ts` | 52 | Added `loading="lazy"` |
| BrandsListComponent | `brands-list.component.ts` | 42 | Added `loading="lazy"` |
| RecentlyViewedComponent | `recently-viewed.component.ts` | 34 | Added `loading="lazy"` |
| FrequentlyBoughtComponent | `frequently-bought.component.ts` | 29, 62 | Added `loading="lazy"` |
| OrdersComponent | `orders.component.ts` | 182 | Added `loading="lazy"` |
| OrderDetailsComponent | `order-details.component.ts` | 126 | Added `loading="lazy"` |
| RelatedProductsManager | `related-products-manager.component.ts` | 61, 125 | Added `loading="lazy"` |
| PromotionsListComponent | `promotions-list.component.ts` | 32 | Added `loading="lazy"` |

#### Preconnect Hints Added

| Resource | File | Change |
|----------|------|--------|
| Stripe JS | `index.html` | Added `<link rel="preconnect" href="https://js.stripe.com">` |
| Unsplash Images | `index.html` | Added `<link rel="preconnect" href="https://images.unsplash.com">` |

#### fetchpriority Added to LCP Images

| Component | File | Change |
|-----------|------|--------|
| ProductGalleryComponent | `product-gallery.component.ts` | Main image: `loading="eager" fetchpriority="high"` |
| BrandDetailComponent | `brand-detail.component.ts` | Hero logo: `loading="eager" fetchpriority="high"` |

### 4.2 Already Implemented (Pre-Audit)

- Route-level lazy loading (all routes)
- Image lazy loading (7 components)
- Intersection Observer for category backgrounds
- RAF throttled scroll handlers
- Passive event listeners
- Reduced motion support
- Skeleton loading screens
- Bundle size budgets

---

## 5. Recommendations Checklist

### Immediate Actions (Do Now)

- [x] Add `loading="lazy"` to all below-fold images
- [x] Add `fetchpriority="high"` to main product gallery image
- [x] Add preconnect hints for Stripe and Unsplash
- [ ] Measure baseline Core Web Vitals with Lighthouse

### Short-Term (Next Sprint)

- [ ] Implement OnPush change detection on stateless components
- [ ] Add explicit width/height to all images to prevent CLS
- [ ] Implement route preloading strategy for common paths
- [ ] Add `web-vitals` package for production RUM

### Medium-Term (Next Quarter)

- [ ] Implement image blur-up placeholder strategy
- [ ] Add service worker for offline support and caching
- [ ] Implement virtual scrolling for product lists > 50 items
- [ ] Optimize Google Fonts (subset, fewer weights)
- [ ] Add image CDN with automatic optimization

### Long-Term (Roadmap)

- [ ] Implement Lighthouse CI in deployment pipeline
- [ ] Add performance budgets to CI/CD
- [ ] Implement edge caching strategy
- [ ] Consider Server-Side Rendering (Angular Universal) for SEO pages

---

## 6. Browser Support & Considerations

### Supported Browsers

| Browser | Version | Notes |
|---------|---------|-------|
| Chrome | 90+ | Full support for all features |
| Firefox | 88+ | Full support |
| Safari | 14+ | Full support |
| Edge | 90+ | Full support |
| Safari iOS | 14+ | Full support |
| Chrome Android | 90+ | Full support |

### Feature Compatibility

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| `loading="lazy"` | 77+ | 75+ | 15.4+ | 79+ |
| `fetchpriority` | 101+ | No | No | 101+ |
| Intersection Observer | 51+ | 55+ | 12.1+ | 15+ |
| CSS `aspect-ratio` | 88+ | 89+ | 15+ | 88+ |
| `prefers-reduced-motion` | 74+ | 63+ | 10.1+ | 79+ |

### Fallbacks Needed

| Feature | Fallback Strategy |
|---------|-------------------|
| `fetchpriority` | Gracefully ignored by unsupported browsers |
| `loading="lazy"` | Images load immediately (worse perf, still works) |
| Intersection Observer | Polyfill available if needed (not currently used) |
| CSS aspect-ratio | Use padding-bottom hack for older browsers |

### Cross-Browser Testing Checklist

- [ ] Test on Safari (often has different rendering)
- [ ] Test on Firefox (check font rendering)
- [ ] Test on mobile Safari (iOS-specific issues)
- [ ] Test with reduced motion enabled
- [ ] Test with slow network (DevTools throttling)
- [ ] Test with blocked JavaScript (graceful degradation)

---

## 7. Performance Testing Commands

### Build Analysis

```bash
# Production build with stats
cd src/ClimaSite.Web
ng build --configuration=production --stats-json

# Analyze bundle
npx webpack-bundle-analyzer dist/clima-site.web/browser/stats.json
```

### Lighthouse CLI

```bash
# Install Lighthouse
npm install -g lighthouse

# Run audit
lighthouse http://localhost:4200 --output=html --output-path=./lighthouse-report.html

# Run with specific categories
lighthouse http://localhost:4200 --only-categories=performance,accessibility
```

### Web Vitals Monitoring

```typescript
// Add to main.ts for production monitoring
import { onCLS, onFID, onLCP, onINP, onTTFB } from 'web-vitals';

function sendToAnalytics(metric: any) {
  console.log(metric); // Replace with analytics call
}

onCLS(sendToAnalytics);
onFID(sendToAnalytics);
onLCP(sendToAnalytics);
onINP(sendToAnalytics);
onTTFB(sendToAnalytics);
```

---

## 8. Metrics Baseline

> To be filled after running Lighthouse audit

### Home Page (`/`)

| Metric | Score | Value | Target |
|--------|-------|-------|--------|
| Performance | - | - | > 90 |
| LCP | - | - | < 2.5s |
| FID | - | - | < 100ms |
| CLS | - | - | < 0.1 |
| TTFB | - | - | < 800ms |

### Product List (`/products`)

| Metric | Score | Value | Target |
|--------|-------|-------|--------|
| Performance | - | - | > 90 |
| LCP | - | - | < 2.5s |
| FID | - | - | < 100ms |
| CLS | - | - | < 0.1 |

### Product Detail (`/products/:slug`)

| Metric | Score | Value | Target |
|--------|-------|-------|--------|
| Performance | - | - | > 90 |
| LCP | - | - | < 2.5s |
| FID | - | - | < 100ms |
| CLS | - | - | < 0.1 |

---

## 9. Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-24 | Initial audit document created | Claude |
| 2026-01-24 | Added lazy loading to 7 components | Claude |
| 2026-01-24 | Added preconnect hints for Stripe and Unsplash | Claude |
| 2026-01-24 | Added fetchpriority to gallery main image | Claude |

---

## Appendix A: Component Performance Checklist

When creating new components, ensure:

- [ ] Images below fold have `loading="lazy"`
- [ ] LCP images have `loading="eager" fetchpriority="high"`
- [ ] Images have explicit width/height or aspect-ratio
- [ ] Skeleton screens for async data
- [ ] OnPush change detection if possible
- [ ] Subscriptions cleaned up in ngOnDestroy
- [ ] Scroll handlers use RAF throttling
- [ ] Respects `prefers-reduced-motion`
- [ ] Uses CSS variables for theming
- [ ] Has `data-testid` for E2E tests

## Appendix B: Image Optimization Guidelines

### Image Sizing

| Context | Max Width | Format | Quality |
|---------|-----------|--------|---------|
| Product thumbnail | 300px | WebP/JPEG | 80% |
| Product main | 800px | WebP/JPEG | 85% |
| Hero/banner | 1920px | WebP/JPEG | 80% |
| Logo/icon | 200px | SVG/PNG | 100% |

### Responsive Images (Future)

```html
<img
  srcset="image-300.webp 300w,
          image-600.webp 600w,
          image-1200.webp 1200w"
  sizes="(max-width: 600px) 300px,
         (max-width: 1200px) 600px,
         1200px"
  src="image-600.webp"
  alt="Description"
  loading="lazy"
/>
```

## Appendix C: References

- [Web Vitals](https://web.dev/vitals/)
- [Angular Performance Checklist](https://angular.io/guide/performance-optimization)
- [Chrome Lighthouse](https://developer.chrome.com/docs/lighthouse/)
- [fetchpriority MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/img#fetchpriority)
- [Lazy Loading Images](https://web.dev/lazy-loading-images/)
