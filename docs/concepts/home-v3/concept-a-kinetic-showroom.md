# Concept A — Kinetic Showroom

**Tagline:** "Meet the machine."
**Elevator:** A scroll-driven orbit around a hero product in a dark studio, with hotspot-triggered reveals and a quiet, premium aesthetic. The page sells *engineering pride*.

## Why it fits "interactive product showcase"
The entire page is a staged product demo. The user's scroll is directly wired to the camera orbit of a single hero unit, so every inch of scroll reveals a new facet of the product. It's the clearest "showroom" interpretation of the direction.

## Structural blueprint (top to bottom)

### 1. Hero — Orbital Stage (100vh, sticky pin for 200vh scroll)
- **Visual:** A single premium inverter AC unit floats in a pitch-black studio with one rim light and a soft floor gradient. Three.js, MeshStandardMaterial, environment HDR.
- **Interaction:** Scroll drives camera orbit (0° → 360°). GSAP `ScrollTrigger.scrub`. Lenis is **not** used (lesson from v1).
- **Text layer:** One persistent H1 ("Engineered for silence") bottom-left. Three caption cards fade in at scroll milestones: `0.2 → "Inverter compressor"`, `0.5 → "45 dB whisper mode"`, `0.8 → "SEER 23 efficiency"`.
- **Fallback:** Poster PNG + looping MP4 when `prefers-reduced-motion` OR no WebGL2.

### 2. Airflow Simulation Strip (50vh, full-bleed)
- **Visual:** A horizontal WebGL flow-field sim — thin ribbons of particles drifting from the hero product (ghosted on the left) across the viewport, curving and dissipating.
- **Copy:** "Air that actually reaches the corners." Tiny eyebrow: "Dynamic flow control".
- **Why:** Bridges the hero (product-as-object) to the catalog (product-as-value). Cheap to render; even low-end devices can handle 2D particles.

### 3. Category Rail — "Pick your climate" (80vh)
- **Layout:** Horizontal scroll-snap row of 4 oversized tiles: Cooling / Heating / Ventilation / Parts & Accessories. Each tile is a full-height card with the category name in huge type, a background video loop muted/autoplay/playsinline, and a minimal "Browse →" link.
- **Interaction:** Trackpad horizontal scroll OR arrow keys. On mobile: vertical stack, still full-bleed.
- **Data source:** Real categories from the API, not hardcoded.

### 4. Comparison Slab — "Why inverter?" (100vh, dark)
- **Layout:** Two-column before/after. Left column: traditional compressor (small product still, stats: 62dB, SEER 14). Right column: the hero inverter (same product still, stats: 45dB, SEER 23). A diagonal divider that slides on scroll.
- **Interaction:** Draggable vertical divider so users can "swipe" between the two stats panels (like image compare widgets).
- **Copy:** One-line headline, one-paragraph body, one CTA: "See inverter models".

### 5. Trust band (compact, 40vh)
- **Layout:** Three narrow columns — certifications (CE, Energy Star BG), installation partners count, years in business. Minimal type. Uses real data from backend, not hardcoded.
- No marquees, no infinite scrollers — it's trust, not noise.

### 6. Footer CTA (60vh)
- **Layout:** Full-bleed dark panel. Big line: "Ready when you are." Two buttons: "Browse catalog" (primary) + "Book installation consultation" (secondary).
- **Visual:** Subtle gradient mesh in the background. No product image — the product is what you remember from the hero.

## Technical spec

| Layer | Choice |
|---|---|
| Rendering | Three.js r150+ (r128 is too old per CLAUDE.md note). Must confirm CDN supports it. Otherwise pin to latest. |
| Model format | `.glb` (DRACO-compressed), target ≤ 800 KB for hero, ≤ 200 KB per category tile product. |
| Animation | GSAP + ScrollTrigger (already in repo). No Lenis. |
| Change detection | `OnPush` everywhere. |
| SSR | Hero renders poster PNG during SSR; WebGL hydrates client-side after `afterNextRender`. |
| A11y | Orbit pauses when focus is on a caption card. Full keyboard path: Tab cycles through captions → category tiles → CTAs. |
| Fallbacks | (1) `prefers-reduced-motion: reduce` → all scrolling is free-scroll, hero becomes poster; (2) no WebGL2 → same poster + MP4 loop. |

## Asset + perf budget

- Total JS on home: ≤ 400 KB gzipped (including Three.js and GSAP).
- Total hero model + textures: ≤ 1 MB.
- LCP target: ≤ 2.5s on 4G throttled mid-tier phone.
- CLS: 0.
- INP: < 200ms (orbit scroll).

## Pros
- Strongest "wow" beat — the page looks nothing like competitors.
- Clearest match to the chosen direction.
- Hero is re-usable: same `HomeHeroSceneService` can power any future product-detail 3D view.

## Cons
- Highest perf risk; WebGL budget is tight.
- Requires one high-quality hero model (real 3D asset cost: time + money).
- Scroll-pinning can feel "stuck" on some trackpads if not tuned carefully.

## ASCII wireframe (desktop)

```
┌──────────────────────────────────────────────────────────┐
│ header (persistent, translucent on dark)                │
├──────────────────────────────────────────────────────────┤
│                                                          │
│         [ 3D AC unit orbiting in dark studio ]           │
│                                                          │
│  ENGINEERED                                              │
│  FOR SILENCE                                             │
│                                    [caption card 1/3]   │  ← section 1
│                                                          │
├──────────────────────────────────────────────────────────┤
│  ≈≈≈≈  particle flow field  ≈≈≈≈                         │  ← section 2
│           "air that actually reaches the corners"        │
├──────────────────────────────────────────────────────────┤
│  [ Cooling ] [ Heating ] [ Ventilation ] [ Parts ]      │  ← section 3
│  horizontal scroll-snap rail                             │
├──────────────────────────────────────────────────────────┤
│  TRADITIONAL          │          INVERTER                │  ← section 4
│  62 dB                │          45 dB                   │
│  SEER 14              │          SEER 23                 │
│               [ draggable divider ]                      │
├──────────────────────────────────────────────────────────┤
│   CE | Energy Star | 12 years | 340 installations       │  ← section 5
├──────────────────────────────────────────────────────────┤
│              READY WHEN YOU ARE.                         │
│   [ Browse catalog ]   [ Book installation ]            │  ← section 6
└──────────────────────────────────────────────────────────┘
```
