namespace ClimaSite.Core.Entities;

public class Brand : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? BannerImageUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? CountryOfOrigin { get; private set; }
    public int FoundedYear { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Navigation
    public virtual ICollection<BrandTranslation> Translations { get; private set; } = new List<BrandTranslation>();

    public (string Name, string? Description, string? MetaTitle, string? MetaDescription) GetTranslatedContent(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return (Name, Description, MetaTitle, MetaDescription);
        }

        var translation = Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (translation == null)
        {
            return (Name, Description, MetaTitle, MetaDescription);
        }

        return (
            translation.Name ?? Name,
            translation.Description ?? Description,
            translation.MetaTitle ?? MetaTitle,
            translation.MetaDescription ?? MetaDescription
        );
    }

    private Brand() { }

    public Brand(string name, string slug)
    {
        SetName(name);
        SetSlug(slug);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Brand name cannot exceed 100 characters", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Brand slug cannot be empty", nameof(slug));

        if (slug.Length > 100)
            throw new ArgumentException("Brand slug cannot exceed 100 characters", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
        SetUpdatedAt();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void SetLogoUrl(string? logoUrl)
    {
        if (logoUrl != null && logoUrl.Length > 500)
            throw new ArgumentException("Logo URL cannot exceed 500 characters", nameof(logoUrl));

        LogoUrl = logoUrl?.Trim();
        SetUpdatedAt();
    }

    public void SetBannerImageUrl(string? bannerImageUrl)
    {
        if (bannerImageUrl != null && bannerImageUrl.Length > 500)
            throw new ArgumentException("Banner image URL cannot exceed 500 characters", nameof(bannerImageUrl));

        BannerImageUrl = bannerImageUrl?.Trim();
        SetUpdatedAt();
    }

    public void SetWebsiteUrl(string? websiteUrl)
    {
        if (websiteUrl != null && websiteUrl.Length > 500)
            throw new ArgumentException("Website URL cannot exceed 500 characters", nameof(websiteUrl));

        WebsiteUrl = websiteUrl?.Trim();
        SetUpdatedAt();
    }

    public void SetCountryOfOrigin(string? countryOfOrigin)
    {
        if (countryOfOrigin != null && countryOfOrigin.Length > 100)
            throw new ArgumentException("Country of origin cannot exceed 100 characters", nameof(countryOfOrigin));

        CountryOfOrigin = countryOfOrigin?.Trim();
        SetUpdatedAt();
    }

    public void SetFoundedYear(int year)
    {
        if (year < 1800 || year > DateTime.UtcNow.Year)
            throw new ArgumentException("Invalid founded year", nameof(year));

        FoundedYear = year;
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
}

public class BrandTranslation
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public virtual Brand? Brand { get; set; }
}
