---
unit: SEC-07-stripe-secrets
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
---

# Unit plan — SEC-07: remove dummy Stripe keys + production fail-fast

## Context (verified)
`src/ClimaSite.Api/appsettings.json` commits **non-empty dummy Stripe keys** (`sk_test_51Dummy…`,
`pk_test_…`, `whsec_Dummy…`). `StripePaymentService` only throws on `IsNullOrEmpty(secretKey)` — the
non-empty dummies **defeat that fail-fast**, so a by-the-book prod deploy that forgets the real keys boots
and silently runs with non-functional dummy payment keys. **JWT is already handled**
(`JwtConfiguration.ResolveSecret` throws in Production if `JWT_SECRET` is unset — the appsettings JWT
placeholder is dev-only), so SEC-07 narrows to the **Stripe** side. `StripePaymentService` is
**`AddScoped`** (constructed per-request, not at startup) and integration tests **replace `IPaymentService`
with `FakePaymentService`** — so emptying the keys does NOT break Dev/Testing startup; E2E provides keys via
`Stripe__SecretKey` env.

## Scope
1. `appsettings.json`: empty the three Stripe keys (`SecretKey`/`PublishableKey`/`WebhookSecret` → `""`).
   (Devs supply their own test keys via `appsettings.Development.json`/env; tests use `FakePaymentService`.)
2. New `src/ClimaSite.Api/Configuration/StripeConfiguration.cs` (mirrors `JwtConfiguration`):
   `ValidateProductionConfiguration(config, env)` — in **Production only**, throw if SecretKey /
   WebhookSecret / PublishableKey are missing or a placeholder (`IsPlaceholder`: contains
   dummy/placeholder/changeme). No-op outside Production.
3. Wire `StripeConfiguration.ValidateProductionConfiguration(configuration, environment)` early in
   `ConfigureServices` (Program.cs) so Production fail-fasts **at startup**, not at the first payment.
4. `CLAUDE.md` env table: correct the Stripe rows to the names the code actually reads
   (`Stripe__SecretKey` / `Stripe__PublishableKey` / `Stripe__WebhookSecret`, double-underscore →
   `Stripe:*` section) and point to `docs/runbooks/deploy.md` for the full matrix.

## Acceptance criteria
- [ ] No dummy Stripe keys remain in any committed file (`grep -r "sk_test_51Dummy\|whsec_Dummy" src` → 0).
- [ ] Booting **Production** without real Stripe config throws `InvalidOperationException` at startup
      (proven by `StripeConfigurationTests`); **non-Production** with empty keys does NOT throw.
- [ ] `IsPlaceholder` detects the committed dummy; a real `sk_live_…`/`sk_test_…` (no "dummy") passes.
- [ ] Dev/Testing startup unaffected (scoped service + FakePaymentService); existing tests stay green.
- [ ] `dotnet test tests/ClimaSite.Api.Tests` green; build clean.

## Test / verification plan
- `StripeConfigurationTests` (mirror `JwtConfigurationTests`): Production+empty→throws,
  Production+dummy→throws, Production+real→passes, Development+empty→no-throw, `IsPlaceholder` cases.
- Build + `dotnet test tests/ClimaSite.Api.Tests` (integration startup still works via FakePaymentService).

## Out of scope (tracked)
- SMTP/MinIO production validation (same pattern; follow-up). The DB connection-string dev password +
  JWT dev placeholder stay (needed for local/test; prod overrides via env — JWT already prod-guarded).
