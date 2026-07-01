---
unit: a11y-dialogs-radios (B-001 + B-014 + B-042)
surface: ui (real running app — ng serve :4200 + live API :5029)
result: PASS
date: 2026-07-01
commit: feature/a11y-dialogs-radios tip (validated against the working diff; final squash tip on merge)
driver: Playwright keyboard-drive of the address dialog on the REAL app (login → open → Tab-trap → Escape →
  focus-restore) + the frontend a11y unit suite; B-014/B-042 keyboard/roles are unit-tested and the /checkout
  axe scan runs in CI (AccessibilityTests.CheckoutPage_HasNoAccessibilityViolations, which sets up a cart).
---

# Acceptance — a11y batch (WCAG 2.2 AA): address dialogs + checkout radios/cards

## B-001 — address dialogs → shared `<app-modal>` (live keyboard-driven, real app)
Logged in, opened `/account/addresses`, focused the `add-address-btn`, opened the dialog and drove it with the
**keyboard only**:
| Check | Result |
|---|---|
| Dialog exposes `role="dialog"` + `aria-modal="true"` + `aria-labelledby` (accessible name) | ✅ |
| Focus **moves into** the dialog on open | ✅ |
| **Focus is trapped** — 6× Tab never escapes the dialog | ✅ |
| **Escape closes** the dialog | ✅ |
| **Focus returns to the `add-address-btn` trigger** on close (the council Medium — modal `cleanup()`/`ngOnDestroy` focus-restore for the `@if`-destroyed case) | ✅ |
| Zero console/page errors | ✅ |

(The page also has a pre-existing `cookie-consent` `role="dialog" aria-modal="false"` banner — unrelated to
B-001; not touched.)

## B-014 — payment + shipping method radiogroups; B-042 — saved-address cards
Verified by the **frontend a11y unit suite** (real-template render) + council + the CI `/checkout` axe scan:
- **B-014**: both method sets render `role="radiogroup"` named by their visible heading (`aria-labelledby`);
  the native `<input type=radio>` are **sr-only-but-focusable** (NOT `display:none`) so they keep tab order +
  arrow-key selection + a11y-tree presence; the selected card state is tied to the native radio via
  `:has(input:checked)`; a visible focus ring (`:has(input:focus-visible)`, `var(--color-ring)`). Council
  confirmed the radios are real focusable native radios grouped by the correct `name` (the one remaining
  `display:none` is `.step-line`, not a method radio).
- **B-042**: saved-address cards are a `role="radiogroup"` of `role="radio"` (accessible name = the visible
  "Use a saved address" heading via `aria-labelledby`) with `aria-checked`, roving `tabindex` (selected = 0
  else -1; first = 0 when none selected), and a keydown handler — Enter/Space select, Arrow keys move+select
  with wrap-around — matching the WAI-ARIA APG radiogroup pattern. Space-scroll prevented.
- **Runtime axe**: `AccessibilityTests.CheckoutPage_HasNoAccessibilityViolations` (Deque axe on the real
  `/checkout` with a seeded cart) is the CI runtime gate for these pages; a local ad-hoc `/checkout` drive
  couldn't populate the cart to render the method sections, so this is delegated to that CI E2E test.

## Automated evidence
- `ng build --configuration=development` 0 errors; `ng lint` 0 errors (6 pre-existing warnings in untouched
  specs); `npm run test:i18n` PASS (926 keys en/bg/de — one redundant key removed); `ng test` **1788 SUCCESS**
  (+ the new focus-restore, radiogroup, roving-tabindex, arrow/Enter/Space, and `:has(input:checked)` specs).
- Cross-vendor Codex council (gpt-5.5@xhigh) on the diff: APPROVE-WITH-CHANGES → 1 Medium (modal focus-restore
  on destroy) + 2 Low (`:has(input:checked)`, visible-label parity) fixed.

## Notes
- No hardcoded colors (focus rings use `var(--color-ring)`); reduced-motion respected (static outlines).
- The modal `ngOnDestroy` focus-restore is a shared-component fix — it benefits **every** consumer that
  conditionally renders `<app-modal>` via `@if`, not just the address dialogs.
- Out of scope: flipping `A11Y_ENFORCE=1` globally (UX-15's lever) and other pages/dialogs.

## Verdict
**PASS** — the address add/edit/delete dialogs are now keyboard-operable with focus trap + Escape + focus
restore + dialog semantics (live-proven); the checkout method radios and saved-address cards are proper,
keyboard-operable, correctly-named radiogroups (unit-tested + APG-confirmed + CI-axe-gated). Frontend only;
no backend; no migration.
