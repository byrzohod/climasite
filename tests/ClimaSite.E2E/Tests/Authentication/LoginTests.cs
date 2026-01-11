using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Authentication;

[Collection("Playwright")]
public class LoginTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public LoginTests(PlaywrightFixture fixture)
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

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        // Arrange - Create REAL user via API
        var user = await _dataFactory.CreateUserAsync();

        // Act - Login via UI
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Assert
        var isLoggedIn = await loginPage.IsLoggedInAsync();
        isLoggedIn.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        // Arrange
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();

        // Act - Try to login with invalid credentials
        await loginPage.LoginAsync("invalid@email.com", "wrongpassword");

        // Assert
        var errorMessage = await loginPage.GetErrorMessageAsync();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithEmptyFields_ShowsValidationErrors()
    {
        // Arrange
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();

        // Act - Try to submit empty form
        await loginPage.LoginAsync("", "");

        // Assert - Form should show validation errors
        var hasErrors = await _page.IsVisibleAsync("[data-testid='validation-error']");
        hasErrors.Should().BeTrue();
    }
}
