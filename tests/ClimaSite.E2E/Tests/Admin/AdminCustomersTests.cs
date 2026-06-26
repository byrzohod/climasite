using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.Infrastructure.Retry;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Admin;

/// <summary>
/// E2E tests for the GAP-02 Admin Customers UI (list + detail panel + status toggle).
/// Real data only — each test creates its own admin + customer and cleans up via the factory.
/// </summary>
[Collection("Playwright")]
public class AdminCustomersTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public AdminCustomersTests(PlaywrightFixture fixture)
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

    [RetryFact]
    public async Task AdminCustomers_ListsCustomers()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var customer = await _dataFactory.CreateUserAsync();
        customer.Id.Should().NotBe(Guid.Empty, "the seeded customer must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var customersPage = new AdminCustomersPage(_page);
        await customersPage.NavigateToListAsync();

        // The list is paginated; search by email to guarantee the row is on screen.
        await customersPage.SearchAsync(customer.Email);

        // Assert
        var rowCount = await customersPage.GetCustomerRowCountAsync();
        rowCount.Should().BeGreaterThan(0, "the admin customers list should render at least one customer row");
        var hasRow = await customersPage.HasCustomerRowAsync(customer.Id.ToString());
        hasRow.Should().BeTrue("the seeded customer should appear in the admin customers list");
    }

    [RetryFact]
    public async Task AdminCustomers_CanOpenDetail()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var customer = await _dataFactory.CreateUserAsync();
        customer.Id.Should().NotBe(Guid.Empty, "the seeded customer must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var customersPage = new AdminCustomersPage(_page);
        await customersPage.NavigateToListAsync();
        await customersPage.SearchAsync(customer.Email);
        await customersPage.OpenDetailAsync(customer.Id.ToString());

        // Assert — the detail panel is visible and shows the customer's email.
        await Assertions.Expect(customersPage.DetailPanel)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await Assertions.Expect(customersPage.DetailPanel)
            .ToContainTextAsync(customer.Email, new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
    }

    [RetryFact]
    public async Task AdminCustomers_CanToggleStatus()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var customer = await _dataFactory.CreateUserAsync();
        customer.Id.Should().NotBe(Guid.Empty, "the seeded customer must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var customersPage = new AdminCustomersPage(_page);
        await customersPage.NavigateToListAsync();
        await customersPage.SearchAsync(customer.Email);
        await customersPage.OpenDetailAsync(customer.Id.ToString());

        // A freshly registered customer starts Active. Capture the badge text, toggle, then
        // assert it flipped (and a success indicator is shown).
        var before = await customersPage.GetActiveBadgeTextAsync();
        before.Should().NotBeNullOrWhiteSpace("the detail panel should display the customer's status");

        await customersPage.ToggleStatusAsync();

        // Assert — the status badge text changes after toggling, and the success banner appears.
        await Assertions.Expect(_page.Locator("[data-testid='customer-action-success']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await Assertions.Expect(customersPage.ActiveBadge)
            .Not.ToHaveTextAsync(before, new LocatorAssertionsToHaveTextOptions { Timeout = 30000 });
    }

    private async Task LoginAsAdminAsync(TestUser admin)
    {
        admin.Token.Should().NotBeNullOrWhiteSpace("admin test users must include a real access token");

        await _page.GotoAsync("/");
        await _page.Locator("[data-testid='home-v3-hero']").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await _page.EvaluateAsync("token => window.localStorage.setItem('climasite_token', token)", admin.Token);
        await _page.ReloadAsync();
        // Settle on the re-bootstrapped app shell rather than NetworkIdle before navigating to admin.
        await _page.Locator("[data-testid='home-v3-hero']").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }
}
