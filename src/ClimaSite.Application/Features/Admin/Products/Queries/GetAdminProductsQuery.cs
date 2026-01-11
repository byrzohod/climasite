using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Products.Queries;

public record GetAdminProductsQuery : IRequest<AdminProductsListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Status { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortOrder { get; init; } = "desc";
}

public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, AdminProductsListDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProductsListDto> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Sku.ToLower().Contains(searchLower) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchLower)));
        }

        // Category filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = request.Status.ToLower() switch
            {
                "active" => query.Where(p => p.IsActive),
                "inactive" => query.Where(p => !p.IsActive),
                "lowstock" => query.Where(p => p.Variants.Any(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold)),
                "outofstock" => query.Where(p => !p.Variants.Any(v => v.IsActive && v.StockQuantity > 0)),
                _ => query
            };
        }

        // Price filter
        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= request.MinPrice);
        }
        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= request.MaxPrice);
        }

        // Sorting
        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.Name)
                : query.OrderByDescending(p => p.Name),
            "price" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.BasePrice)
                : query.OrderByDescending(p => p.BasePrice),
            "stock" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.Variants.Where(v => v.IsActive).Sum(v => v.StockQuantity))
                : query.OrderByDescending(p => p.Variants.Where(v => v.IsActive).Sum(v => v.StockQuantity)),
            "sku" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.Sku)
                : query.OrderByDescending(p => p.Sku),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(p => new AdminProductListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            Slug = p.Slug,
            Price = p.BasePrice,
            SalePrice = p.CompareAtPrice,
            StockQuantity = p.TotalStock,
            Status = GetStatus(p),
            PrimaryImageUrl = p.PrimaryImage?.Url,
            CategoryName = p.Category?.Name,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return new AdminProductsListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }

    private static string GetStatus(Core.Entities.Product product)
    {
        if (!product.IsActive) return "Inactive";
        if (!product.InStock) return "OutOfStock";
        if (product.Variants.Any(v => v.IsActive && v.IsLowStock)) return "LowStock";
        return "Active";
    }
}
