using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Reviews.Commands;

public class VoteReviewCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private VoteReviewCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = Guid.NewGuid(), IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be authenticated");
    }

    [Fact]
    public async Task Handle_WhenReviewMissing_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = Guid.NewGuid(), IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenVotingOnOwnReview_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var review = new Review(Guid.NewGuid(), userId, 5);
        _context.Reviews.Add(review);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = review.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot vote on your own review");
    }

    [Fact]
    public async Task Handle_NewHelpfulVote_IncrementsAndRecordsVote()
    {
        var voterId = Guid.NewGuid();
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4);
        _context.Reviews.Add(review);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = review.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HelpfulCount.Should().Be(1);
        result.Value.UserVotedHelpful.Should().BeTrue();
        review.HelpfulCount.Should().Be(1);

        var vote = await _context.ReviewVotes.SingleAsync();
        vote.ReviewId.Should().Be(review.Id);
        vote.UserId.Should().Be(voterId);
        vote.IsHelpful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RepeatedSameVote_IsNoOp()
    {
        var voterId = Guid.NewGuid();
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4);
        review.AddHelpfulVote();
        _context.Reviews.Add(review);
        _context.ReviewVotes.Add(new ReviewVote(review.Id, voterId, true));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = review.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HelpfulCount.Should().Be(1, "voting the same way again does not double-count");
        review.HelpfulCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ChangingVote_MovesCountFromHelpfulToUnhelpful()
    {
        var voterId = Guid.NewGuid();
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4);
        review.AddHelpfulVote();
        _context.Reviews.Add(review);
        var existingVote = new ReviewVote(review.Id, voterId, true);
        _context.ReviewVotes.Add(existingVote);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteReviewCommand { ReviewId = review.Id, IsHelpful = false },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        review.HelpfulCount.Should().Be(0);
        review.UnhelpfulCount.Should().Be(1);
        existingVote.IsHelpful.Should().BeFalse();
    }
}
