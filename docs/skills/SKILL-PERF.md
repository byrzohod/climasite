# Skill: Performance Auditor (SKILL-PERF)

## Purpose

The Performance Auditor identifies slowness, inefficiencies, and bottlenecks that degrade user experience. Performance IS user experience - a slow site feels broken.

## Performance Metrics to Measure

### Core Web Vitals
| Metric | Target | Description |
|--------|--------|-------------|
| LCP (Largest Contentful Paint) | < 2.5s | Time until largest content element visible |
| FID (First Input Delay) | < 100ms | Time from interaction to response |
| CLS (Cumulative Layout Shift) | < 0.1 | Visual stability (content doesn't jump) |

### Additional Metrics
| Metric | Target | Description |
|--------|--------|-------------|
| TTFB (Time to First Byte) | < 600ms | Server response time |
| FCP (First Contentful Paint) | < 1.8s | Time until first content visible |
| TTI (Time to Interactive) | < 3.0s | Time until fully interactive |
| Total Blocking Time | < 200ms | Sum of blocking tasks |
| Speed Index | < 3.0s | How quickly content is visually populated |

## What You Look For

### 1. Initial Load Performance
- [ ] Bundle size is reasonable (< 200KB gzipped initial)
- [ ] Critical CSS is inlined or prioritized
- [ ] JavaScript doesn't block rendering
- [ ] Fonts are loaded efficiently (preload, font-display)
- [ ] Above-the-fold content loads first
- [ ] No render-blocking resources

### 2. Runtime Performance
- [ ] Scrolling is smooth (60fps)
- [ ] Animations don't cause jank
- [ ] No long tasks blocking main thread
- [ ] Memory usage is stable (no leaks)
- [ ] Event handlers are debounced/throttled where needed

### 3. Network Efficiency
- [ ] No unnecessary API calls
- [ ] No duplicate requests
- [ ] Data is cached appropriately
- [ ] Pagination for large lists
- [ ] Images are optimized and lazy-loaded
- [ ] API responses are reasonably sized

### 4. Image Optimization
- [ ] Images are WebP/AVIF where supported
- [ ] Images have appropriate dimensions (not oversized)
- [ ] Responsive images with srcset
- [ ] Images are lazy-loaded below fold
- [ ] Placeholder/blur-up for images
- [ ] No broken image requests

### 5. JavaScript Optimization
- [ ] Tree-shaking is working
- [ ] Code splitting (lazy-loaded routes)
- [ ] No unused dependencies
- [ ] No console.log in production
- [ ] Efficient DOM manipulation
- [ ] Event listeners cleaned up

### 6. CSS Optimization
- [ ] No unused CSS
- [ ] CSS is minified
- [ ] No expensive selectors
- [ ] Animations use transform/opacity (GPU-accelerated)
- [ ] will-change used sparingly

### 7. API Performance
- [ ] Endpoints respond quickly (< 200ms typical)
- [ ] No N+1 query problems
- [ ] Appropriate indexes on database
- [ ] Response payload is minimal
- [ ] Compression is enabled (gzip/brotli)

### 8. Caching
- [ ] Static assets have cache headers
- [ ] API responses cached where appropriate
- [ ] Service worker caching (if applicable)
- [ ] Browser caching utilized
- [ ] CDN for static assets (if applicable)

## Testing Methodology

### 1. Lighthouse Audit
```bash
# Run in Chrome DevTools > Lighthouse
# Or use Lighthouse CLI:
lighthouse http://localhost:4200 --view
```

Check:
- Performance score (target: > 90)
- Opportunities (specific improvements)
- Diagnostics (deeper issues)

### 2. Network Analysis
In Chrome DevTools > Network:
- [ ] Check total transfer size
- [ ] Check number of requests
- [ ] Look for waterfall inefficiencies
- [ ] Check for slow requests (> 500ms)
- [ ] Look for failed/retry requests

### 3. Performance Timeline
In Chrome DevTools > Performance:
- [ ] Record page load
- [ ] Look for long tasks (> 50ms)
- [ ] Check for layout thrashing
- [ ] Identify scripting bottlenecks

### 4. Memory Profiling
In Chrome DevTools > Memory:
- [ ] Check heap size over time
- [ ] Look for memory leaks (increasing memory)
- [ ] Check detached DOM nodes

### 5. Bundle Analysis
```bash
# In Angular project
ng build --stats-json
npx webpack-bundle-analyzer dist/clima-site.web/stats.json
```

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | Site unusably slow | Page takes > 10s to load |
| HIGH | Noticeably slow, impacts conversion | LCP > 4s, janky scrolling |
| MEDIUM | Could be faster | LCP 2.5-4s, some animation jank |
| LOW | Minor optimization | Small bundle reduction possible |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: Performance
- **Metric Impacted**: [LCP/FID/CLS/Bundle Size/etc.]
- **Location**: [Page/Component/Endpoint]
- **Current Value**: [Measured value]
- **Target Value**: [What it should be]
- **Description**: [What's slow and why]
- **Evidence**: [Screenshot/measurement]
- **Suggested Fix**: [How to improve]
- **Estimated Impact**: [How much improvement expected]
- **Status**: Open
```

## Common Issues to Watch For

1. **Large bundle size** - Initial JS > 300KB
2. **Unoptimized images** - 2MB product images
3. **Too many API calls** - 20 requests on page load
4. **No lazy loading** - All routes loaded upfront
5. **Memory leaks** - Subscriptions not unsubscribed
6. **Layout shifts** - Content jumps as images load
7. **Slow API** - Endpoint takes > 1s
8. **Blocking scripts** - JS in `<head>` without async/defer
9. **No caching** - Same data fetched on every page view
10. **Animation jank** - Animating width/height instead of transform
11. **Unused CSS** - Large style files with dead code
12. **Font flash** - FOUT (Flash of Unstyled Text)

## Performance Budget

| Resource | Budget |
|----------|--------|
| HTML | < 50KB |
| CSS | < 100KB |
| JS (initial) | < 200KB |
| JS (total) | < 500KB |
| Images (above fold) | < 200KB |
| Fonts | < 100KB |
| Total page weight | < 1.5MB |
| Requests | < 50 |
