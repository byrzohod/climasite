using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Controllers;

public class TestControllerTests : IntegrationTestBase
{
    public TestControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ElevateAdmin_Returns500_InTesting_WhenAdminSecretIsMissing()
    {
        var response = await Client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = Guid.NewGuid(),
            testSecret = "test-admin-secret"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test admin secret is not configured");
    }

    [Fact]
    public async Task ElevateAdmin_Returns401_WhenConfiguredSecretDoesNotMatch()
    {
        using var client = CreateClientWithAdminSecret("configured-secret");

        var response = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = Guid.NewGuid(),
            testSecret = "wrong-secret"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ElevateAdmin_UsesConfiguredSecret_InTesting()
    {
        using var client = CreateClientWithAdminSecret("configured-secret");

        var response = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = Guid.NewGuid(),
            testSecret = "configured-secret"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("User not found");
    }

    [Fact]
    public async Task ElevateAdmin_ReturnsNotFound_OutsideTestEnvironments()
    {
        using var client = Factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Staging"))
            .CreateClient();

        var response = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = Guid.NewGuid(),
            testSecret = "test-admin-secret"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClientWithAdminSecret(string adminSecret)
    {
        return Factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["TestSettings:AdminSecret"] = adminSecret
                    });
                });
            })
            .CreateClient();
    }
}
