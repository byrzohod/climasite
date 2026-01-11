using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<PagedResult<Review>> GetByProductIdAsync(Guid productId, int page = 1, int pageSize = 20, ReviewStatus? status = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Review>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ReviewSummary> GetSummaryByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<PagedResult<Review>> GetPendingReviewsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public record ReviewSummary(
    double AverageRating,
    int TotalReviews,
    int VerifiedPurchaseCount,
    Dictionary<int, int> RatingDistribution
);
