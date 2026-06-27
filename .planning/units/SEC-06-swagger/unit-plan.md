---
unit: SEC-06-swagger
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
---

# Unit plan — SEC-06: gate Swagger out of production

## Context (verified)
`app.UseSwagger()` + `app.UseSwaggerUI()` (Program.cs:313-318) run **unconditionally** → the API schema +
the Swagger UI are exposed in every environment, including Production (info disclosure). `AddSwaggerGen`
(DI) is harmless; only the middleware exposes the surface. Only `SecurityHeadersTests` references `/swagger`
and it does NOT assert a 200 (just that the security headers apply + CSP is skipped for the `/swagger`
path) — so gating doesn't break it.

## Decision
Serve Swagger **only in Development** (`app.Environment.IsDevelopment()`). This satisfies the acceptance
(404 in Production, available in Development) and is the more secure reading (also off in Staging/Testing —
Swagger is a local dev tool here). It's also testable without a fragile full-Production boot: the Testing
integration factory is non-Development, so `/swagger` → 404 there proves the gate.

## Scope
1. Program.cs: wrap `UseSwagger()` + `UseSwaggerUI(...)` in `if (app.Environment.IsDevelopment()) { ... }`.
2. SecurityHeadersTests: update the now-misleading "so the UI renders" comment (the CSP-skip applies to the
   `/swagger` path regardless; Swagger itself is Dev-only).
3. New test asserting `/swagger/index.html` and `/swagger/v1/swagger.json` → 404 in the (non-Development)
   Testing environment.

## Acceptance criteria
- [ ] `/swagger` + `/swagger/v1/swagger.json` return 404 outside Development (asserted in the Testing factory).
- [ ] The `/swagger` path still carries the security headers + no CSP (existing SecurityHeadersTests stays green).
- [ ] `dotnet test tests/ClimaSite.Api.Tests` green; build clean.

## Test / verification plan
- New `SwaggerGatingTests` (or extend SecurityHeadersTests): GET `/swagger/index.html` + `/swagger/v1/swagger.json`
  in the Testing factory → `NotFound`.
- `/acceptance`: N/A — no Dev-observable runtime UX change (the change is an env-conditional security gate;
  the Production behavior can't be driven locally). Covered by the integration test + council.
- Cross-vendor council (security change).
