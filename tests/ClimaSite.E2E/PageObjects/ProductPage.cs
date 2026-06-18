using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public class ProductPage : BasePage
{
    private const string ProductTitle = "[data-testid='product-title']";
    private const string ProductPrice = "[data-testid='product-price']";
    private const string AddToCartButton = "[data-testid='product-detail'] [data-testid='add-to-cart']";
    private const string QuantityInput = "[data-testid='product-detail'] [data-testid='quantity-input']";
    private const string CartNotification = "[data-testid='cart-notification']";
    private const string ProductCard = "[data-testid='product-card']";
    private const string ProductName = "[data-testid='product-name']";

    public ProductPage(IPage page) : base(page) { }

    public async Task NavigateAsync(string slug)
    {
        await Page.GotoAsync($"/products/{slug}");
        await WaitForLoadAsync();
        await Page.WaitForSelectorAsync(
            $"{ProductTitle}, [data-testid='error']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task NavigateByIdAsync(Guid productId)
    {
        await Page.GotoAsync($"/products/{productId}");
        await WaitForLoadAsync();
        await Page.WaitForSelectorAsync(
            $"{ProductTitle}, [data-testid='error']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task NavigateToListAsync()
    {
        await Page.GotoAsync("/products");
        await WaitForLoadAsync();
        // Wait for product cards to appear or empty state
        try
        {
            await Page.WaitForSelectorAsync($"{ProductCard}, .empty-state", new PageWaitForSelectorOptions { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Products may still be loading, continue anyway
        }
    }

    public async Task<string> GetProductTitleAsync()
    {
        return await GetTextAsync(ProductTitle);
    }

    public async Task<decimal> GetProductPriceAsync()
    {
        var priceText = await GetTextAsync(ProductPrice);
        // Handle multiple currency symbols
        var cleanedPrice = priceText
            .Replace("$", "")
            .Replace("€", "")
            .Replace("лв", "") // Bulgarian Lev
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();
        return decimal.TryParse(cleanedPrice, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    public async Task AddToCartAsync(int quantity = 1)
    {
        // Wait for the add to cart button to be ready
        await Page.WaitForSelectorAsync(AddToCartButton, new PageWaitForSelectorOptions { Timeout = 5000 });

        if (quantity > 1)
        {
            await FillAsync(QuantityInput, quantity.ToString());
        }
        await ClickAsync(AddToCartButton);

        // Wait for either notification or button state change (added state)
        try
        {
            await Page.WaitForSelectorAsync($"{CartNotification}, {AddToCartButton}.added", new PageWaitForSelectorOptions { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Cart notification might not show; prefer to wait for the header cart badge — but stay
            // best-effort: on out-of-stock / overselling paths the cart legitimately does NOT update,
            // and callers assert the resulting state themselves. Must not throw here.
            try
            {
                await Page.WaitForSelectorAsync("[data-testid='cart-count']", new PageWaitForSelectorOptions { Timeout = 5000 });
            }
            catch (TimeoutException)
            {
                // Tolerated — no cart confirmation appeared (e.g. add was rejected).
            }
        }
    }

    public async Task<int> GetProductCardCountAsync()
    {
        // Tolerant settle: a rendered list OR an empty state both count as "settled" (0 is valid).
        await Page.WaitForSelectorAsync($"{ProductCard}, .empty-state", new PageWaitForSelectorOptions { Timeout = 10000 });
        var cards = await Page.QuerySelectorAllAsync(ProductCard);
        return cards.Count;
    }

    public async Task<bool> HasProductWithNameAsync(string name)
    {
        var pageContent = await Page.ContentAsync();
        return pageContent.Contains(name);
    }
}
