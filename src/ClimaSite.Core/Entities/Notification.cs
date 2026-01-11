namespace ClimaSite.Core.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public Dictionary<string, object> Data { get; private set; } = new();
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Navigation properties
    public virtual ApplicationUser User { get; private set; } = null!;

    private Notification() { }

    public Notification(Guid userId, string type, string title, string message)
    {
        UserId = userId;
        SetType(type);
        SetTitle(title);
        SetMessage(message);
    }

    public void SetType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Notification type cannot be empty", nameof(type));

        Type = type;
        SetUpdatedAt();
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title cannot be empty", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));

        Title = title.Trim();
        SetUpdatedAt();
    }

    public void SetMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Notification message cannot be empty", nameof(message));

        if (message.Length > 1000)
            throw new ArgumentException("Message cannot exceed 1000 characters", nameof(message));

        Message = message.Trim();
        SetUpdatedAt();
    }

    public void SetLink(string? link)
    {
        Link = link;
        SetUpdatedAt();
    }

    public void SetData(Dictionary<string, object>? data)
    {
        Data = data ?? new Dictionary<string, object>();
        SetUpdatedAt();
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            SetUpdatedAt();
        }
    }

    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;
            SetUpdatedAt();
        }
    }
}

public static class NotificationTypes
{
    public const string OrderPlaced = "order_placed";
    public const string OrderShipped = "order_shipped";
    public const string OrderDelivered = "order_delivered";
    public const string OrderCancelled = "order_cancelled";
    public const string PaymentReceived = "payment_received";
    public const string PaymentFailed = "payment_failed";
    public const string ReviewPosted = "review_posted";
    public const string WishlistPriceDrop = "wishlist_price_drop";
    public const string WishlistBackInStock = "wishlist_back_in_stock";
    public const string AccountUpdate = "account_update";
    public const string PasswordChanged = "password_changed";
    public const string Promotional = "promotional";
}
