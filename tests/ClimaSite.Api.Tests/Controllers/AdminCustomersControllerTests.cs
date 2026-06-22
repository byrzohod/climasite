using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ApplicationUserEntity = ClimaSite.Core.Entities.ApplicationUser;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <c>AdminCustomersController</c> (<c>/api/admin/customers</c>).
/// Covers list/detail/status-update happy paths, not-found, the deactivation side
/// effect, and the 401/403 auth gates. Real HTTP + real Postgres.
/// </summary>
public class AdminCustomersControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _admin;

    public AdminCustomersControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
        _admin = AdminTestHelpers.CreateClientWithAdminSecret(factory);
    }

    public override async Task DisposeAsync()
    {
        _admin.Dispose();
        await base.DisposeAsync();
    }

    // --- Auth gates ---------------------------------------------------------

    [Fact]
    public async Task GetCustomers_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomers_Returns403_WhenAuthenticatedAsNonAdmin()
    {
        var (token, _) = await AdminTestHelpers.AuthenticateCustomerAsync(
            Factory, _admin, $"plain-{Guid.NewGuid()}@example.com");

        using var customer = Factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await customer.GetAsync("/api/admin/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCustomerStatus_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/admin/customers/{Guid.NewGuid()}/status",
            new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- List ---------------------------------------------------------------

    [Fact]
    public async Task GetCustomers_ReturnsRegisteredUsers_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var (_, customerId) = await AdminTestHelpers.AuthenticateCustomerAsync(
            Factory, _admin, $"listed-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync("/api/admin/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CustomersListResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(i => i.Id == customerId);
    }

    [Fact]
    public async Task GetCustomers_FiltersBySearch_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var email = $"searchable-{unique}@example.com";
        var (_, customerId) = await AdminTestHelpers.AuthenticateCustomerAsync(Factory, _admin, email);

        var response = await _admin.GetAsync($"/api/admin/customers?search={unique}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CustomersListResponse>(JsonOptions);
        result!.Items.Should().Contain(i => i.Id == customerId && i.Email == email);
    }

    // --- Detail -------------------------------------------------------------

    [Fact]
    public async Task GetCustomer_ReturnsDetail_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var email = $"detail-{Guid.NewGuid()}@example.com";
        var (_, customerId) = await AdminTestHelpers.AuthenticateCustomerAsync(Factory, _admin, email);

        var response = await _admin.GetAsync($"/api/admin/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<CustomerDetailResponse>(JsonOptions);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(customerId);
        detail.Email.Should().Be(email);
        detail.IsActive.Should().BeTrue();
        detail.Stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomer_Returns404_ForUnknownId()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync($"/api/admin/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Customer not found");
    }

    // --- Status update ------------------------------------------------------

    [Fact]
    public async Task UpdateCustomerStatus_Deactivates_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var (_, customerId) = await AdminTestHelpers.AuthenticateCustomerAsync(
            Factory, _admin, $"deactivate-{Guid.NewGuid()}@example.com");

        var response = await _admin.PutAsJsonAsync(
            $"/api/admin/customers/{customerId}/status",
            new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify persisted via a fresh scope (the customer should now be inactive).
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserEntity>>();
        var user = await userManager.FindByIdAsync(customerId.ToString());
        user!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomerStatus_Returns400_ForUnknownCustomer()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.PutAsJsonAsync(
            $"/api/admin/customers/{Guid.NewGuid()}/status",
            new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Customer not found");
    }

    // --- Response DTOs ------------------------------------------------------

    private sealed record CustomersListResponse(
        List<CustomerListItem> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);

    private sealed record CustomerListItem(Guid Id, string Email, bool IsActive);

    private sealed record CustomerDetailResponse(
        Guid Id, string Email, bool IsActive, CustomerStats Stats);

    private sealed record CustomerStats(int TotalOrders, decimal TotalSpent);
}
