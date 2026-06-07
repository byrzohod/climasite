using ClimaSite.Api.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Tests.Configuration;

public class JwtConfigurationTests
{
    [Fact]
    public void ResolveSecret_ThrowsInProduction_WhenEnvironmentSecretIsMissing()
    {
        var configuration = CreateConfiguration(("JwtSettings:Secret", "DevelopmentOnlySecretThatIsLongEnough"));
        var environment = CreateEnvironment(Environments.Production);

        var act = () => JwtConfiguration.ResolveSecret(configuration, environment, environmentSecret: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT_SECRET must be configured in Production.");
    }

    [Fact]
    public void ResolveSecret_UsesEnvironmentSecret_InProduction()
    {
        var configuration = CreateConfiguration(("JwtSettings:Secret", "DevelopmentOnlySecretThatIsLongEnough"));
        var environment = CreateEnvironment(Environments.Production);

        var secret = JwtConfiguration.ResolveSecret(configuration, environment, "ProductionSecretFromEnvironment");

        secret.Should().Be("ProductionSecretFromEnvironment");
    }

    [Fact]
    public void ResolveSecret_UsesConfiguredSecret_OutsideProduction()
    {
        var configuration = CreateConfiguration(("JwtSettings:Secret", "DevelopmentOnlySecretThatIsLongEnough"));
        var environment = CreateEnvironment(Environments.Development);

        var secret = JwtConfiguration.ResolveSecret(configuration, environment, environmentSecret: null);

        secret.Should().Be("DevelopmentOnlySecretThatIsLongEnough");
    }

    [Fact]
    public void ResolveSecret_ThrowsOutsideProduction_WhenNoSecretExists()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Development);

        var act = () => JwtConfiguration.ResolveSecret(configuration, environment, environmentSecret: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT Secret not configured. Set JWT_SECRET or JwtSettings:Secret.");
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
