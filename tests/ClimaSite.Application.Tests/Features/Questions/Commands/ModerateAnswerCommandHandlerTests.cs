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
    /// Seeds an answer with the object graph the handler's
    /// <c>Include(a =&gt; a.Question).ThenInclude(q =&gt; q.Answers)</c> would materialize — the MockDbContext
    /// does not perform EF Include joins, so both the <c>answer.Question</c> back-reference AND the
    /// <c>question.Answers</c> forward collection (which <c>RefreshAnsweredState</c> reads) are wired here,
    /// pointing at the SAME answer instance the way EF's identity map would.
    /// </summary>
    private ProductAnswer SeedAnswer(ProductQuestion question)
    {
        var answer = new ProductAnswer(question.Id, "A pending answer awaiting moderation.");
        typeof(ProductAnswer).GetProperty(nameof(ProductAnswer.Question))!.SetValue(answer, question);
        question.Answers.Add(answer);
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

    [Fact]
    public async Task Handle_UnapprovingLastApprovedAnswer_ClearsAnsweredAt()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Is the remote backlit?");
        _context.ProductQuestions.Add(question);
        var answer = SeedAnswer(question);

        // Approve the only answer → the question becomes answered.
        await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = answer.Id, NewStatus = AnswerStatus.Approved },
            CancellationToken.None);
        question.AnsweredAt.Should().NotBeNull("precondition: approving the only answer marks it answered");

        // Then reject that same (only) approved answer → the question returns to unanswered (B-038 self-heal).
        var result = await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = answer.Id, NewStatus = AnswerStatus.Rejected },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        answer.Status.Should().Be(AnswerStatus.Rejected);
        question.AnsweredAt.Should().BeNull("un-approving the last approved answer clears answered-state");
    }

    [Fact]
    public async Task Handle_ApprovingSecondAnswer_KeepsOriginalAnsweredAt()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Does it support scheduling?");
        _context.ProductQuestions.Add(question);
        var first = SeedAnswer(question);
        var second = SeedAnswer(question);

        await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = first.Id, NewStatus = AnswerStatus.Approved },
            CancellationToken.None);
        var originalAnsweredAt = question.AnsweredAt;
        originalAnsweredAt.Should().NotBeNull();

        await CreateHandler().Handle(
            new ModerateAnswerCommand { AnswerId = second.Id, NewStatus = AnswerStatus.Approved },
            CancellationToken.None);

        question.AnsweredAt.Should().Be(originalAnsweredAt, "a second approval keeps the original answered timestamp");
    }
}
