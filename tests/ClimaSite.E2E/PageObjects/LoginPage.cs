using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public class LoginPage : BasePage
{
    private const string EmailInput = "[data-testid='login-email'] input";
    private const string PasswordInput = "[data-testid='login-password'] input";
    private const string SubmitButton = "[data-testid='login-submit']";
    private const string ErrorMessage = "[data-testid='login-error']";
    private const string RegisterLink = "[data-testid='register-link']";

    public LoginPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/login");
        await SettleAsync("[data-testid='login-email']");
    }

    public async Task LoginAsync(string email, string password)
    {
        await FillAsync(EmailInput, email);
        await FillAsync(PasswordInput, password);
        await ClickAsync(SubmitButton);

        // Wait for either navigation (success) or error message (failure). The post-login page
        // settles via locator auto-waiting in the caller; do not wait on NetworkIdle here.
        try
        {
            await Page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            // No navigation means login failed or validation error - wait for error to appear.
            // Tolerated if neither appears; callers assert the resulting state themselves.
            try
            {
                await Page.WaitForSelectorAsync($"{ErrorMessage}, [data-testid='validation-error']", new PageWaitForSelectorOptions { Timeout = 3000 });
            }
            catch
            {
                // Tolerated — no navigation and no visible error surfaced.
            }
        }
    }

    public async Task<string> GetErrorMessageAsync()
    {
        await WaitForSelectorAsync(ErrorMessage);
        return await GetTextAsync(ErrorMessage);
    }

    public async Task<bool> IsLoggedInAsync()
    {
        try
        {
            // After successful login, user is redirected away from login page
            // Check for any of these indicators: not on login page anymore, or user menu visible
            await Page.WaitForSelectorAsync("[data-testid='user-menu'], [data-testid='account-link']", new PageWaitForSelectorOptions { Timeout = 5000 });
            return true;
        }
        catch
        {
            // Fallback: check if we're no longer on login page
            return !Page.Url.Contains("/login");
        }
    }

    public async Task GoToRegisterAsync()
    {
        await ClickAsync(RegisterLink);
        await SettleAsync("[data-testid='register-email']");
    }
}
