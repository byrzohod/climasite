using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductQuestionTests
{
    private static ProductQuestion CreateValid() =>
        new(Guid.NewGuid(), "Does this support WiFi?");

    [Fact]
    public void Constructor_WithValidData_CreatesPendingQuestion()
    {
        var productId = Guid.NewGuid();

        var question = new ProductQuestion(productId, "Is it quiet?");

        question.ProductId.Should().Be(productId);
        question.QuestionText.Should().Be("Is it quiet?");
        question.Status.Should().Be(QuestionStatus.Pending);
        question.HelpfulCount.Should().Be(0);
        question.AnsweredAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyQuestionText_ThrowsArgumentException(string text)
    {
        var act = () => new ProductQuestion(Guid.NewGuid(), text);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Question text cannot be empty*");
    }

    [Fact]
    public void SetQuestionText_ExceedingMaxLength_ThrowsArgumentException()
    {
        var question = CreateValid();

        var act = () => question.SetQuestionText(new string('a', 2001));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 2000 characters*");
    }

    [Fact]
    public void SetQuestionText_TrimsValue()
    {
        var question = CreateValid();

        question.SetQuestionText("  How heavy?  ");

        question.QuestionText.Should().Be("How heavy?");
    }

    [Fact]
    public void SetUser_SetsUserId()
    {
        var question = CreateValid();
        var userId = Guid.NewGuid();

        question.SetUser(userId);

        question.UserId.Should().Be(userId);
    }

    [Fact]
    public void SetAskerInfo_TrimsNameAndEmail()
    {
        var question = CreateValid();

        question.SetAskerInfo("  Bob  ", "  bob@test.com  ");

        question.AskerName.Should().Be("Bob");
        question.AskerEmail.Should().Be("bob@test.com");
    }

    [Fact]
    public void AddHelpfulVote_IncrementsCount()
    {
        var question = CreateValid();

        question.AddHelpfulVote();
        question.AddHelpfulVote();

        question.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void MarkAsAnswered_SetsAnsweredAt()
    {
        var question = CreateValid();

        question.MarkAsAnswered();

        question.AnsweredAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_ApprovedWithApprovedAnswer_SetsAnsweredAt()
    {
        var question = CreateValid();
        var answer = new ProductAnswer(question.Id, "Yes it does");
        answer.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(answer);

        question.SetStatus(QuestionStatus.Approved);

        question.AnsweredAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStatus_ApprovedWithNoAnswers_DoesNotSetAnsweredAt()
    {
        var question = CreateValid();

        question.SetStatus(QuestionStatus.Approved);

        question.AnsweredAt.Should().BeNull();
    }

    [Fact]
    public void SetStatus_Rejected_UpdatesStatus()
    {
        var question = CreateValid();

        question.SetStatus(QuestionStatus.Rejected);

        question.Status.Should().Be(QuestionStatus.Rejected);
    }

    [Fact]
    public void HasApprovedAnswer_WithApprovedAnswer_ReturnsTrue()
    {
        var question = CreateValid();
        var answer = new ProductAnswer(question.Id, "Answer");
        answer.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(answer);

        question.HasApprovedAnswer.Should().BeTrue();
    }

    [Fact]
    public void HasApprovedAnswer_WithOnlyPendingAnswers_ReturnsFalse()
    {
        var question = CreateValid();
        question.Answers.Add(new ProductAnswer(question.Id, "Pending answer"));

        question.HasApprovedAnswer.Should().BeFalse();
    }

    [Fact]
    public void AnswerCount_CountsOnlyApprovedAnswers()
    {
        var question = CreateValid();
        var approved = new ProductAnswer(question.Id, "A");
        approved.SetStatus(AnswerStatus.Approved);
        var pending = new ProductAnswer(question.Id, "B");
        question.Answers.Add(approved);
        question.Answers.Add(pending);

        question.AnswerCount.Should().Be(1);
    }
}
