---
name: code-review
description: The CHANNEL layer of code review, run UNDER /review-orchestrate -- progressive review channels with iterative fix cycles: self-review then /review (in-session subagents) then ask the user for /ultrareview (cloud) then optional Codex CLI fallback. Hard-capped at <=2 fix rounds per channel, then a human decides; sequenced on the short-lived branch (cheap in-session review BEFORE the merge queue, full real-infra suite IN the queue). The 13 review dimensions live in Agent Workflow.md §6 -- this skill references them, it does not restate them. Use this whenever the user asks for code review, wants a single review channel (just /review, or just the Codex sanity pass), before any PR merge, after completing a feature or phase, after any significant refactor, or when reviewing the diff for correctness, security, performance, error handling, code quality, AI code smells, testing, accessibility, data modeling, API design, observability, dependencies, or documentation. For full multi-ANGLE fan-out (correctness + security + perf + architecture + best-practices + SEO + a11y in parallel) the user wants /review-orchestrate, which calls this skill as one of its channels.
---

# /code-review - The channel layer of review (runs under /review-orchestrate)

This skill is the **channel** layer. It runs the same review diff through progressively deeper *channels* -- self -> `/review` (in-session) -> ask the user for `/ultrareview` (cloud) -> optional Codex (cross-vendor) -- and iterates fixes. **`/review-orchestrate` is the layer above it**: that skill fans out independent *angles* (correctness/quality, security, performance, architecture-fit, best-practices, SEO, accessibility) in parallel and calls **this** skill as the correctness/quality channel inside its fan-out.

- **Channels = how independent is the reviewer?** (same-model in-session, cloud, other vendor). This skill.
- **Angles = what is being reviewed?** (security vs perf vs architecture vs ...). `/review-orchestrate`.

When the user just wants one channel ("run `/review`", "give it a Codex sanity pass"), invoke this skill directly. When they want the full multi-angle pre-merge pass ("is this ready to merge?", "review the unit"), they want `/review-orchestrate`, which orchestrates these channels under each angle.

## When to use

Invoke this skill:
- **A single, narrow review channel is what's wanted** -- just `/review`, or just the Codex cross-vendor pass. This skill *is* the channel layer.
- **Before every PR merge** -- no exceptions (as the channel layer inside `/review-orchestrate`'s pre-queue pass, or standalone for a trivial change).
- **After completing a feature or phase** -- full channel sweep of all changes in that scope.
- **After any significant refactor** -- verify nothing was broken or degraded.
- **Periodically on the whole codebase** -- spot-check for accumulated issues.

For a full multi-angle pass (7 angles in parallel, consolidated, with a disagreements register), invoke **`/review-orchestrate`** instead -- it calls this skill as one channel per angle.

The authoring agent should NOT be the only one reviewing its own code. This skill drives an independent perspective so the model that wrote the code does not also have the final word on it.

## The channels (cheapest-first)

Three review **channels** are available, ordered cheapest-first. They differ by **independence** (how far the reviewer is from the authoring agent), not by *what* they check -- all channels evaluate the **13 review dimensions single-sourced in `Agent Workflow.md` §6** (and mirrored in `agents/reviewer.md`). Use them progressively.

| Channel | Trigger | What it does | Independence |
|---------|---------|--------------|---------------|
| **`/review`** (global skill) | Agent-launchable in current session | Spawns parallel Claude subagents per dimension. Free, fast, in-context. | Different runs; same model family |
| **`/ultrareview`** (user-triggered) | **User runs the slash command** -- agent suggests it, cannot launch | Cloud multi-agent review of the branch (or a GitHub PR by number). Deepest, billed. | Multiple independent Claude runs in cloud, no in-session contamination |
| **Codex CLI fallback** | When user wants a non-Anthropic vendor opinion | OpenAI-model review via local CLI. | Different vendor entirely (strongest independence signal) |

**Default flow for a feature/phase PR:**

1. Self-review (Step 1)
2. `/review` in-session (Step 2-A) -- agent launches, fixes, iterates (**<=2 rounds**, see Iteration Protocol)
3. **Ask user to run `/ultrareview`** before merging (Step 2-B) -- cloud review, the agent cannot launch this. For high-stakes units only when invoked under `/review-orchestrate`.
4. Optional: Codex CLI for cross-vendor sanity check on critical changes (Step 2-C)
5. Document outcomes in PR description (Step 3)

> When this skill runs **under `/review-orchestrate`**, the orchestrator decides which channels fire per angle and reserves the billed cloud/cross-vendor channels (steps 2-B / 2-C) for **high-stakes units** (auth, payments, migrations, security-sensitive). Run standalone for a trivial change, self + one `/review` round is the whole flow.

## Trunk sequencing -- where these channels run

These channels are **cheap in-session review on the short-lived branch (<24h), BEFORE the merge queue** -- run while the branch is still cheap to change. They are **not the merge gate**; the gate is the merge queue's full real-infra suite (§3.2 / §J.2).

```
short branch  ->  cheap in-session review (this skill's channels)  ->  merge queue (full real-infra suite = the gate)  ->  main
                  self -> /review [-> /ultrareview / Codex if high-stakes]      integration + cross-browser e2e + a11y + perf
                  <=2 rounds per channel, then a human decides            against future-main; THIS is what merges
```

- **Order:** resolve every finding on the branch first; do not enter the queue with open criticals/warnings. A unit that is review-clean still has to pass the `merge_group` full suite -- review precedes the gate, it does not replace it.
- **Don't run cloud `/ultrareview` on every merge** -- it burns trunk velocity and EUR spend. Reserve the billed channels for high-stakes units; the FAST in-session channels run every time.
- This skill drives the **channels**; `/review-orchestrate` drives the **angles** and owns the pre-queue placement. `/trunk-merge` Step 6 calls `/review-orchestrate`, which calls this.

## Review dimensions

Every review evaluates the code against the **13 review dimensions** -- **single-sourced in `Agent Workflow.md` §6** (and mirrored, with the per-dimension checklist, in `agents/reviewer.md`). They are: correctness, security, performance, error handling, code quality, AI code smells, testing, accessibility, data modeling, API design, observability, dependencies, documentation.

Do **not** restate the dimension table here -- read §6 / `agents/reviewer.md`. Not every dimension applies to every change; use judgment, but don't skip dimensions out of laziness.

## Process

### Step 1: Self-review first

Before invoking any external reviewer, the agent reads its own diff and checks against the 13 dimensions (§6 / `agents/reviewer.md`). This catches obvious issues that don't need an external opinion.

```bash
# For a full PR review (compared to main)
git diff main...HEAD > /tmp/review-diff.txt

# For a pre-commit review (staged changes only)
git diff --staged > /tmp/review-diff.txt
```

Read the diff yourself. Fix anything obvious. Then move to step 2.

### Step 2-A: Run `/review` (in-session, agent-launchable)

`/review` is a global skill that spawns parallel Claude subagents to review different dimensions concurrently. The agent can launch this directly.

Invoke it via the Skill tool. It will:
- Generate the diff (or use the one you specify)
- Spawn parallel subagents per review dimension
- Aggregate findings by severity (critical / warning / suggestion)

Fix the findings, then re-run **once** over the fix diff (Round 2). Stop at the round cap -- see Iteration Protocol (<=2 rounds, then a human decides).

### Step 2-B: Ask the user to run `/ultrareview`

`/ultrareview` is a **user-triggered, billed** cloud multi-agent review. The agent **cannot launch it** -- you must ask the user to run the slash command.

Tell the user:

> "Self-review and `/review` are clean. For final pre-merge review, please run `/ultrareview` in your terminal. Two forms:
>   - `/ultrareview` -- bundles the current local branch (no GitHub remote needed).
>   - `/ultrareview <PR#>` -- reviews a GitHub PR by number.
> 
> When the report comes back, paste it here and I'll address every finding."

When the user pastes the report, parse it, fix the issues, and ask the user to re-run `/ultrareview` **once** over the fix diff if it changed substantially -- then stop at the round cap (<=2 rounds, then the human decides). This billed cloud channel is **reserved for high-stakes units** (auth, payments, migrations, security-sensitive); skip it for routine merges to preserve trunk velocity.

### Step 2-C: Cross-vendor review — `/council` (Codex, read-only advisor)

For high-stakes units (auth, payments, data migrations, security-sensitive code), add an independent **non-Anthropic** perspective via the **`/council`** skill — the *vendor* layer (see `skills/council.md`). Codex is a **READ-ONLY advisor**; Claude triages and applies any fixes. This catches vendor-family-correlated blind spots.

**Preflight**: `which codex` — if absent, skip (optional layer).

The full council (blind Claude + Codex → 4-section synthesis, ≤2 rounds → human) is `/council`. For a quick one-off cross-vendor pass on a diff, run the Codex leg directly — read-only, final message captured:

```bash
git diff main...HEAD > /tmp/review-diff.txt
codex exec "You are a senior, READ-ONLY code reviewer (advice only — you cannot change files).
Review the diff below across: correctness, security (OWASP), performance, error handling,
code quality (SOLID/DRY/KISS), tests, accessibility, data modeling, API design, observability,
dependencies, documentation. For each issue: file, line, severity (critical/warning/suggestion),
dimension, description, suggested fix. If none, respond exactly 'LGTM'.

$(cat /tmp/review-diff.txt)" \
  -m gpt-5.5 -c model_reasoning_effort="xhigh" \
  -s read-only -c approval_policy="never" \
  -o /tmp/codex-review.md --json < /dev/null
# Findings → /tmp/codex-review.md (advice only — Claude applies fixes, Codex never does)
```

Shapes that matter: `codex exec` (the removed `codex -q` no longer exists); prompt **as an argument** + `< /dev/null` (defuses the non-TTY hang); **`-s read-only`** so Codex cannot modify the repo or push; `-o` captures just the final message. Codex egresses the diff to OpenAI.

### Step 3: Document outcomes in PR description

After review terminates (clean, or human-decided at the cap), log a summary in the PR description. Record the **termination** explicitly -- clean at round N, or the human go/no-go.

```markdown
## Review (channels)

- Self-review: ✓
- `/review` (in-session): N findings, all addressed (round 1: X, round 2: Y) — clean at round 2 ✓ [<=2 rounds]
- `/ultrareview` (cloud): N findings, all addressed   (or "skipped — not a high-stakes unit")
- Codex CLI (cross-vendor): N findings, all addressed   (or "skipped — not safety-critical / not installed")
- Termination: review-clean at round N   (or "human decision: merge with follow-up tickets #…, #…")
- Trade-offs / known limitations: [link to ADR if applicable]
```

(When run under `/review-orchestrate`, this rolls up into that skill's per-angle report; don't duplicate.)

## Iteration Protocol

- **Round 1**: Full review of all changes by the chosen channel
- **Round 2**: Review only the new diff (the changes made to fix round 1 issues), unless the user requests a full re-review
- **Hard cap: <=2 rounds per channel, then a HUMAN decides.** Do **not** start a Round 3. After two resolve->re-review cycles, surface the residue to the user -- remaining findings + a recommendation (merge-with-follow-up-tickets vs hold) -- and proceed on their call. This is the **termination guarantee**: "zero open comments" can never be reached on some diffs, and an over-eager channel must not loop forever. (Matches `/review-orchestrate`'s cap and §6.3 / Blueprint §J.3 / §A-2.)
- **Exit conditions**: Channel returns "clean" (no new critical/warning findings), the human decides at the round cap, or the user says stop.
- **No-progress guard**: if fixes introduce as many criticals as they resolve twice in a row, HALT and escalate -- the plan or design may be wrong; re-plan, don't keep patching.

## Why a hierarchy of channels

- **`/review`** is fast and free -- agent launches it without user intervention. Catches most issues. Use first, every time.
- **`/ultrareview`** is the deepest available; cloud agents have no in-session context contamination from the authoring agent. Reserve for high-stakes units (it is billed) -- not every merge.
- **Codex CLI** uses a different vendor entirely -- the strongest independence signal for the few cases where vendor diversity matters (vendor-specific blind spots, model-family-shared assumptions).

All three check the **same 13 dimensions (§6)**; they differ only in how independent the reviewer is. `/review-orchestrate` is the orthogonal axis -- it varies the *angle* (security, perf, architecture, ...) and runs these channels inside each angle.

## Goal

The goal is not to write perfect code on the first pass -- it's to catch problems before they reach main, **without letting review stall a merge forever**. The channels are the independence axis of review; the <=2-round cap keeps them bounded; the merge queue's full suite is the actual gate. Multi-channel review is the safety net for AI-authored code on the short branch, before the queue.

## See also

- **`/review-orchestrate`** -- the **layer above** this skill. It fans out independent *angles* (correctness/quality, security, performance, architecture-fit, best-practices, SEO, accessibility) in parallel and calls **this** skill as the channel layer per angle. Owns the pre-queue placement and the same <=2-round-then-human cap. Use it for the full pre-merge pass; use this skill for a single channel.
- `/trunk-merge` -- Step 6 runs `/review-orchestrate` (which runs this) on the short branch before the queue; Step 7 hands the PR to the merge queue (the real gate).
- `/security-review` -- the deep OWASP pass the security dimension/angle defers to.
- `agents/reviewer.md` -- the read-only correctness/quality reviewer; the per-dimension checklist for the 13 dimensions lives there (single source with §6). `agents/developer.md` fixes findings; reviewers report only.
- [[../Agent Workflow]] -- §6 (Code Review: the 13 dimensions, the channel matrix, the <=2-rounds-then-human cap), §6.3 (standard multi-angle flow), §3.2 / §J.2 (the merge-queue full suite that is the actual gate this review precedes).
