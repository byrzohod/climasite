---
name: skill-health
description: REPORT-ONLY audit of the skill/agent/template system + auto-memory against the self-evolution boundary (Agent Workflow §9.6). Catches stale skills, never-triggered skills, skills whose output keeps getting user-edited, CLAUDE.md.template drift, agent/skill description-trigger overlap, missing/malformed frontmatter, a memory-health sub-check on MEMORY.md (staleness + contradiction), and EVOLUTION velocity-cap breaches (>5 entries/30d → recommend HALT). Run it BEFORE /skill-eval — it is the detect side that gates the act side. It NEVER edits files; it emits a findings report grouped by severity and a gate verdict. Same detect-vs-act independence as /skill-verifier and /kb-health.
---

# /skill-health - REPORT-ONLY audit of the skill system + memory

## When to use

Run **before any evolution of the skill/agent/template system** — never evolve off stale state ("evolution off stale state is drift", Blueprint §K.1). Specifically:

- **Before `/skill-eval`** — this skill is the gate. `/skill-eval` does not promote a quarantined skill, refactor a skill body, adjust a hook threshold, or append an `EVOLUTION.md` row until `/skill-health` is clean (or its findings are explicitly accepted by the user).
- **End of a unit / phase**, and **every `/update-workflow`** — the non-skippable loop is `skill-health → skill-eval → Sessions/decision-log entry → commit` (Blueprint §K.3). This skill is its first step.
- **Ad hoc** when a skill stops triggering when it should, an agent and a skill keep both grabbing the same request, or memory starts contradicting itself ("which project am I building again?").

Skip for: a single skill you're still drafting in-session (it's not yet structural — it isn't promoted, isn't referenced by CLAUDE.md, and `/skill-eval` hasn't been asked to act on it).

## What this skill is NOT

- It **does not edit, refactor, promote, demote, rename, or delete** any skill, agent, hook, template, `EVOLUTION.md`, `_methodology.md`, or memory file. **Read-only.**
- It **does not promote** quarantined skills from `skills/_proposed/`, **does not rewrite** stale skill bodies, **does not fix** description overlap, **does not update** memory. It *detects and reports*; **`/skill-eval`** (with the user's go-ahead, and only within the FULL-AUTO scope) *acts*. Memory *content* edits are propose-only even for `/skill-eval` — they stay user-owned (Blueprint §K.5).
- This detect-vs-act separation is the whole point: the auditor must not share the bias of the agent that will do the fixing (Agent Workflow §5.7, self-review optimism). Same pattern as [[skill-verifier]] (audits a skill's *output claims*) and [[kb-health]] (audits the *knowledge graph*). This skill audits the *skill system itself*.

## Ground truth

Three contracts, read fresh each run — never audit from memory:

1. **The self-evolution boundary** — declared **once** in the principles doc (`vault/AI/Agent Workflow.md`, §9.6 self-evolution) and restated in this conversation's locked instructions. The split it enforces:
   - **AUTO-editable** (by `/skill-eval`): individual **skill bodies**, `_methodology.md` lessons log, `EVOLUTION.md`, individual `Knowledge/` notes.
   - **PROPOSE-ONLY** (cool-off, needs a human via a Sessions/decision-log entry): `CLAUDE.md.template`, `settings.json` hooks, CI templates (`templates/ci/*`), `Knowledge/_schema.md`, `.base` / `_views/*`, and **ALL auto-memory content**.
   - **Velocity cap:** ≤5 structural edits/cycle. **HALT gate:** >5 `EVOLUTION.md` entries / 30 days → halt evolution until human review.
   - **Dynamic skills (§K.4):** AI may only DRAFT into `skills/_proposed/` (INERT until a human promotes); generated skills may not add MCP servers, network calls, or unscoped Bash; each must pass `/skill-verifier` before promotion.
   If the principles doc and this skill ever disagree, **the principles doc wins** — flag the drift as a `BOUNDARY` finding rather than silently following this skill.
2. **The `EVOLUTION.md` fixed schema** — `File / Section / Before / After / Why (+source citation) / Reversibility (post-commit SHA)` (Blueprint §K.1). The velocity-cap and HALT-gate checks count entries against this schema, so a free-form/un-converted log is itself a finding.
3. **The skill spec** — the frontmatter contract every house skill follows: `name` (kebab-case, matching the slash command) + a "pushy", keyword-rich `description` carrying both WHAT and WHEN (per the `skill-creator` standard the template adopted). [[skill-verifier]] enforces output claims; this skill enforces the *metadata + lifecycle* of the skill catalog.

`metrics` (DORA-lite from git + CI: change-fail rate, rework, lead-time, escaped-defect on AI-heavy changes) supplies the **objective signal** so this audit isn't vibes (Blueprint §K.3). Treat its numbers as an **input signal, never a KPI** (Goodhart) — they *corroborate* a staleness/churn finding, they don't *manufacture* one.

## Scope

Audit, for the project at hand:
- `.claude/skills/*.md` (the copied house skills + any project-local ones) and `.claude/skills/_proposed/*` (the INERT quarantine).
- `.claude/agents/*.md` (role-based subagents — for description-overlap with skills).
- `.claude/settings.json` hooks and `CLAUDE.md` vs the source `CLAUDE.md.template` (drift).
- `EVOLUTION.md` and `_methodology.md` (velocity, schema conformance, dangling action items).
- **Auto-memory** (`MEMORY.md` + the per-fact notes it indexes) — the **memory-health sub-check** (§K.5).

When auditing the **template itself** (the `vault/AI/project-template/` source rather than a project copy), the same passes apply to `vault/AI/project-template/{skills,agents,hooks,templates,EVOLUTION.md,_methodology.md}` and the boundary's PROPOSE-ONLY list maps to those source files. State which mode you're in (template-source vs project-copy) at the top of the report — the gate verdict differs (editing template source is higher blast-radius).

## Detection passes

Run all passes, collect findings, then group by severity. grep/bash mechanics below are illustrative — read files with the Read tool and resolve references against the actual tree. **Read-only**: never invoke a skill or hook to "test" it; inspect its definition.

### 1. Stale skills
A skill whose body references tools, agents, file paths, model ids, commands, or conventions that no longer exist or have changed.
- **Dangling reference:** a skill that points at an agent (`agents/<x>.md`), a sibling skill, a template path, or a CI file that isn't on disk (renamed/removed and not relinked).
- **Stale model / tooling:** hard-coded model ids, removed plugins (e.g., a re-mention of the removed GSD/Caveman plugins), or a tool/MCP the project no longer has — contradicting the locked "Opus 4.8, trusted-source-only" posture.
- **Contract drift:** a skill that restates a rule which the single source of truth (principles doc, `_schema.md`, the 13 review dimensions) has since changed — duplicated rules that have diverged.
- **Age + untouched-while-deps-moved:** a skill not edited in a long window whose dependencies (an agent it spawns, a CI stack it assumes) *did* change — likely silently broken. (`git log -1 --format=%cs -- <skill>` vs the dep's last change.)

### 2. Never-triggered skills
A skill that exists in the catalog but shows **no evidence of ever firing** in this project.
- Cross-check skill names against `Sessions/`/decision-log entries, `.planning/` docs, PR/commit bodies, and `EVOLUTION.md` for any mention of the slash command or its outputs. Zero hits since it was installed → never-triggered.
- Distinguish **legitimately-dormant** (a domain concern that simply doesn't apply to this project — e.g., `data-classify` dormant because the project handles no personal data; the domain-concern matrix says it's correctly inactive) from a **genuine dead skill** (in scope for this tier, relevant work happened, yet it never fired → its `description` is mis-tuned and *undertriggers*, the documented failure mode `skill-creator` warns about).
- A never-triggered skill is not auto-removed here — it's a signal that `/skill-eval` should retune the description (or the user should prune it). Report which.

### 3. Skills whose output keeps getting user-edited (churn)
A skill whose produced artifacts the **user repeatedly hand-edits right after the skill runs** — the strongest promotion signal in the engine (the discovery `/workflow-evolve` table: "skill output user edited manually ≥2 times → refactor the skill").
- Heuristic: for each skill that writes a file (`/design-doc` → `DESIGN.md`, `/plan-tree` → `.planning/*`, `/release` → CHANGELOG, etc.), look at git history for a **skill-authored commit immediately followed by a human commit** touching the same artifact, ≥2 times. (`git log --follow --format='%an %cs %s' -- <artifact>` and look for the agent→human alternation.)
- This is the "the skill is *almost* right but the human always patches the same gap" pattern. Report the artifact, the count, and the *kind* of edit if discernible (e.g., "user always adds a Rollback section that `/design-doc` omits").
- **Do not refactor the skill** — that's `/skill-eval`'s call once the pattern clears the ≥2 threshold.

### 4. Template drift (`CLAUDE.md` ⟂ `CLAUDE.md.template`; copied skills ⟂ source)
- **CLAUDE drift:** the project's `CLAUDE.md` has diverged from the source `CLAUDE.md.template` (or its human-readable `CLAUDE.md.template.original.md`) in ways that look like *unported template improvements* or *stale project text*, not deliberate per-project customization. Diff structure/sections, not prose the project legitimately owns.
- **Skill-copy drift:** a house skill copied into `.claude/skills/` has drifted from its `vault/AI/project-template/skills/` source (project got a local hotfix that was never upstreamed, OR the template improved and the copy is stale). Report direction (copy-ahead vs source-ahead).
- **`settings.json` hook drift:** project hooks differ from `templates/settings.json` — especially a **missing protective hook** (the Stop hook, the 5 PreToolUse guards, `no-spec-no-code`, `dynamic-skill-quarantine`). A missing `dynamic-skill-quarantine` hook while `skills/_proposed/` is non-empty is a **BLOCKER** (the INERT guarantee for §K.4 dynamic skills isn't actually enforced).
- **Frontmatter contract:** any skill missing `name`/`description`, a `name` that doesn't match its slash command, or a `description` that's the terse procedure-doc style with no triggering keywords (undertriggers by construction).

### 5. Agent ⟂ skill description-trigger overlap
Two definitions whose `description` triggers compete for the **same** request → the harness picks unpredictably, or both fire and duplicate work.
- **Agent vs skill:** a subagent (`agents/*.md`) and a skill (`skills/*.md`) with overlapping trigger surface (e.g., a `security` agent and the `/security-review` skill both claiming "OWASP, auth, secrets"). The intended split is *skill = procedure/checklist, agent = the executor a skill spawns* — flag where a description blurs that so they collide instead of compose.
- **Skill vs skill:** two skills with near-duplicate trigger keywords (e.g., two review skills both owning "review the diff"). Report the overlapping keyword set and which requests would be ambiguous.
- Heuristic: extract the `description` line from each skill/agent, look for high keyword overlap among trigger nouns/verbs. Report *pairs to disambiguate*; the actual description retune is `/skill-eval`'s job.

### 6. Memory health sub-check (MEMORY.md) — Blueprint §K.5
Audit **auto-memory** for staleness and contradiction. Memory *content* is **PROPOSE-ONLY / user-owned** — this sub-check **only reports**; even `/skill-eval` proposes memory deltas via a Sessions/decision-log entry, never auto-writes them.
- **Index integrity:** every file in the memory dir has a row in `MEMORY.md`, and every `MEMORY.md` row resolves to an existing file (no phantom index entries, no unindexed notes). Each note has the required frontmatter (`created`, a `description`).
- **Staleness:** dated facts gone cold — a memory pinned to an old date that newer reality has overtaken. (`grep` the dated/“active” lines.)
- **Contradiction (the headline §K.5 case):** two memory entries that can't both be current. **Live example to expect:** `project_active_build.md` says *"2026-05-22: W3 AI Sports Coach primary"* while `project_ai_sonar.md` adds *"2026-05-24: AI Sonar … active build"* — two "active builds" with no stated relationship. Flag mutually-exclusive "current state" claims and ask which supersedes (do **not** pick for the user).
- **Boundary leakage:** a memory note that records something belonging in a skill/`_methodology.md`/`Knowledge/` instead (memory should hold durable user/profile facts, not transient build lessons).
- **Velocity:** memory edits are subject to the **same ≤5/cycle velocity cap**; if the trailing window shows a burst of memory churn, flag it (memory thrash is its own drift smell).
- Output of this sub-check is a *proposal list* for the user — phrased as "consider updating/merging X and Y", never an applied change.

### 7. EVOLUTION velocity-cap & HALT-gate breach
The hard governor on the whole engine (Blueprint §K.1; this conversation's locked velocity cap).
- **Per-cycle cap:** >5 structural edits proposed/applied in the current cycle → over cap; the excess must be queued, not applied.
- **30-day HALT gate:** count `EVOLUTION.md` entries dated within the trailing 30 days. **>5 → recommend HALT**: `/skill-eval` (and any structural evolution) must stop until the user reviews. This is a **BLOCKER** — it's the system's "you're changing yourself too fast" circuit breaker, identical to the discovery `/discovery-health` Check 8 and `/workflow-evolve` Step 7 gate.
- **Schema conformance:** `EVOLUTION.md` entries that don't carry all six fixed fields (`File / Section / Before / After / Why+citation / Reversibility-SHA`), or whose `Reversibility` SHA was never backfilled post-commit, or **cool-off items past an unstated/blown expiry** (the discovery log has a cool-off entry that sat ~1 month unresolved — do not repeat). A still-free-form `EVOLUTION.md` (not yet converted to the fixed schema) is itself a `WARN` because the count-based gate can't run reliably against it.
- **Quarantine integrity (§K.4):** anything in `skills/_proposed/` that (a) was promoted into `.claude/skills/` without a matching `EVOLUTION.md` row + a passing `/skill-verifier` eval, or (b) adds an MCP server / network call / unscoped Bash → **BLOCKER** (a dynamic skill escaped the safety rails that replaced GSD/Caveman).

### 8. Dangling methodology action items
Read `_methodology.md`'s lessons log for queued actions ("promote to rubric next cycle", "retune X's description", "watch for Y over next 5 sessions"). Any item **outstanding past its stated window** (or >30 days with none) → flag, so `/skill-eval` either actions it or the user retires it. (Mirrors `/discovery-health` Check 5.) If no `_methodology.md` exists yet (Phase 5.1 not landed), report that as the gap.

## Severity model

| Severity | Meaning | Examples |
|----------|---------|----------|
| **BLOCKER** | The engine's safety rails are breached or about to be; `/skill-eval` must NOT proceed until resolved or the user explicitly accepts the risk. | >5 EVOLUTION entries/30d (HALT gate); a `_proposed/` skill promoted without `/skill-verifier` + EVOLUTION row; a dynamic skill adding MCP/network/unscoped-Bash; missing `dynamic-skill-quarantine` hook while `_proposed/` is non-empty; an AUTO edit that actually targets a PROPOSE-ONLY file (`BOUNDARY`). |
| **WARN** | Real system debt; fix soon, doesn't hard-block this cycle if acknowledged. | Stale skill (dangling ref / drifted contract); churn pattern ≥2 (refactor candidate); template/CLAUDE/skill-copy drift; agent⟂skill description overlap; memory staleness/contradiction; free-form (un-converted) EVOLUTION.md; dangling methodology action item. |
| **INFO** | Hygiene / future risk; record, don't gate. | Legitimately-dormant tier-gated skill (noted, not actioned); minor frontmatter nits; single-occurrence output edit (below the ≥2 churn threshold); `metrics` signal worth watching but not yet conclusive. |

## Procedure

1. **Confirm scope + mode** — which project (or the template source), and state template-source vs project-copy at the top of the report.
2. **Snapshot the trees** (paths only) for `.claude/skills` (+ `_proposed`), `.claude/agents`, the memory dir, and the `vault/AI/project-template/` source — to resolve references and detect drift.
3. **Read the three contracts fresh** — the principles-doc self-evolution boundary, the `EVOLUTION.md` schema, the skill frontmatter spec. Never audit from memory.
4. **Pull the objective signal** — `metrics` (DORA-lite) if available, to corroborate staleness/churn findings. Input signal only.
5. **Run passes 1–8.** Read each skill/agent/memory file; diff copies against source; count EVOLUTION entries by date. Resolve every reference against the snapshotted trees.
6. **Group findings by severity**, dedupe, emit the report (format below).
7. **State the gate verdict** explicitly. **Change no file.**

## Output

REPORT-ONLY. Emit findings inline (and, if the caller is running the §K.3 loop, into that Sessions/decision-log entry). Never write fixes.

```markdown
## skill-health audit (REPORT-ONLY — no files modified)

**Mode**: project-copy <project> (or: template-source)   **Run**: 2026-MM-DD HH:MM
**Contracts read this run**: Agent Workflow §9.6 boundary · EVOLUTION.md schema · skill frontmatter spec
**Counts**: <N> skills · <A> agents · <P> in _proposed · <E> EVOLUTION entries (trailing 30d: <E30>) · <M> memory notes
**Objective signal (metrics, input-only)**: change-fail <x>%, rework <y>, escaped-defect on AI changes <z>

### BLOCKER (resolve or explicitly accept before /skill-eval runs)
- [HALT-GATE] EVOLUTION.md — 7 entries in trailing 30d (>5). Recommend HALT: /skill-eval and all structural evolution stop until user review. (path)
- [QUARANTINE] skills/_proposed/foo.md — promoted into .claude/skills/ with no /skill-verifier eval and no EVOLUTION row. (paths)
- [BOUNDARY] proposed edit to templates/settings.json is PROPOSE-ONLY but queued as an AUTO edit. (path)

### WARN (system debt — fix soon)
- [STALE] skills/release.md — spawns agents/release-manager.md which no longer exists (renamed?). (path)
- [CHURN] /design-doc → .planning/design/DESIGN.md edited by user immediately after the skill 3×; always adds a "Rollback" section. Refactor candidate. (paths)
- [DRIFT] CLAUDE.md diverged from CLAUDE.md.template (source-ahead): template's trunk-mode block not ported. (paths)
- [OVERLAP] agents/security.md and skills/security-review.md share trigger keywords {OWASP, auth, secrets}; requests ambiguous. (paths)
- [MEMORY] project_active_build.md ("W3 primary", 2026-05-22) vs project_ai_sonar.md ("active build", 2026-05-24) — two current builds; propose the user reconcile which supersedes. (paths) [PROPOSE-ONLY]
- [EVOLUTION] EVOLUTION.md still free-form narrative; convert to fixed 6-field schema so the 30d gate can count. (path)

### INFO (hygiene)
- [DORMANT] skills/data-classify.md never triggered — correct: this project handles no personal data. No action. (path)
- [NIT] skills/foo.md description lacks trigger keywords; mild undertrigger risk. (path)

### Memory-health sub-check (PROPOSE-ONLY — user-owned, no auto-edits)
- <staleness / contradiction / index-integrity findings, each phrased as a proposal>

### Gate verdict
- **<N> BLOCKER(s)** → /skill-eval BLOCKED until resolved or the user accepts the risk. If any is [HALT-GATE], recommend HALTING all structural evolution until user review.
- (or) **0 BLOCKER** → cleared to proceed; <M> WARN logged for /skill-eval to action within the ≤5/cycle velocity cap; memory items remain propose-only.

**This audit modified no files.**
```

Keep the report tight. If it would exceed ~100 lines, that volume is itself the signal — prioritize the top BLOCKERs + top WARNs and say so; don't suppress findings to look clean.

## Handoff to /skill-eval

- **0 BLOCKER** → `/skill-eval` may proceed. It actions the WARN items **within the AUTO-editable scope only** (skill bodies, `_methodology.md`, `EVOLUTION.md`, `Knowledge/` notes), **within the ≤5/cycle velocity cap**, each with an `EVOLUTION.md` row. PROPOSE-ONLY findings (template/hook/CI/schema **and all memory**) it surfaces in a Sessions/decision-log entry for the human — it does not apply them.
- **≥1 BLOCKER** → `/skill-eval` must STOP. Present BLOCKERs to the user; fix them as a separate deliberate change or get explicit acceptance. A `[HALT-GATE]` BLOCKER means **all structural evolution halts until user review** — `/skill-eval` does not get to "just do a few small ones".
- Dynamic-skill promotion (§K.4) additionally requires a passing `/skill-verifier` eval on the quarantined skill before `/skill-eval` promotes it — this audit only confirms the *gate exists and was honored*, it does not run that eval.
- Findings feed `EVOLUTION.md` **only after the fixing change lands**, and the velocity cap still applies to those logged edits.

## When NOT to use

- Mid-draft on a single skill not yet promoted, referenced by CLAUDE.md, or handed to `/skill-eval` (not yet structural).
- The user has said "just evolve it, skip the audit" for a throwaway/experimental skill (note that they overrode the gate, and that the §K.4 quarantine + `/skill-verifier` rails still apply to anything promoted).

## See also

- [[skill-verifier]] — the sibling detect auditor: it audits a skill's *output claims* (self-review optimism, §5.7); this skill audits the *skill system + memory*.
- [[kb-health]] — the other sibling detect auditor: same REPORT-only / BLOCKER-WARN-INFO / gate-verdict shape, applied to the knowledge graph; gates `/kb-capture` the way this gates `/skill-eval`.
- `skill-eval` — the act-side consumer this skill gates (does the promotion/refactor/EVOLUTION-logging this audit forbids itself from doing).
- `metrics` — the DORA-lite objective signal consumed here (input signal, never a KPI).
- `vault/AI/Agent Workflow.md` §9.6 — the single source of truth for the self-evolution boundary every check enforces.
- `vault/AI/project-template/EVOLUTION.md` + `_methodology.md` — the velocity cap, cool-off, and lessons log this skill checks.
- `vault/AI/Workflow Redesign 2026-06/Blueprint.md` §K — the subsystem spec (K.1 governance, K.3 non-skippable loop, K.4 dynamic-skill quarantine, K.5 memory hygiene).
- `vault/AI/project-discovery/skills/discovery-health.md` + `workflow-evolve.md` — the discovery-side detect/act pair this is ported from.
