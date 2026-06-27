# Plan 19 — Test hardening (E2E + UI) + Knowledge-graph enrichment

**Method:** diagnosed by a parallel Explore fan-out, then **council-reviewed cross-vendor** (Codex
`gpt-5.5` xhigh + a blind Claude leg) — both legs independently confirmed the diagnosis and converged on
the strategy below.

**STATUS (2026-06-24):**
- ✅ **A1** NetworkIdle purge + `SettleAsync` — **PR #58**
- ✅ **A3** trace/screenshot-on-failure — **PR #58**
- ✅ **A4** guarded `[RetryFact]` (timeout-only, max-1, loud) — **PR #59**; applied to the 3 flaky journey/order classes + the 12 `AccessibilityTests` (PR #60)
- ✅ **B1** specs for the 4 untested services — **PR #58**
- ✅ **C2 / UX-15** contrast fixed (`--color-primary-surface`) + reduced-motion scans + **`A11Y_ENFORCE=1` enforced** — **PR #60**
- ✅ **D** Knowledge graph enriched (vault)
- 🚧 **B2** highest-value untested components specced over 2 batches: batch 1 = product-list 32 + cart 18 + register 15 (+65, suite→1311); batch 2 = admin-order-detail 24 + admin-moderation 20 + mega-menu 21 (+65, suite→1386). ~21 lower-value components + **B3** (replace ~27 placeholder `should create` specs) remain
- ⏳ **A2** finish the no-wait-read / hard-sleep cleanup (`QuerySelectorAsync`/`IsVisibleAsync`/`Task.Delay`)
- ⏳ **C1** `@defer` e2e-build mitigation (low priority — NetworkIdle gone) · **C3** dev rate-limit exemption · sharding (deferred)

## 1. Validated diagnosis

### Two first-pass theories were REFUTED (verified + both council legs agree)
- ❌ **"Auth rate-limiter causes 429s in CI."** `Program.cs:321` skips `app.UseRateLimiter()` when
  `ASPNETCORE_ENVIRONMENT=Testing` (which CI sets), so `[EnableRateLimiting("auth")]` is **inert in CI**
  (it's metadata the absent middleware never reads). Active only in local Development — a minor local
  annoyance, not the CI flake. *(Latent footgun: the policies are still registered, so a careless
  "enable everywhere" would silently re-introduce the hazard.)*
- ❌ **"Unbounded xUnit parallelism → auth storms."** All 35 E2E classes share `[Collection("Playwright")]`
  (one `CollectionDefinition`), so xUnit runs them **strictly sequentially**. No parallelism exists.

### The REAL primary root cause (the smoking gun the first pass missed)
**`WaitForLoadState(NetworkIdle)` (~195 calls) against an app that is never reliably network-idle.**
`main-layout.component.ts:18,61` lazy-loads the header **and** footer with `@defer (on timer(3200ms))`,
so **every page** fires two lazy-chunk fetches ~3.2s after navigation. Each fetch resets NetworkIdle's
500ms quiet-window. Whichever test's NetworkIdle wait straddles that drifting ~3.2s boundary under a
given run's CI jitter times out — which is exactly why a **different** test fails each run and a re-run
goes green. The team already half-found this: `AxeAccessibilityMatrixTests.cs:65` deliberately avoids
NetworkIdle ("the app may long-poll") — but the other ~194 call sites didn't get the memo. Raising the
blanket timeout 10s→30s only widened the window; it can't fix an unbounded-in-*when* event.

Secondary causes: NetworkIdle used as a stand-in for "ready" (defeating Playwright's locator
auto-waiting); wait-then-act races + no-wait reads (`QuerySelectorAsync`, `IsVisibleAsync` branching) +
hard sleeps (`WaitForTimeout`/`Task.Delay`); no trace/screenshot-on-failure (flakes undiagnosable); the
fully-sequential suite is slow → CI-budget pressure; running E2E against `ng serve` (dev-server variance).

## 2. Council consensus — strategy

1. **Purge NetworkIdle → locator auto-waiting + web-first assertions.** This is THE fix (Rank 1+2 of the
   flake). The good pattern already exists (~178 `Expect(...)` calls) right next to the bad one.
2. **Add per-test trace + screenshot on failure.** `PlaywrightFixture` is an `ICollectionFixture` (no
   per-test xUnit failure hook) → do tracing at the **BrowserContext** level inside each class's
   `IAsyncLifetime` (the context is per-test even though the browser is shared): start tracing in
   `CreatePageAsync` under CI, save-on-failure / discard-on-success in teardown.
3. **Guarded retry — only AFTER 1+2 land and the suite is demonstrably <1% flaky.** Max 1 retry; only on
   `PlaywrightException`/`TimeoutException` (never assertions/4xx/5xx); every attempt saves artifacts;
   loud + counted (flake budget); opt-in, not blanket. Without these guardrails, **don't add retry** —
   it would mask regressions.
4. **Defer sharding.** The suite is sequential; sharding before the NetworkIdle purge multiplies flake
   surfaces without lowering the rate. Revisit only if wall-clock stays the binding constraint.
5. **UI: unit tests BEFORE enforcing axe.** The axe matrix lives *inside* the flaky E2E job and admits
   "real violations likely exist"; flipping `A11Y_ENFORCE=1` now stacks a guaranteed-red gate on an
   already-flaky one. Add the missing Angular unit tests first (they ride the already-green
   `frontend-tests` + `coverage` jobs), then triage+fix axe violations in `src/`, then enforce.

## 3. Work plan (split by no-spec-no-code gating)

### Phase A — E2E harness, `tests/` (UNGATED) — executing now
- **A1** `BasePage.SettleAsync(anchor)` helper (locator-wait, no NetworkIdle) + **purge all ~195
  `WaitForLoadState(NetworkIdle)`** across PageObjects + tests (delete where a locator wait follows;
  else `GotoAsync(WaitUntil=Load)` + wait on a known shell anchor). *(High impact, M effort.)*
- **A2** Replace no-wait reads (`QuerySelectorAsync` existence checks, `IsVisibleAsync` branching) with
  `Locator` + web-first `Expect`; delete hard sleeps (`WaitForTimeout`/`Task.Delay`). *(Med/Med.)*
- **A3** Per-context **trace + screenshot on failure** in `PlaywrightFixture.CreatePageAsync` +
  per-test save/discard; wire artifact upload (already `if: failure()` in `test.yml`). *(High debug
  leverage, M.)*
- **A4** Guarded **1-retry** `[RetryFact]` — **only after A1–A3 prove <1% flake.** *(M.)*

### Phase B — UI unit tests, `tests/`/`*.spec.ts` (UNGATED)
- **B1** Add specs for the **4 untested services** (review, category, moderation, promotion) — highest
  silent-regression risk. *(High/M.)*
- **B2** Add specs for the highest-value of **~27 untested components** (cart, product-list, register
  first — revenue path), modeled on the strong `checkout`/`order-confirmation` specs. *(High/L.)*
- **B3** Replace the ~27 placeholder `should create` specs with behavioral assertions over time. *(M.)*

### Phase C — `src/` (GATED — needs a `.planning/**/unit-plan.md` via /plan-tree)
- **C1** Make the `@defer on timer(3200ms)` header/footer **not** fire in the e2e build (eager, or a
  larger/condition trigger), and/or serve the **built** app statically instead of `ng serve`. Lower
  priority once A1 lands (NetworkIdle gone → the timer no longer breaks tests).
- **C2** Fix the axe baseline violations (UX-15 + others), then flip `A11Y_ENFORCE=1` into Test Summary.
- **C3** Local-dev convenience: a Testing-style rate-limit exemption / higher Development limit.

### Phase D — Knowledge graph (vault) — done in parallel this session
Imported the 3 repo ADRs as decision nodes; added 9 domain/feature component nodes + 3 milestone nodes
(M-001 PROC-01, M-002 home-v3, M-003 Plan-18-Phase-2); enriched `affects`/`blocks`/`relates_to` edges;
added hub Dataview queries. Graph went from coarse-layers-only to feature-queryable.

## 4. Sequencing + guardrails
A1 → A3 → (measure flake) → A4 · B1 → B2 → B3 in parallel · C gated behind unit-plans · enforce axe
(C2) only on a green, stable foundation. A test-design rule blocking **new** `NetworkIdle` usage locks
in A1.
