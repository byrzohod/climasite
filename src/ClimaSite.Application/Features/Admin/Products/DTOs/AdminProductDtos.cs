namespace ClimaSite.Application.Features.Admin.Products.DTOs;

public record AdminProductListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public string? CategoryName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record AdminProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool RequiresInstallation { get; init; }
    public int WarrantyMonths { get; init; }
    public decimal? WeightKg { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Dictionary<string, object> Specifications { get; init; } = new();
    public List<ProductFeatureDto> Features { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public List<AdminProductImageDto> Images { get; init; } = [];
    public List<AdminProductVariantDto> Variants { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ProductFeatureDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

public record AdminProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}

public record AdminProductVariantDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal PriceAdjustment { get; init; }
    public Dictionary<string, object> Attributes { get; init; } = new();
    public int StockQuantity { get; init; }
    public int LowStockThreshold { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public record AdminProductsListDto
{
    public List<AdminProductListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
