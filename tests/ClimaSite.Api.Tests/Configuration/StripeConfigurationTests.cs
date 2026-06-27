using ClimaSite.Api.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Tests.Configuration;

public class StripeConfigurationTests
{
    [Fact]
    public void ValidateProduction_Throws_WhenKeysAreMissing()
    {
        var configuration = CreateConfiguration(); // no Stripe section
        var environment = CreateEnvironment(Environments.Production);

        var act = () => StripeConfiguration.ValidateProductionConfiguration(configuration, environment);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Stripe:SecretKey*Production*");
    }

    [Fact]
    public void ValidateProduction_Throws_WhenKeysAreCommittedDummies()
    {
        var configuration = CreateConfiguration(
            ("Stripe:SecretKey", "sk_test_51DummyKeyForTestingPurposesOnly000000000000"),
            ("Stripe:WebhookSecret", "whsec_DummyWebhookSecretForTestingOnly00000000"),
            ("Stripe:PublishableKey", "pk_test_51DummyKeyForTestingPurposesOnly000000000000"));
        var environment = CreateEnvironment(Environments.Production);

        var act = () => StripeConfiguration.ValidateProductionConfiguration(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateProduction_Passes_WithRealKeys()
    {
        // NOTE: fixtures intentionally avoid the literal sk_/whsec_/pk_ Stripe key prefixes so GitHub
        // push-protection's secret scanner doesn't flag these as real keys. IsPlaceholder inspects the
        // value for dummy/placeholder markers — not the prefix — so this fully exercises the real path.
        var configuration = CreateConfiguration(
            ("Stripe:SecretKey", "a-real-secret-value-123456"),
            ("Stripe:WebhookSecret", "a-real-webhook-signing-secret-123456"),
            ("Stripe:PublishableKey", "a-real-publishable-value-123456"));
        var environment = CreateEnvironment(Environments.Production);

        var act = () => StripeConfiguration.ValidateProductionConfiguration(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProduction_IsNoOp_OutsideProduction_EvenWithEmptyKeys()
    {
        var configuration = CreateConfiguration(("Stripe:SecretKey", ""));
        var environment = CreateEnvironment(Environments.Development);

        var act = () => StripeConfiguration.ValidateProductionConfiguration(configuration, environment);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("sk_test_51DummyKeyForTestingPurposesOnly000000000000", true)]
    [InlineData("whsec_PLACEHOLDER", true)]
    [InlineData("changeme", true)]
    [InlineData("a-real-secret-value-123456", false)]
    [InlineData("another-genuine-runtime-secret-789", false)]
    public void IsPlaceholder_DetectsDummies_NotRealKeys(string value, bool expected)
    {
        StripeConfiguration.IsPlaceholder(value).Should().Be(expected);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
    }

    private static IWebHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestWebHostEnvironment { EnvironmentName = environmentName };
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
