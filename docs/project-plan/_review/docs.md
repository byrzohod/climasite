# Documentation Audit — Review Findings (2026-06-11)

## Summary

ClimaSite's documentation is unusually extensive for a solo project (CLAUDE.md, 6 AGENTS.md files, 20+ plans, 19 validation docs, ADRs, CHANGELOG, audits), and the newer artifacts (Plan 18, the 2026-04-08 gap report, CHANGELOG, ADR index, the 07-wishlist validation refresh) are genuinely good. However, the corpus has split into two generations: a January-2026 layer (validation suite, plans 19/20/21-series, AGENTS.md snapshots, tech-stack.md) that is now substantially stale, and an April–June 2026 layer (Plan 18, gap report, PROJECT_MEMORY) that is current — and nothing marks which layer is authoritative.

The most damaging concrete defect is that all three command references (CLAUDE.md, root AGENTS.md, tests/ClimaSite.E2E/AGENTS.md) document a TypeScript Playwright E2E workflow (`npx playwright test`, fixtures/test-data-factory.ts, port 5000) that has never matched the actual C# Playwright-for-.NET suite run via `dotnet test` against an API on port 5029 — a new developer following the docs cannot run E2E tests or reach the API/Swagger.

Status tracking is contradictory across at least four sources: 00-master-overview says Notifications is "Complete" while CLAUDE.md says "Partial" and Plan 18 lists open NOT-* work; the master index links to two plan files (15, 16) that do not exist and omits plans 18–22 entirely. The 20-issue-registry still claims 129 open / 0 fixed even though several of its CRITICAL items were verified as fixed in code, and it includes issues against the deleted v2 home page; no active plan owns it. Plan-level checkboxes are dead (Plan 12: 0/163 checked despite implemented code; "complete" plans 21H/21I have 111/213 unchecked boxes), so real status lives only in the CLAUDE.md table and .codex/PROJECT_MEMORY.md. There are numbering collisions (two Plan 19s, four Plan 21s) and three competing redesign master plans whose design philosophies contradict each other (Plan 19's "more motion" vs 21F's "Nordic Tech" restraint that the project actually adopted).

There is no README.md at the repo root, no human onboarding guide, no deployment runbook for the Railway/Docker artifacts, and only 2 ADRs (both home-v3) despite Plan 18 mandating ADR-per-decision and targeting ≥8.

The fix is mostly archival and consolidation, not rewriting: correct the two command/port defects immediately, archive the superseded January layer with banners, and establish docs/project-plan/ plus a single source-of-truth rule per topic.

## Findings

### 1. 00-master-overview plan index contradicts other status sources and links to two non-existent plan files

- **Finding:** The self-described master plan index (docs/plans/00-master-overview.md) reports wrong statuses for Notifications and Plan 17, links to two plan files that do not exist, omits plans 18–22, and makes "100% Complete" claims contradicted by the gap report.
- **Category:** doc accuracy / broken links
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** docs/plans/00-master-overview.md:183 marks Notifications (Plan 12) "Complete" while CLAUDE.md status table says "Partial" and Plan 18 Phase 2 (18-project-completion.md:36) lists remaining NOT-* tasks as open. Line 187 marks Plan 17 "Backlog" while 17-future-enhancements.md's own status table shows all 5 phases "✅ COMPLETE" and CLAUDE.md says Complete. Lines 185-186 link `15-ui-ux-enhancements-phase2.md` and `16-e2e-testing-comprehensive.md` — neither exists in docs/plans/ nor docs/plans/archive/ (verified by ls). The index omits plans 18-22 entirely. Lines 1136-1148 declare "Frontend Implementation: 100% Complete / Backend Implementation: 100% Complete", contradicted by the 2026-04-08 gap report scorecard (docs/audit/2026-04-08-gap-report.md, e.g. integration tests 18% of controllers).
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/plans/00-master-overview.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/17-future-enhancements.md; /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md
- **Why it matters:** 00-master-overview presents itself as the master index. A developer (or agent) consulting it gets the wrong answer about what is finished (Notifications) and follows dead links; the "100% Complete" section actively contradicts the gap report that drives Plan 18.
- **Recommended fix:** Rewrite §4 Feature Plans Index to list only files that exist, with statuses sourced from Plan 18 / CLAUDE.md; add rows for plans 18-22; delete or date-stamp the "100% Complete" implementation summary; alternatively demote 00-master-overview to ARCHIVE (historical inception plan) and move the live index into the new docs/project-plan/ hub. (Effort: Small)
- **Acceptance criteria:** Every link in the plan index resolves; Notifications/17 statuses match CLAUDE.md and Plan 18; no unqualified "100% Complete" claims remain.
- **Dependencies or follow-up:** Decision on docs/project-plan/ hub.
- **Confidence:** verified. Verifier: every cited fact holds in the working tree, and the finding actually understates the damage — lines 172-182 also link plans 01-11 that were moved to archive/, so 13 of 16 index links are dead; the only mitigation (PROJECT_STATUS.md:112) is not referenced from CLAUDE.md or the overview itself, so P1 is appropriate.

### 2. CLAUDE.md and both AGENTS.md files document a non-existent TypeScript Playwright E2E workflow

- **Finding:** All three command-reference docs (CLAUDE.md, root AGENTS.md, tests/ClimaSite.E2E/AGENTS.md) document a TypeScript Playwright E2E workflow that has never matched the actual C# Playwright-for-.NET suite.
- **Category:** doc-vs-code mismatch
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** CLAUDE.md:521-526 instructs `npx playwright install/test/--ui/-g/--debug` from tests/ClimaSite.E2E; CLAUDE.md:537 "Full Test Suite" ends with `npx playwright test`; CLAUDE.md:689 cites Test Factory at `tests/ClimaSite.E2E/fixtures/test-data-factory.ts`; CLAUDE.md:706 lists "Playwright Report | http://localhost:9323". AGENTS.md:112 and tests/ClimaSite.E2E/AGENTS.md:77-79 repeat the same `npx playwright` commands. Reality: the E2E project is C# Playwright-for-.NET — tests/ClimaSite.E2E/ClimaSite.E2E.csproj exists, there is no .ts file or fixtures/ dir in the project (verified by find), the factory is tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs, and CI runs it via `dotnet test tests/ClimaSite.E2E` (.github/workflows/test.yml:228) after `playwright.ps1 install` (test.yml:197). CLAUDE.md is even internally inconsistent: its Post-Implementation Workflow at line 257 correctly uses `cd ../../tests/ClimaSite.E2E && dotnet test`.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md; /Users/sarkisharalampiev/Projects/climasite/AGENTS.md; /Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/AGENTS.md; /Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml; /Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs
- **Why it matters:** Any developer or agent following the documented commands gets "no package.json" / "playwright not found" errors and cannot run the E2E suite; the wrong factory path and report URL compound the confusion. This breaks the project's own MANDATORY post-implementation workflow as written in three separate authority docs.
- **Recommended fix:** Replace all E2E command blocks with the real workflow: `dotnet build tests/ClimaSite.E2E`, `pwsh tests/ClimaSite.E2E/bin/.../playwright.ps1 install chromium` (first time), `dotnet test tests/ClimaSite.E2E` (filter with `--filter`), env vars E2E_BASE_URL/E2E_API_URL/TEST_ADMIN_SECRET per .codex/PROJECT_MEMORY.md. Update the Test Data Factory example to the C# API (CreateUserAsync/CleanupAsync, already correct in tests/ClimaSite.E2E/AGENTS.md body) and fix the Quick Reference factory path. Delete the localhost:9323 row. (Effort: Small)
- **Acceptance criteria:** Copy-pasting every command block in CLAUDE.md "Commands", AGENTS.md "COMMANDS", and tests/ClimaSite.E2E/AGENTS.md "COMMANDS" on a fresh clone (with shared infra up) succeeds; no `npx playwright` or `.ts` factory references remain anywhere outside archived docs.
- **Dependencies or follow-up:** None.
- **Confidence:** verified. Verifier (adjust → P2): every cited line verifies and the project is genuinely Playwright-for-.NET with CI using `dotnet test`; P1 was inflated because the failure is doc-only, immediate and self-evident, the correct command exists in the same CLAUDE.md at ~line 257, and CI is unaffected — real but moderate-impact doc rot.

### 3. CLAUDE.md documents API/Swagger on port 5000; the API actually runs on 5029

- **Finding:** CLAUDE.md's commands and Important URLs table point at http://localhost:5000 for the API and Swagger, but the API binds only to port 5029.
- **Category:** doc-vs-code mismatch
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** CLAUDE.md:509 "# Start API server (http://localhost:5000)", CLAUDE.md:704 "API Backend | http://localhost:5000", CLAUDE.md:705 "Swagger UI | http://localhost:5000/swagger". Actual: src/ClimaSite.Api/Properties/launchSettings.json defines applicationUrl http://localhost:5029 (both profiles); CI sets E2E_API_URL=http://localhost:5029 (.github/workflows/test.yml:231); AGENTS.md:103 and .codex/PROJECT_MEMORY.md both say 5029.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Properties/launchSettings.json; /Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml
- **Why it matters:** Anyone configuring the frontend proxy, curling the API, or opening Swagger from CLAUDE.md hits a dead port. Authority docs disagree on a basic fact, eroding trust in all of them.
- **Recommended fix:** Change all 5000 references in CLAUDE.md to 5029 (and note the `--launch-profile http` flag as AGENTS.md does). Add a single "ports" table that AGENTS.md and PROJECT_MEMORY link to instead of restating. (Effort: Small)
- **Acceptance criteria:** grep for "localhost:5000" across *.md returns zero hits outside docs/plans/archive; CLAUDE.md Important URLs table matches launchSettings.json.
- **Dependencies or follow-up:** None.
- **Confidence:** verified. Verifier (adjust → P2): mismatch fully verified and unmitigated in CLAUDE.md, but "three authority docs disagree" overstates it — AGENTS.md and PROJECT_MEMORY agree with the code; the failure is an immediate, self-diagnosing connection refused with a trivial fix, though CLAUDE.md being auto-loaded into every agent session makes it worth fixing promptly.

### 4. 20-issue-registry claims 129 open / 0 fixed, but verified fixes exist in code and some issues target deleted code; no plan owns the registry

- **Finding:** docs/plans/20-issue-registry.md presents 129 issues as all open with zero fixed, yet several CRITICAL items are verifiably fixed in code, some rows target the deleted v2 home page, and no active plan references or owns the registry.
- **Category:** stale tracking doc
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** docs/plans/20-issue-registry.md:13-25 dashboard: "Open: 129, Fixed: 0" (last commit 2026-01-24). Spot-checks: API-002 "PaymentsController missing authorization | CRITICAL" — PaymentsController has [Authorize] at src/ClimaSite.Api/Controllers/PaymentsController.cs:10; PROD-010 price swap — fixed per 21B-product-experience-redesign.md TASK-21B-001 "[x] ✅ Completed 2026-01-24"; API-018 "EmailService not sending emails" — src/ClimaSite.Infrastructure/Services/EmailService.cs implemented (validation summary records it FIXED). Registry rows HOME-003..HOME-006 (20-issue-registry.md:115-118) target the v2 home page deleted by Plan 18 FND-004 (18-project-completion.md:48). grep shows Plan 18 never references the registry — it has no owning plan.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/plans/20-issue-registry.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/PaymentsController.cs; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21B-product-experience-redesign.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md
- **Why it matters:** A 129-item "all open" registry of CRITICAL/HIGH issues that is actually part-fixed and part-obsolete is worse than no registry: it triggers re-investigation of fixed security issues and work against deleted code, and its real still-open items (e.g. API-015/API-016 transaction races, if still valid) are buried in noise.
- **Recommended fix:** One triage pass: mark each of the 129 issues Fixed / Obsolete (deleted code) / Still open with a date; fold the surviving open items into Plan 18's phase backlog (Phases 3-6 cover the same i18n/theme/test/security ground); then ARCHIVE the registry with a header pointing to Plan 18. (Effort: Medium)
- **Acceptance criteria:** Registry dashboard counts match row states; every still-open item appears in an active plan; file carries a "superseded by Plan 18" banner or lives in docs/plans/archive/.
- **Dependencies or follow-up:** Plan 18 backlog structure.
- **Confidence:** verified. Verifier (adjust → P2): every cited fact verifies, but the harm is substantially mitigated — PROJECT_STATUS.md flags the registry as materially stale/unowned, PRIORITIZED_BACKLOG.md declares precedence over it, and backlog task DOC-04 already tracks the triage-or-archive fix; acceptance criteria remain unmet, so the issue stands as a P2 consolidation item.

### 5. No README.md at repo root, no human onboarding/setup guide, no deployment runbook

- **Finding:** The repository has no README.md, no human-oriented onboarding/setup guide, and no deployment runbook explaining the Railway/Docker artifacts at the repo root.
- **Category:** missing documentation
- **Severity/Priority:** P2 — verification: adjusted
- **Evidence:** ls -a of /Users/sarkisharalampiev/Projects/climasite shows no README.md (only AGENTS.md, CHANGELOG.md, CLAUDE.md at root). All setup knowledge lives in agent-oriented files whose command sections are partly wrong (see findings on ports/E2E). Deployment artifacts railway.toml, railway.api.toml, Dockerfile.api, Dockerfile.web, docker-compose.yml exist at root with zero explanatory documentation (grep for deploy/railway across docs/ hits only tech-stack.md in passing). The 2026-04-08 gap report itself lists "Release process: Missing" and Plan 18 Phase 7/8 promise runbooks that don't exist yet.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/railway.toml; /Users/sarkisharalampiev/Projects/climasite/Dockerfile.api; /Users/sarkisharalampiev/Projects/climasite/docker-compose.yml; /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md
- **Why it matters:** GitHub renders nothing for the repo; a human contributor (or future-you on a new machine) has no entry point, and the only setup instructions that exist are agent context files containing wrong ports and wrong E2E commands. Deploy configuration is undiscoverable and unexplained.
- **Recommended fix:** Create README.md (what the project is, stack, prerequisites, shared-infra setup, run commands verified against launchSettings/CI, test commands, link map to CLAUDE.md / docs/). Add docs/operations/deployment.md explaining the Railway topology (railway.toml vs railway.api.toml), the two Dockerfiles, env vars, and migration strategy — much of this can be lifted from the gap report §infrastructure and Plan 18 Phase 7. (Effort: Medium)
- **Acceptance criteria:** Fresh-clone smoke test: a developer can go from git clone to running app + passing one E2E test using only README.md; deployment doc explains every root-level deploy artifact.
- **Dependencies or follow-up:** Findings on E2E commands and ports fixed first so the README copies correct commands.
- **Confidence:** verified. Verifier (adjust → P2): missing README confirmed, but two evidence pillars are overstated — deploy/Railway documentation partially exists (validation area 18, _review/devops.md) and DEV_WORKFLOW.md (2026-06-11, authoritative) now provides onboarding with correct ports/E2E commands; on a solo pre-release project with the gap tracked in Plan 18 Phase 8, P2 is the calibrated priority.

### 6. Three competing UI-redesign master plans with colliding numbers and contradictory design philosophies; Plan 18 supersession list is incomplete

- **Finding:** docs/plans/ contains three mutually contradictory redesign master plans, with two plans numbered 19 and four numbered 21, and Plan 18's supersession header does not name most of them.
- **Category:** duplication / structure
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Active docs/plans/ contains 19-ui-ux-redesign-masterplan.md ("Liquid Glass aesthetic", "Scroll-Driven Storytelling", last commit 2026-01-24), 20-quality-audit-masterplan.md + 20-issue-registry.md (audit program), and 21-ui-redesign-master-plan.md (parent of 21B-21J) plus 21-ui-improvement-plan.md ("Status: COMPLETE") and 21-cdl-data-import-and-ux-improvements.md — i.e., two different plans numbered 19 (archive/19-home-page-redesign.md is a third 19-artifact) and four plans numbered 21 (plus archived 21A). Plan 19's over-animation direction was explicitly reversed by 21F-animation-interaction-audit.md ("over-animation... Remove FloatingDirective, TiltEffectDirective, ParallaxDirective", confirmed removed — shared/directives/ has 7 directives, no parallax/tilt/floating). 18-project-completion.md:8 says it supersedes "plans 12, 13, 21F, and all previous home-page plans (19, 21A, 23, 24, 25)" but is silent about 19-ui-ux-redesign-masterplan's non-home content, 21-ui-redesign-master-plan, 21B-E/G/J open tasks, and the 20-series.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/plans/19-ui-ux-redesign-masterplan.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21-ui-redesign-master-plan.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21-ui-improvement-plan.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21F-animation-interaction-audit.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md
- **Why it matters:** An agent told to "continue the redesign plan" can land on three mutually contradictory masters; following Plan 19 would reintroduce the exact animations 21F removed. Number collisions defeat the NN-prefix convention that the whole docs/plans tree relies on.
- **Recommended fix:** Archive 19-ui-ux-redesign-masterplan, 20-quality-audit-masterplan, 21-ui-improvement-plan (complete), and 21-cdl (decide: complete or fold into Plan 18) with "superseded by Plan 18 / 21F" banners. Update Plan 18's supersession header to name them explicitly. Keep 21-ui-redesign-master-plan + 21B-21J only if their open tasks are still wanted — otherwise extract surviving tasks into Plan 18 phases and archive the rest. (Effort: Medium)
- **Acceptance criteria:** docs/plans/ contains at most one master plan per number; every archived plan has a banner naming its successor; Plan 18 header lists everything it supersedes.
- **Dependencies or follow-up:** Issue-registry triage (shares disposition decisions).
- **Confidence:** verified

### 7. Plan checkbox tracking is abandoned — "complete" plans have hundreds of unchecked boxes and Plan 12 shows 0/163 despite shipped code

- **Finding:** Per-task checkbox state in plan files is unreliable in both directions: implemented work is unchecked (Plan 12: 0/163) and plans declared complete carry hundreds of open boxes (21H/21I/21E).
- **Category:** stale tracking / process
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Checkbox counts (grep -c '\- \[x\]' / '\- \[ \]'): 12-notifications-system.md 0 done / 163 open even though Notification.cs exists in Core/Entities and EmailService.cs is implemented (and validation summary marks email service FIXED); 21H-visual-assets-imagery.md 7/111 and 21I-mobile-experience-optimization.md 10/213 despite commit 49ec083 "feat: Complete mobile optimization and visual assets (Plans 21I + 21H)" and CLAUDE.md listing both Complete; 21E claims "All tasks completed 49/49" in prose while file shows 16 checked / 54 unchecked.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/plans/12-notifications-system.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21H-visual-assets-imagery.md; /Users/sarkisharalampiev/Projects/climasite/docs/plans/21I-mobile-experience-optimization.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Core/Entities/Notification.cs
- **Why it matters:** Task-level state in plan files is unreliable in both directions (done-but-unchecked and claimed-complete-with-open-boxes), so nobody can compute remaining Notifications scope for Plan 18 Phase 2 from Plan 12 — the very next piece of work. Real status lives only in the CLAUDE.md table and .codex/PROJECT_MEMORY.md prose.
- **Recommended fix:** Stop pretending checkboxes are tracked: for Plan 12, do one reconciliation pass marking what's actually implemented vs the remaining NOT-* scope (this is required input for Plan 18 Phase 2 anyway); for archived/complete plans, add a header "checkbox state not maintained — see status header". Adopt the rule: status lives in a plan's Status header + Plan 18 phase table, not in per-task checkboxes, unless actively maintained. (Effort: Medium)
- **Acceptance criteria:** Plan 12 header states implemented-vs-remaining NOT-* scope accurately; all complete plans carry the non-maintained-checkbox disclaimer or true checkbox state.
- **Dependencies or follow-up:** Notifications decision (Plan 18 Phase 2).
- **Confidence:** verified

### 8. docs/validation/ suite (17 of 18 areas + summary) frozen at 2026-01-24 with claims now false

- **Finding:** The 8,000+-line validation suite is a January-2026 snapshot whose headline claims (zero auth unit tests, no catalog E2E, no E2E in CI) are now demonstrably false, with no banner marking it superseded.
- **Category:** stale audit docs
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** All area docs except 07-wishlist say "Generated: 2026-01-24" (07 was updated 2026-06-07 on this branch). Re-verified stale claims: 00-validation-summary.md:21 "01 Auth: Zero backend unit tests for auth logic" — false, tests/ClimaSite.Application.Tests/Features/Auth/Commands/{Login,Register,RefreshToken}CommandHandlerTests.cs exist; :25 "05: No E2E tests for catalog" — false, tests/ClimaSite.E2E/Tests/Products/ has ProductBrowsing/Catalog/FilteringTests.cs; areas/18-build-cicd-deployment.md:560 recommends "Add E2E tests to CI pipeline" — already done (.github/workflows/test.yml:135-243 E2E job).
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/validation/00-validation-summary.md; /Users/sarkisharalampiev/Projects/climasite/docs/validation/areas/18-build-cicd-deployment.md; /Users/sarkisharalampiev/Projects/climasite/docs/validation/areas/01-auth-authorization.md; /Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/Tests/Products/
- **Why it matters:** These 8,000+ lines look authoritative ("Validation Report") and Plan 18 Phase 4 (coverage closure) is the natural consumer — using their numbers would misallocate effort to already-closed gaps. The 2026-04-08 gap report already supersedes them as the current baseline but no doc says so.
- **Recommended fix:** Add a banner to 00-validation-summary.md and each unrefreshed area doc: "Snapshot 2026-01-24 — superseded by docs/audit/2026-04-08-gap-report.md; refresh per-area before relying on it" (the 07-wishlist refresh shows the intended pattern). Refresh only the areas Plan 18 Phase 4-6 actually need, on demand. (Effort: Small)
- **Acceptance criteria:** Every validation doc carries either a current "Updated:" date or a superseded banner; no doc claims auth has zero tests or that CI lacks E2E.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 9. Tech stack version contradictions: CLAUDE.md says EF Core 9.x (actual 10.0.1); docs/tech-stack.md still in undecided "options" mode

- **Finding:** The two stack documents disagree with the code and with each other — CLAUDE.md states EF Core 9.x against an actual 10.0.1, and docs/tech-stack.md still presents Angular Material/PrimeNG as undecided options when neither is installed.
- **Category:** doc-vs-code mismatch
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** CLAUDE.md:56 "| ORM | Entity Framework Core | 9.x |" vs src/ClimaSite.Infrastructure/ClimaSite.Infrastructure.csproj: Microsoft.EntityFrameworkCore 10.0.1, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0. docs/tech-stack.md (last commit 2026-01-12) says "UI Components: Angular Material or PrimeNG — Options:" but package.json contains neither — only tailwindcss ^3.4.19; tech-stack.md does say EF Core 10 (correct), so the two docs disagree with each other as well. CLAUDE.md's stack table omits Tailwind entirely.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md; /Users/sarkisharalampiev/Projects/climasite/docs/tech-stack.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/ClimaSite.Infrastructure.csproj; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/package.json
- **Why it matters:** Two stack docs disagree on ORM version and UI library; an agent could write EF 9-era code or install Angular Material believing it's the sanctioned component library. The "or" language reveals tech-stack.md was never updated after decisions were made.
- **Recommended fix:** Fix CLAUDE.md:56 to EF Core 10.x and add Tailwind row. Rewrite tech-stack.md as decided-state (Tailwind + custom components, no Material/PrimeNG; actual versions from csproj/package.json) or MERGE it into CLAUDE.md's stack table + ADRs and delete it. (Effort: Small)
- **Acceptance criteria:** Single stack table whose versions match csproj/package.json; zero "or"-style undecided entries.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 10. ADR coverage gap: only 2 ADRs (both home-v3), key decisions unrecorded, and ADR 002 was amended in-place against its own rules

- **Finding:** The ADR system contains only two entries (both about the home page), ADR 002 was edited in-place to reverse its renderer decision in violation of the ADR README's immutability rule, and numerous significant decisions (Argon2, Tailwind, rate limiting, wishlist share tokens) have no ADR.
- **Category:** missing documentation / process
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** docs/adr/ index lists only 001 and 002 (both home page). docs/adr/README.md mandates "Never edit the body of an accepted ADR to reverse its decision. Write a new ADR that supersedes it" — yet 002-home-v3-stack-and-assets.md:17 was edited to record that production ships Canvas 2D instead of the decided Three.js ("Use Three.js latest stable... pinned in package.json" at :24-26; three is absent from package.json, verified by grep). Plan 18 guardrail 5 requires "ADR for every non-obvious decision" and Phase 8 targets ≥8 ADRs. Undocumented decisions visible in code/docs: Argon2 password hashing, JWT refresh rotation, Tailwind-only UI (no Material/PrimeNG), AspNetCoreRateLimit, Stripe webhook signature handling, shared-infra local dev model, EF Core 10 upgrade, and the current branch's wishlist public share-token design (SetWishlistSharingCommand/RegenerateWishlistShareTokenCommand are new uncommitted files with no ADR).
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/adr/README.md; /Users/sarkisharalampiev/Projects/climasite/docs/adr/002-home-v3-stack-and-assets.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Commands/SetWishlistSharingCommand.cs
- **Why it matters:** The ADR system is well-designed but two entries deep; the first real decision change after its creation (Canvas 2D vs Three.js) already violated its core immutability rule, and the in-flight wishlist sharing feature is shipping a security-relevant token design with no recorded rationale.
- **Recommended fix:** Write ADR 003 "Home v3 ships Canvas 2D renderer; Three.js deferred" superseding the relevant section of 002 (restore 002's original decision text or mark the amendment as such). Write ADR 004 for the wishlist share-token model before merging this branch. Backfill short ADRs for Argon2, rate limiting, Tailwind, shared-infra — satisfies Plan 18 Phase 8's ≥8 target. (Effort: Medium)
- **Acceptance criteria:** ADR index has entries covering Canvas-2D amendment and wishlist sharing; no accepted ADR body contains decision-reversing edits without a superseding ADR.
- **Dependencies or follow-up:** Wishlist branch merge timing.
- **Confidence:** verified

### 11. Triplicated agent-context docs (CLAUDE.md / AGENTS.md / .codex/PROJECT_MEMORY.md) and triplicated skills with no sync rule and visible drift

- **Finding:** Three agent-facing context documents and three skill trees duplicate the same knowledge with no declared hierarchy or sync rule, and they have visibly drifted (stale counts, divergent commands, divergent skill copies).
- **Category:** duplication
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Root AGENTS.md header: "Generated: 2026-01-25, Commit: 69b7e8b, Branch: main" (AGENTS.md:3-5) — predates Home v3 and wishlist work; its "Full test suite" (AGENTS.md:114-115) omits E2E entirely while CLAUDE.md's includes it (with the wrong runner). Nested snapshots drifted: shared/AGENTS.md claims "45+ components, 9 directives" but shared/directives/ now has 7 directives (parallax/tilt/floating removed by 21F); Application/Features/AGENTS.md claims "20 feature folders" vs 18 actual; core/services AGENTS.md claims "28 singleton services" vs 19 files in that dir (27 repo-wide). Skills exist in three trees: .claude/skills/ (9), .codex/skills/ (9 "Codex-compatible summaries" per AGENTS.md:120), .opencode/skills/ui-ux-pro-max (diff confirms it diverges from the .claude copy). PROJECT_MEMORY.md duplicates shared-infra and merge-readiness rules from AGENTS.md with slightly different wording.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/AGENTS.md; /Users/sarkisharalampiev/Projects/climasite/.codex/PROJECT_MEMORY.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/shared/AGENTS.md; /Users/sarkisharalampiev/Projects/climasite/.opencode/skills/ui-ux-pro-max/SKILL.md
- **Why it matters:** Three agent-facing sources of truth that disagree on ports, test commands, and component counts mean every agent session starts from partially wrong context; nothing documents which file wins or when each must be regenerated.
- **Recommended fix:** Declare a hierarchy in each file's header: CLAUDE.md = canonical conventions; AGENTS.md = generated snapshot (add "regenerate after each merged plan phase" rule and refresh it now); PROJECT_MEMORY.md = Codex session state only, linking rather than restating. Regenerate nested AGENTS.md counts. For skills, keep .claude/skills as source, note that .codex copies are derived, and delete or sync the orphan .opencode copy. (Effort: Medium)
- **Acceptance criteria:** Each context file states its role and update trigger; AGENTS.md regenerated (correct directive/feature counts, dotnet-test E2E command, full suite includes E2E); .opencode/skills either removed or byte-identical to source.
- **Dependencies or follow-up:** Finding on E2E commands (same edit pass).
- **Confidence:** verified

### 12. Root docker-compose.yml (project-local Postgres on 5433) contradicts the documented shared-infra workflow and is referenced by no doc

- **Finding:** A project-local docker-compose.yml defining Postgres (host port 5433) and Redis sits at the repo root, contradicting every written shared-infra rule, unused by CI, and unexplained by any document.
- **Category:** doc-vs-code mismatch / missing documentation
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** docker-compose.yml at repo root defines climasite_postgres on host port 5433 and redis. AGENTS.md: "use shared infra at ~/Projects/shared-infra, not project-local compose" and .codex/PROJECT_MEMORY.md: "Do not add project-local compose services when the shared stack already provides the service"; user-global rules forbid project-local compose for these services. CI uses its own service containers (.github/workflows/test.yml), not this file. grep across docs/ finds no explanation of the file's purpose; CLAUDE.md never mentions it.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docker-compose.yml; /Users/sarkisharalampiev/Projects/climasite/AGENTS.md; /Users/sarkisharalampiev/Projects/climasite/.codex/PROJECT_MEMORY.md
- **Why it matters:** A developer who finds docker-compose.yml will `docker compose up` a second Postgres on 5433 and then wonder why the app (configured for shared 5432) sees no data — the artifact silently contradicts every written workflow rule in the repo.
- **Recommended fix:** Either delete the compose file (if truly unused) or document its purpose (e.g., "CI-parity stack / non-shared-infra fallback") in README + a header comment, with explicit precedence: shared-infra is the default for local dev. (Effort: Small)
- **Acceptance criteria:** Repo contains either no project-local compose for pg/redis, or a documented one whose doc explains when to use it vs shared-infra.
- **Dependencies or follow-up:** Owner decision (may be intentionally kept for CI parity).
- **Confidence:** verified

### 13. CLAUDE.md misc stale references: non-existent ClimaSite.Web.Tests project, docs/skills.md file, "plans 12, 13, 17 active", parallax listed as live feature

- **Finding:** CLAUDE.md contains several small stale references: a test project and a docs file that do not exist, an outdated active-plans note, and a status row listing the removed parallax feature.
- **Category:** doc accuracy
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** CLAUDE.md:128 lists "tests/ClimaSite.Web.Tests/ # Angular unit tests (karma.conf.js)" — tests/ contains only Api.Tests, Application.Tests, Core.Tests, E2E (verified by ls; Angular tests live in src/ClimaSite.Web). CLAUDE.md:132 lists "docs/skills.md" — actual is docs/skills/ directory (14 files). CLAUDE.md:131 says "Implementation plans (12, 13, 17 active)" — 13 is archived, 17 complete, 18 is the active plan. CLAUDE.md:23 status row "Motion/Animation System | Complete | AnimationService, flying cart, confetti, parallax" — ParallaxDirective was removed (no parallax.directive.ts in shared/directives/, and CLAUDE.md's own Recently Completed table records its removal under Animation Audit 21F).
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md
- **Why it matters:** Individually small, but each sends a reader to a non-existent path or stale plan; the parallax row contradicts another row in the same file.
- **Recommended fix:** Single cleanup pass on CLAUDE.md's structure tree, plans note, and status table (drop "parallax", point to Plan 18 as active). (Effort: Small)
- **Acceptance criteria:** Every path in CLAUDE.md's structure section exists; status table self-consistent.
- **Dependencies or follow-up:** None.
- **Confidence:** verified

### 14. CHANGELOG.md is well-structured but missing an entry for the wishlist-completion slice on this branch

- **Finding:** CHANGELOG.md follows Keep a Changelog and covers Plan 18 Phase 0-1, but the completed wishlist slice on the current branch has no Added/Changed bullets.
- **Category:** doc maintenance
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** CHANGELOG.md follows Keep a Changelog with a populated [Unreleased] covering Home v3 / Plan 18 Phase 0-1, and its own guidance says "One bullet per user-visible or developer-visible change". The working tree contains the completed wishlist slice (new share endpoints, ClearWishlistCommand, SetWishlistSharingCommand, guest merge — per git status and CLAUDE.md status table "Wishlist | Complete"), yet CHANGELOG.md is unmodified in git status — no Added/Changed bullets for sharing, merge, or the new public route.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/CHANGELOG.md; /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Commands/SetWishlistSharingCommand.cs
- **Why it matters:** The changelog discipline established in Plan 18 FND-003 breaks on its second feature; the /release skill will produce incomplete v1.0.0 notes if slices skip the changelog.
- **Recommended fix:** Add Unreleased bullets for the wishlist slice (public sharing, guest-to-login merge, clear endpoint, concurrent-add protection) before merging this branch; add "CHANGELOG updated" to the branch merge checklist in AGENTS.md/PROJECT_MEMORY. (Effort: Small)
- **Acceptance criteria:** Branch diff includes CHANGELOG.md entries matching the shipped wishlist behavior.
- **Dependencies or follow-up:** Do before branch merge.
- **Confidence:** verified

### 15. docs/skills/ mixes audit personas, superseded motion research, and a misplaced plan; motion docs contradict the adopted design direction

- **Finding:** docs/skills/ contains audit personas for the superseded Plan 20, January motion/scroll research that contradicts the adopted 21F restraint philosophy, and a plan document misfiled under skills/.
- **Category:** structure / stale docs
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** docs/skills/ (14 files, all last committed 2026-01-24) contains: 6 SKILL-*.md audit personas consumed by the superseded 20-quality-audit-masterplan; research docs (motion-scroll-patterns.md, scroll-storytelling-patterns.md, climasite-motion-opportunities.md, micro-interaction-patterns.md, visual-hierarchy-patterns.md, ecommerce-ux-patterns.md) that fed the deleted v2 scroll-driven home and the reversed Plan 19 direction; and ui-ux-improvement-plan.md — a v1.0 plan document misfiled under skills/. 21F's adopted "Nordic Tech" restraint philosophy (21F-animation-interaction-audit.md:18-27) directly contradicts the scroll-storytelling/motion-opportunity recommendations.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/skills/ui-ux-improvement-plan.md; /Users/sarkisharalampiev/Projects/climasite/docs/skills/scroll-storytelling-patterns.md; /Users/sarkisharalampiev/Projects/climasite/docs/skills/SKILL-UI.md
- **Why it matters:** An agent looking for "how should motion work here" finds January research advocating the over-animation the project later removed; a plan living inside a skills folder undermines the directory convention.
- **Recommended fix:** Move the motion/scroll research and ui-ux-improvement-plan.md to docs/plans/archive/ (or docs/archive/research/) with superseded banners; keep SKILL-*.md only if the quality-audit process will run again, else archive alongside Plan 20. Note real reusable skills now live in .claude/skills/. (Effort: Small)
- **Acceptance criteria:** docs/skills/ contains only documents consistent with the current 21F motion direction, or is emptied/archived.
- **Dependencies or follow-up:** Plan 20 disposition.
- **Confidence:** verified

### 16. Plan 18 guardrails instruct updating a non-existent .auto-memory/ directory

- **Finding:** The active master plan's non-negotiable guardrails reference a `.auto-memory/` directory that does not exist in the checkout, a fact the plan itself admits later without fixing the guardrail text.
- **Category:** doc accuracy
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** 18-project-completion.md:13 "These derive from CLAUDE.md and .auto-memory/" and :20 "Update .auto-memory/, CLAUDE.md, and this plan after every iteration" — but ls confirms no .auto-memory/ exists; the plan itself admits this at line 163 ("No .auto-memory/ directory exists in this checkout; .codex/PROJECT_MEMORY.md is the GPT-compatible shared memory file") without fixing the guardrail text. The actual auto-memory lives outside the repo at ~/.claude/projects/.../memory/.
- **Affected files/areas:** /Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md
- **Why it matters:** The active master plan's non-negotiable guardrails reference a path that cannot be updated, creating a rule agents silently skip — which trains them to skip other guardrails.
- **Recommended fix:** Edit guardrails 0.6 and the §0 intro to reference .codex/PROJECT_MEMORY.md (and the external Claude auto-memory) instead of .auto-memory/. (Effort: Small)
- **Acceptance criteria:** grep '.auto-memory' in docs/plans/18-project-completion.md returns only the historical note at line 163 (or nothing).
- **Dependencies or follow-up:** None.
- **Confidence:** verified

## Dimension data

# Documentation Audit — Extras

## A. Full Disposition Table

| Doc | Status (verified 2026-06-11) | Disposition | Reason |
|---|---|---|---|
| **README.md** | MISSING | **CREATE** | No repo entry point; only agent-context files exist and they contain wrong commands/ports |
| **CLAUDE.md** | Mostly current status tables; Commands/URLs/structure sections wrong (npx playwright, port 5000, ClimaSite.Web.Tests, docs/skills.md, EF 9.x, parallax) | **UPDATE** | Canonical conventions doc; fix commands, ports, stack versions, structure tree, active-plans note |
| **AGENTS.md (root)** | Snapshot of 2026-01-25 @ 69b7e8b; correct port 5029 but wrong E2E command; full-suite omits E2E; KNOWN ISSUES table unverified-stale | **UPDATE (regenerate)** | Useful generated knowledge base; needs regeneration post-Home-v3/wishlist + explicit "generated snapshot, regenerate after each merged phase" rule |
| AGENTS.md — `src/ClimaSite.Core/Entities/` | Accurate (25 entities verified) | KEEP | Matches code |
| AGENTS.md — `src/ClimaSite.Application/Features/` | "20 feature folders" vs 18 actual | UPDATE | Minor count drift |
| AGENTS.md — `core/services/` | "28 services" vs 19 in dir / 27 repo-wide | UPDATE | Count drift |
| AGENTS.md — `shared/` | "9 directives" vs 7 (post-21F removals) | UPDATE | Drift after directive removals |
| AGENTS.md — `tests/ClimaSite.E2E/` | C# examples correct; COMMANDS block is TS Playwright (wrong) | **UPDATE** | Fix COMMANDS to `dotnet test` + env vars |
| **CHANGELOG.md** | Maintained, Keep-a-Changelog format; missing wishlist-slice entries | UPDATE | Add Unreleased bullets before branch merge |
| **.codex/PROJECT_MEMORY.md** | Current (2026-06-07), most accurate operational doc in repo | KEEP | Best source for local E2E env vars/ports; add header defining its role vs CLAUDE.md/AGENTS.md |
| **docs/tech-stack.md** | Stale (2026-01-12): "Angular Material or PrimeNG" undecided language; EF 10 correct but conflicts w/ CLAUDE.md | MERGE-INTO-CLAUDE.md (+ADRs) or UPDATE to decided-state | Two stack docs disagree; one should win |
| docs/plans/00-master-overview.md | Index wrong (Notifications "Complete", 17 "Backlog", dead links to 15/16, omits 18-22, "100% Complete" claims) | UPDATE index §4 then ARCHIVE bulk | Historical inception plan; live index belongs in docs/project-plan/ |
| docs/plans/12-notifications-system.md | 0/163 boxes checked; partially implemented in code | UPDATE (reconcile) | Required input for Plan 18 Phase 2; mark implemented vs remaining NOT-* |
| docs/plans/17-future-enhancements.md | Complete per own status table | ARCHIVE | Done; move to archive/ like 01-11/13 |
| docs/plans/18-project-completion.md | ACTIVE master plan; accurate except `.auto-memory/` refs | KEEP (UPDATE guardrails) | The single live master plan |
| docs/plans/19-ui-ux-redesign-masterplan.md | Superseded: home content replaced by Plan 18/ADR 001; motion philosophy reversed by 21F | **ARCHIVE** | Number collides with archived 19-home-page-redesign; following it would undo 21F |
| docs/plans/20-quality-audit-masterplan.md | Audit waves executed Jan 2026; process not running | ARCHIVE | Historical program doc; keep SKILL personas only if process restarts |
| docs/plans/20-issue-registry.md | Dashboard false (129 open / 0 fixed; verified fixes exist; HOME-* target deleted code); orphaned (no plan references it) | UPDATE (triage) then ARCHIVE | Surviving open items fold into Plan 18 backlog |
| docs/plans/21-ui-improvement-plan.md | Self-declared COMPLETE (v2.0, 2026-01-24) | ARCHIVE | Done |
| docs/plans/21-cdl-data-import-and-ux-improvements.md | 0/13 boxes; unclear if executed | UPDATE status then ARCHIVE or fold into 18 | Needs one disposition decision from owner |
| docs/plans/21-ui-redesign-master-plan.md | Parent of 21B-21J; partially executed | UPDATE: add per-child status table, mark superseded-by-18 where applicable | One of three competing masters; either becomes the 21-series index or archives |
| docs/plans/21B..21E, 21G | Large open-task counts (e.g. 21B 5/135) | UPDATE status header; fold surviving wanted tasks into Plan 18; ARCHIVE rest | Checkbox state unreliable; design direction partly superseded |
| docs/plans/21F-animation-interaction-audit.md | Phases 1-2 done (per CLAUDE.md); explicitly continued by Plan 18 Phase 2 | KEEP until ANIM-* done, then ARCHIVE | Still referenced as open work |
| docs/plans/21H, 21I | Declared complete (commit 49ec083, CLAUDE.md) but 111/213 unchecked boxes | ARCHIVE with non-maintained-checkbox banner | Done per status-of-record |
| docs/plans/21J-dark-mode-refinement.md | 20/134 checked; dark-mode work continues in Plan 18 Phase 3 | UPDATE: extract remaining tasks into Plan 18 Phase 3, then ARCHIVE | Overlaps Phase 3 scope |
| docs/plans/22-validation-master-plan.md | "Status: Draft — Area Catalog"; its output (docs/validation/) already produced | ARCHIVE | Fulfilled; catalog duplicated by validation suite |
| docs/plans/archive/ (01-11, 13, 19, 21A) | Correctly archived | KEEP | Working archive pattern — extend it |
| docs/validation/00-validation-summary.md | 2026-01-24 snapshot, part-updated with FIXED strikethroughs; several key findings now false (verified) | UPDATE (banner: superseded by 2026-04-08 gap report) | Don't refresh wholesale; refresh per-area on demand |
| docs/validation/areas/01-06, 08-18 | Frozen 2026-01-24 | UPDATE (banner) | Stale; area 18's top recommendation (E2E in CI) already done |
| docs/validation/areas/07-wishlist.md | Updated 2026-06-07 on this branch | KEEP | The model for area refreshes |
| docs/adr/README.md + 000-template.md | Good process doc | KEEP | — |
| docs/adr/001, 002 | Accepted; 002 amended in-place (Canvas 2D vs Three.js) violating README rule; Three.js absent from package.json | UPDATE: ADR 003 supersedes the renderer decision | Process integrity |
| docs/audit/2026-04-08-gap-report.md | Current baseline; drives Plan 18 | KEEP | Single best status snapshot; date-stamped by design |
| docs/skills/SKILL-{UI,UX,API,A11Y,I18N,PERF}.md | Audit personas for superseded Plan 20 | ARCHIVE (with Plan 20) | Unused unless audit process restarts |
| docs/skills/{motion,scroll,micro-interaction,visual-hierarchy,ecommerce}-patterns.md, climasite-motion-opportunities.md, ui-ux-skills.md | Jan research feeding deleted v2 home / reversed Plan 19 direction | ARCHIVE | Contradicts adopted 21F restraint philosophy |
| docs/skills/ui-ux-improvement-plan.md | A plan misfiled under skills/ | MOVE to docs/plans/archive/ | Wrong location |
| docs/concepts/home-v3/ (README, 3 concepts, mock, screenshots) | Accurate record of HOME-001/003 decision inputs | KEEP | Referenced by ADR 001/Plan 18; valuable decision artifact |
| docs/performance/performance-audit.md | Referenced by CLAUDE.md File Locations | KEEP (spot-check next perf phase) | Not re-verified in depth |
| .claude/skills/ (9) | Source skills | KEEP | Source of truth |
| .codex/skills/ (9) | Intentional Codex summaries (per AGENTS.md:120) | KEEP (document derivation + sync rule) | Deliberate, but un-synced duplication risk |
| .opencode/skills/ui-ux-pro-max | Divergent third copy (diff confirms) | DELETE or SYNC from .claude | Orphan |
| docker-compose.yml (root) | Contradicts shared-infra rule; undocumented; unused by CI | DELETE or DOCUMENT | See finding |

## B. Doc-vs-Code Mismatch Quick Map

| Claim | Where | Reality | Proof |
|---|---|---|---|
| E2E = `npx playwright test` + TS factory | CLAUDE.md:521-526,537,689; AGENTS.md:112; E2E/AGENTS.md:77-79 | C# Playwright .NET, `dotnet test` | ClimaSite.E2E.csproj; test.yml:228; Infrastructure/TestDataFactory.cs |
| API on :5000 | CLAUDE.md:509,704-705 | :5029 | launchSettings.json; test.yml:231 |
| EF Core 9.x | CLAUDE.md:56 | 10.0.1 | Infrastructure csproj |
| Angular Material or PrimeNG | tech-stack.md | Tailwind only | package.json |
| tests/ClimaSite.Web.Tests exists | CLAUDE.md:128 | No such project | ls tests/ |
| Notifications Complete | 00-master-overview:183 | Partial (NOT-* open) | Plan 18:36; CLAUDE.md status table |
| Plans 15/16 files | 00-master-overview:185-186 | Don't exist | ls plans/ + archive/ |
| 129 issues open / 0 fixed | 20-issue-registry:13-25 | Multiple verified fixed; some target deleted code | PaymentsController.cs:10; 21B TASK-21B-001 |
| Zero auth unit tests / no catalog E2E / no E2E in CI | validation summary:21,25; area 18:560 | All three false now | Application.Tests/Features/Auth/; E2E/Tests/Products/; test.yml:135 |
| Three.js pinned in package.json | ADR 002:24-26 | Absent; Canvas 2D shipped | package.json grep; ADR 002:17 amendment |
| `.auto-memory/` exists | Plan 18:13,20 | Doesn't | ls; Plan 18:163 self-admission |

## C. Missing Docs (create)

1. **README.md** (root) — project intro, prerequisites, shared-infra setup, verified run/test commands, doc map. P1.
2. **Onboarding/setup guide** — can be README §Setup; must include the Testing-env E2E recipe currently only in .codex/PROJECT_MEMORY.md.
3. **Deployment runbook** (docs/operations/deployment.md) — railway.toml vs railway.api.toml topology, Dockerfiles, env vars, migration-on-startup behavior and rollback story (gap report flags it), graceful shutdown status. Feeds Plan 18 Phase 7.
4. **API overview beyond Swagger** — short docs/architecture/api.md: auth model, correlation IDs, pagination envelope, error/ProblemDetails contract (currently scattered in CLAUDE.md §API Conventions; fine to extract rather than rewrite).
5. **ERD / schema overview** — one exists embedded at 00-master-overview §7 (line ~640) but is pre-implementation and will drift; extract to docs/architecture/data-model.md regenerated from current EF model/migrations.
6. **Documentation standard** (docs/README.md) — see §D.

## D. Recommended Target Structure & Standard

```
README.md                  # humans: what/why/run (NEW)
CLAUDE.md                  # agent conventions + commands (canonical for workflow rules)
AGENTS.md (+nested)        # GENERATED snapshots; header states generation commit + refresh rule
CHANGELOG.md               # release log (keep)
docs/
  README.md                # doc map + the standard below (NEW)
  project-plan/            # NEW planning hub (created by this review): live status, backlog,
                           # plan index (replaces 00-master-overview §4 / CLAUDE.md Active Plans)
  plans/                   # ONLY active plans (today: 18, 12-reconciled, 21F remainder)
  plans/archive/           # everything done/superseded, each with a banner naming successor
  adr/                     # decisions (keep; grow per finding)
  architecture/            # NEW: tech-stack (decided-state), data-model/ERD, api overview
  operations/              # NEW: deployment runbook, local-infra notes (incl. compose verdict)
  audit/                   # dated, immutable snapshots (gap report pattern — keep)
  validation/              # per-area reports with Updated: dates; banner when stale
  concepts/                # design artifacts (keep)
```

**Standard (single source of truth per topic):**
- Commands/ports: CLAUDE.md is canonical; AGENTS.md/PROJECT_MEMORY link or are regenerated from it. Never restate in plans.
- Feature status: one table, in docs/project-plan/ (CLAUDE.md status table becomes a pointer or thin mirror). Plan checkboxes are not status — each plan carries a Status header.
- Decisions: ADRs only; never edit accepted ADR bodies (write superseding ADRs).
- Dated snapshots (audits, validation): immutable + banner when superseded; new findings = new dated file.
- Archive rule: a plan is either in plans/ (active) or plans/archive/ (with successor banner) — no third state.
- Update triggers: CHANGELOG + status table + relevant AGENTS.md regeneration on every merged slice (add to merge checklist).

## E. Open Questions for Owner

1. Should 21B/21C/21D/21E/21G/21J's remaining tasks survive (fold into Plan 18 phases) or be dropped wholesale when archiving? (~700 unchecked boxes at stake.)
2. Was 21-cdl-data-import ever executed? (0/13 boxes, no completion claim anywhere.)
3. Is root docker-compose.yml intentionally kept (CI-parity / non-Mac contributors), or deletable under the shared-infra rule?
4. Is the 20-issue-registry triage worth doing item-by-item, or is the 2026-04-08 gap report accepted as the superseding baseline (cheaper: archive registry with banner, trust gap report)?
5. Who/what regenerates AGENTS.md — is there a script, or was it a one-off generation at 69b7e8b? (Determines whether 'regenerate after each phase' is feasible.)
6. docs/performance/performance-audit.md was not deep-verified in this pass — flag for re-check during Plan 18 Phase 6.

## Refuted during verification

None.
