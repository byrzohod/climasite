using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Common.Interfaces;

public interface IApplicationDbContext
{
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
