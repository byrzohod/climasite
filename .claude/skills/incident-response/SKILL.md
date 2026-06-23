---
name: incident-response
description: The production-incident loop for a solo on-call dev -- detect/triage (assign a severity), assess blast radius (users / data-at-risk / security), MITIGATE FIRST (revert-first via /rollback, or a feature-flag kill via /feature-flag -- stop the bleeding before root-causing), communicate status to users, then resolve + /verify-work the fix, and schedule the blameless postmortem. Records an incident node (with a timeline) in Knowledge/<Project>/ and carries every action item into STATE.md + the backlog so nothing is dropped. Use this whenever prod is down or degrading, an alert fired, users are reporting breakage, error rate / latency (p95/p99) spiked, a deploy is failing in production, there's a live security exposure, or the user says "we have an incident", "prod is broken", "site is down", "handle this outage", "we're on fire", or asks how to run the incident before the postmortem.
---

# /incident-response - The production incident loop (detect -> mitigate -> verify -> postmortem)

The end-to-end loop for a **live production incident**, owned by a **solo dev who is also the on-call** -- there is no separate ops team to page, no incident commander to hand off to; the dev detects, decides, mitigates, communicates, and learns. This skill is the **orchestrator** of that loop: it sequences the smaller skills that do the actual work -- `/rollback` and `/feature-flag` (mitigate), `/verify-work` (confirm resolution), `/kb-capture` (record the incident node), `/postmortem` (the Learn step) -- and guarantees the loop *terminates* in a recorded incident node, a scheduled postmortem, and tracked action items, so an incident never just "ends" in someone's memory.

It is the canonical implementation of **`Agent Workflow.md` §19** (Detect -> Assess -> Mitigate -> Communicate -> Resolve -> Learn) for steps 1-5; the **Learn** step (6) is owned by its dedicated sibling **`/postmortem`**, which this skill *schedules and hands off to*. The governing rule, inherited from §19 and `/rollback`/`/deploy-checklist`: **stop the bleeding first, root-cause second.** Mitigation is not the fix; it buys a calm, green trunk to fix from. (`/deploy-checklist`'s "If Something Goes Wrong" hands off here; this skill is that hand-off's owner.)

## When to use

Invoke this skill the moment **production is broken or degrading for real users**:

- **An alert fired** (error-rate, latency p95/p99, saturation, a failed health check) or a **synthetic/uptime check is red**.
- **Users are reporting breakage** -- the symptom is real, not a one-off.
- **A deploy is failing or degrading prod** -- 5xx spike, latency cliff, or a key flow broke right after a merge/deploy.
- **A live security exposure** -- a leaked secret in use, an exploited hole, data exfil in progress (this also pulls in `/security-review` and the §34 posture).
- **Data is at risk** -- corruption, a destructive migration that ran, deletion past the point a code revert can fix.
- **The user says** "we have an incident", "prod is down", "we're on fire", "handle this outage", or asks how to run the incident before writing the postmortem.

This is the **production** loop. A red trunk that *no user is feeling yet* (caught by the merge-queue gate, a pre-prod smoke failure) is not an incident -- fix it as normal trunk work.

## When NOT to use (reach for something else)

- **The mitigation is the whole job and the diagnosis is obvious** -- e.g. one flagged path is misbehaving, kill the flag. Go straight to `/feature-flag` (or `/rollback`); you don't need the full loop's ceremony for a 30-second flag flip with no user impact. (But if users *were* affected, still record the incident node + postmortem -- come back here for that.)
- **Bad code reached `main` but it's pre-prod / no user impact** -- that's a `/rollback` (revert-first on trunk) without the incident wrapper. `/rollback` is a *step* of this skill, not a synonym for it.
- **The bad change is still on a branch / in the merge queue** -- there's nothing live to mitigate; dequeue/close the PR. No incident.
- **A planned deploy or release** (not a fault) -- that's `/deploy-checklist` / `/release`.
- **Writing the postmortem itself** -- that's **`/postmortem`** (the §19.1 *Learn* step: fills the §19.2 template, files each action item as a `Risks/`/`Questions/` graph node). This skill owns triage -> mitigate -> communicate -> resolve/verify and *schedules* the postmortem; it does not perform the 5-whys retrospective.
- **A pure data-corruption recovery with no code component** -- restore-from-backup / `/db-migrate` rollback is the mechanism; this loop still wraps it (severity, comms, postmortem), but the *recovery* is owned there.

## Read first (always)

Mid-incident, read the minimum that changes your decision -- this is seconds-matter territory, not a research loop:

- **`STATE.md`** -- the current active unit, the release path, recent merges (what likely changed). The incident's action items land back here.
- **`Knowledge/<Project>/`** -- the project hub + any open risks (a risk that was logged "we'll get to it" is often the incident's root cause), and prior incident nodes (`Incidents/`) -- a *recurring* incident escalates severity and changes the action items.
- **Monitoring / logs** -- correlate the symptom's start time with the last merge/deploy (`git log --oneline -15 origin/main`). This is the load-bearing read: it tells you *what to revert*.
- **`flags.json` / flagd defs** -- is the bad behavior already behind a flag? If yes, the fastest mitigation is a flip, not a revert.

## Severity (set it first -- it drives everything else)

Assign a severity in the first minute; it sets urgency, whether you communicate externally, and the postmortem deadline. Reuse `Agent Workflow.md` §19.2's `P0-P3`:

| Sev | Meaning (solo-dev-realistic) | Comms | Postmortem |
|---|---|---|---|
| **P0** | Prod down / data loss / active security exploit. Everything stops. | Status page + affected-user notice, updated as it moves | Required, within 48h |
| **P1** | Major feature broken or severe degradation; a workaround may exist. | Status page if users hit it | Required, within 48h |
| **P2** | Minor/partial degradation, limited blast radius, not urgent. | Optional note if user-visible | Lightweight (a Knowledge node + action items) |
| **P3** | Cosmetic / negligible user impact; basically a bug ticket. | None | Skip the formal postmortem; just file the fix |

Don't inflate or deflate: a deflated P0 means you under-communicate and skip the postmortem on the thing most likely to recur; an inflated P3 burns your one pair of hands on ceremony. **When unsure between two levels, pick the higher** -- you can downgrade once the blast radius is known.

## Process

The six steps map 1:1 to §19. **Steps 1-3 are the clock-is-running phase** (triage -> assess -> mitigate); steps 4-6 close the loop. Do not skip ahead to root-causing before the bleeding is stopped.

### Step 1 -- Detect & triage (open the incident, set severity)

1. **Confirm it's real and live.** Reproduce the symptom or confirm the alert against a second signal (don't mitigate off a single flaky check). If it's not actually user-facing, it's not an incident -- exit to normal trunk work.
2. **Start the clock + timeline.** Record the **detection time** and the symptom in one line -- this is the seed of the incident node's timeline (Step 6). Every subsequent action gets an `HH:MM` line.
3. **Set the severity** (table above). This decides comms and postmortem obligations for the rest of the loop.

### Step 2 -- Assess blast radius (before you act)

4. **Quantify impact** (§19 step 2): *how many users*, *which flows*, *is data at risk*, *is it a security exposure*. These three -- data-at-risk and security-exploited especially -- change the mitigation choice: they tilt you **away** from a naive code revert (which can de-sync code from already-moved data, or re-expose an exploited hole) and **toward** forward-fix / restore-from-backup.
5. **Correlate to a cause.** Line the symptom's start up with the last merge/deploy. A clear "started right after PR #N / deploy X" points the mitigation; an unclear cause means you mitigate by the broadest safe lever (scale, flag, or revert the most likely suspect) and keep assessing.

### Step 3 -- MITIGATE: stop the bleeding by the fastest *safe* lever

Pick the fastest mitigation that is *safe*, in this order (this is `/rollback` Step 2's ladder -- defer to it for the mechanics):

6. **Flag off** -- if the bad behavior is behind a feature flag, **disable it** (`/feature-flag` kill-switch). No deploy, effective in seconds. This is *why* incomplete/risky work merges behind default-off flags (§J.5). Done -> Step 4.
7. **Revert on trunk** -- if it's **code-only and stateless**, **`/rollback`** (revert-first beats hotfix): branch off `main`, `git revert` the offending merge, push through the **same merge queue** (the gate never bends, even mid-incident; narrow break-glass only per `/rollback`'s emergency-bypass section). This is the default for un-flagged bad code.
8. **Forward-fix through the queue** -- *only* when `/rollback`'s "revert vs forward-fix" rule says revert is unsafe/impossible (data moved forward, security re-exposure, buried commit). Still a gated PR, never a hand-pushed hotfix to `main`.
9. **Scale / restore** -- if it's saturation, scale; if data is corrupted, restore-from-backup / `/db-migrate` rollback (a code revert does **not** fix data).

**Mitigation != resolution.** The bug is now *quarantined*, not fixed. Note in the timeline *which* lever you pulled and *why* (especially any deviation from revert-first -- that justification is load-bearing).

### Step 4 -- Communicate (proportional to severity)

10. **Tell affected users** per the severity table -- update the **status page** (and any user notice) when the incident starts, when it's mitigated, and when it's resolved. Solo-dev reality: *you* are the comms channel; a terse, honest "we're aware, mitigating" beats silence. P2/P3 may need nothing. Keep the message free of internal blame and of detail an attacker could use (especially for security incidents).

### Step 5 -- Resolve & verify (the real fix, calmly)

11. **Root-cause on a fresh branch** with a **failing test first** -- reproduce the bug the suite missed (§4.5 / `/mutation-check`). The failing-then-passing test pins the regression so it can't recur.
12. **Re-land the real fix** through the **full normal flow** -- `/test-plan` -> break-the-code (`/mutation-check`) -> `/review-orchestrate` / `/code-review` -> merge queue. No shortcuts; the clock is off now.
13. **Verify resolution with `/verify-work`** -- the symptom that spiked is back to baseline *and holding* (watch it, don't assume), trunk is green, no *new* symptom appeared (the revert didn't undo something newer code needed), and the fix actually exercises the real path end-to-end (no mock standing in for the failed dependency). Update the status page to **resolved**.

### Step 6 -- Record the incident node + schedule the postmortem + carry action items

The loop is not done until it is **durable**. Do all three:

14. **Create the incident node** in `Knowledge/<Project>/Incidents/` (see "The incident node" below) -- a timeline-bearing record, wired into the graph. This is the record `/postmortem` reads as the spine of its analysis.
15. **Schedule the blameless postmortem -> hand off to `/postmortem`** -- per the severity table (P0/P1 within 48h; P2 lightweight; P3 skip). **`/postmortem` owns the Learn step**: it fills the §19.2 template into `Knowledge/<Project>/Postmortems/<slug>.md`, runs the 5-whys, and -- the load-bearing move -- files **each action item as a graph node** (`Risks/ R-NNN` for a prevention, `Questions/ Q-NNN` for an open investigation) with an owner + deadline. Don't inline that here; **put the due date on the backlog/calendar and invoke `/postmortem`** when the incident is resolved. (For a P3 you skip the formal postmortem, but still capture any fix as a backlog item.)
16. **Carry every action item into `STATE.md` + the backlog** (see "Action items" below) -- the missing test, the flag that should have guarded the change, the CI check to add, the alert that should have fired sooner. STATE + backlog are the **resume/tracking surface** (so the next session and the next plan see the work); their durable *graph* form (the `Risks/`/`Questions/` nodes) is filed by `/postmortem` and pointed at from here. **Feed the gap back** so the same class of incident can't reach prod again. An incident whose action items aren't tracked *will* recur.

## The incident node (Knowledge/<Project>/Incidents/)

**Schema note (read this before creating the node).** `incident` is a **first-class node type** in `Knowledge/_schema.md` -- it lives in `Incidents/`, uses ID `INC-NNN-<slug>`, and has its own status enum: **`open → mitigated → resolved → postmortem-done`** (mirroring this loop's phases). It is a *production-reality* node: per the schema's product loop, it **emits `question` and `risk` nodes** (via `raised_by` / `relates_to`) that feed the `/kb-capture` Step-6 loop back into `/plan-tree` and `/design-doc`. Use `type: incident` directly -- no milestone work-around, no propose-only step.

ID convention: **`INC-NNN-<slug>`**. Frontmatter (full `_schema.md` contract; **all edges in frontmatter** so Dataview/graph see them):

```yaml
---
tags: [knowledge, <project-slug>, incident]
created: YYYY-MM-DD
type: incident             # first-class node type (Incidents/, INC-NNN-<slug>)
status: resolved           # open -> mitigated -> resolved -> postmortem-done
project: <project-slug>
severity: P1               # P0|P1|P2|P3 (extra field; informational, not in the enum)
# --- typed edges (frontmatter ONLY; each value is a [[wikilink]]) ---
part_of:    "[[<Project>/README]]"
affects:    ["[[Components/<the-broken-component>]]"]   # what broke
raised_by:  "[[Phases/Phase-N]]"                        # the unit/phase whose change caused it, if known
relates_to: ["[[Decisions/ADR-NNN-...]]", "[[Postmortems/<incident-slug>]]"]  # the decision/PR implicated + the postmortem doc /postmortem writes
mitigates:  "[[Risks/R-NNN-...]]"                       # the open risk this incident realized, if one existed
---
```

Body = the **§19.2 timeline**, kept verbatim to house format (reuse the AI-Sonar `Decision Log` discipline -- dated, append-only):

```markdown
# INC-007: <one-line title>

**Severity:** P1 · **Detected:** 2026-06-23 14:02 · **Mitigated:** 14:19 · **Resolved:** 16:40
**Impact:** <what users experienced>  ·  **Time to mitigate:** 17 min

## Timeline
- 14:02 - Alert: 5xx rate 0.4% -> 11% on /checkout
- 14:05 - Confirmed against logs; reproduced. Severity = P1. Incident opened.
- 14:09 - Blast radius: ~all checkout users; no data at risk; not security.
- 14:11 - Correlated to PR #214 (merged 13:58). Code-only, stateless -> revert.
- 14:19 - Reverted via PR #215 through the queue; 5xx back to baseline. Status page: mitigated.
- 16:40 - Root-caused (null guard), failing test added, fix re-landed PR #218, /verify-work green. Resolved.

## Root cause
<one line -- full analysis lives in the postmortem>

## Action items  (tracked in STATE.md + backlog; filed as Risks/Questions nodes by /postmortem)
- [ ] Add the missing null-guard test to the checkout suite — owner @byrzohod — due 2026-06-25 — → [[Risks/R-0NN-...]]
- [ ] Add a /checkout 5xx alert at 2% — owner @byrzohod — due 2026-06-27 — → [[Risks/R-0NN-...]]
```

Then **append one `Knowledge/EVOLUTION.md` row** (new node) and **refresh the project hub** `README.md` (`type: moc`) so the incident is reachable -- exactly as `/kb-capture` does for any node (delegate the multi-edge wiring to the `knowledge-curator` agent if it re-points existing risks/components). Run `/kb-health`'s core checks (broken links, orphans) before committing the node.

## Action items (carry them, or they evaporate)

Division of labor with `/postmortem`: **`/postmortem` files each action item as the durable graph node** (`Risks/ R-NNN` for a prevention, `Questions/ Q-NNN` for an open investigation) with an `owner` + `due` + a `raised_by` edge to the postmortem doc -- that is what feeds `plan-critic`'s attack seed and the next `/plan-tree`. **This skill's job is the tracking surface**: make sure every item also lands in the two places the *next working session* will actually look, each pointing at its graph node:

- **`STATE.md`** -- under **Blockers** (if an item gates the next planned work) or **Key decisions & pointers** (the load-bearing follow-up + its `[[Knowledge/Incidents/INC-NNN]]` / `[[Risks/R-NNN]]` link). STATE is the resume contract the SessionStart hook injects -- putting the action item here means the *next* session sees it. Keep it lean: a pointer line, not the prose.
- **The backlog** -- each item as a tracked task with an **owner and a deadline** (§19.2: "owners and deadlines, not just 'we should do X'"). Solo-dev: the owner is you; the deadline is real. Highest-leverage items are the **gap-closers** -- the test/assertion that would have caught it (`/mutation-check`), the flag that should have guarded it (`/feature-flag`), the alert that should have fired, the CI check to add.

Review these in the next planning cycle (`/plan-tree` reads `Knowledge/` + STATE) -- an open action item from a P0/P1 outranks new feature work. (If you handle a P3 without a formal postmortem, *you* file the one backlog item here -- the graph node is `/postmortem`'s output only when a postmortem runs.)

## Reporting

State the outcome explicitly so the incident record is complete and the postmortem has a spine:

```
Incident report -- INC-NNN:

Severity:        P1
Trigger:         <alert / user report / failed deploy>, detected HH:MM
Blast radius:    <~N users / which flows> · data at risk: no · security: no
Cause:           PR #<n> "<title>", merged HH:MM (commit <sha>)   [or "under investigation"]
Mitigation:      FLAG-OFF / REVERT / FORWARD-FIX / SCALE / RESTORE -- because <one line>
                 (effective HH:MM; time to mitigate = detect -> baseline = NN min)
Comms:           status page updated (opened HH:MM / mitigated HH:MM / resolved HH:MM)  [or "no user impact"]
Resolution:      real fix PR #<m> through full gate, failing test added; /verify-work green (symptom <x> -> baseline, holding)
Knowledge node:  Knowledge/<Project>/Incidents/INC-NNN-<slug>.md (timeline recorded)
Postmortem:      scheduled <date> (blameless, §19.2)   [or "skipped -- P3, see node"]
Action items:    N tracked -> STATE.md + backlog (owners + deadlines); gap-closer = <the missing test/flag/alert/CI check>
```

If you deviated from **revert-first** (chose forward-fix / restore over revert), say *why revert was unsafe* (data moved forward, security re-exposure, buried commit, behavior was flagged) -- revert is the default, and any deviation must be defended, same as `/rollback`'s report.

## Iteration & STOP

- This is a **bounded recovery loop**, not an open-ended one: triage -> assess -> mitigate -> communicate -> resolve+verify -> record+schedule. Don't thrash reverting suspect after suspect -- if the first mitigation doesn't move the symptom, **re-assess the cause** (Step 2) before the next lever.
- **STOP the mitigation phase** when the spiked symptom is back to baseline *and holding*, no new symptom appeared, and users have been told it's mitigated. The incident's *mitigation* is closed even though the bug isn't fixed.
- **STOP the whole incident** only when **all** of: the real fix re-landed through the normal gate with a regression test; `/verify-work` is green; the incident node exists with its timeline; the postmortem is scheduled (or explicitly skipped for P3); and every action item is in STATE.md + the backlog with an owner + deadline. A mitigated-but-unrecorded incident is an **open loop** -- it will recur, and the next on-call (future you) won't know why it was patched.
- **Never weaken a gate to "save time" mid-incident.** The gate let the bug through because a *test was missing*, not because the gate was too strict -- the answer is the action item that adds the test, not lowering the bar. Quality is never the variable, even on fire (same rule as `/rollback` and §J).
- **Recurrence is an escalation signal.** If the incident node search shows this same failure before, bump the severity, and make the action item *structural* (the class of bug, not this instance) -- a second occurrence means the first postmortem's action items didn't land.

## See also

- `/rollback` -- the **Mitigate** mechanism for merged code: revert-first on trunk, the revert-vs-forward-fix decision rule, the emergency-bypass exception. Step 3 of this loop *is* `/rollback` when the bad code isn't flagged. (`/rollback` pairs with §19; this skill is the §19 wrapper around it.)
- `/feature-flag` -- the **fastest** Mitigate lever when the bad behavior is flagged: kill-switch off, no deploy, seconds. Default-off flags on incomplete/risky units are *why* most incidents can be stopped without a revert (§J.5).
- `/verify-work` -- Step 5's resolution check: the symptom is back to baseline, trunk green, no new symptom, the fix exercises the real path (no mock). Resolution isn't claimed without it.
- `/db-migrate` -- when the incident touched the schema/data: expand/contract, `down` migrations, restore-from-backup. A `git revert` rolls back code, **not** data -- the boundary where mitigation switches skills.
- `/security-review` & `/threat-model` -- pulled in for a security incident (Step 2 flags it): contain the exposure, rotate secrets, then the threat-model row that should have caught it becomes an action item.
- `/postmortem` -- the **Learn** step (§19.1 step 6) this loop *schedules and hands off to* once the incident is resolved: fills the blameless §19.2 template, runs the 5-whys, and files **each action item as a `Risks/`/`Questions/` graph node** with owner + deadline. This skill records the incident node + the STATE/backlog tracking pointers; `/postmortem` owns the retrospective + the durable action-item nodes. The two are the §19 loop split at step 6.
- `/test-plan` & `/mutation-check` -- the failing-first regression test Step 5 demands (pins the escaped defect so it can't recur); the postmortem's #1 gap-closer.
- `/kb-capture` -- the machinery this skill uses to write the **incident node** (`type: incident`) into `Knowledge/<Project>/Incidents/` (conforms to `_schema.md`, appends the EVOLUTION row, refreshes the hub). (The *postmortem* nodes are filed by `/postmortem`, a different caller of the same machinery.)
- `/deploy-checklist` -- its "If Something Goes Wrong" hands off **here**; this skill owns that hand-off. The deploy-time rollback plan ("rollback first, investigate later") is the pre-incident half.
- `/release` -- planned releases vs. unplanned recovery; if a *tagged release* caused the incident, reconcile the revert with the tag/changelog there.
- [[../../Knowledge/_schema|Knowledge/_schema.md]] -- defines `incident` as a first-class node type (`Incidents/`, `INC-NNN-<slug>`, status `open → mitigated → resolved → postmortem-done`) + the edge vocabulary the incident node conforms to. [[../../Knowledge/EVOLUTION|EVOLUTION log]] -- where each new incident node is logged.
- [[../Agent Workflow]] -- §19 (Incident Response & blameless postmortem -- this skill **is** §19's implementation; §19.2 is the postmortem template), §3.x ("revert beats hotfix" on trunk, C-1), §J.5 (flags / rollback / migrations), §J.1-J.2 (the merge queue the mitigation PR runs through), §34 (security posture for security incidents).
