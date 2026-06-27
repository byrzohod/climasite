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

## ▶ Next action — OWNER: answer the 5 OPS-08 questions, then deploy-hardening dev items
**DEC-SHIPPING + OPS-08 deploy-readiness prep are DONE.** The canonical deploy runbook is
**`docs/runbooks/deploy.md`** (artifact map, env-var matrix with the REAL var names the code reads, deploy
procedure, rollback, the 5 OPS-08 owner questions, known gaps).
- **OWNER-GATED (one pass):** answer the 5 OPS-08 questions in `docs/runbooks/deploy.md §5` — create the
  Railway project + 2 services (build context = repo root; **API target port = 8080**), set the secret
  env-vars, provision Postgres/Redis/object-storage + **enable+test DB backups**, and **rotate the seeded
  `admin@climasite.local` + JWT_SECRET if any prod DB was ever booted**. Record answers in DECISIONS.md.
- **Autonomous dev follow-ups the runbook surfaced** (good next units, each gated/PR'd):
  **SEC-06** gate Swagger out of production (recommended NEXT); **OPS-03** delete the dead duplicate
  `src/**` Dockerfiles + `src/ClimaSite.Api/railway.toml` (root is canonical) + add a `deploy.yml`;
  **F3/OPS-07** make `Dockerfile.api` honor Railway `$PORT` + run non-root; **OPS-04** move EF migrations
  to a pre-deploy step (today it auto-migrates on startup — crash-loop risk, keep API at 1 replica).
  **SEC-07 is DONE** (below).

## Remaining (tracked — none blocking; full detail in `docs/project-plan/PRIORITIZED_BACKLOG.md`)
- **DEC-SHIPPING** (next, above) · **OPS-08 deploy-readiness prep** (Railway; owner adds account+secrets).
- **UX-16** transitional dual EUR/BGN display (peg 1.95583) — only needed pre-BG-launch.
- **SEC-14 residual follow-ups** (council round-3, non-blocking): `ExportUserDataQuery` should also cover
  same-email guest orders (Art-15/20 consistency with the new deletion scope); outbox-worker send-race
  (a row already being dispatched can't be unsent — needs worker-level suppression for absolute guarantee).
- **Plan 19 B2/B3** — ~24 more untested components + replace ~27 placeholder `should create` specs (tests/).
- **SEC-12** Angular 19→major upgrade (7 high npm advisories); **SEC-13** gitleaks allowlist→enforce.
- **Lighthouse** flip warn→enforce (needs a stable baseline first — currently a non-required, variance-prone job).
- **OPS-11** trunk merge queue — plan-blocked (paid GitHub plan); ruleset + `merge_group` staged.
- **Inventory reservations** (no reserve/hold; oversell window) · **Search** ILIKE→FTS/Meilisearch.
- **KG open items** — R-006/R-007/R-008; **VERIFY-first**: Q-003 stock-reservation, Q-006 SalePrice (confirm vs code first).

## Recently done (2026-06-26)
- **DOC-02 verified per-feature status pass** — code-read across 4 clusters (+ hand spot-checks) → refreshed `docs/project-plan/PROJECT_STATUS.md` to a dated, evidence-backed SSOT. REFUTED the stale "broken/stub" claims (admin CRUD, notifications, contact, legal, installation, GDPR-delete, payments all verified complete); corrected 2 verifier errors (BUG-03 cart-merge IS fixed; wishlist guest-merge EXISTS); confirmed the real open gaps (search-ILIKE, inventory-no-reservations, Lighthouse-reporting-only, SEC-08/14, OPS-08). CLAUDE.md caveat updated → PROJECT_STATUS is current.
- **BUG-11 / DEC-CURRENCY (display)** — `DEFAULT_CURRENCY_CODE='EUR'` + all 18 bare `\| currency` → `:'EUR'`; checkout shipping labels now show the **server's** EUR tiers (€5.99/€15.99/€19.99, mirrors `CheckoutPricing.cs`) instead of "free"/`$9.99`/`$19.99` (displayed==charged). 1246 FE tests green. Follow-ups filed: **UX-16** (dual EUR/BGN), **DEC-SHIPPING** (should standard be free?).
- **SEC-08 (headers)** — `SecurityHeadersMiddleware` adds nosniff / X-Frame DENY / Referrer-Policy / Permissions-Policy / X-XSS-Protection + a strict API CSP (skipped for `/swagger`); integration-tested (2/2). Remaining (deploy-time, OPS-08): frontend Stripe-CSP + CORS allowlist + AllowedHosts.
- **E2E retry net extended** to the 4 auth-heavy admin classes (recurring AdminPanelTests redirect flake) — same guardrail (timeout-only).
- **OPS-05 observability floor** — `CorrelationIdMiddleware` (X-Correlation-Id generate/echo + Serilog LogContext), `traceId` in error responses, `Log.CloseAndFlush` on shutdown; integration-tested (3/3). Remaining (deploy-time / O-4): error tracker (Sentry), OTel, JSON-console-prod, uptime alerts.
- **Plan-19 B2 (partial)** — specs for the 3 highest-value untested components: product-list (32) + cart (18) + register (15) = +65, suite 1246→1311 green. ~24 lower-value components + B3 (placeholder-spec replacement) remain.
- **SEC-14 GDPR Orders-PII (DONE, ADR-0004, PR #69)** — owner decision = anonymize order PII on deletion, retain the accounting record (Art. 17(3)(b)). `Order.AnonymizePersonalData()` scrubs email/phone/addresses/**Notes/CancellationReason/GuestAccessToken**; handler erases account **+ same-email guest** orders (case-insensitive) and deletes the matching **outbox** rows (case-insensitive, pre-anonymize) to stop post-erasure sends. **Cross-vendor council (Codex) ran 3 rounds** — found real High gaps each round (free-text PII, outbox, guest orders, case mismatch); all fixed; round-3 = no High/Medium blocker. 10/10 GDPR tests. KG R-002 mitigated / Q-005 resolved. Residuals tracked above.
- **Owner decisions made (2026-06-27, "best for a real company"):** SEC-14 = anonymize-but-retain (DONE) · DEC-SHIPPING = free standard shipping over €50 (DONE) · OPS-08 deploy = prepare Railway-readiness artifacts, owner adds account+secrets (NEXT).
- **DEC-SHIPPING (DONE 2026-06-27):** free standard shipping when subtotal ≥ €50, else €5.99 (express €15.99 / overnight €19.99). Server `CheckoutPricing.GetShippingCost(method, subtotal)` is the single source of truth; `CalculateTotal` + `CreateOrderCommand` pass the subtotal; **fixed a latent checkout-summary bug** (the summary read the always-0 `cart.shipping` → always "Free"/total omitted express+overnight) — summary now uses one centralized `shippingCostFor()` so displayed==charged. Built via a Workflow (parallel server+UI → adversarial money-path verify=PASS). Cart-page Medium (council) also fixed via a shared `core/pricing/shipping.ts` helper used by cart+checkout. Tests: Application 823 + Api money-path 9/9 + ng 1321 green; 2 council rounds clean. Advisory follow-up: normalize `CartDto.Total` server-side (no live consumer now).
- **OPS-08 deploy-readiness prep (DONE 2026-06-27):** `docs/runbooks/deploy.md` — devops review found the root `Dockerfile.api`/`Dockerfile.web` + `railway.toml`/`railway.api.toml` are canonical (the `src/**` Dockerfiles + `src/ClimaSite.Api/railway.toml` are DEAD/stale → OPS-03 delete); env-var matrix uses the REAL var names the code reads (`ADMIN_INITIAL_PASSWORD` throws on prod startup; `API_URL`, `AllowedOrigins__*`, `Minio__*`, double-underscore `Stripe__*`/`Email__*`); 5 owner-gated questions in §5. Surfaced: F3 API hardcodes :8080 (set Railway port 8080), SEC-06 Swagger-in-prod, SEC-07 committed dummy secrets, OPS-07 root containers, OPS-04 auto-migrate-on-startup. The actual deploy is owner-gated (Railway account+secrets).
- **SEC-07 (DONE 2026-06-27):** removed the committed dummy Stripe keys from `appsettings.json` (0 in tracked source); new `StripeConfiguration.ValidateProductionConfiguration` (mirrors `JwtConfiguration`) wired in `ConfigureServices` → **Production fail-fasts at startup** on missing/placeholder Stripe keys; safe in Dev/Testing (scoped `StripePaymentService` + `FakePaymentService`). CLAUDE.md env table → `Stripe__*`. JWT was already prod-guarded. Tests green; council-reviewed. Follow-up: same pattern for SMTP/MinIO.

## Recently done (2026-06-25)
- **Docs consolidated to ONE planning system** (`/hygiene-sweep`-style pass): retired the bespoke PROC-01 `docs/features/` pipeline + its duplicate hooks (`require-approved-plan`, `session-phase`) + skills (`feature-kickoff`, `verify-plan`) — the vault `.planning/units` + `/plan-tree` + `no-spec-no-code` flow is now the single system; added `docs/README.md` map; bannered ~20 stale legacy trackers; ADR-002 immutability fixed via superseding **ADR-0003**; leaned CLAUDE.md (status tables + pipeline → pointers). No protection lost (`no-spec-no-code` + test-ship + git/secret guards intact).

## Key pointers
- **Plan ahead + tracking**: this file (resume) · `docs/plans/19-test-and-kg-hardening.md` (test/KG plan) · `docs/project-plan/PRIORITIZED_BACKLOG.md` (full backlog) · `docs/features/PROC-01/STATE.md` (SDLC waves) · vault `Knowledge/climasite/` (graph).
- Currency = EUR + dual EUR/BGN display (peg 1.95583) — DECIDED. Nothing deployed yet (OPS-08).
- Reversible pre-adopt checkpoint: `pre-adopt-backup-20260623-180552`.
- Gotchas (memory): NEVER reintroduce `LoadState.NetworkIdle` in E2E; CRLF endings; classic branch protection is the gate.

## Open loops / checkpoints
- none. main is clean (0 open PRs); all CI gates green.
