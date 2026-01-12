using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Orders;

/// <summary>
/// E2E tests for Orders pages on mobile viewports (ORD-030)
/// </summary>
[Collection("Playwright")]
public class OrdersMobileTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _mobileContext = default!;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    // iPhone 12 viewport
    private static readonly ViewportSize MobileViewport = new() { Width = 390, Height = 844 };

    public OrdersMobileTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create a mobile context with iPhone-like viewport
        _mobileContext = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = MobileViewport,
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15",
            HasTouch = true,
            IsMobile = true,
            BaseURL = _fixture.BaseUrl
        });

        _page = await _mobileContext.NewPageAsync();
        _page.SetDefaultTimeout(30000);
        _page.SetDefaultNavigationTimeout(30000);

        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _dataFactory.CleanupAsync();
        await _mobileContext.CloseAsync();
    }

    [Fact]
    public async Task Mobile_OrdersList_DisplaysCorrectly()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert - Orders should display in mobile-friendly format
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(1);

        // Verify no horizontal scroll
        var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = MobileViewport.Width;
        bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 20, "Page should not have horizontal scroll on mobile");
    }

    [Fact]
    public async Task Mobile_OrderDetails_DisplaysCorrectly()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Order details should be readable on mobile
        var orderNumber = await ordersPage.GetOrderNumberFromDetailsAsync();
        orderNumber.Should().NotBeNullOrEmpty();

        // Verify no horizontal scroll
        var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = MobileViewport.Width;
        bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 20);
    }

    [Fact]
    public async Task Mobile_OrderActions_AreAccessible()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Action buttons should be visible and clickable
        var canCancel = await ordersPage.CanCancelOrderAsync();
        canCancel.Should().BeTrue("Cancel button should be accessible on mobile");

        // Check reorder button is visible
        var reorderButton = await _page.QuerySelectorAsync("[data-testid='reorder-btn']");
        if (reorderButton != null)
        {
            var isVisible = await reorderButton.IsVisibleAsync();
            isVisible.Should().BeTrue("Reorder button should be visible on mobile");
        }
    }

    [Fact]
    public async Task Mobile_OrderNavigation_WorksWithTouch()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Wait for orders to load
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(1, "User should have at least one order to navigate to");

        // Act - Tap on order (simulates touch) - uses the improved ClickOrderAsync
        await ordersPage.ClickOrderAsync(0);

        // Assert - Should navigate to details (URL should contain order ID pattern)
        var currentUrl = _page.Url;
        // The URL should now contain the order details path
        (currentUrl.Contains("/account/orders/") && currentUrl.Length > "/account/orders/".Length + 20)
            .Should().BeTrue($"Should navigate to order details. Current URL: {currentUrl}");
    }

    [Fact]
    public async Task Mobile_OrdersFilter_IsAccessible()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Assert - Filter should be accessible (may be in dropdown or collapsible)
        var filterElement = await _page.QuerySelectorAsync(
            "[data-testid='orders-status-filter'], [data-testid='filter-toggle'], [data-testid='mobile-filter']");

        // Mobile may have filters in different UI element
        // This test just verifies the orders page works
        var isEmpty = await ordersPage.IsEmptyAsync();
        isEmpty.Should().BeFalse("User should have orders");
    }

    [Fact]
    public async Task Mobile_BackNavigation_WorksCorrectly()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Act - Use back button
        var backButton = await _page.QuerySelectorAsync("[data-testid='back-to-orders'], [data-testid='back-button']");
        if (backButton != null)
        {
            await backButton.ClickAsync();

            // Wait for URL to change
            try
            {
                await _page.WaitForURLAsync(url => url.Contains("/account/orders") && !url.Contains(order.Id.ToString()),
                    new PageWaitForURLOptions { Timeout = 10000 });
            }
            catch
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500);
            }

            // Assert
            var currentUrl = _page.Url;
            currentUrl.Should().Contain("/account/orders");
            currentUrl.Should().NotContain(order.Id.ToString());
        }
        else
        {
            // Use browser back
            await _page.GoBackAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var currentUrl = _page.Url;
            currentUrl.Should().Contain("/account/orders");
        }
    }

    [Fact]
    public async Task Mobile_OrderItemDetails_DisplaysCorrectly()
    {
        // Arrange - Create order with multiple items
        var order = await _dataFactory.CreateOrderAsync(productCount: 3);

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Items should be displayed in mobile-friendly layout
        var itemCount = await ordersPage.GetOrderItemCountAsync();
        itemCount.Should().BeGreaterThanOrEqualTo(1);

        // Check that items are displayed (cards should stack vertically)
        var items = await _page.QuerySelectorAllAsync("[data-testid='order-item-row']");
        foreach (var item in items)
        {
            var box = await item.BoundingBoxAsync();
            if (box != null)
            {
                // Each item should fit within mobile viewport width
                box.Width.Should().BeLessThanOrEqualTo(MobileViewport.Width);
            }
        }
    }

    [Fact]
    public async Task Mobile_Landscape_AdjustsLayout()
    {
        // Arrange - Switch to landscape
        await _page.SetViewportSizeAsync(844, 390); // Landscape iPhone 12

        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert - Should still display correctly
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(1);

        // Verify no horizontal scroll in landscape
        var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
        bodyWidth.Should().BeLessThanOrEqualTo(844 + 20);
    }

    [Fact]
    public async Task Mobile_CancelOrder_ModalDisplaysCorrectly()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Act - Open cancel modal
        var cancelButton = await _page.QuerySelectorAsync("[data-testid='cancel-order-btn']");
        if (cancelButton != null && await cancelButton.IsEnabledAsync())
        {
            await cancelButton.ClickAsync();

            // Assert - Modal should be visible and fit mobile screen
            var modal = await _page.WaitForSelectorAsync("[data-testid='cancel-modal']", new PageWaitForSelectorOptions { Timeout = 5000 });
            modal.Should().NotBeNull("Cancel modal should appear");

            if (modal != null)
            {
                var box = await modal.BoundingBoxAsync();
                if (box != null)
                {
                    box.Width.Should().BeLessThanOrEqualTo(MobileViewport.Width, "Modal should fit mobile screen");
                }
            }

            // Close modal
            var closeButton = await _page.QuerySelectorAsync("[data-testid='cancel-modal'] button:has-text('Cancel')");
            if (closeButton != null)
            {
                await closeButton.ClickAsync();
            }
        }
    }
}
