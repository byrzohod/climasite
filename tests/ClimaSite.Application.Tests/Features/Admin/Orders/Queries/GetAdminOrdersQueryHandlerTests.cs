using ClimaSite.Application.Features.Admin.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders.Queries;

// Exercises the admin order-listing pipeline: _context.Orders.Include(...).AsQueryable() with
// search/status/date filters and sorting, then CountAsync()/ToListAsync(). MockDbContext treats
// Include as a no-op, so the Order.User navigation is wired manually (the same way EF materialises it).
public class GetAdminOrdersQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminOrdersQueryHandler CreateHandler() => new(_context);

    private static void SetCreatedAt(Order order, DateTime createdAt) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!.SetValue(order, createdAt);

    private static void LinkUserNavigation(Order order, ApplicationUser user) =>
        typeof(Order).GetProperty("User")!.SetValue(order, user);

    private static Product CreateProduct()
    {
        var product = new Product("ORD-PROD", "Order Product", "order-product", 100m);
        var variant = new ProductVariant(product.Id, "ORD-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private Order SeedOrder(
        Product product,
        string orderNumber,
        string customerEmail = "guest@test.com",
        ApplicationUser? user = null,
        OrderStatus? status = null,
        decimal unitPrice = 100m,
        int quantity = 1,
        int itemLines = 1,
        DateTime? createdAt = null)
    {
        var order = new Order(orderNumber, customerEmail);
        if (user != null)
        {
            order.SetUser(user.Id);
            LinkUserNavigation(order, user);
        }
        var variant = product.Variants.First();
        for (var i = 0; i < itemLines; i++)
        {
            order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, quantity, unitPrice);
        }
        WalkToStatus(order, status);
        if (createdAt.HasValue)
        {
            SetCreatedAt(order, createdAt.Value);
        }
        _context.AddOrder(order);
        return order;
    }

    // Order status transitions are domain-validated, so reaching a later status requires walking the
    // full valid path (Pending -> Paid -> Processing -> Shipped -> ...) rather than a single jump.
    private static void WalkToStatus(Order order, OrderStatus? target)
    {
        if (!target.HasValue || target.Value == OrderStatus.Pending)
        {
            return;
        }

        switch (target.Value)
        {
            case OrderStatus.Cancelled:
                order.SetStatus(OrderStatus.Cancelled);
                break;
            case OrderStatus.Paid:
                order.SetStatus(OrderStatus.Paid);
                break;
            case OrderStatus.Processing:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                break;
            case OrderStatus.Shipped:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                break;
            case OrderStatus.Delivered:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                order.SetStatus(OrderStatus.Delivered);
                break;
            default:
                order.SetStatus(OrderStatus.Paid);
                break;
        }
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoOrders()
    {
        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
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

        SeedOrder(product, "ORD-USR-001", customerEmail: "registered@test.com", user: user, itemLines: 2);

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        var item = result.Items.Should().ContainSingle().Subject;
        item.OrderNumber.Should().Be("ORD-USR-001");
        item.CustomerName.Should().Be("Reg Buyer");
        item.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FallsBackToCustomerEmail_ForGuestOrders()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-GST-001", customerEmail: "guest@test.com");

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        var item = result.Items.Should().ContainSingle().Subject;
        item.CustomerName.Should().Be("guest@test.com");
        item.CustomerEmail.Should().Be("guest@test.com");
    }

    [Fact]
    public async Task Handle_PaymentStatus_ReflectsPaidAt()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-PAID", status: OrderStatus.Paid);
        SeedOrder(product, "ORD-PENDING");

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.Items.Single(o => o.OrderNumber == "ORD-PAID").PaymentStatus.Should().Be("Paid");
        result.Items.Single(o => o.OrderNumber == "ORD-PENDING").PaymentStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_SearchMatchesOrderNumber()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-ABC-123", customerEmail: "a@test.com");
        SeedOrder(product, "ORD-XYZ-999", customerEmail: "b@test.com");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Search = "abc" }, CancellationToken.None);

        result.Items.Should().ContainSingle(o => o.OrderNumber == "ORD-ABC-123");
    }

    [Fact]
    public async Task Handle_SearchMatchesCustomerEmail()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-1", customerEmail: "findme@test.com");
        SeedOrder(product, "ORD-2", customerEmail: "other@test.com");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Search = "FINDME" }, CancellationToken.None);

        result.Items.Should().ContainSingle(o => o.OrderNumber == "ORD-1");
    }

    [Fact]
    public async Task Handle_StatusFilter_ReturnsMatchingOrders()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-PAID", status: OrderStatus.Paid);
        SeedOrder(product, "ORD-PENDING");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Status = "Paid" }, CancellationToken.None);

        result.Items.Should().ContainSingle(o => o.OrderNumber == "ORD-PAID");
    }

    [Fact]
    public async Task Handle_InvalidStatusFilter_IsIgnored()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-1");
        SeedOrder(product, "ORD-2");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Status = "NotARealStatus" }, CancellationToken.None);

        result.TotalCount.Should().Be(2, "an unparseable status filter must be ignored");
    }

    [Fact]
    public async Task Handle_DateRange_FiltersOrders()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-OLD", createdAt: DateTime.UtcNow.AddDays(-10));
        SeedOrder(product, "ORD-NEW", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { DateFrom = DateTime.UtcNow.AddDays(-3) }, CancellationToken.None);

        result.Items.Should().ContainSingle(o => o.OrderNumber == "ORD-NEW");
    }

    [Fact]
    public async Task Handle_SortsByTotalDescending()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-LOW", unitPrice: 50m);
        SeedOrder(product, "ORD-HIGH", unitPrice: 500m);
        SeedOrder(product, "ORD-MID", unitPrice: 200m);

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { SortBy = "total", SortOrder = "desc" }, CancellationToken.None);

        result.Items.Select(o => o.OrderNumber).Should().ContainInOrder("ORD-HIGH", "ORD-MID", "ORD-LOW");
    }

    [Fact]
    public async Task Handle_DefaultsToCreatedAtDescending()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        SeedOrder(product, "ORD-OLD", createdAt: DateTime.UtcNow.AddDays(-3));
        SeedOrder(product, "ORD-NEW", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.Items.Select(o => o.OrderNumber).Should().ContainInOrder("ORD-NEW", "ORD-OLD");
    }

    [Fact]
    public async Task Handle_AppliesPagingAndComputesTotalPages()
    {
        var product = CreateProduct();
        _context.AddProduct(product);

        for (var i = 0; i < 5; i++)
        {
            SeedOrder(product, $"ORD-{i}", createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
    }
}
