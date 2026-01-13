using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Brands.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Brands.Queries;

public record GetBrandsQuery : IRequest<PaginatedList<BrandBriefDto>>, ICacheableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 24;
    public string? LanguageCode { get; init; }
    public bool? FeaturedOnly { get; init; }

    public string CacheKey => $"brands_{PageNumber}_{PageSize}_{LanguageCode ?? "en"}_{FeaturedOnly}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, PaginatedList<BrandBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBrandsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<BrandBriefDto>> Handle(
        GetBrandsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .Where(b => b.IsActive);

        if (request.FeaturedOnly == true)
        {
            query = query.Where(b => b.IsFeatured);
        }

        query = query
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var brands = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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

        var items = brands.Select(b =>
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

        return new PaginatedList<BrandBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
