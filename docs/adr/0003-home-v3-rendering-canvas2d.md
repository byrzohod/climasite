# ADR 0003 — Home v3 configurator preview renders with Canvas 2D

**Status:** Accepted
**Date:** 2026-06-25
**Deciders:** sarkisharalampiev
**Supersedes:** [ADR 002 — Home v3 stack, assets, and build order](./002-home-v3-stack-and-assets.md) — the *renderer* sub-decision (sub-decisions 1 & 2: WebGL library + 3D assets) only. ADR 002's recommendation-scoring (sub-decision 3) and backend-first build order (sub-decision 4) stand unchanged.
**Plan:** [`docs/plans/18-project-completion.md`](../plans/18-project-completion.md) — HOME phase 1
**Decision log:** ratifies [DECISIONS.md D-014](../project-plan/DECISIONS.md)

## Context

ADR 002 (Accepted, 2026-04-08) chose **Three.js + procedural geometry** to render the home
configurator's axonometric room preview. During Plan 18 Phase 1 the preview actually shipped as a
**Canvas 2D** axonometric renderer; `three` was never added to `src/ClimaSite.Web/package.json`.

That reality was first recorded as an "Implementation note — 2026-06-07" *edited into the body of the
already-accepted ADR 002*. That violates the ADR immutability rule (`docs/adr/README.md`: never edit an
accepted ADR's body to reverse its decision — write a superseding ADR). This ADR is that superseding
record, written to restore process integrity and to capture the rationale that the in-place note
under-documented. ADR 002's body is left intact for history; only its **Status** line was updated to
point here (the allowed immutability-preserving edit).

The forces that pushed the implementation away from Three.js:

- The configurator preview is a stylized, mostly-static axonometric room, not an interactive 3D scene.
- The project's reduced-motion / no-WebGL fallback had to be a first-class path, not a degraded one.
- Bundle budget matters for the Core Web Vitals targets on `/`.
- "No new binary asset pipeline" was already a hard constraint in ADR 002.

## Decision

**Canvas 2D is the production renderer for the home configurator's room preview.** Three.js is *not*
added as a dependency for Home v3.

Rationale: a Canvas 2D axonometric render delivers the concept's live, theme-aware preview with no WebGL
dependency, a smaller bundle, and — decisively — makes the reduced-motion / no-WebGL rendering the
*primary* path rather than a fallback that could rot. The stylized low-poly aesthetic that ADR 002
wanted from procedural geometry is achievable in 2D, so the visual intent of the concept is preserved.

**Rejected (for this preview):** Three.js / WebGL — its bundle cost and the second rendering path it
would require (WebGL + a separate reduced-motion fallback) are not justified for a largely-static
axonometric preview. Three.js is **deferred, not banned**: it remains the sanctioned stack for future
product-detail 3D or richer category exhibits where genuine interactivity justifies the bundle cost.

## Consequences

**Positive**
- No WebGL dependency; `three` stays out of `package.json` and the bundle.
- The reduced-motion / no-WebGL render is the single primary path — no divergent fallback to maintain.
- The ADR process is repaired: the first amendment to an accepted ADR is now a proper superseding record
  rather than a normalized body-edit.

**Negative / risks**
- Canvas 2D has a lower visual ceiling than WebGL for any future richly-3D, freely-rotatable preview. If
  product-detail or category pages later need that, write a new ADR adopting Three.js *there* — this ADR
  only governs the home configurator preview.

**Verified outcomes at acceptance** (production build of `/`):
- Mobile Lighthouse: Performance 0.97, LCP 2.296s, CLS 0.
- Desktop Lighthouse: Performance 1.00, LCP 0.576s, CLS 0.000057.
- Full E2E suite green against real API/data (213/213 at the time the note was written).

## Follow-ups

- `docs/adr/README.md` index and `DECISIONS.md` D-014 status updated to reflect this ratification.
- Open Decision O-5 (in `DECISIONS.md`) — "write ADR 003 (Canvas 2D)" — is satisfied by this ADR.
