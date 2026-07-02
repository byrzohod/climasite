---
unit: INV-01-checkout-reservations
type: NORMAL
status: done
created: 2026-07-01
approved: 2026-07-01
completed: 2026-07-02
plan_status: approved
design: ./design-brief.md
council: >
  codex gpt-5.5@xhigh + Claude 3-lens plan-critic. DESIGN R1 = REWORK (architecture-breaking: P1 reconcile
  racy under overlapping writers; reserved_quantity decorative vs non-reserving decrements; order-create
  non-idempotent under retry). REWORKED → R2 = REWORK-NARROW ("P0 FOR UPDATE is the right primitive; only the
  P2/P3 expiry-CAS + refund-idempotency + lock-ordering need tightening"). All R2 findings folded in →
  R3 = CONVERGED (no High/Medium; Codex: "I cannot break the core interleavings under the stated P0 row lock …
  the counter converges to Σ Active and does not oversell"; 2 Low clarifications folded). The diff-council on the
  real code is the hard merge bar per wave (money-path + migration + concurrency).
---

# Unit plan — INV-01: checkout-start stock reservations (kill charge-then-refund + stock leaks)

## Context / Definition of Ready
The DB never oversells (atomic `TryDecrementVariantStockAsync`, `ApplicationDbContext.cs:129`, proven by
`StockConcurrencyTests`). The real defects are that **stock is taken only at the end of checkout, after the card
is already charged client-side** (`payment.service.ts:184` → `CreateOrderCommand.cs:308`):
1. **Charge-then-refund** — the losing last-unit buyer is charged, then refunded (fees/chargebacks/trust).
2. **Bank-transfer leak** — offline orders hard-decrement at creation, never restock if unpaid.
3. **Dishonest availability** — cart/PDP show raw `StockQuantity`; `ReservedQuantity` DTO is vestigial (`0`).

**Verified anchors:** stock on `ProductVariant.StockQuantity` (private set); **no RowVersion/xmin anywhere**;
`EnableRetryOnFailure(3)` global (`DependencyInjection.cs:32`); `order.Id`/`orderNumber` generated INSIDE the
retried delegate (`CreateOrderCommand.cs:192`); one-order-per-intent = unique filtered index on
`orders.payment_intent_id`; guest cart keyed by client-supplied `SessionId` (no server issuance); enum storage
mixed (string vs int); integration tests use `EnsureCreatedAsync` (new table+column+filtered-index must be in the
model); worker template `EmailOutboxBackgroundService`; proven ledger+counter pattern = B-039 (counts via
`ExecuteUpdate` gated on a from-state conditional op). Full map: `design-brief.md` §2.

## Locked owner decisions
1. **Reserve at checkout-start** — atomic hold at PaymentIntent creation, BEFORE the charge; order-create
   converts hold→sold; **TTL = 20 min (config)** + expiry sweeper.
2. **Model = `stock_reservations` ledger** + denormalized `reserved_quantity` counter on `product_variants`
   (the B-039 ledger+count pattern).
3. **Enforce at checkout + honest availability on PDP + cart only**; browse/search/recommendation/admin stay raw
   (accepted inconsistency — recorded as an ADR trade).
4. **Bank-transfer = hold with longer TTL + auto-restock if unpaid** (Wave B).
5. **Anti-grief: FULL hardening now** — server-minted signed guest-session token (Wave A0) + per-IP mint limit +
   per-variant anon-hold cap + **per-user authed hold cap** + rate-limit on create-intent & order-create.

---

## THE CONCURRENCY MECHANISM (the crux — implement EXACTLY this)

### Invariant (clock-independent)
`product_variants.reserved_quantity == Σ quantity of stock_reservations with status='Active'` for that variant —
**regardless of `expires_at`.** An expired-but-unswept Active hold STILL counts and STILL protects its unit;
`available = GREATEST(stock_quantity − reserved_quantity, 0)` is therefore briefly *pessimistic* between a hold's
expiry and the next sweep (SAFE direction — never oversell). The **sweeper is the sole releaser of expired holds**.

### P0 — serialization primitive: `SELECT ... FOR UPDATE` on the variant row
Every mutator of a variant's holds/counter (P1 reserve, P2 consume, P3 release/expire, R reconcile, the
reserved-aware decrement) **first locks the variant row** (`SELECT id FROM product_variants WHERE id=@v FOR UPDATE`),
THEN reads from-state, THEN writes — so all concurrent hold-mutations of a variant serialize. **(R3-Low) Every helper
RELOADS the reservation's `status`/`quantity`/`expires_at` from the DB AFTER acquiring the lock — never trust a
pre-lock DTO** (a `reservation` argument is an identity handle, not a trusted snapshot). **Lock-ordering is a
CONTRACT on EVERY multi-variant loop** (reserve batch, consume loop, bank-decrement loop, cart-merge release loop,
reconciler batch): acquire locks in **ascending `variant_id`** order (dedup variant ids first). The lock is held
only for cheap in-DB statements — **NEVER across the Stripe network call.** All multi-statement bodies run inside
`CreateExecutionStrategy().ExecuteAsync`.

### P1 — `TryReserve(variantId, cartId, targetQty, kind, ttl, ct)` (reconcile-to-target, under the lock)
1. Lock the variant row.
2. Read THIS cart's hold for `(cartId, variantId)`. **If a `status='Active'` row exists (expired or not), treat its
   qty as the live `E`** — it still counts; do NOT treat it as `E=0` (that would double-count). If none → `E=0`.
3. `delta = targetQty − E`:
   - `delta == 0`: CAS `UPDATE stock_reservations SET expires_at=now()+@ttl WHERE id=@r AND status='Active'`
     (re-assert liveness + refresh the lease). No counter change (qty already counted).
   - `delta > 0`: `UPDATE product_variants SET reserved_quantity = reserved_quantity + @delta
     WHERE id=@v AND (stock_quantity − reserved_quantity) >= @delta`. `rows==0` → **insufficient** → throw
     (whole-batch rollback). `rows==1` → upsert the ledger row to `(Active, qty=targetQty, expires_at=now()+@ttl)`
     (and refresh expiry).
   - `delta < 0`: guarded `UPDATE product_variants SET reserved_quantity = reserved_quantity − @|delta|
     WHERE id=@v AND reserved_quantity >= @|delta|` (**no `GREATEST` floor**; `rows==0` = invariant violation →
     assert/alert); set ledger qty=targetQty, or P3-Release if targetQty==0.
Counter moves as a function of the ledger op's actual effect, read **under the lock** → exact and convergent for
BOTH the isolated-retry and the overlapping-writer cases.

### P2 — `TryConsumeReservation(reservation, orderId, orderLineQty, ct)` (hold→sold; decrement FIRST)
1. Lock the variant row.
2. **Assert `reservation.Quantity == orderLineQty`** (else → re-reserve/refund path; protects Wave B which has no
   Stripe amount check).
3. `UPDATE product_variants SET stock_quantity = stock_quantity − @q, reserved_quantity = reserved_quantity − @q
   WHERE id=@v AND stock_quantity >= @q AND reserved_quantity >= @q` (physical sale + hold release together,
   decrement BEFORE the status flip). `rows==0` → fail → refund path.
4. Only on `rows==1`: `UPDATE stock_reservations SET status='Consumed', order_id=@o WHERE id=@r AND status='Active'`.
Consumes **any `status='Active'` hold (expired or not)** — an Active hold still counts/protects its unit, so
completing a slightly-late checkout is correct and refund-free. Decrement-before-flip means a failure leaves the
hold Active (recoverable) rather than phantom-Consumed.

### P3 — `TryReleaseReservation(reservation, newStatus, ct)`
1. Lock the variant row. 2. **For `newStatus=Expired`**: CAS `UPDATE stock_reservations SET status='Expired'
   WHERE id=@r AND status='Active' AND expires_at <= now()` (so it can NOT expire a hold P1 just refreshed).
   **For `newStatus=Released`** (cart-clear/remove/merge): CAS `WHERE id=@r AND status='Active'` (explicit release).
   3. Only on `rows==1`: guarded `UPDATE product_variants SET reserved_quantity = reserved_quantity − @q
   WHERE id=@v AND reserved_quantity >= @q` (`rows==0` → assert/alert).

### Reserved-aware decrement — make `TryDecrementVariantStockAsync` honor holds (the universal guard)
Change the sole physical-decrement primitive to `... WHERE id=@v AND (stock_quantity − reserved_quantity) >= @q`
and **route EVERY non-consume decrement through it** (card-fallback when a hold genuinely expired, **bank order-create
in Wave A**, any future path). This is what makes `reserved_quantity` load-bearing: no path can take a unit another
cart holds. (P2 decrements stock AND reserved together, converting its OWN hold, so it is unaffected by the guard.)

### R — drift reconciler (self-heal; in the sweeper tick)
Per variant touched, under the variant lock: `reserved_quantity = COALESCE((SELECT SUM(quantity) FROM
stock_reservations WHERE variant_id=@v AND status='Active'),0)` (**Active-STATUS, clock-independent** — matches the
invariant; safe because the lock serializes it vs P1/P2/P3). Emit a drift metric when it corrects a row.

### Order-create idempotency (pre-existing bug, fully closed here because consume rides this delegate)
1. Generate `order.Id` + `orderNumber` **OUTSIDE** the retried delegate (deterministic per command).
2. At the TOP of the delegate, **look up an existing order by BOTH `order.Id` AND `payment_intent_id`**; if found
   → return it idempotently (no refund, no re-consume, no cart re-read). (order.Id covers bank+card+self-retry;
   payment_intent_id covers a concurrent sibling with a different id but the same intent.)
3. **Guard EVERY refund call-site** (`RefundOrphanedChargeAsync` + the 6 sites at `CreateOrderCommand.cs`
   154/173/180/266/275/285/316): re-query "does a committed order already exist for this PaymentIntentId?"
   immediately before issuing the refund; if yes → return that order idempotently, do NOT refund. Closes the
   concurrent double-submit "refund a placed order → shipped-for-free" hole.
4. Extend the `DbUpdateException` catch to treat the **orders PK conflict** (deterministic order.Id) as an
   idempotent duplicate (return the existing order), not a rethrow.
5. In consume, disambiguate P2 `rows==0`: `Consumed` with THIS `order_id` → idempotent no-op; `Consumed` with a
   DIFFERENT `order_id` → sibling → let the unique index reject, do NOT fallback-decrement; genuinely Absent →
   **re-reserve (P1) first**, then consume; only refund if re-reserve fails (truly unavailable).

---

## Wave A0 — server-minted signed guest-session token, shipped DARK (PR 1) ✅ DONE
> **Decomposition (diff-council R1):** wiring carts to *prefer* the cookie while checkout still used legacy ids
> was a half-switch (3 Highs: legacy carts orphaned, checkout unwired, over-cap legacy bypass). Resolved by
> shipping A0 **dark** — the cookie is minted/validated/published but NOT authoritative for cart-keying. The
> authoritative flip moved to **Wave A** (below), where it couples with reservations (legacy-reject-for-holds).
- Server issues a **signed httpOnly cookie** (`cs_guest` in dev; **`__Host-cs_guest` + Secure** on any deployed
  HTTPS env) = `{id}.{expUnixSeconds}.{HMAC-SHA256(key, "id.exp")}`; key derived `HMAC(jwtSecret,
  "climasite-guest-session-v1")` (no new secret; inherits the JWT prod fail-fast). CSPRNG 128-bit id;
  constant-time verify; **cryptographically-enforced expiry**. Validated + published on `IGuestSessionAccessor`
  on every path; **minted only on `/api/cart`** (Wave A adds `/api/payments`,`/api/orders`), under a per-IP mint
  cap (`GuestSessionMintLimiter`, `Interlocked`, RemoteIpAddress-keyed).
- **Zero behavior change:** `CartController` byte-identical to `main`; carts still key off the legacy id. FE cart
  calls send `withCredentials` (readies Wave A). Council: design R3 CONVERGED; diff R1 REWORK→dark→R2
  APPROVE-WITH-CHANGES (all applied). Tests: token 18, middleware 5, limiter 4, FE 38 (Core 430/App 986/Api 488).
  `/acceptance` PASS at `.planning/acceptance/INV-01-a0-guest-token.md`.

## Wave A — card checkout reservations (PR 2; the core) ✅ DONE — #100/#101 `06bcae7`/`51fd8bf`
- **Guest-identity switch (from A0's dark ship — do this FIRST in Wave A):** flip cart + checkout
  (`CartController`, `PaymentsController` create-intent, `OrdersController` create-order) to key the guest off the
  server-trusted `IGuestSessionAccessor` id; FE payment/checkout services send `withCredentials`; add
  `/api/payments`,`/api/orders` to `MintPathPrefixes`; **migrate legacy carts** (re-key `carts.session_id` from a
  supplied legacy id to the cookie id on first cookie interaction, merge if both exist); **legacy ids may NOT
  create reservations** (reservation-bearing flows require the signed cookie); over-cap on a mint-required path →
  reject rather than fall back to a spoofable legacy id. Re-introduce the `AllowLegacyId` flag for the transition.
- New entity `StockReservation : BaseEntity` (`VariantId` FK CASCADE · `Quantity`>0 · `Status`
  `Active|Consumed|Released|Expired` **stored as string** `HasConversion<string>().HasMaxLength(20)` · `ExpiresAt` ·
  `CartId` FK carts **ON DELETE SET NULL** · `PaymentIntentId?` · `OrderId?` · `Kind` `Card|BankTransfer` string).
- Column `product_variants.reserved_quantity` int **NOT NULL `.HasDefaultValue(0)`**.
- Migration `AddStockReservations` (snake_case, `ValueGeneratedNever`, `NOW()` defaults): filtered **UNIQUE
  `(cart_id, variant_id) WHERE status='Active'`** (literal must byte-match the model `HasFilter`); indexes
  `(status, expires_at)`, `(variant_id, status)`, `(payment_intent_id)`, `(order_id)`. Add DbSet to
  `ApplicationDbContext` + `IApplicationDbContext` (EnsureCreated parity). **Model-parity guard test.**
- **Reserve at `CreatePaymentIntentCommand`, BEFORE the Stripe intent:** in ONE execution-strategy transaction,
  lock+reserve ALL lines (P1) in ascending `variant_id` order; any insufficient line → whole-tx rollback →
  `Result.Failure` naming short items — **all before any Stripe call.** Commit reservations, THEN create the intent
  (never inside the retry delegate). Stamp `payment_intent_id` onto the cart's Active holds. Enforce **per-variant
  anon-hold cap** (guest cart) + **per-user authed hold cap** in P1. **Rate-limit create-intent** (strict/strict-user)
  + reuse the client idempotency key.
- **Consume at `CreateOrderCommand`:** join holds by `cart_id`; consume loop **sorted by variant_id**; P2 per line +
  the order-create idempotency guards above. Order commits `Pending`; webhook unchanged. **Rate-limit `POST /api/orders`.**
- **Sweeper** `StockReservationSweeperBackgroundService` (clone `EmailOutboxBackgroundService`): **enabled-by-default
  in Production, fail-fast at startup if disabled in Prod**, per-tick `try/catch`, poll ~30–60 s, fresh scope,
  `Where(status='Active' AND kind='Card' AND expires_at<now()).OrderBy(expires_at).Take(batch)` → P3(Expired); then a
  reconciler (R) pass. Emits active-hold count + oldest-hold-age metrics; alert if oldest > 2×TTL. Disabled in Testing.
- **Cart-lifecycle release (complete):** `ClearCart`/`RemoveFromCart`/qty-decrease → P1-reconcile (Release at 0);
  `MergeGuestCartCommand` → release the guest cart's Active holds (loop sorted by variant_id) before delete; GDPR/
  account-delete + guest-cart cleanup enumerated as release points; `cart_id ON DELETE SET NULL` → orphan holds still
  expire via the sweeper (self-heal).
- **Availability (PDP + cart only):** `GetProductBySlugQuery` → `ReservedQuantity=v.ReservedQuantity`,
  `AvailableQuantity=GREATEST(stock−reserved,0)`; `GetCartQuery` + add/update guards → `GREATEST(stock−reserved,0)`.
  Everything else raw (decision #3).
- **Admin reserved-awareness:** `AdjustStockCommand` guard `stock+change ≥ reserved_quantity`; `BulkAdjustStock`
  reject/clamp if absolute set `< reserved_quantity`.
- **MockDbContext + IApplicationDbContext:** in-memory mirrors of P1/P2/P3/reserved-aware-decrement/R (necessary,
  NOT sufficient — see test gates).

## Wave B — bank-transfer hold-with-expiry (PR 3) ✅ DONE — #102 `9dbe3ff`
- Bank order-create **reserves only** (a `Kind=BankTransfer` hold via P1, respecting `available = stock − reserved`
  so it can't steal a card hold; loop **sorted by variant_id**), linked to the order, `ExpiresAt=now()+N days`
  (config) — **no physical `stock_quantity` decrement at order-create.** Payment-confirmed (admin mark-paid) → the
  **sorted P2 consume loop** does the physical decrement (hold→sold). Expiry →
  sweeper bank branch → P3 release + cancel order (no restock needed). Filtered **UNIQUE `(order_id, variant_id)
  WHERE status='Active' AND kind='BankTransfer'`** (cart_id null won't dedupe). **Bank order-create idempotency via
  the deterministic-order.Id lookup + PK-conflict catch** (no intent to key on). `CancelOrderCommand`: Consumed card
  order → **atomic `ExecuteUpdate(stock += qty)`** restock (not tracked `AdjustStock`); Active bank hold → P3 release.

## Acceptance criteria (per wave; all break-probe-verified)
- [x] **A:** N concurrent create-intents for the last unit → exactly ONE reserve succeeds; the others fail **before
      any Stripe charge**; `reserved_quantity` never exceeds stock and equals `Σ Active` after settle; two
      independent carts race → one winner. A card hold is NOT drainable by a concurrent bank order or admin edit
      (reserved-aware). Abandon → hold swept → unit returns to availability. Order-create is idempotent under retry
      (exactly one order, zero spurious refund). PDP/cart show `available = stock − reserved`. Light+dark, EN/BG/DE.
- [x] **A0:** guest checkout works via the signed cookie; a forged/tampered cookie is rejected; a legacy id cannot
      create a hold; per-IP mint + per-variant/per-user hold caps enforced (429 / rejected past budget).
- [x] **B:** an unpaid bank order's hold auto-expires and the order auto-cancels (stock never leaked); mark-paid
      consumes the hold (stock−1); cancel of a Consumed card order restocks atomically.

## Test / verification plan
- **Integration (Testcontainers) — MANDATORY merge-gate break-probes that MUST fail with the fix removed:**
  (A) double create-intent same cart → over-reserve — fails without P0 lock; (B) reserve vs sweeper-expire →
  oversell — fails without the choice-X counter / P3 expiry-CAS; (S) sibling double-submit where Req1 commits
  between Req2's top-lookup and its cart-read → must NOT refund a placed order; (D) two carts, shared variants in
  reverse item order → deadlocks without ascending-variant_id locking. Plus: consume hold→sold; reserved-aware guard
  blocks bank/admin drain; sweeper release + reconciler heal of an injected drift; `ExecutionStrategyAttempts=2`
  net-once consume + net-once bank order; anon/user hold caps; guest-token verify; create-intent + order rate-limit 429.
- **Application unit (MockDbContext + `ExecutionStrategyAttempts=2`):** P1 reconcile idempotency (own-Active-hold
  reuse leaves `reserved == Σ live` — no transient inflation), P2 decrement-first + qty assertion + rows==0
  disambiguation, P3 expiry-CAS + floor guard, reserved-aware decrement. **These are necessary-not-sufficient**
  (MockDbContext can't model FOR UPDATE) — the concurrency proof lives in the Testcontainers gates above.
- **Migration:** applies cleanly; table + column + both filtered-unique indexes exist; EnsureCreated model parity.
- **Frontend (Jasmine):** PDP/cart `available=stock−reserved` + "only X left"; guest-cookie flow; i18n en/bg/de.
- **/acceptance (REAL stack, per wave):** two concurrent buyers race the last unit → loser blocked **before payment**;
  abandon → hold expires/swept → unit returns; PDP/cart honest; guest signed-cookie checkout. Committed PASS at
  `.planning/acceptance/INV-01-...-wave{A0,A,B}.md` matching the merged tip.
- **CI** (six checks) = evidence of record. **Cross-vendor Codex council on each wave's diff = hard merge bar.**

## Council traceability (design R1 REWORK → R2 REWORK-narrow → folded)
- **R1 (architecture):** P1 stale-delta race, reserved_quantity decorative vs non-reserving decrements, order-create
  non-idempotent, partial-reserve crash, GREATEST-hides-drift, admin under-cut, anon inventory-denial, Wave-B null
  cart_id → resolved by **P0 FOR UPDATE**, **reserved-aware universal decrement**, **order-create idempotency**,
  **whole-cart-reserve-before-Stripe**, **guarded decrements + reconciler**, **reserved-aware admin guards**,
  **guest-token + caps**, **(order_id,variant_id) filtered unique**.
- **R2 (expiry/refund/lock edges):** P3 must expiry-CAS; expired-Active still consumable/counted (choice-X);
  lazy-expire double-count; lock-ordering on all loops; refund-a-placed-order via later refund sites; anon-only cap
  (add authed cap); Wave-B idempotency-by-order.Id; cancel tracked-restock; create-intent unrate-limited;
  legacy-ids-can't-reserve; MockDbContext necessary-not-sufficient → **ALL folded above.**

## Out of scope
Add-to-cart reservations; reservation-adjusted availability on browse/search/recommendation/admin (decision #3);
distributed/Redis locks (single DB instance); 3DS/SCA async-auth reservation semantics; full guest-token rollout
beyond cart/checkout; backfilling `reserved_quantity`. **Accepted residual:** authed grief bound = (per-user cap ×
active accounts × TTL churn) — recorded, not eliminated; registration rate-limiting is the mitigation lever.
