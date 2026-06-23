---
name: verify-work
description: Manual feature verification across 7 layers -- build/boot, API smoke test, UI smoke test, full user flow walkthrough (asserting the path has NO mocks), state persistence read back through the real app/UI against the real DB, cross-cutting regression check, and a break-the-code confirmation (inject a bug, confirm targeted tests fail, revert, attach evidence). Use this whenever the user mentions verifying a feature, manual testing, smoke test, sanity check, "does it work", before opening a PR, after implementing any feature to confirm it actually behaves correctly (tests passing != feature working), or before marking work done.
---

# /verify-work - Manual verification of features

## When to use

Invoke this skill:
- **After implementing every feature** -- before marking it done
- **Before opening a PR** -- to catch what tests miss
- **After merging dependency updates** -- to catch silent breakages
- **As part of release verification** -- before tagging

Tests prove code correctness. This skill proves *feature* correctness -- the actual behavior a user would experience. They overlap but are not the same: a test suite can be 100% green while the feature is broken (wrong copy, broken UX flow, missing data, console errors, dev server not even starting).

This is the standalone manual-verification skill — use it for any feature or PR. It complements the verification stages built into `/feature` and `/implement`, and the role-based verifier agent (see `Agent Workflow.md` §10.5).

## Verification Layers

Verification is layered. Skip a layer only if it doesn't apply (e.g., no UI = skip UI layer).

### Layer 1: Build & Boot

The agent must successfully:

- [ ] **Install dependencies** (`npm install`, `dotnet restore`, `pip install -r requirements.txt`, `go mod download`, etc.) -- no errors, no warnings about peer deps
- [ ] **Type-check** (`tsc --noEmit`, `mypy`, `go vet`, etc.) -- zero errors
- [ ] **Lint** (`eslint`, `ruff`, `golangci-lint`, etc.) -- zero errors, warnings reviewed
- [ ] **Build** the production artifact (`npm run build`, `dotnet build -c Release`, `cargo build --release`, etc.) -- succeeds with no errors
- [ ] **Start the dev server** -- runs without crashing, no startup errors in logs
- [ ] **Start dependent services** -- DB, cache, queue all reachable from the app
- [ ] **Run database migrations** -- apply cleanly, no errors

If any of these fail, the feature is not done. Fix before continuing.

### Layer 2: API Smoke Test (if backend exists)

Hit the actual endpoints the feature touches:

- [ ] **Authentication endpoints** -- log in, get a valid token
- [ ] **Each new endpoint** -- happy path returns expected shape and status
- [ ] **Each modified endpoint** -- still returns what it used to plus the new behavior
- [ ] **Error responses** -- malformed input returns 400 with helpful message, not 500
- [ ] **Auth-protected endpoints** -- return 401/403 for unauthenticated/unauthorized requests
- [ ] **Health check** -- `/health` (or equivalent) returns 200 and structured status
- [ ] **No 500s** in the dev server log during smoke test

Use `curl`, the project's HTTP client (e.g., `httpie`, `bruno`, `bombardier`), or direct browser network tab to verify.

### Layer 3: UI Smoke Test (if UI exists)

Use Playwright MCP for visual verification. Do NOT rely on screenshots alone -- run actual interactions.

- [ ] **Open the app in a browser** (Playwright MCP) at the dev URL
- [ ] **Navigate to every page** the feature touches
- [ ] **Take screenshots** at desktop (1280×720) and mobile (375×667)
- [ ] **Capture console errors** -- there should be none
- [ ] **Capture network errors** (4xx, 5xx) -- there should be none on happy paths
- [ ] **Click every new button / link / control** the feature added
- [ ] **Submit every new form** with valid input -- success state shows
- [ ] **Submit every new form** with invalid input -- error state shows, error is clear
- [ ] **Verify no raw translation keys** (`user.login.button`) leak into UI
- [ ] **Verify no placeholder content** (lorem ipsum, TODO, undefined, [object Object])

For deeper UI checks, run `/ui-qa` after this passes.

### Layer 4: User Flow Test (end-to-end)

Walk through the full user journey for the feature, as a real user would:

- [ ] **Sign up / log in** through the actual UI (no API shortcuts)
- [ ] **Reach the new feature** by clicking the natural navigation path (not a deep-link)
- [ ] **Complete the feature's core flow** -- the thing the feature was built to enable
- [ ] **Verify the result** -- did the action persist? Is it visible after a page refresh?
- [ ] **Try the unhappy paths** -- network failure, validation error, permission denial
- [ ] **Log out** and verify the feature is appropriately gated
- [ ] **Confirm the asserted path contains NO mocks** -- the flow you just exercised hit the real app, real handlers, and real dependencies end to end. No mock server, no stubbed client, no `MSW`/`nock`/`WireMock`/`responses` interceptor, no in-memory fake DB, no feature-flag bypass, no hard-coded fixture standing in for a live call. If any link in the chain is faked, say so explicitly -- a green flow through a mock proves nothing about the real feature.

If you cannot describe what a user would do without saying "I'll use the API to set up state" -- you're not testing the feature, you're testing the API. Go through the UI.

### Layer 5: State Persistence

After everything above:

- [ ] **Restart the dev server** and verify the test data still exists
- [ ] **Restart the database container** and verify the schema and data survive
- [ ] **Clear browser cache** and verify the feature still works for a fresh session
- [ ] **Check database state directly** (via psql, `docker exec`, or equivalent) -- the data shape matches expectations
- [ ] **Assert real persistence by reading it back THROUGH the real app/UI against the real DB** -- after writing the data via the feature, read it back the way a user would: reload the page (or call the real read endpoint) so the value is fetched *from the real database through the real app code*, and confirm it matches what you wrote. The write and the read-back must both traverse the real stack against the real DB -- not an in-memory store, not a cached client-side copy, not a fixture. A value that only shows up because it is still in browser/component state, or only appears in a direct DB row but never surfaces back through the app, is NOT verified persistence.

### Layer 6: Cross-Cutting Verification

- [ ] **Existing tests still pass** (`npm test`, `dotnet test`, `pytest`, `go test ./...`)
- [ ] **No regression** in adjacent features -- click around in features the change *shouldn't* have affected, verify they still work
- [ ] **Logs are clean** -- INFO/DEBUG only, no unexpected WARN/ERROR
- [ ] **Performance acceptable** -- the feature responds within reasonable time (no 5-second freezes, no infinite spinners)

### Layer 7: Break-the-Code Confirmation

Green tests only prove something if they can also go red. A test that passes whether or not the feature works is worthless -- and far more common than you'd think (assertion on the wrong value, mocked-away under test, tautological `expect(true)`, test never actually reaching the code path). Before trusting the suite, prove the tests are wired to the behavior they claim to cover:

- [ ] **Identify the targeted tests** -- the specific test(s) that are supposed to guard this feature's core behavior
- [ ] **Inject a representative bug** into the production code those tests cover -- a real, plausible defect that breaks the behavior under test (e.g., flip a comparison, return early, drop the persistence write, off-by-one a boundary, skip the validation). Not a syntax error or a `throw` -- a defect a careless edit could actually introduce
- [ ] **Re-run the targeted tests and confirm they FAIL** -- and fail for the right reason (the assertion on the broken behavior, not an unrelated crash). If the tests still pass with the bug in place, the tests do not actually guard the feature -- fix the tests before trusting them
- [ ] **Revert the injected bug** -- restore the code exactly (`git diff` must be empty / `git checkout` the file), then re-run and confirm the tests are GREEN again
- [ ] **Attach evidence** -- in the report, record what bug was injected, which tests went red (with the failing assertion), and confirmation that revert restored green. Paste the relevant failing-then-passing test output

This is the cheapest insurance against a falsely-green suite. Do it for the feature's critical-path tests at minimum; for high-risk changes, do it for each meaningful behavior.

## Reporting

After verification, the agent must report explicitly:

```
Verification report for <feature name>:

Layer 1 (Build & Boot): ✓ all green
Layer 2 (API Smoke): ✓ 4 endpoints verified
Layer 3 (UI Smoke): ✓ 3 pages, 0 console errors, screenshots saved
Layer 4 (User Flow): ✓ signup -> create item -> edit -> delete -> verify gone; asserted path has NO mocks (real handlers + real DB)
Layer 5 (Persistence): ✓ data survives server restart; read back through real UI -> real DB and matched
Layer 6 (Cross-cutting): ✓ existing tests pass, adjacent features unaffected
Layer 7 (Break-the-code): ✓ injected <bug> into <file> -> <test(s)> went RED on <assertion>; reverted -> GREEN again

Break-the-code evidence:
- Injected: [the representative bug, e.g. "dropped the INSERT in createItem()"]
- Tests that failed: [test name(s)] -- failing assertion: [paste]
- After revert: [clean git diff confirmed] -- tests GREEN

Issues found and fixed during verification:
- [issue and fix]
- [issue and fix]

Outstanding issues:
- [none / list]
```

Never claim "feature done" without running verification. Tests passing != feature working.

## When the agent cannot verify

Some environments make full verification impossible (no dev server access, no Playwright in headless CI, no database in the agent's sandbox). In those cases:

- **State explicitly what was verified and what was not** -- never claim verification you didn't do
- **Document the gap** in the PR description
- **Ask the user** to verify the parts you couldn't reach

The principle: the agent's report must reflect what *actually* happened, not what the agent intended to happen. Better to say "I verified A and B but couldn't reach C; please confirm C" than to claim a clean verification that wasn't performed.

## Integration with other skills

- After `/verify-work` passes -> run `/ui-qa` for the deeper UI QA pass
- After `/verify-work` passes -> run `/code-review` for the multi-channel review
- After `/verify-work` passes -> run `/security-review` if the feature touches auth, validation, or user data
- For releases -> `/verify-work` before `/release` and `/deploy-checklist`

## What this skill does NOT do

- Replace tests (it's verification, not coverage)
- Catch subtle correctness bugs -- those are the test suite's job
- Replace `/ui-qa` (which is more thorough on UI)
- Replace `/security-review` (which is more thorough on security)

This is the "did you actually try the thing" check. The other skills go deeper in their respective areas.
