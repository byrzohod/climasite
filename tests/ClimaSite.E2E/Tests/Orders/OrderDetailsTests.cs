using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Orders;

/// <summary>
/// E2E tests for the Order Details page (ORD-028)
/// </summary>
[Collection("Playwright")]
public class OrderDetailsTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public OrderDetailsTests(PlaywrightFixture fixture)
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
    public async Task OrderDetails_DisplaysOrderNumber()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert
        var orderNumber = await ordersPage.GetOrderNumberFromDetailsAsync();
        orderNumber.Should().NotBeNullOrEmpty("Order number should be displayed");
    }

    [Fact]
    public async Task OrderDetails_DisplaysOrderStatus()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert
        var status = await ordersPage.GetOrderStatusAsync();
        status.Should().NotBeNullOrEmpty("Order status should be displayed");
    }

    [Fact]
    public async Task OrderDetails_DisplaysOrderTotal()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert
        var total = await ordersPage.GetOrderTotalAsync();
        total.Should().BeGreaterThan(0, "Order total should be greater than zero");
    }

    [Fact]
    public async Task OrderDetails_DisplaysOrderItems()
    {
        // Arrange - Create order with 2 products
        var order = await _dataFactory.CreateOrderAsync(productCount: 2);

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert
        var itemCount = await ordersPage.GetOrderItemCountAsync();
        itemCount.Should().BeGreaterThanOrEqualTo(1, "Order should have at least one item");
    }

    [Fact]
    public async Task OrderDetails_PendingOrder_ShowsCancelButton()
    {
        // Arrange - New order should be in Pending status
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert
        var canCancel = await ordersPage.CanCancelOrderAsync();
        canCancel.Should().BeTrue("Pending orders should be cancellable");
    }

    [Fact]
    public async Task OrderDetails_DisplaysShippingAddress()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Check for shipping address section
        var addressElement = await _page.QuerySelectorAsync("[data-testid='shipping-address']");
        addressElement.Should().NotBeNull("Shipping address should be displayed");
    }

    [Fact]
    public async Task OrderDetails_DisplaysPaymentMethod()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Check for payment method section
        var paymentElement = await _page.QuerySelectorAsync("[data-testid='payment-method']");
        paymentElement.Should().NotBeNull("Payment method should be displayed");
    }

    [Fact]
    public async Task OrderDetails_OtherUserOrder_AccessDenied()
    {
        // Arrange - Create an order for one user
        var order = await _dataFactory.CreateOrderAsync();

        // Create and login as a different user
        var otherUser = await _dataFactory.CreateUserAsync();
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(otherUser.Email, otherUser.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Assert - Should show error or redirect
        // The page should not show the order details for a different user
        var currentUrl = _page.Url;
        var hasError = await _page.QuerySelectorAsync("[data-testid='access-denied'], [data-testid='error-message']");

        // Either redirected away or shows error
        (currentUrl.Contains("/account/orders/") == false || hasError != null).Should().BeTrue(
            "Should deny access to another user's order");
    }

    [Fact]
    public async Task OrderDetails_InvalidOrderId_ShowsNotFound()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var ordersPage = new OrdersPage(_page);

        // Act - Navigate to a non-existent order
        await ordersPage.NavigateToOrderDetailsAsync(Guid.NewGuid().ToString());

        // Assert - Should show not found or error
        var errorElement = await _page.QuerySelectorAsync("[data-testid='not-found'], [data-testid='error-message']");
        (errorElement != null || _page.Url.Contains("orders") == false).Should().BeTrue(
            "Should show not found for invalid order ID");
    }

    [Fact]
    public async Task OrderDetails_BackToOrders_NavigatesCorrectly()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateToOrderDetailsAsync(order.Id.ToString());

        // Act - Click back button and wait for navigation
        var backButton = await _page.WaitForSelectorAsync("[data-testid='back-to-orders']", new PageWaitForSelectorOptions { Timeout = 5000 });
        backButton.Should().NotBeNull("Back button should exist");

        await backButton!.ClickAsync();

        // Wait for URL to change to orders list (without order ID)
        try
        {
            await _page.WaitForURLAsync(url => url.Contains("/account/orders") && !url.Contains(order.Id.ToString()),
                new PageWaitForURLOptions { Timeout = 10000 });
        }
        catch
        {
            // Fallback - wait and check
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(500);
        }

        // Assert
        var currentUrl = _page.Url;
        currentUrl.Should().Contain("/account/orders");
        currentUrl.Should().NotContain(order.Id.ToString());
    }
}
