---
name: verify-plan
description: Phase 3 gate of the per-feature pipeline (PROC-01). Independently verifies a feature's plan.md against the real working tree before any code is written, then stamps plan-verification.md and flips plan_status to approved only if it holds up. Use after writing plan.md and before implementing — "verify the plan", "/verify-plan <FEAT-ID>", or whenever plan_status is still draft and you're about to touch src/.
---

# /verify-plan — independent plan verification (Phase 3 gate)

A plan written by the same agent that will implement it is a plan that can quietly drift from reality.
This gate puts an **independent, read-only `verifier` agent** between the plan and the keyboard: it
re-checks the plan against the actual working tree, and `plan_status` only flips to `approved` if the
plan survives. Wave 2 hooks block edits to `src/` for this feature while `plan_status != approved`.

**Separation of duties:** the verifier agent's `tools:` whitelist excludes Write/Edit, so its normal
path can't touch files — its job is to read and report, and this skill (in the main thread) records the
verdict, keeping the judgment independent from the implementation. (The verifier still has `Bash`, so
the read-only property here is by convention, not absolute; the Wave 2 `plan_status` PreToolUse hooks —
which block `src/` edits until approved — are the hard enforcement.) The recorded verdict must reflect
what the verifier actually found, not this thread's optimism.

## Inputs
- **FEAT-ID** — the feature folder under `docs/features/<FEAT-ID>/` (must contain `research.md` + `plan.md`).

## Procedure

1. **Pre-flight.** Confirm `docs/features/<FEAT-ID>/research.md` and `plan.md` exist and `plan_status`
   is currently `draft`. If already `approved`, stop and report (re-run only if the plan changed).

2. **Spawn the read-only `verifier` agent** (Task tool, `subagent_type: verifier`). Give it the FEAT-ID
   and have it independently confirm each check below **against the working tree** (not against the
   plan's own claims). It must return a structured verdict: per-check `CONFIRMED | DISPUTED |
   UNVERIFIABLE` + an overall `PASS | FAIL`, with file:line evidence for every dispute.

   **Verification checklist (the verifier runs this):**
   - **V1 — Paths resolve.** Every path in `research.md §2 (Affected files)` exists on disk
     (`test -e`). Any `MISSING` → DISPUTED.
   - **V2 — Assumptions grounded.** Each `plan.md §2` assumption cites a real `research.md:line`, and
     the cited fact is actually true in the code. A claim that the code contradicts → DISPUTED.
   - **V3 — Exit-criteria table non-empty** and each row is an observable, checkable outcome (not a
     restatement of "implement X").
   - **V4 — Test matrix non-empty** and names concrete, real test locations (no "add tests"); every
     `automated` Scenario maps to a plausible test path; integration/E2E declare real infra (no mocks).
   - **V5 — ADR flag correct.** If `research.md §4` checked Payment / Auth / GDPR / Migration, then
     `plan.md §5` must mark ADR **required** (and name the `docs/adr/NNNN-*.md`, four-digit per
     `docs/adr/README.md`). A missing-but-required ADR → DISPUTED. (The other §4 constraints —
     i18n / theme / a11y / z-index — are *not* ADR triggers; they're verified as test-matrix gates in
     V4/V6.)
   - **V6 — Scenario coverage.** Every happy/edge/error journey in `research.md §5` is represented in
     the plan's exit-criteria + test matrix (nothing silently dropped).
   - **V7 — No contradictions** between plan and the real tree (e.g. plan targets a controller method
     that doesn't exist, or a layer the feature shouldn't touch).

3. **Record the verdict** by writing `docs/features/<FEAT-ID>/plan-verification.md` (the orchestrator
   writes this — the verifier can't). Include: date, verifier verdict per check (V1–V7) with evidence,
   and the overall result. Use this shape:
   ```markdown
   # Plan verification — <FEAT-ID>
   - **Date:** <YYYY-MM-DD>   - **Verifier:** verifier agent (read-only)   - **Result:** PASS | FAIL
   | Check | Verdict | Evidence |
   |-------|---------|----------|
   | V1 paths resolve | CONFIRMED | … |
   | … | | |
   **Disputes to resolve:** <none | list>
   ```

4. **Gate the flip:**
   - **PASS (all checks CONFIRMED, no DISPUTED):** flip `plan.md` front-matter `plan_status: draft →
     approved`, set `verified_by: verifier` and `verified_date: <today>`; tick `STATE.md` Phase 3 and
     set **plan_status: approved** + **Current phase: 4 — Track**. Report that code edits to `src/` are
     now unblocked.
   - **FAIL (any DISPUTED):** leave `plan_status: draft`. Report each dispute and what must change in
     `research.md`/`plan.md`. The user fixes the plan and re-runs `/verify-plan`. **Never** flip to
     approved with open disputes — that defeats the gate.

## Guardrails
- The verdict comes from the verifier agent, not from this thread's optimism. If the verifier says
  FAIL, the plan is not approved — full stop.
- `UNVERIFIABLE` checks (e.g. needs a running stack) are not a pass; note them for `/verify-work` later
  and treat a checklist that's all-UNVERIFIABLE as FAIL (nothing was actually confirmed).
- Re-run this gate whenever `plan.md` materially changes after approval (flip back to `draft` first).
