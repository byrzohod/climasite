using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetTopSellingProductsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetTopSellingProductsQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(string sku, string name, string? primaryImageUrl = null)
    {
        var product = new Product(sku, name, sku.ToLower(), 100m);
        if (primaryImageUrl != null)
        {
            var image = new ProductImage(product.Id, primaryImageUrl);
            image.SetPrimary(true);
            product.Images.Add(image);
        }
        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(100);
        product.Variants.Add(variant);
        // Product MUST be added before its orders so MockDbContext wires OrderItem.Product.
        _context.AddProduct(product);
        return product;
    }

    private void SeedOrder(IEnumerable<(Product Product, int Quantity, decimal UnitPrice)> lines, OrderStatus? status = null)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..12], "buyer@test.com");
        foreach (var (product, quantity, unitPrice) in lines)
        {
            var variant = product.Variants.First();
            order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, quantity, unitPrice);
        }
        DashboardOrderSeeding.WalkToStatus(order, status);
        _context.AddOrder(order);
        // oi.Order is read for the date filter; wire it the way Include would.
        DashboardOrderSeeding.LinkOrderNavigation(order);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoOrders()
    {
        var result = await CreateHandler().Handle(new GetTopSellingProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AggregatesQuantityAndRevenue_PerProduct()
    {
        var ac = SeedProduct("TOP-AC", "Air Conditioner");
        var heater = SeedProduct("TOP-HEAT", "Heater");

        SeedOrder(new[] { (ac, 3, 100m) });   // ac: 3 units, 300
        SeedOrder(new[] { (ac, 2, 100m) });   // ac: +2 units, +200
        SeedOrder(new[] { (heater, 1, 500m) }); // heater: 1 unit, 500

        var result = await CreateHandler().Handle(new GetTopSellingProductsQuery(), CancellationToken.None);

        var acDto = result.Single(p => p.Id == ac.Id);
        acDto.QuantitySold.Should().Be(5);
        acDto.Revenue.Should().Be(500m);
        acDto.Name.Should().Be("Air Conditioner");

        var heaterDto = result.Single(p => p.Id == heater.Id);
        heaterDto.QuantitySold.Should().Be(1);
        heaterDto.Revenue.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_OrdersByQuantitySoldDescending_AndRespectsCount()
    {
        var top = SeedProduct("TOP-1", "Best Seller");
        var mid = SeedProduct("TOP-2", "Mid Seller");
        var low = SeedProduct("TOP-3", "Low Seller");

        SeedOrder(new[] { (top, 10, 100m) });
        SeedOrder(new[] { (mid, 5, 100m) });
        SeedOrder(new[] { (low, 1, 100m) });

        var result = await CreateHandler().Handle(
            new GetTopSellingProductsQuery { Count = 2 },
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(top.Id);
        result[1].Id.Should().Be(mid.Id);
    }

    [Fact]
    public async Task Handle_ExcludesCancelledOrders()
    {
        var product = SeedProduct("TOP-CANC", "Cancelled Buy");

        SeedOrder(new[] { (product, 4, 100m) }, OrderStatus.Cancelled);

        var result = await CreateHandler().Handle(new GetTopSellingProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PopulatesPrimaryImageUrl()
    {
        var product = SeedProduct("TOP-IMG", "Imaged Product", primaryImageUrl: "https://example.com/top.jpg");

        SeedOrder(new[] { (product, 2, 100m) });

        var result = await CreateHandler().Handle(new GetTopSellingProductsQuery(), CancellationToken.None);

        result.Single().ImageUrl.Should().Be("https://example.com/top.jpg");
    }
}
