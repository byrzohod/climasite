using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetOrderStatusChartQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetOrderStatusChartQueryHandler CreateHandler() => new(_context);

    private static Product CreateProduct()
    {
        var product = new Product("STAT-PROD", "Stat Product", "stat-product", 100m);
        var variant = new ProductVariant(product.Id, "STAT-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private void SeedOrder(Product product, OrderStatus? status = null)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..12], "buyer@test.com");
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, 100m);
        DashboardOrderSeeding.WalkToStatus(order, status);
        _context.AddOrder(order);
    }

    [Fact]
    public async Task Handle_ReturnsZeroCounts_WhenNoOrders()
    {
        var result = await CreateHandler().Handle(new GetOrderStatusChartQuery(), CancellationToken.None);

        result.Pending.Should().Be(0);
        result.Processing.Should().Be(0);
        result.Shipped.Should().Be(0);
        result.Delivered.Should().Be(0);
        result.Cancelled.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GroupsOrdersByStatus()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product);                          // Pending
        SeedOrder(product);                          // Pending
        SeedOrder(product, OrderStatus.Processing);  // Processing (Pending -> Paid -> Processing path)
        SeedOrder(product, OrderStatus.Shipped);     // Shipped
        SeedOrder(product, OrderStatus.Delivered);   // Delivered
        SeedOrder(product, OrderStatus.Cancelled);   // Cancelled

        var result = await CreateHandler().Handle(new GetOrderStatusChartQuery(), CancellationToken.None);

        result.Pending.Should().Be(2);
        result.Processing.Should().Be(1);
        result.Shipped.Should().Be(1);
        result.Delivered.Should().Be(1);
        result.Cancelled.Should().Be(1);
    }
}
