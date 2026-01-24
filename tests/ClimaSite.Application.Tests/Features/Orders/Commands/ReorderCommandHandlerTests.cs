using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Commands;

public class ReorderCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MockDbContext _context;

    public ReorderCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _context = new MockDbContext();
    }

    [Fact]
    public async Task Handle_AddsItemsToCartFromPreviousOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsAdded.Should().Be(1);
        result.Value.Cart.Items.Should().HaveCount(1);
        result.Value.Cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = Guid.NewGuid() };

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
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task Handle_WhenProductOutOfStock_SkipsItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(0); // No stock

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsSkipped.Should().Be(1);
        result.Value.SkippedReasons.Should().Contain(r => r.Contains("max quantity"));
    }

    [Fact]
    public async Task Handle_WhenProductNotAvailable_SkipsItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        product.SetActive(false); // Product is no longer active

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsSkipped.Should().Be(1);
        result.Value.SkippedReasons.Should().Contain(r => r.Contains("no longer available"));
    }

    [Fact]
    public async Task Handle_WhenVariantNotAvailable_TriesToFindActiveVariant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var originalVariant = product.Variants.First();
        originalVariant.SetActive(false); // Original variant is inactive

        // Add a new active variant
        var newVariant = new ProductVariant(product.Id, "NEW-VAR", "New Variant");
        newVariant.SetStockQuantity(10);
        product.Variants.Add(newVariant);

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsAdded.Should().Be(1);
        result.Value.Cart.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_AdminCanReorderAnyUsersOrder()
    {
        // Arrange
        var orderOwnerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var order = CreateOrder("ORD-001", "user@test.com", orderOwnerId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOrderHasNoItems_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        // No items added

        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order has no items to reorder");
    }

    [Fact]
    public async Task Handle_CreatesNewCartIfNoneExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);
        // No cart added

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Cart.Should().NotBeNull();
        result.Value.Cart.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_AddsToExistingCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct("PROD-001", "Product 1");
        var product2 = CreateProduct("PROD-002", "Product 2");
        var variant1 = product1.Variants.First();
        var variant2 = product2.Variants.First();
        variant1.SetStockQuantity(10);
        variant2.SetStockQuantity(10);

        // Existing cart with product1
        var cart = new Cart(userId, null);
        cart.AddItem(product1.Id, variant1.Id, 1, 100m);

        // Order with product2
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product2, 2, 150m);

        _context.AddProduct(product1);
        _context.AddProduct(product2);
        _context.AddCart(cart);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_IncreasesQuantityIfItemAlreadyInCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        // Existing cart with product (qty 1)
        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        // Order with same product (qty 2)
        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Cart.Items.Should().HaveCount(1);
        result.Value.Cart.Items.First().Quantity.Should().Be(3); // 1 + 2
    }

    [Fact]
    public async Task Handle_LimitsQuantityBasedOnAvailableStock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(3); // Only 3 in stock

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product, 5, 299.99m); // Order had 5

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsAdded.Should().Be(1);
        result.Value.Cart.Items.First().Quantity.Should().Be(3); // Limited to stock
        result.Value.SkippedReasons.Should().Contain(r => r.Contains("limited stock"));
    }

    [Fact]
    public async Task Handle_WithGuestSession_CreatesGuestCart()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        // Guest order (no userId)
        var order = CreateOrder("ORD-001", "guest@test.com", null);
        AddOrderItem(order, product);

        _context.AddProduct(product);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id, GuestSessionId = sessionId };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Cart.GuestSessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ProcessesAllItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct("PROD-001", "Product 1");
        var product2 = CreateProduct("PROD-002", "Product 2");
        var variant1 = product1.Variants.First();
        var variant2 = product2.Variants.First();
        variant1.SetStockQuantity(10);
        variant2.SetStockQuantity(10);

        var order = CreateOrder("ORD-001", "user@test.com", userId);
        AddOrderItem(order, product1, 2, 100m);
        AddOrderItem(order, product2, 3, 150m);

        _context.AddProduct(product1);
        _context.AddProduct(product2);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);
        var handler = new ReorderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = new ReorderCommand { OrderId = order.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsAdded.Should().Be(2);
        result.Value.Cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Validator_WhenOrderIdEmpty_ReturnsValidationError()
    {
        // Arrange
        var validator = new ReorderCommandValidator();
        var command = new ReorderCommand { OrderId = Guid.Empty };

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
        var validator = new ReorderCommandValidator();
        var command = new ReorderCommand { OrderId = Guid.NewGuid() };

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
