# STATE — resume contract for climasite

> Auto-injected by the **SessionStart hook** (`hooks/state-prime.sh`) after any `/clear`, compaction, or
> `--resume`. Read **Next action** first. Kept fresh via `/checkpoint` at each unit/phase boundary.
> LEAN — pointers, not prose. **This is the single entry point; everything else is linked below.**

- **Last checkpoint**: 2026-06-24 (after Plan 19 test-hardening + a11y enforcement)

## Goal
Production-grade multi-language (EN/BG/DE), multi-theme HVAC e-commerce platform — finish to production readiness with a hardened SDLC.

## Current position
- **Phase**: maintenance / incremental hardening. No feature in-flight. Three big initiatives DONE (below); a tracked backlog remains.

## ✅ Done (all merged to `main`)
1. **PROC-01 SDLC hardening** — all 7 waves, 18 PRs. Gated pipeline, CI gates, 80/70 coverage, branch protection, 163 integration tests, real-card Stripe E2E. Tracker: `docs/features/PROC-01/STATE.md`.
2. **Workflow adoption** (`/project-adopt`, PRs #55–57) — latest vault agents/skills/hooks, vault **Knowledge graph** at `…/vault/Knowledge/climasite/` (18 components, 3 ADR decisions, 3 milestones, 8 risks, 6 questions), `.planning/` scaffold. `no-spec-no-code` now gates `src/**` (escape `ALLOW_EXPLORATORY=1`).
3. **Plan 19 — test + KG hardening** (`docs/plans/19-test-and-kg-hardening.md`, council-validated):
   - E2E: **195 NetworkIdle waits purged** → locator auto-waiting + `SettleAsync`; trace/screenshot-on-failure; **`[RetryFact]` guarded retry** (PRs #58, #59).
   - UI: **57 specs for the 4 untested services** (PR #58).
   - **UX-15 a11y: fixed + ENFORCED** — `--color-primary-surface` token + reduced-motion scans; `A11Y_ENFORCE=1` live in CI; both axe suites are hard gates (PR #60).
   - KG enriched (vault).

## ▶ Next action
No active unit. Pick the next item from **Remaining** below. For any `src/**` change, first write a `.planning/units/<unit>/unit-plan.md` (the `no-spec-no-code` gate) — see `.planning/units/UX-15-contrast/unit-plan.md` as the worked example.

## Remaining (tracked — none blocking; full detail in `docs/project-plan/PRIORITIZED_BACKLOG.md`)
- **Plan 19 B2/B3** — specs for the ~27 untested Angular components (cart, product-list, register first) + replace ~27 placeholder `should create` specs. (tests/ — ungated)
- **SEC-12** Angular 19→major upgrade (7 high npm advisories); **SEC-13** gitleaks allowlist→enforce; **SEC-14** GDPR Orders-PII anonymization (needs an ADR; KG: R-002 / Q-005).
- **OPS-11** enable the trunk merge queue — **plan-blocked** (needs a paid GitHub plan); ruleset + `merge_group` trigger are staged. See `docs/runbooks/merge-queue.md`.
- **Plan 19 C1** (`@defer` e2e-build mitigation — low priority now NetworkIdle is gone) · **C3** (dev-env rate-limit exemption, local-only convenience).
- **KG open items** — R-001 observability (=OPS-05), R-006/R-007/R-008; **VERIFY-first**: Q-003 stock-reservation, Q-006 SalePrice mapping (confirm vs current code before treating as bugs).

## Recently done (2026-06-26)
- **DOC-02 verified per-feature status pass** — code-read across 4 clusters (+ hand spot-checks) → refreshed `docs/project-plan/PROJECT_STATUS.md` to a dated, evidence-backed SSOT. REFUTED the stale "broken/stub" claims (admin CRUD, notifications, contact, legal, installation, GDPR-delete, payments all verified complete); corrected 2 verifier errors (BUG-03 cart-merge IS fixed; wishlist guest-merge EXISTS); confirmed the real open gaps (search-ILIKE, inventory-no-reservations, Lighthouse-reporting-only, SEC-08/14, OPS-08). CLAUDE.md caveat updated → PROJECT_STATUS is current.
- **BUG-11 / DEC-CURRENCY (display)** — `DEFAULT_CURRENCY_CODE='EUR'` + all 18 bare `\| currency` → `:'EUR'`; checkout shipping labels now show the **server's** EUR tiers (€5.99/€15.99/€19.99, mirrors `CheckoutPricing.cs`) instead of "free"/`$9.99`/`$19.99` (displayed==charged). 1246 FE tests green. Follow-ups filed: **UX-16** (dual EUR/BGN), **DEC-SHIPPING** (should standard be free?).
- **SEC-08 (headers)** — `SecurityHeadersMiddleware` adds nosniff / X-Frame DENY / Referrer-Policy / Permissions-Policy / X-XSS-Protection + a strict API CSP (skipped for `/swagger`); integration-tested (2/2). Remaining (deploy-time, OPS-08): frontend Stripe-CSP + CORS allowlist + AllowedHosts.
- **E2E retry net extended** to the 4 auth-heavy admin classes (recurring AdminPanelTests redirect flake) — same guardrail (timeout-only).

## Recently done (2026-06-25)
- **Docs consolidated to ONE planning system** (`/hygiene-sweep`-style pass): retired the bespoke PROC-01 `docs/features/` pipeline + its duplicate hooks (`require-approved-plan`, `session-phase`) + skills (`feature-kickoff`, `verify-plan`) — the vault `.planning/units` + `/plan-tree` + `no-spec-no-code` flow is now the single system; added `docs/README.md` map; bannered ~20 stale legacy trackers; ADR-002 immutability fixed via superseding **ADR-0003**; leaned CLAUDE.md (status tables + pipeline → pointers). No protection lost (`no-spec-no-code` + test-ship + git/secret guards intact).

## Key pointers
- **Plan ahead + tracking**: this file (resume) · `docs/plans/19-test-and-kg-hardening.md` (test/KG plan) · `docs/project-plan/PRIORITIZED_BACKLOG.md` (full backlog) · `docs/features/PROC-01/STATE.md` (SDLC waves) · vault `Knowledge/climasite/` (graph).
- Currency = EUR + dual EUR/BGN display (peg 1.95583) — DECIDED. Nothing deployed yet (OPS-08).
- Reversible pre-adopt checkpoint: `pre-adopt-backup-20260623-180552`.
- Gotchas (memory): NEVER reintroduce `LoadState.NetworkIdle` in E2E; CRLF endings; classic branch protection is the gate.

## Open loops / checkpoints
- none. main is clean (0 open PRs); all CI gates green.
