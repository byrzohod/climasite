namespace ClimaSite.Core.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Navigation properties
    public virtual Cart Cart { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;
    public virtual ProductVariant Variant { get; private set; } = null!;

    private CartItem() { }

    public CartItem(Guid cartId, Guid productId, Guid variantId, int quantity, decimal unitPrice)
    {
        CartId = cartId;
        ProductId = productId;
        VariantId = variantId;
        SetQuantity(quantity);
        SetUnitPrice(unitPrice);
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
