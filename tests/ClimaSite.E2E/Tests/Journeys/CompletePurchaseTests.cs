using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Journeys;

/// <summary>
/// Complete user journey E2E tests (E2E-060 to E2E-064).
/// Tests full purchase flows from browsing to order completion.
/// NO MOCKING - All data is created via real API calls.
/// </summary>
[Collection("Playwright")]
public class CompletePurchaseTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public CompletePurchaseTests(PlaywrightFixture fixture)
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

    // E2E-060: User login → Browse → Add to Cart → Checkout flow
    [Fact]
    public async Task AuthenticatedUser_BrowseAndAddToCart_CompletesSuccessfully()
    {
        // Create user and product via API
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync();

        // Step 1: Login via UI
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Verify login succeeded
        var isLoggedIn = await loginPage.IsLoggedInAsync();
        isLoggedIn.Should().BeTrue("User should be logged in");

        // Step 2: Browse products
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(_page.Locator("[data-testid='product-card']").First).ToBeVisibleAsync();

        // Step 3: View product detail
        await _page.ClickAsync("[data-testid='product-card'] >> nth=0");
        await _page.WaitForURLAsync(url => url.Contains("/products/"));

        // Step 4: Add to cart
        await _page.ClickAsync("[data-testid='add-to-cart']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify cart badge updated
        await Assertions.Expect(_page.Locator("[data-testid='cart-count']")).ToBeVisibleAsync();

        // Step 5: Go to cart
        await _page.ClickAsync("[data-testid='cart-icon']");
        await _page.WaitForURLAsync(url => url.Contains("/cart"));

        // Verify cart has items
        await Assertions.Expect(_page.Locator("[data-testid='cart-item']").First).ToBeVisibleAsync();

        // Step 6: Verify checkout button is available
        await Assertions.Expect(_page.Locator("[data-testid='proceed-to-checkout']")).ToBeVisibleAsync();
    }

    // E2E-061: User can proceed to checkout with items in cart
    [Fact]
    public async Task AuthenticatedUser_ProceedToCheckout_ShowsCheckoutPage()
    {
        // Create user and product via API
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Add product to cart
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Go to cart and proceed to checkout
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Verify checkout page loaded
        await Assertions.Expect(_page.Locator("[data-testid='checkout-page']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='shipping-section']")).ToBeVisibleAsync();
    }

    // E2E-062: User can fill shipping information
    [Fact]
    public async Task Checkout_FillShippingInfo_CanProceedToPayment()
    {
        // Create user and product via API
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync();

        // Login and add to cart
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Go to checkout
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Fill shipping info
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Test",
            lastName: "User",
            email: user.Email,
            street: "123 Test Street",
            city: "Sofia",
            state: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888123456"
        );

        // Proceed to payment
        await checkoutPage.SubmitShippingFormAsync();

        // Verify payment step (card number field visible)
        var isOnPayment = await checkoutPage.IsOnPaymentStepAsync();
        isOnPayment.Should().BeTrue("Should proceed to payment step");
    }

    // E2E-063: Browse by Category → Filter → View Product
    [Fact]
    public async Task User_BrowseByCategoryAndFilter_FindsProducts()
    {
        // Create products in a category
        await _dataFactory.CreateProductAsync();

        // Step 1: Open mega menu and hover on a category to show subcategories
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.HoverAsync("[data-testid='mega-menu-trigger']");
        await _page.WaitForSelectorAsync("[data-testid='mega-menu-dropdown']");

        // Hover over first category to show its subcategories
        await _page.HoverAsync("[data-testid='category-item'] >> nth=0");
        await _page.WaitForSelectorAsync("[data-testid='subcategories-panel']");

        // Click on a subcategory link to navigate
        await _page.ClickAsync("[data-testid='subcategory-link'] >> nth=0");

        // Assert - Products page with category filter
        await _page.WaitForURLAsync(url => url.Contains("/products"), new PageWaitForURLOptions { Timeout = 10000 });

        // Step 2: Products are displayed
        await Assertions.Expect(_page.Locator("[data-testid='product-card']").First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Step 3: Select a product
        await _page.ClickAsync("[data-testid='product-card'] >> nth=0");

        // Assert - Product detail page
        await _page.WaitForURLAsync(url => url.Contains("/products/"));
        await Assertions.Expect(_page.Locator("[data-testid='product-title']")).ToBeVisibleAsync();
    }

    // E2E-064: Search input exists and is usable
    [Fact]
    public async Task User_CanTypeInSearchInput()
    {
        // Navigate to homepage
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify search input exists and can be typed into
        var searchInput = _page.Locator("[data-testid='search-input']");
        await Assertions.Expect(searchInput).ToBeVisibleAsync();

        await _page.FillAsync("[data-testid='search-input']", "Air Conditioner");

        // Verify text was entered
        var inputValue = await searchInput.InputValueAsync();
        inputValue.Should().Be("Air Conditioner");

        // Verify search button exists
        await Assertions.Expect(_page.Locator("[data-testid='search-button']")).ToBeVisibleAsync();
    }

    // E2E: Cart persists across page navigation
    [Fact]
    public async Task Cart_AfterAddingProduct_PersistsAcrossPages()
    {
        // Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Add to cart
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.ClickAsync("[data-testid='add-to-cart'] >> nth=0");

        // Get cart count
        await Assertions.Expect(_page.Locator("[data-testid='cart-count']")).ToBeVisibleAsync();
        var countBefore = await _page.Locator("[data-testid='cart-count']").TextContentAsync();

        // Navigate to different pages
        await _page.GotoAsync("/about");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Cart count persists
        var countAfter = await _page.Locator("[data-testid='cart-count']").TextContentAsync();
        countAfter.Should().Be(countBefore);

        // Navigate to home
        await _page.GotoAsync("/");

        // Assert - Cart count still persists
        var countHome = await _page.Locator("[data-testid='cart-count']").TextContentAsync();
        countHome.Should().Be(countBefore);
    }

    // E2E: Authenticated user can view their account
    [Fact]
    public async Task AuthenticatedUser_CanAccessAccountPage()
    {
        // Create user via API
        var user = await _dataFactory.CreateUserAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Navigate to account via user menu
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await _page.WaitForSelectorAsync("[data-testid='user-dropdown']");
        await _page.ClickAsync("[data-testid='account-link']");

        // Verify account page
        await _page.WaitForURLAsync(url => url.Contains("/account"));
        await Assertions.Expect(_page.Locator("[data-testid='account-page']")).ToBeVisibleAsync();
    }
}
