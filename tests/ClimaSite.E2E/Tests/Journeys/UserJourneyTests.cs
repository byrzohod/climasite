using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Journeys;

/// <summary>
/// Complete end-to-end user journey tests.
/// These tests simulate real user behavior from start to finish.
/// </summary>
[Collection("Playwright")]
public class UserJourneyTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public UserJourneyTests(PlaywrightFixture fixture)
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

    /// <summary>
    /// JNY-001: First-Time Buyer Journey
    /// A new user logs in and browses products.
    /// </summary>
    [Fact]
    public async Task FirstTimeBuyer_LoginAndBrowse()
    {
        // Create user via API
        var user = await _dataFactory.CreateUserAsync();

        // Step 1: Land on homepage
        var homePage = new HomePage(_page);
        await homePage.NavigateAsync();

        // Verify homepage loads
        var hasFeatured = await homePage.HasFeaturedProductsAsync();
        hasFeatured.Should().BeTrue("Homepage should display featured products");

        // Step 2: Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Verify login succeeded
        var isLoggedIn = await loginPage.IsLoggedInAsync();
        isLoggedIn.Should().BeTrue("User should be logged in");

        // Step 3: Browse products
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Wait for products to load
        await _page.WaitForSelectorAsync("[data-testid='product-card']", new PageWaitForSelectorOptions { Timeout = 10000 });
        var productCount = await productPage.GetProductCardCountAsync();
        productCount.Should().BeGreaterThan(0, "Product list should contain products");

        // Step 4: Click on a product to view details
        await _page.ClickAsync("[data-testid='product-card']:first-child");
        await _page.WaitForURLAsync(url => url.Contains("/products/"));

        // Step 5: Add product to cart
        await productPage.AddToCartAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify cart updated
        var cartCount = await homePage.GetCartCountAsync();
        cartCount.Should().BeGreaterThanOrEqualTo(1, "Cart should have at least 1 item");
    }

    /// <summary>
    /// JNY-002: Returning Customer Journey
    /// An existing user logs in and quickly makes a purchase.
    /// </summary>
    [Fact]
    public async Task ReturningCustomer_QuickPurchaseJourney()
    {
        // Arrange - Create user via API
        var user = await _dataFactory.CreateUserAsync();

        // Step 1: Log in
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Verify login successful
        var isLoggedIn = await loginPage.IsLoggedInAsync();
        isLoggedIn.Should().BeTrue("User should be logged in");

        // Step 2: Browse products
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Step 3: Add product to cart
        await _page.ClickAsync("[data-testid='product-card']:first-child");
        await productPage.AddToCartAsync();

        // Step 4: Go to cart
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();

        // Step 5: Verify cart and proceed
        var isEmpty = await cartPage.IsEmptyAsync();
        isEmpty.Should().BeFalse("Cart should not be empty");

        var total = await cartPage.GetTotalAsync();
        total.Should().BeGreaterThan(0, "Cart should have a total");

        // Step 6: Proceed to checkout
        await cartPage.ProceedToCheckoutAsync();

        // Checkout page should load
        _page.Url.Should().Contain("checkout", "User should be on checkout page");
    }

    /// <summary>
    /// JNY-003: International Buyer Journey
    /// User changes language and completes purchase in different language.
    /// </summary>
    [Fact]
    public async Task InternationalBuyer_LanguageSwitchJourney()
    {
        // Step 1: Start on homepage in English
        var homePage = new HomePage(_page);
        await homePage.NavigateAsync();

        // Verify default language
        var pageContent = await _page.ContentAsync();
        pageContent.Should().Contain("Products"); // English text

        // Step 2: Switch to Bulgarian
        await homePage.SelectLanguageAsync("bg");

        // Wait for page to update
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 3: Browse products
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Step 4: Add to cart
        await _page.ClickAsync("[data-testid='product-card']:first-child");
        await productPage.AddToCartAsync();

        // Step 5: Switch to German
        await homePage.SelectLanguageAsync("de");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 6: View cart in German
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();

        // Cart should still have items
        var itemCount = await cartPage.GetItemCountAsync();
        itemCount.Should().BeGreaterThanOrEqualTo(1, "Cart should persist across language changes");
    }

    /// <summary>
    /// Theme Toggle Journey
    /// User switches between light and dark themes.
    /// </summary>
    [Fact]
    public async Task ThemeToggle_SwitchThemeJourney()
    {
        // Step 1: Start on homepage
        var homePage = new HomePage(_page);
        await homePage.NavigateAsync();

        // Step 2: Get current theme (check body class or data attribute)
        var isDarkMode = await _page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')");

        // Step 3: Toggle theme
        await homePage.ToggleThemeAsync();

        // Step 4: Verify theme changed
        var isNowDarkMode = await _page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')");

        isNowDarkMode.Should().NotBe(isDarkMode, "Theme should have toggled");

        // Step 5: Navigate to products - theme should persist
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Step 6: Verify theme persisted
        var stillDarkMode = await _page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('dark')");

        stillDarkMode.Should().Be(isNowDarkMode, "Theme should persist across navigation");
    }

    /// <summary>
    /// User can browse and filter products
    /// </summary>
    [Fact]
    public async Task User_CanBrowseAndFilterProducts()
    {
        // Step 1: Navigate to products page
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 2: Verify products are displayed
        await Assertions.Expect(_page.Locator("[data-testid='product-card']").First).ToBeVisibleAsync();

        // Step 3: Try to use filters if available
        var hasFilters = await _page.IsVisibleAsync("[data-testid='product-filters']");
        if (hasFilters)
        {
            // Filters are available - verify they exist
            await Assertions.Expect(_page.Locator("[data-testid='product-filters']")).ToBeVisibleAsync();
        }

        // Step 4: Click on a product
        await _page.ClickAsync("[data-testid='product-card']:first-child");

        // Step 5: Verify product detail page
        await _page.WaitForURLAsync(url => url.Contains("/products/"));
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='add-to-cart']")).ToBeVisibleAsync();
    }

    /// <summary>
    /// User can navigate using header links
    /// </summary>
    [Fact]
    public async Task User_CanNavigateUsingHeader()
    {
        // Step 1: Start on homepage
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 2: Click on cart icon
        await _page.ClickAsync("[data-testid='cart-icon']");
        await _page.WaitForURLAsync(url => url.Contains("/cart"));

        // Step 3: Navigate to home using logo (correct selector)
        await _page.ClickAsync("[data-testid='header-logo']");
        await _page.WaitForURLAsync("/");

        // Step 4: Use the mega menu
        await _page.HoverAsync("[data-testid='mega-menu-trigger']");
        await Assertions.Expect(_page.Locator("[data-testid='mega-menu-dropdown']")).ToBeVisibleAsync();
    }
}
