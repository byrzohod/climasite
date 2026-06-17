using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
    private readonly MockDbContext _context;

    // GAP-06: bank-transfer account details surfaced in the instructions email.
    private static readonly BankTransferOptions BankOptions = new()
    {
        Iban = "BG80BNBG96611020345678",
        AccountName = "ClimaSite EOOD",
        BankName = "placeholder Bank"
    };

    public CreateOrderCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();
        _context = new MockDbContext();

        // BUG-04: the handler refunds an already-charged intent on any post-charge failure.
        // Default RefundAsync to a succeeded result so the await never hits a null Task; tests
        // that assert refund behaviour can still Verify the call count.
        _paymentServiceMock
            .Setup(x => x.RefundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => PaymentIntentResult.Success(id, string.Empty, "succeeded"));
    }

    // Use a real EmailOutbox over the mock context so tests can assert the confirmation email
    // is staged in the same unit of work as the order (GAP-03).
    private CreateOrderCommandHandler CreateHandler() => new(
        _context,
        _currentUserServiceMock.Object,
        _paymentServiceMock.Object,
        new EmailOutbox(_context),
        BankOptions,
        _loggerMock.Object);

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
        var handler = CreateHandler();
        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.CustomerEmail.Should().Be("customer@test.com");

        // GAP-03: a confirmation email is staged for the customer in the same unit of work.
        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().ContainSingle(m =>
            m.Type == OutboxMessageTypes.OrderConfirmation && m.ToEmail == "customer@test.com");
    }

    [Fact]
    public async Task Handle_CardOrderWithoutPaymentIntent_IsRejected()
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
        var handler = CreateHandler();

        // A card order with no PaymentIntent must never create an unpaid, stock-depleting order.
        var command = CreateValidCommand() with { PaymentMethod = "card" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("verified card payment");
        _paymentServiceMock.Verify(x => x.GetPaymentIntentAsync(It.IsAny<string>()), Times.Never);
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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
        var handler = CreateHandler();
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

    #region Payment intent verification (BUG-01)

    [Fact]
    public async Task Handle_WithVerifiedPaymentIntent_SetsPaymentInfoOnOrder()
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

        // Expected order total: subtotal 100 + standard shipping 5.99 + tax 20.00 = 125.99
        var expectedTotal = CheckoutPricing.CalculateTotal(100m, "standard");
        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "succeeded",
                Currency = "eur",
                Amount = CheckoutPricing.ToMinorUnits(expectedTotal)
            });

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with
        {
            PaymentIntentId = "pi_test_123",
            PaymentMethod = "card"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentMethod.Should().Be("card");
        result.Value.Total.Should().Be(expectedTotal);

        var persisted = await _context.Orders.SingleAsync();
        persisted.PaymentIntentId.Should().Be("pi_test_123");
        persisted.PaymentMethod.Should().Be("card");
        // Status stays Pending; the webhook flips it to Paid.
        persisted.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenPaymentIntentAmountMismatch_ReturnsFailure()
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

        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "succeeded",
                Currency = "eur",
                Amount = 100 // way short of the real total
            });

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentIntentId = "pi_test_123" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Payment could not be verified");
    }

    [Fact]
    public async Task Handle_WhenPaymentIntentWrongCurrency_ReturnsFailure()
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

        var expectedTotal = CheckoutPricing.CalculateTotal(100m, "standard");
        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "succeeded",
                Currency = "usd", // wrong currency
                Amount = CheckoutPricing.ToMinorUnits(expectedTotal)
            });

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentIntentId = "pi_test_123" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Payment could not be verified");
    }

    [Fact]
    public async Task Handle_WhenPaymentIntentNotSucceeded_ReturnsFailure()
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

        var expectedTotal = CheckoutPricing.CalculateTotal(100m, "standard");
        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "requires_payment_method", // not succeeded
                Currency = "eur",
                Amount = CheckoutPricing.ToMinorUnits(expectedTotal)
            });

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentIntentId = "pi_test_123" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Payment could not be verified");
    }

    [Fact]
    public async Task Handle_WithoutPaymentIntentId_DoesNotCallPaymentService()
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
        var handler = CreateHandler();
        var command = CreateValidCommand(); // no PaymentIntentId

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _paymentServiceMock.Verify(x => x.GetPaymentIntentAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenChargedIntentButStockUnavailable_RefundsCharge()
    {
        // Arrange: a verified, correctly-charged card intent, but the variant has zero stock
        // at order time. BUG-04: the charge would otherwise be orphaned, so it must be refunded.
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(0); // MockDbContext.TryDecrementVariantStockAsync returns 0 when stock < qty

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        var expectedTotal = CheckoutPricing.CalculateTotal(100m, "standard");
        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "succeeded",
                Currency = "eur",
                Amount = CheckoutPricing.ToMinorUnits(expectedTotal)
            });
        _paymentServiceMock
            .Setup(x => x.RefundAsync("pi_test_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentResult.Success("pi_test_123", string.Empty, "succeeded"));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with
        {
            PaymentIntentId = "pi_test_123",
            PaymentMethod = "card"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");
        _paymentServiceMock.Verify(x => x.RefundAsync("pi_test_123", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Bank transfer / offline orders (GAP-06)

    [Fact]
    public async Task Handle_BankTransferOrder_PersistsMethodAndStaysPending()
    {
        // Arrange: an offline (bank) order has no PaymentIntent, so the payment service is never
        // called, the method is still persisted, and the order legitimately remains Pending.
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentMethod = "bank" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentMethod.Should().Be("bank");
        result.Value.Status.Should().Be("Pending");

        var persisted = await _context.Orders.SingleAsync();
        persisted.PaymentMethod.Should().Be("bank");
        persisted.PaymentIntentId.Should().BeNull();
        persisted.Status.Should().Be(OrderStatus.Pending);

        // No card verification happens for an offline order.
        _paymentServiceMock.Verify(x => x.GetPaymentIntentAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BankTransferOrder_EnqueuesBankInstructionsEmail()
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
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentMethod = "bank" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: both the confirmation email AND the bank-instructions email are staged for the
        // customer in the same unit of work (real EmailOutbox over the mock context).
        result.IsSuccess.Should().BeTrue();
        var queued = await _context.OutboxMessages.ToListAsync();

        queued.Should().Contain(m =>
            m.Type == OutboxMessageTypes.OrderConfirmation && m.ToEmail == "customer@test.com");

        var bankEmail = queued.SingleOrDefault(m =>
            m.Type == OutboxMessageTypes.Generic && m.ToEmail == "customer@test.com");
        bankEmail.Should().NotBeNull();
        // The instructions carry the payment reference (order number), the bank account, and amount.
        bankEmail!.Payload.Should().Contain(result.Value!.OrderNumber);
        bankEmail.Payload.Should().Contain(BankOptions.Iban);
        bankEmail.Payload.Should().Contain(BankOptions.AccountName);
        bankEmail.Payload.Should().Contain("Bank transfer instructions");
    }

    [Fact]
    public async Task Handle_CardOrderWithVerifiedIntent_DoesNotEnqueueBankEmail()
    {
        // Arrange: a verified card order must NOT stage bank-transfer instructions.
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var variant = product.Variants.First();
        variant.SetStockQuantity(10);

        var cart = new Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 1, 100m);

        _context.AddProduct(product);
        _context.AddCart(cart);

        var expectedTotal = CheckoutPricing.CalculateTotal(100m, "standard");
        _paymentServiceMock
            .Setup(x => x.GetPaymentIntentAsync("pi_test_123"))
            .ReturnsAsync(new PaymentIntentResult
            {
                Succeeded = true,
                PaymentIntentId = "pi_test_123",
                Status = "succeeded",
                Currency = "eur",
                Amount = CheckoutPricing.ToMinorUnits(expectedTotal)
            });

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var handler = CreateHandler();
        var command = CreateValidCommand() with { PaymentIntentId = "pi_test_123", PaymentMethod = "card" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().NotContain(m => m.Type == OutboxMessageTypes.Generic);
    }

    [Fact]
    public void Validator_WhenPaymentMethodIsPaypal_ReturnsValidationError()
    {
        // GAP-06: the fake "paypal" option is no longer accepted.
        var validator = new CreateOrderCommandValidator();
        var command = CreateValidCommand() with { PaymentMethod = "paypal" };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PaymentMethod");
    }

    [Fact]
    public void Validator_WhenPaymentMethodIsUnknown_ReturnsValidationError()
    {
        var validator = new CreateOrderCommandValidator();
        var command = CreateValidCommand() with { PaymentMethod = "crypto" };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PaymentMethod");
    }

    [Theory]
    [InlineData("card")]
    [InlineData("bank")]
    [InlineData("BANK")]
    public void Validator_WhenPaymentMethodIsSupported_PassesValidation(string method)
    {
        var validator = new CreateOrderCommandValidator();
        var command = CreateValidCommand() with { PaymentMethod = method };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    #endregion

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
