---
unit: a11y-dialogs-radios
type: NORMAL
status: approved
created: 2026-07-01
plan_status: approved
design: ../../design/DESIGN.md
covers: [B-001, B-014, B-042]
---

# Unit plan — a11y batch: address dialogs + checkout radios/cards (WCAG 2.2 AA)

## Context / Definition of Ready
Three keyboard/screen-reader barriers, all verified:
- **B-001**: the account **address add/edit/delete dialogs** hand-roll a `.modal-overlay`
  (`features/account/addresses/addresses.component.ts:90,246`) with **no `role=dialog` / `aria-modal` /
  Escape / focus-trap / focus-restore** — unlike the shared `shared/components/modal/modal.component.ts`,
  which already provides ALL of that (`role="dialog"` + `aria-modal="true"` + `aria-labelledby`,
  `closeOnEscape` input, `setupFocusTrap()`, and focus-restore via `previouslyFocusedElement`).
- **B-014**: the checkout **payment + shipping method `<input type=radio>`** are `display:none`
  (`features/checkout/checkout.component.ts` CSS ~:695), so they're **out of the tab order AND the a11y
  tree** — keyboard/SR users are stuck on the preselected default and can't change method.
- **B-042**: the checkout **saved-address cards** are `<div (click)>` (`checkout.component.ts:84`) with
  **no `role`/`tabindex`/keydown**, selection conveyed visually only.

The E2E axe infra exists (`tests/ClimaSite.E2E/Tests/Accessibility/AxeAccessibilityMatrixTests.cs` +
`AccessibilityTests.cs`, gated by `A11Y_ENFORCE=1`). Colors are tokens in `_colors.scss`; motion respects
reduced-motion. All text is i18n.

## Scope / approach (frontend only; ONE PR)
1. **B-001 — address dialogs → shared `<app-modal>`.** Replace the hand-rolled `.modal-overlay` markup in
   `addresses.component.ts` (add/edit form dialog + the delete-confirm dialog) with `<app-modal>` bound to
   the existing open/close signals, passing a translated title (drives `aria-labelledby`) and wiring the
   `(closed)` output + `closeOnEscape`/`closeOnBackdropClick` (default true). Delete the now-dead overlay
   CSS. Result: Escape-to-close, focus-trap, focus-restore, `role=dialog`, `aria-modal` — for free.
2. **B-014 — real radiogroups for payment + shipping method.** Keep the native `<input type=radio>` (native
   semantics + arrow-key selection) but replace `display:none` with an **sr-only-but-focusable** rule (the
   input stays in the tree + tab/arrow order; `position:absolute;opacity:0;` NOT `display:none`), and put a
   **visible focus ring** on the associated visible label/card via `input:focus-visible + label` (or
   `:has()`). Wrap each set in a container with `role="radiogroup"` + an `aria-label`/`aria-labelledby`
   naming the group (e.g. "Payment method" / "Shipping method"). Verify keyboard: Tab reaches the group,
   arrows move + select, the selection is announced, and the visible card reflects `:checked`.
3. **B-042 — saved-address cards → keyboard-activatable radio.** Render each saved-address card as
   `role="radio"` inside a `role="radiogroup"` (`aria-label` = "Saved addresses"): add `tabindex`
   (roving: selected = 0, others = -1), `aria-checked`, and a `(keydown)` handler (Enter/Space select;
   Arrow Up/Down/Left/Right move the roving focus + select) plus the existing `(click)`. Give the group a
   visible focus ring via the color tokens. (Alternatively `<button>` per card — but radio matches the
   "pick one saved address" semantics and pairs with B-014's group.)

Shared: any new focus-ring styling uses `var(--color-ring)`/existing tokens (no hardcoded colors); new
group labels are i18n keys in en/bg/de.

## Acceptance criteria
- [ ] **B-001**: opening add/edit/delete moves focus into the dialog, **Tab is trapped** inside it,
      **Escape closes** it, and focus **returns** to the trigger; the dialog exposes `role=dialog` +
      `aria-modal=true` + an accessible name. Verified with the keyboard only + a screen-reader-tree check.
- [ ] **B-014**: both method groups are reachable by keyboard, each is a `role=radiogroup` with a name,
      arrow keys move + change the selection, the focused radio shows a visible focus ring, and the
      selection is programmatically determinable (`:checked` + reflected in the visible card).
- [ ] **B-042**: saved-address cards are a `role=radiogroup` of `role=radio`; Enter/Space + arrows select,
      `aria-checked` tracks selection, roving `tabindex`, visible focus ring.
- [ ] **axe**: `AxeAccessibilityMatrixTests` + `AccessibilityTests` report **0 new serious/critical**
      violations on `/account/addresses` and `/checkout` (light + dark) — no regression vs the current
      baseline; ideally these pages get cleaner.
- [ ] Works light + dark, EN/BG/DE; no visual regression; no new console/lint errors.

## Test / verification plan
- **Frontend (Jasmine)**: address dialog spec — opening renders `<app-modal>` (role=dialog/aria-modal),
  `(closed)` wired, Escape closes (input `closeOnEscape`); the delete-confirm dialog too. Checkout: the
  method groups render `role=radiogroup` with a name and focusable radios (NOT `display:none`); pressing an
  arrow key / selecting updates the model + `aria-checked`/`:checked`; saved-address cards render
  `role=radio` with `aria-checked`, `tabindex` roving, and a keydown handler that selects on Enter/Space and
  moves on arrows. Assert the old `<div (click)>`-only / `display:none` states are gone.
- **/acceptance (real app, keyboard-driven via Playwright)**: on `/account/addresses` open each dialog and
  drive it with Tab/Shift+Tab/Escape only (assert focus trap + restore + no focus escaping to the page);
  run an **axe scan** on `/account/addresses` and `/checkout` (light + dark) asserting **0 serious/critical**
  incl. the previously-failing rules; keyboard-select a payment method + a saved address with arrows/Enter
  and confirm the checkout model updates. Committed PASS at `.planning/acceptance/a11y-dialogs-radios.md`.
- **CI**: the six required checks; the axe matrix runs in the E2E job.

## Out of scope
- Broader a11y (other pages/dialogs), and flipping `A11Y_ENFORCE=1` globally (that's UX-15's enforcement
  lever; here we only ensure these two pages don't regress and fix the three specific barriers).
