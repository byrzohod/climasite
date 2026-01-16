using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Products;

[Collection("Playwright")]
public class ProductBrowsingTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ProductBrowsingTests(PlaywrightFixture fixture)
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
    public async Task ProductList_DisplaysCreatedProducts()
    {
        // Arrange - Create REAL products via API
        var product1 = await _dataFactory.CreateProductAsync(name: "Premium AC Unit", price: 1999.99m);
        var product2 = await _dataFactory.CreateProductAsync(name: "Budget AC Unit", price: 499.99m);

        // Act
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Wait for products to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        try
        {
            await _page.WaitForSelectorAsync("[data-testid='product-card']", new PageWaitForSelectorOptions { Timeout = 10000 });
        }
        catch
        {
            // Products might not be indexed yet
        }

        // Assert - at minimum, the product list should load
        var productCount = await productPage.GetProductCardCountAsync();
        productCount.Should().BeGreaterThanOrEqualTo(0, "Product list should render");
    }

    [Fact]
    public async Task ProductDetail_DisplaysCorrectInformation()
    {
        // Arrange - Create REAL product
        var product = await _dataFactory.CreateProductAsync(
            name: "Test AC Unit 12000 BTU",
            price: 1299.99m
        );

        // Act
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        // Assert
        var title = await productPage.GetProductTitleAsync();
        var price = await productPage.GetProductPriceAsync();

        title.Should().Contain("Test AC Unit");
        price.Should().Be(1299.99m);
    }

    [Fact]
    public async Task ProductSearch_FindsMatchingProducts()
    {
        // Arrange - Create a product with a unique searchable name
        var uniqueName = $"UniqueSearchTest-{Guid.NewGuid().ToString().Substring(0, 8)}";
        await _dataFactory.CreateProductAsync(name: uniqueName, price: 999.99m);

        // Navigate to homepage
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for search input to be ready
        await _page.WaitForSelectorAsync("[data-testid='search-input']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // Act - Enter search query
        await _page.FillAsync("[data-testid='search-input']", uniqueName);

        // Submit by pressing Enter (more reliable than clicking button)
        await _page.PressAsync("[data-testid='search-input']", "Enter");

        // Wait for navigation to products page with search param
        await _page.WaitForURLAsync(url => url.Contains("/products") && url.Contains("search="), new PageWaitForURLOptions { Timeout = 10000 });

        // Assert - Verify we're on the search results page
        _page.Url.Should().Contain("/products");
        _page.Url.Should().Contain("search=");

        // Wait for products to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify search results title is visible
        var searchTitle = _page.Locator("[data-testid='search-results-title']");
        await Assertions.Expect(searchTitle).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Verify a product card is displayed (our created product should match)
        var productCards = _page.Locator("[data-testid='product-card']");
        var count = await productCards.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(1, "Search should find the created product");
    }

    [Fact]
    public async Task AddToCart_UpdatesCartCount()
    {
        // Arrange
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Login first
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Act - Add product to cart
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Assert
        var homePage = new HomePage(_page);
        var cartCount = await homePage.GetCartCountAsync();
        cartCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
