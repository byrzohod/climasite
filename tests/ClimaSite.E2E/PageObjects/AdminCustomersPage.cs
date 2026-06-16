using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the Admin Customers (users) list and the customer detail panel.
/// Mirrors AdminOrdersPage: navigation waits on real selectors, no arbitrary sleeps.
/// The admin users route is /admin/users; the component renders as app-admin-users.
/// </summary>
public class AdminCustomersPage : BasePage
{
    public AdminCustomersPage(IPage page) : base(page) { }

    public async Task NavigateToListAsync()
    {
        await Page.GotoAsync("/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync(
            "[data-testid='customer-row'], [data-testid='customers-empty'], [data-testid='customers-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task<int> GetCustomerRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync("[data-testid='customer-row']");
        return rows.Count;
    }

    public async Task SearchAsync(string query)
    {
        await Page.FillAsync("[data-testid='customer-search']", query);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync(
            "[data-testid='customer-row'], [data-testid='customers-empty'], [data-testid='customers-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public ILocator CustomerRow(string customerId) =>
        Page.Locator($"[data-testid='customer-row'][data-customer-id='{customerId}']");

    public async Task<bool> HasCustomerRowAsync(string customerId)
    {
        var row = await Page.QuerySelectorAsync(
            $"[data-testid='customer-row'][data-customer-id='{customerId}']");
        return row != null;
    }

    /// <summary>
    /// Opens the customer detail modal panel and waits for it to render and finish loading.
    /// </summary>
    public async Task OpenDetailAsync(string customerId)
    {
        var selector = $"[data-testid='view-customer'][data-customer-id='{customerId}']";
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync(selector);
        await Page.WaitForSelectorAsync(
            "[data-testid='customer-detail']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // The status badge only renders once the detail finishes loading without error.
        await Page.WaitForSelectorAsync(
            "[data-testid='customer-active-badge'], [data-testid='customer-detail-error']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public ILocator DetailPanel => Page.Locator("[data-testid='customer-detail']");

    public ILocator ActiveBadge => Page.Locator("[data-testid='customer-active-badge']");

    public async Task<string> GetActiveBadgeTextAsync()
    {
        var badge = await Page.QuerySelectorAsync("[data-testid='customer-active-badge']");
        if (badge == null) return string.Empty;
        return (await badge.TextContentAsync() ?? string.Empty).Trim();
    }

    /// <summary>
    /// Clicks the status toggle in the detail panel and waits for the action to settle.
    /// </summary>
    public async Task ToggleStatusAsync()
    {
        await Page.WaitForSelectorAsync(
            "[data-testid='toggle-customer-status']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync("[data-testid='toggle-customer-status']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
