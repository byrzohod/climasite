# Premium E-Commerce UX Patterns

> A comprehensive guide to best-in-class e-commerce user experience patterns that drive conversions and create exceptional shopping experiences.

## Table of Contents

1. [Product Discovery UX](#1-product-discovery-ux)
2. [Product Detail Page Patterns](#2-product-detail-page-patterns)
3. [Cart & Checkout UX](#3-cart--checkout-ux)
4. [Trust & Conversion Patterns](#4-trust--conversion-patterns)
5. [Navigation & Wayfinding](#5-navigation--wayfinding)
6. [Implementation Checklist](#6-implementation-checklist)

---

## 1. Product Discovery UX

### 1.1 Smart Filter Interactions

#### Pattern: Faceted Filtering with Live Updates

**When to Use:** Category pages with 20+ products and multiple filterable attributes.

**UX Benefits:**
- Reduces cognitive load by showing only relevant options
- Enables rapid product narrowing without page reloads
- Shows result counts to prevent dead-ends

**Implementation:**

```html
<!-- Filter Panel Structure -->
<aside class="filter-panel" aria-label="Product filters">
  <!-- Active Filters Summary -->
  <div class="active-filters">
    <span class="filter-chip" data-testid="filter-chip-brand-lg">
      Brand: LG <button aria-label="Remove filter">×</button>
    </span>
    <button class="clear-all">Clear All</button>
  </div>

  <!-- Collapsible Filter Groups -->
  <details open>
    <summary>
      Brand <span class="count">(12)</span>
    </summary>
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

**Best Practices:**
| Do | Don't |
|-----|-------|
| Show result count next to each filter option | Hide count, leading to dead ends |
| Update counts dynamically as filters change | Require form submission to see results |
| Remember filter state on back navigation | Reset filters on every navigation |
| Use URL parameters for shareable filtered views | Store filter state only in memory |
| Provide "Clear All" for quick reset | Force users to deselect individually |

**Examples:**
- **Wayfair:** Collapsible filter groups with visual swatches for colors
- **Home Depot:** Range sliders for price with histogram distribution
- **Best Buy:** "Shop by" sections with popular filter combinations

---

#### Pattern: Sort Controls with Smart Defaults

**When to Use:** Any product listing with more than 12 items.

**Implementation Considerations:**

```typescript
// Recommended sort options for HVAC e-commerce
const sortOptions = [
  { value: 'relevance', label: 'Relevance' },        // Default for search
  { value: 'popular', label: 'Most Popular' },       // Default for category
  { value: 'price-asc', label: 'Price: Low to High' },
  { value: 'price-desc', label: 'Price: High to Low' },
  { value: 'rating', label: 'Highest Rated' },
  { value: 'newest', label: 'Newest Arrivals' },
  { value: 'energy-rating', label: 'Energy Efficiency' }, // Industry-specific
];
```

**Smart Default Logic:**
- Search results: Sort by relevance
- Category browsing: Sort by popularity/bestsellers
- Sale/Clearance: Sort by discount percentage
- New arrivals section: Sort by date added

---

### 1.2 Quick View Modal

#### Pattern: Product Preview Without Navigation

**When to Use:** Category pages where users are comparing multiple products.

**UX Benefits:**
- 40% faster product evaluation
- Reduces back-button fatigue
- Keeps browsing context intact

**Implementation:**

```html
<!-- Quick View Trigger -->
<button 
  class="quick-view-btn"
  data-testid="quick-view-trigger"
  aria-label="Quick view LG 12000 BTU Air Conditioner"
>
  Quick View
</button>

<!-- Quick View Modal Content -->
<dialog class="quick-view-modal" aria-labelledby="qv-title">
  <div class="qv-layout">
    <!-- Image Gallery (simplified) -->
    <div class="qv-gallery">
      <img src="product-main.jpg" alt="Product image" />
      <div class="thumbnail-strip">...</div>
    </div>
    
    <!-- Essential Info -->
    <div class="qv-info">
      <h2 id="qv-title">LG 12000 BTU Window AC</h2>
      <div class="qv-rating">★★★★☆ (234 reviews)</div>
      <div class="qv-price">$449.99</div>
      
      <!-- Key Specs -->
      <dl class="qv-specs">
        <dt>BTU</dt><dd>12,000</dd>
        <dt>Coverage</dt><dd>550 sq ft</dd>
        <dt>Energy Rating</dt><dd>A++</dd>
      </dl>
      
      <!-- Quick Actions -->
      <button class="add-to-cart-btn" data-testid="qv-add-to-cart">
        Add to Cart
      </button>
      <a href="/products/lg-12000-btu" class="view-full-details">
        View Full Details
      </a>
    </div>
  </div>
  
  <button class="close-modal" aria-label="Close quick view">×</button>
</dialog>
```

**Content Priority for Quick View:**
1. Product image (single hero, not full gallery)
2. Name and price
3. Rating summary
4. 3-4 key specifications
5. Add to cart button
6. Link to full product page

---

### 1.3 Product Comparison Feature

#### Pattern: Side-by-Side Comparison Table

**When to Use:** Technical products with spec-heavy purchase decisions (HVAC, appliances, electronics).

**UX Benefits:**
- Reduces decision paralysis
- Highlights differentiators clearly
- Increases confidence in purchase decision

**Implementation:**

```html
<!-- Comparison Table -->
<table class="comparison-table" role="table">
  <thead>
    <tr>
      <th scope="col">Feature</th>
      <th scope="col">
        <img src="product1.jpg" alt="LG 12K BTU" />
        <span>LG 12K BTU</span>
        <span class="price">$449</span>
      </th>
      <th scope="col">
        <img src="product2.jpg" alt="Samsung 14K BTU" />
        <span>Samsung 14K BTU</span>
        <span class="price">$549</span>
      </th>
      <th scope="col">
        <img src="product3.jpg" alt="Carrier 12K BTU" />
        <span>Carrier 12K BTU</span>
        <span class="price">$499</span>
      </th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <th scope="row">Cooling Capacity</th>
      <td>12,000 BTU</td>
      <td class="highlight">14,000 BTU</td>
      <td>12,000 BTU</td>
    </tr>
    <tr>
      <th scope="row">Energy Rating</th>
      <td class="highlight">A++</td>
      <td>A+</td>
      <td class="highlight">A++</td>
    </tr>
    <!-- Highlight best value per row -->
  </tbody>
</table>

<!-- Sticky Add to Cart Row -->
<div class="comparison-actions sticky">
  <button data-testid="compare-add-1">Add LG to Cart</button>
  <button data-testid="compare-add-2">Add Samsung to Cart</button>
  <button data-testid="compare-add-3">Add Carrier to Cart</button>
</div>
```

**Best Practices:**
- Maximum 4 products for comparison
- Highlight "winner" in each row
- Pin product headers on scroll
- Show "Remove" option per product
- Remember comparison list across pages

---

### 1.4 Recently Viewed Tracking

#### Pattern: Persistent Product History

**When to Use:** Always - essential for multi-session shopping journeys.

**Implementation:**

```typescript
// Recently Viewed Service
export class RecentlyViewedService {
  private readonly STORAGE_KEY = 'recently_viewed';
  private readonly MAX_ITEMS = 12;
  
  addProduct(product: ProductSummary): void {
    const viewed = this.getProducts();
    // Remove if already exists (will be re-added to front)
    const filtered = viewed.filter(p => p.id !== product.id);
    // Add to front, limit to MAX_ITEMS
    const updated = [product, ...filtered].slice(0, this.MAX_ITEMS);
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updated));
  }
  
  getProducts(): ProductSummary[] {
    const stored = localStorage.getItem(this.STORAGE_KEY);
    return stored ? JSON.parse(stored) : [];
  }
}
```

**Placement Options:**
- Below product details on PDP
- On homepage for returning visitors
- In cart sidebar as "Continue Shopping" suggestions
- In empty cart state

---

### 1.5 "Complete the Look" Suggestions

#### Pattern: Complementary Product Bundles

**When to Use:** Products that have natural accessories or complementary items.

**UX Benefits:**
- Increases average order value by 15-25%
- Reduces separate shopping trips
- Provides helpful purchase guidance

**Implementation for HVAC:**

```html
<!-- Complete Your Installation Section -->
<section class="complete-the-look" aria-labelledby="complete-heading">
  <h2 id="complete-heading">Complete Your Installation</h2>
  
  <div class="bundle-items">
    <div class="main-product selected">
      <img src="ac-unit.jpg" alt="LG Window AC" />
      <span>LG 12K BTU AC</span>
      <span class="price">$449.99</span>
    </div>
    
    <span class="plus-icon">+</span>
    
    <div class="add-on-product" data-testid="addon-support-bracket">
      <input type="checkbox" id="addon-1" checked />
      <label for="addon-1">
        <img src="support-bracket.jpg" alt="Support Bracket" />
        <span>Window Support Bracket</span>
        <span class="price">$29.99</span>
      </label>
    </div>
    
    <span class="plus-icon">+</span>
    
    <div class="add-on-product" data-testid="addon-weatherseal">
      <input type="checkbox" id="addon-2" />
      <label for="addon-2">
        <img src="weatherseal.jpg" alt="Weather Seal Kit" />
        <span>Weather Seal Kit</span>
        <span class="price">$19.99</span>
      </label>
    </div>
  </div>
  
  <div class="bundle-summary">
    <span class="bundle-total">Bundle Total: $499.97</span>
    <span class="bundle-savings">Save $15.00 when bought together</span>
    <button class="add-bundle-btn" data-testid="add-bundle">
      Add Bundle to Cart
    </button>
  </div>
</section>
```

**HVAC-Specific Bundles:**
- AC Unit + Support Bracket + Weather Seal
- Heat Pump + Thermostat + Installation Kit
- Portable AC + Exhaust Hose Extension + Drip Tray
- Split System + Line Set + Disconnect Box

---

## 2. Product Detail Page Patterns

### 2.1 Image Gallery Interactions

#### Pattern: Multi-View Gallery with Zoom

**When to Use:** All product detail pages.

**Implementation Structure:**

```html
<div class="product-gallery" data-testid="product-gallery">
  <!-- Main Image with Zoom -->
  <div class="main-image-container">
    <figure 
      class="zoomable"
      data-testid="main-image"
      role="img"
      aria-label="Product main image, hover to zoom"
    >
      <img 
        src="product-large.jpg" 
        alt="LG 12000 BTU Window AC - Front View"
        loading="eager"
      />
      <div class="zoom-lens" aria-hidden="true"></div>
    </figure>
    
    <!-- Image Type Badges -->
    <div class="image-badges">
      <span class="badge-360">360°</span>
      <span class="badge-video">Video</span>
    </div>
    
    <!-- Navigation Arrows -->
    <button class="gallery-prev" aria-label="Previous image">‹</button>
    <button class="gallery-next" aria-label="Next image">›</button>
  </div>
  
  <!-- Thumbnail Strip -->
  <div class="thumbnail-strip" role="tablist" aria-label="Product images">
    <button 
      role="tab" 
      aria-selected="true"
      data-testid="thumb-1"
    >
      <img src="thumb-front.jpg" alt="Front view" />
    </button>
    <button role="tab" data-testid="thumb-2">
      <img src="thumb-side.jpg" alt="Side view" />
    </button>
    <button role="tab" data-testid="thumb-3">
      <img src="thumb-controls.jpg" alt="Control panel" />
    </button>
    <button role="tab" data-testid="thumb-video">
      <img src="thumb-video.jpg" alt="Product video" />
      <span class="play-icon" aria-hidden="true">▶</span>
    </button>
  </div>
</div>
```

**Zoom Implementations:**

| Type | Best For | UX Consideration |
|------|----------|------------------|
| Hover Zoom | Desktop with large screens | Show zoomed area in adjacent panel |
| Click-to-Zoom | Touch devices | Opens fullscreen lightbox |
| Pinch Zoom | Mobile | Native gesture in lightbox |
| 360° Spin | Products with multiple angles | Use drag or arrow controls |

**Image Requirements:**
- Main image: 1200x1200px minimum for zoom
- Thumbnails: 100x100px
- Alt text: Descriptive, unique per image
- Lazy load below-fold thumbnails

---

### 2.2 Variant Selection

#### Pattern: Visual Variant Selector with Instant Feedback

**When to Use:** Products with multiple options (color, size, capacity).

**Implementation:**

```html
<div class="variant-selector" data-testid="variant-selector">
  <!-- Color Selection (Visual Swatches) -->
  <fieldset class="variant-group">
    <legend>
      Color: <span class="selected-value">Arctic White</span>
    </legend>
    <div class="swatch-options" role="radiogroup">
      <label class="swatch selected" data-testid="color-white">
        <input type="radio" name="color" value="white" checked />
        <span 
          class="swatch-visual" 
          style="background: #FFFFFF"
          aria-label="Arctic White"
        ></span>
      </label>
      <label class="swatch" data-testid="color-black">
        <input type="radio" name="color" value="black" />
        <span 
          class="swatch-visual" 
          style="background: #1F2937"
          aria-label="Midnight Black"
        ></span>
      </label>
      <label class="swatch out-of-stock" data-testid="color-silver">
        <input type="radio" name="color" value="silver" disabled />
        <span 
          class="swatch-visual" 
          style="background: #9CA3AF"
          aria-label="Silver - Out of stock"
        ></span>
        <span class="oos-indicator">×</span>
      </label>
    </div>
  </fieldset>

  <!-- Size/Capacity Selection (Button Group) -->
  <fieldset class="variant-group">
    <legend>
      BTU Capacity: <span class="selected-value">12,000 BTU</span>
    </legend>
    <div class="button-options" role="radiogroup">
      <label class="variant-btn" data-testid="btu-8000">
        <input type="radio" name="btu" value="8000" />
        <span class="btn-content">
          <span class="primary">8,000 BTU</span>
          <span class="secondary">Up to 350 sq ft</span>
        </span>
      </label>
      <label class="variant-btn selected" data-testid="btu-12000">
        <input type="radio" name="btu" value="12000" checked />
        <span class="btn-content">
          <span class="primary">12,000 BTU</span>
          <span class="secondary">Up to 550 sq ft</span>
        </span>
      </label>
      <label class="variant-btn" data-testid="btu-15000">
        <input type="radio" name="btu" value="15000" />
        <span class="btn-content">
          <span class="primary">15,000 BTU</span>
          <span class="secondary">Up to 800 sq ft</span>
          <span class="price-diff">+$100</span>
        </span>
      </label>
    </div>
  </fieldset>
</div>
```

**Feedback Mechanisms:**
1. Update main product image when color changes
2. Show price difference for variants
3. Cross out unavailable combinations
4. Update stock status per variant

---

### 2.3 Stock & Availability Indicators

#### Pattern: Real-Time Availability with Urgency

**When to Use:** Always show stock status; add urgency for low stock.

**Implementation:**

```html
<!-- Stock Status Variants -->

<!-- In Stock (High Quantity) -->
<div class="stock-status in-stock" data-testid="stock-status">
  <span class="status-icon">✓</span>
  <span class="status-text">In Stock</span>
  <span class="delivery-estimate">
    Order within 2h 34m for next-day delivery
  </span>
</div>

<!-- Low Stock (Urgency) -->
<div class="stock-status low-stock" data-testid="stock-status-low">
  <span class="status-icon">⚠</span>
  <span class="status-text">Only 3 left in stock</span>
  <span class="urgency-badge">High Demand</span>
</div>

<!-- Out of Stock -->
<div class="stock-status out-of-stock" data-testid="stock-status-oos">
  <span class="status-icon">✗</span>
  <span class="status-text">Out of Stock</span>
  <button class="notify-btn" data-testid="notify-me">
    Notify When Available
  </button>
  <span class="restock-estimate">Expected: March 15</span>
</div>

<!-- Available for Pre-Order -->
<div class="stock-status preorder" data-testid="stock-status-preorder">
  <span class="status-icon">📅</span>
  <span class="status-text">Pre-Order Available</span>
  <span class="ship-date">Ships: April 1, 2025</span>
</div>
```

**Urgency Best Practices:**
| Do | Don't |
|-----|-------|
| Show real stock numbers under 5 | Fake scarcity ("Only 2 left!" always) |
| Display restock dates if known | Leave users without options |
| Offer "Notify Me" for OOS items | Just say "Out of Stock" |
| Show delivery countdown timers | Use misleading countdown timers |

---

### 2.4 Trust Signals Placement

#### Pattern: Strategic Trust Signal Distribution

**When to Use:** Throughout PDP, concentrated near price and CTA.

**Placement Map:**

```
┌──────────────────────────────────────────────────────┐
│  [Breadcrumbs]                                       │
├───────────────────────┬──────────────────────────────┤
│                       │  Product Title               │
│                       │  ★★★★☆ 4.5 (234 reviews)    │ ← Social Proof
│   Product Image       │  $449.99                     │
│   Gallery             │                              │
│                       │  [Add to Cart]               │
│                       │                              │
│                       │  ✓ Free Shipping over $99   │ ← Trust Signals
│                       │  ✓ 30-Day Returns           │   (near CTA)
│                       │  ✓ 2-Year Warranty          │
│                       │  🔒 Secure Checkout          │
├───────────────────────┴──────────────────────────────┤
│  [Trust Badges: Visa, Mastercard, PayPal, SSL]      │ ← Payment Trust
├──────────────────────────────────────────────────────┤
│  Product Description Tabs                            │
│  [Description] [Specifications] [Reviews]           │
├──────────────────────────────────────────────────────┤
│  Customer Reviews Section                            │ ← Social Proof
│  "Verified Purchase" badges                          │
└──────────────────────────────────────────────────────┘
```

**Trust Signal Categories:**

1. **Transactional Trust**
   - Secure checkout badge
   - Payment method logos
   - SSL/encryption indicators

2. **Product Trust**
   - Warranty information
   - Certification badges (Energy Star, CE, UL)
   - "Authorized Dealer" badge

3. **Service Trust**
   - Free shipping threshold
   - Return policy summary
   - Customer service availability

4. **Social Trust**
   - Star ratings
   - Review count
   - "Verified Purchase" labels

---

### 2.5 Sticky Add-to-Cart Bar

#### Pattern: Persistent Purchase Action

**When to Use:** Long product pages, especially on mobile.

**Implementation:**

```html
<!-- Sticky Bar (appears on scroll past main CTA) -->
<div 
  class="sticky-cart-bar"
  data-testid="sticky-cart-bar"
  aria-label="Quick add to cart"
>
  <div class="sticky-product-info">
    <img src="product-thumb.jpg" alt="" aria-hidden="true" />
    <div class="sticky-details">
      <span class="sticky-name">LG 12K BTU Window AC</span>
      <span class="sticky-price">$449.99</span>
    </div>
  </div>
  
  <div class="sticky-actions">
    <div class="sticky-quantity">
      <button aria-label="Decrease quantity">−</button>
      <span>1</span>
      <button aria-label="Increase quantity">+</button>
    </div>
    <button class="sticky-add-btn" data-testid="sticky-add-to-cart">
      Add to Cart
    </button>
  </div>
</div>
```

**Trigger Logic:**
```typescript
// Show sticky bar when main CTA scrolls out of view
const mainCTA = document.querySelector('[data-testid="main-add-to-cart"]');
const stickyBar = document.querySelector('.sticky-cart-bar');

const observer = new IntersectionObserver(
  ([entry]) => {
    stickyBar.classList.toggle('visible', !entry.isIntersecting);
  },
  { threshold: 0 }
);

observer.observe(mainCTA);
```

**Best Practices:**
- Show only when main CTA is not visible
- Include product thumbnail for context
- Keep price visible
- Animate in smoothly (slide up/fade)
- Don't obscure content (small height)

---

## 3. Cart & Checkout UX

### 3.1 Mini-Cart / Drawer Pattern

#### Pattern: Slide-Out Cart Drawer

**When to Use:** Default cart interaction for desktop and tablet.

**UX Benefits:**
- Keeps user in shopping context
- Shows cart summary without page load
- Enables quick edits before checkout

**Implementation:**

```html
<!-- Cart Drawer -->
<aside 
  class="cart-drawer"
  data-testid="cart-drawer"
  aria-label="Shopping cart"
  aria-hidden="true"
>
  <div class="cart-drawer-header">
    <h2>Your Cart (3 items)</h2>
    <button 
      class="close-drawer" 
      aria-label="Close cart"
      data-testid="close-cart-drawer"
    >
      ×
    </button>
  </div>
  
  <div class="cart-drawer-body">
    <!-- Free Shipping Progress -->
    <div class="shipping-progress" data-testid="shipping-progress">
      <div class="progress-bar" style="width: 75%"></div>
      <span>Add $24.01 more for FREE shipping!</span>
    </div>
    
    <!-- Cart Items -->
    <ul class="cart-items" role="list">
      <li class="cart-item" data-testid="cart-item-1">
        <img src="product.jpg" alt="LG Window AC" />
        <div class="item-details">
          <a href="/products/lg-ac" class="item-name">LG 12K BTU AC</a>
          <span class="item-variant">Color: White</span>
          <span class="item-price">$449.99</span>
        </div>
        <div class="item-quantity">
          <button aria-label="Decrease">−</button>
          <input type="number" value="1" min="1" aria-label="Quantity" />
          <button aria-label="Increase">+</button>
        </div>
        <button class="remove-item" aria-label="Remove item">🗑</button>
      </li>
    </ul>
    
    <!-- Cart Upsell -->
    <div class="cart-upsell" data-testid="cart-upsell">
      <h3>Frequently Bought Together</h3>
      <div class="upsell-item">
        <img src="bracket.jpg" alt="Support Bracket" />
        <span>AC Support Bracket</span>
        <span class="price">$29.99</span>
        <button data-testid="add-upsell">Add</button>
      </div>
    </div>
  </div>
  
  <div class="cart-drawer-footer">
    <div class="cart-totals">
      <div class="subtotal">
        <span>Subtotal:</span>
        <span>$479.98</span>
      </div>
      <p class="tax-note">Shipping & taxes calculated at checkout</p>
    </div>
    
    <a href="/cart" class="view-cart-btn" data-testid="view-cart">
      View Cart
    </a>
    <a href="/checkout" class="checkout-btn" data-testid="checkout">
      Checkout
    </a>
    
    <!-- Payment Icons -->
    <div class="payment-icons">
      <img src="visa.svg" alt="Visa" />
      <img src="mastercard.svg" alt="Mastercard" />
      <img src="paypal.svg" alt="PayPal" />
    </div>
  </div>
</aside>

<!-- Backdrop -->
<div class="cart-drawer-backdrop" aria-hidden="true"></div>
```

**Drawer States:**
1. **Empty Cart:** Show illustration + "Continue Shopping" CTA
2. **With Items:** Full functionality
3. **Loading:** Skeleton placeholders
4. **Error:** Inline error message with retry

---

### 3.2 Cart Upsells & Cross-Sells

#### Pattern: Contextual Product Recommendations

**When to Use:** In cart drawer and cart page.

**Recommendation Types:**

| Type | Logic | Example |
|------|-------|---------|
| **Complementary** | Items that go with cart items | AC → Support Bracket |
| **Upgrade** | Higher-tier version of cart item | 12K BTU → 15K BTU |
| **Bundle** | Complete solution sets | AC + Install Kit + Warranty |
| **Popular** | Best sellers in category | Top-rated thermostats |

**Implementation:**

```html
<!-- Cart Page Upsell Section -->
<section class="cart-recommendations" aria-labelledby="rec-heading">
  <h2 id="rec-heading">Complete Your Purchase</h2>
  
  <div class="recommendation-carousel">
    <div class="rec-item" data-testid="rec-item-1">
      <img src="product.jpg" alt="Product name" />
      <div class="rec-info">
        <span class="rec-name">AC Support Bracket</span>
        <span class="rec-price">$29.99</span>
        <span class="rec-reason">Recommended for your LG AC</span>
      </div>
      <button class="add-btn" data-testid="add-rec-1">
        Add to Cart
      </button>
    </div>
  </div>
</section>
```

**Best Practices:**
- Limit to 3-4 recommendations
- Show "Why" connection to cart items
- One-click add without page reload
- Don't repeat items already in cart

---

### 3.3 Checkout Progress Indicators

#### Pattern: Multi-Step Checkout Progress

**When to Use:** Checkout flows with 3+ steps.

**Implementation:**

```html
<!-- Checkout Progress Bar -->
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
  
  <!-- Visual Progress Bar -->
  <div class="progress-bar-visual">
    <div class="progress-fill" style="width: 40%"></div>
  </div>
</nav>
```

**Progress Patterns:**

| Pattern | Best For | Description |
|---------|----------|-------------|
| **Numbered Steps** | Desktop, 3-5 steps | Clear step count and position |
| **Breadcrumb Trail** | Simple checkouts | Cart → Shipping → Payment → Done |
| **Progress Bar** | Single-page checkout | Visual percentage complete |
| **Accordion** | Long forms | Expand/collapse sections |

---

### 3.4 Form Optimization

#### Pattern: Smart Form Design

**When to Use:** All checkout forms.

**Implementation:**

```html
<form class="checkout-form" data-testid="checkout-form">
  <!-- Single Column Layout -->
  <div class="form-section">
    <h3>Contact Information</h3>
    
    <!-- Email with Autocomplete -->
    <div class="form-field">
      <label for="email">Email Address</label>
      <input 
        type="email" 
        id="email" 
        name="email"
        autocomplete="email"
        required
        data-testid="email-input"
      />
      <span class="helper-text">For order confirmation and updates</span>
    </div>
  </div>
  
  <div class="form-section">
    <h3>Shipping Address</h3>
    
    <!-- Full Name -->
    <div class="form-field">
      <label for="name">Full Name</label>
      <input 
        type="text" 
        id="name" 
        name="name"
        autocomplete="name"
        required
      />
    </div>
    
    <!-- Address Autocomplete -->
    <div class="form-field">
      <label for="address">Street Address</label>
      <input 
        type="text" 
        id="address" 
        name="address"
        autocomplete="street-address"
        required
        data-testid="address-input"
      />
      <!-- Address suggestions dropdown -->
    </div>
    
    <!-- City, State, ZIP in row -->
    <div class="form-row">
      <div class="form-field">
        <label for="city">City</label>
        <input type="text" id="city" autocomplete="address-level2" required />
      </div>
      <div class="form-field">
        <label for="state">State</label>
        <select id="state" autocomplete="address-level1" required>
          <option value="">Select...</option>
        </select>
      </div>
      <div class="form-field">
        <label for="zip">ZIP Code</label>
        <input type="text" id="zip" autocomplete="postal-code" required />
      </div>
    </div>
  </div>
  
  <!-- Inline Validation -->
  <div class="form-field has-error">
    <label for="phone">Phone Number</label>
    <input 
      type="tel" 
      id="phone" 
      autocomplete="tel"
      aria-invalid="true"
      aria-describedby="phone-error"
    />
    <span id="phone-error" class="error-message" role="alert">
      Please enter a valid phone number
    </span>
  </div>
</form>
```

**Form Best Practices:**

| Principle | Implementation |
|-----------|----------------|
| **Single Column** | Avoid side-by-side fields except City/State/ZIP |
| **Autocomplete** | Use proper `autocomplete` attributes |
| **Inline Validation** | Validate on blur, not on every keystroke |
| **Error Recovery** | Keep field values on error, highlight issues |
| **Progress Save** | Auto-save form state to prevent data loss |
| **Smart Defaults** | Pre-select country based on IP, use saved addresses |

---

### 3.5 Error Recovery

#### Pattern: Graceful Error Handling

**When to Use:** All form submissions and checkout actions.

**Implementation:**

```html
<!-- Form-Level Error Summary -->
<div 
  class="error-summary" 
  role="alert" 
  aria-live="polite"
  data-testid="error-summary"
>
  <h4>Please fix the following errors:</h4>
  <ul>
    <li><a href="#email">Email address is required</a></li>
    <li><a href="#card-number">Card number is invalid</a></li>
  </ul>
</div>

<!-- Field-Level Error -->
<div class="form-field has-error">
  <label for="card-number">Card Number</label>
  <input 
    type="text" 
    id="card-number"
    aria-invalid="true"
    aria-describedby="card-error"
    class="input-error"
  />
  <span id="card-error" class="error-message">
    This card number is invalid. Please check and try again.
  </span>
</div>

<!-- Payment Error with Recovery -->
<div class="payment-error" role="alert" data-testid="payment-error">
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
    <button data-testid="try-again">Try Again</button>
    <button data-testid="use-different-card">Use Different Card</button>
    <a href="/contact">Contact Support</a>
  </div>
</div>
```

**Error Recovery Best Practices:**
- Always provide actionable next steps
- Don't clear user data on error
- Link to specific fields with issues
- Offer alternative actions (different payment, contact support)
- Log errors for debugging (don't show technical details to users)

---

## 4. Trust & Conversion Patterns

### 4.1 Social Proof Placement

#### Pattern: Strategic Review & Rating Display

**When to Use:** Throughout the shopping journey.

**Placement Strategy:**

```
Homepage:
├── Hero → "Trusted by 50,000+ homeowners"
├── Featured Products → Star ratings visible
└── Testimonials Section → Customer quotes

Category Page:
├── Product Cards → Star ratings + review count
└── Filters → Filter by rating option

Product Page:
├── Above fold → Stars + count + "Verified Purchase" link
├── Below specs → Review summary chart
├── Review section → Full reviews with photos
└── Q&A section → Community questions

Cart:
├── Mini-cart → Product ratings reminder
└── Upsells → "4.8★ - Customers also bought"

Checkout:
├── Order summary → Trust badges
└── Payment → "10,000+ successful orders"
```

**Review Display Component:**

```html
<!-- Rating Summary -->
<div class="rating-summary" data-testid="rating-summary">
  <div class="rating-main">
    <span class="rating-score">4.5</span>
    <div class="stars" aria-label="4.5 out of 5 stars">
      ★★★★☆
    </div>
    <span class="review-count">Based on 234 reviews</span>
  </div>
  
  <!-- Rating Distribution -->
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

<!-- Individual Review -->
<article class="review" data-testid="review-1">
  <header class="review-header">
    <div class="reviewer">
      <span class="name">John D.</span>
      <span class="verified-badge">✓ Verified Purchase</span>
    </div>
    <div class="review-meta">
      <div class="stars">★★★★★</div>
      <time datetime="2025-01-15">January 15, 2025</time>
    </div>
  </header>
  
  <h4 class="review-title">Perfect for my apartment!</h4>
  <p class="review-body">This AC unit is incredibly quiet and...</p>
  
  <!-- Review Photos -->
  <div class="review-photos">
    <img src="review-photo-1.jpg" alt="Installed AC unit" />
  </div>
  
  <!-- Helpfulness -->
  <footer class="review-footer">
    <span>Was this helpful?</span>
    <button data-testid="helpful-yes">Yes (12)</button>
    <button data-testid="helpful-no">No (1)</button>
  </footer>
</article>
```

---

### 4.2 Urgency Indicators

#### Pattern: Ethical Urgency Signals

**When to Use:** When urgency is genuine (limited stock, time-bound offers).

**Ethical vs Unethical Urgency:**

| Ethical (Do This) | Unethical (Don't Do This) |
|-------------------|---------------------------|
| Show real stock counts | Fake "Only 2 left!" always |
| Actual sale end dates | Perpetual "Sale ends today!" |
| Real-time inventory updates | Static scarcity messaging |
| Genuine limited editions | Artificial limitations |

**Implementation:**

```html
<!-- Real Low Stock Warning -->
<div class="urgency-indicator low-stock" data-testid="low-stock-warning">
  <span class="icon">⚠️</span>
  <span class="message">Only 4 left in stock - order soon</span>
</div>

<!-- Genuine Time-Bound Offer -->
<div class="urgency-indicator sale-timer" data-testid="sale-timer">
  <span class="icon">🕐</span>
  <span class="message">Summer Sale ends in:</span>
  <div class="countdown" aria-live="polite">
    <span class="time-unit"><strong>02</strong> days</span>
    <span class="time-unit"><strong>14</strong> hrs</span>
    <span class="time-unit"><strong>32</strong> min</span>
  </div>
</div>

<!-- Delivery Countdown -->
<div class="urgency-indicator delivery" data-testid="delivery-countdown">
  <span class="icon">🚚</span>
  <span class="message">
    Order within <strong>2 hours 34 minutes</strong> for delivery by 
    <strong>Tomorrow, Jan 25</strong>
  </span>
</div>

<!-- Recently Purchased (Social Proof + Urgency) -->
<div class="social-urgency" data-testid="recent-purchase">
  <span class="icon">📦</span>
  <span class="message">
    <strong>15 people</strong> bought this in the last 24 hours
  </span>
</div>
```

---

### 4.3 Security Badges

#### Pattern: Payment Security Trust Indicators

**When to Use:** Near payment inputs and checkout CTAs.

**Implementation:**

```html
<!-- Security Badges Section -->
<div class="security-badges" data-testid="security-badges">
  <!-- SSL/Encryption -->
  <div class="badge" aria-label="256-bit SSL encryption">
    <img src="ssl-lock.svg" alt="" aria-hidden="true" />
    <span>256-bit SSL Secure</span>
  </div>
  
  <!-- Payment Processors -->
  <div class="payment-badges">
    <img src="visa.svg" alt="Visa accepted" />
    <img src="mastercard.svg" alt="Mastercard accepted" />
    <img src="amex.svg" alt="American Express accepted" />
    <img src="paypal.svg" alt="PayPal accepted" />
  </div>
  
  <!-- Third-Party Trust -->
  <div class="trust-seals">
    <img src="bbb-accredited.svg" alt="BBB Accredited Business" />
    <img src="google-trusted-store.svg" alt="Google Trusted Store" />
    <img src="mcafee-secure.svg" alt="McAfee Secure" />
  </div>
</div>

<!-- Inline Security Reassurance -->
<div class="payment-security-note">
  <span class="lock-icon">🔒</span>
  <span>Your payment information is encrypted and secure</span>
</div>
```

**Placement Priority:**
1. Next to credit card inputs
2. Below "Place Order" button
3. In footer of checkout pages
4. On cart page near checkout CTA

---

### 4.4 Return Policy Visibility

#### Pattern: Prominent Return Policy Display

**When to Use:** On PDP, cart, and checkout pages.

**Implementation:**

```html
<!-- Product Page Policy Summary -->
<div class="policy-highlights" data-testid="policy-highlights">
  <div class="policy-item">
    <span class="icon">↩️</span>
    <div class="content">
      <strong>30-Day Returns</strong>
      <p>Free returns within 30 days</p>
    </div>
  </div>
  <div class="policy-item">
    <span class="icon">🚚</span>
    <div class="content">
      <strong>Free Shipping</strong>
      <p>On orders over $99</p>
    </div>
  </div>
  <div class="policy-item">
    <span class="icon">🛡️</span>
    <div class="content">
      <strong>2-Year Warranty</strong>
      <p>Full manufacturer coverage</p>
    </div>
  </div>
</div>

<!-- Expandable Policy Details -->
<details class="policy-details" data-testid="return-policy-details">
  <summary>Return Policy Details</summary>
  <div class="policy-content">
    <h4>Easy Returns</h4>
    <ul>
      <li>30-day return window from delivery date</li>
      <li>Items must be unused and in original packaging</li>
      <li>Free return shipping label provided</li>
      <li>Refund processed within 5-7 business days</li>
    </ul>
    <a href="/returns" class="learn-more">Full Return Policy →</a>
  </div>
</details>
```

---

### 4.5 Customer Reviews Integration

#### Pattern: Comprehensive Review System

**Implementation:**

```html
<!-- Review Section -->
<section class="reviews-section" aria-labelledby="reviews-heading">
  <h2 id="reviews-heading">Customer Reviews</h2>
  
  <!-- Review Summary -->
  <div class="review-summary">
    <!-- Rating display (see 4.1) -->
  </div>
  
  <!-- Review Filters -->
  <div class="review-filters" data-testid="review-filters">
    <button class="filter-btn active" data-filter="all">All Reviews</button>
    <button class="filter-btn" data-filter="5">5 Star</button>
    <button class="filter-btn" data-filter="with-photos">With Photos</button>
    <button class="filter-btn" data-filter="verified">Verified Only</button>
    
    <select aria-label="Sort reviews">
      <option value="helpful">Most Helpful</option>
      <option value="recent">Most Recent</option>
      <option value="highest">Highest Rating</option>
      <option value="lowest">Lowest Rating</option>
    </select>
  </div>
  
  <!-- Review List -->
  <div class="reviews-list">
    <!-- Reviews (see 4.1 for individual review structure) -->
  </div>
  
  <!-- Load More -->
  <button class="load-more-reviews" data-testid="load-more-reviews">
    Load More Reviews
  </button>
  
  <!-- Write Review CTA -->
  <div class="write-review-cta">
    <p>Purchased this product?</p>
    <a href="#write-review" class="write-review-btn" data-testid="write-review">
      Write a Review
    </a>
  </div>
</section>

<!-- Q&A Section -->
<section class="qa-section" aria-labelledby="qa-heading">
  <h2 id="qa-heading">Questions & Answers</h2>
  
  <!-- Search Questions -->
  <div class="qa-search">
    <input 
      type="search" 
      placeholder="Have a question? Search for answers"
      aria-label="Search questions"
    />
  </div>
  
  <!-- Question List -->
  <div class="questions-list">
    <article class="qa-item" data-testid="qa-item-1">
      <div class="question">
        <span class="q-badge">Q:</span>
        <p>Does this unit require a dedicated circuit?</p>
        <span class="asker">Asked by HomeOwner123 on Jan 10, 2025</span>
      </div>
      <div class="answer">
        <span class="a-badge">A:</span>
        <p>Yes, we recommend a dedicated 15-amp circuit for optimal performance...</p>
        <span class="answerer">
          Answered by <strong>ClimaSite Support</strong> 
          <span class="official-badge">Official</span>
        </span>
      </div>
    </article>
  </div>
  
  <!-- Ask Question CTA -->
  <button class="ask-question-btn" data-testid="ask-question">
    Ask a Question
  </button>
</section>
```

---

## 5. Navigation & Wayfinding

### 5.1 Mega Menu Patterns

#### Pattern: Category-Rich Mega Menu

**When to Use:** Sites with 3+ main categories and 10+ subcategories.

**Implementation:**

```html
<!-- Mega Menu -->
<nav class="main-nav" aria-label="Main navigation">
  <ul class="nav-list">
    <li class="nav-item has-mega-menu">
      <button 
        aria-expanded="false" 
        aria-controls="mega-cooling"
        data-testid="nav-cooling"
      >
        Cooling <span class="chevron">▼</span>
      </button>
      
      <div id="mega-cooling" class="mega-menu" role="menu">
        <div class="mega-content">
          <!-- Category Columns -->
          <div class="mega-column">
            <h3>Air Conditioners</h3>
            <ul>
              <li><a href="/window-ac">Window Units</a></li>
              <li><a href="/portable-ac">Portable AC</a></li>
              <li><a href="/split-systems">Split Systems</a></li>
              <li><a href="/central-ac">Central Air</a></li>
            </ul>
          </div>
          
          <div class="mega-column">
            <h3>Fans & Ventilation</h3>
            <ul>
              <li><a href="/ceiling-fans">Ceiling Fans</a></li>
              <li><a href="/tower-fans">Tower Fans</a></li>
              <li><a href="/exhaust-fans">Exhaust Fans</a></li>
            </ul>
          </div>
          
          <!-- Featured/Promo Area -->
          <div class="mega-featured">
            <a href="/summer-sale" class="promo-banner">
              <img src="summer-sale-banner.jpg" alt="Summer Sale - Up to 40% Off" />
              <span class="promo-title">Summer Sale</span>
              <span class="promo-cta">Shop Now →</span>
            </a>
          </div>
        </div>
        
        <!-- Shop All Link -->
        <div class="mega-footer">
          <a href="/cooling" class="shop-all-link">
            View All Cooling Products →
          </a>
        </div>
      </div>
    </li>
    
    <!-- More nav items... -->
  </ul>
</nav>
```

**Mega Menu Best Practices:**

| Aspect | Recommendation |
|--------|----------------|
| **Trigger** | Hover on desktop, tap on mobile |
| **Delay** | 100-150ms hover delay to prevent accidental triggers |
| **Columns** | Maximum 4-5 columns, use hierarchy |
| **Featured Area** | Use for promotions, new arrivals, or top picks |
| **Accessibility** | Arrow key navigation, Escape to close |

---

### 5.2 Breadcrumb Behaviors

#### Pattern: Contextual Breadcrumbs

**When to Use:** All pages except homepage.

**Implementation:**

```html
<!-- Standard Breadcrumb -->
<nav aria-label="Breadcrumb" class="breadcrumb" data-testid="breadcrumb">
  <ol itemscope itemtype="https://schema.org/BreadcrumbList">
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
      <a itemprop="item" href="/">
        <span itemprop="name">Home</span>
      </a>
      <meta itemprop="position" content="1" />
    </li>
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
      <a itemprop="item" href="/cooling">
        <span itemprop="name">Cooling</span>
      </a>
      <meta itemprop="position" content="2" />
    </li>
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
      <a itemprop="item" href="/cooling/window-ac">
        <span itemprop="name">Window AC</span>
      </a>
      <meta itemprop="position" content="3" />
    </li>
    <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem" aria-current="page">
      <span itemprop="name">LG 12000 BTU Window AC</span>
      <meta itemprop="position" content="4" />
    </li>
  </ol>
</nav>

<!-- Mobile Breadcrumb (Back Link) -->
<nav aria-label="Back" class="breadcrumb-mobile">
  <a href="/cooling/window-ac" class="back-link">
    ← Window AC
  </a>
</nav>
```

**Breadcrumb Variations:**

| Context | Behavior |
|---------|----------|
| **Category Page** | Home > Category > Subcategory |
| **Product from Search** | Home > Search: "window ac" > Product |
| **Product from Category** | Home > Category > Subcategory > Product |
| **Mobile** | Single "Back" link to parent |

---

### 5.3 Search Experience

#### Pattern: Predictive Search with Rich Results

**When to Use:** All e-commerce sites.

**Implementation:**

```html
<!-- Search Component -->
<div class="search-container" data-testid="search-container">
  <form class="search-form" role="search" action="/search">
    <div class="search-input-wrapper">
      <input 
        type="search"
        name="q"
        placeholder="Search products..."
        autocomplete="off"
        aria-label="Search products"
        aria-autocomplete="list"
        aria-controls="search-suggestions"
        aria-expanded="false"
        data-testid="search-input"
      />
      <button type="submit" aria-label="Search">
        <svg aria-hidden="true"><!-- Search icon --></svg>
      </button>
    </div>
    
    <!-- Search Suggestions Dropdown -->
    <div 
      id="search-suggestions" 
      class="search-dropdown"
      role="listbox"
      aria-label="Search suggestions"
    >
      <!-- Recent Searches -->
      <div class="suggestion-section">
        <h4>Recent Searches</h4>
        <ul>
          <li role="option">
            <a href="/search?q=window+ac">
              <span class="icon">🕐</span>
              window ac
            </a>
          </li>
        </ul>
      </div>
      
      <!-- Popular Searches -->
      <div class="suggestion-section">
        <h4>Popular Searches</h4>
        <ul>
          <li role="option">
            <a href="/search?q=portable+ac">
              <span class="icon">🔥</span>
              portable ac
            </a>
          </li>
        </ul>
      </div>
      
      <!-- Product Suggestions -->
      <div class="suggestion-section">
        <h4>Products</h4>
        <ul class="product-suggestions">
          <li role="option" class="product-suggestion">
            <a href="/products/lg-12k-btu">
              <img src="product-thumb.jpg" alt="" />
              <div class="product-info">
                <span class="product-name">LG 12000 BTU Window AC</span>
                <span class="product-price">$449.99</span>
              </div>
            </a>
          </li>
        </ul>
      </div>
      
      <!-- Category Suggestions -->
      <div class="suggestion-section">
        <h4>Categories</h4>
        <ul>
          <li role="option">
            <a href="/cooling/window-ac">
              <span class="icon">📁</span>
              Window Air Conditioners
            </a>
          </li>
        </ul>
      </div>
    </div>
  </form>
</div>
```

**Search UX Guidelines:**

| Feature | Implementation |
|---------|----------------|
| **Debounce** | 200-300ms delay before API call |
| **Min Characters** | Start suggesting after 2 characters |
| **Recent Searches** | Store last 5-10 searches |
| **Product Previews** | Show image, name, price |
| **No Results** | Suggest alternatives, check spelling |
| **Keyboard Nav** | Arrow keys, Enter to select |

---

### 5.4 Category Page Layouts

#### Pattern: Flexible Grid with View Options

**When to Use:** All category and search result pages.

**Implementation:**

```html
<!-- Category Page Layout -->
<div class="category-page">
  <!-- Category Header -->
  <header class="category-header">
    <h1>Window Air Conditioners</h1>
    <p class="category-description">
      Stay cool with our selection of energy-efficient window AC units...
    </p>
    <span class="result-count">45 products</span>
  </header>
  
  <!-- Toolbar -->
  <div class="category-toolbar" data-testid="category-toolbar">
    <!-- Filter Toggle (Mobile) -->
    <button class="filter-toggle" aria-expanded="false">
      <span class="icon">☰</span> Filters
      <span class="active-count">(3)</span>
    </button>
    
    <!-- View Options -->
    <div class="view-options" role="radiogroup" aria-label="View layout">
      <button 
        role="radio" 
        aria-checked="true" 
        data-view="grid"
        aria-label="Grid view"
      >
        <svg><!-- Grid icon --></svg>
      </button>
      <button 
        role="radio" 
        aria-checked="false" 
        data-view="list"
        aria-label="List view"
      >
        <svg><!-- List icon --></svg>
      </button>
    </div>
    
    <!-- Sort -->
    <div class="sort-control">
      <label for="sort-select">Sort by:</label>
      <select id="sort-select" data-testid="sort-select">
        <option value="popular">Most Popular</option>
        <option value="price-asc">Price: Low to High</option>
        <option value="price-desc">Price: High to Low</option>
        <option value="rating">Highest Rated</option>
        <option value="newest">Newest</option>
      </select>
    </div>
    
    <!-- Items per page -->
    <div class="items-per-page">
      <label for="page-size">Show:</label>
      <select id="page-size">
        <option value="12">12</option>
        <option value="24">24</option>
        <option value="48">48</option>
      </select>
    </div>
  </div>
  
  <!-- Main Content Area -->
  <div class="category-content">
    <!-- Filters Sidebar -->
    <aside class="filter-sidebar" aria-label="Product filters">
      <!-- Filter groups (see 1.1) -->
    </aside>
    
    <!-- Product Grid -->
    <main class="product-grid" data-testid="product-grid">
      <!-- Grid View -->
      <div class="products-grid grid-view">
        <article class="product-card" data-testid="product-card-1">
          <a href="/products/lg-12k">
            <img src="product.jpg" alt="LG 12000 BTU AC" loading="lazy" />
            <div class="product-info">
              <h3>LG 12000 BTU Window AC</h3>
              <div class="rating">★★★★☆ (234)</div>
              <div class="price">$449.99</div>
              <ul class="quick-specs">
                <li>12,000 BTU</li>
                <li>Up to 550 sq ft</li>
                <li>Energy Star</li>
              </ul>
            </div>
          </a>
          <button class="quick-add" data-testid="quick-add-1">
            Add to Cart
          </button>
          <button class="wishlist-btn" aria-label="Add to wishlist">
            ♡
          </button>
        </article>
        <!-- More products... -->
      </div>
      
      <!-- Pagination -->
      <nav class="pagination" aria-label="Product pages">
        <button class="prev" disabled aria-label="Previous page">←</button>
        <span class="page-info">Page 1 of 4</span>
        <button class="next" aria-label="Next page">→</button>
      </nav>
    </main>
  </div>
</div>
```

**Grid Layout Options:**

| View | Use Case | Cards per Row |
|------|----------|---------------|
| Grid (Large) | Image-focused products | 3-4 |
| Grid (Compact) | High-density browsing | 4-6 |
| List | Spec comparison | 1 (full width) |

---

## 6. Implementation Checklist

### Pre-Launch UX Audit

#### Product Discovery

- [ ] Filter counts update dynamically
- [ ] Filters persist on back navigation
- [ ] Sort options have smart defaults
- [ ] Quick view shows essential info only
- [ ] Comparison table is accessible
- [ ] Recently viewed persists across sessions

#### Product Detail Page

- [ ] Main image has zoom functionality
- [ ] Thumbnails indicate image type (photo, video, 360°)
- [ ] Variant selection updates image and price
- [ ] Stock status is accurate and real-time
- [ ] Trust signals are near price and CTA
- [ ] Sticky bar appears at right scroll position

#### Cart & Checkout

- [ ] Mini-cart drawer opens without page load
- [ ] Free shipping progress bar updates
- [ ] Upsells are contextually relevant
- [ ] Progress indicator shows current step
- [ ] Forms have proper autocomplete
- [ ] Errors provide recovery actions

#### Trust & Conversion

- [ ] Star ratings visible on product cards
- [ ] Verified purchase badges on reviews
- [ ] Urgency indicators use real data
- [ ] Security badges near payment inputs
- [ ] Return policy easily accessible
- [ ] Q&A section is searchable

#### Navigation & Wayfinding

- [ ] Mega menu has keyboard navigation
- [ ] Breadcrumbs use structured data
- [ ] Search shows predictive results
- [ ] Category pages have view options
- [ ] Mobile navigation is touch-friendly
- [ ] 404 pages have helpful navigation

### Accessibility Checklist

- [ ] All images have descriptive alt text
- [ ] Focus states visible on all interactive elements
- [ ] Skip link available on navigation-heavy pages
- [ ] Color is not the only indicator of state
- [ ] ARIA labels on icon-only buttons
- [ ] Keyboard navigation works throughout
- [ ] Screen reader tested on key flows

### Performance Checklist

- [ ] Images use lazy loading
- [ ] Critical CSS is inlined
- [ ] JavaScript is deferred
- [ ] Web fonts have fallbacks
- [ ] Skeleton screens for loading states
- [ ] Infinite scroll has pagination fallback

---

## References

**Premium E-Commerce Examples:**
- Apple.com - Product gallery, variant selection, minimalist design
- Dyson.com - Interactive product demos, comparison tools
- Wayfair.com - Filter interactions, category navigation
- BestBuy.com - Spec comparison, trust signals
- Amazon.com - Reviews, Q&A, urgency patterns
- Home Depot - HVAC-specific UX patterns

**Research Sources:**
- Baymard Institute - E-commerce UX research
- Nielsen Norman Group - Usability guidelines
- Google Material Design - Component patterns
- WCAG 2.1 - Accessibility guidelines

---

*Document Version: 1.0*
*Last Updated: January 2025*
*Applicable to: ClimaSite HVAC E-Commerce Platform*
