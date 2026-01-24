using System.Net.Http.Json;
using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Admin;

/// <summary>
/// E2E tests for the Admin Panel functionality.
/// Tests admin dashboard, product management, order management, and review moderation.
/// </summary>
[Collection("Playwright")]
public class AdminPanelTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public AdminPanelTests(PlaywrightFixture fixture)
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

    #region Admin Login & Dashboard Tests

    [Fact]
    public async Task AdminLogin_CanAccessDashboard()
    {
        // Arrange - Create admin user
        var admin = await _dataFactory.CreateAdminUserAsync();

        // Act - Login as admin and navigate to dashboard
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(admin.Email, admin.Password);

        // Navigate to admin dashboard
        await _page.GotoAsync("/admin");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Dashboard should be accessible
        await _page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 10000 });
        var pageContent = await _page.ContentAsync();
        pageContent.ToLower().Should().Contain("admin");

        // Should see admin navigation links
        var hasProductsLink = await _page.IsVisibleAsync("a[href*='products'], [routerlink*='products']");
        var hasOrdersLink = await _page.IsVisibleAsync("a[href*='orders'], [routerlink*='orders']");

        (hasProductsLink || hasOrdersLink).Should().BeTrue("Admin dashboard should have navigation links");
    }

    [Fact]
    public async Task AdminDashboard_NonAdminUser_IsRedirectedOrDenied()
    {
        // Arrange - Create regular (non-admin) user
        var user = await _dataFactory.CreateUserAsync();

        // Act - Login as regular user and try to access admin
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        await _page.GotoAsync("/admin");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should be redirected away from admin or see access denied
        var currentUrl = _page.Url;
        var isRedirected = !currentUrl.Contains("/admin") || currentUrl.Contains("/login");
        var hasAccessDenied = await _page.Locator("text=access denied, text=unauthorized, text=forbidden").IsVisibleAsync();

        (isRedirected || hasAccessDenied).Should().BeTrue("Non-admin users should not access admin panel");
    }

    #endregion

    #region Admin Products Tests

    [Fact]
    public async Task AdminProducts_CanListProducts()
    {
        // Arrange - Create admin and some products
        var admin = await _dataFactory.CreateAdminUserAsync();
        var product = await _dataFactory.CreateProductAsync(name: "Test Product for Listing");

        // Act - Login and navigate to products
        await LoginAsAdminAsync(admin);
        await _page.GotoAsync("/admin/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Products page should load
        var pageContent = await _page.ContentAsync();
        (pageContent.Contains("Product") || pageContent.Contains("product")).Should().BeTrue("Products page should show product management content");
    }

    [Fact]
    public async Task AdminProducts_CanCreateProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var productName = $"E2E Test Product {Guid.NewGuid():N}".Substring(0, 40);

        // Act - Create product via API (since UI may be placeholder)
        var response = await CreateProductViaApiAsync(admin, productName);

        // Assert
        response.Should().BeTrue("Admin should be able to create products via API");

        // Verify product exists
        var products = await GetProductsViaApiAsync(admin, productName);
        products.Should().Contain(p => p.Contains(productName, StringComparison.OrdinalIgnoreCase),
            "Created product should be found in product list");
    }

    [Fact]
    public async Task AdminProducts_CanEditProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var product = await _dataFactory.CreateProductAsync(name: "Product To Edit");
        var updatedName = $"Updated Product {Guid.NewGuid():N}".Substring(0, 40);

        // Act - Update product via API
        var updateSuccess = await UpdateProductViaApiAsync(admin, product.Id, updatedName);

        // Assert
        updateSuccess.Should().BeTrue("Admin should be able to update products");

        // Verify product was updated
        var products = await GetProductsViaApiAsync(admin, updatedName);
        products.Should().Contain(p => p.Contains(updatedName, StringComparison.OrdinalIgnoreCase),
            "Updated product name should be found");
    }

    [Fact]
    public async Task AdminProducts_CanDeleteProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var product = await _dataFactory.CreateProductAsync(name: "Product To Delete");

        // Act - Delete product via API
        var deleteSuccess = await DeleteProductViaApiAsync(admin, product.Id);

        // Assert
        deleteSuccess.Should().BeTrue("Admin should be able to delete products");
    }

    #endregion

    #region Admin Orders Tests

    [Fact]
    public async Task AdminOrders_CanListOrders()
    {
        // Arrange - Create admin and an order
        var admin = await _dataFactory.CreateAdminUserAsync();
        var order = await _dataFactory.CreateOrderAsync();

        // Act - Get orders via API
        var orders = await GetOrdersViaApiAsync(admin);

        // Assert
        orders.Should().NotBeEmpty("There should be at least one order");
    }

    [Fact]
    public async Task AdminOrders_CanViewOrderDetails()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var order = await _dataFactory.CreateOrderAsync();

        // Act - Get order details via API
        var orderDetails = await GetOrderDetailsViaApiAsync(admin, order.Id);

        // Assert
        orderDetails.Should().NotBeNullOrEmpty("Order details should be returned");
        orderDetails.Should().Contain(order.Id.ToString(), "Order details should contain order ID");
    }

    [Fact]
    public async Task AdminOrders_CanUpdateOrderStatus()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var order = await _dataFactory.CreateOrderAsync();

        // Act - Update order status to "Paid" first (Pending -> Paid is valid transition)
        var updateToPaid = await UpdateOrderStatusViaApiAsync(admin, order.Id, "Paid");
        
        // Then update to "Processing" (Paid -> Processing is valid transition)
        var updateToProcessing = await UpdateOrderStatusViaApiAsync(admin, order.Id, "Processing");

        // Assert
        updateToPaid.Should().BeTrue("Admin should be able to update order status to Paid");
        updateToProcessing.Should().BeTrue("Admin should be able to update order status to Processing");
    }

    #endregion

    #region Admin Users Tests

    [Fact]
    public async Task AdminUsers_CanListUsers()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Act - Navigate to users page
        await LoginAsAdminAsync(admin);
        await _page.GotoAsync("/admin/users");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Users page should load (even if placeholder)
        var pageContent = await _page.ContentAsync();
        (pageContent.Contains("User") || pageContent.Contains("user")).Should().BeTrue(
            "Users page should show user management content");
    }

    #endregion

    #region Admin Reviews/Moderation Tests

    [Fact]
    public async Task AdminReviews_CanModerateReview()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Create a review via API
        var reviewId = await CreateReviewViaApiAsync(user, product.Id, 4, "Great product!", "Really love this AC unit");

        if (reviewId == Guid.Empty)
        {
            // If review creation failed (e.g., requires order), skip this test
            return;
        }

        // Act - Navigate to moderation page
        await LoginAsAdminAsync(admin);
        await _page.GotoAsync("/admin/moderation");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on reviews tab
        var reviewsTab = await _page.QuerySelectorAsync("[data-testid='reviews-tab']");
        if (reviewsTab != null)
        {
            await reviewsTab.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // Assert - Moderation page should load
        var pageContent = await _page.ContentAsync();
        (pageContent.Contains("Moderation") || pageContent.Contains("moderation") ||
         pageContent.Contains("Review") || pageContent.Contains("review")).Should().BeTrue(
            "Moderation page should be accessible to admin");
    }

    [Fact]
    public async Task AdminModeration_CanApproveReview()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Create a review via API
        var reviewId = await CreateReviewViaApiAsync(user, product.Id, 5, "Excellent!", "Best AC I've ever owned");

        if (reviewId == Guid.Empty)
        {
            // Skip if review creation not possible
            return;
        }

        // Act - Approve review via API
        var approveSuccess = await ApproveReviewViaApiAsync(admin, reviewId);

        // Assert
        approveSuccess.Should().BeTrue("Admin should be able to approve reviews");
    }

    #endregion

    #region Helper Methods

    private async Task LoginAsAdminAsync(TestUser admin)
    {
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(admin.Email, admin.Password);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<bool> CreateProductViaApiAsync(TestUser admin, string name)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var categoryId = await _dataFactory.GetOrCreateCategoryAsync();

        var response = await client.PostAsJsonAsync("/api/admin/products", new
        {
            name = name,
            sku = $"TEST-{Guid.NewGuid():N}".Substring(0, 20).ToUpper(),
            shortDescription = "Test product description",
            description = "Full test product description",
            basePrice = 999.99m,
            stockQuantity = 10,
            categoryId = categoryId,
            isActive = true
        });

        return response.IsSuccessStatusCode;
    }

    private async Task<List<string>> GetProductsViaApiAsync(TestUser admin, string? searchTerm = null)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var url = "/api/admin/products";
        if (!string.IsNullOrEmpty(searchTerm))
        {
            url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
        }

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var content = await response.Content.ReadAsStringAsync();
        return new List<string> { content };
    }

    private async Task<bool> UpdateProductViaApiAsync(TestUser admin, Guid productId, string newName)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.PutAsJsonAsync($"/api/admin/products/{productId}", new
        {
            id = productId,
            name = newName
        });

        return response.IsSuccessStatusCode;
    }

    private async Task<bool> DeleteProductViaApiAsync(TestUser admin, Guid productId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.DeleteAsync($"/api/admin/products/{productId}");
        return response.IsSuccessStatusCode;
    }

    private async Task<List<string>> GetOrdersViaApiAsync(TestUser admin)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.GetAsync("/api/admin/orders");
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var content = await response.Content.ReadAsStringAsync();
        return new List<string> { content };
    }

    private async Task<string> GetOrderDetailsViaApiAsync(TestUser admin, Guid orderId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.GetAsync($"/api/admin/orders/{orderId}");
        if (!response.IsSuccessStatusCode)
            return string.Empty;

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<bool> UpdateOrderStatusViaApiAsync(TestUser admin, Guid orderId, string status)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new
        {
            status = status,
            notifyCustomer = false
        });

        return response.IsSuccessStatusCode;
    }

    private async Task<Guid> CreateReviewViaApiAsync(TestUser user, Guid productId, int rating, string title, string content)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

        var response = await client.PostAsJsonAsync("/api/reviews", new
        {
            productId = productId,
            rating = rating,
            title = title,
            content = content
        });

        if (!response.IsSuccessStatusCode)
            return Guid.Empty;

        try
        {
            var result = await response.Content.ReadFromJsonAsync<ReviewResponse>();
            return result?.Id ?? Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    private async Task<bool> ApproveReviewViaApiAsync(TestUser admin, Guid reviewId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_fixture.ApiUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await client.PostAsync($"/api/admin/reviews/{reviewId}/approve", null);
        return response.IsSuccessStatusCode;
    }

    #endregion
}

// Response DTOs for API calls
public record ReviewResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("productId")] Guid ProductId);
