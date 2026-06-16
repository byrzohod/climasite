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
    DbSet<InstallationRequest> InstallationRequests { get; }
    DbSet<ProductPriceHistory> ProductPriceHistory { get; }

    /// <summary>
    /// Atomically decrements a variant's stock by <paramref name="quantity"/> only when current
    /// stock is at least that much. Returns rows affected (1 = decremented, 0 = insufficient stock
    /// or missing variant) — the oversell guard for concurrent checkout. (BUG-05)
    /// </summary>
    Task<int> TryDecrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
