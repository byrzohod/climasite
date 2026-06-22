using ClimaSite.Application.Features.PriceHistory.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.PriceHistory.Queries;

public class GetProductPriceHistoryQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetProductPriceHistoryQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(string name = "AC Unit", decimal basePrice = 500m, decimal? compareAt = null)
    {
        var product = new Product("PH-1", name, "ph-1", basePrice);
        if (compareAt.HasValue)
        {
            product.SetCompareAtPrice(compareAt.Value);
        }
        _context.AddProduct(product);
        return product;
    }

    private static ProductPriceHistory MakeHistory(
        Guid productId, decimal price, DateTime recordedAt,
        PriceChangeReason reason = PriceChangeReason.PriceChange, decimal? compareAt = null)
    {
        var history = ProductPriceHistory.Create(productId, price, compareAt, reason);
        typeof(ProductPriceHistory).GetProperty(nameof(ProductPriceHistory.RecordedAt))!
            .SetValue(history, recordedAt);
        return history;
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNull()
    {
        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsSingleCurrentPricePoint()
    {
        var product = SeedProduct(basePrice: 749.99m, compareAt: 899.99m);

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(product.Id);
        result.ProductName.Should().Be("AC Unit");
        result.CurrentPrice.Should().Be(749.99m);
        result.CurrentCompareAtPrice.Should().Be(899.99m);
        result.LowestPrice.Should().Be(749.99m);
        result.HighestPrice.Should().Be(749.99m);
        result.AveragePrice.Should().Be(749.99m);
        result.PricePoints.Should().ContainSingle();
        result.PricePoints[0].Reason.Should().Be("Current");
        result.PricePoints[0].Price.Should().Be(749.99m);
    }

    [Fact]
    public async Task Handle_WithHistory_ComputesLowHighAverageAndOrdersByDate()
    {
        var product = SeedProduct(basePrice: 600m);
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 700m, DateTime.UtcNow.AddDays(-30)));
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 500m, DateTime.UtcNow.AddDays(-10)));
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 600m, DateTime.UtcNow.AddDays(-20)));

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentPrice.Should().Be(600m);
        result.LowestPrice.Should().Be(500m);
        result.HighestPrice.Should().Be(700m);
        result.AveragePrice.Should().Be(600m); // (700 + 500 + 600) / 3
        result.PricePoints.Should().HaveCount(3);
        // ordered oldest -> newest
        result.PricePoints[0].Price.Should().Be(700m);
        result.PricePoints[1].Price.Should().Be(600m);
        result.PricePoints[2].Price.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_ExcludesHistoryOlderThanDaysBackCutoff()
    {
        var product = SeedProduct(basePrice: 400m);
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 450m, DateTime.UtcNow.AddDays(-5)));
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 999m, DateTime.UtcNow.AddDays(-200)));

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id, DaysBack: 90), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PricePoints.Should().ContainSingle();
        result.PricePoints[0].Price.Should().Be(450m);
        result.HighestPrice.Should().Be(450m); // the 200-day-old 999 point is excluded
    }

    [Fact]
    public async Task Handle_AllHistoryOlderThanCutoff_FallsBackToCurrentPricePoint()
    {
        var product = SeedProduct(basePrice: 350m);
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 999m, DateTime.UtcNow.AddDays(-365)));

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id, DaysBack: 30), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PricePoints.Should().ContainSingle();
        result.PricePoints[0].Reason.Should().Be("Current");
        result.PricePoints[0].Price.Should().Be(350m);
    }

    [Fact]
    public async Task Handle_MapsReasonNameOntoPricePoints()
    {
        var product = SeedProduct(basePrice: 500m);
        _context.ProductPriceHistory.Add(
            MakeHistory(product.Id, 480m, DateTime.UtcNow.AddDays(-3), PriceChangeReason.Promotion));

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PricePoints[0].Reason.Should().Be(nameof(PriceChangeReason.Promotion));
    }

    [Fact]
    public async Task Handle_RoundsAverageToTwoDecimals()
    {
        var product = SeedProduct(basePrice: 100m);
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 100m, DateTime.UtcNow.AddDays(-3)));
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 100m, DateTime.UtcNow.AddDays(-2)));
        _context.ProductPriceHistory.Add(MakeHistory(product.Id, 101m, DateTime.UtcNow.AddDays(-1)));

        var result = await CreateHandler().Handle(
            new GetProductPriceHistoryQuery(product.Id), CancellationToken.None);

        // (100 + 100 + 101) / 3 = 100.333... -> rounded to 100.33
        result!.AveragePrice.Should().Be(100.33m);
    }
}
