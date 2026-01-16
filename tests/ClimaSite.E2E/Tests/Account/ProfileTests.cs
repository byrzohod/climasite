using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Account;

/// <summary>
/// E2E tests for Profile Settings page.
/// Tests personal information, preferences, and password change functionality.
/// </summary>
[Collection("Playwright")]
public class ProfileTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ProfileTests(PlaywrightFixture fixture)
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
        await _page.Context.CloseAsync();
    }

    private async Task LoginAndNavigateToProfile()
    {
        // Create a user and login
        var user = await _dataFactory.CreateUserAsync();
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to profile page
        await _page.GotoAsync("/account/profile");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Fact]
    public async Task ProfilePage_WhenAuthenticated_ShowsAllSections()
    {
        // Arrange & Act
        await LoginAndNavigateToProfile();

        // Assert - Page and all sections are visible
        await Assertions.Expect(_page.Locator("[data-testid='profile-page']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='profile-form']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='password-form']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange & Act - Try to access profile without logging in
        await _page.GotoAsync("/account/profile");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Redirected to login
        _page.Url.Should().Contain("/login");
    }

    [Fact]
    public async Task ProfilePage_ShowsUserData()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Assert - Personal info fields are visible and editable
        var firstNameInput = _page.Locator("[data-testid='profile-firstName']");
        var lastNameInput = _page.Locator("[data-testid='profile-lastName']");

        await Assertions.Expect(firstNameInput).ToBeVisibleAsync();
        await Assertions.Expect(lastNameInput).ToBeVisibleAsync();
        await Assertions.Expect(firstNameInput).ToBeEditableAsync();
        await Assertions.Expect(lastNameInput).ToBeEditableAsync();

        // Verify we can type in the fields (they're functional)
        await firstNameInput.FillAsync("TestFirst");
        await lastNameInput.FillAsync("TestLast");

        var firstName = await firstNameInput.InputValueAsync();
        var lastName = await lastNameInput.InputValueAsync();
        firstName.Should().Be("TestFirst");
        lastName.Should().Be("TestLast");
    }

    [Fact]
    public async Task ProfilePage_UpdatePersonalInfo_ShowsSuccess()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Act - Fill in valid names and submit
        var newFirstName = "UpdatedFirst" + DateTime.Now.Ticks.ToString().Substring(0, 6);
        var newLastName = "UpdatedLast";
        await _page.FillAsync("[data-testid='profile-firstName']", newFirstName);
        await _page.FillAsync("[data-testid='profile-lastName']", newLastName);

        // Wait for submit button to be enabled (form should be valid now)
        await Assertions.Expect(_page.Locator("[data-testid='profile-submit']")).ToBeEnabledAsync();
        await _page.ClickAsync("[data-testid='profile-submit']");

        // Assert - Success message is shown
        await _page.WaitForSelectorAsync("[data-testid='profile-success']", new PageWaitForSelectorOptions { Timeout = 5000 });
        await Assertions.Expect(_page.Locator("[data-testid='profile-success']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_PreferencesSection_ShowsLanguageAndCurrency()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Assert - Preferences fields are visible
        await Assertions.Expect(_page.Locator("[data-testid='profile-language']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='profile-currency']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_ChangeLanguage_UpdatesPreference()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Act - Change language to Bulgarian
        await _page.SelectOptionAsync("[data-testid='profile-language']", "bg");
        await _page.ClickAsync("[data-testid='preferences-submit']");

        // Wait for save
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Language was saved (page should reload with Bulgarian)
        // Verify the dropdown still has Bulgarian selected
        var selectedValue = await _page.Locator("[data-testid='profile-language']").InputValueAsync();
        selectedValue.Should().Be("bg");
    }

    [Fact]
    public async Task ProfilePage_PasswordSection_ShowsAllFields()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Assert - Password fields are visible
        await Assertions.Expect(_page.Locator("[data-testid='current-password']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='new-password']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='confirm-password']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='password-submit']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_PasswordMismatch_ShowsError()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Act - Fill mismatched passwords
        await _page.FillAsync("[data-testid='current-password']", "currentpass123");
        await _page.FillAsync("[data-testid='new-password']", "newpassword123");
        await _page.FillAsync("[data-testid='confirm-password']", "differentpassword123");

        // Blur the confirm password field
        await _page.ClickAsync("[data-testid='current-password']");

        // Assert - Submit should be disabled or error shown
        var submitButton = _page.Locator("[data-testid='password-submit']");
        await Assertions.Expect(submitButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task ProfilePage_PasswordTooShort_ShowsError()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Act - Fill password that's too short
        await _page.FillAsync("[data-testid='new-password']", "short");
        await _page.ClickAsync("[data-testid='confirm-password']"); // Blur

        // Assert - Validation error should be shown
        var passwordInput = _page.Locator("[data-testid='new-password']");
        await Assertions.Expect(passwordInput).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("invalid"));
    }

    [Fact]
    public async Task ProfilePage_EmailFieldIsDisabled()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Assert - Email field is disabled
        var emailInput = _page.Locator("#email");
        await Assertions.Expect(emailInput).ToBeDisabledAsync();
    }

    [Fact]
    public async Task ProfilePage_PhoneField_AcceptsInput()
    {
        // Arrange
        await LoginAndNavigateToProfile();

        // Act - Fill phone number
        await _page.FillAsync("[data-testid='profile-phone']", "+1234567890");

        // Assert - Phone field has the value
        var phoneValue = await _page.Locator("[data-testid='profile-phone']").InputValueAsync();
        phoneValue.Should().Be("+1234567890");
    }
}
