using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Admin;

/// <summary>
/// E2E tests for the GAP-08 Admin Installation Requests UI (list + per-row status change).
/// Real data only — each test creates its own admin + product + installation request via the API
/// and cleans up via the factory.
/// </summary>
[Collection("Playwright")]
public class AdminInstallationTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public AdminInstallationTests(PlaywrightFixture fixture)
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
    public async Task AdminInstallation_ListsRequests()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var request = await _dataFactory.CreateInstallationRequestAsync();
        request.Id.Should().NotBe(Guid.Empty, "the seeded installation request must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var installationPage = new AdminInstallationPage(_page);
        await installationPage.NavigateToListAsync();

        // Assert
        var rowCount = await installationPage.GetRowCountAsync();
        rowCount.Should().BeGreaterThan(0, "the admin installation list should render at least one row");
        var hasRow = await installationPage.HasRowAsync(request.Id.ToString());
        hasRow.Should().BeTrue("the seeded installation request should appear in the admin list");
    }

    [Fact]
    public async Task AdminInstallation_CanChangeStatus()
    {
        // Arrange
        var admin = await _dataFactory.CreateAdminUserAsync();
        var request = await _dataFactory.CreateInstallationRequestAsync();
        request.Id.Should().NotBe(Guid.Empty, "the seeded installation request must be created");

        // Act
        await LoginAsAdminAsync(admin);
        var installationPage = new AdminInstallationPage(_page);
        await installationPage.NavigateToListAsync();

        // A freshly submitted request starts Pending. Confirm it.
        await installationPage.ChangeStatusAsync(request.Id.ToString(), "Confirmed");

        // Assert — a success banner appears, and after the list reloads the row shows Confirmed.
        await Assertions.Expect(installationPage.ActionSuccess)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        var badge = await installationPage.GetStatusBadgeTextAsync(request.Id.ToString());
        badge.Should().NotBeNullOrWhiteSpace("the row should display the updated status");
    }

    private async Task LoginAsAdminAsync(TestUser admin)
    {
        admin.Token.Should().NotBeNullOrWhiteSpace("admin test users must include a real access token");

        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.EvaluateAsync("token => window.localStorage.setItem('climasite_token', token)", admin.Token);
        await _page.ReloadAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
