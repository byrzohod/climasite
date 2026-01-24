# Plan 21F: Animation & Interaction Audit

## Overview

### Problem Statement

ClimaSite currently suffers from **over-animation** - a condition where decorative motion overwhelms the user experience rather than enhancing it. Every section animates on scroll, multiple parallax layers compete for attention, and the overall effect is chaotic rather than premium.

**Current State:**
- 22+ animation types in RevealDirective alone
- Parallax running on scroll, mouse, or both simultaneously
- FloatingDirective with gentle/medium/pronounced variants
- TiltEffectDirective with 3D perspective and glare
- Hero gradient blob animation (20s infinite cycle)
- Brand marquee (30s infinite scroll)
- Confetti, flying cart, count-up numbers

### Design Direction: "Nordic Tech"

**Philosophy:** Animation should communicate, not decorate.

| Principle | Description |
|-----------|-------------|
| **Purposeful** | Every animation must have a reason to exist |
| **Subtle** | Movement should be felt, not noticed |
| **Meaningful** | Animation communicates state or provides feedback |
| **Restrained** | Less is more; silence makes motion impactful |

### Goals

1. **Reduce cognitive load** - Remove animations that add visual noise
2. **Preserve function** - Keep animations that provide user feedback
3. **Improve performance** - Fewer animations = better performance
4. **Maintain accessibility** - Enhance reduced motion support
5. **Create hierarchy** - Important actions get motion; static content stays still

### Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Animation directives per page | 15-25 | 3-5 |
| Parallax layers | 2-3 simultaneous | 0-1 optional |
| Scroll-triggered animations | Every section | Hero + key CTAs only |
| Continuous background animations | 3+ | 0-1 subtle |
| Lighthouse Performance | ~75 | 90+ |
| Time to Interactive | ~3.5s | <2.5s |

### Estimated Effort

| Phase | Effort | Priority |
|-------|--------|----------|
| Phase 1: Remove decorative animations | 4 hours | P0 |
| Phase 2: Simplify parallax | 2 hours | P0 |
| Phase 3: Refine functional animations | 3 hours | P1 |
| Phase 4: Update reduced motion | 2 hours | P1 |
| Phase 5: Performance verification | 2 hours | P1 |
| Phase 6: Documentation | 1 hour | P2 |
| **Total** | **14 hours** | |

---

## Animation Audit

### Complete Animation Inventory

#### Directive-Based Animations

| Animation | Directive | Location | Decision | Reasoning |
|-----------|-----------|----------|----------|-----------|
| fade | RevealDirective | Multiple sections | **REMOVE** | Overused; creates visual noise |
| fade-up | RevealDirective | Multiple sections | **MODIFY** | Keep for hero only, 1 element |
| fade-down | RevealDirective | Headers | **REMOVE** | Unnecessary decoration |
| fade-left | RevealDirective | Alternating sections | **REMOVE** | Adds no value |
| fade-right | RevealDirective | Alternating sections | **REMOVE** | Adds no value |
| scale | RevealDirective | Cards | **REMOVE** | Distracting on scroll |
| scale-up | RevealDirective | Featured items | **REMOVE** | Competes for attention |
| blur | RevealDirective | Hero text | **REMOVE** | Performance heavy, decorative |
| blur-up | RevealDirective | Hero | **REMOVE** | Performance heavy |
| slide-up | RevealDirective | Sections | **REMOVE** | Too aggressive for content |
| slide-down | RevealDirective | Sections | **REMOVE** | Disorienting |
| slide-left | RevealDirective | Side panels | **REMOVE** | Unnecessary |
| slide-right | RevealDirective | Side panels | **REMOVE** | Unnecessary |
| flip-up | RevealDirective | Cards | **REMOVE** | Overly dramatic |
| flip-down | RevealDirective | Cards | **REMOVE** | Overly dramatic |
| zoom-in | RevealDirective | Images | **REMOVE** | Distracting |
| zoom-out | RevealDirective | Backgrounds | **REMOVE** | Distracting |
| rotate-left | RevealDirective | Decorative | **REMOVE** | Pure decoration |
| rotate-right | RevealDirective | Decorative | **REMOVE** | Pure decoration |
| clip-left/right/top/bottom | RevealDirective | Text reveals | **REMOVE** | Overly complex |
| scroll parallax | ParallaxDirective | Hero, sections | **MODIFY** | Remove from all but hero background |
| mouse parallax | ParallaxDirective | Hero elements | **REMOVE** | Distracting, performance heavy |
| combined parallax | ParallaxDirective | Multiple layers | **REMOVE** | Too complex |
| gentle float | FloatingDirective | Background blobs | **REMOVE** | Constant motion is distracting |
| medium float | FloatingDirective | Decorative elements | **REMOVE** | Not purposeful |
| pronounced float | FloatingDirective | Hero elements | **REMOVE** | Competes with content |
| 3D tilt | TiltEffectDirective | Product cards | **REMOVE** | Gimmicky, not professional |
| tilt with glare | TiltEffectDirective | Featured products | **REMOVE** | Excessive |
| count-up | CountUpDirective | Statistics | **KEEP** | Provides value, draws attention to data |

#### Service-Based Animations

| Animation | Service | Location | Decision | Reasoning |
|-----------|---------|----------|----------|-----------|
| Flying cart | FlyingCartService | Add to cart | **KEEP** | Functional feedback - confirms action |
| Cart bump | FlyingCartService | Cart icon | **KEEP** | Subtle feedback - indicates change |
| Confetti burst | ConfettiService | Order confirmation | **KEEP** | Celebration moment - appropriate |
| Sparkle fallback | ConfettiService | Reduced motion | **KEEP** | Accessible alternative |

#### CSS-Based Animations

| Animation | File | Location | Decision | Reasoning |
|-----------|------|----------|----------|-----------|
| Hero gradient blob | _animations.scss | Hero background | **REMOVE** | Constant motion, distracting |
| Aurora movement | _animations.scss | Backgrounds | **REMOVE** | Decorative only |
| Brand marquee | _animations.scss | Partner logos | **MODIFY** | Pause on hover, slower (60s), or static |
| Morph blob | _animations.scss | Decorative | **REMOVE** | Not purposeful |
| Float animations | _animations.scss | Background | **REMOVE** | Constant, distracting |
| Gradient flow | _animations.scss | Buttons, headers | **REMOVE** | Too flashy |
| Glow pulse | _animations.scss | CTAs | **REMOVE** | Overdone, tacky |
| Shimmer | _animations.scss | Skeleton loading | **KEEP** | Functional - indicates loading |
| Spin | _animations.scss | Loading spinners | **KEEP** | Functional - indicates loading |
| Pulse | _animations.scss | Loading states | **KEEP** | Functional - indicates loading |
| Modal enter/exit | _animations.scss | Modals | **KEEP** | Functional - context change |
| Backdrop fade | _animations.scss | Modals | **KEEP** | Functional - focus attention |
| Toast enter | _animations.scss | Notifications | **KEEP** | Functional - draws attention |

#### Transition-Based Animations

| Transition | Location | Decision | Reasoning |
|------------|----------|----------|-----------|
| Button hover scale | Buttons | **MODIFY** | Reduce scale (1.02 max) |
| Card hover lift | Product cards | **MODIFY** | Subtle shadow change only |
| Link underline | Navigation | **KEEP** | Functional feedback |
| Input focus | Forms | **KEEP** | Functional - indicates focus |
| Tab transitions | Tab components | **KEEP** | Functional - shows selection |
| Accordion expand | FAQ, filters | **KEEP** | Functional - reveals content |
| Dropdown open | Menus | **KEEP** | Functional - shows options |

---

## Animation Philosophy

### When to Animate

Animation is appropriate when it:

1. **Communicates state change**
   - Loading → Loaded
   - Open → Closed
   - Selected → Unselected
   - Enabled → Disabled

2. **Provides feedback**
   - Button clicked (subtle press)
   - Item added to cart (flying animation)
   - Form submitted (success state)
   - Error occurred (shake, attention)

3. **Guides attention**
   - New notification appears
   - Important action becomes available
   - User needs to take action

4. **Establishes hierarchy**
   - Hero section entry (one-time, subtle)
   - Modal focus (draws eye to center)

5. **Celebrates success**
   - Order completed (confetti)
   - Achievement unlocked
   - Milestone reached

### When NOT to Animate

Animation is inappropriate when it:

1. **Decorates for decoration's sake**
   - Background blobs floating
   - Parallax on every scroll
   - Continuous gradient animations

2. **Competes for attention**
   - Multiple animated elements visible simultaneously
   - Animation on static content sections
   - Hover effects on non-interactive elements

3. **Slows comprehension**
   - Text that fades in letter by letter
   - Content that reveals line by line
   - Information hidden behind animation

4. **Creates motion sickness**
   - Large parallax movements
   - Continuous floating/bobbing
   - Rapid or jarring transitions

5. **Impacts performance**
   - Animations on mobile devices
   - Multiple simultaneous CSS animations
   - JavaScript-based animation loops

### Duration Guidelines

| Category | Duration | Use Case |
|----------|----------|----------|
| **Micro** | 100-150ms | Button states, hover effects, toggles |
| **Standard** | 200-300ms | Modals, dropdowns, tooltips, focus states |
| **Emphasis** | 400-500ms | Page transitions, cart animations, celebrations |
| **Extended** | 600-800ms | Complex reveals (hero only), loading sequences |

**Rule:** When in doubt, go faster. Users prefer snappy interfaces.

### Easing Guidelines

| Easing | CSS | Use Case |
|--------|-----|----------|
| **ease-out** | `cubic-bezier(0.0, 0, 0.2, 1)` | Enter animations, opening |
| **ease-in** | `cubic-bezier(0.4, 0, 1, 1)` | Exit animations, closing |
| **ease-in-out** | `cubic-bezier(0.4, 0, 0.2, 1)` | State changes, morphing |
| **linear** | `linear` | Continuous animations (spinners only) |

**Never use:** Spring, bounce, elastic for UI elements (too playful for Nordic Tech).

---

## Task List

### Phase 1: Remove Decorative Animations (P0)

- [x] **TASK-21F-001**: Remove all RevealDirective usages except hero section
  - Simplified RevealDirective to only support fade, fade-up, fade-down
  - Updated all components using removed animation types (fade-left, fade-right, scale, etc.)
  - Content is now visible without excessive animation

- [x] **TASK-21F-002**: Remove FloatingDirective entirely
  - Removed all `appFloating` usages from home.component.ts and final-cta.component.ts
  - Deleted FloatingDirective file
  - All floating animations removed from hero and CTA sections

- [x] **TASK-21F-003**: Remove TiltEffectDirective entirely
  - Removed `appTiltEffect` from category cards in home.component.ts
  - Deleted TiltEffectDirective file
  - Cards now have simpler hover states

- [x] **TASK-21F-004**: Remove continuous background animations
  - Removed float keyframe animation from hero gradients
  - Removed ctaGradient keyframe animation from CTA section
  - Removed float and pulse animations from final-cta shapes
  - Hero background gradients are now static decorative elements

- [x] **TASK-21F-005**: Simplify brand marquee
  - Changed duration from 30s to 45s (slowed down)
  - Pause on hover already implemented (`.paused` class)
  - Marquee provides useful brand awareness without being distracting

### Phase 2: Simplify Parallax (P0)

- [x] **TASK-21F-006**: Remove ParallaxDirective from all components
  - Removed `appParallax` from home.component.ts (hero, CTA)
  - Removed `appParallax` from resources.component.ts
  - Removed `appParallax` from promotion-detail.component.ts
  - Removed `appParallax` from brand-detail.component.ts
  - Removed `appParallax` from category-header.component.ts
  - Removed `appParallax` from final-cta.component.ts
  - **Decision**: Parallax entirely removed for cleaner, less distracting UX

- [x] **TASK-21F-007**: Delete ParallaxDirective
  - Deleted parallax.directive.ts file entirely
  - Parallax effect was too performance-heavy and distracting
  - Static backgrounds are cleaner and more professional

- [x] **TASK-21F-008**: No mobile detection needed
  - ParallaxDirective removed entirely, no mobile concerns
  - All hero backgrounds now use static positioning (inset: 0)

### Phase 3: Refine Functional Animations (P1)

- [ ] **TASK-21F-009**: Optimize flying cart animation
  - Reduce duration from 600ms to 400ms
  - Simplify arc trajectory (less dramatic)
  - Ensure cart bump is subtle (no scale change, just background pulse)

- [ ] **TASK-21F-010**: Refine confetti animation
  - Reduce particle count from 75 to 50
  - Reduce duration from 3s to 2s
  - Simplify particle shapes (squares only, no ribbons)

- [ ] **TASK-21F-011**: Update button hover states
  - Remove scale transforms (no `scale(1.02)`)
  - Use color/shadow changes only
  - Ensure 150ms transition duration

- [ ] **TASK-21F-012**: Update card hover states
  - Remove lift/translate transforms
  - Use subtle shadow change only
  - Border color change as focus indicator

- [ ] **TASK-21F-013**: Simplify modal animations
  - Remove scale from modal enter (opacity + translate only)
  - Reduce duration to 200ms
  - Keep backdrop fade simple

- [ ] **TASK-21F-014**: Refine toast notifications
  - Slide in from top (not scale)
  - Duration: 200ms enter, 150ms exit
  - Remove bounce/spring easing

- [ ] **TASK-21F-015**: Keep and verify CountUpDirective
  - Ensure only used on statistics/metrics
  - Verify reduced motion behavior
  - Confirm performance on multiple instances

### Phase 4: Update Reduced Motion Support (P1)

- [ ] **TASK-21F-016**: Audit current reduced motion implementation
  - Review AnimationService.prefersReducedMotion()
  - Verify all directives check preference
  - Test with system setting enabled

- [ ] **TASK-21F-017**: Enhance reduced motion CSS
  - Update `_animations.scss` @media query
  - Ensure ALL animations respect preference
  - Keep functional transitions (instant, not removed)

- [ ] **TASK-21F-018**: Add reduced motion toggle in UI
  - Add toggle in accessibility settings
  - Store preference in localStorage
  - Override system setting if explicitly set

- [ ] **TASK-21F-019**: Test reduced motion experience
  - Enable preference and navigate full site
  - Verify no animations remain
  - Ensure site is fully functional

### Phase 5: Performance Verification (P1)

- [ ] **TASK-21F-020**: Run Lighthouse audit before changes
  - Record Performance score
  - Record Time to Interactive
  - Record Cumulative Layout Shift
  - Save report as baseline

- [ ] **TASK-21F-021**: Run Lighthouse audit after changes
  - Compare Performance score (target: +10 points)
  - Compare Time to Interactive (target: -500ms)
  - Compare CLS (target: 0.1 or less)

- [ ] **TASK-21F-022**: Profile animation performance
  - Use Chrome DevTools Performance tab
  - Identify any remaining janky animations
  - Verify GPU acceleration on remaining animations

- [ ] **TASK-21F-023**: Test on low-end devices
  - Test on mobile emulation (4x CPU slowdown)
  - Verify no dropped frames
  - Ensure smooth scrolling

### Phase 6: Cleanup and Documentation (P2)

- [ ] **TASK-21F-024**: Remove unused animation code
  - Delete unused keyframes from `_animations.scss`
  - Remove unused animation utility classes
  - Clean up AnimationService if simplified

- [ ] **TASK-21F-025**: Update directive documentation
  - Document remaining animation directives
  - Add usage guidelines
  - Include examples of appropriate use

- [ ] **TASK-21F-026**: Create animation style guide
  - Document when to animate
  - Document duration/easing standards
  - Add do's and don'ts with examples

- [ ] **TASK-21F-027**: Update CLAUDE.md
  - Add animation philosophy section
  - Reference new style guide
  - Update status table

---

## Technical Specifications

### Animations to Remove Completely

```typescript
// Files to delete
src/app/shared/directives/floating.directive.ts
src/app/shared/directives/tilt-effect.directive.ts

// Remove exports from
src/app/shared/shared.module.ts (if exists)
src/app/shared/index.ts (if exists)
```

### Animations to Modify

#### RevealDirective
```typescript
// Simplify to only support:
export type RevealAnimation = 'fade' | 'fade-up';

// Reduce default duration
duration = input<number>(300); // was 600

// Reduce default distance
distance = input<number>(16); // was 40
```

#### ParallaxDirective
```typescript
// Remove mouse mode entirely
export type ParallaxMode = 'scroll'; // remove 'mouse' | 'both'

// Reduce default speed
speed = input<number>(0.15); // was 0.5

// Add device check
private isTouchDevice = 'ontouchstart' in window;

ngOnInit(): void {
  if (this.isTouchDevice) return; // Skip on mobile
  // ...
}
```

#### FlyingCartService
```typescript
private readonly ANIMATION_DURATION = 400; // was 600
private readonly IMAGE_SIZE = 48; // was 60

// Simplify arc (less dramatic)
const midY = Math.min(startY, endY) - 50; // was -100
```

### CSS Animations to Remove

```scss
// Remove from _animations.scss or mark as deprecated:
@keyframes float { ... }
@keyframes floatSlow { ... }
@keyframes auroraMove { ... }
@keyframes morph { ... }
@keyframes gradientFlow { ... }
@keyframes glowPulse { ... }

// Remove utility classes:
.animate-float
.animate-float-slow
.animate-aurora
.animate-morph
.animate-gradient
.animate-glow-pulse
```

### CSS Animations to Keep

```scss
// Essential loading/feedback animations:
@keyframes shimmer { ... }      // Skeleton loading
@keyframes spin { ... }         // Spinners
@keyframes pulse { ... }        // Loading dots
@keyframes fadeIn { ... }       // Modal/toast
@keyframes fadeOut { ... }      // Modal/toast
@keyframes modalEnter { ... }   // Modals
@keyframes modalExit { ... }    // Modals
@keyframes shake { ... }        // Error feedback
```

### New Animation Patterns Needed

#### Subtle Hero Entrance
```scss
// Single, subtle hero text animation
@keyframes heroTextEnter {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.hero-title {
  animation: heroTextEnter 400ms var(--ease-out) 100ms both;
}

.hero-subtitle {
  animation: heroTextEnter 400ms var(--ease-out) 200ms both;
}

.hero-cta {
  animation: heroTextEnter 400ms var(--ease-out) 300ms both;
}
```

#### Interaction Feedback
```scss
// Button press feedback (no scale)
.btn:active {
  transform: translateY(1px);
  transition: transform 100ms var(--ease-out);
}

// Card focus (shadow only)
.card:hover,
.card:focus-within {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  transition: box-shadow 150ms var(--ease-out);
}
```

### Performance Benchmarks

| Metric | Threshold | How to Measure |
|--------|-----------|----------------|
| Animation FPS | 60fps | Chrome DevTools Performance |
| JS Heap during animation | <10MB increase | Chrome DevTools Memory |
| Paint time per frame | <16ms | Performance monitor |
| Composite layers | <10 per view | Layers panel |
| CLS from animations | 0 | Lighthouse |

---

## Reduced Motion Strategy

### Current Implementation Review

**What exists:**
- `AnimationService.prefersReducedMotion()` signal
- Directives check this signal in `ngOnInit`
- CSS `@media (prefers-reduced-motion: reduce)` in `_animations.scss`

**Gaps:**
- Not all CSS animations respect the preference
- No UI toggle for users
- Some transitions may still be jarring
- Flying cart skips to bump but no visual feedback

### Enhancements Needed

1. **Complete CSS coverage**
   ```scss
   @media (prefers-reduced-motion: reduce) {
     *, *::before, *::after {
       animation-duration: 0.01ms !important;
       animation-iteration-count: 1 !important;
       transition-duration: 0.01ms !important;
       scroll-behavior: auto !important;
     }
     
     // Exceptions: keep these but make instant
     .toast, .modal, .dropdown {
       transition-duration: 0.01ms !important;
     }
   }
   ```

2. **User preference toggle**
   ```typescript
   // In settings/accessibility
   interface AccessibilitySettings {
     reduceMotion: 'system' | 'on' | 'off';
   }
   ```

3. **Enhanced fallbacks**
   - Flying cart: Instant appearance at cart + subtle highlight
   - Confetti: Single sparkle emoji (already implemented)
   - CountUp: Show final number immediately (already implemented)

---

## Dependencies

| This Plan | Depends On | Dependency Type |
|-----------|------------|-----------------|
| TASK-21F-001 | Plan 21A (Typography) complete | Soft - content should be readable without animation |
| TASK-21F-006 | Hero section finalized | Hard - need to know final hero structure |
| TASK-21F-018 | Settings page exists | Soft - can add toggle later |

| Other Plans | Depend On This | Notes |
|-------------|----------------|-------|
| Plan 21G (Polish) | This plan complete | Final polish assumes animations are settled |
| Plan 22 (Launch) | This plan complete | Performance must be verified |

---

## Testing Checklist

### Functional Testing

- [ ] All pages load without JavaScript errors
- [ ] Flying cart animation works on add to cart
- [ ] Confetti plays on order confirmation
- [ ] Modals open and close smoothly
- [ ] Toasts appear and dismiss correctly
- [ ] Dropdowns and accordions function
- [ ] Tab switching works correctly
- [ ] Form focus states visible

### Visual Testing

- [ ] Hero section looks polished without excessive animation
- [ ] Product cards are static until hovered
- [ ] No content shifts during page load
- [ ] No flickering or janky motion anywhere
- [ ] Dark mode animations match light mode

### Accessibility Testing

- [ ] Enable "Reduce motion" in system settings
- [ ] Verify ALL animations stop or become instant
- [ ] Content is immediately visible (no waiting for fade-in)
- [ ] Site is fully navigable and functional
- [ ] Screen reader experience unchanged

### Performance Testing

- [ ] Lighthouse Performance score 90+
- [ ] No dropped frames during scroll (60fps)
- [ ] Time to Interactive under 2.5s
- [ ] Cumulative Layout Shift under 0.1
- [ ] CPU usage reasonable during animations

### Cross-Browser Testing

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

### Device Testing

- [ ] Desktop (1920x1080)
- [ ] Laptop (1440x900)
- [ ] Tablet (768x1024)
- [ ] Mobile (375x812)
- [ ] 4x CPU slowdown (Chrome DevTools)

---

## Rollback Plan

If animation changes cause issues:

1. **Revert directive changes** - Files are in git, simply revert
2. **Restore CSS animations** - Keyframes are preserved in scss
3. **Test incrementally** - Re-add animations one by one to find issue

---

## Success Criteria

The animation audit is complete when:

1. Decorative animations removed (FloatingDirective, TiltEffectDirective deleted)
2. Parallax simplified to single subtle hero background only
3. All functional animations working correctly
4. Reduced motion preference fully respected
5. Lighthouse Performance score 90+
6. No dropped frames during normal usage
7. Documentation updated
8. All tests passing
