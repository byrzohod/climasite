using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the Admin Products list and the create/edit form panel.
/// Mirrors AdminOrdersPage: navigation waits on real selectors, no arbitrary sleeps.
/// </summary>
public class AdminProductsPage : BasePage
{
    public AdminProductsPage(IPage page) : base(page) { }

    public async Task NavigateToListAsync()
    {
        await Page.GotoAsync("/admin/products");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Wait for the list, the empty state, or the error state to settle.
        await Page.WaitForSelectorAsync(
            "[data-testid='product-row'], [data-testid='products-empty'], [data-testid='products-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task<int> GetProductRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync("[data-testid='product-row']");
        return rows.Count;
    }

    public async Task SearchAsync(string query)
    {
        await Page.FillAsync("[data-testid='product-search']", query);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Allow the filtered list (or empty/error state) to re-render.
        await Page.WaitForSelectorAsync(
            "[data-testid='product-row'], [data-testid='products-empty'], [data-testid='products-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public ILocator ProductRow(string productId) =>
        Page.Locator($"[data-testid='product-row'][data-product-id='{productId}']");

    public async Task<bool> HasProductRowAsync(string productId)
    {
        var row = await Page.QuerySelectorAsync(
            $"[data-testid='product-row'][data-product-id='{productId}']");
        return row != null;
    }

    /// <summary>
    /// Opens the create-product form panel and waits for it to render.
    /// </summary>
    public async Task OpenCreateFormAsync()
    {
        await Page.ClickAsync("[data-testid='create-product']");
        await Page.WaitForSelectorAsync(
            "[data-testid='product-form']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Fills the create form's required fields and saves. Waits for the panel to close.
    /// </summary>
    public async Task CreateProductAsync(string name, string sku, decimal price)
    {
        await Page.FillAsync("[data-testid='product-name-input']", name);
        await Page.FillAsync("[data-testid='product-sku-input']", sku);
        await Page.FillAsync(
            "[data-testid='product-price-input']",
            price.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await SubmitFormAsync();
    }

    /// <summary>
    /// Opens the edit form for the given product row and waits for the form to hydrate
    /// (the name input is populated from the product detail response).
    /// </summary>
    public async Task OpenEditFormAsync(string productId, string expectedName)
    {
        var selector = $"[data-testid='edit-product'][data-product-id='{productId}']";
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync(selector);
        await Page.WaitForSelectorAsync(
            "[data-testid='product-form']",
            new PageWaitForSelectorOptions { Timeout = 10000 });

        // The form seeds the name immediately from the row, then hydrates from detail.
        // Wait for the name input to carry a value so edits land on a populated form.
        await Assertions.Expect(Page.Locator("[data-testid='product-name-input']"))
            .ToHaveValueAsync(expectedName, new LocatorAssertionsToHaveValueOptions { Timeout = 10000 });
    }

    public async Task SetNameAsync(string name)
    {
        await Page.FillAsync("[data-testid='product-name-input']", name);
    }

    public async Task SubmitFormAsync()
    {
        await Page.ClickAsync("[data-testid='save-product']");
        // On success the panel closes and the list reloads.
        await Page.WaitForSelectorAsync(
            "[data-testid='product-form']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 15000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Clicks the row's deactivate action. Deactivate triggers a window.confirm() in the
    /// component, so the caller must register a dialog handler that accepts BEFORE calling this.
    /// </summary>
    public async Task ClickDeactivateAsync(string productId)
    {
        var selector = $"[data-testid='deactivate-product'][data-product-id='{productId}']";
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync(selector);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public ILocator StatusBadge(string productId) =>
        ProductRow(productId).Locator("[data-testid='product-status-badge']");

    public ILocator ActivateAction(string productId) =>
        Page.Locator($"[data-testid='activate-product'][data-product-id='{productId}']");

    public async Task<string> GetStatusBadgeTextAsync(string productId)
    {
        var badge = await Page.QuerySelectorAsync(
            $"[data-testid='product-row'][data-product-id='{productId}'] [data-testid='product-status-badge']");
        if (badge == null) return string.Empty;
        return (await badge.TextContentAsync() ?? string.Empty).Trim();
    }
}
