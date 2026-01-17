using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Reviews.DTOs;

public class AdminReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerEmail { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public ReviewStatus Status { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingReviewModerationDto
{
    public int PendingReviews { get; set; }
    public List<AdminReviewDto> Reviews { get; set; } = new();
}
