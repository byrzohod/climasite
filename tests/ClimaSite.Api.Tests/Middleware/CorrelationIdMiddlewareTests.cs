using ClimaSite.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ClimaSite.Api.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static async Task<string?> ResolveAsync(string? inbound)
    {
        var context = new DefaultHttpContext();
        if (inbound is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = inbound;
        }

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        return context.Items[CorrelationIdMiddleware.ItemKey] as string;
    }

    [Theory]
    [InlineData("valid-abc_123.45")]
    [InlineData("ABCdef0123456789")]
    public async Task ValidInboundId_IsHonoured(string id)
    {
        (await ResolveAsync(id)).Should().Be(id);
    }

    [Theory]
    [InlineData(null)]                 // none supplied
    [InlineData("")]                   // empty
    [InlineData("has space")]          // illegal char
    [InlineData("semi;colon")]         // illegal char
    [InlineData("trailing-newline\n")] // \z (not $) must reject a trailing newline — anti-log-forging
    [InlineData("lead\ninject")]       // embedded newline
    public async Task MissingOrInvalidInboundId_IsReplacedWithAFreshGuid(string? id)
    {
        var resolved = await ResolveAsync(id);

        resolved.Should().NotBe(id);
        Guid.TryParse(resolved, out _).Should().BeTrue();
    }

    [Fact]
    public async Task OverlongInboundId_IsReplacedWithAFreshGuid()
    {
        var resolved = await ResolveAsync(new string('a', 129)); // one over the 128 bound

        Guid.TryParse(resolved, out _).Should().BeTrue();
    }
}
