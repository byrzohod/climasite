using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Reviews.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reviews.Queries;

public record GetPendingReviewsQuery : IRequest<PendingReviewModerationDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ReviewStatus? Status { get; init; }
}

public class GetPendingReviewsQueryHandler : IRequestHandler<GetPendingReviewsQuery, PendingReviewModerationDto>
{
    private readonly IApplicationDbContext _context;

    public GetPendingReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PendingReviewModerationDto> Handle(
        GetPendingReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
            .Include(r => r.User)
            .Where(r => request.Status.HasValue
                ? r.Status == request.Status.Value
                : r.Status == ReviewStatus.Pending || r.Status == ReviewStatus.Flagged);

        var pendingCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(r => r.Status == ReviewStatus.Flagged)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                ProductSlug = r.Product.Slug,
                Rating = r.Rating,
                Title = r.Title,
                Content = r.Content,
                ReviewerName = r.User.FirstName != null ? $"{r.User.FirstName} {r.User.LastName}" : r.User.Email,
                ReviewerEmail = r.User.Email,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                Status = r.Status,
                HelpfulCount = r.HelpfulCount,
                UnhelpfulCount = r.UnhelpfulCount,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PendingReviewModerationDto
        {
            PendingReviews = pendingCount,
            Reviews = reviews
        };
    }
}
