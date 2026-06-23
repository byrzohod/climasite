---
name: metrics
description: Compute DORA-lite delivery signals from git + CI as the OBJECTIVE INPUT for self-evolution -- change-fail rate, rework/churn, lead-time, escaped-defect rate, each segmented AI-heavy vs human-authored (via the Co-Authored-By trailer). REPORT-ONLY read-only computation feeding /skill-health then /skill-eval so evolution is evidence-driven, not vibes -- an INPUT SIGNAL, never a KPI or target (Goodhart). Use this whenever the user mentions DORA, delivery metrics, four keys, change-failure rate, MTTR/lead time, rework or code churn, escaped/bug-escape defects, "are our AI changes regressing more", whether the workflow is improving, or before /skill-health / /skill-eval / the post-project evolution loop / an /update-workflow cycle.
---

# /metrics - DORA-lite delivery signal from git + CI (REPORT-ONLY, the input to evolution)

## When to use

Invoke this skill to produce the **objective signal** the self-evolution loop runs on (Blueprint §K.3):

- **Inside `/skill-health`** -- `/skill-health` is REPORT-only and gates `/skill-eval`; it calls this skill to attach hard delivery numbers to its findings so promotion/refactor decisions are evidence-driven, not vibes. This is the primary caller.
- **Before the non-skippable evolution loop** -- the loop is `skill-health → skill-eval → Sessions/decision-log entry → commit`, run after each project and every `/update-workflow`. `metrics` supplies the numbers `skill-eval` reasons over.
- **On demand** when the user asks "is the workflow actually getting better?", "are AI-authored changes regressing more than ours?", or "what's our change-fail / rework / escaped-defect trend?"
- **End of a phase / release** -- a quick read of the trend window to see if the last phase's process changes moved the signal.

Skip the heavy run for: a single in-progress unit (no merge/incident history to compute over yet), or a brand-new repo with <~20 merges to main (the sample is too small to mean anything -- say so and emit a "insufficient history" note instead of fabricating rates).

## What this skill is NOT

- It is **NOT a KPI, a target, a scorecard, or a quota.** The moment a delivery metric becomes a goal, it gets gamed and stops measuring anything (Goodhart's law). These numbers exist **only** to *inform* `skill-eval` about where the workflow is leaking, and to *detect* whether a process change helped. Never optimize the number directly; optimize the process, then watch whether the number moves.
- It **does not edit, fix, or evolve anything.** Read-only computation over git + CI. It writes a report; `skill-health` consumes it; `skill-eval` (with the user's go-ahead and inside the velocity cap) acts. This **detect-vs-act separation** is deliberate -- the thing measuring the workflow must not also be the thing changing it, exactly as `*-health` skills REPORT and `*-eval` skills ACT (Blueprint §K, workflow §5.7 self-review optimism). Same posture as [[kb-health]] and [[skill-verifier]].
- It is **not an incident tool.** Reverts/incidents are an *input* it counts; getting a bad change out of main is [[rollback]]'s job.
- It does **not** dictate a "good" or "bad" verdict on a developer or an agent. It segments AI-authored vs human-authored changes to find *where the workflow needs help*, not to assign blame.

## Tier

**Every project.** The computation is a cheap, read-only pass over `git log` + the CI/PR API -- no infra, no cost. It is an *input signal*, not a gate, so it never blocks a merge. The four core signals run everywhere there is enough history. Heavier segmentation (per-component churn, full nightly history) is **scoped by whether the data exists, not by tier** -- run it wherever the repo has per-component history and a CI/incident trail to compute over, and skip it where that source is absent; the rigor of the four core signals never relaxes.

## The four DORA-lite signals (read-only definitions)

DORA's "four keys" are deployment frequency, lead time for changes, change-failure rate, and time-to-restore. This skill computes a **git+CI-derivable approximation** of the change-quality subset most relevant to *workflow evolution* -- plus rework/churn and escaped defects, which are the leading indicators of test/plan quality. Every metric below is computed over a **trend window** (default: last 90 days OR last 50 merges to the trunk, whichever is larger; state the window in the report).

| Signal | Definition (what to compute) | Primary source | Why it informs evolution |
|--------|------------------------------|----------------|--------------------------|
| **Change-fail rate** | Of merges to trunk in the window, the fraction that triggered a *remediation* within N days (default 14): a `git revert` of the merge, a follow-up commit referencing it as a fix/hotfix, a reopened-incident link, or a post-merge `merge_group`/deploy CI failure on trunk. | git (revert/fix lineage) + CI runs on trunk | High or rising → the *gate* (review/test/verify) is letting bad changes through. Points `skill-eval` at `/code-review`, `/test-plan`, `/verify-work`, `/mutation-check`. |
| **Rework / churn** | Lines (or files) re-touched soon after landing: code added in window then modified/deleted again within M days (default 21) -- a proxy for "we didn't get it right the first time." Report as a churn ratio (reworked LoC ÷ total changed LoC) and flag hotspot files. | git (blame/log diff over the window) | High churn → planning or design altitude is off (thrash), or tests didn't pin behavior. Points at `/plan-tree`, `/design-doc`. |
| **Lead-time-for-changes** | Wall-clock from a unit's first commit (or branch creation) to its merge on trunk. Report median + p75 (means lie on skewed data). | git (first-commit → merge timestamps) + PR API for branch/merge times | Trend, not a target. A rising tail can mean review bottleneck or units too large (split them); a healthy short branch (<24h, §J trunk-mode) is the design intent, not a quota to beat. |
| **Escaped-defect rate** | Defects found **after** a change reached trunk/users (bug-fix commits / issues labelled `bug`/`regression` / incident reverts) attributed back to the originating merge, normalized per N merges or per KLoC. | git (fix→cause linkage) + issue tracker labels if available (`gh issue list`) | The clearest "the suite didn't catch it" signal -- the *escaped* 5% the mutation gate and `/verify-work` exist to shrink. Points at `/test-plan`, `/mutation-check`, `/verify-work`. |

### The AI-heavy segmentation (the whole point of §K.3)

Every signal above is reported **twice**: once for **AI-heavy** changes and once for **human-authored** changes, plus the blended total. This is what lets evolution ask the right question: *"is the workflow leaking specifically where agents do the work?"*

- **How to classify a commit/merge as AI-heavy:** the **`Co-Authored-By:` trailer** is the established marker for tracking which code was AI-generated vs human-written -- the same commit-message discipline enforced by the Stop hook (`hooks/workflow-check.sh`) and the conventional-commit PreToolUse hook in `templates/settings.json`. A merge whose commits carry an agent co-author trailer (e.g., `Co-Authored-By: Claude ...`) is AI-heavy; one without is human-authored. For squash-merge repos, read the trailer on the squashed commit; for merge-commit repos, read it across the merged commits and classify by majority of changed LoC.
- **If the project does not use the trailer consistently:** say so in the report and fall back to whatever the repo *does* track (a `[ai]`/`[agent]` commit-tag convention, a PR label, or `PATHS.md`-noted agent-owned dirs). If there is **no** reliable marker, report the blended numbers only and flag "AI/human segmentation unavailable -- adopt the `Co-Authored-By` trailer to unlock §K.3 segmentation." Do **not** guess authorship from `git author` (the human runs the agent, so author ≠ AI-heavy).
- **Read-only.** Segmentation is a `git log --grep`/trailer parse. It never rewrites history or relabels commits.

## Per-stack / per-platform source commands (read-only)

All commands are **read-only** (`git log`, `gh api`, issue *list*). Never run anything that mutates history, force-pushes, edits issues, or deploys. Mechanics are illustrative -- adapt to the repo's actual host and squash/merge style; prefer the dedicated tool over hand-rolled `sed`/`awk` where one exists.

| Concern | Read-only mechanic (illustrative) |
|---------|-----------------------------------|
| **Trend window of merges to trunk** | `git log --first-parent --since="90 days ago" --pretty='%H%x09%ct%x09%an%x09%s' origin/<trunk>` (first-parent isolates merges/squashes on trunk) |
| **AI-heavy vs human split** | parse the `Co-Authored-By:` trailer per commit: `git log --format='%H%x09%(trailers:key=Co-Authored-By,valueonly)'` over the window |
| **Reverts (change-fail input)** | `git log --first-parent --grep='^Revert ' --since=...` + match the reverted SHA back to its original merge |
| **Fix/hotfix follow-ups (change-fail / escaped-defect)** | commits referencing a prior merge: `--grep='fix\|hotfix\|regression\|revert'` (case-insensitive), or conventional-commit `fix:` type, linked to the cause merge by SHA/PR ref |
| **Rework / churn** | per-file: `git log --since=<window> --numstat -- <path>` → compute LoC re-touched within M days of landing; surface top hotspot files |
| **Lead-time-for-changes** | per merge: branch-create / first-commit `%ct` → merge `%ct`; via PR API `gh pr list --state merged --json number,createdAt,mergedAt,labels` for accurate branch times |
| **Post-merge CI failures on trunk (change-fail input)** | GitHub: `gh run list --branch <trunk> --json conclusion,headSha,event` (count `merge_group`/push runs that concluded `failure` after a green PR) |
| **Escaped defects via tracker (when a tracker exists)** | `gh issue list --label bug --label regression --state all --json number,createdAt,closedAt` → link to the originating merge by reference/commit-closes |

Notes:
- **First-parent matters.** Computing rates over *all* commits (not first-parent on trunk) double-counts feature-branch churn and corrupts every ratio. Default to first-parent unless the repo is pure-rebase/linear.
- **Small sample → say so.** Below ~20 merges in the window, a "rate" is noise. Report raw counts + "insufficient history for stable rates" rather than a misleading percentage.
- **Issue-tracker inputs are best-effort.** If `gh`/tracker access is unavailable from the sandbox, compute the git-only approximation (revert/fix lineage) and mark the tracker-derived rows `UNVERIFIABLE (no tracker access)` -- never fabricate them.

## Procedure

1. **Confirm scope + window.** Identify the trunk (`main` per ruleset, or the repo default), and set the window (default: max(90 days, 50 merges); state it explicitly). Note the merge style (squash vs merge-commit vs rebase) -- it changes how you read trailers and first-parent.
2. **Pull the read-only inputs.** Run the `git log` window, the `Co-Authored-By` trailer parse, revert/fix lineage, `--numstat` for churn, PR/CI API for lead-time + post-merge failures, and (if available) the issue tracker for escaped defects. Everything is read-only.
3. **Compute the four signals** over the window, each split **AI-heavy / human / blended**. Use medians + p75 for time-based signals; use ratios + raw counts for the rest.
4. **Compute deltas vs the previous run.** Compare to the last `metrics` report (or the prior window). A *direction of travel* is far more useful to `skill-eval` than an absolute number -- "change-fail on AI-heavy changes 9% → 17% since the last two phases" is an actionable signal; "17%" alone is not.
5. **Map each notable signal to the process lever it implicates** -- NOT to a target. (See the mapping table.) This is the bridge to `skill-eval`: the metric points at *which skill/agent/rubric* to examine, and `skill-eval` decides (with the velocity cap + cool-off) whether anything actually changes.
6. **Emit the report** (format below). Modified files: **none**.

## Signal model (how `skill-health`/`skill-eval` should read the numbers)

There are no pass/fail thresholds here on purpose (a threshold becomes a target becomes Goodhart). Use **direction + segmentation gap** instead:

| Reading | Meaning | What it points evolution at (the *lever*, not the *number*) |
|---------|---------|-------------------------------------------------------------|
| **A signal is *worsening* run-over-run** | The workflow change two phases ago may have hurt, or a new gap opened | Investigate the implicated skill (mapping below); candidate for `skill-eval` refactor / `_methodology.md` lesson |
| **AI-heavy >> human on the same signal** | The leak is specifically where agents do the work | Strengthen the AI-facing gate: `/code-review` (AI code smells dimension), `/mutation-check`, `/verify-work`, `claim-verifier`; log the pattern to `_methodology.md` |
| **A signal *improved* after a known process change** | Evidence the last evolution worked | Record as a *kept* change; do NOT chase the number further (avoid over-fitting) |
| **Sample too small / source missing** | Not enough signal to act | Report "insufficient signal"; `skill-eval` must NOT evolve off it ("evolution off stale/thin state is drift", §K) |

### Metric → process-lever mapping (for `skill-eval`, advisory only)

| Signal that moved | Likely process gap | Skill/agent to examine (skill-eval's call, within velocity cap) |
|-------------------|--------------------|----------------------------------------------------------------|
| Change-fail ↑ | Gate too weak | `/code-review`, `/review-orchestrate`, `/verify-work`, `/security-review` |
| Escaped-defect ↑ | Tests not catching it | `/test-plan`, `/mutation-check`, `verifier` break-the-code |
| Rework/churn ↑ | Plan/design altitude off; thrash | `/plan-tree`, `/design-doc`, `plan-critic` |
| Lead-time tail ↑ | Units too large / review bottleneck | `/plan-tree` (smaller units), `/trunk-merge`, `/review-orchestrate` round cap |
| AI-heavy gap wide | AI-specific quality leak | AI code-smell dimension in `/code-review`, `claim-verifier`, `_methodology.md` lesson |

## Output

REPORT-ONLY. Emit inline (and into the `skill-health` report / Sessions decision-log if a caller is writing one). Never write fixes, never relabel commits, never touch history.

```markdown
## metrics -- DORA-lite delivery signal (REPORT-ONLY -- no files modified, INPUT SIGNAL not a KPI)

**Repo / trunk**: <repo> @ <trunk>     **Run**: 2026-MM-DD HH:MM
**Window**: last 90 days / 57 merges (first-parent)     **Merge style**: squash
**AI/human segmentation**: via Co-Authored-By trailer (✓ reliable) | (or) UNAVAILABLE -- blended only

| Signal | AI-heavy | Human | Blended | Δ vs last run |
|--------|----------|-------|---------|---------------|
| Change-fail rate (≤14d) | 17% (6/35) | 9% (2/22) | 14% (8/57) | ▲ +5pp (AI) |
| Rework/churn (≤21d) | 0.31 | 0.18 | 0.26 | ▲ +0.06 |
| Lead-time median / p75 | 19h / 41h | 14h / 28h | 17h / 36h | ▲ tail +9h |
| Escaped-defect / 50 merges | 5 | 2 | 7 | ▬ flat |

### Notable readings (direction + gap -- NOT targets)
- Change-fail on **AI-heavy** changes 12% → 17% over the last two phases; AI-vs-human gap = 8pp. Lever: `/code-review` AI-code-smell dimension + `/mutation-check`.
- Rework hotspots: `src/api/payments.ts` (re-touched 4× in 21d), `src/agents/orchestrator.ts`. Lever: `/design-doc` altitude on those components.
- Escaped-defect flat -- the `/verify-work` change last phase appears to be holding; do NOT chase further.

### Data caveats
- Issue-tracker rows: UNVERIFIABLE (no `gh` access from sandbox) -- escaped-defect is the git-only approximation.
- Sample for human-authored lead-time is thin (n=22) -- treat the median as indicative.

**This computation modified no files.** Hand to /skill-health → /skill-eval; treat every figure as an input signal, never a goal.
```

Keep the report tight -- the table + a handful of notable readings is the decision-useful part. The caller acts on direction and the implicated lever, not on a leaderboard.

## Handoff

- **To `/skill-health`** -- which is the gate: it folds these numbers into its REPORT, alongside its own staleness/never-triggered/velocity findings, and states whether the signal is strong enough to act on. `/skill-health` does not fix either; both are detect-only.
- **To `/skill-eval`** (only via `skill-health`, only with the user's go-ahead, only inside the **≤5 edits/cycle** velocity cap + cool-off boundary): it reads the implicated levers and decides whether a skill body / `_methodology.md` lesson actually changes. Every resulting change is logged to `EVOLUTION.md` with the metric citation as the "Why".
- **HALT respected:** if `/skill-health` has tripped the drift gate (>5 `EVOLUTION.md` entries / 30 days), evolution halts regardless of what these metrics say -- a worsening number is **not** a licence to exceed the cap.

## When NOT to use

- **As a target to optimize.** (Stated thrice on purpose: Goodhart.) Optimize the process; let the number follow.
- **On a repo with too little history** (<~20 merges in the window) -- emit "insufficient signal" instead of a fabricated rate.
- **Mid-unit, before anything has merged** -- there is nothing to compute over yet.
- **To assign blame** to a person or an agent -- the AI/human split locates *where the workflow needs help*, full stop.
- **To gate a merge or a release** -- it is an input signal, not a CI check. The gates are `/code-review`, the CI suite, `/verify-work`, `/mutation-check`.

## See also

- `vault/AI/Workflow Redesign 2026-06/Blueprint.md` §K.3 -- the non-skippable evolution loop this skill supplies the objective signal for; §K.1 velocity cap / cool-off / detect-vs-act.
- `vault/AI/project-template/templates/settings.json` + `vault/AI/project-template/hooks/workflow-check.sh` -- the Stop-hook commit-message machinery (conventional commits + `Co-Authored-By` trailer) that the AI-vs-human tracking convention this skill's segmentation relies on is enforced by; `vault/AI/Agent Workflow.md` §5.7 self-review optimism (why measuring is separated from evolving).
- [[skill-verifier]] -- the sibling detect-vs-act auditor (same §5.7 rationale).
- [[kb-health]] -- the other REPORT-only auditor; same gate-before-evolve, never-edit posture.
- `skill-health` -- the consumer/gate that folds these numbers into its findings (REPORT-only, gates `skill-eval`).
- `skill-eval` -- the actor that evolves skills off this signal, within the velocity cap (the only one that edits).
- [[rollback]], [[mutation-check]], [[verify-work]], [[test-plan]], [[code-review]], [[plan-tree]], [[design-doc]] -- the process levers a worsening signal points evolution at.
