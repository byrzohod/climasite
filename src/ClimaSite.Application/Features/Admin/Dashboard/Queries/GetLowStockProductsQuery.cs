using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetLowStockProductsQuery : IRequest<List<LowStockProductDto>>
{
    public int Count { get; init; } = 10;
}

public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, List<LowStockProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLowStockProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LowStockProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var lowStockProducts = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .Where(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold)
            .OrderBy(v => v.StockQuantity)
            .Take(request.Count)
            .Select(v => new LowStockProductDto
            {
                Id = v.Product.Id,
                Name = v.Product.Name + (v.Name != null ? " - " + v.Name : ""),
                Sku = v.Sku,
                CurrentStock = v.StockQuantity,
                Threshold = v.LowStockThreshold,
                ImageUrl = v.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault() ?? ""
            })
            .ToListAsync(cancellationToken);

        return lowStockProducts;
    }
}
