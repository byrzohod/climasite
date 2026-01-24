# UI/UX Skills Playbook

> A practical, actionable guide for creating exceptional user experiences. Each skill solves a real problem with specific rules, implementation patterns, and common mistakes to avoid.

---

## Table of Contents

**Part 1: UI/UX Skills**
- [1.1 Visual Hierarchy Skills](#11-visual-hierarchy-skills)
- [1.2 Layout & Composition Skills](#12-layout--composition-skills)
- [1.3 Typography & Readability Skills](#13-typography--readability-skills)
- [1.4 Form UX Skills](#14-form-ux-skills)
- [1.5 Navigation & Wayfinding Skills](#15-navigation--wayfinding-skills)
- [1.6 Feedback & States Skills](#16-feedback--states-skills)
- [1.7 Accessibility Skills](#17-accessibility-skills)
- [1.8 Consistency & Systems Skills](#18-consistency--systems-skills)

**Part 2: Motion & Scroll Skills**
- [2.1 Scroll-Driven Reveal Skills](#21-scroll-driven-reveal-skills)
- [2.2 Cinematic Section Transition Skills](#22-cinematic-section-transition-skills)
- [2.3 Parallax Layering Skills](#23-parallax-layering-skills)
- [2.4 Motion-as-Feedback Skills](#24-motion-as-feedback-skills)
- [2.5 Sticky-Section Storytelling Skills](#25-sticky-section-storytelling-skills)
- [2.6 Progressive Disclosure via Motion Skills](#26-progressive-disclosure-via-motion-skills)
- [2.7 Motion Pacing & Easing Skills](#27-motion-pacing--easing-skills)
- [2.8 Reduced Motion Strategy Skills](#28-reduced-motion-strategy-skills)
- [2.9 Performance-Safe Animation Skills](#29-performance-safe-animation-skills)

**Part 3: E-commerce Specific Skills**
- [3.1 Product Discovery Skills](#31-product-discovery-skills)
- [3.2 Cart & Checkout Skills](#32-cart--checkout-skills)
- [3.3 Trust & Conversion Skills](#33-trust--conversion-skills)

---

# Part 1: UI/UX Skills

## 1.1 Visual Hierarchy Skills

### Skill: Size-Based Priority Anchoring

**Problem:** Users don't know where to look first; all elements compete for attention equally.

**When to Use:** Any page with multiple content types (headings, body, CTAs, images).

**Rules:**
1. Establish a clear modular type scale (recommend 1.25 ratio for e-commerce)
2. Limit to 5-6 distinct sizes maximum
3. Largest element = most important message
4. Minimum 16px for body text; 14px absolute minimum for secondary
5. Headlines should be 2-3x body size for clear hierarchy

**Size Scale (1.25 Major Third):**
```scss
$font-size-xs: 0.75rem;   // 12px - Labels, captions
$font-size-sm: 0.875rem;  // 14px - Secondary text
$font-size-base: 1rem;    // 16px - Body text
$font-size-lg: 1.25rem;   // 20px - Lead paragraphs
$font-size-xl: 1.5rem;    // 24px - H3
$font-size-2xl: 2rem;     // 32px - H2
$font-size-3xl: 2.5rem;   // 40px - H1
$font-size-4xl: 3rem;     // 48px - Display/Hero
```

**Accessibility:** Test on mobile (16px min body); ensure 1.5+ line-height for body text.

**Common Mistakes:**
- Using too many different sizes (creates visual chaos)
- Making everything large (nothing stands out)
- Picking arbitrary sizes instead of a scale

---

### Skill: Color Contrast Emphasis

**Problem:** Users miss important elements because color doesn't create clear priority.

**When to Use:** CTAs, alerts, status indicators, interactive elements.

**Rules:**
1. Follow the 60-30-10 rule: 60% dominant background, 30% secondary elements, 10% accent/CTAs
2. Primary CTAs get brand color at full saturation
3. Secondary actions get muted brand color or neutral
4. Body text: high contrast neutral (#1A1A1A on white)
5. Secondary text: medium contrast (#666666 on white)
6. Disabled: low contrast (#999999 on white)

**Contrast Ratios (WCAG):**
| Element | Minimum | Recommended |
|---------|---------|-------------|
| Body text | 4.5:1 | 7:1+ |
| Large text (18px+) | 3:1 | 4.5:1+ |
| UI components | 3:1 | 4.5:1+ |

**Performance:** No impact - CSS only.

**Accessibility:** Never use color as the only indicator; pair with icons/text.

**Common Mistakes:**
- Changing color meanings between pages
- Using color as sole indicator (fails colorblind users)
- Not checking contrast ratios with tools
- Making error states same red as CTAs

---

### Skill: Whitespace Grouping (Proximity Principle)

**Problem:** Related content appears disconnected; unrelated content appears grouped.

**When to Use:** Forms, cards, content sections, any grouped information.

**Rules:**
1. Use an 8px grid system for all spacing
2. Related elements: tight spacing (8-12px)
3. Separate groups: larger spacing (24-32px)
4. Section dividers: maximum spacing (48-80px)
5. Increase spacing at larger breakpoints

**Spacing Scale:**
```scss
$space-1: 0.25rem;   // 4px  - Tight inline
$space-2: 0.5rem;    // 8px  - Compact
$space-3: 0.75rem;   // 12px - Comfortable
$space-4: 1rem;      // 16px - Default
$space-6: 1.5rem;    // 24px - Spacious
$space-8: 2rem;      // 32px - Sections
$space-12: 3rem;     // 48px - Major sections
$space-16: 4rem;     // 64px - Page sections
```

**Implementation:**
```
WRONG: Even spacing
[Header]        32px
[Subheader]     32px
[Body]          32px
[Button]

CORRECT: Grouped by relationship
[Header]        8px   ← Related
[Subheader]     24px  ← Introduces body
[Body]          16px  ← Paragraphs
[Button]              ← New group
```

**Common Mistakes:**
- Spacing everything equally
- Letting content touch viewport edges
- Not increasing spacing for larger screens

---

### Skill: Focal Point Placement

**Problem:** Users don't follow the intended path through the page.

**When to Use:** Landing pages, product pages, any page with conversion goals.

**Rules:**
1. One primary focal point per viewport
2. Place CTAs in terminal areas (bottom-right for Z-pattern)
3. Use largest/brightest element for primary attention
4. Guide eyes with directional cues (arrows, pointing imagery)
5. Follow F-pattern for text-heavy pages, Z-pattern for minimal pages

**Eye-Tracking Patterns:**
- **F-Pattern:** Text-heavy pages - users scan top, then left side
- **Z-Pattern:** Minimal pages - top-left → top-right → bottom-left → bottom-right
- **Gutenberg Diagram:** Dense layouts - reading gravity flows top-left to bottom-right

**Focal Point Techniques:**
| Technique | Effect |
|-----------|--------|
| Size dominance | Largest element first |
| Color contrast | Bright against neutral |
| Isolation (whitespace) | Surrounded = important |
| Faces/eyes | Humans look at faces instinctively |
| Motion | Movement attracts peripheral vision |

**Common Mistakes:**
- Competing for attention with multiple equal-weight CTAs
- Hiding CTAs in weak fallow areas (bottom-left)
- No clear visual path through content

---

## 1.2 Layout & Composition Skills

### Skill: 12-Column Grid Mastery

**Problem:** Elements feel misaligned; layouts lack consistency across pages.

**When to Use:** All page layouts.

**Rules:**
1. Use 12-column grid (divides into 1, 2, 3, 4, 6, 12)
2. Mobile: 4 columns, 16px gutter
3. Tablet: 6-12 columns, 24px gutter
4. Desktop: 12 columns, 32px gutter
5. Content containers max-width: 1440px

**Breakpoints:**
```scss
$breakpoints: (
  'sm': 640px,   // Large phones
  'md': 768px,   // Tablets
  'lg': 1024px,  // Small laptops
  'xl': 1280px,  // Desktops
  '2xl': 1536px  // Large screens
);
```

**Column Configurations:**
| Breakpoint | Columns | Gutter | Container Max |
|------------|---------|--------|---------------|
| < 640px | 4 | 16px | 100% (16px padding) |
| 640-767px | 6 | 24px | 640px |
| 768-1023px | 12 | 24px | 768px |
| 1024+ | 12 | 32px | 1440px |

**Common Mistakes:**
- Placing elements arbitrarily
- Inconsistent gutters
- Breaking grid unintentionally

---

### Skill: Content Rhythm Pacing

**Problem:** Pages feel monotonous or overwhelming; users lose engagement.

**When to Use:** Landing pages, long-form content, marketing pages.

**Rules:**
1. Vary section heights for rhythm
2. Major sections: 80-120px vertical spacing
3. Alternate between visual-heavy and text-heavy sections
4. Use rhythm variety: even (calm), accelerating (energy), varied (dynamic)
5. Place breather sections (testimonials, quotes) between dense content

**Section Pacing Pattern:**
```
HERO SECTION
    ↓ 80-120px
SOCIAL PROOF (Logos)
    ↓ 80-120px
FEATURES (3-column)
    ↓ 80-120px
TESTIMONIAL (Quote)
    ↓ 80-120px
PRICING (Cards)
    ↓ 80-120px
CTA SECTION
    ↓ 40-60px
FOOTER
```

**Common Mistakes:**
- All sections same height
- No breathing room between dense sections
- Ending pages abruptly without resolution

---

### Skill: Full-Bleed vs Contained Layouts

**Problem:** Pages feel cramped or content floats without structure.

**When to Use:** Hero sections, background sections, content areas.

**Rules:**
1. Full-bleed: backgrounds, hero images, color sections
2. Contained: text content, forms, product grids
3. Hybrid: full-bleed background with contained content
4. Never let text touch viewport edges (minimum 16px mobile, 32px desktop)

**Implementation:**
```html
<!-- Hybrid: Full-bleed background, contained content -->
<section class="w-full bg-blue-900">        <!-- Full-bleed -->
  <div class="max-w-7xl mx-auto px-4 py-16"> <!-- Contained -->
    <h2>Section Title</h2>
    <p>Content goes here...</p>
  </div>
</section>
```

**Common Mistakes:**
- Text running edge-to-edge
- Inconsistent container widths between sections

---

## 1.3 Typography & Readability Skills

### Skill: Font Pairing Harmony

**Problem:** Typography feels inconsistent or clashes visually.

**When to Use:** All projects at design system setup.

**Rules:**
1. Maximum 2 font families
2. Pair serif with sans-serif OR two complementary sans-serifs
3. Use weight variations (400, 500, 600, 700) instead of different fonts
4. Headlines: 600-700 weight; Body: 400 weight
5. Load only needed weights to minimize performance impact

**Recommended Pairings for E-commerce:**
| Category | Heading Font | Body Font | Mood |
|----------|--------------|-----------|------|
| Professional | Poppins | Open Sans | Modern, clean |
| Premium | Playfair Display | Inter | Elegant |
| Technical | Inter | Inter | Minimal |
| Friendly | Plus Jakarta Sans | Plus Jakarta Sans | Approachable |

**Performance:** Load fonts with `font-display: swap`; subset to needed characters.

**Common Mistakes:**
- Using 3+ different fonts
- Using different fonts for emphasis (use weight instead)
- Loading all weights (400, 500, 600, 700, 800...)

---

### Skill: Optimal Line Length Control

**Problem:** Long lines cause reading fatigue; short lines feel choppy.

**When to Use:** Any body text, articles, descriptions.

**Rules:**
1. Optimal line length: 45-75 characters (65ch ideal)
2. Use `max-width: 65ch` for prose
3. Headlines can be wider (up to 100%)
4. Constrain text, not containers

**Implementation:**
```css
.prose {
  max-width: 65ch; /* ~600px at 16px */
}

.headline {
  max-width: 20ch; /* Short, punchy */
}
```

**Line Height Rules:**
- Headlines: 1.1-1.3
- Body text: 1.5-1.75
- Dense UI: 1.3-1.5

**Common Mistakes:**
- Full-width body text
- Line height too tight (1.0-1.2 for body)
- Inconsistent line lengths across pages

---

## 1.4 Form UX Skills

### Skill: Single-Column Form Flow

**Problem:** Multi-column forms cause confusion about completion order.

**When to Use:** All forms, especially checkout.

**Rules:**
1. Single column layout always (exception: City/State/ZIP row)
2. Labels above fields, not beside
3. One input per row
4. Group related fields with section headings
5. Place primary action at bottom, full-width on mobile

**Implementation:**
```html
<form class="max-w-md mx-auto">
  <div class="form-section">
    <h3>Contact Information</h3>
    <div class="form-field">
      <label for="email">Email Address</label>
      <input type="email" id="email" autocomplete="email" />
    </div>
  </div>
  
  <!-- Only exception: related short fields -->
  <div class="form-row grid grid-cols-3 gap-4">
    <div class="form-field">
      <label for="city">City</label>
      <input type="text" id="city" />
    </div>
    <div class="form-field">
      <label for="state">State</label>
      <select id="state">...</select>
    </div>
    <div class="form-field">
      <label for="zip">ZIP</label>
      <input type="text" id="zip" />
    </div>
  </div>
</form>
```

**Common Mistakes:**
- Side-by-side fields on mobile
- Hiding labels (placeholder-only)
- Multi-column layouts

---

### Skill: Inline Validation Feedback

**Problem:** Users don't know about errors until form submission.

**When to Use:** All forms with validation requirements.

**Rules:**
1. Validate on blur, not on every keystroke
2. Show success state for completed fields
3. Show error with explanation, not just red border
4. Keep field values on error (don't clear)
5. Use `aria-invalid` and `aria-describedby` for accessibility

**Implementation:**
```html
<div class="form-field has-error">
  <label for="email">Email Address</label>
  <input 
    type="email" 
    id="email"
    aria-invalid="true"
    aria-describedby="email-error"
    class="border-red-500"
  />
  <span id="email-error" class="text-red-500 text-sm" role="alert">
    Please enter a valid email address
  </span>
</div>
```

**Visual States:**
```scss
.input {
  &:focus { border-color: var(--color-primary); }
  &:valid:not(:placeholder-shown) { border-color: var(--color-success); }
  &:invalid:not(:placeholder-shown) { border-color: var(--color-error); }
}
```

**Common Mistakes:**
- Validating on every keystroke (annoying)
- Error state with no explanation
- Clearing user input on error
- Red borders only (fails colorblind users)

---

### Skill: Smart Defaults & Autocomplete

**Problem:** Users waste time filling known information.

**When to Use:** Any repeated form fields.

**Rules:**
1. Use proper `autocomplete` attributes
2. Pre-select country based on IP
3. Remember previous addresses
4. Default to most common option
5. Pre-fill from user profile when logged in

**Autocomplete Attributes:**
```html
<input type="email" autocomplete="email" />
<input type="text" autocomplete="name" />
<input type="text" autocomplete="street-address" />
<input type="text" autocomplete="address-level2" /> <!-- City -->
<input type="text" autocomplete="address-level1" /> <!-- State -->
<input type="text" autocomplete="postal-code" />
<input type="tel" autocomplete="tel" />
<input type="text" autocomplete="cc-number" /> <!-- Card number -->
```

**Common Mistakes:**
- Missing autocomplete attributes
- `autocomplete="off"` on standard fields
- Not leveraging browser autofill

---

## 1.5 Navigation & Wayfinding Skills

### Skill: Mega Menu Organization

**Problem:** Users can't find categories in dropdown menus; menus feel overwhelming.

**When to Use:** Sites with 3+ main categories and 10+ subcategories.

**Rules:**
1. Maximum 4-5 columns in mega menu
2. Include featured/promo area for marketing
3. Use 100-150ms hover delay to prevent accidental triggers
4. Include "View All" link for each category
5. Support keyboard navigation (arrow keys, Escape)

**Implementation:**
```html
<nav aria-label="Main navigation">
  <ul class="nav-list">
    <li class="has-mega-menu">
      <button aria-expanded="false" aria-controls="mega-cooling">
        Cooling <span class="chevron">▼</span>
      </button>
      
      <div id="mega-cooling" class="mega-menu" role="menu">
        <div class="mega-content grid grid-cols-4 gap-8">
          <div class="mega-column">
            <h3>Air Conditioners</h3>
            <ul>
              <li><a href="/window-ac">Window Units</a></li>
              <li><a href="/portable-ac">Portable AC</a></li>
            </ul>
          </div>
          
          <!-- Featured area -->
          <div class="mega-featured">
            <a href="/summer-sale" class="promo-banner">
              <img src="sale-banner.jpg" alt="Summer Sale" />
            </a>
          </div>
        </div>
        
        <div class="mega-footer">
          <a href="/cooling">View All Cooling Products →</a>
        </div>
      </div>
    </li>
  </ul>
</nav>
```

**Accessibility:**
- Use `aria-expanded` on trigger
- Use `role="menu"` on dropdown
- Support Escape to close
- Arrow key navigation

**Common Mistakes:**
- Instant hover trigger (causes accidental opens)
- No keyboard navigation
- Missing "View All" links
- Too many columns (overwhelming)

---

### Skill: Breadcrumb Context

**Problem:** Users don't know where they are in the site hierarchy.

**When to Use:** All pages except homepage.

**Rules:**
1. Show full path from Home
2. Current page is last item (not a link)
3. Use Schema.org markup for SEO
4. Mobile: simplify to "← Back to [Parent]"
5. Truncate middle items if path is too long

**Implementation:**
```html
<nav aria-label="Breadcrumb" class="breadcrumb">
  <ol itemscope itemtype="https://schema.org/BreadcrumbList">
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
      <a itemprop="item" href="/"><span itemprop="name">Home</span></a>
      <meta itemprop="position" content="1" />
    </li>
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
      <a itemprop="item" href="/cooling"><span itemprop="name">Cooling</span></a>
      <meta itemprop="position" content="2" />
    </li>
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem" aria-current="page">
      <span itemprop="name">Window AC</span>
      <meta itemprop="position" content="3" />
    </li>
  </ol>
</nav>

<!-- Mobile alternative -->
<nav aria-label="Back" class="breadcrumb-mobile md:hidden">
  <a href="/cooling" class="back-link">← Cooling</a>
</nav>
```

**Common Mistakes:**
- Missing on product pages
- Current page as clickable link
- Not adapting for mobile
- Missing Schema.org markup

---

### Skill: Predictive Search Experience

**Problem:** Users can't find products quickly; search returns irrelevant results.

**When to Use:** All e-commerce sites.

**Rules:**
1. Start suggesting after 2 characters
2. Debounce API calls (200-300ms delay)
3. Show recent searches for returning users
4. Include product thumbnails in suggestions
5. Provide "No results" state with alternatives

**Search Suggestion Types:**
1. Recent searches (user history)
2. Popular searches (trending)
3. Product matches (with image, price)
4. Category matches
5. Spelling suggestions

**Implementation:**
```html
<div class="search-container" role="search">
  <input 
    type="search"
    placeholder="Search products..."
    aria-autocomplete="list"
    aria-controls="search-suggestions"
    aria-expanded="false"
  />
  
  <div id="search-suggestions" class="search-dropdown" role="listbox">
    <div class="suggestion-section">
      <h4>Recent Searches</h4>
      <ul>
        <li role="option"><a href="/search?q=window+ac">window ac</a></li>
      </ul>
    </div>
    
    <div class="suggestion-section">
      <h4>Products</h4>
      <ul class="product-suggestions">
        <li role="option" class="product-suggestion">
          <a href="/products/lg-12k">
            <img src="product.jpg" alt="" />
            <div>
              <span>LG 12000 BTU Window AC</span>
              <span class="price">$449.99</span>
            </div>
          </a>
        </li>
      </ul>
    </div>
  </div>
</div>
```

**Common Mistakes:**
- No debouncing (API overload)
- Text-only results (no thumbnails)
- No recent searches
- Empty "No results" state

---

## 1.6 Feedback & States Skills

### Skill: Loading State Communication

**Problem:** Users don't know if something is happening; they click again or leave.

**When to Use:** Any async operation (API calls, form submissions, navigation).

**Rules:**
1. Show feedback within 100ms of user action
2. Use skeleton loaders for content loading
3. Use spinners for button/action loading
4. Disable buttons during loading (prevent double-submit)
5. Show progress for long operations

**Loading Patterns:**
| Pattern | Use Case | Duration |
|---------|----------|----------|
| Skeleton shimmer | Page/content loading | > 500ms |
| Button spinner | Form submission | Any |
| Progress bar | File uploads, long ops | > 3s |
| Inline spinner | Async validation | Any |

**Skeleton Implementation:**
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

**Accessibility:** Add `aria-busy="true"` to loading containers.

**Common Mistakes:**
- No feedback (users click multiple times)
- Spinner only with no context
- Allowing double-submission
- Static loading states (no animation)

---

### Skill: Error State Recovery

**Problem:** Users don't know what went wrong or how to fix it.

**When to Use:** All error scenarios (validation, API failures, 404s).

**Rules:**
1. Always provide actionable next steps
2. Keep user data on error (never clear forms)
3. Link to specific fields with issues
4. Offer alternatives (try again, contact support)
5. Use plain language (no technical jargon)

**Error Message Structure:**
```html
<!-- Form-level error summary -->
<div class="error-summary" role="alert">
  <h4>Please fix the following errors:</h4>
  <ul>
    <li><a href="#email">Email address is required</a></li>
    <li><a href="#card-number">Card number is invalid</a></li>
  </ul>
</div>

<!-- Field-level error -->
<div class="form-field has-error">
  <label for="card-number">Card Number</label>
  <input 
    type="text" 
    id="card-number"
    aria-invalid="true"
    aria-describedby="card-error"
  />
  <span id="card-error" class="error-message">
    This card number is invalid. Please check and try again.
  </span>
</div>

<!-- Recovery options -->
<div class="error-actions">
  <button>Try Again</button>
  <button>Use Different Card</button>
  <a href="/contact">Contact Support</a>
</div>
```

**Common Mistakes:**
- "An error occurred" (no context)
- Clearing form data on error
- No recovery path
- Technical error messages

---

### Skill: Empty State Design

**Problem:** Empty states confuse users; they don't know what to do.

**When to Use:** Empty carts, no search results, empty wishlists, new accounts.

**Rules:**
1. Explain why it's empty
2. Provide clear next action
3. Use friendly illustration or icon
4. Suggest relevant content
5. Keep tone positive and encouraging

**Empty State Structure:**
```html
<div class="empty-state text-center py-16">
  <img src="empty-cart.svg" alt="" class="w-32 mx-auto mb-4" />
  <h3>Your cart is empty</h3>
  <p class="text-gray-600 mb-6">
    Looks like you haven't added anything yet. 
    Start shopping to fill it up!
  </p>
  <a href="/products" class="btn btn-primary">Browse Products</a>
  
  <!-- Optional: Show recently viewed -->
  <div class="recently-viewed mt-8">
    <h4>Recently Viewed</h4>
    <!-- Product cards -->
  </div>
</div>
```

**Empty State Types:**
| Context | Primary Action | Secondary Suggestion |
|---------|---------------|---------------------|
| Empty cart | Browse Products | Recently viewed |
| No search results | Modify search | Popular products |
| Empty wishlist | Browse Products | Featured items |
| No orders | Start Shopping | Trending items |

**Common Mistakes:**
- Blank screen with no guidance
- Generic "No items" message
- No clear CTA
- Negative tone ("Nothing here!")

---

## 1.7 Accessibility Skills

### Skill: Focus Management

**Problem:** Keyboard users can't navigate efficiently; focus gets lost.

**When to Use:** All interactive elements, modals, dynamic content.

**Rules:**
1. All interactive elements must be focusable
2. Focus order matches visual order
3. Focus visible at all times (never `outline: none` without replacement)
4. Trap focus in modals
5. Return focus to trigger when modal closes

**Focus Visible Styles:**
```scss
/* Visible focus for keyboard users only */
.btn:focus-visible {
  outline: 2px solid var(--color-focus);
  outline-offset: 2px;
}

/* Don't remove focus, replace it */
.btn:focus {
  outline: none; /* Only if you provide alternative */
  box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.3);
}
```

**Focus Trap Implementation:**
```typescript
function trapFocus(container: HTMLElement) {
  const focusable = container.querySelectorAll(
    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
  );
  const first = focusable[0] as HTMLElement;
  const last = focusable[focusable.length - 1] as HTMLElement;
  
  container.addEventListener('keydown', (e) => {
    if (e.key !== 'Tab') return;
    
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  });
}
```

**Common Mistakes:**
- `outline: none` without replacement
- Focus lost after modal close
- No focus trap in modals
- Invisible focus indicators

---

### Skill: Screen Reader Announcements

**Problem:** Screen reader users don't know when things change dynamically.

**When to Use:** Cart updates, form submissions, loading states, notifications.

**Rules:**
1. Use `aria-live` regions for dynamic updates
2. `aria-live="polite"` for non-urgent updates
3. `aria-live="assertive"` for critical alerts
4. Announce success/error states
5. Keep announcements concise

**Implementation:**
```html
<!-- Live region for announcements -->
<div aria-live="polite" aria-atomic="true" class="sr-only" id="announcer"></div>

<!-- Cart update announcement -->
<script>
function announceToScreenReader(message: string) {
  const announcer = document.getElementById('announcer');
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
</script>
```

**Common Mistakes:**
- No announcements for dynamic changes
- Overly verbose announcements
- Missing `aria-live` on status messages
- Not announcing loading completion

---

### Skill: Semantic HTML Structure

**Problem:** Screen readers can't navigate efficiently; content lacks meaning.

**When to Use:** Always - foundation of accessibility.

**Rules:**
1. Use proper heading hierarchy (h1 → h2 → h3, never skip)
2. Use landmarks (`<nav>`, `<main>`, `<aside>`, `<footer>`)
3. Use `<button>` for actions, `<a>` for navigation
4. Use lists for related items (`<ul>`, `<ol>`)
5. Use tables for tabular data (with proper headers)

**Semantic Structure:**
```html
<header>
  <nav aria-label="Main navigation">...</nav>
</header>

<main id="main-content">
  <h1>Page Title</h1> <!-- Only one h1 per page -->
  
  <section aria-labelledby="features-heading">
    <h2 id="features-heading">Features</h2>
    <ul>
      <li>Feature one</li>
      <li>Feature two</li>
    </ul>
  </section>
  
  <aside aria-label="Related products">
    <h2>You May Also Like</h2>
    ...
  </aside>
</main>

<footer>...</footer>
```

**Common Mistakes:**
- Skipping heading levels (h1 → h4)
- `<div>` for everything
- `<a href="#">` for buttons
- Non-semantic tables for layout

---

## 1.8 Consistency & Systems Skills

### Skill: Design Token Definition

**Problem:** Colors/spacing/typography vary across components; inconsistent look.

**When to Use:** At project setup; maintained throughout.

**Rules:**
1. Define all colors as CSS custom properties
2. Never use hardcoded colors in components
3. Include semantic color names (not just color values)
4. Support light/dark themes from the start
5. Document all tokens

**Token Structure:**
```scss
:root {
  /* Primitive colors */
  --color-blue-500: #3b82f6;
  --color-blue-600: #2563eb;
  
  /* Semantic colors */
  --color-primary: var(--color-blue-500);
  --color-primary-hover: var(--color-blue-600);
  
  --color-text-primary: #1a1a1a;
  --color-text-secondary: #666666;
  
  --color-bg-primary: #ffffff;
  --color-bg-secondary: #f5f5f5;
  
  /* Status colors */
  --color-success: #22c55e;
  --color-warning: #f59e0b;
  --color-error: #ef4444;
  
  /* Spacing */
  --space-xs: 0.25rem;
  --space-sm: 0.5rem;
  --space-md: 1rem;
  --space-lg: 1.5rem;
  --space-xl: 2rem;
  
  /* Typography */
  --font-family-heading: 'Poppins', sans-serif;
  --font-family-body: 'Open Sans', sans-serif;
}

[data-theme="dark"] {
  --color-text-primary: #ffffff;
  --color-text-secondary: #a0a0a0;
  --color-bg-primary: #1a1a1a;
  --color-bg-secondary: #2a2a2a;
}
```

**Common Mistakes:**
- Hardcoded colors in components
- No dark mode support
- Inconsistent naming
- Missing semantic tokens

---

### Skill: Component API Consistency

**Problem:** Similar components work differently; developers confused about usage.

**When to Use:** All shared components.

**Rules:**
1. Consistent prop naming across components
2. Same variants use same names (primary, secondary, danger)
3. Same sizes use same scale (sm, md, lg)
4. Boolean props use consistent naming (isLoading, isDisabled)
5. Event handlers use consistent naming (onClick, onChange, onSubmit)

**Consistent Component API:**
```typescript
// Button variants
<Button variant="primary" size="md" isLoading={false} onClick={...} />
<Button variant="secondary" size="sm" isDisabled={true} />

// Same pattern for other components
<Input size="md" isDisabled={false} onChange={...} />
<Card variant="elevated" size="lg" />
<Alert variant="success" size="md" onDismiss={...} />
```

**Size Scale Consistency:**
| Size | Use Case |
|------|----------|
| xs | Dense UI, table cells |
| sm | Secondary elements |
| md | Default |
| lg | Emphasis, touch targets |
| xl | Hero elements |

**Common Mistakes:**
- `size` in one component, `variant` for same concept in another
- `disabled` vs `isDisabled`
- `click` vs `onClick`
- Different size scales per component

---

# Part 2: Motion & Scroll Skills

## 2.1 Scroll-Driven Reveal Skills

### Skill: Fade-In on Scroll

**Problem:** Content appears suddenly, jarring the user experience.

**When to Use:** Standard content sections, cards, lists.

**Rules:**
1. Use CSS `animation-timeline: view()` for native performance
2. Complete animation early in viewport entry (at 40% entry, not 100%)
3. Translate 20-40px maximum (subtle movement)
4. Duration 400-600ms for content reveals
5. Use `ease-out` easing for natural deceleration

**Implementation:**
```scss
.reveal {
  opacity: 0;
  transform: translateY(30px);
  animation: reveal-fade 500ms ease-out forwards;
  animation-timeline: view();
  animation-range: entry 10% entry 40%;
}

@keyframes reveal-fade {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .reveal {
    opacity: 1;
    transform: none;
    animation: none;
  }
}
```

**Fallback (Intersection Observer):**
```typescript
const observer = new IntersectionObserver(
  (entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        observer.unobserve(entry.target);
      }
    });
  },
  { threshold: 0.1, rootMargin: '50px' }
);
```

**Performance:** CSS-only, GPU-accelerated. Zero JS overhead with native support.

**Accessibility:** Content in DOM from start; animation is enhancement only.

**Common Mistakes:**
- Animation completes at viewport center (user waits too long)
- Large translate values (50px+) feel slow
- Missing reduced-motion support
- Animation on every element (visual noise)

---

### Skill: Staggered List Reveals

**Problem:** Lists appear all at once, overwhelming the user.

**When to Use:** Product grids, feature lists, team members, portfolio items.

**Rules:**
1. Maximum 10-15 items animating simultaneously
2. Stagger delay: 50-100ms between items
3. Use CSS custom properties for delay calculation
4. Grid wave: delay = row + column for diagonal reveal
5. Stop staggering after ~10 items (all reveal together)

**Implementation:**
```scss
.stagger-list {
  > * {
    opacity: 0;
    transform: translateY(20px);
    animation: stagger-in 400ms ease-out forwards;
    animation-timeline: view();
    animation-range: entry 0% entry 50%;
  }
  
  @for $i from 1 through 12 {
    > *:nth-child(#{$i}) {
      animation-delay: calc((#{$i} - 1) * 80ms);
    }
  }
  
  // After 12, no more stagger
  > *:nth-child(n+13) {
    animation-delay: 960ms;
  }
}

@keyframes stagger-in {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

**Grid Wave Pattern:**
```scss
.grid-wave {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  
  // Diagonal: delay = (row + col) * base
  @for $row from 1 through 5 {
    @for $col from 1 through 3 {
      $index: ($row - 1) * 3 + $col;
      $delay: ($row + $col - 2) * 80ms;
      
      > *:nth-child(#{$index}) {
        animation-delay: #{$delay};
      }
    }
  }
}
```

**Performance:** Limit active animations; use CSS for static lists.

**Common Mistakes:**
- All items stagger (50+ items = 5 second reveal)
- Delay too long (150ms+)
- No maximum stagger limit

---

## 2.2 Cinematic Section Transition Skills

### Skill: Hero Scroll Sequence

**Problem:** Hero sections feel static; transition to content is abrupt.

**When to Use:** Homepage heroes, product launch pages, campaign landings.

**Rules:**
1. Use double-height container (200vh) for scroll space
2. Sticky container holds hero content
3. Layer elements transform at different rates
4. Complete hero exit by 50% scroll through section
5. Content enters as hero exits

**Implementation:**
```scss
.hero-cinematic {
  position: relative;
  height: 200vh;
}

.hero-sticky {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.hero-headline {
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

.hero-background {
  animation: bg-zoom linear;
  animation-timeline: scroll();
  animation-range: exit 0% exit 100%;
}

@keyframes bg-zoom {
  to {
    transform: scale(1.2);
    opacity: 0.3;
  }
}
```

**Performance:** Use transform and opacity only; preload hero images.

**Accessibility:** All text readable without animation; skip hero option for repeat visitors.

**Common Mistakes:**
- Animation too slow (hero still visible at 80% scroll)
- Background zooms while text is readable (distracting)
- No fallback for Safari (no scroll-timeline support)

---

### Skill: Section Color Transitions

**Problem:** Content sections feel disconnected; no visual flow between them.

**When to Use:** Mood transitions, storytelling, section differentiation.

**Rules:**
1. Use CSS custom property updated on scroll
2. Gradient crossfade for complex color shifts
3. Ensure text contrast at all states (WCAG compliant)
4. Subtle hue shifts (30-60 degrees)
5. Transition happens between sections, not during reading

**Implementation:**
```scss
:root {
  --scroll-progress: 0;
}

.gradient-bg {
  position: fixed;
  inset: 0;
  z-index: -1;
  background: linear-gradient(
    135deg,
    hsl(calc(220 + var(--scroll-progress) * 40), 50%, 20%) 0%,
    hsl(calc(260 + var(--scroll-progress) * 30), 40%, 25%) 100%
  );
}
```

```typescript
window.addEventListener('scroll', () => {
  const progress = window.scrollY / (document.body.scrollHeight - window.innerHeight);
  document.documentElement.style.setProperty('--scroll-progress', progress.toString());
}, { passive: true });
```

**Performance:** CSS gradients are GPU-rendered; throttle scroll updates.

**Accessibility:** Don't convey meaning through color alone; ensure text contrast.

**Common Mistakes:**
- Color changes while reading (distracting)
- Text becomes unreadable at certain scroll positions
- Jarring color jumps instead of smooth transitions

---

## 2.3 Parallax Layering Skills

### Skill: Subtle Depth Parallax

**Problem:** Background feels flat; no sense of depth.

**When to Use:** Hero backgrounds, decorative accents, immersive sections.

**Rules:**
1. Keep ratios subtle: 0.2-0.5 speed difference from scroll
2. Maximum 3-4 layers
3. Never use for content (decorative only)
4. Use transform3d for GPU acceleration
5. Disable completely for prefers-reduced-motion

**Layer Structure:**
- Layer 0 (Back): 0.2x scroll speed
- Layer 1 (Mid): 0.5x scroll speed
- Layer 2 (Content): 1x (normal scroll)

**Implementation:**
```scss
.parallax-layer {
  position: absolute;
  inset: -20%; // Extra space for movement
  animation: parallax linear;
  animation-timeline: view();
  animation-range: entry 0% exit 100%;
}

.parallax-layer--back {
  @keyframes parallax {
    from { transform: translateY(-10%); }
    to { transform: translateY(10%); }
  }
}

.parallax-layer--mid {
  @keyframes parallax {
    from { transform: translateY(-5%); }
    to { transform: translateY(5%); }
  }
}

@media (prefers-reduced-motion: reduce) {
  .parallax-layer {
    animation: none;
    transform: none;
  }
}
```

**Performance:** CSS transforms are GPU-accelerated; avoid JS for simple parallax.

**Common Mistakes:**
- Extreme ratios (content moves too fast/slow)
- Parallax on content (hard to read)
- Not disabling for reduced motion
- Too many layers (performance impact)

---

## 2.4 Motion-as-Feedback Skills

### Skill: Button State Trio

**Problem:** Users don't get feedback that their interaction was registered.

**When to Use:** All interactive buttons and clickable elements.

**Rules:**
1. Hover: subtle lift (translateY -2px) + shadow increase
2. Active/Press: press down effect (translateY 0) + reduced shadow
3. Focus: visible outline for keyboard users
4. Duration: 150-200ms for micro-interactions
5. Use ease-out for natural deceleration

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

**Accessibility:** `:focus-visible` for keyboard-only focus styles; maintain 3:1 contrast.

**Common Mistakes:**
- No hover state (feels unresponsive)
- Same hover and active state (no press feedback)
- Removing focus styles without replacement
- Too long duration (feels laggy)

---

### Skill: Add-to-Cart Animation

**Problem:** Users don't get confirmation that item was added to cart.

**When to Use:** E-commerce product pages, quick-add buttons.

**Rules:**
1. Animation is supplementary; success message is primary
2. Clone product image, don't move original
3. Flight path to cart icon (top-right typically)
4. Pulse cart icon with scale animation
5. Update cart count with bounce

**Animation Sequence:**
1. Clone product thumbnail
2. Animate clone to cart icon position
3. Scale clone down during flight
4. Remove clone on arrival
5. Pulse cart icon
6. Update count with bounce

**Implementation:**
```typescript
async function animateAddToCart(productEl: HTMLElement, cartEl: HTMLElement) {
  const clone = productEl.querySelector('img')?.cloneNode(true) as HTMLElement;
  const productRect = productEl.getBoundingClientRect();
  const cartRect = cartEl.getBoundingClientRect();
  
  clone.style.cssText = `
    position: fixed;
    top: ${productRect.top}px;
    left: ${productRect.left}px;
    width: 80px;
    height: 80px;
    object-fit: cover;
    border-radius: 8px;
    transition: all 500ms cubic-bezier(0.2, 1, 0.3, 1);
    z-index: 1000;
    pointer-events: none;
  `;
  
  document.body.appendChild(clone);
  clone.offsetHeight; // Trigger reflow
  
  clone.style.cssText += `
    top: ${cartRect.top + cartRect.height / 2}px;
    left: ${cartRect.left + cartRect.width / 2}px;
    width: 20px;
    height: 20px;
    opacity: 0;
  `;
  
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
- Announce via `aria-live` region: "Item added to cart"
- Disable flight animation for `prefers-reduced-motion`
- Success message is primary feedback (not animation)

**Common Mistakes:**
- Animation only, no text feedback
- Moving original element (breaks layout)
- Animation blocks interaction
- No reduced motion alternative

---

### Skill: Cart Badge Bounce

**Problem:** Users don't notice cart count updates.

**When to Use:** After any cart modification (add, remove, quantity change).

**Rules:**
1. Scale bounce: 1 → 1.3 → 0.9 → 1.1 → 1
2. Duration: 300-400ms
3. Trigger via class toggle (remove after animation)
4. Color flash optional for emphasis
5. Number increment animation optional

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
}

.cart-badge.updated {
  animation: badge-bounce 400ms ease-out;
}

@keyframes badge-bounce {
  0% { transform: scale(1); }
  30% { transform: scale(1.4); }
  50% { transform: scale(0.9); }
  70% { transform: scale(1.1); }
  100% { transform: scale(1); }
}
```

**Common Mistakes:**
- Animation too subtle (not noticed)
- Animation too long (feels slow)
- Not removing animation class (won't replay)

---

## 2.5 Sticky-Section Storytelling Skills

### Skill: Pin-and-Transform Sections

**Problem:** Content flies past too quickly; complex features need focused attention.

**When to Use:** Product demonstrations, feature deep-dives, multi-step tutorials.

**Rules:**
1. Section height = number of "frames" × 100vh
2. Sticky container pins content at top
3. Transform internal content based on scroll progress
4. Show frame indicators for orientation
5. Support keyboard/click navigation between frames

**Implementation:**
```html
<section class="pin-section" style="--num-frames: 4">
  <div class="pin-container">
    <div class="frame frame-1 active">Frame 1 Content</div>
    <div class="frame frame-2">Frame 2 Content</div>
    <div class="frame frame-3">Frame 3 Content</div>
    <div class="frame frame-4">Frame 4 Content</div>
  </div>
  
  <nav class="frame-indicators">
    <button class="indicator active" data-frame="1">1</button>
    <button class="indicator" data-frame="2">2</button>
    <button class="indicator" data-frame="3">3</button>
    <button class="indicator" data-frame="4">4</button>
  </nav>
</section>
```

```scss
.pin-section {
  height: calc(100vh * var(--num-frames, 3));
}

.pin-container {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.frame {
  position: absolute;
  inset: 0;
  opacity: 0;
  transition: opacity 0.3s ease-out;
  
  &.active {
    opacity: 1;
  }
}
```

**Performance:** Use `position: sticky` (native); only transform active frame.

**Accessibility:** All frames in DOM; keyboard navigation between frames.

**Common Mistakes:**
- No frame indicators (users disoriented)
- Can't skip frames (forced linear progression)
- Too many frames (becomes tedious)
- No scroll indication (looks stuck)

---

### Skill: Horizontal Scroll Within Vertical

**Problem:** Horizontal content doesn't fit; normal horizontal scroll is awkward.

**When to Use:** Product showcases, portfolio galleries, feature cards.

**Rules:**
1. Section height = viewport height + (track width - viewport width)
2. Sticky container holds horizontal track
3. Vertical scroll translates to horizontal movement
4. Provide visual indication of horizontal content
5. Support native horizontal scroll on mobile

**Implementation:**
```scss
.horizontal-scroll-section {
  height: 300vh; // Adjust based on track width
}

.horizontal-container {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.horizontal-track {
  display: flex;
  gap: 2rem;
  padding: 2rem;
  height: 100%;
  align-items: center;
  
  animation: scroll-horizontal linear;
  animation-timeline: view();
  animation-range: contain;
}

@keyframes scroll-horizontal {
  to {
    transform: translateX(calc(-100% + 100vw));
  }
}

.horizontal-card {
  flex: 0 0 350px;
  height: 70%;
}
```

**Performance:** CSS transforms are GPU-accelerated; calculate section height carefully.

**Accessibility:** Provide alternative vertical navigation; all cards keyboard accessible.

**Common Mistakes:**
- Section height too short (content cuts off)
- No indication of horizontal content
- Mobile uses same pattern (awkward on touch)

---

### Skill: Content Swapping While Pinned

**Problem:** Need to show multiple related states without page navigation.

**When to Use:** Product configurators, feature comparisons, interactive demos.

**Rules:**
1. All states preloaded and in DOM
2. Swap via opacity transition (not display)
3. Include manual navigation (not just scroll)
4. Sync navigation indicators with scroll position
5. Preload images for all states

**Implementation:**
```scss
.swap-section {
  height: 300vh;
}

.swap-container {
  position: sticky;
  top: 0;
  height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
}

.swap-image,
.swap-content {
  position: absolute;
  opacity: 0;
  transition: opacity 0.4s ease-out;
  
  &.active {
    position: relative;
    opacity: 1;
  }
}

.swap-indicators {
  position: absolute;
  bottom: 2rem;
  display: flex;
  gap: 1rem;
}

.indicator {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: var(--color-text-muted);
  
  &.active {
    background: var(--color-primary);
  }
}
```

**Common Mistakes:**
- States not preloaded (flash of loading)
- Only scroll controls (no manual navigation)
- No visual indicator of current state

---

## 2.6 Progressive Disclosure via Motion Skills

### Skill: Accordion Expand Animation

**Problem:** Content appears/disappears instantly; jarring experience.

**When to Use:** FAQ sections, product specifications, mobile navigation.

**Rules:**
1. Animate height with CSS `grid-template-rows: 0fr → 1fr`
2. Duration: 200-300ms
3. Icon rotation indicates state
4. Only one item open at a time (optional)
5. Content in DOM always (for accessibility)

**Implementation:**
```scss
.accordion-item {
  .accordion-content {
    display: grid;
    grid-template-rows: 0fr;
    transition: grid-template-rows 300ms ease-out;
    
    > div {
      overflow: hidden;
    }
  }
  
  &.open .accordion-content {
    grid-template-rows: 1fr;
  }
  
  .accordion-icon {
    transition: transform 200ms ease-out;
  }
  
  &.open .accordion-icon {
    transform: rotate(180deg);
  }
}
```

**Accessibility:** Use `aria-expanded`, `aria-controls`; content always in DOM.

**Common Mistakes:**
- Animating `height` directly (bad performance)
- `display: none` on hidden content (inaccessible)
- No visual indicator of expandable content

---

### Skill: Tooltip Reveal Animation

**Problem:** Tooltips appear instantly, startling users.

**When to Use:** Info icons, field hints, feature explanations.

**Rules:**
1. Delay: 200-400ms before showing (prevents flicker)
2. Duration: 150-200ms fade in
3. Position above/below element (never cover trigger)
4. Dismiss on mouse leave or Escape
5. Touch: tap to toggle

**Implementation:**
```scss
.tooltip-trigger {
  position: relative;
  
  .tooltip {
    position: absolute;
    bottom: calc(100% + 8px);
    left: 50%;
    transform: translateX(-50%);
    
    opacity: 0;
    visibility: hidden;
    transition: opacity 150ms ease-out, visibility 150ms;
    
    padding: 0.5rem 1rem;
    background: var(--color-surface-elevated);
    border-radius: 4px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    white-space: nowrap;
  }
  
  &:hover .tooltip,
  &:focus .tooltip {
    opacity: 1;
    visibility: visible;
    transition-delay: 300ms; // Delay before showing
  }
}
```

**Accessibility:** Use `role="tooltip"`, `aria-describedby`; keyboard accessible.

**Common Mistakes:**
- No delay (appears on every hover)
- Tooltip covers trigger
- Not keyboard accessible
- Tooltip cuts off at viewport edge

---

## 2.7 Motion Pacing & Easing Skills

### Skill: Easing Function Selection

**Problem:** Motion feels mechanical, unnatural, or wrong for the context.

**When to Use:** All animations - foundation of good motion design.

**Rules:**
1. `ease-out` (default): Natural deceleration, things coming to rest
2. `ease-in`: Things starting to move, building energy
3. `ease-in-out`: Full journey, loop animations
4. `linear`: Progress bars, continuous motion
5. Custom cubic-bezier for branded motion

**Easing Reference:**
```scss
// Standard easings
--ease-out: cubic-bezier(0, 0, 0.2, 1);      // Decelerate
--ease-in: cubic-bezier(0.4, 0, 1, 1);       // Accelerate
--ease-in-out: cubic-bezier(0.4, 0, 0.2, 1); // Both

// Expressive easings
--ease-bounce: cubic-bezier(0.175, 0.885, 0.32, 1.275);  // Overshoot
--ease-snappy: cubic-bezier(0.7, 0, 0.3, 1);             // Quick snap
--ease-smooth: cubic-bezier(0.45, 0, 0.55, 1);           // Very smooth
```

**When to Use Each:**
| Easing | Use Case |
|--------|----------|
| ease-out | Content reveals, hover states, most UI |
| ease-in | Exit animations, things leaving |
| ease-in-out | Page transitions, modals |
| linear | Progress bars, loading |
| bounce | Celebratory feedback, badges |
| snappy | Micro-interactions, toggles |

**Common Mistakes:**
- Using `linear` for UI motion (robotic)
- Using `ease-in` for entrances (feels slow)
- Same easing for everything

---

### Skill: Duration Calibration

**Problem:** Animations feel too fast (jarring) or too slow (sluggish).

**When to Use:** All animations.

**Rules:**
1. Micro-interactions: 100-200ms
2. Standard UI: 200-300ms
3. Content reveals: 300-500ms
4. Page transitions: 300-500ms
5. Complex sequences: 500-800ms
6. Loading animations: 1500-2000ms (infinite)

**Duration Reference:**
| Use Case | Duration |
|----------|----------|
| Button hover | 150ms |
| Dropdown open | 200ms |
| Card hover lift | 250ms |
| Modal open | 300ms |
| Fade in content | 400ms |
| Slide in panel | 300ms |
| Page transition | 400ms |
| Stagger delay | 50-100ms each |
| Skeleton shimmer | 1500ms |

**Scale by Distance:**
- Small movements (2-10px): 150-200ms
- Medium movements (20-50px): 250-350ms
- Large movements (100px+): 400-500ms

**Common Mistakes:**
- Same duration for all animations
- Too fast for large movements
- Too slow for micro-interactions

---

## 2.8 Reduced Motion Strategy Skills

### Skill: prefers-reduced-motion Support

**Problem:** Animations cause discomfort for users with vestibular disorders.

**When to Use:** ALWAYS - non-negotiable accessibility requirement.

**Rules:**
1. Check `prefers-reduced-motion: reduce` media query
2. Remove motion, not feedback (instant transitions okay)
3. Keep opacity changes (usually safe)
4. Remove parallax, scroll hijacking, bounces
5. Reduce, don't eliminate (some feedback still helpful)

**Implementation:**
```scss
// Full experience (default)
.animated-element {
  opacity: 0;
  transform: translateY(30px);
  animation: reveal 500ms ease-out forwards;
}

// Reduced motion: instant, no movement
@media (prefers-reduced-motion: reduce) {
  .animated-element {
    opacity: 1;
    transform: none;
    animation: none;
  }
  
  // Or: minimal fade only
  .animated-element {
    animation: fade-only 200ms ease-out forwards;
  }
  
  @keyframes fade-only {
    to { opacity: 1; }
  }
  
  // Disable all motion animations
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

**JavaScript Check:**
```typescript
const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

if (prefersReducedMotion.matches) {
  // Use simple transitions
  element.classList.add('visible'); // Instant
} else {
  // Use full animation
  animateElement(element);
}

// Listen for changes
prefersReducedMotion.addEventListener('change', (e) => {
  if (e.matches) {
    disableAnimations();
  } else {
    enableAnimations();
  }
});
```

**What to Remove:**
- Parallax scrolling
- Bouncing/elastic animations
- Auto-playing carousels
- Scroll hijacking
- Background motion
- Large translates/rotations

**What to Keep (Safe):**
- Opacity fades (under 500ms)
- Color transitions
- Focus indicators
- Loading spinners (essential feedback)
- Button hover states (subtle)

**Common Mistakes:**
- Not supporting at all
- Removing all animation (including essential feedback)
- Only checking once (not listening for changes)

---

## 2.9 Performance-Safe Animation Skills

### Skill: GPU-Accelerated Properties Only

**Problem:** Animations cause jank, dropped frames, slow performance.

**When to Use:** All animations - foundation of smooth motion.

**Rules:**
1. SAFE: `transform`, `opacity`
2. SOMETIMES SAFE: `filter`, `clip-path`
3. NEVER: `width`, `height`, `top`, `left`, `margin`, `padding`
4. Use `will-change` sparingly and temporarily
5. Aim for < 16ms per frame (60fps)

**Safe Properties:**
```scss
// GOOD - Compositor only, GPU-accelerated
.element {
  transform: translateX(100px);
  transform: scale(1.2);
  transform: rotate(45deg);
  opacity: 0.5;
}

// SOMETIMES OKAY - Depends on element size
.element {
  filter: blur(10px);
  clip-path: inset(0 50% 0 0);
}

// BAD - Triggers layout/paint
.element {
  width: 200px;       // Layout
  height: 200px;      // Layout
  top: 50px;          // Layout
  left: 50px;         // Layout
  margin: 20px;       // Layout
  padding: 20px;      // Layout
  border-width: 2px;  // Layout
  font-size: 18px;    // Layout + Paint
  background-position: 50% 50%; // Paint
  box-shadow: 0 10px 20px; // Paint (animate opacity instead)
}
```

**will-change Usage:**
```scss
// Apply before animation, remove after
.element:hover {
  will-change: transform;
}

// In JS, apply just before animation
element.style.willChange = 'transform';
element.animate([...]).finished.then(() => {
  element.style.willChange = 'auto';
});

// NEVER do this
* {
  will-change: transform, opacity; // Memory waste
}
```

**Common Mistakes:**
- Animating width/height (use scale instead)
- Animating top/left (use translateX/Y instead)
- `will-change` on everything
- Not testing on low-end devices

---

### Skill: Scroll Handler Optimization

**Problem:** Scroll listeners cause jank and battery drain.

**When to Use:** Any scroll-based animation or effect.

**Rules:**
1. Always use `{ passive: true }` for scroll listeners
2. Throttle with `requestAnimationFrame`
3. Use CSS scroll-driven animations when possible
4. Batch DOM reads, then DOM writes
5. Debounce non-critical updates

**Implementation:**
```typescript
// Basic rAF throttling
let ticking = false;

window.addEventListener('scroll', () => {
  if (!ticking) {
    requestAnimationFrame(() => {
      updateAnimations();
      ticking = false;
    });
    ticking = true;
  }
}, { passive: true });

// Debounce for non-critical updates
function debounce(fn: Function, delay: number) {
  let timeoutId: number;
  return (...args: any[]) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const updateProgressDebounced = debounce(updateProgress, 100);
```

**Avoid Layout Thrashing:**
```typescript
// BAD - Layout thrashing
elements.forEach(el => {
  const height = el.offsetHeight; // Read
  el.style.height = height + 10 + 'px'; // Write
});

// GOOD - Batch reads, then writes
const heights = elements.map(el => el.offsetHeight); // All reads
elements.forEach((el, i) => {
  el.style.height = heights[i] + 10 + 'px'; // All writes
});
```

**Performance Targets:**
- < 16ms per frame (60fps)
- < 10ms JavaScript per frame
- No layout thrashing
- Passive scroll listeners

**Common Mistakes:**
- Non-passive scroll listeners
- Reading and writing DOM interleaved
- Not using rAF for animations
- Too many simultaneous animations

---

# Part 3: E-commerce Specific Skills

## 3.1 Product Discovery Skills

### Skill: Faceted Filter UX

**Problem:** Users can't narrow down large product catalogs efficiently.

**When to Use:** Category pages with 20+ products.

**Rules:**
1. Show result count next to each filter option
2. Update counts dynamically as filters change
3. Persist filter state in URL (shareable links)
4. Provide "Clear All" for quick reset
5. Remember filter state on back navigation

**Implementation:**
```html
<aside class="filter-panel" aria-label="Product filters">
  <!-- Active Filters -->
  <div class="active-filters">
    <span class="filter-chip">
      Brand: LG <button aria-label="Remove filter">×</button>
    </span>
    <button class="clear-all">Clear All</button>
  </div>

  <!-- Filter Groups -->
  <details open>
    <summary>Brand <span class="count">(12)</span></summary>
    <div class="filter-options">
      <label class="filter-option">
        <input type="checkbox" name="brand" value="lg" />
        <span>LG</span>
        <span class="result-count">(45)</span>
      </label>
    </div>
  </details>
</aside>
```

**Filter State in URL:**
```typescript
// Update URL without reload
function updateFilters(filters: FilterState) {
  const params = new URLSearchParams(filters);
  history.pushState(null, '', `?${params}`);
  fetchProducts(filters);
}

// Restore on page load
const params = new URLSearchParams(location.search);
const filters = Object.fromEntries(params);
```

**Common Mistakes:**
- No result counts (users hit dead ends)
- Filters reset on navigation
- No "Clear All" option
- Filters not in URL (can't share)

---

### Skill: Quick View Modal

**Problem:** Users must navigate to product page just to see basic details.

**When to Use:** Category pages with comparison shopping behavior.

**Rules:**
1. Include: image, name, price, rating, 3-4 key specs
2. One-click add to cart
3. Link to full product page
4. Load async (don't slow page)
5. Focus trap and keyboard accessible

**Content Priority:**
1. Product image (single hero)
2. Name and price
3. Rating summary
4. Key specifications (3-4)
5. Add to cart button
6. "View Full Details" link

**Implementation:**
```html
<dialog class="quick-view-modal" aria-labelledby="qv-title">
  <div class="qv-layout grid grid-cols-2 gap-6">
    <div class="qv-gallery">
      <img src="product-main.jpg" alt="Product" />
    </div>
    
    <div class="qv-info">
      <h2 id="qv-title">LG 12000 BTU Window AC</h2>
      <div class="qv-rating">★★★★☆ (234 reviews)</div>
      <div class="qv-price">$449.99</div>
      
      <dl class="qv-specs">
        <dt>BTU</dt><dd>12,000</dd>
        <dt>Coverage</dt><dd>550 sq ft</dd>
        <dt>Energy Rating</dt><dd>A++</dd>
      </dl>
      
      <button class="add-to-cart-btn">Add to Cart</button>
      <a href="/products/lg-12000-btu">View Full Details</a>
    </div>
  </div>
  
  <button class="close-modal" aria-label="Close">×</button>
</dialog>
```

**Common Mistakes:**
- Too much content (becomes full page)
- No link to full details
- Not keyboard accessible
- Slow to load

---

### Skill: Product Card Hierarchy

**Problem:** Product cards lack clear visual priority; users can't scan quickly.

**When to Use:** All product listings.

**Rules:**
1. Image: largest element, primary attention
2. Name: largest text, identification
3. Price: high contrast, key decision factor
4. Rating: social proof, visible but secondary
5. CTA: visible on hover (desktop) or always (mobile)

**Visual Priority Order:**
```
┌─────────────────────┐
│ [     IMAGE     ]   │ ← Primary attention
│ [Badge: SALE]       │ ← Urgency overlay
├─────────────────────┤
│ Product Name        │ ← Identification
│ Brand               │ ← Secondary
│ ★★★★☆ (42)         │ ← Social proof
│ $299 ̶$̶3̶9̶9̶         │ ← Price decision
│ [Add to Cart]       │ ← Action
└─────────────────────┘
```

**Implementation:**
```html
<article class="product-card">
  <a href="/products/lg-ac" class="card-link">
    <div class="card-image-wrapper">
      <img src="product.jpg" alt="LG 12000 BTU AC" loading="lazy" />
      <span class="badge badge-sale">SALE</span>
    </div>
    
    <div class="card-info">
      <h3 class="product-name">LG 12000 BTU Window AC</h3>
      <span class="product-brand">LG</span>
      <div class="product-rating">★★★★☆ (234)</div>
      <div class="product-price">
        <span class="price-current">$449.99</span>
        <span class="price-original">$549.99</span>
      </div>
    </div>
  </a>
  
  <button class="quick-add" aria-label="Add to cart">
    Add to Cart
  </button>
</article>
```

**Common Mistakes:**
- Image not dominant
- Price buried or hard to find
- No visual hierarchy (everything same weight)
- CTA hidden on mobile

---

## 3.2 Cart & Checkout Skills

### Skill: Mini-Cart Drawer UX

**Problem:** Adding to cart takes users away from shopping flow.

**When to Use:** Default cart interaction for desktop/tablet.

**Rules:**
1. Slide in from right (standard e-commerce pattern)
2. Show cart summary without page load
3. Allow quantity edits inline
4. Include shipping progress ("Add $24 for free shipping")
5. Show upsell suggestions (1-2 items max)

**Drawer Content:**
1. Header: "Your Cart (X items)" + close button
2. Free shipping progress bar
3. Cart items (image, name, variant, price, quantity, remove)
4. Upsell section (optional)
5. Subtotal + tax note
6. View Cart + Checkout buttons
7. Payment icons

**Implementation:**
```html
<aside class="cart-drawer" aria-label="Shopping cart" aria-hidden="true">
  <div class="cart-header">
    <h2>Your Cart (3 items)</h2>
    <button class="close-drawer" aria-label="Close cart">×</button>
  </div>
  
  <div class="cart-body">
    <!-- Shipping Progress -->
    <div class="shipping-progress">
      <div class="progress-bar" style="width: 75%"></div>
      <span>Add $24.01 more for FREE shipping!</span>
    </div>
    
    <!-- Cart Items -->
    <ul class="cart-items">
      <li class="cart-item">
        <img src="product.jpg" alt="LG Window AC" />
        <div class="item-details">
          <a href="/products/lg-ac">LG 12K BTU AC</a>
          <span>Color: White</span>
          <span>$449.99</span>
        </div>
        <div class="item-quantity">
          <button aria-label="Decrease">−</button>
          <input type="number" value="1" min="1" />
          <button aria-label="Increase">+</button>
        </div>
        <button aria-label="Remove item">🗑</button>
      </li>
    </ul>
    
    <!-- Upsell -->
    <div class="cart-upsell">
      <h3>Complete Your Purchase</h3>
      <div class="upsell-item">
        <img src="bracket.jpg" alt="Support Bracket" />
        <span>AC Support Bracket</span>
        <span>$29.99</span>
        <button>Add</button>
      </div>
    </div>
  </div>
  
  <div class="cart-footer">
    <div class="cart-totals">
      <div class="subtotal">
        <span>Subtotal:</span>
        <span>$479.98</span>
      </div>
      <p class="tax-note">Shipping & taxes calculated at checkout</p>
    </div>
    
    <a href="/cart" class="view-cart-btn">View Cart</a>
    <a href="/checkout" class="checkout-btn">Checkout</a>
    
    <div class="payment-icons">
      <img src="visa.svg" alt="Visa" />
      <img src="mastercard.svg" alt="Mastercard" />
    </div>
  </div>
</aside>
```

**Common Mistakes:**
- No shipping progress (missed upsell opportunity)
- Can't edit quantity (must go to cart page)
- No upsells (missed AOV opportunity)
- No payment icons (trust signals)

---

### Skill: Checkout Progress Indication

**Problem:** Users don't know how far along they are or what's next.

**When to Use:** Multi-step checkouts (3+ steps).

**Rules:**
1. Show all steps upfront
2. Indicate current step clearly
3. Allow back navigation
4. Show completion checkmarks
5. Optional: show time estimate

**Progress Patterns:**
| Pattern | Best For |
|---------|----------|
| Numbered steps | Desktop, 3-5 steps |
| Breadcrumb trail | Simple checkouts |
| Progress bar | Single-page checkout |
| Accordion | Long forms |

**Implementation:**
```html
<nav class="checkout-progress" aria-label="Checkout steps">
  <ol class="progress-steps">
    <li class="step completed" aria-current="false">
      <span class="step-number">✓</span>
      <span class="step-label">Cart</span>
    </li>
    <li class="step current" aria-current="step">
      <span class="step-number">2</span>
      <span class="step-label">Shipping</span>
    </li>
    <li class="step upcoming">
      <span class="step-number">3</span>
      <span class="step-label">Payment</span>
    </li>
    <li class="step upcoming">
      <span class="step-number">4</span>
      <span class="step-label">Review</span>
    </li>
  </ol>
  
  <div class="progress-bar-visual">
    <div class="progress-fill" style="width: 40%"></div>
  </div>
</nav>
```

**Common Mistakes:**
- No progress indicator (users feel lost)
- Can't go back (frustrating)
- Steps not clearly labeled
- Progress resets on error

---

### Skill: Checkout Error Recovery

**Problem:** Payment errors frustrate users; they abandon checkout.

**When to Use:** All checkout error scenarios.

**Rules:**
1. Explain what went wrong (plain language)
2. Keep all form data
3. Suggest specific fixes
4. Offer alternatives
5. Provide support contact

**Implementation:**
```html
<div class="payment-error" role="alert">
  <div class="error-icon">⚠️</div>
  <div class="error-content">
    <h4>Payment Failed</h4>
    <p>Your card was declined. This could be due to:</p>
    <ul>
      <li>Insufficient funds</li>
      <li>Card expired</li>
      <li>Incorrect card details</li>
    </ul>
  </div>
  <div class="error-actions">
    <button>Try Again</button>
    <button>Use Different Card</button>
    <button>Pay with PayPal</button>
    <a href="/contact">Contact Support</a>
  </div>
</div>
```

**Recovery Actions:**
1. Try Again (same card)
2. Edit Card Details
3. Use Different Card
4. Alternative Payment (PayPal, etc.)
5. Contact Support

**Common Mistakes:**
- "Payment failed" with no explanation
- Clearing form data
- No alternative payment options
- No support contact

---

## 3.3 Trust & Conversion Skills

### Skill: Strategic Trust Signal Placement

**Problem:** Users don't trust the site enough to complete purchase.

**When to Use:** Throughout purchase journey, concentrated near CTAs.

**Trust Signal Categories:**
1. **Transactional:** Secure checkout badge, payment logos, SSL
2. **Product:** Warranty, certifications (Energy Star), authorized dealer
3. **Service:** Free shipping threshold, return policy, customer service
4. **Social:** Star ratings, review count, verified purchase badges

**Placement Strategy:**
```
Product Page:
├── Near price: Rating + review count
├── Near CTA: Free shipping, returns, warranty
├── Below CTA: Payment icons, secure checkout
└── Reviews section: Full reviews with verification

Cart:
├── Header: Trust badges
├── Items: Product ratings reminder
└── Footer: Payment icons, policies

Checkout:
├── Header: Progress + security
├── Payment section: SSL badge, card logos
└── Submit button: Security reassurance
```

**Implementation:**
```html
<!-- Near Add to Cart -->
<div class="trust-signals">
  <div class="signal">
    <span class="icon">✓</span>
    <span>Free Shipping over $99</span>
  </div>
  <div class="signal">
    <span class="icon">↩</span>
    <span>30-Day Returns</span>
  </div>
  <div class="signal">
    <span class="icon">🛡</span>
    <span>2-Year Warranty</span>
  </div>
</div>

<!-- Payment Section -->
<div class="payment-security">
  <span class="lock-icon">🔒</span>
  <span>Your payment is encrypted and secure</span>
</div>

<div class="payment-icons">
  <img src="visa.svg" alt="Visa accepted" />
  <img src="mastercard.svg" alt="Mastercard accepted" />
  <img src="ssl-secure.svg" alt="SSL Secure" />
</div>
```

**Common Mistakes:**
- Trust signals only on homepage
- No signals near payment input
- Generic badges (specific is better)
- Too many badges (dilutes trust)

---

### Skill: Ethical Urgency Indicators

**Problem:** Users delay purchase; conversion drops.

**When to Use:** When urgency is REAL (not fabricated).

**Rules:**
1. Only show real data (actual stock, real deadlines)
2. Be specific ("Only 4 left" not "Low stock")
3. Provide context (why is it urgent?)
4. Don't pressure first-time visitors
5. Allow dismissal

**Ethical Urgency Types:**
| Type | Ethical Use | Unethical Use |
|------|-------------|---------------|
| Stock count | Real inventory < 5 | Always shows "2 left" |
| Sale timer | Actual sale end date | Perpetual countdown |
| Delivery cutoff | Real shipping deadline | Fake urgency |
| Popularity | Real recent purchases | Made-up numbers |

**Implementation:**
```html
<!-- Real Low Stock -->
<div class="urgency-indicator low-stock">
  <span class="icon">⚠️</span>
  <span>Only 4 left in stock - order soon</span>
</div>

<!-- Real Sale Timer -->
<div class="urgency-indicator sale-timer">
  <span class="icon">🕐</span>
  <span>Summer Sale ends in:</span>
  <div class="countdown">
    <span><strong>02</strong> days</span>
    <span><strong>14</strong> hrs</span>
  </div>
</div>

<!-- Real Delivery Cutoff -->
<div class="urgency-indicator delivery">
  <span class="icon">🚚</span>
  <span>
    Order within <strong>2h 34min</strong> for delivery by 
    <strong>Tomorrow, Jan 25</strong>
  </span>
</div>
```

**Common Mistakes:**
- Fake scarcity ("Only 2 left!" always)
- Perpetual "Sale ends today!"
- Countdown that resets
- Made-up popularity numbers

---

### Skill: Review Display Optimization

**Problem:** Users don't trust product quality without social proof.

**When to Use:** All product pages.

**Rules:**
1. Show rating summary at top (visible without scroll)
2. Display rating distribution chart
3. Highlight "Verified Purchase" badges
4. Allow filtering by rating
5. Show most helpful reviews first

**Review Display Structure:**
```html
<div class="rating-summary">
  <div class="rating-main">
    <span class="rating-score">4.5</span>
    <div class="stars" aria-label="4.5 out of 5 stars">★★★★☆</div>
    <span class="review-count">Based on 234 reviews</span>
  </div>
  
  <div class="rating-distribution">
    <div class="rating-bar">
      <span>5 stars</span>
      <div class="bar"><div class="fill" style="width: 65%"></div></div>
      <span>65%</span>
    </div>
    <div class="rating-bar">
      <span>4 stars</span>
      <div class="bar"><div class="fill" style="width: 20%"></div></div>
      <span>20%</span>
    </div>
    <!-- ... -->
  </div>
</div>

<div class="review-filters">
  <button class="active">All Reviews</button>
  <button>5 Star</button>
  <button>With Photos</button>
  <button>Verified Only</button>
</div>

<article class="review">
  <header>
    <span class="reviewer-name">John D.</span>
    <span class="verified-badge">✓ Verified Purchase</span>
    <div class="stars">★★★★★</div>
    <time>January 15, 2025</time>
  </header>
  <h4 class="review-title">Perfect for my apartment!</h4>
  <p class="review-body">This AC unit is incredibly quiet and...</p>
  <div class="review-photos">
    <img src="review-photo.jpg" alt="Customer photo" />
  </div>
  <footer>
    <span>Was this helpful?</span>
    <button>Yes (12)</button>
    <button>No (1)</button>
  </footer>
</article>
```

**Common Mistakes:**
- Reviews hidden below fold
- No rating distribution
- No "Verified Purchase" indicator
- Can't filter reviews
- No review photos

---

# Quick Reference

## Animation Timing Cheat Sheet

| Use Case | Duration | Easing |
|----------|----------|--------|
| Button hover | 150ms | ease-out |
| Card hover lift | 250ms | ease-out |
| Fade in content | 400ms | ease-out |
| Modal open | 300ms | ease-out |
| Slide in panel | 300ms | ease-out |
| Page transition | 400ms | ease-in-out |
| Stagger delay | 80ms each | ease-out |
| Skeleton shimmer | 1500ms | linear |
| Micro-feedback | 150ms | ease-out |

## Safe Animation Properties

**Always Safe (GPU-accelerated):**
- `transform` (translate, scale, rotate)
- `opacity`

**Sometimes Safe:**
- `filter` (small elements only)
- `clip-path`

**Never Animate:**
- `width`, `height`
- `top`, `left`, `right`, `bottom`
- `margin`, `padding`
- `font-size`
- `border-width`

## Spacing Quick Reference

| Context | Value |
|---------|-------|
| Related inline elements | 4-8px |
| Form field spacing | 16-24px |
| Card internal padding | 16-24px |
| Section spacing | 48-80px |
| Page section dividers | 80-120px |
| Touch target minimum | 44×44px |

## Accessibility Checklist

- [ ] All interactive elements keyboard accessible
- [ ] Focus visible at all times
- [ ] Color not sole indicator
- [ ] `prefers-reduced-motion` supported
- [ ] ARIA labels on interactive elements
- [ ] Heading hierarchy correct (h1→h2→h3)
- [ ] Form fields have labels
- [ ] Error messages descriptive
- [ ] Dynamic updates announced

---

*Document created: January 2025*
*For: ClimaSite HVAC E-Commerce Platform*
*Version: 1.0*
