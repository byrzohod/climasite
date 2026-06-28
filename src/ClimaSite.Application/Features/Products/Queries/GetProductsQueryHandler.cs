using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedList<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProductSearchService _searchService;

    public GetProductsQueryHandler(IApplicationDbContext context, IProductSearchService searchService)
    {
        _context = context;
        _searchService = searchService;
    }

    public async Task<PaginatedList<ProductBriefDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        // SEARCH-01-fts: when a search term is present, route through the shared Postgres FTS service so the
        // user-facing header search (this is the path it actually hits) gets ranking + tags + translations +
        // SKU + multilang. All of GetProductsQuery's facets/sort are preserved by passing the full filter.
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var (orderedIds, total) = await _searchService.SearchAsync(new ProductSearchFilter
            {
                RawQuery = request.SearchTerm,
                CategoryIds = request.CategoryId.HasValue ? new[] { request.CategoryId.Value } : null,
                Brands = !string.IsNullOrWhiteSpace(request.Brand) ? new[] { request.Brand.ToLowerInvariant() } : null,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                InStock = request.InStock ?? false,
                OnSale = request.OnSale ?? false,
                IsFeatured = request.IsFeatured ?? false,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
            }, cancellationToken);

            var searched = await HydrateAsync(orderedIds, request.LanguageCode, cancellationToken);
            return new PaginatedList<ProductBriefDto>(searched, total, request.PageNumber, request.PageSize);
        }

        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        // Apply filters
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            query = query.Where(p => p.Brand == request.Brand);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= request.MaxPrice.Value);
        }

        if (request.InStock.HasValue && request.InStock.Value)
        {
            query = query.Where(p => p.Variants.Any(v => v.StockQuantity > 0));
        }

        if (request.OnSale.HasValue && request.OnSale.Value)
        {
            query = query.Where(p => p.CompareAtPrice.HasValue && p.CompareAtPrice > p.BasePrice);
        }

        if (request.IsFeatured.HasValue && request.IsFeatured.Value)
        {
            query = query.Where(p => p.IsFeatured);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "price" => request.SortDescending
                ? query.OrderByDescending(p => p.BasePrice)
                : query.OrderBy(p => p.BasePrice),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // Include translations for language support
        var queryWithTranslations = query
            .Include(p => p.Translations)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Variants);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await queryWithTranslations
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(p => ToBrief(p, request.LanguageCode)).ToList();

        return new PaginatedList<ProductBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    /// <summary>Loads the ranked products by id, re-orders them to the SQL relevance order, and projects to DTOs.</summary>
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
            .Include(p => p.Translations)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Variants)
            .Where(p => idSet.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var byId = products.ToDictionary(p => p.Id);
        return orderedIds
            .Where(byId.ContainsKey)
            .Select(id => ToBrief(byId[id], languageCode))
            .ToList();
    }

    private static ProductBriefDto ToBrief(Core.Entities.Product p, string? languageCode)
    {
        var (name, shortDescription, _, _, _) = p.GetTranslatedContent(languageCode);
        return new ProductBriefDto
        {
            Id = p.Id,
            Name = name,
            Slug = p.Slug,
            ShortDescription = shortDescription,
            BasePrice = p.BasePrice,
            SalePrice = ProductPricing.GetSalePrice(p.BasePrice, p.CompareAtPrice),
            IsOnSale = ProductPricing.IsOnSale(p.BasePrice, p.CompareAtPrice),
            DiscountPercentage = ProductPricing.GetDiscountPercentage(p.BasePrice, p.CompareAtPrice),
            Brand = p.Brand,
            AverageRating = 0, // Will be calculated from reviews
            ReviewCount = 0, // Will be calculated from reviews
            PrimaryImageUrl = p.Images
                .Where(i => i.IsPrimary)
                .Select(i => i.Url)
                .FirstOrDefault(),
            InStock = p.Variants.Any(v => v.StockQuantity > 0)
        };
    }
}
