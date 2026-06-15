# Dev Workflow, CI/CD & Operations — Review Findings (2026-06-11)

## Summary

The dev-workflow/ops dimension is mid-maturity. CI is genuinely strong: a 5-job GitHub Actions pipeline runs backend unit, integration, frontend, build verification, and a real .NET Playwright E2E suite against live Postgres/Redis services — the latest main run is green at ~14.5 min. Health endpoints are properly wired (`/health` with Npgsql+Redis checks, `/health/live`, `/health/ready`), and Railway deployment scaffolding exists (multi-stage Dockerfiles, PORT-templated nginx with `/api` proxy, railway.toml healthchecks, env-var-first config with Railway URL converters).

However, the project is NOT production-launchable today, and its own Plan 18 Phase 7 (PROD-100..107) honestly admits most of the gaps. The single worst issue is that `Program.cs` unconditionally runs `DataSeeder` at startup, which in a Production container (Dockerfile.api sets `ASPNETCORE_ENVIRONMENT=Production`) migrates the DB and seeds a hardcoded admin account `admin@climasite.local` / `Admin123!` plus a demo catalog — a P0 credential backdoor and data-pollution bug in one. There is no deploy workflow at all; the deployment story is fragmented across three railway.toml files and duplicated, drifting Dockerfiles. Observability is console-only Serilog: the `X-Correlation-Id` convention documented in CLAUDE.md is not implemented anywhere, and there is no error tracker, metrics, tracing, alerting, or runbooks. The API never calls `UseForwardedHeaders`, so behind the nginx proxy/Railway edge the per-IP rate limiter collapses into one shared 100 req/min bucket for all users — a self-inflicted outage waiting for the first traffic spike.

Developer docs are dangerously stale: CLAUDE.md and both AGENTS.md files document a TypeScript Playwright workflow (`npx playwright test`, `fixtures/test-data-factory.ts`) that does not exist (the suite is Microsoft.Playwright for .NET, run via `dotnet test`), the documented API port (5000) is wrong (5029), and the local DB story contradicts itself (appsettings defaults to project-local compose Postgres on 5433 while AGENTS.md and the user's global convention mandate shared-infra on 5432). `main` is unprotected and CLAUDE.md still mandates direct pushes to main, while actual recent practice is PR-based. Secrets handling is split between two conventions (flat env vars for JWT/DB/Redis vs config-section keys for Stripe/SMTP/MinIO), with non-empty dummy Stripe keys in appsettings.json that defeat fail-fast if Railway vars are set under the documented (wrong) names. No release has ever been tagged despite a release skill and Keep-a-Changelog scaffolding.

## Findings

### 1. Production startup seeds hardcoded admin credentials (Admin123!) and demo catalog unconditionally

- **Finding:** `Program.cs` unconditionally runs `DataSeeder` at startup, so a Production container migrates the database and seeds a hardcoded admin account (`admin@climasite.local` / `Admin123!`) plus a demo catalog into the production database on first boot.
- **Category:** security / production-readiness
- **Severity/Priority:** P0 (Critical) — verification: confirmed
- **Evidence:** `src/ClimaSite.Api/Program.cs:32` calls `await SeedDatabaseAsync(app)` with no environment gate; `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:27-46` `SeedAsync()` has no env check and runs SeedAdminUserAsync/SeedProductsAsync; DataSeeder.cs:64-65 hardcodes `adminEmail = "admin@climasite.local"`, `adminPassword = "Admin123!"`; DataSeeder.cs:92+ seeds demo categories/brands/products whenever tables are empty. Dockerfile.api:37 sets `ENV ASPNETCORE_ENVIRONMENT=Production`, so the production container executes this on first boot against the production database.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/DataSeeder.cs`, `/Users/sarkisharalampiev/Projects/climasite/Dockerfile.api`
- **Why it matters:** Anyone who reads this public-pattern seeding code (or guesses Admin123!) gets full Admin role on the production store — order data, customer PII, price changes. Demo HVAC products also pollute the live catalog. This is a launch blocker on its own.
- **Recommended fix:** Gate SeedRolesAsync as always-on, but run admin/product/promotion seeding only in Development/Testing (check IWebHostEnvironment). Bootstrap the production admin from required env vars (ADMIN_EMAIL/ADMIN_INITIAL_PASSWORD) with forced password rotation, or a one-time CLI command. Fail startup in Production if seed credentials would be used. *(Effort: Small)*
- **Acceptance criteria:** Starting the API with ASPNETCORE_ENVIRONMENT=Production against an empty DB creates no admin@climasite.local user and no demo products; an integration test asserts seeding is env-gated; login with Admin123! fails in prod.
- **Dependencies or follow-up:** Coordinate with the migrations-at-startup fix (same code path, DataSeeder.cs:31).
- **Confidence:** verified — Verifier confirmed every cited line with no mitigation anywhere (DataSeeder registered unconditionally, no ADMIN_EMAIL/ADMIN_PASSWORD override exists); the GitHub repo is PUBLIC so the credentials are world-readable, the account is seeded EmailConfirmed=true and recreated on every restart if deleted, login succeeds with no env restriction, and the project's own PRODUCTION_READINESS_CHECKLIST.md:19 independently flags this exact issue as a launch blocker — P0 justified.

### 2. No deploy workflow; deployment config fragmented across 3 railway.toml files and drifting duplicate Dockerfiles

- **Finding:** There is no deploy workflow at all, and the deployment configuration is fragmented across three disagreeing railway.toml files plus duplicated, drifting Dockerfiles and a stale nginx.conf copy.
- **Category:** deployment
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `.github/` contains only `workflows/test.yml` (find output). Three Railway configs disagree: `railway.toml` (root, dockerfilePath Dockerfile.web), `railway.api.toml` (root, Dockerfile.api), `src/ClimaSite.Api/railway.toml` (dockerfilePath "Dockerfile", adds watchPatterns `["src/**"]`). Duplicate Dockerfiles drift: `src/ClimaSite.Web/Dockerfile` differs from root `Dockerfile.web` (EXPOSE 80 + wget HEALTHCHECK vs Railway-PORT model; diff output captured). `src/ClimaSite.Web/nginx.conf` is a stale non-template copy lacking the `/api` proxy block (diff vs nginx.conf.template). `docs/plans/18-project-completion.md:352-359` (PROD-104) confirms `.github/workflows/deploy.yml` is still TODO; no docs state which Railway service consumes which config file.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/railway.toml`, `/Users/sarkisharalampiev/Projects/climasite/railway.api.toml`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/railway.toml`, `/Users/sarkisharalampiev/Projects/climasite/Dockerfile.web`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/Dockerfile`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/nginx.conf`
- **Why it matters:** There is no reproducible path from green main to deployed artifact. Whoever deploys must guess which Dockerfile/railway.toml Railway actually uses; editing the wrong duplicate silently changes nothing (or worse, changes prod unexpectedly). No deploy gate means untested images can ship.
- **Recommended fix:** Pick one canonical Dockerfile per service (root `Dockerfile.api` / `Dockerfile.web`), delete `src/ClimaSite.Api/Dockerfile`+`railway.toml`, `src/ClimaSite.Web/Dockerfile` and stale nginx.conf. Add `.github/workflows/deploy.yml` (build images, run migration step, deploy to Railway via railway CLI/API on main or tag), and document the Railway service-to-config mapping in docs/. *(Effort: Medium)*
- **Acceptance criteria:** Exactly one Dockerfile and one Railway config per service in the repo; a deploy workflow exists that only runs after the Test Suite passes; docs name the Railway services and their config paths.
- **Dependencies or follow-up:** Railway dashboard access to confirm which config each service is currently bound to (needs owner).
- **Confidence:** verified — Verifier confirmed every cited fact (both Dockerfiles copy only the template, leaving nginx.conf a dead edit-trap; PROD-104 still TODO) and found no mitigation: the repo's own PRODUCTION_READINESS_CHECKLIST.md:129-130 lists both gaps as open P1 items and line 147 confirms the service-to-config mapping is unknown. P1 correctly calibrated — must-fix before launch, but no confirmed deployment and no security/data-loss exposure.

### 3. EF migrations run at every app startup with no pre-deploy step, no down-tested migrations, no rollback story

- **Finding:** `MigrateAsync()` runs on every application boot with no dedicated pre-deploy migration step, no tested down-migrations, and no tagged version to roll back to.
- **Category:** deployment / data-safety
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:31` `await _context.Database.MigrateAsync()` invoked from Program.cs:32 on every boot. railway.toml/railway.api.toml: restartPolicyType on_failure, max 3 retries — a failed migration crash-loops 3 times against prod data. No migration job in CI (test.yml has none). `git tag` is empty (no prior image/version to roll back to). The repo's own `.claude/skills/deploy-checklist/SKILL.md` requires expand-contract migrations, tested down-migrations, and <5-minute reversibility — none demonstrated. `docs/plans/18-project-completion.md:343-344` (PROD-102) itself says: stop running MigrateAsync() at startup in prod and add a dedicated migrate script as a CD pre-deploy step.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/DataSeeder.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs`, `/Users/sarkisharalampiev/Projects/climasite/.claude/skills/deploy-checklist/SKILL.md`, `/Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md`
- **Why it matters:** Schema changes apply implicitly and irreversibly whenever any instance restarts; with >1 replica two instances can race MigrateAsync; a bad migration takes the site down with no tested way back. This is the classic data-loss scenario the deploy-checklist skill exists to prevent.
- **Recommended fix:** Move MigrateAsync behind an env flag (on for Development/Testing only). Add `scripts/migrate.sh` using `dotnet ef database update` (or a migration bundle) executed as a CD pre-deploy step. Adopt expand-contract for destructive changes and verify `down` for each new migration in CI against a scratch Postgres. *(Effort: Medium)*
- **Acceptance criteria:** Production startup performs zero schema changes; CD logs show an explicit migration step; CI has a job that applies all migrations to a clean Postgres and rolls the latest one back successfully.
- **Dependencies or follow-up:** Deploy workflow (previous finding) must exist to host the pre-deploy step.
- **Confidence:** verified — Verifier confirmed all evidence and found no mitigations (no env flags, migrate scripts, or CD pre-deploy steps anywhere in the repo); the only soft spot is the multi-replica race sub-claim, partially mitigated by EF Core 9's built-in migration locking, which does not affect the core finding. P1 appropriate: real data-safety/deployment risk, but not an active defect.

### 4. No UseForwardedHeaders: per-IP rate limiter degrades to one shared global bucket behind nginx/Railway proxy

- **Finding:** The API never configures forwarded-headers handling, so behind the nginx proxy/Railway edge every end user shares the proxy's `RemoteIpAddress` and all per-IP rate limiters collapse into single shared buckets.
- **Category:** production-readiness / availability
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** grep for ForwardedHeaders/UseForwardedHeaders/KnownProxies across `src/ClimaSite.Api` returns zero hits. `Program.cs:208-217` partitions the global limiter by `context.Connection.RemoteIpAddress` (100 req/min), `Program.cs:220-241` same for auth (10/min) and strict (5/min) policies. In the deployed topology all browser traffic reaches the API via the web container's nginx proxy (`nginx.conf.template:30-46` proxies `/api/` and sets X-Forwarded-For, which ASP.NET ignores without the middleware) and/or Railway's edge — so every end user shares the proxy's RemoteIpAddress. `Program.cs:283` UseHttpsRedirection similarly never sees X-Forwarded-Proto.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/nginx.conf.template`
- **Why it matters:** In production, ~100 total requests per minute across ALL customers triggers site-wide 429s, and the brute-force limiter (10/min shared) lets one user lock out all logins — an availability incident, not just a tuning issue. Client IPs in Serilog request logs are also wrong, hampering incident forensics.
- **Recommended fix:** Add ForwardedHeadersOptions (XForwardedFor | XForwardedProto, with KnownNetworks/KnownProxies cleared or set to Railway's range) and `app.UseForwardedHeaders()` before rate limiting/HTTPS redirection. Verify HttpContext.Connection.RemoteIpAddress reflects the real client behind the proxy. *(Effort: Small)*
- **Acceptance criteria:** A test or manual check behind a proxy shows distinct client IPs partition independently; logged request IPs match X-Forwarded-For; rate-limit E2E/integration test passes with two simulated clients.
- **Dependencies or follow-up:** None.
- **Confidence:** verified — Verifier confirmed all three limiters partition by RemoteIpAddress, rate limiting runs in Production (only the "Testing" env is skipped), and a repo-wide grep finds no UseForwardedHeaders, ForwardedHeadersOptions, or ASPNETCORE_FORWARDEDHEADERS_ENABLED anywhere; the only X-Forwarded-For handling is AuthController.cs:178-179, used solely for security-event logging. P1 correctly calibrated: a few concurrent users cause site-wide 429s and the shared auth bucket enables a trivial global login lockout, yet the fix is small.

### 5. Observability gap: console-only logs, documented X-Correlation-Id convention not implemented, no error tracking/metrics/alerts/runbooks

- **Finding:** Production observability consists solely of console Serilog output: the X-Correlation-Id convention documented in CLAUDE.md is not implemented anywhere, and there is no error tracker, metrics, tracing, alerting, or runbooks.
- **Category:** observability
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `Program.cs:18-24` Serilog writes to Console only; `appsettings.json:43-50` confirms single Console sink. CLAUDE.md API Conventions section documents header `X-Correlation-Id: <guid> # Request tracking`, but grep for "correlation" across src/ matches only `src/ClimaSite.Api/Controllers/TestController.cs` (E2E data cleanup), and `src/ClimaSite.Api/Middleware/` has no correlation middleware. No Sentry/OpenTelemetry/AppInsights/Prometheus packages in any csproj (grep across src). `docs/runbooks/` does not exist. No `Log.CloseAndFlush` in Program.cs (logs lost on crash). The repo's own `.claude/skills/deploy-checklist/SKILL.md` mandates correlation IDs in structured logs, RED metrics, an error tracker, dashboards, and actionable alerts — all absent. Plan 18 PROD-103 (`docs/plans/18-project-completion.md:346`) acknowledges OpenTelemetry is future work.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/appsettings.json`, `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/.claude/skills/deploy-checklist/SKILL.md`
- **Why it matters:** In production on Railway, the only diagnostics are unstructured-ish console lines with no request correlation, no error aggregation, and no alerting — incidents will be detected by customers, and a payment/webhook failure cannot be traced across requests. Documentation actively misleads developers into believing correlation tracking exists.
- **Recommended fix:** Add correlation middleware (read/generate X-Correlation-Id, push to Serilog LogContext, echo on response), switch console output to JSON in Production, wire an error tracker (Sentry SDK is the lowest-effort fit) plus OpenTelemetry traces/metrics per PROD-103, add `Log.CloseAndFlush` in a try/finally, set up at minimum Railway healthcheck-based alerting/uptime monitoring, and either implement or delete the CLAUDE.md header claim. *(Effort: Medium)*
- **Acceptance criteria:** Every API response carries X-Correlation-Id; logs for one request share the ID; a thrown test exception appears in the error tracker; deploy-checklist observability section passes.
- **Dependencies or follow-up:** Choice of error-tracking vendor (user decision per global rules).
- **Confidence:** verified — Verifier confirmed every point: the console sink's output template doesn't even render RequestId despite UseSerilogRequestLogging, there is no appsettings.Production.json to override it, Middleware/ contains only ExceptionHandlingMiddleware, and no observability packages or runbooks exist. P1 not P0 because the health endpoints (/health, /health/ready, /health/live) provide a minimal liveness signal, but live Stripe/webhook flows mean production incidents are currently undiagnosable.

### 6. main branch unprotected and CLAUDE.md mandates direct push to main, contradicting the (better) PR-based practice actually in use

- **Finding:** The `main` branch has no protection rules, while CLAUDE.md's mandatory workflow prescribes direct `git push origin main` — contradicting the PR-based flow actually used recently and leaving the entire CI investment advisory.
- **Category:** dev-workflow / release-process
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** `gh api repos/byrzohod/climasite/branches/main/protection` returns 404 "Branch not protected" (verified live). CLAUDE.md "MANDATORY: Post-Implementation Workflow" step 4: `git push origin main` — direct-to-main, no PR. Actual recent flow uses PRs (gh pr list: PRs #1-#3, two merged 2026-06-06/07), and AGENTS.md:124 prescribes a pre-merge gate (tests, lint, E2E, code+security review). History shows a failing CI run on a main push (run 27071713464, failure, push to main 2026-06-06) — i.e., red commits can and do land on main.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/AGENTS.md`, `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`
- **Why it matters:** Without protection + required checks, the entire CI investment is advisory: anyone (or any agent following CLAUDE.md literally) can push a broken or unreviewed commit straight to the deploy branch. Once Railway auto-deploy is wired to main this becomes a production incident vector.
- **Recommended fix:** Enable branch protection on main: require the five Test Suite checks (unit, integration, frontend, build, e2e), require PRs (1 review or self-merge-after-green for solo work), forbid force-push. Update CLAUDE.md step 4 to "push feature branch, open PR, merge on green". *(Effort: Small)*
- **Acceptance criteria:** Direct push to main is rejected by GitHub; CLAUDE.md and AGENTS.md describe the same branch→PR→checks→merge flow.
- **Dependencies or follow-up:** Repo admin rights (user has them).
- **Confidence:** verified — Verifier confirmed live: the rulesets API additionally returns empty (no hidden mitigation), the cited failing run checks out, and the finding actually understates the issue — 8 of the 10 most recent push-to-main CI runs are failures, so red commits demonstrably land on main while test.yml remains purely advisory. P1 correct: not an active incident, but a demonstrated process gap with a small fix.

### 7. CLAUDE.md and AGENTS.md document a nonexistent TypeScript Playwright E2E workflow and wrong API port

- **Finding:** CLAUDE.md and both AGENTS.md files document a TypeScript Playwright E2E workflow (`npx playwright test`, `fixtures/test-data-factory.ts`) that does not exist — the suite is Microsoft.Playwright for .NET, run via `dotnet test` — and the documented API port (5000) is wrong (5029).
- **Category:** docs / dev-workflow
- **Severity/Priority:** P2 — verification: adjusted (downgraded from P1)
- **Evidence:** CLAUDE.md Commands section: `cd tests/ClimaSite.E2E && npx playwright install / npx playwright test`, file-location table cites `tests/ClimaSite.E2E/fixtures/test-data-factory.ts`, and "Important URLs" lists API at http://localhost:5000. Reality: `tests/ClimaSite.E2E/ClimaSite.E2E.csproj:16` references Microsoft.Playwright 1.49.0 (.NET), the directory has no package.json or fixtures/ (ls output: csproj, Infrastructure, PageObjects, Tests), CI runs `dotnet test tests/ClimaSite.E2E` after `pwsh .../playwright.ps1 install` (test.yml:194-234), and `src/ClimaSite.Api/Properties/launchSettings.json` sets applicationUrl http://localhost:5029. `tests/ClimaSite.E2E/AGENTS.md:77-79` and root AGENTS.md Commands block repeat the npx commands. CLAUDE.md's mandatory "Full Test Suite" command therefore fails at the E2E step.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`, `/Users/sarkisharalampiev/Projects/climasite/AGENTS.md`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/AGENTS.md`, `/Users/sarkisharalampiev/Projects/climasite/tests/ClimaSite.E2E/ClimaSite.E2E.csproj`, `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`
- **Why it matters:** The project's own non-negotiable post-implementation workflow is unexecutable as written; new developers (and AI agents, which this repo heavily relies on) will burn time on npx commands that cannot work and target the wrong API port. Stale instructions in agent-facing docs directly degrade every future automated session.
- **Recommended fix:** Rewrite the E2E sections of CLAUDE.md, AGENTS.md, and tests/ClimaSite.E2E/AGENTS.md to the real flow: build, `pwsh bin/Debug/net10.0/playwright.ps1 install`, start API (ASPNETCORE_ENVIRONMENT=Testing, port 5029) + ng serve, then `dotnet test tests/ClimaSite.E2E` with E2E_BASE_URL/E2E_API_URL/TEST_ADMIN_SECRET. Fix the port references (5029/7008) everywhere. *(Effort: Small)*
- **Acceptance criteria:** Copy-pasting the documented full-test-suite command from CLAUDE.md on a fresh checkout runs all three suites successfully.
- **Dependencies or follow-up:** None.
- **Confidence:** verified — Verifier fully confirmed the evidence (the factory is actually Infrastructure/TestDataFactory.cs) but downgraded P1→P2: docs-only with zero CI/runtime impact, the wrong commands fail fast and obviously, and correct instructions already exist nearby (root AGENTS.md:103 gives the right port; CLAUDE.md's own mandatory post-implementation block uses `dotnet test` for E2E). Real but recoverable doc rot.

### 8. Local dev bootstrap is self-contradictory: appsettings targets project-local compose Postgres (5433) while AGENTS.md and global convention mandate shared-infra (5432); no root README

- **Finding:** The default connection string targets project-local compose Postgres on port 5433 while AGENTS.md and the user's global convention mandate shared-infra Postgres on 5432, and there is no root README to arbitrate between the two contradictory onboarding paths.
- **Category:** dev-workflow
- **Severity/Priority:** P2 — verification: adjusted (downgraded from P1)
- **Evidence:** `src/ClimaSite.Api/appsettings.json:3` default connection `Host=localhost;Port=5433;...Password=climasite_dev_password` matches project-local `docker-compose.yml:10` ("5433:5432") with postgres+redis+minio+pgadmin services. AGENTS.md:120-122 says: use shared infra at ~/Projects/shared-infra, NOT project-local compose; shared Postgres on localhost:5432, DB name climasite — and the user's global CLAUDE.md forbids project-local compose for these services. So a dev following AGENTS.md (shared infra up) gets a connection refused on 5433 from plain `dotnet run` unless they know to export `ConnectionStrings__DefaultConnection`; a dev using the repo compose works but violates the stated convention. There is no root README (`ls README*` → none) to arbitrate.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/appsettings.json`, `/Users/sarkisharalampiev/Projects/climasite/docker-compose.yml`, `/Users/sarkisharalampiev/Projects/climasite/AGENTS.md`, `/Users/sarkisharalampiev/Projects/climasite/.codex/PROJECT_MEMORY.md`
- **Why it matters:** Clone-to-running currently requires tribal knowledge; the two documented paths contradict each other and the user's own infrastructure convention. Every onboarding (human or agent session) re-derives this, and the unused MinIO/pgadmin compose services add confusion about what production storage actually is.
- **Recommended fix:** Decide one default (per global rules: shared-infra). Point appsettings.json Development connection at localhost:5432/climasite, move MinIO into shared-infra if still needed, then delete or clearly mark docker-compose.yml as CI-only/legacy. Add a root README with prerequisites, shared-infra startup, seed users (admin@climasite.local in dev), Stripe test config, and the real test commands. *(Effort: Small)*
- **Acceptance criteria:** Fresh clone + documented steps yields running API+frontend without editing config; docker-compose.yml is removed or its purpose explicitly documented; README exists.
- **Dependencies or follow-up:** User decision on keeping project-local compose vs shared-infra (convention says shared-infra).
- **Confidence:** verified — Verifier confirmed the contradiction in full but downgraded P1→P2: AGENTS.md:123 and PROJECT_MEMORY.md:12 already document the ConnectionStrings__DefaultConnection override (with SSL Mode=Disable) for the shared-infra path, so the workaround lives in the canonical agent-facing docs and both setup paths do produce a working environment — recoverable local-dev friction with no effect on CI, tests (Testcontainers), or production.

### 9. CI lacks lint, analyzers, docker-build verification, dependency/security scanning, coverage gates, and concurrency control

- **Finding:** The only workflow (test.yml) runs tests and a build but enforces none of the project's other declared quality gates: no lint, no analyzers, no Docker image build check, no dependency/vulnerability scanning, no coverage thresholds, and no concurrency control.
- **Category:** ci
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** Full read of `.github/workflows/test.yml`: jobs are unit-tests, integration-tests, frontend-tests, build, e2e-tests, test-summary — none runs `ng lint` although `src/ClimaSite.Web/package.json:11` defines it (and eslint+angular-eslint are devDependencies); no dotnet format/analyzer gate; Dockerfile.api/Dockerfile.web are never built in CI so image breakage ships undetected; no dependabot.yml or audit step (find .github → only test.yml); coverage uploads to codecov are continue-on-error (test.yml:38-43) with no threshold despite CLAUDE.md's 80%/70% coverage policy; no `concurrency:` group (parallel pushes waste ~15 min runners); no NuGet caching (three jobs restore from cold). CI triggers reference a `develop` branch (test.yml:5,7) that doesn't exist (`git branch -a`).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/package.json`
- **Why it matters:** Quality rules that the project declares mandatory (ESLint clean, coverage minimums, deployable Docker images) are unenforced, so they will drift; missing dependency scanning leaves Stripe/JWT-handling code exposed to known CVEs silently.
- **Recommended fix:** Add jobs: lint (`ng lint` + `dotnet format --verify-no-changes`), docker-build (docker build both Dockerfiles, optionally Trivy scan), and dependency audit (`npm audit --audit-level=high`, `dotnet list package --vulnerable`); add dependabot.yml; add `concurrency: { group: ci-${{ github.ref }}, cancel-in-progress: true }`; add actions/cache for NuGet; enforce coverage thresholds or drop the claim from CLAUDE.md. *(Effort: Medium)*
- **Acceptance criteria:** A PR introducing a lint error, a vulnerable package, or a broken Dockerfile fails CI.
- **Dependencies or follow-up:** Branch protection finding (the new checks should become required).
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

### 10. Inconsistent secret-injection conventions: Stripe/SMTP/MinIO only read config-section keys while docs promise flat env vars; non-empty dummy keys defeat fail-fast

- **Finding:** JWT/DB/Redis read flat env vars while Stripe/SMTP/MinIO read only config-section keys, contradicting CLAUDE.md's documented flat env-var names; non-empty dummy Stripe keys in appsettings.json defeat the fail-fast check, so a deploy configured per the docs silently runs with dummy keys.
- **Category:** configuration / production-readiness
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** JWT/DB/Redis read flat env vars directly (`JwtConfiguration.cs:13` JWT_SECRET; `DependencyInjection.cs:21` DATABASE_URL, `:54` REDIS_URL). But `StripePaymentService.cs:16` reads only `configuration["Stripe:SecretKey"]`, `WebhooksController.cs:51` "Stripe:WebhookSecret", `EmailService.cs:83` "Email:SmtpHost", and MinIO defaults to localhost:9000 with hardcoded creds climasite/climasite_minio_secret (`DependencyInjection.cs:74-81`). CLAUDE.md's Environment Variables table documents STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET, SMTP_HOST etc. — flat names that the .NET config provider will NOT map to those section keys (needs `Stripe__SecretKey`). `appsettings.json:16-20` ships non-empty dummy keys (sk_test_51DummyKey...), so the fail-fast at `StripePaymentService.cs:18-21` never fires: a Railway deploy configured per the docs would silently run with dummy Stripe keys until the first checkout fails.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Services/StripePaymentService.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/WebhooksController.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/DependencyInjection.cs`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/appsettings.json`, `/Users/sarkisharalampiev/Projects/climasite/CLAUDE.md`
- **Why it matters:** The first production deploy is highly likely to be misconfigured for payments/email/storage because the documented variable names don't reach the code, and dummy fallbacks mask the error until a customer hits checkout — a revenue-impacting silent failure mode.
- **Recommended fix:** Standardize: either read the flat env names in code (matching CLAUDE.md), or document the double-underscore names (`Stripe__SecretKey`, `Email__SmtpHost`, `Minio__Endpoint`...). Remove dummy Stripe keys from appsettings.json (keep them only in a Testing config) and add a Production startup check that fails fast when Stripe/SMTP/MinIO config is placeholder/missing. *(Effort: Small)*
- **Acceptance criteria:** Booting in Production without real Stripe config throws at startup; the env-var table in docs matches variable names proven by an integration test or startup validation.
- **Dependencies or follow-up:** Overlaps Plan 18 Phase 5 (security hardening) scope.
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

### 11. Docker hardening gaps: both containers run as root; web container's API_URL silently defaults to localhost

- **Finding:** Neither container drops root privileges, and the web container's entrypoint silently defaults API_URL to http://localhost:8080, so a misconfigured web service passes all health checks while every `/api` call fails.
- **Category:** deployment / security
- **Severity/Priority:** P2 (Medium) — verification: unverified (P2/P3)
- **Evidence:** No USER directive in Dockerfile.api, Dockerfile.web, or src/ClimaSite.Web/Dockerfile (grep verified) — API runs as root, nginx master as root. `docker-entrypoint.sh:8` defaults `API_URL="${API_URL:-http://localhost:8080}"`, so a Railway web service missing the API_URL var starts "healthy" (nginx /health returns static 200, nginx.conf.template:61-65; railway.toml healthcheckPath=/health) while every /api call proxies to localhost:8080 inside the container and fails — a misconfiguration that passes all health checks. Dockerfile.api does have a HEALTHCHECK (lines 43-44) and .dockerignore exists (good). Plan 18 PROD-100 (docs/plans/18-project-completion.md:334) already targets non-root hardening.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/Dockerfile.api`, `/Users/sarkisharalampiev/Projects/climasite/Dockerfile.web`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/docker-entrypoint.sh`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/nginx.conf.template`
- **Why it matters:** Root containers widen the blast radius of any RCE in the API or nginx; the silent API_URL fallback turns a one-variable misconfiguration into a fully "green" but completely broken storefront.
- **Recommended fix:** Add non-root users (USER app with `adduser` in final stages; use nginx-unprivileged or drop privileges for web). In docker-entrypoint.sh, fail hard (exit 1 with clear message) when API_URL is unset outside local dev, or at least log a prominent warning and make the nginx /health return 503 until API_URL is set. *(Effort: Small)*
- **Acceptance criteria:** `docker run` images show non-root PID 1 (`whoami` != root); starting the web image without API_URL exits with a clear error instead of serving a broken proxy.
- **Dependencies or follow-up:** PROD-100 task in Plan 18.
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

### 12. Release process exists only on paper: no tags ever cut, CHANGELOG perpetually Unreleased, release/deploy-checklist skills unexercised

- **Finding:** No release has ever been tagged: `git tag` is empty, CHANGELOG.md has only a perpetual "[Unreleased]" section, and the repo's own release and deploy-checklist skills have never been exercised.
- **Category:** release-process
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `git tag` returns empty; CHANGELOG.md has a single "## [Unreleased]" section and claims SemVer adherence with zero versions; `.claude/skills/release/SKILL.md` (semver tagging, changelog from conventional commits, rollback verification) and deploy-checklist skill have never been run against this repo (no tags, no runbooks, no release artifacts). `docs/plans/18-project-completion.md` Phase 8 targets a v1.0.0 tag as future work. Commit hygiene is mixed: conventional prefixes mostly used, but commits are huge multi-feature batches (e.g., 5166b6d "complete home v3 merge readiness", b2920b7 bundles four features) and one "[codex]"-prefixed commit breaks the convention (a065dde).
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/CHANGELOG.md`, `/Users/sarkisharalampiev/Projects/climasite/.claude/skills/release/SKILL.md`, `/Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md`
- **Why it matters:** Without any tagged version there is no rollback target, no way to correlate a deployed artifact with source, and the rollback requirement in the project's own deploy checklist ("previous version's artifacts still available") is unsatisfiable. The changelog discipline exists but produces nothing consumable.
- **Recommended fix:** Cut a v0.x pre-release tag now from main (proves the release skill works), wire image tagging to git tags in the future deploy workflow, and roll [Unreleased] into versioned sections at each release per the existing skill. *(Effort: Small)*
- **Acceptance criteria:** At least one annotated tag exists with a matching CHANGELOG section; the deploy workflow publishes an image labeled with that tag.
- **Dependencies or follow-up:** Deploy workflow for image tagging; otherwise standalone.
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

### 13. Dead/duplicate frontend environment file: environment.prod.ts is never used by any build configuration

- **Finding:** `environment.prod.ts` is referenced by no angular.json build configuration (production builds use `environment.production.ts`), leaving a dead, independently-editable duplicate that was already flagged in a prior validation report.
- **Category:** configuration
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** angular.json build configurations (parsed): production replaces environment.ts with environment.production.ts; e2e with environment.e2e.ts; no configuration references environment.prod.ts. Both prod files currently coincide (production:true, apiUrl:'') but are independently editable — `src/ClimaSite.Web/src/environments/environment.prod.ts` vs `environment.production.ts`. The 2026-01-24 validation report (`docs/validation/areas/18-build-cicd-deployment.md`, "Two production environment files", High) flagged this and it is still unfixed.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/environments/environment.prod.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/environments/environment.production.ts`, `/Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/angular.json`
- **Why it matters:** Someone "fixing" prod config in environment.prod.ts changes nothing shipped; classic trap that has already been flagged once and ignored.
- **Recommended fix:** Delete environment.prod.ts. *(Effort: Small)*
- **Acceptance criteria:** Only environment.ts / environment.e2e.ts / environment.production.ts remain; production build unchanged.
- **Dependencies or follow-up:** None.
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

### 14. Prior CI/CD validation report is partially stale and should be re-baselined

- **Finding:** The 2026-01-24 CI/CD validation report still lists as Critical/High several gaps that have since been resolved (no E2E in CI, Application tests missing, no ESLint), while its still-true claims remain valid — the report needs re-baselining.
- **Category:** docs
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** `docs/validation/areas/18-build-cicd-deployment.md` (generated 2026-01-24) lists as Critical/High: "No E2E tests in CI", "Application tests not in CI", "No ESLint configured" — all three are now false (test.yml:36 runs ClimaSite.Application.Tests; test.yml:134-252 full E2E job; package.json:11 + @angular-eslint devDependencies present). Its still-true claims (no deploy workflow, no scanning, no branch protection, no release tagging, dual prod env files, duplicated URL-conversion logic in Program.cs:150-176 vs DependencyInjection.cs:96-135) match independent verification.
- **Affected files/areas:** `/Users/sarkisharalampiev/Projects/climasite/docs/validation/areas/18-build-cicd-deployment.md`, `/Users/sarkisharalampiev/Projects/climasite/.github/workflows/test.yml`
- **Why it matters:** Stale audit docs cause agents and reviewers to re-fix solved problems or trust outdated severity calls; this repo's workflow leans heavily on docs as agent memory.
- **Recommended fix:** Update the report's gap tables (mark resolved items with date/commit), or add a "superseded by" banner pointing to the current review. Also fold in the still-open duplicated Postgres/Redis URL-conversion helpers (extract to one shared utility). *(Effort: Small)*
- **Acceptance criteria:** Report reflects current CI reality; duplicated URL converters consolidated or ticketed.
- **Dependencies or follow-up:** None.
- **Confidence:** verified (reviewer); not independently re-verified (P2/P3 tier).

## Dimension data

### Extras — Dev Workflow / Deployment / Operations

#### 1. CI coverage map (.github/workflows/test.yml — the only workflow)

| Concern | Status | Evidence |
|---|---|---|
| Backend unit tests (Core, Application) | YES, with cobertura collection | test.yml:32-36 |
| Backend integration tests (Api.Tests vs Postgres 16 service) | YES | test.yml:45-81 |
| Frontend unit tests (Karma headless, i18n check via `npm test` → test:i18n) | YES | test.yml:83-103; package.json scripts |
| Release build verification (dotnet Release + ng build) | YES | test.yml:105-132 |
| E2E (.NET Playwright vs live API:5029 + ng serve:4200 + Postgres + Redis) | YES, green on latest main run (27083766101) | test.yml:134-252; `gh run list` |
| Final gate aggregation | YES (test-summary fails if any job failed) | test.yml:254-271 |
| Lint (ng lint / dotnet format) | NO — script exists, never run in CI | package.json:11 |
| .NET analyzers / format gate | NO | — |
| Docker image build check | NO | — |
| Dependency / vulnerability scanning (npm audit, dotnet vulnerable, Trivy, dependabot, CodeQL) | NO | .github contains only test.yml |
| Coverage thresholds (CLAUDE.md claims 80%/70% mandatory) | NO — codecov upload is continue-on-error, no token | test.yml:38-43 |
| Concurrency cancellation / NuGet caching | NO (npm cache only) | test.yml:94,122,177 |
| Deploy workflow | NO — Plan 18 PROD-104 still open | docs/plans/18-project-completion.md:352-359 |
| Trigger oddity | references nonexistent `develop` branch | test.yml:5,7 |
| Secrets usage | none required; E2E uses inline test secrets (fine) | test.yml:212-215 |

#### 2. Production-launch blocker list (ordered)

1. **P0** Hardcoded admin `admin@climasite.local` / `Admin123!` + demo catalog seeded on production startup (Program.cs:32, DataSeeder.cs:64-65).
2. **P1** No forwarded-headers handling → global shared rate-limit bucket behind proxy (Program.cs:208-241) → site-wide 429s / login lockout.
3. **P1** Migrations at startup, no pre-deploy migration step, no rollback target (no tags, no down-tests) (DataSeeder.cs:31).
4. **P1** No CD pipeline + ambiguous Railway config (3 railway.toml, drifting duplicate Dockerfiles).
5. **P1** Observability floor: console-only Serilog, no correlation IDs (despite CLAUDE.md claim), no Sentry/OTel, no alerts, no runbooks, no uptime monitoring.
6. **P2** Stripe/SMTP/MinIO env-var naming mismatch + non-empty dummy keys → silent payment misconfiguration on first deploy.
7. **P2** Root containers; web container reports healthy with unset API_URL (broken proxy).
8. **P2** main unprotected (verified via GitHub API) while docs mandate direct push to main.
9. **Needs-confirmation** DB backups: presumably Railway-managed Postgres snapshots — nothing in repo configures or documents backup/restore; confirm in Railway dashboard and document an RPO/RTO + restore drill.
10. **Needs-confirmation** Which Railway services exist and which config file each is bound to (root railway.toml vs railway.api.toml vs src/ClimaSite.Api/railway.toml); whether auto-deploy from main is currently enabled.

#### 3. Recommended standard workflow

```
branch (feature/x | fix/x)
  → conventional commits, small scope (avoid current multi-feature mega-commits)
  → push → PR to main (never direct push; fix CLAUDE.md step 4)
  → required checks: unit / integration / frontend / build / e2e  (+ new: lint, docker-build, audit)
  → code-review + security-review skills per AGENTS.md:124
  → squash-merge on green (branch protection enforces)
  → release: annotated semver tag via /release skill → CHANGELOG section rolled
  → deploy.yml on tag (or main): build & tag images → run scripts/migrate.sh against prod DB (expand-contract) → Railway deploy → verify /health + smoke E2E → /deploy-checklist post-deploy section
  → rollback = redeploy previous image tag + (if needed) tested down-migration
```

#### 4. Environment/config matrix (verified resolution order)

| Setting | Env var actually read | Config fallback | Prod fail-fast? |
|---|---|---|---|
| DB | `DATABASE_URL` (URL→Npgsql converter, .railway.internal → SSL off) | ConnectionStrings:DefaultConnection (localhost:5433!) | throws only if both missing |
| Redis | `REDIS_URL` (same converter pattern) | ConnectionStrings:Redis → "localhost:6379" | NO — silent localhost fallback |
| JWT secret | `JWT_SECRET` | JwtSettings:Secret | YES in Production (JwtConfiguration.cs:22-24) |
| JWT issuer/audience | `JWT_ISSUER` / `JWT_AUDIENCE` | config → localhost defaults | NO |
| Stripe | none (only `Stripe__SecretKey` would map) | appsettings dummy sk_test_… | NO — dummy key passes the null check |
| SMTP | none (Email:* section keys) | appsettings smtp.example.com | throws on first send only |
| MinIO | none (Minio:* section keys) | localhost:9000 + hardcoded creds | NO |
| Frontend API URL | `API_URL` (nginx proxy, entrypoint) | localhost:8080 default | NO — health stays green |
| CORS | AllowedOrigins config (localhost-only in appsettings.json:28-33) | http://localhost:4200 | NO (mitigated by same-origin /api proxy) |

Duplication note: Postgres/Redis URL converters exist twice (Program.cs:150-176 for health checks; DependencyInjection.cs:96-135 for the app) — drift risk.

#### 5. Doc-vs-reality dispositions

| Claim | Source | Verdict |
|---|---|---|
| E2E = TypeScript Playwright, `npx playwright test`, fixtures/test-data-factory.ts | CLAUDE.md, AGENTS.md:107-109, tests/ClimaSite.E2E/AGENTS.md:77-79 | FALSE — Microsoft.Playwright .NET, `dotnet test` (csproj:16, test.yml:228) |
| API at http://localhost:5000 | CLAUDE.md Important URLs | FALSE — 5029 (launchSettings.json) |
| `X-Correlation-Id` request-tracking header | CLAUDE.md API conventions | NOT IMPLEMENTED (grep: only TestController) |
| STRIPE_SECRET_KEY / SMTP_HOST env vars | CLAUDE.md env table | NOT WIRED — code reads config-section keys only |
| "use shared-infra, not project-local compose" | AGENTS.md:121, global rules | CONTRADICTED by repo docker-compose.yml + appsettings 5433 default |
| Validation report: "No E2E in CI / no ESLint / Application tests missing" | docs/validation/areas/18 (2026-01-24) | STALE — all three resolved since |
| Plan 18 Phase 7 production-readiness items open | docs/plans/18:332-371 | ACCURATE — matches independent findings (PROD-100..107) |
| Coverage minimums 80%/70% "mandatory" | CLAUDE.md | UNENFORCED — no CI threshold |

#### 6. Open questions for the owner

1. Is Railway auto-deploy currently connected to this repo, and which services/config files? (Determines whether the P0 seeding issue is already live.)
2. Has the production database ever been provisioned/seeded? If yes, rotate/delete `admin@climasite.local` immediately.
3. What is production object storage — Railway MinIO service, external S3, or none? (MinIO config defaults to localhost.)
4. Are Railway Postgres backups enabled, and has a restore ever been tested?
5. Should `develop` be removed from CI triggers, or is a develop branch planned?

## Refuted during verification

None.
