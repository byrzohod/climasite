using System.Net;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests;

/// <summary>
/// OPS-05: every response carries an X-Correlation-Id (generated or echoed), and error responses include
/// a non-empty traceId so a caller can quote a single id when reporting a problem.
/// </summary>
[Collection("Integration")]
public class CorrelationIdTests : IClassFixture<TestWebApplicationFactory>
{
    private const string Header = "X-Correlation-Id";
    private readonly HttpClient _client;

    public CorrelationIdTests(TestWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Response_CarriesAGeneratedCorrelationId_WhenNoneSupplied()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.TryGetValues(Header, out var values).Should().BeTrue();
        var id = string.Join("", values!);
        id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_EchoesTheSuppliedCorrelationId()
    {
        var supplied = "test-correlation-" + Guid.NewGuid().ToString("N");
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(Header, supplied);

        var response = await _client.SendAsync(request);

        response.Headers.GetValues(Header).Should().ContainSingle().Which.Should().Be(supplied);
    }

    [Fact]
    public async Task ErrorResponse_IncludesANonEmptyTraceId()
    {
        // The slug query throws NotFoundException → ExceptionHandlingMiddleware → JSON with traceId.
        var response = await _client.GetAsync("/api/products/this-slug-does-not-exist-" + Guid.NewGuid().ToString("N"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrWhiteSpace();
    }
}
