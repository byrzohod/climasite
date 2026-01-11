namespace ClimaSite.Core.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Navigation properties
    public virtual Order Order { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;
    public virtual ProductVariant Variant { get; private set; } = null!;

    private OrderItem() { }

    public OrderItem(
        Guid orderId,
        Guid productId,
        Guid variantId,
        string productName,
        string variantName,
        string sku,
        int quantity,
        decimal unitPrice)
    {
        OrderId = orderId;
        ProductId = productId;
        VariantId = variantId;
        SetProductName(productName);
        SetVariantName(variantName);
        SetSku(sku);
        SetQuantity(quantity);
        SetUnitPrice(unitPrice);
    }

    public void SetProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));

        ProductName = productName;
        SetUpdatedAt();
    }

    public void SetVariantName(string variantName)
    {
        if (string.IsNullOrWhiteSpace(variantName))
            throw new ArgumentException("Variant name cannot be empty", nameof(variantName));

        VariantName = variantName;
        SetUpdatedAt();
    }

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        Sku = sku;
        SetUpdatedAt();
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        Quantity = quantity;
        SetUpdatedAt();
    }

    public void SetUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        UnitPrice = unitPrice;
        SetUpdatedAt();
    }

    public decimal LineTotal => Quantity * UnitPrice;
}
