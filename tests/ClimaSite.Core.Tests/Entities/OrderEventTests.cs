using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class OrderEventTests
{
    [Fact]
    public void Constructor_WithValidData_SetsFields()
    {
        var orderId = Guid.NewGuid();

        var orderEvent = new OrderEvent(orderId, OrderStatus.Paid, "Payment received", "via card");

        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.Status.Should().Be(OrderStatus.Paid);
        orderEvent.Description.Should().Be("Payment received");
        orderEvent.Notes.Should().Be("via card");
    }

    [Fact]
    public void Constructor_WithoutOptionalArgs_SetsNullDescriptionAndNotes()
    {
        var orderEvent = new OrderEvent(Guid.NewGuid(), OrderStatus.Pending);

        orderEvent.Description.Should().BeNull();
        orderEvent.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_FactoryMethod_BuildsEquivalentEvent()
    {
        var orderId = Guid.NewGuid();

        var orderEvent = OrderEvent.Create(orderId, OrderStatus.Shipped, "Shipped", "tracking added");

        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.Status.Should().Be(OrderStatus.Shipped);
        orderEvent.Description.Should().Be("Shipped");
        orderEvent.Notes.Should().Be("tracking added");
    }

    [Fact]
    public void Create_WithoutOptionalArgs_SetsNulls()
    {
        var orderEvent = OrderEvent.Create(Guid.NewGuid(), OrderStatus.Delivered);

        orderEvent.Description.Should().BeNull();
        orderEvent.Notes.Should().BeNull();
    }
}
