<!--
DOGFOOD / worked example: PROC-01 (this very SDLC-hardening initiative) tracked through the pipeline
it creates. PROC-01 is GRANDFATHERED — its Research + Plan + owner decisions predate the pipeline and
live in docs/project-plan/SDLC_HARDENING_PLAN.md (the canonical, owner-approved plan). Rather than
duplicate that into research.md/plan.md/test-design.md (which would instantly drift), this STATE.md is
the live execution tracker for the 7 build waves. New features created AFTER this one get the full
_template/ set via /feature-kickoff.
-->

# STATE — PROC-01: SDLC hardening (process overhaul)

- **Current phase:** Implement (Wave 1 of 7)
- **plan_status:** approved (owner-approved 2026-06-21; canonical plan: `docs/project-plan/SDLC_HARDENING_PLAN.md`)
- **Research + Plan:** `docs/project-plan/SDLC_HARDENING_PLAN.md` (8 gated phases, 7 waves, owner decisions §4)
- **Last updated:** 2026-06-21

## Wave execution tracker

| Wave | Deliverable | Status | PR |
|------|-------------|--------|----|
| 0 — Foundations | vault agents + skills pinned into `.claude/`; workflow doc-drift fixed; DEV_WORKFLOW.md canonical | ✅ merged | #40 |
| 1 — Per-feature pipeline | `docs/features/_template/*`, `docs/features/README.md`, `/feature-kickoff`, `/verify-plan`, CLAUDE.md front-phase wiring | 🚧 in progress | this PR |
| 2 — Phase-aware hooks | SessionStart phase inject; PreToolUse no-code-without-approved-plan; tests-with-feature; PostToolUse test-ran marker; Stop hook | ⏳ pending (⚠️ owner pause before flipping the blocking PreToolUse gates) | — |
| 3 — CI hard gates | lint/format, dependency-audit (+gitleaks/Trivy), coverage gate 80/70, test-design-coverage lint, ADR gate | ⏳ pending (⚠️ MEASURE coverage first; owner pause before flipping the hard gate) | — |
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
- 2026-06-21 — Wave 1: pipeline templates + `/feature-kickoff` + `/verify-plan` + CLAUDE.md wiring authored; PROC-01 dogfooded into this tracker.
- 2026-06-21 — Wave 0 merged (#40): agents/skills pinned, DEV_WORKFLOW.md made canonical.
- 2026-06-21 — Merged PR #38 (Stripe card payments) and #39 (plan doc).
