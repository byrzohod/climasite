using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Accessibility;

/// <summary>
/// Automated accessibility tests using axe-core.
/// Tests key pages for WCAG 2.1 compliance, focusing on critical and serious violations.
/// </summary>
[Collection("Playwright")]
public class AccessibilityTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    // Rule IDs to exclude from checks (with justification)
    // Add rules to exclude here if needed, with comments explaining why
    // Example: "color-contrast" - if using custom contrast ratios for specific brand colors
    private static readonly string[] ExcludedRules = Array.Empty<string>();

    public AccessibilityTests(PlaywrightFixture fixture)
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

    #region Page Tests

    [Fact]
    public async Task HomePage_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Home page");
    }

    [Fact]
    public async Task ProductListPage_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for products to load
        await WaitForContentToLoad("[data-testid='product-card'], [data-testid='no-products']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Product list page");
    }

    [Fact]
    public async Task ProductDetailPage_HasNoAccessibilityViolations()
    {
        // Arrange - Create a product to test
        var product = await _dataFactory.CreateProductAsync(
            name: "Accessibility Test AC",
            price: 499.99m
        );

        await _page.GotoAsync($"/products/{product.Slug}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for product details to load
        await WaitForContentToLoad("[data-testid='product-name'], [data-testid='product-price']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Product detail page");
    }

    [Fact]
    public async Task CartPage_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/cart");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for cart content to load
        await WaitForContentToLoad("[data-testid='cart-items'], [data-testid='empty-cart']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Cart page");
    }

    [Fact]
    public async Task CheckoutPage_HasNoAccessibilityViolations()
    {
        // Arrange - Need to add item to cart first and be logged in
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync(
            name: "Checkout A11y Test AC",
            price: 299.99m
        );

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Add product to cart
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Navigate to checkout
        await _page.GotoAsync("/checkout");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for checkout form to load
        await WaitForContentToLoad("[data-testid='checkout-form'], [data-testid='checkout-summary']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Checkout page");
    }

    [Fact]
    public async Task LoginPage_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/auth/login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for login form to load
        await WaitForContentToLoad("[data-testid='login-form'], [data-testid='email-input']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Login page");
    }

    [Fact]
    public async Task RegisterPage_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/auth/register");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for register form to load
        await WaitForContentToLoad("[data-testid='register-form'], [data-testid='email-input']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Register page");
    }

    #endregion

    #region Theme Tests

    [Fact]
    public async Task HomePage_DarkTheme_HasNoAccessibilityViolations()
    {
        // Arrange - Enable dark theme
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Toggle to dark theme if not already
        await EnableDarkTheme();

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Home page (dark theme)");
    }

    [Fact]
    public async Task ProductListPage_DarkTheme_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForContentToLoad("[data-testid='product-card'], [data-testid='no-products']");

        // Enable dark theme
        await EnableDarkTheme();

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Product list page (dark theme)");
    }

    #endregion

    #region Component State Tests

    [Fact]
    public async Task CartPage_WithItems_HasNoAccessibilityViolations()
    {
        // Arrange - Add items to cart
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync(
            name: "Cart A11y Test AC",
            price: 399.99m
        );

        // Login and add to cart
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Navigate to cart
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Cart page (with items)");
    }

    [Fact]
    public async Task ProductListPage_WithFilters_HasNoAccessibilityViolations()
    {
        // Arrange
        await _page.GotoAsync("/products?category=air-conditioners");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForContentToLoad("[data-testid='product-card'], [data-testid='no-products']");

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Product list page (with filters)");
    }

    #endregion

    #region Modal and Dialog Tests

    [Fact]
    public async Task LoginModal_WhenOpen_HasNoAccessibilityViolations()
    {
        // Arrange - Go to page where login modal can be triggered
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to open login modal via header login button
        var loginButton = _page.Locator("[data-testid='header-login-button'], [data-testid='login-link']");
        if (await loginButton.IsVisibleAsync())
        {
            await loginButton.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        else
        {
            // Navigate directly to login page
            await _page.GotoAsync("/auth/login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // Act
        var results = await RunAccessibilityScan();

        // Assert
        AssertNoSeriousViolations(results, "Login modal/page");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Runs an axe-core accessibility scan with standard configuration.
    /// </summary>
    private async Task<AxeResult> RunAccessibilityScan(AxeRunOptions? options = null)
    {
        var runOptions = options ?? new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions
            {
                Type = "tag",
                Values = new List<string> { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice" }
            }
        };

        // Exclude specific rules if needed
        if (ExcludedRules.Length > 0)
        {
            runOptions.Rules = ExcludedRules
                .ToDictionary(rule => rule, _ => new RuleOptions { Enabled = false });
        }

        return await _page.RunAxe(runOptions);
    }

    /// <summary>
    /// Asserts that no critical or serious accessibility violations were found.
    /// </summary>
    private static void AssertNoSeriousViolations(AxeResult results, string pageName)
    {
        var seriousViolations = results.Violations
            .Where(v => v.Impact == "critical" || v.Impact == "serious")
            .ToList();

        if (seriousViolations.Count > 0)
        {
            var violationDetails = string.Join("\n", seriousViolations.Select(v =>
                $"  - [{v.Impact?.ToUpper()}] {v.Id}: {v.Description}\n" +
                $"    Help: {v.HelpUrl}\n" +
                $"    Affected elements: {string.Join(", ", v.Nodes.Select(n => n.Html).Take(3))}"));

            seriousViolations.Should().BeEmpty(
                $"{pageName} should have no critical or serious accessibility violations.\n\nViolations found:\n{violationDetails}");
        }

        // Log moderate/minor violations as warnings (not failures)
        var minorViolations = results.Violations
            .Where(v => v.Impact == "moderate" || v.Impact == "minor")
            .ToList();

        if (minorViolations.Count > 0)
        {
            // These are logged but don't fail the test
            Console.WriteLine($"[A11Y WARNING] {pageName} has {minorViolations.Count} minor/moderate violations:");
            foreach (var violation in minorViolations.Take(5))
            {
                Console.WriteLine($"  - [{violation.Impact}] {violation.Id}: {violation.Description}");
            }
        }
    }

    /// <summary>
    /// Waits for content to load on the page.
    /// </summary>
    private async Task WaitForContentToLoad(string selector, int timeoutMs = 5000)
    {
        try
        {
            await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs
            });
        }
        catch (TimeoutException)
        {
            // Content may not exist, continue with the test
        }
    }

    /// <summary>
    /// Enables dark theme on the page.
    /// </summary>
    private async Task EnableDarkTheme()
    {
        var themeToggle = _page.Locator("[data-testid='theme-toggle'], [data-testid='theme-switcher']");
        if (await themeToggle.IsVisibleAsync())
        {
            // Check if already in dark mode
            var isDarkMode = await _page.EvaluateAsync<bool>(
                "() => document.documentElement.classList.contains('dark') || " +
                "document.body.classList.contains('dark-theme')");

            if (!isDarkMode)
            {
                await themeToggle.ClickAsync();
                // Wait for theme transition
                await _page.WaitForTimeoutAsync(300);
            }
        }
    }

    #endregion
}
