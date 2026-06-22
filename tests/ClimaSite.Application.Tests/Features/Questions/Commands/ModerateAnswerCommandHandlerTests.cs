using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class ModerateAnswerCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private ModerateAnswerCommandHandler CreateHandler() => new(_context);

    /// <summary>
    /// Seeds an answer with its <c>Question</c> navigation wired via reflection — the MockDbContext
    /// does not perform EF Include joins, so the handler's <c>answer.Question.*</c> access must be
    /// satisfied manually.
    /// </summary>
    private ProductAnswer SeedAnswer(ProductQuestion question)
    {
        var answer = new ProductAnswer(question.Id, "A pending answer awaiting moderation.");
        typeof(ProductAnswer).GetProperty(nameof(ProductAnswer.Question))!.SetValue(answer, question);
        _context.ProductAnswers.Add(answer);
        return answer;
    }

    [Fact]
    public async Task Handle_WhenAnswerMissing_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = Guid.NewGuid(), NewStatus = AnswerStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Answer not found");
    }

    [Fact]
    public async Task Handle_ApprovingFirstAnswer_MarksQuestionAnswered()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "How loud is this unit at night?");
        _context.ProductQuestions.Add(question);
        var answer = SeedAnswer(question);

        var result = await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = answer.Id, NewStatus = AnswerStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        answer.Status.Should().Be(AnswerStatus.Approved);
        question.AnsweredAt.Should().NotBeNull("approving the first answer marks the question answered");
    }

    [Fact]
    public async Task Handle_MarkAsOfficial_SetsOfficialFlag()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Is there an extended warranty option?");
        _context.ProductQuestions.Add(question);
        var answer = SeedAnswer(question);

        var result = await CreateHandler().Handle(
            new ModerateAnswerCommand
            {
                AnswerId = answer.Id,
                NewStatus = AnswerStatus.Approved,
                MarkAsOfficial = true
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        answer.IsOfficial.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RejectingAnswer_DoesNotMarkQuestionAnswered()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Does it ship with mounting brackets?");
        _context.ProductQuestions.Add(question);
        var answer = SeedAnswer(question);

        var result = await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = answer.Id, NewStatus = AnswerStatus.Rejected },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        answer.Status.Should().Be(AnswerStatus.Rejected);
        question.AnsweredAt.Should().BeNull();
    }
}
