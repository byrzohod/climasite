using ClimaSite.Application.Features.Reviews.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Reviews.Queries;

public class GetProductReviewsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetProductReviewsQueryHandler CreateHandler() => new(_context);
    private GetProductReviewSummaryQueryHandler CreateSummaryHandler() => new(_context);

    /// <summary>
    /// Seeds an approved review with its <c>User</c> navigation wired via reflection — the
    /// MockDbContext does not perform EF Include joins, so the handler's projection of
    /// <c>r.User.FirstName</c>/<c>r.User.LastName</c> must be satisfied manually.
    /// </summary>
    private Review SeedApprovedReview(
        Guid productId,
        int rating,
        string firstName = "Jane",
        string lastName = "Buyer",
        int helpfulCount = 0)
    {
        var review = new Review(productId, Guid.NewGuid(), rating);
        review.SetStatus(ReviewStatus.Approved);
        for (var i = 0; i < helpfulCount; i++)
        {
            review.AddHelpfulVote();
        }

        var user = new ApplicationUser
        {
            Id = review.UserId,
            Email = $"{firstName}@example.com",
            UserName = $"{firstName}@example.com",
            FirstName = firstName,
            LastName = lastName
        };
        typeof(Review).GetProperty(nameof(Review.User))!.SetValue(review, user);

        _context.Reviews.Add(review);
        return review;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyApprovedReviewsForProduct()
    {
        var productId = Guid.NewGuid();
        SeedApprovedReview(productId, 5);

        var pending = new Review(productId, Guid.NewGuid(), 1);
        typeof(Review).GetProperty(nameof(Review.User))!
            .SetValue(pending, new ApplicationUser { Id = pending.UserId, FirstName = "X", LastName = "Y" });
        _context.Reviews.Add(pending);

        SeedApprovedReview(Guid.NewGuid(), 4); // different product

        var result = await CreateHandler().Handle(
            new GetProductReviewsQuery { ProductId = productId },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Status.Should().Be(ReviewStatus.Approved.ToString());
    }

    [Fact]
    public async Task Handle_FormatsUserName_AsFirstNamePlusLastInitial()
    {
        var productId = Guid.NewGuid();
        SeedApprovedReview(productId, 5, firstName: "Maria", lastName: "Petrova");

        var result = await CreateHandler().Handle(
            new GetProductReviewsQuery { ProductId = productId },
            CancellationToken.None);

        result.Items[0].UserName.Should().Be("Maria P.");
    }

    [Fact]
    public async Task Handle_SortsByHelpful_WhenRequested()
    {
        var productId = Guid.NewGuid();
        SeedApprovedReview(productId, 3, helpfulCount: 1);
        SeedApprovedReview(productId, 4, helpfulCount: 9);

        var result = await CreateHandler().Handle(
            new GetProductReviewsQuery { ProductId = productId, SortBy = "helpful" },
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].HelpfulCount.Should().Be(9);
        result.Items[1].HelpfulCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Paginates()
    {
        var productId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            SeedApprovedReview(productId, 4);
        }

        var result = await CreateHandler().Handle(
            new GetProductReviewsQuery { ProductId = productId, PageNumber = 1, PageSize = 2 },
            CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Summary_ComputesAverageAndDistribution()
    {
        var productId = Guid.NewGuid();
        SeedApprovedReview(productId, 5);
        SeedApprovedReview(productId, 3);
        SeedApprovedReview(productId, 4);

        var result = await CreateSummaryHandler().Handle(
            new GetProductReviewSummaryQuery { ProductId = productId },
            CancellationToken.None);

        result.TotalReviews.Should().Be(3);
        result.AverageRating.Should().Be(4m);
        result.RatingDistribution[5].Should().Be(1);
        result.RatingDistribution[4].Should().Be(1);
        result.RatingDistribution[3].Should().Be(1);
        result.RatingDistribution[1].Should().Be(0);
    }

    [Fact]
    public async Task Summary_NoReviews_ReturnsZeroAverage()
    {
        var result = await CreateSummaryHandler().Handle(
            new GetProductReviewSummaryQuery { ProductId = Guid.NewGuid() },
            CancellationToken.None);

        result.TotalReviews.Should().Be(0);
        result.AverageRating.Should().Be(0);
    }
}
