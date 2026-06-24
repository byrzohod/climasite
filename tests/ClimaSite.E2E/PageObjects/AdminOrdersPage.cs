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
        // Settle on the re-rendered orders list (or empty/error state) rather than NetworkIdle.
        await Page.WaitForSelectorAsync(
            "[data-testid='order-row'], [data-testid='orders-empty'], [data-testid='orders-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task SearchAsync(string query)
    {
        await Page.FillAsync("[data-testid='order-search']", query);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForSelectorAsync(
            "[data-testid='order-row'], [data-testid='orders-empty'], [data-testid='orders-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task OpenOrderAsync(string orderId)
    {
        var selector = $"[data-testid='view-order'][data-order-id='{orderId}']";
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.ClickAsync(selector);
        await Page.WaitForSelectorAsync(
            "[data-testid='admin-order-detail']",
            new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    public async Task OpenOrderDirectAsync(string orderId)
    {
        await Page.GotoAsync($"/admin/orders/{orderId}");
        await Page.WaitForSelectorAsync(
            "[data-testid='admin-order-detail']",
            new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    public async Task<string> GetStatusBadgeTextAsync()
    {
        await Page.WaitForSelectorAsync("[data-testid='order-status-badge']", new PageWaitForSelectorOptions { Timeout = 30000 });
        return (await Page.Locator("[data-testid='order-status-badge']").InnerTextAsync()).Trim();
    }

    public async Task ChangeStatusAsync(string statusValue, string? note = null, bool notifyCustomer = false)
    {
        await Page.WaitForSelectorAsync("[data-testid='status-select']", new PageWaitForSelectorOptions { Timeout = 30000 });
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
        // No NetworkIdle: callers assert the updated status badge via web-first Expect(...).
    }

    public async Task SetShippingAsync(string trackingNumber, string? shippingMethod = null, bool markAsShipped = true)
    {
        await Page.WaitForSelectorAsync("[data-testid='tracking-input']", new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.FillAsync("[data-testid='tracking-input']", trackingNumber);

        if (!string.IsNullOrEmpty(shippingMethod))
        {
            await Page.FillAsync("[data-testid='shipping-method-input']", shippingMethod);
        }

        await Page.SetCheckedAsync("[data-testid='mark-shipped']", markAsShipped);
        await Page.ClickAsync("[data-testid='save-shipping']");
        // No NetworkIdle: callers assert the tracking number via web-first Expect(...).
    }

    public async Task AddNoteAsync(string note)
    {
        await Page.WaitForSelectorAsync("[data-testid='note-input']", new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.FillAsync("[data-testid='note-input']", note);
        await Page.ClickAsync("[data-testid='add-note']");
        // No NetworkIdle: callers assert the success message via web-first Expect(...).
    }

    public async Task<string> GetTrackingNumberAsync()
    {
        await Page.WaitForSelectorAsync("[data-testid='order-tracking-number']", new PageWaitForSelectorOptions { Timeout = 30000 });
        return (await Page.Locator("[data-testid='order-tracking-number']").InnerTextAsync()).Trim();
    }

    public async Task<bool> HasTrackingNumberAsync()
    {
        var tracking = await Page.QuerySelectorAsync("[data-testid='order-tracking-number']");
        return tracking != null;
    }
}
