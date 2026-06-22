using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetRevenueChartQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetRevenueChartQueryHandler CreateHandler() => new(_context);

    private static Product CreateProduct()
    {
        var product = new Product("REV-PROD", "Rev Product", "rev-product", 100m);
        var variant = new ProductVariant(product.Id, "REV-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private void SeedOrder(Product product, decimal unitPrice, OrderStatus? status = null)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..12], "buyer@test.com");
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, unitPrice);
        DashboardOrderSeeding.WalkToStatus(order, status);
        _context.AddOrder(order);
    }

    [Theory]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    [InlineData("12m", 365)]
    public async Task Handle_ReturnsContiguousDataPoints_ForPeriod(string period, int expectedDays)
    {
        var result = await CreateHandler().Handle(
            new GetRevenueChartQuery { Period = period },
            CancellationToken.None);

        result.Period.Should().Be(period);
        // Inclusive range from (today - days + 1) through today.
        result.DataPoints.Should().HaveCount(expectedDays);
        result.DataPoints.Last().Date.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Handle_DefaultsTo7Days_ForUnknownPeriod()
    {
        var result = await CreateHandler().Handle(
            new GetRevenueChartQuery { Period = "weird" },
            CancellationToken.None);

        result.DataPoints.Should().HaveCount(7);
    }

    [Fact]
    public async Task Handle_AggregatesTodaysRevenue_ExcludingCancelled()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, 100m);                        // counts
        SeedOrder(product, 250m, OrderStatus.Paid);      // counts
        SeedOrder(product, 999m, OrderStatus.Cancelled); // excluded

        var result = await CreateHandler().Handle(
            new GetRevenueChartQuery { Period = "7d" },
            CancellationToken.None);

        var todayPoint = result.DataPoints.Single(p => p.Date.Date == DateTime.UtcNow.Date);
        todayPoint.Value.Should().Be(350m);
    }

    [Fact]
    public async Task Handle_ReturnsZeroValuedPoints_WhenNoOrders()
    {
        var result = await CreateHandler().Handle(
            new GetRevenueChartQuery { Period = "7d" },
            CancellationToken.None);

        result.DataPoints.Should().OnlyContain(p => p.Value == 0);
        result.DataPoints.Should().OnlyContain(p => p.Label != string.Empty);
    }
}
