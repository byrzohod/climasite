# ClimaSite — Project Plan

**Created:** 2026-06-11 · **Review date:** 2026-06-11

This folder is the output of a full project review of ClimaSite (senior-engineering, product, security, QA, and documentation lenses) and the **living planning hub** for the rest of the project. It was produced by reading the working tree of `feature/plan18-wishlist-completion` directly; every finding cites `file:line` evidence, and every P0/P1 finding was adversarially verified before being kept.

## How to use this folder

Read in this order:

1. **[PROJECT_STATUS.md](PROJECT_STATUS.md)** — what ClimaSite actually is, what works, what is claimed-but-not-real, and what blocks launch. The single source of truth for current state; start here.
2. **[CONSOLIDATED_ROADMAP.md](CONSOLIDATED_ROADMAP.md)** — the path from today to launch in four milestones (M1 Stabilize & Secure → M2 Operable & Compliant → M3 Production-Ready → M4 Feature-Complete). Supersedes `docs/plans/18-project-completion.md` as the planning entry point.
3. **[PRIORITIZED_BACKLOG.md](PRIORITIZED_BACKLOG.md)** — THE actionable, deduplicated task list (task IDs `SEC-`, `BUG-`, `GAP-`, `OPS-`, `TS-`, `UX-`, `ARCH-`, `PERF-`, `DOC-`), ending with the strict **"Next 10 tasks"** execution order. This is the work queue.

Then consult the specialty reference docs as needed — each is the deep version of one review dimension:

| Document | Covers |
|---|---|
| [SECURITY_REVIEW.md](SECURITY_REVIEW.md) | Findings SR-01..SR-20 (Critical→Low), manual pen-test list, production security checklist, dependency status |
| [BUGS_AND_TECH_DEBT.md](BUGS_AND_TECH_DEBT.md) | Likely bugs BUG-01..BUG-18, TODO/FIXME triage, technical-debt register, fragile areas, refactoring recs |
| [TESTING_STRATEGY.md](TESTING_STRATEGY.md) | Real test-stack ground truth, coverage matrix, CI-gate gaps, critical journeys, backlog TS-01..TS-17 |
| [UI_UX_REVIEW.md](UI_UX_REVIEW.md) | Missing states, form/nav issues, accessibility, responsiveness, i18n status, theming, work list (P0–P3) |
| [ARCHITECTURE_REVIEW.md](ARCHITECTURE_REVIEW.md) | Layering verdicts, CQRS hygiene, data flow, frontend structure, improvements, "areas to avoid changing" |
| [PRODUCTION_READINESS_CHECKLIST.md](PRODUCTION_READINESS_CHECKLIST.md) | Launch-blocker table + checklists: security, testing, performance, observability, deploy, data, legal, QA |
| [DEV_WORKFLOW.md](DEV_WORKFLOW.md) | Corrected local setup, branching, **correct test commands**, PR/merge gates, release process, Definition of Done |
| [DECISIONS.md](DECISIONS.md) | Decision log: backfilled D-001..D-017, open owner decisions O-1..O-7; `docs/adr/` remains the canonical ADR home |

### `_review/` — raw verified findings

The [`_review/`](_review/) subfolder holds the ten dimension-by-dimension finding records the summary docs above were synthesized from (`status`, `product`, `uiux`, `security`, `architecture`, `bugs`, `testing`, `performance`, `docs`, `devops`). Each finding carries full evidence, verifier notes, and per-dimension inventory tables. Read these only when you need the proof behind a backlog task or want a dimension's complete table — the top-level docs are the curated, deduplicated view.

## Maintenance rules

This is a living set of docs, not a one-time report. Keep it honest:

- **`PRIORITIZED_BACKLOG.md`** — update on every merged PR: tick off the task, note new findings discovered during the work.
- **`PROJECT_STATUS.md`** — refresh at each milestone boundary (and whenever a "claimed-vs-actual" row is resolved). It must never lag behind reality the way `CLAUDE.md`'s table did.
- **`CONSOLIDATED_ROADMAP.md`** — revisit when a milestone closes or a blocking decision (DEC-CURRENCY, OPS-08, O-1…) is resolved; record the decision as an ADR in `docs/adr/`.
- **Specialty docs** (SECURITY/TESTING/UI_UX/etc.) — amend when their domain changes materially; do not let them drift into the same staleness this review found in `docs/validation/`.
- **`_review/`** — treat as a dated snapshot (2026-06-11). Do not edit retroactively; if a finding is resolved, mark it done in the backlog rather than rewriting history here.

## Relationship to existing docs

- **`docs/plans/`** — historical and feature-level implementation plans. `18-project-completion.md` is still a valid feature plan whose task IDs this folder absorbs, but **this folder is now the planning hub** (see the roadmap's "Relationship to Plan 18"). `00-master-overview.md` and `20-issue-registry.md` are materially stale (see DOC-02/DOC-04) — do not trust their status claims.
- **`docs/audit/2026-04-08-gap-report.md`** and **`docs/validation/areas/07-wishlist.md`** — the most accurate of the older docs; used as leads and re-verified here.
- **`docs/adr/`** — remains the canonical home for Architecture Decision Records; `DECISIONS.md` indexes it and backfills decisions that were never recorded.
- **`CLAUDE.md` / `AGENTS.md`** — agent operating instructions. Their *status table* and *test commands* are wrong today (DOC-01/DOC-02 fix them); their conventions otherwise still apply.
