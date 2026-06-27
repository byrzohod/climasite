# ClimaSite — Standard Development Workflow

**Date:** 2026-06-11 (branch-protection + release-flow corrections 2026-06-21)
**Status:** **Canonical — the single source of truth for the development workflow.** Where this document disagrees with CLAUDE.md, AGENTS.md, or any `.claude/skills/*/SKILL.md`, **this document wins** and the other file must be corrected to match. The earlier command/port/direct-push errors those files carried were fixed in DOC-01 (2026-06-16) and the Wave 0 governance pass (2026-06-21); the original findings remain on record in `docs/project-plan/_review/devops.md`, `_review/testing.md`, and `_review/docs.md`.

**How to use this document:** This is the day-to-day operating manual for any developer or AI agent working on ClimaSite. Read "Local setup" once per machine, follow "Day-to-day workflow" for every change, and treat "Testing requirements before merge" and "Definition of Done" as hard gates. All commands below were verified against the actual codebase on 2026-06-11 (the E2E suite is **Playwright for .NET run via `dotnet test`** — not the TypeScript `npx playwright` workflow that older docs describe, and the API runs on port **5029**, not 5000). Anything not directly verifiable is marked "Needs confirmation".

---

## 1. Local setup

### 1.1 Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.x | All backend + E2E projects target `net10.0` |
| Node.js + npm | 20+ | Angular 19 |
| Docker Desktop | current | Required for integration tests (Testcontainers) and local infra |
| PowerShell (`pwsh`) | 7+ | Required once to install Playwright browsers for the .NET E2E suite |

### 1.2 Infrastructure: shared-infra is the convention

Local dev uses the **shared stack** at `~/Projects/shared-infra/docker-compose.yml` (Postgres on `5432`, Redis on `6379`, plus Meilisearch/MinIO/RabbitMQ/MailHog). Do **not** use the repo-local `docker-compose.yml` for these services — it starts a second Postgres on **5433** and contradicts the convention (see `_review/devops.md` finding 8; the repo-local compose file is slated for deletion or explicit CI-parity documentation).

```bash
cd ~/Projects/shared-infra && docker compose up -d postgres redis
```

**Known trap (P2, open):** `src/ClimaSite.Api/appsettings.json` still defaults `ConnectionStrings:DefaultConnection` to the *project-local* Postgres (`Host=localhost;Port=5433;Username=climasite;Password=climasite_dev_password`). Until that default is repointed to 5432, you must override the connection string when using shared-infra (next step).

### 1.3 Run the API (port 5029)

```bash
# From repo root. Override the connection string to hit shared-infra Postgres.
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=climasite;Username=<shared-infra-user>;Password=<shared-infra-password>;SSL Mode=Disable" \
dotnet run --project src/ClimaSite.Api
```

- API: **http://localhost:5029** (HTTPS profile: 7008). Swagger: http://localhost:5029/swagger. The port-5000 references in CLAUDE.md are wrong (`launchSettings.json` is the source of truth).
- Use `ConnectionStrings__DefaultConnection` with `SSL Mode=Disable` for localhost — do **not** use `DATABASE_URL` locally, because the app's URL converter forces SSL for non-Railway hosts (`.codex/PROJECT_MEMORY.md`).
- Shared-infra Postgres credentials: defined in `~/Projects/shared-infra/docker-compose.yml` — **Needs confirmation** (not stored in this repo; check that file).
- Redis falls back to `localhost:6379` automatically, which matches shared-infra.

**What startup does (be aware):** `Program.cs` unconditionally runs `DataSeeder`, which (a) applies EF migrations (`MigrateAsync`) and (b) seeds roles, an admin user, and a demo catalog when tables are empty. Locally this is convenient; **in Production it is a verified P0 security hole** (hardcoded `admin@climasite.local` / `Admin123!` — see `_review/devops.md` finding 1). Never point a Production-configured boot at a real database until seeding is environment-gated.

### 1.4 Seed data / dev logins

| Account | Credentials | Source |
|---|---|---|
| Admin (dev only) | `admin@climasite.local` / `Admin123!` | `src/ClimaSite.Infrastructure/Data/DataSeeder.cs:64-65` |

Demo categories/brands/products are seeded automatically when product tables are empty. There is **no documented way to seed a specific test dataset** outside the E2E `TestDataFactory` — gap, P2. (For test data specifically: when the API runs with `ASPNETCORE_ENVIRONMENT=Testing`, `TestController` exposes `/api/test/*` endpoints gated by `TestSettings:AdminSecret`, and `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs` uses them to create users/products/orders with correlation-ID cleanup — that is the only sanctioned data-seeding mechanism beyond `DataSeeder`.)

### 1.5 Run the frontend (port 4200)

```bash
cd src/ClimaSite.Web
npm install
npm start          # ng serve → http://localhost:4200
```

Dev environment (`src/environments/environment.ts`) points `apiUrl` at `http://localhost:5029` directly — no proxy config needed locally. (Production uses relative `apiUrl: ''` through the nginx `/api` proxy; never change that — see `.codex/PROJECT_MEMORY.md`.)

### 1.6 One-time E2E browser install

```bash
dotnet build tests/ClimaSite.E2E
pwsh tests/ClimaSite.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

### 1.7 Stripe / email test configuration — explicit gaps

- **Stripe (SEC-07):** the code reads **config-section keys only** — `Stripe:SecretKey` in `StripePaymentService.cs`, `Stripe:WebhookSecret` in `WebhooksController.cs`. Set them via the **double-underscore** env (`Stripe__SecretKey` / `Stripe__PublishableKey` / `Stripe__WebhookSecret`) or `appsettings.Development.json` — the flat `STRIPE_SECRET_KEY` documented in older CLAUDE.md does **not** reach the code. As of SEC-07 `appsettings.json` ships **no Stripe keys** (empty), and **Production fail-fasts at startup** if they're missing/placeholder/non-Stripe-shaped (`StripeConfiguration`); in Dev/Testing the payment service is constructed lazily, and integration tests use `FakePaymentService`. Local test-mode setup (a `pk_test`/`sk_test` pair + `stripe listen` webhook forwarding) is still undocumented — minor gap.
- **Email:** `EmailService` reads `Email:*` section keys, defaults to `smtp.example.com`. Shared-infra provides **MailHog** for local SMTP capture, but no ClimaSite doc wires it up — gap, P2. Note the forgot-password flow currently never sends email at all (P1 bug, `_review/status.md` finding 2).
- **MinIO:** defaults to `localhost:9000` with hardcoded dev creds; production object-storage story is **Needs confirmation**.

### 1.8 Database migrations

```bash
cd src/ClimaSite.Infrastructure
dotnet ef migrations add <Name> --startup-project ../ClimaSite.Api
dotnet ef database update --startup-project ../ClimaSite.Api
```

(Verified 2026-06-11: `dotnet ef migrations list --startup-project ../ClimaSite.Api` works from `src/ClimaSite.Infrastructure`; the `--startup-project` flag is **required** because no `IDesignTimeDbContextFactory` exists in Infrastructure — CLAUDE.md's command, which omits the flag, fails.) Use the `/db-migrate` skill (`.claude/skills/`) for the expand-contract procedure. Remember migrations also auto-apply at API startup today (a P1 deploy-safety issue tracked as Plan 18 PROD-102 — do not rely on this behavior in anything production-shaped).

---

## 2. Day-to-day workflow

### 2.1 Branching

- `main` = integration branch (and future deploy branch). **Never push directly to main; every change ships as a feature-branch PR.** Branch protection is now **enabled** on GitHub (verified 2026-06-21 via `gh api repos/byrzohod/climasite/branches/main/protection`): the six Test Suite checks (Unit Tests, Integration Tests, Frontend Unit Tests, Build Verification, E2E Tests, Test Summary) are **required**, `enforce_admins=true` (admins are not exempt), force-pushes and branch deletion are **blocked**, and conversation-resolution is required. Required approving reviews = **0**, so AI auto-merge on green CI still works (the review gate is enforced by the `reviewer` agent + PR template + CI danger check, not a GitHub approval). A direct `git push origin main` is rejected server-side — open a PR.
- Branch names: `feature/<description>`, `fix/<description>` (e.g. `feature/plan18-wishlist-completion`).
- CI triggers are push/PR to `main` (and a nonexistent `develop`) — **feature-branch pushes get no CI until a PR exists. Open the PR early (draft is fine)** so every push is tested.

### 2.2 Commits

- Conventional commits: `feat:` / `fix:` / `refactor:` / `docs:` / `test:` / `chore:` (the release changelog is generated from these).
- Keep commits **small and single-purpose**. The current history's multi-feature mega-commits (e.g. `5166b6d`) and the `[codex]`-prefixed commit are anti-patterns flagged in `_review/devops.md` finding 12 — they break changelog generation and bisection.
- No secrets in git, ever.

### 2.3 Test commands — the correct ones (ground truth from `_review/testing.md`)

> **Critical correction:** the E2E suite is **xUnit + Microsoft.Playwright for .NET** (`tests/ClimaSite.E2E/ClimaSite.E2E.csproj`). There is no `package.json`, no `playwright.config.ts`, no `fixtures/test-data-factory.ts`. Every `npx playwright …` command in CLAUDE.md, AGENTS.md, and the release skill is wrong. Also: **do not run bare `dotnet test` at the repo root** — `ClimaSite.sln` includes the E2E project, so root `dotnet test` attempts 208 browser tests without servers and produces ~200 false failures.

**Fast inner loop (run constantly while coding):**

```bash
# Backend unit tests for the layer you touched
dotnet test tests/ClimaSite.Core.Tests/ClimaSite.Core.Tests.csproj
dotnet test tests/ClimaSite.Application.Tests/ClimaSite.Application.Tests.csproj
# narrow further:  --filter "FullyQualifiedName~Wishlist"

# Frontend unit tests (npm test also runs the i18n key check first)
cd src/ClimaSite.Web && npm test -- --watch=false --browsers=ChromeHeadless
```

**Integration tests (needs Docker running — Testcontainers spins its own Postgres/Redis; NEVER uses shared-infra):**

```bash
dotnet test tests/ClimaSite.Api.Tests/ClimaSite.Api.Tests.csproj
```

**Lint + build (before every PR):**

```bash
cd src/ClimaSite.Web && npm run lint && npm run build
dotnet build   # 0 warnings expected
```

**E2E (Playwright for .NET — needs both servers running):**

```bash
# Terminal 1 — API in Testing env (the checked-in launch profile forces Development,
# which enables rate limiting and throttles the browser suite):
ASPNETCORE_ENVIRONMENT=Testing \
TestSettings__AdminSecret=test-admin-secret \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=climasite;Username=<user>;Password=<pw>;SSL Mode=Disable" \
dotnet run --project src/ClimaSite.Api --no-launch-profile --urls http://localhost:5029

# Terminal 2 — frontend:
cd src/ClimaSite.Web && npm start

# Terminal 3 — the suite:
E2E_BASE_URL=http://localhost:4200 \
E2E_API_URL=http://localhost:5029 \
TEST_ADMIN_SECRET=test-admin-secret \
dotnet test tests/ClimaSite.E2E
# narrow: dotnet test tests/ClimaSite.E2E --filter "FullyQualifiedName~Wishlist"
```

E2E rules remain as in CLAUDE.md and they are correct: no mocking, self-contained data via `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs` (C#, not .ts), correlation-ID cleanup, `data-testid` on all interactive elements.

**When to run what:**

| Moment | Run |
|---|---|
| While coding | Unit tests for the touched project + relevant frontend specs |
| Before pushing | Touched-layer unit tests + `npm run lint` |
| Before marking a slice done / requesting review | Full pre-merge suite (section 4) |
| After any UI change | E2E for the affected area + manual check in both themes, EN/BG/DE |

---

## 3. PR & review expectations

1. **One PR per slice/phase**, branched from `main`, opened early so CI runs on every push.
2. CI (`.github/workflows/test.yml`) must be fully green: `unit-tests`, `integration-tests`, `frontend-tests`, `build`, `e2e-tests`, `test-summary`. CI is the source of truth for test evidence — local TRX snapshots go stale (this bit the wishlist branch; `_review/testing.md` finding 11).
3. Run the **`/code-review`** skill on the diff, and **`/security-review`** when the change touches auth, payments, uploads, tokens, headers, or config (per `AGENTS.md:124`).
4. Run **`/ui-qa`** when UI changed (per `.codex/PROJECT_MEMORY.md` merge-readiness list).
5. PR description: what/why, test evidence (CI link), screenshots for UI (light + dark), and any new env vars or migrations called out explicitly.
6. Keep pre-existing debt out of scope: report it, don't silently bundle fixes ("Keep unrelated existing debt separate from issues introduced by the current branch" — `.codex/PROJECT_MEMORY.md`).
7. Squash-merge on green. Known CI blind spots you must cover manually until fixed (P2, `_review/testing.md` finding 7): CI runs **no lint job** and enforces **no coverage threshold** — run `npm run lint` yourself; coverage numbers in CLAUDE.md (80%/70%) are currently aspirational, not enforced.

## 4. Testing requirements before merge

All of the following, in this order (mirrors `.codex/PROJECT_MEMORY.md` "Merge Readiness", with correct commands):

```bash
dotnet test tests/ClimaSite.Core.Tests/ClimaSite.Core.Tests.csproj
dotnet test tests/ClimaSite.Application.Tests/ClimaSite.Application.Tests.csproj
dotnet test tests/ClimaSite.Api.Tests/ClimaSite.Api.Tests.csproj        # Docker required
cd src/ClimaSite.Web && npm test -- --watch=false --browsers=ChromeHeadless
cd src/ClimaSite.Web && npm run lint
cd src/ClimaSite.Web && npm run build
# E2E per section 2.3 against real servers on :4200 / :5029
dotnet test tests/ClimaSite.E2E
```

Plus, for new code (CLAUDE.md policy, still valid):

- Unit tests for every new backend handler/service (use the `tests/ClimaSite.Application.Tests/TestHelpers/MockDbContext.cs` pattern).
- Integration tests for every new/changed API endpoint (incl. 401/403 cases) using `tests/ClimaSite.Api.Tests/Infrastructure/TestWebApplicationFactory.cs`.
- At least one E2E happy path per new user-facing flow.
- Do not add to the known gaps: Payments/Webhooks/Orders/GDPR currently have **zero** integration tests and the Stripe card path has **zero** end-to-end coverage (P1 backlog, `_review/testing.md` findings 2–5) — if you touch those areas, tests are part of the change, not optional.

## 5. Documentation update rules

Update docs **in the same PR** as the change. Single-source-of-truth rules (from `_review/docs.md` §D):

| When you… | You must update |
|---|---|
| Ship any user- or developer-visible change | `CHANGELOG.md` `[Unreleased]` (one bullet per change — the wishlist slice missed this; don't repeat it) |
| Complete/advance a feature or plan phase | CLAUDE.md status table + the plan's **Status header** in `docs/plans/` (per-task checkboxes are explicitly *not* maintained status), + `.codex/PROJECT_MEMORY.md` if workflow-relevant |
| Make a non-obvious technical/architectural decision | New ADR in `docs/adr/` (never edit an accepted ADR's decision — write a superseding one; this rule was already violated once by ADR 002) |
| Change commands, ports, env vars, or workflow | CLAUDE.md (canonical for conventions) and regenerate/patch the affected `AGENTS.md` snapshot — they currently drift and must not be left contradicting each other |
| Add an env var or config key | CLAUDE.md env table **using the name the code actually reads** (section keys need `__` form, e.g. `Stripe__SecretKey`) |
| Finish a plan | Move it to `docs/plans/archive/` with a "superseded by" banner — no third state |
| Find a stale/contradicting doc | Fix it or add a dated "superseded by …" banner; never silently leave two answers in the repo |

The active master plan is `docs/plans/18-project-completion.md`. `docs/plans/00-master-overview.md`, the `docs/validation/` suite (except `areas/07-wishlist.md`), `docs/plans/19-*`/`20-*`/most `21-*`, and `docs/tech-stack.md` are **stale** (verified in `_review/docs.md`) — do not treat them as current status.

## 6. Release process

There has **never been a release**: `git tag` is empty, `CHANGELOG.md` has only `[Unreleased]`, and no deploy workflow exists (`.github/` contains only `test.yml`; deploy is Plan 18 PROD-104). The `/release` and `/deploy-checklist` skills are the intended process and are mostly sound, with these **corrections and unmet preconditions**:

1. **Pre-flight test command (`.claude/skills/release/SKILL.md`):** corrected in the Wave 0 governance pass (2026-06-21) to the per-project commands + `dotnet test tests/ClimaSite.E2E` — the old `npx playwright test` (no such suite here) and bare root `dotnet test` are gone. Use the section 4 suite of this document as the canonical list.
2. **Procedure (per the skill, valid):** confirm green CI on `main` → decide semver bump from conventional commits (`feat!:`→MAJOR, `feat:`→MINOR, `fix:`/`chore:`→PATCH) → roll `[Unreleased]` into a versioned CHANGELOG section → release notes in `docs/releases/v<X.Y.Z>.md` (directory doesn't exist yet — create it) → `chore(release): v<X.Y.Z>` commit **via PR** (main is protected — the release commit lands through the PR merge, not a direct push) → after merge, annotate the tag on the squashed `main` commit and push **the tag only**: `git tag -a v<X.Y.Z> -m v<X.Y.Z> && git push origin v<X.Y.Z>` (never a direct push to the `main` branch — it is protected).
3. **Unmet preconditions from `/deploy-checklist` — must be fixed before any production deploy** (all verified open in `_review/devops.md`):
   - P0: env-gate `DataSeeder` (hardcoded admin credentials seed in Production).
   - P1: add `UseForwardedHeaders` (rate limiter is one global bucket behind the proxy); move migrations out of startup into a pre-deploy step; create `.github/workflows/deploy.yml`; consolidate the 3 `railway.toml` files / duplicate Dockerfiles; correlation-ID logging + error tracker (the checklist's observability section currently cannot pass).
   - Fix the `Stripe__*`/`SMTP` env-var naming and remove dummy keys so startup fails fast on missing payment config.
4. **First concrete step:** cut a `v0.x` pre-release tag from green `main` to exercise the skill and create a rollback reference point (`_review/devops.md` finding 12).
5. Which Railway services exist and whether auto-deploy from `main` is wired: **Needs confirmation** (owner's Railway dashboard).

## 7. Definition of Done

Merged from CLAUDE.md (still the right bar), with the testing corrections applied. A feature/slice is done only when ALL of these hold:

1. **Compiles clean** — `dotnet build` with 0 warnings; `npm run build` succeeds.
2. **All tests pass via the section 4 commands** (per-project `dotnet test` — never bare root `dotnet test`; E2E via `dotnet test tests/ClimaSite.E2E` against real servers — never `npx playwright`).
3. **New tests written** at every applicable layer (unit / integration / E2E) for the new behavior.
4. **CI green on the PR** — local-only test evidence does not count as done.
5. **App runs** — API on 5029 and `ng serve` on 4200 start without errors; feature manually verified in the browser.
6. **No regressions**; **no ESLint errors** (`npm run lint` — CI does not catch this for you yet).
7. **Responsive** (mobile/tablet/desktop) and **accessible** (keyboard, screen reader; axe-core E2E checks pass).
8. **i18n complete** — all text via translation keys, present in `en.json`, `bg.json`, `de.json` (`npm test` runs the key check); verified in all three languages.
9. **Themed** — colors only from `src/ClimaSite.Web/src/styles/_colors.scss` variables; verified in light AND dark.
10. **Docs updated per section 5** — CHANGELOG `[Unreleased]` bullet, status table/plan header, ADR if a decision was made, env-var table if config changed.
11. **Committed and merged via PR** — working-tree-only "done" is not done (the wishlist slice sat uncommitted for days; `_review/status.md` finding 4).
12. **Reviews run** — `/code-review` always; `/security-review` for auth/payments/config-touching changes; `/ui-qa` for UI changes.
