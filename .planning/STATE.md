# STATE — resume contract for climasite

> Auto-injected by the **SessionStart hook** (`hooks/state-prime.sh`) after any `/clear`, compaction, or
> `--resume`. Read **Next action** first. Kept fresh via `/checkpoint` at each unit/phase boundary.
> LEAN — pointers, not prose. **This is the single entry point; everything else is linked below.**

- **Last checkpoint**: 2026-07-01 (**B-039 per-voter Q&A vote ledger** — in PR `feature/b-039-qa-vote-ledger`, council-clean, `/acceptance` PASS, awaiting CI. **B-044 SEO is fully MERGED** — #92 Wave A `819fb18` + #93 Wave B `ae082df`.)

## Goal
Production-grade multi-language (EN/BG/DE), multi-theme HVAC e-commerce platform — finish to production readiness with a hardened SDLC.

## Current position
- **Phase**: maintenance / incremental hardening. External-review register CLEARED. **B-044 SEO** (#92/#93), **B-039 Q&A vote ledger** (#94 `80f0c89`), and the **a11y batch B-001/B-014/B-042** (#95 `23bd15e`) are all merged; `main` tip = `23bd15e`.
- **B-016 canonical HVAC spec resolver + BTU recalibration — in PR `feature/b-016-hvac-spec-resolver` (awaiting CI → squash-merge).** Recommendation scoring + query handler + HVAC facet extractors read canonical spec keys the display-key catalog never emits → flat fallback. Fixed with a shared alias-aware **`HvacSpecResolver`** (ordered aliases so cooling-BTU beats a heat pump's `"BTU Heating"`; per-key typed parsers Int/Decimal/Bool/StringList/String; CLR + JSONB `JsonElement`; unit-stripping `"22 dB"`→22 / `"12,000 BTU"`→12000; rejects `"0.8 sones"`; NOT `"Operating Range"`→minTemp) — dropped the duplicated `Extract*` helpers; facet extractors now group by canonical key with precedence + one-value-per-canonical (G29 decimals). **Nested defect (owner-approved recalibration):** the `90/110/140` BTU/m² model was ~2–3× too low → real ratings scored 0 for normal rooms → recalibrated to `A=200/B=250/C=320` across the BE scoring service + query handler + FE `home-wizard-state` preview, default wizard area `24→35`. `DataSeeder` gains the 3 machine-only canonical fields; the public product DTO **strips** them from the PDP (kept display specs). Fixed the BG/DE FAQ "per square metre"→"per square foot" mistranslation. **No migration** (JSONB); write-boundary key-rewriting deliberately **not** added (resolver-at-read is the single-source enforcement — council High). Design council descoped 3 Highs; diff council: no Highs → 3 Med + 2 Low all fixed → **re-council CLEAN**. `/acceptance` PASS at `.planning/acceptance/B-016-spec-aliasing.md` (live recs size-matched: area 48→12000-BTU / area 12→2600-BTU / area 75-C→22000+ inverter units; PDP hides machine keys; FE preview shows 8750). Break-probe proven. Core 428 / Application 965 / Api 470 / FE 1788 green; `dotnet format` clean. **Local /acceptance note:** the 5 hand-authored seed products were DB-patched to the new machine-field shape (seeder skips when products exist); the resolver reads `"BTU"` from all 2260 unchanged.
- **NEXT queue (owner picks)**: a11y batch [DONE], deploy-hardening F3/OPS-07/OPS-04 (+Railway, owner-gated), SEC-12 dependency upgrades (incl. the Microsoft.OpenApi/Swashbuckle bump the B-039 allowlist defers), B-038 (answered-state bug), or the B-039 follow-up (per-user rate-limit partition). Only acknowledged Low left = notifications `summary` `recentCount` (intentional "recent N").

## ✅ Done (all merged to `main`)
1. **PROC-01 SDLC hardening** — all 7 waves, 18 PRs. Gated pipeline, CI gates, 80/70 coverage, branch protection, 163 integration tests, real-card Stripe E2E. Tracker: `docs/features/PROC-01/STATE.md`.
2. **Workflow adoption** (`/project-adopt`, PRs #55–57) — latest vault agents/skills/hooks, vault **Knowledge graph** at `…/vault/Knowledge/climasite/` (18 components, 3 ADR decisions, 3 milestones, 8 risks, 6 questions), `.planning/` scaffold. `no-spec-no-code` now gates `src/**` (escape `ALLOW_EXPLORATORY=1`).
3. **Plan 19 — test + KG hardening** (`docs/plans/19-test-and-kg-hardening.md`, council-validated):
   - E2E: **195 NetworkIdle waits purged** → locator auto-waiting + `SettleAsync`; trace/screenshot-on-failure; **`[RetryFact]` guarded retry** (PRs #58, #59).
   - UI: **57 specs for the 4 untested services** (PR #58).
   - **UX-15 a11y: fixed + ENFORCED** — `--color-primary-surface` token + reduced-motion scans; `A11Y_ENFORCE=1` live in CI; both axe suites are hard gates (PR #60).
   - KG enriched (vault).

## ▶ RESUME HERE (2026-06-30)

**B-044 SEO foundation — Wave A merged (#92 `819fb18`); Wave B backend in PR `feature/b-044-seo-backend`.**
Unit-plan: `.planning/units/B-044-seo/unit-plan.md` (Wave A items 1–10 + Wave B items 11–17; 3 design council
rounds + both diffs councilled). `/acceptance` PASS for both waves (committed reports).

**NEXT ACTION:**
1. **Finish Wave B:** the diff council returned APPROVE-WITH-CHANGES (2 Mediums) — apply the slug
   `Uri.EscapeDataString` + the nginx private-prefix-before-static-asset regex reorder, re-verify
   (`dotnet build` + Application + Api-SEO tests), then commit (with these doc updates — CHANGELOG/STATE/
   backlog/PROJECT_STATUS), push, open the PR, get the green CI checks, and squash-merge.
2. **Then OWNER picks the next larger item:** B-039 (Q&A vote per-voter ledger) · B-016 (recommendation
   spec-key aliasing) · a11y batch B-001/B-014/B-042 (address dialogs + checkout radios) · deploy-hardening
   F3/OPS-07/OPS-04 (+Railway, owner-gated).

**Gotchas reconfirmed this unit:** `/acceptance` on the REAL running app is mandatory — it caught a fatal
NG0200 circular-DI (Router injected into the Router-constructed `TitleStrategy`) that ALL 1764 unit tests
missed. CRLF C# files: `git diff --check` flags CRLF as trailing-ws but the real gate `dotnet format
--verify-no-changes` passes (`.editorconfig` leaves `end_of_line` unset). Local run: API `DATABASE_URL`/
`ConnectionStrings__DefaultConnection` → shared-infra Postgres `:5432/climasite` (creds in
`~/Projects/shared-infra/.env`); a fresh API build is needed per branch switch.

_(Historical SEARCH-01-fts / #77 / PAY-IDEM resume notes below are SUPERSEDED — kept for context only.)_

**What SEARCH-01-fts is:** public product search migrated ILIKE→**Postgres FTS**. Trigger-maintained
denormalised `products.search_vector` (base + tags + ALL translations), `climasite_search` config
(simple+unaccent), GIN + pg_trgm indexes; one shared `IProductSearchService` (parameterized raw SQL) does
match + facets + `ts_rank_cd` + exact-SKU boost + paging + window-count; per-term substring branch = recall
superset; both public paths unified. Unit-plan: `.planning/units/SEARCH-01-fts/unit-plan.md`. Design + diff
**council-validated** (Codex + plan-critic — they caught the array_to_string-STABLE, cross-table-multi-term,
websearch-vs-plainto, and MigrateAsync-not-EnsureCreated issues; all folded in). **12 FTS integration tests
+ rank-reversal mutation gate pass; full backend suite green (Core 424 / App 823 / Api 305).**

**FIRST STEPS ON RESUME:**
1. `git checkout feature/search-fts`; `git log --oneline -3`. If a PR is open: `gh pr checks` → merge if green
   (after a diff-council-clean + an `/acceptance` PASS committed at the tip per the DoD).
2. If NOT pushed yet: finish the diff council (`scratchpad/codex-diff/codex.md`), address any High/Medium,
   then push + open PR to main, get 6 green checks, run `/acceptance` against the running app (drive the
   header search box), then squash-merge.
3. **Follow-up surfaced:** `AccessibilityTests.cs:167` navigates the wrong `/auth/register` route (masked by a
   tolerant wait) → its a11y scan runs on the wrong page; fix route as a separate triaged unit (may surface
   new axe violations under `A11Y_ENFORCE=1`).

### Merged to main this session (2026-06-27) — for context (newest first)
#76 B2 component specs batch 2 (+65, admin-order-detail/admin-moderation/mega-menu) · #75 OPS-03 delete
dead duplicate Docker/Railway config · #74 SEC-06 gate Swagger out of prod · #73 workflow sync (adopted
`/acceptance` gate + check #11 + broadened council cadence) · #72 SEC-07 remove dummy Stripe keys + prod
fail-fast · #71 OPS-08 deploy-readiness runbook · #70 DEC-SHIPPING free standard shipping >€50 · #69 SEC-14
GDPR order-PII erasure · #68 B2 specs batch 1 · #67 OPS-05 correlation IDs · #66 SEC-08 security headers ·
#65 admin retry-net · #64 BUG-11 EUR currency. **Standing rule:** run the cross-vendor Codex council
(`gpt-5.5`@`xhigh`) on every non-trivial change; behaviour/source PRs want an `/acceptance` pass. See
[[feedback_council_merge_gate]].

### Remaining backlog after #77 (all CI-verifiable unless noted)
- **Deploy-hardening (best validated against a real Railway deploy — owner-gated):** F3 `Dockerfile.api`
  honor `$PORT`, OPS-07 non-root containers, OPS-04 pre-deploy migrations, OPS-03's `deploy.yml` CD workflow.
- **Search → Postgres FTS** (high value; needs a tsvector-column migration — blast radius = all integration
  tests, so do it carefully in a focused session; the search handlers have NO InMemory unit tests so FTS is
  feasible; hybrid FTS-for-prose + substring-for-codes to avoid regressing SKU/model search).
- **Plan-19 B3** (replace ~27 placeholder `should create` specs) · ~18 more untested components.
- **VERIFIED NOT-BUGS this session:** Q-006 SalePrice (correctly mapped) + Q-003 stock (atomic `stock>=qty`
  guard + charged-but-no-stock refund) — inventory reservations would be an enhancement, not a fix.

## After #77 — OWNER: answer the 5 OPS-08 questions, then deploy-hardening dev items
**DEC-SHIPPING + OPS-08 deploy-readiness prep are DONE.** The canonical deploy runbook is
**`docs/runbooks/deploy.md`** (artifact map, env-var matrix with the REAL var names the code reads, deploy
procedure, rollback, the 5 OPS-08 owner questions, known gaps).
- **OWNER-GATED (one pass):** answer the 5 OPS-08 questions in `docs/runbooks/deploy.md §5` — create the
  Railway project + 2 services (build context = repo root; **API target port = 8080**), set the secret
  env-vars, provision Postgres/Redis/object-storage + **enable+test DB backups**, and **rotate the seeded
  `admin@climasite.local` + JWT_SECRET if any prod DB was ever booted**. Record answers in DECISIONS.md.
- **Autonomous dev follow-ups the runbook surfaced** (good next units, each gated/PR'd):
  **F3/OPS-07** make `Dockerfile.api` honor Railway `$PORT` + run containers non-root (recommended NEXT);
  **OPS-04** move EF migrations to a pre-deploy step (today it auto-migrates on startup — crash-loop risk,
  keep API at 1 replica). **SEC-06, SEC-07, OPS-03 (cleanup) are DONE** (below). OPS-03's `deploy.yml` CD
  workflow + the actual Railway deploy are **owner-gated** (need the Railway project + `RAILWAY_TOKEN`).

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
- **SEC-07 (DONE 2026-06-27):** removed the committed dummy Stripe keys from `appsettings.json` (0 in tracked source); new `StripeConfiguration.ValidateProductionConfiguration` (mirrors `JwtConfiguration`) wired in `ConfigureServices` → **Production fail-fasts at startup** on missing/placeholder Stripe keys; safe in Dev/Testing (scoped `StripePaymentService` + `FakePaymentService`). CLAUDE.md env table → `Stripe__*`. JWT was already prod-guarded. Tests green; council-reviewed (3 rounds). Follow-up: same pattern for SMTP/MinIO.
- **OPS-03 cleanup (DONE 2026-06-27):** deleted the 4 dead/stale deploy duplicates (`src/ClimaSite.Api/Dockerfile` [identical to root], `src/ClimaSite.Web/Dockerfile` [wrong build context], `src/ClimaSite.Api/railway.toml`, `src/ClimaSite.Web/nginx.conf` [stale]) — root `Dockerfile.api`/`Dockerfile.web` + `railway.toml`/`railway.api.toml` are the single canonical set. CD `deploy.yml` deferred (owner-gated). Council-reviewed.
- **SEC-06 (DONE 2026-06-27):** gated `UseSwagger`/`UseSwaggerUI` behind `IsDevelopment()` — API schema + UI served only in Development (404 in Prod/Staging/Testing); integration-tested (`/swagger` + `/swagger/v1/swagger.json` → 404 in Testing; security headers still apply). Council-reviewed.
- **Workflow sync from vault (DONE 2026-06-27):** adopted the new **`/acceptance`** skill (runtime exploratory gate — drive the REAL app adversarially before merge; PASS report at `.planning/acceptance/<id>.md` matching HEAD) + `workflow-check.sh` **check #11** (advisory nudge) + the updated `review-orchestrate`/`trunk-merge`/`verify-work` skills that wire it in. Confirmed `council.sh` already = Codex `gpt-5.5`@`xhigh` (best/highest). **Owner preference recorded:** use the cross-vendor council on **every non-trivial change**, not just security/compliance (see [[feedback_council_merge_gate]] memory). Vault has no other new agents/hooks since the 2026-06-23 adoption (only minor skill refreshes — autoresearch/cost-check/etc. — deferred).

## Recently done (2026-06-25)
- **Docs consolidated to ONE planning system** (`/hygiene-sweep`-style pass): retired the bespoke PROC-01 `docs/features/` pipeline + its duplicate hooks (`require-approved-plan`, `session-phase`) + skills (`feature-kickoff`, `verify-plan`) — the vault `.planning/units` + `/plan-tree` + `no-spec-no-code` flow is now the single system; added `docs/README.md` map; bannered ~20 stale legacy trackers; ADR-002 immutability fixed via superseding **ADR-0003**; leaned CLAUDE.md (status tables + pipeline → pointers). No protection lost (`no-spec-no-code` + test-ship + git/secret guards intact).

## Key pointers
- **Plan ahead + tracking**: this file (resume) · `docs/plans/19-test-and-kg-hardening.md` (test/KG plan) · `docs/project-plan/PRIORITIZED_BACKLOG.md` (full backlog) · `docs/features/PROC-01/STATE.md` (SDLC waves) · vault `Knowledge/climasite/` (graph).
- Currency = EUR + dual EUR/BGN display (peg 1.95583) — DECIDED. Nothing deployed yet (OPS-08).
- Reversible pre-adopt checkpoint: `pre-adopt-backup-20260623-180552`.
- Gotchas (memory): NEVER reintroduce `LoadState.NetworkIdle` in E2E; CRLF endings; classic branch protection is the gate.

## Open loops / checkpoints
- **B-044 Wave B** (`feature/b-044-seo-backend`) — finishing 2 council Mediums → commit → PR → CI → squash-merge. Wave A is merged (#92).
