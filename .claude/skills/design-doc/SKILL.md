---
name: design-doc
description: Gate 1 of two-level planning -- produce a RATIFIED whole-system Design Doc / RFC at .planning/design/DESIGN.md BEFORE any code. N=3 architects propose blind, plan-critic attacks each, /threat-model + /data-classify run here at design time, a judge-panel adjudicates by rubric (preserves dissent, 10%-tie escalates to human). Use this whenever a project or a major phase is starting, the user mentions a design doc, RFC, architecture, "how should we build this", "design first", whole-system design, choosing an architecture/pattern, or before /plan-tree and before any implementation. This is the convergence target that prevents architectural dead-ends; ADRs spawn from it into Knowledge/<Project>/.
---

# /design-doc - Ratify the whole-system architecture before any code

The first gate of two-level planning (Blueprint §H, Agent Workflow §2.2). Produces ONE ratified whole-system Design Doc / RFC at `.planning/design/DESIGN.md` that every later phase/wave/unit must conform to. It exists so multi-agent planning has a **convergence target** and the project never hits an architectural wall mid-build (Req #6). Multi-agent and **evidence-based, blind, adversarially verified -- never debate, never average** (same shape as `/research-loop`).

## When to use

Invoke this skill:
- **At the start of a project** -- before `/plan-tree`, before any code (the no-spec-no-code hook will block implementation without it)
- **At the start of each major phase** that changes the system shape (new subsystem, new integration, a pivot)
- **When a load-bearing architectural decision is in play** -- a choice with real alternatives and a high cost of being wrong
- **After `/research-loop`** when external knowledge was needed first -- you build on its CONFIRMED claims and treat DISPUTED ones as disputed

## When NOT to use

- **A single unit's internals** -- that's `/plan-tree`'s per-unit `unit-plan.md` (Gate 3), not a whole-system RFC
- **A choice with no real alternatives** -- if there's one obvious way, write one ADR via `/kb-capture` and skip the panel; don't theatre-stage a 3-architect bake-off for a foregone conclusion
- **Pure refactors / behavior-preserving restructuring** -- `/refactor` (the system shape isn't changing)
- **Exploratory spikes** -- spike first to retire the unknown, THEN run `/design-doc` on what you learned
- **Hobby-tier trivial scaffolds** where the design is the template default -- record the one decision and move on (the gate scales to tier; the rigor doesn't get skipped, the ceremony does)

## Inputs (read BEFORE proposing)

1. **`Knowledge/<Project>/` first, always** (Blueprint §F, §1.1 dedup rule):
   - `Decisions/` accepted ADRs -> **constraints** the design must honor (do not re-litigate)
   - `Questions/` with `status: open` -> seed the **Gate-0 clarifying batch** (decided ones are **excluded** from re-asking)
   - `Risks/` open/accepted -> seed the **plan-critic's attack list** (every risk is a vector the design must address)
2. **`.planning/research/REPORT.md`** if `/research-loop` ran -- consolidated claims + its disagreements register (disputed != fact).
3. **Problem statement + goals / non-goals + tier** (hobby/MVP/production) + the user's constraints and unfair-advantage/domain context.

If problem/goals or a hard constraint is missing, **ASK before proposing** -- do not guess the requirement.

## Process

### Step 1: Gate 0 -- one clarifying-question batch (then STOP for the human)

Read `Knowledge/<Project>/Questions/`. The N=3 architects each surface assumptions/questions during proposal; the orchestrator **dedupes all proposers' questions into ONE batch** to the user (§1.1 batch protocol: 5-15 questions, numbered, answered together, 2-3 rounds max -- never drip-fed). **Decided questions are excluded.** Wait for answers before adjudicating -- a design ratified on a guessed requirement is an architectural dead-end with a rubber stamp.

### Step 2: N=3 architects propose BLIND (adapt judge-panel.js -> the Propose phase)

Spawn **N=3 `architect` agents in parallel**, each with a different angle for diversity (`simplest thing that works` / `risk-first` / `long-horizon extensibility`). They **cannot see each other's proposals** -- blind generation is the point; three hedged near-copies give the judge nothing. Each returns a complete candidate DESIGN.md as **text** (architects are read-only; they never write the file). Each justifies every choice against the rubric with **cited evidence** (a benchmark, a prior ADR, a vendor doc, a named failure mode) -- not assertion or "best practice."

#### Cross-vendor architect (optional, `/council`)

Whole-system design is the **highest-value `/council` stage** -- a wrong architecture is the most expensive thing to get wrong. On a **high-leverage / whole-system** design, add Codex as **one more blind architect**: the orchestrator runs `/council` so Codex (OpenAI, **READ-ONLY ADVISOR** -- cannot write/push/sync) produces its own blind design proposal in parallel with the N=3 Claude architects. Its proposal is **captured as text** and fed into the existing judge-panel as an additional candidate, scored against the same rubric; if it survives the critic and places, its **dissent is preserved** like any runner-up. The council **never auto-applies** Codex's design -- Claude remains the sole orchestrator and the only agent that writes DESIGN.md; the judge + human ratification gate (Steps 5-6) is unchanged. **Gate:** opt-in, high-stakes/high-leverage designs only -- not every phase. **Egress:** Codex sends the prompt + read context to OpenAI; for a repo where egress is unacceptable, degrade to Claude-only (raise N) instead. (Contract: `skills/council.md`.)

### Step 3: plan-critic attacks each proposal (default-to-refute)

Spawn the **`plan-critic`** against every candidate. It loads the attack seed from `Risks/` + open `Questions/` and tries to BREAK each design across the standard vectors: **wall at 10x? what when requirement X changes? data-flow holes? hidden coupling? failure modes? boundary fit? test/verify gaps? migration/rollback? cost/operability?** Verdict per design: BROKEN / SURVIVES-WITH-FIXES / SURVIVES. The critic feeds the judge; it does not pick a winner and does not revise designs.

### Step 4: design-time security + data passes run HERE (tiered)

This is the **shift-left** point -- run before code exists, not after:
- **`/threat-model`** -- STRIDE + agentic-AI (prompt-injection / tool-abuse / exfil) against each candidate's trust boundaries. On by default at **MVP+** or whenever the system is AI/network-facing.
- **`/data-classify`** -- build/refresh `DATA.md` (public/internal/PII/PHI/**biometric**, retention, mask-before-model, RAG/vector-PII rule, DPIA trigger). On if the project touches personal data.

Their findings are inputs to the judge (a design that fails the threat model or mishandles a data class loses on the risk/data axes) and become risks fed back to `Knowledge/<Project>/Risks/`.

### Step 5: judge-panel adjudicates by rubric -- preserve dissent, NEVER average

Run the **Judge phase** of the adapted `judge-panel.js`. Each candidate is scored **independently** 0-10 per criterion, adversarially, with a cited reason -- judges **do not debate and do not vote**; a single confident-wrong agent drops group accuracy 10-40%, so evidence-scored-against-a-rubric is the guard.

Rubric (adapt the `RUBRIC` constant): **Fit to requirements · Simplicity/KISS · Scalability headroom (10x) · Risk/failure surface · Reversibility** -- and weigh, within those, the design-level security posture (Step 4), data-classification fit, testability/verifiability (no-mock real-infra), and a11y + perf budgets **noted at design time**. Quality bars are **uniform across all projects** -- the rubric does not soften by tier.

The panel returns `{ winner, runnersUp, needsHumanTiebreak, dissent }`. **Preserve dissent** (the runners-up scores + verdicts go into DESIGN.md's "Dissent" note); graft good ideas from runners-up into the winner during synthesis.

### Step 6: tie-break + human ratification (the gate -- a hard STOP)

- **Tie within 10%** (`needsHumanTiebreak === true`, i.e. `(top1 - top2)/top1 < 0.10`) -> **the human picks.** Do NOT auto-select; present both finalists, their rubric tables, and the critic's verdicts.
- **Clear winner** -> still present DESIGN.md to the user for **ratification**. Final tech-stack / database / auth / hosting sign-off is the **user's prerogative** (global rule: user decides, agent decides implementation details). Design ratification is an **explicit human decision-right**, not an agent auto-approve.

Ratification is the gate. Until the user accepts, there is no DESIGN.md and no code.

### Step 7: write DESIGN.md + spawn ADRs into Knowledge/

Only after ratification: **write `.planning/design/DESIGN.md`** (the orchestrator writes the file; architects never do) with these sections:

```
# DESIGN: <project / phase> -- RATIFIED <date>
## Problem & scope
## Goals / Non-goals          (non-goals are explicit -- what is OUT)
## Whole-system architecture  (components & boundaries · data flow · deployment topology · cross-cutting: auth/error/observability/config)
## Chosen patterns & principles   (each named + justified vs rubric, incl. ones rejected and why)
## Alternatives considered     (the runner-up architectures + the trade-off that lost them)
## Data model                  (entities + storage choice + why; link DATA.md)
## Test + verify strategy      (how the SYSTEM is made testable/verifiable through real DB/UI -- the no-mock contract; what proves it works)
## Risks                       (open risks incl. threat-model + data-classify findings; each -> R-NNN)
## Dissent                     (runner-up rubric scores + verdicts; preserved, not erased)
## Open questions remaining    (anything Gate 0 left open -> feeds /plan-tree)
```

Then **spawn ADRs** into `Knowledge/<Project>/Decisions/` for each load-bearing choice (delegate to **`/kb-capture` / knowledge-curator**): one ADR per decision, AI-Sonar format verbatim (**Status / Date / Context / Decision / Alternatives Considered / Consequences**), append-only, supersede-don't-edit, with frontmatter `[[wikilink]]` edges (`decides_on` the question it answers, `affects` the components, `part_of` the project hub). Flip resolved `Questions/` to `status: resolved`; record threat-model/data-classify findings as `Risks/`.

## Adapting judge-panel.js

`orchestration/judge-panel.js` is the template (`orchestration/README.md` for the hard rules). Copy it, then:
- `N = 3` (design uses 3, not research's 4-6 -- deeper, fewer).
- `ANGLES = ['simplest thing that works', 'risk-first', 'long-horizon / extensibility']`.
- `RUBRIC = ['fit to requirements', 'simplicity / KISS', 'scalability headroom (10x)', 'risk / failure surface', 'reversibility']`.
- Proposer prompt: the `architect` agent brief (read `Knowledge/<Project>/` + research REPORT first; one whole-system design; evidence-cited; self-adversarial weaknesses section; surface questions, don't answer them).
- **Insert the critic + threat-model/data-classify between Propose and Judge** (Steps 3-4) and feed their findings into the judge prompt so weaknesses are scored, not hidden.
- Keep the 10%-tie -> `needsHumanTiebreak` and the dissent-preserving return shape unchanged.
- Pass the adapted script to the **`Workflow` tool**. **Sequential fallback** (Workflow unavailable): the `orchestrator` agent spawns 3 `architect` Tasks blind -> 1 `plan-critic` Task per proposal -> `/threat-model` + `/data-classify` -> 3 judge Tasks (one per proposal, at **maximum reasoning effort (no downgrade)**) -> ranks, applies the 10% tie rule, routes to the human. Same STOP logic, enforced by the orchestrator reading results between steps.

## Iteration & STOP (Blueprint §G/§H ceilings -- pinned)

- **Rounds: =2 consolidation rounds.** If after a round every design is BROKEN by the critic, the orchestrator briefs the architects with the landed attacks and runs ONE more round.
- **No-progress HALT:** if **all 3 designs are BROKEN by the critic in every round** (2 consecutive), **STOP and escalate to the human** -- the problem is under-specified or the requirements conflict; do not lower the bar to manufacture a winner.
- **Tie -> human** (Step 6), never auto-pick.
- **Concurrency / agent-call ceiling:** =16 concurrent (Workflow cap), **one-level supervisor->worker nesting only**, a per-loop agent-call ceiling. **Verification/judging votes run at maximum reasoning effort (no downgrade)**, same as every other agent.
- **NEVER implement without an approved DESIGN.md.** This is the gate -- it is mechanical (the no-spec-no-code hook) and non-negotiable.

## Reporting

After the gate, report explicitly:

```
/design-doc report for <project/phase>:

Gate 0:        <N questions batched, answered | none needed>
Architects:    3 blind proposals (angles: simplest / risk-first / long-horizon)
Critic:        #1 SURVIVES-WITH-FIXES, #2 BROKEN, #3 SURVIVES
Threat-model:  <ran | tier-skipped> -- <N findings -> Risks/>
Data-classify: <ran | n/a> -- DATA.md <created/updated>
Judge:         rubric scores [#1 38, #3 35, #2 28]; tie within 10%? <no | YES -> human picked #_>
Ratified by:   <user> on <date>
Written:       .planning/design/DESIGN.md
ADRs spawned:  Decisions/ADR-00N-... (x M), Questions resolved (x K), Risks recorded (x J)
Dissent kept:  runners-up #3 (35), #2 (28) recorded in DESIGN.md
Rounds used:   1 of 2   |   HALT: <no>
Feeds:         /plan-tree (M open questions, J risks)
```

Never claim "design done" without a human-ratified DESIGN.md, preserved dissent, and the ADRs spawned.

## See also
- `orchestration/judge-panel.js` -- the template adapted here (N proposers -> evidence-based judge -> winner + dissent + 10% tie); `orchestration/README.md` -- the hard rules (blind · evidence-based · consolidate-ALL · adversarial · explicit STOP)
- `agents/architect.md` -- the N=3 blind proposers · `agents/plan-critic.md` -- the adversary · `agents/orchestrator.md` -- runs the loop + fallback
- `/threat-model`, `/data-classify` -- the design-time security + data passes (Step 4)
- `/council` (`skills/council.md`) -- adds Codex as one more blind architect on high-leverage whole-system designs (Step 2); read-only advisor, dissent preserved, never auto-applied -- the highest-value council stage
- `/plan-tree` -- Gate 2/3, consumes the ratified DESIGN.md (the convergence target) and decomposes into phases->waves->units
- `/research-loop` -- runs first when external knowledge is needed; feeds CONFIRMED claims in
- `/kb-capture`, `agents/knowledge-curator.md` -- spawn ADRs from the design into `Knowledge/<Project>/Decisions/`; the §F feedback loop
- [[../Agent Workflow]] -- §2.2 (two-level planning), §0.1 (the one architectural rule), §7.1/§7.2 (design-time threat-model + data governance), reference/domain-concerns.md §34 (untrusted-content security)
- `vault/AI/Workflow Redesign 2026-06/Blueprint.md` -- §H Gate 1, §G (ceilings), §C (skills), §D (agents)
