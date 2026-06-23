---
name: mutation-check
description: Prove the tests are meaningful, not vacuous -- run diff-scoped mutation testing (>=75% mutants killed on changed files) AND the mandatory break-the-code self-check (inject a representative bug, prove the targeted tests FAIL, revert, record evidence). Use this whenever the user mentions mutation testing, Stryker, mutmut, cosmic-ray, cargo-mutants, go-mutesting, PIT, surviving mutants, mutation score, kill rate, break-the-code, test meaningfulness, vacuous/trivial/tautological tests, "do these tests actually catch bugs", "are my tests any good", false-positive tests, after unit tests go green and before code review, or when a test claim is disputed and must be proven before it is trusted.
---

# /mutation-check - Prove the tests are meaningful (mutation + break-the-code)

## When to use

Invoke this skill on a unit **after its unit tests pass and before review** (workflow §4.5 / Blueprint §I.3):

- **After unit tests go green, before `/code-review`** -- a green suite proves the tests *ran*, not that they *mean* anything. This is the gate between "tests pass" and "send to review".
- **Before claiming any unit "done"** -- the break-the-code self-check is mandatory at the Definition of Done; a unit without recorded break-the-code evidence is not done.
- **When a test claim is disputed** -- the `verifier` agent re-runs the break-the-code probe on DISPUTED test claims (`agents/verifier.md`); this skill is the proactive, developer-run version of that same check.
- **When mutation score regresses** in CI -- triage the surviving mutants the diff-scoped gate reports.

Coverage and a green suite are necessary, not sufficient. A test suite can be 100% green and 95% covered while testing nothing -- assertions on the wrong thing, `toBeDefined`-only checks, tautologies, or tests that pass whether the code is right or wrong. This skill catches that **"hard-5% drift"**: confident, plausible, subtly-wrong tests. There is **zero tolerance for vacuous tests**.

There are two complementary checks, and you run **both**:
1. **Mutation testing (automated, gated):** a tool mutates the changed source (flip `>` to `>=`, `&&` to `||`, delete a statement, swap a boundary) and re-runs the tests. A mutant that *survives* (tests still pass with the bug in place) is a hole in the suite. Gate: **>=75% killed on changed files.**
2. **Break-the-code self-check (manual, mandatory):** you inject one representative bug by hand, prove the *targeted* tests FAIL, then revert and record the evidence. This is mutation testing in miniature, aimed at the specific behavior the unit was built to guarantee -- it catches the mutant the tool's operators don't generate.

## When NOT to use

- **Before unit tests pass.** Mutation testing on a red suite is noise. Get to green first (`/test-plan` designed the suite, `qa` agent wrote it), *then* prove the green means something.
- **On e2e / UI files.** Mutation and the patch-coverage gate explicitly exclude e2e/UI (line-instrumenting full-system tests is meaningless, Blueprint §I.1). Meaningfulness of e2e/UI is enforced by the behavior/flow checklist in `/verify-work`, not by a kill rate. Scope mutation to unit + integration source.
- **On generated code, vendored code, migrations, or pure config** -- nothing to assert behavior about. Exclude these from the mutation scope.
- **As a substitute for `/verify-work`.** Mutation proves the *tests* are sharp; `/verify-work` proves the *feature* actually behaves through the real stack. Both run; neither replaces the other.
- **As a substitute for writing tests.** Surviving mutants tell you *where* tests are missing; they don't write them. Fix the suite (`qa` agent), then re-run.

## Process

### Step 1: Confirm preconditions

- [ ] The unit's **unit tests pass** (and integration, if this unit has them). Red suite -> stop, fix first.
- [ ] You know the **changed files** for this unit (`git diff --name-only --diff-filter=AM "origin/<base>...HEAD"`), excluding test files, e2e/UI, generated code, and migrations.
- [ ] The mutation tool for this stack is **installed as a pinned dependency** (never a floating `npx`/`uvx`/`go install @latest` -- supply-chain rule). See the per-stack table below.

### Step 2: Run diff-scoped mutation testing

Run the stack's mutation tool **scoped to the changed source files only** (full-module runs are nightly and non-blocking; the per-unit/PR gate is the diff). The gate is **>=75% mutants killed on changed files** -- one fixed number, **uniform across every project and tier (no hobby-tier relaxation)**.

| Stack | Tool (pinned) | Diff-scoped command | Gate |
|-------|---------------|---------------------|------|
| **JS / TS** | Stryker | `npx stryker run --since=origin/<base>` with `"thresholds": { "break": 75 }` in `stryker.conf` (exits non-zero below 75% on analyzed files) | break at 75 |
| **.NET / C#** | Stryker.NET (pinned in `.config/dotnet-tools.json`, `dotnet tool restore`) | `dotnet stryker --since:origin/<base> --break-at 75` | `--break-at 75` |
| **Python** | mutmut (alt: cosmic-ray) | mutate only changed `*.py` source: `mutmut run --paths-to-mutate "<changed,files>"`, then parse `mutmut results` and **fail if killed/(killed+survived) < 75%** (mutmut has no native gate -- compute the kill-rate, see `ci/python.yml`) | scripted >=75% |
| **Rust** | cargo-mutants | `cargo mutants --in-diff <(git diff origin/<base>)` (scopes mutants to the diff); gate on the surviving count | >=75% killed |
| **Go** | go-mutesting (alt: ooze) | map changed `*.go` files -> package dirs, run `go-mutesting <pkgs>`, parse "mutation score is X" and **fail if X*100 < 75** (no native gate -- compute it, see `ci/go.yml`) | scripted >=75% |
| **JVM** | PIT | `pitest` with `withHistory` + changed-class targeting; gate on mutation coverage | >=75% killed |

Notes:
- **Diff-scoped, not whole-repo.** Whole-repo mutation is minutes-to-hours; the diff keeps it inside the <10 min PR budget. Full-module runs over 1-2 critical modules are a separate **nightly, non-blocking** job (already wired in the `ci/` templates).
- **No mutants generated = pass, but suspicious.** If a change is pure config/types with no mutatable logic, an empty mutant set is a legitimate pass. If you expected logic and got zero mutants, your scope is wrong -- fix the file list.
- **Pin the tool.** Stryker/Stryker.NET/mutmut/cargo-mutants/go-mutesting must be locked dev-deps or tool-manifest entries. No floating installs.

### Step 3: Triage surviving mutants

Every surviving mutant is a behavior your tests do **not** pin down. For each one, classify and act:

- **Genuine gap (the common case):** the mutant is a real bug your tests would miss. **Add or strengthen a test** so the mutant dies (delegate to the `qa` agent). Re-run. Do not weaken the gate to make it pass.
- **Equivalent mutant (rare, must be justified):** the mutation produces code that is provably behaviorally identical (e.g., a redundant bound that can't be reached). Document *why* it's equivalent in the unit-plan / PR; only then exclude it. Treat "equivalent" claims with suspicion -- most "equivalent" mutants are actually untested behavior.
- **Out-of-scope mutant:** it landed in generated code, a migration, or vendored code that slipped into scope. Fix the scope (exclude the path), don't fix a test.

Re-run mutation after fixes until the changed files clear **>=75% killed**. Record the final score and any justified equivalents.

### Step 4: Break-the-code self-check (mandatory)

This is the human-judgment half and it is **non-negotiable** -- it catches the subtly-wrong test the mutation operators never generate, and it is required evidence for the Definition of Done.

1. **Pick a representative bug** for the unit's core guarantee -- the single behavior this unit exists to provide (e.g., for a price calculator, make the discount add instead of subtract; for an auth check, return `true` on the deny path; for a validator, accept the invalid input). Pick a *meaningful* break, not a typo.
2. **Confirm the suite is green and the tree is clean** first (`git status` clean, tests pass). You need a known-good baseline to revert to.
3. **Inject the bug** into the code under test (edit the source, not the test).
4. **Re-run the targeted tests** for that unit.
   - Tests **FAIL** -> the suite actually exercises the behavior. Good. Note *which* test failed.
   - Tests **PASS** -> the tests do **not** exercise the behavior. This is a vacuous/false-confidence test. The unit is **not done**: add the missing assertion (`qa` agent), then repeat this step.
5. **Revert exactly** (`git checkout -- <file>` or revert the edit), then confirm the tests pass again **and the working tree is clean** (`git status` shows no leftover diff). Leave nothing behind -- the break is a read-only probe in spirit: inject, observe, revert.
6. **Record the evidence:** the bug injected, the test(s) that caught it, and confirmation of clean revert. Attach to the unit-plan / PR (the Stop hook warns if tests were added without break-the-code evidence).

Do this for **each distinct core guarantee** of the unit, not just one global break. A unit that does three things needs three breaks.

### Step 5: Report

Emit the report (below). Then proceed to `/code-review`. A unit does not enter review until both halves are green: mutation **>=75% killed on changed files** AND every core guarantee has a passing break-the-code probe with recorded evidence.

## Reporting

After running, report explicitly:

```
Mutation + break-the-code report for <unit name>:

Changed files in scope: <list> (excluded: tests, e2e/UI, generated, migrations)

== Mutation (diff-scoped) ==
Tool: <Stryker / mutmut / cargo-mutants / go-mutesting / ...>
Score on changed files: 84% killed (38/45)  [gate >=75% -> PASS]
Surviving mutants: 7 -> triaged:
  - 5 genuine gaps  -> tests added, re-run, now killed
  - 2 equivalent    -> justified: <one line each on why behaviorally identical>

== Break-the-code (per core guarantee) ==
Guarantee 1 "<discount is subtracted>": injected add-instead-of-subtract
  -> test `applies 10% discount` FAILED as expected -> reverted, tree clean ✓
Guarantee 2 "<rejects negative qty>": injected accept-negative
  -> test `rejects negative quantity` FAILED as expected -> reverted, tree clean ✓

Vacuous tests found and fixed: <none / list -- the test that passed when code was broken>

Verdict: PASS -- both halves green, evidence attached. Cleared for /code-review.
```

If a verdict is **FAIL**, name exactly which mutant survived or which break-the-code probe passed-when-it-should-have-failed, and what was done (or is still needed) to fix it. Never report PASS on the strength of a green run alone -- a green test proves nothing until you have seen it go red for the right reason.

## When the agent cannot run mutation

Some environments lack the mutation toolchain or the history depth (`fetch-depth: 0`) the diff scope needs. In those cases:

- **The break-the-code self-check still runs** -- it needs zero new deps and seconds to run. It is never optional and never blocked by environment.
- **State explicitly** that automated mutation was skipped and *why* (no tool / no history / sandbox), exactly as `/verify-work` reports verification gaps -- never claim a kill rate you didn't measure.
- **Defer the automated gate to CI** -- the `ci/` templates run diff-scoped mutation on the PR; flag in the PR that local mutation was not run so the CI gate is the backstop.

## Iteration & STOP

- This is a **bounded pass per unit**, not a loop: scope -> mutate -> triage -> break-the-code -> report. The iteration is internal (re-run mutation after each test fix until the changed files clear 75%); once green, **stop** -- do not chase 100% kill rate or gold-plate mutants on unchanged code.
- **STOP** when: the changed files clear **>=75% killed** (with any equivalents justified in writing), **every** core guarantee of the unit has a break-the-code probe that failed for the right reason and was cleanly reverted, and the evidence is recorded. A surviving genuine mutant or a break-the-code probe that *passed* means NOT done.
- **Re-open** only when the unit's code changes again -- then re-run on the *new* diff, not the whole module.
- The kill-rate threshold is **fixed at 75% on changed files for every project and tier** -- do not lower it for "small" or "hobby" units. Quality is never the variable.

## See also

- `/test-plan` -- designs this unit's test suite *before* code (levels, exact assertions, edge-case matrix, **mutation target**, and the break-the-code bug list this skill executes). `/mutation-check` is the enforcement of what `/test-plan` specified.
- `/verify-work` -- the feature-reality counterpart; it attaches the same break-the-code evidence and confirms real behavior through the real DB/UI. Both run before review; neither replaces the other.
- `/code-review` -- runs immediately after this skill passes; a unit does not enter review with surviving genuine mutants or missing break-the-code evidence.
- `agents/qa.md` -- writes/strengthens the tests that kill surviving mutants ("if a test wouldn't fail when the feature breaks, it's not a test").
- `agents/verifier.md` -- re-runs the break-the-code probe on DISPUTED test claims (CONFIRMED/DISPUTED/UNVERIFIABLE); this skill is its proactive, developer-run twin.
- `templates/ci/{node,dotnet,python,go}.yml` -- all four enforce the same >=75% diff-scoped gate in the PR stage plus a non-blocking nightly full run, but the **job names differ per stack**: node/go/dotnet name them `fast-mutation` (diff-scoped, gating) + `nightly-mutation` (full, non-blocking); python names them `mutation-diff` (diff-scoped, gating) + `mutation-full` (full, non-blocking).
- [[../Agent Workflow]] -- §4.5 (Test Reality Safeguards: break-the-code + mutation + assertion-density), §I.1 (per-level coverage rule), §I.3 (test-reality safeguards), §9.4 DoD (break-the-code evidence attached).
