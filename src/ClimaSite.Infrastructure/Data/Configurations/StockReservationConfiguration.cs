using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

/// <summary>
/// INV-01 A2 stock-reservation ledger. The denormalized <c>product_variants.reserved_quantity</c> counter
/// equals the sum of a variant's <c>status='Active'</c> holds; the filtered unique index enforces at most one
/// live hold per (cart, variant). The <c>HasFilter</c> literal MUST byte-match the migration's filter.
/// </summary>
public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        builder.Property(r => r.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(r => r.CartId)
            .HasColumnName("cart_id");

        builder.Property(r => r.PaymentIntentId)
            .HasColumnName("payment_intent_id")
            .HasMaxLength(255);

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id");

        builder.Property(r => r.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Variant FK CASCADE — deleting a variant drops its holds.
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(r => r.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cart FK SET NULL — a deleted cart orphans its holds, which then expire via the sweeper (self-heal).
        builder.HasOne<Cart>()
            .WithMany()
            .HasForeignKey(r => r.CartId)
            .OnDelete(DeleteBehavior.SetNull);

        // At most one LIVE hold per (cart, variant). Literal must byte-match the migration filter.
        builder.HasIndex(r => new { r.CartId, r.VariantId })
            .IsUnique()
            .HasFilter("status = 'Active'");

        // Sweeper scan (Active + expiring first), per-variant hold lookup, and the intent/order joins.
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });
        builder.HasIndex(r => new { r.VariantId, r.Status });
        builder.HasIndex(r => r.PaymentIntentId);
        builder.HasIndex(r => r.OrderId);
    }
}
