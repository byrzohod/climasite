using ClimaSite.Application.Features.Questions.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Queries;

public class GetProductQuestionsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetProductQuestionsQueryHandler CreateHandler() => new(_context);

    private ProductQuestion SeedApprovedQuestion(
        Guid productId,
        string text,
        bool answered,
        string? askerName = null)
    {
        var question = new ProductQuestion(productId, text);
        if (askerName != null)
        {
            question.SetAskerInfo(askerName, null);
        }
        question.SetStatus(QuestionStatus.Approved);
        if (answered)
        {
            var answer = new ProductAnswer(question.Id, "An approved, helpful answer.");
            answer.SetStatus(AnswerStatus.Approved);
            question.Answers.Add(answer);
            question.MarkAsAnswered();
        }
        _context.ProductQuestions.Add(question);
        return question;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyApprovedQuestionsForProduct()
    {
        var productId = Guid.NewGuid();
        SeedApprovedQuestion(productId, "Approved question one here?", answered: false);

        var pending = new ProductQuestion(productId, "A pending question that is hidden?");
        _context.ProductQuestions.Add(pending);

        var otherProduct = new ProductQuestion(Guid.NewGuid(), "Belongs to another product entirely?");
        otherProduct.SetStatus(QuestionStatus.Approved);
        _context.ProductQuestions.Add(otherProduct);

        var result = await CreateHandler().Handle(
            new GetProductQuestionsQuery { ProductId = productId },
            CancellationToken.None);

        result.ProductId.Should().Be(productId);
        result.TotalQuestions.Should().Be(1);
        result.Questions.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_CountsAnsweredQuestions()
    {
        var productId = Guid.NewGuid();
        SeedApprovedQuestion(productId, "Answered question is here?", answered: true);
        SeedApprovedQuestion(productId, "Unanswered question is here?", answered: false);

        var result = await CreateHandler().Handle(
            new GetProductQuestionsQuery { ProductId = productId },
            CancellationToken.None);

        result.TotalQuestions.Should().Be(2);
        result.AnsweredQuestions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExcludesUnanswered_WhenIncludeUnansweredFalse()
    {
        var productId = Guid.NewGuid();
        SeedApprovedQuestion(productId, "Answered question is here?", answered: true);
        SeedApprovedQuestion(productId, "Unanswered question is here?", answered: false);

        var result = await CreateHandler().Handle(
            new GetProductQuestionsQuery { ProductId = productId, IncludeUnanswered = false },
            CancellationToken.None);

        result.Questions.Should().ContainSingle();
        result.Questions.Single().AnsweredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MapsAnswersAndAnonymousAskerName()
    {
        var productId = Guid.NewGuid();
        SeedApprovedQuestion(productId, "Question with an approved answer?", answered: true);

        var result = await CreateHandler().Handle(
            new GetProductQuestionsQuery { ProductId = productId },
            CancellationToken.None);

        var dto = result.Questions.Single();
        dto.AskerName.Should().Be("Anonymous", "no asker name was provided");
        dto.AnswerCount.Should().Be(1);
        dto.Answers.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_PaginatesResults()
    {
        var productId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            SeedApprovedQuestion(productId, $"Paginated question number {i} here?", answered: false);
        }

        var result = await CreateHandler().Handle(
            new GetProductQuestionsQuery { ProductId = productId, PageNumber = 1, PageSize = 2 },
            CancellationToken.None);

        result.TotalQuestions.Should().Be(5);
        result.Questions.Should().HaveCount(2);
    }
}
