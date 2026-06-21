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
- [ ] No new feature flags
- [ ] PR description states: "This is a pure refactor. Tests pass without modification. Behavior is preserved."

### Step 7: Merge strategy

- **Small refactors** (1-3 commits, single area): merge normally via PR
- **Large refactors** (many commits, broad surface): consider merging in stages -- ship the first half, let it bake on `main` for a day or two, then ship the second half. Reduces review burden and risk of broad merge conflicts.
- **High-risk refactors** (touching auth, payments, data layer): pair with `/security-review` and `/ultrareview` before merging.

## Definition of Done (Refactor variant)

A refactor is done when:

- [ ] Behavior preserved -- all existing tests pass with no modifications
- [ ] No new tests required (a refactor doesn't change behavior, so it doesn't need new tests for new behavior)
- [ ] If characterization tests were added in Step 1, they are committed and remain green
- [ ] Code-quality dimensions (readability, modularity, separation of concerns) measurably improved
- [ ] Reviewers can read the diff in one sitting (split if too large)
- [ ] Commit history tells a clean story (one logical step per commit)
- [ ] CLAUDE.md updated only if a project convention changed (e.g., "we now organize feature folders by domain, not by layer")
- [ ] Memory updated if the refactor revealed a long-standing pain point worth remembering

## Anti-patterns specific to refactors

- **Refactor + behavior change in one PR**: split them. Refactor first (no test changes), behavior change second (with test changes).
- **Refactor + dependency upgrade in one PR**: split them. The diff blames different signals.
- **Refactor + lint config change in one PR**: same -- split. Otherwise you can't tell whether a change was forced by the lint rule or by the refactor.
- **Renaming for personal taste**: if the rename doesn't make the next reader's job clearly easier, skip it.
- **Extracting a helper used in two places**: usually premature. Wait for the third.
- **Refactoring untested code**: see Step 1. Add characterization tests first or skip the refactor.

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
