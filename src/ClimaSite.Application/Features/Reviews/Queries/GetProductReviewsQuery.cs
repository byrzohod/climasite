using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Reviews.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reviews.Queries;

public record GetProductReviewsQuery : IRequest<PaginatedList<ReviewDto>>
{
    public Guid ProductId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; } = "newest";
}

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, PaginatedList<ReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ReviewDto>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == request.ProductId && r.Status == ReviewStatus.Approved);

        query = request.SortBy?.ToLower() switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "helpful" => query.OrderByDescending(r => r.HelpfulCount),
            "rating_high" => query.OrderByDescending(r => r.Rating),
            "rating_low" => query.OrderBy(r => r.Rating),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var projectedQuery = query.Select(r => new ReviewDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            UserId = r.UserId,
            UserName = $"{r.User.FirstName} {r.User.LastName.Substring(0, 1)}.",
            Rating = r.Rating,
            Title = r.Title,
            Content = r.Content,
            Status = r.Status.ToString(),
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            HelpfulCount = r.HelpfulCount,
            UnhelpfulCount = r.UnhelpfulCount,
            AdminResponse = r.AdminResponse,
            AdminRespondedAt = r.AdminRespondedAt,
            CreatedAt = r.CreatedAt
        });

        return await PaginatedList<ReviewDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

public record GetProductReviewSummaryQuery : IRequest<ProductReviewSummaryDto>
{
    public Guid ProductId { get; init; }
}

public class GetProductReviewSummaryQueryHandler : IRequestHandler<GetProductReviewSummaryQuery, ProductReviewSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductReviewSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductReviewSummaryDto> Handle(
        GetProductReviewSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == request.ProductId && r.Status == ReviewStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        var distribution = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };

        foreach (var rating in reviews)
        {
            distribution[rating]++;
        }

        return new ProductReviewSummaryDto
        {
            ProductId = request.ProductId,
            AverageRating = reviews.Any() ? (decimal)reviews.Average() : 0,
            TotalReviews = reviews.Count,
            RatingDistribution = distribution
        };
    }
}
