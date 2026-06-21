---
name: skill-verifier
description: Meta-skill that audits other skills' and agents' output claims to catch self-review optimism (workflow Section 5.7). Use this whenever a high-stakes skill just reported "done" on release-bound work -- specifically after /code-review (did it surface real findings or rubber-stamp?), /security-review (were all OWASP categories actually checked?), /verify-work (was the app actually launched?), /release (were all steps completed?), or /deploy-checklist (was each item actually verified?). Spawns the verifier agent to audit claims against artifacts.
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

- `agents/verifier.md` -- The agent definition this skill invokes
- `vault/AI/Agent Workflow.md` -- Section 5.7 (anti-patterns), Section 6 (Code Review), Section 10 (Subagents)
- `skills/code-review.md`, `skills/security-review.md`, `skills/verify-work.md` -- The skills you most often verify
