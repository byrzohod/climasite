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

    // B-055: an inbound id is only honoured if it's a short, safe token (^[A-Za-z0-9._-]{1,128}$).
    // Anything else (oversized, spaces, control/special chars used for log-forging) is dropped in favour of
    // a fresh GUID, so an attacker can't inject arbitrary content into the echoed header or the logs.
    [Theory]
    [InlineData("id with spaces")]
    [InlineData("inva!id$char")]
    [InlineData("tab\tinside")]
    public async Task Response_ReplacesAnInvalidCorrelationId_WithAFreshGuid(string badId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation(Header, badId);

        var response = await _client.SendAsync(request);

        var echoed = string.Join("", response.Headers.GetValues(Header));
        echoed.Should().NotBe(badId);
        Guid.TryParse(echoed, out _).Should().BeTrue("an invalid inbound id is replaced with a GUID");
    }

    [Fact]
    public async Task Response_ReplacesAnOverlongCorrelationId_WithAFreshGuid()
    {
        var tooLong = new string('a', 200); // exceeds the 128-char bound
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation(Header, tooLong);

        var response = await _client.SendAsync(request);

        var echoed = string.Join("", response.Headers.GetValues(Header));
        echoed.Should().NotBe(tooLong);
        echoed.Length.Should().BeLessThanOrEqualTo(128);
        Guid.TryParse(echoed, out _).Should().BeTrue();
    }
}
