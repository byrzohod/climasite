using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Notifications.DTOs;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the authenticated in-app notifications surface
/// (<c>/api/notifications</c>): real HTTP -> MediatR handler -> Postgres.
/// Notifications are produced server-side, so tests seed them directly into the
/// database for the authenticated user, then exercise list/summary/mark-read/delete.
/// Covers the happy path, 401 for anonymous callers, ownership isolation and not-found.
/// </summary>
public class NotificationsControllerTests : IntegrationTestBase
{
    public NotificationsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Auth

    [Fact]
    public async Task GetNotifications_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/notifications/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAsRead_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteNotification_Returns401_WhenUnauthenticated()
    {
        var response = await Client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Fact]
    public async Task GetNotifications_ReturnsOnlyTheCurrentUsersNotifications()
    {
        var email = $"notif-list-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);

        // A second user, created directly so we never disturb the active bearer token.
        var otherUserId = await CreateUserAsync($"notif-other-{Guid.NewGuid():N}@example.com");

        await SeedNotificationsAsync(userId, ("order_placed", "Order placed", false));
        await SeedNotificationsAsync(otherUserId, ("promotional", "Other promo", false));

        var response = await Client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<NotificationsListDto>();
        list.Should().NotBeNull();
        list!.TotalCount.Should().Be(1);
        list.Items.Should().ContainSingle();
        list.Items[0].Title.Should().Be("Order placed");
        list.Items.Should().NotContain(n => n.Title == "Other promo");
    }

    [Fact]
    public async Task GetNotifications_FiltersByReadStatus()
    {
        var email = $"notif-filter-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);

        await SeedNotificationsAsync(userId,
            ("order_placed", "Unread one", false),
            ("order_shipped", "Read one", true));

        var unreadResponse = await Client.GetAsync("/api/notifications?isRead=false");

        unreadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unread = await unreadResponse.Content.ReadFromJsonAsync<NotificationsListDto>();
        unread!.Items.Should().ContainSingle(n => n.Title == "Unread one");
        unread.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummary_ReturnsCountsAndRecentItems()
    {
        var email = $"notif-summary-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);

        await SeedNotificationsAsync(userId,
            ("order_placed", "Summary A", false),
            ("order_shipped", "Summary B", true),
            ("promotional", "Summary C", false));

        var response = await Client.GetAsync("/api/notifications/summary?recentCount=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<NotificationSummaryDto>();
        summary.Should().NotBeNull();
        summary!.TotalCount.Should().Be(3);
        summary.UnreadCount.Should().Be(2);
        summary.RecentItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkAsRead_MarksOwnedNotificationRead()
    {
        var email = $"notif-read-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);
        var ids = await SeedNotificationsAsync(userId, ("order_placed", "To be read", false));

        var response = await Client.PutAsync($"/api/notifications/{ids[0]}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await Client.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationsListDto>();
        list!.UnreadCount.Should().Be(0);
        list.Items.Single().IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsRead_ClearsUnreadCount()
    {
        var email = $"notif-readall-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);
        await SeedNotificationsAsync(userId,
            ("order_placed", "First", false),
            ("order_shipped", "Second", false));

        var response = await Client.PutAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllResponse>();
        body!.MarkedCount.Should().Be(2);

        var listResponse = await Client.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationsListDto>();
        list!.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteNotification_RemovesOwnedNotification()
    {
        var email = $"notif-delete-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);
        var ids = await SeedNotificationsAsync(userId, ("order_placed", "Delete me", false));

        var response = await Client.DeleteAsync($"/api/notifications/{ids[0]}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await Client.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationsListDto>();
        list!.TotalCount.Should().Be(0);
    }

    #endregion

    #region Ownership + not found

    [Fact]
    public async Task MarkAsRead_Returns400_WhenNotificationBelongsToAnotherUser()
    {
        var ownerEmail = $"notif-owner-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(ownerEmail);
        var ownerId = await GetUserIdAsync(ownerEmail);
        var ids = await SeedNotificationsAsync(ownerId, ("order_placed", "Owner only", false));

        await AuthenticateAsync($"notif-attacker-{Guid.NewGuid():N}@example.com");

        var response = await Client.PutAsync($"/api/notifications/{ids[0]}/read", null);

        // Handler scopes by UserId; a non-owner gets a failure Result -> BadRequest.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteNotification_Returns400_WhenNotificationBelongsToAnotherUser()
    {
        var ownerEmail = $"notif-owner2-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(ownerEmail);
        var ownerId = await GetUserIdAsync(ownerEmail);
        var ids = await SeedNotificationsAsync(ownerId, ("order_placed", "Owner only 2", false));

        await AuthenticateAsync($"notif-attacker2-{Guid.NewGuid():N}@example.com");

        var response = await Client.DeleteAsync($"/api/notifications/{ids[0]}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MarkAsRead_Returns400_ForUnknownId()
    {
        await AuthenticateAsync($"notif-missing-{Guid.NewGuid():N}@example.com");

        var response = await Client.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteNotification_Returns400_ForUnknownId()
    {
        await AuthenticateAsync($"notif-delete-missing-{Guid.NewGuid():N}@example.com");

        var response = await Client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private async Task<Guid> CreateUserAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Other",
            LastName = "User"
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        result.Succeeded.Should().BeTrue();
        return user.Id;
    }

    private async Task<List<Guid>> SeedNotificationsAsync(
        Guid userId,
        params (string Type, string Title, bool Read)[] notifications)
    {
        var ids = new List<Guid>();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var (type, title, read) in notifications)
        {
            var notification = new Notification(userId, type, title, $"{title} body");
            if (read)
            {
                notification.MarkAsRead();
            }

            db.Notifications.Add(notification);
            ids.Add(notification.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private record MarkAllResponse(int MarkedCount);
}
