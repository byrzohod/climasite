using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests;

/// <summary>
/// SEC-08: every API response carries the defensive security headers, and the strict CSP is skipped for
/// Swagger UI (which needs inline scripts/styles).
/// </summary>
[Collection("Integration")]
public class SecurityHeadersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(TestWebApplicationFactory factory) => _client = factory.CreateClient();

    // Header may land in either the response or content header collection depending on the header.
    private static string? Header(HttpResponseMessage r, string name)
    {
        if (r.Headers.TryGetValues(name, out var v)) return string.Join(",", v);
        if (r.Content.Headers.TryGetValues(name, out var cv)) return string.Join(",", cv);
        return null;
    }

    [Fact]
    public async Task ApiResponse_CarriesAllSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "X-Frame-Options").Should().Be("DENY");
        Header(response, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
        Header(response, "X-XSS-Protection").Should().Be("0");
        Header(response, "Permissions-Policy").Should().Contain("camera=()");
        Header(response, "Content-Security-Policy").Should().Contain("default-src 'none'");
        Header(response, "Content-Security-Policy").Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task SwaggerResponse_KeepsHeadersButOmitsCsp()
    {
        // The middleware runs for every path; CSP is intentionally skipped for /swagger so the UI renders.
        var response = await _client.GetAsync("/swagger/index.html");

        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "X-Frame-Options").Should().Be("DENY");
        Header(response, "Content-Security-Policy").Should().BeNull("Swagger UI needs inline scripts/styles");
    }
}
