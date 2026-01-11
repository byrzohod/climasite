using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetTopSellingProductsQuery : IRequest<List<TopSellingProductDto>>
{
    public int Count { get; init; } = 10;
    public string Period { get; init; } = "30d";
}

public class GetTopSellingProductsQueryHandler : IRequestHandler<GetTopSellingProductsQuery, List<TopSellingProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTopSellingProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopSellingProductDto>> Handle(GetTopSellingProductsQuery request, CancellationToken cancellationToken)
    {
        var days = request.Period switch
        {
            "7d" => 7,
            "12m" => 365,
            _ => 30
        };

        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var topProducts = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Images)
            .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.Sku })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Sku,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(request.Count)
            .ToListAsync(cancellationToken);

        // Get images for top products
        var productIds = topProducts.Select(p => p.ProductId).ToList();
        var productImages = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Images)
            .ToDictionaryAsync(p => p.Id, p => p.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? "", cancellationToken);

        return topProducts.Select(p => new TopSellingProductDto
        {
            Id = p.ProductId,
            Name = p.Name,
            Sku = p.Sku,
            QuantitySold = p.QuantitySold,
            Revenue = p.Revenue,
            ImageUrl = productImages.GetValueOrDefault(p.ProductId, "")
        }).ToList();
    }
}
