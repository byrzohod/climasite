namespace ClimaSite.Application.Features.Products.DTOs;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public bool IsOnSale { get; init; }
    public decimal DiscountPercentage { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public CategoryBriefDto? Category { get; init; }
    public List<ProductImageDto> Images { get; init; } = new();
    public List<ProductVariantDto> Variants { get; init; } = new();
    public Dictionary<string, object>? Specifications { get; init; }
    public Dictionary<string, object>? Features { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProductBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public bool IsOnSale { get; init; }
    public decimal DiscountPercentage { get; init; }
    public string? Brand { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public bool InStock { get; init; }
}

public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}

public record ProductVariantDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsActive { get; init; }
    public Dictionary<string, string>? Attributes { get; init; }
}

public record CategoryBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public record ProductTranslationDto
{
    public Guid Id { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}
