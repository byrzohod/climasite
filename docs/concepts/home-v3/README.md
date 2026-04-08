# Home v3 — Concept Proposals

**Direction (user-selected):** Interactive product showcase — 3D/WebGL, configurator teaser, parallax product reveals, showroom vibe.

Three concepts below. They all stay inside the chosen direction, but the **structure, interaction model, and content order are deliberately different** so you can pick a real design rather than a first-draft-as-final-draft (lesson from v1).

| | Concept A — Kinetic Showroom | Concept B — Configurator-First | Concept C — Comfort Lab |
|---|---|---|---|
| Hero | 3D AC unit floating in a dark studio, orbit on scroll | Question panel ("How big is your space?") with live 3D preview | Split-screen thermal simulation (hot → cool) with product sitting in the middle |
| Primary interaction | Scroll-driven orbit + hotspot reveal | Wizard → instant recommended products | Drag temperature slider → room reacts → product efficiency curve updates |
| Content order | Hero → airflow sim → category rail → comparison slab → trust → footer | Wizard → recommended set → category rail → testimonials → trust → footer | Lab hero → "problem" chapters → products as lab experiments → CTA → footer |
| Emotional beat | "Look at this thing." | "Tell us about you. We'll show you the one." | "Watch comfort happen." |
| Risk | WebGL perf on low-end | Wizard completion friction | Requires strong copywriting + motion tuning |
| Fallback | Static poster + MP4 loop | Wizard works w/o 3D | Static before/after stills |

Concept files:

- [Concept A — Kinetic Showroom](./concept-a-kinetic-showroom.md)
- [Concept B — Configurator-First](./concept-b-configurator-first.md)
- [Concept C — Comfort Lab](./concept-c-comfort-lab.md)

**Decision (2026-04-08):** **Concept B — Configurator-First** selected. Rationale in [`docs/adr/001-home-page-concept.md`](../../adr/001-home-page-concept.md). Concepts A and C are archived as future patterns (A → product-detail 3D view; C → category-page "exhibit" modules).

**Next step:** HOME-002 stack-decision question round, then HOME-003 HTML mock for visual sign-off, *then* Angular scaffolding. No Angular code lands before the mock is approved.
