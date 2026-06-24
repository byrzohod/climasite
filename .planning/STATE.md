# STATE — resume contract for climasite

> The single durable handoff the **SessionStart hook** (`hooks/state-prime.sh`) auto-injects after any `/clear`, compaction, or `--resume`. Read **Next action** first, then open the linked plan/Knowledge as needed. Kept fresh via **`/checkpoint`** at every unit/phase boundary. Keep it LEAN — pointers, not prose.

- **Last checkpoint**: 2026-06-23 18:10 · `/checkpoint` after each unit/phase

## Goal
Production-grade multi-language (EN/BG/DE), multi-theme HVAC e-commerce platform (catalog, cart, checkout/Stripe, orders, admin, reviews, wishlist, GDPR) — finish to production-readiness.

## Current position
- **Phase**: Phase 0 — adopted existing codebase (latest AI workflow installed over a mature PROC-01 install) · **Wave**: — · **Unit**: none yet (—)

## Approved plan
- none yet  <!-- .planning/units/<unit>/unit-plan.md — the path no-spec-no-code reads; must exist + be approved before that unit's code -->
- **Design target**: `.planning/design/DESIGN.md` (status: **reconstructed** — reverse-engineered, not yet ratified; ratify via `/design-doc`)

## Last completed
`/project-adopt` — latest vault workflow installed (23 agents, 43 skills, 6 orchestration scripts, vault hooks merged); codebase mapped into `Knowledge/climasite/` (9 components, 8 risks, 6 questions); planning scaffold seeded. The repo already carried a full PROC-01 SDLC-hardening install (gated pipeline, CI gates, 80/70 coverage, branch protection) — both coexist (see CLAUDE.md).

## ▶ Next action
Triage the seeded Risks/Questions in `Knowledge/climasite/`, then `/research-loop` (if needed) → ratify `.planning/design/DESIGN.md` via `/design-doc` → `/plan-tree` for the next unit. (Two questions are VERIFY-first: Q-003 stock-reservation, Q-006 SalePrice mapping — confirm against current code before treating as bugs.)

## Blockers
- **Merge queue DEFERRED (plan-blocked, OPS-11)**: GitHub's `merge_queue` rule + `evaluate`-mode rulesets need a paid plan (Team/Enterprise); this Free-plan personal public repo returned 422. The existing **classic branch protection** is the gate (6 checks incl. Test Summary, admins-included, no force-push/delete, PR-required); the `merge_group` trigger + ruleset artifact are ready for when the plan supports it. See `docs/runbooks/merge-queue.md`.
- Dual planning systems pending consolidation: PROC-01 (`docs/features/<ID>/plan.md` + `require-approved-plan` warn hook) and the vault standard (`.planning/**/unit-plan.md` + `no-spec-no-code` block hook). Pick one as authoritative for new work.

## Key decisions & pointers
- **Knowledge graph**: `Knowledge/climasite/` in the vault (ADRs, components, open questions, risks — Dataview over frontmatter `[[wikilink]]` edges)
- **Planning root**: `.planning/` — `design/`, `phases/→waves/→units/`, `research/REPORT.md`
- **Currency** = EUR with transitional dual EUR/BGN display (peg 1.95583) — DECIDED, not open.
- **PROC-01** SDLC-hardening initiative complete (all 7 waves, 18 PRs); canonical plan `docs/project-plan/SDLC_HARDENING_PLAN.md`, tracker `docs/features/PROC-01/STATE.md`.
- Reversible pre-adopt checkpoint: `pre-adopt-backup-20260623-180552`.

## Open loops / checkpoints
- none. Adoption complete (Phases A–D; merge queue deferred to OPS-11 as plan-blocked).
