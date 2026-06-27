using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClimaSite.Api.Configuration;

/// <summary>
/// Production fail-fast for Stripe configuration (SEC-07). The committed appsettings no longer carries
/// dummy keys, so a by-the-book deploy that forgets to set the real Stripe secrets fails loudly at
/// startup instead of silently booting with non-functional/placeholder keys. In Production each value
/// must (a) be present, (b) not be a known placeholder, and (c) have the real Stripe key SHAPE (correct
/// prefix + plausible length) — so an arbitrary non-Stripe string can't slip through either. No-op
/// outside Production (Dev supplies its own test keys; integration tests use a fake payment service).
/// </summary>
public static class StripeConfiguration
{
    private const int MinKeyLength = 16;

    // Real Stripe key prefixes: secret keys `sk_`/restricted `rk_`, publishable `pk_`, webhook `whsec_`.
    private static readonly string[] SecretPrefixes = { "sk_", "rk_" };
    private static readonly string[] PublishablePrefixes = { "pk_" };
    private static readonly string[] WebhookPrefixes = { "whsec_" };

    /// <summary>Throws if required Stripe config is missing, a placeholder, or not Stripe-shaped (Production only).</summary>
    public static void ValidateProductionConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var section = configuration.GetSection("Stripe");
        RequireRealKey(section["SecretKey"], "Stripe:SecretKey (env Stripe__SecretKey)", SecretPrefixes);
        RequireRealKey(section["WebhookSecret"], "Stripe:WebhookSecret (env Stripe__WebhookSecret)", WebhookPrefixes);
        RequireRealKey(section["PublishableKey"], "Stripe:PublishableKey (env Stripe__PublishableKey)", PublishablePrefixes);
    }

    private static void RequireRealKey(string? value, string name, string[] validPrefixes)
    {
        if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value) || !HasStripeShape(value, validPrefixes))
        {
            throw new InvalidOperationException(
                $"{name} must be a real Stripe key in Production (expected prefix {string.Join("/", validPrefixes)} and length ≥ {MinKeyLength}; " +
                "missing, placeholder/dummy, or non-Stripe-shaped values are rejected).");
        }
    }

    /// <summary>True when the value has a real Stripe key prefix and a plausible length.</summary>
    public static bool HasStripeShape(string value, string[] validPrefixes)
        => value.Length >= MinKeyLength
        && validPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>A committed dummy/placeholder value — never a real Stripe key.</summary>
    public static bool IsPlaceholder(string value)
        => value.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || value.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
        || value.Contains("changeme", StringComparison.OrdinalIgnoreCase)
        || value.Contains("example", StringComparison.OrdinalIgnoreCase)
        || value.Contains("yoursecret", StringComparison.OrdinalIgnoreCase);
}
