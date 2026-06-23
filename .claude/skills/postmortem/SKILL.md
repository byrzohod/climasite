---
name: postmortem
description: Blameless postmortem after an incident resolves -- fill the §19.2 template (timeline, impact, 5-whys root cause, what went well/poorly) and, the load-bearing move, file EACH action item as a knowledge-graph node (a Risks/ R-NNN node, or a backlog Questions/ Q-NNN) with a named owner + deadline so it flows through /kb-capture Step 6 into the next /plan-tree and the plan-critic's attack seed. Use this whenever an incident just resolved, after /rollback or /incident-response stopped the bleeding, when the user says write the postmortem / post-mortem / retro / RCA / root-cause analysis / "what did we learn", within 48h of any P0-P3, or when action items from an incident need owners and deadlines and a place to live so they don't evaporate.
---

# /postmortem - Blameless postmortem that turns an incident into owned, tracked graph nodes

The **Learn** step of incident response (§19.1 step 6). An incident is only closed when the *mitigation* stopped the bleeding, the *real fix* re-landed through the normal gate, **and** the lesson is written down in a form that changes future behavior. A postmortem that lives as prose nobody re-reads is theatre. This skill fills the blameless §19.2 template *and* — the load-bearing move — **files every action item as a knowledge-graph node** (`Knowledge/<Project>/Risks/` for a prevention/mitigation, or a backlog `Questions/` for an open investigation) with a **named owner and a deadline**, so each one flows through `/kb-capture` Step 6 into the next `/plan-tree` clarifying batch and the `plan-critic`'s attack seed. The loop only closes when the same class of failure cannot silently reach prod again.

Blameless means **systems and processes, not individuals** (§19): "the deploy had no canary so a bad config reached 100%", never "X pushed bad config". Same uniform rigor as everywhere else — severity does not relax the analysis; a P3 gets a real 5-whys, just a shorter one.

See `Agent Workflow.md` §19 (Incident Response & Postmortems), §19.2 (the template), §18 (Observability — the timeline's evidence source).

## When to use

Invoke this skill:
- **After an incident resolves** — once `/rollback` or `/incident-response` stopped the bleeding *and* the real fix re-landed through the merge queue (the §19.1 mitigation + resolution steps are done).
- **Within 48h of any P0–P3** — the §19 deadline; memory and log fidelity decay fast.
- **When the user asks** for a postmortem / post-mortem / retro / RCA / root-cause analysis, or "what did we learn / how do we stop this recurring".
- **When incident action items need a home** — owners, deadlines, and a place that feeds planning rather than a doc that rots.

## When NOT to use

- **Mid-incident, still bleeding** → this is the *Learn* step, not *Mitigate*. Use `/rollback` (or `/incident-response`) to stop the bleeding first; come back here once trunk is green and the symptom is back to baseline and holding.
- **The real fix has not re-landed yet** → a reverted-but-never-fixed change is an open incident (`/rollback` Iteration & STOP). Write the postmortem once the root-cause fix is merged through the normal gate, so the action items reflect what actually shipped.
- **Not an incident** — a failed CI run, a caught-in-review bug, a planned-and-clean rollback for a known-incomplete flag → no postmortem; that's normal flow. (A *clean* `/release` rollback isn't an incident; a user-impacting outage is.)
- **A routine decision to record** → that's `/kb-capture` directly (an ADR / component / question). Postmortem is specifically for *something broke and we want it to not recur*.

## Read first (always)

The analysis is grounded in evidence, not memory:
- **The incident log / `/rollback` report** — detection time, the assess→mitigate→resolve timeline, the revert-vs-forward-fix decision and *why*, re-land time. This is the spine of your timeline.
- **`Knowledge/<Project>/`** — existing `Risks/` (did this incident realize an already-known risk? then it's a `mitigates`/`relates_to`, not a brand-new R-NNN), prior postmortem-spawned risks, related ADRs (did a decision contribute? link it, don't re-litigate it), and `CLAUDE.md` conventions.
- **Observability evidence (§18)** — logs, traces, dashboards, alert timestamps. Every timeline entry should cite an artifact (a log line, an alert, a graph), not a recollection.
- **The fix diff + its regression test** — the failing-first test that pins the bug (`/mutation-check` / `/test-plan`). "What stops recurrence" is usually *that test plus the gap that let the bug through*.

## Severity & the 48h clock

| Severity | Meaning | Postmortem depth |
|---|---|---|
| **P0** | Full outage / data loss / security exploited | Full template, deep 5-whys per causal branch, action items reviewed weekly until closed |
| **P1** | Major feature down / many users affected | Full template, 5-whys, owned action items |
| **P2** | Degraded / workaround exists | Template, focused 5-whys on the primary cause |
| **P3** | Minor / few users | Short but real — timeline, one 5-whys, =1 prevention item |

Write within **48h** (§19). The depth scales with severity; **the rigor does not** — every postmortem gets a real root cause and owned, tracked action items, never "we should be more careful".

## Process

### Step 0 — Preconditions

1. Confirm the incident is **resolved**: trunk is green, the symptom is at baseline and holding, the real fix re-landed through the merge queue, and resolution was communicated (§19.1 steps 4–5). If not, stop — finish `/rollback` / `/incident-response` first.
2. Gather the evidence above (incident log, `Knowledge/<Project>/`, observability artifacts, fix diff + regression test). Pin the **detection** and **resolution** timestamps.
3. Assign the **postmortem severity** (table above) — it sets the depth, not the rigor.

### Step 1 — Fill the §19.2 blameless template

4. Write the postmortem doc from the §19.2 template *verbatim in shape* (header → Timeline → Root Cause → What Went Well → What Went Wrong → Action Items). It is a **repo-side runbook/postmortem doc** (the `docs` agent's domain; see `agents/docs.md`) — the durable *graph* nodes come in Step 5.

```markdown
# Incident: [Title]
**Date:** YYYY-MM-DD
**Duration:** X hours (detect HH:MM → resolved HH:MM)
**Severity:** P0/P1/P2/P3
**Impact:** [What users experienced — scope, count, data-at-risk y/n]
**Incident ID / rollback ref:** [link to the /rollback report or incident log]

## Timeline
- HH:MM — [What happened]          (evidence: [log/alert/graph link])
- HH:MM — [What was detected/done] (evidence: ...)
- HH:MM — [Mitigated / resolved]

## Root Cause
[The 5-whys chain — see Step 2. The *systemic* cause, not the proximate symptom.]

## What Went Well
- [Detection/mitigation/tooling that helped — name it so we keep it]

## What Went Wrong
- [What made it worse or delayed resolution — systems/process, never a person]

## Action Items
- [ ] [Specific fix] — owner: @name — due: YYYY-MM-DD — → [[Risks/R-NNN-...]] | [[Questions/Q-NNN-...]]
- [ ] [Preventive measure] — owner: @name — due: YYYY-MM-DD — → [[Risks/R-NNN-...]]
```

5. **Blameless filter:** scrub the whole doc of individual blame. "The migration had no `down` and no canary" — not "Y forgot the rollback". If a human-error sentence is load-bearing, the real action item is the *guardrail that should have caught it* (a hook, a check, a default-off flag), per §19's "systems and processes".

### Step 2 — Root cause via 5-whys (evidence at each link)

6. Start from the **proximate symptom** (what the alert/users saw) and ask **why** until you reach a cause you can *act on systemically* — typically 5 steps, sometimes a branching tree for P0/P1 (separate chains for *why it broke*, *why detection was slow*, *why mitigation was slow*). Each **why** must cite evidence (a log line, the diff, a config), not a guess; an unsupported link is an assumption to verify, not a conclusion.

```
Symptom: 500s on checkout (alert 14:03, error-rate graph)
  → why? the new pricing service returned null (trace abc123)
    → why? a config key was renamed but the old name was still read (diff #482)
      → why? no schema/contract test on the config (test gap)
        → why? config isn't covered by the config-validation hook (process gap)
          → ROOT: config changes can reach prod with no contract check  ← act here
```

7. **Stop at an actionable systemic cause**, not at "human error" and not at infinite regress. The deepest *actionable* node(s) become the prevention action item(s). For high-stakes / contested causes, route the 5-whys through an adversarial `claim-verifier` pass (`pipeline.js` shape, default-to-refute) so a confident-wrong root cause doesn't drive the wrong fix — and you may add a **`/council`** cross-vendor read for a P0 with a disputed cause. **Codex is a READ-ONLY advisor; Claude owns the analysis and writes every node** (the council never auto-applies). Opt-in for high-stakes only — never per-postmortem.

### Step 3 — What went well / what went wrong

8. **What went well** — name the detection, tooling, runbook, or decision that helped, *concretely*, so it's deliberately kept (e.g. "the default-off flag let us kill the feature in 30s with no deploy" — reinforces §J.5). **What went wrong** — every friction that lengthened detection→mitigation→resolution, framed as a system/process gap. Each "went wrong" item should map to an action item in Step 4 (if it's worth complaining about, it's worth a tracked fix or an explicit accept-the-risk).

### Step 4 — Derive action items (each gets an owner + a deadline)

9. Turn the root cause(s) + "what went wrong" into **specific, owned, dated** action items. Every action item MUST have a **named owner** and a **deadline** (§19: "owners and deadlines, not just 'we should do X'"). Prefer the cheapest guardrail that makes the failure *structurally impossible or loud*, in this order:
   - **A regression test** that fails-first on the exact bug (designed/proven via `/mutation-check` + `/test-plan`) — the §19 / `/rollback` "feed the gap back" demand.
   - **A CI gate or hook** that would have blocked the change (the `no-spec-no-code`-style guardrail; a contract/config check).
   - **A default-off feature flag** so the same class of change can be killed without a revert (§J.5 / `/feature-flag`).
   - **An observability fix** — the missing alert/dashboard that would have cut detection time (§18).
   - **A doc/runbook fix** (`/deploy-checklist`, `/db-migrate`) only when the gap was genuinely procedural.
10. **De-dup against reality:** if the incident realized an **already-known** `Risks/` node, the action item *mitigates* that existing R-NNN (don't mint a duplicate). If it's a genuinely new failure surface, it's a new risk. If the fix is uncertain / needs investigation, it's a backlog `Questions/` (open), not a risk.

### Step 5 — File EACH action item as a graph node (the load-bearing move)

11. **This is what makes the postmortem matter.** For **every** action item, create or extend exactly one `Knowledge/<Project>/` node so it lives in the queryable graph and feeds planning — not just as a checkbox in a doc that rots. Use `/kb-capture`'s machinery (it conforms to `Knowledge/_schema.md`):

   - **Prevention / mitigation / now-known failure surface → a `risk` node** in `Risks/`, ID `R-NNN-<slug>`, `status: open` (or `accepted` if you're explicitly deciding *not* to fix and accepting it):

     ```yaml
     ---
     tags: [knowledge, <project-slug>, risk]
     created: YYYY-MM-DD
     type: risk
     status: open                 # open | mitigated | accepted
     project: <project-slug>
     owner: "@name"               # the action-item owner (named, not "the team")
     due: YYYY-MM-DD              # the action-item deadline
     part_of:   "[[<Project>/README]]"
     raised_by: "[[<Project>/Postmortems/<incident-slug>]]"   # the postmortem doc
     affects:   ["[[Components/<area>]]"]
     blocks:    "[[<thing it endangers>]]"        # if it gates other work
     mitigates: "[[Risks/R-0xx-...]]"             # ONLY if it addresses a prior risk (de-dup)
     ---
     ```
     Body: the failure mode, the realized impact (link the postmortem), and the **specific prevention** (the test / gate / flag / alert) that closes it. When the prevention lands and the regression test is green, flip `status: mitigated` and link the ADR/PR that did it.

   - **Open investigation / undecided fix → a backlog `question` node** in `Questions/`, ID `Q-NNN-<slug>`, `status: open`, same `owner:` + `due:` + `raised_by:` the postmortem. It seeds the next `/plan-tree` clarifying batch (decided questions are excluded; open ones get asked).

   - **A decision the incident forces** (e.g. "adopt canary deploys") → an **ADR** in `Decisions/` (`/kb-capture` Step 3, append-only) with `mitigates: "[[Risks/R-NNN-...]]"` pointing at the risk it closes.

12. **Delegate to the `knowledge-curator`** for the graph write (it owns multi-node edits + edge wiring; brief it with the node IDs, the `owner`/`due`, and the exact edges). Respect `/kb-capture`'s **velocity cap (=5 structural edits/cycle)** — if an incident spawns more, file the top risks now and queue the rest (noted in `Knowledge/EVOLUTION.md`). Append one `EVOLUTION.md` row per node created.

### Step 6 — Close the loop into planning (the whole point)

13. The graph nodes now feed planning automatically — this is `/kb-capture` **Step 6** and it is *why* Step 5 files nodes instead of checkboxes:
   - **Open `Risks/`** → the `plan-critic`'s **attack seed** and the next `/research-loop` failure-modes lens. The next plan is *attacked with this incident's root cause*, so a plan that would re-introduce it fails the critique.
   - **Open backlog `Questions/`** → the next `/plan-tree` clarifying-question batch.
   - **The owner + due** make each item trackable: surface the open postmortem risks/questions (with owners + deadlines) so the orchestrator can route them, and **review them in the next cycle** (§19: "review action items in the next cycle to ensure they're completed"). An action-item risk still `open` past its `due` is the review's first agenda item.
14. **Verify the loop is real, not claimed:** confirm each action item resolves to an actual node file with `owner` + `due` set and a `raised_by` edge back to the postmortem — the same "did you actually do the thing" bar as `/verify-work` (a postmortem whose action items aren't in the graph hasn't closed the loop). Then `git revert`-safe: the regression test that pins the bug is the proof the class of failure is now guarded.

## Iteration & STOP

- **Bounded, not a loop:** Steps 0→6 run once per incident. The only iteration is reviewing open action items each cycle until they close — not re-writing the postmortem.
- **Root-cause STOP:** the 5-whys stops at the first *actionable systemic* cause; do not regress to "human error" and do not chase infinite whys. Branch (not deepen) for P0/P1's separate break/detect/mitigate chains.
- **Blameless STOP-gate:** if any line names an individual as the cause, it isn't done — rewrite it as the missing guardrail before filing.
- **Done when:** the §19.2 doc is written and blameless; **every** action item is a graph node with a named **owner + deadline** and a `raised_by` edge to the postmortem; the open risks/questions are confirmed feeding `/kb-capture` Step 6; and the regression test that pins the root cause is green. A postmortem with un-filed or unowned action items is **not** done.
- **Quality is never the variable:** severity scales depth, never rigor; the 48h clock never justifies skipping the 5-whys or shipping ownerless action items.

## Outputs

- A **blameless §19.2 postmortem doc** (repo-side, `docs` agent) under `Knowledge/<Project>/Postmortems/<incident-slug>.md` (or the repo's runbook location), within 48h.
- **One graph node per action item** in `Knowledge/<Project>/{Risks,Questions,Decisions}/` — each with a named `owner`, a `due` date, and `raised_by` → the postmortem — plus an `EVOLUTION.md` row each (within the velocity cap).
- A **regression test** pinning the root cause (failing-first), green on the re-landed fix.
- A short report: severity, root cause (one line), action items with owner + due + their node IDs, and confirmation they're fed into `/kb-capture` Step 6.

## Reporting

After the postmortem, report explicitly:

```
Postmortem report -- incident <id> (<title>):

Severity:    P<0-3>   ·  Duration: detect HH:MM -> resolved HH:MM
Doc:         Knowledge/<Project>/Postmortems/<slug>.md  (blameless ✓, within 48h ✓)
Root cause:  <one line -- the actionable systemic cause from the 5-whys>
Regression test: <test name> -- fails-first on the bug, GREEN on re-landed fix

Action items (each owned + dated + filed as a node):
  - [ ] <fix> -- owner @name -- due YYYY-MM-DD -> Risks/R-0NN-<slug> (new, open)
  - [ ] <prevention> -- owner @name -- due YYYY-MM-DD -> Risks/R-0NN (mitigates R-0xx)
  - [ ] <investigation> -- owner @name -- due YYYY-MM-DD -> Questions/Q-0NN-<slug> (open)
Nodes filed: <N risks, M questions, K ADRs>  ·  EVOLUTION rows: <N>  ·  curator delegated: <y/n>
Fed to planning (kb-capture Step 6): <N open risks -> plan-critic attack seed, M open questions -> next plan-tree>
Queued (over velocity cap): <none | list>
```

Never claim "postmortem done" without the doc being blameless AND every action item existing as a graph node with an owner + deadline. Action items that aren't in the graph don't get done.

## See also

- `/incident-response` — the parent incident flow (detect → assess → mitigate → communicate → resolve → **learn**). This skill **is** the *Learn* step (§19.1 step 6); it runs once `/incident-response` resolves.
- `/rollback` — the §19 *Mitigate* step for merged code (revert-first). Its incident log + "feed the gap back" demand are this skill's inputs; the regression test it requires is this skill's first action item.
- `/kb-capture` — the machinery this skill files action items through (node types, frontmatter edges, `Knowledge/_schema.md`); **Step 6** is the feedback loop that carries the action-item risks/questions into planning. This skill is a specialized caller of it.
- `/plan-tree`, `/research-loop` — consume the filed `Risks/` (plan-critic attack seed / failure-modes lens) and open `Questions/` (clarifying batch). This is where the lesson actually changes future behavior.
- `/mutation-check`, `/test-plan` — design and prove the failing-first regression test that pins the root cause (the load-bearing prevention item).
- `/feature-flag`, `/deploy-checklist`, `/db-migrate` — sources of common prevention action items (default-off flags; tested rollback; expand-contract + `down` migrations).
- `/council` — optional cross-vendor read on a *disputed P0 root cause* only; Codex is a READ-ONLY advisor, Claude owns the analysis and writes the nodes. Opt-in per high-stakes incident, never per-postmortem.
- `agents/docs.md` (writes the repo-side postmortem doc/runbook), `agents/knowledge-curator.md` (files the graph nodes + edges), `agents/claim-verifier.md` (adversarial check on a contested root cause).
- `Knowledge/_schema.md` — the node/edge contract every action-item node conforms to (`risk` = `R-NNN`, `question` = `Q-NNN`, edges `raised_by` / `mitigates` / `blocks`).
- [[../Agent Workflow]] — §19 (Incident Response & Postmortems: §19.1 the 6 steps, §19.2 the template this skill fills, blameless + owned-action-items rules), §18 (Observability — the timeline's evidence), §J.5 (flags/rollback as prevention).
