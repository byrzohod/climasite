using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Notifications.Commands;

public class MarkAllNotificationsReadCommandHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private MarkAllNotificationsReadCommandHandler CreateHandler() =>
        new(_context, _currentUser.Object);

    private Notification SeedNotification(Guid userId, bool read = false)
    {
        var notification = new Notification(userId, NotificationTypes.OrderPlaced, "Title", "Message");
        if (read)
        {
            notification.MarkAsRead();
        }
        _context.Notifications.Add(notification);
        return notification;
    }

    [Fact]
    public async Task Handle_MarksAllUnreadForUser_AndReturnsCount()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        var unread1 = SeedNotification(userId);
        var unread2 = SeedNotification(userId);
        SeedNotification(userId, read: true); // already read, not counted

        var result = await CreateHandler().Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        unread1.IsRead.Should().BeTrue();
        unread2.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DoesNotMarkOtherUsersNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId);
        var otherUsers = SeedNotification(otherUserId);

        var result = await CreateHandler().Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        otherUsers.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoUnread_ReturnsZero()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);
        SeedNotification(userId, read: true);

        var result = await CreateHandler().Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User must be authenticated");
    }
}
