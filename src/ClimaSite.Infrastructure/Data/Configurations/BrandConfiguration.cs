using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasColumnName("description");

        builder.Property(b => b.LogoUrl)
            .HasColumnName("logo_url")
            .HasMaxLength(500);

        builder.Property(b => b.BannerImageUrl)
            .HasColumnName("banner_image_url")
            .HasMaxLength(500);

        builder.Property(b => b.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(500);

        builder.Property(b => b.CountryOfOrigin)
            .HasColumnName("country_of_origin")
            .HasMaxLength(100);

        builder.Property(b => b.FoundedYear)
            .HasColumnName("founded_year")
            .HasDefaultValue(0);

        builder.Property(b => b.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(b => b.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(b => b.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(b => b.MetaTitle)
            .HasColumnName("meta_title")
            .HasMaxLength(200);

        builder.Property(b => b.MetaDescription)
            .HasColumnName("meta_description")
            .HasMaxLength(500);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => b.Name);
        builder.HasIndex(b => b.IsActive);
        builder.HasIndex(b => b.IsFeatured);
        builder.HasIndex(b => b.SortOrder);
    }
}

public class BrandTranslationConfiguration : IEntityTypeConfiguration<BrandTranslation>
{
    public void Configure(EntityTypeBuilder<BrandTranslation> builder)
    {
        builder.ToTable("brand_translations");

        builder.HasKey(bt => bt.Id);

        builder.Property(bt => bt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(bt => bt.BrandId)
            .HasColumnName("brand_id")
            .IsRequired();

        builder.Property(bt => bt.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(bt => bt.Name)
            .HasColumnName("name")
            .HasMaxLength(100);

        builder.Property(bt => bt.Description)
            .HasColumnName("description");

        builder.Property(bt => bt.MetaTitle)
            .HasColumnName("meta_title")
            .HasMaxLength(200);

        builder.Property(bt => bt.MetaDescription)
            .HasColumnName("meta_description")
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(bt => bt.Brand)
            .WithMany(b => b.Translations)
            .HasForeignKey(bt => bt.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bt => new { bt.BrandId, bt.LanguageCode }).IsUnique();
    }
}
