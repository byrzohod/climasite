namespace ClimaSite.Application.Common.Pricing;

/// <summary>
/// Single source of truth for how a product's display prices are derived (BUG-06).
///
/// Contract:
/// <list type="bullet">
/// <item><c>BasePrice</c> is the CURRENT selling price — what the customer actually
/// pays. It is used for charging and order calculations and is never repurposed.</item>
/// <item><c>CompareAtPrice</c> is the ORIGINAL ("compare-at") list price, which is
/// higher than <c>BasePrice</c> when the product is on sale.</item>
/// <item>A product is on sale when <c>CompareAtPrice</c> has a value greater than
/// <c>BasePrice</c>.</item>
/// <item>On the wire and in the UI, <c>SalePrice</c> carries the original compare-at
/// price (shown struck-through when on sale, null otherwise). The active/emphasized
/// price is always <c>BasePrice</c>.</item>
/// </list>
/// </summary>
public static class ProductPricing
{
    /// <summary>
    /// Returns true when the product is on sale: there is a compare-at price and it
    /// is strictly higher than the current selling (base) price.
    /// </summary>
    public static bool IsOnSale(decimal basePrice, decimal? compareAtPrice)
        => compareAtPrice.HasValue && compareAtPrice.Value > basePrice;

    /// <summary>
    /// Returns the struck-through original price when on sale, otherwise null.
    /// This is the value carried by the <c>SalePrice</c> DTO field.
    /// </summary>
    public static decimal? GetSalePrice(decimal basePrice, decimal? compareAtPrice)
        => IsOnSale(basePrice, compareAtPrice) ? compareAtPrice : null;

    /// <summary>
    /// Returns the discount percentage off the original price, rounded to a whole
    /// number, or 0 when the product is not on sale.
    /// </summary>
    public static decimal GetDiscountPercentage(decimal basePrice, decimal? compareAtPrice)
        => IsOnSale(basePrice, compareAtPrice)
            ? Math.Round((compareAtPrice!.Value - basePrice) / compareAtPrice.Value * 100, 0)
            : 0;
}
