using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class VoteQuestionCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private VoteQuestionCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_WhenQuestionMissing_Throws()
    {
        var act = () => CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_IncrementsHelpfulCount_AndReturnsNewTotal()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "How energy efficient is this model?");
        _context.ProductQuestions.Add(question);

        var first = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);
        var second = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        first.Should().Be(1);
        second.Should().Be(2);
        question.HelpfulCount.Should().Be(2);
    }
}
