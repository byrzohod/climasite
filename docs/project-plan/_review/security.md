# Security — Review Findings (2026-06-11)

## Summary

ClimaSite's backend has a generally solid security baseline. JWT validation is correctly configured (issuer/audience/lifetime/signing-key validation, zero clock skew, HMAC-SHA256, 15-minute access tokens); refresh tokens use 64 cryptographically random bytes and are rotated on login/refresh and revoked on logout/password-reset; Identity enforces a reasonable password policy plus account lockout (5 attempts / 15 min); CORS is properly origin-restricted with credentials; the Stripe webhook verifies signatures; all admin controllers carry `[Authorize(Roles="Admin")]`; and most resource-scoped handlers (orders-by-id, addresses, notifications, wishlist, GDPR-delete) correctly filter by the authenticated userId. NuGet and npm (prod) dependency scans both came back clean (verified). GDPR deletion is well implemented with password re-confirmation, anonymization, and SecurityStamp rotation.

However, several real issues stand out. The most serious is an IDOR / PII leak: `GET /api/orders/by-number/{orderNumber}` is anonymous and its handler skips the ownership check entirely for unauthenticated callers, returning full customer email, phone, shipping address, and line items to anyone who knows or guesses an order number. Second, the live forgot-password handler logs the raw reset token at Information level and never sends an email (TODO), so the reset flow is both broken and leaks reset credentials into logs. Third, TestController (DB cleanup, product seeding, admin elevation) is compiled into the production image and the destructive cleanup endpoint is gated only by an environment-name string with no secret.

Additionally: Swagger is exposed unconditionally in production. Rate limiting partitions on `Connection.RemoteIpAddress` with no ForwardedHeaders middleware, so behind Railway/nginx the auth brute-force limiter collapses to a single shared bucket (ineffective protection plus mass-lockout risk). A weak default JWT secret is committed to appsettings.json and is only required-via-env for the literal "Production" environment, so any other environment name silently signs tokens with the public key. The wishlist share-token design is sound (122-bit GUID entropy, not enumerable). Areas needing manual pen-testing are listed under Dimension data.

## Findings

### 1. IDOR / PII disclosure: GET /api/orders/by-number/{orderNumber} is anonymous and skips ownership check for unauthenticated callers
- **Finding:** The order-lookup-by-number endpoint is reachable anonymously and its handler bypasses the ownership check when no userId is present, returning full customer PII to any caller with a valid (and guessable) order number.
- **Category:** security
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** OrdersController.cs:98-108 exposes `[HttpGet("by-number/{orderNumber}")]` with NO `[Authorize]`/`[AllowAnonymous]` attribute; the controller has no class-level `[Authorize]` and Program.cs adds no FallbackPolicy (Program.cs:84-88 only registers named policies), so the action is reachable anonymously. The handler GetOrderByNumberQuery.cs:43 only guards `if (userId.HasValue && order.UserId != userId && !IsAdmin)` — when userId is null (anonymous) the check is skipped and the full OrderDto is returned, including CustomerEmail, CustomerPhone, full ShippingAddress, and item details (GetOrderByNumberQuery.cs:82-118). By contrast GetOrderByIdQuery.cs:44-46 DOES reject anonymous callers, proving the asymmetry. Order numbers are predictable: `GenerateUniqueOrderNumber()` = ORD-yyyyMMdd-HHmmss-XXXX with only 4 hex chars of randomness (CreateOrderCommand.cs:221-226).
- **Affected files/areas:** `src/ClimaSite.Api/Controllers/OrdersController.cs`, `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByNumberQuery.cs`, `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs`
- **Why it matters:** Any unauthenticated party who knows or guesses an order number can read another customer's personal data (email, phone, postal address, purchased items) — a GDPR-relevant personal-data breach and a classic OWASP A01 Broken Access Control / IDOR.
- **Recommended fix:** Add an explicit ownership/guest-token check for anonymous callers in GetOrderByNumberQueryHandler (mirror GetOrderByIdQuery: reject when `!userId.HasValue && !IsAdmin`), OR require a per-order opaque confirmation token for guest order lookups instead of the guessable order number. Do not return PII to anonymous callers.
- **Acceptance criteria:** An anonymous request to `/api/orders/by-number/{someValidOrderNumber}` returns 401/404 and never exposes customer PII; an authenticated user receives only their own orders; admins can still read any. Add an integration test asserting anonymous and cross-user access are denied.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified. Verifier confirmed all cited evidence: anonymous callers bypass the ownership check and receive the full OrderDto with PII, while GetOrderByIdQuery correctly rejects them — a bug, not a guest-lookup design (no guest token exists); the global rate limiter dampens blind enumeration but does not mitigate the IDOR. P1 calibrated: unauthenticated GDPR-relevant PII disclosure, but it requires a valid order number and is not a full auth bypass, so not P0.

### 2. Password-reset token written to logs in plaintext and reset email never sent (broken + leaky reset flow)
- **Finding:** The live forgot-password handler logs the raw reset token at Information level and the email send is commented out, making the reset flow both a credential leak into logs and silently non-functional.
- **Category:** security
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** The wired handler ForgotPasswordCommandHandler.cs:34-42 generates the reset token then `_logger.LogInformation("Password reset token generated for user: {UserId}. Token: {Token}", user.Id, token)` (line 37) and only has a commented-out `// await _emailService.SendPasswordResetEmailAsync(...)` (line 40). EmailService.SendPasswordResetEmailAsync exists (EmailService.cs:51-57) but is never called from this handler. Serilog writes to Console at Information level (Program.cs:18-22, appsettings.json:34-50).
- **Affected files/areas:** `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`, `src/ClimaSite.Infrastructure/Services/EmailService.cs`
- **Why it matters:** Anyone with access to application logs (console, log aggregation, Railway logs) can read a valid password-reset token and take over any account. Separately, because no email is sent, the real password-reset feature is non-functional in production despite being advertised in the UI/GDPR docs.
- **Recommended fix:** Remove the token from the log statement entirely (log only the user id at Information, never the token). Wire the handler to call IEmailService.SendPasswordResetEmailAsync with the token so the email is actually delivered.
- **Acceptance criteria:** Logs contain no reset tokens; a forgot-password request triggers an email with the reset link; a unit test asserts the email service is invoked and that the token never appears in logged output.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified. Verifier confirmed evidence exactly as cited: the handler is live via unauthenticated `POST /api/auth/forgot-password` (AuthController.cs:83-89), the email service is never injected or called, and the Serilog console sink at Information has no Production override — so an attacker with log access can mint a takeover token for any account on demand. P1/High calibrated; not P0 only because the app is not yet deployed to production.

### 3. TestController (DB wipe, product seeding, admin elevation) is compiled into the production image and the destructive cleanup endpoint is gated only by an environment-name string with no secret
- **Finding:** TestController ships in the production artifact with no build-time exclusion; the mass-delete cleanup endpoint has no secret gate, and the admin-elevation endpoint falls back to a hardcoded default secret in Development.
- **Category:** security
- **Severity/Priority:** P1 (High) — verification: confirmed
- **Evidence:** TestController.cs is unconditionally compiled (no `#if DEBUG`, no conditional registration). `DELETE /api/test/cleanup/{correlationId}` (TestController.cs:98-201) mass-deletes orders, carts, reviews, wishlists, notifications, products, and users matching a substring, guarded ONLY by `IsTestEnvironment()` = `IsDevelopment() || IsEnvironment("Testing")` (lines 34-35) with NO secret. `POST /api/test/elevate-admin` (lines 40-90) grants Admin role; in Development the secret defaults to the hardcoded literal "test-admin-secret" (line 56), and CI uses that same value (.github/workflows/test.yml TestSettings__AdminSecret/TEST_ADMIN_SECRET).
- **Affected files/areas:** `src/ClimaSite.Api/Controllers/TestController.cs`, `.github/workflows/test.yml`
- **Why it matters:** If ASPNETCORE_ENVIRONMENT is ever misconfigured to Development or Testing in a deployed environment (a common operational mistake), an unauthenticated attacker can destroy the entire dataset and (with the well-known default secret) elevate any account to Admin. The controller's mere presence in the prod artifact is unnecessary attack surface.
- **Recommended fix:** Exclude TestController from Release builds via `#if DEBUG` or register it conditionally only when env is Development/Testing at startup. Require a strong, non-default secret for ALL test endpoints including cleanup, and never fall back to a hardcoded default.
- **Acceptance criteria:** A Release/Production build does not expose any `/api/test/*` route (404); cleanup/seed require a configured strong secret; no hardcoded default secret remains in source.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified. Verifier confirmed all cited evidence: the controller is unconditionally compiled and exposed via MapControllers; cleanup matches users by `u.Email.Contains(correlationId)` with no minimum length, so a substring like "@" wipes nearly all data (destructiveness understated if anything); the "test-admin-secret" default is also committed in CI. Exploitation requires an ASPNETCORE_ENVIRONMENT misconfiguration; that single point of failure plus the catastrophic blast radius justifies High/P1.

### 4. Swagger UI and OpenAPI JSON exposed unconditionally in production
- **Finding:** UseSwagger/UseSwaggerUI are called outside the Development-only block, so the full API schema and interactive console are public in production.
- **Category:** security
- **Severity/Priority:** P2 (Medium) — verification: unverified (P2/P3)
- **Evidence:** Program.cs:271-277 calls `app.UseSwagger()` and `app.UseSwaggerUI(...)` outside the `if (app.Environment.IsDevelopment())` block (which ends at line 265). The DeveloperExceptionPage is correctly gated to Development (lines 262-265) but Swagger is not.
- **Affected files/areas:** `src/ClimaSite.Api/Program.cs`
- **Why it matters:** Publishing the full API schema and an interactive "try it out" console in production hands attackers a complete map of endpoints, parameters, and models, lowering the cost of reconnaissance (OWASP A05 Security Misconfiguration).
- **Recommended fix:** Wrap UseSwagger/UseSwaggerUI in `if (app.Environment.IsDevelopment())`, or protect the Swagger route behind authentication/an allowlist in non-dev environments.
- **Acceptance criteria:** GET `/swagger` and `/swagger/v1/swagger.json` return 404 (or require auth) in Production while remaining available in Development.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 5. Rate limiter keys on Connection.RemoteIpAddress with no ForwardedHeaders middleware — auth brute-force protection collapses behind reverse proxy
- **Finding:** All rate-limit partitions key on the raw connection IP, which behind Railway/nginx is the proxy's IP, so every client shares one bucket and the auth limiter is ineffective.
- **Category:** security
- **Severity/Priority:** P2 (Medium) — verification: unverified (P2/P3)
- **Evidence:** All rate-limit partitions use `context.Connection.RemoteIpAddress?.ToString()` (Program.cs:210, 222, 235), including the "auth" policy applied to AuthController (AuthController.cs:13). No UseForwardedHeaders / ForwardedHeaders configuration exists anywhere (grep across src/*.cs, Dockerfile*, *.toml, *.yml returned nothing). Deployment is via Railway (railway.api.toml) behind a proxy, and nginx fronts the web tier. Notably AuthController.GetIpAddress() (AuthController.cs:176-181) DOES read X-Forwarded-For for audit logging, but the limiter does not, so the two disagree.
- **Affected files/areas:** `src/ClimaSite.Api/Program.cs`, `src/ClimaSite.Api/Controllers/AuthController.cs`
- **Why it matters:** Behind a proxy, Connection.RemoteIpAddress is the proxy's IP, so every client shares one rate-limit bucket: (a) the 10/min auth limiter no longer isolates a single attacker (brute force from many IPs still funnels through one bucket, making detection meaningless) and (b) legitimate users can be collectively locked out (DoS). OWASP A07 Identification & Authentication Failures.
- **Recommended fix:** Add `app.UseForwardedHeaders` (KnownProxies/KnownNetworks for the Railway/nginx hop) early in the pipeline so RemoteIpAddress reflects the real client, OR derive the partition key from a validated X-Forwarded-For. Validate that auth limiting partitions per real client IP.
- **Acceptance criteria:** Behind the proxy, two different client IPs receive independent auth rate-limit buckets; verified via a test hitting `/api/auth/login` with distinct X-Forwarded-For values.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 6. Weak default JWT secret committed to appsettings.json; strong-secret requirement only enforced for the literal 'Production' environment
- **Finding:** A placeholder-strength JWT signing key is committed to source, and the env-var requirement only triggers when the environment name is exactly "Production", so any other environment name silently signs tokens with the public key.
- **Category:** security
- **Severity/Priority:** P2 (Medium) — verification: unverified (P2/P3)
- **Evidence:** appsettings.json:21-22 ships `"Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"`. JwtConfiguration.ResolveSecret only throws when `environment.IsProduction() && string.IsNullOrWhiteSpace(environmentSecret)` (JwtConfiguration.cs:21-24); for any other environment name (Staging, QA, etc.) it silently falls back to the committed config secret. Both Program.cs:79 (validation) and TokenService.cs:24-26 (issuance) read the same fallback chain.
- **Affected files/areas:** `src/ClimaSite.Api/appsettings.json`, `src/ClimaSite.Api/Configuration/JwtConfiguration.cs`, `src/ClimaSite.Infrastructure/Services/TokenService.cs`
- **Why it matters:** The signing key is public (in git). Any deployment whose ASPNETCORE_ENVIRONMENT is not exactly "Production" (or that simply forgets the env var while not literally Production) signs and validates tokens with a known key, allowing an attacker to forge valid JWTs for any user/role (full auth bypass / privilege escalation). OWASP A02 Cryptographic Failures.
- **Recommended fix:** Remove the secret from committed appsettings.json (use a placeholder/none). Require JWT_SECRET in all non-Development environments, not just IsProduction(). Fail fast if the configured secret equals the known placeholder.
- **Acceptance criteria:** App refuses to start in any non-Development environment without an externally-provided JWT_SECRET; the committed config no longer contains a usable signing key.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 7. Active LoginCommand/RegisterCommand path has no FluentValidation; duplicate dead handler set with validators is not wired
- **Finding:** The auth commands actually dispatched by AuthController have no validators, while a parallel, validator-bearing implementation exists as unused dead code under Features/Auth.
- **Category:** security
- **Severity/Priority:** P2 (Low) — verification: unverified (P2/P3)
- **Evidence:** AuthController uses ClimaSite.Application.Auth.Commands.* (LoginCommandHandler.cs namespace Auth.Handlers, RegisterCommandHandler.cs) which have NO AbstractValidator (grep of src/ClimaSite.Application/Auth returned none). A parallel, validator-bearing implementation exists at Features/Auth/Commands (LoginCommand.cs:14 LoginCommandValidator, RegisterCommand.cs) but targets different command types (`Result<AuthResponseDto>` vs `Result<LoginResponseDto>`) and is unused dead code. ValidationBehavior only runs when a validator exists (DependencyInjection.cs:21,26).
- **Affected files/areas:** `src/ClimaSite.Application/Auth/Commands/RegisterCommand.cs`, `src/ClimaSite.Application/Auth/Handlers/LoginCommandHandler.cs`, `src/ClimaSite.Application/Features/Auth/Commands/LoginCommand.cs`
- **Why it matters:** The live registration/login commands rely solely on ASP.NET Identity for password policy and have no input-length/format caps on email/name fields, so oversized or malformed inputs are only loosely constrained. The duplicated dead handlers create confusion and risk a future MediatR ambiguity if namespaces are merged.
- **Recommended fix:** Delete the unused Features/Auth duplicate set (or consolidate onto it), and add validators (email format/length, name length, password policy) for the command types the AuthController actually dispatches.
- **Acceptance criteria:** Each auth command dispatched by AuthController has a registered validator enforcing field constraints; no duplicate IRequestHandler implementations for the same command type remain.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 8. Refresh tokens stored in plaintext in the database (single token per user)
- **Finding:** Refresh tokens are persisted as plaintext strings on the user entity and looked up by direct equality, so a read-only DB compromise yields directly usable tokens.
- **Category:** security
- **Severity/Priority:** P3 (Low) — verification: unverified (P2/P3)
- **Evidence:** ApplicationUser.RefreshToken is a plaintext string column (ApplicationUser.cs:13, set via SetRefreshToken lines 38-43); RefreshTokenCommandHandler.cs:35-36 looks tokens up by direct equality. Generation is cryptographically strong (64 RNG bytes, LoginCommandHandler.cs:133-139).
- **Affected files/areas:** `src/ClimaSite.Core/Entities/ApplicationUser.cs`, `src/ClimaSite.Application/Auth/Handlers/RefreshTokenCommandHandler.cs`
- **Why it matters:** A read-only DB compromise (backup leak, SQL injection elsewhere, snapshot exposure) yields directly usable long-lived refresh tokens. Storing only a hash limits the blast radius. Also, the single-token-per-user model invalidates other sessions on each login (UX) without per-device revocation.
- **Recommended fix:** Store a SHA-256 hash of the refresh token and compare hashes on refresh; optionally move to a RefreshTokens table supporting per-device tokens and revocation/rotation history.
- **Acceptance criteria:** The database stores only hashed refresh tokens; refresh still works by hashing the presented token; a DB dump no longer reveals usable tokens.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 9. Generic exception handler echoes ArgumentException messages to clients
- **Finding:** The exception middleware places raw ArgumentException messages into the response detail field, exposing developer-oriented internals; stack traces are not leaked.
- **Category:** security
- **Severity/Priority:** P3 (Low) — verification: unverified (P2/P3)
- **Evidence:** ExceptionHandlingMiddleware.cs:38 maps ArgumentException to BadRequest with arg.Message, and lines 47-57 place the raw exception.Message into the response `detail` field for ArgumentException/Validation/NotFound. Truly unexpected exceptions correctly return a generic message with null detail (line 39, 51-56), so stack traces are not leaked.
- **Affected files/areas:** `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs`
- **Why it matters:** ArgumentException messages are frequently developer-oriented and may reveal internal parameter names, value constraints, or logic details, aiding attacker reconnaissance (OWASP A05). Low impact because no stack traces or unexpected-exception details are exposed.
- **Recommended fix:** Return a generic 400 message for ArgumentException (reserve detailed messages for explicit ValidationException with curated, user-safe text), and consider adopting RFC 7807 ProblemDetails (already noted as TODO in Program.cs:108).
- **Acceptance criteria:** API error responses contain only curated, user-safe messages; raw internal exception text is not returned for ArgumentException.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 10. Public shared-wishlist endpoint leaks owner UserId and is only protected by the global rate limit
- **Finding:** The anonymous shared-wishlist response includes the owner's internal UserId GUID; share-token entropy is strong, so overall risk is low.
- **Category:** security
- **Severity/Priority:** P3 (Low) — verification: unverified (P2/P3)
- **Evidence:** WishlistController.cs:28-34 exposes `[AllowAnonymous]` GET `shared/{shareToken}`. The returned WishlistDto includes UserId (WishlistDto.cs:6, populated in WishlistApplicationService.cs:129). Share tokens have strong entropy (Wishlist.cs:36-41 base64url of a 122-bit GUID) so enumeration is impractical, and only IsPublic wishlists are returned (WishlistApplicationService.cs:96-98). No wishlist-specific rate limiting is applied (only the global 100/min in Program.cs:208-217).
- **Affected files/areas:** `src/ClimaSite.Api/Controllers/WishlistController.cs`, `src/ClimaSite.Application/Features/Wishlist/DTOs/WishlistDto.cs`, `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs`, `src/ClimaSite.Core/Entities/Wishlist.cs`
- **Why it matters:** Sharing the owner's internal UserId GUID with anonymous viewers is unnecessary data exposure (it could be correlated across other endpoints/DTOs). Token entropy makes enumeration unlikely, so overall risk is low.
- **Recommended fix:** Omit UserId (and any non-essential owner identifiers) from the DTO returned by the anonymous shared-wishlist path; consider applying the named rate-limit policy to the public endpoint.
- **Acceptance criteria:** The shared-wishlist response contains only product/item display data and no owner UserId; a test asserts UserId is absent for the anonymous share path.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 11. Email confirmation not required and not enforced; registration accepts unverified emails
- **Finding:** RequireConfirmedEmail is disabled and login does not check EmailConfirmed, so accounts are fully usable with unverified email addresses.
- **Category:** security
- **Severity/Priority:** P3 (Low) — verification: unverified (P2/P3)
- **Evidence:** Infrastructure DependencyInjection.cs:46 sets `options.SignIn.RequireConfirmedEmail = false`; LoginCommandHandler does not check EmailConfirmed before issuing tokens (LoginCommandHandler.cs:33-91). The confirm-email endpoint exists (AuthController.cs:101-109) but is optional.
- **Affected files/areas:** `src/ClimaSite.Infrastructure/DependencyInjection.cs`, `src/ClimaSite.Application/Auth/Handlers/LoginCommandHandler.cs`
- **Why it matters:** Users can register and fully use accounts with email addresses they do not control, enabling impersonation/spam-signup and complicating account recovery. Low severity for a shop but worth a deliberate decision.
- **Recommended fix:** Decide policy explicitly: either enforce RequireConfirmedEmail (and gate sensitive actions on EmailConfirmed) or document that verification is intentionally optional.
- **Acceptance criteria:** Email-verification policy is intentional and documented; if enforced, login/sensitive actions are blocked until EmailConfirmed is true.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified by reviewer; not independently re-verified (P2/P3).

### 12. Dependency vulnerability scans clean (NuGet + npm prod) — verified
- **Finding:** Positive baseline: both backend (NuGet, including transitive) and frontend (npm production) dependency vulnerability scans came back clean; recorded as a verified positive rather than a defect.
- **Category:** security
- **Severity/Priority:** P3 (Low) — verification: unverified (P2/P3)
- **Evidence:** `dotnet list ClimaSite.sln package --vulnerable --include-transitive` reported "no vulnerable packages" for all 8 projects (Core, Application, Infrastructure, Api, and test projects). `npm audit --omit=dev` in src/ClimaSite.Web reported 0 vulnerabilities across info/low/moderate/high/critical.
- **Affected files/areas:** `ClimaSite.sln`, `src/ClimaSite.Web/package.json`
- **Why it matters:** Confirms OWASP A06 (Vulnerable & Outdated Components) is currently not an issue at the dependency level; recorded as a positive verified baseline rather than a defect.
- **Recommended fix:** No action required now; keep automated dependency scanning in CI so regressions are caught.
- **Acceptance criteria:** CI runs dotnet/npm vulnerability scans on each build and fails on high/critical findings.
- **Dependencies or follow-up:** None.
- **Confidence:** Verified (scan output observed); not independently re-verified (P2/P3).

## Dimension data

### Severity-ordered findings

| # | Severity | Priority | Finding | Key evidence |
|---|----------|----------|---------|--------------|
| 1 | High | P1 | IDOR/PII leak: anonymous GET /api/orders/by-number/{orderNumber} skips ownership check | GetOrderByNumberQuery.cs:43; OrdersController.cs:98-108 |
| 2 | High | P1 | Password-reset token logged in plaintext, email never sent | ForgotPasswordCommandHandler.cs:37-40 |
| 3 | High | P1 | TestController shipped to prod; destructive cleanup gated only by env name (no secret) | TestController.cs:34-35,98-201,56 |
| 4 | Medium | P2 | Swagger UI/JSON exposed unconditionally in production | Program.cs:271-277 |
| 5 | Medium | P2 | Rate limiter keys on RemoteIpAddress, no ForwardedHeaders → auth limiter broken behind proxy | Program.cs:210,222,235; no UseForwardedHeaders anywhere |
| 6 | Medium | P2 | Weak default JWT secret committed; strong-secret required only for literal "Production" | appsettings.json:22; JwtConfiguration.cs:21-24 |
| 7 | Low | P2 | Active login/register commands lack validators; duplicate dead handler set | Auth/Handlers vs Features/Auth/Commands |
| 8 | Low | P3 | Refresh tokens stored plaintext in DB | ApplicationUser.cs:13,38-43 |
| 9 | Low | P3 | ArgumentException messages echoed to clients | ExceptionHandlingMiddleware.cs:38,47-57 |
| 10 | Low | P3 | Shared wishlist leaks owner UserId; only global rate limit | WishlistDto.cs:6; WishlistController.cs:28-34 |
| 11 | Low | P3 | Email confirmation not required/enforced | DependencyInjection.cs:46 |
| 12 | (positive) | P3 | NuGet + npm(prod) dependency scans clean — verified | dotnet list / npm audit output |

### OWASP Top-10 coverage map

| OWASP 2021 | Status | Notes |
|------------|--------|-------|
| A01 Broken Access Control | ISSUE | Order-by-number IDOR (#1). Most other handlers (orders-by-id, addresses, notifications, wishlist, gdpr) correctly filter by userId. All admin controllers have `[Authorize(Roles="Admin")]`. No global FallbackPolicy — endpoints are opt-in auth. |
| A02 Cryptographic Failures | ISSUE | Committed JWT secret + env-name gap (#6); plaintext refresh tokens (#8). JWT alg HMAC-SHA256 OK. |
| A03 Injection | OK | No raw SQL (FromSqlRaw/ExecuteSqlRaw/SqlQueryRaw absent); EF Core parameterizes. No file-upload endpoint currently wired (IStorageService.UploadAsync defined but never called from Application layer). |
| A04 Insecure Design | PARTIAL | Test endpoints in prod (#3); single refresh token per user. |
| A05 Security Misconfiguration | ISSUE | Swagger in prod (#4); ArgumentException leak (#9); AllowedHosts="*". HSTS enabled non-dev (Program.cs:268, default maxage). DeveloperExceptionPage correctly dev-only. |
| A06 Vulnerable Components | OK (verified) | dotnet + npm prod scans clean. |
| A07 Auth Failures | ISSUE | Rate-limit/proxy issue (#5); reset-flow broken (#2); email confirmation optional (#11). Lockout 5/15min OK; login does not reveal user existence; forgot-password returns generic message (AuthController.cs:88). |
| A08 Data Integrity Failures | OK | Stripe webhook signature verified (WebhooksController.cs:58-71). See payments note below. |
| A09 Logging/Monitoring Failures | ISSUE | Sensitive token logged (#2). Serilog request logging present. Login attempts logged with email (acceptable). |
| A10 SSRF | N/A | No user-controlled outbound URL fetching observed. |

### Payments review notes
- WebhooksController.cs:51-71 correctly loads Stripe:WebhookSecret and verifies signature via EventUtility.ConstructEvent; returns 400 on bad signature. Endpoint is anonymous (correct for webhooks since auth is the signature).
- IDEMPOTENCY: not verified at the webhook layer — HandleStripeWebhookCommand handling should be checked for duplicate-event protection (Stripe retries). Needs-confirmation (see pen-test list).
- AMOUNT TAMPERING: PaymentsController.CreatePaymentIntent (PaymentsController.cs:42-71) takes Amount directly from the client body with only `Amount > 0` validation — the payment amount is NOT derived server-side from the cart/order. A malicious client could create a payment intent for an arbitrary (e.g., lower) amount. This is a real concern worth a dedicated follow-up; flagged here under Payments rather than as a separate numbered finding because the actual order Total is computed server-side in CreateOrderCommand (prices come from product/variant entities, not client — CreateOrderCommand.cs:128-202), so order pricing itself is not client-trusted. Confirm how the create-intent amount is reconciled against the order before capture. PaymentsController requires [Authorize] (line 10); GetConfig is [AllowAnonymous] and returns only the publishable key (safe).

### Secrets scan results
- appsettings.json: committed JWT secret (placeholder-strength, #6), dummy Stripe test keys (sk_test_/pk_test_/whsec_ all literal "Dummy..." — not real), dev DB password `climasite_dev_password`. No real production secrets.
- docker-compose.yml: dev-only creds (climasite_dev_password, climasite_minio_secret, pgAdmin admin/admin) — local infra only.
- railway.toml / railway.api.toml: no secrets (build/deploy/healthcheck only).
- .github/workflows/test.yml: only test values (POSTGRES_PASSWORD=test, JWT_SECRET="e2e-test-secret...", TestSettings__AdminSecret="test-admin-secret"). No real secrets, but the test admin secret matches TestController's accepted value — relevant to finding #3.
- environments/*.ts: no secrets (apiUrl only).
- MinioStorageService.cs:11-15 has hardcoded default access/secret keys as fallbacks (climasite/climasite_minio_secret) — dev defaults; ensure overridden in prod.

### Production security checklist (recommended before release)
- [ ] Fix order-by-number IDOR (#1) and add cross-user access tests.
- [ ] Remove reset-token logging; wire reset email (#2).
- [ ] Exclude TestController from Release builds / require strong secret on all test endpoints (#3).
- [ ] Gate Swagger to non-production or behind auth (#4).
- [ ] Add UseForwardedHeaders and verify per-client rate limiting behind Railway/nginx (#5).
- [ ] Remove committed JWT secret; require JWT_SECRET in all non-dev envs; fail on placeholder value (#6).
- [ ] Reconcile create-payment-intent amount against server-computed order total before capture (Payments note).
- [ ] Confirm Stripe webhook idempotency / duplicate-event handling.
- [ ] Set explicit AllowedHosts (not "*") and confirm AllowedOrigins is the real prod origin.
- [ ] Hash refresh tokens at rest (#8); consider per-device tokens + revocation.
- [ ] Verify Minio/SMTP/Stripe/DB/Redis secrets are injected via env in prod (not the committed defaults).
- [ ] Decide and enforce email-verification policy (#11).
- [ ] Tune HSTS (max-age, includeSubDomains, preload) for prod.
- [ ] Ensure HTTPS termination + UseHttpsRedirection behaves correctly behind the proxy.

### Areas requiring manual penetration testing
1. Guest-cart access model: GetCart/UpdateCartItem/RemoveFromCart accept a client-supplied guestSessionId (CartController.cs:29,57-92). Test whether a guest session ID is guessable/enumerable and whether one guest can manipulate another's cart, and whether an authenticated user's cart can be reached via guestSessionId.
2. Order create-payment-intent amount vs order total reconciliation (can a client pay less than owed?) and Stripe webhook idempotency / replay.
3. JWT forgery test against a deployed non-Production-named environment (verify the committed key is not in use).
4. Rate-limit effectiveness behind the actual Railway/nginx topology (single-bucket / mass-lockout behavior).
5. TestController reachability in the deployed environment (confirm /api/test/* is 404 in prod).
6. Brute-force/credential-stuffing against /api/auth/login given the proxy IP issue; verify lockout actually triggers per-account.
7. Order-number enumeration feasibility for the IDOR endpoint (timing/format) and broader PII exposure surface.
8. Authorization matrix sweep: attempt cross-user access to every {id:guid} resource endpoint (addresses, orders, notifications, reviews edit/delete) with a second user's IDs.

### Open questions
- Is GET /api/orders/by-number used by a guest order-confirmation page, and if so what proves caller authorization (a token in the URL/cookie)? Currently nothing server-side.
- Where is the payment amount authoritatively validated against the order before funds are captured?
- Is the deployed ASPNETCORE_ENVIRONMENT guaranteed to be exactly "Production"? (Determines exposure of findings #3 and #6.)
- Are application logs (which currently receive a reset token, #2) shipped to a retained aggregator?

## Refuted during verification

None.
