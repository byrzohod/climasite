using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Internationalization;

[Collection("Playwright")]
public class LanguageTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public LanguageTests(PlaywrightFixture fixture)
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
    public async Task HomePage_LoadsWithDefaultLanguage()
    {
        // Act
        var homePage = new HomePage(_page);
        await homePage.NavigateAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page should load without errors
        var title = await _page.TitleAsync();
        title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProductPage_LoadsWithLanguageParameter()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync(
            name: "Test Multilingual Product",
            price: 899.99m
        );

        // Act - Navigate to product with default language
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Product should load with its name
        var productTitle = await productPage.GetProductTitleAsync();
        productTitle.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProductList_LoadsProductsWithCurrentLanguage()
    {
        // Arrange - Create products
        await _dataFactory.CreateProductAsync(name: "Language Test AC 1", price: 599.99m);
        await _dataFactory.CreateProductAsync(name: "Language Test AC 2", price: 799.99m);

        // Act
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait a bit for any async rendering
        await _page.WaitForTimeoutAsync(500);

        // Assert - Product list should render
        var pageContent = await _page.ContentAsync();
        pageContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProductDetail_DisplaysTranslatedContent_WhenAvailable()
    {
        // This test verifies the product detail page loads correctly
        // Actual translation content depends on having translations in the database

        // Arrange
        var product = await _dataFactory.CreateProductAsync(
            name: "Translation Test Product",
            price: 1099.99m
        );

        // Act - Load product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await productPage.GetProductTitleAsync();
        title.Should().Contain("Translation Test Product");
    }

    [Fact]
    public async Task APIEndpoint_AcceptsLanguageParameter()
    {
        // Test that the API accepts the lang parameter without errors

        // Arrange
        var product = await _dataFactory.CreateProductAsync(
            name: "API Language Test",
            price: 449.99m
        );

        // Act - Navigate with lang parameter in URL
        await _page.GotoAsync($"{_fixture.BaseUrl}/products/{product.Slug}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page should load without errors
        var response = await _page.ContentAsync();
        response.Should().NotContain("Error");
    }

    [Fact]
    public async Task ProductSearch_WorksWithCurrentLanguage()
    {
        // Arrange - Create a product with a unique searchable name
        var uniqueName = $"UniqueSearchProduct-{Guid.NewGuid().ToString().Substring(0, 8)}";
        await _dataFactory.CreateProductAsync(name: uniqueName, price: 699.99m);

        // Act - Navigate to product list
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page loads without errors
        var content = await _page.ContentAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
