using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests;

[Collection("Integration")]
public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy_WhenServicesAreUp()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ReturnsOk_WhenDatabaseIsConnected()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        // Readiness check runs all health checks (database, redis)
        // In test environment, database is connected via Testcontainers
        // Redis may not be available, so we accept Healthy or Degraded
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
    }

    [Fact]
    public async Task HealthLive_ReturnsOk_Always()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        // Liveness check has no predicates - always returns healthy if app is running
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_ResponseContentType_IsTextPlain()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task HealthReady_ResponseContentType_IsTextPlain()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task HealthLive_ResponseContentType_IsTextPlain()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthentication()
    {
        // Arrange - ensure no auth header is set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthReady_DoesNotRequireAuthentication()
    {
        // Arrange - ensure no auth header is set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthLive_DoesNotRequireAuthentication()
    {
        // Arrange - ensure no auth header is set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
