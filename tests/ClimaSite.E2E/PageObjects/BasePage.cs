using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    /// <summary>
    /// Waits for the page to be ready by anchoring on a stable, page-specific element rather than
    /// <see cref="LoadState.NetworkIdle"/>. NetworkIdle is unreliable in this app: the main layout
    /// lazy-loads the header/footer via <c>@defer (on timer(3200ms))</c>, so every page fires
    /// lazy-chunk fetches ~3.2s after navigation that reset NetworkIdle's quiet window
    /// nondeterministically (Plan 19 / A1). Locator auto-waiting + web-first assertions are the
    /// recommended pattern. Do NOT reintroduce NetworkIdle.
    /// </summary>
    /// <param name="anchorSelector">A stable selector (ideally a <c>data-testid</c>) known to be
    /// visible once the page is interactable.</param>
    /// <param name="timeoutMs">How long to wait for the anchor to become visible.</param>
    protected async Task SettleAsync(string anchorSelector, int timeoutMs = 15000)
    {
        await Page.Locator(anchorSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    protected async Task ClickAsync(string selector)
    {
        await Page.ClickAsync(selector);
    }

    protected async Task FillAsync(string selector, string value)
    {
        await Page.FillAsync(selector, value);
    }

    protected async Task<string> GetTextAsync(string selector)
    {
        return await Page.TextContentAsync(selector) ?? string.Empty;
    }

    protected async Task<bool> IsVisibleAsync(string selector)
    {
        return await Page.IsVisibleAsync(selector);
    }

    protected async Task WaitForSelectorAsync(string selector, int timeoutMs = 5000)
    {
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            Timeout = timeoutMs
        });
    }
}
