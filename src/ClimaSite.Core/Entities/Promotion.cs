namespace ClimaSite.Core.Entities;

public class Promotion : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Code { get; private set; }
    public PromotionType Type { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? BannerImageUrl { get; private set; }
    public string? ThumbnailImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }
    public string? TermsAndConditions { get; private set; }

    // Navigation
    public virtual ICollection<PromotionProduct> Products { get; private set; } = new List<PromotionProduct>();
    public virtual ICollection<PromotionTranslation> Translations { get; private set; } = new List<PromotionTranslation>();

    public bool IsCurrentlyActive => IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

    public (string Name, string? Description, string? TermsAndConditions) GetTranslatedContent(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return (Name, Description, TermsAndConditions);
        }

        var translation = Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (translation == null)
        {
            return (Name, Description, TermsAndConditions);
        }

        return (
            translation.Name ?? Name,
            translation.Description ?? Description,
            translation.TermsAndConditions ?? TermsAndConditions
        );
    }

    private Promotion() { }

    public Promotion(string name, string slug, PromotionType type, decimal discountValue, DateTime startDate, DateTime endDate)
    {
        SetName(name);
        SetSlug(slug);
        Type = type;
        SetDiscountValue(discountValue);
        SetDates(startDate, endDate);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Promotion name cannot be empty", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Promotion slug cannot be empty", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
        SetUpdatedAt();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void SetCode(string? code)
    {
        Code = code?.Trim().ToUpperInvariant();
        SetUpdatedAt();
    }

    public void SetDiscountValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Discount value cannot be negative", nameof(value));

        if (Type == PromotionType.Percentage && value > 100)
            throw new ArgumentException("Percentage discount cannot exceed 100", nameof(value));

        DiscountValue = value;
        SetUpdatedAt();
    }

    public void SetMinimumOrderAmount(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
            throw new ArgumentException("Minimum order amount cannot be negative", nameof(amount));

        MinimumOrderAmount = amount;
        SetUpdatedAt();
    }

    public void SetDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");

        StartDate = startDate;
        EndDate = endDate;
        SetUpdatedAt();
    }

    public void SetBannerImageUrl(string? url)
    {
        BannerImageUrl = url?.Trim();
        SetUpdatedAt();
    }

    public void SetThumbnailImageUrl(string? url)
    {
        ThumbnailImageUrl = url?.Trim();
        SetUpdatedAt();
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

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        SetUpdatedAt();
    }

    public void SetTermsAndConditions(string? terms)
    {
        TermsAndConditions = terms?.Trim();
        SetUpdatedAt();
    }
}

public enum PromotionType
{
    Percentage = 0,
    FixedAmount = 1,
    BuyOneGetOne = 2,
    FreeShipping = 3
}

public class PromotionProduct
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid ProductId { get; set; }

    public virtual Promotion? Promotion { get; set; }
    public virtual Product? Product { get; set; }
}

public class PromotionTranslation
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TermsAndConditions { get; set; }

    public virtual Promotion? Promotion { get; set; }
}
