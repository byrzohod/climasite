using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.Infrastructure.Retry;
using ClimaSite.E2E.PageObjects;
using FluentAssertions;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Authentication;

/// <summary>
/// E2E coverage for the customer registration UI (route <c>/register</c> — registered at the app
/// root like <c>/login</c>, NOT <c>/auth/register</c>; see <see cref="RegisterPage"/>).
/// NO MOCKING — registration hits the real /api/auth/register endpoint and real database.
/// Each test uses a fresh unique email so runs are self-contained.
///
/// Behavioural note: registration does NOT auto-login (it shows a success banner then redirects to
/// /login after ~3s — see register.component.ts), so the happy-path test asserts on the success
/// state, not a logged-in session.
/// </summary>
[Collection("Playwright")]
public class RegistrationTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public RegistrationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _dataFactory.CleanupAsync();
        await _fixture.CloseTracedContextAsync(_page);
    }

    // A fresh, unique email per call so the test owns its data and never collides with reruns.
    // CRITICAL: Angular's Validators.email enforces a 64-char local-part (regex lookahead
    // `(?=.{1,64}@)`), so the part before '@' MUST stay short. A single 32-char GUID keeps it at
    // ~36 chars ("{tag}_" + 32) — safely under the limit — AND is regenerated on every call, so a
    // RetryFact retry of an already-succeeded registration gets a NEW email rather than colliding
    // on a duplicate. (Two GUIDs here previously produced a 69-char local-part → email control
    // invalid → form never submits → the whole registration silently no-ops.)
    private string UniqueEmail(string tag = "reg") =>
        $"{tag}_{Guid.NewGuid():N}@test.com".ToLowerInvariant();

    /// <summary>
    /// Happy path: a brand-new customer fills the registration form and is accepted — the success
    /// banner appears and the customer can then log in with the new credentials.
    /// </summary>
    [RetryFact]
    public async Task Register_NewUniqueUser_SucceedsAndCanLogIn()
    {
        var email = UniqueEmail();
        const string password = "TestPassword123@";

        var registerPage = new RegisterPage(_page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync("Nina", "Newbuyer", email, password);

        // Registration succeeded (success banner shown, or redirected to /login).
        (await registerPage.IsRegisteredAsync())
            .Should().BeTrue("a brand-new unique registration should be accepted");

        // No server error surfaced.
        (await registerPage.GetErrorAsync())
            .Should().BeEmpty("a successful registration must not show an error banner");

        // Prove the account was really created: log in with the new credentials via the UI.
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(email, password);
        (await loginPage.IsLoggedInAsync())
            .Should().BeTrue("the newly registered customer should be able to log in");
    }

    /// <summary>
    /// Password / confirm-password mismatch is caught client-side: the inline validation error is
    /// shown and the submit button stays disabled, so no account is created.
    /// </summary>
    [RetryFact]
    public async Task Register_PasswordMismatch_ShowsValidationError_AndCreatesNoAccount()
    {
        var email = UniqueEmail("mismatch");

        var registerPage = new RegisterPage(_page);
        await registerPage.NavigateAsync();

        // Fill the form with deliberately mismatched passwords (terms ticked) but do NOT submit.
        await registerPage.FillFormAsync(
            firstName: "Mis",
            lastName: "Match",
            email: email,
            password: "TestPassword123@",
            confirmPassword: "DifferentPassword456@");

        // The inline passwordMismatch validation error is shown...
        (await registerPage.HasValidationErrorAsync())
            .Should().BeTrue("a password/confirm mismatch must surface the inline validation error");

        // ...and the form is invalid, so the submit button is disabled (account cannot be created).
        (await registerPage.IsSubmitEnabledAsync())
            .Should().BeFalse("submit must be disabled while the passwords do not match");

        // Confirm no account was created: logging in with that email must fail.
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(email, "TestPassword123@");
        (await loginPage.IsLoggedInAsync())
            .Should().BeFalse("no account should exist after a blocked mismatched-password registration");
    }

    /// <summary>
    /// Registering the same email twice is rejected — the second attempt surfaces the server error
    /// banner instead of a success.
    /// </summary>
    [RetryFact]
    public async Task Register_DuplicateEmail_ShowsServerError()
    {
        var email = UniqueEmail("dup");
        const string password = "TestPassword123@";

        var registerPage = new RegisterPage(_page);

        // First registration succeeds.
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync("First", "Owner", email, password);
        (await registerPage.IsRegisteredAsync())
            .Should().BeTrue("the first registration of a fresh email should succeed");

        // Second registration with the SAME email must be rejected with the error banner.
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync("Second", "Owner", email, password);

        var error = await registerPage.GetErrorAsync();
        error.Should().NotBeNullOrWhiteSpace("registering a duplicate email must show a server error");
    }
}
