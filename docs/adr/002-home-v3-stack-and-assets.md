# ADR 002 — Home v3 stack, assets, and build order

**Status:** Accepted
**Date:** 2026-04-08
**Deciders:** sarkisharalampiev
**Depends on:** [ADR 001 — Home page v3 concept (Configurator-First)](./001-home-page-concept.md)
**Plan:** [`docs/plans/18-project-completion.md`](../plans/18-project-completion.md) — HOME-002

## Context

Concept B (Configurator-First) is accepted. Before writing Angular code or the HTML mock, four foundational questions needed batched answers: WebGL library, 3D asset source, recommendation algorithm complexity, and build order.

## Decisions

### 1. WebGL library — **Three.js latest stable**

Use Three.js latest stable (~r160+, pinned to a specific release in `package.json`). The r128 reference in CLAUDE.md is outdated; the upgrade will be documented inline in the scaffolding PR and referenced here.

**Rationale:** Already part of the team's mental model, biggest ecosystem, first-class glTF/DRACO loaders, stable TypeScript types, largest pool of examples. Bundle cost (~600 KB pre-tree-shake, significantly less after) is acceptable given we're lazy-loading the home-v3 module and the 3D scene.

**Rejected:** OGL (too bare-bones, maintenance risk), Babylon.js (1 MB cost + zero existing Babylon code in repo), raw WebGL (too expensive to iterate on art direction).

### 2. 3D assets — **Procedural geometry in code**

The room, furniture, and AC unit are built from Three.js primitives (`BoxGeometry`, `ExtrudeGeometry`, `CylinderGeometry`, merged meshes) plus a flat low-poly material palette. No external `.glb` files.

**Rationale:**
- Zero external binary assets means the entire 3D scene is version-controlled, code-reviewable, and trivially diffable.
- Bundle cost is just JS, no network round-trips for model files.
- Iteration loop is instant — tweak constants, recompile, see the result.
- The stylized aesthetic is a *feature*, not a limitation: procedural low-poly reads as intentional design language and ties to the "Nordic Tech" philosophy already in the project.
- No licensing or attribution headaches.

**Rejected:** custom-modeled glTF (cost, timeline, binary pipeline), free asset packs (style inconsistency, CC-BY attribution baggage on a commercial site).

### 3. Recommendation algorithm — **Rules-based weighted scoring**

`GET /api/products/recommendations?area={m²}&type={roomType}&zone={climateZone}` implemented as deterministic scoring:

```
score(product) =
    btu_fit_score(product.btu, area * zone_multiplier)   * 0.40  // exact sizing dominates
  + inverter_bonus(product.is_inverter)                   * 0.15  // prefer inverter
  + zone_fit_score(product.min_temp, zone)                * 0.15  // cold-climate capable in zone C
  + room_type_fit(product.recommended_room_types, type)   * 0.15  // e.g. bedroom = quieter
  + stock_available(product.stock)                        * 0.10  // in-stock required
  + price_band_affinity(product.price, zone)              * 0.05  // mild regional price tilt
```

Returns top 3 by score, with tie-break on inverter + quiet-mode + stock recency.

**Rationale:** deterministic = testable = debuggable. Every scoring weight becomes a unit test. Zero ML infrastructure required. Can be upgraded to learned weights later without changing the endpoint contract.

**Rejected:** simple lookup table (brittle, no stock-out handling), ML-backed (overkill, no data, adds a serving path we don't have).

### 4. Build order — **Backend endpoint first, UI second**

Sequence:
1. Backend: `GET /api/products/recommendations` — command/query, validator, repository query, integration test (WebApplicationFactory + testcontainers Postgres), unit tests for scoring.
2. Seed data: add `btu`, `is_inverter`, `min_temp`, `recommended_room_types`, `stock` fields to `Product` entity if not already present; backfill migration.
3. Angular: `HomeV3Component` + `HomeWizardStateService` (Signals) consuming the real endpoint. No mock service, no fixtures.
4. Playwright E2E against the real endpoint with `TestDataFactory`.

**Rationale:** matches the project's no-mocking rule exactly. No throwaway UI code. Backend gets tested in isolation first, then UI is layered on top. Slightly slower wall-clock than parallel, but lower total cost because nothing gets rewritten.

**Rejected:** parallel build (coordination overhead), UI-first against a mock (violates spirit of the rule, creates throwaway code).

## Consequences

**Positive:**
- Everything in this concept is deterministic and testable.
- No binary asset pipeline means the 3D scene is version-controlled and code-reviewable.
- The scoring algorithm becomes a first-class, documented domain object that can later drive product-detail cross-sells.

**Negative / risks:**
- Procedural 3D has a visual ceiling. If the scene ends up looking amateur in HOME-003 mock review, we revisit this decision and either (a) invest in a custom glTF model of just the AC unit while keeping the room procedural, or (b) fall back to Concept C's Canvas 2D approach for the preview panel only.
- The Product entity may need new fields (`min_temp`, `recommended_room_types`) — a small migration that needs ADR 004 (migration strategy) to have landed first. If it hasn't, we document the migration inline and include it in the Phase 1 PR.

## Follow-ups

- **HOME-003** — single-file HTML mock at `docs/concepts/home-v3/mock-chosen.html` for visual sign-off. Uses the exact procedural geometry approach proposed above so the mock *is* the proof-of-concept.
- **HOME-005** — backend endpoint implementation PR (before any Angular scaffolding).
- **Data audit** — verify `Product` entity fields currently available vs needed; note gap in Phase 1 task list.
