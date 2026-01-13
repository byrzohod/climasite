using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Promotions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Promotions.Queries;

public record GetPromotionBySlugQuery : IRequest<PromotionDto?>, ICacheableQuery
{
    public string Slug { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"promotion_{Slug}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class GetPromotionBySlugQueryHandler : IRequestHandler<GetPromotionBySlugQuery, PromotionDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPromotionBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDto?> Handle(
        GetPromotionBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var promotion = await _context.Promotions
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Products)
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(prod => prod!.Images)
            .Include(p => p.Products)
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(prod => prod!.Variants)
            .Include(p => p.Products)
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(prod => prod!.Translations)
            .Where(p => p.Slug == request.Slug && p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .FirstOrDefaultAsync(cancellationToken);

        if (promotion == null)
            return null;

        var translated = promotion.GetTranslatedContent(request.LanguageCode);

        return new PromotionDto
        {
            Id = promotion.Id,
            Name = translated.Name,
            Slug = promotion.Slug,
            Description = translated.Description,
            Code = promotion.Code,
            Type = promotion.Type,
            DiscountValue = promotion.DiscountValue,
            MinimumOrderAmount = promotion.MinimumOrderAmount,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            BannerImageUrl = promotion.BannerImageUrl,
            ThumbnailImageUrl = promotion.ThumbnailImageUrl,
            IsActive = promotion.IsActive,
            IsFeatured = promotion.IsFeatured,
            TermsAndConditions = translated.TermsAndConditions,
            Products = promotion.Products
                .Where(pp => pp.Product != null && pp.Product.IsActive)
                .Select(pp =>
                {
                    var prod = pp.Product!;
                    var prodTranslated = prod.GetTranslatedContent(request.LanguageCode);
                    return new ProductBriefDto
                    {
                        Id = prod.Id,
                        Name = prodTranslated.Name,
                        Slug = prod.Slug,
                        ShortDescription = prodTranslated.ShortDescription,
                        BasePrice = prod.BasePrice,
                        SalePrice = prod.CompareAtPrice,
                        IsOnSale = prod.CompareAtPrice.HasValue && prod.CompareAtPrice > prod.BasePrice,
                        DiscountPercentage = prod.CompareAtPrice.HasValue && prod.CompareAtPrice > prod.BasePrice
                            ? Math.Round((prod.CompareAtPrice.Value - prod.BasePrice) / prod.CompareAtPrice.Value * 100, 0)
                            : 0,
                        Brand = prod.Brand,
                        AverageRating = 0,
                        ReviewCount = 0,
                        PrimaryImageUrl = prod.Images
                            .Where(i => i.IsPrimary)
                            .Select(i => i.Url)
                            .FirstOrDefault(),
                        InStock = prod.Variants.Any(v => v.StockQuantity > 0)
                    };
                })
                .ToList()
        };
    }
}
