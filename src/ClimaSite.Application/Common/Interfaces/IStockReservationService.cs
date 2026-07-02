using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Common.Interfaces;

/// <summary>A single line a cart wants to hold: which variant, how many, and a display name for the
/// short-item message when the reserve can't be satisfied.</summary>
public sealed record ReservationRequestLine(Guid VariantId, int Quantity, string DisplayName);

/// <summary>Outcome of a whole-cart reserve (INV-01 A2). On failure, <see cref="Error"/> names the short
/// items / cap violation for the caller to surface — no Stripe call is ever made on failure.</summary>
public sealed record ReservationResult(bool Succeeded, string? Error)
{
    public static ReservationResult Success() => new(true, null);
    public static ReservationResult Failure(string error) => new(false, error);
}

/// <summary>Per-line outcome of consuming a hold at order-create (P2 + the no-Active-hold disambiguation).</summary>
public enum ReservationConsumeOutcome
{
    /// <summary>The physical sale happened (stock and reserved both decremented; hold flipped Consumed).</summary>
    Consumed,

    /// <summary>THIS order already consumed the hold — an idempotent no-op on a retry (no double decrement).</summary>
    AlreadyConsumedByThisOrder,

    /// <summary>No hold and stock could not be (re-)reserved — genuinely unavailable → the caller refunds.</summary>
    Unavailable
}

/// <summary>
/// Outcome of consuming a bank order's holds at mark-paid (INV-01 B). <see cref="ExpectedHolds"/> is the number
/// of Active bank holds the order had (read under the order lock); <see cref="ConsumedHolds"/> is how many were
/// actually converted to a sale. Mark-paid is ALL-OR-NOTHING: the caller commits the Paid transition only when
/// <see cref="AllConsumed"/> — otherwise it rolls the whole unit of work back (never a partial physical decrement).
/// </summary>
public readonly record struct BankConsumeResult(int ExpectedHolds, int ConsumedHolds)
{
    /// <summary>True only when there was at least one live hold and EVERY one of them was consumed.</summary>
    public bool AllConsumed => ExpectedHolds > 0 && ConsumedHolds == ExpectedHolds;
}

/// <summary>
/// Orchestrates the stock-reservation concurrency mechanism (INV-01 A2): P1 reserve-to-target at
/// checkout-start, P2 consume at order-create, P3 release/expire, and the R reconciler. Every variant mutation
/// serializes on a <c>SELECT ... FOR UPDATE</c> variant-row lock (P0); every multi-variant loop acquires locks
/// in ascending <c>variant_id</c> order; counts move only via atomic, from-state-gated SQL (never a tracked
/// load-increment-save). The invariant is clock-independent: <c>reserved_quantity == Σ Active holds</c>,
/// regardless of expiry — the sweeper is the sole releaser of expired holds. Registered scoped.
/// </summary>
public interface IStockReservationService
{
    /// <summary>
    /// P1 — reserve the WHOLE cart to its target quantities in one execution-strategy transaction, locking each
    /// variant (ascending id) before mutating its counter/ledger. Reconcile-to-target: an existing Active hold
    /// for this cart is reused (its expiry refreshed, counter moved only by the delta), so a retry / concurrent
    /// same-cart writer converges without inflation. Any insufficient line rolls the whole batch back and names
    /// the short items — the caller must NOT charge. Enforces the per-variant + per-cart anti-grief caps first.
    /// </summary>
    Task<ReservationResult> ReserveCartAsync(
        Guid cartId,
        IReadOnlyList<ReservationRequestLine> lines,
        ReservationKind kind,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>Stamp the created PaymentIntent id onto this cart's Active holds (payment linkage + diagnostics),
    /// after the intent is created and outside any reserve transaction. Idempotent.</summary>
    Task StampPaymentIntentAsync(Guid cartId, string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// P2 — consume ONE cart line (hold → sold) WITHIN the caller's already-open order-create transaction: lock
    /// the variant, decrement stock AND reserved together, then flip the hold Consumed with this order id.
    /// Consumes any Active hold (expired or not). When no Active hold exists it is either an idempotent retry of
    /// THIS order (already-Consumed-by-this-order) or genuinely absent for this order (swept, or only a stale
    /// Consumed row from a PRIOR order on the reused cart) → re-reserve then consume. Concurrent same-intent
    /// siblings are made safe by the FOR UPDATE lock + the caller's order idempotency (deterministic order.Id
    /// lookup + unique orders.payment_intent_id), NOT by rejecting here. The caller drives the loop in ascending
    /// <c>variant_id</c> order and refunds only on <see cref="ReservationConsumeOutcome.Unavailable"/>.
    /// </summary>
    Task<ReservationConsumeOutcome> ConsumeLineAsync(
        Guid cartId,
        Guid variantId,
        Guid orderId,
        int orderLineQuantity,
        ReservationKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>P3 — release ALL of a cart's Active holds (cart clear / guest-cart merge before delete). Locks
    /// each variant in ascending id order; the counter drops by each released hold's quantity.</summary>
    Task ReleaseCartAsync(Guid cartId, CancellationToken cancellationToken = default);

    /// <summary>P3 — release a single (cart, variant) Active hold (a cart item removed at checkout).</summary>
    Task ReleaseCartVariantAsync(Guid cartId, Guid variantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shrink/release this cart's holds to match its CURRENT quantities (a qty decrease at checkout). Never
    /// GROWS a hold (growth only happens at reserve, where it is stock-gated) — a variant no longer in the cart
    /// is released; a hold larger than the cart quantity is shrunk. Locks each affected variant (ascending id).
    /// </summary>
    Task ReconcileCartToQuantitiesAsync(
        Guid cartId,
        IReadOnlyDictionary<Guid, int> cartQuantities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// R — one sweeper tick: expire up to <paramref name="batchSize"/> Active card holds whose lease has
    /// elapsed (P3 with an <c>expires_at &lt;= now()</c> CAS so it cannot expire a just-refreshed hold), then
    /// reconcile each touched variant's counter to <c>Σ Active</c>. Returns the number of holds expired.
    /// </summary>
    Task<int> SweepExpiredHoldsAsync(int batchSize, CancellationToken cancellationToken = default);

    // ---- INV-01 Wave B: bank-transfer hold-with-expiry ----

    /// <summary>
    /// Wave B — reserve a bank-transfer order's stock as an <c>Active</c> <c>BankTransfer</c> hold (NO physical
    /// <c>stock_quantity</c> decrement): lock each line's variant (ascending id) and increment
    /// <c>reserved_quantity</c> available-gated (so a bank hold can't steal a card hold's unit), inserting a hold
    /// keyed on the order with <c>expires_at = now + BankHoldExpiryDays</c>. Any short line ⇒ Failure naming it;
    /// the caller's transaction rolls the whole batch back. Idempotent: an existing Active hold for (order,
    /// variant) is reused without re-incrementing. Runs INSIDE the caller's (order-create's) open transaction.
    /// </summary>
    Task<ReservationResult> ReserveBankOrderAsync(
        Guid orderId,
        IReadOnlyList<ReservationRequestLine> lines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Wave B — consume a bank order's Active holds (hold → sold) at admin mark-paid: for each hold (ascending
    /// variant_id, under the lock) decrement stock AND reserved together, then flip <c>Consumed</c>. Returns
    /// <see cref="BankConsumeResult"/> = (holds that were Active, holds actually consumed). The caller must be
    /// holding the ORDER lock so the hold set is stable, and marks the order Paid ONLY when
    /// <see cref="BankConsumeResult.AllConsumed"/> — otherwise it rolls the whole unit of work back (no partial
    /// physical decrement). Runs INSIDE the caller's (mark-paid's) open transaction.
    /// </summary>
    Task<BankConsumeResult> ConsumeBankOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wave B — release a bank order's Active holds on cancellation (drops <c>reserved_quantity</c>; NO restock,
    /// since a held bank order was never physically decremented). Locks each variant ascending id. Runs INSIDE
    /// the caller's (cancel's) open transaction.
    /// </summary>
    Task ReleaseBankOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wave B sweeper branch — for up to <paramref name="batchSize"/> orders holding an Active, expired
    /// <c>BankTransfer</c> hold: expire the holds (drop the counter, ascending variant_id under the lock) and, if
    /// any were actually expired, cancel the still-Pending order (the payment window elapsed unpaid), then
    /// reconcile the touched variants. A concurrent mark-paid consumes the holds first, so its holds are no longer
    /// Active here and the order is NOT cancelled. Returns the number of orders auto-cancelled.
    /// </summary>
    Task<int> SweepExpiredBankHoldsAsync(int batchSize, CancellationToken cancellationToken = default);
}
