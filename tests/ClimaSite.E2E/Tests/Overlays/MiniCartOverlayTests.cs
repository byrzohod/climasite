using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Overlays;

/// <summary>
/// SLICE D regression coverage for the reported mini-cart overlay bug: the drawer's "Checkout" and
/// "View cart" actions must be the real topmost, clickable elements — NOT covered by the drawer
/// backdrop or the cookie-consent banner. These tests render the REAL cookie banner
/// (CreatePageAsync(seedCookieConsent: false)) and perform NON-forced clicks plus an
/// elementFromPoint hit-test, so a regression that lets an overlay intercept the click would fail
/// here instead of shipping. NO MOCKING — real product, real cart, real navigation.
/// </summary>
[Collection("Playwright")]
public class MiniCartOverlayTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public MiniCartOverlayTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // seedCookieConsent: false -> the real cookie-consent banner renders. It is fixed to the
        // viewport and is exactly the kind of overlay that could intercept clicks on the drawer
        // actions, which is what this test must catch.
        _page = await _fixture.CreatePageAsync(seedCookieConsent: false);
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _dataFactory.CleanupAsync();
        await _fixture.CloseTracedContextAsync(_page);
    }

    /// <summary>Creates a product and adds it to the (guest) cart through the real product-detail UI.</summary>
    private async Task AddProductToCartAsync()
    {
        var product = await _dataFactory.CreateProductAsync(name: "Overlay Test AC", price: 1199.99m, stock: 25);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        // Confirm the cart actually has an item before we open the drawer (the footer with the
        // checkout/view-cart actions only renders for a non-empty cart). The add-to-cart signal can
        // be slow on a loaded CI runner, so wait generously and retry the add once if the badge has
        // not appeared — without this the test occasionally flakes on the add step (not the overlay).
        var cartCount = _page.Locator("[data-testid='cart-count']");
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            await productPage.AddToCartAsync();
            try
            {
                await cartCount.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 15000
                });
                return;
            }
            catch (TimeoutException) when (attempt == 1)
            {
                // Transient: the cart signal did not surface in time — re-attempt the add once.
            }
        }

        await Assertions.Expect(cartCount).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
    }

    // THE test that would have caught the reported bug: checkout action is the topmost element and a
    // real (non-forced) click navigates to /checkout.
    [Fact]
    public async Task MiniCart_CheckoutButton_IsTopmostAndNavigatesToCheckout()
    {
        // Arrange
        await AddProductToCartAsync();

        var drawer = new MiniCartDrawer(_page);
        await drawer.OpenAsync();
        (await drawer.IsOpenAsync()).Should().BeTrue("Mini-cart drawer should be visible after opening");

        // Assert (the core of the regression): the element the browser hit-tests at the centre of the
        // checkout button is the checkout link itself (or one of its descendants), NOT the drawer
        // backdrop and NOT the cookie-consent banner.
        var topmost = await drawer.GetTopmostElementAtAsync(MiniCartDrawer.CheckoutButton);
        topmost.Found.Should().BeTrue("An element should be hit-tested at the checkout button centre");
        topmost.MatchedTestId.Should().Be("mini-cart-checkout",
            "the checkout link must be the topmost element at its own centre — not covered by an overlay");
        topmost.MatchedTestId.Should().NotBe("mini-cart-backdrop",
            "the backdrop must not cover the checkout button");
        topmost.MatchedTestId.Should().NotStartWith("cookie-consent",
            "the cookie-consent banner must not cover the checkout button");

        // Act - a REAL, non-forced click. If anything overlays the button, Playwright's actionability
        // checks fail (or the click lands on the overlay) and this navigation assertion fails.
        await _page.ClickAsync(MiniCartDrawer.CheckoutButton);

        // Assert - navigation actually reached the checkout page.
        await _page.WaitForURLAsync(u => u.Contains("/checkout"),
            new PageWaitForURLOptions { Timeout = 15000 });
        _page.Url.Should().Contain("/checkout");
        _page.Url.Should().NotContain("/login", "guest checkout must not bounce to login (GAP-07)");
    }

    // Second test: the "View cart" action real-clicks through to /cart.
    [Fact]
    public async Task MiniCart_ViewCartButton_IsTopmostAndNavigatesToCart()
    {
        // Arrange
        await AddProductToCartAsync();

        var drawer = new MiniCartDrawer(_page);
        await drawer.OpenAsync();
        (await drawer.IsOpenAsync()).Should().BeTrue("Mini-cart drawer should be visible after opening");

        // Assert the view-cart action is the real topmost element (not the backdrop / cookie banner).
        var topmost = await drawer.GetTopmostElementAtAsync(MiniCartDrawer.ViewCartButton);
        topmost.Found.Should().BeTrue("An element should be hit-tested at the view-cart button centre");
        topmost.MatchedTestId.Should().Be("mini-cart-view-cart",
            "the view-cart link must be the topmost element at its own centre — not covered by an overlay");
        topmost.MatchedTestId.Should().NotBe("mini-cart-backdrop",
            "the backdrop must not cover the view-cart button");
        topmost.MatchedTestId.Should().NotStartWith("cookie-consent",
            "the cookie-consent banner must not cover the view-cart button");

        // Act - real, non-forced click.
        await _page.ClickAsync(MiniCartDrawer.ViewCartButton);

        // Assert - navigation reached the full cart page.
        await _page.WaitForURLAsync(u => u.Contains("/cart"),
            new PageWaitForURLOptions { Timeout = 15000 });
        _page.Url.Should().Contain("/cart");
        await Assertions.Expect(_page.Locator("[data-testid='cart-item']").First).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
    }
}
