namespace ClimaSite.Application.Features.Questions.DTOs;

public class QuestionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AskerName { get; set; }
    public int HelpfulCount { get; set; }

    /// <summary>Whether the CURRENT user has cast a helpful vote on this question (false when anonymous).</summary>
    public bool HasVotedHelpful { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public int AnswerCount { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public string? AnswererName { get; set; }
    public bool IsOfficial { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }

    /// <summary>The CURRENT user's own vote: true = helpful, false = unhelpful, null = no vote / anonymous.</summary>
    public bool? UserVoteHelpful { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductQuestionsDto
{
    public Guid ProductId { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}
