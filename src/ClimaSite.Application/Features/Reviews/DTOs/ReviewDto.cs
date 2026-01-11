namespace ClimaSite.Application.Features.Reviews.DTOs;

public class ReviewDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsVerifiedPurchase { get; init; }
    public int HelpfulCount { get; init; }
    public int UnhelpfulCount { get; init; }
    public string? AdminResponse { get; init; }
    public DateTime? AdminRespondedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ProductReviewSummaryDto
{
    public Guid ProductId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
}
