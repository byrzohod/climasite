namespace ClimaSite.Core.Entities;

public class WishlistItem : BaseEntity
{
    public Guid WishlistId { get; private set; }
    public Guid ProductId { get; private set; }
    public string? Note { get; private set; }
    public int Priority { get; private set; }
    public decimal? PriceWhenAdded { get; private set; }
    public bool NotifyOnSale { get; private set; }

    // Navigation properties
    public virtual Wishlist Wishlist { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;

    private WishlistItem() { }

    public WishlistItem(Guid wishlistId, Guid productId)
    {
        WishlistId = wishlistId;
        ProductId = productId;
    }

    public void SetNote(string? note)
    {
        if (note != null && note.Length > 500)
            throw new ArgumentException("Note cannot exceed 500 characters", nameof(note));

        Note = note?.Trim();
        SetUpdatedAt();
    }

    public void SetPriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentException("Priority cannot be negative", nameof(priority));

        Priority = priority;
        SetUpdatedAt();
    }

    public void SetPriceWhenAdded(decimal? price)
    {
        PriceWhenAdded = price;
        SetUpdatedAt();
    }

    public void SetNotifyOnSale(bool notify)
    {
        NotifyOnSale = notify;
        SetUpdatedAt();
    }
}
