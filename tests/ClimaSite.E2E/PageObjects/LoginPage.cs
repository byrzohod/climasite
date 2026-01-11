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
        await WaitForLoadAsync();
    }

    public async Task LoginAsync(string email, string password)
    {
        await FillAsync(EmailInput, email);
        await FillAsync(PasswordInput, password);
        await ClickAsync(SubmitButton);

        // Wait for either navigation (success) or error message (failure)
        try
        {
            await Page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = 5000 });
            // Additional wait for any pending requests to complete
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        catch (TimeoutException)
        {
            // No navigation means login failed or validation error - wait for error to appear
            try
            {
                await Page.WaitForSelectorAsync($"{ErrorMessage}, [data-testid='validation-error']", new PageWaitForSelectorOptions { Timeout = 3000 });
            }
            catch
            {
                // If no error appears either, just wait for network to settle
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
        await WaitForLoadAsync();
    }
}
