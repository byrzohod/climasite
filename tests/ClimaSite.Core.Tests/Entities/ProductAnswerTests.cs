using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductAnswerTests
{
    private static ProductAnswer CreateValid() =>
        new(Guid.NewGuid(), "Yes, it supports WiFi.");

    [Fact]
    public void Constructor_WithValidData_CreatesPendingAnswer()
    {
        var questionId = Guid.NewGuid();

        var answer = new ProductAnswer(questionId, "It is quiet.");

        answer.QuestionId.Should().Be(questionId);
        answer.AnswerText.Should().Be("It is quiet.");
        answer.Status.Should().Be(AnswerStatus.Pending);
        answer.IsOfficial.Should().BeFalse();
        answer.HelpfulCount.Should().Be(0);
        answer.UnhelpfulCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyAnswerText_ThrowsArgumentException(string text)
    {
        var act = () => new ProductAnswer(Guid.NewGuid(), text);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Answer text cannot be empty*");
    }

    [Fact]
    public void SetAnswerText_ExceedingMaxLength_ThrowsArgumentException()
    {
        var answer = CreateValid();

        var act = () => answer.SetAnswerText(new string('a', 5001));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 5000 characters*");
    }

    [Fact]
    public void SetAnswerText_TrimsValue()
    {
        var answer = CreateValid();

        answer.SetAnswerText("  Trimmed  ");

        answer.AnswerText.Should().Be("Trimmed");
    }

    [Fact]
    public void SetUser_SetsUserId()
    {
        var answer = CreateValid();
        var userId = Guid.NewGuid();

        answer.SetUser(userId);

        answer.UserId.Should().Be(userId);
    }

    [Fact]
    public void SetAnswererName_TrimsValue()
    {
        var answer = CreateValid();

        answer.SetAnswererName("  Support Team  ");

        answer.AnswererName.Should().Be("Support Team");
    }

    [Fact]
    public void SetOfficial_UpdatesValue()
    {
        var answer = CreateValid();

        answer.SetOfficial(true);

        answer.IsOfficial.Should().BeTrue();
    }

    [Fact]
    public void SetStatus_UpdatesValue()
    {
        var answer = CreateValid();

        answer.SetStatus(AnswerStatus.Approved);

        answer.Status.Should().Be(AnswerStatus.Approved);
    }

    [Fact]
    public void AddHelpfulVote_IncrementsHelpfulCount()
    {
        var answer = CreateValid();

        answer.AddHelpfulVote();
        answer.AddHelpfulVote();

        answer.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void AddUnhelpfulVote_IncrementsUnhelpfulCount()
    {
        var answer = CreateValid();

        answer.AddUnhelpfulVote();

        answer.UnhelpfulCount.Should().Be(1);
    }

    [Fact]
    public void TotalVotes_SumsHelpfulAndUnhelpful()
    {
        var answer = CreateValid();
        answer.AddHelpfulVote();
        answer.AddHelpfulVote();
        answer.AddUnhelpfulVote();

        answer.TotalVotes.Should().Be(3);
    }

    [Fact]
    public void HelpfulPercentage_WithNoVotes_IsZero()
    {
        var answer = CreateValid();

        answer.HelpfulPercentage.Should().Be(0);
    }

    [Fact]
    public void HelpfulPercentage_ComputesRatio()
    {
        var answer = CreateValid();
        answer.AddHelpfulVote();
        answer.AddHelpfulVote();
        answer.AddHelpfulVote();
        answer.AddUnhelpfulVote();

        answer.HelpfulPercentage.Should().Be(75d);
    }
}
