# Unit: SEC-LOG-redact — redact secrets/credentials from MediatR request logging

## Problem (found 2026-06-28 during the live Google-OAuth test)
`LoggingBehavior<TRequest,TResponse>` logs `{@Request}` (Serilog destructuring of the whole MediatR
command) at **Information** for every request. That dumps credentials in **cleartext**:
- `LoginCommand.Password`, `RegisterCommand.Password`
- `GoogleSignInCommand.IdToken` (a live bearer token)
- (any future command with a password/token/secret/card field)

Proof (local run logs): `ClimaSite Request: RegisterCommand … {"Password":"<real>"}`,
`… GoogleSignInCommand … {"IdToken":"eyJ…"}`. Impact: anyone with log access (aggregators, shipped log
files, support bundles) gets live passwords + tokens. OWASP A09 (logging failures) + GDPR. Pre-existing
(predates Google OAuth); the Google work surfaced it. Auth/credential path → hard merge bar.

## Approach
Introduce a small, testable `LogSanitizer` and have `LoggingBehavior` log the **sanitized** projection
instead of the raw request. Redaction is **by property-name substring** (case-insensitive) so it is
self-maintaining: any current or future command whose property name contains a sensitive marker is
auto-redacted (defense in depth — the leak was a *missing* explicit redaction, so a denylist that fails
*closed* on new sensitive fields is the right shape).

- `LogSanitizer.Redact(object request) : IReadOnlyDictionary<string, object?>` — reflects public
  instance readable props (per-type `PropertyInfo[]` cache via `ConcurrentDictionary` to avoid
  per-request reflection cost); replaces any prop whose name contains a sensitive marker with the
  constant `"***REDACTED***"`; reads other values defensively (try/catch → null on throw). Non-sensitive
  values pass through unchanged so request logging keeps its debugging value (email/ids still visible —
  consistent with existing `"User signed in: {Email}"` logging; PII-in-logs is tracked separately).
- Sensitive markers (grounded in the sweep agent's audit of real command/DTO prop names):
  `password, token, secret, cvc, cvv, cardnumber, card, pin, apikey, clientsecret, creditcard` (final
  list reconciled with the sweep output before merge).
- `LoggingBehavior` logs `{@Request}` ← `LogSanitizer.Redact(request)` in BOTH the normal + the
  long-running branches.
- Top-level redaction only (the commands are flat records); document that nested secret-bearing objects
  aren't deep-redacted (none exist today — verified against the command shapes).

## Test plan (`tests/ClimaSite.Application.Tests/Common/Behaviors/LogSanitizerTests.cs`)
1. `RegisterCommand`-shaped object → `Password` → `"***REDACTED***"`; `Email`/`FirstName` preserved.
2. `GoogleSignInCommand`-shaped → `IdToken` redacted (matches `token`).
3. Case-insensitivity: `password`/`Password`/`PASSWORD`/`ConfirmPassword` all redacted.
4. A card-shaped object (`CardNumber`, `Cvc`) → both redacted.
5. Non-sensitive command (e.g. `GetProductBySlugQuery`) → all values pass through unchanged.
6. Null property values + a throwing getter → no exception, sane output.
7. (Optional behavior-level) a fake `ILogger` capturing structured state proves `LoggingBehavior` emits
   the redacted projection, not the raw command.

## Second sink investigated — request logging (sweep agent's HIGH) → DOES NOT REPRODUCE
The sweep flagged `UseSerilogRequestLogging` as logging the URL query string (→ guest-order `?token=`
capability token). Investigated + tested LIVE on both the override and the plain default: this
Serilog.AspNetCore version's default `RequestPath` is `Request.Path` **without** the query string —
`GET /api/orders/abc/guest?token=PLAINDEFAULTSECRET777` logged as `/api/orders/abc/guest`, cleartext-leak
count 0. An `EnrichDiagnosticContext.Set("RequestPath", …)` override was also tried and is **superseded**
by the middleware's default property, i.e. ineffective. Conclusion: the query token is **not** logged →
no fix needed; the speculative `RequestLogRedaction` helper was removed rather than shipped as dead code.
(If a future Serilog upgrade switches the path source to `RawTarget`, revisit.) The MEDIUM wishlist
share-token-in-route is accepted-as-designed (shareable-link token, already in URLs/browser history).

## Acceptance
- New `LogSanitizerTests` (incl. nested recursion) green; existing Application tests green; build clean.
- LIVE-VERIFIED: a `LoginCommand`/`RegisterCommand` now logs `"Password":"***REDACTED***"` (Email/Ip
  preserved) and the cleartext password appears nowhere in the log (leak-count 0). ✓ 2026-06-28.
- Cross-vendor council + security re-verify (auth/secret path) clean before `/trunk-merge`.
