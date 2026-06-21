# ClimaSite — SDLC Hardening Plan (PROC-01)

**Status:** Plan approved by owner (decisions recorded below). Execution NOT started (Wave 0 is next).
**Owner decisions captured:** 2026-06-21. **Origin:** a 6-agent research workflow (test-coverage gaps, process docs, enforcement tooling, Claude Code capabilities, real-infra E2E/UI/visual) → synthesized SDLC design.

> Goal (owner's words): build software "like a real company" with **discrete, gated phases** —
> research → plan → verify-plan → tracking docs → implement → test → review → merge — that we
> can't skip or rush. Testing & review are the strongest gates: **every workflow/use case tested
> with real-infra E2E + UI tests simulating real human interaction (NO mocking the API/DB)**, plus
> automated a11y, performance budgets, and ephemeral UI screenshots for visual verification.

---

## 1. The 8 gated phases

Each feature gets a folder `docs/features/<FEAT-ID>/` with these artifacts. Only **hooks**, **CI
required-checks**, and **permission denies** truly block; everything else is a nudge, so each gate is
anchored to a real blocker.

| # | Phase | Artifact | Exit criterion | Enforcement (hard gate) |
|---|-------|----------|----------------|--------------------------|
| 1 | **Research** | `research.md` (goal, affected files verified by grep, existing tests, constraints {payment/auth/GDPR/i18n/theme/a11y}, user journeys happy+edge+error) | Every affected-file path resolves; ≥1 happy + ≥1 edge + ≥1 error journey; constraints listed | SessionStart hook injects current phase from `STATE.md`; CLAUDE.md "no plan without research.md" |
| 2 | **Plan** | `plan.md` (assumptions citing research lines, **exit-criteria table**, **test matrix naming the E2E/visual/a11y cases**, rollout, `ADR-needed?`); header `plan_status: draft` | Non-empty exit-criteria table + test matrix; payment/auth/GDPR/migration flagged for ADR | **PreToolUse(Edit/Write) exit 2** when editing `src/` and active feature's `plan.md` is missing |
| 3 | **Verify-Plan** *(most-missing today — 0 repo refs)* | verification stamp → `plan_status: approved`; `plan-verification.md` | Independent agent re-confirms paths/assumptions/test-matrix/ADR vs working tree; status flipped | **Read-only `verifier` agent** (can't edit→can't launder); **Stop hook exit 2** while `plan_status != approved`; PreToolUse(src) blocked unless approved |
| 4 | **Track** | backlog row (full anatomy) + `test-design.md` (every human scenario → stable Scenario ID → the test that covers it) + ADR if flagged | Backlog row w/ acceptance; every journey has a Scenario ID marked automated\|manual | CI **test-design-coverage lint**; **ADR gate** (decision PR with no `docs/adr/NNNN` fails; in-place edits of Accepted ADRs rejected) |
| 5 | **Implement** | code **+ colocated tests** on `feature/`\|`fix/` branch; `STATE.md` ticks; expand-contract migrations | All "automated" Scenario IDs have a test; builds clean; no anti-patterns (`as any`, hardcoded colors/text, RxJS-for-state, NgModules) | Existing 5 PreToolUse git/secret hooks **+ NEW "tests-with-feature" hook (exit 2)** unless `[no-tests]` justified in commit body; `check-i18n.mjs` |
| 6 | **Test** ⭐ | green CI incl. real-infra E2E + UI + **visual** + **a11y** + **perf** across light/dark × EN/BG/DE × mobile/desktop | Real card happy+declined+3DS E2E vs Stripe test mode + webhook reconcile; axe matrix; Lighthouse ≥0.90 mobile; GDPR+Inventory covered; coverage ≥ floor; **all required checks green** | **CI required status checks** (server-side, agent-proof). LOCAL: PostToolUse(Bash) drops a test-ran marker; **Stop hook exit 2 if src changed & no marker this session** ("refuse to finish untested") |
| 7 | **Review** ⭐ | `review.md` (findings by severity) + `security-review.md` (auth/payment/GDPR/config) + PR checklist | `/code-review` (13 dims) + `/security-review` (OWASP) run with real findings triaged; verifier confirms tests are real not placeholders | **Read-only `reviewer` agent** (no Edit/Write — physically can't edit-and-hide); **PR template** + **CI danger.js** failing on empty test-matrix/review section; Stop hook requires `review.md` when >3 src files changed |
| 8 | **Merge** | squash commit on `main` + tracking docs updated in same PR | All required checks green; review recorded; `PROJECT_STATUS.md`/`PRIORITIZED_BACKLOG.md`/`CLAUDE.md` table updated | GitHub branch protection (`enforce_admins=true`) with the **full expanded required-check set**; existing push/commit/branch PreToolUse hooks as local backstop |

---

## 2. Build plan — 7 waves

| Wave | Deliverables | Effort | Depends |
|------|--------------|--------|---------|
| **0 — Foundations** | Copy version-pinned governance from the vault `~/Projects/vault/vault/AI/project-template/` into `.claude/`: **agents** `reviewer.md`, `verifier.md`, `qa.md`, `developer.md`, `security.md` (reviewer+verifier are read-only by `tools:` whitelist = the separation-of-duties enforcement); **skills** `verify-work`, `perf-budget`, `accessibility-audit` (+ refactor, skill-verifier). Fix doc drift: CLAUDE.md step 4 ("git push origin main" is wrong) + `DEV_WORKFLOW.md:93` (branch protection IS enabled now). Declare `DEV_WORKFLOW.md` canonical; CLAUDE.md/AGENTS.md link to it. | S | — |
| **1 — Per-feature pipeline** | `docs/features/_template/{research.md,plan.md,test-design.md,STATE.md}`; `/feature-kickoff` command (scaffolds folder + registers backlog row); `/verify-plan` skill (checklist → writes approval stamp, flips `plan_status: approved`, run by `verifier`); CLAUDE.md rules wiring the four front phases in. | M | Wave 0 |
| **2 — Phase-aware hooks** | `SessionStart` (inject phase+DoD from `STATE.md`); `PreToolUse(Edit/Write)` **no-code-without-approved-plan** (exit 2); `PreToolUse(Edit/Write)` **tests-with-feature** (exit 2 unless colocated test present/staged or `[no-tests]`); `PostToolUse(Bash)` test-ran marker; **Stop hook** exit 2 when src changed & no marker / plan still draft. KEEP the existing 5 git/secret hooks. **Test hooks carefully — a broken PreToolUse can block all edits.** | M | Wave 1 (plan_status convention) |
| **3 — CI hard gates** | lint/format job (`dotnet format --verify-no-changes` + `ng lint`); dependency-audit (`dotnet list package --vulnerable` + Trivy + **gitleaks**); **coverage gate ENFORCED at 80% backend / 70% frontend** (replace codecov `continue-on-error` with coverlet `/p:Threshold` + karma); **test-design-coverage lint**; **ADR gate**. Add all to `test-summary` needs[] + branch-protection required checks. | M | Wave 1 (test-design convention) |
| **4 — Review hardening** | `.github/pull_request_template.md` (mandatory: tests added, CI link, **light+dark screenshots**, migrations/env-vars, `/code-review` run, `/security-review` for auth/payment/GDPR); `.github/CODEOWNERS`; CI **danger.js** failing on empty test-matrix/review section. **NOT requiring a GitHub approval** (owner chose agent+template+danger, keeping AI auto-merge). | S | Waves 0, 3 |
| **5 — Visual + a11y + perf harness** | **Visual = EPHEMERAL** (owner: do NOT commit baselines): capture screenshots across {light,dark}×{en,bg,de}×{desktop1280,mobile390} for AI/human review during test/review, then delete; **screenshot+trace on E2E failure** wired into `PlaywrightFixture` (uploaded as `if:failure()` artifacts, retention-expired). axe matrix deepened (parameterize lang×theme, extend to admin/orders/account/wishlist/reviews/contact/legal/mega-menu, keyboard-journey + focus-trap + ARIA-live, fail on serious). **Lighthouse CI** job + `lighthouserc.json` (perf ≥0.90 mobile). Shard E2E (functional/visual/a11y/perf/payments) to beat the 50-min timeout. | XL | Wave 3 |
| **6 — Close coverage gaps** | **P0**: real Stripe card E2E (`e2e-payments` job w/ CI secrets; 4242 + 3DS `4000 0027 6000 3184` + decline `4000…0002`; webhook signature/reconcile contract test) — bank transfer stays the no-secret fallback. **P0**: GDPR export/delete (integration + E2E). **P1**: Inventory handlers; SearchProductsQuery facets; forgot/reset-password + lockout E2E; address-book E2E. **P2/P3**: email body+BG/DE content; notifications E2E; admin KPI/low-stock; consent reject/customize. | XL | Wave 5 (for UI flows) |

---

## 3. Test-gap backlog (prioritized — the "what's missing")

- **P0** Real **credit-card money path** never run E2E (every completing E2E uses bank transfer; `Api.Tests` swap in `FakePaymentService`; only key is a dummy). Card capture, 3DS/SCA, declines, **webhook signature verification** are never exercised against real Stripe.
- **P0** Declined/failed-card UX (error shown, stays on payment, cart not left paid).
- **P0** **GDPR** `ExportUserDataQuery` / `DeleteUserDataCommand` = **zero coverage** (stated business rule).
- **P0** Stripe webhook **contract test** (signed `payment_intent.succeeded/failed` → reconcile).
- **P0 cross-cutting** No **visual regression** of any kind; **no Lighthouse/perf budget** in CI; coverage thresholds advisory only (codecov `continue-on-error`).
- **P1** Inventory handlers (Adjust/BulkAdjust/SetLowStockThreshold/GetInventoryList + reservation lifecycle); SearchProductsQuery facet aggregation/ranking; forgot/reset-password + lockout E2E; account address-book E2E+integration; **a11y breadth** (admin/orders/account/wishlist/reviews/legal/mega-menu + keyboard journeys).
- **P2/P3** Transactional-email body + BG/DE content assertions; notifications mark-read/all/delete E2E; admin dashboard KPI/low-stock; consent reject/customize E2E; Q&A + Cart handler unit tests.

**Two real bugs the screenshots already surfaced (good test targets):** checkout shows "Free Shipping"/€599.99 while the server charges €5.99 standard (€605.98) and the line item shows `$` not `€` (pricing-display, BUG-11 area); dark-mode home **room-preview renders empty**.

---

## 4. Owner decisions (RECORDED 2026-06-21)

1. **Real card E2E in CI → YES, use the keys already provided.** Add `STRIPE_SECRET_KEY` / `STRIPE_PUBLISHABLE_KEY` as GitHub Actions secrets; add an `e2e-payments` job. Bank transfer stays the no-secret fallback. *(Secrets NOT yet set — do this in Wave 6 setup. Keys are in `git`-ignored user-secrets locally; see §6.)*
2. **Review gate → reviewer AGENT + PR template + CI danger check.** Do NOT raise required-reviews to ≥1 (keeps AI auto-merge for the solo workflow).
3. **Coverage → ENFORCE 80% backend / 70% frontend NOW.** ⚠️ Must measure current coverage first (Wave 3): if below the floor, the gate will block every PR — so Wave 3 includes a coverage-raising step OR a documented short ratchet to reach the floor before flipping it hard.
4. **Visual baselines → DO NOT commit screenshots; delete after use.** No Git-LFS, no committed baselines. Visual = ephemeral capture + AI/human review + screenshot/trace-on-failure artifacts (retention-expired). Automated correctness via axe + functional + a11y.

**Defaults chosen for the other items** (owner can override): tests-with-feature hook = **hard-block** with `[no-tests]` escape; **lock hooks via managed settings** + add **husky/commitlint** git-native backstop; perf floor **≥0.90 mobile** (not the tight 0.97, to reduce flake); **`DEV_WORKFLOW.md` canonical** + a CI doc-lint grepping known-wrong tokens (`git push origin main`, `localhost:5000`, `npx playwright`, `Accept-Language`).

---

## 5. Reusable assets already in the vault

`~/Projects/vault/vault/AI/project-template/` already contains `agents/{reviewer,verifier,qa,developer,security}.md` and skills `{verify-work,perf-budget,accessibility-audit,refactor,skill-verifier}` and `templates/ci/dotnet.yml`. Wave 0 is mostly **copy + pin + wire**, not author-from-scratch. (`/research`, `/plan`, `/verify` exist only as global plugins today — a fresh clone/CI doesn't carry them, so they must be pinned into `.claude/`.)

---

## 6. Current state / where we paused (2026-06-21)

- **Card payments WORK** end-to-end (verified live: 4242 → order `ORD-…`; 4000…0002 → graceful decline). Two real bugs fixed (Stripe-init-on-Next; PaymentMethod captured on payment step then used at review). Shipped as **PR #38** — after the test-fix push (bank transfer in `Checkout_OrderReview_ShowsCorrectTotal`), **CI is re-running; merge once green.**
- **Stripe keys** live in local **.NET user-secrets** (`dotnet user-secrets`, `UserSecretsId` `dcf821de-…` in `ClimaSite.Api.csproj`) — never in git. The API auto-loads them in Development. **GitHub Actions secrets NOT yet set** (decision #1 — do in Wave 6).
- **This plan doc** is the durable artifact of the research; the raw 6-agent research output was in `/tmp` (ephemeral).
- **Process work (Waves 0–6) NOT started** — Wave 0 is the next action.
- Merged earlier this session: #35 (overlay/z-index + reviews + i18n + E2E), #36 (review cache freshness), #37 (z-index convention docs). Bug register addendum in `docs/project-plan/BUGS_AND_TECH_DEBT.md §8`.

### Run the app locally (Development)
Shared Postgres is on **:5432** (NOT the stale `:5433` in `appsettings.json`). Stripe keys come from user-secrets, so only the DB/redis overrides are needed:
```
export ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5029
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=climasite;Username=climasite;Password=climasite_dev_password"
export ConnectionStrings__Redis="localhost:6379"
dotnet run --project src/ClimaSite.Api --no-launch-profile     # API :5029
cd src/ClimaSite.Web && npm start                              # ng serve :4200
```

---

## 7. Immediate next steps (resume order)

1. **Merge PR #38** once its re-run CI is green (card payments).
2. **Wave 0** — copy vault agents + skills into `.claude/`, fix the CLAUDE.md/DEV_WORKFLOW.md doc drift, declare DEV_WORKFLOW.md canonical. (One PR.)
3. **Wave 1** — per-feature templates + `/feature-kickoff` + `/verify-plan`. Dogfood: run *this* initiative (PROC-01) through the new pipeline.
4. **Wave 2** — phase-aware hooks (build + test each hook in isolation; a broken PreToolUse blocks all edits).
5. **Wave 3** — CI gates; **measure coverage** before flipping the 80/70 hard gate.
6. **Waves 4–6** — review hardening, visual/a11y/perf harness, then close the P0/P1 coverage gaps (real Stripe card E2E first).
