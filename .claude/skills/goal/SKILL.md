---
name: goal
description: Drive ONE long goal across many context resets by keeping the orchestrator THIN and running each unit in a FRESH disposable context, so the main context never fills. The driver loops: ensure a ratified plan (/plan-tree) + lean STATE.md exist, then per unit delegate to a subagent / Workflow (its own fresh context; only a compact result returns) -- or for unattended multi-hour runs chain `claude -p --resume` / `/loop` (fresh context per tick), or Cloud Routines (/schedule) when the machine will be off -- update STATE.md via /checkpoint and commit after each unit, and rely on the SessionStart hook re-injecting STATE.md so any clear/compact mid-goal self-heals. Use this whenever the user wants to run a big multi-hour / multi-day / multi-unit goal to completion, mentions running a goal across context resets / compaction / a long autonomous or overnight run / "keep going until it's done" / "don't let context fill up" / babysitting a long build, or asks how the orchestrator survives /clear, auto-compaction, or a resume mid-goal. NOT a planner (that is /plan-tree) and NOT a single-unit runner (that is /implement + /trunk-merge); this is the long-horizon driver that sequences them.
---

# /goal - Drive a long goal across context resets (thin orchestrator, fresh contexts)

The **long-horizon driver**. A big goal -- a whole phase, a migration, an overnight optimization, a project from ratified plan to merged finish -- runs longer than any single context window survives. The failure mode is always the same: the orchestrator does the work *in its own context*, that context fills, it auto-compacts (lossy) or the user `/clear`s, and the thread of the goal is lost. This skill's whole job is to make that failure mode impossible by keeping the **orchestrator thin** and pushing every unit of *work* into a **fresh, disposable context** whose only residue is a compact result + an updated `STATE.md`.

The core idea, stated once: **the orchestrator never accumulates.** It reads `STATE.md`, picks the next unit, delegates that unit to a child context that does the heavy lifting and dies, folds back a small result, checkpoints, commits, and loops. The main context stays near-empty no matter how long the goal runs -- so it almost never *needs* to compact, and on the rare clear/compact it self-heals from `STATE.md`. This is the same fan-out-and-keep-the-parent-lean principle as `/plan-tree` and `/research-loop` (§G), applied across *time* instead of across *angles*. See `Agent Workflow.md` §10 (subagents) + §10.4 (context management).

## When to use

Invoke this skill:
- **For any goal too big for one context** -- a full phase/wave, a multi-unit feature, a migration, a long refactor, an overnight `/autoresearch` sweep -- where you want it driven to completion, not babysat.
- **When the user says** "run this to completion", "keep going until it's done", "work through the whole plan", "don't let the context fill up", "run it overnight / unattended", or asks how the orchestrator survives `/clear` / auto-compaction / a `--resume`.
- **As the layer above the per-unit loop** -- this skill *sequences* `/plan-tree`'s units, calling `/implement` + `/trunk-merge` per unit; it is the loop those skills run *inside*.
- **Whenever a run will outlast a sitting** -- if you'd otherwise be re-priming the agent by hand every hour, this is the pattern that removes the hand.

## When NOT to use

- **You need a plan, not a driver** -> run `/plan-tree` first. This skill *requires* a ratified plan + `STATE.md`; it executes the plan, it does not invent it. No plan -> the `no-spec-no-code` hook blocks the code anyway.
- **A single unit, here and now, in this context** -> just run `/implement` then `/trunk-merge`. One unit fits one context; wrapping it in the long-horizon driver is overkill (same trivial-skip judgment as the other orchestrators).
- **A one-shot question or a trivial edit** -> answer / edit directly. The driver is for *sequenced multi-unit* work.
- **Pure unattended single-metric optimization with a runner + one number** -> that *is* `/autoresearch` (it owns its own keep-or-revert loop + crash recovery). Use `/goal` to *launch and supervise* an autoresearch run as one unit of a larger goal, not to replace it.
- **The work isn't decomposable into checkpointable units** -> if there's no natural unit boundary to checkpoint + commit at, there's nothing for this driver to loop over; reshape it in `/plan-tree` first.

## Read first (always)

Durable state is the whole game here -- the driver trusts files, not its own memory (§10.4):
- **`.planning/STATE.md`** -- the **lean resume contract**: active unit, approved `unit-plan.md` path, what just landed, and a single explicit **Next action**. This is what the `state-prime.sh` SessionStart hook re-injects on every `startup | resume | clear | compact`, and what `no-spec-no-code` reads for the active unit. Keep it a contract, not a dump -- it is injected every session start.
- **`.planning/` tree** -- `phases/ -> waves/ -> units/` (coarse) and the active unit's `unit-plan.md` (the contract each unit is built + verified against).
- **`Knowledge/<Project>/`** -- decisions/risks/conventions; each finished unit writes back via `/kb-capture`, so a fresh child context can be briefed cold.
- **`git log` / working tree** -- the ground truth of what actually landed. If `STATE.md` and git disagree, **git wins** -- re-verify and re-checkpoint before continuing (the PreCompact breadcrumb says exactly this).

## Orchestration

The driver is a **loop over units**, and each unit's work runs in a **separate context** so the parent never grows. Three delegation modes, picked by how attended the run is:

| Mode | Fresh context per | Use when | Mechanism |
|---|---|---|---|
| **Subagent / Workflow** | unit (and per angle/finding inside it) | attended, interactive -- you're at the machine | Delegate the unit to a role agent or a `Workflow`; the child does the heavy reads/edits/tests in **its own context** and returns a **compact result** (ask for "report under 200 words"). The parent stays lean. This is the default. |
| **`/loop` (self-paced or interval)** | tick | unattended for hours, machine stays on | `/loop` re-runs the driver prompt on a **fresh context each tick** -- the cleanest "context never fills" guarantee, because every tick *starts* empty and rehydrates from `STATE.md` via the SessionStart hook. |
| **`claude -p --resume` chain** | invocation | unattended, scripted, multi-hour headless | Chain non-interactive invocations; `--resume` + the SessionStart hook re-prime each one from `STATE.md`. Each `claude -p` is a fresh process/context that does one slice and exits. |
| **Cloud Routines (`/schedule`)** | scheduled run | the machine will be **off** (overnight, away) | A cron routine runs the driver in the cloud on schedule; same `STATE.md` contract, no local machine required. |

All four share one contract: **the unit of work runs in a context that starts fresh, rehydrates from `STATE.md`, does one slice, checkpoints, and exits.** Concurrency stays within the §G ceilings (<=16 concurrent, **one-level supervisor->worker nesting only** -- the driver is the supervisor; it does not spawn drivers that spawn drivers). Every agent runs at **maximum reasoning effort (no downgrade)**; quality bars are uniform -- the long-horizon driver does not get a "fast mode" that the in-session work wouldn't.

## Process

### Step 0 -- Preconditions (plan + state exist)

1. Confirm a **ratified plan** exists: `.planning/design/DESIGN.md` ratified and the `.planning/` coarse tree present. If not -> stop and run `/plan-tree` (and `/design-doc` under it). The driver executes a plan; it never improvises one.
2. Confirm **`.planning/STATE.md`** exists and is current: it names the **active unit**, its approved `unit-plan.md` path, and a single explicit **Next action**. If it's missing or stale, run `/checkpoint` to (re)write it from git + the active unit-plan *before* looping.
3. Confirm the **self-heal wiring** is in place: `settings.json` has the `state-prime.sh` SessionStart hook and the `pre-compact-checkpoint.sh` PreCompact hook (both ship in the template). These are what make a mid-goal `/clear`/compact recoverable -- the driver depends on them.

### Step 1 -- Pick the next unit (thin)

4. Read **`STATE.md`** for the active unit + Next action. Read *only* what the next unit needs (its `unit-plan.md`, the relevant `Knowledge/` slice) -- the orchestrator must stay lean; bulk reading belongs in the child context, not here.
5. If the active unit has no approved `unit-plan.md` yet, hand back to `/plan-tree` Gate 3 to produce one (its own fan-out), then return. Do not write unit code without the plan -- the hook blocks it regardless.

### Step 2 -- Delegate the unit to a FRESH context

6. Run the unit in a **separate context** using the mode from the Orchestration table (subagent/Workflow when attended; `/loop` or `claude -p --resume` when unattended; `/schedule` if the machine will be off). Brief the child **like a colleague who walked in cold** (§10.3): goal, the `unit-plan.md` path, what's already decided/ruled out (from `Knowledge/`), and the exact compact-result shape you want back.
7. Inside that child context, the unit runs its normal closer: `/implement` -> tests -> `/mutation-check` -> `/verify-work` -> `/review-orchestrate` -> `/trunk-merge`. All of that heavy work lives in the **child's** context window, not the driver's. The child returns a **compact result** (landed? merged? blocked? + the one-line outcome), nothing more.

### Step 3 -- Fold back: checkpoint + commit

8. On the unit's compact result, run **`/checkpoint`**: update `STATE.md` to the *new* active unit + the *new* Next action, and record what just landed. Keep it lean -- prune finished detail; `STATE.md` is a forward-looking contract, not a log.
9. The unit's own merge already produced its commit via `/trunk-merge` (conventional commit -> merge queue). Confirm it landed (git is truth); `/kb-capture` has written decisions/risks back to `Knowledge/`. The driver itself adds no extra code commit -- it commits **state**, not source.

### Step 4 -- Loop (the orchestrator stays empty)

10. Return to Step 1 for the next unit. Because every unit's work happened in a *disposable* context and only `STATE.md` + a one-line result came back, the driver's context has barely grown -- it can loop over many units without filling. **This is the mechanism that makes a long goal survive: the parent never accumulates, so it rarely needs to compact.**

### Step 5 -- Self-heal on clear / compact (the safety net)

11. If the driver context *does* get cleared or compacted mid-goal (user `/clear`, auto-compaction at the limit, or a `--resume`/cloud run starting cold), the **`state-prime.sh` SessionStart hook re-injects `STATE.md`** into the next context automatically -- no manual re-priming. The PreCompact hook has already appended a "re-verify Next action against git before continuing" breadcrumb.
12. On any fresh/rehydrated context: **re-verify `STATE.md` against `git log` + the active unit-plan first** (git wins on conflict), run `/checkpoint` if anything is stale, then resume the loop at Step 1. Trust the file, but verify it against ground truth before acting on it.

### Step 6 -- Terminate (goal done, or HALT)

13. **Exit clean** when `STATE.md`'s Next action is "goal complete": the last unit merged, `Knowledge/` captured, working tree clean. Report the units landed + final state, and tear down any `/loop` / routine (`/schedule` delete) so it isn't left running.
14. **HALT to a human** on a STOP/HALT trigger (below) -- don't grind. Write the blocker + current state into `STATE.md` via `/checkpoint` first, so the human (or the next run) resumes from an accurate contract, not a guess.

## Iteration & STOP

- **No-progress HALT (C-6):** if the same unit fails to land **twice** (e.g., review can't converge in its <=2 rounds, the merge queue keeps ejecting, verification keeps failing), HALT and escalate -- the unit-plan or design is likely wrong. Re-plan via `/plan-tree`; do not re-drive a wedged unit forever.
- **Budget cap:** a long unattended run gets an explicit ceiling -- max units, max wall-clock, and (for `claude -p` / `/loop` / cloud) a **cost cap**. On exceed -> checkpoint + HALT, report spend, ask the human. Uncapped overnight loops are how a goal quietly burns the budget (M§10 / `/cost-check`).
- **Drift guard:** if `STATE.md` and git disagree on what landed, **stop and reconcile** before the next unit -- never let the driver act on a stale contract. git is truth; re-checkpoint to match.
- **Concurrency / nesting cap:** the driver is a **one-level supervisor** -- it delegates units to children; children do not spawn further drivers. Stay within <=16 concurrent (§G). Abort the loop on exceeding the ceilings.
- **The honest limit:** the agent **cannot `/clear` *itself* mid-turn** -- there is no tool to wipe the current context from inside a running turn. So the "fresh context per unit" guarantee comes from the *delegation boundary* (a child context, a `/loop` tick, a new `claude -p --resume`, a scheduled run) -- not from the orchestrator self-clearing. The payoff of keeping the orchestrator thin is that it **rarely needs to clear**: it accumulates so little per unit that it can drive a long goal without hitting the limit, and on the rare time it does, the SessionStart hook self-heals it. Don't claim the parent "resets between units" -- claim that the *work* runs in fresh contexts and the parent stays small enough not to need a reset.

## Outputs

- A **current `.planning/STATE.md`** at every unit boundary -- the lean resume contract the SessionStart hook injects (active unit, approved plan path, last landed, Next action).
- **Units merged onto `main`** -- each via its own `/trunk-merge` (conventional commits feeding `/release` later), with `Knowledge/<Project>/` captured per unit.
- A **termination record**: goal complete (units landed + final state) or a HALT with the blocker + accurate state for the human / next run.
- **No bloat in the driver context** -- the proof the pattern worked is that the orchestrator drove the whole goal without its own context filling.

## See also

- `/plan-tree` -- the **prerequisite**: produces the ratified `DESIGN.md`, the coarse `phases/->waves/->units/` tree, and the per-unit `unit-plan.md` + initial `STATE.md` this driver loops over. `/goal` executes that plan; it does not author it.
- `/checkpoint` -- the agent-driven **`STATE.md` updater** the driver calls after every unit (and what the `state-prime.sh` / `pre-compact-checkpoint.sh` hooks point the next context at). The primary self-heal mechanism; the hooks are the safety net.
- `/implement` -- the per-unit **execution** workflow the driver delegates into a fresh context; it runs the unit's code + verification, returning a compact result.
- `/trunk-merge` -- the per-unit **closer** (short branch -> review -> merge queue -> delete) that each delegated unit ends with; it produces the unit's commit. `/goal` sequences many of these.
- `/loop` -- **fresh context per tick** for unattended multi-hour runs while the machine stays on; the cleanest "context never fills" mode (every tick starts empty and rehydrates from `STATE.md`).
- `/schedule` -- **Cloud Routines** (cron) for when the local machine will be **off**; runs the driver in the cloud against the same `STATE.md` contract.
- `/autoresearch` -- the self-contained single-metric optimizer; `/goal` *launches and supervises* an autoresearch run as one unit, it does not replace that loop's own keep-or-revert + crash recovery.
- `/cost-check` -- the budget/cost cap for long unattended runs (M§10); consult before launching an overnight `/loop` / `claude -p` / cloud-routine drive.
- `/research-loop`, `/review-orchestrate` -- sibling convergence loops with the same fan-out-keep-the-parent-lean shape and the same §G ceilings; `/goal` applies that shape across *time* (units) rather than across *angles*.
- `hooks/state-prime.sh` (SessionStart re-injector), `hooks/pre-compact-checkpoint.sh` (PreCompact breadcrumb), `templates/settings.json` (wires both) -- the self-heal plumbing the driver relies on.
- [[../Agent Workflow]] -- §10 (subagents -- delegate aggressively, orchestrate at the top), §10.2 (background + scheduled wake-ups: `/loop`, `ScheduleWakeup`), §10.3 (briefing subagents cold), §10.4 (context management: external state in files, handoff to `STATE.md`, resume next session), §G (shared loop shape + ceilings).
