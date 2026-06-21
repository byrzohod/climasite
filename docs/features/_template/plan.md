---
plan_status: draft   # draft → approved (only /verify-plan, run by the read-only verifier agent, may flip this)
feature_id: <FEAT-ID>
verified_by:          # set by /verify-plan
verified_date:        # set by /verify-plan
---

<!--
PHASE 2 — PLAN. Depends on research.md (cite its line numbers in Assumptions).
Exit criterion (Phase 2 → Phase 3): a NON-EMPTY exit-criteria table AND a NON-EMPTY test matrix
naming the concrete E2E/visual/a11y cases; every payment/auth/GDPR/migration constraint checked in
research.md §4 is reflected in "ADR needed?" below.
The plan_status stays `draft` until the verifier approves it (Phase 3). PreToolUse hooks (Wave 2)
block edits to src/ for this feature while plan_status != approved — do not hand-edit it to `approved`.
-->

# Plan — <FEAT-ID>: <short title>

## 1. Approach

2–4 sentences: the chosen implementation strategy and why. Note the layers touched
(Core → Application → Infrastructure → Api → Web) and the order of work.

## 2. Assumptions (each cites a line in `research.md`)

> If an assumption can't be tied to a researched fact, it's a risk — move it to §6.

| # | Assumption | Source (`research.md:line`) |
|---|------------|-----------------------------|
| A1 | | research.md:NN |

## 3. Exit-criteria table (REQUIRED — non-empty)

> Each row is a checkable, user-observable outcome. These are what "done" means; they map 1:1 to
> acceptance in the backlog row and to Scenario IDs in `test-design.md`.

| # | Exit criterion (observable behavior) | Covered by Scenario ID(s) |
|---|--------------------------------------|---------------------------|
| EC1 | | H1 |
| EC2 | | E1 |
| EC3 | | X1 |

## 4. Test matrix (REQUIRED — name the concrete cases, not "add tests")

> Every "automated" Scenario becomes a real test here. NO MOCKING the API/DB in integration/E2E.
> Matrix dimensions where UI is involved: light/dark × EN/BG/DE × mobile/desktop.

| Scenario ID | Level (unit/integration/E2E/visual/a11y/perf) | Test name / location | Real infra? |
|-------------|-----------------------------------------------|----------------------|:-----------:|
| H1 | E2E | `tests/ClimaSite.E2E/Tests/<Area>/<Name>.cs` | yes |
| E1 | integration | `tests/ClimaSite.Api.Tests/...` (Testcontainers) | yes |
| X1 | unit | `<handler>Tests.cs` / `<component>.spec.ts` | n/a |
| — | a11y | axe matrix (lang×theme) on `<page>` | yes |
| — | visual | ephemeral screenshot {light,dark}×{en,bg,de} for review | yes |

## 5. ADR needed? (flag from research.md §4 constraints)

- **Payment / Auth / GDPR / Migration touched?** <yes/no> — if yes, ADR is **required** before merge.
- **ADR:** <`docs/adr/NNNN-<slug>.md` — or "n/a, no architectural decision">

## 6. Rollout & risks

- **Migration:** <expand-contract steps, or "none">
- **Config / env vars:** <new keys + which env, or "none">
- **Feature flag / phased rollout:** <if any>
- **Risks & mitigations:** <what could break; how we'd notice; rollback>

## 7. Out of scope

What this feature deliberately does NOT do (prevents scope creep; future backlog rows).
