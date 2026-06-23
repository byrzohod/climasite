---
name: review-orchestrate
description: Multi-ANGLE review orchestration on the short branch BEFORE the merge queue. Fans out independent angle reviewers in parallel -- correctness/quality (the 13 dimensions, §6), security, performance, architecture-fit, best-practices, SEO, accessibility -- consolidates ALL findings, resolves them, then re-reviews ONLY the fix diff. Hard cap: <=2 rounds, then a HUMAN decision (zero comments cannot stall a merge forever). Cloud /ultrareview + cross-vendor Codex are reserved for high-stakes units only. /code-review is the channel layer that runs UNDER this. Use this whenever the user asks to review a PR/branch/unit before merging, after a feature/phase/refactor is implemented and its tests + mutation pass, before entering the merge queue, or mentions multi-angle review, review fan-out, review rounds, who reviews what, or "is this ready to merge".
---

# /review-orchestrate - Multi-angle review on the short branch, before the queue

The review **orchestration** layer. Where `/code-review` is one *channel* (self -> `/review` -> `/ultrareview` -> Codex), this skill runs many independent **angles in parallel** over the same diff, consolidates everything, drives the fix loop, and **terminates deterministically** -- `<=2` rounds then a human decides. It runs on the **short-lived branch (<24h) BEFORE the merge queue**, so findings are resolved while the branch is cheap to change; the queue + full real-infra suite (§J.2) is the *gate*, this is the *human/agent judgment* that precedes it.

This is a **convergence loop with the same shape as `/plan-tree` and `/research-loop`** (§G/§H): fan out N blind, diverse reviewers -> consolidate ALL with a disagreements register -> resolve -> at most one more sweep -> explicit STOP. **Evidence-based, never debate, never majority-vote** (a single confident-wrong reviewer drops group accuracy 10-40%).

See `Agent Workflow.md` §6 (Code Review), §J.3 (review sequencing on trunk), Blueprint §J.3 / §6.

## When to use

Invoke this skill:
- **After a unit's tests + mutation pass, before it enters the merge queue** -- this is the standard pre-queue gate (the step right after `/mutation-check` and `/verify-work`).
- **After completing a feature, phase, or significant refactor** -- full multi-angle pass over that scope.
- **When the user asks** to review a PR/branch, asks "is this ready to merge", or asks which angles to run.
- **On the short branch, not on main** -- trunk-mode merges incomplete units behind flags; review happens before the queue, not after merge.

## When NOT to use

- **A single, narrow channel is all that's wanted** (just "run `/review`" or just the Codex sanity pass) -> invoke `/code-review` directly; it *is* the channel layer this skill orchestrates.
- **Trivial / throwaway change** (typo, doc fix, one-line refactor) -> self-review + one round of `/review` per §6.3 is enough; orchestrating 7 angles is overkill. (Same trivial-skip judgment as `/code-review` / skill-verifier.)
- **Tests or mutation haven't passed yet** -> run `/mutation-check` + `/verify-work` first. Review does not substitute for proving the tests are meaningful; a unit with surviving genuine mutants does not enter review.
- **No plan exists for the unit** -> the `no-spec-no-code` hook will have blocked the code anyway; go back to `/plan-tree`. Review against a ratified `unit-plan.md` / `DESIGN.md`, not against improvised code.
- **This is the actual merge gate** -> it is not. The server-side ruleset + merge queue + full real-infra suite (§J) is the gate. This skill precedes it.

## Read first (always)

The reviewers are graded against the unit's own contract, not against generic taste:
- **`.planning/units/<unit>/unit-plan.md`** -- acceptance criteria, the test plan, the verification plan. A reviewer checks the diff *against* this, not in a vacuum.
- **`.planning/design/DESIGN.md`** -- the architecture-fit angle scores conformance to the ratified design; deviations are findings.
- **`Knowledge/<Project>/`** -- prior ADRs (the diff must conform, not re-litigate), open risks (seed the security/architecture angles' attack list), conventions in `CLAUDE.md`.
- **The diff itself** -- `git diff main...HEAD` (full PR) or `git diff --staged` (pre-commit). Every reviewer reads the whole diff before scoring.

## The angles (fan out independently, in parallel)

Each angle is an **independent reviewer** with a read-only tool whitelist -- it spots and reports, it does not fix (the `developer` agent fixes; this preserves the author-vs-reviewer separation). Diversity across angles is the point; they do **not** see each other during the fan-out.

| Angle | Reviewer | What it owns |
|---|---|---|
| **Correctness / quality** | `reviewer` agent | The **13 review dimensions** -- single-sourced in `Agent Workflow.md` §6 / `agents/reviewer.md`; do NOT restate them here. Correctness, error handling, code quality, AI code smells, testing, data modeling, API design, observability, dependencies, documentation. |
| **Security** | `security` agent | OWASP Top 10 + agentic-AI angles (prompt-injection, tool-abuse, exfil, output validation). Deeper than the `reviewer`'s security dimension -- this is the full pass the `reviewer` *defers* to. Seeded by `Knowledge/` risks + `/threat-model` output. |
| **Performance** | `performance` agent | N+1s, hot-path cost, payload size, missing pagination/indexes, bundle/LCP/INP/CLS regressions against the **perf budget** (single-sourced in `/perf-budget`). |
| **Architecture-fit** | `plan-critic` (as reviewer) | Conformance to `DESIGN.md`: does the implementation match the ratified architecture and chosen patterns? Did it introduce a wall at 10x, a layering violation, an unowned coupling? Adversarial, evidence-cited. |
| **Best-practices** | thin angle prompt | Language/framework idioms, SOLID/DRY/KISS at the structural level, dead code, stale flags, copy-paste, over-abstraction -- the "would a senior on this stack wince" pass. (A thin reviewer prompt, read-only; not a heavyweight agent.) |
| **SEO** | `/seo-review` skill | Public-facing web only: meta/OG/canonical/JSON-LD/sitemap/robots/redirects. Skip for non-web / internal tools. |
| **Accessibility** | `/accessibility-audit` skill | Public-facing UI: WCAG 2.2 AA -- keyboard, contrast, ARIA, focus, reduced-motion, axe-core clean. Skip for non-UI. (The `reviewer`'s a11y dimension is the shallow flag; this is the deep pass when a11y matters.) |

**Angle selection (don't run all 7 blindly).** Use judgment, like `/code-review`'s dimension selection: always run correctness/quality + best-practices; run security on anything touching auth/data/payments/AI; performance on hot paths; architecture-fit on any non-trivial structural change; **SEO + accessibility only for public-facing web/UI**. A backend-only unit runs ~4 angles; a public marketing page runs all 7. Record which angles ran and why in the consolidated report.

## Orchestration

Adapt `orchestration/parallel-fanout.js` and pass it to the `Workflow` tool; the `orchestrator` agent owns the **sequential `Task`-subagent fallback** when Dynamic Workflows are unavailable (same prompts, same blind-generation + STOP logic, 4-6 at a time). The `pipeline.js` template is the right shape if you want each finding to flow finding -> adversarial-verify independently for high-stakes units.

- **Fan-out** -> adapt `parallel-fanout.js`: the `LENSES` become the **selected angles**; each angle agent reads the diff + `unit-plan.md` + `DESIGN.md` + `Knowledge/` and returns structured findings (file:line, severity, dimension/angle, concrete fix). Blind: no cross-talk during the fan-out.
- **Consolidate ALL** -> the consolidation agent dedupes overlaps (the same line flagged by `reviewer` and `security`), reconciles severity, and **emits a disagreements register** -- every point where angles conflicted (e.g. perf wants a cache, security flags the cache as a stale-data leak) plus the **evidence-based adjudication**. Never silently merge a conflict; never average.
- **Resolve** -> the `developer` agent addresses findings; the orchestrator routes each finding to the owner.
- **Re-review** -> a second fan-out over **only the fix diff** (not the whole PR), unless substantial new code landed.

**Ceilings (same as §G, non-negotiable):** one reviewer per selected angle (no redundant duplicate angles), **<=2 rounds**, **<=16 concurrent**, **one-level supervisor->worker nesting only**, a per-loop agent-call ceiling (abort on exceed). Every reviewer -- and every verification vote -- runs at **maximum reasoning effort (no downgrade, never Fast Mode for the review judgment itself)**. Quality bars are uniform across all projects; the gates never bend.

## Process

### Step 0 -- Preconditions

1. Confirm the unit's **tests + `/mutation-check` + `/verify-work` passed** (this skill is downstream of them). If not, stop and run those first.
2. Read the unit's `unit-plan.md`, `DESIGN.md`, and `Knowledge/<Project>/` (risks/ADRs/conventions).
3. Generate the diff: `git diff main...HEAD` (full) or `git diff --staged` (pre-commit).
4. **Select the angles** for this diff (see Angle selection). Record the selection + rationale.

### Step 1 -- Round 1: fan out the angles (blind, parallel)

5. Spawn the selected angle reviewers **in parallel**, each independent and read-only, each grounding every finding in `file:line` + the contract (`unit-plan.md` / `DESIGN.md`). SEO and accessibility run via their skills (`/seo-review`, `/accessibility-audit`) when selected.
6. Each reviewer categorizes findings by severity -- **critical** (blocks merge: breaks prod / security hole / breaks a public contract), **warning** (should fix: degrades quality/maintainability/perf or risks regression), **suggestion** (non-blocking). Don't inflate severities -- the signal degrades.

### Step 2 -- Consolidate ALL

7. The consolidation agent merges all angle outputs into one report: dedupe overlapping findings, reconcile severity (take the highest justified), and **emit the disagreements register** with an evidence-based adjudication for each conflict. **High-stakes units:** route each critical/warning finding through an adversarial `claim-verifier` pass (`pipeline.js` shape, default-to-refute) so a confident-wrong angle can't force a needless rewrite.

### Step 3 -- Resolve

8. The `developer` agent fixes findings in priority order (critical -> warning -> agreed suggestions). For each disagreement, apply the adjudicated resolution, not both sides. Findings explicitly deferred (e.g. tracked as a follow-up) get a ticket + rationale logged in the PR.

### Step 4 -- Round 2: re-review the fix diff only

9. Fan out the **affected** angles over **only the fix diff**. New criticals/warnings introduced by the fixes are in scope; do not re-litigate already-resolved findings.

### Step 5 -- Terminate: clean, or human decision

10. **Exit clean** if Round 2 returns no new critical/warning findings -> the unit is review-clear; proceed to the merge queue (§J.2).
11. **STOP at the round cap.** If findings remain after **2 rounds**, do **not** start a Round 3. Escalate to a **human decision** with a one-screen summary: the remaining findings, the disagreements register, and a recommendation (merge-with-follow-up-tickets vs hold). The human decides. **"Zero comments" or an angle that keeps nitpicking cannot stall a merge forever** -- this cap is the termination guarantee (§J.3 / A-2).

### Step 6 -- High-stakes escalation (only when warranted)

12. For **high-stakes units** (auth, payments, data migrations, security-sensitive, irreversible): ask the user to run cloud **`/ultrareview`** (the cloud *channel*, via `/code-review`; agent suggests, cannot launch), and add the cross-vendor **`/council`** pass -- the *vendor* layer (blind Claude + Codex -> evidence-based synthesis, see `skills/council.md`). **Codex is a READ-ONLY advisor; Claude is the sole orchestrator and applies any fix** -- the council never auto-applies a Codex suggestion. The council's own **<=2-rounds-then-human** cap folds under Step 5's; it is **opt-in per high-stakes unit, never per-merge** (Codex egresses prompt+diff to OpenAI; for a repo where that's unacceptable, degrade to Claude-only multi-run). All of this is **reserved for high-stakes units only**; running cloud/cross-vendor review on every merge burns trunk velocity and EUR spend for no signal (default in M§10).

### Step 7 -- Document

13. Record the outcome in the PR description: angles run (+ why those), findings per round, the disagreements register + adjudications, deferred-with-ticket items, whether `/ultrareview`/Codex ran, and the termination (clean at round N, or human decision X). Then proceed to the queue.

## Iteration & STOP

- **Round cap:** **<=2 rounds**, then a **human decision** -- the hard termination guarantee. No Round 3, ever, without the human explicitly asking.
- **Re-review scope:** Round 2 reviews only the fix diff (unless substantial new code landed).
- **Adjudication:** evidence-based, dissent preserved in the disagreements register -- never debate, never majority-vote, never average severities.
- **No-progress / churn guard:** if a fix introduces as many criticals as it resolves twice in a row, HALT and escalate -- the unit-plan or design may be wrong; re-plan, don't keep patching.
- **Call / concurrency cap:** abort on exceeding the §G ceilings (per-loop agent-call ceiling, <=16 concurrent, one-level nesting).
- **Order on trunk:** this runs on the short branch BEFORE the queue. It does not replace the queue's full real-infra suite -- a unit that is review-clear still has to pass the `merge_group` gate (§J.2).

## Outputs

- A consolidated review report (per round) with the **disagreements register** -- in the PR description and, for decisions worth keeping, summarized into `Knowledge/<Project>/` via `/kb-capture`.
- A clear **termination record**: review-clear at round N, or the human decision and its rationale.
- Resolved fix diff on the short branch, ready to enter the merge queue.

## See also

- `/code-review` -- the **channel** layer that runs *under* this skill (self -> `/review` -> `/ultrareview` -> Codex). This skill orchestrates **angles**; `/code-review` orchestrates **channels**. The 13 dimensions are single-sourced there + in §6.
- `/council` -- the **vendor** layer (blind Claude + Codex -> evidence-based synthesis); Step 6's cross-vendor pass for high-stakes units. Codex is a READ-ONLY advisor, Claude applies fixes; opt-in per unit, never per-merge. The diversity stack is channel (`/code-review`) -> angle (this skill) -> vendor (`/council`).
- `/mutation-check`, `/verify-work` -- run immediately **before** this skill; a unit does not enter review with surviving genuine mutants or without break-the-code + real-behavior evidence.
- `/security-review`, `/perf-budget`, `/accessibility-audit`, `/seo-review` -- the deep skills the security / performance / a11y / SEO angles invoke (single source of truth for their thresholds; CI reads the same).
- `/threat-model` -- its output seeds the security + architecture-fit angles' attack lists.
- `/plan-tree`, `/research-loop` -- the sibling convergence loops with the same fan-out -> consolidate-ALL -> adjudicate -> STOP shape and the same §G ceilings.
- `agents/reviewer.md` (correctness/quality, read-only), `agents/security.md`, `agents/performance.md`, `agents/plan-critic.md` (architecture-fit angle), `agents/claim-verifier.md` (high-stakes finding verification), `agents/developer.md` (fixes), `agents/orchestrator.md` (owns the fan-out + sequential fallback), `agents/verifier.md` (may audit whether the review actually checked the angles or skimmed).
- `orchestration/README.md`, `orchestration/parallel-fanout.js` (the fan-out this skill adapts), `orchestration/pipeline.js` (per-finding verify chain for high-stakes units).
- `templates/ci/{node,python,go,dotnet}.yml` -- the PR-stage + `merge_group` gates this skill precedes (§J.2).
- [[../Agent Workflow]] -- §6 (Code Review: channels, the 13 dimensions, the <=2-rounds-then-human cap), §J.2 (two-tier CI), §J.3 (trunk review sequencing). Blueprint §J.3 / §6 / §J are the design source.
