# Modern Web Motion & Scroll Patterns Research

## Overview

This document catalogs best-in-class motion design patterns for e-commerce and product websites, with a focus on performance, accessibility, and implementation approaches.

---

## 1. Scroll-Driven Animations

### 1.1 Scroll Progress Indicator

**Pattern Name:** Scroll Progress Bar

**Description:** A horizontal bar (typically at the top of the page) that fills as the user scrolls, indicating reading/viewing progress.

**When to Use:**
- Long-form content pages
- Product detail pages with extensive information
- Article/blog pages

**Performance:**
- CSS-only using `animation-timeline: scroll()`
- GPU-accelerated via `transform: scaleX()`
- Zero JavaScript overhead when using native CSS

**Implementation:**
```css
.progress-bar {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 4px;
  background: var(--color-primary);
  transform-origin: left;
  animation: grow-progress linear;
  animation-timeline: scroll();
}

@keyframes grow-progress {
  from { transform: scaleX(0); }
  to { transform: scaleX(1); }
}
```

**Accessibility:**
- Purely decorative; no impact on screen readers
- Works with `prefers-reduced-motion: reduce` by removing animation

---

### 1.2 View Progress Timeline

**Pattern Name:** Element Reveal on Scroll

**Description:** Elements animate based on their visibility within the scrollport. Animation progresses from 0% when element enters to 100% when it exits.

**When to Use:**
- Content sections that should draw attention as they appear
- Feature showcases
- Testimonial cards
- Product feature lists

**Performance:**
- Native CSS with `animation-timeline: view()`
- No JavaScript Intersection Observer needed
- Runs on compositor thread

**Implementation:**
```css
.reveal-element {
  animation: fade-in-up linear;
  animation-timeline: view();
  animation-range: entry 0% entry 100%;
}

@keyframes fade-in-up {
  from {
    opacity: 0;
    transform: translateY(50px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

**Accessibility:**
- Use `animation-range` to complete animation early (e.g., `entry 0% entry 50%`) so content is fully visible before center of viewport
- Provide `prefers-reduced-motion` alternative

---

### 1.3 Parallax Scrolling (Subtle)

**Pattern Name:** Subtle Parallax Depth

**Description:** Background or decorative elements move at different speeds than foreground content, creating subtle depth. Modern implementations avoid the "gimmicky" parallax of the 2010s.

**When to Use:**
- Hero sections with background imagery
- Decorative accent elements
- Product photography backgrounds

**Performance:**
- Use `transform: translateY()` or `translate3d()` only
- Never animate `background-position` (triggers paint)
- Keep parallax ratio subtle (0.1-0.3 difference)

**Implementation (React/Motion):**
```typescript
const { scrollYProgress } = useScroll();
const y = useTransform(scrollYProgress, [0, 1], [0, -100]);

return <motion.div style={{ y }} className="parallax-bg" />;
```

**Implementation (CSS-only):**
```css
.parallax-container {
  perspective: 1px;
  overflow-y: auto;
  height: 100vh;
}

.parallax-bg {
  transform: translateZ(-0.5px) scale(1.5);
}
```

**Accessibility:**
- Disable for `prefers-reduced-motion: reduce`
- Ensure content is readable without parallax effect

---

### 1.4 Sticky Section Transformations

**Pattern Name:** Sticky Transform Sequence

**Description:** A section sticks to the viewport while content within it transforms based on scroll position. Used for storytelling and feature reveals.

**When to Use:**
- Feature comparison sections
- Product transformation showcases
- Step-by-step guides
- Interactive timelines

**Performance:**
- Use CSS `position: sticky`
- Animate only `transform` and `opacity`
- Consider using `will-change: transform` sparingly

**Implementation:**
```css
.sticky-section {
  position: sticky;
  top: 0;
  height: 100vh;
}

.sticky-content {
  animation: transform-sequence linear;
  animation-timeline: view();
  animation-range: contain 0% contain 100%;
}
```

**Accessibility:**
- Ensure keyboard navigation works through sticky sections
- Provide skip links for lengthy interactive sections

---

## 2. Micro-Interactions

### 2.1 Button States

**Pattern Name:** Button Feedback Trio (Hover, Press, Focus)

**Description:** A cohesive set of visual feedback states for interactive buttons.

**States:**
1. **Hover:** Subtle lift + color shift
2. **Active/Press:** Slight press-down effect
3. **Focus:** Clear visible outline for keyboard users

**When to Use:**
- All interactive buttons
- Call-to-action elements
- Navigation links

**Performance:**
- Use `transform` for lift/press effects
- Use `box-shadow` transitions carefully (can trigger paint)
- Keep transition duration short (150-200ms)

**Implementation:**
```scss
.btn {
  transition: transform 150ms ease-out, 
              box-shadow 150ms ease-out,
              background-color 150ms ease-out;
  
  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  }
  
  &:active {
    transform: translateY(0);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  
  &:focus-visible {
    outline: 2px solid var(--color-focus);
    outline-offset: 2px;
  }
}
```

**Accessibility:**
- `:focus-visible` for keyboard-only focus styles
- Maintain 3:1 contrast ratio for focus indicators
- Don't rely solely on color changes

---

### 2.2 Card Hover Effects

**Pattern Name:** Product Card Lift

**Description:** Product cards that subtly lift and reveal additional information on hover.

**When to Use:**
- Product grids
- Category cards
- Feature cards
- Team member cards

**Performance:**
- Use `transform: translateY()` for lift
- Fade in overlay content with `opacity`
- Avoid animating `height` or `padding`

**Implementation:**
```scss
.product-card {
  transition: transform 250ms ease-out, box-shadow 250ms ease-out;
  
  .quick-actions {
    opacity: 0;
    transform: translateY(10px);
    transition: opacity 200ms ease-out, transform 200ms ease-out;
  }
  
  &:hover {
    transform: translateY(-8px);
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
    
    .quick-actions {
      opacity: 1;
      transform: translateY(0);
    }
  }
}
```

**Accessibility:**
- Ensure hover content is accessible via keyboard focus
- Quick actions should be tabbable
- Consider touch device alternatives

---

### 2.3 Loading States & Skeletons

**Pattern Name:** Skeleton Shimmer Loading

**Description:** Placeholder content with a subtle shimmer animation while real content loads.

**When to Use:**
- Initial page loads
- Lazy-loaded content
- Image placeholders
- Data fetching states

**Performance:**
- CSS-only animation using `background-position`
- Single gradient, animated position
- Low CPU overhead

**Implementation:**
```scss
.skeleton {
  background: linear-gradient(
    90deg,
    var(--skeleton-base) 0%,
    var(--skeleton-shine) 50%,
    var(--skeleton-base) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

@media (prefers-reduced-motion: reduce) {
  .skeleton {
    animation: none;
    background: var(--skeleton-base);
  }
}
```

**Accessibility:**
- Add `aria-busy="true"` to loading containers
- Provide screen reader announcements when loading completes
- Static fallback for reduced motion

---

### 2.4 Form Field Interactions

**Pattern Name:** Floating Label with Validation Feedback

**Description:** Labels that animate from placeholder position to above the field, with color-coded validation states.

**When to Use:**
- All form inputs
- Search fields
- Filter inputs

**Performance:**
- Use `transform: translateY()` for label movement
- Animate `transform` and `opacity` only
- Color transitions on border

**Implementation:**
```scss
.form-field {
  position: relative;
  
  .label {
    position: absolute;
    left: 12px;
    top: 50%;
    transform: translateY(-50%);
    transition: transform 200ms ease-out, font-size 200ms ease-out;
    pointer-events: none;
  }
  
  .input:focus ~ .label,
  .input:not(:placeholder-shown) ~ .label {
    transform: translateY(-150%);
    font-size: 0.75rem;
  }
  
  .input {
    transition: border-color 200ms ease-out, box-shadow 200ms ease-out;
    
    &:focus {
      border-color: var(--color-primary);
      box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
    }
    
    &:invalid:not(:placeholder-shown) {
      border-color: var(--color-error);
    }
    
    &:valid:not(:placeholder-shown) {
      border-color: var(--color-success);
    }
  }
}
```

**Accessibility:**
- Never remove labels; position them visually
- Error states must have text explanations, not just color
- `aria-invalid="true"` for invalid fields

---

### 2.5 Add-to-Cart Animation

**Pattern Name:** Cart Icon Pulse + Item Flight

**Description:** When adding to cart, the item visually "flies" to the cart icon, which pulses to confirm.

**When to Use:**
- E-commerce product pages
- Product cards with quick-add
- Any add-to-cart action

**Performance:**
- Clone element and animate clone (don't move original)
- Use `transform` for flight path
- Use `scale` and `opacity` for cart pulse

**Implementation Approach:**
1. Clone product image/thumbnail
2. Calculate start position (product) and end position (cart icon)
3. Animate clone using CSS or Web Animations API
4. Pulse cart icon with scale animation
5. Update cart count with number animation

```typescript
// Animation sequence
async function animateAddToCart(productEl: HTMLElement, cartEl: HTMLElement) {
  const clone = productEl.cloneNode(true) as HTMLElement;
  const productRect = productEl.getBoundingClientRect();
  const cartRect = cartEl.getBoundingClientRect();
  
  clone.style.cssText = `
    position: fixed;
    top: ${productRect.top}px;
    left: ${productRect.left}px;
    width: ${productRect.width}px;
    height: ${productRect.height}px;
    transition: all 500ms cubic-bezier(0.2, 1, 0.3, 1);
    z-index: 1000;
  `;
  
  document.body.appendChild(clone);
  
  // Trigger reflow
  clone.offsetHeight;
  
  // Animate to cart
  clone.style.cssText += `
    top: ${cartRect.top}px;
    left: ${cartRect.left}px;
    width: 20px;
    height: 20px;
    opacity: 0.5;
  `;
  
  // Cleanup and pulse cart
  setTimeout(() => {
    clone.remove();
    cartEl.animate([
      { transform: 'scale(1)' },
      { transform: 'scale(1.2)' },
      { transform: 'scale(1)' }
    ], { duration: 300, easing: 'ease-out' });
  }, 500);
}
```

**Accessibility:**
- Announce addition via `aria-live` region
- Animation is supplementary; success message is primary
- Disable flight animation for `prefers-reduced-motion`

---

### 2.6 Wishlist Heart Animation

**Pattern Name:** Heart Fill + Burst

**Description:** When adding to wishlist, the heart icon fills with color and small particles burst outward.

**When to Use:**
- Wishlist/favorite toggles
- Like buttons
- Save actions

**Performance:**
- SVG path animation for fill
- CSS pseudo-elements for particles
- Limit to 6-8 particles maximum

**Implementation:**
```scss
.wishlist-btn {
  position: relative;
  
  .heart-icon {
    transition: transform 200ms ease-out;
    fill: transparent;
    stroke: currentColor;
  }
  
  &.active .heart-icon {
    fill: var(--color-error);
    animation: heart-pop 400ms ease-out;
  }
  
  &.active::after {
    content: '';
    position: absolute;
    inset: -10px;
    background: radial-gradient(circle, var(--color-error) 0%, transparent 70%);
    animation: burst 400ms ease-out forwards;
  }
}

@keyframes heart-pop {
  0% { transform: scale(1); }
  50% { transform: scale(1.3); }
  100% { transform: scale(1); }
}

@keyframes burst {
  0% { transform: scale(0); opacity: 0.5; }
  100% { transform: scale(2); opacity: 0; }
}
```

**Accessibility:**
- Toggle state via `aria-pressed`
- Announce state change: "Added to wishlist" / "Removed from wishlist"
- Works without animation

---

## 3. Page Transitions

### 3.1 Section Flow Transitions

**Pattern Name:** Staggered Section Entrance

**Description:** Page sections animate in sequence as they enter the viewport, creating a flowing narrative.

**When to Use:**
- Landing pages
- About pages
- Feature showcases

**Performance:**
- Use Intersection Observer for triggering
- Stagger with CSS custom properties or JS delays
- Animate `opacity` and `transform` only

**Implementation:**
```css
.section {
  opacity: 0;
  transform: translateY(40px);
  transition: opacity 600ms ease-out, transform 600ms ease-out;
}

.section.in-view {
  opacity: 1;
  transform: translateY(0);
}

/* Stagger children */
.section.in-view > * {
  animation: fade-in-up 600ms ease-out forwards;
}

.section.in-view > *:nth-child(1) { animation-delay: 0ms; }
.section.in-view > *:nth-child(2) { animation-delay: 100ms; }
.section.in-view > *:nth-child(3) { animation-delay: 200ms; }
```

**Accessibility:**
- Content should be in DOM and accessible before animation
- Don't delay content visibility excessively

---

### 3.2 Hero to Content Transition

**Pattern Name:** Hero Scroll Reveal

**Description:** Hero section elements (text, image, CTA) transition or parallax as user scrolls, seamlessly connecting to content below.

**When to Use:**
- Homepage heroes
- Product landing pages
- Campaign pages

**Performance:**
- Use CSS scroll-driven animations
- Fade and translate hero elements
- Content section slides up as hero fades

**Implementation:**
```css
.hero {
  position: relative;
  height: 100vh;
}

.hero-content {
  animation: hero-exit linear;
  animation-timeline: scroll();
  animation-range: exit 0% exit 50%;
}

@keyframes hero-exit {
  to {
    opacity: 0;
    transform: translateY(-50px) scale(0.95);
  }
}

.content-section {
  animation: content-enter linear;
  animation-timeline: scroll();
  animation-range: entry 0% entry 50%;
}

@keyframes content-enter {
  from {
    opacity: 0;
    transform: translateY(50px);
  }
}
```

---

### 3.3 Navigation State Changes

**Pattern Name:** Sticky Header Transform

**Description:** Header transforms as user scrolls - shrinks, changes background, updates shadow.

**When to Use:**
- All pages with scrollable content
- E-commerce sites
- Content-heavy pages

**Performance:**
- Use `position: sticky`
- Animate via CSS scroll-driven animations or throttled scroll listener
- Animate `transform`, `opacity`, `background-color`, `box-shadow`

**Implementation:**
```scss
.header {
  position: sticky;
  top: 0;
  transition: transform 200ms ease-out,
              background-color 200ms ease-out,
              box-shadow 200ms ease-out;
  
  &.scrolled {
    background-color: var(--header-bg-solid);
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    
    .logo {
      transform: scale(0.8);
    }
    
    .nav-link {
      padding-block: 0.5rem;
    }
  }
}
```

**JavaScript (throttled):**
```typescript
let ticking = false;

window.addEventListener('scroll', () => {
  if (!ticking) {
    requestAnimationFrame(() => {
      header.classList.toggle('scrolled', window.scrollY > 100);
      ticking = false;
    });
    ticking = true;
  }
});
```

---

## 4. E-commerce Specific Patterns

### 4.1 Product Image Gallery

**Pattern Name:** Thumbnail Navigation with Smooth Transition

**Description:** Main image transitions smoothly when thumbnails are clicked, with optional zoom on hover.

**When to Use:**
- Product detail pages
- Image showcases
- Portfolio galleries

**Performance:**
- Preload adjacent images
- Use `opacity` and `transform` for transitions
- Lazy load non-visible thumbnails

**Implementation:**
```scss
.gallery-main {
  position: relative;
  overflow: hidden;
  
  .gallery-image {
    position: absolute;
    inset: 0;
    opacity: 0;
    transform: scale(1.05);
    transition: opacity 400ms ease-out, transform 400ms ease-out;
    
    &.active {
      opacity: 1;
      transform: scale(1);
    }
  }
}

.thumbnail {
  cursor: pointer;
  opacity: 0.6;
  transition: opacity 200ms ease-out, transform 200ms ease-out;
  
  &:hover, &.active {
    opacity: 1;
    transform: scale(1.05);
  }
  
  &.active {
    outline: 2px solid var(--color-primary);
  }
}
```

---

### 4.2 Carousel/Slider Interactions

**Pattern Name:** Snap Carousel with Momentum

**Description:** Horizontal carousel with CSS scroll-snap, smooth momentum scrolling, and navigation controls.

**When to Use:**
- Product recommendations
- Related products
- Image galleries
- Category browsing

**Performance:**
- Native CSS scroll-snap (no JS for snapping)
- Use `scroll-behavior: smooth` for navigation
- Lazy load off-screen slides

**Implementation:**
```scss
.carousel {
  display: flex;
  gap: 1rem;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
  scroll-behavior: smooth;
  scrollbar-width: none;
  
  &::-webkit-scrollbar {
    display: none;
  }
}

.carousel-item {
  flex: 0 0 auto;
  scroll-snap-align: start;
  width: clamp(280px, 30vw, 350px);
}
```

**Accessibility:**
- Provide previous/next buttons
- Use `aria-roledescription="carousel"`
- Announce current slide position
- Support keyboard navigation

---

### 4.3 Price & Stock Indicators

**Pattern Name:** Dynamic Price Update

**Description:** Prices and stock levels animate when they change (variant selection, quantity updates).

**When to Use:**
- Product variant selection
- Quantity adjustments
- Sale price reveals
- Stock level updates

**Performance:**
- Simple fade/slide transition
- Avoid layout thrashing
- Use fixed-width containers when possible

**Implementation:**
```scss
.price {
  display: inline-block;
  transition: transform 200ms ease-out, opacity 200ms ease-out;
  
  &.updating {
    transform: translateY(-5px);
    opacity: 0;
  }
}

.stock-indicator {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  
  .stock-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    transition: background-color 300ms ease-out;
    
    &.in-stock { background-color: var(--color-success); }
    &.low-stock { 
      background-color: var(--color-warning);
      animation: pulse 1s infinite;
    }
    &.out-of-stock { background-color: var(--color-error); }
  }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
```

---

### 4.4 Cart Icon Updates

**Pattern Name:** Cart Badge Bounce

**Description:** Cart icon badge animates when item count changes, drawing attention to the update.

**When to Use:**
- After add-to-cart
- After remove from cart
- Quantity updates

**Performance:**
- CSS keyframe animation
- Triggered via class toggle
- Short duration (300-400ms)

**Implementation:**
```scss
.cart-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--color-primary);
  color: white;
  font-size: 0.75rem;
  transition: transform 300ms cubic-bezier(0.175, 0.885, 0.32, 1.275);
  
  &.updated {
    animation: badge-bounce 400ms ease-out;
  }
}

@keyframes badge-bounce {
  0% { transform: scale(1); }
  30% { transform: scale(1.4); }
  50% { transform: scale(0.9); }
  70% { transform: scale(1.1); }
  100% { transform: scale(1); }
}
```

---

## 5. Performance Best Practices

### 5.1 GPU-Accelerated Properties

**The Safe List:**
- `transform` (translate, scale, rotate, skew)
- `opacity`
- `filter` (with caveats)
- `clip-path` (on modern browsers)

**Avoid Animating:**
- `width`, `height`, `top`, `left`, `right`, `bottom`
- `margin`, `padding`
- `border-width`, `border-radius` (on large elements)
- `font-size`
- `background-position` (use `transform` instead)
- `box-shadow` (use `opacity` on pseudo-element)

### 5.2 The Rendering Pipeline

1. **Style** - Calculate computed styles
2. **Layout** - Calculate geometry and positions
3. **Paint** - Fill in pixels
4. **Composite** - Combine layers

**Goal:** Skip to Composite step by only animating `transform` and `opacity`

### 5.3 will-change Usage

```css
/* Good: Apply before animation, remove after */
.element:hover {
  will-change: transform;
}

/* Bad: Applied to many elements permanently */
* {
  will-change: transform, opacity;
}
```

**Rules:**
- Apply via JS before animation starts
- Remove after animation completes
- Never apply to more than a few elements
- Each `will-change` creates a new composite layer (memory cost)

### 5.4 Intersection Observer Pattern

```typescript
const observer = new IntersectionObserver(
  (entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add('animate');
        observer.unobserve(entry.target); // One-time animation
      }
    });
  },
  {
    threshold: 0.1,
    rootMargin: '50px', // Start slightly before viewport
  }
);

document.querySelectorAll('.animate-on-scroll').forEach((el) => {
  observer.observe(el);
});
```

### 5.5 requestAnimationFrame Usage

```typescript
// Throttle scroll handlers
let ticking = false;

function onScroll() {
  if (!ticking) {
    requestAnimationFrame(() => {
      // Do animation work here
      updateScrollProgress();
      ticking = false;
    });
    ticking = true;
  }
}

window.addEventListener('scroll', onScroll, { passive: true });
```

---

## 6. Accessibility Considerations

### 6.1 prefers-reduced-motion

**Always provide reduced motion alternatives:**

```scss
/* Base animation */
.animated-element {
  transition: transform 500ms ease-out, opacity 500ms ease-out;
}

.animated-element.animate {
  transform: translateY(0);
  opacity: 1;
}

/* Reduced motion: instant transitions */
@media (prefers-reduced-motion: reduce) {
  .animated-element {
    transition-duration: 0.01ms !important;
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
  }
}
```

**In JavaScript:**
```typescript
const prefersReducedMotion = window.matchMedia(
  '(prefers-reduced-motion: reduce)'
).matches;

if (prefersReducedMotion) {
  // Skip animations, show content immediately
  element.classList.add('visible');
} else {
  // Run normal animation
  animateElement(element);
}
```

### 6.2 Focus Management

- Never hide focused elements during animation
- Animate focus indicators for smooth transitions
- Use `:focus-visible` for keyboard-only focus styles

```scss
.button:focus-visible {
  outline: 2px solid var(--color-focus);
  outline-offset: 2px;
  transition: outline-offset 150ms ease-out;
}

.button:focus-visible:active {
  outline-offset: 0;
}
```

### 6.3 Screen Reader Announcements

```html
<!-- Live region for animation-related updates -->
<div aria-live="polite" aria-atomic="true" class="sr-only" id="animation-announcer">
</div>
```

```typescript
function announceToScreenReader(message: string) {
  const announcer = document.getElementById('animation-announcer');
  announcer.textContent = message;
  
  // Reset for next announcement
  setTimeout(() => {
    announcer.textContent = '';
  }, 1000);
}

// Usage
addToCart(product).then(() => {
  announceToScreenReader(`${product.name} added to cart`);
});
```

### 6.4 Motion That Aids Understanding

**Good motion:**
- Indicates connection between elements (item flying to cart)
- Shows cause and effect (button press feedback)
- Maintains spatial orientation (page transitions)
- Provides loading feedback (progress indicators)

**Bad motion:**
- Decorative loops with no purpose
- Excessive parallax that obscures content
- Autoplay carousels without controls
- Animations that delay access to content

---

## 7. Implementation Approach Summary

| Pattern Category | CSS-Only | JS Required | Library Recommended |
|-----------------|----------|-------------|---------------------|
| Scroll Progress | Yes (animation-timeline) | Fallback for Safari | - |
| View Animations | Yes (animation-timeline) | Intersection Observer fallback | - |
| Parallax | Partial | For complex effects | Motion/GSAP |
| Button States | Yes | - | - |
| Card Hover | Yes | - | - |
| Skeletons | Yes | - | - |
| Form Fields | Yes | For validation | - |
| Add to Cart Flight | No | Yes | Motion/GSAP |
| Carousels | Mostly | Navigation controls | - |
| Page Transitions | Partial | For complex sequences | Motion |

---

## 8. Browser Support Notes

### CSS Scroll-Driven Animations
- Chrome 115+, Edge 115+: Full support
- Firefox 110+: Behind flag (`layout.css.scroll-driven-animations.enabled`)
- Safari: Not yet supported (use Intersection Observer fallback)

### Web Animations API
- All modern browsers: Full support
- Use for JavaScript-driven animations

### Intersection Observer
- All modern browsers: Full support
- Best for scroll-triggered animations with wide compatibility

---

## 9. Quick Reference: Animation Timing

| Use Case | Duration | Easing |
|----------|----------|--------|
| Button hover | 150-200ms | ease-out |
| Card hover | 200-300ms | ease-out |
| Fade in/out | 200-400ms | ease-out |
| Slide/translate | 300-500ms | ease-out or cubic-bezier |
| Complex sequences | 500-800ms | custom cubic-bezier |
| Page transitions | 300-500ms | ease-in-out |
| Loading shimmer | 1500-2000ms | linear (infinite) |
| Micro-feedback | 100-200ms | ease-out |

**Common Easing Functions:**
```css
/* Smooth deceleration */
--ease-out: cubic-bezier(0, 0, 0.2, 1);

/* Smooth acceleration */
--ease-in: cubic-bezier(0.4, 0, 1, 1);

/* Smooth both */
--ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);

/* Bouncy/springy */
--ease-bounce: cubic-bezier(0.175, 0.885, 0.32, 1.275);

/* Snappy */
--ease-snappy: cubic-bezier(0.7, 0, 0.3, 1);
```

---

## 10. Resources

- [MDN: animation-timeline](https://developer.mozilla.org/en-US/docs/Web/CSS/animation-timeline)
- [web.dev: High-performance CSS animations](https://web.dev/articles/animations-guide)
- [Motion (Framer Motion) docs](https://motion.dev/docs)
- [Intersection Observer API](https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API)
- [prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion)
