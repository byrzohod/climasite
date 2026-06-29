using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Configuration;

/// <summary>
/// Resolves and validates the JWT signing secret ONCE at startup (SEC-05 / B-011). The result is
/// bound into a single <c>JwtOptions</c> used by both bearer validation and token issuance, so the
/// committed placeholder key can never sign a token again and every non-Development/Testing
/// environment fails fast at startup when no real secret is configured.
/// </summary>
public static class JwtConfiguration
{
    public const string SecretEnvironmentVariable = "JWT_SECRET";

    /// <summary>
    /// Secrets that must NEVER be accepted in any environment — primarily the placeholder that was
    /// once committed to <c>appsettings.json</c> (B-011). Reintroducing it (env or config) is a hard
    /// startup error.
    /// </summary>
    private static readonly HashSet<string> KnownInsecureSecrets = new(StringComparer.Ordinal)
    {
        "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
    };

    /// <summary>
    /// Labelled, non-deployable fallback returned ONLY in Development/Testing when nothing is
    /// configured, so those envs boot out-of-the-box. It is ≥32 chars and is intentionally NOT in
    /// <see cref="KnownInsecureSecrets"/>; the require-a-secret gate guarantees it can never be used
    /// in Staging/QA/Production.
    /// </summary>
    public const string DevelopmentOnlyFallbackSecret =
        "climasite-dev-only-insecure-jwt-signing-key-DO-NOT-USE-IN-PRODUCTION";

    public static string ResolveSecret(IConfiguration configuration, IWebHostEnvironment environment)
    {
        return ResolveSecret(configuration, environment, Environment.GetEnvironmentVariable(SecretEnvironmentVariable));
    }

    public static string ResolveSecret(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string? environmentSecret)
    {
        // Treat whitespace-only values as unset BEFORE precedence/length/reject checks, so the
        // emptied appsettings.json "" never short-circuits the Dev/Test fallback (council [Medium]).
        var normalizedEnv = Normalize(environmentSecret);
        var normalizedConfig = Normalize(configuration.GetSection("JwtSettings")["Secret"]);
        var configured = normalizedEnv ?? normalizedConfig;

        // Reject the committed placeholder in EVERY environment (incl. Dev/Test).
        if (configured != null && KnownInsecureSecrets.Contains(configured))
        {
            throw new InvalidOperationException(
                "The committed/placeholder JWT secret must not be used. Configure a fresh JWT_SECRET.");
        }

        var exempt = environment.IsDevelopment() || environment.IsEnvironment("Testing");

        if (configured == null)
        {
            if (exempt)
            {
                return DevelopmentOnlyFallbackSecret;
            }

            throw new InvalidOperationException(
                $"JWT_SECRET must be configured in environment '{environment.EnvironmentName}'.");
        }

        if (configured.Length < 32)
        {
            throw new InvalidOperationException("JWT signing secret must be at least 32 characters.");
        }

        return configured;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
