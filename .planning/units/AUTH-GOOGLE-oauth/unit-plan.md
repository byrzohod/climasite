---
unit: AUTH-GOOGLE-oauth
type: NORMAL
status: approved
created: 2026-06-28
plan_status: approved
design: ../../design/DESIGN.md
council: required (auth — hard merge bar) before merge
---

# Unit plan — AUTH-GOOGLE: "Sign in with Google" (OIDC ID-token flow)

## Context / Definition of Ready
The app has email/password auth only (ASP.NET Identity `IdentityUser<Guid>`, `ITokenService` JWTs,
register/login/refresh in `src/ClimaSite.Application/Auth/`). No external/social login exists. Add Google
sign-in using the **ID-token flow** (frontend Google Identity Services → ID token → backend verifies →
issues the SAME app JWT), which is simpler + more testable than a server-side redirect flow and reuses the
existing JWT/session machinery. Identity already supports external logins (`aspnetuserlogins` table via
`UserManager.AddLoginAsync`).

## Scope / approach
### Backend
1. **`IGoogleTokenValidator`** (Application/Common/Interfaces) — `Task<GoogleUserInfo?> ValidateAsync(string
   idToken, CancellationToken)`; returns `{ Subject, Email, EmailVerified, GivenName, FamilyName, Picture }`
   or **null** if invalid/expired/wrong-audience. The interface is the SEAM that makes this unit-testable.
2. **`GoogleTokenValidator`** (Infrastructure) — real impl using `Google.Apis.Auth`
   (`GoogleJsonWebSignature.ValidateAsync(idToken, new ValidationSettings { Audience = [clientId] })`),
   reads `Authentication:Google:ClientId` from config. Add the **Google.Apis.Auth** package (1.70.0, cached).
   On any exception → return null (never throw to the handler).
3. **`GoogleSignInCommand(string IdToken)`** + handler (Application/Auth) — validate token (→ 401-mapped
   `UnauthorizedException`/null-guard if invalid). Then, in order: `UserManager.FindByLoginAsync("Google",
   sub)` → if none, `FindByEmailAsync(email)` (LINK Google to the existing account via `AddLoginAsync`) → if
   none, **create** a new `ApplicationUser` (email, FirstName=givenName, LastName=familyName,
   `EmailConfirmed = emailVerified`, UserName=email, IsActive) + `AddLoginAsync("Google", sub)`. Require
   `EmailVerified == true` (reject unverified Google emails). Issue JWT + refresh exactly like
   `LoginCommandHandler` (reuse `ITokenService`, set refresh token, `RecordLogin`), return the SAME auth
   response DTO `LoginCommandHandler` returns.
4. **`AuthController` `POST("google")`** → `GoogleSignInCommand`; 200 with the auth payload, 401 on invalid token.
5. **Config endpoint**: serve the Google `ClientId` (public) to the SPA. Add it to the existing public config
   surface (mirror `GET /api/payments/config` which serves the Stripe publishable key) — e.g. `GET
   /api/auth/config` → `{ googleClientId: "<id or empty>" }`. Empty when unconfigured.
6. **DI**: register `IGoogleTokenValidator → GoogleTokenValidator` (scoped). No prod fail-fast (Google is
   optional — when ClientId is empty the feature is simply unavailable; the endpoint returns 400 "Google
   sign-in is not configured").

### Frontend
7. **Google Identity Services**: load the GSI script lazily; render the official "Sign in with Google" button
   on the **login** and **register** pages (only when `googleClientId` from `/api/auth/config` is non-empty).
   On the GSI credential callback (an ID token), call `authService.googleSignIn(idToken)`.
8. **`AuthService.googleSignIn(idToken)`** → `POST /api/auth/google` → reuse the EXISTING login-success
   handling (store token, set user signal, merge guest cart + wishlist, navigate). Same `LoginResponse` shape.
9. i18n: add `auth.google.signIn` etc. keys to en/bg/de.

## Acceptance criteria
- [ ] `POST /api/auth/google` with a valid (verified) Google ID token returns the app JWT + user, creating
      the user on first sign-in and linking/finding on subsequent ones; an invalid/expired/unverified token → 401.
- [ ] An existing email/password account is LINKED (not duplicated) when its email signs in via Google.
- [ ] Frontend shows the Google button on login/register ONLY when configured; clicking it (with a real
      Google account — owner-driven) logs the user in and merges their guest cart/wishlist.
- [ ] Feature degrades cleanly when `ClientId` is unset (button hidden, endpoint 400) — safe to ship dark.
- [ ] No secret committed; `Authentication:Google:ClientId` via config, `Authentication:Google:ClientSecret`
      (if a redirect flow is ever added) via env only. (The ID-token flow needs only the ClientId server-side.)

## Test / verification plan
- **Application unit** (`GoogleSignInCommandHandlerTests`): mock `IGoogleTokenValidator` + `UserManager`
  (MockUserManager) — new-user-created, existing-by-login-found, existing-by-email-linked,
  invalid-token→Unauthorized, unverified-email→rejected.
- **Integration** (`GoogleAuthTests` in Api.Tests): register a **fake `IGoogleTokenValidator`** in
  `TestWebApplicationFactory` returning a canned payload; `POST /api/auth/google` → 200 + a usable JWT
  (call `/api/auth/me` with it); second call with same sub → same user; a "null" payload → 401.
- **Frontend** (`auth.service.spec.ts` additions): `googleSignIn` POSTs the id token + stores the session.
- **E2E** (optional, follow-up): stub GSI; the real Google click is owner-driven.
- Council (auth = hard merge bar) on the diff. Build + all suites green.

## Out of scope
Server-side redirect/code flow; other providers (Apple/FB); account-unlink UI; the LIVE Google click (needs
the owner's Google OAuth client + a real Google account — owner-gated to fully prove).

## Owner-gated to PROVE live
Create a Google Cloud **OAuth 2.0 Web client**; authorized JS origin `http://localhost:4200` (+ prod later);
provide the **Client ID** → `Authentication:Google:ClientId` (config) + served via `/api/auth/config`. No
client secret needed for the ID-token flow. Then the login-page button works end-to-end.

## DoD gates
Green CI (6 checks) · cross-vendor council (auth) · update CHANGELOG/BACKLOG. (Live proof = owner adds the
Google client; CI integration tests prove the backend flow with a mocked provider regardless.)
