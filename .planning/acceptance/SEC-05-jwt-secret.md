---
unit: SEC-05-jwt-secret
surface: api (real running host, two environments) + automated unit/integration evidence
result: PASS
date: 2026-06-29
commit: feature/fix-b-011-jwt-secret tip (validated against the uncommitted diff; final squash tip on merge)
driver: exploratory runtime — drove the REAL running API (`dotnet run`, not the test harness) in two
  environments against the shared-infra Postgres (db `climasite`): a Staging boot WITHOUT JWT_SECRET (security
  fail-fast) and a Development login → token → protected-endpoint flow (centralized issuance↔validation).
---

# Acceptance — SEC-05 / B-011 (remove committed JWT secret + centralize token issuance)

## What this unit changed
Removed the committed JWT signing key from `appsettings.json`; hardened `JwtConfiguration.ResolveSecret` to
require a real `JWT_SECRET` in **every environment except Development/Testing**, reject the committed
placeholder + whitespace + `<32`-char secrets in **all** envs, and return a labelled Dev/Test-only fallback so
those envs boot out-of-the-box. Bound one `JwtOptions` (secret + issuer + audience + expiry) at startup, used by
**both** bearer validation and `TokenService`. Routed all access-token issuance through `TokenService`
(deleting the three handler-local `GenerateAccessToken` copies). No frontend change (the FE uses the login
response body, not JWT claims — verified).

## Environment
- API booted via `dotnet run` (not the Testcontainers harness) against the shared-infra Postgres 17 (db
  `climasite`) — the real boot path, exercising the startup secret-resolution that the unit tests stub.

## Scenarios driven
| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | Boot `ASPNETCORE_ENVIRONMENT=Staging` **without** `JWT_SECRET` (DB conn string supplied so it isn't the first failure) | **fail fast** at startup | ✅ `Unhandled exception. System.InvalidOperationException: JWT_SECRET must be configured in environment 'Staging'.` — host never starts listening |
| 2 | Boot **Development** (no `JWT_SECRET` → uses the labelled Dev fallback) | clean boot | ✅ API up on :5029 |
| 3 | `POST /api/auth/login` (seeded dev admin) | 200 + a real JWT + `UserDto` | ✅ 3-segment JWT (680 chars), `user.role = Admin` |
| 4 | `GET /api/orders` **with** the token | 200 (issuance↔validation share the secret) | ✅ 200 |
| 5 | `GET /api/orders` **without** a token | 401 | ✅ 401 (auth enforced) |
| 6 | `GET /api/admin/dashboard` with the admin token | 200 (role claim works) | ✅ 200 |
| 7 | Decode the issued token's claims (the centralized `TokenService` output) | live claim set; dead claims gone | ✅ `nameidentifier, emailaddress, jti, firstName, lastName, role, iss, aud, exp`; **no** `GivenName`/`Surname`/`preferred_*`; `iss`==`aud`==`https://climasite.local` (single-sourced) |

**Scenario 1** is the core security proof: a non-Development/Testing environment with no configured secret now
**refuses to start** rather than silently signing with the committed key. **Scenarios 3–6** prove the centralized
`TokenService` issuance and the shared-secret validation work end-to-end live, including role-based admin
authorization. **Scenario 7** confirms the claim reconciliation is behaviour-preserving.

## Automated evidence (not re-driven here)
- `JwtConfigurationTests` — 25 cases: Production/Staging/`staging`/QA/`Productionn` + no secret ⇒ throw;
  Development/Testing ⇒ fallback; committed placeholder (env & config) ⇒ throw in all envs; `""`/`"   "`
  treated as unset; 31-char ⇒ throw; exactly-32 + valid-in-each-env ⇒ returned; env preferred over config.
  **Mutation gate**: removing the placeholder from the reject-list makes the 5 rejection cases fail.
- `TokenServiceTests` — issued token validates, carries the live claims, rejects a foreign secret.
- `JwtIssuanceValidationTests` (integration, real HTTP pipeline + DB) — login-issued token authenticates a
  `[Authorize]` endpoint; no token ⇒ 401; foreign-signed token ⇒ 401.
- Refactored `Login`/`RefreshToken`/`GoogleSignIn` handler tests assert issuance flows through `ITokenService`.
- Build 0/0; Core 424 / Application 862 / Api 355 green; `dotnet format` gate exit 0.

## Note
The committed placeholder string remains as descriptive evidence in three historical audit/review docs
(`docs/audit/2026-04-08-gap-report.md`, `docs/project-plan/SECURITY_REVIEW.md`, `_review/security.md`) — those
quote the now-fixed finding, are not live config, and the reject-list neutralizes any reintroduction; gitleaks
is informational (SEC-13). Nothing was ever deployed (OPS-08), so there is no live secret to rotate.

## Verdict
**PASS** — zero blocker, zero major. The forgery path is closed (non-Dev/Test fail-fast proven live), token
issuance is centralized through `TokenService` with a single shared secret (login → authenticate → admin
authorization all green live), and the claim change is behaviour-preserving.
