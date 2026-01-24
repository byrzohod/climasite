# Visual Hierarchy & Composition Patterns

Comprehensive guide to visual hierarchy, layout composition, and information architecture patterns for modern web design.

---

## Table of Contents

1. [Visual Hierarchy Principles](#1-visual-hierarchy-principles)
2. [Layout Composition](#2-layout-composition)
3. [Content Density & Scannability](#3-content-density--scannability)
4. [Responsive Patterns](#4-responsive-patterns)
5. [Page Type Patterns](#5-page-type-patterns)
6. [Quick Reference](#6-quick-reference)

---

## 1. Visual Hierarchy Principles

Visual hierarchy guides users through content by establishing clear relationships between elements through size, color, spacing, and position.

### 1.1 Size & Scale Relationships

The most fundamental hierarchy tool. Larger elements attract attention first.

#### The Modular Scale

Use a consistent type scale for predictable sizing relationships:

| Scale Name | Ratio | Example Sizes (px) | Use Case |
|------------|-------|-------------------|----------|
| Minor Second | 1.067 | 12, 13, 14, 15, 16 | Dense data interfaces |
| Major Second | 1.125 | 12, 14, 16, 18, 20 | Body-focused content |
| Minor Third | 1.200 | 12, 14, 17, 21, 25 | Balanced hierarchy |
| Major Third | 1.250 | 12, 15, 19, 24, 30 | Marketing pages |
| Perfect Fourth | 1.333 | 12, 16, 21, 28, 37 | High contrast hierarchy |
| Golden Ratio | 1.618 | 12, 19, 31, 50, 81 | Dramatic impact |

**Recommended Scale for ClimaSite (E-commerce):**

```scss
// Using Major Third (1.250) scale
$font-size-xs: 0.75rem;   // 12px - Labels, captions
$font-size-sm: 0.875rem;  // 14px - Secondary text
$font-size-base: 1rem;    // 16px - Body text
$font-size-lg: 1.25rem;   // 20px - Lead paragraphs
$font-size-xl: 1.5rem;    // 24px - Section headings (H3)
$font-size-2xl: 2rem;     // 32px - Page headings (H2)
$font-size-3xl: 2.5rem;   // 40px - Hero headings (H1)
$font-size-4xl: 3rem;     // 48px - Marketing displays
```

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Use a consistent scale system | Pick random sizes (14, 17, 23, 31) |
| Limit to 5-6 distinct sizes | Use 10+ different sizes |
| Reserve largest sizes for key messages | Make everything large |
| Test scale on mobile (16px minimum body) | Use smaller than 14px for body |

---

### 1.2 Color & Contrast for Emphasis

Color creates instant visual priority. High contrast = high attention.

#### Contrast Hierarchy Strategy

```
Primary Action     → Brand color, high saturation
Secondary Action   → Muted brand color or neutral
Tertiary/Links     → Underlined text or subtle color
Body Text          → High contrast neutral (#1A1A1A on white)
Secondary Text     → Medium contrast (#666666 on white)
Disabled/Hints     → Low contrast (#999999 on white)
```

#### The 60-30-10 Rule

| Proportion | Application | Example |
|------------|-------------|---------|
| 60% | Dominant background | White/light gray |
| 30% | Secondary elements | Cards, sections, text |
| 10% | Accent/CTAs | Primary buttons, highlights |

#### Contrast Ratios (WCAG Compliance)

| Element Type | Minimum Ratio | Recommended |
|--------------|---------------|-------------|
| Body text (normal) | 4.5:1 | 7:1+ |
| Large text (18px+) | 3:1 | 4.5:1+ |
| UI components | 3:1 | 4.5:1+ |
| Non-essential graphics | No minimum | 3:1+ |

#### Color Emphasis Techniques

| Technique | Use Case | Visual Example |
|-----------|----------|----------------|
| Saturation contrast | CTAs on neutral backgrounds | Bright blue button on gray page |
| Value contrast | Text hierarchy | Black heading, gray subtext |
| Hue isolation | Single accent color | Monochrome page with orange CTA |
| Complementary pop | Key elements | Purple interface, yellow notifications |

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Use color consistently for meaning | Change color meanings between pages |
| Pair color with icons/text for accessibility | Use color as the only indicator |
| Test in both light and dark modes | Assume colors work everywhere |
| Check contrast ratios with tools | Guess at readability |

---

### 1.3 Whitespace & Breathing Room

Whitespace (negative space) is not empty - it's an active design element that:
- Creates visual groupings
- Improves readability
- Conveys premium quality
- Reduces cognitive load

#### Whitespace Scale (8px Grid)

```scss
// Tailwind-compatible spacing scale
$space-0: 0;
$space-1: 0.25rem;   // 4px  - Tight: related inline elements
$space-2: 0.5rem;    // 8px  - Compact: form fields, buttons
$space-3: 0.75rem;   // 12px - Comfortable: list items
$space-4: 1rem;      // 16px - Default: paragraphs
$space-5: 1.25rem;   // 20px - Relaxed: card padding
$space-6: 1.5rem;    // 24px - Spacious: section gaps
$space-8: 2rem;      // 32px - Sections: between components
$space-10: 2.5rem;   // 40px - Large: major sections
$space-12: 3rem;     // 48px - Extra: page sections
$space-16: 4rem;     // 64px - Hero: above fold spacing
$space-20: 5rem;     // 80px - Maximum: section dividers
```

#### Proximity Principle (Gestalt)

**The Law of Proximity**: Elements near each other are perceived as related.

```
WRONG: Even spacing everywhere
[Header]        32px
[Subheader]     32px
[Body text]     32px
[Button]

CORRECT: Grouped by relationship
[Header]        8px   ← Tight: header + subheader are related
[Subheader]     24px  ← Medium: subheader introduces body
[Body text]     16px  ← Default: body paragraphs
[Button]        32px  ← Large: button is a new group
```

#### Macro vs Micro Whitespace

| Type | Definition | Application |
|------|------------|-------------|
| Micro | Within components | Line-height, letter-spacing, button padding |
| Macro | Between components | Section spacing, page margins, grid gaps |

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Use more space for luxury/premium feel | Crowd elements to "fit more" |
| Group related items with tight spacing | Space everything equally |
| Add generous margins on content edges | Let content touch viewport edges |
| Increase spacing at larger breakpoints | Keep mobile spacing on desktop |

---

### 1.4 Typography Hierarchy

Typography is the backbone of visual hierarchy. A clear type system creates instant understanding.

#### The 4-Level Type System

| Level | HTML | Purpose | Styling Characteristics |
|-------|------|---------|------------------------|
| Display | h1 | Page hero, marketing | Largest, bold, can be decorative |
| Heading | h2, h3 | Section titles | Large, semi-bold to bold |
| Body | p, li | Main content | Comfortable size, regular weight |
| Caption | small, span | Labels, meta | Small, lighter weight or color |

#### Type Hierarchy Formula

```scss
// Display (Hero Headlines)
.display-1 {
  font-size: 3rem;      // 48px
  font-weight: 700;
  line-height: 1.1;
  letter-spacing: -0.02em;
}

// Heading 1 (Page Titles)
.h1 {
  font-size: 2.25rem;   // 36px
  font-weight: 600;
  line-height: 1.2;
  letter-spacing: -0.01em;
}

// Heading 2 (Section Titles)
.h2 {
  font-size: 1.5rem;    // 24px
  font-weight: 600;
  line-height: 1.3;
}

// Heading 3 (Subsection Titles)
.h3 {
  font-size: 1.25rem;   // 20px
  font-weight: 600;
  line-height: 1.4;
}

// Body (Paragraphs)
.body {
  font-size: 1rem;      // 16px
  font-weight: 400;
  line-height: 1.6;
}

// Caption (Labels, Meta)
.caption {
  font-size: 0.875rem;  // 14px
  font-weight: 400;
  line-height: 1.5;
  color: var(--color-text-secondary);
}
```

#### Recommended Font Pairings for E-commerce

| Category | Heading Font | Body Font | Mood |
|----------|--------------|-----------|------|
| Professional | Poppins | Open Sans | Modern, clean, trustworthy |
| Premium | Playfair Display | Inter | Elegant, sophisticated |
| Technical | Inter | Inter | Minimal, functional |
| Friendly | Plus Jakarta Sans | Plus Jakarta Sans | Approachable, modern |

#### Line Length (Measure)

Optimal line length for readability: **45-75 characters** (65ch is ideal)

```css
.prose {
  max-width: 65ch; /* ~600px at 16px */
}

/* Or in Tailwind */
.max-w-prose /* equals max-width: 65ch */
```

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Use 1-2 font families maximum | Mix 3+ different fonts |
| Establish clear heading hierarchy (h1→h6) | Skip heading levels (h1 → h4) |
| Use weight changes for emphasis | Use different fonts for emphasis |
| Limit line length to 65-75 characters | Let text span full viewport width |
| Use 1.5-1.75 line-height for body | Use 1.0 line-height (too tight) |

---

### 1.5 Focal Points & Visual Flow

Users scan pages in predictable patterns. Design should guide this flow.

#### Eye-Tracking Patterns

**F-Pattern (Text-heavy pages)**
```
→→→→→→→→→→→→→
→→→→→→→→
↓
→→→→→→→→→→
↓
→→→→→
↓
```
Users read across the top, then down the left side, making horizontal movements.

**Z-Pattern (Minimal pages, landing pages)**
```
→→→→→→→→→→→→→
              ↘
                ↘
              ↙
→→→→→→→→→→→→→
```
Eye moves from top-left → top-right → bottom-left → bottom-right.

**Gutenberg Diagram (Dense layouts)**
```
┌─────────────┬─────────────┐
│ PRIMARY     │  Strong     │
│ Optical     │  Fallow     │
│ Area        │  Area       │
├─────────────┼─────────────┤
│ Weak        │ TERMINAL    │
│ Fallow      │ Area        │
│ Area        │ (CTA here)  │
└─────────────┴─────────────┘
```
Reading gravity flows from top-left to bottom-right.

#### Creating Focal Points

| Technique | Effect | Example |
|-----------|--------|---------|
| Size dominance | Largest element draws first attention | Hero image, large headline |
| Color contrast | Bright against neutral catches eye | Orange CTA on blue page |
| Isolation | Surrounded by whitespace = important | Centered hero text |
| Faces & eyes | Humans instinctively look at faces | Testimonial photos |
| Motion | Movement attracts peripheral vision | Subtle button animation |
| Arrows/lines | Guide eye along a path | Pointing hands, directional shapes |

#### Visual Weight Hierarchy

```
HIGH WEIGHT (First attention)
├── Large images
├── Bold headlines
├── Bright colored buttons
├── Icons with color
│
MEDIUM WEIGHT
├── Subheadings
├── Product cards
├── Secondary buttons
│
LOW WEIGHT (Background)
├── Body text
├── Borders and dividers
├── Muted icons
└── Footer content
```

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Place CTAs in terminal areas | Hide CTAs in weak fallow areas |
| Use one primary focal point per viewport | Compete for attention with multiple CTAs |
| Guide eyes with directional cues | Let users figure out the path |
| Place most important info top-left | Bury key info bottom-right |

---

## 2. Layout Composition

### 2.1 Grid Systems & Breakpoints

Grids create consistency, alignment, and rhythm across pages.

#### The 12-Column Grid

The standard for web design. Divides evenly into 1, 2, 3, 4, 6, 12 columns.

```
12 cols = Full width
6 cols  = Half
4 cols  = Third
3 cols  = Quarter
2 cols  = Sixth
```

#### Recommended Breakpoints

```scss
// Mobile-first breakpoints
$breakpoints: (
  'sm': 640px,   // Large phones
  'md': 768px,   // Tablets
  'lg': 1024px,  // Small laptops
  'xl': 1280px,  // Desktops
  '2xl': 1536px  // Large screens
);
```

| Breakpoint | Columns | Gutter | Container Max-Width |
|------------|---------|--------|---------------------|
| < 640px | 4 | 16px | 100% (padding: 16px) |
| 640-767px | 6 | 24px | 640px |
| 768-1023px | 12 | 24px | 768px |
| 1024-1279px | 12 | 32px | 1024px |
| 1280-1535px | 12 | 32px | 1280px |
| 1536px+ | 12 | 32px | 1440px |

#### Grid Gutter Scale

```scss
$gutter-sm: 1rem;     // 16px - Mobile
$gutter-md: 1.5rem;   // 24px - Tablet
$gutter-lg: 2rem;     // 32px - Desktop
```

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Align elements to grid columns | Place elements arbitrarily |
| Use consistent gutters | Vary spacing randomly |
| Let content breathe in gutters | Fill every pixel |
| Break grid intentionally for emphasis | Accidentally misalign elements |

---

### 2.2 Content Rhythm & Pacing

Rhythm creates flow and prevents monotony.

#### Vertical Rhythm

Maintain consistent spacing based on line-height units:

```scss
// Base line-height: 1.5 × 16px = 24px
// All vertical spacing should be multiples of 24px

$rhythm-1: 24px;   // 1 unit
$rhythm-2: 48px;   // 2 units
$rhythm-3: 72px;   // 3 units
$rhythm-4: 96px;   // 4 units
```

#### Section Pacing Pattern

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

#### Rhythm Variety

| Pattern | Visual Effect | Use Case |
|---------|---------------|----------|
| Even rhythm | Calm, predictable | Documentation, forms |
| Accelerating | Building energy | Sales pages, countdown |
| Decelerating | Slowing down, conclusion | Thank you pages |
| Varied | Dynamic, engaging | Landing pages, portfolios |

---

### 2.3 Section Transitions

How sections connect affects flow and readability.

#### Transition Techniques

| Technique | CSS/Visual Approach | Best For |
|-----------|---------------------|----------|
| Color shift | Background color change | Separating major sections |
| Full-bleed divider | HR or decorative line | Subtle separation |
| Diagonal cut | `clip-path` or SVG | Dynamic, energetic sites |
| Wave/organic | SVG wave shapes | Friendly, approachable brands |
| Gradient blend | Linear gradient overlap | Smooth premium feel |
| Card emergence | Cards elevated from section | Feature highlights |
| Whitespace gap | Large margin only | Minimal designs |

#### Example: Diagonal Transition

```css
.section-diagonal::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 100px;
  background: var(--color-bg-previous);
  clip-path: polygon(0 0, 100% 0, 100% 0%, 0 100%);
}
```

---

### 2.4 Full-Bleed vs Contained Layouts

#### Definitions

| Layout | Description | Use Case |
|--------|-------------|----------|
| **Contained** | Content in max-width container | Text, forms, products |
| **Full-bleed** | Extends to viewport edge | Hero images, backgrounds |
| **Hybrid** | Full-bleed BG, contained content | Most marketing pages |

#### Full-Bleed Best Practices

```html
<!-- Hybrid: Full-bleed background, contained content -->
<section class="w-full bg-blue-900"> <!-- Full-bleed -->
  <div class="max-w-7xl mx-auto px-4"> <!-- Contained -->
    <h2>Section Title</h2>
    <p>Content goes here...</p>
  </div>
</section>
```

#### When to Use Each

| Full-Bleed | Contained |
|------------|-----------|
| Hero sections | Body text |
| Background images | Product grids |
| Color sections | Forms |
| Footer | Navigation content |

---

### 2.5 Asymmetric vs Symmetric Balance

#### Symmetric Balance

```
┌─────────────────────────────────┐
│                                 │
│        [  HEADING  ]            │
│                                 │
│   [Card]    [Card]    [Card]    │
│                                 │
│        [  BUTTON  ]             │
│                                 │
└─────────────────────────────────┘
```

**Characteristics:**
- Centered, mirrored
- Stable, formal, trustworthy
- Best for: Pricing tables, feature grids, hero sections

#### Asymmetric Balance

```
┌─────────────────────────────────┐
│                                 │
│  [IMAGE                ]        │
│  [        LARGE        ]  TEXT  │
│  [        IMAGE        ]  goes  │
│                           here  │
│                                 │
│             [BUTTON]            │
└─────────────────────────────────┘
```

**Characteristics:**
- Off-center, dynamic
- Modern, creative, energetic
- Best for: Hero sections, about pages, editorial

#### When to Use Each

| Symmetric | Asymmetric |
|-----------|------------|
| E-commerce product grids | Agency portfolios |
| Pricing pages | Blog layouts |
| Comparison tables | Hero sections with personality |
| Form layouts | Case studies |

---

## 3. Content Density & Scannability

### 3.1 Information Scent

Users follow "information scent" - clues that suggest where to find what they need.

#### Strengthening Information Scent

| Technique | Example | Effect |
|-----------|---------|--------|
| Clear labels | "Air Conditioners" not "Products" | Users know what to expect |
| Descriptive links | "View all cooling products" not "Click here" | Clear destination |
| Consistent icons | Snowflake = cooling everywhere | Pattern recognition |
| Breadcrumbs | Home > HVAC > Air Conditioners | Location awareness |
| Preview content | Product thumbnails, excerpts | Confidence to click |

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Use specific, descriptive headings | Use vague headings like "Solutions" |
| Show content previews | Hide everything behind clicks |
| Maintain consistent navigation labels | Rename sections on different pages |
| Use recognizable icons | Invent new icon meanings |

---

### 3.2 Progressive Disclosure

Show only what's needed at each level. Reveal complexity on demand.

#### Disclosure Levels

```
LEVEL 1: Overview (visible immediately)
├── Product name, image, price
└── "Add to Cart" button

LEVEL 2: Details (one click/hover)
├── Product specifications
├── Color/size options
└── Stock availability

LEVEL 3: Deep info (intentional navigation)
├── Full technical specs
├── Installation guides
└── Warranty information
```

#### Implementation Patterns

| Pattern | Interaction | Use Case |
|---------|-------------|----------|
| Accordion | Click to expand | FAQ, specifications |
| Tabs | Click to switch views | Product details, account sections |
| Modal/Dialog | Click to overlay | Quick view, confirmations |
| Mega menu | Hover to reveal | Navigation categories |
| Tooltip | Hover for info | Field hints, icon labels |
| Read more | Click to expand text | Long descriptions |

---

### 3.3 Scannability Patterns

#### F-Pattern Optimization (Product Listings)

```
┌─────────────────────────────────────────┐
│ [FILTERS]  │  [Sort ▼]  [Grid│List]     │ ← Top bar scanned
│            │                             │
│ Category   │  [PROD 1]  [PROD 2]  [PROD 3]
│ Price      │   Name      Name      Name  │ ← Row 1 scanned
│ Brand      │   $XXX      $XXX      $XXX  │
│ Rating     │                             │
│            │  [PROD 4]  [PROD 5]  [PROD 6]
│ [Apply]    │   Name      Name      Name  │ ← Less attention
│            │   $XXX      $XXX      $XXX  │
└─────────────────────────────────────────┘
```

**Key insight:** First row gets the most attention. Feature best products there.

#### Z-Pattern Optimization (Landing Pages)

```
┌─────────────────────────────────────────┐
│ [LOGO]              [Nav] [Nav] [CTA]   │ ← Start top-left
│                                         │
│        HERO HEADLINE                    │ ← Top center
│        Subheadline text                 │
│                                         │
│            ↘                            │ ← Diagonal scan
│               ↘                         │
│                  ↘                      │
│   Social proof     [PRIMARY CTA]        │ ← End bottom-right
└─────────────────────────────────────────┘
```

**Key insight:** Place CTA at the terminal point (bottom-right area).

---

### 3.4 Scannable Content Formatting

#### Typography for Scanning

| Element | Purpose | Guidelines |
|---------|---------|------------|
| Headlines | Entry points | Clear, benefit-focused |
| Subheads | Section markers | Every 2-3 paragraphs |
| Bold text | Inline emphasis | Key terms only, 2-3 per paragraph max |
| Bullet lists | Quick info | 3-7 items, parallel structure |
| Numbers | Sequences, rankings | Use for ordered information |
| Pull quotes | Highlighted insights | Important statistics, testimonials |

#### Content Chunking Rules

```
OPTIMAL PARAGRAPH LENGTH
├── Web: 2-4 sentences (40-80 words)
├── Mobile: 1-3 sentences (20-50 words)
└── Marketing: 1-2 sentences (15-35 words)

LIST LENGTH
├── Minimum: 3 items (fewer feels incomplete)
├── Optimal: 5-7 items (cognitive sweet spot)
└── Maximum: 9 items (break into categories above)
```

---

### 3.5 Above the Fold Optimization

"Above the fold" = visible without scrolling (typically top 600-800px)

#### What MUST Be Above the Fold

| Page Type | Essential Elements |
|-----------|-------------------|
| Homepage | Logo, value prop, primary CTA, nav |
| Product page | Image, name, price, add to cart |
| Category page | Category name, filters access, first products |
| Landing page | Headline, subhead, CTA, trust indicator |
| Checkout | Progress indicator, current step form |

#### Above-the-Fold Checklist

```
[ ] Primary value proposition visible
[ ] Main CTA button visible
[ ] Visual hierarchy clear at a glance
[ ] Navigation accessible
[ ] No layout shift after load (CLS)
[ ] Key image/hero fully visible
[ ] Trust indicators visible (for transactional pages)
```

---

## 4. Responsive Patterns

### 4.1 Mobile-First Considerations

Start with mobile constraints, then enhance for larger screens.

#### Mobile-First Principles

| Principle | Mobile Approach | Desktop Enhancement |
|-----------|-----------------|---------------------|
| Content priority | Show essential only | Add secondary content |
| Navigation | Hamburger menu | Expanded nav bar |
| Layout | Single column | Multi-column grids |
| Touch vs mouse | Larger tap targets | Hover states |
| Performance | Critical CSS first | Full styles loaded |

#### Mobile-First CSS Pattern

```scss
// Base styles = mobile
.card {
  padding: 1rem;
  flex-direction: column;
}

// Enhance for tablet
@media (min-width: 768px) {
  .card {
    padding: 1.5rem;
    flex-direction: row;
  }
}

// Enhance for desktop
@media (min-width: 1024px) {
  .card {
    padding: 2rem;
  }
}
```

---

### 4.2 Touch Target Sizing

Fingers are imprecise. Design for touch.

#### Minimum Touch Target Sizes

| Standard | Minimum Size | Recommended |
|----------|--------------|-------------|
| Apple HIG | 44 x 44 pt | 44 x 44 pt |
| Material Design | 48 x 48 dp | 48 x 48 dp |
| WCAG 2.5.5 | 44 x 44 CSS px | 48 x 48 CSS px |

#### Touch Target Spacing

```
MINIMUM GAP: 8px between touch targets

[Button]  8px  [Button]  8px  [Button]

Stacked:
[Button       ]
      8px
[Button       ]
```

#### Do's and Don'ts

| Do | Don't |
|----|-------|
| Make buttons at least 44px tall | Use tiny 24px buttons on mobile |
| Add padding to increase tap area | Rely on text-only links |
| Space touch targets 8px+ apart | Crowd buttons together |
| Use full-width buttons on mobile | Use small inline buttons |

---

### 4.3 Content Reflow Strategies

How content adapts across breakpoints.

#### Common Reflow Patterns

**1. Stack (Column Collapse)**
```
Desktop:                    Mobile:
[A] [B] [C]         →       [A]
                            [B]
                            [C]
```

**2. Priority (Hide Secondary)**
```
Desktop:                    Mobile:
[Main Content] [Sidebar] →  [Main Content]
                            [Sidebar as accordion]
```

**3. Resize (Scale Down)**
```
Desktop:                    Mobile:
[   Large Card   ]   →      [ Smaller Card ]
```

**4. Transform (Change Layout)**
```
Desktop:                    Mobile:
| Table Row |        →      ┌──────────┐
| Data | Data |             │ Card     │
                            │ Layout   │
                            └──────────┘
```

#### Reflow Decision Matrix

| Content Type | Mobile Strategy |
|--------------|-----------------|
| Product grid | 2 cols → 1 col |
| Data table | Card layout or horizontal scroll |
| Hero with text + image | Stack vertically |
| Navigation | Collapse to hamburger |
| Sidebar filters | Slide-out drawer |
| Multi-column text | Single column |
| Image gallery | Swipeable carousel |

---

### 4.4 Navigation Adaptation

#### Navigation Pattern by Screen Size

| Screen Size | Pattern | Description |
|-------------|---------|-------------|
| Mobile (< 768px) | Hamburger drawer | Off-canvas slide-in menu |
| Tablet (768-1024px) | Condensed nav | Priority items visible, "More" dropdown |
| Desktop (> 1024px) | Full nav bar | All primary items visible |
| Large (> 1280px) | Mega menu | Full category exposure on hover |

#### Hamburger Menu Best Practices

```
DO:
├── Use recognizable ☰ icon
├── Add "Menu" label for clarity
├── Animate open/close transition
├── Allow close by tapping overlay
├── Trap focus inside menu when open
└── Support ESC key to close

DON'T:
├── Hide critical nav items only in hamburger
├── Use non-standard icons
├── Open full-page blocking menu
├── Forget close button
└── Ignore keyboard accessibility
```

---

## 5. Page Type Patterns

### 5.1 Landing Page Composition

Goal: Convert visitors with focused message and single CTA.

#### Hero-Centric Pattern

```
┌─────────────────────────────────────────┐
│ [Logo]                    [CTA Button]  │
├─────────────────────────────────────────┤
│                                         │
│            HERO HEADLINE                │
│     Supporting subheadline text         │
│                                         │
│         [Primary CTA Button]            │
│                                         │
│        [Hero Image or Video]            │
│                                         │
├─────────────────────────────────────────┤
│   [Logo]  [Logo]  [Logo]  [Logo]        │
│        "Trusted by 500+ companies"      │
├─────────────────────────────────────────┤
│                                         │
│      Problem Statement Section          │
│                                         │
├─────────────────────────────────────────┤
│   [Feature 1]  [Feature 2]  [Feature 3] │
│   Description  Description  Description │
├─────────────────────────────────────────┤
│                                         │
│   "Testimonial quote from customer"     │
│        — Customer Name, Title           │
│                                         │
├─────────────────────────────────────────┤
│         Final CTA Section               │
│      [Primary CTA]  [Secondary]         │
└─────────────────────────────────────────┘
```

#### Section Order Options

| Pattern | Sections | Best For |
|---------|----------|----------|
| Trust-first | Hero → Logos → Features → Pricing | B2B SaaS |
| Problem-solution | Hero → Problem → Solution → Proof | New products |
| Story-driven | Hero → Journey → Benefits → CTA | Brand building |
| Conversion-focused | Hero → Proof → Pricing → CTA | Sales pages |

---

### 5.2 Product Listing Layouts (PLP)

Goal: Enable product discovery and comparison.

#### Standard Grid Layout

```
┌─────────────────────────────────────────┐
│ Breadcrumb: Home > Category > Subcategory
├─────────────────────────────────────────┤
│ CATEGORY TITLE              X products  │
├─────────────────────────────────────────┤
│ [Filters]  │  Sort: [Dropdown ▼]        │
│            │  View: [Grid] [List]       │
│ □ Brand A  ├────────────────────────────│
│ □ Brand B  │ [PROD] [PROD] [PROD] [PROD]│
│            │  $XXX   $XXX   $XXX   $XXX │
│ Price      │                            │
│ $0-$500    │ [PROD] [PROD] [PROD] [PROD]│
│ $500-$1000 │  $XXX   $XXX   $XXX   $XXX │
│            │                            │
│ [Apply]    │ [PROD] [PROD] [PROD] [PROD]│
│            │  $XXX   $XXX   $XXX   $XXX │
├────────────┴────────────────────────────┤
│        [1] [2] [3] ... [Next →]         │
└─────────────────────────────────────────┘
```

#### Grid Configurations

| Products/Row | Best For | Card Size |
|--------------|----------|-----------|
| 2 | Featured, large images | Large (50% width) |
| 3 | Standard catalog | Medium (33% width) |
| 4 | Dense catalogs | Small (25% width) |
| 5+ | Marketplace, many SKUs | Compact (20% width) |

#### Product Card Hierarchy

```
┌─────────────────────┐
│ [     IMAGE     ]   │ ← Primary attention
│                     │
│ [Badge: SALE]       │ ← Urgency/status
├─────────────────────┤
│ Product Name        │ ← Identification
│ Brand Name          │ ← Trust/filtering
│                     │
│ ★★★★☆ (42)         │ ← Social proof
│                     │
│ $299 ̶$̶3̶9̶9̶         │ ← Price (key decision)
│                     │
│ [Add to Cart]       │ ← Action
└─────────────────────┘
```

---

### 5.3 Product Detail Page (PDP)

Goal: Provide information to complete purchase decision.

#### Standard PDP Layout

```
┌─────────────────────────────────────────┐
│ Breadcrumb: Home > AC > Split Systems   │
├─────────────────────────────────────────┤
│                           │             │
│  [    MAIN IMAGE    ]     │ Product Name│
│                           │ Brand       │
│  [thumb][thumb][thumb]    │             │
│                           │ ★★★★☆ (128) │
│                           │             │
│                           │ $1,299.00   │
│                           │ In Stock ✓  │
│                           │             │
│                           │ Variants:   │
│                           │ [Size ▼]    │
│                           │             │
│                           │ Qty: [1][+] │
│                           │             │
│                           │ [ADD TO CART]
│                           │ [♡ Wishlist]│
├─────────────────────────────────────────┤
│ [Description] [Specs] [Reviews] [Q&A]   │
├─────────────────────────────────────────┤
│                                         │
│ RELATED PRODUCTS                        │
│ [PROD] [PROD] [PROD] [PROD]            │
│                                         │
└─────────────────────────────────────────┘
```

#### Image Gallery Patterns

| Pattern | Interaction | Best For |
|---------|-------------|----------|
| Thumbnail carousel | Click to change main | Most products |
| Sticky image | Scrolls with content | Long descriptions |
| Zoom on hover | Magnify inline | Detail-oriented products |
| Full-screen gallery | Click for lightbox | Visual products (furniture, fashion) |

---

### 5.4 Dashboard Layouts

Goal: Surface key metrics and enable quick actions.

#### Executive Dashboard Pattern

```
┌─────────────────────────────────────────┐
│ [Logo]  Dashboard     [Search] [Profile]│
├─────────────────────────────────────────┤
│ Welcome back, John        [+ New Order] │
├─────────────────────────────────────────┤
│ [KPI Card] [KPI Card] [KPI Card] [KPI]  │
│  Revenue    Orders    Customers  AOV    │
│  $24,500    156       1,234     $157    │
│  ↑ 12%      ↑ 8%      ↑ 3%     ↓ 2%    │
├─────────────────────────────────────────┤
│                          │              │
│  REVENUE CHART           │ TOP PRODUCTS │
│  [Line Chart]            │ 1. Product A │
│                          │ 2. Product B │
│                          │ 3. Product C │
├──────────────────────────┴──────────────┤
│                                         │
│  RECENT ORDERS                          │
│  ┌───────┬──────────┬─────────┬───────┐ │
│  │ Order │ Customer │ Amount  │ Status│ │
│  ├───────┼──────────┼─────────┼───────┤ │
│  │ #1234 │ John Doe │ $299    │ ● New │ │
│  └───────┴──────────┴─────────┴───────┘ │
└─────────────────────────────────────────┘
```

#### Dashboard Card Types

| Card Type | Content | Size |
|-----------|---------|------|
| KPI/Metric | Single number + trend | Small (1/4 width) |
| Chart | Data visualization | Medium (1/2 width) |
| Table | Tabular data | Large (full or 2/3) |
| Activity | Recent events feed | Medium sidebar |

---

### 5.5 Form Page Layouts

Goal: Minimize friction, guide completion.

#### Single-Column Form (Best Practice)

```
┌─────────────────────────────────────────┐
│                                         │
│        FORM TITLE                       │
│     Brief explanation text              │
│                                         │
├─────────────────────────────────────────┤
│                                         │
│   Email *                               │
│   [                               ]     │
│                                         │
│   Password *                            │
│   [                               ] 👁  │
│   ✓ At least 8 characters               │
│                                         │
│   Confirm Password *                    │
│   [                               ]     │
│                                         │
│   ☐ I agree to the Terms of Service     │
│                                         │
│   [       CREATE ACCOUNT       ]        │
│                                         │
│   Already have an account? Sign in      │
│                                         │
└─────────────────────────────────────────┘
```

#### Multi-Step Form Pattern

```
Step 1          Step 2          Step 3
━━━━━━━━━       ──────          ──────
Shipping   →    Payment    →    Review

┌─────────────────────────────────────────┐
│ STEP 1 OF 3: Shipping Information       │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
├─────────────────────────────────────────┤
│                                         │
│   [Form fields for this step]           │
│                                         │
├─────────────────────────────────────────┤
│   [← Back]              [Continue →]    │
└─────────────────────────────────────────┘
```

#### Form UX Patterns

| Pattern | Use Case | Example |
|---------|----------|---------|
| Inline validation | Real-time feedback | Check email format on blur |
| Progress indicator | Multi-step forms | "Step 2 of 4" |
| Error summary | Multiple errors | List at top of form |
| Autofill support | Speed completion | `autocomplete` attributes |
| Smart defaults | Reduce typing | Country from IP, prefilled dates |

---

## 6. Quick Reference

### 6.1 Visual Hierarchy Checklist

```
[ ] Clear focal point in each viewport
[ ] Size hierarchy follows importance
[ ] Color contrast guides attention
[ ] Whitespace groups related content
[ ] Typography scale is consistent
[ ] Reading flow is natural (F/Z pattern)
[ ] CTAs are visually prominent
```

### 6.2 Layout Checklist

```
[ ] Grid system applied consistently
[ ] Responsive breakpoints tested
[ ] Touch targets are 44px minimum
[ ] Content reflows gracefully
[ ] Navigation adapts to screen size
[ ] No horizontal scroll on mobile
[ ] Loading states prevent layout shift
```

### 6.3 Content Checklist

```
[ ] Above-fold content is compelling
[ ] Progressive disclosure reduces overwhelm
[ ] Text is scannable (headings, lists, bold)
[ ] Line length is 45-75 characters
[ ] Information scent is strong
[ ] Breadcrumbs show location
[ ] Content chunks are digestible
```

### 6.4 Spacing Quick Reference

```
COMPONENT INTERNAL PADDING
├── Buttons: 12-16px vertical, 24-32px horizontal
├── Cards: 16-24px all sides
├── Inputs: 12-16px vertical, 12-16px horizontal
└── Modals: 24-32px all sides

COMPONENT EXTERNAL MARGINS
├── Between form fields: 16-24px
├── Between paragraphs: 16-24px
├── Between sections: 48-80px
├── Between page sections: 80-120px
└── Page edge padding: 16px (mobile), 24-32px (desktop)
```

### 6.5 Typography Quick Reference

```
FONT SIZES
├── Caption/Label: 12-14px
├── Body: 16px (minimum)
├── Lead/Large body: 18-20px
├── H3: 20-24px
├── H2: 24-32px
├── H1: 32-40px
└── Display: 40-64px

LINE HEIGHT
├── Headlines: 1.1-1.3
├── Body text: 1.5-1.75
└── Dense UI: 1.3-1.5

FONT WEIGHT
├── Body: 400 (regular)
├── Emphasis: 500-600 (medium/semibold)
└── Headlines: 600-700 (semibold/bold)
```

### 6.6 Color Usage Quick Reference

```
PRIMARY COLOR
├── Primary buttons
├── Active states
├── Links (with underline)
└── Key UI elements

SECONDARY COLOR
├── Secondary buttons
├── Supporting elements
├── Section backgrounds
└── Hover states

NEUTRAL COLORS
├── Text (dark neutrals)
├── Backgrounds (light neutrals)
├── Borders
└── Disabled states

SEMANTIC COLORS
├── Success: Green (#22c55e)
├── Warning: Amber (#f59e0b)
├── Error: Red (#ef4444)
└── Info: Blue (#3b82f6)
```

---

## References

- Material Design Guidelines (Google)
- Human Interface Guidelines (Apple)
- WCAG 2.1 Accessibility Guidelines
- Nielsen Norman Group Research
- Smashing Magazine UX Articles
- Baymard Institute E-commerce Research
- A Book Apart Series (Web Typography, Responsive Design)
- Refactoring UI by Adam Wathan & Steve Schoger

---

*Document created: January 2026*
*For: ClimaSite HVAC E-Commerce Platform*
