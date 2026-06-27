using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Configuration;

/// <summary>
/// Production fail-fast for Stripe configuration (SEC-07). The committed appsettings no longer carries
/// dummy keys, so a by-the-book deploy that forgets to set the real Stripe secrets fails loudly at
/// startup instead of silently booting with non-functional/placeholder keys. No-op outside Production
/// (Dev supplies its own test keys; integration tests use a fake payment service).
/// </summary>
public static class StripeConfiguration
{
    /// <summary>Throws if required Stripe config is missing or a placeholder when running in Production.</summary>
    public static void ValidateProductionConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var section = configuration.GetSection("Stripe");
        RequireRealValue(section["SecretKey"], "Stripe:SecretKey (env Stripe__SecretKey)");
        RequireRealValue(section["WebhookSecret"], "Stripe:WebhookSecret (env Stripe__WebhookSecret)");
        RequireRealValue(section["PublishableKey"], "Stripe:PublishableKey (env Stripe__PublishableKey)");
    }

    private static void RequireRealValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
        {
            throw new InvalidOperationException(
                $"{name} must be set to a real value in Production — missing or placeholder/dummy keys are not allowed.");
        }
    }

    /// <summary>A committed dummy/placeholder value — never a real Stripe key.</summary>
    public static bool IsPlaceholder(string value)
        => value.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || value.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
        || value.Contains("changeme", StringComparison.OrdinalIgnoreCase)
        || value.Contains("yoursecret", StringComparison.OrdinalIgnoreCase);
}
