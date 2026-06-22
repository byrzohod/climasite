using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetDashboardKpisQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetDashboardKpisQueryHandler CreateHandler() => new(_context);

    private static Product CreateProduct()
    {
        var product = new Product("KPI-PROD", "Kpi Product", "kpi-product", 100m);
        var variant = new ProductVariant(product.Id, "KPI-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private Order SeedOrder(Product product, decimal unitPrice, OrderStatus? status = null)
    {
        // BaseEntity.CreatedAt defaults to UtcNow, so the order falls within today/week/month windows.
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..12], "buyer@test.com");
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, unitPrice);
        DashboardOrderSeeding.WalkToStatus(order, status);
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task Handle_ReturnsZeroedKpis_WhenNoData()
    {
        var result = await CreateHandler().Handle(new GetDashboardKpisQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.ThisWeek.Should().Be(0);
        result.Revenue.ThisWeek.Should().Be(0);
        result.NewCustomers.ThisWeek.Should().Be(0);
        result.PendingOrders.Should().Be(0);
        result.LowStockCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CountsOrdersAndRevenue_ForCurrentPeriods()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, 100m);                          // pending, counts toward orders + revenue
        SeedOrder(product, 200m, OrderStatus.Paid);        // paid, counts toward orders + revenue
        SeedOrder(product, 999m, OrderStatus.Cancelled);   // cancelled: counts as an order, NOT revenue

        var result = await CreateHandler().Handle(new GetDashboardKpisQuery(), CancellationToken.None);

        result.TotalOrders.Today.Should().Be(3);
        result.TotalOrders.ThisWeek.Should().Be(3);
        result.TotalOrders.ThisMonth.Should().Be(3);
        // Revenue excludes the cancelled order.
        result.Revenue.Today.Should().Be(300m);
        result.Revenue.ThisWeek.Should().Be(300m);
        result.Revenue.ThisMonth.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_CountsNewCustomers_InCurrentPeriods()
    {
        _context.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "new@test.com",
            UserName = "new@test.com",
            CreatedAt = DateTime.UtcNow
        });

        var result = await CreateHandler().Handle(new GetDashboardKpisQuery(), CancellationToken.None);

        result.NewCustomers.Today.Should().Be(1);
        result.NewCustomers.ThisWeek.Should().Be(1);
        result.NewCustomers.ThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CountsPendingAndProcessingOrders_AsPending()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, 100m);                            // Pending
        SeedOrder(product, 100m, OrderStatus.Paid);          // Paid -> Processing below
        SeedOrder(product, 100m, OrderStatus.Processing);    // Processing
        SeedOrder(product, 100m, OrderStatus.Cancelled);     // not pending

        var result = await CreateHandler().Handle(new GetDashboardKpisQuery(), CancellationToken.None);

        // Pending = Pending + Processing statuses.
        result.PendingOrders.Should().Be(2);
    }

    [Fact]
    public async Task Handle_CountsLowStockVariants()
    {
        var product = new Product("LOW-PROD", "Low Product", "low-product", 100m);

        var lowVariant = new ProductVariant(product.Id, "LOW-V1", "Low");
        lowVariant.SetLowStockThreshold(5);
        lowVariant.SetStockQuantity(3); // at/under threshold
        product.Variants.Add(lowVariant);

        var healthyVariant = new ProductVariant(product.Id, "LOW-V2", "Healthy");
        healthyVariant.SetLowStockThreshold(5);
        healthyVariant.SetStockQuantity(50);
        product.Variants.Add(healthyVariant);

        var inactiveLowVariant = new ProductVariant(product.Id, "LOW-V3", "Inactive");
        inactiveLowVariant.SetLowStockThreshold(5);
        inactiveLowVariant.SetStockQuantity(0);
        inactiveLowVariant.SetActive(false); // inactive variants are ignored
        product.Variants.Add(inactiveLowVariant);

        _context.AddProduct(product);

        var result = await CreateHandler().Handle(new GetDashboardKpisQuery(), CancellationToken.None);

        result.LowStockCount.Should().Be(1);
    }
}
