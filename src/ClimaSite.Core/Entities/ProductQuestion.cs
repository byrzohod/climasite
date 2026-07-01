namespace ClimaSite.Core.Entities;

public class ProductQuestion : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? UserId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public string? AskerName { get; private set; }
    public string? AskerEmail { get; private set; }
    public QuestionStatus Status { get; private set; } = QuestionStatus.Pending;
    public int HelpfulCount { get; private set; }
    public DateTime? AnsweredAt { get; private set; }

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual ApplicationUser? User { get; private set; }
    public virtual ICollection<ProductAnswer> Answers { get; private set; } = new List<ProductAnswer>();

    private ProductQuestion() { }

    public ProductQuestion(Guid productId, string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty", nameof(questionText));

        ProductId = productId;
        SetQuestionText(questionText);
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
        SetUpdatedAt();
    }

    public void SetAskerInfo(string? name, string? email)
    {
        AskerName = name?.Trim();
        AskerEmail = email?.Trim();
        SetUpdatedAt();
    }

    public void SetQuestionText(string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty", nameof(questionText));

        if (questionText.Length > 2000)
            throw new ArgumentException("Question text cannot exceed 2000 characters", nameof(questionText));

        QuestionText = questionText.Trim();
        SetUpdatedAt();
    }

    public void SetStatus(QuestionStatus status)
    {
        // A question's status transition must NOT guess answered-state: answered-state is owned solely by
        // RefreshAnsweredState (driven off approved-answer existence at the answer-moderation boundary).
        Status = status;
        SetUpdatedAt();
    }

    /// <summary>
    /// Reconciles <see cref="AnsweredAt"/> with the presence of an approved answer, derived from the loaded
    /// <see cref="Answers"/> collection. Idempotent: stamps the timestamp on the first approved answer,
    /// clears it when the last approved answer is removed/un-approved, and never overwrites an existing
    /// timestamp. This is the SINGLE writer of answered-state — a still-<c>Pending</c> answer can no longer
    /// flag the question as answered (B-038). Callers MUST have the <see cref="Answers"/> collection loaded
    /// (e.g. via <c>Include(...).ThenInclude(...)</c>). See <c>ModerateAnswerCommand</c>.
    /// </summary>
    public void RefreshAnsweredState()
    {
        var hasApprovedAnswer = Answers.Any(a => a.Status == AnswerStatus.Approved);
        if (hasApprovedAnswer && !AnsweredAt.HasValue)
        {
            AnsweredAt = DateTime.UtcNow;
            SetUpdatedAt();
        }
        else if (!hasApprovedAnswer && AnsweredAt.HasValue)
        {
            AnsweredAt = null;
            SetUpdatedAt();
        }
    }

    public void AddHelpfulVote()
    {
        HelpfulCount++;
        SetUpdatedAt();
    }

    /// <summary>
    /// Decrements the helpful count, floored at zero. Kept for the count-floor invariant and admin
    /// paths only — the per-voter vote handler mutates the count via atomic SQL, never this method
    /// (a load-decrement-save would lose concurrent updates). See B-039 unit-plan §9.
    /// </summary>
    public void RemoveHelpfulVote()
    {
        if (HelpfulCount > 0)
        {
            HelpfulCount--;
        }
        SetUpdatedAt();
    }

    public bool HasApprovedAnswer => Answers.Any(a => a.Status == AnswerStatus.Approved);
    public int AnswerCount => Answers.Count(a => a.Status == AnswerStatus.Approved);
}

public enum QuestionStatus
{
    Pending,
    Approved,
    Rejected,
    Flagged
}
