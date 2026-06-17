using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateOrderStatusCommandHandler CreateHandler() =>
        new(_context, new EmailOutbox(_context));

    private Order SeedPendingOrder(string email = "buyer@test.com", Guid? userId = null)
    {
        var order = new Order("ORD-TEST-0002", email);
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task NotifyCustomer_True_QueuesStatusEmail()
    {
        var order = SeedPendingOrder();

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);

        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().ContainSingle(m =>
            m.Type == OutboxMessageTypes.Generic && m.ToEmail == "buyer@test.com");
    }

    [Fact]
    public async Task AuthenticatedOrder_CreatesInAppNotification_OnStatusChange()
    {
        var userId = Guid.NewGuid();
        var order = SeedPendingOrder(userId: userId);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notifications = await _context.Notifications.ToListAsync();
        notifications.Should().ContainSingle(n =>
            n.UserId == userId
            && n.Type == NotificationTypes.PaymentReceived
            && n.Link == $"/account/orders/{order.Id}");
    }

    [Theory]
    [InlineData(OrderStatus.Paid, NotificationTypes.PaymentReceived)]
    [InlineData(OrderStatus.Cancelled, NotificationTypes.OrderCancelled)]
    public async Task AuthenticatedOrder_MapsStatusToNotificationType(
        OrderStatus status, string expectedType)
    {
        var userId = Guid.NewGuid();
        var order = SeedPendingOrder(userId: userId);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = status.ToString(),
            NotifyCustomer = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notifications = await _context.Notifications.ToListAsync();
        notifications.Should().ContainSingle(n => n.UserId == userId && n.Type == expectedType);
    }

    [Fact]
    public async Task GuestOrder_DoesNotCreateInAppNotification()
    {
        // Guest order: null UserId -> no in-app notification (no inbox to read it).
        var order = SeedPendingOrder();

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Notifications.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyCustomer_False_QueuesNothing()
    {
        var order = SeedPendingOrder();

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.OutboxMessages.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task OrderNotFound_ReturnsFailure_AndQueuesNothing()
    {
        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = Guid.NewGuid(),
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await _context.OutboxMessages.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidStatus_ReturnsFailure()
    {
        var order = SeedPendingOrder();

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = "NotARealStatus",
            NotifyCustomer = true
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
