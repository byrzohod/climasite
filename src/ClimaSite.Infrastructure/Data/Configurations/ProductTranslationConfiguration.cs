using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.ToTable("product_translations");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(pt => pt.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(pt => pt.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(pt => pt.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(pt => pt.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(500);

        builder.Property(pt => pt.Description)
            .HasColumnName("description");

        builder.Property(pt => pt.MetaTitle)
            .HasColumnName("meta_title")
            .HasMaxLength(200);

        builder.Property(pt => pt.MetaDescription)
            .HasColumnName("meta_description")
            .HasMaxLength(500);

        builder.Property(pt => pt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(pt => pt.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationship
        builder.HasOne(pt => pt.Product)
            .WithMany(p => p.Translations)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pt => pt.ProductId);
        builder.HasIndex(pt => pt.LanguageCode);

        // Unique constraint: one translation per language per product
        builder.HasIndex(pt => new { pt.ProductId, pt.LanguageCode }).IsUnique();
    }
}
