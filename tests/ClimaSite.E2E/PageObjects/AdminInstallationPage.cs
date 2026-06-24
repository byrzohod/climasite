using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the GAP-08 Admin Installation Requests list + per-row status change.
/// The route is /admin/installation-requests; the component renders as app-admin-installation.
/// Navigation waits on real selectors, no arbitrary sleeps.
/// </summary>
public class AdminInstallationPage : BasePage
{
    public AdminInstallationPage(IPage page) : base(page) { }

    public async Task NavigateToListAsync()
    {
        await Page.GotoAsync("/admin/installation-requests");
        await Page.WaitForSelectorAsync(
            "[data-testid='installation-row'], [data-testid='installation-empty'], [data-testid='installation-error']",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    public async Task<int> GetRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync("[data-testid='installation-row']");
        return rows.Count;
    }

    public ILocator Row(string requestId) =>
        Page.Locator($"[data-testid='installation-row'][data-request-id='{requestId}']");

    public async Task<bool> HasRowAsync(string requestId)
    {
        var row = await Page.QuerySelectorAsync(
            $"[data-testid='installation-row'][data-request-id='{requestId}']");
        return row != null;
    }

    public async Task<string> GetStatusBadgeTextAsync(string requestId)
    {
        var sel = $"[data-testid='installation-row'][data-request-id='{requestId}'] [data-testid='installation-status-badge']";
        await Page.WaitForSelectorAsync(sel, new PageWaitForSelectorOptions { Timeout = 30000 });
        return (await Page.Locator(sel).InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Selects a target status on a row and clicks Apply, then waits for the action to settle.
    /// </summary>
    public async Task ChangeStatusAsync(string requestId, string targetStatus)
    {
        var selectSelector =
            $"[data-testid='installation-status-select'][data-request-id='{requestId}']";
        await Page.WaitForSelectorAsync(selectSelector, new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.SelectOptionAsync(selectSelector, targetStatus);

        var applySelector =
            $"[data-testid='apply-installation-status'][data-request-id='{requestId}']";
        await Page.ClickAsync(applySelector);
        // No NetworkIdle: callers assert ActionSuccess / the status badge via web-first
        // Expect(...), which auto-waits for the row to re-render.
    }

    public ILocator ActionSuccess => Page.Locator("[data-testid='installation-action-success']");
}
