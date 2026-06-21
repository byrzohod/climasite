<!--
PIPELINE STATE for this feature. The SessionStart hook (Wave 2) reads this to remind the agent
which phase is active and what the gate is. Update "Current phase" as you progress and tick each
phase's exit criterion. Do NOT mark a phase done until its artifact exists and its gate is met.
-->

# STATE — <FEAT-ID>: <short title>

- **Current phase:** 1 — Research
- **Branch:** <feature|fix>/<slug>  (created at Phase 5)
- **plan_status:** draft  (mirror of plan.md front-matter; only /verify-plan flips it)
- **Last updated:** <YYYY-MM-DD>

## Phase checklist (each phase is a gate — see docs/features/README.md)

- [ ] **1 · Research** — `research.md` complete; every affected-file path resolves; ≥1 happy/edge/error journey; constraints listed.
- [ ] **2 · Plan** — `plan.md` has a non-empty exit-criteria table + test matrix; ADR flagged if payment/auth/GDPR/migration. `plan_status: draft`.
- [ ] **3 · Verify-Plan** — `/verify-plan` (verifier agent) wrote `plan-verification.md` and flipped `plan_status: approved`. *Code edits to `src/` are blocked until this is checked.*
- [ ] **4 · Track** — backlog row added (full anatomy) + `test-design.md` maps every scenario → Scenario ID (automated|manual); ADR written if flagged.
- [ ] **5 · Implement** — code + colocated tests on the branch; expand-contract migration if any; no anti-patterns. `STATE.md` ticked as work lands.
- [ ] **6 · Test** — green CI incl. real-infra E2E + a11y (+ visual/perf where applicable) across light/dark × EN/BG/DE; coverage ≥ floor.
- [ ] **7 · Review** — `review.md` (+ `security-review.md` if auth/payment/GDPR) recorded; `/code-review` + reviewer agent ran with real findings triaged.
- [ ] **8 · Merge** — squash to `main` on green CI; apply the **DEV_WORKFLOW.md §5 doc-update table** in the same PR: `CHANGELOG.md [Unreleased]` bullet, `PROJECT_STATUS.md` / `PRIORITIZED_BACKLOG.md` / `CLAUDE.md` status, ADR if a decision was made.

## Log (newest first)

- <YYYY-MM-DD> — kicked off via `/feature-kickoff`; Phase 1 started.
