using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class UpdateShippingInfoCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private static Order CreateProcessingOrder(string email = "buyer@test.com", Guid? userId = null)
    {
        var order = new Order("ORD-TEST-0001", email);
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        return order;
    }

    private UpdateShippingInfoCommandHandler CreateHandler() =>
        new(_context, new EmailOutbox(_context));

    [Fact]
    public async Task MarkAsShipped_QueuesShippedEmail_WithTracking()
    {
        var order = CreateProcessingOrder();
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = order.Id,
            TrackingNumber = "TRACK-123",
            MarkAsShipped = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);

        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().ContainSingle(m =>
            m.Type == OutboxMessageTypes.OrderShipped && m.ToEmail == "buyer@test.com");
    }

    [Fact]
    public async Task MarkAsShipped_AuthenticatedOrder_CreatesShippedNotification_WithTracking()
    {
        var userId = Guid.NewGuid();
        var order = CreateProcessingOrder(userId: userId);
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = order.Id,
            TrackingNumber = "TRACK-123",
            MarkAsShipped = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notifications = await _context.Notifications.ToListAsync();
        notifications.Should().ContainSingle(n =>
            n.UserId == userId
            && n.Type == NotificationTypes.OrderShipped
            && n.Message.Contains("TRACK-123")
            && n.Link == $"/account/orders/{order.Id}");
    }

    [Fact]
    public async Task MarkAsShipped_GuestOrder_DoesNotCreateNotification()
    {
        var order = CreateProcessingOrder();
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = order.Id,
            TrackingNumber = "TRACK-123",
            MarkAsShipped = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Notifications.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTrackingOnly_DoesNotCreateNotification()
    {
        var order = CreateProcessingOrder(userId: Guid.NewGuid());
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = order.Id,
            TrackingNumber = "TRACK-123",
            MarkAsShipped = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Notifications.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTrackingOnly_DoesNotQueueShippedEmail()
    {
        var order = CreateProcessingOrder();
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = order.Id,
            TrackingNumber = "TRACK-123",
            MarkAsShipped = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing);
        (await _context.OutboxMessages.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task OrderNotFound_ReturnsFailure_AndQueuesNothing()
    {
        var result = await CreateHandler().Handle(new UpdateShippingInfoCommand
        {
            OrderId = Guid.NewGuid(),
            MarkAsShipped = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await _context.OutboxMessages.ToListAsync()).Should().BeEmpty();
    }
}
