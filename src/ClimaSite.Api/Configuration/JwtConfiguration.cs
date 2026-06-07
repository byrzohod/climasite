using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Configuration;

public static class JwtConfiguration
{
    public const string SecretEnvironmentVariable = "JWT_SECRET";

    public static string ResolveSecret(IConfiguration configuration, IWebHostEnvironment environment)
    {
        return ResolveSecret(configuration, environment, Environment.GetEnvironmentVariable(SecretEnvironmentVariable));
    }

    public static string ResolveSecret(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string? environmentSecret)
    {
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(environmentSecret))
        {
            throw new InvalidOperationException("JWT_SECRET must be configured in Production.");
        }

        return environmentSecret
            ?? configuration.GetSection("JwtSettings")["Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET or JwtSettings:Secret.");
    }
}
