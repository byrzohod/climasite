using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MockDbContext _context;

    public CreateOrderCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _context = new MockDbContext();
    }

    [Fact]
    public async Task Handle_WithValidCart_CreatesOrderSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.CustomerEmail.Should().Be("customer@test.com");
    }

    [Fact]
    public async Task Handle_ReducesStockForEachItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 3, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        variant.StockQuantity.Should().Be(7); // 10 - 3 = 7
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(2); // Only 2 in stock

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 5, 299.99m); // Trying to order 5

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task Handle_SetsCorrectOrderStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_GeneratesUniqueOrderNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderNumber.Should().NotBeNullOrEmpty();
        result.Value.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task Handle_ClearsCartAfterOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 2, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new Cart(userId, null); // Empty cart

        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty");
    }

    [Fact]
    public async Task Handle_WhenNoCartExists_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty");
    }

    [Fact]
    public async Task Handle_WhenProductNotActive_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        product.SetActive(false); // Mark as inactive
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no longer available");
    }

    [Fact]
    public async Task Handle_WhenVariantNotActive_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);
        variant.SetActive(false); // Mark variant as inactive

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("variant is no longer available");
    }

    [Fact]
    public async Task Handle_WithGuestSession_CreatesOrderSuccessfully()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(null, sessionId);
        cart.AddItem(product.Id, variant.Id, 1, 299.99m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand() with { GuestSessionId = sessionId };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithNoUserOrSession_ReturnsFailure()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand(); // No GuestSessionId

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("authentication or guest session");
    }

    [Fact]
    public async Task Handle_CalculatesShippingCostCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand() with { ShippingMethod = "express" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ShippingCost.Should().Be(15.99m);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ReducesStockForAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct("PROD-001", "Product 1");
        var product2 = CreateProduct("PROD-002", "Product 2");
        var variant1 = product1.Variants.First();
        var variant2 = product2.Variants.First();
        variant1.SetStockQuantity(10);
        variant2.SetStockQuantity(20);

        var cart = new Cart(userId, null);
        cart.AddItem(product1.Id, variant1.Id, 3, 100m);
        cart.AddItem(product2.Id, variant2.Id, 5, 150m);

        _context.AddProduct(product1);
        _context.AddProduct(product2);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = new CreateOrderCommandHandler(_context, _currentUserServiceMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        variant1.StockQuantity.Should().Be(7); // 10 - 3
        variant2.StockQuantity.Should().Be(15); // 20 - 5
    }

    [Fact]
    public void Validator_WhenEmailMissing_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            CustomerEmail = "",
            ShippingAddress = CreateValidAddress(),
            ShippingMethod = "standard"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Validator_WhenEmailInvalid_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            CustomerEmail = "invalid-email",
            ShippingAddress = CreateValidAddress(),
            ShippingMethod = "standard"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Validator_WhenShippingAddressMissing_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            CustomerEmail = "test@example.com",
            ShippingAddress = null!,
            ShippingMethod = "standard"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingAddress");
    }

    [Fact]
    public void Validator_WhenShippingMethodMissing_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            CustomerEmail = "test@example.com",
            ShippingAddress = CreateValidAddress(),
            ShippingMethod = ""
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingMethod");
    }

    [Fact]
    public void Validator_WhenValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = CreateValidCommand();

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

    private static CreateOrderCommand CreateValidCommand()
    {
        return new CreateOrderCommand
        {
            CustomerEmail = "customer@test.com",
            CustomerPhone = "+1234567890",
            ShippingAddress = CreateValidAddress(),
            ShippingMethod = "standard"
        };
    }

    private static AddressDto CreateValidAddress()
    {
        return new AddressDto
        {
            FirstName = "John",
            LastName = "Doe",
            AddressLine1 = "123 Test Street",
            City = "Test City",
            PostalCode = "12345",
            Country = "US"
        };
    }
}
