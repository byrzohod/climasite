using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Payments.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Payments.Commands;

public class CreatePaymentIntentCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly MockDbContext _context;

    public CreatePaymentIntentCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _context = new MockDbContext();
    }

    private CreatePaymentIntentCommandHandler CreateHandler() => new(
        _context,
        _currentUserServiceMock.Object,
        _paymentServiceMock.Object);

    [Fact]
    public async Task Handle_WithCartAndShippingMethod_ComputesEurTotalServerSide()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 2, 100m); // subtotal 200

        _context.AddProduct(product);
        _context.AddCart(cart);

        // 200 + express(15.99) + tax(40.00) = 255.99
        var expectedTotal = CheckoutPricing.CalculateTotal(200m, "express");

        decimal? capturedAmount = null;
        string? capturedCurrency = null;
        _paymentServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<decimal, string, Dictionary<string, string>?>((amount, currency, _) =>
            {
                capturedAmount = amount;
                capturedCurrency = currency;
            })
            .ReturnsAsync(PaymentIntentResult.Success("pi_123", "pi_123_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "express" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(expectedTotal);
        result.Value.Currency.Should().Be("EUR");
        result.Value.PaymentIntentId.Should().Be("pi_123");
        result.Value.ClientSecret.Should().Be("pi_123_secret");

        capturedAmount.Should().Be(expectedTotal);
        capturedCurrency.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_WithGuestCart_ComputesTotalFromSessionCart()
    {
        // Arrange
        const string sessionId = "guest-session-xyz";
        var product = CreateProduct();
        var variant = product.Variants.First();

        var cart = new Cart(null, sessionId);
        cart.AddItem(product.Id, variant.Id, 1, 50m); // subtotal 50

        _context.AddProduct(product);
        _context.AddCart(cart);

        var expectedTotal = CheckoutPricing.CalculateTotal(50m, "standard");

        _paymentServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(PaymentIntentResult.Success("pi_guest", "pi_guest_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand
        {
            ShippingMethod = "standard",
            GuestSessionId = sessionId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new Cart(userId, null); // empty
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "standard" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty");
        _paymentServiceMock.Verify(
            x => x.CreatePaymentIntentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoCartExists_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "standard" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty");
    }

    [Fact]
    public void Validator_WhenShippingMethodEmpty_ReturnsValidationError()
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "" };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingMethod");
    }

    private static Product CreateProduct(string sku = "TEST-SKU", string name = "Test Product")
    {
        var product = new Product(sku, name, name.ToLower().Replace(" ", "-"), 100m);
        var variant = new ProductVariant(product.Id, $"{sku}-VAR", "Default");
        variant.SetStockQuantity(100);
        product.Variants.Add(variant);
        return product;
    }
}
