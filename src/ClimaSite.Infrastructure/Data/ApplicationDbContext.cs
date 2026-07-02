using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClimaSite.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    DbSet<ApplicationUser> IApplicationDbContext.Users => Set<ApplicationUser>();
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress pending model changes warning as the ValueGeneratedNever change
        // doesn't require schema changes - the application generates IDs
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<RelatedProduct> RelatedProducts => Set<RelatedProduct>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<PromotionTranslation> PromotionTranslations => Set<PromotionTranslation>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BrandTranslation> BrandTranslations => Set<BrandTranslation>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
    public DbSet<ProductAnswer> ProductAnswers => Set<ProductAnswer>();
    public DbSet<ProductQuestionVote> ProductQuestionVotes => Set<ProductQuestionVote>();
    public DbSet<ProductAnswerVote> ProductAnswerVotes => Set<ProductAnswerVote>();
    public DbSet<InstallationRequest> InstallationRequests => Set<InstallationRequest>();
    public DbSet<ProductPriceHistory> ProductPriceHistory => Set<ProductPriceHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // SEARCH-01-fts: Postgres full-text search. unaccent folds diacritics (DE umlauts / BG accents) and
        // pg_trgm backs the ILIKE substring fallback (gin_trgm_ops indexes). The climasite_search text-search
        // CONFIGURATION + the search_vector trigger are NOT modelled by EF — they live in the migration SQL.
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Keyless projection for the raw FTS ranking query (maps to no table).
        modelBuilder.Entity<Search.ProductSearchHit>().HasNoKey().ToView(null);

        // Configure Identity tables with snake_case naming
        ConfigureIdentityTables(modelBuilder);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("roles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.SetUpdatedAt();
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    // INV-01 A2: reserved-aware. The universal physical-decrement guard now honours Active holds — a
    // non-consume decrement (bank order-create, any future path) may take only units NOT held by another cart:
    // (stock - reserved) >= qty. (P2 consume decrements stock AND reserved together, converting its OWN hold, so
    // it uses TryConsumeVariantStockAsync and is unaffected by this guard.) With reserved_quantity == 0 this is
    // byte-identical to the old stock >= qty gate, so the pre-reservations oversell guard is preserved.
    public Task<int> TryDecrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId && v.StockQuantity - v.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - quantity),
                cancellationToken);

    // INV-01 A2: reserved-aware absolute set for admin bulk stock edits — the single-statement WHERE makes it
    // atomic, so a reserve committing between an admin read and this write can never strand a card holder
    // (stock < reserved). rows==0 ⇒ the set would drop below the held units (or the variant is missing).
    public Task<int> TrySetVariantStockAtOrAboveReservedAsync(Guid variantId, int newQuantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId && newQuantity >= v.ReservedQuantity)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.StockQuantity, newQuantity),
                cancellationToken);

    // INV-01 A1: a single set-based UPDATE re-keys the legacy guest cart onto the trusted cookie id. Bypasses
    // the change tracker (and Cart.SessionId's private setter), and re-running it after the row is gone
    // affects 0 rows — the idempotent no-op the migration relies on under EnableRetryOnFailure.
    public Task<int> RekeyGuestCartAsync(string fromSessionId, string toSessionId, CancellationToken cancellationToken = default)
        => Carts
            .Where(c => c.SessionId == fromSessionId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.SessionId, toSessionId),
                cancellationToken);

    // INV-01 A1: hashtextextended maps the cookie id to the bigint pg_advisory_xact_lock takes; the lock is
    // transaction-scoped (auto-released on commit/rollback), so it serialises concurrent same-cookie migrations
    // with no explicit unlock. Run as a query command purely for its lock side effect.
    public Task AcquireGuestCartMigrationLockAsync(string cookieSessionId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({cookieSessionId}, 0))",
            cancellationToken);

    public void ClearChangeTracker() => ChangeTracker.Clear();

    // ---- B-039: per-voter Q&A vote atomics (see IApplicationDbContext for the contract) ----
    // The INSERT uses ON CONFLICT DO NOTHING so a concurrent duplicate never throws (the transaction
    // stays alive); the rows it returns gate the caller's count delta. Counts are only ever mutated
    // via these atomic statements, never a tracked load-increment-save.

    public Task<int> TryInsertQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO product_question_votes (id, question_id, user_id, created_at, updated_at)
            VALUES ({Guid.NewGuid()}, {questionId}, {userId}, NOW(), NOW())
            ON CONFLICT (question_id, user_id) DO NOTHING
            """,
            cancellationToken);

    public Task<int> DeleteQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
        => ProductQuestionVotes
            .Where(v => v.QuestionId == questionId && v.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

    // A vote-tally change intentionally does NOT bump the parent's content updated_at (it tracks when
    // the question/answer text was last edited, not its helpful count) — only the ledger row's
    // updated_at moves on a flip below. The `delta >= 0 || count > 0` guard floors a decrement at zero
    // so it can never unwind past the legacy anonymous baseline.
    public Task<int> AdjustQuestionHelpfulCountAsync(Guid questionId, int delta, CancellationToken cancellationToken = default)
        => ProductQuestions
            .Where(q => q.Id == questionId && (delta >= 0 || q.HelpfulCount > 0))
            .ExecuteUpdateAsync(
                s => s.SetProperty(q => q.HelpfulCount, q => q.HelpfulCount + delta),
                cancellationToken);

    public Task<int> TryInsertAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO product_answer_votes (id, answer_id, user_id, is_helpful, created_at, updated_at)
            VALUES ({Guid.NewGuid()}, {answerId}, {userId}, {isHelpful}, NOW(), NOW())
            ON CONFLICT (answer_id, user_id) DO NOTHING
            """,
            cancellationToken);

    public Task<int> DeleteAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default)
        => ProductAnswerVotes
            .Where(v => v.AnswerId == answerId && v.UserId == userId && v.IsHelpful == isHelpful)
            .ExecuteDeleteAsync(cancellationToken);

    // The ledger row genuinely changed, so bump its updated_at too (the raw ExecuteUpdate bypasses the
    // SaveChanges hook that would otherwise set it via the tracked ChangeVote()).
    public Task<int> FlipAnswerVoteAsync(Guid answerId, Guid userId, bool fromHelpful, bool toHelpful, CancellationToken cancellationToken = default)
        => ProductAnswerVotes
            .Where(v => v.AnswerId == answerId && v.UserId == userId && v.IsHelpful == fromHelpful)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(v => v.IsHelpful, toHelpful)
                    .SetProperty(v => v.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

    public Task<int> AdjustAnswerVoteCountAsync(Guid answerId, bool helpful, int delta, CancellationToken cancellationToken = default)
        => helpful
            ? ProductAnswers
                .Where(a => a.Id == answerId && (delta >= 0 || a.HelpfulCount > 0))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.HelpfulCount, a => a.HelpfulCount + delta),
                    cancellationToken)
            : ProductAnswers
                .Where(a => a.Id == answerId && (delta >= 0 || a.UnhelpfulCount > 0))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.UnhelpfulCount, a => a.UnhelpfulCount + delta),
                    cancellationToken);

    // ---- INV-01 A2: stock-reservation atomic primitives (see IApplicationDbContext for the contract) ----
    // Counter moves go through ExecuteUpdate with a from-state guard in the WHERE; the ledger status transitions
    // use raw CAS SQL (NOW() in the predicate for the expiry check, unambiguous string enum literals). All run
    // inside the caller's transaction (routed through the ambient transaction), under the P0 variant-row lock.

    public Task LockVariantForUpdateAsync(Guid variantId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM product_variants WHERE id = {variantId} FOR UPDATE",
            cancellationToken);

    // INV-01 B: the order-row lock serialises all status transitions of one order (mark-paid / cancel / sweep).
    // Always taken BEFORE any variant lock so the global lock order is order → ascending variant_id (no deadlock).
    public Task LockOrderForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM orders WHERE id = {orderId} FOR UPDATE",
            cancellationToken);

    public Task<int> TryIncrementReservedQuantityAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId && v.StockQuantity - v.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.ReservedQuantity, v => v.ReservedQuantity + quantity),
                cancellationToken);

    public Task<int> TryDecrementReservedQuantityAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId && v.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.ReservedQuantity, v => v.ReservedQuantity - quantity),
                cancellationToken);

    public Task<int> TryConsumeVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId && v.StockQuantity >= quantity && v.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(v => v.StockQuantity, v => v.StockQuantity - quantity)
                    .SetProperty(v => v.ReservedQuantity, v => v.ReservedQuantity - quantity),
                cancellationToken);

    public Task<int> SetVariantReservedQuantityAsync(Guid variantId, int value, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.ReservedQuantity, value),
                cancellationToken);

    public async Task<int> SumActiveReservedQuantityAsync(Guid variantId, CancellationToken cancellationToken = default)
        => await StockReservations
            .Where(r => r.VariantId == variantId && r.Status == ReservationStatus.Active)
            .SumAsync(r => r.Quantity, cancellationToken);

    public Task<int> InsertActiveReservationAsync(Guid id, Guid variantId, Guid? cartId, int quantity, DateTime expiresAt, string kind, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO stock_reservations (id, variant_id, quantity, status, expires_at, cart_id, kind, created_at, updated_at)
            VALUES ({id}, {variantId}, {quantity}, 'Active', {expiresAt}, {cartId}, {kind}, NOW(), NOW())
            ON CONFLICT (cart_id, variant_id) WHERE status = 'Active' DO NOTHING
            """,
            cancellationToken);

    // INV-01 B: bank hold keyed on the ORDER (cart_id null). Its own filtered-unique index dedupes it — the
    // card index's ON CONFLICT (cart_id, variant_id) never fires for a null cart_id (Postgres NULLs are distinct).
    public Task<int> InsertActiveBankReservationAsync(Guid id, Guid variantId, Guid orderId, int quantity, DateTime expiresAt, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO stock_reservations (id, variant_id, quantity, status, expires_at, order_id, kind, created_at, updated_at)
            VALUES ({id}, {variantId}, {quantity}, 'Active', {expiresAt}, {orderId}, 'BankTransfer', NOW(), NOW())
            ON CONFLICT (order_id, variant_id) WHERE status = 'Active' AND kind = 'BankTransfer' DO NOTHING
            """,
            cancellationToken);

    // INV-01 B: atomic restock for order cancellation. A single set-based UPDATE (no reserved guard — restock
    // only adds physical stock back) so a concurrent decrement can't be lost the way a tracked read-modify-save would.
    public Task<int> IncrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        => ProductVariants
            .Where(v => v.Id == variantId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantity),
                cancellationToken);

    public Task<int> SetReservationQuantityAndExpiryAsync(Guid reservationId, int quantity, DateTime expiresAt, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET quantity = {quantity}, expires_at = {expiresAt}, updated_at = NOW()
            WHERE id = {reservationId} AND status = 'Active'
            """,
            cancellationToken);

    public Task<int> RefreshReservationExpiryAsync(Guid reservationId, DateTime expiresAt, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET expires_at = {expiresAt}, updated_at = NOW()
            WHERE id = {reservationId} AND status = 'Active'
            """,
            cancellationToken);

    public Task<int> TryExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET status = 'Expired', updated_at = NOW()
            WHERE id = {reservationId} AND status = 'Active' AND expires_at <= NOW()
            """,
            cancellationToken);

    public Task<int> TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET status = 'Released', updated_at = NOW()
            WHERE id = {reservationId} AND status = 'Active'
            """,
            cancellationToken);

    public Task<int> TryConsumeReservationRowAsync(Guid reservationId, Guid orderId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET status = 'Consumed', order_id = {orderId}, updated_at = NOW()
            WHERE id = {reservationId} AND status = 'Active'
            """,
            cancellationToken);

    public Task<int> StampReservationsPaymentIntentAsync(Guid cartId, string paymentIntentId, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE stock_reservations SET payment_intent_id = {paymentIntentId}, updated_at = NOW()
            WHERE cart_id = {cartId} AND status = 'Active'
            """,
            cancellationToken);
}
