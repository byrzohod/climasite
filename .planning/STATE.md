# STATE — resume contract for climasite

> Auto-injected by the **SessionStart hook** (`hooks/state-prime.sh`) after any `/clear`, compaction, or
> `--resume`. Read **Next action** first. Kept fresh via `/checkpoint` at each unit/phase boundary.
> **LEAN — pointers, not prose.** Per-PR history lives in `CHANGELOG.md`; the full backlog in
> `docs/project-plan/PRIORITIZED_BACKLOG.md`. This file states the single current truth — nothing historical.

- **Last checkpoint**: 2026-07-02 — **✅ INV-01 checkout stock-reservations COMPLETE** (5 waves, #98–#102,
  `main` tip `9dbe3ff`). Working tree clean; 0 open PRs. No feature in flight — awaiting the owner's next pick.

## Goal
Production-grade multi-language (EN/BG/DE), multi-theme HVAC e-commerce platform — finish to production
readiness with a hardened SDLC.

## Current position
- `main` = **`9dbe3ff`** (#102 INV-01 Wave B). No feature in flight; 0 open PRs; tree clean.
- **INV-01 shipped in 5 trunk-merged waves** — A0 #98 `02e700b` (server-minted signed guest cookie, dark) ·
  A1 #99 `607e7c7` (cookie authoritative + legacy-cart migration) · A2 #100 `06bcae7` (reservations core —
  loser blocked before payment) · A3 #101 `51fd8bf` (reservation-aware PDP/cart availability) · B #102
  `9dbe3ff` (bank-transfer hold-with-expiry). Every wave: tests-with-code → cross-vendor Codex diff-council →
  live `/acceptance` (PASS reports at `.planning/acceptance/INV-01-*.md`) → green CI → squash-merge.
- INV-01 detail (design-brief + unit-plan): `.planning/units/INV-01-checkout-reservations/`.

## NEXT ACTION (single)
**Ask the OWNER which NEXT-queue item to start** — nothing below is started:
- **deploy-hardening F3/OPS-07/OPS-04** (+ the actual Railway deploy, owner-gated — needs the Railway project + secrets)
- **SEC-12 dependency upgrades** (Microsoft.OpenApi/Swashbuckle bump the B-039 CI allowlist defers + Angular 19 advisories)
- **Plan-19 B3 test-quality** (remaining frontend spec coverage)
- the **2 INV-01 follow-ups** (below)

## Open follow-ups (non-blocking; filed in `docs/project-plan/PRIORITIZED_BACKLOG.md`)
- **FOUND-INV01-paid-cancel** — cancelling a PAID order via the admin STATUS endpoint
  (`UpdateOrderStatusCommand`→Cancelled) does NOT restock/release the decremented stock (pre-existing, all
  order types); route it through the same restock/release path as `CancelOrderCommand`.
- **FOUND-INV01-e2e-flake** — harden two flaky E2E tests that forced CI re-runs:
  `CardPaymentE2ETests.FillStripeCardAsync` (Stripe iframe 30s timeout) +
  `AdminPanelTests.AdminDashboard_NonAdminUser_IsRedirectedOrDenied` (redirect timing).

## Open loops / checkpoints
- **None.** INV-01 fully merged (#98–#102); working tree clean; 0 open PRs.

## Key pointers
- **Plan + tracking**: this file (resume) · `docs/project-plan/PRIORITIZED_BACKLOG.md` (full backlog, SSOT) ·
  `docs/project-plan/PROJECT_STATUS.md` (per-feature status) · `CHANGELOG.md` (per-PR history) ·
  `docs/features/PROC-01/STATE.md` (SDLC waves) · vault `Knowledge/climasite/` (decisions/risks/questions graph) ·
  `.planning/PATHS.md` (canonical paths) · `docs/README.md` (map of all docs).
- Currency = EUR + transitional dual EUR/BGN display (peg 1.95583) — DECIDED. **Nothing deployed yet** (OPS-08).
- **Standing rule**: run the cross-vendor Codex council (`gpt-5.5`@`xhigh`) on every non-trivial change;
  behaviour/source PRs need a committed `/acceptance` PASS at `.planning/acceptance/<id>.md` matching the merged tip.
- **Gotchas** (reusable): never reintroduce `LoadState.NetworkIdle` in E2E; CRLF files — `git diff --check`
  false-flags CRLF as trailing-ws, the REAL gate is `dotnet format --verify-no-changes` (`.editorconfig` leaves
  `end_of_line` unset); the `--z-*` scale lives ONLY in `_tokens.scss`; an output-cached GET goes stale after a
  mutation during `/acceptance` — bust it with a random `?_cb=` query param; `/acceptance` on the REAL running
  app is mandatory (it once caught an NG0200 all 1764 unit tests missed); classic branch protection is the merge gate.

## ✅ Foundational milestones (full shipped history → `CHANGELOG.md`)
1. **PROC-01 SDLC hardening** — 7 waves, 18 PRs: gated pipeline, 6 required CI checks + 80/70 coverage +
   enforced a11y, 163 integration tests, real-card Stripe E2E, branch protection. Tracker: `docs/features/PROC-01/STATE.md`.
2. **Workflow adoption** (`/project-adopt`, #55–57) — vault agents/skills/hooks, the Knowledge graph
   (`vault/Knowledge/climasite/`), `.planning/` scaffold; `no-spec-no-code` gates `src/**` (escape `ALLOW_EXPLORATORY=1`).
3. **Plan 19 — test + KG hardening** — E2E NetworkIdle purge + guarded retry; frontend service specs; UX-15 a11y enforced.
- Feature history since (INV-01, SEO B-044, Q&A B-038/B-039, a11y, B-016, SEARCH-01-fts, Google OAuth,
  PAY-IDEM, …) is single-sourced in `CHANGELOG.md` — not restated here.
