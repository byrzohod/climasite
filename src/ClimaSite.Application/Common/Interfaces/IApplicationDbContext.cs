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

    /// <summary>
    /// Atomically decrements a variant's stock by <paramref name="quantity"/> only when current
    /// stock is at least that much. Returns rows affected (1 = decremented, 0 = insufficient stock
    /// or missing variant) — the oversell guard for concurrent checkout. (BUG-05)
    /// </summary>
    Task<int> TryDecrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
