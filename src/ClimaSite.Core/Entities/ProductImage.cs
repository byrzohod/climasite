namespace ClimaSite.Core.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual ProductVariant? Variant { get; private set; }

    private ProductImage() { }

    public ProductImage(Guid productId, string url)
    {
        ProductId = productId;
        SetUrl(url);
    }

    public void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL cannot be empty", nameof(url));

        if (url.Length > 500)
            throw new ArgumentException("Image URL cannot exceed 500 characters", nameof(url));

        Url = url.Trim();
        SetUpdatedAt();
    }

    public void SetAltText(string? altText)
    {
        if (altText != null && altText.Length > 255)
            throw new ArgumentException("Alt text cannot exceed 255 characters", nameof(altText));

        AltText = altText?.Trim();
        SetUpdatedAt();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        SetUpdatedAt();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        SetUpdatedAt();
    }

    public void SetVariant(Guid? variantId)
    {
        VariantId = variantId;
        SetUpdatedAt();
    }
}
