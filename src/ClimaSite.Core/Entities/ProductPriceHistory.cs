namespace ClimaSite.Core.Entities;

public class ProductPriceHistory : BaseEntity
{
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public PriceChangeReason Reason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation property
    public Product Product { get; private set; } = null!;

    private ProductPriceHistory() { }

    public static ProductPriceHistory Create(
        Guid productId,
        decimal price,
        decimal? compareAtPrice,
        PriceChangeReason reason,
        string? notes = null)
    {
        return new ProductPriceHistory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Price = price,
            CompareAtPrice = compareAtPrice,
            RecordedAt = DateTime.UtcNow,
            Reason = reason,
            Notes = notes
        };
    }

    public static ProductPriceHistory CreateInitial(
        Guid productId,
        decimal price,
        decimal? compareAtPrice)
    {
        return Create(productId, price, compareAtPrice, PriceChangeReason.Initial, "Initial price");
    }
}

public enum PriceChangeReason
{
    Initial,
    PriceChange,
    Promotion,
    PromotionEnd,
    SeasonalSale,
    CostAdjustment
}
