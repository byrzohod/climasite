# ClimaSite Documentation — index & map

This is the "where do I look?" map for everything under `docs/`. Read it top-to-bottom once; after
that, jump straight to the canonical entry points below.

## Start here (resume contract)

**`.planning/STATE.md`** (repo root, not under `docs/`) is the single resume entry point. It is
auto-injected by the SessionStart hook after any `/clear`, compaction, or `--resume`, and is kept
fresh via `/checkpoint` at each unit/phase boundary. Read its **Next action** first. Everything else
is linked from there.

For what actually shipped (per PR), see [`../CHANGELOG.md`](../CHANGELOG.md) — the per-PR change history.

## The planning hub — `docs/project-plan/`

The living planning hub: the output of a full multi-lens project review plus the docs that are kept
current as work lands. Its own [`README.md`](project-plan/README.md) explains how to read the folder;
the one-line role of each file:

| File | Role |
|---|---|
| [PROJECT_STATUS.md](project-plan/PROJECT_STATUS.md) | What ClimaSite is, what works, what blocks launch. **Refreshed at milestone boundaries** — it is dated; can lag main by a full epic — always cross-check `.planning/STATE.md` + `CHANGELOG.md` for the latest. |
| [CONSOLIDATED_ROADMAP.md](project-plan/CONSOLIDATED_ROADMAP.md) | Path to launch in milestones (M1→M4); supersedes plan 18 as the roadmap entry point. |
| [PRIORITIZED_BACKLOG.md](project-plan/PRIORITIZED_BACKLOG.md) | THE actionable, deduplicated work queue (`SEC-`/`BUG-`/`GAP-`/`OPS-`/`ARCH-`/`PERF-`/`TS-`/`UX-`/`DOC-`/`PROC-` IDs). |
| [DECISIONS.md](project-plan/DECISIONS.md) | Decision log (D-001..); indexes `docs/adr/` and backfills decisions never recorded as ADRs. |
| [DEV_WORKFLOW.md](project-plan/DEV_WORKFLOW.md) | Canonical local setup, exact test commands, branching, PR/merge gates, release flow. Wins over CLAUDE.md where they differ. |
| [PRODUCTION_READINESS_CHECKLIST.md](project-plan/PRODUCTION_READINESS_CHECKLIST.md) | Launch-blocker table + security/testing/perf/observability/deploy/legal checklists. |
| [SECURITY_REVIEW.md](project-plan/SECURITY_REVIEW.md) · [TESTING_STRATEGY.md](project-plan/TESTING_STRATEGY.md) · [UI_UX_REVIEW.md](project-plan/UI_UX_REVIEW.md) · [ARCHITECTURE_REVIEW.md](project-plan/ARCHITECTURE_REVIEW.md) · [BUGS_AND_TECH_DEBT.md](project-plan/BUGS_AND_TECH_DEBT.md) | Deep per-dimension reference docs behind the backlog. |
| [SDLC_HARDENING_PLAN.md](project-plan/SDLC_HARDENING_PLAN.md) | The canonical, owner-approved PROC-01 process-overhaul plan (8 gated phases, 7 build waves). |
| [`_review/`](project-plan/_review/) | Raw, dated (2026-06-11) per-dimension finding records the summary docs were synthesized from. Treat as a frozen snapshot. |

## Decisions — `docs/adr/`

Architecture Decision Records. See [`adr/README.md`](adr/README.md) for the index, format, and the
immutability rule (never edit an accepted ADR's body to reverse it — write a superseding ADR). ADRs are
mirrored into the vault Knowledge graph (below). `DECISIONS.md` in the planning hub indexes these and
backfills decisions that predate the ADR process.

## Implementation plans — `docs/plans/`

Feature-level implementation plans, some active and many archived/superseded. See
[`plans/README.md`](plans/README.md) for the active-vs-archived index (and the plan-number collisions
the docs audit found). The active master is [18-project-completion.md](plans/18-project-completion.md);
the current test/KG-hardening plan is [19-test-and-kg-hardening.md](plans/19-test-and-kg-hardening.md).
Completed plans live under [`plans/archive/`](plans/archive/).

## Per-feature pipeline — `docs/features/`

The bespoke PROC-01 8-phase per-feature pipeline. **Complete and superseded for new work** by the vault
`.planning/units/` + `/plan-tree` flow — see [`features/README.md`](features/README.md). Kept as a
reference case study; `features/PROC-01/STATE.md` is the archived execution tracker.

## Other reference material under `docs/`

| Area | What it is |
|---|---|
| [`animation-style-guide.md`](animation-style-guide.md) | The "Nordic Tech" motion guide: when-to-animate, duration/easing tables, directive reference, reduced-motion contract. Current. |
| [`runbooks/`](runbooks/) | Operational runbooks: [`deploy.md`](runbooks/deploy.md) (canonical Railway deploy runbook — topology, env-var matrix, procedure, rollback, owner checklist; OPS-08), `merge-queue.md`. |
| [`audit/2026-04-08-gap-report.md`](audit/2026-04-08-gap-report.md) | Date-stamped gap snapshot that drove Plan 18; the most accurate of the older status docs. |
| [`concepts/home-v3/`](concepts/home-v3/) | Decision inputs for Home v3 (3 concepts, chosen mock, screenshots); referenced by ADR 001/002. |
| [`performance/performance-audit.md`](performance/performance-audit.md) | Core Web Vitals audit. |
| [`tech-stack.md`](tech-stack.md) | Older stack note; the authoritative stack lives in `CLAUDE.md` + ADRs. |
| [`skills/`](skills/) | Audit personas + UI/UX research notes; mostly tied to the (superseded) Plan 20 audit program. |

## Stale / archived docs

Some legacy trackers are kept for history but are no longer trustworthy for status. Each carries a
`SUPERSEDED` banner at the top pointing back to the canonical sources:

- `plans/00-master-overview.md` — original inception plan (claims "100% complete").
- `plans/20-issue-registry.md` — Jan-2026 audit registry (129 open / 0 fixed; many since fixed).
- `validation/` — the entire 2026-01-24 validation snapshot (summary + 18 area docs).

For current status always prefer `.planning/STATE.md` + `docs/project-plan/PROJECT_STATUS.md`, and the
work queue in `docs/project-plan/PRIORITIZED_BACKLOG.md`.

## Vault Knowledge graph (cross-link)

The Obsidian vault holds the ClimaSite **Knowledge graph** at `vault/Knowledge/climasite/`
(components, ADR decisions, milestones, risks, open questions) and the workflow scaffold under
`.planning/`. ADRs here are mirrored into that graph. When the in-repo docs and the graph disagree,
`.planning/STATE.md` is the tiebreaker for current state.
