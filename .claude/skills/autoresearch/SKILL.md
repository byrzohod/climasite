---
name: autoresearch
description: Autonomous experimentation loop for single-metric optimization (latency, bundle size, eval score, hyperparameters) -- tweak/measure/keep-or-revert/repeat in isolated branches with TSV result logging and crash recovery. Use this whenever the user wants to optimize a measurable metric overnight or unattended, mentions autoresearch, automated experiments, A/B testing code variants, sweeping hyperparameters, or has a clear runner command and single metric to chase.
---

# /autoresearch - Autonomous experimentation loop for any measurable metric

## When to use

Invoke this skill when you have a clearly **measurable optimization problem** where:

- A **single file** (or small set of files) drives the behavior you want to improve
- You can run **one command** that prints a single **numeric metric**
- The metric has a clear **direction** (lower-is-better or higher-is-better)
- Each run completes in a **bounded time** (seconds to a few minutes)
- You're willing to let the agent iterate **autonomously** and review the trail later

Typical fits:

- **Performance**: API latency, query exec time, render time, p95 response, throughput
- **Bundle / asset size**: webpack/vite output, image size after compression
- **ML / numerics**: hyperparameter search, model size, eval accuracy on a fixed set
- **Prompt engineering**: prompt edits scored against a fixed eval set
- **Cost**: tokens-per-task, infra cost per request
- **Conversion proxies**: LLM-judge score on landing copy variants
- **Compiler / build**: build time, output size, warning count

Bad fits (do NOT use this skill):

- The metric is subjective or requires human judgment per run
- Each run takes >30 minutes (loop becomes too slow to be useful)
- Changes touch many files / require coordinated edits across services
- The action is destructive or has side effects beyond the local repo (sends emails, writes to prod DB, calls paid APIs without budget guardrails)

This skill is adapted from [karpathy/autoresearch](https://github.com/karpathy/autoresearch). The core loop is his; this skill generalizes it beyond LLM training and wires it into the project workflow.

## Prerequisites

- Git repo with a clean working tree (commit or stash first)
- The runner command works locally and prints the metric to stdout in a greppable format
- A baseline value has been captured (or will be captured on first run)
- For paid / cloud-burning runs: a hard cost cap or a kill-switch is in place

## Configuration

The skill stores per-project config in `.autoresearch/program.md` so the agent can re-read it across context resets. Create this file on first use.

```markdown
# autoresearch config -- <project name>

## What we're optimizing
<one-sentence problem statement>

## Files
- EDIT_FILE: <path/to/file>            # the only file the agent modifies
- RUNNER_CMD: <command that runs the experiment>
- CONSTRAINT_CMD: <command that must pass, e.g. "pnpm test && pnpm lint">  # tests/lint/safety gate; exit 0 = pass
- HOLDOUT_CMD: <command that scores on a held-out split the loop never optimizes against>  # anti-overfit gate; prints METRIC_PATTERN
- LOG_FILE: run.log                    # where stdout/stderr go

## Metric
- METRIC_NAME: <e.g. p95_ms, bundle_kb, val_bpb, eval_score>
- METRIC_PATTERN: <grep pattern, e.g. ^p95_ms:>
- DIRECTION: <lower|higher>
- BUDGET_PER_RUN_SECONDS: <soft cap, e.g. 300>

## What counts as a keep
A `keep` requires ALL THREE, in order:
1. **Metric improved** on RUNNER_CMD above MIN_IMPROVEMENT_TO_KEEP (in DIRECTION)
2. **CONSTRAINT_CMD passes** (exit 0) -- tests, lint, and any safety check still green
3. **HOLDOUT_CMD validates** -- the gain holds on the held-out split (no overfit; see "Hold-out / anti-gaming")
Any one failing -> DISCARD. If CONSTRAINT_CMD or HOLDOUT_CMD is empty, the agent MUST flag the gap before starting and treat the missing gate as not-yet-satisfied (do not silently skip it).

## Constraints
- DO NOT modify: <list paths agent must not touch>
- DO NOT install: <leave empty to forbid new deps, or list allowed>
- VRAM / memory soft cap: <if relevant>
- Cost cap per loop: <e.g. $20 of cloud compute>

## Stop conditions
- MAX_RUNS: <e.g. 100, or "unlimited">
- MIN_IMPROVEMENT_TO_KEEP: <e.g. 0.001 -- below this, treat as no-op>
- WALL_CLOCK_LIMIT: <e.g. 8h>

## Notes for the agent
<freeform context: papers to reference, ideas to try, ideas to avoid>
```

## Setup

### Step 1: Confirm the config

Read `.autoresearch/program.md`. If it doesn't exist, ask the user the questions above as a single batch and write the file. If it exists, summarize what you understand from it and ask for confirmation before continuing.

### Step 2: Create the experiment branch

```bash
TAG="$(date +%b%d | tr '[:upper:]' '[:lower:]')"   # e.g. may05
BRANCH="autoresearch/$TAG"

# Branch must not already exist -- this is a fresh run
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  echo "Branch $BRANCH exists. Pick a new tag (e.g. ${TAG}-2)."
  exit 1
fi

git checkout -b "$BRANCH"
```

If the user runs multiple loops in parallel (e.g. one per machine or per GPU), suffix the tag: `autoresearch/may05-a`, `autoresearch/may05-b`.

### Step 3: Initialize the results log

```bash
mkdir -p .autoresearch
printf "commit\tmetric\tstatus\tdescription\n" > .autoresearch/results.tsv
```

The results log is **untracked by git** (add `.autoresearch/results.tsv` to `.gitignore` if not already covered) -- it represents per-machine experiment history, not source.

### Step 4: Baseline run

The very first run must be the **unmodified baseline**. Do not edit `EDIT_FILE` first. Just run:

```bash
$RUNNER_CMD > run.log 2>&1
grep "$METRIC_PATTERN" run.log
```

Record the baseline metric in `results.tsv` with status `keep` and description `baseline`.

### Step 5: Confirm and go

Tell the user:
- Branch: `autoresearch/<tag>`
- Baseline metric: `<value>`
- Direction: lower-better or higher-better
- Stop conditions in effect

Wait for confirmation. Once confirmed, **the loop begins and does not pause for further confirmation**.

## The Experiment Loop

```
LOOP UNTIL STOP CONDITION:
  1. Note current commit (the "anchor")
  2. Form a hypothesis: what to change in EDIT_FILE and why
  3. Edit EDIT_FILE
  4. git add EDIT_FILE && git commit -m "exp: <one-line description>"
  5. Run: $RUNNER_CMD > run.log 2>&1
  6. Extract metric: grep "$METRIC_PATTERN" run.log
  7. Decide (KEEP requires all three gates; see "What counts as a keep"):
     - Crash (no metric line)        -> see "Crashes" below
     - Metric NOT improved           -> DISCARD
     - Improved, but CONSTRAINT_CMD fails (tests/lint/safety) -> DISCARD
     - Improved + constraints pass, but HOLDOUT_CMD does not validate -> DISCARD (overfit; see "Hold-out / anti-gaming")
     - Improved + CONSTRAINT_CMD passes + HOLDOUT_CMD validates -> KEEP (advance branch)
  8. Append a row to .autoresearch/results.tsv (record both train-delta and holdout-delta)
  9. Loop
```

### Logging format

`results.tsv` is **tab-separated** (commas break long descriptions):

```
commit  metric  holdout status      description
a1b2c3d 124.5   125.1   keep        baseline
e4f5g6h 119.8   120.2   keep        cache the parsed schema between requests
i7j8k9l 124.4   124.6   discard     batch DB writes (no win, adds complexity)
m0n1o2p 0.0     -       crash       precompile regex (typo: missing import)
n2o3p4q 116.0   124.9   discard     overfit: train-win but holdout flat
q3r4s5t 118.2   118.9   keep        + reuse connection pool across handlers
```

Columns:
1. **commit**: short hash (7 chars)
2. **metric**: the train/RUNNER_CMD value extracted from the log; `0.0` for crashes
3. **holdout**: the HOLDOUT_CMD value (`-` for crashes or when no holdout gate is configured); compare its delta-from-baseline against the metric delta
4. **status**: `keep`, `discard`, or `crash`
5. **description**: short freeform; what the experiment tried (note "overfit" when holdout diverges from train)

### Branch advancement

After `KEEP`: do nothing -- the commit stays on the branch and becomes the new anchor for the next iteration.

After `DISCARD` (default -- hook-compatible, since `templates/settings.json` blocks `git reset --hard`):

```bash
git checkout "$ANCHOR_COMMIT" -- "$EDIT_FILE"
git commit -m "revert: <one-line description> (discard)"
```

This restores `EDIT_FILE` to the anchor and records the revert as a normal commit -- no destructive operation, so the protective PreToolUse hook lets it through. It is the canonical discard path.

> `git reset --hard "$ANCHOR_COMMIT"` is faster (no revert commit) but is **blocked** by the project's PreToolUse hook. Do NOT assume a carve-out. Using it is **PROPOSE-ONLY**: surface it to the user as a proposed, human-approved cool-off hooks change, and keep using the `git checkout -- EDIT_FILE` form until they explicitly approve and adjust `settings.json`. See the safety section.

### Simplicity criterion

All else being equal, **simpler is better**. Weigh complexity cost against improvement magnitude:

- A tiny improvement that adds 50 lines of hacky code → **discard**
- A tiny improvement from **deleting** code → **keep** (simplification win)
- ~Zero metric change but cleaner code → **keep**
- Big metric win but unreadable / fragile → think hard, often discard

When in doubt, prefer the simpler version.

## Hold-out / anti-gaming

An autoresearch loop is a Goodhart machine: it relentlessly maximizes the number you gave it, including by gaming the measurement. The defense is a **held-out split the loop never optimizes against**.

**Rule**: Any target that is itself an *evaluation* or a *test-meaningfulness* signal (eval-set score, judge score, test pass-rate, mutation-kill score, coverage) MUST be validated by `HOLDOUT_CMD` on data/cases the loop has never seen or edited. Concretely:

- Split your eval/test corpus once, up front, into a **train** slice (RUNNER_CMD optimizes against it) and a **holdout** slice (HOLDOUT_CMD only ever *reads* it). The agent must never inspect, log per-example, or tune against the holdout slice.
- On every candidate, **report train-delta and holdout-delta separately** (the `metric` and `holdout` columns).
- **Train-win + holdout-flat-or-regress = overfit -> DISCARD**, no matter how good the train number looks. A real improvement moves both, roughly together.
- A holdout-win *larger* than the train-win is also suspect (noise / leakage) -- think hard, usually discard.

**Goodhart failure modes to actively watch for** (each is a "win" the loop will reach for if unguarded):

- **CI-speed gaming**: skipping or `.skip`-ing tests, shrinking the suite, or lowering timeouts to make a "build time" / "test time" metric drop. CONSTRAINT_CMD must run the *full* suite; holdout = the unshrunk suite.
- **Golden-set overfit**: hard-coding expected outputs, special-casing the exact eval prompts, or memorizing the answer key so an eval/judge score climbs. Holdout = unseen prompts.
- **Cost under-answering**: cutting `max_tokens` / truncating output / refusing edge cases to lower a tokens-per-task or cost metric, at the expense of answer quality. Holdout = a quality gate that must not regress while cost drops.
- **Mutation gaming via brittle tests**: writing over-fitted, tautological, or snapshot-everything tests that raise a mutation-kill or coverage number without testing real behavior. Holdout = mutation run on a held-out mutant set; CONSTRAINT_CMD = the linter/quality bar.
- **Bundle gaming**: dropping needed code, lazy-deferring required modules, or externalizing deps to shrink a bundle-size metric while breaking functionality. CONSTRAINT_CMD = the e2e/smoke suite that proves nothing broke.

If you cannot construct a meaningful holdout for the chosen metric, say so explicitly and treat the third keep-gate as unsatisfiable -- do not pretend it passed.

## Crash handling

If `grep "$METRIC_PATTERN" run.log` returns nothing, the run failed. Then:

1. Run `tail -n 50 run.log` to see the stack / error
2. **Easy mistake** (typo, missing import, off-by-one): fix in `EDIT_FILE`, `git commit --amend --no-edit`, re-run. Counts as one experiment.
3. **Fundamentally broken idea** (architectural mistake, OOM): record `crash` in `results.tsv`, revert with `git checkout "$ANCHOR_COMMIT" -- "$EDIT_FILE" && git commit -m "revert: <desc> (crash)"` (the hook-compatible discard path), move on
4. **Three crashes in a row** on related ideas: stop, summarize what's not working, ask the user before continuing in that direction

## Timeouts

- Hard-kill any run that exceeds `BUDGET_PER_RUN_SECONDS * 2`. Treat as crash, revert.
- If the loop has burned `WALL_CLOCK_LIMIT`, stop and write a summary.

## NEVER STOP (within stop conditions)

Once the loop has begun, do **not** pause to ask the human "should I keep going?". The user might be asleep or away. They expect the loop to run until a stop condition fires.

If you run out of ideas:

- Re-read `EDIT_FILE` and `.autoresearch/program.md` for new angles
- Look at recent `discard`s -- can two near-misses combine?
- Try a more radical change you've been avoiding
- Search for prior art (papers, blog posts, library docs) for the specific bottleneck
- If after honest effort you have nothing left, **then** stop and summarize -- don't spin on trivial variations

## Stop conditions

The loop terminates when **any** of these fire:

- User interrupt
- `MAX_RUNS` reached
- `WALL_CLOCK_LIMIT` exceeded
- N consecutive runs with no improvement (configurable; default 20) -- diminishing returns
- Cost cap reached (for paid compute)
- All ideas exhausted with honest confidence

On stop, run the **end-of-run gate chain in order** -- each gate must pass before the next, and the PR is only ever *suggested* at the very end:

1. Write a summary to `.autoresearch/SUMMARY-<tag>.md`:
   - Baseline metric → final metric (delta + %)
   - **Holdout-delta report**: baseline → final on the held-out split, side by side with the train delta. Call out any train/holdout divergence as a residual overfit risk. (If no holdout was possible, state that the third keep-gate went unsatisfied for the run.)
   - Total runs, kept, discarded, crashed
   - Top 5 wins (commits + descriptions)
   - Top 3 surprising discards (things you'd have bet on)
   - What to try next time
2. Print the summary to the user.
3. **`/code-review` (MANDATORY)**: run it against the diff between the autoresearch branch and `main`. The loop's per-experiment commits were never reviewed. Do not proceed past a failing review.
3b. **Live-skill-untouched assertion (MANDATORY when in skill-optimization mode)**: assert the final diff changed NO live skill. Any non-empty result is a carve-out violation -- STOP, do not hand off to `/skill-verifier`, surface to the user.

   ```bash
   # Lists changed files under skills/ that are NOT in _proposed/ -- must be empty
   git diff --name-only main...HEAD -- '.claude/skills/' ':(exclude).claude/skills/_proposed/'
   ```
4. **`/council` (if EDIT_FILE is high-stakes)**: if `EDIT_FILE` touches auth, validation, money, migrations, infra, safety, or is a skill body/description, convene `/council` on the final diff before any merge.
5. **Then** suggest the next action: open a PR (`gh pr create`) merging the experiment branch, or cherry-pick specific commits. The PR is suggested, never auto-opened. (If `EDIT_FILE` was a skill, there is no `skills/` PR -- hand off to `/skill-verifier` per the self-evolution carve-out.)

## Safety

- **Branch isolation**: All work happens on `autoresearch/*`. Never run this loop on `main`.
- **No paid side effects**: the runner command must not call paid APIs, send emails, or write to prod. If it does, the user must explicitly opt in and a hard cost cap must be in `.autoresearch/program.md`.
- **Hooks compatibility (default)**: `templates/settings.json` ships a PreToolUse hook that **blocks `git reset --hard`** (and `push -f`, `checkout -- .`, `clean -f`, `branch -D`, `stash drop/clear`). The loop's discard/revert therefore defaults to the compatible form: `git checkout "$ANCHOR_COMMIT" -- "$EDIT_FILE" && git commit -m "revert: <desc>"`. No carve-out is assumed.
- **`git reset --hard` is PROPOSE-ONLY**: faster, but blocked. The agent may *propose* a human-approved cool-off hooks change (e.g. allow `git reset --hard` only on `autoresearch/*` branches) and explain the tradeoff, but must NOT edit `settings.json` itself or rely on the carve-out until the user approves. Until then, keep using the `git checkout -- EDIT_FILE` revert.
- **Shared infra**: The runner must NOT hammer shared local infra (Postgres in `~/Projects/shared-infra`). Use a project-local DB or testcontainers.
- **Resource caps**: Respect VRAM / RAM soft caps from the config. A run that OOMs the machine is a crash, not an opportunity.

### Self-evolution carve-out (EDIT_FILE is a skill) -- skill-optimization mode

If `EDIT_FILE` is a **skill body or description** (the loop is optimizing a skill against an eval -- e.g. trigger accuracy, task success), the loop runs in **skill-optimization mode** and does NOT touch a live skill. This preserves the §K.4 dynamic-skill rails: nothing the loop writes can change agent behavior until a human-gated verifier clears it.

> **Why a dedicated mode, not just a keep-rule.** The generic loop does "Edit EDIT_FILE" then commits it (steps 3-4), and `skill-quarantine.sh` only blocks *creating* a new top-level skill -- **editing an existing live `skills/<skill>.md` is allowed by the hook**. So if `EDIT_FILE` pointed at a live skill, the loop would mutate and commit it directly, before any "keep -> _proposed" rule applied. The fix is to make the live skill unreachable from the loop: from the **START**, edit only an inert copy.

- **At setup, before the baseline run, COPY the live skill into the inert tree and repoint EDIT_FILE at the copy:**

  ```bash
  mkdir -p .claude/skills/_proposed
  SKILL="$(basename "$EDIT_FILE" .md)"               # e.g. code-review
  cp ".claude/skills/$SKILL.md" ".claude/skills/_proposed/$SKILL.md"
  EDIT_FILE=".claude/skills/_proposed/$SKILL.md"     # the loop now ONLY ever edits this inert copy
  ```

  Update `.autoresearch/program.md` so `EDIT_FILE` records the `_proposed/` path. `skills/_proposed/` is an **INERT** location that is never loaded or triggered. From here the loop is structurally incapable of touching the live skill -- every "Edit EDIT_FILE" and commit lands on the copy.
- Every `keep` therefore stays inside `skills/_proposed/<skill>.md`. **Never edit, `git checkout`, or commit the live `skills/<skill>.md`.**
- The run ends by **handing off to `/skill-verifier`** for adversarial eval + variance analysis. The loop NEVER opens a direct PR into the live `skills/`.
- `/skill-verifier` (human-gated) is the only path from `skills/_proposed/` to live `skills/`. The autoresearch loop's job is to *propose*, not to promote.
- **End-of-run assertion (enforced in the gate chain below):** the final branch-vs-`main` diff MUST contain NO changes to any live `skills/<skill>.md` -- only `skills/_proposed/`. If it does, the carve-out was violated: stop, do not hand off, surface to the user.

## Multi-agent / parallel loops

If the user wants multiple agents iterating in parallel (e.g. one per GPU or container):

- Each agent gets its own branch suffix: `autoresearch/may05-a`, `autoresearch/may05-b`
- Each agent gets its own `.autoresearch/results-<suffix>.tsv` to avoid write conflicts
- A separate "merge" pass at the end picks the best commits across branches
- Do not have two agents writing the same `EDIT_FILE` in the same worktree -- use git worktrees: `git worktree add ../<project>-a autoresearch/may05-a`

## Integration with other skills

These compose into the **end-of-run gate chain** (see "On stop"): holdout-delta report -> `/code-review` (mandatory) -> `/council` (if high-stakes) -> suggest PR.

- **`/loop`** (global skill): Wrap the experiment loop with `/loop` if you want scheduled wake-ups instead of one long-lived agent session. The loop body becomes "run one experiment, log, decide, sleep". This trades context per iteration for cache misses -- worth it for multi-hour runs.
- **`/code-review`** (this template): **Mandatory** end-of-run gate. Run against the diff between the autoresearch branch and `main` before anything else downstream. The agent's per-experiment commits were never reviewed.
- **`/council`** (this template): Convene on the final diff when `EDIT_FILE` is high-stakes (auth, validation, money, migrations, infra, safety, or a skill body/description) before any merge.
- **`/security-review`** (this template): If `EDIT_FILE` touches auth, validation, or anything user-facing, run `/security-review` before merging (folds into the `/council` step for high-stakes diffs).
- **`/skill-verifier`** (this template): The mandatory handoff when `EDIT_FILE` is a skill -- the only path from `skills/_proposed/` to live `skills/`. See the self-evolution carve-out.

## Worked example -- API latency optimization

```markdown
# .autoresearch/program.md

## What we're optimizing
p95 latency of POST /api/checkout under fixed synthetic load

## Files
- EDIT_FILE: src/api/checkout/handler.ts
- RUNNER_CMD: pnpm bench:checkout                 # k6 against local server on the TRAIN load profile, prints metric
- CONSTRAINT_CMD: pnpm test && pnpm lint          # behavior + style must stay green
- HOLDOUT_CMD: pnpm bench:checkout --profile=holdout   # same metric on an UNSEEN load profile (anti-overfit)
- LOG_FILE: run.log

## Metric
- METRIC_NAME: p95_ms
- METRIC_PATTERN: ^p95_ms:
- DIRECTION: lower
- BUDGET_PER_RUN_SECONDS: 120

## Constraints
- DO NOT modify: src/db/**, src/auth/**
- DO NOT install: any new packages
- Cost cap: local only, no cloud spend

## Stop conditions
- MAX_RUNS: 50
- MIN_IMPROVEMENT_TO_KEEP: 1.0          # 1ms = noise, ignore
- WALL_CLOCK_LIMIT: 4h
```

Agent runs `uv run train.py` → no, that's nanochat. Here it runs `pnpm bench:checkout`. Same loop pattern; every `keep` also had to pass `pnpm test && pnpm lint` and hold up on the unseen `--profile=holdout` load. After 4 hours: ~80 experiments, summary at `.autoresearch/SUMMARY-may05.md` reporting 41ms → 24ms p95 on train and 42ms → 26ms on holdout (gain holds, not overfit), then mandatory `/code-review` on the branch-vs-`main` diff before a PR is suggested.

## What this skill does NOT do

- Choose what to optimize (the user does)
- Design the metric / runner (the user does, the agent can help scaffold)
- Make architectural decisions outside `EDIT_FILE`
- Open the final PR (suggested at end, but user-confirmed)
- Skip the end-of-run gate chain -- `/code-review` is mandatory, `/council` is required for high-stakes diffs, and a skill `EDIT_FILE` always goes through `/skill-verifier`, never a direct `skills/` PR
- Promote a skill on its own -- the loop writes to `skills/_proposed/` only; `/skill-verifier` is the human-gated promoter
