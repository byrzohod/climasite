---
name: refactor
description: Behavior-preserving code restructuring with strict no-test-change rule -- rename, extract, simplify, decompose, deduplicate without altering observable behavior. Use this whenever the user mentions refactoring, restructuring, cleanup, simplifying, extracting helpers, deduplication, renaming, breaking up large files/functions, or wants to improve code structure/readability without changing functionality. All tests must still pass unchanged.
---

# /refactor - Behavior-preserving code restructuring

## When to use

Invoke this skill when the goal is to **restructure code without changing behavior**:

- Renaming for clarity (variables, functions, files, modules)
- Extracting a shared helper from duplicated code
- Splitting a too-large file/function/class into smaller pieces
- Replacing one pattern with another (e.g., callbacks -> async/await)
- Moving code between modules to improve cohesion
- Removing dead code
- Simplifying logic while preserving observable outputs
- Tightening types

A refactor is **not**:
- A bug fix (use `/feature` flow or a `fix:` commit)
- A new feature (use `/feature` flow)
- A behavior change disguised as cleanup (split it: refactor first, behavior change second)

If the change alters what the code does -- even subtly -- it is not a refactor. The non-negotiable test of a refactor: **all existing tests must still pass with zero modification.** If you find yourself changing tests, the change has crossed from refactor into behavior change. Stop, split the work, do them in separate commits.

## The Refactor Bargain

A refactor pays for itself by making the next change easier. The benefit must justify:

- The diff that reviewers have to read
- The merge conflict surface for in-flight branches
- The risk of accidental behavior change
- The git-blame noise it creates

Refactors with no clear next-change benefit ("I just thought it was cleaner") often add cost without payoff. When in doubt, leave it alone or pair the refactor with the next concrete feature work that benefits from it.

## Process

### Step 1: Capture the baseline

Before touching code:

```bash
# Confirm clean working tree
git status   # should show nothing uncommitted

# Run the full test suite -- it must be green NOW
npm test          # or dotnet test, pytest, go test ./..., etc.

# Run the linter and type checker -- must be clean
npm run lint && npm run typecheck
```

If anything is red, fix it FIRST in a separate commit. You cannot refactor on a broken baseline -- you'll never know whether a failure was pre-existing or caused by your refactor.

If the project lacks tests in the area being refactored, that is a precondition not satisfied. Either (a) add characterization tests first (in a separate commit) that lock in current behavior, or (b) abort the refactor until tests exist. **Never refactor untested code.** You cannot prove behavior was preserved without tests.

### Step 2: Plan the refactor

Use Claude Code's plan mode for non-trivial refactors. The plan should state:

- **What is changing** (the file/function/module/pattern)
- **What stays the same** (the public API, the observable behavior, the test results)
- **Why** (what next change does this enable, or what specific problem does it solve)
- **Scope boundary** (files in scope, files explicitly NOT touched)
- **Verification plan** (which tests prove behavior is preserved)
- **Rollback plan** (if review reveals a problem, can this be reverted cleanly?)

### Step 3: Identify all affected files

Refactors are usually cross-cutting (rename a function -> all callers change). Use parallel `Explore` subagents (workflow section 10.2) to find:

- All callers of any function you're renaming or moving
- All importers of any module you're restructuring
- All references in tests
- All references in docs and ADRs
- All references in config (env vars, feature flags, build scripts)

Missing a single caller turns a refactor into a behavior change.

### Step 4: Make the change in small commits

A refactor is a sequence of small, reversible steps -- each commit by itself a valid state of the code:

- One commit per logical step (rename, move, extract, simplify)
- Each commit must build and pass tests on its own
- Use `refactor:` as the conventional commit prefix
- Keep the diff per commit small enough to review in 5 minutes

If the only way to make the refactor work is one giant commit, that is a sign the refactor is too ambitious -- split it into smaller refactors first.

### Step 5: Verify behavior preservation

After every commit:

```bash
# Same tests as Step 1, must still pass with zero modifications
npm test
npm run lint && npm run typecheck

# Build the production artifact
npm run build
```

Then run `/verify-work` for any user-facing surface affected by the refactor. The point is to confirm the refactor changed structure only; behavior is intact.

If a test required modification to pass, you have introduced a behavior change. Investigate:
- If the test was wrong before and right now -> that is a bug fix; commit it as `fix:` separately.
- If the test was right before and wrong now -> revert the offending change, the refactor is broken.

### Step 6: Pre-merge checks

- [ ] All tests pass with no test modifications
- [ ] Linter and typechecker clean
- [ ] Production build succeeds
- [ ] `/verify-work` passes for affected surfaces
- [ ] `/code-review` (in-session `/review` at minimum) finds no behavior changes
- [ ] Commit messages all use `refactor:` prefix and describe what was restructured
- [ ] No new dependencies added (refactors should not introduce libraries)
- [ ] No new feature flags -- **except** a temporary migration-seam flag for branch-by-abstraction (see that section); if present, it must default OFF and have a removal step planned
- [ ] PR description states: "This is a pure refactor. Tests pass without modification. Behavior is preserved." (For a branch-by-abstraction step: also note which step it is and that `main` stays shippable.)

### Step 7: Merge strategy

- **Small refactors** (1-3 commits, single area): merge normally via PR
- **Large refactors** (many commits, broad surface): do NOT accumulate them on a long-lived branch. We are trunk-based everywhere -- branches live <24h and merge through the merge queue as soon as they're green. A multi-day refactor must be decomposed so each step is independently mergeable to `main`. Use **branch-by-abstraction** (next section) to land the steps incrementally behind a stable seam.
- **High-risk refactors** (touching auth, payments, data layer): pair with `/security-review` and `/ultrareview` before merging.

## Branch by abstraction (large refactors on trunk)

A large refactor -- swapping an implementation, restructuring a subsystem, migrating a layer -- cannot be a single reviewable commit, and it must NOT become a weeks-long feature branch. We are trunk-based: every step merges to `main` within a day. **Branch by abstraction** is how a big structural change lands as a sequence of small, trunk-merged steps while `main` stays green and shippable the entire time. ("Branch" here means a layer of indirection in the code -- a seam -- not a VCS branch.)

The shape of it:

1. **Introduce a seam.** Add a stable abstraction (interface, adapter, façade, function boundary) in front of the code you're going to change. Route all existing callers through it. This step preserves behavior exactly -- it's a pure refactor, all tests pass unchanged. Commit and merge.
2. **Build the new implementation behind the seam.** Add the new code path alongside the old one. It is not yet wired in (or is gated -- see below), so behavior on `main` is unchanged and tests stay green. Land it in as many small trunk merges as the work needs.
3. **Switch over incrementally.** Move callers from the old implementation to the new one through the seam -- one caller, one module, or one slice at a time. Each switch is small, independently reviewable, and independently revertible. Behavior is preserved at every step, so tests still pass unchanged.
4. **Remove the old implementation and (usually) the seam.** Once nothing uses the old path, delete it. If the abstraction was only scaffolding for the migration, remove it too so it doesn't become permanent indirection debt. Each deletion is its own small commit.

### Gating incomplete work with feature flags

When the new implementation can't be finished within a single <24h step, the partial work must still merge to `main` without changing behavior. Gate it behind a **feature flag** (default OFF) so the new path is dormant in production while its code lives on trunk:

- The flag selects old-vs-new *at the seam* -- the rest of the codebase never sees the flag.
- `main` stays shippable at every merge: flag OFF == current behavior, exercised by the existing (unchanged) tests.
- Add tests for the new path that force the flag ON (these are *new tests for the new path*, not modifications of existing tests -- the no-test-change rule below still holds for the behavior you're preserving).
- Flip the flag to ON only after the new path is verified (`/verify-work`), let it bake, then remove the flag and the dead old path as cleanup commits.

This is the one sanctioned exception to "refactors add no new feature flags" in the pre-merge checklist: a flag introduced *solely* as a temporary migration seam for branch-by-abstraction. It is scaffolding, and removing it is part of the Definition of Done -- it must not outlive the migration.

### Why this fits trunk-based development

- **No long-lived branch, no big-bang merge.** The integration happens continuously on `main` instead of as one terrifying merge at the end -- which is exactly the merge-conflict and risk surface the Refactor Bargain warns about.
- **Reviewable steps.** Each commit is small enough to read in one sitting (Definition of Done), because the seam lets you change one thing at a time.
- **Always revertible.** If a switch-over step regresses, revert that one small commit; the seam and the old path are still there. With a flag, you flip OFF instead of reverting.
- **`main` is always green and shippable**, satisfying the merge-queue gate.

### Worked sketch

Replacing a hand-rolled date parser with a library, trunk-based:

1. `refactor:` extract `parseDate(input)` seam; every call site goes through it; implementation is still the old hand-rolled code. Tests unchanged, merge.
2. `refactor:` add `parseDateV2(input)` (library-backed) behind a `DATE_PARSER_V2` flag inside `parseDate`; flag OFF. Tests unchanged; add tests exercising V2 with the flag ON. Merge.
3. Bake, `/verify-work`, flip `DATE_PARSER_V2` ON in production.
4. `refactor:` delete the old branch inside `parseDate`, remove the `DATE_PARSER_V2` flag, inline `parseDateV2`. Tests still pass unchanged. Merge.

Each numbered step is a separate sub-24h trunk merge; `main` is shippable throughout.

## Definition of Done (Refactor variant)

A refactor is done when:

- [ ] Behavior preserved -- all existing tests pass with no modifications
- [ ] No new tests required (a refactor doesn't change behavior, so it doesn't need new tests for new behavior)
- [ ] If characterization tests were added in Step 1, they are committed and remain green
- [ ] Code-quality dimensions (readability, modularity, separation of concerns) measurably improved
- [ ] Reviewers can read the diff in one sitting (split if too large)
- [ ] Commit history tells a clean story (one logical step per commit)
- [ ] If branch-by-abstraction was used: the old implementation is deleted, and any temporary migration-seam flag and scaffolding abstraction are removed (they must not outlive the migration)
- [ ] CLAUDE.md updated only if a project convention changed (e.g., "we now organize feature folders by domain, not by layer")
- [ ] Memory updated if the refactor revealed a long-standing pain point worth remembering

## Anti-patterns specific to refactors

- **Refactor + behavior change in one PR**: split them. Refactor first (no test changes), behavior change second (with test changes).
- **Refactor + dependency upgrade in one PR**: split them. The diff blames different signals.
- **Refactor + lint config change in one PR**: same -- split. Otherwise you can't tell whether a change was forced by the lint rule or by the refactor.
- **Renaming for personal taste**: if the rename doesn't make the next reader's job clearly easier, skip it.
- **Extracting a helper used in two places**: usually premature. Wait for the third.
- **Refactoring untested code**: see Step 1. Add characterization tests first or skip the refactor.
- **Editing existing tests to make a branch-by-abstraction switch-over pass**: forbidden. Switching a caller from the old to the new path through the seam must be behavior-preserving, so the existing tests pass *unchanged*. New tests may be added for the new path (e.g., with the migration flag ON), but no existing test may be modified. If one needs modifying, the switch changed behavior -- that's a separate `fix:`/`feat:`, not part of the refactor.
- **Leaving the migration seam or flag in place "for later"**: the temporary abstraction and flag are scaffolding. Removing them is part of Definition of Done. Permanent indirection added "just in case" is its own anti-pattern.

## When to abandon a refactor mid-flight

Stop if any of these become clear:

- The scope keeps growing -- you're touching files you didn't expect, the diff is becoming unreviewable
- You can't prove behavior is preserved (test failures appear that aren't pre-existing)
- The "improved" structure is no clearer than the original; you're moving code around for its own sake
- A new feature requirement comes in that will affect this code -- finish or revert the refactor first, then do the feature

Reverting a half-done refactor is cheap. Merging a confused refactor is expensive.

## Integration with other skills

- Before refactor: `/code-review` to identify the highest-leverage cleanup targets
- During refactor: parallel `Explore` subagents to find all affected callers
- After refactor: `/verify-work` for affected user-facing surfaces
- Pre-merge: `/code-review` (which now includes `/review` + suggesting `/ultrareview` to the user)
