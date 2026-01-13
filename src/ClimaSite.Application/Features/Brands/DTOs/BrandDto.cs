using ClimaSite.Application.Features.Products.DTOs;

namespace ClimaSite.Application.Features.Brands.DTOs;

public record BrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? BannerImageUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? CountryOfOrigin { get; init; }
    public int FoundedYear { get; init; }
    public bool IsFeatured { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public int ProductCount { get; init; }
    public List<ProductBriefDto> Products { get; init; } = new();
}

public record BrandBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? CountryOfOrigin { get; init; }
    public bool IsFeatured { get; init; }
    public int ProductCount { get; init; }
}
