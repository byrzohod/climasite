using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetRelatedProductsQuery : IRequest<List<ProductBriefDto>>, ICacheableQuery
{
    public Guid ProductId { get; init; }
    public RelationType? RelationType { get; init; }
    public int Count { get; init; } = 8;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"related_products_{ProductId}_{RelationType}_{Count}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetRelatedProductsQueryHandler : IRequestHandler<GetRelatedProductsQuery, List<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRelatedProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductBriefDto>> Handle(
        GetRelatedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.RelatedProducts
            .AsNoTracking()
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Images)
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Variants)
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Translations)
            .Where(rp => rp.ProductId == request.ProductId && rp.Related.IsActive);

        if (request.RelationType.HasValue)
        {
            query = query.Where(rp => rp.RelationType == request.RelationType.Value);
        }

        var relatedProducts = await query
            .OrderBy(rp => rp.SortOrder)
            .Take(request.Count)
            .ToListAsync(cancellationToken);

        return relatedProducts.Select(rp =>
        {
            var translated = rp.Related.GetTranslatedContent(request.LanguageCode);
            return new ProductBriefDto
            {
                Id = rp.Related.Id,
                Name = translated.Name,
                Slug = rp.Related.Slug,
                ShortDescription = translated.ShortDescription,
                BasePrice = rp.Related.BasePrice,
                SalePrice = rp.Related.CompareAtPrice,
                IsOnSale = rp.Related.CompareAtPrice.HasValue && rp.Related.CompareAtPrice > rp.Related.BasePrice,
                DiscountPercentage = rp.Related.CompareAtPrice.HasValue && rp.Related.CompareAtPrice > rp.Related.BasePrice
                    ? Math.Round((rp.Related.CompareAtPrice.Value - rp.Related.BasePrice) / rp.Related.CompareAtPrice.Value * 100, 0)
                    : 0,
                Brand = rp.Related.Brand,
                AverageRating = 0,
                ReviewCount = 0,
                PrimaryImageUrl = rp.Related.Images
                    .Where(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                InStock = rp.Related.Variants.Any(v => v.StockQuantity > 0)
            };
        }).ToList();
    }
}
