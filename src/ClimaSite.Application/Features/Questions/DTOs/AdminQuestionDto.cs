using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Questions.DTOs;

public class AdminQuestionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? AskerName { get; set; }
    public string? AskerEmail { get; set; }
    public QuestionStatus Status { get; set; }
    public int HelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public int TotalAnswers { get; set; }
    public int PendingAnswers { get; set; }
}

public class AdminAnswerDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public string? AnswererName { get; set; }
    public bool IsOfficial { get; set; }
    public AnswerStatus Status { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingModerationDto
{
    public int PendingQuestions { get; set; }
    public int PendingAnswers { get; set; }
    public List<AdminQuestionDto> Questions { get; set; } = new();
    public List<AdminAnswerDto> Answers { get; set; } = new();
}
