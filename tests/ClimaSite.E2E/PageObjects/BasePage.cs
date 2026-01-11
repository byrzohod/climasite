using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    public async Task WaitForLoadAsync()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
