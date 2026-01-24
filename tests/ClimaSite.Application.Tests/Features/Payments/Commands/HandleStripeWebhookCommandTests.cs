using ClimaSite.Application.Features.Payments.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Payments.Commands;

public class HandleStripeWebhookCommandTests
{
    private readonly MockDbContext _context;
    private readonly Mock<ILogger<HandleStripeWebhookCommandHandler>> _loggerMock;
    private readonly HandleStripeWebhookCommandHandler _handler;

    public HandleStripeWebhookCommandTests()
    {
        _context = new MockDbContext();
        _loggerMock = new Mock<ILogger<HandleStripeWebhookCommandHandler>>();
        _handler = new HandleStripeWebhookCommandHandler(_context, _loggerMock.Object);
    }

    #region payment_intent.succeeded tests

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_WhenOrderIsPending_MarksAsPaid()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_test_123"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_WhenOrderIsPaymentFailed_MarksAsPaid()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.PaymentFailed, "Previous payment failed");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_test_123"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_WhenOrderAlreadyPaid_SkipsUpdate()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.Paid, "Already paid");
        var originalPaidAt = order.PaidAt;
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_test_123"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().Be(originalPaidAt);
    }

    #endregion

    #region payment_intent.payment_failed tests

    [Fact]
    public async Task Handle_PaymentIntentFailed_WhenOrderIsPending_MarksAsPaymentFailed()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.payment_failed",
            PaymentIntentId = "pi_test_123",
            FailureMessage = "Your card was declined"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
    }

    [Fact]
    public async Task Handle_PaymentIntentFailed_WhenOrderAlreadyPaid_SkipsUpdate()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.Paid);
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.payment_failed",
            PaymentIntentId = "pi_test_123",
            FailureMessage = "Your card was declined"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid); // Status unchanged
    }

    [Fact]
    public async Task Handle_PaymentIntentFailed_WithNoFailureMessage_StillMarksAsPaymentFailed()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.payment_failed",
            PaymentIntentId = "pi_test_123",
            FailureMessage = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
    }

    #endregion

    #region charge.refunded tests

    [Fact]
    public async Task Handle_ChargeRefunded_WhenOrderIsPaid_MarksAsRefunded()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.Paid);
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "charge.refunded",
            PaymentIntentId = "pi_test_123",
            ChargeId = "ch_test_123",
            AmountRefunded = 10000 // 100.00 in cents
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public async Task Handle_ChargeRefunded_WhenOrderIsProcessing_MarksAsRefunded()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "charge.refunded",
            PaymentIntentId = "pi_test_123",
            ChargeId = "ch_test_123",
            AmountRefunded = 5000
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public async Task Handle_ChargeRefunded_WhenOrderIsPending_CannotRefund()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        // Order is still Pending, hasn't been paid
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "charge.refunded",
            PaymentIntentId = "pi_test_123",
            ChargeId = "ch_test_123",
            AmountRefunded = 10000
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Webhook acknowledged but no action taken
        order.Status.Should().Be(OrderStatus.Pending); // Status unchanged
    }

    [Fact]
    public async Task Handle_ChargeRefunded_WhenOrderAlreadyRefunded_SkipsUpdate()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Refunded);
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "charge.refunded",
            PaymentIntentId = "pi_test_123",
            ChargeId = "ch_test_123",
            AmountRefunded = 10000
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsSuccessWithoutAction()
    {
        // Arrange - no order in context
        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_nonexistent"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Webhook acknowledged
    }

    [Fact]
    public async Task Handle_UnknownEventType_ReturnsSuccess()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "unknown.event",
            PaymentIntentId = "pi_test_123"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Pending); // Unchanged
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_AddsOrderEvent()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_test_123"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        order.Events.Should().Contain(e => e.Status == OrderStatus.Paid);
    }

    [Fact]
    public async Task Handle_PaymentIntentFailed_AddsOrderEventWithDescription()
    {
        // Arrange
        var order = CreateOrder("ORD-001", "user@test.com");
        order.SetPaymentInfo("pi_test_123", "card");
        _context.AddOrder(order);

        var command = new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.payment_failed",
            PaymentIntentId = "pi_test_123",
            FailureMessage = "Insufficient funds"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        order.Events.Should().Contain(e => 
            e.Status == OrderStatus.PaymentFailed && 
            e.Description != null && 
            e.Description.Contains("Insufficient funds"));
    }

    #endregion

    private static Order CreateOrder(string orderNumber, string email, Guid? userId = null)
    {
        var order = new Order(orderNumber, email);
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        return order;
    }
}
