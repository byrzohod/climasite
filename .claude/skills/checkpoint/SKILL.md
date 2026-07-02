---
name: checkpoint
description: Refresh the resume contract at .planning/STATE.md so a /clear or auto-compaction self-heals with zero manual re-priming. Rewrites STATE.md per templates/STATE.md.template -- current position, approved plan path, last completed, the single Next action, blockers, key pointers, and any in-flight loop's checkpoint location. Use this whenever you finish a unit/phase or cross a wave boundary, BEFORE a planned /clear or compaction, when context is getting full, after the PreCompact hook drops a "re-verify" NOTE in STATE.md, when the Stop hook warns "code changed but .planning/STATE.md not updated", or when handing off mid-task. Cheap; run often.
---

# /checkpoint - Refresh the resume contract (`.planning/STATE.md`)

Rewrites `.planning/STATE.md` into a lean, current **resume contract**: the one durable handoff a *cold* context needs to continue. This is the explicit, agent-driven half of the context self-healing loop -- the hooks are the safety net, this is the primary mechanism.

**Why this matters (the self-heal).** The **SessionStart hook** (`hooks/state-prime.sh`) prints `STATE.md` to stdout, and Claude Code injects a SessionStart hook's stdout as context on `startup | resume | clear | compact`. So the moment you `/clear`, the moment auto-compaction fires, or you `--resume` tomorrow, the *next* context automatically re-reads `STATE.md` -- **no manual re-priming, no "where were we"**. That free recovery is only as good as the file. `/checkpoint` keeps the file good. The **PreCompact hook** (`hooks/pre-compact-checkpoint.sh`) can't serialize your working memory, so it just appends a `> NOTE: ... re-verify` breadcrumb -- the actual content is your job, here. Because the payoff is automatic and the cost is one short rewrite, **run it often**; a stale `STATE.md` silently poisons every future session it's injected into.

External state lives in files, not in the agent's memory alone (Agent Workflow §10.4). `STATE.md` is the single most load-bearing of those files.

## When to use

Invoke this skill:
- **At every unit boundary** -- right after `/verify-work` + `/kb-capture`, before moving to the next unit. (The **Stop hook** warns "code changed but `.planning/STATE.md` not updated" -- this is the fix.)
- **At every phase / wave boundary** -- the position, active unit, and approved-plan pointer all change.
- **BEFORE a planned `/clear`** -- always checkpoint first, or you throw away the working memory the cleared context will need.
- **When context is getting full** (before voluntary compaction, or when you feel the window tightening) -- capture now so the inevitable compaction self-heals cleanly.
- **After auto-compaction left a `> NOTE:` line in `STATE.md`** -- re-verify the contract against git + the active `unit-plan.md` and rewrite it fresh (the NOTE means "this file may be stale").
- **When handing off mid-task** -- end of session, or passing to a future you. The bold **Next action** is the handoff.
- **When the active unit, approved plan, branch, or a blocker changes** mid-unit -- a small refresh keeps the contract honest.

## When NOT to use

- **No `.planning/STATE.md` and no project scaffolding** -- there's nothing to refresh. `/project-setup` creates `STATE.md` from the template; `/plan-tree` populates the active unit + plan path. Run those first.
- **Nothing changed since the last checkpoint** -- if position, Next action, and blockers are all still accurate, don't churn the file (and don't bump the timestamp for nothing). Re-checkpoint when something actually moved.
- **As a substitute for `/kb-capture`** -- they are different layers. `STATE.md` is *ephemeral working position* ("where am I, what's next"); the **Knowledge graph** (`Knowledge/<Project>/`) is the *durable decision/component/risk record*. A decision worth keeping goes to `/kb-capture`, not into `STATE.md`. Do both at a unit boundary; don't fold one into the other.
- **As a place to dump detail** -- `STATE.md` is injected every single session start. Bloating it taxes every future context. Pointers, not prose (see Anti-patterns).

## Read first (always)

- **`templates/STATE.md.template`** -- the canonical shape. The headings below (`Current position`, `Approved plan`, `Last completed`, `▶ Next action`, `Blockers`, `Key decisions & pointers`, `Open loops / checkpoints`) are defined there; keep `STATE.md` structurally identical so the hooks and `/kb-health`'s STATE-drift check keep working.
- **The existing `.planning/STATE.md`** -- you are refreshing it, not writing blind. Note any `> NOTE: ... compaction` breadcrumb the PreCompact hook appended; treat the file as suspect until re-verified.
- **Ground truth to reconcile against** -- `git status` / `git log` (what actually landed), the active `.planning/units/<u-id>/unit-plan.md` (the contract you're executing), and `STATE.md`'s current pointers (do they still resolve?). The contract must match reality, not intention.

## Process

### Step 0 -- Preconditions
1. Confirm `.planning/STATE.md` exists. If not and the project is scaffolded, copy `templates/STATE.md.template` to `.planning/STATE.md`; if the project isn't scaffolded at all, stop -- this is a `/project-setup` job.

### Step 1 -- Reconcile against ground truth
2. Read the current `STATE.md`. Read `git status` + recent `git log`, and the active `unit-plan.md`. Resolve every existing pointer in `STATE.md` -- a path that 404s, an "active unit" that's already merged, or a plan that no longer exists is **drift** to fix now (this is exactly what `/kb-health` flags). If a PreCompact `> NOTE:` is present, assume staleness and re-derive every field from ground truth.

### Step 2 -- Rewrite the contract (lean)
3. Rewrite `STATE.md` to the template shape, filling each section from reconciled reality:
   - **Current position** -- phase · wave · **active unit** + its type (`NORMAL | OPTIMIZATION`).
   - **Approved plan** -- the exact `.planning/units/<u-id>/unit-plan.md` path **and whether it's approved** (the `no-spec-no-code` hook reads this path; an unapproved/missing plan means the next context can't write code -- say so). Plus the ratified **Design target** (`DESIGN.md`).
   - **Last completed** -- the last unit/phase that truly landed (merged + verified), with its merge/flag ref if any. This is the solid ground a cold context can trust.
   - **▶ Next action** -- see Step 3.
   - **Blockers** -- what's stopping Next action + what unblocks it; `none` if clear. Never leave a silent blocker out.
   - **Key decisions & pointers** -- pointers only: `Knowledge/<Project>/` (Dataview over frontmatter `[[wikilink]]` edges), the `.planning/` root, and any *load-bearing* file the next context must open (a migration, a fixture, a threat-model). Link recent decisions; don't restate them (they live in the graph).
   - **Open loops / checkpoints** -- if a multi-agent loop is mid-run (`/research-loop`, `/autoresearch`, `/review-orchestrate`, `/council`), record **where its own checkpoint/artifacts live** so the next context **resumes that loop from its checkpoint, not from scratch**; `none` if idle.

### Step 3 -- Nail the single Next action
4. Write **exactly one** bold `▶ Next action`: one imperative, concrete step a cold context can start with no other memory -- the exact skill/command + the precise target (file + section, endpoint, test). "Continue the feature" is useless; "Implement `validateToken()` in `src/auth/token.ts` per unit-plan §interfaces -- red tests already in `token.test.ts`; then `/verify-work`" is a resume contract. The SessionStart hook resumes from this line; if it's vague, every recovered session starts confused. One action only -- a list is a plan, not a next step.

### Step 4 -- Trim, timestamp, verify-cold
5. Cut anything that isn't needed to resume -- `STATE.md` is injected every session start; every stale or verbose line taxes all future contexts. Remove any superseded `> NOTE:` breadcrumb once you've re-verified. Update the **Last checkpoint** timestamp.
6. **Cold-read test**: reread the file as if you just woke with zero history. Can you execute **▶ Next action** from `STATE.md` + its pointers alone? If not, the contract is incomplete -- fix it before stopping. This is the whole point: prove the self-heal would actually work.

### Step 5 -- (optional) Commit
7. If the user has asked for commits this session, `STATE.md` is a normal tracked file -- commit it with the unit's work (e.g. `chore: checkpoint state`). Otherwise leave it staged/dirty; the SessionStart hook reads the working-tree file regardless of commit status. Never block a checkpoint on a commit.

## Anti-patterns

- **Dumping instead of pointing.** `STATE.md` is not a log, a diary, or a design doc -- it's injected verbatim every session start. Long narratives, pasted code, or full decision rationale belong in the Knowledge graph or the unit-plan; `STATE.md` links to them.
- **Appending a new dated block on top of the old.** `/checkpoint` **REWRITES** the file in place -- it never stacks a "2026-07-02 update" section above last week's. Per-PR history goes to `CHANGELOG.md`, not `STATE.md`. (The SessionStart hook now injects only `STATE.md`'s **live top** -- it stops at the first `## Recently done` / `## ✅ Done` / `## ▶ RESUME HERE` section -- so any history left below is dropped from resume anyway; don't create it.)
- **More than one Next action.** Multiple "next" steps mean none is *the* next step. Pick one; the rest are the plan (in `unit-plan.md` / `ROADMAP.md`).
- **Letting it drift.** A `STATE.md` pointing at a closed unit or a missing plan is worse than none -- it confidently misdirects every injected session. Reconcile against git every time.
- **Folding the Knowledge graph into it.** Decisions/risks/components are durable graph nodes (`/kb-capture`), not ephemeral state. Keep the layers separate.
- **Skipping it before `/clear`.** `/clear` discards working memory; if `STATE.md` is stale at that moment, the recovery injects stale state. Checkpoint, *then* clear.

## See also

- **`templates/STATE.md.template`** -- the canonical resume-contract shape this skill writes to (single source of truth for the sections).
- **`hooks/state-prime.sh`** (SessionStart) -- injects `STATE.md` on `startup|resume|clear|compact`; the mechanism that makes a good checkpoint self-heal automatically. **Injects only the live top** (stops at the first archived / `## Recently done` / `## ✅ Done` / `## ▶ RESUME HERE` section) so stray history never re-enters a fresh context -- a backstop; keeping `STATE.md` lean here is still the primary mechanism.
- **`hooks/pre-compact-checkpoint.sh`** (PreCompact) -- the safety-net breadcrumb that tells the next context to re-verify; `/checkpoint` is the primary mechanism it backstops.
- **`hooks/workflow-check.sh`** (Stop) -- warns "code changed but `.planning/STATE.md` not updated" -- the nudge to run this.
- `/goal` -- the **long-horizon driver** that loops over units and calls this skill after each one to refresh `STATE.md` (this template's `skills/goal.md`). Note: a *different* skill also named `/goal` lives in the project-discovery workflow (`AI/project-discovery/skills/goal.md`) and only sets the discovery north-star goal -- they share the name across the two workflows but are not the same skill.
- `/plan-tree` -- produces the active unit + approved `unit-plan.md` path that `STATE.md` records; re-plan there when the active unit changes.
- `/kb-capture` -- the *durable* sibling: decisions/components/risks -> `Knowledge/<Project>/`. Run alongside `/checkpoint` at unit/phase boundaries; they cover different layers.
- `/kb-health` -- its STATE-drift check audits whether `STATE.md`'s active-unit / approved-plan pointers still resolve; a failing check means: run `/checkpoint`.
- `/verify-work` -- run immediately before `/checkpoint` at a unit boundary; "Last completed" should reflect a unit that actually passed verification.
- `/research-loop`, `/autoresearch`, `/review-orchestrate`, `/council` -- the in-flight loops whose own checkpoint locations go in **Open loops / checkpoints** so a cold context resumes them mid-flight.
- [[../Agent Workflow]] -- §10.4 (Context Management: external state in files, handoff to `STATE.md`), §11.1 (the `.planning/` structure).
