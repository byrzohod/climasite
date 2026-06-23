---
name: trunk-merge
description: Drive one unit through the trunk-based delivery loop end to end -- cut a short-lived branch (<24h), push, open a PR, run multi-angle review (review-orchestrate, <=2 rounds then human), enter the GitHub merge queue, auto-merge on green, delete the branch. The server-side ruleset + merge queue (merge_group) is the real gate; merge happens server-side after the queue re-runs the full suite against future-main. Use this whenever the user mentions trunk-based development, short-lived branches, merging a unit, opening/merging a PR, the merge queue, merge_group, auto-merge, "merge ASAP when green", merging incomplete work behind a flag, branches living under a day, ruleset/branch-protection as the gate, or asks how to get a finished unit onto main. NOT for the migration itself (/db-migrate), not for reverting bad main (/rollback), not for the review content (review-orchestrate).
---

# /trunk-merge - Drive the trunk delivery loop (branch -> PR -> queue -> auto-merge -> delete)

The runner for **trunk-based delivery** (Blueprint §J, `Agent Workflow.md` §3). It moves **one finished unit** from a clean working tree onto `main` through a short-lived branch and the **GitHub merge queue**, with multi-angle review in between, and tears the branch down after. It is the orchestration glue that makes "merge ASAP when green" actually runnable without losing a gate.

**The one load-bearing fact:** on trunk-mode the **real gate is server-side** -- the GitHub **ruleset** (`templates/ruleset.json`, applied by `/project-setup` via `gh api`) requires a PR, requires the merge queue, requires every queue entry to pass the required checks, and enforces linear history. The merge **happens in the queue** (`merge_group` event), which re-runs the **full real-infra suite against future-main** and merges server-side only when green. The local push-to-main block in `settings.json` is **defense-in-depth, not the gate** -- it never impedes the queue, which merges server-side as the ruleset's deputy. **Never bypass the queue** (no admin-merge, no `--admin`, no disabling the ruleset to "just get it in").

**Branches live <24h.** A branch open longer than a day is a smell: the unit was too big (split it), or it is blocked (merge what is done behind a default-off flag and open a fresh branch for the rest). Long-lived branches are the integration debt trunk-based delivery exists to kill. **Incomplete units merge to green trunk behind a default-off feature flag** (`/feature-flag`) -- you do not hold a branch open waiting for a unit to be "fully done".

This skill **drives** the loop; it does not own the pieces it calls. Review content is `/review-orchestrate`; flags are `/feature-flag`; reverting a bad merge is `/rollback`; the migration mechanics are `/db-migrate`; the actual checks live in `templates/ci/*.yml`.

## When to use

Invoke this skill:
- **When a unit is done and proven** -- tests green, `/mutation-check` passed (>=75% killed + break-the-code evidence), `/verify-work` confirmed real behavior. This is the step that gets it onto `main`.
- **When merging an incomplete unit dark** -- the unit is partial but you want it integrated on green trunk behind a **default-off flag** rather than rotting on a long branch.
- **When the user says** "merge this", "open a PR and merge when green", "get this into main", "put it in the merge queue", "merge ASAP", on a trunk-mode project.
- **As the per-unit closer in `/plan-tree`'s loop** -- each unit ends: code -> tests -> mutation-check -> verify-work -> **trunk-merge** -> (knowledge capture) -> next unit.

## When NOT to use

- **Before the unit is proven.** A unit enters this loop only **after** `/mutation-check` and `/verify-work` pass. This skill does not write code, tests, or verify behavior -- it ships an already-green, already-verified unit. Red suite or missing break-the-code evidence -> stop, finish that first.
- **To revert a bad merge on `main`.** That is `/rollback` -- **revert-first beats hotfix** on trunk (Blueprint §J.5 / §C-1). Do not open a "fix-forward" branch for a regression you can revert in one click.
- **To run the review.** The review *content* (correctness/security/perf/arch/best-practices/a11y/SEO angles, the <=2-round cap) is `/review-orchestrate`. This skill *calls* it and gates on its verdict.
- **To author the migration or the flag.** Migrations -> `/db-migrate` (expand/contract mandatory, never a destructive schema change in the same PR as the code using it, §J.5 / §C-2). Flags -> `/feature-flag`. This skill only *checks* that they were done right before queueing.
- **To bypass the queue under time pressure.** There is no "just admin-merge it" path. If the queue is red, the unit is not ready -- fix it or revert the offending entry.

## The loop at a glance

```
clean tree, unit proven (mutation-check + verify-work green)
   |
[1] preflight gate (rebased on main? flag? migration? secrets? <24h scope?)
   |
[2] cut short-lived branch  (feature|fix|chore/<slug>, advisory naming)
   |
[3] commit (conventional) + push -> set upstream
   |
[4] open PR  (gh pr create; title=conventional, body=plan link + DoD + flag + break-the-code evidence)
   |
[5] FAST PR stage runs (lint/typecheck/unit+patch-cov/assertion-density/diff-mutation/secret/SAST/chromium smoke, <10min)  --> gates the BUTTON
   |
[6] /review-orchestrate  (multi-angle; resolve; re-review <=2 rounds; then HUMAN decides)  <-- can't stall forever
   |
[7] enter merge queue   (gh pr merge --auto --squash)  --> queue re-runs FULL suite (merge_group) against future-main
   |
[8] auto-merge on green (server-side, ruleset is the gate)   OR  queue ejects on red -> fix or /rollback
   |
[9] delete branch + pull main + sync local      (branch lifespan < 24h)
   |
-> /kb-capture (record decisions), then next unit
```

## Process

### Step 1 - Preflight gate (do not skip; this is what keeps the queue green)

Before cutting the branch, confirm **all** of these. Each one is a thing that, skipped, ejects you from the queue or breaks `main` for everyone:

- [ ] **Unit is proven, not just written:** `/mutation-check` PASS (>=75% killed on changed files + break-the-code evidence recorded) AND `/verify-work` PASS (real behavior through real DB/UI, no mocks in the asserted path). Evidence is in hand to paste into the PR body.
- [ ] **Working tree is clean and current main is fetched:** `git fetch origin && git status` clean. You will branch from up-to-date `origin/main` (linear history is required by the ruleset; start fresh, not from a stale local main).
- [ ] **Scope fits <24h.** If this unit cannot realistically merge within a day, **split it** (one unit per branch) or plan to **merge the done part behind a default-off flag** and continue the rest on a new branch. Do not plan to hold a branch open.
- [ ] **Incomplete work is flag-gated.** If any part of this unit is not finished/safe to be live, it is behind a **default-off** flag (`/feature-flag`), the flag has a **creation date + expiry + `temp-` prefix**, and its **removal is a DoD item**. Half-finished code merges **dark**, never live.
- [ ] **Migrations are safe for trunk.** If this unit touches the schema: it uses **expand/contract** (`/db-migrate`), and there is **no destructive schema change in the same PR as the code using it** (§C-2). The additive migration ships first; the contract step is a later, separate PR.
- [ ] **No secrets in the diff.** `git diff --staged` reviewed; the FAST stage runs gitleaks but you do not rely on it to catch your own paste.

If any box fails, fix it **before** branching. The cheapest place to catch a queue ejection is here.

### Step 2 - Cut the short-lived branch

Branch from fresh `origin/main`:

```bash
git fetch origin
git switch -c <type>/<slug> origin/main     # type in feature|fix|hotfix|chore
```

Naming (`feature|fix|chore/<short-slug>`) is **advisory on trunk-mode, not blocking** -- `settings.json`'s branch-name hook only *suggests* it (it was softened from a hard block in the trunk flip; Blueprint §E). Keep the slug short and tied to the unit. The branch exists to carry **this one unit's PR through the queue and then die.**

### Step 3 - Commit and push

Commit with a **conventional-commit** message (the `settings.json` commit-msg hook enforces the format; the type drives `/release` semver later):

```bash
git add <changed files>          # the unit's src + tests; nothing stray
git commit -m "feat(<scope>): <imperative summary>"   # or fix/chore/refactor/perf/docs/test
git push -u origin <type>/<slug>
```

One unit -> ideally one focused PR. Keep the diff reviewable -- a smaller PR clears the queue faster and is safer to revert.

### Step 4 - Open the PR

```bash
gh pr create \
  --base main \
  --title "feat(<scope>): <imperative summary>" \
  --body  "<see required body below>"
```

The PR body is the unit's evidence packet -- it is what the human approver and your future self read. Include:
- **Link to the `unit-plan.md`** (the approved plan this implements; the `no-spec-no-code` hook required it before code).
- **Definition of Done checklist** satisfied (tests, patch coverage, mutation, verify-work) -- mirror `Agent Workflow.md` §9.4.
- **Break-the-code evidence** (which bug was injected per core guarantee, which test went red) -- the Stop hook warns if tests were added without it; reviewers and the queue trust this.
- **Flag note:** which flag gates this unit, its default state (must be **off** if incomplete), and its expiry/removal-DoD line.
- **Migration note:** expand/contract phase, and confirmation no destructive change rides with the code using it.
- **Knowledge link:** the `Knowledge/<Project>/` ADR(s)/decisions this unit changed or will record (so `/kb-capture` is not forgotten).

### Step 5 - Let the FAST PR stage gate the button

Opening the PR triggers the **FAST tier** (`templates/ci/*.yml`, `pull_request: branches:[main]`): lint+typecheck, unit + **patch-coverage gate** (>=90% line/>=85% branch on changed), assertion-density lint, **diff-scoped mutation (>=75%)**, gitleaks, Semgrep, **chromium-only smoke e2e** -- target **<10 min**. This stage **gates the merge button**: you cannot queue until it is green.

- **Red FAST stage -> fix on the branch and re-push.** Do not request review or queue on red. (Coverage/mutation failures mean the unit was not actually proven in Step 1 -- back up.)
- This is deliberately *fast*; the heavy real-infra suite runs later **in the queue**, not here (that is the latency resolution, §J.2 -- keeping firefox/webkit + integration + a11y + perf out of the per-push hot path).

### Step 6 - Multi-angle review, bounded to <=2 rounds then human

Run `/review-orchestrate` on the **short branch** (cheap in-session multi-angle agent review: correctness, security, performance, architecture, best-practices, and a11y/SEO where they apply). Then:

- **Resolve every finding in code**, push, and **re-review** the delta.
- **Hard cap: <=2 rounds, then a HUMAN decides** (Blueprint §J.3 / §A-2). This is the termination guarantee -- "zero open comments" can never stall a unit forever, and an over-eager reviewer cannot loop indefinitely. After the second round, the open items go to the human approver as a go/no-go, not back into another bot round.
- **High-stakes units only:** escalate to cloud `/ultrareview` + cross-vendor Codex (Blueprint §J.3, M-10). Do **not** run cloud review on every merge -- it burns velocity and spend; reserve it for auth, payments, migrations, security-sensitive surfaces.
- The ruleset's **required PR approval** is satisfied by the human decision, not by the bot rounds.

Do not enter the queue until review has terminated (resolved, or human-approved with the open items accepted).

### Step 7 - Enter the merge queue (the real gate)

Hand the PR to the queue with **auto-merge**:

```bash
gh pr merge --auto --squash      # squash keeps linear history the ruleset requires
```

- `--auto` tells GitHub: **add to the merge queue and merge when the queue's checks pass**. You do not sit and watch -- the queue is asynchronous and server-side.
- Entering the queue triggers the **FULL tier** via the **`merge_group`** event (`types: [checks_requested]`): integration against a **real DB**, **full Playwright e2e/UI across chromium/firefox/webkit (no mocks)**, a11y (axe), perf budget -- run against **future-main** (your change rebased on top of everything ahead of you in the queue). This is what catches the semantic conflict two green PRs create when combined.
- **Every required workflow MUST declare `merge_group: types:[checks_requested]`** or the queue silently stalls waiting for a check that never reports (Blueprint §J.2 -- the templates already wire this; verify if a project hand-edited CI).
- **Do not bypass:** no `gh pr merge --admin`, no temporarily relaxing the ruleset. The queue *is* the gate. If you feel the urge to bypass, the unit is not ready.

### Step 8 - Auto-merge on green, or handle ejection

- **Green queue -> GitHub merges server-side**, squash onto `main`, linear history preserved. Nothing for you to do at the moment of merge -- the ruleset/queue did it.
- **Red queue (your entry ejected) -> the full suite found a problem your FAST stage and review missed** (usually an integration/cross-browser/real-infra failure, or a conflict with a PR ahead of you). Pull the queue's logs, **fix on the branch, re-push, re-queue.** The queue protected `main` -- this is the system working.
- **If a bad change does land on `main`** (escaped every gate): **do not hotfix-forward.** Use **`/rollback`** -- revert-first on trunk, wired to the §19 incident/postmortem flow (§J.5 / §C-1). A revert is one PR through the same queue; a forward-fix under pressure is how the second bug ships.
- **Throughput is honest, not infinite:** a serial queue + a 15-40 min full suite is ~2-4 merges/hour (Blueprint §J.2). Fine for a solo/weekend dev; if a wave has many ready units, queue them and let the queue serialize -- do not bypass to "go faster".

### Step 9 - Delete the branch and resync (close the <24h loop)

After the merge lands:

```bash
git switch main
git pull --ff-only origin main          # linear history -> fast-forward only
git branch -d <type>/<slug>             # local
git push origin --delete <type>/<slug>  # remote (or rely on the repo's auto-delete-on-merge)
```

The branch has done its one job and is gone -- **lifespan well under 24h.** A branch left undeleted is noise; a branch left *open and unmerged* past a day is the integration debt this whole flow prevents -- if you are at that point, you skipped Step 1's split/flag guidance. Then run **`/kb-capture`** to record any decisions/components/risks this unit produced into `Knowledge/<Project>/`, and move to the next unit in `/plan-tree`.

## Reporting

After driving the loop, report explicitly:

```
Trunk merge report for <unit name>:

Preflight:   mutation-check PASS · verify-work PASS · rebased on origin/main · tree clean
Branch:      feature/<slug>  (opened <ts>, merged <ts> -> lifespan <Xh>m, <24h ✓)
PR:          #<n>  "<conventional title>"  (plan + DoD + break-the-code evidence + flag note attached)
Flag:        <name> default OFF, expiry <date> (removal = DoD)   | or "n/a — unit complete, no flag"
Migration:   expand/contract, no destructive change with calling code   | or "n/a — no schema change"

FAST stage (gates button): lint ✓ · unit+patch-cov ✓ (94% line/88% branch on changed) · diff-mutation ✓ (82% killed)
                           · assertion-density ✓ · gitleaks ✓ · semgrep ✓ · chromium smoke ✓   [<10 min]
Review (review-orchestrate): round 1 -> N findings resolved · round 2 -> clean · HUMAN approved   [<=2 rounds ✓]
Merge queue (merge_group, gates merge): integration ✓ · e2e chromium/firefox/webkit ✓ · a11y ✓ · perf ✓
Merge:       AUTO-merged on green, squash, linear history   (server-side via ruleset)   | or "EJECTED on <check> -> fixed + re-queued"
Cleanup:     branch deleted (local+remote) · main fast-forwarded · /kb-capture done

Verdict: MERGED — unit on main behind <flag/none>, branch gone, knowledge recorded. Next unit: <...>
```

If the verdict is **NOT MERGED**, name exactly where it stopped (red FAST stage / review round-2 human hold / queue ejection on `<check>`) and the next action. **Never report MERGED on the strength of a green FAST stage alone** -- the queue's full suite is the gate, and merge is confirmed only when GitHub has merged server-side.

## Iteration & STOP

- This is a **bounded per-unit runner**, not an open loop. The only internal iteration is: review **<=2 rounds then human** (Step 6), and **fix-and-re-queue** on a queue ejection (Step 8). Neither loops indefinitely -- the review cap is hard, and a repeatedly-ejecting PR means the unit is wrong (back to `/mutation-check`/`/verify-work` or split it), not that you re-queue forever.
- **STOP (success)** when: the PR is **merged server-side by the queue**, the branch is **deleted local+remote**, `main` is fast-forwarded, and `/kb-capture` has recorded the unit's decisions. Branch lifespan **<24h**.
- **STOP (escalate)** when: the queue ejects the **same** entry **twice** for the same real-infra reason (the unit has a defect the unit-level checks missed -- reopen `/test-plan`/`/verify-work`, do not brute-force re-queue), or a bad change reached `main` (-> `/rollback`, then a postmortem), or review hits round 2 with unresolved high-severity findings (-> human go/no-go, do not auto-proceed).
- **Do NOT** bypass the queue, admin-merge, disable the ruleset, hold a branch open past a day, or merge incomplete work **live** (it goes behind a default-off flag or not at all). The gate and the <24h rule are not the variables.

## See also

- `/review-orchestrate` — Step 6; the multi-angle review (correctness/security/perf/arch/best-practices/a11y/SEO) with the <=2-round-then-human cap. This skill calls it and gates on its verdict.
- `/feature-flag` — Step 1/4; how incomplete units merge **dark** (default-off, dated, expiry, removal=DoD). `/flag-cleanup` removes stale flags (AST-based, review-gated) in `/hygiene-sweep`.
- `/rollback` — Step 8; **revert-first beats hotfix** when a bad change reaches `main`; wired to the §19 incident/postmortem flow. The counterpart to this skill's "merge", not a fix-forward branch.
- `/db-migrate` — Step 1; expand/contract is mandatory on trunk, and a destructive schema change must never ride in the same PR as the code using it (§C-2).
- `/mutation-check`, `/verify-work` — the two gates a unit must pass **before** entering this loop (>=75% killed + break-the-code; real behavior through real DB/UI). This skill ships an already-proven unit.
- `/plan-tree` — the per-unit loop this skill closes: plan -> code -> tests -> mutation-check -> verify-work -> **trunk-merge** -> kb-capture -> next unit.
- `/release` — after merge: trunk release / tag-from-main / changelog from the conventional commits this loop produced (§C-4).
- `/project-setup` — applies `templates/ruleset.json` via `gh api` (fails loudly if it can't); this skill assumes the ruleset + merge queue it installs.
- `agents/reviewer.md` — the read-only 13-dimension reviewer used as review angles under `/review-orchestrate`; `agents/devops.md` — owns the CI templates whose FAST/`merge_group` stages this loop depends on.
- `templates/ruleset.json` — the **server-side gate** (require PR · require merge queue · require checks · linear history); `templates/settings.json` — the local push-to-main block is **defense-in-depth**, and the branch-name hook is **advisory** on trunk-mode.
- `templates/ci/{node,python,go,dotnet}.yml` — the two-tier checks: `pull_request` FAST stage gates the button, `merge_group: types:[checks_requested]` FULL stage gates the merge (declare it or the queue stalls).
- [[../Agent Workflow]] — §3 (Trunk-based delivery + the <24h/queue/flags rule), §6 (review channels + the <=2-round cap), §9.4 (Definition of Done: flag expiry, break-the-code evidence, Knowledge updated), §19 (incident + rollback).
- Blueprint §J (Trunk-Based Merge / CI Model: §J.1 branching, §J.2 latency resolution, §J.3 review sequencing, §J.5 flags/rollback/migrations), §E (hook posture: ruleset is the gate, local block is defense-in-depth).
