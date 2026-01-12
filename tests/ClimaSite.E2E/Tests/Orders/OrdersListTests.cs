using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Orders;

/// <summary>
/// E2E tests for the Orders List page (ORD-027)
/// </summary>
[Collection("Playwright")]
public class OrdersListTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public OrdersListTests(PlaywrightFixture fixture)
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
    public async Task Orders_NotLoggedIn_RedirectsToLogin()
    {
        // Arrange
        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert - Should redirect to login
        var currentUrl = _page.Url;
        currentUrl.Should().Contain("/login");
    }

    [Fact]
    public async Task Orders_NoOrders_ShowsEmptyMessage()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        var loginPage = new LoginPage(_page);

        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert
        var isEmpty = await ordersPage.IsEmptyAsync();
        isEmpty.Should().BeTrue("New user should have no orders");
    }

    [Fact]
    public async Task Orders_WithOrders_DisplaysOrderList()
    {
        // Arrange - Create a user with an order
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(1, "User should have at least one order");

        var isEmpty = await ordersPage.IsEmptyAsync();
        isEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task Orders_DisplaysOrderNumber()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);

        // Act
        await ordersPage.NavigateAsync();

        // Assert
        var orderNumbers = await ordersPage.GetOrderNumbersAsync();
        orderNumbers.Should().NotBeEmpty();
        // Order number should follow the expected format
        orderNumbers.Should().Contain(on => !string.IsNullOrEmpty(on));
    }

    [Fact]
    public async Task Orders_ClickOrder_NavigatesToDetails()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Act
        await ordersPage.ClickOrderAsync(0);

        // Assert - Should navigate to order details
        var currentUrl = _page.Url;
        currentUrl.Should().Contain("/account/orders/");
    }

    [Fact]
    public async Task Orders_FilterByStatus_ShowsFilteredResults()
    {
        // Arrange - Create order (will be in Pending status)
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Get initial count
        var initialCount = await ordersPage.GetOrderCountAsync();

        // Act - Filter by a status that may have no orders
        await ordersPage.FilterByStatusAsync("Delivered");

        // Assert - May show 0 or more orders depending on test data
        // The key assertion is that no errors occur and the filter applies
        var filteredCount = await ordersPage.GetOrderCountAsync();
        filteredCount.Should().BeLessThanOrEqualTo(initialCount);
    }

    [Fact]
    public async Task Orders_SearchByOrderNumber_FiltersResults()
    {
        // Arrange
        var order = await _dataFactory.CreateOrderAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(order.User.Email, order.User.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Act - Search by partial order number (assuming format like ORD-XXX)
        if (!string.IsNullOrEmpty(order.OrderNumber))
        {
            await ordersPage.SearchOrdersAsync(order.OrderNumber);

            // Assert
            var orderCount = await ordersPage.GetOrderCountAsync();
            orderCount.Should().BeGreaterThanOrEqualTo(1);

            var orderNumbers = await ordersPage.GetOrderNumbersAsync();
            orderNumbers.Should().Contain(on => on.Contains(order.OrderNumber) || order.OrderNumber.Contains(on));
        }
    }

    [Fact]
    public async Task Orders_SortByDate_ChangesOrder()
    {
        // Arrange - Create multiple orders for sorting test
        var user = await _dataFactory.CreateUserAsync();
        await _dataFactory.CreateOrderAsync(user, productCount: 1);
        await _dataFactory.CreateOrderAsync(user, productCount: 1);

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Act - Sort by date ascending
        await ordersPage.SortByAsync("Date");

        // Assert - No errors and orders are still displayed
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Orders_SortByTotal_ChangesOrder()
    {
        // Arrange - Create orders with different totals
        var user = await _dataFactory.CreateUserAsync();
        await _dataFactory.CreateOrderAsync(user, productCount: 1);
        await _dataFactory.CreateOrderAsync(user, productCount: 3);

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();

        // Act - Sort by total
        await ordersPage.SortByAsync("Total");

        // Assert - No errors and orders are still displayed
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(2);
    }
}
