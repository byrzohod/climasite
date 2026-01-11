using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class OrderTests
{
    private Order CreateValidOrder(
        string orderNumber = "ORD-2026-001",
        string customerEmail = "customer@test.com") =>
        new(orderNumber, customerEmail);

    [Fact]
    public void Constructor_WithValidData_CreatesOrder()
    {
        // Arrange & Act
        var order = CreateValidOrder();

        // Assert
        order.OrderNumber.Should().Be("ORD-2026-001");
        order.CustomerEmail.Should().Be("customer@test.com");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetOrderNumber_WithEmptyValue_ThrowsArgumentException(string orderNumber)
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetOrderNumber(orderNumber);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Order number cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCustomerEmail_WithEmptyValue_ThrowsArgumentException(string email)
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetCustomerEmail(email);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer email cannot be empty*");
    }

    [Fact]
    public void SetCustomerEmail_NormalizesToLowerCase()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetCustomerEmail("CUSTOMER@TEST.COM");

        // Assert
        order.CustomerEmail.Should().Be("customer@test.com");
    }

    [Fact]
    public void SetShippingCost_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetShippingCost(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Shipping cost cannot be negative*");
    }

    [Fact]
    public void SetTaxAmount_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetTaxAmount(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Tax amount cannot be negative*");
    }

    [Fact]
    public void SetDiscountAmount_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetDiscountAmount(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Discount amount cannot be negative*");
    }

    [Fact]
    public void SetCurrency_WithEmptyValue_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetCurrency("");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Currency cannot be empty*");
    }

    [Fact]
    public void SetCurrency_NormalizesToUpperCase()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetCurrency("eur");

        // Assert
        order.Currency.Should().Be("EUR");
    }

    [Fact]
    public void SetShippingAddress_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetShippingAddress(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetStatus_FromPendingToPaid_UpdatesStatus()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetStatus(OrderStatus.Paid);

        // Assert
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_FromPendingToShipped_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        var act = () => order.SetStatus(OrderStatus.Shipped);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot transition order from Pending to Shipped*");
    }

    [Fact]
    public void SetStatus_FromPaidToProcessing_UpdatesStatus()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);

        // Act
        order.SetStatus(OrderStatus.Processing);

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void SetStatus_FromProcessingToShipped_SetsShippedAt()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);

        // Act
        order.SetStatus(OrderStatus.Shipped);

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_FromShippedToDelivered_SetsDeliveredAt()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetStatus(OrderStatus.Shipped);

        // Act
        order.SetStatus(OrderStatus.Delivered);

        // Assert
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_ToCancelled_SetsCancelledAt()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetStatus(OrderStatus.Cancelled);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_FromCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Cancelled);

        // Act & Assert
        var act = () => order.SetStatus(OrderStatus.Paid);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot transition order from Cancelled*");
    }

    [Fact]
    public void CanBeCancelled_WhenPending_ReturnsTrue()
    {
        // Arrange
        var order = CreateValidOrder();

        // Assert
        order.CanBeCancelled.Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_WhenPaid_ReturnsTrue()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);

        // Assert
        order.CanBeCancelled.Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_WhenShipped_ReturnsFalse()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetStatus(OrderStatus.Shipped);

        // Assert
        order.CanBeCancelled.Should().BeFalse();
    }

    [Fact]
    public void CanBeRefunded_WhenPaid_ReturnsTrue()
    {
        // Arrange
        var order = CreateValidOrder();
        order.SetStatus(OrderStatus.Paid);

        // Assert
        order.CanBeRefunded.Should().BeTrue();
    }

    [Fact]
    public void CanBeRefunded_WhenPending_ReturnsFalse()
    {
        // Arrange
        var order = CreateValidOrder();

        // Assert
        order.CanBeRefunded.Should().BeFalse();
    }

    [Fact]
    public void SetPaymentInfo_SetsPaymentIntentIdAndMethod()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetPaymentInfo("pi_123456", "card");

        // Assert
        order.PaymentIntentId.Should().Be("pi_123456");
        order.PaymentMethod.Should().Be("card");
    }

    [Fact]
    public void SetTrackingNumber_SetsTrackingNumber()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.SetTrackingNumber("TRACK123456");

        // Assert
        order.TrackingNumber.Should().Be("TRACK123456");
    }
}
