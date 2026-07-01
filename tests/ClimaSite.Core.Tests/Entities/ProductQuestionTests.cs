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
    public void RemoveHelpfulVote_DecrementsCount()
    {
        var question = CreateValid();
        question.AddHelpfulVote();
        question.AddHelpfulVote();

        question.RemoveHelpfulVote();

        question.HelpfulCount.Should().Be(1);
    }

    [Fact]
    public void RemoveHelpfulVote_AtZero_FloorsAtZero()
    {
        var question = CreateValid();

        question.RemoveHelpfulVote();

        question.HelpfulCount.Should().Be(0, "the count must never go negative");
    }

    [Fact]
    public void SetStatus_DoesNotTouchAnsweredState_EvenWithApprovedAnswer()
    {
        // B-038: answered-state moved out of SetStatus — a status transition must not stamp AnsweredAt.
        var question = CreateValid();
        var approved = new ProductAnswer(question.Id, "Yes it does");
        approved.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(approved);

        question.SetStatus(QuestionStatus.Approved);

        question.AnsweredAt.Should().BeNull("SetStatus no longer owns answered-state — RefreshAnsweredState does");
    }

    [Fact]
    public void RefreshAnsweredState_WithApprovedAnswer_SetsAnsweredAt()
    {
        var question = CreateValid();
        var approved = new ProductAnswer(question.Id, "An approved, helpful answer.");
        approved.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(approved);

        question.RefreshAnsweredState();

        question.AnsweredAt.Should().NotBeNull();
    }

    [Fact]
    public void RefreshAnsweredState_WithOnlyPendingAnswers_LeavesAnsweredNull()
    {
        // The B-038 invariant at the domain level: a still-Pending answer never flags the question answered.
        var question = CreateValid();
        question.Answers.Add(new ProductAnswer(question.Id, "A pending answer awaiting moderation."));

        question.RefreshAnsweredState();

        question.AnsweredAt.Should().BeNull();
    }

    [Fact]
    public void RefreshAnsweredState_WhenLastApprovedAnswerRemoved_ClearsAnsweredAt()
    {
        var question = CreateValid();
        var approved = new ProductAnswer(question.Id, "An approved, helpful answer.");
        approved.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(approved);
        question.RefreshAnsweredState();
        question.AnsweredAt.Should().NotBeNull("precondition: the question is answered");

        // The only approved answer is un-approved (e.g. rejected on re-moderation).
        approved.SetStatus(AnswerStatus.Rejected);
        question.RefreshAnsweredState();

        question.AnsweredAt.Should().BeNull("clearing the last approved answer returns the question to unanswered");
    }

    [Fact]
    public void RefreshAnsweredState_IsIdempotent_KeepsOriginalTimestamp()
    {
        var question = CreateValid();
        var approved = new ProductAnswer(question.Id, "An approved, helpful answer.");
        approved.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(approved);
        question.RefreshAnsweredState();
        var firstAnsweredAt = question.AnsweredAt;

        question.RefreshAnsweredState();

        question.AnsweredAt.Should().Be(firstAnsweredAt, "reconciliation never overwrites an existing timestamp");
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
