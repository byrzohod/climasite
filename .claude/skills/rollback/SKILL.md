---
name: rollback
description: Get a bad change out of main fast and safely on a trunk-based repo -- revert-first beats hotfix. Use this whenever bad code reached main/trunk (a broken or regressing merge is live), a deploy is failing, error rates/latency spiked after a merge, you need to "roll back", "revert the merge", "undo a deploy", "git revert", back out a commit, kill a feature flag to stop the bleeding, or you're mid-incident deciding between reverting and forward-fixing. Also use to decide WHEN a forward-fix (roll-forward / fix-forward) is the safer choice instead. Pairs with incident response (Agent Workflow.md §19).
---

# /rollback - Revert-first recovery on trunk

## When to use

Invoke this skill the moment a change in **main/trunk is bad** and users (or CI, or prod) are feeling it:

- **A merged change broke main** -- the build is red on trunk, a smoke test fails post-merge, or a regression slipped past the gate.
- **A deploy is failing or degrading prod** -- error rate, latency (p95/p99), or a key business metric spiked right after a merge/deploy.
- **You are mid-incident** (Agent Workflow.md §19) and at the **Mitigate** step: "stop the bleeding first, fix root cause second." This skill is the *stop the bleeding* mechanism for code that came from a merge.
- **You need to undo a specific merged commit** while keeping everything merged after it.
- **You're deciding** between backing the change out vs. fixing it forward -- this skill gives the decision rule (see "Revert vs. forward-fix" below).

The governing principle on trunk: **revert-first beats hotfix.** A `git revert` of the offending merge is mechanical, reviewable, fast, and *reversible* (you can re-revert to bring the change back once it's fixed). A hand-written hotfix under incident pressure is none of those things -- it's net-new code, untested, written by a stressed human, racing the clock. **Back the bad change out through the normal merge queue; debug it calmly on a branch afterward.** This mirrors `/deploy-checklist` ("Don't try to fix forward under pressure. Rollback first, investigate later") and `/db-migrate`'s rollback discipline, applied to source on trunk.

## When NOT to use (reach for something else)

- **The bad code is NOT yet on main** -- it's still on a short-lived branch or stuck in the merge queue. Just **dequeue / close the PR or amend the branch**; there's nothing to revert. Reverting only applies to what already landed on trunk.
- **The problem is a stateful/schema change, not code.** A `git revert` rolls back *code*, not data. If a migration ran, see **"When revert is NOT enough"** below and `/db-migrate` (rollback section, expand/contract) -- reverting the code that uses a new column does **not** drop the column, and reverting a migration's code never un-runs the SQL.
- **The fastest mitigation is a flag flip, not a code change.** If the bad behavior is behind a feature flag, **turn the flag off** -- it's faster than any git operation and needs no deploy. Use `/feature-flag`; come back to `/rollback` only if the change is *not* flagged. (This is why incomplete units merge behind default-off flags in the first place -- §J.5.)
- **You are deploying a planned release** (not recovering from a fault) -- that's `/release` / `/deploy-checklist`, not this skill.

## Decision: revert vs. forward-fix

Default to **revert**. Choose a **forward-fix (roll-forward)** only when reverting is *more* dangerous or simply impossible. Decide in seconds, not minutes -- when in doubt, revert.

**Revert (the default) when:**
- The bad change is recent and reverts cleanly (no heavy entanglement with merges on top of it).
- The change is code-only / stateless -- no migration ran, no data was written under the new behavior, no irreversible external side effect (emails sent, payments captured, webhooks fired).
- You're under time pressure or unsure of the root cause. Revert buys you a calm, green trunk to debug from.

**Forward-fix (roll forward) when -- and say so explicitly in the incident log:**
- **A revert would itself cause harm or data loss** -- e.g., reverting would re-expose a security hole already exploited, or undo a migration/data write that newer rows now depend on (reverting the *code* while the *schema/data* moved forward de-syncs them). Reverting forward-only state is how you turn one incident into two.
- **The fix is genuinely a one-line, obvious, low-risk change** that ships through the *same* fast PR + merge-queue gate as anything else (it is **not** a hand-pushed hotfix to main -- see "No bypassing the gate" below). A trivial, gated forward-fix can beat untangling a deeply-buried revert.
- **The bad commit is buried** under many later merges that depend on it, so a clean revert is impossible without reverting good work too.
- **The mitigation is config/flag/scale, not code at all** -- then it's not a code rollback; flip the flag (`/feature-flag`) or scale, and fix the code calmly afterward.

If you forward-fix, it still goes **through the merge queue** with the normal fast checks -- the urgency changes the priority, never the gate. "Forward-fix" is not a license to push to main.

## Process

### Step 1: Assess and declare (tie into §19)

- **Confirm the change is on main** and is actually the cause (correlate the regression's start with the merge/deploy time; `git log --oneline -15 main`). Don't revert an innocent commit.
- **Assess blast radius** (§19 step 2): how many users, is *data* at risk, is it a security issue? Data-at-risk or security-exploited tilts you toward forward-fix / restore-from-backup, not a naive code revert.
- **Start the incident clock and timeline** (§19) -- note the detection time and "decided to revert/forward-fix because X". You'll need this for the blameless postmortem.

### Step 2: Stop the bleeding by the fastest safe lever

Pick the **fastest** mitigation that is *safe*, in this order:

1. **Flag off** -- if the change is behind a feature flag, disable it (`/feature-flag`). No deploy, seconds to take effect. Done; skip to Step 5.
2. **Revert on trunk** -- if it's code-only and stateless, revert the merge (Step 3). This is the default for un-flagged bad code.
3. **Forward-fix through the queue** -- only if Step 1 of "revert vs. forward-fix" said so (revert is unsafe/impossible).
4. **Restore from backup / data recovery** -- only if data was corrupted; see `/db-migrate` rollback and the §19 mitigation path. A code revert does not fix corrupted data.

### Step 3: Revert the merge through the queue (the core path)

The change reached main via a **merge-queue squash/merge commit** (trunk = short-lived branch + merge queue, §J.1). Revert *that*:

```bash
# 1. Identify the offending commit on main (the squash/merge from the bad PR)
git fetch origin && git log --oneline -15 origin/main

# 2. Branch off current main (short-lived, like any change -- never commit straight to main)
git switch -c revert/<short-desc> origin/main

# 3a. Squash-merge PR (one commit on main): a plain revert is correct
git revert --no-edit <bad_commit_sha>

# 3b. True merge commit (rare in squash-merge repos): revert the merge, keeping mainline
#     -m 1 = keep parent #1 (main); the merged branch's changes are undone
git revert -m 1 --no-edit <merge_commit_sha>

# 4. Push the branch and open a PR -- this goes through the SAME merge queue + fast checks
git push -u origin revert/<short-desc>
gh pr create --fill --title "Revert: <what and why> (incident <id>)" \
  --body "Reverts <bad PR #>. Backing out under incident <id>; root-cause fix to follow on a separate branch."
```

- **Push through the merge queue, not around it.** The revert PR runs the fast PR stage (lint/typecheck, unit + patch-coverage, mutation-diff, smoke, Gitleaks/Semgrep) and then the `merge_group` full suite -- the same gate as any change (§J.2). The queue *is* the gate; that the revert is reversing something doesn't exempt it. (For a true emergency where the queue itself is the bottleneck, see "Emergency bypass" -- it is a narrow, logged exception, not the path.)
- **Why a branch + PR and not a direct push:** the local push-to-main block and the server-side ruleset both forbid direct-to-main; the revert respects them. Defense-in-depth stays on even mid-incident.
- **Conflicts on revert?** If the merge no longer reverts cleanly (later merges touched the same lines), that's the signal a clean revert is *not* available -- re-evaluate toward a targeted forward-fix (revert-vs-forward-fix rule), don't force a messy revert.

### Step 4: Verify the revert actually fixed it

A revert that merges is not a revert that *worked*. After it lands and deploys:

- [ ] **Trunk is green** -- the build/CI that was red is now passing on main.
- [ ] **The symptom is gone** -- the metric that spiked (error rate, p95 latency, the failing smoke/e2e) is back to baseline. Watch it, don't assume.
- [ ] **No new symptom** -- the revert didn't undo something *else* that newer code depended on (the de-sync risk). Confirm the broader smoke flow.
- [ ] **Communicate resolution** (§19 step 4) if users were affected -- update the status page / channel that the bleeding has stopped.

If the symptom persists, the reverted commit was **not** the (whole) cause -- widen the assessment (Step 1), consider reverting the next suspect or escalating; do not assume "I reverted, therefore it's fixed."

### Step 5: Fix forward calmly, then re-land (close the loop)

The revert bought a green trunk; the bug is **not fixed**, only quarantined. Now do it right, with no clock running:

1. **Root-cause it** on a fresh short-lived branch -- reproduce with a **failing test first** (this is what the original suite missed; §4.5 / `/mutation-check`).
2. **Re-apply + fix** the reverted change (`git revert <the_revert_sha>` brings the code back as a base, or cherry-pick + amend), now with the failing-then-passing test pinning the regression so it can't recur.
3. **Full normal flow** -- `/test-plan` → break-the-code (`/mutation-check`) → `/code-review` → merge queue. No shortcuts on the *real* fix; the urgency is over.
4. **Postmortem within 48h** (§19.2, blameless): timeline (incl. detect→revert→re-land times), root cause, why CI/review missed it, action items with owners. **Feed the gap back**: the missing test/assertion, a flag that should have guarded the change, or a CI check to add -- so the same class of regression can't reach main again. Capture the ADR/lesson in `Knowledge/<Project>/` (`/kb-capture`).

## Reporting

State the outcome explicitly so the incident log is complete:

```
Rollback report -- incident <id>:

Trigger:        <red trunk / prod error spike / failed deploy>, detected HH:MM
Bad change:     PR #<n> "<title>", merged HH:MM (commit <sha>)
Decision:       REVERT  (or FORWARD-FIX / FLAG-OFF / RESTORE) -- because <one line>
Mitigation:     reverted via PR #<m> through merge queue; landed HH:MM
Verification:   trunk green ✓ | error rate back to baseline ✓ | no new symptom ✓
                (symptom that spiked: <p95 / 5xx rate / failing e2e> -> resolved)
Time to mitigate: <detect -> green> = NN min
Forward fix:    branch <fix/...>, failing test added (reproduces regression), in review / merged
Postmortem:     due <date>, blameless; root cause = <one line>; action items: <test added / flag / CI check>
```

If you chose **forward-fix or flag-off over revert**, say *why revert was unsafe/impossible* (data already migrated, security re-exposure, buried commit, behavior was flagged) -- that justification is the load-bearing part of the report, because revert is the default and any deviation must be defended.

## Emergency bypass (the narrow exception)

The merge queue is the gate, full stop -- but a true P0 where **the queue itself is down or its full suite is the only thing between users and a fix** is the one case for break-glass. If you must:

- Use the **documented break-glass path** (an admin merge / ruleset bypass that is *logged* server-side, per `templates/ruleset.json`), **never** a silent local force-push to main.
- It is reserved for **reverts and trivial forward-fixes only**, never net-new feature code.
- It **must** be announced in the incident channel as it happens and **reconciled immediately after**: open the retroactive PR, let CI run post-hoc, and record the bypass in the postmortem as an action item ("queue was the bottleneck → why, and how we avoid bypassing next time").
- If you can possibly wait for the queue, **wait** -- bypassing is how a one-line "fix" becomes the second incident.

## Iteration & STOP

- This is a **bounded recovery**, not a loop: assess → stop the bleeding → verify → (separately) fix forward → postmortem. Don't thrash reverting commit after commit hoping one sticks -- if the first revert doesn't resolve it, **re-assess the cause** (Step 4) before the next action.
- **STOP the mitigation phase** when: trunk is green, the spiked symptom is back to baseline and holding, no new symptom appeared, and resolution is communicated. The incident's *mitigation* is then closed even though the bug isn't fixed.
- **STOP the whole incident** only after the real fix has re-landed through the normal gate (with a regression test) and the blameless postmortem is written with owned action items. A reverted-but-never-fixed change is an open loop -- it *will* be re-introduced by someone who doesn't know why it was backed out.
- **Never** weaken a CI gate, skip review, or push to main to "save time" -- the gate caught nothing here precisely because the *missing test* let the bug through; the answer is to add the test, not to lower the bar. Quality is never the variable, even mid-incident.

## See also

- [[../Agent Workflow]] -- §19 (Incident Response & blameless postmortem -- this skill is the §19 *Mitigate* step for merged code), §3.x ("revert beats hotfix" on trunk, C-1), §J.5 (flags/rollback/migrations), §J.1-J.2 (trunk = short-lived branch + merge queue + the two-tier gate the revert PR runs through).
- `/postmortem` -- the §19 *Learn* step that runs after this one resolves: the 48h blameless postmortem whose first action item is the failing-first regression test this skill demands, each action item filed as a `Knowledge/<Project>/Risks/` (or backlog `Questions/`) node with owner + deadline so the escaped defect feeds the next `/plan-tree`.
- `/deploy-checklist` -- the deploy-time rollback plan ("rollback first, investigate later"; tested rollback under 5 min; previous-version artifacts kept). `/rollback` is the *source-on-trunk* counterpart to its *deploy-artifact* rollback.
- `/db-migrate` -- when the bad change touched the schema: expand/contract, `down` migrations, restore-from-backup. A `git revert` rolls back code, **not** data -- this is the boundary where you switch skills.
- `/feature-flag` -- the fastest mitigation when the bad behavior is flagged: flip it off, no deploy, then fix forward. (Default-off flags on incomplete units are *why* most bad merges can be killed without a revert -- §J.5.)
- `/mutation-check` & `/test-plan` -- the regression test the postmortem demands (failing-first, pins the bug) is designed and proven here; the escaped defect is exactly the "hard-5% drift" these guard against.
- `/code-review` & `/verify-work` -- the re-landed real fix goes through the full normal flow; the revert bought the time to do that properly.
- `/release` -- planned releases vs. unplanned recovery; if a *tagged release* is bad, reconcile the revert with the tag/changelog there.
