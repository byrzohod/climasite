---
name: skill-eval
description: The ACT side of the build-side self-evolution engine (Blueprint §K, ported from discovery's /workflow-evolve). Use this at the end of a unit/phase and on every /update-workflow, AFTER /skill-health has run, to turn accumulated lessons into concrete workflow improvements -- promote a recurring pattern to a rubric/anti-pattern, formalize a useful axis, refactor a skill the user keeps hand-editing, DRAFT a brand-new skill for a repeated task that has none, or adjust a misfiring hook threshold. Edits only auto-editable files (individual skill bodies, _methodology.md, EVOLUTION.md, individual Knowledge/ notes); everything high-blast-radius is propose-only. Calls the first-party skill-creator to draft new/changed skills into skills/_proposed/ (INERT, enforced by the skill-quarantine hook), grades each with /skill-verifier, and leaves promotion to a human. Logs every change to EVOLUTION.md (fixed schema, backfilled commit SHA). Respects the velocity cap (<=5 edits/cycle), the cool-off boundary, and the >5-edits/30d HALT.
---

# /skill-eval - the ACT side of the self-evolution engine

Turns accumulated build lessons into concrete workflow improvements. This is the **`*-eval` actor** in the detect-vs-act pair; its detector is `/skill-health`. Health REPORTS; eval ACTS. **`/skill-health` runs first, every time** -- "evolution off stale state is drift." This skill is the build-side port of discovery's `/workflow-evolve` (Blueprint §K), applied uniformly to skills, dynamically-generated skills, memory (propose-only), and the methodology log.

> The FULL-AUTO-vs-propose-only boundary and the self-evolution policy are declared **once** in `vault/AI/Agent Workflow.md` (§ Self-Evolution) and in the top-of-context LOCKED/SELF-EVOLUTION-BOUNDARY block. This skill **references** that boundary; it does not restate it (discovery's mistake was triple-maintaining it). When the two disagree, the principles doc wins -- flag the drift.

## Why the build side needs this (and why the caps matter MORE here)

The build template **copies into many projects**. A change to a skill body here propagates to every future project that runs `/project-setup`. That makes the blast radius *higher* than discovery's, so the velocity cap, cool-off, and HALT gate are not bureaucracy -- they are the brakes on a system that edits itself and ships those edits widely. A self-evolving system that **authors skills at runtime** is also exactly where the owner's trusted-source-only posture is most at risk (the reason GSD/Caveman were removed). Hence the quarantine: AI may DRAFT, only a human promotes.

## When to use

Invoke this skill:
- **End of a unit/phase** -- as the non-skippable loop `/skill-health → /skill-eval → write a decision-log entry → commit` (Blueprint §K.3).
- **Every `/update-workflow`** -- the monthly maintenance pass over plugins/skills/MCP/the workflow itself runs this as its evolution step.
- **On demand after a major external event** -- an Anthropic release, a new methodology lesson, a user-driven mode pivot.

Always **after** `/skill-health`. Never before.

## When NOT to use

- **`/skill-health` has not run this cycle** -- run it first. No exceptions; evolving off stale state is drift.
- **`/skill-health` flagged the HALT gate** (>5 EVOLUTION entries in the trailing 30 days, or velocity-cap breach) -- STOP, surface for human review, apply nothing.
- **No promotion candidates cleared their thresholds** -- that is a normal outcome. Write "no workflow edits this cycle" to the decision-log entry and stop. Do not invent edits to look productive.
- **The only warranted change is to a high-blast-radius file** (`CLAUDE.md.template`, `settings.json` hooks, CI templates, `Knowledge/_schema.md`, `_views/*`, or any auto-memory content) -- you may not edit these. Write a propose-only entry (Step 5) and stop.

## What this skill is NOT

- It **does not detect** -- it acts on `/skill-health`'s findings plus the `_methodology.md` lessons log. If it has no health report, it has no mandate. (Detect-vs-act separation, workflow §5.7 self-review optimism -- the actor must not also be the auditor.)
- It **does not promote a drafted skill into `skills/`.** It DRAFTS into `skills/_proposed/` (INERT) and stops. A human promotes. The `skill-quarantine` hook enforces this even if the model tries.
- It **does not edit** high-blast-radius files; it proposes.
- It **does not skip the EVOLUTION row.** Every applied edit gets a row before the cycle closes. The audit trail is non-negotiable.

## Ground truth: the substrate it runs against

| Artifact | Role | Editable? |
|----------|------|-----------|
| `vault/AI/project-template/_methodology.md` | append-only **build lessons log** -- the substrate promotion runs against ("this stack's CI flaked 3x", "verify-work keeps getting skipped") | **auto** (append/extend lessons + rubric) |
| `vault/AI/project-template/EVOLUTION.md` | append-only audit trail, **fixed schema** so health can count entries/30d | **auto** (append rows only) |
| `vault/AI/project-template/skills/<name>.md` | individual skill bodies | **auto** (refactor body) |
| `vault/AI/project-template/skills/_proposed/<name>.md` | quarantine for AI-drafted new/changed skills | **auto** (draft here; INERT) |
| `CLAUDE.md.template`, `settings.json` hooks, `templates/ci/*`, `Knowledge/_schema.md`, `_views/*`, all auto-memory | high-blast-radius | **propose-only** (cool-off) |
| `Goal.md`, user-owned vault contracts | user domain | **never** |

If `_methodology.md` or `EVOLUTION.md` do not yet exist (early in the rollout), **bootstrap them first**: create `_methodology.md` with a "Lessons Learned Log" + "Rubric / anti-patterns" section, and `EVOLUTION.md` with the fixed-schema header below. Log the bootstrap itself as the first EVOLUTION row.

## The promotion-criteria table (ported verbatim -- Blueprint §K.1)

A pattern is only promoted once it clears its threshold. Below threshold = leave it in the lessons log and re-check next cycle.

| Pattern observed (in `/skill-health` report + `_methodology.md` lessons) | Promotion action | Target file | Editable? |
|---|---|---|---|
| Same pattern in **≥3** lessons-log entries | Promote to a **rubric rule or anti-pattern** | `_methodology.md` | auto |
| A scoring/decision **axis empirically useful ≥2×** across cycles | **Formalize** it into the methodology rubric | `_methodology.md` | auto |
| A skill's output the **user hand-edited ≥2×** the same way | **Refactor that skill** to incorporate the edit pattern | `skills/<name>.md` (or DRAFT to `_proposed/` if the change is large/structural) | auto / draft |
| A **new repeated task with no skill** (seen ≥2×) | **DRAFT a new skill** via `skill-creator` | `skills/_proposed/<name>.md` | draft only |
| A **hook misfiring** -- fires too often (noise) or too rarely to do its job | **Adjust the threshold** | hook is in `settings.json`/`hooks/*` → **propose-only** (cool-off) | propose |
| A cross-cutting lesson belongs in **principles** | Edit `Agent Workflow.md` | **propose-only** (cool-off) | propose |

Thresholds are deliberate: a single occurrence is noise, two is a coincidence worth watching, three is a pattern worth encoding. Don't promote on n=1.

## Procedure

### Step 1 -- Confirm the health gate, then read learnings

1. **Confirm `/skill-health` ran this cycle and read its report.** If it did not run, stop and run it. If its report flags the **HALT gate** (>5 EVOLUTION entries/30d or a velocity-cap breach) or any BLOCKER your edit would touch, **STOP** -- surface for human review; apply nothing.
2. Read, in order:
   - `vault/AI/project-template/_methodology.md` -- the most recent "Lessons Learned Log" entries (the substrate).
   - The latest `/skill-health` report (stale/never-triggered/drift/velocity findings; `metrics` DORA-lite signal if present -- treat it as an *input signal*, never a KPI, to avoid Goodhart).
   - `vault/AI/project-template/EVOLUTION.md` -- recent rows, **so you do not repeat an edit already applied** and so you can count the trailing-30d total against the HALT gate yourself.

### Step 2 -- Identify promotion candidates

Run each lesson/finding against the promotion-criteria table. For every candidate, write down: the **pattern**, its **occurrence count** (with citations to the lessons/iterations/health-findings that evidence it), the **promotion action**, the **target file**, and whether the target is **auto / draft / propose-only**. Anything below threshold stays in the log -- note "re-check next cycle."

### Step 3 -- Apply auto-editable edits (within the velocity cap)

For each candidate whose target is **auto-editable** (`_methodology.md`, an existing `skills/<name>.md` body, `EVOLUTION.md`, an individual `Knowledge/` note), up to the **velocity cap of 5 edits this cycle**:

1. **Make the edit.** For a skill-body refactor, preserve the house format (frontmatter + pushy description, When to use / When NOT, numbered Steps, Guardrails, Reporting, See also). For a methodology promotion, add the rubric rule/anti-pattern with its citation.
2. **Append one EVOLUTION row** (Step 6 schema) -- before moving on. One row per structural change.
3. **Update affected dependencies** -- if `_methodology.md`'s rubric changed, note it in the skills that consume the rubric; if a skill's signature/name changed, update the skills list it appears in. Do not orphan a referenced skill.

If more than 5 edits are warranted, **apply the highest-leverage 5 and queue the rest** (record them in the decision-log entry + the EVOLUTION "Why" so next cycle picks them up). Velocity caps reduce drift; the build side's wide blast radius makes this stricter, not looser.

### Step 4 -- DRAFT new/changed skills safely (the highest-risk path -- A14)

When the candidate is "new repeated task with no skill" or "skill change too large to edit in place":

1. **Call the first-party `skill-creator`** (Skill tool, `skill: skill-creator`) to author the draft. Use the vetted authoring tool -- do not hand-roll skill scaffolding. Brief it with: what the skill should enable, when it should trigger (the user phrases/contexts), the expected output, and that the **output path is `vault/AI/project-template/skills/_proposed/<name>.md`** (create `_proposed/` if absent). When the candidate is a **description-triggering** improvement (a skill the user keeps invoking by hand because it isn't firing, or one that mis-fires), use skill-creator's `run_loop.py` -- the existing autoresearch-shaped loop **with a train/test holdout** -- as the description-optimizer: it iterates the description against the held-out trigger set so a gain is measured on unseen cases, not memorized. Its output still lands in `skills/_proposed/<name>.md` (INERT); `/skill-verifier` grades it and a human promotes (Step 4.2-4.5 below). Do not promote on the train-split score.
   - **`/metrics` (DORA-lite) and any `/metrics` signal are NEVER an autoresearch optimization target** -- feed them to `run_loop.py` (or any optimizer) only as *input signal*, never as the objective being maximized. Optimizing the metric directly is Goodhart: the number improves while the thing it proxied for does not.
2. **The draft lands INERT in `skills/_proposed/`.** The `skill-quarantine` hook (`hooks/skill-quarantine.sh`, wired in `templates/settings.json`) **blocks** writing a NEW top-level `skills/<name>.md`; new skills must go to `_proposed/`. Do not attempt to bypass it.
3. **Hard constraints on generated skills (enforce in the draft, verify in Step 5):** a generated skill **may NOT** add MCP servers, network calls, or Bash patterns outside the existing allowlist. It must declare a minimal/scoped `allowed-tools`. If the task genuinely needs new network/MCP/Bash surface, that is a **human decision** -- write a propose-only entry instead of drafting around it.
4. **Grade the draft with `/skill-verifier`.** Treat the draft as a high-stakes claim ("this skill is correct, scoped, triggers well, conforms to the house format"). Spawn the verifier to audit: frontmatter validity + description triggering quality, scope (no new MCP/network/unscoped Bash, minimal `allowed-tools`), house-format conformance, and that it does what it claims. Per-claim CONFIRMED / DISPUTED / UNVERIFIABLE.
   - **Any DISPUTED** → revise the draft (still in `_proposed/`) and re-grade. Do not promote.
   - **All CONFIRMED** → the draft is *promotion-ready*, but **stays in `_proposed/`**. A human promotes it (moves it to `skills/`). Record it in the decision-log as "awaiting human promotion."
5. **Log the draft** as an EVOLUTION row (target `skills/_proposed/<name>.md`, status "drafted -- INERT, awaiting human promotion after skill-verifier: <verdict>").

A drafted skill counts toward the velocity cap as one edit.

### Step 5 -- Surface propose-only (cool-off) items; never auto-apply them

For every candidate whose target is **high-blast-radius** -- `CLAUDE.md.template`, `settings.json` hooks (including a hook **threshold** adjustment), `templates/ci/*`, `Knowledge/_schema.md`, `_views/*`, or **any auto-memory content** -- do **not** edit. Instead write a **propose-only entry** to the cycle's decision-log (`Sessions/<date>.md` or the unit/phase decision log) under "Workflow proposals (cool-off, awaiting human)", each with:

- the exact file + section + the precise proposed change,
- the pattern + citation that justifies it,
- an **explicit expiry/decision-by date** (the discovery cool-off entry sat unresolved ~1 month -- do not repeat that; force a date).

Memory **content** edits stay propose-only because memory is user-owned (the boundary). A hook threshold that `/skill-health` shows is misfiring is a real, common candidate here -- propose the new threshold, never silently change the hook.

### Step 6 -- Log every applied edit to EVOLUTION.md (fixed schema)

`EVOLUTION.md` uses the **fixed schema** so `/skill-health` can mechanically count entries/30d. Never reformat it. One row/entry per structural change:

```markdown
## YYYY-MM-DD -- <one-line summary>

- **File**: <relative path>
- **Section**: <heading or line range>
- **Before**: <one-sentence summary>
- **After**: <one-sentence summary>
- **Why**: <pattern observed + citation to the lessons/iterations/health-findings that cleared its threshold>
- **Reversibility**: vault git commit `<sha>`  (write "pending commit" now; **backfill the SHA in Step 7**)
```

(If the project's `EVOLUTION.md` uses the table variant `| Date | File/Area | Before | After | Why | Reversibility (SHA) |`, match the existing format -- do not introduce a second format in the same file.)

### Step 7 -- Verify + backfill the commit SHA

Before closing the cycle:

- All auto edits are applied; **every** one has a matching EVOLUTION row.
- No edit touched a propose-only / never-edit file. (If one did, revert it and convert to a Step-5 proposal.)
- Every drafted skill is in `_proposed/`, INERT, with a `/skill-verifier` verdict recorded.
- No referenced skill orphaned; no broken wikilinks introduced.
- The trailing-30d EVOLUTION count is **≤5** after this cycle's rows. If this cycle pushed it over 5, you over-applied -- stop and surface.
- After the commit lands (via `/vault-save` or the project's commit step), **backfill the short commit SHA** into each new EVOLUTION row's Reversibility field, and stamp each propose-only item with its expiry date. (The commit is what makes every change revertible.)

## Guardrails (the brakes -- ranked)

- **Health gate (hard).** `/skill-health` runs first. If it flagged HALT (>5 entries/30d or velocity breach), apply nothing -- surface for human review.
- **Velocity cap: ≤5 structural edits/cycle.** A structural edit = a methodology rubric change, a skill-body refactor, a drafted skill, a new/removed/rewired Knowledge edge. Queue the rest. Body-text typo fixes don't count.
- **Cool-off = propose-only.** `CLAUDE.md.template`, `settings.json`/hooks (incl. thresholds), `templates/ci/*`, `Knowledge/_schema.md`, `_views/*`, **all auto-memory content** → Step 5 proposal with an explicit expiry. Never edited here.
- **Auto-editable only:** individual skill bodies, `_methodology.md`, `EVOLUTION.md`, individual `Knowledge/` notes.
- **Never:** `Goal.md` and user-owned contracts. Ever.
- **Dynamic-skill safety (A14):** AI DRAFTS via `skill-creator` into `_proposed/` (INERT, hook-enforced); generated skills add **no** MCP/network/unscoped-Bash and declare minimal `allowed-tools`; each passes `/skill-verifier`; **a human promotes.** This is the one rule that keeps a self-authoring system inside the trusted-source-only posture.
- **No edit without an EVOLUTION row.** The audit trail is non-negotiable; backfill the SHA.
- **Don't repeat an edit already in EVOLUTION.md.** Read it first (Step 1).
- **Don't promote on n=1.** Thresholds exist precisely so transient findings don't churn the template.

## Failure modes

- **Conflicting edits** -- two cycles propose contradictory changes to the same skill → halt that one candidate, surface for the user to choose.
- **Verifier keeps DISPUTING a draft** -- the task may not be skill-shaped, or the constraints (no MCP/network) make it infeasible; stop drafting, write a propose-only entry describing what the task would actually need.
- **Drift suspicion** -- `/skill-health` flagged drift/HALT → halt (per the health gate).
- **Skill-creator unavailable** -- do not hand-roll a skill into `_proposed/` as a workaround beyond a minimal stub; note the blocked draft in the decision-log and queue it.

## Reporting

Close with an explicit report (append to the cycle's decision-log entry; this is the "write a decision-log entry" step of the non-skippable loop):

```
/skill-eval report for <unit/phase | update-workflow @ YYYY-MM-DD>:

/skill-health (pre): <clean | N findings | HALT — stopped>
Candidates evaluated: <n>  (≥-threshold: <m>, below-threshold/re-check-next-cycle: <k>)

Applied (auto, within velocity cap <count>/5):
  - _methodology.md: promoted "<pattern>" → anti-pattern (seen in 3 lessons: …)
  - skills/verify-work.md: refactored Step 4 (user hand-edited 2×: …)

Drafted (INERT in _proposed/, awaiting human promotion):
  - skills/_proposed/<name>.md  — skill-verifier: ALL CONFIRMED  (no MCP/network/Bash added; allowed-tools: <…>)

Proposed (cool-off, awaiting human, decision-by <date>):
  - settings.json hook <name>: threshold X→Y (misfires N×/cycle per health)
  - Agent Workflow.md §…: <principle change>

EVOLUTION rows appended: <n>   (trailing-30d total now: <t>/5)
Queued (over velocity cap): <none | list>
SHA backfill: <pending commit | done: abc1234>
```

Never claim "evolved" without naming the actual files edited, the drafts placed in `_proposed/` with their verifier verdicts, and the EVOLUTION rows appended.

## See also

- `/skill-health` -- the REPORT-only detector that **runs first** and gates this skill (Blueprint §K, detect-vs-act)
- `/skill-verifier` -- grades every drafted skill before it is promotion-ready (Step 4); the sibling auditor for self-review optimism (workflow §5.7)
- `skill-creator` (first-party, `~/.claude/skills/skill-creator/`) -- the vetted authoring tool this skill calls to draft into `_proposed/`; its `run_loop.py` (autoresearch-shaped, train/test holdout) is the description-optimizer used in Step 4 -- output lands in `_proposed/`, `/skill-verifier` promotes
- `hooks/skill-quarantine.sh` + `templates/settings.json` -- enforce `_proposed/` quarantine (INERT until human promotion)
- `_methodology.md` -- the build lessons log this skill promotes from
- `EVOLUTION.md` -- the fixed-schema audit trail this skill appends to
- `/hygiene-sweep` -- the sibling de-bloat/accuracy pass that runs alongside this loop (Blueprint §K.6)
- `vault/AI/Agent Workflow.md` § Self-Evolution -- where the FULL-AUTO/propose-only boundary is declared **once** (this skill references it)
- `vault/AI/project-discovery/skills/workflow-evolve.md` + `vault/AI/project-discovery/EVOLUTION.md` -- the discovery engine this is ported from
- `vault/AI/Workflow Redesign 2026-06/Blueprint.md` §K -- the canonical spec
