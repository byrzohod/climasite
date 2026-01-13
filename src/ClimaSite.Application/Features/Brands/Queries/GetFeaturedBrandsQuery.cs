using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Brands.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Brands.Queries;

public record GetFeaturedBrandsQuery : IRequest<List<BrandBriefDto>>, ICacheableQuery
{
    public int Limit { get; init; } = 8;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"featured_brands_{Limit}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetFeaturedBrandsQueryHandler : IRequestHandler<GetFeaturedBrandsQuery, List<BrandBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturedBrandsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandBriefDto>> Handle(
        GetFeaturedBrandsQuery request,
        CancellationToken cancellationToken)
    {
        var brands = await _context.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .Where(b => b.IsActive && b.IsFeatured)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        // Get product counts for each brand
        var brandNames = brands.Select(b => b.Name).ToList();
        var productCounts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Brand != null && brandNames.Contains(p.Brand))
            .GroupBy(p => p.Brand)
            .Select(g => new { BrandName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var productCountDict = productCounts.ToDictionary(x => x.BrandName!, x => x.Count);

        return brands.Select(b =>
        {
            var translated = b.GetTranslatedContent(request.LanguageCode);
            return new BrandBriefDto
            {
                Id = b.Id,
                Name = translated.Name,
                Slug = b.Slug,
                Description = translated.Description,
                LogoUrl = b.LogoUrl,
                CountryOfOrigin = b.CountryOfOrigin,
                IsFeatured = b.IsFeatured,
                ProductCount = productCountDict.GetValueOrDefault(b.Name, 0)
            };
        }).ToList();
    }
}
