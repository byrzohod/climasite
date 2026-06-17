# ClimaSite Animation Style Guide

> Companion to Plan 21F (Animation & Interaction Audit). This is the canonical reference for
> motion on ClimaSite. When deciding whether to add or change an animation, start here.

## Philosophy: "Nordic Tech"

**Animation should communicate, not decorate.**

| Principle | Meaning |
|-----------|---------|
| **Purposeful** | Every animation must have a reason to exist. If you cannot name the state change or feedback it conveys, remove it. |
| **Subtle** | Movement should be felt, not noticed. Short distances, small scales, fast durations. |
| **Meaningful** | Motion communicates state or provides feedback — loading, opening, selection, success. |
| **Restrained** | Less is more. Silence makes the motion that remains impactful. |

When in doubt: go faster, move less, or do nothing. A snappy, still interface beats a busy one.

---

## When to Animate

Animation is appropriate when it does one of these jobs:

1. **Communicates a state change** — Loading → Loaded, Open → Closed, Selected → Unselected, Enabled → Disabled.
2. **Provides feedback** — button press, item added to cart (flying cart), form submitted (success), error (shake).
3. **Guides attention** — a new notification appears, an important action becomes available.
4. **Establishes hierarchy** — a one-time, subtle hero entrance; a modal drawing the eye to center.
5. **Celebrates success** — order completed (confetti), a milestone reached.

## When NOT to Animate

Animation is inappropriate — remove it — when it:

1. **Decorates for decoration's sake** — floating background blobs, parallax on every scroll, continuous gradient cycles.
2. **Competes for attention** — multiple animated elements visible at once, animation on static content sections, hover effects on non-interactive elements.
3. **Slows comprehension** — text that fades in letter-by-letter, content revealed line-by-line, information hidden behind motion.
4. **Risks motion sickness** — large parallax movement, continuous floating/bobbing, rapid or jarring transitions.
5. **Hurts performance** — JS animation loops, many simultaneous CSS animations, anything heavy on mobile.

---

## Duration Guidelines

| Category | Duration | Use Case |
|----------|----------|----------|
| **Micro** | 100–150ms | Button states, hover effects, toggles |
| **Standard** | 200–300ms | Modals, dropdowns, tooltips, focus states |
| **Emphasis** | 400–500ms | Page transitions, cart animation, celebrations |
| **Extended** | 600–800ms | Complex reveals (hero only), loading sequences |

**Rule:** When in doubt, go faster. Users prefer snappy interfaces.

## Easing Guidelines

| Easing | CSS | Use Case |
|--------|-----|----------|
| **ease-out** | `cubic-bezier(0.0, 0, 0.2, 1)` | Enter animations, opening |
| **ease-in** | `cubic-bezier(0.4, 0, 1, 1)` | Exit animations, closing |
| **ease-in-out** | `cubic-bezier(0.4, 0, 0.2, 1)` | State changes, morphing |
| **linear** | `linear` | Continuous animations (spinners only) |

**Never use** spring, bounce, or elastic easing for UI elements — too playful for Nordic Tech.

---

## KEEP / REMOVE Inventory (Audit Result)

The 21F audit triaged every animation in the app. The outcome:

### Removed (decorative, deleted in Phases 1–2)

- **Directives deleted entirely:** `FloatingDirective` (gentle/medium/pronounced float), `TiltEffectDirective` (3D tilt + glare), `ParallaxDirective` (scroll/mouse/combined parallax).
- **RevealDirective simplified** from 22+ animation types down to `fade`, `fade-up`, `fade-down`.
- **Continuous CSS background motion removed:** hero gradient-blob loop, aurora movement, morph blob, float keyframes, gradient flow, glow pulse.

### Kept (functional feedback / loading state)

- **Flying cart + cart bump** (`FlyingCartService`) — confirms "added to cart", refined to a 400ms arc and a subtle bump.
- **Confetti burst + sparkle fallback** (`ConfettiService`) — celebrates order confirmation; reduced to 50 particles / 2s, with a sparkle fallback under reduced motion.
- **Skeleton shimmer, spinners, pulse** — communicate loading.
- **Modal enter/exit + backdrop fade, toast enter, dropdown/accordion expand, tab transitions** — communicate context and selection.
- **Link underline, input focus, button press, card hover (shadow-only)** — functional interaction feedback. Hovers reduced to color/shadow only; no scale/lift transforms.
- **CountUpDirective** — draws attention to statistics; shows the final value immediately under reduced motion.

---

## Remaining Animation Directives

All live under `src/ClimaSite.Web/src/app/shared/directives/` and are standalone (import directly into the
component that needs them). All are SSR-safe (browser-guarded) and all honor reduced motion (see the
contract below). Add `data-testid` to any interactive host element these are applied to.

### `RevealDirective` — `[appReveal]`

Subtle, scroll-triggered entrance for a section heading or a single hero element. Supports only
`fade`, `fade-up` (default), `fade-down`. Default duration 300ms, distance 16px, easing
`cubic-bezier(0.16, 1, 0.3, 1)`.

```html
<h2 appReveal>Section heading (fade-up)</h2>
<p appReveal="fade" [delay]="100">Simple fade</p>
```

**Use for:** one entrance per section, near the top of the fold. **Do not** stack on every card/row —
that is the over-animation 21F removed. Under reduced motion the element is shown immediately with no
transform.

### `CountUpDirective` — `[appCountUp]`

Animates a number from a start value to a target when scrolled into view. RAF-based, default 2000ms,
`ease-out-quart`.

```html
<span [appCountUp]="5000">0</span>
<span [appCountUp]="99.9" [decimals]="1" suffix="%">0</span>
```

**Use for:** statistics and metrics only. Under reduced motion (and on SSR) it renders the final value
instantly and emits `countComplete`.

### `AnimateOnScrollDirective` — `[appAnimateOnScroll]`

Legacy scroll-reveal with a wider set of transforms (`fade-in`, `fade-in-up`, `scale-in`, etc.).
Prefer `RevealDirective` for new work; this remains for existing usages. Note: it does **not** read the
`AnimationService` signal — it relies on the global `prefers-reduced-motion` CSS to neutralize the
transition, so do not introduce it on new reduced-motion-sensitive surfaces.

### `MagneticHoverDirective` — `[appMagneticHover]`

Element subtly follows the cursor on hover. Default strength 0.2, scale 1.02, 150ms.

```html
<button appMagneticHover data-testid="cta-primary">Hover me</button>
```

**Use sparingly** — reserve for a single primary CTA, never for cards or lists. Keep `scale` ≤ 1.02 to
stay within the restrained budget. No-ops entirely under reduced motion (the hover handlers early-return).

### `ScrollProgressDirective` — `[appScrollProgress]`

Drives a property (width / height / scale-x / scale-y / opacity / background position) from the page
scroll progress signal. For reading-progress bars and scroll indicators.

```html
<div appScrollProgress="scale-x"></div>
```

Reads `AnimationService.scrollProgress()`; under reduced motion the progress indicator still tracks
position but without smooth scrolling (the service forces `behavior: 'auto'`).

### `SplitTextDirective` — `[appSplitText]`

Splits text into per-char/word/line spans for staggered entrance. Off by default (`animate=false`).

```html
<h1 appSplitText="words" [animate]="true" [staggerDelay]="100">Headline</h1>
```

**Use rarely** — a single hero headline at most; never on body copy (it slows comprehension). When
`animate` is true and reduced motion is preferred it skips the split entirely and leaves the text intact.

---

## Reduced-Motion Contract

Motion must always be optional. ClimaSite honors reduced motion at two levels:

### 1. System preference (live today)

- **CSS:** a global `@media (prefers-reduced-motion: reduce)` block in `styles/_animations.scss`
  collapses `animation-duration` / `transition-duration` to `0.01ms`, sets `animation-iteration-count: 1`,
  and forces `scroll-behavior: auto`. Component- and feature-level stylesheets add their own
  `prefers-reduced-motion` overrides for canvas/feedback effects (flying cart bump, gallery, etc.).
- **TypeScript:** `AnimationService.prefersReducedMotion()` is a signal initialized from
  `matchMedia('(prefers-reduced-motion: reduce)')` and updated on the media query's `change` event.
  Every remaining directive (`RevealDirective`, `CountUpDirective`, `MagneticHoverDirective`,
  `SplitTextDirective`, `ScrollProgressDirective`) and canvas service (`FlyingCartService`,
  `ConfettiService`) checks this signal and either no-ops, jumps to the final state, or uses a static
  fallback (e.g. confetti's sparkle).

### 2. User override — `.reduce-motion` class

When a user explicitly opts in/out via the accessibility toggle (overriding the OS setting), the choice
is persisted and applied by toggling a **`.reduce-motion` class on the document root**. Reduced-motion
CSS rules are written to match both the media query **and** the `.reduce-motion` class so an explicit
user choice wins over the system default. TypeScript consumers should treat the
`AnimationService.prefersReducedMotion()` signal as the single source of truth for the effective
preference (system OR user override).

### Rules for new motion

1. Any new animation **must** either sit inside a `prefers-reduced-motion`-aware stylesheet or check
   `AnimationService.prefersReducedMotion()` before running.
2. Reduced motion makes feedback **instant**, it does not **remove** it — the user must still see the
   result (cart item appears, count shows its final value, modal is present).
3. Loading affordances (spinner, shimmer, pulse) may stop animating but must remain visually legible as
   "in progress".
