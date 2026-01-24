using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public class CartPage : BasePage
{
    private const string CartItem = "[data-testid='cart-item']";
    private const string CartTotal = "[data-testid='cart-total']";
    private const string CheckoutButton = "[data-testid='proceed-to-checkout']";
    private const string RemoveItemButton = "[data-testid='remove-item']";
    private const string QuantityInput = "[data-testid='item-quantity'] input";
    private const string EmptyCartMessage = "[data-testid='empty-cart']";
    private const string ContinueShoppingButton = "[data-testid='continue-shopping']";

    public CartPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/cart");
        await WaitForLoadAsync();
        // Wait for cart page to fully load (either empty message or cart items)
        try
        {
            await Page.WaitForSelectorAsync($"{EmptyCartMessage}, {CartItem}, {CartTotal}", new PageWaitForSelectorOptions { Timeout = 5000 });
        }
        catch { }
    }

    public async Task<decimal> GetTotalAsync()
    {
        var totalText = await GetTextAsync(CartTotal);
        // Handle various currency formats: $360.00, €360.00, EUR360.00, 360.00 EUR, etc.
        var cleaned = totalText
            .Replace("$", "")
            .Replace("€", "")
            .Replace("EUR", "")
            .Replace("BGN", "")
            .Replace(",", "")
            .Trim();
        return decimal.Parse(cleaned, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int> GetItemCountAsync()
    {
        var items = await Page.QuerySelectorAllAsync(CartItem);
        return items.Count;
    }

    public async Task ProceedToCheckoutAsync()
    {
        // First ensure we have items in cart
        var itemCount = await GetItemCountAsync();
        if (itemCount == 0)
        {
            throw new InvalidOperationException("Cannot proceed to checkout with empty cart");
        }

        // Wait for checkout button to be visible
        await Page.WaitForSelectorAsync(CheckoutButton, new PageWaitForSelectorOptions { Timeout = 5000 });

        await ClickAsync(CheckoutButton);
        // Wait for navigation to complete
        await Page.WaitForURLAsync(url => url.Contains("checkout") || url.Contains("login"), new PageWaitForURLOptions { Timeout = 10000 });
        await WaitForLoadAsync();

        // Wait for checkout page content to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task RemoveItemAsync(int index = 0)
    {
        var itemCountBefore = await GetItemCountAsync();
        var removeButtons = await Page.QuerySelectorAllAsync(RemoveItemButton);
        if (removeButtons.Count > index)
        {
            // Set up dialog handler to accept the confirmation before clicking
            Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

            await removeButtons[index].ClickAsync();

            // Wait for item count to decrease or for empty cart message
            if (itemCountBefore == 1)
            {
                await Page.WaitForSelectorAsync(EmptyCartMessage, new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            else
            {
                // Wait for item to be removed
                await Page.WaitForFunctionAsync(
                    $"document.querySelectorAll('[data-testid=\"cart-item\"]').length < {itemCountBefore}",
                    new PageWaitForFunctionOptions { Timeout = 10000 }
                );
            }
        }
    }

    public async Task UpdateQuantityAsync(int index, int quantity)
    {
        var quantityInputs = await Page.QuerySelectorAllAsync(QuantityInput);
        if (quantityInputs.Count > index)
        {
            var input = quantityInputs[index];

            // Triple-click to select all, then type new value
            await input.ClickAsync(new ElementHandleClickOptions { ClickCount = 3 });
            await input.TypeAsync(quantity.ToString());

            // Trigger the change event by dispatching blur event
            await input.DispatchEventAsync("change", new { });
            await input.DispatchEventAsync("blur", new { });

            // Wait for network to settle after the update
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public async Task<bool> IsEmptyAsync()
    {
        return await IsVisibleAsync(EmptyCartMessage);
    }

    public async Task ContinueShoppingAsync()
    {
        // Wait for cart to load and the button to appear
        await Page.WaitForSelectorAsync(ContinueShoppingButton, new PageWaitForSelectorOptions { Timeout = 5000 });
        await ClickAsync(ContinueShoppingButton);
        await Page.WaitForURLAsync(url => url.Contains("products"), new PageWaitForURLOptions { Timeout = 10000 });
        await WaitForLoadAsync();
    }
}
