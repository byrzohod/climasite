using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedList<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductBriefDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        // Apply filters
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                (p.Model != null && p.Model.ToLower().Contains(searchTerm)));
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

        var items = products.Select(p =>
        {
            var (name, shortDescription, _, _, _) = p.GetTranslatedContent(request.LanguageCode);
            return new ProductBriefDto
            {
                Id = p.Id,
                Name = name,
                Slug = p.Slug,
                ShortDescription = shortDescription,
                BasePrice = p.BasePrice,
                SalePrice = p.CompareAtPrice,
                IsOnSale = p.CompareAtPrice.HasValue && p.CompareAtPrice > p.BasePrice,
                DiscountPercentage = p.CompareAtPrice.HasValue && p.CompareAtPrice > p.BasePrice
                    ? Math.Round((p.CompareAtPrice.Value - p.BasePrice) / p.CompareAtPrice.Value * 100, 0)
                    : 0,
                Brand = p.Brand,
                AverageRating = 0, // Will be calculated from reviews
                ReviewCount = 0, // Will be calculated from reviews
                PrimaryImageUrl = p.Images
                    .Where(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                InStock = p.Variants.Any(v => v.StockQuantity > 0)
            };
        }).ToList();

        return new PaginatedList<ProductBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
