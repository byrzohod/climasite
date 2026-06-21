<!--
PHASE 1 — RESEARCH. The first artifact in the gated pipeline (docs/features/README.md).
Fill EVERY section from the real working tree (grep/read the code) — not from memory.
Exit criterion (Phase 1 → Phase 2): every "Affected files" path resolves on disk; at
least one happy + one edge + one error user journey is written; the constraints checklist
is completed. Plan.md must cite line numbers in THIS file, so keep its content stable.
-->

# Research — <FEAT-ID>: <short title>

- **Feature ID:** <FEAT-ID> (matches the backlog row + the `docs/features/<FEAT-ID>/` folder)
- **Author:** <handle/agent>
- **Date:** <YYYY-MM-DD>

## 1. Goal

One paragraph: what user-facing or operational problem this solves and the desired end state.
State who the user is (guest / customer / admin) and what they can do after this ships that they
can't today.

## 2. Affected files (verified by grep — every path must resolve)

> List the real files this work will touch or depend on. Verify each exists:
> `for f in <paths>; do test -e "$f" && echo "ok $f" || echo "MISSING $f"; done`.
> A path that doesn't resolve fails the Phase-1 gate.

| Path | Role in this feature | Verified |
|------|----------------------|:--------:|
| `src/ClimaSite.Api/Controllers/<X>.cs` | <what changes / why it matters> | ☐ |
| `src/ClimaSite.Application/Features/<X>/...` | | ☐ |
| `src/ClimaSite.Core/Entities/<X>.cs` | | ☐ |
| `src/ClimaSite.Web/src/app/features/<X>/...` | | ☐ |
| `src/ClimaSite.Web/src/assets/i18n/{en,bg,de}.json` | translation keys | ☐ |

## 3. Existing tests (what already covers this area)

> Find them: `grep -rl "<symbol>" tests/ src/**/*.spec.ts`. List unit / integration / E2E that
> touch this code path, so the plan extends them instead of duplicating.

| Test file | Level (unit/integration/E2E) | What it covers today |
|-----------|------------------------------|----------------------|
| | | |

## 4. Constraints (check every box that applies — these gate the plan's ADR/test decisions)

- [ ] **Payment** — touches Stripe / orders / money path (→ ADR + real-card E2E required, see Wave 6)
- [ ] **Auth** — touches JWT / login / roles / guards
- [ ] **GDPR / user data** — touches personal data, export, or delete (stated business rule)
- [ ] **i18n** — adds/changes user-facing text (must land in en.json, bg.json, de.json)
- [ ] **Theme** — adds UI (must work in light AND dark; colors only from `_colors.scss`)
- [ ] **a11y** — adds interactive UI (keyboard, focus, ARIA, reduced-motion; axe must pass)
- [ ] **Migration** — changes the DB schema (→ ADR + expand-contract via `/db-migrate`)
- [ ] **z-index / overlay** — adds an overlay/drawer/modal (use `var(--z-*)`; see CLAUDE.md layering rule)

Notes on each checked constraint (what specifically must hold):

## 5. User journeys (≥1 happy + ≥1 edge + ≥1 error — these become Scenario IDs in test-design.md)

> Write them as real human interactions ("the user clicks…"), the way an E2E test will drive them.

### Happy path
- **H1 —** <as a `<role>`, when I …, then …>

### Edge cases
- **E1 —** <boundary / empty / concurrent / unusual-but-valid input → expected behavior>

### Error paths
- **X1 —** <invalid input / declined card / network failure / unauthorized → graceful handling>

## 6. Open questions / owner decisions needed

> Anything that needs an owner decision (per the batch-questions rule) before planning. If none, say so.

- [ ] <question> — **blocks:** <plan section>
