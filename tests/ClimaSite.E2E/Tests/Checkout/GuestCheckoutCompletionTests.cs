using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Checkout;

/// <summary>
/// SLICE D full guest-purchase coverage (GAP-07). A guest buyer completes a real bank-transfer
/// purchase end-to-end and lands on the order-confirmation page with an ORD- number and bank-transfer
/// instructions. The confirmation is then re-opened in a FRESH anonymous browser context using the
/// guest access token, proving the token-gated lookup loads the order without authentication
/// (SEC-02 intact). NO MOCKING — real product, real cart, real order, real confirmation.
/// </summary>
[Collection("Playwright")]
public class GuestCheckoutCompletionTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public GuestCheckoutCompletionTests(PlaywrightFixture fixture)
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
    public async Task GuestCheckout_BankTransfer_CompletesAndConfirmationSurvivesReload()
    {
        // Arrange - a guest (no login) adds a product to the cart through the real product UI.
        var product = await _dataFactory.CreateProductAsync(name: "Guest Checkout AC", price: 1349.99m, stock: 20);
        var guestEmail = $"guest_{_dataFactory.CorrelationId:N}@test.com";

        // The confirmation page strips the guest access token from the URL on first load (GAP-07), so
        // capture the token-bearing confirmation URL the moment the SPA router pushes it, before the
        // history entry is replaced. This init script records it into a window global we read later.
        await _page.AddInitScriptAsync(@"
            (() => {
                const record = (url) => {
                    try {
                        const abs = new URL(url, window.location.origin).toString();
                        if (abs.includes('/checkout/confirmation/') && abs.includes('token=')) {
                            window.__guestConfirmationUrl = abs;
                        }
                    } catch (e) { /* ignore */ }
                };
                const push = history.pushState;
                history.pushState = function (s, t, u) { if (u) record(u); return push.apply(this, arguments); };
            })();");

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        await Assertions.Expect(_page.Locator("[data-testid='cart-count']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Act - proceed to checkout as a guest. GAP-07 removed the /checkout auth guard, so the buyer
        // reaches checkout without being bounced to login.
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        _page.Url.Should().Contain("/checkout");
        _page.Url.Should().NotContain("/login", "guest checkout must not require login (GAP-07)");

        // Fill shipping with the guest email, choose Standard shipping + Bank Transfer, place the order.
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Guest",
            lastName: "Buyer",
            email: guestEmail,
            street: "5 Guest Lane",
            city: "Sofia",
            state: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888000111"
        );
        await checkoutPage.SubmitShippingFormAsync();

        await SelectStandardShippingIfPresentAsync();

        await checkoutPage.SelectPaymentMethodAsync("bank");
        await checkoutPage.ProceedToReviewAsync();
        await checkoutPage.PlaceOrderAsync();

        // Assert - confirmation page renders with an ORD- order number and bank-transfer instructions.
        (await checkoutPage.IsOrderConfirmedAsync())
            .Should().BeTrue("Order confirmation should be displayed for the guest");

        var orderNumber = await checkoutPage.GetOrderNumberAsync();
        orderNumber.Should().StartWith("ORD-", "the confirmation should show the ORD- order number");

        (await checkoutPage.IsBankTransferInstructionsVisibleAsync())
            .Should().BeTrue("Bank-transfer instructions should be shown on the guest confirmation");
        var reference = await checkoutPage.GetBankTransferReferenceAsync();
        reference.Should().StartWith("ORD-", "the bank-transfer reference is the order number");

        // Capture the token-bearing confirmation URL recorded by the init script during navigation.
        var confirmationUrl = await _page.EvaluateAsync<string?>("() => window.__guestConfirmationUrl || null");
        confirmationUrl.Should().NotBeNullOrEmpty(
            "the guest confirmation URL must carry the access token so it can be re-opened anonymously");
        confirmationUrl!.Should().Contain("token=");

        // Act 2 - re-open the confirmation in a FRESH anonymous context (no cart, no auth, no shared
        // storage). The token-gated lookup must still resolve the order (SEC-02).
        var freshContext = await _page.Context.Browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _fixture.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        try
        {
            var freshPage = await freshContext.NewPageAsync();
            freshPage.SetDefaultTimeout(30000);

            await freshPage.GotoAsync(confirmationUrl);
            await freshPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - the order details load for the anonymous visitor (no login redirect, no error).
            await Assertions.Expect(freshPage.Locator("[data-testid='order-confirmation-page']")).ToBeVisibleAsync(
                new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });
            await Assertions.Expect(freshPage.Locator("[data-testid='order-number']")).ToBeVisibleAsync(
                new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });

            var reloadedNumber = await freshPage.Locator("[data-testid='order-number']").TextContentAsync();
            (reloadedNumber ?? string.Empty).Trim().Should().Be(orderNumber.Trim(),
                "the same order must load via the guest token in a fresh anonymous context");

            freshPage.Url.Should().NotContain("/login",
                "the token-gated lookup must not require authentication");
            (await freshPage.Locator("[data-testid='error-state']").IsVisibleAsync())
                .Should().BeFalse("the guest order lookup should succeed, not error out");
        }
        finally
        {
            await freshContext.CloseAsync();
        }
    }

    /// <summary>
    /// Selects the Standard shipping option if the checkout exposes a selectable shipping list.
    /// Tolerant: if Standard is already the default (or no explicit selector renders) this is a no-op.
    /// </summary>
    private async Task SelectStandardShippingIfPresentAsync()
    {
        var standard = _page.Locator("[data-testid='shipping-standard']");
        try
        {
            if (await standard.CountAsync() > 0 && await standard.First.IsVisibleAsync())
            {
                // The radio input is style-hidden; click the surrounding label like the payment method does.
                await standard.First.Locator("..").ClickAsync();
            }
        }
        catch
        {
            // Standard shipping is the default when no explicit option control is present.
        }
    }
}
