---
unit: SEC-05-jwt-secret
type: NORMAL
status: approved
created: 2026-06-29
plan_status: approved
design: ../../design/DESIGN.md
closes: SEC-05, B-011 (external-review register), SR-09
---

# Unit plan — SEC-05 / B-011: remove the committed JWT secret + centralize token issuance

## Context / Definition of Ready
External-review item **B-011** (council fix-first, real **High**) confirmed against live code:

- **Committed usable signing key** — `src/ClimaSite.Api/appsettings.json:38`
  `"Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"` (public repo).
- **Fail-fast too narrow** — `JwtConfiguration.ResolveSecret` (`src/ClimaSite.Api/Configuration/JwtConfiguration.cs:21`)
  throws only when `environment.IsProduction()` (literal "Production"). Any other deployed env (Staging, QA,
  preview, a typo) with no `JWT_SECRET` falls back to the committed key → **token forgery / admin escalation**.
- **Three handlers bypass the resolver** — `LoginCommandHandler.cs:94-131`, `RefreshTokenCommandHandler.cs:63-100`,
  `GoogleSignInCommandHandler.cs:198-235` each have a LOCAL `GenerateAccessToken` reading `JwtSettings:Secret`
  directly; the central `TokenService` (`src/ClimaSite.Infrastructure/Services/TokenService.cs`, `ITokenService`,
  DI at `DependencyInjection.cs:69`) **is never used for issuance**. Only `Program.cs:69` (bearer *validation*)
  uses the resolver — so issuance and validation read the secret from different code with different lax fallbacks.
- Nothing is deployed yet (OPS-08) → **latent**, no live exposure today, but a **P0 pre-deploy gate**.

**Claim-shape de-risk (verified):** the Angular frontend consumes the **login response body** (`response.user`
= `UserDto`, `auth.service.ts:171-173`) and treats the JWT as an opaque bearer — there is **no `jwtDecode`** in
the FE. The backend reads only `NameIdentifier` + `Role` from the token. So unifying issuance is
behaviour-preserving as long as the unified token keeps `NameIdentifier`/`Email`/`Role`/`Jti` (+ the existing
`firstName`/`lastName` custom claims the live handlers emit). `TokenService`'s *current* `GivenName`/`Surname`/
`preferred_*` claims are dead code (it's unused for issuance) and will be aligned to the live handler shape.

## Scope / approach
1. **Single resolved+validated secret at startup (fixes the Infra→Api layering trap).** `JwtConfiguration` lives
   in the Api layer and `TokenService` in Infrastructure (which cannot reference Api), so the secret is resolved
   ONCE at startup and injected via options:
   - New `JwtOptions` (in `ClimaSite.Application/Common` or Infrastructure — a layer both can see): `Secret`,
     `Issuer`, `Audience`, `AccessTokenExpirationMinutes`, `RefreshTokenExpirationDays`.
   - `Program.cs`: `var secret = JwtConfiguration.ResolveSecret(config, env);` (hardened, below) → bind a
     `JwtOptions` from it + the Issuer/Audience/expiry config, AND use `secret` for the existing JwtBearer
     validation (so **issuance and validation provably share one secret**).
2. **Harden `JwtConfiguration.ResolveSecret`:**
   - **Require** a real secret (env `JWT_SECRET` → config `JwtSettings:Secret`) in **every environment except
     Development and Testing** (was: only Production). Else throw at startup (`ValidateOnStart` semantics via the
     Program.cs call before the app runs).
   - **Reject the known committed placeholder** `"YourSuperSecretKeyThatIsAtLeast32CharactersLong!"` (a small
     `KnownInsecureSecrets` set) AND any secret `< 32` chars, in **all** environments (incl. Dev/Test) — so the
     old key can never sign a token again even if reintroduced.
   - **Dev/Test out-of-the-box:** when env is Development or Testing and nothing is configured, return an explicit
     `DevelopmentOnlyFallbackSecret` constant (clearly labelled, ≥32 chars, NOT the rejected committed value).
     It is returned ONLY for Dev/Test — the gate guarantees it can never be used in Staging/Prod.
   - Precedence unchanged: `JWT_SECRET` env → `JwtSettings:Secret` config → (Dev/Test only) fallback.
3. **`appsettings.json`:** remove the committed `JwtSettings:Secret` value (empty string). Keep Issuer/Audience/
   expiry (non-secret). `appsettings.Development.json` needs no JWT entry (the code Dev fallback covers it).
4. **Centralize issuance on `TokenService`:**
   - `TokenService` reads everything from `IOptions<JwtOptions>` (no direct `IConfiguration`/`Environment` reads).
   - `TokenService.GenerateAccessToken` emits the **live** claim set: `NameIdentifier`, `Email`, `Jti`,
     `firstName`, `lastName`, and one `Role` claim per role (matching the 3 handlers today, so the token is
     byte-compatible in the claims that matter). Drop the dead `GivenName`/`Surname`/`preferred_*` claims.
   - Refactor `LoginCommandHandler`, `RefreshTokenCommandHandler`, `GoogleSignInCommandHandler` to **inject
     `ITokenService`** and call `_tokenService.GenerateAccessToken(user, roles)`; **delete the three local
     `GenerateAccessToken` methods** and the now-unused `IConfiguration` dependency where applicable.
   - Result: `JwtSettings:Secret` is read in **exactly one place** (the startup resolver); zero handler-local
     secret reads remain (grep gate).

## Council outcome (design-stage, 2026-06-29 — Codex gpt-5.5@xhigh + Claude security leg)
Both legs: design **sound + a net security win**, no [High] *design* blocker, claim reconciliation **confirmed
safe**. HOLD until these are folded in (done below):
- **[High] (Claude)** `tests/ClimaSite.Api.Tests/Controllers/TestControllerTests.cs:64` boots a **`Staging`**
  sub-factory; Staging is never exempt, so the emptied secret + no `JWT_SECRET` in the Integration CI job would
  throw at startup → red Integration check. **Fix:** `TestWebApplicationFactory.ConfigureWebHost` injects a
  deterministic ≥32-char non-placeholder `["JwtSettings:Secret"]` into its in-memory config (flows to the
  Testing default AND every `WithWebHostBuilder` sub-factory incl. Staging) — do **not** exempt Staging.
- **[Medium] (both)** Normalize every secret tier with `string.IsNullOrWhiteSpace` (treat `""`/`"   "` as unset)
  BEFORE precedence + length/reject checks, else the emptied `appsettings.json` `""` short-circuits the Dev/Test
  fallback and Dev fails to boot.
- **[Medium] (both)** Bind **one** `JwtOptions { Secret, Issuer, Audience, AccessTokenExpirationMinutes }` at
  startup and use that SAME instance for both the `JwtBearer` `TokenValidationParameters` (ValidIssuer/Audience)
  AND `TokenService` issuance; delete the divergent inline `?? "https://localhost:5001"` /
  `?? "climasite.local"` defaults in `Program.cs` + `TokenService`. Preserve `JWT_ISSUER`/`JWT_AUDIENCE` env >
  config > default precedence in that single resolution.
- **[Low] (Codex)** Do NOT add `RefreshTokenExpirationDays` to `JwtOptions` (refresh-token generation/lifetime
  stays in the handlers — centralizing/hashing it is **SEC-09/B-040**, out of scope) → avoids false centralization.
- **Testing exemption (both flagged):** keep **Development + Testing** exempt from the require-a-secret gate, BUT
  (a) the reject-list blocks the committed placeholder + `<32` + whitespace in **all** envs (so Dev/Test can
  never use the committed key — only the fresh `DevelopmentOnlyFallbackSecret`), (b) the integration factory
  injects a real secret so tests never rely on the fallback, and (c) document Testing as a **test-only,
  non-deployable** env. Tests must prove Staging/QA/Production/`Productionn`/casing all fail-fast.
- **[Nit]** Flip the existing `JwtConfigurationTests.cs:47` (`...ThrowsOutsideProduction_WhenNoSecretExists` →
  Development now returns the fallback, not a throw). Fix the placeholder sample at
  `docs/validation/areas/18-build-cicd-deployment.md:181` (the reject-list + gitleaks would otherwise flag it).

## Acceptance criteria
- [ ] No committed usable signing key: `appsettings.json` has no real `JwtSettings:Secret` value; `grep` for the
      old placeholder across tracked source returns 0 (except the explicit reject-list + its test).
- [ ] **Startup fail-fast in every non-Development/Testing environment** without a real `JWT_SECRET` (Staging, QA,
      Production, typo'd prod-like names) — proven by `JwtConfiguration` tests + a startup test.
- [ ] The committed placeholder and any `<32`-char secret are **rejected in all environments**.
- [ ] Development + Testing run out-of-the-box (no env var) via the labelled Dev/Test fallback.
- [ ] **All token issuance flows through `TokenService`** (the 3 handler-local generators are gone); issuance and
      bearer validation provably use the **same** secret (an integration test: token from `/api/auth/login`
      authenticates a `[Authorize]` endpoint).
- [ ] No behaviour change for clients: login/refresh/Google still return a working token + `UserDto`; the token's
      `NameIdentifier`/`Email`/`Role`/`firstName`/`lastName` claims are preserved.
- [ ] No new C# warnings; non-E2E suites green locally; **all CI checks green** (incl. `dotnet format` gate).
- [ ] Cross-vendor Codex council on the design AND the final diff (auth hard merge bar) → every High/Medium fixed
      and re-counciled until clean.
- [ ] `/acceptance` pass: drive the real app — login issues a token that authenticates; boot a non-Dev env
      without `JWT_SECRET` and confirm fail-fast; committed PASS at `.planning/acceptance/SEC-05-jwt-secret.md`.

## Test / verification plan
- **Unit (`JwtConfigurationTests`, extend):** Production/Staging/QA + no secret ⇒ throw; Development/Testing +
  no secret ⇒ returns the Dev/Test fallback; any env + the committed placeholder ⇒ throw; any env + `<32`-char ⇒
  throw; env var preferred over config; a real `JWT_SECRET` in each env ⇒ returned. (Add a typo'd "Productionn"
  case to show only Dev/Test are exempt.)
- **Unit (handler tests):** `LoginCommandHandlerTests` / `RefreshTokenCommandHandlerTests` /
  `GoogleSignInCommandHandlerTests` now assert the handler invokes `ITokenService.GenerateAccessToken(user,roles)`
  and returns its token (the local generators are gone). Keep one end-to-end token-shape assertion in a dedicated
  `TokenServiceTests` (issued token validates + carries NameIdentifier/Email/Role/firstName/lastName).
- **Integration (`ClimaSite.Api.Tests`):** a token from `POST /api/auth/login` authenticates a `[Authorize]`
  endpoint (issuance↔validation share the secret); a startup/config test that a non-Dev/Test env without
  `JWT_SECRET` fails fast.
- **Mutation gate (the one break-the-code bug):** re-add the committed secret as an accepted non-Dev fallback →
  the "non-Dev + placeholder ⇒ throw" test MUST fail. Revert.
- **CI:** the required checks are the evidence of record.

## Out of scope (separate tracked items)
- **B-040 / SEC-09** — hashing refresh tokens at rest (separate auth change; do next).
- No frontend change (verified: the FE uses the response body, not JWT claims).
- Per-device refresh-token table / revocation history (SEC-09 territory).
