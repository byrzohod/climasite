using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
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

    public SearchProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductBriefDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var searchTerms = request.Query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var langCode = request.LanguageCode?.ToLowerInvariant();

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => p.IsActive)
            .AsQueryable();

        // Search in both default fields and translations
        foreach (var term in searchTerms)
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                (p.Model != null && p.Model.ToLower().Contains(term)) ||
                p.Tags.Any(t => t.Contains(term)) ||
                // Search in translations
                p.Translations.Any(t =>
                    t.Name.ToLower().Contains(term) ||
                    (t.ShortDescription != null && t.ShortDescription.ToLower().Contains(term)) ||
                    (t.Description != null && t.Description.ToLower().Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var categoryIds = await GetCategoryIdsWithDescendantsAsync(request.CategorySlug, cancellationToken);
            if (categoryIds.Any())
            {
                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
            }
        }

        if (request.Brands != null && request.Brands.Any())
        {
            var normalizedBrands = request.Brands.Select(b => b.ToLowerInvariant()).ToList();
            query = query.Where(p => p.Brand != null && normalizedBrands.Contains(p.Brand.ToLower()));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= request.MaxPrice.Value);
        }

        query = query.OrderByDescending(p =>
            (p.Name.ToLower().Contains(request.Query.ToLower()) ? 10 : 0) +
            (p.Brand != null && p.Brand.ToLower().Contains(request.Query.ToLower()) ? 5 : 0))
            .ThenBy(p => p.Name);

        // Fetch products then apply translations in memory
        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(p =>
        {
            var translated = p.GetTranslatedContent(request.LanguageCode);
            return new ProductBriefDto
            {
                Id = p.Id,
                Name = translated.Name,
                Slug = p.Slug,
                ShortDescription = translated.ShortDescription,
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
            };
        }).ToList();

        return new PaginatedList<ProductBriefDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
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
