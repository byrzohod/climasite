using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record SearchProductsQuery : IRequest<PaginatedList<ProductBriefDto>>
{
    public string Query { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public string? CategorySlug { get; init; }
    public List<string>? Brands { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
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

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive)
            .AsQueryable();

        foreach (var term in searchTerms)
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                (p.Model != null && p.Model.ToLower().Contains(term)) ||
                p.Tags.Any(t => t.Contains(term)));
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

        var projectedQuery = query.Select(p => new ProductBriefDto
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
        });

        return await PaginatedList<ProductBriefDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
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
