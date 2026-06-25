> **COMPLETE & SUPERSEDED for NEW work (2026-06-25).** The PROC-01 8-phase per-feature pipeline below
> shipped (all 7 build waves — see [`PROC-01/STATE.md`](PROC-01/STATE.md)) and is **kept as a reference
> case study**. New features no longer use this `docs/features/<ID>/` flow: the authoritative planning
> system for new `src/**` work is the vault `.planning/units/<unit>/unit-plan.md` + `/plan-tree` flow,
> gated by the `no-spec-no-code` hook. Start at [`.planning/STATE.md`](../../.planning/STATE.md) (resume
> contract); the worked example is `.planning/units/UX-15-contrast/unit-plan.md`, and the migration is
> tracked as "Process debt" in STATE.md and as plan [19](../plans/19-test-and-kg-hardening.md).
> Do not delete this folder or `PROC-01/STATE.md` — they are the archive of how the pipeline was built.

# Per-feature pipeline (PROC-01)

Every non-trivial feature or fix flows through **eight gated phases**. Each phase produces an
artifact in `docs/features/<FEAT-ID>/`, and each gate is anchored to a real blocker (a hook, a CI
required-check, or a read-only agent) so it can't be skipped or rushed. This directory is the
single home for that per-feature work; the full rationale is `docs/project-plan/SDLC_HARDENING_PLAN.md`.

> **Canonical workflow:** `docs/project-plan/DEV_WORKFLOW.md` (local setup, commands, branching).
> This file is the *feature lifecycle*; DEV_WORKFLOW.md is the *day-to-day mechanics*.

## The eight phases

| # | Phase | Artifact in `docs/features/<FEAT-ID>/` | Gate (what blocks you) |
|---|-------|----------------------------------------|------------------------|
| 1 | **Research** | `research.md` | Every affected-file path resolves; ≥1 happy + ≥1 edge + ≥1 error journey; constraints listed. |
| 2 | **Plan** | `plan.md` (`plan_status: draft`) | Non-empty exit-criteria table + test matrix; payment/auth/GDPR/migration flagged for ADR. |
| 3 | **Verify-Plan** | `plan-verification.md` → flips `plan_status: approved` | `/verify-plan` (read-only **verifier** agent) re-confirms paths/assumptions/matrix/ADR. **Code edits to `src/` are blocked until approved** (Wave 2 hook). |
| 4 | **Track** | backlog row + `test-design.md` (+ ADR if flagged) | Every journey → a stable Scenario ID marked `automated`\|`manual`. |
| 5 | **Implement** | code **+ colocated tests** on a `feature/`\|`fix/` branch; `STATE.md` ticks | Tests land with code (Wave 2 "tests-with-feature" hook); no anti-patterns. |
| 6 | **Test** ⭐ | green CI: real-infra E2E + a11y (+ visual/perf where applicable) across light/dark × EN/BG/DE | CI required status checks (server-side, agent-proof). |
| 7 | **Review** ⭐ | `review.md` (+ `security-review.md` for auth/payment/GDPR) | Read-only **reviewer** agent + `/code-review`; verifier confirms tests are real, not placeholders. |
| 8 | **Merge** | squash to `main`; the **DEV_WORKFLOW.md §5 doc-update table** applied in the same PR (`CHANGELOG.md [Unreleased]`, `PROJECT_STATUS.md` / `PRIORITIZED_BACKLOG.md` / `CLAUDE.md` status, ADR if a decision was made) | Branch protection (6 required checks, `enforce_admins=true`). |

Phases 1–4 are the **front phases** built in Wave 1; the hooks/CI that *enforce* them land in Waves 2–4.

## How to start a feature

```
/feature-kickoff <FEAT-ID> "<short title>"
```

This scaffolds `docs/features/<FEAT-ID>/` from `_template/` and appends a backlog row to
`docs/project-plan/PRIORITIZED_BACKLOG.md`. Then:

1. Fill in `research.md` (Phase 1) from the real code — grep, don't guess.
2. Write `plan.md` (Phase 2); keep `plan_status: draft`.
3. Run `/verify-plan <FEAT-ID>` (Phase 3). The verifier agent re-checks the plan against the working
   tree and, only if it holds up, writes `plan-verification.md` and flips `plan_status: approved`.
   **Do not hand-edit `plan_status` to `approved`** — the verifier is read-only precisely so a plan
   can't be self-laundered.
4. Complete `test-design.md` + the backlog row (Phase 4), write an ADR if the plan flagged one.
5. Create the branch and implement with colocated tests (Phase 5) → green CI (Phase 6) →
   review (Phase 7) → merge (Phase 8).

## FEAT-ID scheme

Reuse the backlog category prefixes (`SEC`, `BUG`, `GAP`, `OPS`, `ARCH`, `PERF`, `TS`, `DOC`, `UX`,
`PROC`) + the next free number in that category (e.g. `GAP-10`, `PROC-02`). The folder name **is** the
FEAT-ID, and it matches the backlog row's ID so the two never drift.

## Conventions

- `/feature-kickoff` and `/verify-plan` are **skills** (`.claude/skills/<name>/SKILL.md`), invoked by name like every other automation in this repo — there is no `.claude/commands/` directory.
- `_template/` is the source of truth for the artifact shapes — improve the templates there, not in copies.
- Small fixes that don't touch `src/` (typo, doc, config) don't need a full folder; use judgment, but
  anything touching `src/` needs at least an approved `plan.md` (the Wave 2 hook enforces this).
