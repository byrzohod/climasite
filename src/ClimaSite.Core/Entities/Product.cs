using System.Text.Json;

namespace ClimaSite.Core.Entities;

public class Product : BaseEntity
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public Dictionary<string, object> Specifications { get; private set; } = new();
    public List<ProductFeature> Features { get; private set; } = new();
    public List<string> Tags { get; private set; } = new();
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public bool RequiresInstallation { get; private set; }
    public int WarrantyMonths { get; private set; } = 12;
    public decimal? WeightKg { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Navigation properties
    public virtual Category? Category { get; private set; }
    public virtual ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public virtual ICollection<RelatedProduct> RelatedProducts { get; private set; } = new List<RelatedProduct>();
    public virtual ICollection<ProductTranslation> Translations { get; private set; } = new List<ProductTranslation>();
    public virtual ICollection<ProductQuestion> Questions { get; private set; } = new List<ProductQuestion>();

    /// <summary>
    /// Gets the translated content for the specified language, or returns the default (English) content.
    /// </summary>
    public (string Name, string? ShortDescription, string? Description, string? MetaTitle, string? MetaDescription) GetTranslatedContent(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return (Name, ShortDescription, Description, MetaTitle, MetaDescription);
        }

        var translation = Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (translation == null)
        {
            return (Name, ShortDescription, Description, MetaTitle, MetaDescription);
        }

        return (
            translation.Name,
            translation.ShortDescription ?? ShortDescription,
            translation.Description ?? Description,
            translation.MetaTitle ?? MetaTitle,
            translation.MetaDescription ?? MetaDescription
        );
    }

    private Product() { }

    public Product(string sku, string name, string slug, decimal basePrice)
    {
        SetSku(sku);
        SetName(name);
        SetSlug(slug);
        SetBasePrice(basePrice);
    }

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        if (sku.Length > 50)
            throw new ArgumentException("SKU cannot exceed 50 characters", nameof(sku));

        Sku = sku.Trim().ToUpperInvariant();
        SetUpdatedAt();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length > 255)
            throw new ArgumentException("Product name cannot exceed 255 characters", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Product slug cannot be empty", nameof(slug));

        if (slug.Length > 255)
            throw new ArgumentException("Product slug cannot exceed 255 characters", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
        SetUpdatedAt();
    }

    public void SetShortDescription(string? shortDescription)
    {
        if (shortDescription != null && shortDescription.Length > 500)
            throw new ArgumentException("Short description cannot exceed 500 characters", nameof(shortDescription));

        ShortDescription = shortDescription?.Trim();
        SetUpdatedAt();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
        SetUpdatedAt();
    }

    public void SetBrand(string? brand)
    {
        if (brand != null && brand.Length > 100)
            throw new ArgumentException("Brand cannot exceed 100 characters", nameof(brand));

        Brand = brand?.Trim();
        SetUpdatedAt();
    }

    public void SetModel(string? model)
    {
        if (model != null && model.Length > 100)
            throw new ArgumentException("Model cannot exceed 100 characters", nameof(model));

        Model = model?.Trim();
        SetUpdatedAt();
    }

    public void SetBasePrice(decimal basePrice)
    {
        if (basePrice < 0)
            throw new ArgumentException("Base price cannot be negative", nameof(basePrice));

        BasePrice = basePrice;
        SetUpdatedAt();
    }

    public void SetCompareAtPrice(decimal? compareAtPrice)
    {
        if (compareAtPrice.HasValue && compareAtPrice.Value < 0)
            throw new ArgumentException("Compare at price cannot be negative", nameof(compareAtPrice));

        CompareAtPrice = compareAtPrice;
        SetUpdatedAt();
    }

    public void SetCostPrice(decimal? costPrice)
    {
        if (costPrice.HasValue && costPrice.Value < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        CostPrice = costPrice;
        SetUpdatedAt();
    }

    public void SetSpecifications(Dictionary<string, object>? specifications)
    {
        Specifications = specifications ?? new Dictionary<string, object>();
        SetUpdatedAt();
    }

    public void SetSpecification(string key, object value)
    {
        Specifications[key] = value;
        SetUpdatedAt();
    }

    public T? GetSpecification<T>(string key)
    {
        if (!Specifications.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }

        return default;
    }

    public void SetFeatures(List<ProductFeature>? features)
    {
        Features = features ?? new List<ProductFeature>();
        SetUpdatedAt();
    }

    public void AddFeature(string title, string description, string? icon = null)
    {
        Features.Add(new ProductFeature(title, description, icon));
        SetUpdatedAt();
    }

    public void SetTags(List<string>? tags)
    {
        Tags = tags?.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList() ?? new List<string>();
        SetUpdatedAt();
    }

    public void AddTag(string tag)
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (!Tags.Contains(normalizedTag))
        {
            Tags.Add(normalizedTag);
            SetUpdatedAt();
        }
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        SetUpdatedAt();
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        SetUpdatedAt();
    }

    public void SetRequiresInstallation(bool requiresInstallation)
    {
        RequiresInstallation = requiresInstallation;
        SetUpdatedAt();
    }

    public void SetWarrantyMonths(int warrantyMonths)
    {
        if (warrantyMonths < 0)
            throw new ArgumentException("Warranty months cannot be negative", nameof(warrantyMonths));

        WarrantyMonths = warrantyMonths;
        SetUpdatedAt();
    }

    public void SetWeightKg(decimal? weightKg)
    {
        if (weightKg.HasValue && weightKg.Value < 0)
            throw new ArgumentException("Weight cannot be negative", nameof(weightKg));

        WeightKg = weightKg;
        SetUpdatedAt();
    }

    public void SetMetaTitle(string? metaTitle)
    {
        if (metaTitle != null && metaTitle.Length > 200)
            throw new ArgumentException("Meta title cannot exceed 200 characters", nameof(metaTitle));

        MetaTitle = metaTitle?.Trim();
        SetUpdatedAt();
    }

    public void SetMetaDescription(string? metaDescription)
    {
        if (metaDescription != null && metaDescription.Length > 500)
            throw new ArgumentException("Meta description cannot exceed 500 characters", nameof(metaDescription));

        MetaDescription = metaDescription?.Trim();
        SetUpdatedAt();
    }

    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > BasePrice;

    public decimal? DiscountPercentage
    {
        get
        {
            if (!IsOnSale || !CompareAtPrice.HasValue)
                return null;

            return Math.Round((CompareAtPrice.Value - BasePrice) / CompareAtPrice.Value * 100, 2);
        }
    }

    public ProductImage? PrimaryImage => Images.FirstOrDefault(i => i.IsPrimary) ?? Images.FirstOrDefault();

    public int TotalStock => Variants.Where(v => v.IsActive).Sum(v => v.StockQuantity);

    public bool InStock => TotalStock > 0;

    public decimal MinPrice => Variants.Any(v => v.IsActive)
        ? Variants.Where(v => v.IsActive).Min(v => BasePrice + v.PriceAdjustment)
        : BasePrice;

    public decimal MaxPrice => Variants.Any(v => v.IsActive)
        ? Variants.Where(v => v.IsActive).Max(v => BasePrice + v.PriceAdjustment)
        : BasePrice;
}

public record ProductFeature(string Title, string Description, string? Icon = null);
