using System;

namespace ClimaSite.Core.Entities;

/// <summary>
/// Stores translated content for products in different languages.
/// </summary>
public class ProductTranslation : BaseEntity
{
    private string _languageCode = string.Empty;
    private string _name = string.Empty;

    public Guid ProductId { get; set; }

    /// <summary>
    /// ISO 639-1 language code (e.g., "en", "bg", "de")
    /// </summary>
    public string LanguageCode
    {
        get => _languageCode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Language code is required", nameof(value));
            if (value.Length != 2)
                throw new ArgumentException("Language code must be 2 characters (ISO 639-1)", nameof(value));
            _languageCode = value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Translated product name
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name is required", nameof(value));
            if (value.Length > 255)
                throw new ArgumentException("Name cannot exceed 255 characters", nameof(value));
            _name = value.Trim();
        }
    }

    /// <summary>
    /// Translated short description (optional)
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Translated full description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Translated meta title for SEO (optional)
    /// </summary>
    public string? MetaTitle { get; set; }

    /// <summary>
    /// Translated meta description for SEO (optional)
    /// </summary>
    public string? MetaDescription { get; set; }

    // Navigation property
    public virtual Product Product { get; set; } = null!;

    // Parameterless constructor for EF Core
    protected ProductTranslation() { }

    public ProductTranslation(Guid productId, string languageCode, string name)
    {
        ProductId = productId;
        LanguageCode = languageCode;
        Name = name;
    }
}
