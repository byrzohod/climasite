using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ReviewVoteTests
{
    private readonly Guid _reviewId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithHelpfulVote_CreatesVote()
    {
        // Arrange & Act
        var vote = new ReviewVote(_reviewId, _userId, true);

        // Assert
        vote.ReviewId.Should().Be(_reviewId);
        vote.UserId.Should().Be(_userId);
        vote.IsHelpful.Should().BeTrue();
        vote.Id.Should().NotBeEmpty();
        vote.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithUnhelpfulVote_CreatesVote()
    {
        // Arrange & Act
        var vote = new ReviewVote(_reviewId, _userId, false);

        // Assert
        vote.ReviewId.Should().Be(_reviewId);
        vote.UserId.Should().Be(_userId);
        vote.IsHelpful.Should().BeFalse();
    }

    [Fact]
    public void ChangeVote_FromHelpfulToUnhelpful_UpdatesVote()
    {
        // Arrange
        var vote = new ReviewVote(_reviewId, _userId, true);
        var originalUpdatedAt = vote.UpdatedAt;

        // Allow some time to pass
        Thread.Sleep(10);

        // Act
        vote.ChangeVote(false);

        // Assert
        vote.IsHelpful.Should().BeFalse();
        vote.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void ChangeVote_FromUnhelpfulToHelpful_UpdatesVote()
    {
        // Arrange
        var vote = new ReviewVote(_reviewId, _userId, false);

        // Act
        vote.ChangeVote(true);

        // Assert
        vote.IsHelpful.Should().BeTrue();
    }

    [Fact]
    public void ChangeVote_ToSameValue_StillUpdatesTimestamp()
    {
        // Arrange
        var vote = new ReviewVote(_reviewId, _userId, true);
        var originalUpdatedAt = vote.UpdatedAt;

        // Allow some time to pass
        Thread.Sleep(10);

        // Act
        vote.ChangeVote(true);

        // Assert
        vote.IsHelpful.Should().BeTrue();
        vote.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
