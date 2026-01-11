namespace ClimaSite.Core.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal PriceAdjustment { get; private set; }
    public Dictionary<string, object> Attributes { get; private set; } = new();
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    private ProductVariant() { }

    public ProductVariant(Guid productId, string sku, string name)
    {
        ProductId = productId;
        SetSku(sku);
        SetName(name);
    }

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Variant SKU cannot be empty", nameof(sku));

        if (sku.Length > 50)
            throw new ArgumentException("Variant SKU cannot exceed 50 characters", nameof(sku));

        Sku = sku.Trim().ToUpperInvariant();
        SetUpdatedAt();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Variant name cannot exceed 100 characters", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetPriceAdjustment(decimal priceAdjustment)
    {
        PriceAdjustment = priceAdjustment;
        SetUpdatedAt();
    }

    public void SetAttributes(Dictionary<string, object>? attributes)
    {
        Attributes = attributes ?? new Dictionary<string, object>();
        SetUpdatedAt();
    }

    public void SetAttribute(string key, object value)
    {
        Attributes[key] = value;
        SetUpdatedAt();
    }

    public void SetStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

        StockQuantity = stockQuantity;
        SetUpdatedAt();
    }

    public void AdjustStock(int adjustment)
    {
        var newQuantity = StockQuantity + adjustment;
        if (newQuantity < 0)
            throw new InvalidOperationException($"Cannot reduce stock below zero. Current: {StockQuantity}, Adjustment: {adjustment}");

        StockQuantity = newQuantity;
        SetUpdatedAt();
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new ArgumentException("Low stock threshold cannot be negative", nameof(threshold));

        LowStockThreshold = threshold;
        SetUpdatedAt();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        SetUpdatedAt();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        SetUpdatedAt();
    }

    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool InStock => StockQuantity > 0;

    public decimal GetPrice(decimal basePrice) => basePrice + PriceAdjustment;
}
