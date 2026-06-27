using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page Object for the customer registration page (route <c>/register</c> — registered at the app root,
/// like <c>/login</c>; NOT <c>/auth/register</c> despite the component's folder path).
///
/// IMPORTANT behavioural note (see register.component.ts): registration does NOT auto-login. On
/// success the form shows a <c>register-success</c> banner and the SPA router redirects to
/// <c>/login</c> after ~3s. Callers that need an authenticated session must follow this with an
/// explicit <see cref="LoginPage"/> login. The form's submit button is also disabled until the
/// "terms" checkbox is ticked (Validators.requiredTrue), so <see cref="RegisterAsync"/> always
/// accepts the terms before submitting.
///
/// The <c>data-testid</c>s land on the <c>app-input</c> host elements, so the inner field is reached
/// via <c>[data-testid='...'] input</c> (the same pattern as <see cref="LoginPage"/>).
/// </summary>
public class RegisterPage : BasePage
{
    private const string FirstNameInput = "[data-testid='register-firstname'] input";
    private const string LastNameInput = "[data-testid='register-lastname'] input";
    private const string EmailInput = "[data-testid='register-email'] input";
    private const string PasswordInput = "[data-testid='register-password'] input";
    private const string ConfirmPasswordInput = "[data-testid='register-confirm-password'] input";
    private const string TermsCheckbox = ".checkbox-label input[type='checkbox']";
    private const string SubmitButton = "[data-testid='register-submit']";
    private const string SuccessMessage = "[data-testid='register-success']";
    private const string ErrorMessage = "[data-testid='register-error']";
    private const string ValidationError = "[data-testid='validation-error']";

    public RegisterPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/register");
        // Settle on the inner native <input> (the data-testid sits on the app-input host; the inner
        // input is the reliably-visible element, same as LoginPage).
        await SettleAsync("[data-testid='register-firstname'] input");
    }

    /// <summary>
    /// Fills the full registration form (firstName/lastName/email/password + matching confirm),
    /// accepts the terms checkbox, and clicks submit. Then waits for the resulting state — a
    /// success banner, the redirect away from /register, or an error banner — without relying on
    /// NetworkIdle. Callers assert the resulting state via <see cref="IsRegisteredAsync"/> /
    /// <see cref="GetErrorAsync"/>.
    /// </summary>
    public async Task RegisterAsync(string firstName, string lastName, string email, string password)
    {
        await FillFormAsync(firstName, lastName, email, password, password);

        // The submit button is disabled until the form is valid (incl. ticked terms). Auto-waiting
        // on the click will surface a real problem if it never becomes enabled.
        await ClickAsync(SubmitButton);

        // Settle on the success banner OR a redirect to /login OR an error banner — whichever the
        // backend produces — rather than NetworkIdle (banned flake source in this app).
        try
        {
            await Page.WaitForSelectorAsync(
                $"{SuccessMessage}, {ErrorMessage}",
                new PageWaitForSelectorOptions { Timeout = 30000 });
        }
        catch (TimeoutException)
        {
            // Tolerated — some flows redirect straight to /login before the banner is observed.
            // Callers assert the resulting state themselves.
            try
            {
                await Page.WaitForURLAsync(url => !url.Contains("/register"),
                    new PageWaitForURLOptions { Timeout = 15000 });
            }
            catch
            {
                // Tolerated — neither a banner nor a redirect surfaced; the caller decides.
            }
        }
    }

    /// <summary>
    /// Fills the registration form fields and ticks terms, but does NOT submit. Useful for testing
    /// client-side validation (e.g. password mismatch keeps the submit button disabled).
    /// </summary>
    public async Task FillFormAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string confirmPassword)
    {
        await FillAsync(FirstNameInput, firstName);
        await FillAsync(LastNameInput, lastName);
        await FillAsync(EmailInput, email);
        await FillAsync(PasswordInput, password);
        await FillAsync(ConfirmPasswordInput, confirmPassword);
        await AcceptTermsAsync();
    }

    /// <summary>Ticks the "accept terms" checkbox if it is present and not already checked.</summary>
    public async Task AcceptTermsAsync()
    {
        var terms = Page.Locator(TermsCheckbox);
        if (await terms.CountAsync() > 0 && !await terms.First.IsCheckedAsync())
        {
            await terms.First.CheckAsync();
        }
    }

    /// <summary>
    /// True if registration succeeded: either the success banner is visible, or the user was
    /// redirected away from the register page (e.g. to /login or logged straight in).
    /// </summary>
    public async Task<bool> IsRegisteredAsync()
    {
        try
        {
            await Page.WaitForSelectorAsync(SuccessMessage, new PageWaitForSelectorOptions { Timeout = 30000 });
            return true;
        }
        catch
        {
            // Fallback: registration may have redirected before we observed the banner.
            return !Page.Url.Contains("/register");
        }
    }

    /// <summary>Reads the server error banner text (e.g. duplicate-email rejection). Empty if absent.</summary>
    public async Task<string> GetErrorAsync()
    {
        try
        {
            await WaitForSelectorAsync(ErrorMessage, 30000);
            return await GetTextAsync(ErrorMessage);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Reads the inline client-side validation error (e.g. password mismatch). Empty if absent.</summary>
    public async Task<string> GetValidationErrorAsync()
    {
        try
        {
            await WaitForSelectorAsync(ValidationError, 30000);
            return await GetTextAsync(ValidationError);
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<bool> HasValidationErrorAsync()
    {
        try
        {
            await WaitForSelectorAsync(ValidationError, 30000);
            return await IsVisibleAsync(ValidationError);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// True if the submit button is currently enabled (form valid + terms ticked). The disabled
    /// state lives on the inner native <c>&lt;button&gt;</c> (app-button binds it there), so we read
    /// that element rather than the app-button host.
    /// </summary>
    public async Task<bool> IsSubmitEnabledAsync()
    {
        return await Page.Locator($"{SubmitButton} button, {SubmitButton}").First.IsEnabledAsync();
    }
}
