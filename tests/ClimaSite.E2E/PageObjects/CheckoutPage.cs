using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public class CheckoutPage : BasePage
{
    // Shipping form selectors - target input inside wrapper components
    private const string FirstNameInput = "[data-testid='shipping-firstname'] input";
    private const string LastNameInput = "[data-testid='shipping-lastname'] input";
    private const string EmailInput = "[data-testid='shipping-email'] input";
    private const string AddressLine1Input = "[data-testid='shipping-street'] input";
    private const string AddressLine2Input = "[data-testid='shipping-apartment'] input";
    private const string CityInput = "[data-testid='shipping-city'] input";
    private const string StateInput = "[data-testid='shipping-state'] input";
    private const string PostalCodeInput = "[data-testid='shipping-postal-code'] input";
    private const string CountrySelect = "[data-testid='shipping-country'] select";
    private const string PhoneInput = "[data-testid='shipping-phone'] input";

    // Payment selectors - target input inside wrapper components
    private const string CardNumberInput = "[data-testid='card-number'] input";
    private const string CardExpiryInput = "[data-testid='card-expiry'] input";
    private const string CardCvvInput = "[data-testid='card-cvv'] input";

    // Actions
    private const string PlaceOrderButton = "[data-testid='place-order']";
    private const string NextStepButton = "[data-testid='next-step']";
    private const string PreviousStepButton = "[data-testid='previous-step']";

    // Confirmation
    private const string OrderConfirmation = "[data-testid='order-confirmation']";
    private const string OrderNumber = "[data-testid='order-number']";

    public CheckoutPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/checkout");
        await WaitForLoadAsync();
    }

    public async Task FillShippingAddressAsync(
        string street,
        string city,
        string postalCode,
        string? country = null,
        string? phone = null,
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? state = null)
    {
        // Debug: Check if we're on the right page
        var currentUrl = Page.Url;
        if (!currentUrl.Contains("checkout"))
        {
            throw new InvalidOperationException($"Expected to be on checkout page, but current URL is: {currentUrl}");
        }

        // Wait for the checkout page container to appear first
        try
        {
            await Page.WaitForSelectorAsync("[data-testid='checkout-page']", new PageWaitForSelectorOptions { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Get more debug info
            var html = await Page.ContentAsync();
            var hasAngularApp = html.Contains("app-root");
            var pageTitle = await Page.TitleAsync();
            throw new InvalidOperationException($"Checkout page container not found. URL: {currentUrl}, Title: {pageTitle}, Has app-root: {hasAngularApp}");
        }

        // Wait for network to be idle (cart data might still be loading)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait a bit more for Angular to settle (cart service loads asynchronously)
        await Task.Delay(500);

        // Wait for checkout page to be fully rendered
        // Check both possible states: shipping form (has cart items) or empty cart message
        try
        {
            await Page.WaitForSelectorAsync("[data-testid='shipping-section'], [data-testid='checkout-empty'], [data-testid='checkout-steps']", new PageWaitForSelectorOptions { Timeout = 15000 });
        }
        catch (TimeoutException)
        {
            // Debug: capture what's on the page
            var pageContent = await Page.ContentAsync();
            var hasCheckoutPage = pageContent.Contains("checkout-page");
            var hasTranslations = pageContent.Contains("checkout.title");

            throw new InvalidOperationException($"Checkout page did not render - neither shipping form nor empty cart message visible. " +
                $"Has checkout-page: {hasCheckoutPage}, Has translations: {hasTranslations}");
        }

        // Check if empty cart is shown
        var emptyCartVisible = await Page.IsVisibleAsync("[data-testid='checkout-empty']");
        if (emptyCartVisible)
        {
            throw new InvalidOperationException("Checkout page shows empty cart - cart data may not have persisted during navigation");
        }

        // Wait specifically for the shipping form input
        await Page.WaitForSelectorAsync("[data-testid='shipping-firstname'] input", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Fill required fields with defaults if not provided
        await FillAsync(FirstNameInput, firstName ?? "Test");
        await FillAsync(LastNameInput, lastName ?? "User");
        await FillAsync(EmailInput, email ?? "test@example.com");
        await FillAsync(AddressLine1Input, street);
        await FillAsync(CityInput, city);
        await FillAsync(StateInput, state ?? city); // Use city as state fallback
        await FillAsync(PostalCodeInput, postalCode);

        if (country != null)
        {
            await Page.SelectOptionAsync(CountrySelect, country);
        }

        if (phone != null)
        {
            await FillAsync(PhoneInput, phone);
        }
    }

    public async Task FillPaymentDetailsAsync(
        string cardNumber,
        string expiry,
        string cvv)
    {
        await FillAsync(CardNumberInput, cardNumber);
        await FillAsync(CardExpiryInput, expiry);
        await FillAsync(CardCvvInput, cvv);
    }

    public async Task GoToNextStepAsync()
    {
        await ClickAsync(NextStepButton);
        await WaitForLoadAsync();
    }

    public async Task GoToPreviousStepAsync()
    {
        await ClickAsync(PreviousStepButton);
        await WaitForLoadAsync();
    }

    public async Task PlaceOrderAsync()
    {
        await ClickAsync(PlaceOrderButton);
        await Page.WaitForSelectorAsync(OrderConfirmation, new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task<string> GetOrderNumberAsync()
    {
        return await GetTextAsync(OrderNumber);
    }

    public async Task<bool> IsOrderConfirmedAsync()
    {
        return await IsVisibleAsync(OrderConfirmation);
    }

    public async Task<bool> IsOnCheckoutPageAsync()
    {
        return Page.Url.Contains("checkout");
    }

    public async Task SubmitShippingFormAsync()
    {
        await ClickAsync(NextStepButton);
        // Wait for payment section to appear instead of NetworkIdle
        await Page.WaitForSelectorAsync("[data-testid='payment-section']", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task<bool> IsOnPaymentStepAsync()
    {
        // Check for payment section container
        return await IsVisibleAsync("[data-testid='payment-section']");
    }

    public async Task SelectPaymentMethodAsync(string method)
    {
        // method can be: card, paypal, bank
        // The input is hidden (styled), so click on the parent label element
        await Page.Locator($"[data-testid='payment-{method}']").Locator("..").ClickAsync();
        await Task.Delay(100); // Wait for UI to update
    }

    public async Task ProceedToReviewAsync()
    {
        await ClickAsync(NextStepButton);
        // Wait for review section to appear instead of NetworkIdle
        await Page.WaitForSelectorAsync("[data-testid='review-section'], [data-testid='place-order']", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task<decimal> GetOrderTotalAsync()
    {
        var totalText = await GetTextAsync("[data-testid='order-total']");
        // Remove currency symbols and formatting (supports $, €, EUR, BGN, etc.)
        var cleanedText = System.Text.RegularExpressions.Regex.Replace(totalText, @"[^\d.,]", "").Trim();
        // Handle European number format (1.234,56) vs US format (1,234.56)
        // If both comma and dot exist, determine which is decimal separator
        if (cleanedText.Contains(",") && cleanedText.Contains("."))
        {
            // Last separator is the decimal separator
            var lastComma = cleanedText.LastIndexOf(',');
            var lastDot = cleanedText.LastIndexOf('.');
            if (lastComma > lastDot)
            {
                // European format: 1.234,56
                cleanedText = cleanedText.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // US format: 1,234.56
                cleanedText = cleanedText.Replace(",", "");
            }
        }
        else if (cleanedText.Contains(","))
        {
            // Could be European decimal (123,45) or US thousands (1,234)
            // If comma has exactly 2 digits after it at the end, treat as decimal
            var commaIdx = cleanedText.LastIndexOf(',');
            if (commaIdx >= 0 && cleanedText.Length - commaIdx - 1 == 2)
            {
                cleanedText = cleanedText.Replace(",", ".");
            }
            else
            {
                cleanedText = cleanedText.Replace(",", "");
            }
        }
        return decimal.TryParse(cleanedText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var total) ? total : 0;
    }

    public async Task GoBackToCartAsync()
    {
        await Page.ClickAsync("[data-testid='back-to-cart']");
        await WaitForLoadAsync();
    }
}
