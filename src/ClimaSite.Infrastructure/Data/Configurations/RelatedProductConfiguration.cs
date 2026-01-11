using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class RelatedProductConfiguration : IEntityTypeConfiguration<RelatedProduct>
{
    public void Configure(EntityTypeBuilder<RelatedProduct> builder)
    {
        builder.ToTable("related_products");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(r => r.RelatedProductId)
            .HasColumnName("related_product_id")
            .IsRequired();

        builder.Property(r => r.RelationType)
            .HasColumnName("relation_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(r => r.Product)
            .WithMany(p => p.RelatedProducts)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Related)
            .WithMany()
            .HasForeignKey(r => r.RelatedProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(r => new { r.ProductId, r.RelatedProductId, r.RelationType }).IsUnique();

        // Indexes
        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.RelatedProductId);
        builder.HasIndex(r => r.RelationType);
    }
}
