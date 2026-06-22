using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class VoteAnswerCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private VoteAnswerCommandHandler CreateHandler() => new(_context);

    private ProductAnswer SeedAnswer()
    {
        var answer = new ProductAnswer(Guid.NewGuid(), "A helpful answer about this product.");
        _context.ProductAnswers.Add(answer);
        return answer;
    }

    [Fact]
    public async Task Handle_WhenAnswerMissing_Throws()
    {
        var act = () => CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = Guid.NewGuid(), IsHelpful = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_HelpfulVote_IncrementsHelpfulCount()
    {
        var answer = SeedAnswer();

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.HelpfulCount.Should().Be(1);
        result.UnhelpfulCount.Should().Be(0);
        answer.HelpfulCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnhelpfulVote_IncrementsUnhelpfulCount()
    {
        var answer = SeedAnswer();

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = false },
            CancellationToken.None);

        result.HelpfulCount.Should().Be(0);
        result.UnhelpfulCount.Should().Be(1);
        answer.UnhelpfulCount.Should().Be(1);
    }
}
