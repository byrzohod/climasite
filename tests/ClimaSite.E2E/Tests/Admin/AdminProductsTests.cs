using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Admin;

/// <summary>
/// E2E tests for the GAP-02 Admin Products UI (list + create/edit form + activate/deactivate).
/// Real data only — each test creates its own admin + products and cleans up via the factory.
/// </summary>
[Collection("Playwright")]
public class AdminProductsTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public AdminProductsTests(PlaywrightFixture fixture)
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
    public async Task AdminProducts_ListsSeededProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var name = $"E2E List {Guid.NewGuid():N}".Substring(0, 24);
        var product = await _dataFactory.CreateProductAsync(name: name);
        product.Id.Should().NotBe(Guid.Empty, "the seeded product must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var productsPage = new AdminProductsPage(_page);
        await productsPage.NavigateToListAsync();

        // The list is paginated (20/page); search by name to guarantee the row is on screen.
        await productsPage.SearchAsync(name);

        // Assert
        var hasRow = await productsPage.HasProductRowAsync(product.Id.ToString());
        hasRow.Should().BeTrue("the seeded product should appear in the admin products list");
        await Assertions.Expect(productsPage.ProductRow(product.Id.ToString()))
            .ToContainTextAsync(name, new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
    }

    [Fact]
    public async Task AdminProducts_CanCreateProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var name = $"E2E Create {suffix}".Substring(0, 24);
        var sku = $"E2E-{suffix}".Substring(0, 16).ToUpperInvariant();

        // Act
        await LoginAsAdminAsync(admin);
        var productsPage = new AdminProductsPage(_page);
        await productsPage.NavigateToListAsync();

        await productsPage.OpenCreateFormAsync();
        await productsPage.CreateProductAsync(name, sku, 1299.99m);

        // Find the newly created product via search.
        await productsPage.SearchAsync(name);

        // Assert — a row carrying the new name is present.
        await Assertions.Expect(
                _page.Locator("[data-testid='product-row']", new PageLocatorOptions { HasText = name }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
    }

    [Fact]
    public async Task AdminProducts_CanEditProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var originalName = $"E2E Edit {Guid.NewGuid():N}".Substring(0, 24);
        var product = await _dataFactory.CreateProductAsync(name: originalName);
        product.Id.Should().NotBe(Guid.Empty, "the seeded product must be created");
        var updatedName = $"E2E Updated {Guid.NewGuid():N}".Substring(0, 24);

        // Act
        await LoginAsAdminAsync(admin);
        var productsPage = new AdminProductsPage(_page);
        await productsPage.NavigateToListAsync();
        await productsPage.SearchAsync(originalName);

        await productsPage.OpenEditFormAsync(product.Id.ToString(), originalName);
        await productsPage.SetNameAsync(updatedName);
        await productsPage.SubmitFormAsync();

        // Re-navigate for a clean list (avoids any stale in-component search state left from the
        // pre-edit filter), then search by the new name.
        await productsPage.NavigateToListAsync();
        await productsPage.SearchAsync(updatedName);

        // Assert — the same product id row now shows the updated name.
        await Assertions.Expect(productsPage.ProductRow(product.Id.ToString()))
            .ToContainTextAsync(updatedName, new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
    }

    [Fact]
    public async Task AdminProducts_CanDeactivateProduct()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var name = $"E2E Deact {Guid.NewGuid():N}".Substring(0, 24);
        var product = await _dataFactory.CreateProductAsync(name: name);
        product.Id.Should().NotBe(Guid.Empty, "the seeded product must be created");

        // The deactivate action triggers window.confirm() — auto-accept any dialog.
        _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        // Act
        await LoginAsAdminAsync(admin);
        var productsPage = new AdminProductsPage(_page);
        await productsPage.NavigateToListAsync();
        await productsPage.SearchAsync(name);

        await productsPage.ClickDeactivateAsync(product.Id.ToString());

        // Assert — after deactivation the status badge flips to the inactive state and the
        // row exposes an "activate" action (the deactivate action is replaced).
        await Assertions.Expect(productsPage.StatusBadge(product.Id.ToString()))
            .ToContainTextAsync("Inactive", new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
        await Assertions.Expect(productsPage.ActivateAction(product.Id.ToString()))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
    }

    private async Task LoginAsAdminAsync(TestUser admin)
    {
        admin.Token.Should().NotBeNullOrWhiteSpace("admin test users must include a real access token");

        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.EvaluateAsync("token => window.localStorage.setItem('climasite_token', token)", admin.Token);
        await _page.ReloadAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
