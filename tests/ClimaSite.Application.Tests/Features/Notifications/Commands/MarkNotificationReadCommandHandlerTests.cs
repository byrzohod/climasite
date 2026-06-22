using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Notifications.Commands;

public class MarkNotificationReadCommandHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private MarkNotificationReadCommandHandler CreateHandler() =>
        new(_context, _currentUser.Object);

    private Notification SeedNotification(Guid userId)
    {
        var notification = new Notification(userId, NotificationTypes.OrderPlaced, "Title", "Message");
        _context.Notifications.Add(notification);
        return notification;
    }

    [Fact]
    public async Task Handle_MarksOwnedNotificationRead()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);
        var notification = SeedNotification(userId);

        var result = await CreateHandler().Handle(
            new MarkNotificationReadCommand { NotificationId = notification.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new MarkNotificationReadCommand { NotificationId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User must be authenticated");
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new MarkNotificationReadCommand { NotificationId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found");
    }

    [Fact]
    public async Task Handle_WhenNotificationBelongsToAnotherUser_ReturnsFailure()
    {
        var ownerId = Guid.NewGuid();
        var notification = SeedNotification(ownerId);
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new MarkNotificationReadCommand { NotificationId = notification.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found");
        notification.IsRead.Should().BeFalse();
    }
}
