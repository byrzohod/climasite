using System.Text.Json;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id");

        builder.Property(p => p.Brand)
            .HasColumnName("brand")
            .HasMaxLength(100);

        builder.Property(p => p.Model)
            .HasColumnName("model")
            .HasMaxLength(100);

        builder.Property(p => p.BasePrice)
            .HasColumnName("base_price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.CompareAtPrice)
            .HasColumnName("compare_at_price")
            .HasPrecision(10, 2);

        builder.Property(p => p.CostPrice)
            .HasColumnName("cost_price")
            .HasPrecision(10, 2);

        builder.Property(p => p.Specifications)
            .HasColumnName("specifications")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonOptions) ?? new Dictionary<string, object>()
            )
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>>(
                (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
                c => JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
                c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions) ?? new()));

        builder.Property(p => p.Features)
            .HasColumnName("features")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ProductFeature>>(v, JsonOptions) ?? new List<ProductFeature>()
            )
            .Metadata.SetValueComparer(new ValueComparer<List<ProductFeature>>(
                (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
                c => JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
                c => JsonSerializer.Deserialize<List<ProductFeature>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions) ?? new()));

        builder.Property(p => p.Tags)
            .HasColumnName("tags")
            .HasDefaultValueSql("'{}'::text[]")
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList()
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(p => p.RequiresInstallation)
            .HasColumnName("requires_installation")
            .HasDefaultValue(false);

        builder.Property(p => p.WarrantyMonths)
            .HasColumnName("warranty_months")
            .HasDefaultValue(12);

        builder.Property(p => p.WeightKg)
            .HasColumnName("weight_kg")
            .HasPrecision(8, 2);

        builder.Property(p => p.MetaTitle)
            .HasColumnName("meta_title")
            .HasMaxLength(200);

        builder.Property(p => p.MetaDescription)
            .HasColumnName("meta_description")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Brand);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.BasePrice);
        builder.HasIndex(p => p.CreatedAt).IsDescending();
        builder.HasIndex(p => p.Tags).HasMethod("gin");
        builder.HasIndex(p => p.Specifications).HasMethod("gin");
    }
}
