using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Commands;

public class CancelOrderCommandTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MockDbContext _context;

    public CancelOrderCommandTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _context = new MockDbContext();
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
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
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task Handle_WhenAdminCancelsAnyOrder_Succeeds()
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
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id, CancellationReason = "Admin cancellation" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Handle_WhenOrderIsPending_CancelsSuccessfully()
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
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand
        {
            OrderId = order.Id,
            CancellationReason = "Changed my mind"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Cancelled");
        result.Value.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenOrderIsPaid_CancelsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);
        order.SetPaymentInfo("pi_123", "card");
        order.SetStatus(OrderStatus.Paid);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Handle_WhenOrderIsShipped_CannotCancel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        // Progress order to Shipped status
        order.SetPaymentInfo("pi_123", "card");
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetStatus(OrderStatus.Shipped);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task Handle_WhenOrderIsDelivered_CannotCancel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        // Progress order to Delivered status
        order.SetPaymentInfo("pi_123", "card");
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetStatus(OrderStatus.Shipped);
        order.SetStatus(OrderStatus.Delivered);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task Handle_WhenOrderIsAlreadyCancelled_CannotCancelAgain()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);
        order.SetStatus(OrderStatus.Cancelled);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task Handle_RestoresStockWhenCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10); // Initial stock

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 3); // Order 3 items

        // Simulate stock reduction (as would happen during order placement)
        variant.AdjustStock(-3); // Now 7 items

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        variant.StockQuantity.Should().Be(10); // Stock restored
    }

    [Fact]
    public async Task Handle_RestoresStockForMultipleItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct("PROD-001", "Product 1");
        var product2 = CreateProduct("PROD-002", "Product 2");
        var variant1 = product1.Variants.First();
        var variant2 = product2.Variants.First();

        variant1.SetStockQuantity(20);
        variant2.SetStockQuantity(15);

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product1, 5);
        AddOrderItem(order, product2, 3);

        // Simulate stock reduction
        variant1.AdjustStock(-5);
        variant2.AdjustStock(-3);

        _context.AddProduct(product1);
        _context.AddProduct(product2);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        variant1.StockQuantity.Should().Be(20);
        variant2.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task Handle_SetsCancellationReason()
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
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand
        {
            OrderId = order.Id,
            CancellationReason = "Found a better deal elsewhere"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AddsOrderEvent()
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
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Events.Should().Contain(e => e.Status == "Cancelled");
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedOrderDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 2, 150m);
        order.SetShippingCost(10m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new CancelOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(order.Id);
        result.Value.OrderNumber.Should().Be("ORD-001");
        result.Value.Status.Should().Be("Cancelled");
        result.Value.Items.Should().HaveCount(1);
        result.Value.Subtotal.Should().Be(300m); // 2 * 150
        result.Value.ShippingCost.Should().Be(10m);
    }

    [Fact]
    public void Validator_WhenOrderIdEmpty_ReturnsValidationError()
    {
        // Arrange
        var validator = new CancelOrderCommandValidator();
        var command = new CancelOrderCommand { OrderId = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Validator_WhenOrderIdProvided_PassesValidation()
    {
        // Arrange
        var validator = new CancelOrderCommandValidator();
        var command = new CancelOrderCommand { OrderId = Guid.NewGuid() };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_CancellationReasonIsOptional()
    {
        // Arrange
        var validator = new CancelOrderCommandValidator();
        var command = new CancelOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CancellationReason = null
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
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
