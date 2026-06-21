---
name: accessibility-audit
description: Deep WCAG 2.2 AA accessibility audit including keyboard navigation, screen reader compatibility, color contrast, ARIA review, focus management, and reduced-motion support. Use this whenever the user mentions accessibility, a11y, WCAG, ADA, Section 508, screen readers, VoiceOver, NVDA, keyboard navigation, color contrast, ARIA, or compliance for users with disabilities, especially for gov/healthcare/education/large-consumer projects.
---

# /accessibility-audit - Deep WCAG 2.2 AA conformance pass

## When to use

Invoke this skill:
- **Before launching public-facing UI** -- baseline a11y conformance
- **When accessibility is critical** -- gov, healthcare, education, large consumer products
- **After major design system changes** -- catch regressions in shared components
- **When users report a11y issues** -- structured investigation

`/ui-qa` covers a11y at a checklist level (contrast, focus, alt text, labels). This skill goes deeper: WCAG 2.2 AA conformance, keyboard-only flows, screen reader behavior, ARIA correctness, cognitive load, motion sensitivity. Use `/ui-qa` for every PR; use `/accessibility-audit` when a11y is a primary product concern.

## Standards Targeted

- **WCAG 2.2 Level AA** -- the accepted modern baseline
- **WAI-ARIA 1.2** -- correct ARIA usage; broken ARIA is worse than no ARIA
- **Section 508** (US gov) -- if the project is US gov-adjacent
- **EN 301 549** (EU public sector) -- if the project ships in the EU public sector
- **WCAG 2.2 Level AAA** -- target where feasible (most products won't reach AAA across the board, but specific success criteria are achievable and worth aiming for)

If the project is in a regulated industry, confirm the applicable standard with the user before auditing.

## Audit Areas

### 1. Perceivable

#### 1.1 Text Alternatives
- [ ] Every non-decorative `<img>` has descriptive `alt` text
- [ ] Decorative images use `alt=""` (not omitted)
- [ ] Icons that convey meaning have `aria-label` or visually-hidden text
- [ ] Icons that are decorative use `aria-hidden="true"`
- [ ] SVG illustrations have `<title>` and `role="img"` (or be hidden)
- [ ] Chart/diagram images have a textual summary nearby
- [ ] Audio/video has captions (live or pre-recorded)
- [ ] Audio descriptions for video where visual content is essential

#### 1.2 Time-Based Media
- [ ] Captions on all pre-recorded video
- [ ] Live captions on live video
- [ ] Transcripts for audio-only content
- [ ] No autoplay video with sound (or one-click stop)

#### 1.3 Adaptable
- [ ] Heading hierarchy is correct (single h1 per page, no level skips)
- [ ] Semantic HTML used for structure (`<nav>`, `<main>`, `<aside>`, `<article>`, `<section>`)
- [ ] Form labels are associated with inputs (label[for] -> input[id], or wrapping)
- [ ] Lists use `<ul>` / `<ol>` / `<dl>`, not `<div>`-soup
- [ ] Tables have `<th scope>`, `<caption>`, and proper structure
- [ ] Meaningful sequence preserved when CSS is removed (does the page still make sense?)
- [ ] Orientation not locked to landscape or portrait without reason
- [ ] Inputs use correct `autocomplete` attributes
- [ ] Identifying purpose: form fields and UI components have programmatically determinable purpose

#### 1.4 Distinguishable
- [ ] Color contrast: 4.5:1 for normal text, 3:1 for large (>=18px or >=14px bold) and UI components
- [ ] Color is never the only way to convey information
- [ ] Audio control: any auto-playing audio over 3s can be paused/stopped/muted
- [ ] Text resize to 200% does not break layout or hide content
- [ ] Images of text avoided (use real text + CSS)
- [ ] Reflow: no horizontal scrolling at 320px width (except for content that must, like data tables)
- [ ] Non-text contrast: 3:1 for UI components and graphical objects
- [ ] Text spacing: layout doesn't break with line-height 1.5x, paragraph spacing 2x font size, letter-spacing 0.12x, word-spacing 0.16x
- [ ] Content on hover/focus: dismissible, hoverable (can move pointer without dismissing), persistent (stays until dismissed)

### 2. Operable

#### 2.1 Keyboard Accessible
- [ ] Every interactive element reachable by Tab
- [ ] Every interactive element activatable by Enter/Space (correct one for the element)
- [ ] Logical tab order matches visual order (no tabindex > 0)
- [ ] No keyboard traps -- focus can always move out
- [ ] Custom shortcuts can be turned off, remapped, or only active on focus
- [ ] Skip-to-main-content link present on every page

#### 2.2 Enough Time
- [ ] Time limits can be adjusted, extended, or disabled (or are essential, like an auction)
- [ ] Auto-updating content can be paused (carousels, news tickers, auto-refresh feeds)
- [ ] No content moves/blinks/flashes for more than 5 seconds
- [ ] Re-authentication after timeout preserves user data

#### 2.3 Seizures and Physical Reactions
- [ ] No content flashes more than 3 times per second
- [ ] Animation respects `prefers-reduced-motion` media query
- [ ] Parallax, auto-scroll, and large transforms all gated by reduced-motion preference

#### 2.4 Navigable
- [ ] Page title is unique and describes the page
- [ ] Focus order is meaningful
- [ ] Link purpose clear from text alone (no "click here", "read more" without context)
- [ ] Multiple ways to find a page (search, sitemap, navigation, breadcrumbs)
- [ ] Headings and labels describe topic or purpose
- [ ] Visible focus indicator on every focusable element (`outline: none` requires a replacement)
- [ ] Focus not obscured: minimum/enhanced (the focused element is fully visible, not behind sticky headers/footers)

#### 2.5 Input Modalities
- [ ] Touch targets at least 24x24 CSS px (WCAG 2.2 minimum), aim for 44x44
- [ ] Pointer gestures: any path-based gesture (swipe, pinch) has a single-pointer alternative
- [ ] Pointer cancellation: actions trigger on up-event, not down-event (so users can move off to cancel)
- [ ] Label in name: visible label text matches accessible name (so voice control works)
- [ ] Motion actuation: any feature triggered by device motion has a UI alternative
- [ ] Dragging movements: any drag interaction has a single-pointer non-drag alternative
- [ ] Help is available consistently across pages (where help exists)

### 3. Understandable

#### 3.1 Readable
- [ ] `<html lang="...">` set correctly
- [ ] Language changes within content marked with `lang` attribute
- [ ] Reading level: avoid technical jargon where plain language works
- [ ] Unusual words have definitions (glossary, hover, footnote)
- [ ] Abbreviations expanded on first use or via `<abbr title="...">`

#### 3.2 Predictable
- [ ] Focusing an element does not cause unexpected context changes (e.g., auto-submit on focus)
- [ ] Changing a setting does not cause unexpected context changes (e.g., auto-redirect on dropdown change)
- [ ] Navigation is consistent across pages (same nav in same place)
- [ ] Components with the same function look and behave consistently
- [ ] Consistent help: help mechanisms (chat, contact, FAQ links) appear in the same relative order across pages

#### 3.3 Input Assistance
- [ ] Errors identified clearly, in text (not just color)
- [ ] Labels and instructions present for required fields
- [ ] Error suggestions provided when known (e.g., "did you mean...?")
- [ ] Error prevention for legal/financial/data-deletion: confirm, review, or undo
- [ ] Redundant entry: data the user has entered before is auto-populated or selectable (not retyped)
- [ ] Accessible authentication: no cognitive function tests (CAPTCHAs requiring memorization, transcription, etc.) without alternative

### 4. Robust

#### 4.1 Compatible
- [ ] HTML parses cleanly (no duplicate IDs, properly nested elements)
- [ ] ARIA roles, states, and properties are valid for the elements they're on
- [ ] Status messages can be programmatically determined (use `role="status"`, `role="alert"`, or live regions)
- [ ] Custom controls have correct ARIA: `role`, `aria-expanded`, `aria-pressed`, `aria-controls`, etc.
- [ ] Custom controls work with screen readers (test with VoiceOver, NVDA, JAWS, TalkBack)

## Manual Testing Procedure

Automation catches ~40% of WCAG issues. The rest requires manual testing.

### Keyboard-Only Pass
- Disconnect or ignore the mouse
- Reach every interactive element via Tab and Shift+Tab
- Activate each control via Enter or Space (whichever is correct for the element)
- Verify Escape closes modals, menus, and tooltips
- Verify focus is never trapped or lost
- Verify visible focus indicator at all times
- Test arrow-key navigation in custom widgets (radio groups, listboxes, tabs, menus)

### Screen Reader Pass

Test with at least one screen reader:
- **macOS**: VoiceOver (Cmd+F5)
- **Windows**: NVDA (free) or JAWS (commercial)
- **iOS**: VoiceOver (triple-click side button)
- **Android**: TalkBack

Walk through:
- Navigate by headings (rotor / heading shortcut)
- Navigate by landmarks (rotor / landmark shortcut)
- Navigate by form fields
- Navigate by links
- Read the entire page top-to-bottom
- Listen for: unannounced state changes, unlabeled controls, redundant or misleading announcements

### Zoom & Reflow Pass
- Browser zoom to 200%, 300%, 400%
- Verify nothing is cut off, nothing requires horizontal scrolling at 400%
- Increase font size in OS settings (system-level zoom)
- Verify text in the app respects OS preferences

### Color & Contrast Pass
- Use a color contrast checker (axe DevTools, WebAIM Contrast Checker)
- Test in dark mode if supported
- Test with browser high-contrast mode (forced colors)
- Test with grayscale to verify color is never the sole carrier of meaning

### Reduced Motion Pass
- Enable OS-level reduced motion (macOS: System Settings -> Accessibility -> Display -> Reduce motion)
- Verify animations and transitions are disabled or significantly tamed
- Verify auto-play, parallax, and large transforms are stopped

### Cognitive Load Pass
- Read every page assuming distraction (scan, don't deeply parse)
- Are tasks broken into manageable steps?
- Are errors recoverable without losing work?
- Is jargon explained?
- Is reading level appropriate for the audience?

## Automated Tooling

Run alongside the manual passes -- automation is necessary but insufficient.

```js
// In Playwright tests
import AxeBuilder from '@axe-core/playwright';

test.describe('accessibility', () => {
  for (const path of ['/', '/login', '/dashboard', '/settings']) {
    test(`no a11y violations on ${path}`, async ({ page }) => {
      await page.goto(path);
      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa', 'best-practice'])
        .analyze();
      expect(results.violations).toEqual([]);
    });
  }
});
```

```bash
# Lighthouse a11y score (CI gate)
npx --yes @lhci/cli@latest collect --url=http://localhost:3000
# Set categories.accessibility >= 0.95 in lighthouserc.json
```

```bash
# pa11y (alternative, CLI-friendly)
npx --yes pa11y http://localhost:3000 --standard WCAG2AA
```

For component libraries: include axe checks in Storybook via `@storybook/addon-a11y`.

## Reporting

Produce an audit report with:

- **Scope**: which pages, which user flows, which standards
- **Pass/Fail per success criterion**: explicit, with evidence
- **Critical issues** (blocks users from completing core tasks): list, with file/line and fix suggestion
- **Major issues** (significantly degrades experience): list
- **Minor issues** (small papercuts): list
- **Pattern issues** (same root cause across multiple instances): grouped, with a single fix proposal
- **Manual test results**: keyboard pass, screen reader pass, zoom pass, reduced motion pass
- **Recommendations for design system changes**: where the same fix at the component level would resolve many instances

## Remediation Priority

Don't fix items in random order. Prioritize:

1. **Blockers**: keyboard traps, content invisible to screen readers, illegible contrast on critical text
2. **High-frequency patterns**: a missing-label issue affecting 30 forms is one fix in the form component
3. **High-traffic pages first**: home, login, sign-up, checkout
4. **WCAG 2.2 net-new criteria**: dragging movements, focus-not-obscured, accessible authentication, redundant entry, consistent help -- often missed in older audits

## Integration with other skills

- Run `/ui-qa` first as a quick check; this skill is the deeper follow-up
- After remediation, re-run this skill to confirm fixes (no regressions)
- For UI-heavy projects: include axe checks in CI (CI templates already wired with an `accessibility` job)
- For motion-heavy projects: pair with `/ui-animation` to ensure motion respects reduced-motion preferences

## When NOT to use

- Internal-only admin tools used by a small team where a11y is a soft requirement (use `/ui-qa` only)
- Pre-MVP prototypes where the UI will change significantly before launch
- API-only services with no UI

A11y is non-negotiable for public-facing apps. Internal tools are a judgment call -- but consider that team members may have or develop disabilities.
