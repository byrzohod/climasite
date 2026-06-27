---
name: acceptance
description: Exploratory, adversarial, runtime acceptance gate -- after a unit/wave/feature is code-complete and automated tests are green, the agent MANUALLY drives the REAL running surface (ui | api | cli | library | static) like a demanding real user + QA, exercising real scenarios and edge/abuse cases against a real backend + real DB (when the product has persistent state) + real-or-sandboxed third parties, to catch what tests and static review miss: broken functionality, UI/UX problems, visual/formatting/layout breaks, responsive issues, obvious a11y issues, console/network errors, performance jank, side effects, data-integrity problems, and regressions in adjacent features. PASS = zero blocker AND zero major AND every minor/nit fixed, agent-waived-as-Knowledge-Question (nits), tracked-as-follow-up (off-critical-path minors), or human-waived (borderline minors); otherwise FAIL -> fix properly -> re-run the full scenario set until clean. This is a MANUAL exploratory gate, so it CANNOT be a merge-queue-required CI check (the queue can't compute a manual test); it is enforced PROCEDURALLY via the agent's /trunk-merge path, a committed+validated PASS report at the merged tip, the Stop-hook reminder, and the DoD. Use this whenever the user mentions acceptance testing, an acceptance gate, manually/exploratory testing the running app like a real user, dogfooding the feature, "does this actually work end-to-end", finding UI/UX/formatting/functional/side-effect bugs, or asks before merging a feature/wave/unit. Mandatory gate: no /trunk-merge of a behavior/source change without a committed PASS report whose commit matches the merged tip.
---

# /acceptance — Exploratory, adversarial, runtime acceptance gate

The **runtime, real-usage** gate. After a unit/wave/feature is **code-complete** and the **automated suite is green**, the agent stops trusting the tests and *uses the product* — driving the **real running surface** like a demanding real user crossed with a hostile QA, through real scenarios and edge/abuse cases, against the **real backend + real-or-sandboxed third parties** (and a **real DB whenever the product has persistent state**) with **realistic seed data**. The job is to catch everything that green tests and a clean static review still let through: broken functionality, UI/UX friction, visual/formatting/layout breaks, responsive failures, obvious accessibility problems, console/network errors, performance jank, unintended side effects, data-integrity problems, regressions in adjacent features, and anything plainly "unordinary".

It is **surface-agnostic**. The accepted surfaces are exactly: **`ui | api | cli | library | static`**, driven through the interface a real consumer would use:

| surface | how it's driven | real DB? |
|---|---|---|
| **ui** | the browser, via Playwright (Playwright MCP if available, else the project's Playwright) | yes, if it persists state |
| **api** | the real HTTP surface (curl/httpie/the project's client) | yes, if it persists state |
| **cli** | the real built binary/command, end to end | yes, if it persists state |
| **library** / SDK | a small **example/consumer harness** that calls the **public package API** the way a downstream user would (**NOT** unit tests) | only if the library owns persistent state |
| **static** | the **built** site (run the production build, serve it, drive the served output — not the dev server, not the source) | n/a |

A **real DB is required only when the product has persistent state**; a pure-compute library or a static site has none, so "no DB" is correct there, not a gap. It is the third leg of the "did we actually ship something that works" tripod, alongside the **automated test suite** (does the code pass its assertions) and the two skills it complements:

- **`/verify-work`** — *spec-confirmation*: a layered checklist that the feature does the thing it was built to do (build/boot, smoke, the happy user flow, persistence, break-the-code). Largely **confirmatory**.
- **`/code-review`** — *static*: reads the diff against the 13 dimensions without running anything.
- **`/acceptance`** — *exploratory + adversarial + runtime*: roams the live product hunting for what nobody specced, tried to assert, or could see in a diff.

`/acceptance` is the **runtime complement** to those — **NOT a replacement** for any of them. Run the suite, run `/verify-work`, run `/code-review`, then run **this** as the final real-usage gate before the unit enters `/trunk-merge`.

## When to use

Invoke this skill:
- **Before merging a feature / wave / unit** — as the runtime acceptance gate the merge depends on (see Gate + DoD).
- **When asked to "manually test", "exploratory test", or "dogfood" the running app** like a real user.
- **When the question is "does this actually work end-to-end?"** — not "do the tests pass" (they already do) but "is the product good when I use it".
- **To hunt UI/UX, visual/formatting, functional, side-effect, or regression bugs** that tests and static review miss.
- **After a wave/phase** — exercise the integrated surface, not just the last unit (cross-feature interactions live here).

## What this is / is NOT

| | `/acceptance` | `/verify-work` | `/code-review` | automated tests |
|---|---|---|---|---|
| **Mode** | exploratory + adversarial | confirmatory checklist | static read | static assertions |
| **Runs the app?** | yes, real stack | yes, real stack | no | yes (its own harness) |
| **Finds** | unspecced/UX/visual/abuse/regression | spec done? path real? persists? | diff-level defects | asserted behaviors |
| **Verdict** | PASS/FAIL (gate) | report (gate inside /trunk-merge) | findings | green/red |

`/acceptance` does **NOT** replace tests (it doesn't add coverage), does **NOT** replace `/verify-work` (it assumes that confirmation already passed), and does **NOT** replace `/code-review`/`/security-review` (it's runtime, not a static audit). It is the "roam the live product and try to break it like a real, hostile user" check that sits on top of all three.

## Non-negotiables (LOCKED policy)

- **REAL stack, NO mocks.** Boot the full product against a **real backend** and **real-or-sandboxed** third parties (sandbox/test keys, never prod side effects), with **realistic seed data**. **A real DB is required whenever the product has persistent state** (shared-infra dev DB or a dedicated real test DB — see `~/Projects/shared-infra/docker-compose.yml`; **never** an in-memory fake); a pure-compute library or static site has no persistent state, so "no DB" is correct, not a skipped check. A scenario that passes through a mock proves nothing — if any link in the chain is faked, say so explicitly and treat that scenario as not-verified.
- **Surface-agnostic, real interface.** Drive the real surface (`ui | api | cli | library | static`) the way a real consumer would: **ui** → the real browser (Playwright MCP if available, else the project's Playwright); **api** → the real HTTP endpoints (curl/httpie/the project's client); **cli** → the real built binary; **library/SDK** → a small **example/consumer harness** exercising the **public package API** (NOT unit tests); **static** → the **built** site (production build, served). Do not substitute a unit-test harness for the real interface.
- **Mandatory gate (enforced PROCEDURALLY, not by CI).** This is a **manual exploratory** gate, so it **cannot be a merge-queue-required check** — the queue runs automated CI and **can't compute a manual test**. It is enforced by four procedural mechanisms: (a) the agent's **`/trunk-merge`** path validates it in Step 1; (b) a **committed, validated PASS report at the merged tip** is binding evidence; (c) the **Stop-hook reminder**; (d) the **DoD**. Concretely: no `/trunk-merge` may merge a **behavior/source change** without a **committed PASS report whose `commit` (full HEAD sha) matches the tip being merged**. A stale report (different sha) is invalid — re-run. Applies to behavior/source changes; **docs/config-only changes are exempt**. Backend-only / library / static repos still run it (against their respective surface). *(Optional, non-blocking: the agent MAY post an `acceptance` commit status on the PR head — `gh api repos/<slug>/statuses/<full-sha> -f state=success -f context=acceptance` — for PR visibility/audit. This is **explicitly NOT a merge-queue gate**; the merge queue must never be configured to require it.)*
- **Fix loop, no hacks.** On FAIL, fix the issues **properly** (no band-aids, no "skip that scenario"), then **RE-RUN the full relevant scenario set** (fixes regress) until a clean PASS. Escalate to the human after **3 unresolved/recurring rounds**.
- **Uniform rigor, trunk-based, trusted-source-only, max reasoning.** Same bar for every unit; the report gates the trunk merge; tooling is first-party/vetted only (Playwright, project HTTP client, Codex CLI as read-only advisor); every agent runs at maximum reasoning.

## Prerequisites

Do not start until **all** hold (if one fails, that's an earlier skill's job — go back):
- [ ] **Automated suite is GREEN** — unit + integration + e2e all pass (this gate is for what they miss, not a substitute for them).
- [ ] **`/verify-work` done** — build/boot, smoke, happy-flow, persistence, and break-the-code confirmation already passed (this skill **consumes** that report, §Step 3).
- [ ] **The product boots on the real stack** — dev server / service / CLI / built site starts clean; **DB reachable + migrations applied if the product has persistent state**; third parties reachable in sandbox.
- [ ] **A driver is available for the surface** — **Playwright MCP** (preferred) or the **project's Playwright** for **ui**; the project's HTTP client for **api**; the built binary for **cli**; an example/consumer harness for **library**; the served build for **static**. If no Playwright is available for a UI, **degrade gracefully** (§Step 2 / Step 4): drive the UI manually and document each step, and mark driver-dependent scenarios as not-verified in the report.
- [ ] **The work is committed on the branch** — `/acceptance` runs against the **committed branch HEAD** (so the report's `commit` can pin to a real sha that the merge will carry), not an uncommitted working tree.
- [ ] **Full HEAD sha noted** — `git rev-parse HEAD` (the **full** sha, not short); the report's frontmatter `commit:` is pinned to it.

(Seed data is **not** a prerequisite — it's a Step 0 setup *action* this skill performs; see Step 0.)

## Step 0 — Setup (provision the environment; don't refuse for lack of it)

Before enumerating scenarios, **set the stage** — this skill provisions what it needs rather than refusing to start:

- **Ensure realistic seed data exists.** If the environment is bare, **create it**: accounts across the relevant **roles** (anonymous / normal / admin / expired-or-limited), representative records, and at least one of each meaningful **state** — **empty, single, many, error**. (For a product with persistent state, seed the real DB; for a library/static surface, prepare representative inputs/fixtures the consumer harness or built site will exercise.) Don't block on "seed data is missing" — provisioning it is the job here.
- **Confirm the real stack is up** for the surface (server/service/CLI/served build; DB + migrations if stateful; sandbox third parties).
- **Note the full HEAD sha** (`git rev-parse HEAD`) of the committed branch tip you're testing.

## Step 1 — Build the scenario set (think like a real user + an adversary)

Before touching the app, enumerate what you'll exercise. Cover the **whole touched surface**, not just the happy path. Aim for breadth a real user + QA would hit:

- **Happy path** — the core thing the unit/feature exists to enable, done the obvious way.
- **Key alternate flows** — the other legitimate ways to reach/complete the goal (different entry points, optional fields, shortcuts).
- **Realistic edge cases** — boundaries (0 / 1 / many / max-length), unusual-but-valid input, long names, special characters, emoji, leading/trailing whitespace, timezones, large lists.
- **Error / abuse cases** — invalid input, missing required fields, wrong types, duplicate submit, forbidden actions, malformed payloads, oversized input.
- **Multi-step stateful journeys** — flows that build state across screens/requests (wizard, cart→checkout, draft→publish); verify state at each step and at the end.
- **Cross-feature interactions / regressions** — does this change affect adjacent features that *shouldn't* have changed? Exercise the neighbors.
- **Roles / states** — anonymous, normal user, admin, expired/limited account; new vs returning; permission boundaries.
- **Viewports / devices (responsive)** — mobile (375), tablet (768), desktop (1280/1920); portrait where relevant.
- **Empty / loading / error states** — first-run/no-data, slow network (loading/skeleton), backend error (graceful failure, not a white screen).
- **Concurrency / double-submit** — rapid double-click, two tabs, submit-while-pending, racing mutations.
- **Back / refresh / deep-link** — browser back mid-flow, hard refresh, open a deep link cold, bookmark a mid-flow URL.
- **i18n (EN / BG)** — switch locale; check both languages render, no raw keys, no overflow, formats localized.
- **Quick a11y pass** — keyboard-only path through the core flow; visible focus; obvious contrast; alt text on images; labels on inputs.

**(Optional) pull a second perspective to widen coverage:** invoke **`/council`** (or run the **Codex read-only leg** directly) over the **diff** to propose scenarios/edge cases you missed. Codex **reads the diff**; it **cannot drive a browser** — it suggests, you execute. (Egress caveat in Step 5.)

Write the scenario list into the report skeleton (Step 6) before executing — it's your checklist and your audit trail.

## Step 2 — Execute against the REAL surface

Drive the live product the way a real consumer would, instrumented to catch what the eye misses. Pick the block for your surface (`ui | api | cli | library | static`).

**For a UI (Playwright MCP preferred; else project Playwright):**
- **Open the app at the real dev URL** and walk each scenario by clicking/typing/navigating — **no API shortcuts to set up state**; reach state the way a user reaches it.
- **Capture screenshots** at each meaningful step and at every viewport in the scenario set — they're the evidence for the report.
- **Watch the browser console** the whole time — collect **errors and warnings** (there should be none on happy paths).
- **Watch the network** — collect **4xx/5xx and failed requests**; an action that "looks fine" but throws a 500 underneath is a finding.
- **Verify side effects + DB after actions** — after a write, confirm the data landed correctly (query the real DB via psql/`docker exec`/the read endpoint), and read it back **through the real app/UI** (reload so it's fetched from the real DB through real code, not just sitting in component state).
- **Hit the API directly where relevant** — for actions the UI wraps, also exercise the underlying endpoint (shape, status, error path).

Useful Playwright instrumentation (see `/ui-qa` for the full snippet library — console/network capture, axe-core, horizontal-scroll, CLS, alt-text, raw-i18n-key, placeholder-content detection): wire console + `pageerror` + `response`(≥400) + `requestfailed` listeners and assert they're empty per scenario.

**For a backend service / API / CLI (no UI):**
- **Exercise the real API/CLI** end to end with the project's client (curl/httpie/the CLI binary) against the real service (and **real DB if it has persistent state**).
- **Contract check** each endpoint/command: status codes, response shape, headers, pagination, error bodies (400 not 500 on bad input; 401/403 on auth boundaries).
- **Side effects + data integrity** — confirm each call's effect is correct and read it back through the real read path; check idempotency where promised; check transactional integrity (partial failure doesn't leave half-written state).
- **Logs** — watch the service log: no unexpected WARN/ERROR, no stack traces on the happy path.

**For a library / SDK (no UI, no service):**
- **Drive the public package API through a small example/consumer harness** — a throwaway script/program that `import`s/links the *built* package the way a downstream user would and calls its public surface. **Not** unit tests — a real consumer integration.
- **Contract check** the public API: return shapes/types, error/exception behavior, edge inputs, documented invariants; confirm the published surface matches the docs/README examples (the examples should actually run).
- **Real DB only if the library owns persistent state**; a pure-compute library has none — that's correct, not a gap.

**For a static site:**
- **Run the production build, serve it, and drive the served output** (not the dev server, not the source) — via Playwright if there's interactivity, else by loading the built pages.
- Check links resolve, assets load (no 404s), built HTML/CSS render, and any client-side JS works against the *built* bundle.

**If no Playwright at all (UI/static):** degrade gracefully — perform the steps manually in a real browser and **describe each step + observation**, use **browser DevTools** for the `[driver]`-tagged scenarios where possible, and **mark the rest "not-verified (no driver)"** in the report (Step 6) — flagging that automation was unavailable so the PASS can't hide skipped checks.

## Step 3 — Scrutinize like a demanding user (the "little things")

On **every touched screen / flow**, go past "it worked" and inspect quality. This is where exploratory testing earns its keep. **Do not re-run other skills' checklists here** — `/acceptance` *consumes* their prior reports and adds only the exploratory deltas on top:

- **UI quality (visual/formatting/layout, responsive, states, obvious a11y, copy/typos/i18n, perf-feel, UI/UX) → use the `/ui-qa` checklist.** Don't re-list those ~12 categories here; run the `/ui-qa` pass (or consume its existing report) and pull through anything it flags. This skill's job on top of `/ui-qa` is the *exploratory* judgement: "does this feel right to a demanding user", not the box-tick.
- **Persistence / read-back → see `/verify-work` Layer 5.** Don't re-derive the persistence check; `/verify-work` already confirmed writes land and read back through real code against the real DB. Consume that result.

Then inspect the **genuinely exploratory deltas** `/acceptance` owns — the things no checklist enumerates:

- **Functional correctness in the wild** — the action does *exactly* what it should across the real scenario set; correct data shown; correct success/error feedback; no dead ends; no "looks done but does the wrong thing".
- **Side effects + data integrity** — only the intended data changed; nothing unintended was created/deleted/mutated; counts/totals/relationships stay consistent after a sequence of real actions (not just a single write — that's `/verify-work`'s job).
- **Regressions** — adjacent features the change *shouldn't* have touched still work — exercise the neighbors.
- **Anything "unordinary"** — anything that makes you go "huh, that's odd" even if you can't name the category yet. Chase it down; that instinct is the whole point of an exploratory gate.
- **User-surface security smells** — can you see another user's data by changing an id in the URL/request (**IDOR**)? Does a non-admin reach admin actions? Do error messages leak internals? (Deep audit is `/security-review`; this is the surface-level smell test.)

**For a backend/API/CLI/library surface (no UI):** skip the `/ui-qa` pointer; the exploratory deltas above still apply — focus on response-shape/contract correctness, idempotency, transactional integrity under sequences, and the security smells (IDOR, authz boundaries, internal-leak in error bodies). The adversarial backend set is Step 4.

## Step 4 — Adversarial: actively try to break it

Stop being a cooperative user. Attack the live product. Scenarios tagged **`[driver]`** depend on automation (a browser driver / scripting) to do faithfully — see graceful degradation below.

**For a UI:**
- **Bad / huge / empty / special-char input** — paste 10k characters, emoji, `<script>`, SQL-ish strings, leading spaces, nulls, wrong types into every field/param.
- **Rapid clicks / double-submit `[driver]`** — hammer the submit/save/buy button; open two tabs and act in both (precise double-submit timing needs a driver).
- **Navigate away mid-flow** — leave a wizard halfway, come back; does state recover or corrupt?
- **Refresh mid-action** — hard-refresh during a pending request / multi-step flow.
- **Back button** — go back after a submit; does it re-submit, show stale data, or break the SPA?
- **Expired / missing session** — let the session expire (or clear the token) and keep acting; expect a clean re-auth, not a 401 loop or a crash.
- **Wrong permissions** — as a low-privilege user, attempt high-privilege actions directly (deep-link, crafted request).
- **Offline / flaky network `[driver]`** — drop the connection mid-action; throttle to slow 3G; expect graceful handling, not a silent failure (network throttle/offline needs a driver).

**For a backend/API/CLI (and library SDKs via the consumer harness):**
- **Malformed / oversized / wrong-type payloads** — truncated/invalid JSON, 10MB bodies, wrong content-type, wrong field types, extra/unknown fields, missing required fields; expect 4xx (not 500), bounded memory.
- **Missing / duplicate idempotency keys** — omit the key on a mutation; replay the *same* key with a *different* body; expect deterministic, non-duplicating behavior.
- **Concurrent identical mutations (race / double-spend) `[driver]`** — fire the same mutation N times in parallel; expect exactly-once effect (no double charge, no duplicate row), via a unique constraint / lock / idempotency.
- **Replayed / expired / forged tokens** — re-send a captured token, an expired one, a tampered-signature one, a token for a different user/scope; expect 401/403, never acceptance.
- **Out-of-order multi-call sequences** — call step 3 before step 1; finalize before init; cancel then act; expect a clean state-machine rejection, not corruption.
- **Partial-failure transactional integrity** — induce a mid-transaction failure (kill a dependency, force a downstream error); expect all-or-nothing, no half-written state.
- **Pagination / cursor abuse** — negative/huge `limit`, out-of-range `offset`, a forged/expired/foreign cursor, page past the end; expect bounded, consistent results.
- **IDOR via id-swap** — request/mutate another tenant's or user's resource by swapping the id in the path/body; expect 403/404, never another principal's data.

Every break is a finding (severity per the schema in Step 6). The goal is to find them now, in the gate, not in production.

**Graceful degradation (no Playwright / no driver).** The **`[driver]`**-tagged attacks (network throttle/offline, precise double-submit/race) can't be done faithfully by hand. Fallback: do them via the **browser DevTools manually** where possible (DevTools Network → offline/throttle; rapid manual double-click) and record the observation. Where even that can't approximate the scenario, mark it **"not-verified (no driver)"** in the report's scenario list — and add the explicit report line **"driver-dependent scenarios not run: <list>"** (see Step 6) so a PASS can **never silently hide skipped checks**.

## Step 5 — (Optional) cross-vendor second perspective

For high-stakes units, add an independent perspective via **`/council`** (the vendor layer; see `skills/council.md`) — or run the **Codex read-only leg** directly — over **the diff + your findings**, for two things:
1. **Propose missed scenarios / edge cases** — a different model lineage catches abuse cases yours systematically skips.
2. **Sanity-check a PASS** — before you call it green, have the second leg argue why it might *not* be.

```bash
git diff main...HEAD > /tmp/acceptance-diff.txt
codex exec "You are a READ-ONLY QA adversary (advice only — you cannot run anything or change files).
Given this diff, list (a) realistic user/edge/abuse SCENARIOS a demanding QA should run against the
live app that automated tests likely miss, grouped by surface, and (b) reasons a PASS verdict might be
premature. You cannot drive a browser — propose, do not execute.

$(cat /tmp/acceptance-diff.txt)" \
  -m gpt-5.5 -c model_reasoning_effort="xhigh" \
  -s read-only -c approval_policy="never" \
  -o /tmp/codex-acceptance.md --json < /dev/null
```

**Caveats (LOCKED):** **Codex is a read-only advisor; Claude executes the scenarios and decides the verdict.** Codex **reads the diff but cannot drive a browser** — it only proposes. **Codex egresses the prompt + read code to OpenAI** (this owner accepts that; for a repo that doesn't, run Claude-only — several blind Claude subagents proposing scenarios, zero egress). Keep `.planning/council/` gitignored. Never auto-apply a Codex suggestion; fold accepted scenarios into the set and run them yourself.

## Step 6 — Report + verdict (the artifact) — and COMMIT it

Write the redacted report to **`.planning/acceptance/<id>.md`** where `<id>` is the unit/wave/feature slug, and **commit it with the work** so the gate's freshness is real (a committed report at the merged tip is the binding evidence `/trunk-merge` validates).

- **The `.md` report is COMMITTED** (with the unit's diff, before review/merge). It is text-only and redacted — no secrets, tokens, PII, or raw user data.
- **Binary evidence (screenshots / HAR / network traces / logs) goes in an IGNORED `.planning/acceptance/evidence/` subdir — never committed.** Add `.planning/acceptance/evidence/` to `.gitignore`. The report references evidence by relative path; the artifacts themselves stay local.

**Frontmatter (required):**
```yaml
---
verdict: PASS | FAIL
commit: <full HEAD sha tested>    # FULL 40-char sha (git rev-parse HEAD), must == the tip being merged
date: YYYY-MM-DD
surface: ui | api | cli | library | static
rounds: <n>                       # fix-loop rounds executed (1 = clean first pass)
---
```

**Body:**
1. **Environment** — how the product was booted; backend/DB used if stateful (shared-infra dev DB / dedicated test DB / "n/a — no persistent state"); third parties (sandbox); seed data provisioned (Step 0); driver (Playwright MCP / project Playwright / manual+DevTools / curl / CLI / consumer harness / served build).
2. **Scenario list** — every scenario from Step 1 and Step 4, each marked pass / fail / **not-verified (no driver)**, with a one-line note.
3. **Driver-dependent scenarios not run** — an explicit line listing every `[driver]` scenario marked "not-verified (no driver)" (or "none — all driver-dependent scenarios run"). **A PASS must never silently hide skipped checks** — this line makes the skip visible.
4. **Issues table** — one row per finding:

   | id | severity | category | repro steps | expected vs actual | evidence | suggested fix |
   |----|----------|----------|-------------|--------------------|----------|---------------|

   - **severity** ∈ `blocker | major | minor | nit`
   - **category** ∈ `functional | ui-ux | visual-formatting | responsive | a11y | perf | console-error | network-error | side-effect | data-integrity | regression | security`
   - **evidence** = relative path under `.planning/acceptance/evidence/` (screenshot/HAR/log) / DB query output / network trace.
5. **Verdict** — PASS or FAIL with the one-line justification, plus the **disposition lines** for every non-blocker/major finding (fixed / agent-waived-as-KQ / tracked-follow-up / human-waived — see below).

**PASS rule (LOCKED) — with an autonomous relief valve so PASS is reachable without a human round-trip on every unit:**

- **`blocker` and `major` ALWAYS block.** No waiver, no relief valve — they must be genuinely fixed (Step 7) before PASS. Full stop.
- **`nit`** — the **agent may self-waive** by logging it as a **Knowledge Question** via `/kb-capture` (no human needed). Logged = dispositioned; it no longer blocks.
- **`minor`** — **blocks only if** it is **on the changed critical path** *or* **violates a stated acceptance criterion**. Otherwise the agent may **log it as a tracked follow-up** (Knowledge Question/Risk via `/kb-capture`) and proceed. A **borderline** minor (genuinely unsure whether it's on the critical path / violates a criterion) is the one case that goes to an **explicit HUMAN waiver**.
- **Anything left undispositioned** (an open blocker/major, a critical-path/criterion-violating minor that isn't fixed, or a nit/off-path minor neither fixed nor logged) = **FAIL**.

This makes a clean PASS reachable **autonomously** for the common case (blockers/majors fixed, nits and off-path minors logged), reserving the human only for borderline minors. Every waiver/deferral is **logged via `/kb-capture`** — never silently dropped.

**Follow-ups feed the product loop:** issues you choose not to fix now (agent-waived nits, off-critical-path minors, human-waived borderline minors, "nice to have" UX, deferred polish) become **Knowledge Questions/Risks** via `/kb-capture` — so they re-enter planning, not the void.

## Step 7 — The mandatory fix loop

On **FAIL**:
1. **Fix the issues properly** — root-cause, no hacks, no "comment out the failing scenario", no lowering the bar. A blocker/major must be genuinely fixed (no waiver path); a minor/nit is fixed or **dispositioned per the relief valve** (nit → agent-waived as Knowledge Question; off-critical-path minor → tracked follow-up; borderline minor → human waiver) — always logged via `/kb-capture`.
2. **RE-RUN the full relevant scenario set** — not just the failed scenario. Fixes cause regressions; a fix that breaks a neighbor is a net loss. For a small isolated fix, "relevant set" = the touched flow + its neighbors; for anything cross-cutting, re-run everything.
3. **Repeat until a clean PASS** — increment `rounds` in the frontmatter; append each round's deltas to the report (what was found, what was fixed, what the re-run showed).
4. **Escalate to the human after 3 unresolved/recurring rounds** — if the same class of issue keeps recurring after three fix→re-run cycles, the design or plan is likely wrong. Stop brute-forcing; surface to the human with the round history and a recommendation (re-plan / re-design / accept-with-tickets). Do **not** silently keep looping.

Document **every round**. The final report reflects the last (passing) run, with the round history preserved.

## Gate + Definition of Done

This gate is **procedural, not a CI check** — a manual exploratory test can't be computed by the merge queue, so the queue is **never** configured to require it. Enforcement is the four mechanisms named in the Non-negotiables: the agent's `/trunk-merge` path, the committed+validated PASS report at the merged tip, the Stop-hook reminder, and the DoD below.

- **No `/trunk-merge` without a COMMITTED, VALIDATED PASS report at the merged tip.** Before a unit enters the merge queue, there must be a **committed** `.planning/acceptance/<id>.md` with `verdict: PASS` and a `commit:` (**full** sha) that **equals `git rev-parse HEAD` of the branch tip** being merged, with **zero open blocker/major issues**. `/trunk-merge` Step 1 parses and validates this (not a loose grep) — if missing / `FAIL` / stale, it **BLOCKS** and runs `/acceptance` + the fix loop.
- **Sequencing (so the report represents the FINAL diff):** branch → **commit the code** → run automated gates + `/verify-work` + `/acceptance` against that **committed branch HEAD** → validate the report → then queue. `/acceptance` runs *after* the commit exists, never before. **Re-run `/acceptance` after ANY post-review fix commit** — the report's `commit:` must match the FINAL tip, or it's stale.
- **Record the commit.** The report's `commit:` is the proof-of-freshness. If the branch advances after the report was written (any new behavior/source commit, including a review fix), the report is **stale → re-run** before merging.
- **Scope:** applies to **behavior/source changes** (runtime changes), **not docs-only/config-only changes** (no behavior to exercise). Backend-only / library / static repos are **not** exempt — run it against their surface.
- This gate composes with `/trunk-merge` Step 1 (preflight): alongside `/mutation-check` PASS and `/verify-work` PASS, require a **validated `/acceptance` PASS report at the committed HEAD**.
- **(Optional, non-blocking)** the agent may mirror the verdict as an `acceptance` commit status on the PR head (`gh api repos/<slug>/statuses/<full-sha> -f state=success -f context=acceptance`) for PR visibility/audit — **never** a merge-queue-required check.

## Composition

- Run **after** the suite is green, **after** `/verify-work`, and **after / alongside** `/code-review`; run **before** `/trunk-merge`.
- Pull **`/ui-qa`** for the exhaustive UI checklist + Playwright snippet library when the touched surface is UI: Step 3 **consumes `/ui-qa`'s report** for the box-tick categories (visual/responsive/states/a11y/copy-i18n/perf-feel) rather than re-listing them; this skill adds the exploratory deltas on top.
- Escalate a UI finding to **`/accessibility-audit`** (deep WCAG 2.2 AA) or **`/perf-budget`** (LCP/INP/CLS/p95 enforcement) when the quick pass surfaces something that needs the deep tool.
- Escalate a security smell to **`/security-review`** (OWASP) — this skill's Step 3 security row is a surface smell test, not the audit.
- Log waivers and deferred polish as **Knowledge Questions/Risks** via **`/kb-capture`** so they re-enter the product loop.
- Optionally widen scenario coverage / sanity-check a PASS via **`/council`** (Codex read-only advisor, Step 5).

## What this skill does NOT do

- **Add test coverage** — it doesn't write or fix tests (that's `/test-plan` / `/mutation-check`); it exercises what's already built.
- **Replace `/verify-work`** — it assumes that confirmatory checklist already passed; this is the exploratory/adversarial layer on top.
- **Replace `/code-review` / `/security-review`** — it's runtime, not a static diff audit; it surfaces security *smells*, not a full OWASP pass.
- **Replace the merge-queue full suite** — the queue's real-infra suite is still the server-side merge gate (`/trunk-merge` Step 7); this gate runs *before* the queue.
- **Drive a browser from Codex** — Codex only proposes scenarios / sanity-checks; Claude executes and decides.

## See also

- `/verify-work` — the confirmatory spec-check this complements (build/boot, smoke, happy flow, persistence, break-the-code).
- `/ui-qa` — the exhaustive UI QA checklist + Playwright automation snippets (console/network/axe/CLS/i18n/alt/placeholder).
- `/council` — optional cross-vendor second perspective (Codex read-only advisor) to widen scenarios + sanity-check a PASS.
- `/code-review` — the static channel/angle review this runs alongside, not instead of.
- `/accessibility-audit` — deep WCAG 2.2 AA pass when the quick a11y check surfaces real issues.
- `/perf-budget` — LCP/INP/CLS/p95/bundle budgets in CI when perf-feel findings need enforcement.
- `/security-review` — OWASP audit the security-smell row defers to.
- `/kb-capture` — log waivers / deferred polish as Knowledge Questions/Risks (the product loop).
- `/trunk-merge` — the merge loop this gate blocks: no merge of a behavior/source change without a PASS report at the merged tip.
- [[../Agent Workflow]] — §3 (trunk-based delivery + the gates a unit clears before the queue), §6 (review channels/angles), §9.4 (Definition of Done), §12 (max-reasoning model policy).
