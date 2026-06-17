using ClimaSite.E2E.Infrastructure;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Account;

/// <summary>
/// GAP-09 E2E: an admin changes a registered customer's order status, which produces an in-app
/// notification. The customer then logs in and sees the notification bell badge and the
/// notification in the dropdown.
///
/// Real data only — the customer, product, and order are created via the API; the admin status
/// change is the real producer. Cleanup runs via the factory correlation id.
/// </summary>
[Collection("Playwright")]
public class NotificationsTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public NotificationsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _dataFactory.CleanupAsync();
        await _page.Context.CloseAsync();
    }

    [Fact]
    public async Task CustomerSeesNotificationBellBadge_AfterAdminChangesOrderStatus()
    {
        // Arrange: a registered customer with a real order.
        var customer = await _dataFactory.CreateUserAsync();
        var order = await _dataFactory.CreateOrderAsync(customer, productCount: 1);
        order.Id.Should().NotBe(Guid.Empty, "the seeded order must be created for the customer");

        // Act (producer): admin transitions the order Pending -> Paid, which emits an in-app
        // notification (PaymentReceived) for the order's authenticated user.
        await _dataFactory.UpdateOrderStatusAsync(order.Id, "Paid");

        // Act (consumer): log in as the customer in the browser.
        await LoginAsAsync(customer);

        // Assert: the notification bell renders with a badge, and the dropdown lists the item.
        var bell = _page.Locator("[data-testid='notification-bell']");
        await Assertions.Expect(bell).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });

        var badge = _page.Locator("[data-testid='notification-badge']");
        await Assertions.Expect(badge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });

        await bell.ClickAsync();

        var dropdown = _page.Locator("[data-testid='notification-dropdown']");
        await Assertions.Expect(dropdown).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        var items = _page.Locator("[data-testid='notification-item']");
        await Assertions.Expect(items.First).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        (await items.CountAsync()).Should().BeGreaterThan(0,
            "the customer should see the notification produced by the admin status change");
    }

    private async Task LoginAsAsync(TestUser user)
    {
        user.Token.Should().NotBeNullOrWhiteSpace("test users must include a real access token");

        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.EvaluateAsync("token => window.localStorage.setItem('climasite_token', token)", user.Token);
        await _page.ReloadAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
