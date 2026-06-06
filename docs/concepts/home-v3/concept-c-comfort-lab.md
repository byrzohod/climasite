# Concept C — Comfort Lab

**Tagline:** "Watch comfort happen."
**Elevator:** The page is staged as a science lab where the user runs live experiments on a virtual room. Drag a temperature slider and the room responds — the AC product in the middle is the instrument making the change happen. Products are introduced as "exhibits" in the lab, each with a live simulation of what it does.

## Why it fits "interactive product showcase"
Every product on the page is shown *doing its job* inside a controlled simulation. It's the most editorial and visually literate take on "interactive product showcase" — more "Apple event" than "e-commerce landing."

## Structural blueprint (top to bottom)

### 1. Lab Hero — Thermal Experiment (100vh)
- **Visual:** A cross-section of a stylized room (side view). On the left wall: an AC unit. Floor, furniture, two people shown in silhouette. A thermal gradient overlay (blue → yellow → red) shows current room temperature. A big slider at the bottom: "Outside temperature: 38°C".
- **Interaction:** Drag the slider from 10°C → 45°C. As you drag:
  - The thermal overlay updates in real time.
  - The AC unit pulses (cold blue or warm orange depending on mode).
  - A live readout displays: "Inside: 23°C — Energy draw: 0.87 kW — Time to target: 4 min".
- **Copy:** H1 top-left: "Comfort, demonstrated." Eyebrow: "The Comfort Lab". Secondary link: "How this works →" opens a small overlay explaining the simulation.
- **Tech:** Canvas 2D with a shader-based gradient. No WebGL required (which is why this concept's fallback story is strongest). GSAP tweens the AC unit's pulse.

### 2. Exhibit 1 — "The quiet one" (100vh, alternating light section)
- **Layout:** Full-viewport dark section. Left: a sound-wave visualization that responds to a "decibel" slider (30 dB ↔ 70 dB). Right: product card for the quietest inverter model, with one-paragraph story.
- **Copy:** "The loudest thing in the room shouldn't be your AC." The slider lets the user *hear* (with opt-in audio) and *see* how big a gap 45 dB vs 60 dB really is.

### 3. Exhibit 2 — "The efficient one" (100vh, alternating light)
- **Layout:** A live energy graph (line chart + running euro counter). A "hours per day" slider. As the user drags 1h → 24h, the graph updates and the counter shows estimated €/month for a traditional vs an inverter unit.
- **Copy:** "Your electricity bill, visualized." Product card right.
- **Real data:** Bulgaria/Germany kWh pricing from backend config.

### 4. Exhibit 3 — "The seasonal one" (100vh, alternating dark)
- **Layout:** A single product with four rotating scene backgrounds (spring / summer / autumn / winter). Scroll-snap locks to each scene for ~1s before continuing. Copy adapts per season: "In summer…", "In winter…".
- **Copy:** "One machine, every season."

### 5. Lab Catalog Wall (80vh)
- **Layout:** A grid of 8–12 product cards styled as lab specimens, each with a tiny thumbnail of its "experiment" (mini version of the visualization from sections 2–4). Clicking a card opens `/products/:slug`.
- **Data source:** Real featured products.

### 6. "Run your own experiment" CTA (60vh)
- **Layout:** Full-bleed. Copy: "Run your own experiment." CTA: "Open full catalog". Secondary: "Talk to an advisor".

## Technical spec

| Layer | Choice |
|---|---|
| Rendering | Canvas 2D + CSS. **No WebGL dependency** — the whole page works without it. This is intentional. |
| Simulations | Pure JavaScript (temperature gradient = 1D LERP; sound-wave = sin-based animation; energy graph = plain line chart via Canvas). Minimal dependencies. |
| Change detection | `OnPush` everywhere. |
| SSR | Every exhibit renders SSR with a static "mid-slider" state. Interactivity hydrates client-side. |
| A11y | Every slider is a standard `<input type="range">` with labels, `aria-valuetext`, and live region for the readout. Audio in exhibit 1 is opt-in and muted by default. Full keyboard path. |
| Fallback | Reduced motion: sliders still work, but all easing becomes instant. No-JS (SSR only): the page is still legible with the static mid-slider screenshots. |

## Asset + perf budget
- Total JS: ≤ 250 KB gzipped (smallest of the three — no 3D library needed).
- No 3D models.
- LCP target: hero Canvas visible ≤ 1.8s.
- CLS: 0.
- INP: < 120ms per slider drag.

## Pros
- **Lowest technical risk** of the three. No WebGL, no large models, no orbit math.
- **Highest accessibility floor** — everything is standard form controls + canvas. Keyboard users get the full experience.
- **Most distinctive editorial voice**. Competitors don't look like this. At all.
- **Reusable pattern**: each "exhibit" is a self-contained component and can be reused on category pages, blog posts, etc.
- **Strongest copy + product-education play** — the simulations teach HVAC literacy, which lifts trust and conversion on product-detail pages later.

## Cons
- Demands strong copywriting (one-line headlines per exhibit, plus a short essay worth of explanatory copy across all of them) in all 3 languages.
- Less "premium object" feel than Concept A — no single hero product gets the "look at this thing" moment.
- Requires more up-front design thinking for the simulations (how does the sound wave *look*? how does the energy graph *feel*?).

## ASCII wireframe (desktop)

```
┌──────────────────────────────────────────────────────────┐
│ header                                                   │
├──────────────────────────────────────────────────────────┤
│ COMFORT, DEMONSTRATED.     the comfort lab               │
│                                                          │
│  ┌─────────────────────────────────────┐                 │
│  │ [ cross-section room — thermal grad ]│                │  ← section 1
│  │   AC ░░░░░░░░░░░░░ silhouettes       │                │
│  │                                       │                │
│  │  Outside [=====●=====] 38°C           │                │
│  │  Inside: 23°C · 0.87 kW · 4 min       │                │
│  └─────────────────────────────────────┘                 │
├──────────────────────────────────────────────────────────┤
│  "The quiet one"       ∿∿∿∿∿∿∿  45 dB ◀─────▶ 70 dB     │  ← section 2 (dark)
│  story + product card  sound wave visualization          │
├──────────────────────────────────────────────────────────┤
│  "The efficient one"   ▁▂▃▅▇ €14/mo                       │  ← section 3 (light)
│  story + product card  energy graph + hours slider       │
├──────────────────────────────────────────────────────────┤
│  "The seasonal one"    [spring][summer][autumn][winter]  │  ← section 4 (dark)
├──────────────────────────────────────────────────────────┤
│  [ lab catalog wall — 12 product specimens ]             │  ← section 5
├──────────────────────────────────────────────────────────┤
│  RUN YOUR OWN EXPERIMENT.  [ Catalog ] [ Advisor ]       │  ← section 6
└──────────────────────────────────────────────────────────┘
```
