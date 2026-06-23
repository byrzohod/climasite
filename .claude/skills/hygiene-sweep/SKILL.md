---
name: hygiene-sweep
description: End-of-phase + every /update-workflow de-bloat & accuracy pass over docs / ADRs / skills / CLAUDE.md / Knowledge. Finds DUPLICATION and single-sources it (the 13 review dimensions, the shared-infra rules, the FULL-AUTO/propose-only boundary, flow blocks), finds STALE / inaccurate / dead-link content, and protects the auto-loaded context budget. REPORT first, then apply within the velocity cap; high-blast-radius files (CLAUDE.md.template, hooks, CI, _schema.md, _views) are PROPOSE-ONLY (cool-off). Runs /flag-cleanup for stale flags. Use this whenever a phase just finished, /update-workflow runs, the user mentions de-bloat / bloat / cleanup / duplication / single-source / DRY the docs / stale docs / accuracy pass / context budget / docs drift / dead links / "these skills repeat each other", or before a release that ships doc/skill changes.
---

# /hygiene-sweep - De-bloat + accuracy pass that single-sources duplication and protects the context budget

The **housekeeping** skill for the workflow's own prose. Code has `/refactor` and `/flag-cleanup`; the *documentation surface* — `Agent Workflow.md`, the skills, `CLAUDE.md`, ADRs, `Knowledge/` — accretes the same debt: the same rule restated in five places (so a fix in one silently diverges from the other four), sections that went stale after a decision changed, dead `[[wikilinks]]`, and slow growth of the **auto-loaded context budget** that degrades every future agent's reasoning. This skill finds that debt, **single-sources** each duplicated rule to one canonical home and replaces the copies with a reference, prunes stale/inaccurate/dead content, and runs `/flag-cleanup` for stale feature flags — **REPORTING first, then applying within the velocity cap**, with high-blast-radius files held to **propose-only**.

It is the documentation-side twin of `kb-health`/`kb-capture` (which keep the *project knowledge graph* clean) and `skill-health`/`skill-eval` (which keep the *skills* sharp). This one keeps the **shared prose** accurate and lean. Blueprint §K.6 (Req #11).

## Why this exists

Two failure modes, both slow and both expensive:

1. **Duplication → drift.** The 13 review dimensions, the shared-infra rules, the velocity-cap/cool-off boundary, the trunk-merge flow — each was at one point copied into several skills "for convenience." The moment one copy is edited and the others aren't, readers (human and agent) get **contradictory instructions** and there is no single source of truth to arbitrate. The redesign's whole §K.1 closing rule — *"the FULL-AUTO vs propose-only boundary is declared ONCE; skills reference it (don't restate — discovery currently triple-maintains it)"* — is the lesson learned from exactly this.
2. **Bloat → degraded reasoning.** The auto-loaded surface (`CLAUDE.md`, `CLAUDE.md.template`) is read on **every** message; the master doc is read at the start of every project. Every stale paragraph and redundant restatement there is pure tax on reasoning quality (§"context hygiene": avoid *wasteful* context because bloat degrades reasoning, not because of token cost). The redesign's net-capability accounting (Req #11) is explicit that the auto-loaded budget must *shrink* — this skill is how it stays shrunk after each phase adds new prose.

The fix is the same as for code: **one canonical home per fact, every other mention is a reference.** This skill is the recurring enforcement of that.

## When to use

Invoke this skill:
- **At the end of every phase** — after `/kb-capture` writes the phase log, before the phase is called done. New prose just landed; sweep it for duplication and staleness while it's fresh (Blueprint §K.6, §F capture cadence).
- **On every `/update-workflow`** — the non-skippable maintenance loop (`skill-health → skill-eval → hygiene-sweep → Sessions/decision-log → commit`, §K.3). `/update-workflow` is also where its own flag-debt step fires `/flag-cleanup`.
- **Before a release that ships documentation or skill changes** — so the released docs are accurate and de-duplicated (pairs with `/release`).
- **On demand** when the user says de-bloat / bloat / cleanup / DRY the docs / single-source / stale docs / docs drift / dead links / context budget / "these skills repeat each other / contradict each other."

## When NOT to use

- **Mid-phase, work in flight.** Sweeping prose that's still being actively rewritten is churn — wait for the phase boundary. (Same "not yet structural" judgment as `kb-health` skipping a note you're still editing.)
- **There is genuinely nothing to sweep.** If the last sweep was recent and no new prose landed, don't invent cleanup work — report "clean, nothing to single-source or prune" and stop. (The `/flag-cleanup` and `kb-health` "don't invent work" rule.)
- **The change you'd make is behavior/policy, not hygiene.** Single-sourcing *moves* a rule and replaces copies with references — it does **not** change what the rule *says*. If a rule is wrong (not just duplicated/stale-by-drift), that's a deliberate decision: a `/workflow-evolve`-style edit with its own `_methodology.md` rationale and (for shared files) cool-off, not a silent hygiene edit. Same bar as `/refactor`/`/flag-cleanup`: **if the meaning changes, it's not hygiene.**
- **Code-level dead code / duplication.** That's `/refactor` (behavior-preserving restructuring) and `/flag-cleanup` (dead flag branches). This skill owns *prose*: docs, ADRs, skill bodies, `CLAUDE.md`, `Knowledge/` notes. (It *invokes* `/flag-cleanup` for the flag-debt step; it does not do AST rewrites itself.)
- **The knowledge graph's structural integrity.** Orphan nodes, broken frontmatter edges, schema violations are `kb-health`'s job. This skill checks `Knowledge/` for the *prose-level* problems (duplication, staleness, dead body-links) and **defers graph-structure findings to `kb-health`** rather than re-auditing them.

## The non-negotiable boundary: REPORT first, then apply within caps; high-blast-radius = propose-only

This skill is the rare one that **both detects and acts** — so it must hold itself to the workflow's engine rules exactly (declared once in the principles doc; this skill *references* them, it does not restate the boundary's contents):

- **REPORT before APPLY.** Always produce the full findings report first (every duplication cluster, every stale/inaccurate item, every dead link, the flag-debt list). Acting before reporting hides what was changed and defeats review. The report is the artifact even when nothing is applied.
- **Velocity cap: ≤5 structural edits per cycle.** A *structural edit* here = consolidating one duplication cluster to its canonical home (with the copies replaced by references), removing/rewriting one stale section, or fixing one dead-link cluster. **Queue the rest** for the next sweep (list them in the report). Each `/flag-cleanup` PR counts toward the cap. Velocity caps reduce drift — the build blast radius is *higher* than discovery's (the template copies into many projects), so the cap matters more here.
- **Cool-off (propose-only) on high-blast-radius files.** Edits to the files in the auto-editable-vs-propose-only boundary's **propose-only** set — `CLAUDE.md.template`, `settings.json` hooks, the CI templates, `Knowledge/_schema.md`, `_views/*.md` files, and **all auto-memory content** — are **NOT applied by this skill.** They are surfaced as proposals (in `Knowledge/EVOLUTION.md` as a cool-off row and/or the `/update-workflow` Sessions/decision-log entry) for a human to land after cool-off. This skill **may** auto-apply to the **auto-editable** set: individual skill bodies, `_methodology.md`, `EVOLUTION.md`, and individual `Knowledge/` notes. The exact membership of each set is **single-sourced in the principles doc — read it, don't trust a copy here.**
- **HALT health-gate.** If the trailing 30 days already hold **>5 `EVOLUTION` entries**, the workflow is changing too fast: **stop applying**, report only, and surface for human review (§K.1 HALT gate). Same rule the discovery engine and `kb-health` enforce.
- **Detect-vs-act ordering.** Run the relevant **`-health` audits BEFORE this sweep applies anything** ("evolution off stale state is drift"): `kb-health` for the `Knowledge/` scope, and read `skill-health`'s latest report for the skills scope if one exists. A health report is a *detector*; this skill is the rare *actor* — keep that separation visible (don't let this skill's acting mask a health finding it didn't surface).

This is the same posture as `/flag-cleanup` (mechanical work by the AI, human gate on broad blast radius) and `/workflow-evolve` (auto-apply the safe set, propose-only the shared set, log every edit, never exceed the cap).

## What to single-source (the canonical homes)

The point of the sweep is **one fact, one home.** When the same rule appears in N places, pick the canonical home, keep it there in full, and replace the other N−1 with a one-line reference. Known clusters and their canonical homes — confirm each against the live files each run, don't trust this list blindly:

| Duplicated thing | Canonical home (keep full here) | Everyone else (reference, don't restate) |
|---|---|---|
| **The 13 review dimensions** | `Agent Workflow.md` §6 + `agents/reviewer.md` | `code-review.md`, `review-orchestrate.md` — already say "single-sourced in §6 / reviewer.md; do NOT restate." Enforce that any *new* skill listing dimensions collapses to a reference. |
| **Shared-infra rules** (shared stack location, per-project DB, never in tests/CI/perf/prod, add-new-service-here) | `Agent Workflow.md` §15.3 | `project-setup.md`, `test-plan.md`, `autoresearch.md`, `CLAUDE.md.template` — keep a one-line pointer + the single load-bearing line, not the full ruleset. (`CLAUDE.md.template` edits are **propose-only**.) |
| **FULL-AUTO vs propose-only boundary** (auto-editable set, cool-off set, velocity cap, HALT gate) | the principles doc (`Agent Workflow.md` §9.6 / self-evolution section), declared **once** | every `*-health`/`*-eval`/`hygiene-sweep`/`workflow-evolve`/`kb-*` skill — *reference* it; the §K.1 closing rule forbids restating it (discovery's triple-maintenance is the anti-example). |
| **Trunk delivery / merge-queue / flow blocks** | `Agent Workflow.md` §3 + the owning skill (`trunk-merge.md`) | other skills mention the flow in one line and link, not re-describe the whole queue. |
| **Perf / a11y thresholds** | `/perf-budget`, `/accessibility-audit` (CI reads the same source) | CI templates + `review-orchestrate` angles reference; don't hardcode the numbers in a second place. |
| **OWASP / threat-model checklists** | `/security-review`, `/threat-model` | review angles + `data-classify` reference the deep skill, don't inline the list. |
| **EVOLUTION schema + velocity cap text** | `EVOLUTION.md` header (template) / `Knowledge/EVOLUTION.md` header (project) | skills cite it; don't paste the table format into each skill body. |

When you find a **new** cluster not in this table, the canonical home is: the **owning skill** if one rule belongs to one skill; otherwise the **principles doc** (`Agent Workflow.md`) for cross-cutting rules; otherwise the **schema/template** for contracts. Then update *this table* (it lives in this skill body — an auto-editable file — so keeping it current is in-scope and within the cap).

## Process

### Step 0 — Scope + preconditions

1. **Set the scope.** Default scope = everything that accreted since the last sweep: `Agent Workflow.md`, `AI/project-template/skills/*`, `AI/project-template/agents/*`, the project `CLAUDE.md`(s), ADRs (`Knowledge/<Project>/Decisions/`), and `Knowledge/<Project>/` notes. On `/update-workflow` the scope is the whole template; at end-of-phase, bias to the files the phase touched (read the phase log / `git diff` for the phase range) plus the always-shared files.
2. **Run/read the detectors first (detect-vs-act).** Run `kb-health` for the `Knowledge/` scope and read the most recent `skill-health` report if present. Note their findings — this sweep must not *act* in a way that masks a *detector's* finding it didn't independently surface.
3. **Check the HALT gate.** Count `EVOLUTION` entries in the trailing 30 days (template `EVOLUTION.md` + relevant `Knowledge/EVOLUTION.md`). **>5 → report-only mode for this run**, no edits applied; say so at the top of the report.

### Step 1 — Detect (collect findings; change nothing yet)

Run all passes, collect everything, group by category for the report.

**Pass A — Duplication / single-source.** Find the same rule, list, checklist, or flow restated in more than one place.
- Cross-reference the **canonical-homes table** above: for each known cluster, grep for the telltale phrasing across `skills/`, `agents/`, `Agent Workflow.md`, `CLAUDE.md*` and list every copy that is a *restatement* rather than a *reference*.
- Hunt **new** clusters: near-identical paragraphs/checklists across two+ files (the 13-dimension list, the OWASP list, the shared-infra block, the velocity-cap/cool-off boundary, trunk-flow descriptions, perf/a11y numbers). A reference (one line + a link) is fine; a *full restatement* is the finding.
- For each cluster, record: the canonical home, every copy's `path:line`, and whether the copies have already **diverged** (diverged copies are higher severity — they're now contradictory).

**Pass B — Staleness / inaccuracy.** Find content that no longer matches reality.
- **Superseded decisions:** an ADR or doc paragraph contradicted by a later `accepted` ADR (cross-check `Knowledge/<Project>/Decisions/` supersede chains — `kb-health` surfaces broken lineage; you surface the *prose that still describes the old decision*).
- **Stale model/tooling/version mentions** (the recurring footgun — e.g. an old model name, a removed plugin still referenced, a renamed skill, a tool that was un-pinned/removed). Cross-check against the current principles doc + memory.
- **`last_reviewed`/`created` frontmatter** far out of date on a file whose body clearly changed.
- **References to files/skills/agents that were renamed or deleted** (a `See also` pointing at a moved skill; a `CLAUDE.md` skill list out of sync with `skills/`).
- **Contradictions** — two places stating opposite rules (often the downstream symptom of un-single-sourced duplication that then diverged).

**Pass C — Dead links / broken references.** Find `[[wikilinks]]`, relative skill paths, and §-anchors that don't resolve.
- Resolve every `[[wikilink]]` and `skills/<name>.md`/`agents/<name>.md` reference against the actual file tree; resolve `§N`/`#anchor` references against the target doc's headings.
- Distinguish **frontmatter-edge breaks** (defer to `kb-health` — it owns graph edges) from **prose/`See also` breaks** (yours to fix).

**Pass D — Context-budget bloat.** Find growth in the always-read surface.
- **Auto-loaded files** (`CLAUDE.md`, `CLAUDE.md.template`) growing past their stated budget (`CLAUDE.md.template` target ~200-300 lines; principles doc target the redesign sets) — flag verbose passages that should be a reference to an on-demand skill instead of inline prose.
- **Detail that belongs in a skill, not the master doc / `CLAUDE.md`** — the workflow's architecture is "short principles in the master doc, detailed procedures in skills that load on-demand." A long procedure inlined into an auto-loaded file is bloat; the fix is *extract → reference* (the §§28-49 → `reference/*.md` pattern, the §8.2 UI-catalog CUT→SKILL pattern).
- **Dead weight:** commented-out blocks, "scar tissue" about removed things kept longer than useful, empty/placeholder sections.

**Pass E — Flag debt (delegate the find to the flag system).** Read the flag registry (what `/feature-flag` writes) for flags **past expiry or decided-permanent**. Do **not** rewrite code here — this pass produces the candidate list and hands it to Step 2's `/flag-cleanup` invocation.

### Step 2 — Report (always, before any edit)

Emit the report (format below) with every finding grouped by category and severity, the proposed canonical home for each duplication cluster, and the explicit **apply plan vs queue plan vs propose-only plan** (which ≤5 you'll apply this cycle, which you're queuing, which are propose-only because they hit high-blast-radius files or because the HALT gate tripped). The report is the deliverable even if zero edits are applied.

### Step 3 — Apply (auto-editable set only, within the cap)

Only if the HALT gate is clear. For the ≤5 highest-leverage findings that touch **auto-editable** files only:

1. **Single-source a duplication cluster:** ensure the canonical home holds the full, correct text; replace each copy with a **one-line reference** (e.g. `The 13 review dimensions are single-sourced in Agent Workflow.md §6 / agents/reviewer.md — not restated here.`). **Preserve meaning exactly** — this is a move, not a rewrite. If the copies had *diverged*, the canonical text must be the *correct* version; reconciling a genuine contradiction is a decision (log the rationale in `_methodology.md`), not a silent merge.
2. **Prune/repair stale or dead content:** rewrite the stale passage to match the current decision (cite the superseding ADR), or remove dead scar tissue; fix the dead `See also`/wikilink to its new target.
3. **De-bloat:** extract an over-long inlined procedure into the owning skill and leave a reference — **but** if the file is auto-loaded (`CLAUDE.md*`) it's propose-only; queue it for Step 4 instead.
4. **Log each edit to `EVOLUTION.md`** (the template's `AI/project-template/EVOLUTION.md`, or the relevant `Knowledge/EVOLUTION.md`) using the **fixed schema — never reformat it**: `Date / File / Section / Before (one line) / After (one line) / Why (+ what drove it: phase, /update-workflow, lesson) / Reversibility (backfill the short commit SHA post-commit)`. One row per structural edit. No edit without a row — the audit trail is non-negotiable.
5. **Record the lesson** in `_methodology.md` when the sweep reveals a *recurring* hygiene pattern (e.g. "every phase re-restates the shared-infra block — add a lint check"), so the system gets better at not creating the debt.

### Step 4 — Surface propose-only + queued items

For findings that touch **propose-only** files (`CLAUDE.md.template`, hooks, CI, `_schema.md`, `_views/*.md`, memory) or that exceeded the ≤5 cap:
- Write them as **proposals**, not edits: a cool-off row in `Knowledge/EVOLUTION.md` (with an **explicit expiry** — don't let cool-off items rot the way discovery's one-month-stale entry did) and/or into the `/update-workflow` Sessions/decision-log entry for a human to land.
- **Memory** findings (stale dated entries in `MEMORY.md`, contradictions) are **always** propose-only — surface them; never auto-edit memory (it's user-owned). This overlaps `skill-health`'s memory sub-check (K.5); don't double-apply, just report.

### Step 5 — Run /flag-cleanup for stale flags

Hand Step 1 Pass E's candidate list to **`/flag-cleanup`** (this skill's flag-debt step, Blueprint §J.5/§K.6). `/flag-cleanup` does the AST rewrite and **opens a review-gated PR per flag — never merges.** Each PR it opens counts toward this sweep's velocity cap. If there are zero overdue flags, it reports "no stale flags" and this step is a no-op. Carry flags over the one-PR-per-flag cap forward to the next sweep.

### Step 6 — Verify + close

Before closing:
- **No meaning changed** by any single-sourcing edit (a hygiene move must be behavior-preserving for prose: a reader following the reference lands on the same rule). Spot-read each consolidated cluster's canonical home + one reference.
- **No new dead links** introduced by the moves (re-resolve the references you wrote).
- **Every applied edit has an `EVOLUTION` row**; every propose-only/queued item is surfaced; the cap (≤5) was respected.
- **No propose-only file was auto-edited.** Re-check the touched-files list against the propose-only set.
- Commit happens via the caller (`/update-workflow`'s loop, or `/vault-save`); backfill the `EVOLUTION` SHAs after the commit.

## Reporting

After running, report explicitly:

```
hygiene-sweep report  (REPORT-first; applied within velocity cap)

Scope: <phase-N files | full template on /update-workflow>   Run: 2026-MM-DD HH:MM
Detectors read first: kb-health <clean|N findings> · skill-health <report date|none>
HALT gate: <N> EVOLUTION entries / 30d  -> <clear | TRIPPED: report-only this run>

DUPLICATION (single-source):
  - [DIVERGED] shared-infra rules restated in test-plan.md:85 + autoresearch.md:246 (drifted from §15.3)
       canonical home: Agent Workflow.md §15.3  -> APPLY: replace both with one-line reference
  - [COPY] velocity-cap boundary restated in 3 *-health skills        -> canonical: principles §9.6; APPLY 1, QUEUE 2 (cap)
STALE / INACCURATE:
  - [SUPERSEDED] design-doc.md §X describes ADR-004 choice; ADR-011 superseded it -> APPLY: rewrite + cite ADR-011
  - [STALE-REF] CLAUDE.md skill list names `ui-ux-pro-max` (renamed)         -> PROPOSE-ONLY (CLAUDE.md.template)
DEAD LINKS:
  - [BROKEN] review-orchestrate.md See also -> agents/reviewer-old.md (moved)  -> APPLY: fix target
CONTEXT BUDGET:
  - [BLOAT] CLAUDE.md.template +60 lines over ~300 target; §X is a full procedure -> PROPOSE-ONLY: extract to skill
FLAG DEBT (-> /flag-cleanup):
  - temp-new-checkout expired 2026-05-01, permanently ON  -> /flag-cleanup opened PR #482 (NOT merged)
  - temp-search-v2 expired, kept-side unclear            -> needs human decision (skipped)

Applied this cycle (<=5): 4   |  Queued (over cap): 2  |  Propose-only (high-blast-radius/memory): 3
EVOLUTION rows appended: 4 (SHAs backfill post-commit)   |  _methodology lesson logged: 1
flag-cleanup: 1 PR opened (queued for human review), 1 needs decision

Nothing else to single-source or prune this run.  (or: list carried-forward)
```

Never report something "single-sourced" without naming the canonical home and the references that replaced the copies; never report an edit applied without an `EVOLUTION` row; never report a flag "removed" — `/flag-cleanup` opens a PR, it does not merge.

## Iteration & STOP

- **One bounded pass per cycle**, not a loop: detect → report → apply ≤5 (auto-editable only) → surface the rest → run `/flag-cleanup` → verify → stop. The next phase / next `/update-workflow` is the next cycle; carried-forward items are picked up then.
- **STOP** when the report is emitted, the ≤5 cap's worth of auto-editable edits are applied (each with an `EVOLUTION` row), propose-only + queued items are surfaced, and `/flag-cleanup` has run. **Do not** keep going past the cap "while you're in there" — that's the drift the cap exists to prevent.
- **HALT** (report-only, no edits) if the >5-EVOLUTION-entries/30-days gate tripped, or if `kb-health` returned a BLOCKER in the `Knowledge/` scope you'd touch — fix the blocker deliberately first, don't sweep over it.
- **Defer, don't act,** on anything that's a *decision* (reconciling a genuine contradiction, changing what a rule says) or a *propose-only* file — surface it for the human/cool-off path. Hygiene moves meaning to one place; it never changes the meaning.

## See also

- `/flag-cleanup` — this skill's **flag-debt step**: AST-based, review-gated removal of stale/expired flags; runs in Step 5; opens one PR per flag, never merges. (Blueprint §J.5/§K.6.)
- `/feature-flag` — produces the flag registry (expiry + `temp-` prefix) that Step 1 Pass E reads.
- `kb-health` — the REPORT-only graph auditor; run **before** this sweep for the `Knowledge/` scope; owns graph-structure findings (orphans, broken frontmatter edges, schema) this skill defers to it.
- `kb-capture` — writes the phase log + `Knowledge/EVOLUTION.md` rows; runs **before** this skill at end-of-phase.
- `skill-health` / `skill-eval` — the skills-quality twins (detect / act); `skill-health`'s memory sub-check (K.5) owns the `MEMORY.md` audit this skill only reports, never applies. `metrics` (DORA-lite) supplies objective signal to the same loop.
- `/workflow-evolve` (discovery engine) — the structural-sibling self-improvement loop this ports: auto-apply the safe set, propose-only the shared set, log every edit, never exceed the cap, HALT on drift.
- `/refactor` — the behavior-preserving **code** twin; same "if the meaning changes, it's not hygiene" bar applied to source.
- `/update-workflow` — the recurring caller; wires `skill-health → skill-eval → hygiene-sweep → Sessions/decision-log → commit` (§K.3) and is where the flag-debt step fires.
- `/release` — run this before a release that ships doc/skill changes so the released prose is accurate + de-duplicated.
- `EVOLUTION.md` (template) / `Knowledge/EVOLUTION.md` (project) — the fixed-schema audit trail every applied edit appends to; `_methodology.md` — the lessons log recurring hygiene patterns feed.
- [[../Agent Workflow]] — §9.6 (self-evolution engine: the FULL-AUTO/propose-only boundary, velocity cap, HALT gate, detect-vs-act — all **single-sourced there**), §6 (the 13 review dimensions), §15.3 (shared-infra rules), §3 (trunk delivery). Blueprint §K.6 / §K.1 are the design source.
