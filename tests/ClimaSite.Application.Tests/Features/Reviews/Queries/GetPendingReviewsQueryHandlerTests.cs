using ClimaSite.Application.Features.Reviews.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Reviews.Queries;

public class GetPendingReviewsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetPendingReviewsQueryHandler CreateHandler() => new(_context);

    /// <summary>
    /// Seeds a review with its <c>Product</c> and <c>User</c> navigations wired via reflection — the
    /// MockDbContext does not perform EF Include joins, so the handler's projection of
    /// <c>r.Product.*</c>/<c>r.User.*</c> must be satisfied manually.
    /// </summary>
    private Review SeedReview(ReviewStatus status, string productName = "Pending AC")
    {
        var product = new Product("PR-001", productName, "pending-ac", 700m);
        var review = new Review(product.Id, Guid.NewGuid(), 3);
        review.SetStatus(status);

        var user = new ApplicationUser
        {
            Id = review.UserId,
            Email = "reviewer@example.com",
            UserName = "reviewer@example.com",
            FirstName = "Reviewer",
            LastName = "One"
        };

        typeof(Review).GetProperty(nameof(Review.Product))!.SetValue(review, product);
        typeof(Review).GetProperty(nameof(Review.User))!.SetValue(review, user);

        _context.AddProduct(product);
        _context.Reviews.Add(review);
        return review;
    }

    [Fact]
    public async Task Handle_DefaultsToPendingAndFlagged()
    {
        SeedReview(ReviewStatus.Pending);
        SeedReview(ReviewStatus.Flagged);
        SeedReview(ReviewStatus.Approved);

        var result = await CreateHandler().Handle(new GetPendingReviewsQuery(), CancellationToken.None);

        result.PendingReviews.Should().Be(2, "approved reviews are excluded by default");
        result.Reviews.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FiltersBySpecificStatus()
    {
        SeedReview(ReviewStatus.Pending);
        SeedReview(ReviewStatus.Rejected);

        var result = await CreateHandler().Handle(
            new GetPendingReviewsQuery { Status = ReviewStatus.Rejected },
            CancellationToken.None);

        result.PendingReviews.Should().Be(1);
        result.Reviews.Single().Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public async Task Handle_MapsProductAndReviewerDetails()
    {
        SeedReview(ReviewStatus.Pending, productName: "Mappable AC");

        var result = await CreateHandler().Handle(new GetPendingReviewsQuery(), CancellationToken.None);

        var dto = result.Reviews.Single();
        dto.ProductName.Should().Be("Mappable AC");
        dto.ProductSlug.Should().Be("pending-ac");
        dto.ReviewerName.Should().Be("Reviewer One");
        dto.ReviewerEmail.Should().Be("reviewer@example.com");
    }

    [Fact]
    public async Task Handle_FlaggedReviewsSortFirst()
    {
        SeedReview(ReviewStatus.Pending);
        SeedReview(ReviewStatus.Flagged);

        var result = await CreateHandler().Handle(new GetPendingReviewsQuery(), CancellationToken.None);

        result.Reviews.First().Status.Should().Be(ReviewStatus.Flagged);
    }
}
