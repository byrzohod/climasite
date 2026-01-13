using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.PriceHistory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.PriceHistory.Queries;

public record GetProductPriceHistoryQuery(Guid ProductId, int DaysBack = 90) : IRequest<ProductPriceHistoryDto?>;

public class GetProductPriceHistoryQueryHandler : IRequestHandler<GetProductPriceHistoryQuery, ProductPriceHistoryDto?>
{
    private readonly IApplicationDbContext _context;

    public GetProductPriceHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductPriceHistoryDto?> Handle(GetProductPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => new { p.Id, p.Name, p.BasePrice, p.CompareAtPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
            return null;

        var cutoffDate = DateTime.UtcNow.AddDays(-request.DaysBack);

        var priceHistory = await _context.ProductPriceHistory
            .Where(ph => ph.ProductId == request.ProductId && ph.RecordedAt >= cutoffDate)
            .OrderBy(ph => ph.RecordedAt)
            .ToListAsync(cancellationToken);

        // If no history, create a single point with current price
        if (!priceHistory.Any())
        {
            return new ProductPriceHistoryDto(
                ProductId: product.Id,
                ProductName: product.Name,
                CurrentPrice: product.BasePrice,
                CurrentCompareAtPrice: product.CompareAtPrice,
                LowestPrice: product.BasePrice,
                HighestPrice: product.BasePrice,
                AveragePrice: product.BasePrice,
                PricePoints: new List<PricePointDto>
                {
                    new(DateTime.UtcNow, product.BasePrice, product.CompareAtPrice, "Current")
                });
        }

        var pricePoints = priceHistory
            .Select(ph => new PricePointDto(
                Date: ph.RecordedAt,
                Price: ph.Price,
                CompareAtPrice: ph.CompareAtPrice,
                Reason: ph.Reason.ToString()))
            .ToList();

        var prices = priceHistory.Select(ph => ph.Price).ToList();

        return new ProductPriceHistoryDto(
            ProductId: product.Id,
            ProductName: product.Name,
            CurrentPrice: product.BasePrice,
            CurrentCompareAtPrice: product.CompareAtPrice,
            LowestPrice: prices.Min(),
            HighestPrice: prices.Max(),
            AveragePrice: Math.Round(prices.Average(), 2),
            PricePoints: pricePoints);
    }
}
