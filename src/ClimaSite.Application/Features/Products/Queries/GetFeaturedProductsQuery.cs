using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetFeaturedProductsQuery : IRequest<List<ProductBriefDto>>, ICacheableQuery
{
    public int Count { get; init; } = 8;

    public string CacheKey => $"featured_products_{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetFeaturedProductsQueryHandler : IRequestHandler<GetFeaturedProductsQuery, List<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturedProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductBriefDto>> Handle(
        GetFeaturedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(request.Count)
            .Select(p => new ProductBriefDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                BasePrice = p.BasePrice,
                SalePrice = p.CompareAtPrice,
                IsOnSale = p.CompareAtPrice.HasValue && p.CompareAtPrice > p.BasePrice,
                DiscountPercentage = p.CompareAtPrice.HasValue && p.CompareAtPrice > p.BasePrice
                    ? Math.Round((p.CompareAtPrice.Value - p.BasePrice) / p.CompareAtPrice.Value * 100, 0)
                    : 0,
                Brand = p.Brand,
                AverageRating = 0,
                ReviewCount = 0,
                PrimaryImageUrl = p.Images
                    .Where(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                InStock = p.Variants.Any(v => v.StockQuantity > 0)
            })
            .ToListAsync(cancellationToken);

        return products;
    }
}
