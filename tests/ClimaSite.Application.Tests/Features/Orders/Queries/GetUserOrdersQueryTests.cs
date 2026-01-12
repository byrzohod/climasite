using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Queries;

public class GetUserOrdersQueryTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MockDbContext _context;

    public GetUserOrdersQueryTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _context = new MockDbContext();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsEmptyList()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUserHasOrders_ReturnsUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var product = CreateProduct();
        var userOrder = CreateOrder("ORD-001", "user@test.com", userId);
        var userOrder2 = CreateOrder("ORD-002", "user@test.com", userId);
        var otherUserOrder = CreateOrder("ORD-003", "other@test.com", otherUserId);

        AddOrderItem(userOrder, product);
        AddOrderItem(userOrder2, product);
        AddOrderItem(otherUserOrder, product);

        _context.AddProduct(product);
        _context.AddOrder(userOrder);
        _context.AddOrder(userOrder2);
        _context.AddOrder(otherUserOrder);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber == "ORD-001" || o.OrderNumber == "ORD-002");
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var pendingOrder = CreateOrder("ORD-001", "user@test.com", userId);
        var paidOrder = CreateOrder("ORD-002", "user@test.com", userId);
        paidOrder.SetPaymentInfo("pi_123", "card");
        paidOrder.SetStatus(OrderStatus.Paid);

        AddOrderItem(pendingOrder, product);
        AddOrderItem(paidOrder, product);

        _context.AddProduct(product);
        _context.AddOrder(pendingOrder);
        _context.AddOrder(paidOrder);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { Status = "Paid" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Paid");
    }

    [Fact]
    public async Task Handle_WithDateFromFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var oldOrder = CreateOrder("ORD-001", "user@test.com", userId);
        var recentOrder = CreateOrder("ORD-002", "user@test.com", userId);

        AddOrderItem(oldOrder, product);
        AddOrderItem(recentOrder, product);

        SetCreatedAt(oldOrder, DateTime.UtcNow.AddDays(-10));
        SetCreatedAt(recentOrder, DateTime.UtcNow.AddDays(-1));

        _context.AddProduct(product);
        _context.AddOrder(oldOrder);
        _context.AddOrder(recentOrder);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { DateFrom = DateTime.UtcNow.AddDays(-5) };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].OrderNumber.Should().Be("ORD-002");
    }

    [Fact]
    public async Task Handle_WithDateToFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var oldOrder = CreateOrder("ORD-001", "user@test.com", userId);
        var recentOrder = CreateOrder("ORD-002", "user@test.com", userId);

        AddOrderItem(oldOrder, product);
        AddOrderItem(recentOrder, product);

        SetCreatedAt(oldOrder, DateTime.UtcNow.AddDays(-10));
        SetCreatedAt(recentOrder, DateTime.UtcNow.AddDays(-1));

        _context.AddProduct(product);
        _context.AddOrder(oldOrder);
        _context.AddOrder(recentOrder);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { DateTo = DateTime.UtcNow.AddDays(-5) };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].OrderNumber.Should().Be("ORD-001");
    }

    [Fact]
    public async Task Handle_WithSearchQuery_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var order1 = CreateOrder("ORD-ABC-001", "user@test.com", userId);
        var order2 = CreateOrder("ORD-XYZ-002", "user@test.com", userId);

        AddOrderItem(order1, product);
        AddOrderItem(order2, product);

        _context.AddProduct(product);
        _context.AddOrder(order1);
        _context.AddOrder(order2);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { SearchQuery = "ABC" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].OrderNumber.Should().Contain("ABC");
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var orders = Enumerable.Range(1, 15)
            .Select(i => CreateOrder($"ORD-{i:D3}", "user@test.com", userId))
            .ToList();

        foreach (var order in orders)
        {
            AddOrderItem(order, product);
        }

        _context.AddProduct(product);
        foreach (var order in orders)
        {
            _context.AddOrder(order);
        }

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(15);
        result.Items.Should().HaveCount(5);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Theory]
    [InlineData("date", "desc")]
    [InlineData("date", "asc")]
    [InlineData("total", "desc")]
    [InlineData("total", "asc")]
    public async Task Handle_WithSorting_ReturnsOrderedResults(string sortBy, string sortDirection)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();

        var order1 = CreateOrder("ORD-001", "user@test.com", userId);
        var order2 = CreateOrder("ORD-002", "user@test.com", userId);
        var order3 = CreateOrder("ORD-003", "user@test.com", userId);

        AddOrderItem(order1, product, 1, 100m);
        AddOrderItem(order2, product, 2, 200m);
        AddOrderItem(order3, product, 1, 50m);

        SetCreatedAt(order1, DateTime.UtcNow.AddDays(-3));
        SetCreatedAt(order2, DateTime.UtcNow.AddDays(-1));
        SetCreatedAt(order3, DateTime.UtcNow.AddDays(-2));

        _context.AddProduct(product);
        _context.AddOrder(order1);
        _context.AddOrder(order2);
        _context.AddOrder(order3);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery { SortBy = sortBy, SortDirection = sortDirection };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);

        if (sortBy == "total" && sortDirection == "desc")
        {
            result.Items[0].Total.Should().BeGreaterThanOrEqualTo(result.Items[1].Total);
            result.Items[1].Total.Should().BeGreaterThanOrEqualTo(result.Items[2].Total);
        }
        else if (sortBy == "total" && sortDirection == "asc")
        {
            result.Items[0].Total.Should().BeLessThanOrEqualTo(result.Items[1].Total);
            result.Items[1].Total.Should().BeLessThanOrEqualTo(result.Items[2].Total);
        }
    }

    [Fact]
    public async Task Handle_ReturnsCorrectItemCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);

        AddOrderItem(order, product, 3, 100m);
        AddOrderItem(order, product, 2, 200m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].ItemCount.Should().Be(5); // 3 + 2
    }

    [Fact]
    public async Task Handle_ReturnsMax3Items()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var products = Enumerable.Range(1, 5).Select(i =>
        {
            var p = CreateProduct($"PROD-{i}", $"Product {i}");
            return p;
        }).ToList();

        var order = CreateOrder("ORD-001", "user@test.com", userId);

        foreach (var product in products)
        {
            AddOrderItem(order, product);
            _context.AddProduct(product);
        }

        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new GetUserOrdersQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetUserOrdersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].Items.Should().HaveCount(3); // Max 3 items returned
    }

    private static Product CreateProduct(string sku = "TEST-SKU", string name = "Test Product")
    {
        var product = new Product(sku, name, name.ToLower().Replace(" ", "-"), 299.99m);
        var variant = new ProductVariant(product.Id, $"{sku}-VAR", "Default");
        variant.SetStockQuantity(100);
        product.Variants.Add(variant);

        var image = new ProductImage(product.Id, "https://example.com/image.jpg");
        image.SetSortOrder(0);
        product.Images.Add(image);

        return product;
    }

    private static Order CreateOrder(string orderNumber, string email, Guid? userId = null)
    {
        var order = new Order(orderNumber, email);
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        return order;
    }

    private static void AddOrderItem(Order order, Product product, int quantity = 1, decimal unitPrice = 299.99m)
    {
        var variant = product.Variants.First();
        order.AddItem(
            product.Id,
            variant.Id,
            product.Name,
            variant.Name,
            variant.Sku,
            quantity,
            unitPrice);
    }

    private static void SetCreatedAt(Order order, DateTime createdAt)
    {
        var property = typeof(BaseEntity).GetProperty("CreatedAt");
        property?.SetValue(order, createdAt);
    }
}
