# ClimaSite Codex Memory

This file translates the repo-local Claude workflow context into Codex-readable project memory.

## Shared Local Infra

- Shared stack: `~/Projects/shared-infra/docker-compose.yml`.
- Start local dev infra with `cd ~/Projects/shared-infra && docker compose up -d postgres redis`.
- Local app/E2E should use shared services:
  - PostgreSQL: `localhost:5432`, one database per project, database name `climasite`.
  - Redis: `localhost:6379`.
- For local shared Postgres, run the API with `ConnectionStrings__DefaultConnection` and `SSL Mode=Disable`. Avoid `DATABASE_URL` for localhost because ClimaSite converts non-Railway URLs to SSL-required connection strings.
- For local E2E, start the API with `ASPNETCORE_ENVIRONMENT=Testing` and `--no-launch-profile`; the checked-in launch profile forces `Development`, which enables rate limiting and can throttle the browser suite.
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
