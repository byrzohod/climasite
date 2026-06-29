---
unit: PAY-IDEM-stripe-keys
type: NORMAL
status: approved
created: 2026-06-28
plan_status: approved
design: ../../design/DESIGN.md
relates: [[dec-payment-arch]]
council: design-stage council run 2026-06-28 (Codex gpt-5.5@xhigh + Claude security leg); see "Council outcome".
---

# Unit plan — PAY-IDEM: Stripe idempotency keys (client-supplied create key + server refund key)

## Context / Definition of Ready
`DEC-PAYMENT` (council-validated) keeps card = Stripe Elements / SAQ A; backend creates a PaymentIntent for
the **server-computed amount only**. Today **no Stripe `IdempotencyKey` is passed anywhere** (grep: 0 hits)
and `MaxNetworkRetries` is unset. Order-level idempotency already exists (unique filtered index on
`orders.payment_intent_id` + a `DbUpdateException` guard), but **Stripe-side** dedup is absent: a network
retry of `POST /api/payments/create-intent` (or a refund retry) can create a duplicate Stripe object.

This unit adds idempotency keys to the two Stripe **create** calls. Backend + a small frontend touch. No DB
migration. Owner-confirmed scope (2026-06-28): idempotency keys ONLY (CSP + Payment Element are follow-ups).

### Council outcome (why client-supplied, not cart-state)
A first design keyed create-intent on a **cart-state hash**. The cross-vendor council (Codex gpt-5.5@xhigh +
Claude security) found a **[High]**: a pure cart-state key REGRESSES the charge→refund→retry path into a
**money-loss bug**. Verified flow — `checkout.component.ts placeOrder()` calls `createPaymentIntent` *fresh on
every click* (the `placingOrder` lock only blocks *concurrent* clicks); on a post-charge failure (e.g. stock),
`CreateOrderCommand` refunds the intent and returns **without** reaching `cart.Clear()` (line 336), so the
cart is unchanged → a cart-state key is unchanged → Stripe replays the **cached, now-refunded** intent for
24h → a retry can create a fulfillable order against refunded money (worse than today, where the retry makes
a fresh charge). A cart-state key cannot encode the "new payment attempt" lifecycle boundary without extra
persisted state. **Resolution (owner-chosen): client-supplied per-attempt key** — the frontend owns "this is
a new attempt", so the key rotates per attempt for free and dedupes only the network retry of a single POST.

## Scope / approach
1. **`ClimaSite.Application.Common.Payments.PaymentIdempotency`** (new static):
   - `ForRefund(string paymentIntentId)` ⇒ `"re_v1_" + SHA256hex(paymentIntentId)` (server-derived; refunds
     here are full-charge compensation, so keying on intent id alone is deterministic and prevents the
     double-refund attempt in the concurrent-failure case).
   - `IsValidClientKey(string key)` ⇒ length 8..200 AND every char ∈ `[A-Za-z0-9_-]` (defensive bound on the
     client-controlled value before it is forwarded to Stripe).
   - `NormalizeClientKey(string? raw)` ⇒ `null` when null/empty, else `"ci_" + raw` (namespaces client keys so
     they can never collide with `re_v1_` refund keys; the `ci_`/`re_v1_` prefixes are greppable in the Stripe
     dashboard).
2. **`CreatePaymentIntentCommand`**: add `string? IdempotencyKey { get; init; }` (the per-attempt key; the
   controller binds the command directly from the JSON body, so no separate request DTO).
   - Validator: when non-empty, `IsValidClientKey` must pass (else 400). Null/absent is allowed (degrades to
     today's no-dedup behaviour).
3. **`CreatePaymentIntentCommandHandler`**: canonicalize shipping once
   (`ShippingMethods.Canonicalize(request.ShippingMethod)`) and use it for the Stripe **metadata** (so the
   value we send Stripe is normalized); compute `idemKey = PaymentIdempotency.NormalizeClientKey(request.
   IdempotencyKey)` and pass it to `CreatePaymentIntentAsync`.
4. **`IPaymentService`**: append `string? idempotencyKey = null` to `CreatePaymentIntentAsync(...)`; insert
   `string? idempotencyKey = null` before the `CancellationToken` on `RefundAsync(...)`. Default null ⇒
   unchanged behaviour for any caller that omits it.
5. **`StripePaymentService`**: when `idempotencyKey` is non-empty, pass
   `new RequestOptions { IdempotencyKey = idempotencyKey }` to `PaymentIntentService.CreateAsync` and
   `RefundService.CreateAsync` (Stripe.net 46.2.0 has the `CreateAsync(options, RequestOptions, ct)` overload
   and `RequestOptions.IdempotencyKey`). Null ⇒ pass no request options (today's behaviour preserved).
6. **`MaxNetworkRetries` at startup** (council [Med]/[Low], both legs): set
   `StripeConfiguration.MaxNetworkRetries = 2` once in `AddInfrastructure(...)` (composition root) — NOT in the
   per-request scoped service ctor. Complements explicit keys (Stripe.net auto-attaches a key for its own
   internal retries; explicit keys make app-level retries safe). 2 is also Stripe.net's default, so this just
   makes the resilience intent explicit and well-placed.
7. **`CreateOrderCommand.RefundOrphanedChargeAsync`**: pass
   `PaymentIdempotency.ForRefund(paymentIntentId)` and the CT as a **named** arg (the old positional CT call at
   line 370 would otherwise mis-bind to the new key parameter — caught at compile time).
8. **Frontend (per-attempt key):**
   - `payment.service.ts` `createPaymentIntent(shippingMethod, sessionId?, idempotencyKey?)` — include
     `idempotencyKey` in the POST body.
   - `checkout.component.ts` `placeOrder()` card branch: `const attemptKey = crypto.randomUUID();` generated
     **once per call**, passed to `createPaymentIntent`. A browser/proxy network retry of that single POST
     resends the identical body (same key) → Stripe dedupes; a *user* retry after a failure is a new click →
     new key → fresh intent (closes the [High]).
9. **`FakePaymentService`** (Api.Tests double): model real Stripe behaviour so tests are meaningful —
   cache `key → (intentId, amountMinor, currency)`; a non-empty key seen again with the **same** params returns
   the **same** intent id (dedup), with **different** params returns a Failure ("idempotency key reused with
   different parameters", mirroring Stripe's 400). Null-key path unchanged (always a new intent). Record
   `CreateIdempotencyKeys` / `LastCreateIdempotencyKey` and `RefundIdempotencyKeys` for assertions.
10. **B-061 (review item) folded in — `CancellationToken` threaded into create-intent:** append
    `CancellationToken cancellationToken = default` as the LAST param of `CreatePaymentIntentAsync` on
    `IPaymentService` + `StripePaymentService` (forwarded as `PaymentIntentService.CreateAsync(options,
    requestOptions, cancellationToken)`), and have the handler pass its existing `cancellationToken`
    through. Pure symmetry with `RefundAsync` (which already takes a CT); backend-only, no behaviour
    change. The `FakePaymentService` double + all `CreatePaymentIntentAsync` Moq setups/verifies gain the
    matching trailing arg, plus one propagation unit test asserting the exact token reaches the service.

## Acceptance criteria
- [ ] Every Stripe **create** call carries an idempotency key when one is supplied: create-intent forwards
      `ci_<clientKey>`; refund forwards `re_v1_<hash(intentId)>`. Verified by unit + integration tests (no live
      Stripe needed for the gate).
- [ ] Per-attempt rotation proven: two create-intent calls with the **same** key (+ same params) ⇒ the **same**
      intent (dedup); with **different** keys ⇒ **different** intents. A retry after a refunded charge gets a
      **fresh** intent (the [High] is closed).
- [ ] Malformed client keys (too short/long, illegal chars) ⇒ 400; null/absent ⇒ unchanged behaviour
      (existing tests stay green).
- [ ] `ForRefund` is deterministic per intent id; the refund-on-failure path passes it.
- [ ] No raw owner data appears **in the idempotency key**; no key/inputs logged. (Existing Stripe *metadata*
      still carries `userId` — standard practice, unchanged, out of scope.)
- [ ] No new C# / TS / ESLint warnings. All non-E2E suites green locally; **all six CI checks green** is the
      evidence of record (CardPaymentE2ETests still pass with the extra body field).
- [ ] Cross-vendor Codex council on the FINAL DIFF → every High/Medium fixed and re-counciled until clean
      (payments hard merge bar).
- [ ] `/acceptance` exploratory pass against the real running checkout (a normal card checkout still completes;
      a duplicate create-intent POST with the same key does not double-create) → PASS report committed at
      `.planning/acceptance/PAY-IDEM-stripe-keys.md` with `commit:` == merged tip.
- [ ] B-061: `CreatePaymentIntentAsync` accepts and forwards a `CancellationToken` (symmetry with
      `RefundAsync`); the handler threads its request token through, proven by a propagation unit test that
      asserts the exact token reaches the payment service.

## Test / verification plan
- **Unit (`ClimaSite.Application.Tests`):**
  - `PaymentIdempotencyTests` (new): `ForRefund` determinism + per-intent uniqueness + shape (`re_v1_` + 64-hex);
    `IsValidClientKey` accepts a UUID, rejects too-short/too-long/`bad chars`; `NormalizeClientKey` ⇒ `ci_…`/null.
  - `CreatePaymentIntentCommand` validator tests: bad key ⇒ invalid; null + valid UUID ⇒ valid.
  - `CreatePaymentIntentCommandHandlerTests`: handler forwards `ci_<key>` when supplied, `null` when absent
    (capture via the mock). **Update the existing 3-arg Moq setups (lines ~49, 93, 132) to the new 4-arg shape**
    (`It.IsAny<string?>()`) — otherwise the non-null key won't match → NRE.
  - `CreateOrderCommandHandlerTests`: assert `RefundAsync` is called with `ForRefund(pi)`. **Update the existing
    2-arg `RefundAsync` Moq setups/verifies (lines ~42, 809, 826)** to the new arity (else compile error).
- **Integration (`ClimaSite.Api.Tests`, `FakePaymentService` + money-path):**
  same key + same cart ⇒ same intent id (dedup); different key + same cart ⇒ different intent; same key +
  different params ⇒ Fake returns the param-mismatch Failure; the refund-on-failure path records `ForRefund`.
- **Mutation gate (the one break-the-code bug):** make the handler ignore the client key (pass `null`). The
  "same key ⇒ same intent (dedup)" integration test MUST fail. If it still passes, the suite is not meaningful.
- **Frontend specs:** `payment.service.spec.ts` — the create-intent POST body includes `idempotencyKey`.
- **CI:** the six required checks are the evidence of record.

## Out of scope (tracked follow-ups, see [[dec-payment-arch]])
- Production Stripe-compatible CSP; `CardElement` → Payment Element (Link/SEPA/iDEAL).
- Idempotency on `Confirm`/`Cancel` (single-intent-id, naturally idempotent; not on the charge-duplication path).
- Broader shipping-method canonicalization in the order-persistence path (the order still stores the raw
  casing; the server validator already constrains it to allowed values case-insensitively — cosmetic only).
- Persisting the attempt key in `sessionStorage` to dedupe across a page reload (the lost-response-then-reload
  edge creates at most one *harmless unconfirmed* intent — no money risk, no regression vs today).
- Partial refunds (would require amount/scope in `ForRefund`; today's refunds are full-charge compensation).
- Server-side key binding: the server enforces only charset/length on the client key; cross-user safety
  relies on the frontend using a CSPRNG (`crypto.randomUUID`/`getRandomValues`). Follow-up = HMAC/derive the
  forwarded key server-side from (userOrSession + clientNonce).
- `FakePaymentService` models amount+currency+metadata mismatch; real Stripe hashes the full request body —
  close enough for these tests.
