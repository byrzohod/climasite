using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClimaSite.Api.Tests;

/// <summary>
/// SEC-03 regression: the API must honor forwarded headers from the reverse proxy so the
/// rate limiter partitions per real client IP instead of per proxy. Guards against the
/// <c>Configure&lt;ForwardedHeadersOptions&gt;</c> call being removed or weakened.
/// </summary>
[Collection("Integration")]
public class ForwardedHeadersConfigurationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ForwardedHeadersConfigurationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ForwardedHeaders_are_configured_for_a_reverse_proxy()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedFor);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);

        // Both lists are cleared so the middleware honors headers from the (dynamic) proxy
        // address instead of only loopback (the default KnownNetworks entry). If the
        // Configure call were removed, ForwardedHeaders would default to None and
        // KnownNetworks would contain the loopback network — either would fail here.
        options.KnownProxies.Should().BeEmpty();
        options.KnownIPNetworks.Should().BeEmpty();
    }
}
