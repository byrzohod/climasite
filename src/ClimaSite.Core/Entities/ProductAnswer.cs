namespace ClimaSite.Core.Entities;

public class ProductAnswer : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public Guid? UserId { get; private set; }
    public string AnswerText { get; private set; } = string.Empty;
    public string? AnswererName { get; private set; }
    public bool IsOfficial { get; private set; }
    public AnswerStatus Status { get; private set; } = AnswerStatus.Pending;
    public int HelpfulCount { get; private set; }
    public int UnhelpfulCount { get; private set; }

    // Navigation properties
    public virtual ProductQuestion Question { get; private set; } = null!;
    public virtual ApplicationUser? User { get; private set; }

    private ProductAnswer() { }

    public ProductAnswer(Guid questionId, string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        QuestionId = questionId;
        SetAnswerText(answerText);
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
        SetUpdatedAt();
    }

    public void SetAnswererName(string? name)
    {
        AnswererName = name?.Trim();
        SetUpdatedAt();
    }

    public void SetAnswerText(string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        if (answerText.Length > 5000)
            throw new ArgumentException("Answer text cannot exceed 5000 characters", nameof(answerText));

        AnswerText = answerText.Trim();
        SetUpdatedAt();
    }

    public void SetOfficial(bool isOfficial)
    {
        IsOfficial = isOfficial;
        SetUpdatedAt();
    }

    public void SetStatus(AnswerStatus status)
    {
        Status = status;
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

    public int TotalVotes => HelpfulCount + UnhelpfulCount;
    public double HelpfulPercentage => TotalVotes > 0 ? (double)HelpfulCount / TotalVotes * 100 : 0;
}

public enum AnswerStatus
{
    Pending,
    Approved,
    Rejected,
    Flagged
}
