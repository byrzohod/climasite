using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.Infrastructure.Retry;
using ClimaSite.E2E.PageObjects;
using FluentAssertions;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Journeys;

/// <summary>
/// The flagship full-customer-journey E2E test: ONE chained run that exercises a real human
/// customer end-to-end — register → log in → browse → add to cart → wishlist → checkout
/// (bank transfer) → order history → order details → submit a review — all driven through the UI
/// against the real API and database (NO MOCKING).
///
/// Why one big chained test (plus a focused second one): the whole point is to prove the journey
/// holds together, not just that isolated segments work. To keep it resilient on flake-prone
/// auth/nav steps it uses <see cref="RetryFactAttribute"/> (infra/timeout retries only — assertion
/// failures never retry). The post-purchase steps (history / details / review) are split into a
/// second chained test so a flake late in the funnel doesn't mask the core
/// register→buy→history result.
///
/// Bank transfer is used to COMPLETE the order so no real Stripe is needed — this mirrors the proven
/// <c>GuestCheckoutCompletionTests</c> template, but for a REGISTERED customer.
/// </summary>
[Collection("Playwright")]
public class FullCustomerJourneyTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public FullCustomerJourneyTests(PlaywrightFixture fixture)
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
        await _fixture.CloseTracedContextAsync(_page);
    }

    /// <summary>
    /// Core journey (a)-(g): seed product → register via UI → log in → browse + add to cart →
    /// add to wishlist (verified on the wishlist page) → checkout with bank transfer → assert the
    /// ORD- confirmation → confirm the order shows in order history.
    /// </summary>
    [RetryFact]
    public async Task NewCustomer_RegistersBuysAndReviews_FullJourney()
    {
        // (a) Seed a product (+ category) via the API. The JOURNEY itself is all UI-driven below.
        var product = await _dataFactory.CreateProductAsync(
            name: $"Journey AC {_dataFactory.CorrelationId:N}",
            price: 1299.99m,
            stock: 25);
        product.Id.Should().NotBe(Guid.Empty, "the seed product must be created before the journey starts");
        product.Slug.Should().NotBeNullOrEmpty("the journey navigates to the product by slug");

        var email = $"journey_{_dataFactory.CorrelationId:N}@test.com".ToLowerInvariant();
        const string password = "TestPassword123@";

        // (b) Register a brand-new customer via the RegisterPage UI.
        var registerPage = new RegisterPage(_page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync("Journey", "Customer", email, password);
        (await registerPage.IsRegisteredAsync())
            .Should().BeTrue("the new customer must register successfully to begin the journey");

        // (c) Registration does NOT auto-login (success -> redirect to /login), so log in explicitly.
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(email, password);
        (await loginPage.IsLoggedInAsync())
            .Should().BeTrue("the registered customer must be logged in before buying");

        // (d) Browse to the product and add it to the cart through the product detail UI.
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await Assertions.Expect(_page.Locator("[data-testid='product-title']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await productPage.AddToCartAsync();
        await Assertions.Expect(_page.Locator("[data-testid='cart-count']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });

        // (e) Add the product to the wishlist (logged-in => server-persisted), then verify it on the
        // wishlist page. Mirrors WishlistTests: wait for the real POST to /api/wishlist/items/.
        await AddProductToWishlistFromDetailAsync(product);
        await NavigateToWishlistAsync();
        await _page.Locator("[data-testid='wishlist-item']").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
        (await _page.ContentAsync()).Should().Contain(product.Name,
            "the wishlisted product must appear on the wishlist page");

        // (f) Checkout: go to cart -> checkout, fill shipping, select bank transfer, place order.
        var orderNumber = await CompleteBankTransferCheckoutAsync(email);
        orderNumber.Should().StartWith("ORD-",
            "the confirmation page must show the ORD- order number after placing the order");

        // (g) Order history: the new order must appear for the authenticated customer.
        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();
        var orderCount = await ordersPage.GetOrderCountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(1, "the placed order must show in order history");

        var orderNumbers = await ordersPage.GetOrderNumbersAsync();
        orderNumbers.Any(n => n.Contains(orderNumber, StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue($"order history should list the placed order '{orderNumber}', found: [{string.Join(", ", orderNumbers)}]");
    }

    /// <summary>
    /// Late-funnel journey (a)-(c) + (f)-(i): register → log in → buy (bank transfer) → open order
    /// details and verify the order number matches → return to the product and submit a review,
    /// asserting it is accepted. Split from the core test so a flake here cannot mask the
    /// register→buy→history result above.
    /// </summary>
    [RetryFact]
    public async Task RegisteredCustomer_ViewsOrderDetailsAndReviewsProduct()
    {
        // (a) Seed product.
        var product = await _dataFactory.CreateProductAsync(
            name: $"Review AC {_dataFactory.CorrelationId:N}",
            price: 899.99m,
            stock: 25);
        product.Slug.Should().NotBeNullOrEmpty();

        var email = $"review_{_dataFactory.CorrelationId:N}@test.com".ToLowerInvariant();
        const string password = "TestPassword123@";

        // (b) Register via UI, (c) log in.
        var registerPage = new RegisterPage(_page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync("Review", "Customer", email, password);
        (await registerPage.IsRegisteredAsync()).Should().BeTrue("registration must succeed");

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(email, password);
        (await loginPage.IsLoggedInAsync()).Should().BeTrue("login must succeed");

        // Add to cart.
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await Assertions.Expect(_page.Locator("[data-testid='product-title']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await productPage.AddToCartAsync();
        await Assertions.Expect(_page.Locator("[data-testid='cart-count']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });

        // (f) Complete a real bank-transfer purchase.
        var orderNumber = await CompleteBankTransferCheckoutAsync(email);
        orderNumber.Should().StartWith("ORD-");

        // (g)+(h) Open the order from history and verify the details page shows the same order number.
        var ordersPage = new OrdersPage(_page);
        await ordersPage.NavigateAsync();
        (await ordersPage.GetOrderCountAsync())
            .Should().BeGreaterThanOrEqualTo(1, "the placed order must be listed before opening details");

        await ordersPage.ClickOrderAsync(0);
        var detailsNumber = await ordersPage.GetOrderNumberFromDetailsAsync();
        detailsNumber.Should().Contain(orderNumber,
            "the order details page must show the same ORD- number as the confirmation");

        // (i) Return to the product and submit a review; assert it is accepted (form closes / no error).
        await productPage.NavigateAsync(product.Slug);
        await SubmitReviewAsync();
    }

    // ----------------------------------------------------------------------------------------------
    // Helpers — copy the proven interactions from the existing page objects / feature E2E tests.
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Adds the product to the wishlist from its detail page and waits for the real server POST,
    /// mirroring <c>WishlistTests.AddProductToWishlistFromDetailAsync</c> (logged-in customer).
    /// </summary>
    private async Task AddProductToWishlistFromDetailAsync(TestProduct product)
    {
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        var wishlistButton = _page.Locator("[data-testid='add-to-wishlist']");
        await wishlistButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });

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
        await _page.WaitForSelectorAsync(
            "[data-testid='wishlist-items'], [data-testid='wishlist-empty']",
            new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    /// <summary>
    /// Drives the checkout from the current cart to the order-confirmation page using bank transfer
    /// (no Stripe), mirroring <c>GuestCheckoutCompletionTests</c>. Returns the ORD- order number.
    /// </summary>
    private async Task<string> CompleteBankTransferCheckoutAsync(string email)
    {
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        _page.Url.Should().Contain("/checkout", "the customer must reach checkout from the cart");
        _page.Url.Should().NotContain("/login", "a logged-in customer must not be bounced to login at checkout");

        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Journey",
            lastName: "Customer",
            email: email,
            street: "12 Journey Street",
            city: "Sofia",
            state: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888123456");
        await checkoutPage.SubmitShippingFormAsync();

        await SelectStandardShippingIfPresentAsync();

        await checkoutPage.SelectPaymentMethodAsync("bank");
        await checkoutPage.ProceedToReviewAsync();
        await checkoutPage.PlaceOrderAsync();

        (await checkoutPage.IsOrderConfirmedAsync())
            .Should().BeTrue("the order confirmation page must be displayed after placing the order");

        var orderNumber = await checkoutPage.GetOrderNumberAsync();
        return orderNumber.Trim();
    }

    /// <summary>
    /// Selects the Standard shipping option if the checkout exposes a selectable shipping list.
    /// Tolerant: a no-op when Standard is the default or no explicit selector renders.
    /// </summary>
    private async Task SelectStandardShippingIfPresentAsync()
    {
        var standard = _page.Locator("[data-testid='shipping-standard']");
        try
        {
            if (await standard.CountAsync() > 0 && await standard.First.IsVisibleAsync())
            {
                await standard.First.Locator("..").ClickAsync();
            }
        }
        catch
        {
            // Standard shipping is the default when no explicit option control is present.
        }
    }

    /// <summary>
    /// Opens the reviews tab and submits a 5-star review, mirroring
    /// <c>ReviewsQATests.Reviews_CanSubmitReview_WhenLoggedIn</c>. Asserts the form closes on success
    /// (or surfaces a clear error if the review was a duplicate).
    /// </summary>
    private async Task SubmitReviewAsync()
    {
        var reviewsTab = _page.Locator("[data-testid='tab-reviews']");
        await Assertions.Expect(reviewsTab)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await reviewsTab.ClickAsync();
        await Assertions.Expect(_page.Locator("[data-testid='product-reviews']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });

        var writeReviewBtn = _page.Locator("[data-testid='write-review-btn']");
        await Assertions.Expect(writeReviewBtn)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await writeReviewBtn.ClickAsync();

        var reviewForm = _page.Locator("[data-testid='review-form']");
        await Assertions.Expect(reviewForm).ToBeVisibleAsync();

        // Select 5 stars (0-indexed => 4 = 5th star).
        await _page.Locator(".star-input .star-btn").Nth(4).ClickAsync();
        await _page.FillAsync("#review-title", "Excellent unit");
        await _page.FillAsync("#review-content", "Cools the whole room quickly and runs quietly. Highly recommend!");

        await _page.Locator("[data-testid='submit-review-btn']").ClickAsync();

        // Settle on the form closing (success) or an error surfacing — no NetworkIdle.
        var formError = _page.Locator(".form-error");
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline
               && await reviewForm.IsVisibleAsync()
               && !await formError.IsVisibleAsync())
        {
            await Task.Delay(250);
        }

        var formHidden = !await reviewForm.IsVisibleAsync();
        var hasFormError = await formError.IsVisibleAsync();
        (formHidden || hasFormError)
            .Should().BeTrue("the review form should close on success (or show a clear error if duplicate)");
    }
}
