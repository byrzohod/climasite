namespace ClimaSite.Core.Entities;

/// <summary>
/// Tracks individual user votes on reviews to prevent duplicate voting.
/// </summary>
public class ReviewVote : BaseEntity
{
    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsHelpful { get; private set; }

    // Navigation properties
    public virtual Review Review { get; private set; } = null!;
    public virtual ApplicationUser User { get; private set; } = null!;

    private ReviewVote() { }

    public ReviewVote(Guid reviewId, Guid userId, bool isHelpful)
    {
        ReviewId = reviewId;
        UserId = userId;
        IsHelpful = isHelpful;
    }

    /// <summary>
    /// Changes the vote type (helpful to unhelpful or vice versa).
    /// </summary>
    public void ChangeVote(bool isHelpful)
    {
        IsHelpful = isHelpful;
        SetUpdatedAt();
    }
}
