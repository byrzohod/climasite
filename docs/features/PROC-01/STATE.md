<!--
DOGFOOD / worked example: PROC-01 (this very SDLC-hardening initiative) tracked through the pipeline
it creates. PROC-01 is GRANDFATHERED — its Research + Plan + owner decisions predate the pipeline and
live in docs/project-plan/SDLC_HARDENING_PLAN.md (the canonical, owner-approved plan). Rather than
duplicate that into research.md/plan.md/test-design.md (which would instantly drift), this STATE.md is
the live execution tracker for the 7 build waves. New features created AFTER this one get the full
_template/ set via /feature-kickoff.
-->

# STATE — PROC-01: SDLC hardening (process overhaul)

- **Current phase:** Implement (Wave 3 complete → Wave 4 next)
- **plan_status:** approved (owner-approved 2026-06-21; canonical plan: `docs/project-plan/SDLC_HARDENING_PLAN.md`)
- **Research + Plan:** `docs/project-plan/SDLC_HARDENING_PLAN.md` (8 gated phases, 7 waves, owner decisions §4)
- **Last updated:** 2026-06-22

## Wave execution tracker

| Wave | Deliverable | Status | PR |
|------|-------------|--------|----|
| 0 — Foundations | vault agents + skills pinned into `.claude/`; workflow doc-drift fixed; DEV_WORKFLOW.md canonical | ✅ merged | #40 |
| 1 — Per-feature pipeline | `docs/features/_template/*`, `docs/features/README.md`, `/feature-kickoff`, `/verify-plan`, CLAUDE.md front-phase wiring | ✅ merged | #41 |
| 2 — Phase-aware hooks | SessionStart phase inject; PreToolUse no-code-without-approved-plan; tests-with-feature (commit-time); PostToolUse test-ran marker; Stop hook — all **staged `warn`** (owner decision) behind `.claude/hooks/gate-mode` | ✅ merged | #42 |
| 2b — Flip gates to `block` | change `gate-mode` → `block` after real features validate the gates | ⏳ pending (⚠️ owner pause — this is the hard gate) | — |
| 2c — Git-native backstop | husky/commitlint + managed-settings locking (deferred from Wave 2; blocking git-native hook needs its own staged review; managed-settings is machine-level) | ⏳ deferred | — |
| 3a — CI gates + baseline | lint+format (dotnet format/.editorconfig/ng lint), dependency-audit (.NET vulns + Trivy CRITICAL enforced; gitleaks→SEC-13 + npm-audit→SEC-12 informational), ADR gate, test-design lint — all enforced via test-summary; coverage **reporting** (non-enforcing) for the baseline | ✅ merged | #43 |
| 3b — Coverage-raising sprint | 3 workflow batches (+750 backend unit tests; MockDbContext FindAsync/async-AsQueryable fix) → backend line 66.8%→**85.6%**, Core→81.25%, frontend ~72% | ✅ merged | #44/#45/#46/#47 |
| 3c — Flip coverage gate hard | Coverage Gate job enforces backend line ≥80% / frontend line ≥70%; added to Test Summary required checks (owner-approved 2026-06-22) | 🚧 in progress | this PR |
| 4 — Review hardening | PR template, CODEOWNERS, CI danger.js (no required GitHub approval — keep AI auto-merge) | ⏳ pending | — |
| 5 — Visual + a11y + perf harness | ephemeral screenshots, screenshot/trace on E2E failure, deepened axe matrix, Lighthouse CI, E2E sharding | ⏳ pending | — |
| 6 — Close coverage gaps | real Stripe card E2E (CI secrets) + webhook contract; GDPR export/delete; inventory; search facets; auth flows; address book; etc. | ⏳ pending | — |

## Owner decisions in force (from SDLC_HARDENING_PLAN.md §4)
1. Real card E2E in CI = YES (Stripe test keys → GitHub Actions secrets; `e2e-payments` job). Wave 6.
2. Review gate = reviewer agent + PR template + CI danger check; **no** required GitHub approval (AI auto-merge kept).
3. Coverage = ENFORCE 80% backend / 70% frontend — **measure first** (Wave 3); ratchet if below.
4. Visual = ephemeral screenshots, no committed baselines; delete after AI/human review.

**Pauses requested by owner:** before flipping any hard gate that could block all future PRs — the
Wave 2 PreToolUse blocking hooks and the Wave 3 coverage gate.

## Log (newest first)
- 2026-06-22 — Wave 3c: flipped the Coverage Gate to ENFORCING (owner-approved) — backend line ≥80% / frontend line ≥70%, added to the Test Summary required checks. Dogfoods on its own PR (main is at 85.6%/~72%).
- 2026-06-22 — Wave 3b complete: 3 workflow batches added 750 backend unit tests (Application + Core) + a MockDbContext FindAsync/async-AsQueryable fix. Backend line coverage 66.8%→71.3%→78.6%→**85.6%**; Core 73.7%→81.25%; frontend ~72%. Merged #45/#46/#47 (foundation #44). Also closed Wave-6 P0/P1 gaps for GDPR + Inventory at the unit level. Found integration tests just needed the postgres image pre-pulled (Mac Docker), not a code fix.
- 2026-06-21 — Wave 3a: CI gates (lint+format with root .editorconfig, dependency-audit, ADR gate, test-design lint) enforced via test-summary; coverage reporting added for the baseline. Measured: frontend lines 72.1%/stmt 70.0%/func 63.6%/branch 51.4%; backend local run unreliable (Testcontainers timed out on Mac) — CI coverage job is the authoritative source. Found 8 npm vulns (Angular major upgrade → SEC-12); gitleaks informational → SEC-13.
- 2026-06-21 — Wave 2: phase-aware hooks (SessionStart/PreToolUse/PostToolUse/Stop) added in **warn mode** behind `gate-mode`; each tested in isolation (warn=exit 0, block=exit 2 proven). Existing git/secret hooks preserved. husky/managed-settings backstop deferred (Wave 2c).
- 2026-06-21 — Wave 1 merged (#41): pipeline templates + `/feature-kickoff` + `/verify-plan` + CLAUDE.md wiring; dogfooded gate caught 6 issues, all fixed.
- 2026-06-21 — Wave 0 merged (#40): agents/skills pinned, DEV_WORKFLOW.md made canonical.
- 2026-06-21 — Merged PR #38 (Stripe card payments) and #39 (plan doc).
