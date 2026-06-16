using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the Admin Orders list and Admin Order detail pages.
/// </summary>
public class AdminOrdersPage : BasePage
{
    public AdminOrdersPage(IPage page) : base(page) { }

    public async Task NavigateToListAsync()
    {
        await Page.GotoAsync("/admin/orders");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync(
            "[data-testid='order-row'], [data-testid='orders-empty'], [data-testid='orders-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task<int> GetOrderRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync("[data-testid='order-row']");
        return rows.Count;
    }

    public async Task FilterByStatusAsync(string statusValue)
    {
        await Page.SelectOptionAsync("[data-testid='status-filter']", new SelectOptionValue { Value = statusValue });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchAsync(string query)
    {
        await Page.FillAsync("[data-testid='order-search']", query);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenOrderAsync(string orderId)
    {
        var selector = $"[data-testid='view-order'][data-order-id='{orderId}']";
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync(selector);
        await Page.WaitForSelectorAsync(
            "[data-testid='admin-order-detail']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenOrderDirectAsync(string orderId)
    {
        await Page.GotoAsync($"/admin/orders/{orderId}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync(
            "[data-testid='admin-order-detail']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task<string> GetStatusBadgeTextAsync()
    {
        var badge = await Page.QuerySelectorAsync("[data-testid='order-status-badge']");
        if (badge == null) return string.Empty;
        return (await badge.TextContentAsync() ?? string.Empty).Trim();
    }

    public async Task ChangeStatusAsync(string statusValue, string? note = null, bool notifyCustomer = false)
    {
        await Page.WaitForSelectorAsync("[data-testid='status-select']", new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.SelectOptionAsync("[data-testid='status-select']", new SelectOptionValue { Value = statusValue });

        if (!string.IsNullOrEmpty(note))
        {
            await Page.FillAsync("[data-testid='status-note']", note);
        }

        var notifyChecked = await Page.IsCheckedAsync("[data-testid='notify-customer']");
        if (notifyChecked != notifyCustomer)
        {
            await Page.SetCheckedAsync("[data-testid='notify-customer']", notifyCustomer);
        }

        await Page.ClickAsync("[data-testid='apply-status']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SetShippingAsync(string trackingNumber, string? shippingMethod = null, bool markAsShipped = true)
    {
        await Page.WaitForSelectorAsync("[data-testid='tracking-input']", new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.FillAsync("[data-testid='tracking-input']", trackingNumber);

        if (!string.IsNullOrEmpty(shippingMethod))
        {
            await Page.FillAsync("[data-testid='shipping-method-input']", shippingMethod);
        }

        await Page.SetCheckedAsync("[data-testid='mark-shipped']", markAsShipped);
        await Page.ClickAsync("[data-testid='save-shipping']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task AddNoteAsync(string note)
    {
        await Page.WaitForSelectorAsync("[data-testid='note-input']", new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.FillAsync("[data-testid='note-input']", note);
        await Page.ClickAsync("[data-testid='add-note']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<string> GetTrackingNumberAsync()
    {
        var tracking = await Page.QuerySelectorAsync("[data-testid='order-tracking-number']");
        if (tracking == null) return string.Empty;
        return (await tracking.TextContentAsync() ?? string.Empty).Trim();
    }

    public async Task<bool> HasTrackingNumberAsync()
    {
        var tracking = await Page.QuerySelectorAsync("[data-testid='order-tracking-number']");
        return tracking != null;
    }
}
