---
name: plan-tree
description: Multi-level spec-driven planning. Clarifying questions (deduped into ONE batch) -> ratified design doc FIRST -> decompose into phases/waves/units (coarse, each tagged NORMAL or OPTIMIZATION) -> a REQUIRED per-unit unit-plan.md before that unit's code: a NORMAL unit's carries a test plan + verification plan + DoR + acceptance criteria; an OPTIMIZATION unit's instead declares an autoresearch contract (EDIT_FILE/RUNNER_CMD/METRIC+DIRECTION/MIN_IMPROVEMENT_TO_KEEP/CONSTRAINT_CMD/HOLDOUT_CMD/caps) whose verification plan is held-out validation on every kept candidate + /code-review of the final diff. Use this whenever the user starts a feature/refactor/project, asks to plan, break down, scope, decompose, or sequence work, or before writing code for any non-trivial unit. The no-spec-no-code hook BLOCKS code in src/** until that unit's unit-plan.md is approved. N=3 blind proposers, evidence-based adjudication, no-progress HALT.
---

# /plan-tree - Multi-level spec-driven planning

The planning spine. Plans top-down but in detail only just-in-time: the whole system is bounded by a ratified design doc, decomposed coarsely into phases -> waves -> units, and **only the active unit gets a detailed, approved `unit-plan.md`** before its code is written. This avoids premature deep planning of branches that will change while still making every unit's contract explicit.

**Hard rule (mechanical, not advisory):** never write code for a unit without an approved `unit-plan.md` that contains its test plan and verification plan. The `no-spec-no-code` hook enforces this — it blocks `Write`/`Edit` on `src/**` until the active unit's plan exists and is approved. There is no "I'll plan after." See `Agent Workflow.md` §2.2 / §H.

Planning is a **convergence loop with the same shape as `/research-loop`** (§G): N blind proposers -> consolidate ALL -> evidence-based adjudication -> at most one more round -> explicit STOP. **Never debate, never majority-vote.**

## When to use

Invoke this skill:
- **At the start of a project** (after `/design-doc`) and **at the start of each major phase**
- **Before implementing any non-trivial unit** — `unit-plan.md` is the gate the hook checks
- **When the user asks** to plan, scope, break down, decompose, sequence, or estimate work
- **When a unit's design assumptions changed** mid-build — re-plan the active unit, don't improvise

## When NOT to use

- **Trivial / throwaway changes** (one-file fix, a typo, a spike): the documented escape hatch is a one-time `ALLOW_EXPLORATORY=1` for the hook (mirrors the skill-verifier "skip for trivial" precedent). Use sparingly — it gets abused if it becomes the default.
- **Whole-system architecture is not yet ratified** — run `/design-doc` first; this skill plans *against* a ratified `DESIGN.md`, it does not invent the architecture.
- **You only need external knowledge, not a plan** — run `/research-loop` first; its REPORT feeds the proposers.
- **Detailed-planning the entire tree up front** — don't. Gate 2 is deliberately coarse; deep-plan only the active unit.

## Read first (always)

`Knowledge/<Project>/` is both input and output. Before planning, the orchestrator reads:
- **`Questions/`** — open questions seed Gate 0's clarifying batch; **decided questions are excluded** from re-asking.
- **Risks** — seed the `plan-critic`'s attack list (Gate 1 and Gate 3).
- **ADRs / decided decisions** — proposers must conform, not re-litigate.
- **`.planning/design/DESIGN.md`** — the ratified convergence target every plan must fit.

After each unit and each phase, run `/kb-capture` to write decisions/components/questions/risks back. The loop closes.

## Orchestration

Adapt the bundled templates and pass to the `Workflow` tool; the `orchestrator` agent owns the **sequential `Task`-subagent fallback** when Dynamic Workflows are unavailable (same prompts, same blind-generation + STOP logic).

- **Gate 0 dedup + fan-answers** — `orchestrator` collects questions, dedupes, batches to the user.
- **Gate 1 (architecture)** — adapt `orchestration/judge-panel.js` (N=3 `architect`s, architecture rubric, 10% human tie-break). Driven by `/design-doc`.
- **Gate 3 (per-unit plan)** — adapt `orchestration/parallel-fanout.js` (N=3 blind `planner`s -> consolidate ALL with a disagreements register) + `orchestration/judge-panel.js` (adjudicate the consolidated plan vs the `plan-critic`'s attacks).

**Ceilings (same as §G, non-negotiable):** N=3 proposers, **≤2 consolidation rounds**, **≤16 concurrent**, **one-level nesting only**, per-loop agent-call ceiling (abort on exceed). `plan-critic` / verification votes run at **maximum reasoning effort (no downgrade)**.

## Process

### Gate 0 — Clarifying questions (§1.1 batch protocol, deduped)

1. **Collect** open questions from `Knowledge/<Project>/Questions/` **plus** each proposer's questions.
2. **Deduplicate into ONE numbered batch** to the user — never 3× overlapping question sets, never drip-fed one at a time (the Req #4 fix). Follow §1.1: 5-15 questions per round, **2-3 rounds maximum**.
3. **Fan the answers back to all proposers** before they plan. Record newly-decided answers as decisions in `Knowledge/` (so they're excluded next time).
4. Exit when no proposer has a blocking question, or after round 3.

### Gate 1 — Whole-system Design Doc FIRST (`/design-doc`)

5. If `.planning/design/DESIGN.md` is not yet ratified, run `/design-doc`: **N=3 `architect`s propose competing architectures blind**; `plan-critic` attacks each (10× scale? requirement-change? data-flow holes?); `/threat-model` + `/data-classify` run here (design-time, tiered).
6. **Judge adjudicates by explicit rubric, citing evidence; preserves dissent — does NOT average.** If the top-2 rubric totals are within **10%**, escalate to a human tie-break (do not auto-pick).
7. Output: ratified `DESIGN.md` (problem · goals/non-goals · whole-system architecture + chosen patterns · alternatives · data model · test+verify strategy · risks). ADRs spawn into `Knowledge/`. This is the convergence target that prevents architectural dead-ends.

### Gate 2 — Decompose (coarse)

8. Decompose the ratified design into `.planning/phases/ -> waves/ -> units/`. **Each level is coarse** — a title, scope, and dependency order only.
9. **Do NOT detail-plan the whole tree.** Identify the single **active unit**; everything downstream stays coarse until it becomes active. **Tag each unit's type** (coarse is fine — it sets which `unit-plan.md` shape Gate 3 uses), and record the active unit **and its type** in `STATE.md`:
   - **NORMAL** (default) — builds or changes behavior. Its `unit-plan.md` carries a **test plan + verification plan** (step 10 below).
   - **OPTIMIZATION** — moves a single measurable metric on already-correct code (latency, bundle size, eval score, cost, …) with no behavior change. Its `unit-plan.md` carries an **autoresearch contract** instead of a test plan, and its verification plan is the **held-out validation + `/code-review`** (step 10-OPT below). Use this only when the unit is a genuine single-metric optimization that fits `/autoresearch`'s "when to use"; a unit that *changes* behavior is NORMAL even if it also moves a metric.

### Gate 3 — Per-unit detailed plan (`unit-plan.md`) — REQUIRED before that unit's code

10. **(NORMAL unit) Fan out N=3 blind `planner`s** over the active unit (adapt `parallel-fanout.js`). They cannot see each other; diversity is the point. Each produces a draft `unit-plan.md` containing **all** of:
    - **Architecture-fit** — how this unit conforms to `DESIGN.md` (cite the section).
    - **Design patterns** chosen, and **interfaces** (signatures, contracts, data shapes).
    - **The test plan** (run `/test-plan`): levels · exact assertions · edge-case matrix · real-infra setup · **mutation target** · and **explicitly how false positives are prevented**.
    - **The verification plan** — how `/verify-work` will confirm real behavior through **real DB / real UI**, asserting persistence by reading it back through the app.
    - **DoR satisfied** — ratified design link · test+verify plan present · threat + data-classification checked · **a11y + perf budget noted (shifted left, not deferred to CI)** · acceptance criteria. (Mirrors §9.4 DoD on the entry side.)
    - **Acceptance criteria** — observable, testable conditions for "done."
10-OPT. **(OPTIMIZATION unit) Same N=3 blind fan-out, but the draft declares an autoresearch contract instead of a test plan.** The code is already correct (a NORMAL unit, with its own test+verify plan, made it so); this unit only *moves a metric* without changing behavior, so its plan binds `/autoresearch` rather than re-specifying tests. Each draft `unit-plan.md` contains **all** of:
    - **Architecture-fit** — how this unit conforms to `DESIGN.md` (cite the section); confirm it is in-scope optimization, not a behavior change in disguise.
    - **The autoresearch contract** (the `.autoresearch/program.md` this unit will run — see `/autoresearch`):
      - `EDIT_FILE` — the single file (or small set) the loop may modify, and nothing else.
      - `RUNNER_CMD` — the one command that prints the metric to stdout (with `METRIC_PATTERN`).
      - `METRIC` + `DIRECTION` — the metric name and lower|higher-is-better.
      - `MIN_IMPROVEMENT_TO_KEEP` — the keep/no-op noise floor (a delta below this counts as no improvement).
      - `CONSTRAINT_CMD` — a command that **must stay green every kept run** (the test/lint/type suite proving behavior is unchanged); a run that reds it is a discard, never a keep. This is how "no behavior change" is mechanically enforced.
      - `HOLDOUT_CMD` — a **held-out** validation the loop never optimizes against, run on **every kept candidate** (not just at the end) to catch overfitting to `RUNNER_CMD`; a candidate whose holdout delta doesn't hold (same direction, ≥ `MIN_IMPROVEMENT_TO_KEEP`) is a discard, and a final holdout summary gates the merge.
      - **Caps** — `MAX_RUNS` · `WALL_CLOCK_LIMIT` · cost cap · `DO NOT modify` / `DO NOT install` · resource soft caps.
    - **DoR satisfied** — ratified design link · baseline metric captured · the underlying code already has a passing test+verify plan (the optimization rides on correct code) · threat + data-classification re-checked if `EDIT_FILE` is sensitive · contract complete.
    - **The verification plan (this is the OPTIMIZATION substitute for the test plan)** — **not** `/verify-work` over real DB/UI, but: (a) `CONSTRAINT_CMD` green on every kept candidate; (b) `HOLDOUT_CMD` validates **every kept candidate** that the win generalizes (held-out delta ≥ `MIN_IMPROVEMENT_TO_KEEP`, same direction) — a `RUNNER_CMD` win that vanishes on holdout is **discarded, not kept** — plus a final holdout summary on the shipped diff; (c) **`/code-review` of the final autoresearch-branch diff vs `main`** (the loop's own per-experiment commits are unreviewed); (d) **`/security-review`** if `EDIT_FILE` touches auth/validation/anything user-facing.
    - **Acceptance criteria** — baseline → target metric (delta + direction), `CONSTRAINT_CMD` green, holdout-validated on every kept candidate, `/code-review` clean.
    > **High-stakes optimization** (auth/payments/migrations, or an irreversible/high-leverage `EDIT_FILE`): add **`/council`** (Codex as a READ-ONLY blind reviewer of the final diff) to the verification plan, alongside `/code-review`. Opt-in, never automatic; Claude owns the verdict — the council never auto-applies a Codex suggestion. Never launch it from a hook.
11. **Consolidate ALL** drafts into one plan with a **disagreements register** — every point where planners conflicted plus the evidence-based adjudication. Never silently merge a conflict.

> **Cross-vendor option (light-touch, high-stakes units only):** for a **high-stakes unit** (auth, payments, migrations, irreversible/high-leverage), you *may* add Codex via `/council` as one more **blind plan proposer** alongside the N=3 `planner`s, then fold its draft into this same consolidation + disagreements register. **Opt-in, never automatic; skip it for coarse phase/wave decomposition (Gate 2) and for routine units.** Codex is a **READ-ONLY advisor** (it egresses the prompt + read code to OpenAI) and **Claude owns the chosen plan** — the council never auto-applies a Codex suggestion. Never launch it from a hook.
12. **`plan-critic` attacks** the consolidated plan (default-to-refute), seeded by `Knowledge/` risks. A judge adjudicates by rubric, citing evidence — not debate, not vote. Run at maximum reasoning effort (no downgrade).
13. **Iterate ≤2 consolidation rounds.** Each round addresses only the critic's surviving objections.
14. **Approval.** The user (or the design-doc sign-off authority) approves. Record the approved `unit-plan.md` path and the active unit in `STATE.md` — this is exactly what `no-spec-no-code` reads.
15. **Then, and only then, implement.** For a **NORMAL** unit, write the code against the approved test+verify plan. For an **OPTIMIZATION** unit, "implement" = **run `/autoresearch` against the approved contract** (it consumes the contract as its `program.md`), then execute the verification plan (`CONSTRAINT_CMD` green · `HOLDOUT_CMD` generalizes · `/code-review` of the final diff · `/council` if high-stakes) before merging. After the unit lands, run `/kb-capture`; promote the next coarse unit to active and return to Gate 3.

## Iteration & STOP

- **Round cap:** ≤2 consolidation rounds at Gate 1 and Gate 3.
- **Tie-break (Gate 1):** top-2 within 10% -> human picks. Never auto-select a near-tie.
- **No-progress HALT (C-6):** if all 3 plans are broken by the `plan-critic` **every round** (no plan survives after the round cap), **HALT and escalate to a human** — do not ship a known-broken plan or quietly lower the bar.
- **Call / concurrency cap:** abort the loop on exceeding the §G ceilings (per-loop agent-call ceiling, ≤16 concurrent, one-level nesting). Quality bars are uniform across all projects — the gates never bend.
- **Never skip Gate 3 — for any unit type.** Tests passing != plan existed. The hook will block the code regardless; a "fast" path that skips the plan is the exact drift this spine exists to prevent. For an **OPTIMIZATION** unit the **autoresearch contract IS the spec** the hook gates on — no contract, no loop. There is no "just optimize it" path; `/autoresearch` runs only against an approved `unit-plan.md`.

## Outputs

- `.planning/design/DESIGN.md` (ratified, Gate 1) — and ADRs into `Knowledge/<Project>/`.
- `.planning/phases/ -> waves/ -> units/` coarse tree (Gate 2), **each unit tagged NORMAL or OPTIMIZATION**.
- `.planning/units/<unit>/unit-plan.md` for the active unit (Gate 3) + its disagreements register — a NORMAL unit's carries a test+verify plan; an OPTIMIZATION unit's carries the autoresearch contract (becomes its `.autoresearch/program.md`).
- `STATE.md` updated: active unit + its type + approved plan path (read by `no-spec-no-code`).
- `Knowledge/<Project>/` updated via `/kb-capture` after each unit/phase.

## See also

- `/design-doc` — Gate 1; produces the ratified `DESIGN.md` this skill plans against.
- `/research-loop` — run before planning when external knowledge is needed; its REPORT feeds proposers (§G).
- `/test-plan` — authored inside every `unit-plan.md` (Gate 3); designs the edge-case matrix + mutation target.
- `/verify-work` — the verification plan in each NORMAL unit-plan declares how this skill will confirm real behavior.
- `/autoresearch` — the engine an **OPTIMIZATION** unit binds at Gate 3; its `program.md` is the unit's autoresearch contract, run only after the `unit-plan.md` is approved.
- `/kb-capture` — writes decisions/questions/risks back to `Knowledge/<Project>/` after each unit/phase.
- `/council` — optional cross-vendor vendor-diversity axis at Gate 3 for high-stakes units only (Codex as a read-only blind proposer; Claude owns the plan).
- `orchestration/README.md`, `parallel-fanout.js`, `judge-panel.js` — the templates Gates 1 & 3 adapt.
- `Agent Workflow.md` §2.2 (two-level planning), §2.3 (DoR), §H (this loop's spec), §G (shared loop shape & ceilings).
