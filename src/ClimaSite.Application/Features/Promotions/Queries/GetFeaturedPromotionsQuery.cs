using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Promotions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Promotions.Queries;

public record GetFeaturedPromotionsQuery : IRequest<List<PromotionBriefDto>>, ICacheableQuery
{
    public int Count { get; init; } = 4;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"featured_promotions_{Count}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class GetFeaturedPromotionsQueryHandler : IRequestHandler<GetFeaturedPromotionsQuery, List<PromotionBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturedPromotionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromotionBriefDto>> Handle(
        GetFeaturedPromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var promotions = await _context.Promotions
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Products)
            .Where(p => p.IsActive && p.IsFeatured && p.StartDate <= now && p.EndDate >= now)
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.EndDate)
            .Take(request.Count)
            .ToListAsync(cancellationToken);

        return promotions.Select(p =>
        {
            var translated = p.GetTranslatedContent(request.LanguageCode);
            return new PromotionBriefDto
            {
                Id = p.Id,
                Name = translated.Name,
                Slug = p.Slug,
                Description = translated.Description,
                Code = p.Code,
                Type = p.Type,
                DiscountValue = p.DiscountValue,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                BannerImageUrl = p.BannerImageUrl,
                ThumbnailImageUrl = p.ThumbnailImageUrl,
                ProductCount = p.Products.Count
            };
        }).ToList();
    }
}
