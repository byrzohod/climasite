using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Reviews.Commands;

public class ModerateReviewCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private ModerateReviewCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_WhenReviewMissing_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(
            new ModerateReviewCommand { ReviewId = Guid.NewGuid(), NewStatus = ReviewStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Review not found");
    }

    [Fact]
    public async Task Handle_ApprovesReview()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4);
        _context.Reviews.Add(review);

        var result = await CreateHandler().Handle(
            new ModerateReviewCommand { ReviewId = review.Id, NewStatus = ReviewStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public async Task Handle_RejectsReview()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 1);
        review.SetStatus(ReviewStatus.Approved);
        _context.Reviews.Add(review);

        var result = await CreateHandler().Handle(
            new ModerateReviewCommand { ReviewId = review.Id, NewStatus = ReviewStatus.Rejected },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Rejected);
    }
}
