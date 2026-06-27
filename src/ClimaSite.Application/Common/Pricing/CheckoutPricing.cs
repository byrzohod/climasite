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
    /// Subtotal at or above which standard shipping is free (DEC-SHIPPING).
    /// HVAC is big-ticket, so most real orders ship free; small orders cover shipping.
    /// </summary>
    public const decimal FreeShippingThreshold = 50m;

    /// <summary>
    /// Resolves the shipping cost for a shipping method given the order subtotal.
    /// Matching is case-insensitive; unknown methods fall back to the default rate.
    /// Only the methods the UI actually offers (standard/express/overnight) are priced;
    /// an unknown method must never resolve to free shipping (it would let a client POST an
    /// undisplayed method to ship for €0). Server-side validators reject unknown methods, so the
    /// default rate here is a defence-in-depth fallback rather than a billable path.
    /// </summary>
    // NOTE (DEC-SHIPPING): standard shipping is free when the subtotal is at or above
    // FreeShippingThreshold (€50), otherwise €5.99. Express €15.99 / overnight €19.99 are flat
    // regardless of subtotal. This is the single source of truth the UI mirrors so the amount
    // displayed at checkout always equals the amount charged.
    public static decimal GetShippingCost(string? method, decimal subtotal)
    {
        return (method?.ToLowerInvariant()) switch
        {
            "express" => 15.99m,
            "standard" => subtotal >= FreeShippingThreshold ? 0m : 5.99m,
            "overnight" => 19.99m,
            _ => 9.99m
        };
    }

    /// <summary>Calculates VAT on the subtotal, rounded to 2 decimals.</summary>
    public static decimal GetTax(decimal subtotal) => Math.Round(subtotal * TaxRate, 2);

    /// <summary>Calculates the grand total: subtotal + shipping + tax.</summary>
    public static decimal CalculateTotal(decimal subtotal, string? method)
        => subtotal + GetShippingCost(method, subtotal) + GetTax(subtotal);

    /// <summary>
    /// Converts a major-unit amount (e.g. EUR) to Stripe minor units (cents),
    /// rounding away from zero so the charged amount is never short.
    /// </summary>
    public static long ToMinorUnits(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
