---
unit: SEC-08-headers
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-26
design: ../../design/DESIGN.md
---

# Unit plan — SEC-08: response security-headers middleware

## Context (verified)
The API sets only `UseHsts()` (prod). No security-headers middleware. The API does **not** serve the
Angular SPA (no `MapFallbackToFile`/`UseSpa`), so its CSP applies to JSON/API responses only — it can be
**strict** without affecting the separately-served frontend or Stripe (which runs in the frontend).
Swagger UI is served by the API and needs inline scripts/styles, so CSP must be skipped for `/swagger`.

## Scope
Add a `SecurityHeadersMiddleware` (matching the `ExceptionHandlingMiddleware` + `UseXxx` extension
convention) wired **unconditionally** early in the pipeline (after `UseForwardedHeaders`), setting on
every response:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-XSS-Protection: 0` (modern guidance — disable the legacy auditor)
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; base-uri 'none'` — **skipped for
  `/swagger*`** so Swagger UI keeps working.

Out of scope (deploy-time, OPS-08): the Stripe-compatible **frontend** CSP, the CORS header-allowlist
(`AllowAnyHeader` → explicit), and `AllowedHosts` tightening — these depend on the deploy topology.

## Acceptance criteria
- [ ] An integration test (`SecurityHeadersTests`, via `IntegrationTestBase`) asserts all six headers are
      present with the expected values on an API response (e.g. `/health` and `/api/products`).
- [ ] CSP is **absent** on a `/swagger` response (so Swagger UI renders) but present on API responses.
- [ ] Middleware runs in the Testing environment (unconditional), so the test exercises it.
- [ ] `dotnet build` clean; `dotnet test tests/ClimaSite.Api.Tests` green.

## Test / verification plan
- **Automated:** `SecurityHeadersTests` (Api.Tests, Testcontainers) — assert header presence/values on a
  normal response + CSP-absent on `/swagger`. Run `dotnet test tests/ClimaSite.Api.Tests --filter
  FullyQualifiedName~SecurityHeaders`.
- **Build:** `dotnet build ClimaSite.sln`.

## Notes
Permissions-Policy/CSP on a JSON API have limited browser effect (no document context) but are
defense-in-depth + asserted for regression safety; the real frontend CSP is the deploy-time piece.
