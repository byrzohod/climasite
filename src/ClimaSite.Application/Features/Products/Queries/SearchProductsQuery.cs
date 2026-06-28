using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record SearchProductsQuery : IRequest<PaginatedList<ProductBriefDto>>, ICacheableQuery
{
    public string Query { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public string? CategorySlug { get; init; }
    public List<string>? Brands { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? LanguageCode { get; init; }

    public string CacheKey => $"search_products_{Query}_{PageNumber}_{PageSize}_{CategorySlug}_{string.Join(",", Brands ?? new())}_{MinPrice}_{MaxPrice}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PaginatedList<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProductSearchService _searchService;

    public SearchProductsQueryHandler(IApplicationDbContext context, IProductSearchService searchService)
    {
        _context = context;
        _searchService = searchService;
    }

    public async Task<PaginatedList<ProductBriefDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        // A blank query never reaches here (the controller 400s on blank q); guard anyway.
        if (string.IsNullOrWhiteSpace(request.Query))
            return new PaginatedList<ProductBriefDto>(new List<ProductBriefDto>(), 0, request.PageNumber, request.PageSize);

        // Resolve facets in EF (category descendants + brand normalisation), then hand the full filter to the
        // Postgres FTS service, which does match + facets + relevance + paging + total in one query.
        IReadOnlyList<Guid>? categoryIds = null;
        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var resolved = await GetCategoryIdsWithDescendantsAsync(request.CategorySlug, cancellationToken);
            categoryIds = resolved.Count > 0 ? resolved : new List<Guid> { Guid.Empty }; // empty-but-nonnull → match nothing
        }

        var brands = request.Brands is { Count: > 0 }
            ? request.Brands.Select(b => b.ToLowerInvariant()).ToList()
            : null;

        var (orderedIds, totalCount) = await _searchService.SearchAsync(new ProductSearchFilter
        {
            RawQuery = request.Query,
            CategoryIds = categoryIds,
            Brands = brands,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        }, cancellationToken);

        var items = await HydrateAsync(orderedIds, request.LanguageCode, cancellationToken);
        return new PaginatedList<ProductBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    /// <summary>
    /// Loads the ranked products by id (with Images/Variants/Translations), re-orders them in memory to match
    /// the SQL relevance order (an IN-fetch loses ordering), and projects to <see cref="ProductBriefDto"/>.
    /// </summary>
    private async Task<List<ProductBriefDto>> HydrateAsync(
        IReadOnlyList<Guid> orderedIds,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        if (orderedIds.Count == 0)
            return new List<ProductBriefDto>();

        var idSet = orderedIds.ToHashSet();
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => idSet.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var byId = products.ToDictionary(p => p.Id);

        return orderedIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .Select(p =>
            {
                var translated = p.GetTranslatedContent(languageCode);
                return new ProductBriefDto
                {
                    Id = p.Id,
                    Name = translated.Name,
                    Slug = p.Slug,
                    ShortDescription = translated.ShortDescription,
                    BasePrice = p.BasePrice,
                    SalePrice = ProductPricing.GetSalePrice(p.BasePrice, p.CompareAtPrice),
                    IsOnSale = ProductPricing.IsOnSale(p.BasePrice, p.CompareAtPrice),
                    DiscountPercentage = ProductPricing.GetDiscountPercentage(p.BasePrice, p.CompareAtPrice),
                    Brand = p.Brand,
                    AverageRating = 0,
                    ReviewCount = 0,
                    PrimaryImageUrl = p.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                    InStock = p.Variants.Any(v => v.StockQuantity > 0)
                };
            })
            .ToList();
    }

    private async Task<List<Guid>> GetCategoryIdsWithDescendantsAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Slug == categorySlug.ToLowerInvariant(), cancellationToken);

        if (category == null)
            return new List<Guid>();

        var allCategoryIds = new List<Guid> { category.Id };
        await GetDescendantIdsAsync(category.Id, allCategoryIds, cancellationToken);

        return allCategoryIds;
    }

    private async Task GetDescendantIdsAsync(Guid parentId, List<Guid> ids, CancellationToken cancellationToken)
    {
        var childIds = await _context.Categories
            .Where(c => c.ParentId == parentId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        ids.AddRange(childIds);

        foreach (var childId in childIds)
        {
            await GetDescendantIdsAsync(childId, ids, cancellationToken);
        }
    }
}
