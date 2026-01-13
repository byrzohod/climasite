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
        Status = status;
        if (status == QuestionStatus.Approved && !AnsweredAt.HasValue && Answers.Any())
        {
            AnsweredAt = DateTime.UtcNow;
        }
        SetUpdatedAt();
    }

    public void MarkAsAnswered()
    {
        AnsweredAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void AddHelpfulVote()
    {
        HelpfulCount++;
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
