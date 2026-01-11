using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ReviewTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesReview()
    {
        // Arrange & Act
        var review = new Review(_productId, _userId, 5);

        // Assert
        review.ProductId.Should().Be(_productId);
        review.UserId.Should().Be(_userId);
        review.Rating.Should().Be(5);
        review.Status.Should().Be(ReviewStatus.Pending);
        review.IsVerifiedPurchase.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_WithInvalidRating_ThrowsArgumentException(int rating)
    {
        // Act & Assert
        var act = () => new Review(_productId, _userId, rating);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Rating must be between 1 and 5*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void SetRating_WithValidValue_UpdatesRating(int rating)
    {
        // Arrange
        var review = new Review(_productId, _userId, 3);

        // Act
        review.SetRating(rating);

        // Assert
        review.Rating.Should().Be(rating);
    }

    [Fact]
    public void SetRating_WithInvalidValue_ThrowsArgumentException()
    {
        // Arrange
        var review = new Review(_productId, _userId, 3);

        // Act & Assert
        var act = () => review.SetRating(0);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Rating must be between 1 and 5*");
    }

    [Fact]
    public void SetTitle_WithValidValue_UpdatesTitle()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetTitle("Great product!");

        // Assert
        review.Title.Should().Be("Great product!");
    }

    [Fact]
    public void SetTitle_TrimsWhitespace()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetTitle("  Great product!  ");

        // Assert
        review.Title.Should().Be("Great product!");
    }

    [Fact]
    public void SetTitle_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        var longTitle = new string('A', 201);

        // Act & Assert
        var act = () => review.SetTitle(longTitle);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Title cannot exceed 200 characters*");
    }

    [Fact]
    public void SetTitle_WithNull_SetsToNull()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        review.SetTitle("Original title");

        // Act
        review.SetTitle(null);

        // Assert
        review.Title.Should().BeNull();
    }

    [Fact]
    public void SetContent_WithValidValue_UpdatesContent()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetContent("This is a great air conditioner!");

        // Assert
        review.Content.Should().Be("This is a great air conditioner!");
    }

    [Fact]
    public void SetContent_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        var longContent = new string('A', 5001);

        // Act & Assert
        var act = () => review.SetContent(longContent);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Content cannot exceed 5000 characters*");
    }

    [Fact]
    public void SetStatus_UpdatesStatus()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetStatus(ReviewStatus.Approved);

        // Assert
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public void SetVerifiedPurchase_WithOrderId_SetsVerifiedAndOrderId()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetVerifiedPurchase(true, _orderId);

        // Assert
        review.IsVerifiedPurchase.Should().BeTrue();
        review.OrderId.Should().Be(_orderId);
    }

    [Fact]
    public void SetVerifiedPurchase_WithFalse_SetsNotVerified()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        review.SetVerifiedPurchase(true, _orderId);

        // Act
        review.SetVerifiedPurchase(false);

        // Assert
        review.IsVerifiedPurchase.Should().BeFalse();
    }

    [Fact]
    public void AddHelpfulVote_IncrementsHelpfulCount()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.AddHelpfulVote();
        review.AddHelpfulVote();

        // Assert
        review.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void AddUnhelpfulVote_IncrementsUnhelpfulCount()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.AddUnhelpfulVote();

        // Assert
        review.UnhelpfulCount.Should().Be(1);
    }

    [Fact]
    public void TotalVotes_CalculatesCorrectly()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        review.AddHelpfulVote();
        review.AddHelpfulVote();
        review.AddUnhelpfulVote();

        // Assert
        review.TotalVotes.Should().Be(3);
    }

    [Fact]
    public void HelpfulPercentage_CalculatesCorrectly()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);
        review.AddHelpfulVote();
        review.AddHelpfulVote();
        review.AddHelpfulVote();
        review.AddUnhelpfulVote();

        // Assert
        review.HelpfulPercentage.Should().Be(75);
    }

    [Fact]
    public void HelpfulPercentage_WithNoVotes_ReturnsZero()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Assert
        review.HelpfulPercentage.Should().Be(0);
    }

    [Fact]
    public void SetAdminResponse_SetsResponseAndTimestamp()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act
        review.SetAdminResponse("Thank you for your feedback!");

        // Assert
        review.AdminResponse.Should().Be("Thank you for your feedback!");
        review.AdminRespondedAt.Should().NotBeNull();
        review.AdminRespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetAdminResponse_WithEmptyValue_ThrowsArgumentException()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act & Assert
        var act = () => review.SetAdminResponse("");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Admin response cannot be empty*");
    }

    [Fact]
    public void SetAdminResponse_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var review = new Review(_productId, _userId, 4);

        // Act & Assert
        var act = () => review.SetAdminResponse("   ");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Admin response cannot be empty*");
    }
}
