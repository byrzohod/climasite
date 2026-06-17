using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Checkout;

/// <summary>
/// Checkout process E2E tests.
/// Tests the complete checkout flow with real data.
/// </summary>
[Collection("Playwright")]
public class CheckoutTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public CheckoutTests(PlaywrightFixture fixture)
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
    public async Task Checkout_AuthenticatedUser_CanProceedToCheckout()
    {
        // Arrange - Create user and add product to cart
        var product = await _dataFactory.CreateProductAsync(name: "Checkout Test AC", price: 1499.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Act
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Assert
        var checkoutPage = new CheckoutPage(_page);
        var isOnCheckout = await checkoutPage.IsOnCheckoutPageAsync();
        isOnCheckout.Should().BeTrue("User should be on checkout page");
    }

    [Fact]
    public async Task Checkout_ShippingForm_ValidatesRequiredFields()
    {
        // Arrange
        var product = await _dataFactory.CreateProductAsync(name: "Validation Test AC", price: 899.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Assert - Submit button should be disabled when form is empty/invalid
        var submitButton = _page.Locator("[data-testid='next-step']");
        var isDisabled = await submitButton.IsDisabledAsync();
        isDisabled.Should().BeTrue("Submit button should be disabled when required fields are empty");
    }

    [Fact]
    public async Task Checkout_CompleteShippingForm_ProceedsToPayment()
    {
        // Arrange
        var product = await _dataFactory.CreateProductAsync(name: "Payment Test AC", price: 1299.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Act - Fill shipping form
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            street: "123 Test Street",
            city: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888123456"
        );

        await checkoutPage.SubmitShippingFormAsync();

        // Assert - Should be on payment step
        var isOnPayment = await checkoutPage.IsOnPaymentStepAsync();
        isOnPayment.Should().BeTrue("Should proceed to payment step");
    }

    [Fact]
    public async Task Checkout_OrderReview_ShowsCorrectTotal()
    {
        // Arrange
        var product = await _dataFactory.CreateProductAsync(name: "Total Test AC", price: 999.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Fill shipping
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            street: "456 Order Street",
            city: "Plovdiv",
            postalCode: "4000",
            country: "Bulgaria",
            phone: "+359888987654"
        );
        await checkoutPage.SubmitShippingFormAsync();

        // Act - Go to review
        await checkoutPage.ProceedToReviewAsync();

        // Assert
        var orderTotal = await checkoutPage.GetOrderTotalAsync();
        orderTotal.Should().BeGreaterThanOrEqualTo(999.99m, "Order total should include product price");
    }

    [Fact]
    public async Task Checkout_GuestUser_CanCheckoutWithEmail()
    {
        // Arrange - Don't login, browse as guest
        var productPage = new ProductPage(_page);
        await productPage.NavigateToListAsync();

        // Add first available product to cart
        await _page.ClickAsync("[data-testid='product-card']:first-child");
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();

        // Act - Proceed to checkout as guest
        await cartPage.ProceedToCheckoutAsync();

        // Assert (TS-13/GAP-07) - guest checkout is enabled: the buyer reaches /checkout WITHOUT
        // being bounced to login. (The previous assertion passed either way and tested nothing.)
        await _page.WaitForURLAsync(u => u.Contains("/checkout"), new PageWaitForURLOptions { Timeout = 15000 });
        _page.Url.Should().Contain("/checkout");
        _page.Url.Should().NotContain("/login");
    }

    [Fact]
    public async Task Checkout_CartModification_CanGoBackAndEdit()
    {
        // Arrange
        var product = await _dataFactory.CreateProductAsync(name: "Edit Cart Test AC", price: 1599.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Act - Go back to cart from checkout
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.GoBackToCartAsync();

        // Assert - Should be back on cart page
        var url = _page.Url;
        url.Should().Contain("cart", "Should be back on cart page");

        // Wait for cart to reload after navigation
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForSelectorAsync("[data-testid='cart-item']", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Check cart has items before modifying
        var itemCount = await cartPage.GetItemCountAsync();
        itemCount.Should().BeGreaterThanOrEqualTo(1, "Cart should have items after navigation back");
    }

    [Fact]
    public async Task Checkout_StockValidation_PreventsOverselling()
    {
        // Arrange - Create product with limited stock
        var product = await _dataFactory.CreateProductAsync(
            name: "Limited Stock AC",
            price: 2499.99m,
            stock: 5
        );
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        // Try to add more than available stock
        await productPage.AddToCartAsync(10);

        // Assert - Should show stock limit message or limit to available
        var hasStockWarning = await _page.IsVisibleAsync("[data-testid='stock-warning']");
        var cartCount = await new HomePage(_page).GetCartCountAsync();

        // Either warning is shown or cart is limited to available stock
        (hasStockWarning || cartCount <= 5).Should().BeTrue(
            "Should either warn about stock or limit to available quantity");
    }

    [Fact]
    public async Task Checkout_SavedAddress_CanBeUsed()
    {
        // Arrange - Create user, product and a saved address
        var product = await _dataFactory.CreateProductAsync(name: "Saved Address Test AC", price: 1299.99m);
        var user = await _dataFactory.CreateUserAsync();

        // Login first
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Go to addresses page and create a saved address
        await _page.GotoAsync($"{_fixture.BaseUrl}/account/addresses");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click add address button
        await _page.ClickAsync("[data-testid='add-address-btn']");
        await _page.WaitForSelectorAsync("[data-testid='address-modal']");

        // Fill in address form
        await _page.FillAsync("[data-testid='address-fullname']", "Test User");
        await _page.FillAsync("[data-testid='address-line1']", "123 Test Street");
        await _page.FillAsync("[data-testid='address-city']", "Sofia");
        await _page.FillAsync("[data-testid='address-postal']", "1000");
        await _page.SelectOptionAsync("[data-testid='address-country']", "Bulgaria");
        await _page.FillAsync("[data-testid='address-phone']", "+359888123456");
        await _page.CheckAsync("[data-testid='address-default']");

        // Save address
        await _page.ClickAsync("[data-testid='save-address-btn']");
        await _page.WaitForSelectorAsync("[data-testid='address-card']");

        // Now add product to cart and go to checkout
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Act - Select the saved address
        await _page.WaitForSelectorAsync("[data-testid='saved-addresses-section']");
        await _page.ClickAsync("[data-testid='saved-address-card']");

        // Assert - Form should be filled with address data
        var firstName = await _page.InputValueAsync("#firstName");
        firstName.Should().Be("Test");

        var city = await _page.InputValueAsync("#city");
        city.Should().Be("Sofia");

        var postalCode = await _page.InputValueAsync("#postalCode");
        postalCode.Should().Be("1000");
    }

    [Fact]
    public async Task Checkout_CompleteOrder_ShowsConfirmation()
    {
        // Arrange - Create user and product
        var product = await _dataFactory.CreateProductAsync(name: "Complete Order Test AC", price: 1799.99m);
        var user = await _dataFactory.CreateUserAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Add product to cart
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        // Go to checkout
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Fill shipping address
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Order",
            lastName: "Test",
            email: user.Email,
            street: "789 Order Complete Street",
            city: "Varna",
            state: "Varna",
            postalCode: "9000",
            country: "Bulgaria",
            phone: "+359888555444"
        );
        await checkoutPage.SubmitShippingFormAsync();

        // Select bank transfer payment method (to avoid Stripe card iframe)
        await checkoutPage.SelectPaymentMethodAsync("bank");

        // Proceed to review
        await checkoutPage.ProceedToReviewAsync();

        // Act - Place order
        await checkoutPage.PlaceOrderAsync();

        // Assert - Should see order confirmation
        var isConfirmed = await checkoutPage.IsOrderConfirmedAsync();
        isConfirmed.Should().BeTrue("Order confirmation should be displayed");

        var orderNumber = await checkoutPage.GetOrderNumberAsync();
        orderNumber.Should().NotBeNullOrEmpty("Order number should be displayed");
    }

    [Fact]
    public async Task Checkout_BankTransfer_ShowsInstructionsWithReferenceOnConfirmation()
    {
        // Arrange - Create user and product (GAP-06: bank transfer is a real, supported method).
        var product = await _dataFactory.CreateProductAsync(name: "Bank Transfer Test AC", price: 1299.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        // Fill shipping address
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Bank",
            lastName: "Buyer",
            email: user.Email,
            street: "12 Wire Transfer Street",
            city: "Sofia",
            state: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888111222"
        );
        await checkoutPage.SubmitShippingFormAsync();

        // Select bank transfer — an info panel with the bank details should appear in checkout.
        await checkoutPage.SelectPaymentMethodAsync("bank");
        var bankInfoVisible = await checkoutPage.IsBankInfoPanelVisibleAsync();
        bankInfoVisible.Should().BeTrue("Bank info panel should be shown when bank transfer is selected");

        // Proceed to review and place the order (no Stripe needed for bank transfer).
        await checkoutPage.ProceedToReviewAsync();
        await checkoutPage.PlaceOrderAsync();

        // Assert - The confirmation page shows the bank-transfer instructions with the reference.
        var instructionsVisible = await checkoutPage.IsBankTransferInstructionsVisibleAsync();
        instructionsVisible.Should().BeTrue("Bank transfer instructions should be displayed on confirmation");

        var reference = await checkoutPage.GetBankTransferReferenceAsync();
        reference.Should().StartWith("ORD-", "The payment reference is the order number");
    }
}
