---
name: flag-cleanup
description: Remove stale, past-expiry feature flags with an AST-rewrite tool (Polyglot Piranha or equivalent), delete the now-dead branches, and open a review-gated PR -- NEVER auto-merged. Use this whenever the user mentions flag cleanup, stale/expired/dead feature flags, removing a flag, retiring a flag, flag debt, flag-cleanup, Piranha, "this flag is permanent now", "delete the old branch behind the flag", stale-flag accumulation, or when /hygiene-sweep reaches its flag-debt step. Finds flags past their recorded expiry, rewrites the code to inline the kept side and drop the dead side, and leaves the change in a PR for human review through the merge queue.
---

# /flag-cleanup - AST-based stale-flag removal (review-gated, never auto-merged)

A feature flag is temporary scaffolding. Once a unit has fully shipped behind a flag and the flag is past its expiry, the flag and its losing branch are pure debt -- dead code paths, doubled test surface, and a fork in every reader's head. This skill removes that debt **mechanically and safely**: an AST-rewrite tool inlines the kept value, constant-folds the dead branch away, deletes the orphaned code, and the result lands in a **PR a human reviews** -- it is **never** merged automatically.

This is the cleanup half of the flag lifecycle. `/feature-flag` creates flags with an expiry and a `temp-` prefix; the `flag-expiry-warn` hook nags at commit time once one is overdue; this skill closes the loop by deleting overdue flags for good.

## When to use

Invoke this skill:
- **Inside `/hygiene-sweep`** (its primary home, Blueprint §J.5 / §K.6) -- the flag-debt step at end-of-phase and every `/update-workflow` runs `/flag-cleanup` to retire everything past expiry.
- **When the `flag-expiry-warn` hook fires** -- a `temp-`/expiring flag is older than its recorded expiry date; clear it now rather than letting it accumulate.
- **When a flag is permanently decided** -- the experiment concluded or the rollout reached 100% and the flag will never flip back. Inline the winning side and delete the flag, even if not yet past expiry (an explicit decision is its own trigger).
- **When the `stale-flag accumulation` anti-pattern is flagged** -- code is drifting two ways behind a flag nobody intends to remove.

## When NOT to use

- **A flag that is still live.** If the experiment is still running, the rollout is still ramping, or the kill-switch must stay available, the flag is doing its job -- leave it. This skill is for flags whose decision is **made and permanent**, not for cleaning up active ones.
- **You cannot prove which side won.** Removing a flag means picking the surviving branch. If you are not certain whether the flag is permanently ON or permanently OFF (check `/feature-flag` state, the rollout %, and the ADR / unit-plan that introduced it), **stop and ask** -- deleting the wrong branch is a behavior change disguised as cleanup. Never guess the kept side.
- **Behavior-change work.** Removing dead branches must be **behavior-preserving** -- the kept side already runs in production today. If "cleanup" would change what users experience, it is not a flag removal; it is a feature change (use the `/feature` flow). Same rule as `/refactor`: if you find yourself changing tests to make them pass, you have crossed from removal into behavior change -- split it.
- **As an auto-merger.** This skill **opens a PR and stops.** It never merges, never bypasses the merge queue, never force-pushes. Merging is a human decision through the server-side ruleset gate (see "The non-negotiable rule").

## The non-negotiable rule: review-gated, never auto-merged

Flag removal deletes code paths across a codebase -- exactly the kind of broad AST rewrite that is usually correct and occasionally, confidently, wrong (the hard-5% drift). So the output is **always a pull request for a human to review**, never a direct merge:

- **Open a PR; never merge it.** The skill's terminal action is `gh pr create`. It does **not** call `gh pr merge`, does **not** push to `main`, does **not** enable auto-merge.
- **The merge queue + server-side ruleset are the real gate.** Per the owner's trunk decision, merge happens server-side through the GitHub ruleset and `merge_group` queue after a human approves -- the same gate as every other change. `/flag-cleanup` produces a queue *entry*; it never *bypasses* the queue.
- **One flag (or one tightly-related flag group) per PR.** A removal PR must be small enough to actually read. Do not batch ten unrelated flag deletions into one diff -- that defeats the review.
- **The PR must be obviously verifiable.** The diff is a removal + the AST tool's constant-folding; the reviewer should be able to confirm "kept the ON side, deleted the OFF side, tests still green" in one pass.

This is the same posture as the `dynamic-skill-quarantine` and `no-spec-no-code` hooks: the AI does the mechanical work, a human holds the gate on anything with broad blast radius.

## Process

### Step 1: Find flags past expiry

Build the list of removal candidates -- do not start from a single flag name unless the user named one.

1. **Read the flag registry.** Source of truth is whatever `/feature-flag` writes: the hobby-tier `flags.json`/env file, or the production-tier OpenFeature/flagd config. Each flag carries a **creation date, an expiry date, and (for temporaries) a `temp-` prefix** (workflow §17.2 / reference/domain-concerns.md §32 flag lifecycle).
2. **Select the overdue + decided ones:**
   - [ ] `expiry < today` (the `flag-expiry-warn` hook's condition), **or**
   - [ ] explicitly marked permanent / rolled out to 100% / experiment concluded.
3. **Establish the kept side for each.** From `/feature-flag` state + the introducing ADR (`Knowledge/<Project>/Decisions/`) + the unit-plan: is the permanent value **ON** or **OFF**? Record it. If ambiguous -> "When NOT to use" applies; skip and surface for a human decision.
4. **One PR per flag.** Process flags one at a time (or one cohesive group). Carry the rest forward to the next cycle within `/hygiene-sweep`'s per-cycle cap.

If there are zero overdue flags, report "no stale flags" and stop -- do not invent cleanup work.

### Step 2: Remove the flag with an AST-rewrite tool

Use a tool that rewrites the **syntax tree**, not text search-and-replace -- so the kept branch is inlined, the dead branch is constant-folded away, and code orphaned by the removal (now-unused methods, imports, helpers) is deleted cleanly. **Polyglot Piranha** is the default; per-language fallbacks below. Every tool is **pinned** (locked dep / tool-manifest entry), never a floating `npx`/`uvx`/`pip install @latest` -- supply-chain rule.

| Stack | Tool (pinned) | Invocation shape | Notes |
|-------|---------------|------------------|-------|
| **Java / Kotlin / Swift / Go / Python / TS·TSX·JS** | **Polyglot Piranha** (`polyglot-piranha`, tree-sitter engine) | run with the flag name + **treated value** (`true`/`false` = kept side) + the language; it inlines the check, folds dead branches, deletes orphaned methods | the cross-language default; one engine, many grammars |
| **Java** | PiranhaJava (legacy, alt) | annotation-driven flag API config | use when the codebase already wires the legacy Piranha API |
| **Kotlin** | PiranhaKotlin (legacy, alt) | as above | |
| **Swift** | PiranhaSwift (legacy, alt) | as above | |
| **JS / TS** | PiranhaJS (legacy, alt) | as above | Polyglot Piranha is preferred for new setups |
| **Other / unsupported grammar** | a `jscodeshift` / `ts-morph` / `libcst` / `gofmt -r` AST codemod, or `comby` (structural, AST-aware) | hand-write a small codemod that inlines the kept value and removes the dead branch | **never** a line-based `sed`/regex edit of control flow -- it silently mangles nesting |

Run the tool scoped to the flag's references, then let it do its job:
- It replaces every `isEnabled("temp-foo")` (or your flag API's call) with the kept boolean.
- It constant-folds: `if (true) { A } else { B }` -> `A`; `if (false) { A } else { B }` -> `B`; ternaries, guard clauses, and early returns collapse the same way.
- It deletes code made dead by the fold -- the losing branch, methods only that branch called, now-unused imports.
- **Then clean up what the tool cannot see:** the flag's own definition/registration in the registry, its config entry, its env var, and any **test fixtures or test cases that existed only to exercise the now-deleted branch** (those tests are dead too -- removing them here is correct and is the one place a flag-cleanup legitimately touches tests).

Review the tool's diff before trusting it. AST codemods are usually right and occasionally fold a branch with a side effect you wanted -- read the diff like a reviewer, not a rubber stamp.

### Step 3: Prove it's behavior-preserving

Removing the flag must not change what ships. Before opening the PR:
- [ ] **The full test suite passes unchanged** -- except the deletion of tests that *only* covered the removed branch (Step 2). If a kept test now fails, you removed the wrong side or broke folding -> stop and fix.
- [ ] **Run `/verify-work`** on the affected flow -- boot the real app, exercise the path that was behind the flag through the real UI/DB, confirm it behaves exactly as the kept side did in production. (Blueprint §I.4.)
- [ ] **No mock crept into the asserted path**, and the flag's removed branch is genuinely unreachable -- grep the codebase for any lingering reference to the flag name (registry, config, env, docs, comments); there must be **zero** left.
- [ ] **Build/lint/typecheck green** -- the AST rewrite can leave an unused import or a now-unreachable `else` the linter will catch.

If you cannot run the app or the suite in this environment, say so explicitly (as `/verify-work` does) and leave the verification to the PR's CI -- but never claim it passed when you didn't run it.

### Step 4: Open the PR (and stop)

1. **Short-lived branch** (trunk-mode: `<24h`), one flag per PR.
2. **Commit** with a conventional message: `chore(flags): remove stale flag temp-foo (expired 2026-05-01, permanently ON)`.
3. **`gh pr create`** with a body the reviewer can check in one pass:
   - which flag, its creation + expiry dates, and **which side was kept and why** (link the ADR / rollout decision);
   - what the AST tool did (inlined kept side, deleted dead branch + N orphaned methods);
   - which tests were deleted (the branch-only ones) and confirmation the rest pass unchanged;
   - `/verify-work` result (or an explicit "deferred to CI" note).
4. **STOP.** Do not merge, do not enable auto-merge, do not push to `main`. The PR enters the normal merge queue for a human to approve. Record the removal so `/hygiene-sweep` and the `flag-manager` agent can mark the flag retired and append a `Knowledge/EVOLUTION.md` row (the flag's lifecycle is now closed).

### Step 5: Report

Emit the report (below) back to the caller (`/hygiene-sweep`, or the user). If more overdue flags remain beyond this cycle's one-PR-per-flag cap, list them as carried-forward.

## Reporting

After running, report explicitly:

```
flag-cleanup report:

Candidates (past expiry / decided permanent): 3
  - temp-new-checkout   expired 2026-05-01, permanently ON  -> processing
  - temp-search-v2      expired 2026-04-15, permanently OFF  -> carried forward (next cycle)
  - temp-dark-mode      still ramping (60%)                  -> SKIPPED (still live)

Processed this run: temp-new-checkout (kept side: ON)
  Tool: Polyglot Piranha (TS/TSX)
  AST rewrite: inlined isEnabled("temp-new-checkout")=true; folded 4 if/else, 1 ternary
  Deleted: old checkout branch, 2 orphaned helpers (legacyCheckout, oldTotal), 1 unused import
  Registry/config: removed flag entry from flags.json + CHECKOUT_V2 env var
  Tests: deleted 3 tests that only covered the OFF branch; remaining suite green (unchanged)
  Lingering references after removal: 0  (grep clean)
  /verify-work: real checkout flow exercised through UI+DB -> behaves as kept side ✓

PR: opened #482 "chore(flags): remove stale flag temp-new-checkout"  -> NOT merged (queued for human review)

Carried forward (over one-PR-per-flag cap / ambiguous): temp-search-v2
Needs human decision (kept side unclear): <none>
```

Never report a flag "removed" without an open PR number, and never report the PR "merged" -- merging is not this skill's action.

## When the agent cannot run the AST tool

Some environments lack the Piranha/codemod toolchain or the language grammar. In that case:
- **Do the removal by hand as a careful, behavior-preserving edit** -- inline the kept value, delete the dead branch, remove orphaned code -- and be *extra* explicit in the PR that this was a manual rewrite needing close review.
- **Never fall back to a line-based `sed`/regex** rewrite of control flow -- it mangles nesting and is exactly the silent-wrong edit this skill exists to avoid. Hand-edit with the AST in your head, or defer the flag to a later cycle when the tool is available.
- **State explicitly** that the AST tool was unavailable and why, and rely harder on `/verify-work` + the human PR review as the backstop.

## Iteration & STOP

- **One bounded pass per flag**, not a loop: find -> establish kept side -> AST-rewrite -> verify behavior-preserving -> open PR -> stop. Inside `/hygiene-sweep`, iterate flag-by-flag within the per-cycle cap; one PR each.
- **STOP** for a flag when its removal PR is open, the suite is green (minus branch-only tests), `/verify-work` confirms the kept behavior, and zero references to the flag remain. The PR being **open and queued for review is the terminal state** -- waiting for merge is not this skill's job.
- **Skip and surface** any flag whose kept side is ambiguous or whose branch removal would change behavior -- those need a human, not another iteration.
- **Re-open** only when new flags pass expiry -- the next `/hygiene-sweep` cycle picks them up.

## See also
- `/feature-flag` -- the lifecycle other half: creates flags with the expiry + `temp-` prefix this skill consumes; hobby = `flags.json`/env, production = OpenFeature + self-hosted flagd.
- `/hygiene-sweep` -- this skill's primary caller; runs `/flag-cleanup` at its flag-debt step (end-of-phase + every `/update-workflow`) within the per-cycle cap.
- `agents/flag-manager.md` -- owns flag creation/tracking/expiry and DoD enforcement; marks the flag retired and logs `Knowledge/EVOLUTION.md` after the PR opens.
- `/refactor` -- the behavior-preserving sibling; same "if you're changing tests, it's not cleanup" bar. Flag removal is a specialized, AST-driven refactor.
- `/verify-work` -- Step 3's behavior-preserving proof through the real stack (Blueprint §I.4).
- `/trunk-merge`, `/rollback` -- the trunk-mode delivery context; the PR this skill opens flows through the same merge queue + revert-first rollback.
- `/db-migrate` -- the parallel "broad, irreversible-looking change, do it safely" skill (expand/contract); flags + expand/contract are the two pillars of safe trunk-based delivery.
- [[../Agent Workflow]] -- §3 (trunk delivery + flags), §9.4 DoD (flags created have expiry), §17.2 / reference/domain-concerns.md §32 (flag lifecycle), Blueprint §J.5 / §K.6 (review-gated cleanup inside hygiene).
