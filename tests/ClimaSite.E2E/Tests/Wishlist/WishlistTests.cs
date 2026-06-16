using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using FluentAssertions;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Wishlist;

/// <summary>
/// E2E tests for Wishlist functionality.
/// Tests real API persistence, public share links, and guest-to-login merge.
/// </summary>
[Collection("Playwright")]
public class WishlistTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public WishlistTests(PlaywrightFixture fixture)
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
    public async Task Wishlist_AddFromProductDetail_HydratesFromBackendOnWishlistPage()
    {
        var product = await _dataFactory.CreateProductAsync(name: "Wishlist Backend Hydration AC");
        await LoginUserAsync();

        await AddProductToWishlistFromDetailAsync(product);
        await ClearWishlistLocalStorageAsync();

        await NavigateToWishlistAsync();

        await ExpectWishlistProductAsync(product);
    }

    [Fact]
    public async Task Wishlist_RemoveButton_RemovesServerItem()
    {
        var product = await _dataFactory.CreateProductAsync(name: "Wishlist Remove Server AC");
        await LoginUserAsync();
        await AddProductToWishlistFromDetailAsync(product);
        await ClearWishlistLocalStorageAsync();
        await NavigateToWishlistAsync();
        await ExpectWishlistProductAsync(product);

        var responseTask = _page.WaitForResponseAsync(response =>
            response.Url.Contains("/api/wishlist/items/", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method == "DELETE"
            && response.Status == 200);

        await _page.Locator("[data-testid='remove-from-wishlist']").First.ClickAsync();
        await responseTask;
        await ClearWishlistLocalStorageAsync();
        await NavigateToWishlistAsync();

        await ExpectWishlistEmptyAsync();
    }

    [Fact]
    public async Task Wishlist_ClearAll_RemovesServerItems()
    {
        var productOne = await _dataFactory.CreateProductAsync(name: "Wishlist Clear Server AC 1");
        var productTwo = await _dataFactory.CreateProductAsync(name: "Wishlist Clear Server AC 2");
        await LoginUserAsync();
        await AddProductToWishlistFromDetailAsync(productOne);
        await AddProductToWishlistFromDetailAsync(productTwo);
        await ClearWishlistLocalStorageAsync();
        await NavigateToWishlistAsync();

        await _page.Locator("[data-testid='wishlist-item']").First.WaitForAsync();
        (await _page.Locator("[data-testid='wishlist-item']").CountAsync()).Should().BeGreaterThanOrEqualTo(2);

        // Clearing the whole wishlist is a two-step action: the first click opens an inline
        // confirmation, the confirm button issues the server DELETE.
        await _page.Locator("[data-testid='clear-wishlist']").ClickAsync();

        var responseTask = _page.WaitForResponseAsync(response =>
            response.Url.EndsWith("/api/wishlist", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method == "DELETE"
            && response.Status == 200);

        await _page.Locator("[data-testid='confirm-clear-wishlist']").ClickAsync();
        await responseTask;
        await ClearWishlistLocalStorageAsync();
        await NavigateToWishlistAsync();

        await ExpectWishlistEmptyAsync();
    }

    [Fact]
    public async Task Wishlist_SharedLink_LoadsForAnonymousUser()
    {
        var product = await _dataFactory.CreateProductAsync(name: "Wishlist Public Share AC");
        await LoginUserAsync();
        await AddProductToWishlistFromDetailAsync(product);
        await NavigateToWishlistAsync();

        var responseTask = _page.WaitForResponseAsync(response =>
            response.Url.EndsWith("/api/wishlist/share", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method == "PUT"
            && response.Status == 200);

        await _page.Locator("[data-testid='wishlist-share-toggle']").ClickAsync();
        await responseTask;

        var shareUrlLocator = _page.Locator("[data-testid='wishlist-share-url']");
        await shareUrlLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var shareUrl = (await shareUrlLocator.InnerTextAsync()).Trim();
        shareUrl.Should().Contain("/wishlist/shared/");

        var anonymousPage = await _fixture.CreatePageAsync();
        try
        {
            await anonymousPage.GotoAsync(shareUrl);
            await anonymousPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await anonymousPage.Locator("[data-testid='wishlist-item']").First.WaitForAsync();
            (await anonymousPage.ContentAsync()).Should().Contain(product.Name);
            (await anonymousPage.Locator("[data-testid='remove-from-wishlist']").CountAsync()).Should().Be(0);
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task Wishlist_GuestItems_MergeIntoServerWishlistAfterLogin()
    {
        var product = await _dataFactory.CreateProductAsync(name: "Wishlist Guest Merge AC");
        await ClearWishlistLocalStorageAsync();

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var wishlistButton = _page.Locator("[data-testid='add-to-wishlist']");
        await wishlistButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await wishlistButton.ClickAsync();

        var storedWishlist = await _page.EvaluateAsync<string?>("localStorage.getItem('climasite_wishlist')");
        storedWishlist.Should().NotBeNullOrWhiteSpace();

        await LoginUserAsync();
        await ClearWishlistLocalStorageAsync();
        await _page.ReloadAsync();
        await NavigateToWishlistAsync();

        await ExpectWishlistProductAsync(product);
    }

    private async Task<TestUser> LoginUserAsync()
    {
        var user = await _dataFactory.CreateUserAsync();
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        (await loginPage.IsLoggedInAsync()).Should().BeTrue();
        return user;
    }

    private async Task AddProductToWishlistFromDetailAsync(TestProduct product)
    {
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var wishlistButton = _page.Locator("[data-testid='add-to-wishlist']");
        await wishlistButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var responseTask = _page.WaitForResponseAsync(response =>
            response.Url.Contains("/api/wishlist/items/", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method == "POST"
            && response.Status == 200);

        await wishlistButton.ClickAsync();
        await responseTask;
    }

    private async Task NavigateToWishlistAsync()
    {
        await _page.GotoAsync("/wishlist");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForSelectorAsync(
            "[data-testid='wishlist-items'], [data-testid='wishlist-empty']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    private async Task ExpectWishlistProductAsync(TestProduct product)
    {
        await _page.Locator("[data-testid='wishlist-item']").First.WaitForAsync();
        (await _page.ContentAsync()).Should().Contain(product.Name);
    }

    private async Task ExpectWishlistEmptyAsync()
    {
        var emptyState = _page.Locator("[data-testid='wishlist-empty']");
        await emptyState.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await emptyState.IsVisibleAsync()).Should().BeTrue();
    }

    private async Task ClearWishlistLocalStorageAsync()
    {
        if (_page.Url == "about:blank")
        {
            await _page.GotoAsync(_fixture.BaseUrl);
        }

        await _page.EvaluateAsync("localStorage.removeItem('climasite_wishlist')");
    }
}
