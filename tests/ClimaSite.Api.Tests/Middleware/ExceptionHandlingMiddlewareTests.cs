using System.Net;
using System.Text.Json;
using ClimaSite.Api.Middleware;
using ClimaSite.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClimaSite.Api.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, JsonElement body)> RunAsync(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, JsonDocument.Parse(json).RootElement.Clone());
    }

    [Fact]
    public async Task ArgumentException_ReturnsGenericBadRequest_WithoutLeakingTheRawMessage()
    {
        // B-008: a raw ArgumentException.Message can carry internal parameter names/state — it must NOT be
        // echoed. The client gets a generic "Invalid request" and a null detail.
        var (status, body) = await RunAsync(new ArgumentException("internal param 'connectionString' was null"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be("Invalid request");
        body.GetProperty("message").GetString().Should().NotContain("connectionString");
        body.GetProperty("detail").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ValidationException_StillEchoesItsUserFacingMessage()
    {
        // Validation messages are intended for the user — they must still surface (regression guard so the
        // B-008 narrowing didn't over-reach and swallow validation feedback).
        var ex = new ValidationException(new[] { new ValidationFailure("Email", "Email is required") });

        var (status, body) = await RunAsync(ex);

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Contain("Email is required");
        body.GetProperty("detail").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task NotFoundException_MapsTo404()
    {
        var (status, _) = await RunAsync(new NotFoundException("Product", Guid.NewGuid()));

        status.Should().Be((int)HttpStatusCode.NotFound);
    }
}
