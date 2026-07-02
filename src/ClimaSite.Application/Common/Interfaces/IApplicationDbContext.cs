using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClimaSite.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Category> Categories { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderEvent> OrderEvents { get; }
    DbSet<Review> Reviews { get; }
    DbSet<ReviewVote> ReviewVotes { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<Address> Addresses { get; }
    DbSet<RelatedProduct> RelatedProducts { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ProductTranslation> ProductTranslations { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }
    DbSet<PromotionTranslation> PromotionTranslations { get; }
    DbSet<Brand> Brands { get; }
    DbSet<BrandTranslation> BrandTranslations { get; }
    DbSet<CategoryTranslation> CategoryTranslations { get; }
    DbSet<ProductQuestion> ProductQuestions { get; }
    DbSet<ProductAnswer> ProductAnswers { get; }
    DbSet<ProductQuestionVote> ProductQuestionVotes { get; }
    DbSet<ProductAnswerVote> ProductAnswerVotes { get; }
    DbSet<InstallationRequest> InstallationRequests { get; }
    DbSet<ProductPriceHistory> ProductPriceHistory { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<StockReservation> StockReservations { get; }

    /// <summary>
    /// Atomically decrements a variant's stock by <paramref name="quantity"/> only when current
    /// stock is at least that much. Returns rows affected (1 = decremented, 0 = insufficient stock
    /// or missing variant) — the oversell guard for concurrent checkout. (BUG-05)
    /// </summary>
    Task<int> TryDecrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically SETS a variant's stock to an absolute <paramref name="newQuantity"/> only when it stays at or
    /// above the units currently held by open checkouts (<c>reserved_quantity</c>): a single
    /// <c>ExecuteUpdate ... WHERE @new &gt;= reserved_quantity</c>. Returns rows affected (1 = set, 0 = would drop
    /// below reserved, or missing variant). The reserved-aware guard for admin bulk stock edits (INV-01 A2) — a
    /// concurrent reserve committing between a read and a tracked save can no longer strand a card holder.
    /// </summary>
    Task<int> TrySetVariantStockAtOrAboveReservedAsync(Guid variantId, int newQuantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically re-keys every guest cart currently owned by <paramref name="fromSessionId"/> onto
    /// <paramref name="toSessionId"/> in a single set-based UPDATE (no entity load). Returns rows affected
    /// (0 = nothing to migrate → an idempotent no-op on a retry). Used by the guest-identity migration when a
    /// returning guest's legacy cart must move onto the trusted signed-cookie id. (INV-01 A1)
    /// </summary>
    Task<int> RekeyGuestCartAsync(string fromSessionId, string toSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a transaction-scoped Postgres advisory lock keyed on the guest cookie id, serializing concurrent
    /// same-cookie cart migrations so the merge branch never races (a duplicate <c>(cart_id, variant_id)</c>
    /// insert on the shared cookie cart, or a 0-row legacy delete). Auto-released on commit/rollback; a no-op
    /// off Postgres (unit tests are single-threaded). Must be called inside the migration transaction. (INV-01 A1)
    /// </summary>
    Task AcquireGuestCartMigrationLockAsync(string cookieSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches all tracked entities (resets the EF change tracker). <c>EnableRetryOnFailure</c> reuses the
    /// request-scoped context across execution-strategy attempts and does NOT reset the tracker on a rollback,
    /// so a retried multi-step operation must clear it at the top of each attempt to re-derive from committed
    /// state rather than stale prior-attempt mutations. No-op on the in-memory test double. (INV-01 A1)
    /// </summary>
    void ClearChangeTracker();

    // ---- B-039: per-voter Q&A vote atomics ----
    // Conditional, rows-affected-gated SQL primitives for the vote ledger. The handler orchestrates the
    // transaction + gates each denormalised count delta on the rows these return; it never uses the
    // tracked Add/Remove entity methods (which would lose concurrent updates). See unit-plan §9.

    /// <summary>
    /// Inserts a question "helpful" vote with <c>ON CONFLICT (question_id, user_id) DO NOTHING</c>.
    /// Returns 1 when this call created the row, 0 when the user had already voted (no exception, so
    /// the surrounding transaction never aborts).
    /// </summary>
    Task<int> TryInsertQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user's question vote row. Returns rows deleted (1 = removed, 0 = none).</summary>
    Task<int> DeleteQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically adjusts a question's denormalised helpful count by <paramref name="delta"/> (±1).
    /// A negative delta is floored at zero so it can never unwind past the legacy anonymous baseline.
    /// </summary>
    Task<int> AdjustQuestionHelpfulCountAsync(Guid questionId, int delta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an answer vote with <c>ON CONFLICT (answer_id, user_id) DO NOTHING</c>. Returns 1 when
    /// this call created the row, 0 when the user had already voted.
    /// </summary>
    Task<int> TryInsertAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the user's answer vote row only when it still has the expected direction
    /// (<paramref name="isHelpful"/>). Returns rows deleted (1 = removed, 0 = none).
    /// </summary>
    Task<int> DeleteAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flips the user's answer vote from <paramref name="fromHelpful"/> to <paramref name="toHelpful"/>
    /// only when the row still has the old direction. Returns 1 when it flipped, 0 otherwise — the gate
    /// for applying both count deltas exactly once under a concurrent flip.
    /// </summary>
    Task<int> FlipAnswerVoteAsync(Guid answerId, Guid userId, bool fromHelpful, bool toHelpful, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically adjusts an answer's denormalised helpful (<paramref name="helpful"/> = true) or
    /// unhelpful count by <paramref name="delta"/> (±1). A negative delta is floored at zero.
    /// </summary>
    Task<int> AdjustAnswerVoteCountAsync(Guid answerId, bool helpful, int delta, CancellationToken cancellationToken = default);

    // ---- INV-01 A2: stock-reservation atomic primitives ----
    // The single serialization primitive is a per-variant SELECT ... FOR UPDATE row lock (P0); every hold /
    // counter mutation below is executed under it, inside the caller's execution-strategy transaction. Counts
    // (product_variants.reserved_quantity) move ONLY via these from-state-gated statements — never a tracked
    // load-increment-save (the B-039 lesson). The clock-independent invariant is
    // reserved_quantity == Σ quantity of the variant's status='Active' holds. See unit-plan §"THE CONCURRENCY
    // MECHANISM" and IStockReservationService.

    /// <summary>
    /// P0 — locks the variant row (<c>SELECT id FROM product_variants WHERE id=@v FOR UPDATE</c>) for the rest
    /// of the current transaction so every concurrent hold/counter mutation of that variant serializes. Executed
    /// for its lock side effect only. MUST be called inside an open transaction. No-op on the in-memory double.
    /// </summary>
    Task LockVariantForUpdateAsync(Guid variantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// INV-01 B — locks the ORDER row (<c>SELECT id FROM orders WHERE id=@o FOR UPDATE</c>) for the rest of the
    /// current transaction so every order-status transition (mark-paid consume, cancel release/restock, sweeper
    /// auto-cancel) serializes per order. Acquired FIRST — BEFORE any variant lock — so the global lock order is
    /// order → ascending variant_id (no deadlock). Executed for its lock side effect only; MUST be called inside
    /// an open transaction. No-op on the in-memory double.
    /// </summary>
    Task LockOrderForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// P1 reserve — available-gated increment of the denormalised counter:
    /// <c>reserved += @qty WHERE (stock_quantity - reserved_quantity) &gt;= @qty</c>. Returns 1 when it moved,
    /// 0 = insufficient available stock (the caller rolls the whole batch back).
    /// </summary>
    Task<int> TryIncrementReservedQuantityAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// P1/P3 release — floor-guarded decrement of the counter:
    /// <c>reserved -= @qty WHERE reserved_quantity &gt;= @qty</c> (no <c>GREATEST</c> — a 0 return signals
    /// invariant drift the reconciler heals). Returns rows affected.
    /// </summary>
    Task<int> TryDecrementReservedQuantityAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// P2 physical sale — decrement stock AND reserved together (converting this cart's own hold):
    /// <c>stock -= @qty, reserved -= @qty WHERE stock_quantity &gt;= @qty AND reserved_quantity &gt;= @qty</c>.
    /// Returns 1 when the sale succeeded, 0 otherwise. Decrement-BEFORE the status flip so a failure leaves the
    /// hold Active (recoverable) rather than phantom-Consumed.
    /// </summary>
    Task<int> TryConsumeVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>R reconcile — set the counter to the authoritative value (Σ Active). Returns rows affected.</summary>
    Task<int> SetVariantReservedQuantityAsync(Guid variantId, int value, CancellationToken cancellationToken = default);

    /// <summary>Sum the quantities of a variant's <c>status='Active'</c> holds (clock-independent) for the reconciler.</summary>
    Task<int> SumActiveReservedQuantityAsync(Guid variantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert a fresh <c>Active</c> hold. <c>ON CONFLICT (cart_id, variant_id) WHERE status='Active' DO NOTHING</c>
    /// so a same-cart duplicate never throws (returns 0); returns 1 when this call created the row. <paramref name="kind"/>
    /// is the string enum name (<c>Card</c>/<c>BankTransfer</c>).
    /// </summary>
    Task<int> InsertActiveReservationAsync(Guid id, Guid variantId, Guid? cartId, int quantity, DateTime expiresAt, string kind, CancellationToken cancellationToken = default);

    /// <summary>
    /// INV-01 B — insert a fresh <c>Active</c> <c>BankTransfer</c> hold keyed on an ORDER (not a cart): cart_id
    /// is null, order_id is set. <c>ON CONFLICT (order_id, variant_id) WHERE status='Active' AND kind='BankTransfer'
    /// DO NOTHING</c> (its own filtered-unique index — the card index doesn't dedupe a null cart_id) so a duplicate
    /// per (order, variant) never throws; returns 1 when this call created the row, 0 on conflict.
    /// </summary>
    Task<int> InsertActiveBankReservationAsync(Guid id, Guid variantId, Guid orderId, int quantity, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// INV-01 B — atomically restock a variant: <c>stock_quantity += @qty</c> in a single <c>ExecuteUpdate</c>
    /// (never a tracked load-increment-save, which would lose a concurrent decrement). Used when cancelling an
    /// order whose stock was physically decremented (a card order, or a bank order already marked Paid). Returns
    /// rows affected (1 = restocked, 0 = missing variant). Does not touch <c>reserved_quantity</c>.
    /// </summary>
    Task<int> IncrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Set an Active hold's quantity and expiry (P1 reconcile of an existing hold). Returns rows affected.</summary>
    Task<int> SetReservationQuantityAndExpiryAsync(Guid reservationId, int quantity, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Refresh an Active hold's lease (P1 delta==0). Returns rows affected (0 = no longer Active).</summary>
    Task<int> RefreshReservationExpiryAsync(Guid reservationId, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// P3 expire — CAS <c>status='Expired' WHERE id=@r AND status='Active' AND expires_at &lt;= now()</c>, so it
    /// can NOT expire a hold P1 just refreshed. Returns 1 when it expired, 0 otherwise.
    /// </summary>
    Task<int> TryExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>P3 explicit release — CAS <c>status='Released' WHERE id=@r AND status='Active'</c>. Returns rows affected.</summary>
    Task<int> TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>P2 flip — CAS <c>status='Consumed', order_id=@o WHERE id=@r AND status='Active'</c>. Returns rows affected.</summary>
    Task<int> TryConsumeReservationRowAsync(Guid reservationId, Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Stamp the PaymentIntent id onto a cart's Active holds. Returns rows affected. Idempotent.</summary>
    Task<int> StampReservationsPaymentIntentAsync(Guid cartId, string paymentIntentId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
