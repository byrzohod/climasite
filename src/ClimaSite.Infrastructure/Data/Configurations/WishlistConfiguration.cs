using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("wishlists");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(w => w.IsPublic)
            .HasColumnName("is_public")
            .HasDefaultValue(false);

        builder.Property(w => w.ShareToken)
            .HasColumnName("share_token")
            .HasMaxLength(50);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(w => w.User)
            .WithOne(u => u.Wishlist)
            .HasForeignKey<Wishlist>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(w => w.UserId).IsUnique();
        builder.HasIndex(w => w.ShareToken).IsUnique();
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("wishlist_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.WishlistId)
            .HasColumnName("wishlist_id")
            .IsRequired();

        builder.Property(i => i.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(i => i.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(i => i.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0);

        builder.Property(i => i.PriceWhenAdded)
            .HasColumnName("price_when_added")
            .HasPrecision(10, 2);

        builder.Property(i => i.NotifyOnSale)
            .HasColumnName("notify_on_sale")
            .HasDefaultValue(false);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(i => i.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - one product per wishlist
        builder.HasIndex(i => new { i.WishlistId, i.ProductId }).IsUnique();

        // Indexes
        builder.HasIndex(i => i.WishlistId);
        builder.HasIndex(i => i.ProductId);
        builder.HasIndex(i => i.Priority);
    }
}
