---
name: climasite-ui-qa
description: Use after ClimaSite UI changes, before PR merge, or when checking visual quality, responsiveness, i18n, accessibility, console errors, or Playwright screenshots.
---

# ClimaSite UI QA

Run the app locally, then verify changed pages in light and dark themes and across responsive breakpoints.

Check:
- No visible translation keys, placeholder text, `undefined`, `null`, `NaN`, TODO/FIXME/HACK, or debug output.
- EN/BG/DE text fits without overflow; German expansion is a useful stress case.
- No hardcoded colors; use theme variables.
- No horizontal scroll, overlap, clipped text, or layout shift.
- Focus indicators, keyboard navigation, accessible names for icon buttons, labels for form controls.
- Touch targets are at least 44x44 px on mobile.
- Loading, empty, error, and validation states are present.
- Console and network errors are understood and reported.
- Capture Playwright screenshots for meaningful UI changes.

