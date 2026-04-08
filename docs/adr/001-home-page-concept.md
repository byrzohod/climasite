# ADR 001 — Home page v3 concept

**Status:** Accepted
**Date:** 2026-04-08
**Deciders:** sarkisharalampiev (delegated pick to Claude in same session)
**Plan:** [`docs/plans/18-project-completion.md`](../plans/18-project-completion.md)
**Supersedes:** previous home page plans (19, 21A, 23, 24, 25) and all prior home implementations.

## Context

The v1 and v2 home page attempts failed for overlapping reasons documented in `CLAUDE.md` "Lessons Learned":
- Generic design patterns producing template-feel pages.
- Scroll-pinning + Lenis caused lag/freeze on some trackpads, and sometimes rendered blank hero sections.
- Designs were accepted without Playwright-verified visual inspection.

The user then requested a complete rewrite with a **different structure** from anything previously attempted and selected the high-level direction **"Interactive product showcase"**.

Three concepts were drafted in `docs/concepts/home-v3/`:

- **Concept A — Kinetic Showroom**: scroll-driven 3D orbit around a single hero unit, airflow simulation, before/after inverter comparison slab.
- **Concept B — Configurator-First**: page opens inside a 3-question sizing wizard with a live stylized 3D room preview that swaps the product in real time based on the user's answers; outputs 3 recommended products from a new backend endpoint.
- **Concept C — Comfort Lab**: editorial science exhibits — a temperature slider on a cross-section room, a sound-wave exhibit, an energy-graph exhibit, a seasonal exhibit. Canvas 2D only, no WebGL dependency.

## Decision

**Adopt Concept B — Configurator-First.**

## Rationale

1. **Solves the actual customer decision.** HVAC purchases are sizing-constrained (BTU vs m² vs climate zone). The wizard is not artificial friction — it is the question every real shopper already has.
2. **Respects the v1/v2 failure modes.** The primary interaction is above the fold without scroll. Scroll-pinning is not load-bearing anywhere on the page. This is the concept least likely to repeat the Lenis/ScrollTrigger rendering failures.
3. **Strongest fallback story.** Wizard is HTML + Angular Signals from SSR. The stylized low-poly 3D preview is decorative garnish: no WebGL → wizard still works, recommendation slab still works, page still converts.
4. **Creates reusable backend infrastructure.** The new `GET /api/products/recommendations?area=&type=&zone=` endpoint will be reused by product-detail cross-sells, category pages, email templates, and account dashboards. Concepts A and C leave no reusable backend behind.
5. **Shortest path to commerce.** Users land inside the shopping funnel in under 30 seconds. Concepts A and C land users inside a story that still has to pivot to products.
6. **Achievable perf budget.** 250–350 KB JS + stylized low-poly (≤ 800 KB total scene) is hittable on a mid-tier 4G phone. Concept A's photorealistic studio is a coin flip on low-end.
7. **Accessibility ceiling is high.** Wizard is standard form controls with `aria-live` updates; 3D preview is `aria-hidden`. Keyboard users get the full experience, which Concept A cannot match cheaply.

## Rejected alternatives

### Concept A — Kinetic Showroom
**Rejected because:** highest perf risk; leans hardest into the exact scroll-pinning mechanic that burned v1 and v2; requires a high-quality photorealistic 3D model (time + money); lowest conversion density (beautiful but the first product card is 3 scrolls down); WebGL fallback collapses to a static poster that betrays the concept.

**Kept for later:** the `HomeHeroSceneService` pattern is a good fit for a future product-detail "3D view" feature. Worth revisiting when a product-detail 3D story becomes a priority.

### Concept C — Comfort Lab
**Rejected because:** demands four pieces of heavy, specific copywriting across three languages (EN, BG, DE) with domain fluency; has no single hero moment; the "science exhibit" metaphor requires strong art direction to avoid feeling like an infographic; commercial density is low — the first product card is also deep into the page.

**Kept for later:** the "exhibit" pattern (a single interactive simulation paired with a product card) is excellent for category pages and product-detail pages. Will be reused in Phase 2 or later without being load-bearing for the homepage.

## Consequences

**Positive:**
- A new recommendation backend endpoint enters the codebase early and is reusable.
- The wizard state model (`HomeWizardStateService` with Signals) becomes a template for similar guided flows elsewhere in the app.
- The homepage has a measurable conversion signal: wizard completion rate → recommendation click-through → product-detail view → add-to-cart.

**Negative / risks:**
- A small fraction of users will bounce on any wizard, even with a Skip link. Mitigation: sensible defaults render a full recommendation slab on scroll regardless of interaction; analytics will monitor the skip rate.
- Requires real backend work in Phase 1 (new endpoint, tests, recommendation logic). This is a feature, not a bug — but it does increase Phase 1 scope.
- Stylized low-poly art direction must land well or the 3D preview becomes a liability. Mitigation: HOME-003 HTML mock is the gate; we do not write Angular code until the mock is approved.

## Follow-ups (Plan 18 tasks now unblocked)

- **HOME-002** — stack decision round: Three.js vs OGL vs Babylon, model format (glTF/DRACO), animation library (GSAP confirmed), Angular Signals wiring, analytics event schema, exact asset budget numbers.
- **HOME-003** — produce `docs/concepts/home-v3/mock-chosen.html` as a Playwright-renderable mock for visual sign-off before Angular scaffolding.
- **HOME-004** — scaffold `features/home-v3/` with `OnPush` and 300-line-per-component cap.
- **Phase 1 backend spike** — draft the recommendation endpoint contract (inputs, outputs, tie-break rules, zone mapping) in a short ADR 002 follow-up before implementation.
