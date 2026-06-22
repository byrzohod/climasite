using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetRecentOrdersQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetRecentOrdersQueryHandler CreateHandler() => new(_context);

    private static Product CreateProduct()
    {
        var product = new Product("REC-PROD", "Rec Product", "rec-product", 100m);
        var variant = new ProductVariant(product.Id, "REC-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private static void SetCreatedAt(Order order, DateTime createdAt) =>
        typeof(BaseEntity).GetProperty("CreatedAt")!.SetValue(order, createdAt);

    private Order SeedOrder(
        Product product,
        string orderNumber,
        ApplicationUser? user = null,
        string customerEmail = "guest@test.com",
        DateTime? createdAt = null)
    {
        var order = new Order(orderNumber, customerEmail);
        if (user != null)
        {
            order.SetUser(user.Id);
            DashboardOrderSeeding.LinkUserNavigation(order, user);
        }
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, 100m);
        if (createdAt.HasValue)
        {
            SetCreatedAt(order, createdAt.Value);
        }
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoOrders()
    {
        var result = await CreateHandler().Handle(new GetRecentOrdersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UsesUserFullName_WhenOrderHasUser()
    {
        var product = CreateProduct();
        _context.AddProduct(product);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "registered@test.com",
            UserName = "registered@test.com",
            FirstName = "Reg",
            LastName = "Buyer"
        };
        _context.Users.Add(user);

        SeedOrder(product, "ORD-USR-001", user: user);

        var result = await CreateHandler().Handle(new GetRecentOrdersQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].OrderNumber.Should().Be("ORD-USR-001");
        result[0].CustomerName.Should().Be("Reg Buyer");
    }

    [Fact]
    public async Task Handle_FallsBackToCustomerEmail_ForGuestOrders()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-GST-001", customerEmail: "guest@test.com");

        var result = await CreateHandler().Handle(new GetRecentOrdersQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CustomerName.Should().Be("guest@test.com");
    }

    [Fact]
    public async Task Handle_OrdersByCreatedAtDescending_AndRespectsCount()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-OLD", createdAt: DateTime.UtcNow.AddDays(-3));
        SeedOrder(product, "ORD-MID", createdAt: DateTime.UtcNow.AddDays(-2));
        SeedOrder(product, "ORD-NEW", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(
            new GetRecentOrdersQuery { Count = 2 },
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(o => o.OrderNumber).Should().ContainInOrder("ORD-NEW", "ORD-MID");
    }
}
