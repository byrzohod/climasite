---
name: test-plan
description: The per-unit test plan authored INSIDE every unit-plan.md, BEFORE that unit's code. Enumerate test levels (unit/integration/e2e/UI/perf/a11y as applicable), the EXACT assertions, the edge-case matrix, real-infra setup (testcontainers, no mocks), the three test buckets (own-it=real; third-party-you-don't-own=WireMock+contract test; non-deterministic/costly external like LLM or payment=vendor sandbox + record-replay + nightly real smoke), the mutation target (>=75% killed on changed files), and EXPLICITLY how false positives are prevented (the break-the-code self-check). Use this whenever a unit-plan.md is being written, the user mentions a test plan/test strategy/what to test/how to test, asks to plan tests or define assertions/edge cases/coverage before coding, or before any unit's code (the no-spec-no-code hook blocks src/** until the plan exists). Authored by the test-strategist agent (read-only: it designs the plan, it does not write the tests).
---

# /test-plan - The per-unit test plan, written before the code

A **design artifact, not test code.** This skill produces the `## Test plan` section that lives **inside `unit-plan.md`** (Gate 3 of `/plan-tree`) — written **before** the unit's code, because it is half the contract the `no-spec-no-code` hook checks (the other half is the verification plan). It enumerates, for this one unit: the test **levels** that apply, the **exact assertions**, the **edge-case matrix**, the **real-infra setup**, which dependencies fall in which of the **three buckets**, the **mutation target**, and — the clause that makes the whole thing honest — **exactly how false positives are prevented**. See Blueprint §I and `Agent Workflow.md` §4.

**Authored by the `test-strategist` agent, which is read-only (`Read`/`Grep`/`Glob`).** That is deliberate: the strategist *designs* the plan and *designs the break-the-code bugs*; it never writes the tests. Writing the tests is the `developer`'s job (with the feature, §4.0) and auditing/extending them is the `qa` agent's job. Separating design from authorship is what stops "the test was written to pass" — the person who breaks the code is not the person who wrote the test.

**Why before the code:** a test plan written after the code is a description of what the code happens to do, not a specification of what it must do. Planning the assertions and the edge-case matrix first is how you discover missing requirements while they are still cheap. Quality rigour is **uniform across every project** — there is no "it's just a weekend project" test-plan-lite.

## When to use

Invoke this skill:
- **Inside every `unit-plan.md`, before that unit's code** — this is the mandatory home; the hook blocks `src/**` until the plan exists and is approved.
- **When the user asks** to plan tests, define a test strategy, decide what/how to test, write assertions, build an edge-case matrix, or set a coverage/mutation target — before implementation.
- **When a unit's interfaces or acceptance criteria change** mid-build — re-derive the assertions and matrix from the new contract; do not patch tests to match drifted code.
- **As the test-design input to `/design-doc`'s system-level "test + verify strategy"** — the design doc states the *system* no-mock contract; this skill instantiates it per unit.

## When NOT to use

- **To write the actual tests** — this skill outputs a *plan*. The `developer` writes tests with the code; `qa` audits and extends. The `test-strategist` stays read-only.
- **To run mutation testing** — that is `/mutation-check` (the executor + the >=75% gate). This skill *sets the target and names the survivors to expect*; it does not run the tool.
- **Manual feature verification** — that is `/verify-work`. The unit-plan's *verification plan* (sibling section) declares how `/verify-work` confirms real behavior; this skill is the *automated*-coverage half.
- **Trivial / throwaway changes** — covered by `/plan-tree`'s documented escape hatch (`ALLOW_EXPLORATORY=1`). Use sparingly; it gets abused if it becomes the default.
- **System-wide architecture** — that is `/design-doc` (Gate 1). This skill plans one unit against a ratified `DESIGN.md`.

## Read first (always)

Before designing the plan, the `test-strategist` reads (read-only):
- **The unit's interfaces + acceptance criteria** (this same `unit-plan.md`) — assertions are derived from the contract, not from the implementation.
- **`.planning/design/DESIGN.md`** §"Test + verify strategy" — the system-level no-mock contract this unit must honor.
- **`Knowledge/<Project>/Risks/`** — every recorded risk that touches this unit becomes a required edge case (the matrix is risk-seeded, not boilerplate).
- **`Knowledge/<Project>/` data-classification (`DATA.md`)** — any PII/secret the unit handles becomes a "must-never-leak" assertion (no secrets in logs/errors/responses).
- **The existing test suite + infra** — what testcontainers/Playwright/fixtures already exist, so the plan reuses rather than re-invents.

## Process

### Step 1 — Select the levels that apply (and justify the skips)

From the unit's contract, mark each level **applies / N/A**, with a one-line reason for every N/A (a skip must be *because the concern is absent*, never because the test is inconvenient — §4 rigour is uniform):

| Level | Plan it when the unit has… | Tooling (per stack) | Mocking allowed |
|-------|----------------------------|---------------------|-----------------|
| **Unit** | any logic, branch, calculation, parser, validator | Jest/Vitest · pytest · `go test` · xUnit | external deps only (bucket 2/3) |
| **Integration** | a DB query, an API route, a queue/cache interaction | Supertest/pytest/testing-library + **testcontainers** | **no DB mock** — real disposable DB |
| **E2E** | a user-reachable workflow spanning the stack | Playwright | **none** — full real stack |
| **UI** | any rendered screen/component/form | Playwright (MCP for visual) | **zero** — real services, data made through the UI |
| **Performance** | a latency/throughput/bundle-sensitive path | k6 · Lighthouse CI | n/a — see `/perf-budget` |
| **Accessibility** | any UI surface | axe-core + Playwright · Lighthouse | n/a — see `/accessibility-audit` |

Rule: **a UI unit without UI tests is incomplete, not optimized.** a11y + perf budgets are noted here up front (shifted left, DoR §2.6), not deferred to CI.

### Step 2 — Write the EXACT assertions (per level, per behavior)

For every applicable level, list the concrete tests. Each entry is **given / when / then**, and the *then* is a **specific, observable assertion** — not "it works", not "returns a value":

- **Bad:** `expect(result).toBeDefined()` · `expect(res.status).toBeTruthy()` · "the user is created".
- **Good:** `POST /users {email:"a@b.c"}` -> `201`, body `{id: <uuid>, email:"a@b.c"}`, **and** a row exists in `users` read back **through the app**, **and** the response contains no `password`/`passwordHash` field.

Per assertion, name the exact: **status code · response shape (field by field) · persisted state (read back through the app, never via a raw DB peek as the primary assertion) · emitted event/log · error code + user-facing message**. State the **expected value**, not just the asserted key. Integration/UI assertions assert persistence by **reading it back through the application**, not by querying the DB directly — that is what proves the *feature*, not just the *write*.

### Step 3 — Build the edge-case matrix

Enumerate edge cases explicitly — a table the implementer fills with tests. Walk the unit's inputs/states against this playbook (from `qa.md`); keep only the rows that are *reachable* for this unit, and **add every risk from `Knowledge/Risks/` as its own row**:

- **Boundary values:** 0, 1, max, max+1, negative, very large.
- **Empty / null:** empty string, empty array, `null`, `undefined`, missing field.
- **Type / injection variants:** unicode (multi-byte), emoji, RTL, HTML/JS (XSS), SQL/NoSQL injection, path traversal — assert they are *neutralized*, not just rejected.
- **Time:** epoch, far-future, leap year, DST transition, mixed timezones.
- **Numerical:** float precision, currency rounding, very small / very large.
- **Concurrency:** two simultaneous requests, race on shared state, double-submit, lock contention.
- **Failure modes:** dependency timeout, 5xx, partial/slow response, network error — assert graceful degradation, not a 500.
- **Permissions:** anonymous, wrong user (**IDOR**), expired/revoked token — assert 401/403, never silent data leak.
- **State:** cold start, warm/full/stale cache, missing dependency.

Each matrix row maps to **the level it is tested at** (boundary/null -> unit; IDOR/permission + concurrency -> integration; the user-visible error states -> UI). Map every acceptance criterion to at least one row, and every "must-never-leak" data-classification item to an assertion.

**Optional — cross-vendor adversarial edge-cases (`/council`):** for a high-stakes unit only, run a one-shot `/council` pass where Codex (read-only advisor) proposes failure modes / blind spots the Claude lineage may systematically miss; the `test-strategist` folds the **non-duplicate** rows into this matrix (de-dup against what's already here, keep only the reachable ones). Opt-in, cheap, high-signal — a single blind pass, not per-unit by default. Codex stays read-only and never authors a test; Claude remains the sole orchestrator that decides which rows to adopt. (Egress applies — for a repo where that's unacceptable, degrade to Claude-only multi-run; see `/council`.)

### Step 4 — Specify the real-infra setup (no mocks for what you own)

State exactly what runs for real and how it is provisioned:

- **Testcontainers for every owned backing service** — DB, cache, queue, search — spun up on a random port, migrations applied, destroyed after the run. **Never an in-memory fake** for the DB (it behaves differently from the real engine); **never the shared dev infra** (`~/Projects/shared-infra`) — that is for local dev, integration tests use isolated disposable containers.
- **Data is created through the UI / public API, never seeded** into the DB. If the plan says "fixture inserts X into the DB," it is wrong — rewrite it to create X the way a user would. No deep-linking past auth; tests sign up + log in through the real flow.
- **Name the containers, the migration step, the seed-through-the-UI flow, and teardown** so the implementer wires the exact harness the plan assumes.

### Step 5 — Classify every dependency into one of the THREE buckets

For each external thing the unit touches, assign a bucket and state how it is handled. **Everything not in bucket 2 or 3 is real; mocks live only in *unit* tests.**

1. **You own it -> real, never mocked.** Your DB, auth, services, search, queues run real via testcontainers (Step 4). *Plan entry: which container, which migration, data made through the UI.*
2. **A third-party process you don't own -> a narrow stub, contract-tested.** **WireMock-as-testcontainer** standing in for the vendor's HTTP API, **plus a contract test** so the stub can't drift into fiction. *Hobby default: schema-snapshot contract; a Pact broker is production-tier only.* *Plan entry: the WireMock stub + the contract test that pins it to the real schema.*
3. **A non-deterministic or costly external (LLM, payment) -> vendor sandbox + record-replay + nightly real smoke.** Three places, deliberately:
   - **Integration:** the **vendor sandbox / test-mode** (Stripe test keys, the provider's test endpoint) — real protocol, no production cost.
   - **UI/e2e:** **record-replay** — recorded fixtures replay the call; **no live model/payment call per UI run** (cost + nondeterminism would make the suite flaky and expensive).
   - **Nightly (non-blocking):** a **separate real-sandbox smoke** hits the live sandbox so the recordings can't silently rot. *Plan entry: the sandbox config, the recording location + how recordings are refreshed, and the nightly smoke job.* Without this bucket, LLM/payment features (AI Sonar, Sports Coach VLM, any Stripe flow) have nowhere honest to be tested.

### Step 6 — Set the mutation target + the false-positive prevention (the load-bearing clause)

This is the section that distinguishes a real test plan from a coverage-theater one. Coverage and a green suite prove tests *ran*, not that they *mean* anything. Plan both safeguards:

- **Mutation target:** **diff-scoped mutation testing must kill >=75% of mutants on the changed files**, enforced at the PR stage (one fixed number, not a range) — executed by `/mutation-check` (Stryker for JS/TS & .NET, mutmut/cosmic-ray for Python, cargo-mutants for Rust, go-mutesting for Go). The plan **names the mutation operators most likely to survive** for this unit (e.g., a flipped boundary `<`/`<=`, a removed null-guard, a negated condition) so the implementer writes the assertion that kills each one. Full-module mutation runs nightly, non-blocking.
- **The break-the-code self-check (mandatory, the explicit false-positive prevention):** for **each critical assertion**, the plan pre-declares a **representative bug** to inject (the §4.5 / Blueprint §I.3 self-check). Before the unit is "done": inject that bug -> confirm the targeted test(s) **FAIL** -> revert -> **record the evidence** in the unit's done-report. *A test that still passes when the code is deliberately broken is worse than no test — it is a false positive, and this step is how it gets caught before merge.* The plan literally lists: *"break X this way -> test Y must go red."* Zero new dependencies; runs in seconds. The `verifier`/`claim-verifier` re-runs this on any disputed claim — it audits, it never fixes.
- **Assertion-density / test-smell gate:** the plan flags any level whose tests risk zero-assertion or `toBeDefined`-only smells; the fast pre-mutation lint fails those files (first-party linters only).
- **Property-based testing (recommend where it pays):** for pure logic — parsers, validators, money/date — name the **invariants** (e.g., "round-trip parse/serialize is identity," "total never negative") for fast-check / Hypothesis / jqwik. Require *meaningful* properties; an always-true property is another false positive.

### Step 7 — Emit the plan into `unit-plan.md` and hand off

Write the section into `unit-plan.md` under `## Test plan` using the template below, then stop. The `test-strategist` does **not** proceed to write tests — it returns the plan to `/plan-tree`'s Gate 3 consolidation, where it is adjudicated alongside the rest of the unit plan and approved before any code. After approval, the `developer` implements the tests-with-the-code against this exact plan; `qa` audits; `/mutation-check` + the break-the-code evidence close the loop.

## The `## Test plan` block (what lands in unit-plan.md)

```
## Test plan  (/test-plan -- author: test-strategist)

Levels:        unit ✓ · integration ✓ · e2e ✓ · UI ✓ · perf <✓/N-A: reason> · a11y <✓/N-A: reason>

Assertions (exact, given/when/then):
- [unit]  <input> -> <exact return / thrown error code>
- [integ] <request> -> <status>, <field-by-field body>, persisted row read back THROUGH the app, <no-leak fields>
- [UI]    <action through real UI> -> <visible state>, 0 console errors, error-state copy = "<exact>"

Edge-case matrix (row -> level; every Risk + every must-never-leak included):
| case | input/state | level | expected |
| ...  | ...         | ...   | ...      |

Real-infra: testcontainers[<db>,<cache>,<queue>] · migrations <step> · data made THROUGH the UI · teardown <step> · (NOT shared dev infra)

Buckets:
- own (real):        <db/auth/services> via testcontainers
- 3rd-party (stub):  <vendor> -> WireMock container + contract test (<schema-snapshot | Pact>)
- nondeterministic:  <LLM/payment> -> sandbox(integration) + record-replay(UI) + nightly real-sandbox smoke

Meaningfulness:
- mutation: diff-scoped >=75% killed (changed files); expected survivors to kill: <op1>, <op2>  [/mutation-check]
- break-the-code: <break X this way> -> <test Y> MUST fail (revert + record evidence)  [mandatory]
- property-based (where it pays): invariant "<...>"  | assertion-density: no zero-assert / toBeDefined-only files
```

## See also

- `/plan-tree` — Gate 3; this skill writes the `## Test plan` section inside the `unit-plan.md` it produces, before the unit's code.
- `/mutation-check` — runs the diff-scoped mutation suite and enforces the >=75%-killed gate this plan targets (the executor; this skill sets the target).
- `/verify-work` — the *verification plan* (sibling section of `unit-plan.md`) declares how this confirms real behavior through real DB/UI; this skill is the automated-coverage half.
- `/design-doc` — Gate 1; its system-level "Test + verify strategy" (the no-mock contract) is what this skill instantiates per unit.
- `/council` — optional cross-vendor pass (Step 3) where Codex (read-only) surfaces adversarial edge-cases the Claude lineage may miss; high-stakes units only, opt-in.
- `/data-classify` — supplies the PII/secret list that becomes the "must-never-leak" assertions in Step 3.
- `agents/test-strategist.md` — the read-only author of this plan (designs the plan + the break-the-code bugs; does not write tests).
- `agents/qa.md`, `agents/developer.md` — `developer` writes the tests with the code; `qa` audits and extends; `verifier`/`claim-verifier` re-run the break-the-code check on disputed claims.
- `Agent Workflow.md` §4 (Testing Strategy: §4.1 pyramid, §4.2 rules, §4.5 reality safeguards, §4.6 three buckets), §2.6 (DoR).
- Blueprint §I (Testing + Verification Standard: §I.1 coverage gate, §I.2 three buckets, §I.3 reality safeguards, §I.4 verify-work).
