using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationUserEntity = ClimaSite.Core.Entities.ApplicationUser;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Shared helpers for the admin-only controller integration tests. The default
/// <see cref="TestWebApplicationFactory"/> does not configure
/// <c>TestSettings:AdminSecret</c>, so <c>POST /api/test/elevate-admin</c> would
/// return 500. These helpers build a client through a factory that injects a known
/// secret (the same DB container is reused), register + log in a real user, then
/// elevate them to the Admin role so admin endpoints can be exercised end to end.
/// </summary>
internal static class AdminTestHelpers
{
    private const string AdminSecret = "integration-admin-secret";

    /// <summary>
    /// Creates an HttpClient backed by a factory that has the test admin secret
    /// configured. The factory shares the same Postgres/Redis containers as the
    /// base factory, so data written through either client is visible to both.
    /// </summary>
    public static HttpClient CreateClientWithAdminSecret(TestWebApplicationFactory factory)
    {
        return factory
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
    }

    /// <summary>
    /// Registers + logs in a real user, looks up their id, and elevates them to
    /// Admin via the test-only endpoint. The bearer token (now carrying the Admin
    /// role) is set on <paramref name="client"/>. Returns the elevated user's id.
    /// </summary>
    public static async Task<Guid> AuthenticateAdminAsync(
        TestWebApplicationFactory factory,
        HttpClient client,
        string email)
    {
        const string password = "AdminPassword123!";

        // Register the user (idempotent enough for the cleaned-db-per-test model).
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Admin",
            lastName = "User"
        });

        // Resolve the user id directly from the DB.
        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserEntity>>();
            var user = await userManager.FindByEmailAsync(email);
            userId = user!.Id;
        }

        // Elevate to Admin BEFORE logging in so the issued JWT carries the Admin role.
        var elevateResponse = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId,
            testSecret = AdminSecret
        });
        elevateResponse.EnsureSuccessStatusCode();

        // Log in to obtain a token that now reflects the Admin role.
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginEnvelope>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        return userId;
    }

    /// <summary>Registers + logs in a plain (non-admin) Customer and returns their token + id.</summary>
    public static async Task<(string Token, Guid UserId)> AuthenticateCustomerAsync(
        TestWebApplicationFactory factory,
        HttpClient client,
        string email)
    {
        const string password = "CustomerPassword123!";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Plain",
            lastName = "Customer"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginEnvelope>();

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserEntity>>();
            var user = await userManager.FindByEmailAsync(email);
            userId = user!.Id;
        }

        return (login!.AccessToken, userId);
    }

    private sealed record LoginEnvelope(string AccessToken, UserEnvelope? User);
    private sealed record UserEnvelope(Guid Id, string Email, string Role);
}
