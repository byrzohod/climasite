---
unit: PAY-IDEM-stripe-keys
surface: api (real Stripe test mode) + ui-path (checkout create-intent) + automated FE/CI evidence
result: PASS
date: 2026-06-29
commit: feature/pay-idem-stripe-keys tip (validated against the uncommitted diff; final squash tip on merge)
driver: exploratory runtime — REAL running API (ASPNETCORE_ENVIRONMENT=Development) on :5029 against the
  shared-infra Postgres 17 (superuser, db `climasite`) + REAL Stripe TEST mode (owner-supplied sk_test/pk_test),
  driving the real create-intent money path with adversarial idempotency-key inputs.
---

# Acceptance — PAY-IDEM-stripe-keys (Stripe idempotency keys + B-061 CancellationToken)

## What this unit changed
Client-supplied per-attempt idempotency key on `POST /api/payments/create-intent` (frontend generates a
`crypto.randomUUID()` per place-order attempt → validated `[A-Za-z0-9_-]{8,200}` → namespaced `ci_<key>` →
Stripe `RequestOptions.IdempotencyKey`), a server-derived `re_v1_<sha256(intentId)>` key on refunds,
`MaxNetworkRetries=2` at startup, shipping-method canonicalization in the Stripe metadata, log-redaction of the
key, and (B-061) a `CancellationToken` threaded through create-intent. Design history: a prior council caught a
[High] money-loss regression in an earlier cart-state-key design → resolved to the client-supplied per-attempt
key (see unit-plan "Council outcome").

## Environment
- API booted via `dotnet run` in **Development** (not the test harness) on :5029, against the shared-infra
  Postgres on :5432 (db `climasite`, already migrated — 37 tables) — i.e. the real boot path the
  Testcontainers integration tests do NOT exercise.
- **Real Stripe TEST mode**: the owner set `Stripe:SecretKey` (sk_test) + I set the public `Stripe:PublishableKey`
  (pk_test) in the git-ignored `appsettings.Development.json`. `GET /api/payments/config` served the real
  publishable key, confirming live Stripe wiring.

## Scenarios driven (adversarial, real Stripe)
A guest cart was created (`POST /api/cart/items`, 1× a €1199.99 variant → server total €1439.99), then
`POST /api/payments/create-intent` was driven with controlled idempotency keys:

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | App boots in Development against shared Postgres + reaches Stripe | clean boot, config served | ✅ `GET /api/payments/config` returns the real `pk_test_…` |
| 2 | create-intent, key **K1** (first) | a real `pi_…` | ✅ `200`, `pi_3TncW8AiZnpErQbE17Z1Zcjt` |
| 3 | create-intent, **same K1**, unchanged cart | the **SAME** `pi_…` (Stripe replays the cached intent — true dedup) | ✅ `200`, **identical** `pi_3TncW8AiZnpErQbE17Z1Zcjt` |
| 4 | create-intent, **different key K2**, same cart | a **DIFFERENT** `pi_…` (per-attempt rotation) | ✅ `200`, `pi_3TncW8AiZnpErQbE1EB5Rkn4` (≠ K1's) |
| 5 | create-intent, **malformed key** `"bad key!"` | `400` (validator rejects) | ✅ in prod-like envs (see note); live-Dev returned `500` via the dev exception page |

**Scenario 3 is the core proof**: the same client key forwarded to Stripe replayed the *identical* real
PaymentIntent rather than creating a duplicate — the whole point of the unit, proven end-to-end against live
Stripe (previously un-runnable locally without a Stripe secret).

## Adversarial finding (root-caused, not a defect)
Scenario 5 returned **HTTP 500 + a raw `ValidationException` stack trace** in the live Development run, not the
`400` the integration test asserts. Root cause: `Program.cs:300` registers the custom `UseExceptionHandling()`
(which maps `ClimaSite.Application.Common.Exceptions.ValidationException → 400`, middleware line 35), but in
Development `Program.cs:305` *also* registers `UseDeveloperExceptionPage()` **inner** to it, so the dev page
intercepts the exception first. In **Testing/Production** (no dev page) the exception reaches the custom
middleware → **400** — exactly what `PaymentMoneyPathTests.CreateIntent_WithMalformedIdempotencyKey_ReturnsBadRequest`
asserts and passes. This is generic ASP.NET Core dev-exception-page behaviour affecting *all* validation
failures, **not** specific to this unit and **not** a regression. (It does incidentally re-confirm review item
**B-008**: raw exception detail in non-prod — tracked separately.)

## Other-layer evidence (not re-driven manually here)
- **FE per-attempt key rotation** — proven in **real Chrome** by `checkout.component.spec.ts` (two
  `placeOrder()` calls forward two *different* `idempotencyKey` args) — guards against a refactor re-opening
  the [High].
- **Full real-Stripe 4242 card charge → order** — covered by CI `CardPaymentE2ETests` (owner test keys;
  happy + declined) as one of the required checks.
- **Backend correctness** — Core 424 / Application 862 / Api 327 green (incl. the money-path dedup +
  param-mismatch + charge→refund→retry-fresh-intent integration tests + the FakePaymentService guards), and
  the `dotnet format` CI gate passes.

## Verdict
**PASS** — zero blocker, zero major. The core idempotency behaviour (same-key dedup + per-attempt rotation) is
proven LIVE against real Stripe; the one adversarial finding (live-Dev 500 on a malformed key) is root-caused
to the Development exception page and is correct (400) in prod-like envs per the passing integration test.
The full UI card-charge path and FE key-rotation are covered by CI E2E + a real-Chrome spec respectively.
