using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetProductsQuery : IRequest<PaginatedList<ProductBriefDto>>, ICacheableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public Guid? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
    public string? Brand { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    public bool? OnSale { get; init; }
    public bool? IsFeatured { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public string? LanguageCode { get; init; }

    public string CacheKey => $"products_{PageNumber}_{PageSize}_{CategoryId}_{SearchTerm}_{Brand}_{MinPrice}_{MaxPrice}_{InStock}_{OnSale}_{IsFeatured}_{SortBy}_{SortDescending}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
