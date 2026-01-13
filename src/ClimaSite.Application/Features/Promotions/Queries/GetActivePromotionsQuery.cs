using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Promotions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Promotions.Queries;

public record GetActivePromotionsQuery : IRequest<PaginatedList<PromotionBriefDto>>, ICacheableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"active_promotions_{PageNumber}_{PageSize}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class GetActivePromotionsQueryHandler : IRequestHandler<GetActivePromotionsQuery, PaginatedList<PromotionBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActivePromotionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PromotionBriefDto>> Handle(
        GetActivePromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query = _context.Promotions
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Products)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.EndDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var promotions = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = promotions.Select(p =>
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

        return new PaginatedList<PromotionBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
