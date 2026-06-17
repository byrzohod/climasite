using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Features.Installation;

/// <summary>
/// GAP-08: a submitted installation request is persisted, the business is notified via the outbox,
/// and an admin can list it and update its status.
/// </summary>
public class AdminInstallationControllerTests : IntegrationTestBase
{
    private const string AdminSecret = "configured-secret";

    public AdminInstallationControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Submit_PersistsRequest_AndQueuesBusinessNotification()
    {
        var product = await SeedProductAsync("INST-001", "Test Split AC", "test-split-ac");

        var response = await Client.PostAsJsonAsync("/api/installation/requests", new
        {
            productId = product.Id,
            installationType = "Standard",
            customerName = "Jane Buyer",
            customerEmail = "jane@example.com",
            customerPhone = "+359888123456",
            addressLine1 = "12 Vitosha Blvd",
            city = "Sofia",
            postalCode = "1000",
            country = "Bulgaria"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var stored = await DbContext.InstallationRequests.AsNoTracking()
            .Where(r => r.ProductId == product.Id)
            .ToListAsync();
        stored.Should().ContainSingle();
        stored[0].CustomerEmail.Should().Be("jane@example.com");
        stored[0].Status.Should().Be(InstallationRequestStatus.Pending);

        // A business-notification email was queued in the outbox.
        var queued = await DbContext.OutboxMessages.AsNoTracking()
            .Where(m => m.Type == OutboxMessageTypes.Generic && m.ToEmail == "support@climasite.local")
            .ToListAsync();
        queued.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AdminList_RequiresAuthentication()
    {
        // No token set — admin endpoints are protected.
        var response = await Client.GetAsync("/api/admin/installation-requests");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminList_AsAdmin_ReturnsSubmittedRequest()
    {
        var product = await SeedProductAsync("INST-002", "Admin List AC", "admin-list-ac");
        var email = $"customer_{Guid.NewGuid():N}@example.com";

        var submit = await Client.PostAsJsonAsync("/api/installation/requests", new
        {
            productId = product.Id,
            installationType = "Premium",
            customerName = "List Customer",
            customerEmail = email,
            customerPhone = "+359888000111",
            addressLine1 = "1 Test St",
            city = "Plovdiv",
            postalCode = "4000",
            country = "Bulgaria"
        });
        submit.StatusCode.Should().Be(HttpStatusCode.Created);

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/installation-requests?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(email);
        content.Should().Contain("Admin List AC");
    }

    [Fact]
    public async Task AdminUpdateStatus_AsAdmin_UpdatesRequest()
    {
        var product = await SeedProductAsync("INST-003", "Status AC", "status-ac");

        var submit = await Client.PostAsJsonAsync("/api/installation/requests", new
        {
            productId = product.Id,
            installationType = "Standard",
            customerName = "Status Customer",
            customerEmail = $"status_{Guid.NewGuid():N}@example.com",
            customerPhone = "+359888222333",
            addressLine1 = "2 Test St",
            city = "Varna",
            postalCode = "9000",
            country = "Bulgaria"
        });
        submit.StatusCode.Should().Be(HttpStatusCode.Created);

        var requestId = (await DbContext.InstallationRequests.AsNoTracking()
            .Where(r => r.ProductId == product.Id)
            .ToListAsync()).Single().Id;

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/installation-requests/{requestId}/status",
            new { status = "Confirmed" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.InstallationRequests.AsNoTracking()
            .FirstAsync(r => r.Id == requestId);
        updated.Status.Should().Be(InstallationRequestStatus.Confirmed);
    }

    private async Task<Product> SeedProductAsync(string sku, string name, string slug)
    {
        var product = new Product(sku, name, slug, 999.99m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Registers a user, elevates them to Admin via the test endpoint (which needs the admin secret
    /// configured), then logs in again so the JWT carries the Admin role claim.
    /// </summary>
    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = Factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["TestSettings:AdminSecret"] = AdminSecret
                    });
                });
            })
            .CreateClient();

        var email = $"admin_{Guid.NewGuid():N}@example.com";
        const string password = "AdminPass123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Admin",
            lastName = "User"
        });
        register.IsSuccessStatusCode.Should().BeTrue();
        var registered = await register.Content.ReadFromJsonAsync<RegisterPayload>();

        var elevate = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = registered!.Id,
            testSecret = AdminSecret
        });
        elevate.IsSuccessStatusCode.Should().BeTrue();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.IsSuccessStatusCode.Should().BeTrue();
        var auth = await login.Content.ReadFromJsonAsync<AuthPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private record RegisterPayload(Guid Id);
    private record AuthPayload(string AccessToken);
}
