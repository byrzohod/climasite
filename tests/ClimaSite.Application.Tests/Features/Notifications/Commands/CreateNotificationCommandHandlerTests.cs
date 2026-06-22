using ClimaSite.Application.Features.Notifications.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Notifications.Commands;

public class CreateNotificationCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private CreateNotificationCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_PersistsNotification_AndReturnsId()
    {
        var userId = Guid.NewGuid();
        var command = new CreateNotificationCommand
        {
            UserId = userId,
            Type = NotificationTypes.OrderPlaced,
            Title = "Order placed",
            Message = "Your order has been placed."
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var stored = await _context.Notifications.ToListAsync();
        stored.Should().ContainSingle(n =>
            n.Id == result.Value
            && n.UserId == userId
            && n.Type == NotificationTypes.OrderPlaced
            && n.Title == "Order placed"
            && n.Message == "Your order has been placed.");
    }

    [Fact]
    public async Task Handle_WithLink_SetsLink()
    {
        var command = new CreateNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationTypes.OrderShipped,
            Title = "Shipped",
            Message = "Your order shipped.",
            Link = "/account/orders/123"
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = await _context.Notifications.ToListAsync();
        stored.Single().Link.Should().Be("/account/orders/123");
    }

    [Fact]
    public async Task Handle_WithData_SetsData()
    {
        var data = new Dictionary<string, object> { ["orderId"] = "ORD-1" };
        var command = new CreateNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationTypes.OrderPlaced,
            Title = "Title",
            Message = "Message",
            Data = data
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = (await _context.Notifications.ToListAsync()).Single();
        stored.Data.Should().ContainKey("orderId");
        stored.Data["orderId"].Should().Be("ORD-1");
    }

    [Fact]
    public async Task Handle_WithoutLinkOrData_LeavesLinkNullAndDataEmpty()
    {
        var command = new CreateNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationTypes.OrderPlaced,
            Title = "Title",
            Message = "Message"
        };

        await CreateHandler().Handle(command, CancellationToken.None);

        var stored = (await _context.Notifications.ToListAsync()).Single();
        stored.Link.Should().BeNull();
        stored.Data.Should().BeEmpty();
    }
}
