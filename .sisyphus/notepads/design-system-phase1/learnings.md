# Learnings - Design System Phase 1

## Conventions Discovered

### Color System Structure
- 558-line `_colors.scss` is single source of truth
- 11-shade palettes (50-950) for each color group
- Both `:root` (light) and `[data-theme="dark"]` blocks must be updated in sync
- Shadow colors use rgba with warm/cool undertones
- Tailwind config references CSS variables (not hardcoded)

### Typography System
- Three font families: display (headlines), body, mono (code/specs)
- Google Fonts loaded via index.html with `display=swap` for performance
- Font fallback chain includes system-ui for reliability

### Hardcoded Color Violations
- 44 hardcoded hex colors found across 3 files
- Payment icons (30 violations) are brand-mandated - DO NOT change
- Product card status badges (5 violations) should use CSS variables
- Confetti service (9 violations) are intentional fallbacks - update values only

## Architectural Choices

### Surgical Replacement Approach
- Changed VALUES only, never NAMES of CSS variables
- Preserved `warm`, `ember`, `aurora` palettes (needed for HVAC product differentiation)
- Deferred component-level refactoring to Phase 2

### Commit Strategy
- Atomic commits per logical unit:
  1. Colors only
  2. Typography + index.html together
  3. Documentation separately

## Issues Encountered

### Subagent Delegation Challenges
- Multi-step tasks rejected by subagents (correctly enforcing SINGLE TASK ONLY directive)
- Solution: Break into truly atomic tasks (one section, one file, one verification)
- Successful pattern: "Update ONLY lines X-Y in file Z"

### Build Warnings
- Pre-existing CSS syntax warnings in component files (not from our changes)
- Build still succeeds (exit code 0)
- Warnings documented for future cleanup

## Gotchas

### Meta Theme Color
- Must update `<meta name="theme-color">` in index.html to match new primary
- Affects mobile browser chrome color

### Shadow Undertones
- Light theme shadows: rgba(28, 25, 23, x) - warm brown
- Dark theme shadows: rgba(0, 0, 0, x) - pure black
- Undertone change is subtle but important for cohesion

## Success Metrics Achieved

✅ Build passes with zero SCSS errors  
✅ All 5 tasks completed  
✅ 3 atomic commits created  
✅ Phase 2 violations documented (44 instances)  
✅ No variable names changed (1400+ references preserved)  
✅ Both light and dark themes updated  

## Recommendations for Future Phases

1. **Phase 2**: Refactor hardcoded colors in components
2. **Phase 3**: Visual QA with real browser testing (Playwright)
3. **Phase 4**: Contrast validation with automated tools
4. **Phase 5**: Component library rebuild (per original 12-week plan)

---

**Session**: ses_40b95ac67ffe989E7zpfiFAyUw  
**Completed**: 2026-01-25  
**Duration**: ~45 minutes  
**Files Changed**: 4 (colors, typography, index.html, documentation)
