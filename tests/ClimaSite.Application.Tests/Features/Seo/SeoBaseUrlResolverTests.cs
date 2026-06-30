using ClimaSite.Application.Features.Seo;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Seo;

public class SeoBaseUrlResolverTests
{
    [Fact]
    public void ConfiguredHttps_Wins_InEveryEnvironment()
    {
        // Public (Staging/Production) environment, but a valid configured base is honored.
        var result = SeoBaseUrlResolver.Resolve(
            "https://www.climasite.com",
            isNonPublicEnvironment: false,
            requestScheme: "http",
            requestHost: "attacker.example");

        result.BaseUrl.Should().Be("https://www.climasite.com");
        result.Warning.Should().BeNull();
    }

    [Fact]
    public void ConfiguredBase_IsNormalized_ToSchemeAndAuthorityOnly()
    {
        var result = SeoBaseUrlResolver.Resolve(
            "https://www.climasite.com/some/path?q=1#frag",
            isNonPublicEnvironment: false,
            requestScheme: null,
            requestHost: null);

        result.BaseUrl.Should().Be("https://www.climasite.com");
    }

    [Fact]
    public void ConfiguredBase_PreservesExplicitPort()
    {
        var result = SeoBaseUrlResolver.Resolve(
            "https://staging.climasite.com:8443",
            isNonPublicEnvironment: false,
            requestScheme: null,
            requestHost: null);

        result.BaseUrl.Should().Be("https://staging.climasite.com:8443");
    }

    [Fact]
    public void ConfiguredHttp_IsAllowed_OnlyInNonPublicEnvironments()
    {
        SeoBaseUrlResolver.Resolve("http://localhost:4200", isNonPublicEnvironment: true, null, null)
            .BaseUrl.Should().Be("http://localhost:4200");
    }

    [Fact]
    public void ConfiguredHttp_IsRejected_InPublicEnvironment_AndFailsClosed()
    {
        var result = SeoBaseUrlResolver.Resolve(
            "http://www.climasite.com",
            isNonPublicEnvironment: false,
            requestScheme: "https",
            requestHost: "www.climasite.com");

        // http is not allowed in public envs; rejected → treated as unset → no request fallback in public → fail closed.
        result.BaseUrl.Should().BeNull();
        result.Warning.Should().NotBeNull();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("//example.com")]
    public void InvalidConfigured_IsTreatedAsUnset(string configured)
    {
        // Public env: invalid config → fail closed.
        var publicResult = SeoBaseUrlResolver.Resolve(configured, isNonPublicEnvironment: false, "https", "www.climasite.com");
        publicResult.BaseUrl.Should().BeNull();
        publicResult.Warning.Should().NotBeNull();

        // Non-public env: invalid config → falls back to the request host.
        var devResult = SeoBaseUrlResolver.Resolve(configured, isNonPublicEnvironment: true, "http", "localhost:5029");
        devResult.BaseUrl.Should().Be("http://localhost:5029");
        devResult.Warning.Should().NotBeNull();
    }

    [Fact]
    public void NonPublic_WithoutConfig_UsesRequestSchemeAndHost()
    {
        var result = SeoBaseUrlResolver.Resolve(
            configuredBaseUrl: null,
            isNonPublicEnvironment: true,
            requestScheme: "https",
            requestHost: "example.test");

        result.BaseUrl.Should().Be("https://example.test");
        result.Warning.Should().BeNull();
    }

    [Fact]
    public void Public_WithoutConfig_FailsClosed()
    {
        var result = SeoBaseUrlResolver.Resolve(
            configuredBaseUrl: "",
            isNonPublicEnvironment: false,
            requestScheme: "https",
            requestHost: "www.climasite.com");

        result.BaseUrl.Should().BeNull();
        result.Warning.Should().NotBeNull();
    }
}
