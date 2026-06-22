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
    public async Task Handle_CreatesAnswer_AndMarksQuestionAnswered()
    {
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
        question.AnsweredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SecondAnswer_DoesNotOverwriteAnsweredAt()
    {
        var question = SeedQuestion();
        question.MarkAsAnswered();
        var firstAnsweredAt = question.AnsweredAt;

        await CreateHandler().Handle(
            new AnswerQuestionCommand
            {
                QuestionId = question.Id,
                AnswerText = "Adding a community follow-up answer here."
            },
            CancellationToken.None);

        question.AnsweredAt.Should().Be(firstAnsweredAt, "an already-answered question keeps its original timestamp");
        _context.ProductAnswers.Should().ContainSingle();
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
