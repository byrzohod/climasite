namespace ClimaSite.Core.Entities;

/// <summary>
/// Tracks an individual user's "helpful" vote on a product question to prevent duplicate voting.
/// Questions are helpful-only, so the presence of a row IS the vote (mirrors <see cref="ReviewVote"/>).
/// </summary>
public class ProductQuestionVote : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public virtual ProductQuestion Question { get; private set; } = null!;
    public virtual ApplicationUser User { get; private set; } = null!;

    private ProductQuestionVote() { }

    public ProductQuestionVote(Guid questionId, Guid userId)
    {
        QuestionId = questionId;
        UserId = userId;
    }
}
