# Concept B — Configurator-First

**Tagline:** "Tell us about your space. We'll pick the one."
**Elevator:** The page opens *inside the configurator*. No lifestyle hero, no scroll-first storytelling — the user is immediately answering three questions (room size, room type, climate zone) while a live 3D room previews their answers. The output is a set of 3 recommended products with reasoning.

## Why it fits "interactive product showcase"
The 3D isn't decorative — it's *instrumental*. The hero product swaps in real time as the user answers the wizard. It shows the showroom inventory by showing *which unit fits the user's life*. This is the most commerce-forward reading of the direction.

## Structural blueprint (top to bottom)

### 1. Wizard Hero — "Find your unit in 3 questions" (100vh, no scroll needed to interact)
- **Layout:** Split 50/50. Left: wizard panel on dark glass. Right: live 3D room preview.
- **Wizard (left panel):**
  - Step 1 — "How big is your space?" Slider 10–120 m² with live m² → BTU conversion shown next to it.
  - Step 2 — "What kind of room?" Segmented control: Living / Bedroom / Office / Commercial.
  - Step 3 — "Where in Bulgaria / Germany / EU?" Region picker (maps to climate zone A/B/C, affects sizing + product filter).
  - Persistent "Skip" link → sends user to `/products` with prefilled default.
- **3D preview (right panel):**
  - A stylized low-poly room that morphs as the user answers. The room grows with the slider, furniture changes with room type, the AC unit on the wall swaps to match the recommended model.
  - Camera does a slow dolly when the user changes steps (GSAP, `scrub: false`, `duration: 0.8`).
- **CTA at end of step 3:** "See my 3 recommendations" — smooth-scrolls to section 2.

### 2. Recommendation Slab — "For your space: 3 options" (100vh)
- **Layout:** Three product cards in a row (desktop) / stacked (mobile). Cards are **oversized**: 40% of viewport each, hero image dominant, big price, one-line reason ("Ideal for 35 m² bedrooms — 9000 BTU, inverter, 42 dB"), single CTA "View details".
- **Interaction:** Subtle parallax on card images as user scrolls. Each card is a link; no hover-only info.
- **Real data:** Pulled from `/api/products/recommendations?area=35&type=bedroom&zone=B`. Creates a real backend endpoint (Phase 2 scope).

### 3. "Not sure? Browse by category" rail (60vh)
- **Layout:** Four compact category cards, horizontal. Smaller than Concept A's tiles — the focus of this page is the wizard, not the rail.

### 4. Social proof (40vh)
- **Layout:** One big testimonial quote with photo, next to a small grid of three more short quotes. No marquees.

### 5. Trust + installation partner locator (60vh)
- **Layout:** Static map of Bulgaria (or DE/EU, localized) with dots for installation partners. Click a dot → partner detail popover. Uses real partner data from backend.

### 6. Footer CTA (40vh)
- "Still unsure? Talk to us." Two buttons: "Book a free consultation" + "Browse catalog".

## Technical spec

| Layer | Choice |
|---|---|
| Rendering | Three.js low-poly room (≤ 150k triangles total). Stylized PBR, warm lighting. |
| Wizard state | Angular Signals (fits CLAUDE.md). Service: `HomeWizardStateService` exposing `area`, `roomType`, `zone`, `recommendations`. |
| Backend | New endpoint `GET /api/products/recommendations?area=&type=&zone=` — tests-with-feature, OpenAPI-documented. |
| SSR | Wizard + static recommendation grid rendered SSR with default "average flat 50m², living room, zone B". Client hydrates the 3D preview. |
| A11y | Wizard is fully keyboard-operable. 3D preview is decorative (`aria-hidden="true"`). Every wizard change announces via `aria-live="polite"`. |
| Fallback | No WebGL → wizard works alone; right panel becomes a static illustration of the currently-selected room type. Wizard still recommends products. |

## Asset + perf budget
- Total JS: ≤ 350 KB gzipped.
- Room scene + all furniture variants: ≤ 800 KB total.
- LCP target: wizard panel visible ≤ 1.5s (pure HTML/CSS, no 3D dependency).
- CLS: 0.
- INP: < 150ms per wizard step change.

## Pros
- **Highest conversion potential** of the three. Users land *already shopping*.
- 3D budget is easier to meet — stylized low-poly is more forgiving than Concept A's photorealistic studio.
- Every interaction has a clear commercial outcome.
- Easiest fallback story (wizard is HTML-first).

## Cons
- Friction risk: some users bounce on any wizard. Mitigated by the "Skip" link and by pre-filling sensible defaults so even a no-interaction user gets a recommendation slab on scroll.
- Requires a real recommendation backend endpoint (good thing long-term, but it's in-scope for the phase).
- Less "wow" than Concept A.

## ASCII wireframe (desktop)

```
┌──────────────────────────────────────────────────────────┐
│ header                                                   │
├──────────────────────────────────────────────────┬───────┤
│                                                  │       │
│  Find your unit in 3 questions.                  │       │
│                                                  │  3D   │
│  ┌──────────────────────────────────┐            │ room  │
│  │ 1. How big?  [===35 m²=======]   │            │preview│
│  │ 2. Type?     (Living)(Bed)(Off)  │            │ (live)│
│  │ 3. Region?   [ Sofia ▼ ]         │            │       │
│  │                                  │            │       │
│  │ [ See my 3 recommendations → ]   │            │       │  ← section 1
│  └──────────────────────────────────┘            │       │
│                                                  │       │
├──────────────────────────────────────────────────┴───────┤
│   [ Product A 40% ] [ Product B 40% ] [ Product C 40% ] │  ← section 2
│                     "For your space"                     │
├──────────────────────────────────────────────────────────┤
│   [ Cooling ] [ Heating ] [ Ventilation ] [ Parts ]     │  ← section 3
├──────────────────────────────────────────────────────────┤
│   "I was nervous…"   │   quote  quote  quote             │  ← section 4
├──────────────────────────────────────────────────────────┤
│   🗺️  map of partner installers • Bulgaria              │  ← section 5
├──────────────────────────────────────────────────────────┤
│       STILL UNSURE?   [ Free consultation ] [ Catalog ]  │  ← section 6
└──────────────────────────────────────────────────────────┘
```
