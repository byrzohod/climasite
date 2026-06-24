---
unit: UX-15-contrast
type: NORMAL
status: approved
created: 2026-06-24
plan_status: approved
design: ../../design/DESIGN.md
---

# Unit plan — UX-15: fix WCAG-AA color-contrast violations (light + dark)

## Context / Definition of Ready
The Plan-19 E2E stabilization (NetworkIdle purge) made the axe scans deterministic and surfaced REAL
serious `color-contrast` violations that flaky scan timing had masked. Both a11y suites
(`AxeAccessibilityMatrixTests`, `AccessibilityTests`) are currently **reporting-first** (gated by
`A11Y_ENFORCE=1`). This unit fixes the offending colors so a11y can be **enforced**.

Known violations (from CI axe output):
- **Light theme** (`#fff` background): muted text `#7f848e` → 3.75:1 and `#9da5af` → 2.48:1 (need ≥4.5:1)
  on `/products` (list), product **detail**, and **cart** pages.
- **Dark theme**: serious `color-contrast` on `/promotions` (4 nodes), `/brands` (7), `/about` (1).

These are almost certainly muted-text / secondary-text design tokens in
`src/ClimaSite.Web/src/styles/_colors.scss` (single source of truth for colors).

## Scope / approach
1. From the live axe run, identify the exact offending **CSS variables/tokens** (likely
   `--color-text-muted` / `--color-text-secondary` / a placeholder-gray, light + dark variants) and the
   selectors using them.
2. Darken (light) / lighten (dark) ONLY those muted tokens to the **minimum** change that reaches
   **≥4.5:1** against their real backgrounds (normal text) / **≥3:1** (large text ≥18.66px bold or 24px).
   Keep the palette cohesive — adjust the token, not per-component overrides; do not introduce hardcoded
   colors (CLAUDE.md rule).
3. Re-check there are no NEW contrast regressions introduced elsewhere by the token change.

## Acceptance criteria
- [ ] `AxeAccessibilityMatrixTests` AND `AccessibilityTests` report **0 serious/critical color-contrast
      violations** with `A11Y_ENFORCE=1`, in BOTH light and dark themes, across the audited pages.
- [ ] All colors remain defined in `_colors.scss` (no hardcoded hex added to components).
- [ ] No visual regression that breaks readability in either theme (spot-check key pages).
- [ ] `A11Y_ENFORCE=1` is set in the CI E2E job so both axe suites become hard gates.

## Test / verification plan
- **Automated:** run `A11Y_ENFORCE=1 E2E_BASE_URL=http://localhost:4200 E2E_API_URL=http://localhost:5029
  dotnet test tests/ClimaSite.E2E --filter "FullyQualifiedName~Accessibility"` against the running stack
  (ng serve hot-reloads the `_colors.scss` change) → must be GREEN (0 serious/critical).
- **Manual:** open `/products`, a product detail, `/cart`, `/promotions`, `/brands`, `/about` in light AND
  dark; confirm the previously-flagged text is now legible and nothing else regressed.
- **CI:** the E2E job with `A11Y_ENFORCE=1` is the evidence of record.

## Out of scope
The E2E retry net (A4) and the broader UI component-spec backfill (Plan 19 B2/B3).
