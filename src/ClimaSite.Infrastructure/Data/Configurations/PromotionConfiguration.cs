using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .HasColumnName("discount_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.MinimumOrderAmount)
            .HasColumnName("minimum_order_amount")
            .HasPrecision(18, 2);

        builder.Property(p => p.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(p => p.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(p => p.BannerImageUrl)
            .HasColumnName("banner_image_url")
            .HasMaxLength(500);

        builder.Property(p => p.ThumbnailImageUrl)
            .HasColumnName("thumbnail_image_url")
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(p => p.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(p => p.TermsAndConditions)
            .HasColumnName("terms_and_conditions")
            .HasColumnType("text");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Code);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => new { p.StartDate, p.EndDate });
    }
}

public class PromotionProductConfiguration : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("promotion_products");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(pp => pp.PromotionId)
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(pp => pp.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        // Relationships
        builder.HasOne(pp => pp.Promotion)
            .WithMany(p => p.Products)
            .HasForeignKey(pp => pp.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(pp => new { pp.PromotionId, pp.ProductId }).IsUnique();
    }
}

public class PromotionTranslationConfiguration : IEntityTypeConfiguration<PromotionTranslation>
{
    public void Configure(EntityTypeBuilder<PromotionTranslation> builder)
    {
        builder.ToTable("promotion_translations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.PromotionId)
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(t => t.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(t => t.TermsAndConditions)
            .HasColumnName("terms_and_conditions")
            .HasColumnType("text");

        // Relationships
        builder.HasOne(t => t.Promotion)
            .WithMany(p => p.Translations)
            .HasForeignKey(t => t.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(t => new { t.PromotionId, t.LanguageCode }).IsUnique();
    }
}
