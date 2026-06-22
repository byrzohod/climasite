using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Notifications.Queries;

public class GetNotificationSummaryQueryHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetNotificationSummaryQueryHandler CreateHandler() =>
        new(_context, _currentUser.Object);

    private Notification SeedNotification(
        Guid userId, bool read = false, string title = "Title", DateTime? createdAt = null)
    {
        var notification = new Notification(userId, NotificationTypes.OrderPlaced, title, "Message");
        if (read)
        {
            notification.MarkAsRead();
        }
        if (createdAt.HasValue)
        {
            SetCreatedAt(notification, createdAt.Value);
        }
        _context.Notifications.Add(notification);
        return notification;
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsEmptySummary()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetNotificationSummaryQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.UnreadCount.Should().Be(0);
        result.RecentItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsTotalAndUnreadCountsForUser()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId);
        SeedNotification(userId);
        SeedNotification(userId, read: true);
        SeedNotification(Guid.NewGuid()); // another user's notification, ignored

        var result = await CreateHandler().Handle(new GetNotificationSummaryQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsRecentItemsNewestFirst()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, title: "Oldest", createdAt: DateTime.UtcNow.AddDays(-3));
        SeedNotification(userId, title: "Middle", createdAt: DateTime.UtcNow.AddDays(-2));
        SeedNotification(userId, title: "Newest", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(new GetNotificationSummaryQuery(), CancellationToken.None);

        result.RecentItems.Should().HaveCount(3);
        result.RecentItems[0].Title.Should().Be("Newest");
        result.RecentItems[2].Title.Should().Be("Oldest");
    }

    [Fact]
    public async Task Handle_LimitsRecentItemsToRecentCount()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        for (var i = 0; i < 8; i++)
        {
            SeedNotification(userId, createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var result = await CreateHandler().Handle(
            new GetNotificationSummaryQuery { RecentCount = 3 }, CancellationToken.None);

        result.TotalCount.Should().Be(8);
        result.RecentItems.Should().HaveCount(3);
    }

    private static void SetCreatedAt(Notification notification, DateTime createdAt)
    {
        var property = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt));
        property!.SetValue(notification, createdAt);
    }
}
