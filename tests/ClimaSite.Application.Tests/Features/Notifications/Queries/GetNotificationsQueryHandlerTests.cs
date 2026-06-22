using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Notifications.Queries;

// These exercise the GetNotificationsQuery handler's paging/filter pipeline, which builds a query via
// _context.Notifications.Where(...).AsQueryable() and then runs CountAsync()/ToListAsync() over it.
// That .AsQueryable() chain is the path the enhanced MockDbContext now keeps async-capable.
public class GetNotificationsQueryHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetNotificationsQueryHandler CreateHandler() =>
        new(_context, _currentUser.Object);

    private Notification SeedNotification(
        Guid userId,
        bool read = false,
        string type = NotificationTypes.OrderPlaced,
        string title = "Title",
        DateTime? createdAt = null)
    {
        var notification = new Notification(userId, type, title, "Message");
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

    private static void SetCreatedAt(Notification notification, DateTime createdAt) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!.SetValue(notification, createdAt);

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsEmptyList()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        SeedNotification(Guid.NewGuid());

        var result = await CreateHandler().Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyReturnsNotificationsForCurrentUser()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, title: "Mine A");
        SeedNotification(userId, title: "Mine B");
        SeedNotification(Guid.NewGuid(), title: "Someone else"); // must be excluded

        var result = await CreateHandler().Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(n => n.Title == "Mine A" || n.Title == "Mine B");
    }

    [Fact]
    public async Task Handle_ComputesUnreadCountIndependentlyOfFilters()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, read: false);
        SeedNotification(userId, read: false);
        SeedNotification(userId, read: true);

        // Filter the list to only read items, but UnreadCount must still reflect the full inbox.
        var result = await CreateHandler().Handle(
            new GetNotificationsQuery { IsRead = true }, CancellationToken.None);

        result.TotalCount.Should().Be(1, "only one read notification matches the filter");
        result.UnreadCount.Should().Be(2, "unread count ignores the IsRead filter");
    }

    [Fact]
    public async Task Handle_FiltersByReadStatus()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, read: true, title: "Read one");
        SeedNotification(userId, read: false, title: "Unread one");
        SeedNotification(userId, read: false, title: "Unread two");

        var result = await CreateHandler().Handle(
            new GetNotificationsQuery { IsRead = false }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact]
    public async Task Handle_FiltersByType()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, type: NotificationTypes.OrderShipped, title: "Shipped");
        SeedNotification(userId, type: NotificationTypes.OrderPlaced, title: "Placed");

        var result = await CreateHandler().Handle(
            new GetNotificationsQuery { Type = NotificationTypes.OrderShipped }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(n => n.Type == NotificationTypes.OrderShipped);
    }

    [Fact]
    public async Task Handle_IgnoresWhitespaceTypeFilter()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, type: NotificationTypes.OrderShipped);
        SeedNotification(userId, type: NotificationTypes.OrderPlaced);

        var result = await CreateHandler().Handle(
            new GetNotificationsQuery { Type = "   " }, CancellationToken.None);

        result.TotalCount.Should().Be(2, "a whitespace-only type filter must be ignored");
    }

    [Fact]
    public async Task Handle_OrdersByCreatedAtDescending()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        SeedNotification(userId, title: "Oldest", createdAt: DateTime.UtcNow.AddDays(-3));
        SeedNotification(userId, title: "Middle", createdAt: DateTime.UtcNow.AddDays(-2));
        SeedNotification(userId, title: "Newest", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Items.Select(n => n.Title).Should().ContainInOrder("Newest", "Middle", "Oldest");
    }

    [Fact]
    public async Task Handle_AppliesPaging()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        for (var i = 0; i < 5; i++)
        {
            SeedNotification(userId, title: $"N{i}", createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var result = await CreateHandler().Handle(
            new GetNotificationsQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.Should().Be(5, "TotalCount reflects the whole filtered set, not the page");
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Select(n => n.Title).Should().ContainInOrder("N2", "N3");
    }

    [Fact]
    public async Task Handle_MapsAllNotificationFieldsToDto()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.UserId).Returns(userId);

        var notification = new Notification(userId, NotificationTypes.OrderShipped, "Your order shipped", "It is on the way");
        notification.SetLink("/orders/123");
        notification.SetData(new Dictionary<string, object> { ["orderId"] = "123" });
        notification.MarkAsRead();
        _context.Notifications.Add(notification);

        var result = await CreateHandler().Handle(new GetNotificationsQuery(), CancellationToken.None);

        var dto = result.Items.Should().ContainSingle().Subject;
        dto.Id.Should().Be(notification.Id);
        dto.Type.Should().Be(NotificationTypes.OrderShipped);
        dto.Title.Should().Be("Your order shipped");
        dto.Message.Should().Be("It is on the way");
        dto.Link.Should().Be("/orders/123");
        dto.Data.Should().ContainKey("orderId");
        dto.IsRead.Should().BeTrue();
        dto.ReadAt.Should().NotBeNull();
    }
}
