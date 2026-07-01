using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class AnswerQuestionCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private AnswerQuestionCommandHandler CreateHandler() => new(_context);

    private ProductQuestion SeedQuestion()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Does it support smart-home integration?");
        _context.ProductQuestions.Add(question);
        return question;
    }

    [Fact]
    public async Task Handle_WhenQuestionMissing_Throws()
    {
        var act = () => CreateHandler().Handle(
            new AnswerQuestionCommand { QuestionId = Guid.NewGuid(), AnswerText = "Yes it does." },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_CreatesAnswer_DoesNotMarkAnswered_ForPendingAnswer()
    {
        // B-038: a newly-submitted answer is Pending (un-moderated) and must NOT flag the question answered.
        var question = SeedQuestion();

        var id = await CreateHandler().Handle(
            new AnswerQuestionCommand
            {
                QuestionId = question.Id,
                AnswerText = "Yes, it integrates with major smart-home hubs.",
                AnswererName = "Support Team",
                IsOfficial = true
            },
            CancellationToken.None);

        var answer = _context.ProductAnswers.Single();
        answer.Id.Should().Be(id);
        answer.QuestionId.Should().Be(question.Id);
        answer.AnswererName.Should().Be("Support Team");
        answer.IsOfficial.Should().BeTrue();
        answer.Status.Should().Be(AnswerStatus.Pending);
        question.AnsweredAt.Should().BeNull("a still-Pending answer never marks the question answered (B-038)");
    }

    [Fact]
    public async Task Handle_DoesNotTouchAnsweredAt_WhenQuestionAlreadyAnswered()
    {
        // Establish an already-answered question the correct way (an approved answer + reconciliation),
        // then submit a second (Pending) answer — the command must leave AnsweredAt untouched.
        var question = SeedQuestion();
        var approved = new ProductAnswer(question.Id, "The first, approved answer.");
        approved.SetStatus(AnswerStatus.Approved);
        question.Answers.Add(approved);
        question.RefreshAnsweredState();
        var firstAnsweredAt = question.AnsweredAt;
        firstAnsweredAt.Should().NotBeNull("precondition: the question is already answered");

        await CreateHandler().Handle(
            new AnswerQuestionCommand
            {
                QuestionId = question.Id,
                AnswerText = "Adding a community follow-up answer here."
            },
            CancellationToken.None);

        question.AnsweredAt.Should().Be(firstAnsweredAt, "submitting another answer never changes answered-state");
    }

    [Fact]
    public async Task Handle_AttachesUserId_WhenAuthenticated()
    {
        var question = SeedQuestion();
        var userId = Guid.NewGuid();

        await CreateHandler().Handle(
            new AnswerQuestionCommand
            {
                QuestionId = question.Id,
                UserId = userId,
                AnswerText = "A detailed community answer to this question."
            },
            CancellationToken.None);

        _context.ProductAnswers.Single().UserId.Should().Be(userId);
    }
}
