---
name: climasite-workflow
description: Use for ClimaSite project work, especially session start, planning, shared local infrastructure, merge readiness, PR checks, or when the user references workflow, memory, Claude skills, or shared infra.
---

# ClimaSite Workflow

Follow `AGENTS.md` first. This skill captures the Claude workflow rules in Codex-compatible form.

## Non-Negotiables

- Ask batch questions only when needed; otherwise inspect and proceed.
- Tests come with the feature. Do not defer or remove tests to get green.
- UI/E2E tests use real app behavior, real backend, and real data. No mocking in E2E.
- Integration tests use Testcontainers, not shared local infra.
- Use conventional commits and feature branches. Do not push directly to `main`.
- Add ADRs in `docs/adr/` for non-obvious architecture or migration decisions.

## Shared Infra

- Shared stack: `~/Projects/shared-infra/docker-compose.yml`.
- Start shared local infra: `cd ~/Projects/shared-infra && docker compose up -d postgres redis`.
- Local app/E2E use shared Postgres on `localhost:5432` with database `climasite`, and shared Redis on `localhost:6379`.
- Use `ConnectionStrings__DefaultConnection` with `SSL Mode=Disable` for local shared Postgres. Avoid `DATABASE_URL` for localhost because ClimaSite's URL converter makes non-Railway hosts SSL-required.
- For local E2E, run the API in `Testing` with `TestSettings__AdminSecret=test-admin-secret` and run the E2E client with `TEST_ADMIN_SECRET=test-admin-secret`.
- Do not use project-local compose for shared services unless this repo needs a unique service.
- CI, integration tests, and perf tests must use isolated services, not shared infra.

## Merge Checklist

Run backend tests, Angular tests, build, lint, E2E, code review, security review, and UI QA for UI changes. Report pre-existing debt separately from branch regressions.
