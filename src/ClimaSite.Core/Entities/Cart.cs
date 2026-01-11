namespace ClimaSite.Core.Entities;

public class Cart : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; private set; }
    public virtual ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private Cart() { }

    public Cart(Guid? userId, string? sessionId)
    {
        if (userId == null && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Either userId or sessionId must be provided");

        UserId = userId;
        SessionId = sessionId;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
        SessionId = null;
        SetUpdatedAt();
    }

    public void ExtendExpiration(int days = 7)
    {
        ExpiresAt = DateTime.UtcNow.AddDays(days);
        SetUpdatedAt();
    }

    public CartItem? GetItem(Guid variantId) =>
        Items.FirstOrDefault(i => i.VariantId == variantId);

    public CartItem AddItem(Guid productId, Guid variantId, int quantity, decimal unitPrice)
    {
        var existingItem = GetItem(variantId);

        if (existingItem != null)
        {
            existingItem.SetQuantity(existingItem.Quantity + quantity);
            return existingItem;
        }

        var item = new CartItem(Id, productId, variantId, quantity, unitPrice);
        Items.Add(item);
        SetUpdatedAt();
        return item;
    }

    public void RemoveItem(Guid variantId)
    {
        var item = GetItem(variantId);
        if (item != null)
        {
            Items.Remove(item);
            SetUpdatedAt();
        }
    }

    public void UpdateItemQuantity(Guid variantId, int quantity)
    {
        var item = GetItem(variantId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                Items.Remove(item);
            }
            else
            {
                item.SetQuantity(quantity);
            }
            SetUpdatedAt();
        }
    }

    public void Clear()
    {
        Items.Clear();
        SetUpdatedAt();
    }

    public int TotalItems => Items.Sum(i => i.Quantity);

    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsGuestCart => UserId == null;
}
