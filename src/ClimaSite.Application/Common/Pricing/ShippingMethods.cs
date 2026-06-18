namespace ClimaSite.Application.Common.Pricing;

/// <summary>
/// The server-side allow-list of shipping methods a client may request. This is the single source
/// of truth shared by the create-order and create-payment-intent validators so a client cannot
/// POST an undisplayed method (e.g. an old "free" value) to obtain a price the UI never offered.
/// Matching is case-insensitive.
/// </summary>
public static class ShippingMethods
{
    public const string Standard = "standard";
    public const string Express = "express";
    public const string Overnight = "overnight";

    /// <summary>The only shipping methods the checkout UI offers and the server will accept.</summary>
    public static readonly IReadOnlyList<string> Allowed = new[] { Standard, Express, Overnight };

    /// <summary>Human-readable, pipe-separated list for validation messages.</summary>
    public static string AllowedDisplay => string.Join("|", Allowed);

    /// <summary>True when <paramref name="method"/> is one of the allowed methods (case-insensitive).</summary>
    public static bool IsAllowed(string? method) =>
        method is not null
        && Allowed.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));
}
