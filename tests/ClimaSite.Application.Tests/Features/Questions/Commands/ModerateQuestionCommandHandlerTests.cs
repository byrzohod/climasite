using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class ModerateQuestionCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private ModerateQuestionCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_WhenQuestionMissing_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(
            new ModerateQuestionCommand { QuestionId = Guid.NewGuid(), NewStatus = QuestionStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Question not found");
    }

    [Fact]
    public async Task Handle_ApprovesQuestion()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Is the remote control included?");
        _context.ProductQuestions.Add(question);

        var result = await CreateHandler().Handle(
            new ModerateQuestionCommand { QuestionId = question.Id, NewStatus = QuestionStatus.Approved },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Approved);
    }

    [Fact]
    public async Task Handle_RejectsQuestion()
    {
        var question = new ProductQuestion(Guid.NewGuid(), "Some spam question text here.");
        _context.ProductQuestions.Add(question);

        var result = await CreateHandler().Handle(
            new ModerateQuestionCommand { QuestionId = question.Id, NewStatus = QuestionStatus.Rejected },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Rejected);
    }
}
