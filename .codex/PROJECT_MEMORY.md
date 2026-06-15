# ClimaSite Codex Memory

This file translates the repo-local Claude workflow context into Codex-readable project memory.

## Shared Local Infra

- Shared stack: `~/Projects/shared-infra/docker-compose.yml`.
- Start local dev infra with `cd ~/Projects/shared-infra && docker compose up -d postgres redis`.
- Local app/E2E should use shared services:
  - PostgreSQL: `localhost:5432`, one database per project, database name `climasite`.
  - Redis: `localhost:6379`.
- For local shared Postgres, run the API with `ConnectionStrings__DefaultConnection` and `SSL Mode=Disable`. Avoid `DATABASE_URL` for localhost because ClimaSite converts non-Railway URLs to SSL-required connection strings.
- For local E2E, start the API with `ASPNETCORE_ENVIRONMENT=Testing`, `TestSettings__AdminSecret=test-admin-secret`, and `--no-launch-profile`; the checked-in launch profile forces `Development`, which enables rate limiting and can throttle the browser suite. Run E2E with matching `TEST_ADMIN_SECRET=test-admin-secret`.
- Do not add project-local compose services when the shared stack already provides the service.
- Never use shared infra for integration tests, CI, or performance tests. Integration tests use Testcontainers.

## Merge Readiness

Run and report:
- `dotnet test tests/ClimaSite.Core.Tests/ClimaSite.Core.Tests.csproj`
- `dotnet test tests/ClimaSite.Application.Tests/ClimaSite.Application.Tests.csproj`
- `dotnet test tests/ClimaSite.Api.Tests/ClimaSite.Api.Tests.csproj`
- `cd src/ClimaSite.Web && npm test -- --watch=false --browsers=ChromeHeadless`
- `cd src/ClimaSite.Web && npm run build`
- `cd src/ClimaSite.Web && npm run lint`
- E2E with real app/services on `localhost:4200` and `localhost:5029`
- code review, security review, and UI QA when UI changed

Keep unrelated existing debt separate from issues introduced by the current branch.

## Home v3 / Plan 18 Status

- Plan 18 Phase 0 and Phase 1 are complete as of 2026-06-07.
- Plan 18 Phase 2 Wishlist WISH-* slice is complete as of 2026-06-07.
- The active homepage is `features/home-v3/`: configurator-first wizard, Canvas 2D room preview, real `/api/products/recommendations` integration, category/trust/CTA sections, EN/BG/DE translations.
- Wishlist is complete for the current Plan 18 scope: hydrated backend DTO sync, public sharing route/API, guest-to-login merge, per-user concurrent add protection, EN/BG/DE keys, unit/API/frontend/E2E coverage.
- Production frontend API calls should remain relative (`apiUrl: ''`) so browser requests go through the deployed `/api` proxy and avoid CORS-origin drift.
- The root route must lazy-load Home v3 with `pathMatch: 'full'`; do not reintroduce an eager `component` import for the home route.
- Final local verification for the Home v3 completion PR:
  - Backend tests: Core 199 passed, Application 162 passed, API 70 passed.
  - Frontend: `npm run lint`, `npm run build`, and Karma 970 passed.
  - E2E: 213/213 passed against real API/data.
  - Lighthouse `/`: mobile 0.97 with LCP 2.296s; desktop 1.00 with LCP 0.576s.
- Next Plan 18 phase: complete the remaining Notifications decision-dependent work and Animation Audit 21F tasks before broader i18n/theme hardening.
