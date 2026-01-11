namespace ClimaSite.Core.Entities;

public class Review : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Content { get; private set; }
    public ReviewStatus Status { get; private set; } = ReviewStatus.Pending;
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulCount { get; private set; }
    public int UnhelpfulCount { get; private set; }
    public string? AdminResponse { get; private set; }
    public DateTime? AdminRespondedAt { get; private set; }

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual ApplicationUser User { get; private set; } = null!;
    public virtual Order? Order { get; private set; }

    private Review() { }

    public Review(Guid productId, Guid userId, int rating)
    {
        ProductId = productId;
        UserId = userId;
        SetRating(rating);
    }

    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
        SetUpdatedAt();
    }

    public void SetTitle(string? title)
    {
        if (title != null && title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));

        Title = title?.Trim();
        SetUpdatedAt();
    }

    public void SetContent(string? content)
    {
        if (content != null && content.Length > 5000)
            throw new ArgumentException("Content cannot exceed 5000 characters", nameof(content));

        Content = content?.Trim();
        SetUpdatedAt();
    }

    public void SetStatus(ReviewStatus status)
    {
        Status = status;
        SetUpdatedAt();
    }

    public void SetVerifiedPurchase(bool isVerified, Guid? orderId = null)
    {
        IsVerifiedPurchase = isVerified;
        OrderId = orderId;
        SetUpdatedAt();
    }

    public void AddHelpfulVote()
    {
        HelpfulCount++;
        SetUpdatedAt();
    }

    public void AddUnhelpfulVote()
    {
        UnhelpfulCount++;
        SetUpdatedAt();
    }

    public void SetAdminResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("Admin response cannot be empty", nameof(response));

        AdminResponse = response.Trim();
        AdminRespondedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public int TotalVotes => HelpfulCount + UnhelpfulCount;

    public double HelpfulPercentage => TotalVotes > 0 ? (double)HelpfulCount / TotalVotes * 100 : 0;
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
    Flagged
}
