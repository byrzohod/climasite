using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Features.Reservations;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateOrderStatusCommandHandler CreateHandler() =>
        new(_context, new EmailOutbox(_context), new StockReservationService(_context, new ReservationOptions()));

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

    private Order SeedPendingBankOrder()
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..14], "bank@test.com");
        order.SetPaymentMethod("bank");
        _context.AddOrder(order);
        return order;
    }

    private ProductVariant SeedVariant(int stock, int reserved = 0)
    {
        var variant = new ProductVariant(Guid.NewGuid(), $"SKU-{Guid.NewGuid():N}"[..12], "Default");
        variant.SetStockQuantity(stock);
        if (reserved > 0)
        {
            variant.SetReservedQuantity(reserved);
        }

        _context.AddProductVariant(variant);
        return variant;
    }

    [Fact]
    public async Task MarkBankTransferOrderPaid_ConsumesHold_DecrementsStock()
    {
        // INV-01 B: admin marking a bank order Paid consumes its Active hold (hold → sold) in the same unit of work.
        var variant = SeedVariant(stock: 10);
        var order = SeedPendingBankOrder();
        var reservations = new StockReservationService(_context, new ReservationOptions());
        await reservations.ReserveBankOrderAsync(order.Id, new[] { new ReservationRequestLine(variant.Id, 2, "AC") });
        variant.StockQuantity.Should().Be(10);
        variant.ReservedQuantity.Should().Be(2);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        variant.StockQuantity.Should().Be(8, "mark-paid physically sells the held units");
        variant.ReservedQuantity.Should().Be(0);
        (await _context.StockReservations.SingleAsync()).Status.Should().Be(ReservationStatus.Consumed);
    }

    [Fact]
    public async Task MarkBankTransferOrderPaid_HoldAlreadyExpired_ReturnsFailure_NoSale()
    {
        // The hold expired (swept ⇒ order should be auto-cancelled). Marking it Paid must be refused rather than
        // sell stock that is no longer held (which would oversell).
        var variant = SeedVariant(stock: 10);
        var order = SeedPendingBankOrder();
        var expiredHold = new StockReservation(variant.Id, null, 2, DateTime.UtcNow.AddMinutes(-5), ReservationKind.BankTransfer);
        expiredHold.SetOrderId(order.Id);
        expiredHold.SetStatus(ReservationStatus.Expired);
        _context.AddStockReservation(expiredHold);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Paid),
            NotifyCustomer = false
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("can no longer be marked as paid");
        variant.StockQuantity.Should().Be(10, "no hold to sell — stock is untouched");
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
