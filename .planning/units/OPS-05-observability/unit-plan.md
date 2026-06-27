---
unit: OPS-05-observability
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
---

# Unit plan — OPS-05 (a): correlation IDs + traceable errors + log flush

## Context (verified)
Serilog is console-only with `Enrich.FromLogContext()`; **no correlation-ID middleware** exists (the
`X-Correlation-Id` convention in CLAUDE.md is implemented nowhere); `ExceptionHandlingMiddleware` returns
`{status,message,detail}` with no trace id; there's no `Log.CloseAndFlush`. `app.Run()` (Program.cs:39)
follows `ConfigurePipeline(app)`.

## Scope (no-decision, no-deploy slice of OPS-05)
1. `CorrelationIdMiddleware` (+ `UseCorrelationId` extension): read `X-Correlation-Id` from the request
   or generate a GUID; stash it in `HttpContext.Items`; push it to Serilog `LogContext` for the request
   scope (so every log line in the request shares the id); echo it on the response header.
2. Wire `app.UseCorrelationId()` early in `ConfigurePipeline` — after `UseSecurityHeaders`, **before**
   `UseExceptionHandling` and `UseSerilogRequestLogging` (so the error handler + the request log both
   see the id).
3. `ExceptionHandlingMiddleware`: add `traceId` (the correlation id, falling back to
   `context.TraceIdentifier`) to the JSON error response so a user can quote it.
4. Flush logs on shutdown: `app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);` before `app.Run()`.

Out of scope (deploy-time / needs the O-4 decision + a service): error tracker (Sentry), OpenTelemetry,
uptime alerts, JSON-console-in-prod formatter. Tracked on the OPS-05 backlog row as the remainder.

## Acceptance criteria
- [ ] Every API response carries an `X-Correlation-Id` header (generated when absent).
- [ ] A client-supplied `X-Correlation-Id` is echoed back unchanged (pass-through).
- [ ] An error response body includes a non-empty `traceId`.
- [ ] No new NuGet packages (uses Serilog already referenced); `dotnet build` clean.
- [ ] Integration tests (`CorrelationIdTests`) green; existing tests unaffected.

## Test / verification plan
- **Automated:** `CorrelationIdTests` (Api.Tests) — (a) `/health` response has a non-empty
  `X-Correlation-Id`; (b) a supplied id is echoed; (c) a 404 (`/api/products/{bad-guid}` or a missing
  route) error body contains `traceId`. Run `dotnet test tests/ClimaSite.Api.Tests --filter
  FullyQualifiedName~CorrelationId`.
- **Build:** `dotnet build ClimaSite.NoE2E.slnf`.
