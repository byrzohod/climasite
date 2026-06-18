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
    /// Only the methods the UI actually offers (standard/express/overnight) are priced;
    /// an unknown method must never resolve to free shipping (it would let a client POST an
    /// undisplayed method to ship for €0). Server-side validators reject unknown methods, so the
    /// default rate here is a defence-in-depth fallback rather than a billable path.
    /// </summary>
    // NOTE: the UI labels standard shipping as "free" and renders prices with a "$" symbol,
    // whereas the server charges €5.99 standard / €15.99 express / €19.99 overnight in EUR. That
    // label/currency-display mismatch is a separate, pre-existing front-end display inconsistency
    // and is intentionally out of scope for this backend fix.
    public static decimal GetShippingCost(string? method)
    {
        return (method?.ToLowerInvariant()) switch
        {
            "express" => 15.99m,
            "standard" => 5.99m,
            "overnight" => 19.99m,
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
