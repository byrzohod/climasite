using ClimaSite.Core.Entities;
using ClimaSite.Application.Features.Products.DTOs;

namespace ClimaSite.Application.Features.Promotions.DTOs;

public record PromotionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Code { get; init; }
    public PromotionType Type { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? BannerImageUrl { get; init; }
    public string? ThumbnailImageUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public string? TermsAndConditions { get; init; }
    public List<ProductBriefDto> Products { get; init; } = new();
}

public record PromotionBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Code { get; init; }
    public PromotionType Type { get; init; }
    public decimal DiscountValue { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? BannerImageUrl { get; init; }
    public string? ThumbnailImageUrl { get; init; }
    public int ProductCount { get; init; }
}
