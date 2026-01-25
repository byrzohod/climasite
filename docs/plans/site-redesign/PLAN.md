# Plan: ClimaSite Full Visual Overhaul

## Status: Draft

## Overview

Comprehensive redesign of ClimaSite HVAC e-commerce platform to achieve a modern, premium visual identity inspired by best-in-class sites (Apple, Stripe, Linear). Focus on visual style, product pages, checkout flow, and mobile experience while preserving the solid technical foundation.

## Requirements

- [ ] Modern, premium visual identity (Apple/Stripe aesthetic)
- [ ] Improved product page experience with better visual hierarchy
- [ ] Streamlined checkout flow with higher conversion focus
- [ ] Mobile-first responsive design throughout
- [ ] Performance optimization (Core Web Vitals compliance)
- [ ] Maintain accessibility standards (WCAG 2.1 AA)

## Current State Analysis

### Strengths (Keep)
| Asset | Status | Notes |
|-------|--------|-------|
| Design tokens system | ✅ Excellent | 11 color palettes, fluid typography, 50+ animations |
| Tailwind + CSS variables | ✅ Excellent | Well-integrated, dark mode ready |
| Component architecture | ✅ Good | 45+ standalone components, Signal-based |
| i18n infrastructure | ✅ Good | EN/BG/DE support |
| Accessibility base | ✅ Good | ARIA, reduced motion, focus management |

### Weaknesses (Fix)
| Issue | Severity | Location |
|-------|----------|----------|
| Component bloat | HIGH | home.component.ts (2219 lines), header (1567 lines) |
| Hardcoded data | MEDIUM | Testimonials, shipping costs, countries |
| Visual style feels "safe" | HIGH | Needs bolder, more premium aesthetic |
| Mobile UX gaps | MEDIUM | Filter sidebar, specs table, form layouts |
| No lazy loading | MEDIUM | Below-fold sections load immediately |
| Product cards generic | HIGH | Need more visual impact, quick actions |

## Proposed Approach

### Option A: Evolutionary Refresh (Recommended)
**Description:** Enhance existing design system with bolder visual language while keeping technical architecture

**Phases:**
1. Update color palette and typography for premium feel
2. Redesign key components (cards, buttons, forms)
3. Refactor bloated components into smaller units
4. Optimize checkout flow
5. Mobile-first responsive polish

**Pros:**
- Lower risk - builds on proven foundation
- Faster implementation (4-6 weeks)
- No migration issues
- Maintains existing tests

**Cons:**
- May not feel "dramatic" enough
- Some legacy patterns remain

**Estimated Scope:** Medium-Large

### Option B: Design System 2.0
**Description:** Create new design system from scratch with shadcn/ui-inspired components

**Pros:**
- Clean slate, fully modern approach
- Consistent new visual language
- Opportunity to adopt new patterns (Spartan UI)

**Cons:**
- 8-12 weeks implementation
- All components need rewriting
- Testing coverage reset
- Higher risk of bugs

**Estimated Scope:** Large-XL

## Selected Approach

**Option B: Design System 2.0** - Full visual rebuild

**Technical Stack:**
- Custom components + Tailwind CSS (refined from current)
- Angular 19 standalone components with Signals
- Keep existing backend/API integration

**Visual Direction: Warm & Premium**
- Luxury e-commerce aesthetic (think high-end appliance brands)
- Warm color palette (cream, terracotta, bronze accents)
- Sophisticated typography (elegant sans-serif + subtle serifs)
- Rich textures and subtle gradients
- Premium photography treatment
- Dark mode with warm undertones

## Implementation Steps (Option B: Full Rebuild)

### Phase 1: Design System Foundation (Week 1-3)
1. [ ] Create new color palette - warm neutrals (cream, stone, warm gray)
2. [ ] Define accent colors - terracotta, bronze, deep teal
3. [ ] Select premium typography - elegant sans + accent serif
4. [ ] Design new shadow system - warm-tinted, layered depth
5. [ ] Create spacing and layout grid system
6. [ ] Design new icon style (refined line icons)
7. [ ] Document design tokens in Figma/code

### Phase 2: Component Library Rebuild (Week 3-5)
1. [ ] Button system - 6 variants with premium feel
2. [ ] Form inputs - elegant floating labels, warm focus states
3. [ ] Cards - product, feature, testimonial variants
4. [ ] Navigation - header, mega menu, mobile drawer
5. [ ] Modal and drawer components
6. [ ] Loading states and skeletons
7. [ ] Toast and alert system
8. [ ] Create Storybook documentation

### Phase 3: Page Templates (Week 5-7)
1. [ ] Home page - luxury hero, editorial product sections
2. [ ] Product listing - refined grid, elegant filters
3. [ ] Product detail - gallery with zoom, premium specs layout
4. [ ] Cart page - sophisticated layout, upsell section
5. [ ] Checkout - streamlined flow with trust elements
6. [ ] Account pages - clean profile, orders, addresses

### Phase 4: Mobile Experience (Week 7-9)
1. [ ] Mobile-first navigation redesign
2. [ ] Touch-optimized product interactions
3. [ ] Gesture-based gallery navigation
4. [ ] Mobile checkout optimization
5. [ ] PWA enhancements

### Phase 5: Animation & Polish (Week 9-10)
1. [ ] Page transitions
2. [ ] Scroll-triggered reveals
3. [ ] Micro-interactions (hover, click, success states)
4. [ ] Loading animations
5. [ ] Reduced motion alternatives

### Phase 6: Launch Prep (Week 10-12)
1. [ ] Visual QA across all pages/breakpoints
2. [ ] Accessibility audit (WCAG 2.1 AA)
3. [ ] Performance audit (Core Web Vitals)
4. [ ] Cross-browser testing
5. [ ] E2E test updates
6. [ ] Documentation finalization
7. [ ] Staged rollout plan

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Scope creep | High | High | Strict phase boundaries, weekly reviews |
| Regression bugs | Medium | Medium | Maintain test coverage, visual regression tests |
| Mobile issues | Medium | High | Mobile-first development, real device testing |
| Performance degradation | Low | High | Lighthouse checks per PR |
| Timeline slip | Medium | Medium | Buffer time in Phase 5 |

## Dependencies

- [ ] Design decisions finalized before Phase 1
- [ ] Current E2E tests passing
- [ ] Access to real mobile devices for testing
- [ ] Stakeholder availability for reviews

## Testing Strategy

- [ ] Unit tests for new/refactored components
- [ ] Visual regression tests (Playwright screenshots)
- [ ] E2E tests maintained and updated
- [ ] Manual testing on iOS Safari, Android Chrome
- [ ] Accessibility testing (axe-core, screen readers)
- [ ] Performance testing (Lighthouse CI)

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Lighthouse Performance | ~75 | >90 |
| Lighthouse Accessibility | ~85 | >95 |
| Mobile Usability | ~85 | >95 |
| Component file sizes | 2000+ lines | <300 lines max |
| Core Web Vitals LCP | Unknown | <2.5s |
| Core Web Vitals CLS | Unknown | <0.1 |
| Cart abandonment | Unknown | <60% |
| Design consistency | N/A | 100% token usage |

## Timeline Summary

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| 1. Design Foundation | 3 weeks | New design tokens, color system, typography |
| 2. Component Library | 2 weeks | All core components rebuilt |
| 3. Page Templates | 2 weeks | All pages redesigned |
| 4. Mobile Experience | 2 weeks | Mobile-first polish |
| 5. Animation & Polish | 1 week | Micro-interactions, transitions |
| 6. Launch Prep | 2 weeks | QA, testing, documentation |
| **Total** | **12 weeks** | Full redesign complete |

## Open Questions

- [ ] Should we adopt a UI library (Spartan UI) or continue custom?
- [ ] Budget for stock photography/illustrations?
- [ ] Priority: visual polish vs new features?
- [ ] Admin dashboard included in this scope?

## Design Inspiration

### Visual Direction: Warm & Premium

**Color Palette Concept:**
```
Primary Neutrals:
- Cream (#FAF7F2) - backgrounds
- Warm Stone (#E8E2D9) - secondary backgrounds
- Warm Gray (#6B6560) - body text

Accent Colors:
- Terracotta (#C4785A) - primary CTA, links
- Bronze (#8B7355) - secondary accents
- Deep Teal (#2A5A5A) - success, trust elements
- Warm Black (#1C1917) - headings, dark mode base

Status Colors:
- Success: Sage Green (#6B8E6B)
- Warning: Amber (#D4A574)
- Error: Coral (#C75C5C)
```

**Typography Direction:**
- Headings: Plus Jakarta Sans or Satoshi (modern, warm)
- Body: Inter or Source Sans (readable, friendly)
- Accents: Optional serif for luxury touches (Cormorant, Libre Baskerville)

**Visual Characteristics:**
- Generous whitespace with warm tints
- Subtle texture overlays (paper, grain)
- Soft, warm-tinted shadows
- Rounded corners (comfortable, approachable)
- Photography with warm color grading
- Elegant line iconography

### Reference Sites
- **Aesop** - Luxury cosmetics, warm minimalism
- **MENU** - Premium furniture, sophisticated simplicity
- **Sonos** - Premium audio, product-focused warmth
- **Bang & Olufsen** - High-end electronics, refined luxury

### E-commerce Patterns (2025)
- AI-powered personalization
- Single-page checkout
- AR product visualization
- Gesture-based mobile navigation
- Micro-interactions for feedback
- Progressive disclosure

## Next Steps

1. ✅ **Approach selected** - Option B: Full Rebuild with Warm & Premium direction
2. **Create color palette** - finalize warm neutral + accent colors in `_colors.scss`
3. **Select typography** - evaluate Plus Jakarta Sans vs Satoshi for headings
4. **Design key screens** - create mockups for Home, Product Detail, Checkout
5. **Start Phase 1** - implement new design tokens

## Immediate Actions (This Week)

```bash
# 1. Create new branch for redesign
git checkout -b feature/design-system-2.0

# 2. Update color palette
# Edit: src/ClimaSite.Web/src/styles/_colors.scss

# 3. Add new fonts
# Update: angular.json, index.html

# 4. Create design tokens documentation
# Create: docs/design-system/
```
