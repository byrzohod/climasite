---
name: skill-verifier
description: Meta-skill that audits other skills' and agents' output claims to catch self-review optimism (workflow Section 5.7). Use this whenever a high-stakes skill just reported "done" on release-bound work -- specifically after /code-review (did it surface real findings or rubber-stamp?), /security-review (were all OWASP categories actually checked?), /verify-work (was the app actually launched?), /release (were all steps completed?), or /deploy-checklist (was each item actually verified?). ALSO the gate that GRADES an AI-generated skill sitting in skills/_proposed/ BEFORE a human promotes it (Blueprint A14 / §K.4) -- checking it adds no MCP/network/unscoped-Bash, has valid frontmatter + a trigger-quality description, references no hallucinated tools/skills/paths, conforms to the house format, and actually does what it claims; called by /skill-eval after it drafts a skill. Spawns the verifier agent to audit claims (or a drafted skill) against artifacts.
---

# /skill-verifier - Meta-skill: verify that other skills actually did what they claim

## When to use

Invoke this skill **after** another skill has reported "done" on high-stakes work. It checks whether the claimed outcome is supported by evidence in the repo.

Mandatory after:
- **`/code-review`** -- did review surface real findings or rubber-stamp?
- **`/security-review`** -- were all OWASP categories actually checked?
- **`/verify-work`** -- was the app actually launched, the endpoint actually hit?
- **`/release`** -- did the release procedure complete every step?
- **`/deploy-checklist`** -- was every checklist item actually verified?

Recommended after:
- **`/ui-qa`** -- were Playwright tests run, or just listed?
- **`/db-migrate`** -- migrations actually applied + rolled back successfully?
- **`/perf-budget`** -- baseline actually measured, or copied from previous run?
- **`/refactor`** -- tests actually still pass, or rationalized?

**Mandatory for AI-generated skills (A14 promotion gate):**
- **A skill drafted into `skills/_proposed/`** (by `/skill-eval` via the first-party `skill-creator`) -- this skill is the **promotion gate**. The draft is INERT until it clears verification AND a human moves it into `skills/`. Here the "claim under audit" isn't a report -- it's the generated skill itself ("this skill is in-scope, well-formed, honest, and does what it claims"). See the dedicated playbook in [Grading an AI-generated skill](#grading-an-ai-generated-skill-a14-promotion-gate) below.

Skip for:
- Trivial single-file changes
- Exploratory iteration (verifying every micro-step blocks flow)

## Why this exists

AI agents have a documented blind spot: **self-review optimism** (workflow Section 5.7). When a model both produces work and reports on it, the report tends to be more favorable than reality. The verifier pattern adds an independent eye that doesn't share the producing agent's bias.

This skill operationalizes that pattern: invoke a separate verifier subagent to audit the previous skill's claims.

## The verifier pattern

```
[Skill X] runs and produces report
       ↓
[Skill X reports "done" + outcome claims]
       ↓
[Orchestrator decides: is this high-stakes? release-bound? auth-touching?]
       ↓ (if yes)
[Spawn verifier subagent with the verifier agent definition]
       ↓
[Verifier reads artifacts: git diff, CI logs, dev-server logs, screenshots, etc.]
       ↓
[Verifier reports per-claim CONFIRMED / DISPUTED / UNVERIFIABLE]
       ↓
[Orchestrator routes DISPUTED back to original agent / skill]
```

## Procedure

### Step 1: Identify claims to verify

After the upstream skill finishes, extract the specific claims to audit. Examples:

| Upstream skill | Typical claims |
|----------------|----------------|
| `/code-review` | "N issues found", "all dimensions checked", "iterated K rounds" |
| `/security-review` | "OWASP Top 10 covered", "no critical findings", "dependency audit clean" |
| `/verify-work` | "Layer 1-6 green", "screenshots saved", "0 console errors" |
| `/release` | "semver bumped", "CHANGELOG updated", "tag pushed", "release notes written" |
| `/deploy-checklist` | "env parity confirmed", "rollback tested", "health check passes" |

Don't verify everything; pick the claims most likely to be optimistic.

### Step 2: Spawn the verifier subagent

Use the `verifier` agent (`.claude/agents/verifier.md`). Brief it with:
- The original skill's claims (paste verbatim where possible)
- Evidence locations (PR / branch / log file / artifact path)
- The audit playbook to use (see verifier agent definition for playbooks)

Example invocation:

```
Task({
  description: "Audit /security-review claims for OWASP coverage",
  subagent_type: "verifier",
  prompt: """
  The /security-review skill just finished on branch feature/payment-flow
  and reported:
    - 'OWASP Top 10 reviewed: all categories N/A or clean'
    - 'No critical findings'
    - 'npm audit --production: 0 high, 0 critical'

  Audit these claims. Specifically:
  - Did the review actually cover A03 (Injection)?
    Check src/api/payments.ts for parameterized queries vs string concat.
  - Did the review actually cover A07 (Auth Failures)?
    Check session timeout, brute-force protection, token rotation in src/auth.
  - Was the dependency audit actually run? Check CI logs for the audit step's output.

  Report per claim: CONFIRMED / DISPUTED / UNVERIFIABLE with evidence cited.
  Report in under 400 words.
  """
})
```

### Step 3: Apply the verdict

Based on the verifier's report:

- **All CONFIRMED**: proceed (merge, deploy, ship)
- **Any DISPUTED**: route back to original skill / agent with the specific contradiction; re-run the relevant portion
- **Any UNVERIFIABLE**: present to the user for spot-check; do NOT proceed without their confirmation if it's release-bound

### Step 4: Update memory if pattern recurs

If the same skill keeps producing optimistic reports that the verifier catches, that's a process signal. Update `CLAUDE.md` or auto-memory with:

- "After /security-review, always run /skill-verifier -- last 3 runs missed A03"
- "Developer agent's 'verified' claims need verifier audit for the next month"

The verifier should become unnecessary for skills that earn trust over time. Pay attention to which skills earn trust and which don't.

## Audit playbooks (reference; full versions in `agents/verifier.md`)

### `/code-review` clean → verify

Check the PR diff size vs reported finding count. <50 lines: 0-3 findings; 50-200: 2-8; 200-500: 5-15; >500: 15+. Way fewer findings than expected = suspicious. Spot-check 3 random files for obvious issues that should have surfaced.

### `/security-review` clean → verify

Confirm each of OWASP A01-A10 was explicitly addressed (or "N/A" stated with reason). Spot-check the actual code for the most-easily-missed: parameterized SQL, server-side validation, secrets in code/git, rate limiting on auth endpoints.

### `/verify-work` green → verify

Confirm each verification layer has evidence: build log, API smoke output, screenshots for UI layer, user-flow walk-through description, persistence test command output. Reports without layer-level detail = suspect.

### `/release` complete → verify

Confirm: version bumped in `package.json` (or equivalent), CHANGELOG.md has new entries, git tag exists at `v{N}`, release notes written, build artifact produced. Each is a `git log` or file existence check.

### `/deploy-checklist` complete → verify

Confirm: env parity check ran, rollback procedure tested (not just documented), health check probed and returned 200, feature flags set correctly. Run the actual health check command, don't trust the claim.

## Grading an AI-generated skill (A14 promotion gate)

This is a distinct mode. Everywhere else this skill audits a **report** an agent wrote; here it audits an **artifact the system wrote about itself** -- a brand-new or substantially-rewritten skill that `/skill-eval` drafted (via the first-party `skill-creator`) into `skills/_proposed/`. That makes this the single highest-risk path in the whole self-evolution engine: a self-authoring system is exactly what the trusted-source-only posture (which removed the GSD/Caveman plugins) exists to contain. So the verifier is the gate that stands between a machine-written skill and the live catalog.

**The contract (declared once in the LOCKED / SELF-EVOLUTION-BOUNDARY block and in `vault/AI/Agent Workflow.md`; this skill enforces it, it doesn't redefine it):** AI may only **DRAFT** into `skills/_proposed/` (INERT, enforced by the `skill-quarantine` hook); a generated skill may add **no** MCP server, **no** network call, and **no** unscoped Bash; it must pass this grading; and **only a human promotes** it into `skills/`. If the boundary doc and this playbook ever disagree, the boundary doc wins -- flag the drift rather than follow this skill.

### Why grading is independent

`/skill-eval` both *commissions* the draft and *would benefit* from it passing (it closes the cycle, ticks the velocity counter). That is the textbook self-review-optimism setup (workflow §5.7). So grading runs in a **separate verifier subagent** that did not author the draft and has no stake in its promotion -- same detect-vs-act split as everywhere else in this skill. The verifier's only job is to try to find a reason this skill should NOT ship.

### The five grading dimensions

Audit the drafted file at `skills/_proposed/<name>.md`. Treat each as a claim to confirm or dispute, with evidence cited (file + line). **Dimension 1 is a hard security gate: any finding there is an automatic BLOCK, no balancing against the others.**

1. **Scope / blast radius (SECURITY GATE -- the rail that replaced GSD/Caveman).** The generated skill must add **no new attack surface**. Concretely, scan the draft (frontmatter `allowed-tools` *and* every command/example/instruction in the body) for:
   - **MCP servers** -- any `mcp__*` tool reference, or instructions to add/configure an MCP server. *Not allowed.*
   - **Network calls** -- `WebFetch`/`WebSearch`, `curl`/`wget`/`nc`, raw HTTP/SDK calls, `npx`/`uvx`/`pip install` from the network, or any instruction to reach off-box. *Not allowed.*
   - **Unscoped Bash** -- a blanket `Bash` grant, or Bash patterns outside the existing project allowlist (compare against `templates/settings.json` / the project's `settings.json` permit list). Destructive verbs (`rm -rf`, `git push --force`, `chmod 777`, piping a remote script to a shell) are an immediate BLOCK.
   - It must declare a **minimal, explicit `allowed-tools`** -- not absent, not a wildcard. If the task genuinely needs new MCP/network/Bash surface, that is a **human decision**: DISPUTE with "this task needs surface X, which is a propose-only/boundary change -- it cannot be granted by drafting." Do not let the skill grant itself the surface.

2. **Frontmatter + trigger-description quality.** Confirm the house frontmatter contract: `name` present and **kebab-case matching the intended slash command**; `description` present and written in the "pushy", keyword-rich style that carries **both WHAT the skill does and WHEN to use it** (the user phrases/contexts that should fire it), per the `skill-creator` standard the template adopted. A terse procedure-doc description with no triggering keywords is a DISPUTE: it will under-trigger by construction (and would never collide-test cleanly against the catalog). Also flag a `description` whose triggers obviously **overlap an existing skill/agent** (the harness would pick unpredictably) -- name the colliding skill.

3. **No hallucinated references.** Every tool, skill, agent, command, file path, and hook the draft names **must actually exist**. Resolve each: `Bash`/`Read`/`Edit`/`Grep`/`Glob`/`Task` etc. are real harness tools; `mcp__*` names must match a configured server (and per Dimension 1 generally shouldn't appear at all); `/<skill>` references must resolve to a file in `skills/` (or `skills/_proposed/`); agent names to `agents/*.md`; cited paths (`hooks/*.sh`, `templates/*`, `Knowledge/_schema.md`, etc.) must exist in the repo. A reference that doesn't resolve is a DISPUTE -- a generated skill that instructs the agent to call a non-existent tool or read a non-existent file is worse than useless, it derails the caller. (This is the same dead/hallucinated-link check the research playbook runs, applied to a skill.)

4. **House-format conformance.** The draft must look like the other skills in `skills/`: YAML frontmatter, then `# /<name> - <one-line>`, then the usual shape (When to use · Why · Procedure with numbered steps · Output · When NOT to use · See also -- match the closest sibling skill, don't invent a new layout). It must **reference shared contracts rather than restate them** (the 13 review dimensions, the self-evolution boundary, the OWASP list live in single sources -- a draft that copy-pastes them is a DISPUTE; duplication rots). Atomic, single-responsibility scope; no dead `See also` links.

5. **Does it do what it claims (the honesty check).** Read the body as if you were the agent that has to execute it. Do the steps, followed literally, actually achieve what the `description` promises? Look for: steps that reference outputs no earlier step produced; a procedure that stops short of the claimed outcome; success criteria that can't be checked; instructions that contradict the frontmatter (`allowed-tools` that don't cover the tools the body tells you to use). If the WHAT can't be reached by following the HOW, that's the most important DISPUTE of all -- a plausible-looking skill that quietly no-ops is exactly the self-review-optimism failure this whole subsystem exists to catch.

### Verdict and routing (stricter than the report case)

Report per dimension CONFIRMED / DISPUTED / UNVERIFIABLE, then a single gate verdict:

- **Any Dimension 1 (scope) finding → BLOCK, full stop.** The draft stays in `_proposed/`; do not promote, do not "fix and wave through." A generated skill reaching for MCP/network/unscoped-Bash is a boundary event -- surface it to the human as such, not as a routine revise.
- **Any other DISPUTED → not promotion-ready.** Route back to `/skill-eval` to revise the draft *in place in `_proposed/`* and re-grade. Drafts do not get promoted with open disputes.
- **All CONFIRMED → promotion-READY, but still INERT.** The verifier does **not** move the file. It records "promotion-ready (skill-verifier: ALL CONFIRMED)"; a **human** moves it from `skills/_proposed/` into `skills/`. The `skill-quarantine` hook enforces this even if an agent tries to shortcut it; never instruct anyone to bypass the hook.
- **UNVERIFIABLE** (e.g., can't confirm a referenced external precondition) → surface to the human; do not count it as a pass.

The verdict is written back into the EVOLUTION row `/skill-eval` opened for the draft (status: "drafted -- INERT, awaiting human promotion; skill-verifier: <verdict>"), so the audit trail shows the gate ran. A draft that never went through this grading must not be promoted -- `/skill-health` treats a `_proposed/` skill promoted without a passing `skill-verifier` verdict + matching EVOLUTION row as a BLOCKER.

### Example invocation (grading mode)

```
Task({
  description: "Grade _proposed/flaky-test-triage.md before promotion (A14)",
  subagent_type: "verifier",
  prompt: """
  /skill-eval drafted a new skill at
  skills/_proposed/flaky-test-triage.md and claims it is promotion-ready.
  GRADE it for promotion (do NOT promote -- it stays INERT in _proposed/).

  Audit the five A14 dimensions, evidence cited (file:line):
  1. SCOPE (hard gate): does it add ANY mcp__* tool, network call
     (WebFetch/curl/npx/uvx/pip), or unscoped/destructive Bash? Is
     `allowed-tools` minimal+explicit (not absent, not wildcard)?
     Any finding here = BLOCK.
  2. Frontmatter: kebab `name` matching the slash command; pushy
     keyword-rich description carrying WHAT + WHEN; no trigger overlap
     with an existing skill/agent.
  3. Hallucinations: does every tool / /skill / agent / file path /
     hook it names actually resolve in this repo?
  4. House format: frontmatter -> `# /name - …` -> standard shape;
     references shared contracts instead of restating them; no dead links.
  5. Honesty: following the steps literally, do they achieve what the
     description promises? Any step depending on an output no earlier
     step produced? Any allowed-tools/body mismatch?

  Verdict per dimension CONFIRMED / DISPUTED / UNVERIFIABLE, then a single
  gate verdict (BLOCK / not-ready-revise / promotion-ready-but-INERT).
  Report in under 450 words.
  """
})
```

## Output

The verifier's output should be added to the same PR / planning doc that the upstream skill wrote to. Format:

```markdown
## Verification audit (skill-verifier)

**Skill audited**: /security-review (ran 2026-MM-DD HH:MM)
**Claims**: OWASP Top 10 reviewed, 0 critical, 0 high in npm audit

**Audit verdict**:
- CONFIRMED: A03 Injection (parameterized queries verified at src/api/payments.ts:42-78)
- CONFIRMED: npm audit clean (CI log at workflow run #142, step "Audit")
- DISPUTED: A07 Auth Failures -- session timeout NOT verified; session config in src/auth/session.ts:14 has no explicit timeout. Recommend security-review re-run with explicit A07 focus.
- UNVERIFIABLE: A09 Logging Failures -- logs not accessible from agent sandbox; user spot-check recommended.

**Recommendation**: re-run /security-review for A07 before merge; ask user to confirm A09.
```

## When NOT to use

- The upstream skill explicitly declines to make a claim (e.g., "I cannot verify X")
- The upstream skill output already includes a verifier-style audit
- The user has overridden with "trust the skill, ship it"
- Iteration speed > confidence (early prototype, no users)

## Calibration over time

After 10+ uses across the project, you'll know which skills produce reliable claims and which don't. Update CLAUDE.md or memory with:

- "Skill X is reliable -- skip verifier"
- "Skill Y has been wrong 3/5 times -- always verify"

The goal is targeted verification, not blanket verification. Blanket adds latency without proportional value.

## See also

- `agents/verifier.md` -- The agent definition this skill invokes (its research playbook's dead/hallucinated-link check is the model for Dimension 3 of the A14 grading)
- `vault/AI/Agent Workflow.md` -- Section 5.7 (anti-patterns), Section 6 (Code Review), Section 10 (Subagents), and the **Self-Evolution** section (the boundary the A14 grading enforces)
- `skills/code-review.md`, `skills/security-review.md`, `skills/verify-work.md` -- The skills you most often verify
- `skills/skill-eval.md` -- the ACT side of self-evolution; **Step 4 drafts a skill into `_proposed/` and calls this skill to grade it** before a human promotes it (A14)
- `skills/skill-health.md` -- the detect side; flags a `_proposed/` skill promoted without a passing grade here + an EVOLUTION row as a **BLOCKER**
- `hooks/skill-quarantine.sh` / `templates/settings.json` -- the `skill-quarantine` hook that keeps drafted skills INERT in `_proposed/` until a human promotes (the enforcement this grading sits in front of)
