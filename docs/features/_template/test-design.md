<!--
PHASE 4 — TEST DESIGN. Every human scenario from research.md §5 gets a STABLE Scenario ID and a
named test (or an explicit manual-verification note). This is the contract the CI "test-design-
coverage lint" (Wave 3) checks: every Scenario marked `automated` must have a test that exists.
Exit criterion (Phase 4 → Phase 5): every journey has a Scenario ID; each is marked automated|manual;
automated rows NAME a test path here (the test itself is written in Phase 5 and must EXIST by Phase 6 —
the Wave 3 coverage lint enforces existence).
-->

# Test design — <FEAT-ID>: <short title>

> Scenario IDs are **stable** — once assigned, never renumber (tests, plan, and the coverage lint
> reference them). Reuse the IDs you wrote in `research.md §5` (H#/E#/X#) and add any new ones.

## Coverage map

| Scenario ID | Human scenario (from research.md §5) | Mode | Level | Test name / location | Status |
|-------------|--------------------------------------|:----:|-------|----------------------|:------:|
| H1 | <happy path, as a user does it> | automated | E2E | `tests/ClimaSite.E2E/Tests/<Area>/<Name>.cs::<Method>` | ☐ |
| E1 | <edge case> | automated | integration | `tests/ClimaSite.Api.Tests/<...>` | ☐ |
| X1 | <error path> | automated | unit | `<...>Tests.cs` / `<...>.spec.ts` | ☐ |
| M1 | <scenario only verifiable by hand, e.g. a real 3DS bank challenge> | manual | — | `/verify-work` layer N + note here | ☐ |

**Modes:** `automated` (a test asserts it — must exist before Phase 6 passes) · `manual` (verified via
`/verify-work` and recorded, with the reason it can't be automated).

## Cross-cutting matrix (tick the axes this feature must hold across)

- [ ] **Themes:** light ✓ / dark ✓
- [ ] **Languages:** EN ✓ / BG ✓ / DE ✓
- [ ] **Viewports:** mobile (390) ✓ / desktop (1280) ✓
- [ ] **a11y:** axe clean (no serious/critical) on every new/changed page, in each lang×theme
- [ ] **Reduced motion:** any animation gated behind `prefers-reduced-motion`

## No-mocking confirmation

- [ ] Integration tests use Testcontainers (real Postgres) — no DB mocks.
- [ ] E2E tests use the real API + real DB + real UI — no mocking, self-contained test data via `TestDataFactory`.
- [ ] No deep-linking past auth; tests drive the real login UI/flow.
