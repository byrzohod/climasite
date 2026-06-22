using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Cart;

/// <summary>
/// SLICE D end-to-end purchase driven entirely from the header mini-cart drawer: add a product,
/// open the mini-cart, click its "Checkout" action, then complete a real bank-transfer purchase
/// through to the order-confirmation page. This exercises the exact path a shopper takes from the
/// drawer (the surface of the reported overlay bug) all the way to a confirmed order.
/// NO MOCKING — real product, real cart, real order.
/// </summary>
[Collection("Playwright")]
public class MiniCartPurchaseTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public MiniCartPurchaseTests(PlaywrightFixture fixture)
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
    public async Task MiniCart_CheckoutAction_DrivesBankTransferPurchaseToConfirmation()
    {
        // Arrange - create a product and add it to the cart through the real product-detail UI.
        var product = await _dataFactory.CreateProductAsync(name: "Mini-Cart Purchase AC", price: 1599.99m, stock: 30);
        var buyerEmail = $"minicart_{_dataFactory.CorrelationId:N}@test.com";

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        await Assertions.Expect(_page.Locator("[data-testid='cart-count']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });

        // Act - open the mini-cart drawer and start checkout from inside it (real, non-forced click).
        var drawer = new MiniCartDrawer(_page);
        await drawer.OpenAsync();
        (await drawer.IsOpenAsync()).Should().BeTrue("Mini-cart drawer should open from the header cart icon");

        await _page.ClickAsync(MiniCartDrawer.CheckoutButton);
        await _page.WaitForURLAsync(u => u.Contains("/checkout"),
            new PageWaitForURLOptions { Timeout = 15000 });
        _page.Url.Should().Contain("/checkout");

        // Complete a bank-transfer purchase (no Stripe needed).
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "MiniCart",
            lastName: "Buyer",
            email: buyerEmail,
            street: "9 Drawer Street",
            city: "Plovdiv",
            state: "Plovdiv",
            postalCode: "4000",
            country: "Bulgaria",
            phone: "+359888222333"
        );
        await checkoutPage.SubmitShippingFormAsync();

        await checkoutPage.SelectPaymentMethodAsync("bank");
        await checkoutPage.ProceedToReviewAsync();
        await checkoutPage.PlaceOrderAsync();

        // Assert - the order is confirmed with an ORD- number and bank-transfer instructions.
        (await checkoutPage.IsOrderConfirmedAsync())
            .Should().BeTrue("The mini-cart-driven purchase should reach the confirmation page");

        var orderNumber = await checkoutPage.GetOrderNumberAsync();
        orderNumber.Should().StartWith("ORD-", "the confirmation should show the ORD- order number");

        (await checkoutPage.IsBankTransferInstructionsVisibleAsync())
            .Should().BeTrue("Bank-transfer instructions should appear on the confirmation");
    }
}
