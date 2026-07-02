using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reservations;

/// <summary>
/// Orchestrates the INV-01 A2 stock-reservation concurrency mechanism over the atomic primitives on
/// <see cref="IApplicationDbContext"/>. Every variant mutation runs under a per-variant <c>SELECT ... FOR
/// UPDATE</c> lock (P0); every multi-variant loop acquires locks in ascending <c>variant_id</c> order; the
/// denormalised counter moves only via from-state-gated SQL (never a tracked load-increment-save). The
/// invariant is clock-independent: <c>reserved_quantity == Σ Active holds</c> for a variant, regardless of
/// expiry — the sweeper is the sole releaser of expired holds. See the unit-plan §"THE CONCURRENCY MECHANISM".
/// <para>
/// The service is deliberately CHANGE-TRACKER-NEUTRAL: every read is <c>AsNoTracking</c> and every write is
/// a raw from-state-gated SQL primitive, so it neither tracks entities nor clears the tracker. That makes it
/// safe to call mid-handler (a caller's tracked, not-yet-saved cart survives the call) AND retry-safe (a
/// commit-unknown retry re-reads committed state rather than a stale first-attempt snapshot).
/// </para>
/// </summary>
public sealed class StockReservationService : IStockReservationService
{
    private readonly IApplicationDbContext _context;
    private readonly ReservationOptions _options;

    public StockReservationService(IApplicationDbContext context, ReservationOptions options)
    {
        _context = context;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<ReservationResult> ReserveCartAsync(
        Guid cartId,
        IReadOnlyList<ReservationRequestLine> lines,
        ReservationKind kind,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        // Anti-grief caps FIRST — reject before any DB work (and before any Stripe call). A cart is 1:1 with its
        // identity (guest cookie or user), so MaxActiveLinesPerCart bounds both the anon and the authed hold count.
        foreach (var line in lines)
        {
            if (line.Quantity > _options.EffectiveMaxUnitsPerVariant)
            {
                return ReservationResult.Failure(
                    $"You can reserve at most {_options.EffectiveMaxUnitsPerVariant} of '{line.DisplayName}'.");
            }
        }

        if (lines.Count > _options.EffectiveMaxActiveLinesPerCart)
        {
            return ReservationResult.Failure(
                $"A cart can hold at most {_options.EffectiveMaxActiveLinesPerCart} distinct items during checkout.");
        }

        if (lines.Count == 0)
        {
            return ReservationResult.Success();
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                // Lock-ordering CONTRACT: acquire per-variant locks in ascending id order to avoid deadlocks with
                // any other multi-variant loop (consume, bank-decrement, release, reconcile).
                foreach (var line in lines.OrderBy(l => l.VariantId))
                {
                    await _context.LockVariantForUpdateAsync(line.VariantId, cancellationToken);

                    // Reconcile-to-target: an existing Active hold (expired or not) still counts as the live E, so its
                    // qty is reused and the counter moves only by the delta — a retry / same-cart writer converges.
                    var hold = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                        r => r.CartId == cartId && r.VariantId == line.VariantId && r.Status == ReservationStatus.Active,
                        cancellationToken);
                    var existing = hold?.Quantity ?? 0;
                    var target = line.Quantity;
                    var delta = target - existing;
                    var expiresAt = DateTime.UtcNow.AddMinutes(_options.EffectiveHoldTtlMinutes);

                    if (delta == 0)
                    {
                        // qty already counted — just re-assert liveness + refresh the lease.
                        await _context.RefreshReservationExpiryAsync(hold!.Id, expiresAt, cancellationToken);
                    }
                    else if (delta > 0)
                    {
                        var incremented = await _context.TryIncrementReservedQuantityAsync(
                            line.VariantId, delta, cancellationToken);
                        if (incremented == 0)
                        {
                            // Insufficient available stock — roll the WHOLE batch back and name the short item.
                            throw new ReservationShortfallException(line.DisplayName);
                        }

                        if (hold is null)
                        {
                            var inserted = await _context.InsertActiveReservationAsync(
                                Guid.NewGuid(), line.VariantId, cartId, target, expiresAt, kind.ToString(), cancellationToken);
                            if (inserted == 0)
                            {
                                // Under the FOR UPDATE lock a concurrent same-cart insert is not possible; on a
                                // commit-unknown retry the row already exists, so reuse it. We already bumped the
                                // counter by @delta above, but that pre-existing hold ALSO already counted its old
                                // qty, so recompute the counter to the authoritative Σ Active — never leave drift.
                                var live = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                                    r => r.CartId == cartId && r.VariantId == line.VariantId && r.Status == ReservationStatus.Active,
                                    cancellationToken);
                                if (live is not null)
                                {
                                    await _context.SetReservationQuantityAndExpiryAsync(
                                        live.Id, target, expiresAt, cancellationToken);
                                }

                                var sum = await _context.SumActiveReservedQuantityAsync(line.VariantId, cancellationToken);
                                await _context.SetVariantReservedQuantityAsync(line.VariantId, sum, cancellationToken);
                            }
                        }
                        else
                        {
                            await _context.SetReservationQuantityAndExpiryAsync(hold.Id, target, expiresAt, cancellationToken);
                        }
                    }
                    else
                    {
                        // Shrink an existing hold (the cart reduced this line before re-running create-intent).
                        await _context.TryDecrementReservedQuantityAsync(line.VariantId, -delta, cancellationToken);
                        await _context.SetReservationQuantityAndExpiryAsync(hold!.Id, target, expiresAt, cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (ReservationShortfallException ex)
        {
            return ReservationResult.Failure($"Insufficient stock for '{ex.DisplayName}'.");
        }

        return ReservationResult.Success();
    }

    /// <inheritdoc />
    public Task StampPaymentIntentAsync(Guid cartId, string paymentIntentId, CancellationToken cancellationToken = default)
        // Single idempotent UPDATE — no transaction needed; runs after the intent is created, outside any reserve
        // tx. DIAGNOSTIC/linkage ONLY: a hold swept between the reserve commit and this call is stamped with
        // nothing, so this value is best-effort. The authoritative one-order-per-intent guarantee is the unique
        // index on orders.payment_intent_id, never this stamp.
        => _context.StampReservationsPaymentIntentAsync(cartId, paymentIntentId, cancellationToken);

    /// <inheritdoc />
    public async Task<ReservationConsumeOutcome> ConsumeLineAsync(
        Guid cartId,
        Guid variantId,
        Guid orderId,
        int orderLineQuantity,
        ReservationKind kind,
        CancellationToken cancellationToken = default)
    {
        // Runs INSIDE the caller's (CreateOrder's) open execution-strategy transaction — no new transaction here.
        await _context.LockVariantForUpdateAsync(variantId, cancellationToken);

        var active = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
            r => r.CartId == cartId && r.VariantId == variantId && r.Status == ReservationStatus.Active,
            cancellationToken);

        Guid holdId;
        if (active is not null)
        {
            if (active.Quantity != orderLineQuantity)
            {
                // A card hold's quantity always matches its cart line (reserve + order both use the cart quantities).
                // A mismatch is drift we must not silently mis-sell — bail to the caller's refund path.
                return ReservationConsumeOutcome.Unavailable;
            }

            holdId = active.Id;
        }
        else
        {
            // No Active hold. If THIS order already consumed one, it's an idempotent retry — no-op.
            if (await _context.StockReservations.AsNoTracking().AnyAsync(
                    r => r.CartId == cartId && r.VariantId == variantId
                      && r.Status == ReservationStatus.Consumed && r.OrderId == orderId,
                    cancellationToken))
            {
                return ReservationConsumeOutcome.AlreadyConsumedByThisOrder;
            }

            // Otherwise the hold is genuinely absent for THIS order — either swept between reserve and
            // order-create, or a stale Consumed row from a PRIOR order on this reused (persistent) cart. Either
            // way we must re-reserve then sell for the current order; a Consumed row from another order is NOT a
            // reason to skip the sale (that under-sells and leaks phantom stock). True concurrent same-intent
            // siblings stay safe without a "rejected" branch: the FOR UPDATE lock serialises them, the loser's
            // re-reserve is stock-gated, and the top-of-delegate FindPlacedOrderAsync + the unique
            // orders.payment_intent_id index return the already-placed order idempotently (no double decrement).
            if (await _context.TryIncrementReservedQuantityAsync(variantId, orderLineQuantity, cancellationToken) == 0)
            {
                return ReservationConsumeOutcome.Unavailable;
            }

            holdId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(_options.EffectiveHoldTtlMinutes);
            if (await _context.InsertActiveReservationAsync(
                    holdId, variantId, cartId, orderLineQuantity, expiresAt, kind.ToString(), cancellationToken) == 0)
            {
                // Unreachable under the FOR UPDATE lock; undo the speculative reserve and bail defensively.
                await _context.TryDecrementReservedQuantityAsync(variantId, orderLineQuantity, cancellationToken);
                return ReservationConsumeOutcome.Unavailable;
            }
        }

        // Decrement stock AND reserved together (converting this cart's own hold), BEFORE flipping the status — a
        // failure leaves the hold Active (recoverable) rather than phantom-Consumed.
        if (await _context.TryConsumeVariantStockAsync(variantId, orderLineQuantity, cancellationToken) == 0)
        {
            return ReservationConsumeOutcome.Unavailable;
        }

        await _context.TryConsumeReservationRowAsync(holdId, orderId, cancellationToken);
        return ReservationConsumeOutcome.Consumed;
    }

    /// <inheritdoc />
    public async Task ReleaseCartAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var variantIds = await _context.StockReservations
                .Where(r => r.CartId == cartId && r.Status == ReservationStatus.Active)
                .Select(r => r.VariantId)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync(cancellationToken);

            foreach (var variantId in variantIds)
            {
                await ReleaseOneAsync(cartId, variantId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task ReleaseCartVariantAsync(Guid cartId, Guid variantId, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await ReleaseOneAsync(cartId, variantId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task ReconcileCartToQuantitiesAsync(
        Guid cartId,
        IReadOnlyDictionary<Guid, int> cartQuantities,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var variantIds = await _context.StockReservations
                .Where(r => r.CartId == cartId && r.Status == ReservationStatus.Active)
                .Select(r => r.VariantId)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync(cancellationToken);

            foreach (var variantId in variantIds)
            {
                await _context.LockVariantForUpdateAsync(variantId, cancellationToken);

                var hold = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                    r => r.CartId == cartId && r.VariantId == variantId && r.Status == ReservationStatus.Active,
                    cancellationToken);
                if (hold is null)
                {
                    continue;
                }

                var target = cartQuantities.TryGetValue(variantId, out var q) ? q : 0;

                // Never GROW a hold here — growth only happens at reserve (stock-gated).
                if (target >= hold.Quantity)
                {
                    continue;
                }

                if (target <= 0)
                {
                    if (await _context.TryReleaseReservationAsync(hold.Id, cancellationToken) == 1)
                    {
                        await _context.TryDecrementReservedQuantityAsync(variantId, hold.Quantity, cancellationToken);
                    }
                }
                else
                {
                    var shrink = hold.Quantity - target;
                    await _context.TryDecrementReservedQuantityAsync(variantId, shrink, cancellationToken);
                    // Keep the existing lease on a shrink (do not refresh expiry).
                    await _context.SetReservationQuantityAndExpiryAsync(hold.Id, target, hold.ExpiresAt, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<int> SweepExpiredHoldsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var due = await _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Active
                     && r.Kind == ReservationKind.Card
                     && r.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(r => r.ExpiresAt)
            .Take(batchSize)
            .Select(r => new { r.Id, r.VariantId })
            .ToListAsync(cancellationToken);

        var expired = 0;
        var touched = new HashSet<Guid>();

        foreach (var hold in due)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            var didExpire = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await _context.LockVariantForUpdateAsync(hold.VariantId, cancellationToken);

                // Reload qty under the lock (never trust the pre-lock snapshot).
                var current = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                    r => r.Id == hold.Id, cancellationToken);

                var ok = false;
                if (current is { Status: ReservationStatus.Active }
                    && await _context.TryExpireReservationAsync(hold.Id, cancellationToken) == 1)
                {
                    await _context.TryDecrementReservedQuantityAsync(hold.VariantId, current.Quantity, cancellationToken);
                    ok = true;
                }

                await transaction.CommitAsync(cancellationToken);
                return ok;
            });

            if (didExpire)
            {
                expired++;
            }

            touched.Add(hold.VariantId);
        }

        // Self-heal: reconcile each touched variant's counter to Σ Active (clock-independent), under its lock.
        foreach (var variantId in touched.OrderBy(v => v))
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await _context.LockVariantForUpdateAsync(variantId, cancellationToken);
                var sum = await _context.SumActiveReservedQuantityAsync(variantId, cancellationToken);
                await _context.SetVariantReservedQuantityAsync(variantId, sum, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }

        return expired;
    }

    // ---- INV-01 Wave B: bank-transfer hold-with-expiry ----

    /// <inheritdoc />
    public async Task<ReservationResult> ReserveBankOrderAsync(
        Guid orderId,
        IReadOnlyList<ReservationRequestLine> lines,
        CancellationToken cancellationToken = default)
    {
        // Runs INSIDE the caller's (order-create's) open execution-strategy transaction — no new transaction here.
        // A fresh order has no holds, but the (order, variant) idempotency check makes a commit-unknown retry a
        // no-op (belt-and-suspenders: order-create's top-of-delegate FindPlacedOrder already short-circuits).
        var expiresAt = DateTime.UtcNow.AddDays(_options.EffectiveBankHoldExpiryDays);

        // Lock-ordering CONTRACT: ascending variant_id, matching every other multi-variant loop (reserve, consume,
        // release, sweep, reconcile) so a bank reserve never deadlocks against them.
        foreach (var line in lines.OrderBy(l => l.VariantId))
        {
            await _context.LockVariantForUpdateAsync(line.VariantId, cancellationToken);

            var existing = await _context.StockReservations.AsNoTracking().AnyAsync(
                r => r.OrderId == orderId && r.VariantId == line.VariantId
                  && r.Kind == ReservationKind.BankTransfer && r.Status == ReservationStatus.Active,
                cancellationToken);
            if (existing)
            {
                // Idempotent: this order already holds this variant — reuse it, do not re-increment the counter.
                continue;
            }

            // Available-gated so a bank hold can take only units NOT held by a card checkout (reserved-aware).
            if (await _context.TryIncrementReservedQuantityAsync(line.VariantId, line.Quantity, cancellationToken) == 0)
            {
                // Insufficient available stock — the caller's transaction rolls back the whole batch (never commits).
                return ReservationResult.Failure($"Insufficient stock for '{line.DisplayName}'.");
            }

            await _context.InsertActiveBankReservationAsync(
                Guid.NewGuid(), line.VariantId, orderId, line.Quantity, expiresAt, cancellationToken);
        }

        return ReservationResult.Success();
    }

    /// <inheritdoc />
    public async Task<BankConsumeResult> ConsumeBankOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Runs INSIDE the caller's (mark-paid's) open transaction, with the caller HOLDING THE ORDER LOCK so no
        // concurrent transition can add/expire this order's holds mid-loop. Returns (expected, consumed) so the
        // caller can enforce ALL-OR-NOTHING and commit Paid only when EVERY line sold, else roll the tx back.
        //
        // expected = every bank hold that still represents an UNSOLD line (any status except Consumed): if even one
        // line's hold has gone Expired/Released, the order can't be fully fulfilled and mark-paid must fail. Only
        // Active holds are consumable, so a pre-expired line makes consumed < expected ⇒ NOT AllConsumed.
        var expected = await _context.StockReservations.AsNoTracking().CountAsync(
            r => r.OrderId == orderId && r.Kind == ReservationKind.BankTransfer
              && r.Status != ReservationStatus.Consumed,
            cancellationToken);

        var holds = await _context.StockReservations.AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Kind == ReservationKind.BankTransfer
                     && r.Status == ReservationStatus.Active)
            .Select(r => new { r.Id, r.VariantId })
            .OrderBy(r => r.VariantId)
            .ToListAsync(cancellationToken);

        var consumed = 0;
        foreach (var hold in holds)
        {
            await _context.LockVariantForUpdateAsync(hold.VariantId, cancellationToken);

            // Re-read under the lock (never trust the pre-lock snapshot).
            var current = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                r => r.Id == hold.Id, cancellationToken);
            if (current is not { Status: ReservationStatus.Active })
            {
                continue;
            }

            // Decrement stock AND reserved together (converting THIS hold), by the hold's own quantity, BEFORE the
            // status flip so a failure leaves the hold Active (recoverable) rather than phantom-Consumed.
            if (await _context.TryConsumeVariantStockAsync(hold.VariantId, current.Quantity, cancellationToken) == 0)
            {
                continue;
            }

            await _context.TryConsumeReservationRowAsync(hold.Id, orderId, cancellationToken);
            consumed++;
        }

        return new BankConsumeResult(expected, consumed);
    }

    /// <inheritdoc />
    public async Task ReleaseBankOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Runs INSIDE the caller's (cancel's) open transaction. Release each Active bank hold (ascending
        // variant_id) — drop the counter, flip Released. NO restock: a held bank order was never decremented.
        var variantIds = await _context.StockReservations
            .Where(r => r.OrderId == orderId && r.Kind == ReservationKind.BankTransfer
                     && r.Status == ReservationStatus.Active)
            .Select(r => r.VariantId)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);

        foreach (var variantId in variantIds)
        {
            await _context.LockVariantForUpdateAsync(variantId, cancellationToken);

            var hold = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                r => r.OrderId == orderId && r.VariantId == variantId
                  && r.Kind == ReservationKind.BankTransfer && r.Status == ReservationStatus.Active,
                cancellationToken);
            if (hold is null)
            {
                continue;
            }

            if (await _context.TryReleaseReservationAsync(hold.Id, cancellationToken) == 1)
            {
                await _context.TryDecrementReservedQuantityAsync(variantId, hold.Quantity, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> SweepExpiredBankHoldsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var dueOrderIds = await _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Active
                     && r.Kind == ReservationKind.BankTransfer
                     && r.ExpiresAt <= DateTime.UtcNow
                     && r.OrderId != null)
            .OrderBy(r => r.ExpiresAt)
            .Select(r => r.OrderId!.Value)
            .Distinct()
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var cancelledOrders = 0;

        foreach (var orderId in dueOrderIds)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            var didCancel = await strategy.ExecuteAsync(async () =>
            {
                // Re-derive from committed state each attempt (the retry reuses this scoped context).
                _context.ClearChangeTracker();
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                // ORDER LOCK FIRST — serialise against a concurrent mark-paid / cancel of THIS order (each also
                // takes the order lock before any variant lock). Under it, this order's Active-hold set is stable.
                await _context.LockOrderForUpdateAsync(orderId, cancellationToken);

                var holds = await _context.StockReservations.AsNoTracking()
                    .Where(r => r.OrderId == orderId && r.Kind == ReservationKind.BankTransfer
                             && r.Status == ReservationStatus.Active)
                    .Select(r => new { r.Id, r.VariantId })
                    .OrderBy(r => r.VariantId)
                    .ToListAsync(cancellationToken);

                var expiredAny = false;
                var touched = new HashSet<Guid>();
                foreach (var hold in holds)
                {
                    await _context.LockVariantForUpdateAsync(hold.VariantId, cancellationToken);

                    var current = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
                        r => r.Id == hold.Id, cancellationToken);
                    if (current is { Status: ReservationStatus.Active }
                        && await _context.TryExpireReservationAsync(hold.Id, cancellationToken) == 1)
                    {
                        await _context.TryDecrementReservedQuantityAsync(hold.VariantId, current.Quantity, cancellationToken);
                        expiredAny = true;
                    }

                    touched.Add(hold.VariantId);
                }

                var orderCancelled = false;
                if (expiredAny)
                {
                    // We actually expired holds ⇒ the wire never arrived. Cancel the order — but only from a
                    // cancellable state (a concurrent mark-paid would have CONSUMED the holds under the same
                    // variant lock, so expiredAny would be false and we would not be here).
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
                    if (order is { Status: OrderStatus.Pending })
                    {
                        order.SetCancellationReason("Bank transfer not received before the payment window elapsed.");
                        order.SetStatus(OrderStatus.Cancelled, "Auto-cancelled: bank-transfer hold expired");
                        orderCancelled = true;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Self-heal: reconcile each touched variant's counter to Σ Active (clock-independent), under its lock.
                foreach (var variantId in touched.OrderBy(v => v))
                {
                    await _context.LockVariantForUpdateAsync(variantId, cancellationToken);
                    var sum = await _context.SumActiveReservedQuantityAsync(variantId, cancellationToken);
                    await _context.SetVariantReservedQuantityAsync(variantId, sum, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return orderCancelled;
            });

            if (didCancel)
            {
                cancelledOrders++;
            }
        }

        return cancelledOrders;
    }

    /// <summary>Releases one (cart, variant) Active hold under its lock, dropping the counter by its live quantity.
    /// The caller must already be inside a transaction; the variant is re-read AFTER the lock.</summary>
    private async Task ReleaseOneAsync(Guid cartId, Guid variantId, CancellationToken cancellationToken)
    {
        await _context.LockVariantForUpdateAsync(variantId, cancellationToken);

        var hold = await _context.StockReservations.AsNoTracking().FirstOrDefaultAsync(
            r => r.CartId == cartId && r.VariantId == variantId && r.Status == ReservationStatus.Active,
            cancellationToken);
        if (hold is null)
        {
            return;
        }

        if (await _context.TryReleaseReservationAsync(hold.Id, cancellationToken) == 1)
        {
            await _context.TryDecrementReservedQuantityAsync(variantId, hold.Quantity, cancellationToken);
        }
    }

    /// <summary>Thrown inside the reserve transaction when a line can't be satisfied, to roll the WHOLE batch back
    /// and surface the short item to the caller. Never leaves the service.</summary>
    private sealed class ReservationShortfallException : Exception
    {
        public ReservationShortfallException(string displayName) => DisplayName = displayName;

        public string DisplayName { get; }
    }
}
