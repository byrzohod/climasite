using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the My Orders page and Order Details page.
/// </summary>
public class OrdersPage : BasePage
{
    public OrdersPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/account/orders");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToOrderDetailsAsync(string orderId)
    {
        await Page.GotoAsync($"/account/orders/{orderId}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Orders List Methods
    public async Task<bool> IsEmptyAsync()
    {
        try
        {
            await Page.WaitForSelectorAsync("[data-testid='orders-empty'], [data-testid='order-card']", new PageWaitForSelectorOptions { Timeout = 5000 });
            var noOrders = await Page.QuerySelectorAsync("[data-testid='orders-empty']");
            return noOrders != null;
        }
        catch
        {
            return true;
        }
    }

    public async Task<int> GetOrderCountAsync()
    {
        await Page.WaitForSelectorAsync("[data-testid='order-card'], [data-testid='orders-empty']", new PageWaitForSelectorOptions { Timeout = 5000 });
        var orderItems = await Page.QuerySelectorAllAsync("[data-testid='order-card']");
        return orderItems.Count;
    }

    public async Task<List<string>> GetOrderNumbersAsync()
    {
        var orderNumbers = new List<string>();
        var orderItems = await Page.QuerySelectorAllAsync("[data-testid='order-number']");

        foreach (var item in orderItems)
        {
            var text = await item.TextContentAsync();
            if (!string.IsNullOrEmpty(text))
            {
                orderNumbers.Add(text.Trim());
            }
        }

        return orderNumbers;
    }

    public async Task ClickOrderAsync(int index)
    {
        var viewDetailsButtons = await Page.QuerySelectorAllAsync("[data-testid='view-order-details']");
        if (index < viewDetailsButtons.Count)
        {
            await viewDetailsButtons[index].ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public async Task FilterByStatusAsync(string status)
    {
        await Page.SelectOptionAsync("[data-testid='orders-status-filter']", new SelectOptionValue { Label = status });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchOrdersAsync(string query)
    {
        await Page.FillAsync("[data-testid='orders-search']", query);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SortByAsync(string sortOption)
    {
        await Page.SelectOptionAsync("[data-testid='orders-sort-by']", new SelectOptionValue { Label = sortOption });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Order Details Methods
    public async Task<string> GetOrderStatusAsync()
    {
        var status = await Page.QuerySelectorAsync("[data-testid='order-status']");
        if (status == null) return string.Empty;
        return await status.TextContentAsync() ?? string.Empty;
    }

    public async Task<string> GetOrderNumberFromDetailsAsync()
    {
        var orderNumber = await Page.QuerySelectorAsync("[data-testid='order-number']");
        if (orderNumber == null) return string.Empty;
        return await orderNumber.TextContentAsync() ?? string.Empty;
    }

    public async Task<decimal> GetOrderTotalAsync()
    {
        var total = await Page.QuerySelectorAsync("[data-testid='order-total']");
        var text = total != null ? await total.TextContentAsync() ?? "0" : "0";
        // Parse the total (remove currency symbols)
        var numericText = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        return decimal.TryParse(numericText.Replace(',', '.'), out var result) ? result : 0;
    }

    public async Task<int> GetOrderItemCountAsync()
    {
        var items = await Page.QuerySelectorAllAsync("[data-testid='order-item-row']");
        return items.Count;
    }

    public async Task<bool> CanCancelOrderAsync()
    {
        var cancelButton = await Page.QuerySelectorAsync("[data-testid='cancel-order-btn']");
        return cancelButton != null && await cancelButton.IsEnabledAsync();
    }

    public async Task CancelOrderAsync(string? reason = null)
    {
        await Page.ClickAsync("[data-testid='cancel-order-btn']");

        // Wait for confirmation modal
        await Page.WaitForSelectorAsync("[data-testid='cancel-modal']", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Enter reason if provided
        if (!string.IsNullOrEmpty(reason))
        {
            await Page.FillAsync("[data-testid='cancel-reason']", reason);
        }

        // Confirm cancellation
        await Page.ClickAsync("[data-testid='confirm-cancel-btn']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the status to update (modal closes and status changes)
        await Page.WaitForSelectorAsync("[data-testid='cancel-modal']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

        // Small delay to allow Angular to update the DOM
        await Task.Delay(500);
    }

    public async Task ReorderAsync()
    {
        await Page.ClickAsync("[data-testid='reorder-btn']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the component to redirect to cart (happens after 1500ms delay in Angular)
        await Task.Delay(2000);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task DownloadInvoiceAsync()
    {
        var downloadPromise = Page.WaitForDownloadAsync();
        await Page.ClickAsync("[data-testid='download-invoice-btn']");
        var download = await downloadPromise;
        // Save to temp location
        await download.SaveAsAsync(Path.Combine(Path.GetTempPath(), download.SuggestedFilename));
    }

    public async Task<bool> HasTrackingNumberAsync()
    {
        var tracking = await Page.QuerySelectorAsync("[data-testid='tracking-number']");
        return tracking != null;
    }

    public async Task<string> GetTrackingNumberAsync()
    {
        var tracking = await Page.QuerySelectorAsync("[data-testid='tracking-number']");
        if (tracking == null) return string.Empty;
        return await tracking.TextContentAsync() ?? string.Empty;
    }

    // Order Timeline Methods
    public async Task<bool> HasTimelineAsync()
    {
        var timeline = await Page.QuerySelectorAsync("[data-testid='order-timeline']");
        return timeline != null;
    }

    public async Task<int> GetTimelineEventCountAsync()
    {
        var events = await Page.QuerySelectorAllAsync("[data-testid='timeline-event']");
        return events.Count;
    }

    // Pagination Methods
    public async Task<bool> HasNextPageAsync()
    {
        var nextButton = await Page.QuerySelectorAsync("[data-testid='pagination-next']");
        return nextButton != null && await nextButton.IsEnabledAsync();
    }

    public async Task GoToNextPageAsync()
    {
        await Page.ClickAsync("[data-testid='pagination-next']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToPreviousPageAsync()
    {
        await Page.ClickAsync("[data-testid='pagination-prev']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
