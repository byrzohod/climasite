namespace ClimaSite.Core.Entities;

public class Wishlist : BaseEntity
{
    public Guid UserId { get; private set; }
    public bool IsPublic { get; private set; }
    public string? ShareToken { get; private set; }

    // Navigation properties
    public virtual ApplicationUser User { get; private set; } = null!;
    public virtual ICollection<WishlistItem> Items { get; private set; } = new List<WishlistItem>();

    private Wishlist() { }

    public Wishlist(Guid userId)
    {
        UserId = userId;
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        if (isPublic && string.IsNullOrEmpty(ShareToken))
        {
            ShareToken = GenerateShareToken();
        }
        SetUpdatedAt();
    }

    public void RegenerateShareToken()
    {
        ShareToken = GenerateShareToken();
        SetUpdatedAt();
    }

    private static string GenerateShareToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public WishlistItem? GetItem(Guid productId) =>
        Items.FirstOrDefault(i => i.ProductId == productId);

    public WishlistItem AddItem(Guid productId, string? note = null, int priority = 0)
    {
        var existingItem = GetItem(productId);
        if (existingItem != null)
        {
            return existingItem;
        }

        var item = new WishlistItem(Id, productId);
        if (note != null) item.SetNote(note);
        item.SetPriority(priority);

        Items.Add(item);
        SetUpdatedAt();
        return item;
    }

    public void RemoveItem(Guid productId)
    {
        var item = GetItem(productId);
        if (item != null)
        {
            Items.Remove(item);
            SetUpdatedAt();
        }
    }

    public void Clear()
    {
        Items.Clear();
        SetUpdatedAt();
    }

    public int TotalItems => Items.Count;
}
