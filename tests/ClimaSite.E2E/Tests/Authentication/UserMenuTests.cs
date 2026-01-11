using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Authentication;

/// <summary>
/// E2E tests for user menu functionality (UX2-001 to UX2-007).
/// Tests user dropdown, navigation links, and logout.
/// </summary>
[Collection("Playwright")]
public class UserMenuTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public UserMenuTests(PlaywrightFixture fixture)
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

    private async Task LoginAsUserAsync(TestUser user)
    {
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);
    }

    // E2E-003: User sees dropdown menu when logged in
    [Fact]
    public async Task LoggedInUser_ClicksUserIcon_SeesDropdownMenu()
    {
        // Arrange - Create and login user
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Act - Click user menu trigger
        await _page.ClickAsync("[data-testid='user-menu-trigger']");

        // Assert - Dropdown is visible with all links
        var dropdown = _page.Locator("[data-testid='user-dropdown']");
        await Assertions.Expect(dropdown).ToBeVisibleAsync();
        await Assertions.Expect(dropdown.Locator("[data-testid='account-link']")).ToBeVisibleAsync();
        await Assertions.Expect(dropdown.Locator("[data-testid='orders-link']")).ToBeVisibleAsync();
        await Assertions.Expect(dropdown.Locator("[data-testid='logout-button']")).ToBeVisibleAsync();
    }

    // E2E-004: User can access account from dropdown
    [Fact]
    public async Task LoggedInUser_ClicksAccountLink_NavigatesToAccount()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Act - Open dropdown and click account link
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await _page.ClickAsync("[data-testid='account-link']");

        // Assert - Navigated to account page
        await _page.WaitForURLAsync(url => url.Contains("/account"));
        _page.Url.Should().Contain("/account");
    }

    // E2E-005: User can access orders from dropdown
    [Fact]
    public async Task LoggedInUser_ClicksOrdersLink_NavigatesToOrders()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Act - Open dropdown and click orders link
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await _page.ClickAsync("[data-testid='orders-link']");

        // Assert - Navigated to orders page
        await _page.WaitForURLAsync(url => url.Contains("/orders"));
        _page.Url.Should().Contain("/orders");
    }

    // E2E-006: User can logout from dropdown
    [Fact]
    public async Task LoggedInUser_ClicksLogout_IsLoggedOut()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Verify user is logged in first
        await Assertions.Expect(_page.Locator("[data-testid='user-menu']")).ToBeVisibleAsync();

        // Act - Open dropdown and click logout
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await _page.ClickAsync("[data-testid='logout-button']");

        // Assert - User is logged out, login button visible
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(_page.Locator("[data-testid='login-button']")).ToBeVisibleAsync();
    }

    // E2E-007: Login link shows when not authenticated
    [Fact]
    public async Task NotLoggedIn_SeesLoginButton_NotUserMenu()
    {
        // Arrange & Act - Navigate to home page without logging in
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Login button visible, user menu not visible
        await Assertions.Expect(_page.Locator("[data-testid='login-button']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='user-menu']")).Not.ToBeVisibleAsync();
    }

    // E2E: User menu displays correct user info
    [Fact]
    public async Task LoggedInUser_UserMenu_DisplaysUserInfo()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Act - Open dropdown
        await _page.ClickAsync("[data-testid='user-menu-trigger']");

        // Assert - User info is displayed
        var dropdown = _page.Locator("[data-testid='user-dropdown']");
        var userNameText = await dropdown.Locator(".dropdown-user-name").TextContentAsync();
        var userEmailText = await dropdown.Locator(".dropdown-user-email").TextContentAsync();

        userNameText.Should().Contain(user.FirstName);
        userEmailText.Should().Contain(user.Email);
    }

    // E2E: User menu closes on click outside
    [Fact]
    public async Task UserMenu_ClickOutside_ClosesDropdown()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Open dropdown
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await Assertions.Expect(_page.Locator("[data-testid='user-dropdown']")).ToBeVisibleAsync();

        // Act - Click outside the dropdown
        await _page.ClickAsync("body", new PageClickOptions { Position = new Position { X = 10, Y = 10 } });

        // Assert - Dropdown is closed
        await Assertions.Expect(_page.Locator("[data-testid='user-dropdown']")).Not.ToBeVisibleAsync();
    }

    // E2E: User menu closes on Escape key
    [Fact]
    public async Task UserMenu_PressEscape_ClosesDropdown()
    {
        // Arrange
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Open dropdown
        await _page.ClickAsync("[data-testid='user-menu-trigger']");
        await Assertions.Expect(_page.Locator("[data-testid='user-dropdown']")).ToBeVisibleAsync();

        // Act - Press Escape
        await _page.Keyboard.PressAsync("Escape");

        // Assert - Dropdown is closed
        await Assertions.Expect(_page.Locator("[data-testid='user-dropdown']")).Not.ToBeVisibleAsync();
    }

    // E2E: Admin user sees admin link in dropdown
    [Fact]
    public async Task AdminUser_SeesAdminLinkInDropdown()
    {
        // Arrange - Create admin user
        var adminUser = await _dataFactory.CreateAdminUserAsync();
        await LoginAsUserAsync(adminUser);

        // Act - Open dropdown
        await _page.ClickAsync("[data-testid='user-menu-trigger']");

        // Assert - Admin link is visible
        await Assertions.Expect(_page.Locator("[data-testid='admin-link']")).ToBeVisibleAsync();
    }

    // E2E: Regular user does not see admin link
    [Fact]
    public async Task RegularUser_DoesNotSeeAdminLink()
    {
        // Arrange - Create regular user
        var user = await _dataFactory.CreateUserAsync();
        await LoginAsUserAsync(user);

        // Act - Open dropdown
        await _page.ClickAsync("[data-testid='user-menu-trigger']");

        // Assert - Admin link is NOT visible
        await Assertions.Expect(_page.Locator("[data-testid='admin-link']")).Not.ToBeVisibleAsync();
    }
}
