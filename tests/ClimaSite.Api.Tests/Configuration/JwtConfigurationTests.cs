using ClimaSite.Api.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Tests.Configuration;

public class JwtConfigurationTests
{
    // The reject-list value (the secret that was once committed to appsettings.json). Referencing it
    // here is the WHOLE point of these tests — it must never be accepted again (SEC-05/B-011).
    private const string CommittedPlaceholder = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string ValidSecret = "a-valid-jwt-signing-secret-that-is-32-plus-chars";

    // ---- Non-Development/Testing environments must fail fast when no secret is configured ----

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("staging")] // casing: only Development/Testing are exempt
    [InlineData("QA")]
    [InlineData("Productionn")] // a typo'd prod-like name is NOT exempt either
    public void ResolveSecret_Throws_WhenNoSecret_InNonExemptEnvironment(string environmentName)
    {
        var act = () => Resolve(environmentName, environmentSecret: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"JWT_SECRET must be configured in environment '{environmentName}'.");
    }

    // ---- Development/Testing boot out-of-the-box via the labelled, non-deployable fallback ----

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void ResolveSecret_ReturnsDevFallback_WhenNoSecret_InExemptEnvironment(string environmentName)
    {
        var secret = Resolve(environmentName, environmentSecret: null);

        secret.Should().Be(JwtConfiguration.DevelopmentOnlyFallbackSecret);
        secret.Length.Should().BeGreaterThanOrEqualTo(32);
        secret.Should().NotBe(CommittedPlaceholder);
    }

    // ---- The committed placeholder is rejected in EVERY environment (env or config) ----

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ResolveSecret_Throws_WhenCommittedPlaceholderIsUsed(string environmentName)
    {
        var act = () => Resolve(environmentName, environmentSecret: CommittedPlaceholder);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The committed/placeholder JWT secret must not be used. Configure a fresh JWT_SECRET.");
    }

    [Fact]
    public void ResolveSecret_Throws_WhenCommittedPlaceholderComesFromConfig()
    {
        var act = () => Resolve("Production", environmentSecret: null, ("JwtSettings:Secret", CommittedPlaceholder));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The committed/placeholder JWT secret must not be used. Configure a fresh JWT_SECRET.");
    }

    // ---- Empty/whitespace is treated as unset BEFORE precedence + length/reject checks ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveSecret_TreatsBlankAsUnset_DevReturnsFallback(string blank)
    {
        var secret = Resolve("Development", environmentSecret: blank, ("JwtSettings:Secret", blank));

        secret.Should().Be(JwtConfiguration.DevelopmentOnlyFallbackSecret);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveSecret_TreatsBlankAsUnset_NonDevThrows(string blank)
    {
        var act = () => Resolve("Production", environmentSecret: blank, ("JwtSettings:Secret", blank));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT_SECRET must be configured in environment 'Production'.");
    }

    // ---- A secret shorter than 32 chars is rejected in all environments ----

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void ResolveSecret_Throws_WhenSecretIsTooShort(string environmentName)
    {
        var shortSecret = new string('a', 31);

        var act = () => Resolve(environmentName, environmentSecret: shortSecret);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT signing secret must be at least 32 characters.");
    }

    // ---- A real >=32 secret is returned in every environment ----

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ResolveSecret_ReturnsConfiguredSecret_WhenValid(string environmentName)
    {
        var secret = Resolve(environmentName, environmentSecret: null, ("JwtSettings:Secret", ValidSecret));

        secret.Should().Be(ValidSecret);
    }

    [Fact]
    public void ResolveSecret_AcceptsExactly32CharSecret()
    {
        var exactly32 = new string('a', 32);

        Resolve("Production", environmentSecret: exactly32).Should().Be(exactly32);
    }

    // ---- Precedence + normalization ----

    [Fact]
    public void ResolveSecret_PrefersEnvironmentSecretOverConfig()
    {
        const string envSecret = "environment-variable-secret-32-characters-xx";

        var secret = Resolve("Production", environmentSecret: envSecret,
            ("JwtSettings:Secret", "config-secret-that-is-also-32-characters-long"));

        secret.Should().Be(envSecret);
    }

    [Fact]
    public void ResolveSecret_TrimsConfiguredSecret()
    {
        var secret = Resolve("Production", environmentSecret: "  " + ValidSecret + "  ");

        secret.Should().Be(ValidSecret);
    }

    private static string Resolve(
        string environmentName,
        string? environmentSecret,
        params (string Key, string Value)[] config)
    {
        return JwtConfiguration.ResolveSecret(
            CreateConfiguration(config),
            CreateEnvironment(environmentName),
            environmentSecret);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
    }

    private static IWebHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ClimaSite.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
