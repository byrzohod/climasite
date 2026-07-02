using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Common.Payments;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Payments.Commands;
using ClimaSite.Application.Features.Reservations;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Payments.Commands;

public class CreatePaymentIntentCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IGuestSessionAccessor> _guestSessionMock;
    private readonly MockDbContext _context;

    public CreatePaymentIntentCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _guestSessionMock = new Mock<IGuestSessionAccessor>();
        _context = new MockDbContext();
    }

    private CreatePaymentIntentCommandHandler CreateHandler() => new(
        _context,
        _currentUserServiceMock.Object,
        _paymentServiceMock.Object,
        new StockReservationService(_context, new ReservationOptions()),
        _guestSessionMock.Object);

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
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<decimal, string, Dictionary<string, string>?, string?, CancellationToken>((amount, currency, _, _, _) =>
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
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentResult.Success("pi_guest", "pi_guest_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        // A2 legacy-reject: the handler keys the guest cart off the server-trusted signed cookie, not the raw
        // request id — so the accessor must publish the session id for a guest checkout to find its cart.
        _guestSessionMock.Setup(x => x.GuestSessionId).Returns(sessionId);
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
            x => x.CreatePaymentIntentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
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

    [Theory]
    [InlineData("free")]       // undisplayed method that used to ship for €0 — must be rejected
    [InlineData("overnightt")] // typo / unknown
    [InlineData("pickup")]
    public void Validator_WhenShippingMethodNotAllowed_ReturnsValidationError(string method)
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand { ShippingMethod = method };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingMethod");
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("express")]
    [InlineData("overnight")]
    [InlineData("STANDARD")] // case-insensitive
    public void Validator_WhenShippingMethodAllowed_Passes(string method)
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand { ShippingMethod = method };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeySupplied_ForwardsNamespacedKeyToPaymentService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        string? capturedKey = null;
        _paymentServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<decimal, string, Dictionary<string, string>?, string?, CancellationToken>((_, _, _, key, _) => capturedKey = key)
            .ReturnsAsync(PaymentIntentResult.Success("pi_idem", "pi_idem_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var clientKey = Guid.NewGuid().ToString("N");
        var command = new CreatePaymentIntentCommand
        {
            ShippingMethod = "standard",
            IdempotencyKey = clientKey
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: the handler namespaces the client key with "ci_" before forwarding it.
        result.IsSuccess.Should().BeTrue();
        capturedKey.Should().Be("ci_" + clientKey);
    }

    [Fact]
    public async Task Handle_WhenNoIdempotencyKey_ForwardsNullToPaymentService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        string? capturedKey = "sentinel";
        _paymentServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<decimal, string, Dictionary<string, string>?, string?, CancellationToken>((_, _, _, key, _) => capturedKey = key)
            .ReturnsAsync(PaymentIntentResult.Success("pi_nokey", "pi_nokey_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "standard" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: absent key degrades to today's no-dedup behaviour (null forwarded).
        result.IsSuccess.Should().BeTrue();
        capturedKey.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToPaymentService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        CancellationToken capturedToken = default;
        _paymentServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<decimal, string, Dictionary<string, string>?, string?, CancellationToken>((_, _, _, _, ct) => capturedToken = ct)
            .ReturnsAsync(PaymentIntentResult.Success("pi_ct", "pi_ct_secret", "requires_payment_method"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "standard" };

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        var result = await handler.Handle(command, token);

        // Assert: the handler threads the request CancellationToken straight through to the payment
        // service (symmetry with RefundAsync, which already takes one — B-061). Assert the EXACT token.
        result.IsSuccess.Should().BeTrue();
        capturedToken.Should().Be(token);
        _paymentServiceMock.Verify(
            x => x.CreatePaymentIntentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>(), token),
            Times.Once);
    }

    [Fact]
    public void Validator_WhenIdempotencyKeyMalformed_ReturnsValidationError()
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand
        {
            ShippingMethod = "standard",
            IdempotencyKey = "bad key!" // contains a space and a '!'
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentIntentCommand.IdempotencyKey));
    }

    [Fact]
    public void Validator_WhenIdempotencyKeyNull_IsValid()
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand { ShippingMethod = "standard", IdempotencyKey = null };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenIdempotencyKeyIsValidUuid_IsValid()
    {
        var validator = new CreatePaymentIntentCommandValidator();
        var command = new CreatePaymentIntentCommand
        {
            ShippingMethod = "standard",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
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
