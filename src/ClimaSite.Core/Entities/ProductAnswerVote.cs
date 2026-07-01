namespace ClimaSite.Core.Entities;

/// <summary>
/// Tracks an individual user's helpful/unhelpful vote on a product answer to prevent duplicate
/// voting. Mirrors <see cref="ReviewVote"/>: one row per (answer, user) with a flip-able direction.
/// </summary>
public class ProductAnswerVote : BaseEntity
{
    public Guid AnswerId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsHelpful { get; private set; }

    // Navigation properties
    public virtual ProductAnswer Answer { get; private set; } = null!;
    public virtual ApplicationUser User { get; private set; } = null!;

    private ProductAnswerVote() { }

    public ProductAnswerVote(Guid answerId, Guid userId, bool isHelpful)
    {
        AnswerId = answerId;
        UserId = userId;
        IsHelpful = isHelpful;
    }

    /// <summary>
    /// Flips the vote direction (helpful to unhelpful or vice versa).
    /// </summary>
    public void ChangeVote(bool isHelpful)
    {
        IsHelpful = isHelpful;
        SetUpdatedAt();
    }
}
