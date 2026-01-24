# ClimaSite UI/UX Redesign Masterplan

> **10x Designer Vision**: Transform ClimaSite from a functional e-commerce platform into a stunning, immersive shopping experience that feels like walking into a premium climate control showroom.

---

## Executive Summary

This plan outlines a comprehensive redesign of ClimaSite's frontend to achieve:

- **Premium Visual Identity**: Liquid Glass aesthetic with Aurora gradients
- **Scroll-Driven Storytelling**: Cinematic animations that guide users through the journey
- **Micro-Interaction Excellence**: Every element responds with purpose and delight
- **Performance-First Motion**: Smooth 60fps animations with accessibility respect
- **Cohesive Design System**: Unified tokens, spacing, and component patterns

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Visual Identity Overhaul](#2-visual-identity-overhaul)
3. [Animation & Motion System](#3-animation--motion-system)
4. [Home Page Redesign](#4-home-page-redesign)
5. [Component Enhancements](#5-component-enhancements)
6. [Page-Specific Improvements](#6-page-specific-improvements)
7. [Micro-Interactions Catalog](#7-micro-interactions-catalog)
8. [Technical Implementation](#8-technical-implementation)
9. [Accessibility & Performance](#9-accessibility--performance)
10. [Implementation Phases](#10-implementation-phases)

---

## 1. Design Philosophy

### Core Principles

| Principle | Description |
|-----------|-------------|
| **Premium Restraint** | Elegant simplicity over visual clutter |
| **Purposeful Motion** | Every animation tells a story or provides feedback |
| **Climate Storytelling** | Design elements evoke air, flow, temperature |
| **Trust Through Polish** | Pixel-perfect details build purchasing confidence |
| **Performance Respect** | Beauty never compromises speed |

### Design Language: "Climate Flow"

The design language embodies the products we sell:
- **Air Flow**: Smooth transitions, floating elements, breath-like rhythms
- **Temperature Gradient**: Cool blues to warm oranges, representing heating & cooling
- **Precision Engineering**: Clean lines, exact spacing, technical credibility
- **Environmental Harmony**: Nature-inspired accents, sustainability undertones

---

## 2. Visual Identity Overhaul

### 2.1 Color Palette Refinement

**Current Issues:**
- Standard blue primary feels generic
- Limited emotional range
- Dark mode could be more sophisticated

**New Palette: "Arctic Aurora"**

```scss
// Primary Spectrum (Cool to Warm)
$arctic-blue: #0EA5E9;      // Primary - Fresh, clean air
$glacier-cyan: #22D3EE;     // Accent - Cooling systems
$aurora-teal: #14B8A6;      // Success - Eco-friendly
$sunset-amber: #F59E0B;     // Warm - Heating systems
$ember-orange: #EA580C;     // Hot - Premium heating

// Neutral Foundation
$midnight: #0F172A;         // Deepest dark
$charcoal: #1E293B;         // Dark surfaces
$slate: #334155;            // Muted elements
$silver: #94A3B8;           // Secondary text
$pearl: #E2E8F0;            // Borders light
$frost: #F8FAFC;            // Background light

// Semantic
$success: #10B981;          // Emerald
$warning: #FBBF24;          // Amber
$error: #EF4444;            // Red
$info: #3B82F6;             // Blue

// Glass Effects
$glass-light: rgba(255, 255, 255, 0.7);
$glass-dark: rgba(15, 23, 42, 0.8);
$glass-border: rgba(255, 255, 255, 0.2);
$glass-glow: rgba(14, 165, 233, 0.3);
```

### 2.2 Typography Upgrade

**Current:** Inter (good, but common)

**Recommended Pairing:**

| Element | Font | Weight | Reasoning |
|---------|------|--------|-----------|
| **Headlines** | **Space Grotesk** | 500-700 | Technical precision, modern, unique |
| **Body** | **Inter** | 400-500 | Excellent readability, keep current |
| **Accent/Numbers** | **JetBrains Mono** | 400 | Technical specs, prices, model numbers |

**Typography Scale:**

```scss
$type-scale: (
  'display': clamp(3rem, 8vw, 5rem),      // Hero headlines
  'h1': clamp(2.25rem, 5vw, 3.5rem),      // Page titles
  'h2': clamp(1.75rem, 4vw, 2.5rem),      // Section headers
  'h3': clamp(1.25rem, 3vw, 1.75rem),     // Card titles
  'h4': 1.25rem,                           // Subsections
  'body-lg': 1.125rem,                     // Featured text
  'body': 1rem,                            // Default
  'body-sm': 0.875rem,                     // Secondary
  'caption': 0.75rem,                      // Labels
);
```

### 2.3 Spacing System

**8px Grid System:**

```scss
$space: (
  '0': 0,
  '1': 0.25rem,   // 4px
  '2': 0.5rem,    // 8px
  '3': 0.75rem,   // 12px
  '4': 1rem,      // 16px
  '5': 1.25rem,   // 20px
  '6': 1.5rem,    // 24px
  '8': 2rem,      // 32px
  '10': 2.5rem,   // 40px
  '12': 3rem,     // 48px
  '16': 4rem,     // 64px
  '20': 5rem,     // 80px
  '24': 6rem,     // 96px
  '32': 8rem,     // 128px
);
```

### 2.4 Shadow System

**Elevation Levels:**

```scss
$shadows: (
  'xs': 0 1px 2px rgba(0, 0, 0, 0.05),
  'sm': 0 2px 4px rgba(0, 0, 0, 0.06), 0 1px 2px rgba(0, 0, 0, 0.04),
  'md': 0 4px 8px rgba(0, 0, 0, 0.08), 0 2px 4px rgba(0, 0, 0, 0.04),
  'lg': 0 8px 24px rgba(0, 0, 0, 0.12), 0 4px 8px rgba(0, 0, 0, 0.04),
  'xl': 0 16px 48px rgba(0, 0, 0, 0.16), 0 8px 16px rgba(0, 0, 0, 0.08),
  '2xl': 0 24px 64px rgba(0, 0, 0, 0.24),
  
  // Glow shadows for primary actions
  'glow-primary': 0 0 24px rgba(14, 165, 233, 0.4),
  'glow-warm': 0 0 24px rgba(245, 158, 11, 0.4),
);
```

### 2.5 Border Radius System

```scss
$radius: (
  'sm': 0.375rem,   // 6px - Buttons, inputs
  'md': 0.5rem,     // 8px - Cards small
  'lg': 0.75rem,    // 12px - Cards
  'xl': 1rem,       // 16px - Modals
  '2xl': 1.5rem,    // 24px - Hero cards
  '3xl': 2rem,      // 32px - Feature sections
  'full': 9999px,   // Pills, avatars
);
```

---

## 3. Animation & Motion System

### 3.1 Animation Principles

| Principle | Implementation |
|-----------|----------------|
| **Purposeful** | Every motion serves UX (feedback, orientation, delight) |
| **Natural** | Easing curves that mimic physics |
| **Performant** | Only animate `transform` and `opacity` |
| **Respectful** | Honor `prefers-reduced-motion` |
| **Consistent** | Unified timing and curves |

### 3.2 Timing Tokens

```scss
$duration: (
  'instant': 100ms,     // Hover states
  'fast': 150ms,        // Micro-interactions
  'normal': 250ms,      // Standard transitions
  'slow': 400ms,        // Page elements
  'slower': 600ms,      // Hero animations
  'slowest': 1000ms,    // Background effects
);

$easing: (
  'ease-out': cubic-bezier(0.16, 1, 0.3, 1),        // Decelerate (enter)
  'ease-in': cubic-bezier(0.7, 0, 0.84, 0),         // Accelerate (exit)
  'ease-in-out': cubic-bezier(0.65, 0, 0.35, 1),    // Symmetrical
  'spring': cubic-bezier(0.34, 1.56, 0.64, 1),      // Bouncy
  'smooth': cubic-bezier(0.4, 0, 0.2, 1),           // Default
);
```

### 3.3 Scroll-Driven Animations

**New Animation Types to Implement:**

| Animation | Trigger | Elements | Effect |
|-----------|---------|----------|--------|
| **Fade Up Reveal** | Enter viewport | All sections | Opacity 0→1, Y 40px→0 |
| **Stagger Reveal** | Enter viewport | Grid items | Sequential fade with 50ms delay |
| **Parallax Depth** | Scroll position | Hero backgrounds | Different scroll speeds |
| **Scale Reveal** | Enter viewport | Cards, images | Scale 0.9→1 with opacity |
| **Blur Reveal** | Enter viewport | Hero text | Blur 8px→0 with opacity |
| **Draw Line** | Enter viewport | Dividers, borders | SVG stroke animation |
| **Counter Up** | Enter viewport | Statistics | Number increment |
| **Text Split** | Enter viewport | Headlines | Letter-by-letter reveal |
| **Mask Reveal** | Enter viewport | Images | Clip-path wipe |
| **Magnetic Hover** | Mouse move | Buttons, cards | Subtle follow cursor |

### 3.4 Page Transition System

```typescript
// Route transition animations
const pageTransitions = {
  enter: {
    opacity: [0, 1],
    y: [20, 0],
    duration: 400,
    easing: 'ease-out'
  },
  exit: {
    opacity: [1, 0],
    duration: 200,
    easing: 'ease-in'
  }
};
```

---

## 4. Home Page Redesign

### 4.1 Current State Analysis

**Strengths:**
- Animated gradient background
- Brand ticker
- Multiple sections

**Weaknesses:**
- Generic hero without unique identity
- Static category panels
- Basic testimonials
- Limited scroll interaction
- No visual narrative

### 4.2 New Home Page Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                        FLOATING NAVBAR                          │
│  Logo    [Navigation]    Search    Cart    User    Theme        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    IMMERSIVE HERO (100vh)                       │
│                                                                 │
│    ┌─────────────────────────────────────────────────────┐     │
│    │  3D Floating AC Unit (WebGL/CSS)                    │     │
│    │  Parallax Particle System (air flow)                │     │
│    │                                                      │     │
│    │  "Perfect Climate.                                   │     │
│    │   Perfectly Delivered."                              │     │
│    │                                                      │     │
│    │  [Shop Cooling]  [Shop Heating]                     │     │
│    │                                                      │     │
│    │  Temperature toggle: ❄️ 18°C ←→ 24°C 🔥              │     │
│    └─────────────────────────────────────────────────────┘     │
│                                                                 │
│                    ↓ Scroll Indicator                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    TRUST BAR (Sticky on scroll)                 │
│  🚚 Free Shipping    🛡️ 5yr Warranty    📞 24/7 Support         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    BRAND MARQUEE                                │
│  ←← Daikin  Mitsubishi  LG  Samsung  Bosch  Carrier  →→        │
│  (Infinite scroll, pause on hover, link to brand pages)        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              CATEGORY EXPERIENCE (Scroll-triggered)             │
│                                                                 │
│    ┌──────────────────┐  ┌──────────────────┐                  │
│    │                  │  │                  │                  │
│    │   AIR            │  │   HEATING        │                  │
│    │   CONDITIONERS   │  │   SYSTEMS        │                  │
│    │                  │  │                  │                  │
│    │   3D tilt card   │  │   3D tilt card   │                  │
│    │   Parallax BG    │  │   Parallax BG    │                  │
│    │   Hover reveal   │  │   Hover reveal   │                  │
│    └──────────────────┘  └──────────────────┘                  │
│    ┌──────────────────┐  ┌──────────────────┐                  │
│    │   HEAT PUMPS     │  │   VENTILATION    │                  │
│    │                  │  │                  │                  │
│    └──────────────────┘  └──────────────────┘                  │
│                                                                 │
│    (Cards reveal with stagger, tilt on mouse move)             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              FEATURED PRODUCTS (Horizontal scroll)              │
│                                                                 │
│    "This Week's Top Picks"                                      │
│                                                                 │
│    ◀ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌───── ▶     │
│      │ Card 1 │ │ Card 2 │ │ Card 3 │ │ Card 4 │ │ Card        │
│      │        │ │        │ │ SALE   │ │        │ │             │
│      │ Hover  │ │ Quick  │ │ Badge  │ │ Energy │ │             │
│      │ Lift   │ │ View   │ │ Glow   │ │ A+++   │ │             │
│      └────────┘ └────────┘ └────────┘ └────────┘ └─────        │
│                                                                 │
│    ○ ○ ● ○ ○ (Pagination dots with progress)                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              WHY CHOOSE US (Scroll narrative)                   │
│                                                                 │
│    ┌─────────────────────────────────────────────┐             │
│    │                                             │             │
│    │   "Expert Installation in Every Region"    │             │
│    │                                             │             │
│    │   ┌───────────────┐                        │             │
│    │   │   Animated    │  ← Map with pins       │             │
│    │   │   Bulgaria    │    animating in        │             │
│    │   │   Map         │                        │             │
│    │   └───────────────┘                        │             │
│    │                                             │             │
│    │   500+ Certified Technicians               │             │
│    │   [  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░  ] 87%         │             │
│    │   Customer Satisfaction                     │             │
│    │                                             │             │
│    └─────────────────────────────────────────────┘             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              STATISTICS (Counter animation)                     │
│                                                                 │
│    ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐         │
│    │         │  │         │  │         │  │         │         │
│    │  50K+   │  │  15+    │  │  98%    │  │  24/7   │         │
│    │ Happy   │  │ Years   │  │ Rating  │  │ Support │         │
│    │ Customers│ │ Industry│  │         │  │         │         │
│    │         │  │         │  │         │  │         │         │
│    └─────────┘  └─────────┘  └─────────┘  └─────────┘         │
│                                                                 │
│    (Numbers count up when scrolled into view)                   │
│    (Background: gradient mesh with subtle motion)               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              TESTIMONIALS (3D Carousel)                         │
│                                                                 │
│              ┌─────────────────────────┐                       │
│         ╱    │                         │    ╲                  │
│    ┌───╱─────│    "Best AC purchase    │─────╲───┐             │
│    │  (fade) │     I've ever made"     │ (fade) │             │
│    │         │                         │         │             │
│    │         │    ⭐⭐⭐⭐⭐              │         │             │
│    │         │                         │         │             │
│    │         │    👤 Ivan Petrov       │         │             │
│    └─────────│    Sofia, Bulgaria      │─────────┘             │
│              └─────────────────────────┘                       │
│                                                                 │
│              ◀   ○ ○ ● ○ ○   ▶                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              HOW IT WORKS (Timeline animation)                  │
│                                                                 │
│    ①─────────②─────────③─────────④                            │
│    Browse    Add to    Checkout   Installation                  │
│    Products  Cart                 & Delivery                    │
│                                                                 │
│    (Line draws as you scroll, icons pulse on reveal)            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              NEWSLETTER (Glassmorphism card)                    │
│                                                                 │
│    ┌───────────────────────────────────────────────────┐       │
│    │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │       │
│    │  ░                                             ░  │       │
│    │  ░   Get 10% Off Your First Order              ░  │       │
│    │  ░                                             ░  │       │
│    │  ░   ┌─────────────────────────┐ ┌──────────┐ ░  │       │
│    │  ░   │ Enter your email        │ │Subscribe │ ░  │       │
│    │  ░   └─────────────────────────┘ └──────────┘ ░  │       │
│    │  ░                                             ░  │       │
│    │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │       │
│    └───────────────────────────────────────────────────┘       │
│                                                                 │
│    (Frosted glass effect with aurora gradient behind)           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│              FINAL CTA (Full-width gradient)                    │
│                                                                 │
│    ┌───────────────────────────────────────────────────┐       │
│    │                                                   │       │
│    │    Ready to Transform Your Climate?               │       │
│    │                                                   │       │
│    │    [  Get Started  ]  ← Magnetic button          │       │
│    │                                                   │       │
│    │    (Animated gradient: cool blue → warm orange)   │       │
│    │                                                   │       │
│    └───────────────────────────────────────────────────┘       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                          FOOTER                                 │
│  Multi-column with hover animations, social icons pulse         │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Hero Section Deep Dive

**Visual Elements:**
1. **Background**: Aurora gradient mesh (blue-cyan-teal) with subtle animation
2. **Particle System**: Floating dots simulating air particles
3. **3D Element**: CSS perspective-transformed AC unit or temperature gauge
4. **Temperature Toggle**: Interactive widget that changes color scheme (cool/warm)

**Animations:**
- Text: Split-letter fade-in with stagger
- CTA buttons: Scale up with glow on appearance
- Background: Continuous slow morph (20s cycle)
- Particles: Gentle drift using CSS transforms

**Interactions:**
- Mouse parallax on background layers
- Magnetic hover on CTA buttons
- Temperature slider changes hero gradient

---

## 5. Component Enhancements

### 5.1 Product Card Redesign

**Current Issues:**
- Standard box layout
- Basic hover effect
- No visual hierarchy emphasis

**Improvements:**

```
┌────────────────────────────────────────┐
│  ┌──────────────────────────────────┐  │
│  │                                  │  │
│  │     🖼️ Product Image             │  │
│  │     (Zoom on hover)              │  │
│  │                                  │  │
│  │  ┌─────┐                    ❤️   │  │ ← Wishlist floats in
│  │  │SALE │                         │  │
│  │  │-20% │                         │  │
│  │  └─────┘                         │  │
│  │                                  │  │
│  │         [Quick View]             │  │ ← Appears on hover
│  │                                  │  │
│  └──────────────────────────────────┘  │
│                                        │
│  BRAND NAME                            │
│  Product Title That Can                │
│  Span Two Lines                        │
│                                        │
│  ⭐⭐⭐⭐⭐ (128)                         │
│                                        │
│  Energy: [A+++]                        │ ← Visual badge
│                                        │
│  ┌─────────┐  ┌─────────────────────┐  │
│  │ €599    │  │  🛒 Add to Cart     │  │ ← Button reveals on hover
│  │ €749    │  └─────────────────────┘  │
│  └─────────┘                           │
│                                        │
└────────────────────────────────────────┘
```

**Hover Effects:**
- Card lifts with shadow increase
- Image zooms slightly (scale 1.05)
- Quick View button slides up
- Add to Cart button fades in
- Wishlist heart pulses

### 5.2 Button System

**New Button Variants:**

| Variant | Use Case | Effect |
|---------|----------|--------|
| **Primary** | Main actions | Glow shadow, scale on hover |
| **Secondary** | Alternative actions | Border highlight |
| **Ghost** | Tertiary actions | Background fill on hover |
| **Magnetic** | CTAs | Follows cursor slightly |
| **Loading** | Async operations | Spinner + disabled state |
| **Success** | Confirmation | Checkmark animation |

**Magnetic Button Effect:**

```typescript
// Button follows cursor within bounds
onMouseMove(e: MouseEvent) {
  const rect = this.el.getBoundingClientRect();
  const x = e.clientX - rect.left - rect.width / 2;
  const y = e.clientY - rect.top - rect.height / 2;
  
  this.el.style.transform = `translate(${x * 0.2}px, ${y * 0.2}px)`;
}
```

### 5.3 Navigation Enhancements

**Header Improvements:**

1. **Floating Navigation**
   - Glass background on scroll
   - Reduces height when scrolled
   - Shadow appears on scroll

2. **Search Enhancement**
   - Expands on focus
   - Live search results dropdown
   - Recent searches
   - Category suggestions

3. **Mega Menu Polish**
   - Smooth slide-down animation
   - Category images
   - Featured products in dropdown
   - Promotional banner

4. **Mobile Menu**
   - Full-screen overlay
   - Staggered item animations
   - Gesture support (swipe to close)

### 5.4 Loading States

**Skeleton Improvements:**
- Gradient shimmer (not pulse)
- Accurate shape matching
- Progressive reveal

```scss
.skeleton {
  background: linear-gradient(
    90deg,
    var(--skeleton-base) 0%,
    var(--skeleton-highlight) 50%,
    var(--skeleton-base) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s ease-in-out infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

---

## 6. Page-Specific Improvements

### 6.1 Product List Page

**Improvements:**

1. **Filter Sidebar**
   - Collapsible sections with animation
   - Range slider for price (instead of inputs)
   - Color swatches with checkmarks
   - Active filters as removable pills

2. **Grid Enhancements**
   - Masonry option for varied heights
   - Infinite scroll with skeleton loading
   - "Back to top" floating button
   - View toggle animation (grid ↔ list)

3. **Sort Dropdown**
   - Animated dropdown
   - Selected indicator
   - Hover preview of sort result

### 6.2 Product Detail Page

**Improvements:**

1. **Gallery**
   - Smooth thumbnail transitions
   - Lightbox with gesture support
   - 360° view option (if images available)
   - Zoom lens with smooth tracking

2. **Product Info**
   - Animated tab transitions
   - Sticky add-to-cart on scroll
   - Price animation on variant change
   - Stock indicator with pulse

3. **Reviews Section**
   - Rating bars animate on scroll
   - Helpful vote with micro-animation
   - Image lightbox for review photos

### 6.3 Cart Page

**Improvements:**

1. **Item Cards**
   - Swipe to remove (mobile)
   - Quantity stepper with animation
   - Save for later option

2. **Summary**
   - Sticky on scroll
   - Promo code input with validation animation
   - Estimated delivery date
   - Trust badges

3. **Empty State**
   - Illustrated empty cart
   - Suggested products
   - Continue shopping button

### 6.4 Checkout Flow

**Improvements:**

1. **Progress Indicator**
   - Step line with animation
   - Completed steps with checkmark
   - Current step highlight

2. **Form Fields**
   - Floating labels
   - Inline validation with icons
   - Address autocomplete
   - Card input with formatting

3. **Order Summary**
   - Collapsible on mobile
   - Product thumbnails
   - Real-time total updates

---

## 7. Micro-Interactions Catalog

### 7.1 Form Interactions

| Element | Trigger | Animation |
|---------|---------|-----------|
| Input focus | Focus | Border glow, label float up |
| Input valid | Blur valid | Green checkmark fade in |
| Input error | Blur invalid | Red shake, error slide down |
| Dropdown open | Click | Scale Y from 0 + fade |
| Checkbox check | Click | Scale pop + checkmark draw |
| Radio select | Click | Inner circle scale in |
| Toggle switch | Click | Circle slide + color change |

### 7.2 Button Interactions

| Element | Trigger | Animation |
|---------|---------|-----------|
| Primary hover | Hover | Scale 1.02, glow increase |
| Primary click | Click | Scale 0.98 (press effect) |
| Add to Cart | Click | Icon changes to check, then back |
| Loading | Async | Spinner appears, text fades |
| Success | Complete | Checkmark animation, green flash |

### 7.3 Card Interactions

| Element | Trigger | Animation |
|---------|---------|-----------|
| Product card hover | Hover | Lift (translateY -4px), shadow increase |
| Category card hover | Hover | Image zoom, overlay fade, text slide |
| Testimonial card | Auto | Rotate in 3D space |
| Stats card | Scroll | Number count up |

### 7.4 Navigation Interactions

| Element | Trigger | Animation |
|---------|---------|-----------|
| Nav link hover | Hover | Underline slide in from left |
| Nav link active | Route | Underline expand |
| Dropdown open | Hover/click | Scale Y + stagger items |
| Mobile menu | Click | Slide from right + overlay fade |
| Header scroll | Scroll | Height reduce, background blur |

### 7.5 Notification Interactions

| Element | Trigger | Animation |
|---------|---------|-----------|
| Toast appear | Show | Slide in from right + fade |
| Toast dismiss | Auto/click | Slide out + fade |
| Badge update | Count change | Scale pop |
| Alert appear | Show | Slide down + fade |

---

## 8. Technical Implementation

### 8.1 New Directives to Create

```typescript
// 1. Scroll Progress Directive
@Directive({ selector: '[scrollProgress]' })
export class ScrollProgressDirective {
  // Tracks scroll position as 0-1 value
  // Useful for progress bars, parallax
}

// 2. Magnetic Hover Directive  
@Directive({ selector: '[magneticHover]' })
export class MagneticHoverDirective {
  // Element follows cursor within bounds
  // Configurable strength
}

// 3. Split Text Directive
@Directive({ selector: '[splitText]' })
export class SplitTextDirective {
  // Wraps each character/word in span
  // For staggered text animations
}

// 4. Tilt Effect Directive
@Directive({ selector: '[tiltEffect]' })
export class TiltEffectDirective {
  // 3D tilt based on mouse position
  // Configurable perspective
}

// 5. Reveal Animation Directive
@Directive({ selector: '[reveal]' })
export class RevealDirective {
  // Enhanced scroll reveal with more options
  // Supports: fade, slide, scale, blur
}

// 6. Smooth Counter Directive
@Directive({ selector: '[smoothCounter]' })
export class SmoothCounterDirective {
  // Counts number with easing
  // Supports: decimal, prefix, suffix
}
```

### 8.2 Animation Service

```typescript
@Injectable({ providedIn: 'root' })
export class AnimationService {
  // Centralized animation control
  
  // Scroll position (0-1)
  scrollProgress = signal(0);
  
  // Scroll direction
  scrollDirection = signal<'up' | 'down'>('down');
  
  // Reduced motion preference
  prefersReducedMotion = signal(false);
  
  // Animation queue for coordination
  animationQueue = signal<Animation[]>([]);
  
  // Stagger delay calculator
  getStaggerDelay(index: number, baseDelay: number = 50): number;
  
  // Check if element is in viewport
  isInViewport(element: Element, threshold?: number): boolean;
}
```

### 8.3 CSS Architecture Updates

```
styles/
├── _variables.scss        # Design tokens
├── _colors.scss          # Color palette (existing, enhanced)
├── _typography.scss      # Font definitions
├── _spacing.scss         # Spacing scale
├── _shadows.scss         # Shadow system
├── _animations.scss      # Keyframes & transitions
├── _utilities.scss       # Utility classes
├── _components.scss      # Component base styles
├── _glass.scss           # Glassmorphism utilities
└── styles.scss           # Main entry point
```

### 8.4 Performance Optimizations

1. **CSS Containment**
   ```css
   .card { contain: content; }
   .animation-container { contain: layout style; }
   ```

2. **Will-Change Hints**
   ```css
   .animated-element {
     will-change: transform, opacity;
   }
   ```

3. **GPU Acceleration**
   ```css
   .smooth-animation {
     transform: translateZ(0);
     backface-visibility: hidden;
   }
   ```

4. **Intersection Observer**
   - Lazy load animations
   - Pause off-screen animations
   - Defer heavy effects

---

## 9. Accessibility & Performance

### 9.1 Accessibility Requirements

| Requirement | Implementation |
|-------------|----------------|
| **Reduced Motion** | All animations check `prefers-reduced-motion` |
| **Focus Visible** | Enhanced focus rings on all interactive elements |
| **Color Contrast** | Minimum 4.5:1 for all text |
| **Keyboard Nav** | Full keyboard support for all features |
| **Screen Reader** | Proper ARIA labels and live regions |
| **Skip Links** | Skip to main content link |

**Reduced Motion Implementation:**

```scss
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### 9.2 Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| **First Contentful Paint** | < 1.5s | Lighthouse |
| **Largest Contentful Paint** | < 2.5s | Lighthouse |
| **Time to Interactive** | < 3s | Lighthouse |
| **Cumulative Layout Shift** | < 0.1 | Lighthouse |
| **Animation FPS** | 60fps | DevTools |
| **Bundle Size** | < 200KB (gzipped) | Build output |

### 9.3 Performance Strategies

1. **Lazy Load Animations**
   - Only initialize when in viewport
   - Unload when scrolled away

2. **Debounce/Throttle**
   - Scroll handlers: 16ms throttle
   - Resize handlers: 100ms debounce
   - Mouse move: 16ms throttle

3. **Animation Budgets**
   - Max 3 concurrent animations per viewport
   - Background animations at 30fps
   - Foreground animations at 60fps

4. **Image Optimization**
   - WebP format
   - Responsive srcset
   - Blur-up placeholders
   - Lazy loading

---

## 10. Implementation Phases

### Phase 1: Foundation (Week 1-2)

**Priority: HIGH | Effort: MEDIUM**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-001 | Update color palette in `_colors.scss` | `_colors.scss` |
| UX-002 | Add Space Grotesk & JetBrains Mono fonts | `index.html`, `styles.scss` |
| UX-003 | Create spacing/shadow/radius tokens | New SCSS files |
| UX-004 | Create `_animations.scss` with keyframes | New file |
| UX-005 | Create animation timing tokens | `_variables.scss` |
| UX-006 | Update AnimationService with new features | `animation.service.ts` |
| UX-007 | Create MagneticHoverDirective | New directive |
| UX-008 | Create TiltEffectDirective | New directive |
| UX-009 | Enhance RevealDirective with new animations | Existing directive |
| UX-010 | Create SmoothCounterDirective | New directive |

**Deliverables:**
- Updated design system tokens
- New animation directives
- Enhanced animation service

---

### Phase 2: Core Components (Week 3-4)

**Priority: HIGH | Effort: HIGH**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-011 | Redesign ButtonComponent with new variants | `button.component.ts/scss` |
| UX-012 | Add magnetic effect to CTA buttons | Button component |
| UX-013 | Create GlassCardComponent | New component |
| UX-014 | Enhance ProductCardComponent hover effects | `product-card.component.ts/scss` |
| UX-015 | Add Quick View button to product cards | Product card |
| UX-016 | Redesign SkeletonComponent with shimmer | `skeleton.component.ts/scss` |
| UX-017 | Enhance InputComponent with floating labels | `input.component.ts/scss` |
| UX-018 | Create animated CheckboxComponent | New component |
| UX-019 | Create animated ToggleSwitchComponent | New component |
| UX-020 | Enhance BadgeComponent with animations | `badge.component.ts/scss` |

**Deliverables:**
- Redesigned core UI components
- New interactive form elements
- Enhanced product cards

---

### Phase 3: Navigation (Week 5)

**Priority: HIGH | Effort: MEDIUM**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-021 | Implement floating header with blur | `header.component.ts/scss` |
| UX-022 | Add header shrink on scroll | Header component |
| UX-023 | Enhance search with live results | `search.component.ts` |
| UX-024 | Polish mega menu animations | `mega-menu.component.ts/scss` |
| UX-025 | Add featured products to mega menu | Mega menu |
| UX-026 | Redesign mobile menu with gestures | Header component |
| UX-027 | Add nav link hover animations | Header SCSS |
| UX-028 | Enhance footer with hover effects | `footer.component.ts/scss` |

**Deliverables:**
- Premium floating navigation
- Enhanced mega menu
- Polished mobile experience

---

### Phase 4: Home Page Transformation (Week 6-8)

**Priority: CRITICAL | Effort: VERY HIGH**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-029 | Create new hero section layout | `home.component.ts/scss` |
| UX-030 | Implement aurora gradient background | Home SCSS |
| UX-031 | Add particle system (CSS-based) | Home component |
| UX-032 | Create split-text headline animation | Home component |
| UX-033 | Add temperature toggle interaction | Home component |
| UX-034 | Implement magnetic CTA buttons | Home component |
| UX-035 | Redesign trust bar with sticky behavior | Home component |
| UX-036 | Enhance brand marquee with hover pause | Home component |
| UX-037 | Create 3D tilt category cards | Home component |
| UX-038 | Implement horizontal product carousel | Home component |
| UX-039 | Add carousel progress indicator | Home component |
| UX-040 | Create statistics section with counters | Home component |
| UX-041 | Redesign testimonials as 3D carousel | Home component |
| UX-042 | Create timeline "How it Works" section | Home component |
| UX-043 | Implement glassmorphism newsletter | Home component |
| UX-044 | Create gradient CTA section | Home component |
| UX-045 | Add scroll-triggered reveals throughout | Home component |
| UX-046 | Implement parallax effects | Home component |

**Deliverables:**
- Completely redesigned home page
- Immersive hero experience
- Scroll-driven storytelling

---

### Phase 5: Product Pages (Week 9-10)

**Priority: HIGH | Effort: HIGH**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-047 | Enhance filter sidebar animations | `product-list.component.ts/scss` |
| UX-048 | Add price range slider | Product list |
| UX-049 | Implement grid/list view transition | Product list |
| UX-050 | Add infinite scroll with skeleton | Product list |
| UX-051 | Enhance gallery with smooth transitions | `product-detail.component.ts/scss` |
| UX-052 | Add sticky add-to-cart on scroll | Product detail |
| UX-053 | Animate tab transitions | Product detail |
| UX-054 | Enhance review section animations | Product detail |
| UX-055 | Add "added to cart" celebration | Product detail |

**Deliverables:**
- Enhanced product browsing
- Polished product details
- Better shopping experience

---

### Phase 6: Cart & Checkout (Week 11-12)

**Priority: MEDIUM | Effort: MEDIUM**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-056 | Redesign cart item cards | `cart.component.ts/scss` |
| UX-057 | Add swipe-to-remove on mobile | Cart component |
| UX-058 | Enhance quantity stepper | Cart component |
| UX-059 | Create animated empty cart state | Cart component |
| UX-060 | Redesign checkout progress indicator | `checkout.component.ts/scss` |
| UX-061 | Add form field micro-interactions | Checkout component |
| UX-062 | Enhance order summary collapse | Checkout component |
| UX-063 | Create order confirmation celebration | Checkout component |

**Deliverables:**
- Streamlined cart experience
- Premium checkout flow
- Delightful confirmation

---

### Phase 7: Polish & Optimization (Week 13-14)

**Priority: HIGH | Effort: MEDIUM**

| Task ID | Task | Files Affected |
|---------|------|----------------|
| UX-064 | Add page transition animations | App routing |
| UX-065 | Implement toast notification animations | Toast service |
| UX-066 | Add loading page transitions | Route guards |
| UX-067 | Performance audit and optimization | Various |
| UX-068 | Accessibility audit and fixes | Various |
| UX-069 | Cross-browser testing | N/A |
| UX-070 | Mobile responsiveness polish | Various |
| UX-071 | Dark mode refinements | `_colors.scss` |
| UX-072 | Reduced motion testing | Various |
| UX-073 | Final QA and bug fixes | Various |

**Deliverables:**
- Polished transitions
- Performance optimized
- Accessibility compliant
- Production ready

---

## Summary

### Total Estimated Time: 14 Weeks

### Task Breakdown:

| Phase | Tasks | Priority | Weeks |
|-------|-------|----------|-------|
| 1. Foundation | 10 | HIGH | 2 |
| 2. Core Components | 10 | HIGH | 2 |
| 3. Navigation | 8 | HIGH | 1 |
| 4. Home Page | 18 | CRITICAL | 3 |
| 5. Product Pages | 9 | HIGH | 2 |
| 6. Cart & Checkout | 8 | MEDIUM | 2 |
| 7. Polish | 10 | HIGH | 2 |
| **Total** | **73 Tasks** | | **14 Weeks** |

### Success Metrics:

- [ ] Lighthouse Performance Score: 90+
- [ ] Lighthouse Accessibility Score: 100
- [ ] Animation FPS: Consistent 60fps
- [ ] User feedback: "Premium feel"
- [ ] Time on home page: +50%
- [ ] Bounce rate: -20%
- [ ] Mobile usability: 100%

---

## Appendix: Design System Files

After implementation, the design system will include:

```
src/ClimaSite.Web/src/
├── styles/
│   ├── _variables.scss      # All design tokens
│   ├── _colors.scss         # Color palette
│   ├── _typography.scss     # Font system
│   ├── _spacing.scss        # Spacing scale
│   ├── _shadows.scss        # Shadow system
│   ├── _animations.scss     # Animation keyframes
│   ├── _glass.scss          # Glassmorphism utilities
│   └── styles.scss          # Main import
│
├── app/
│   ├── shared/
│   │   ├── directives/
│   │   │   ├── magnetic-hover.directive.ts
│   │   │   ├── tilt-effect.directive.ts
│   │   │   ├── reveal.directive.ts
│   │   │   ├── smooth-counter.directive.ts
│   │   │   ├── split-text.directive.ts
│   │   │   └── scroll-progress.directive.ts
│   │   │
│   │   └── components/
│   │       ├── glass-card/
│   │       ├── animated-checkbox/
│   │       ├── toggle-switch/
│   │       └── price-slider/
│   │
│   └── core/
│       └── services/
│           └── animation.service.ts
```

---

*This plan was generated using UI/UX Pro Max design intelligence.*
*Last updated: January 2026*
