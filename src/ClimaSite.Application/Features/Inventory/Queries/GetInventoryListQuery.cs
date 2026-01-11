using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Inventory.Queries;

public record GetInventoryListQuery : IRequest<InventoryListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? StockStatus { get; init; }
    public Guid? CategoryId { get; init; }
    public string SortBy { get; init; } = "sku";
    public string SortOrder { get; init; } = "asc";
}

public class GetInventoryListQueryHandler : IRequestHandler<GetInventoryListQuery, InventoryListDto>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryListDto> Handle(GetInventoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .Where(v => v.IsActive)
            .AsQueryable();

        // Search by SKU or product name
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(v =>
                v.Sku.ToLower().Contains(searchLower) ||
                v.Product.Name.ToLower().Contains(searchLower) ||
                v.Name.ToLower().Contains(searchLower));
        }

        // Stock status filter
        if (!string.IsNullOrWhiteSpace(request.StockStatus))
        {
            query = request.StockStatus.ToLower() switch
            {
                "outofstock" => query.Where(v => v.StockQuantity == 0),
                "lowstock" => query.Where(v => v.StockQuantity > 0 && v.StockQuantity <= v.LowStockThreshold),
                "instock" => query.Where(v => v.StockQuantity > v.LowStockThreshold),
                _ => query
            };
        }

        // Category filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(v => v.Product.CategoryId == request.CategoryId);
        }

        // Get counts for stats before pagination
        var allVariants = await query.ToListAsync(cancellationToken);
        var lowStockCount = allVariants.Count(v => v.StockQuantity > 0 && v.StockQuantity <= v.LowStockThreshold);
        var outOfStockCount = allVariants.Count(v => v.StockQuantity == 0);

        // Sorting
        var sortedQuery = request.SortBy.ToLower() switch
        {
            "name" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(v => v.Product.Name).ThenBy(v => v.Name)
                : query.OrderByDescending(v => v.Product.Name).ThenByDescending(v => v.Name),
            "stock" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(v => v.StockQuantity)
                : query.OrderByDescending(v => v.StockQuantity),
            "updated" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(v => v.UpdatedAt)
                : query.OrderByDescending(v => v.UpdatedAt),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(v => v.Sku)
                : query.OrderByDescending(v => v.Sku)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var variants = await sortedQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = variants.Select(v => new InventoryItemDto
        {
            VariantId = v.Id,
            ProductId = v.ProductId,
            ProductName = v.Product.Name,
            VariantName = v.Name,
            Sku = v.Sku,
            Quantity = v.StockQuantity,
            LowStockThreshold = v.LowStockThreshold,
            StockStatus = GetStockStatus(v.StockQuantity, v.LowStockThreshold),
            ImageUrl = v.Product.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
            UpdatedAt = v.UpdatedAt
        }).ToList();

        return new InventoryListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount
        };
    }

    private static string GetStockStatus(int quantity, int threshold)
    {
        if (quantity == 0) return "OutOfStock";
        if (quantity <= threshold) return "LowStock";
        return "InStock";
    }
}
