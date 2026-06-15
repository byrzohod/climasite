namespace ClimaSite.Application.Common.Pricing;

/// <summary>
/// Single source of truth for checkout pricing so the amount displayed to the
/// customer, the amount charged via Stripe, and the persisted order total are
/// always computed the same way (BUG-02). The store charge currency is EUR.
/// </summary>
public static class CheckoutPricing
{
    /// <summary>The currency every checkout amount is computed and charged in.</summary>
    public const string Currency = "EUR";

    /// <summary>VAT rate applied to the subtotal (20% EU average).</summary>
    public const decimal TaxRate = 0.20m;

    /// <summary>
    /// Resolves the shipping cost for a shipping method. Matching is
    /// case-insensitive; unknown methods fall back to the default rate.
    /// </summary>
    public static decimal GetShippingCost(string? method)
    {
        return (method?.ToLowerInvariant()) switch
        {
            "express" => 15.99m,
            "standard" => 5.99m,
            "free" => 0m,
            _ => 9.99m
        };
    }

    /// <summary>Calculates VAT on the subtotal, rounded to 2 decimals.</summary>
    public static decimal GetTax(decimal subtotal) => Math.Round(subtotal * TaxRate, 2);

    /// <summary>Calculates the grand total: subtotal + shipping + tax.</summary>
    public static decimal CalculateTotal(decimal subtotal, string? method)
        => subtotal + GetShippingCost(method) + GetTax(subtotal);

    /// <summary>
    /// Converts a major-unit amount (e.g. EUR) to Stripe minor units (cents),
    /// rounding away from zero so the charged amount is never short.
    /// </summary>
    public static long ToMinorUnits(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
