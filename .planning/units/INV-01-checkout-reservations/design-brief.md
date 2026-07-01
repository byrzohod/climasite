# Design brief v2 — INV-01: checkout-start stock reservations (close charge-then-refund + leaks)

> **v2** rewrites v1 after the design council (Codex `gpt-5.5`@`xhigh` + Claude 3-lens plan-critic) returned
> **REWORK** with a coherent, correct set of findings. This version folds ALL of them in. It is the artifact
> for the **re-council**. Money-path + concurrency → hard bar; re-council must reach CONVERGED before code.
> Council audit: `scratchpad/inv-council/` (gitignored).

## 0. Problem (reframed — verified, not the literal "oversell")

The DB never oversells today: `TryDecrementVariantStockAsync` (`ApplicationDbContext.cs:129`) is an atomic
`UPDATE ... SET stock=stock-@q WHERE id=@id AND stock>=@q`; concurrent last-unit buyers serialize on the row
lock (proven by `StockConcurrencyTests`). The REAL defects, all rooted in *stock being taken only at the very
end of checkout, after the card is charged*:
1. **Charge-then-refund the loser** — card charged client-side (`payment.service.ts:184`) BEFORE the stock
   decrement (`CreateOrderCommand.cs:308`); the losing last-unit buyer is charged, then refunded.
2. **Bank-transfer stock leak** — offline orders hard-decrement at creation, no restock if the wire never comes.
3. **Dishonest availability** — cart/PDP show raw `StockQuantity`; `ReservedQuantity` DTO field is vestigial (`0`).

## 1. Locked owner decisions

1. **Reserve at checkout-start** (atomic hold at PaymentIntent creation, BEFORE the charge; order-create
   converts hold→sold; **20-min TTL** (config) + expiry sweeper).
2. **Model = `stock_reservations` ledger** (per-hold rows) + a denormalized `reserved_quantity` counter on
   `product_variants` maintained in lockstep — the proven B-039 ledger+`HelpfulCount` pattern.
3. **Enforce at checkout + honest availability on PDP + cart only**; browse/search/recommendation/admin stay raw
   (accepted inconsistency — §8 F-D7).
4. **Bank-transfer = hold with longer TTL + auto-restock if unpaid** (Wave B).
5. **[v2] Anti-grief: FULL hardening now** — server-minted signed guest-session token (httpOnly cookie)
   replacing the client-supplied `SessionId`, + per-IP issuance limit + per-variant anonymous-hold cap +
   rate-limit on `POST /api/orders`. (Owner chose the stronger option over "defer the token".)

## 2. Current-state anchors (verified file:line; from the understand pass)

- Stock on `ProductVariant.StockQuantity` (int, private set, `ProductVariant.cs:10`); **no RowVersion/xmin anywhere.**
- Sole atomic gate `TryDecrementVariantStockAsync` (`ApplicationDbContext.cs:129-134`), called only at `CreateOrderCommand.cs:310`.
- Checkout (card): FE `create-intent` (`CreatePaymentIntentCommand.cs:78` — reads cart, no stock) → **client charge**
  (`confirmCardPayment`) → `POST /api/orders` (`CreateOrderCommand`) inside `CreateExecutionStrategy().ExecuteAsync`
  + explicit `BeginTransactionAsync` (default READ COMMITTED): verify intent server-side → Loop-2 atomic decrement
  → refund on `rows==0` → commit `Pending`; webhook flips `Pending→Paid`, **never touches stock**.
- `EnableRetryOnFailure(3)` global (`DependencyInjection.cs:32`) → multi-statement txns MUST use `CreateExecutionStrategy`.
- **order.Id + orderNumber are generated INSIDE the retried delegate** (`CreateOrderCommand.cs:192-195`) — a retry
  can't recognize its own prior work (council High).
- One-order-per-intent guard = unique filtered index on `orders.payment_intent_id` (`OrderConfiguration.cs:153-157`),
  caught as `DbUpdateException` AFTER SaveChanges (`CreateOrderCommand.cs:343-354` — too late for idempotency).
- Guest cart = client-supplied `SessionId` (`Cart.cs:6`; non-unique index); **no server issuance/ownership** — the grief surface.
- Enum storage is MIXED: Order/Question/Answer/Review use `HasConversion<string>()`; Outbox/Contact use `<int>()`.
- Worker template `EmailOutboxBackgroundService` + `OutboxProcessor` (options-gated poll, fresh scope, `(Status,NextAttemptAt)` index).
- Migrations `MigrateAsync` at startup; **integration tests `EnsureCreatedAsync`** → new table+column+filtered-index must be in the MODEL.
- B-039 proven pattern: ledger + denormalized count, counts moved ONLY via `ExecuteUpdate` gated on a from-state
  conditional op (`ON CONFLICT DO NOTHING`/conditional `DELETE`/`UPDATE ... WHERE from_state`); a **toggle** decided
  inside the retry delegate double-applies (the B-039 lesson).
- Fixtures to reuse: `StockConcurrencyTests` (Testcontainers, 2 independent authed clients, `Task.WhenAll` last-unit race);
  `QuestionsControllerTests` concurrent-double-POST; `GdprControllerTests` execution-strategy retry-safety;
  `PaymentMoneyPathTests` "drain stock after intent" idiom; `MockDbContext` (`ExecutionStrategyAttempts=2` seam).

## 3. Data model

**`reserved_quantity` (int, NOT NULL default 0) on `product_variants`.** `available = GREATEST(stock_quantity − reserved_quantity, 0)`.
**Invariant:** `reserved_quantity == Σ quantity of Active, non-expired StockReservations for that variant` — enforced under the variant row lock (§4 P0), with a reconciler backstop (§4 R).

**`StockReservation : BaseEntity`** — `VariantId` (FK product_variants CASCADE) · `Quantity`(>0) ·
`Status` (`Active|Consumed|Released|Expired`, **stored as string** `HasConversion<string>().HasMaxLength(20)`) ·
`ExpiresAt` (timestamptz) · `CartId` (FK carts, nullable, **ON DELETE SET NULL** — never CASCADE an Active hold) ·
`PaymentIntentId` (string, nullable) · `OrderId` (Guid, nullable) · `Kind` (`Card|BankTransfer`, string) · CreatedAt/UpdatedAt.

**EF config / migration `AddStockReservations`** (snake_case, `ValueGeneratedNever` id, `NOW()` defaults):
- **Filtered UNIQUE `(cart_id, variant_id) WHERE status='Active'`** (Card holds) — model `HasFilter` literal MUST byte-match the migration.
- **[Wave B] Filtered UNIQUE `(order_id, variant_id) WHERE status='Active' AND kind='BankTransfer'`** — because `cart_id` is null for bank holds and Postgres does not dedupe nulls (council M).
- Index `(status, expires_at)` (sweeper due-scan). Index `(variant_id, status)` (reconciler / availability). Index `(payment_intent_id)`, `(order_id)`.
- `reserved_quantity` `.HasDefaultValue(0).IsRequired()`. Add to `ApplicationDbContext` + `IApplicationDbContext` DbSets (EnsureCreated parity).
- **Model-parity guard test**: build the EnsureCreated schema and assert the filtered unique index + column default exist.

## 4. Concurrency mechanism (REWORKED — the council's core fixes)

### P0 — the serialization primitive: `SELECT ... FOR UPDATE` on the variant row
Every mutator of a variant's holds — P1 reserve, P2 consume, P3 release, R reconcile, AND the reserved-aware
decrement — **first locks the variant row** (`SELECT id FROM product_variants WHERE id=@v FOR UPDATE`), THEN reads
from-state, THEN writes. This single decision serializes all concurrent hold-mutations of a variant and eliminates
every stale-read / two-row-drift interleaving the council found (over-reserve, oversell-via-sweeper-race,
cross-cart corruption). Multi-line operations lock **in ascending `variant_id` order** to avoid deadlocks. The lock
is held only for cheap in-DB statements — **NEVER across the Stripe network call** (§6). All bodies run inside
`CreateExecutionStrategy().ExecuteAsync` (retry re-locks; convergent under the lock).

### P1 — `TryReserve(variantId, cartId, targetQty, kind, ttl, ct)` (reconcile-to-target, UNDER the lock)
1. Lock the variant row (P0).
2. Read this cart's hold row for `(cartId, variantId)`; **lazy-expire**: if it exists but `expires_at ≤ now()`, treat `E=0`.
3. `delta = targetQty − E`.
   - `delta == 0` (live reuse): **do NOT no-op** — CAS `UPDATE stock_reservations SET expires_at=now()+@ttl WHERE id=@r AND status='Active'`; success re-asserts liveness + refreshes the lease (so the sweeper can't take it mid-checkout). (Under P0 this is race-free anyway; the refresh is the point.)
   - `delta > 0`: `UPDATE product_variants SET reserved_quantity = reserved_quantity + @delta WHERE id=@v AND (stock_quantity − reserved_quantity) >= @delta`. If `rows==0` → **insufficient** → throw (whole-batch rollback, §6). If `rows==1` → upsert the ledger row to `(Active, qty=targetQty, expires_at=now()+@ttl)`.
   - `delta < 0`: guarded `UPDATE product_variants SET reserved_quantity = reserved_quantity − @(−delta) WHERE id=@v AND reserved_quantity >= @(−delta)` (**no `GREATEST` floor** — 0-rows is an invariant violation → assert/alert); set ledger qty=targetQty (or Release if 0).
Counter moves are a function of the ledger op's actual effect, read under the lock — the B-039 discipline. Convergent AND lock-serialized → the isolated-retry AND overlapping-writer cases are both safe.

### P2 — `TryConsumeReservation(reservation, orderId, orderLineQty, ct)` (hold→sold; decrement-first)
1. Lock the variant row (P0).
2. **Assert `reservation.Quantity == orderLineQty`** (else → reconcile/refund path; protects Wave B which has no Stripe amount check).
3. `UPDATE product_variants SET stock_quantity = stock_quantity − @q, reserved_quantity = reserved_quantity − @q WHERE id=@v AND stock_quantity >= @q AND reserved_quantity >= @q` (physical sale + hold release, **decrement FIRST**). If `rows==0` → fail → refund (defense-in-depth).
4. Only on `rows==1`: `UPDATE stock_reservations SET status='Consumed', order_id=@o WHERE id=@r AND status='Active'`.
Ordering (decrement before status-flip) means a failure leaves the hold **Active** (recoverable by sweeper/reconciler), never a phantom-Consumed row — matters for the Wave B mark-paid caller whose tx boundary differs.

### P3 — `TryReleaseReservation(reservation, newStatus∈{Released,Expired}, ct)`
1. Lock the variant row (P0). 2. `UPDATE stock_reservations SET status=@new WHERE id=@r AND status='Active'`; if `rows==1` → guarded `UPDATE product_variants SET reserved_quantity = reserved_quantity − @q WHERE id=@v AND reserved_quantity >= @q` (0-rows → assert/alert).

### Reserved-aware decrement — make `TryDecrementVariantStockAsync` honor holds (the universal guard)
Change the sole physical-decrement primitive to `UPDATE ... SET stock_quantity = stock_quantity − @q WHERE id=@v AND (stock_quantity − reserved_quantity) >= @q`. **Route EVERY non-consume decrement through it** (card-fallback when a hold expired, bank order-create in Wave A, any future path). This is what makes `reserved_quantity` load-bearing rather than decorative: no path can take a unit another cart holds. (P2-consume decrements stock AND reserved together, so it converts its OWN hold and is unaffected by the guard.)

### R — drift reconciler (self-heal; in the sweeper tick)
Per variant touched, under the variant lock: `reserved_quantity = COALESCE((SELECT SUM(quantity) FROM stock_reservations WHERE variant_id=@v AND status='Active' AND expires_at>now()),0)`. Safe (the lock serializes it vs P1/P2/P3 — a recompute, not the race-prone SUM-predicate rejected for the availability gate). B-039-style self-heal; also absorbs any admin-path drift. Emit a drift metric when it corrects a row.

### Order-create idempotency (council High — pre-existing, hardened here because consume rides it)
1. **Generate `order.Id` + `orderNumber` OUTSIDE the retried delegate** (deterministic per command).
2. At the TOP of the delegate, **look up an existing order by `PaymentIntentId`** (unique index); if found → return it idempotently (no refund, no re-consume, no cart re-read).
3. In consume, **disambiguate P2 `rows==0`**: reservation now `Consumed` with THIS `order_id` → idempotent no-op; `Consumed` with a DIFFERENT `order_id` → sibling/duplicate → do NOT fallback-decrement (let the unique intent index reject); genuinely Absent/Expired → **re-reserve (P1) first**, then consume; only if re-reserve fails (truly unavailable) → refund.

## 5. Wave A0 — server-minted signed guest-session token (prerequisite for secure guest holds)
- Server issues a **signed, httpOnly guest cookie** (`cs_guest`): high-entropy id + HMAC (server secret, reuse `JWT_SECRET`-class config) — minted on first cart interaction (or `POST /api/cart/session`), verified server-side each request; the verified id becomes the cart `SessionId`. Client can no longer forge/rotate ids cheaply (minting needs a round-trip → rate-limitable per IP).
- **Per-IP issuance rate-limit** on the mint endpoint/middleware; keep the client-supplied path **backward-compatible** during transition (accept a valid signed cookie OR, behind a flag, a legacy id) so existing guest carts survive; log/deprecate legacy.
- Cart handlers (`AddToCart`/`Update`/`Get`/`Merge`/`Remove`/`Clear`) resolve the guest id from the verified cookie, not the body/query. `MergeGuestCartCommand` reads the cookie id.
- Ships as its own PR (independently valuable + reviewable). Wave A binds reservations to this trustworthy id.

## 6. Wave A — card checkout reservations (the core)
- **Reserve at `CreatePaymentIntentCommand`, BEFORE the Stripe intent:** load cart; in ONE execution-strategy transaction, lock+reserve **all lines** (P1) in `variant_id` order; if any line insufficient → the whole tx rolls back (no partial holds) → `Result.Failure` naming the short items — **all before any Stripe call**. COMMIT the reservations, THEN create the Stripe PaymentIntent (never inside the retry delegate). Stamp `payment_intent_id` onto the cart's Active holds (audit).
- **Per-variant anonymous-hold cap** (config, e.g. anon Active holds ≤ X% of stock) enforced in P1 when the cart is a guest cart → bounds grief even with the token.
- **Consume at `CreateOrderCommand`:** join reservations by `cart_id`; per line P2 (with the order-create idempotency guard above). Order commits `Pending`; webhook unchanged.
- **Rate-limit `POST /api/orders`** (`strict`/`strict-user`) — none today.
- **Expiry sweeper** `StockReservationSweeperBackgroundService` (clone `EmailOutboxBackgroundService`): **enabled-by-default in Production, fail-fast at startup if disabled in Prod**, per-tick `try/catch` (loop never dies), fresh scope, `Where(status='Active' AND kind='Card' AND expires_at<now()).OrderBy(expires_at).Take(batch)` → P3(Expired) each; then the reconciler pass (R). Emits **active-hold count + oldest-hold age** metrics; alert if oldest > 2×TTL. Disabled in Testing.
- **Cart-lifecycle release (complete set):** `ClearCart`/`RemoveFromCart`/quantity-decrease → P1-reconcile to the new line (Release at 0); `MergeGuestCartCommand` → release the guest cart's Active holds before delete; **GDPR/account-delete + guest-cart cleanup** enumerated as release points; `cart_id ON DELETE SET NULL` means an orphaned hold still expires via the sweeper (self-heal).
- **Availability display (PDP + cart only):** `GetProductBySlugQuery` → `ReservedQuantity=v.ReservedQuantity`, `AvailableQuantity=GREATEST(stock−reserved,0)`; `GetCartQuery` + add/update guards → `available=GREATEST(stock−reserved,0)`. Everything else raw (decision #3).
- **Admin reserved-awareness:** `AdjustStockCommand` guard `stock+change ≥ reserved_quantity`; `BulkAdjustStock` absolute-set rejected/clamped if `< reserved_quantity` (council M6/R5).
- **MockDbContext + IApplicationDbContext:** in-memory mirrors of P1/P2/P3/reserved-aware-decrement/R + `DbSet<StockReservation>`.

## 7. Wave B — bank-transfer hold-with-expiry
Bank-transfer (no create-intent) creates a `Kind=BankTransfer` hold **at order-create** (not a hard decrement), linked to the order, `ExpiresAt=now()+N days` (config), stock NOT yet physically decremented (uses the reserved-aware guard so it can't steal a card hold). Payment-confirmed (admin mark-paid) → P2 consume (physical decrement). Expiry → sweeper (a bank branch) releases (P3) + cancels the order (no restock needed — never decremented). Unique key `(order_id, variant_id) WHERE status='Active' AND kind='BankTransfer'`. `CancelOrderCommand` branches: Consumed card order → restock via `AdjustStock` (today); Active bank hold → P3 release. Separate PR after Wave A.

## 8. Council findings → resolutions (traceability)
- Codex#1/#2, Claude-conc#1, Claude-drift#2: stale-delta/counter-first/P1-vs-sweeper drift → **P0 FOR UPDATE + ledger-gated counter** (§4).
- Claude-drift#1: delta==0 trusts a sweep-able hold → **CAS-refresh expiry on delta==0 + lazy-expire** (§4 P1).
- Codex#3, Claude-conc#2, Claude-abuse#3: order-create not idempotent → **deterministic order id + lookup-by-intent + P2 rows==0 disambiguation + re-reserve-before-fallback** (§4).
- Codex#4: partial-reserve crash → **reserve whole cart in one tx, commit before Stripe** (§6).
- Codex#5, Claude-conc#3, Claude-drift#3: no self-heal / GREATEST hides drift → **guarded decrements + reconciler R** (§4).
- Claude-abuse#2 (High): reserved_quantity decorative vs non-reserving writers → **reserved-aware `TryDecrementVariantStockAsync`, ALL decrements routed through it incl. bank in Wave A** (§4).
- Codex#6, Claude R5: admin can under-cut holds → **reserved-aware admin guards** (§6).
- Codex#7, Claude-abuse#1 (High): anon inventory-denial → **[owner] server-minted guest token (Wave A0) + per-IP issuance limit + per-variant anon-hold cap + rate-limit order-create** (§5/§6).
- Claude-abuse#4: cart-delete orphans a hold → **cart_id ON DELETE SET NULL + enumerate all delete paths** (§6).
- Claude-abuse#5: sweeper SPOF → **prod-default-on + fail-fast + per-tick try/catch + hold-age metric/alert** (§6).
- Claude-abuse#6/Low: TTL < 3DS tail → **20-min config TTL + re-reserve-before-refund** (§1/§4).
- Claude-drift#5: EnsureCreated/Migrate parity → **enum-as-string, pinned filtered-index literal, default(0), parity test** (§3).
- Claude-drift#6: consume qty vs line qty → **assert equal in P2** (§4).
- Codex#9: Wave B null cart_id → **`(order_id,variant_id)` filtered unique** (§3/§7).
- Claude-drift#7/Low (accepted): list "In stock" vs PDP "0 left" inconsistency → **record as accepted ADR trade** (decision #3); cheap future fix = reserved-aware `InStock` badge/filter only.

## 9. Test plan (extends existing fixtures — §2)
- **Integration (Testcontainers; new `ReservationConcurrencyTests` + extend `StockConcurrencyTests`):** N concurrent create-intents for the last unit → exactly ONE reserve succeeds, others fail **before any charge**; `reserved_quantity` never exceeds stock and never drifts from `Σ Active`; two independent carts race → one wins (R3). **Break-probe interleavings A (double create-intent same cart → over-reserve) and B (reserve vs sweeper-expire → oversell) must FAIL without the P0 lock.** Consume converts hold→sold (stock−1, hold Consumed). Reserved-aware guard: a bank/admin decrement can NOT take a held unit. Order-create idempotency: `ExecutionStrategyAttempts=2` → exactly one order, zero spurious refund, net-once consume (GdprControllerTests style). Sweeper releases an expired hold; reconciler heals an injected drift. Anon-hold cap enforced; guest-token verify rejects a forged cookie; order-create rate-limit 429.
- **Application unit (MockDbContext + `ExecutionStrategyAttempts=2`):** P1 reconcile idempotency, P2 decrement-first + from-state gate + qty assertion, P3 floor guard. Break-probe: additive-not-reconcile P1, or drop the reserved-aware guard → the matching test fails.
- **Frontend (Jasmine):** PDP/cart show `available=stock−reserved` + "only X left"; guest-token cookie flow; i18n en/bg/de.
- **/acceptance (REAL stack):** two concurrent buyers race the last unit → loser **blocked before payment** (not charged-then-refunded); abandon checkout → hold expires (or is swept) → unit returns; PDP/cart honest counts; guest checkout works with the signed cookie. Committed PASS `.planning/acceptance/INV-01-...-waveA.md` (per-wave).
- **CI** (six checks) = evidence of record. **Cross-vendor council on each wave's diff = hard bar.**

## 10. Out of scope
Add-to-cart reservations (rejected). Reservation-adjusted availability on browse/search/recommendation/admin (decision #3). Distributed/Redis locks (single DB instance; the variant row lock suffices). 3DS/SCA async-authorization reservation semantics. Backfilling `reserved_quantity` (starts 0). Full guest-token rollout beyond cart/checkout (auth stays JWT).
