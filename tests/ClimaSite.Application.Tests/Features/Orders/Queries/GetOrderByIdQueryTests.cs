using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Queries;

public class GetOrderByIdQueryTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MockDbContext _context;

    public GetOrderByIdQueryTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _context = new MockDbContext();
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
    }

    [Fact]
    public async Task Handle_WhenUserOwnsOrder_ReturnsOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("ORD-001");
        result.Value.CustomerEmail.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnOrder_ReturnsAccessDenied()
    {
        // Arrange
        var orderOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", orderOwnerId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(differentUserId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task Handle_WhenAdminAccessesAnyOrder_ReturnsOrder()
    {
        // Arrange
        var orderOwnerId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", orderOwnerId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminUserId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("ORD-001");
    }

    [Fact]
    public async Task Handle_ReturnsOrderWithItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct("PROD-001", "Product 1");
        var product2 = CreateProduct("PROD-002", "Product 2");
        var order = CreateOrder("ORD-001", "user@test.com", userId);

        AddOrderItem(order, product1, 2, 100m);
        AddOrderItem(order, product2, 1, 200m);

        _context.AddProduct(product1);
        _context.AddProduct(product2);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].ProductName.Should().Be("Product 1");
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.Items[0].UnitPrice.Should().Be(100m);
        result.Value.Items[1].ProductName.Should().Be("Product 2");
        result.Value.Items[1].Quantity.Should().Be(1);
        result.Value.Items[1].UnitPrice.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_ReturnsOrderWithEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        // Add events by changing status
        order.SetPaymentInfo("pi_123", "card");
        order.SetStatus(OrderStatus.Paid, "Payment received");

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Events.Should().NotBeEmpty();
        result.Value.Events.Should().Contain(e => e.Status == "Paid");
    }

    [Fact]
    public async Task Handle_ReturnsCorrectTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);

        AddOrderItem(order, product, 2, 150m); // 300
        order.SetShippingCost(25m);
        order.SetTaxAmount(30m);
        order.SetDiscountAmount(10m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Subtotal.Should().Be(300m);
        result.Value.ShippingCost.Should().Be(25m);
        result.Value.TaxAmount.Should().Be(30m);
        result.Value.DiscountAmount.Should().Be(10m);
        result.Value.Total.Should().Be(345m); // 300 + 25 + 30 - 10
    }

    [Fact]
    public async Task Handle_ReturnsOrderStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        order.SetPaymentInfo("pi_123", "card");
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetShippingMethod("express");
        order.SetTrackingNumber("TRK123456");
        order.SetStatus(OrderStatus.Shipped);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Shipped");
        result.Value.ShippingMethod.Should().Be("express");
        result.Value.TrackingNumber.Should().Be("TRK123456");
        result.Value.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenNoUserIdProvided_ReturnsOrder()
    {
        // Arrange - guest order scenario
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "guest@test.com", null);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsItemImageUrls()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProductWithImage("PROD-001", "Test Product", "https://example.com/product.jpg");
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items[0].ImageUrl.Should().Be("https://example.com/product.jpg");
    }

    [Fact]
    public async Task Handle_ReturnsCancelledOrderInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);
        order.SetCancellationReason("Customer requested cancellation");
        order.SetStatus(OrderStatus.Cancelled);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new GetOrderByIdQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetOrderByIdQuery { OrderId = order.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Cancelled");
        result.Value.CancelledAt.Should().NotBeNull();
    }

    private static Product CreateProduct(string sku = "TEST-SKU", string name = "Test Product")
    {
        var product = new Product(sku, name, name.ToLower().Replace(" ", "-"), 299.99m);
        var variant = new ProductVariant(product.Id, $"{sku}-VAR", "Default");
        variant.SetStockQuantity(100);
        product.Variants.Add(variant);

        var image = new ProductImage(product.Id, "https://example.com/default.jpg");
        image.SetPrimary(true);
        product.Images.Add(image);

        return product;
    }

    private static Product CreateProductWithImage(string sku, string name, string imageUrl)
    {
        var product = new Product(sku, name, name.ToLower().Replace(" ", "-"), 299.99m);
        var variant = new ProductVariant(product.Id, $"{sku}-VAR", "Default");
        variant.SetStockQuantity(100);
        product.Variants.Add(variant);

        var image = new ProductImage(product.Id, imageUrl);
        image.SetPrimary(true);
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
}
