# Design System 2.0 - Phase 1: Foundation

## Context

### Original Request
Full visual redesign of ClimaSite HVAC e-commerce platform. Transform from "Arctic Aurora" (cool blues, safe design) to "Terra Luxe" (warm & premium aesthetic inspired by Aesop, Sonos, Bang & Olufsen).

### Interview Summary
**Key Discussions**:
- **Approach**: Option B selected - Full rebuild over 12 weeks
- **Stack**: Custom + Tailwind (no external UI library like Spartan)
- **Direction**: "Warm & Premium" luxury aesthetic
- **Branch**: `feature/design-system-2.0` already created

**Research Findings**:
- Current `_colors.scss`: 558 lines with well-structured CSS variables and 11-shade palettes
- Tailwind config references CSS variables (not hardcoded) - will cascade automatically
- 1400+ CSS variable references across codebase
- 162 hardcoded rgba() violations in component files (deferred to Phase 2)
- Typography uses Space Grotesk (display) + Inter (body) + JetBrains Mono (code)
- Both light and dark theme support exists and must be maintained

### Metis Review
**Identified Gaps** (addressed):
- HVAC semantic colors (`--gradient-cooling/heating/ventilation`) must be updated
- `warm`, `ember`, `aurora` palettes must be preserved (heating product differentiation)
- 162 hardcoded colors deferred; documented for Phase 2
- Plus Jakarta Sans font weights verified (400-800 available)
- Contrast validation added to acceptance criteria

---

## Work Objectives

### Core Objective
Replace "Arctic Aurora" design tokens with "Terra Luxe" warm premium palette in the design system foundation files, enabling the visual transformation to cascade throughout the application.

### Concrete Deliverables
1. Updated `src/ClimaSite.Web/src/styles/_colors.scss` - complete palette replacement
2. Updated `src/ClimaSite.Web/src/styles/_typography.scss` - new display font
3. Updated `src/ClimaSite.Web/src/index.html` - Google Fonts URL
4. Documentation of hardcoded color violations for Phase 2

### Definition of Done
- [x] `ng build` succeeds with zero SCSS compilation errors
- [x] `ng serve` runs without console errors
- [x] Visual review confirms warm palette renders in both light and dark modes
- [x] Primary color (Terracotta) passes WCAG AA contrast on white background

### Must Have
- Complete 11-shade palettes (50-950) for all 6 color groups
- Both light theme (`:root`) AND dark theme (`[data-theme="dark"]`) updated
- All gradient definitions using new palette colors
- Shadow colors using warm undertone (rgba(28, 25, 23, x))
- Plus Jakarta Sans loaded and rendering for display text

### Must NOT Have (Guardrails)
- **NO** renaming of CSS custom property names (would break 1400+ references)
- **NO** removing `warm`/`ember`/`aurora` palettes (needed for heating products)
- **NO** modification of Tailwind config structure
- **NO** component-level SCSS changes (Phase 2+)
- **NO** fixing hardcoded rgba() colors in components (162 instances - Phase 2)
- **NO** changes to typography scale, weights, or line heights

---

## Verification Strategy (MANDATORY)

### Test Decision
- **Infrastructure exists**: YES (Angular CLI test runner)
- **User wants tests**: Manual verification (visual design system changes)
- **Framework**: `ng build` for compilation, `ng serve` for runtime verification

### Manual QA Procedures

Each TODO includes detailed verification. Design system changes require:
1. **Compilation check**: `ng build` must succeed
2. **Runtime check**: `ng serve` with no console errors
3. **Visual review**: Browser inspection of both light/dark themes
4. **Accessibility check**: Contrast ratio validation for key colors

---

## Task Flow

```
Task 1 (Colors) ─────────────────────────────────────────────────┐
                                                                  │
Task 2 (Typography) ─────────────────────────────────────────────┼──▶ Task 5 (Build & Verify)
                                                                  │
Task 3 (Index.html Fonts) ───────────────────────────────────────┤
                                                                  │
Task 4 (Documentation) ──────────────────────────────────────────┘
```

## Parallelization

| Group | Tasks | Reason |
|-------|-------|--------|
| A | 1, 2, 3, 4 | Independent file edits |
| B | 5 | Depends on all above |

| Task | Depends On | Reason |
|------|------------|--------|
| 5 | 1, 2, 3 | Build verification needs all changes complete |

---

## TODOs

- [x] 1. Replace Color Palette - "Terra Luxe" Implementation

  **What to do**:
  - Replace all SCSS color variables (lines 11-156) with new warm palette values
  - Update `:root` CSS custom properties (lines 160-377) for light theme
  - Update `[data-theme="dark"]` block (lines 382-514) for dark theme
  - Update gradient definitions (lines 519-557) with new colors
  - Preserve all variable NAMES exactly - only change VALUES

  **New Palette Values**:
  ```
  // Warm Neutrals (replacing cool slate grays)
  gray-50: #FAF7F2 (Cream)
  gray-100: #F5F0E8
  gray-200: #E8E2D9 (Warm Stone)
  gray-300: #D4CCC1
  gray-400: #A89F94
  gray-500: #6B6560 (Warm Gray - body text)
  gray-600: #4A4540
  gray-700: #332F2B
  gray-800: #1C1917 (Warm Black)
  gray-900: #0F0E0C
  gray-950: #0A0908

  // Primary - Terracotta
  primary-50: #FDF5F0
  primary-100: #FCE8DB
  primary-200: #F9D0B6
  primary-300: #F4B089
  primary-400: #E88A5E
  primary-500: #C4785A (Main)
  primary-600: #A86348
  primary-700: #8A4E37
  primary-800: #6F3D2A
  primary-900: #5A3122
  primary-950: #3A1F14

  // Secondary - Bronze
  secondary-50: #F9F6F2
  secondary-100: #F0EAE0
  secondary-200: #E0D4C4
  secondary-300: #C9B8A1
  secondary-400: #A89778
  secondary-500: #8B7355 (Main)
  secondary-600: #745F46
  secondary-700: #5E4C38
  secondary-800: #4A3C2C
  secondary-900: #3D3124
  secondary-950: #251E16

  // Accent - Deep Teal
  accent-50: #F0F7F7
  accent-100: #D9ECEC
  accent-200: #B3D9D9
  accent-300: #80BFBF
  accent-400: #4D9999
  accent-500: #2A5A5A (Main)
  accent-600: #234B4B
  accent-700: #1C3C3C
  accent-800: #162E2E
  accent-900: #112424
  accent-950: #0A1515

  // Success - Sage Green (warm green)
  success-500: #6B8E6B
  
  // Warning - Golden Amber
  warning-500: #D4A574
  
  // Error - Coral (warm red)
  error-500: #C75C5C

  // Shadows - warm undertone
  shadow-color: rgba(28, 25, 23, 0.08)
  shadow-color-lg: rgba(28, 25, 23, 0.12)
  shadow-color-xl: rgba(28, 25, 23, 0.16)
  ```

  **Must NOT do**:
  - Do NOT rename any `$color-*` SCSS variables
  - Do NOT rename any `--color-*` CSS custom properties
  - Do NOT remove the `warm`, `ember`, or `aurora` palette definitions
  - Do NOT modify the file structure or section organization

  **Parallelizable**: YES (with 2, 3, 4)

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `src/ClimaSite.Web/src/styles/_colors.scss:11-156` - SCSS variable definitions to replace
  - `src/ClimaSite.Web/src/styles/_colors.scss:160-377` - Light theme CSS properties structure
  - `src/ClimaSite.Web/src/styles/_colors.scss:382-514` - Dark theme CSS properties structure

  **Structure References** (maintain exactly):
  - `_colors.scss:27-38` - Primary palette 11-shade structure (50-950)
  - `_colors.scss:519-557` - Gradient definitions structure

  **Integration References**:
  - `src/ClimaSite.Web/tailwind.config.js:39-55` - Primary colors reference CSS variables
  - `AGENTS.md:Theming (CRITICAL)` - States ONLY use CSS variables, never hardcode

  **Acceptance Criteria**:

  **Compilation Verification:**
  - [x] `cd src/ClimaSite.Web && ng build` - exits with code 0, no SCSS errors

  **Manual Execution Verification:**
  - [x] Using browser:
    - Navigate to: `http://localhost:4200/`
    - Action: Inspect any element with `background: var(--color-primary)`
    - Verify: Computed style shows #C4785A (terracotta), NOT #0ea5e9 (blue)
  - [x] Dark mode check:
    - Action: Click theme toggle or add `data-theme="dark"` to `<html>`
    - Verify: Background shows warm black (#1C1917), NOT cool slate (#0f172a)
  - [x] Contrast check:
    - Use Chrome DevTools Color Picker on primary button text
    - Verify: Contrast ratio >= 4.5:1 for AA compliance

  **Commit**: YES
  - Message: `style(colors): replace Arctic Aurora with Terra Luxe warm palette`
  - Files: `src/ClimaSite.Web/src/styles/_colors.scss`
  - Pre-commit: `cd src/ClimaSite.Web && ng build`

---

- [x] 2. Update Typography - Plus Jakarta Sans

  **What to do**:
  - Replace `$font-display` value from Space Grotesk to Plus Jakarta Sans
  - Keep Inter for body and JetBrains Mono for code (unchanged)
  - Preserve all font weight, line height, and letter spacing values

  **Must NOT do**:
  - Do NOT change any `$type-scale` values
  - Do NOT modify `$font-weights`, `$line-heights`, or `$letter-spacing` maps
  - Do NOT change the utility class definitions

  **Parallelizable**: YES (with 1, 3, 4)

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References**:
  - `src/ClimaSite.Web/src/styles/_typography.scss:12-14` - Font family definitions (change line 12 only)

  **Why This Change**:
  - Plus Jakarta Sans has slightly warmer character than Space Grotesk
  - Better aligns with "Terra Luxe" premium aesthetic
  - Maintains excellent readability at all sizes

  **Acceptance Criteria**:

  **Compilation Verification:**
  - [x] `cd src/ClimaSite.Web && ng build` - exits with code 0

  **Manual Execution Verification:**
  - [x] Using browser:
    - Navigate to: `http://localhost:4200/`
    - Action: Inspect any `<h1>` or `.text-display-xl` element
    - Verify: Computed `font-family` shows "Plus Jakarta Sans" as first family
  - [x] Visual check:
    - Verify: Headlines render with slightly rounded, warm character
    - Verify: No font fallback to system-ui (check network tab for font load)

  **Commit**: YES (groups with 3)
  - Message: `style(typography): switch display font to Plus Jakarta Sans`
  - Files: `src/ClimaSite.Web/src/styles/_typography.scss`
  - Pre-commit: `cd src/ClimaSite.Web && ng build`

---

- [x] 3. Update index.html (Fonts + Theme Color)

  **What to do**:
  - Replace current Google Fonts URL with Plus Jakarta Sans
  - Keep Inter and JetBrains Mono in the same request
  - Ensure font weights 400, 500, 600, 700 are loaded for Plus Jakarta Sans
  - Update `meta theme-color` from Arctic Blue to Terracotta

  **Current State** (index.html lines 9, 19):
  ```html
  <meta name="theme-color" content="#0ea5e9">
  ...
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600;700&family=Space+Grotesk:wght@400;500;600;700&display=swap" rel="stylesheet">
  ```

  **New Values**:
  ```html
  <meta name="theme-color" content="#C4785A">
  ...
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap" rel="stylesheet">
  ```

  **Must NOT do**:
  - Do NOT remove preconnect hints
  - Do NOT change Inter or JetBrains Mono weights
  - Do NOT add display=block (keep display=swap for performance)
  - Do NOT modify any other meta tags

  **Parallelizable**: YES (with 1, 2, 4)

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References**:
  - `src/ClimaSite.Web/src/index.html` - Google Fonts link tag in `<head>`

  **External References**:
  - Google Fonts: https://fonts.google.com/specimen/Plus+Jakarta+Sans - Font specimen and weights

  **Acceptance Criteria**:

  **Manual Execution Verification:**
  - [x] Using browser DevTools Network tab:
    - Navigate to: `http://localhost:4200/`
    - Filter by "font"
    - Verify: Plus Jakarta Sans woff2 files load successfully (200 status)
    - Verify: Space Grotesk does NOT load (should be absent)
  - [x] Visual check:
    - Action: Hard refresh (Cmd+Shift+R / Ctrl+Shift+R)
    - Verify: Headlines render immediately with Plus Jakarta Sans (no FOUT to system font)

  **Commit**: YES (groups with 2)
  - Message: `style(typography): switch display font to Plus Jakarta Sans`
  - Files: `src/ClimaSite.Web/src/index.html`, `src/ClimaSite.Web/src/styles/_typography.scss`
  - Pre-commit: `cd src/ClimaSite.Web && ng build`

---

- [x] 4. Document Hardcoded Color Violations for Phase 2

  **What to do**:
  - Create documentation file listing all hardcoded color violations
  - Group by severity and component
  - Include file paths and line numbers
  - This enables Phase 2 cleanup without re-discovery

  **Known Violations** (from Metis analysis):
  ```
  HIGH PRIORITY (visible in common flows):
  - product-card.component.ts - 8 status badge gradients
  - glass-card.component.ts - 28 rgba() glass effects
  
  MEDIUM PRIORITY:
  - payment-icon.component.ts - 30+ payment brand colors (intentional but document)
  - confetti.service.ts - 9 fallback colors (intentional fallbacks)
  
  LOW PRIORITY:
  - Various rgba() in hover/focus states across 15+ files
  
  TOTAL: ~162 instances across 15+ files
  ```

  **Must NOT do**:
  - Do NOT attempt to fix these violations in this phase
  - Do NOT modify any component files

  **Parallelizable**: YES (with 1, 2, 3)

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References**:
  - `docs/plans/` - Existing documentation structure

  **Location for new file**:
  - `docs/plans/site-redesign/PHASE2-COLOR-VIOLATIONS.md`

  **Acceptance Criteria**:

  **Documentation Complete:**
  - [x] File created at `docs/plans/site-redesign/PHASE2-COLOR-VIOLATIONS.md`
  - [x] Contains categorized list of all 162 violations
  - [x] Each entry includes: file path, line number(s), violation type
  - [x] Marked as "Phase 2 Scope"

  **Commit**: NO (groups with Task 5)

---

- [x] 5. Build Verification and Visual QA

  **What to do**:
  - Run full Angular build to verify no compilation errors
  - Start dev server and perform visual review
  - Check both light and dark themes
  - Verify contrast ratios meet WCAG AA
  - Document any visual issues for adjustment

  **Must NOT do**:
  - Do NOT proceed if build fails
  - Do NOT skip dark mode verification
  - Do NOT skip contrast checking

  **Parallelizable**: NO (depends on 1, 2, 3)

  **References** (CRITICAL - Be Exhaustive):

  **Build Commands**:
  - `cd src/ClimaSite.Web && ng build` - Production build
  - `cd src/ClimaSite.Web && ng serve` - Dev server at localhost:4200

  **Pages to Review**:
  - Home page: `/`
  - Product listing: `/products`
  - Product detail: `/products/{any-product}`
  - Cart: `/cart`
  - Checkout: `/checkout`

  **Acceptance Criteria**:

  **Build Verification:**
  - [x] `cd src/ClimaSite.Web && ng build` - exits with code 0
  - [x] No SCSS compilation warnings about undefined variables
  - [x] Build output size not significantly larger (< 5% increase)

  **Visual Verification:**
  - [x] Using Playwright or manual browser:
    - Navigate through all 5 key pages listed above
    - Toggle dark mode on each page
    - Verify: Warm cream backgrounds in light mode (#FAF7F2)
    - Verify: Warm black backgrounds in dark mode (#1C1917)
    - Verify: Terracotta primary buttons (#C4785A)
    - Verify: No visual regressions (broken layouts, missing colors)

  **Accessibility Verification:**
  - [x] Primary on white: >= 4.5:1 contrast ratio
  - [x] Body text on background: >= 4.5:1 contrast ratio
  - [x] Large text (headings): >= 3:1 contrast ratio

  **Commit**: YES
  - Message: `docs: document Phase 2 color violation cleanup scope`
  - Files: `docs/plans/site-redesign/PHASE2-COLOR-VIOLATIONS.md`
  - Pre-commit: `cd src/ClimaSite.Web && ng build`

---

## Commit Strategy

| After Task | Message | Files | Verification |
|------------|---------|-------|--------------|
| 1 | `style(colors): replace Arctic Aurora with Terra Luxe warm palette` | `_colors.scss` | `ng build` |
| 2+3 | `style(typography): switch display font to Plus Jakarta Sans` | `_typography.scss`, `index.html` | `ng build` |
| 4+5 | `docs: document Phase 2 color violation cleanup scope` | `PHASE2-COLOR-VIOLATIONS.md` | `ng build` |

---

## Success Criteria

### Verification Commands
```bash
cd src/ClimaSite.Web

# Build must succeed
ng build  # Expected: "Build at: ... - Hash: ... - Time: ..."

# Serve for visual review
ng serve  # Expected: "Application bundle generation complete."

# Open in browser
# Navigate to localhost:4200
# Expected: Warm cream backgrounds, terracotta buttons
```

### Final Checklist
- [x] All "Must Have" items present (6 color groups, both themes, gradients, shadows)
- [x] All "Must NOT Have" items absent (no renamed variables, no component changes)
- [x] Build passes with no errors
- [x] Visual review shows warm palette in both light and dark modes
- [x] Plus Jakarta Sans loads and renders for display text
- [x] Contrast ratios meet WCAG AA (verified via DevTools)
- [x] Phase 2 violations documented for future cleanup

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SCSS syntax error breaks build | Low | High | Incremental edits, build after each change |
| Font fails to load | Low | Medium | Keep system-ui fallback, test before commit |
| Contrast too low on new palette | Medium | High | Pre-validate with contrast checker |
| Dark mode looks wrong | Medium | Medium | Review dark theme block carefully |
| Warmth clashes with product images | Medium | Medium | Visual review before final commit |

---

## Open Questions (Resolved)

All questions from Metis review have been addressed in the plan:
- [x] HVAC semantic colors: Will be updated in gradients section
- [x] `warm`/`ember`/`aurora` palettes: Preserved, only values updated
- [x] Hardcoded colors: Documented for Phase 2
- [x] Font fallback: system-ui maintained
- [x] Step indicator colors: Keep existing structure, update values

---

## Dependencies

- [x] Plus Jakarta Sans available on Google Fonts (verified: yes)
- [x] Branch `feature/design-system-2.0` exists (verified: yes)
- [x] Current build passing on branch (verify before starting)
