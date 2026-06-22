using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class AskQuestionCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private AskQuestionCommandHandler CreateHandler() => new(_context);

    private Product SeedProduct()
    {
        var product = new Product("Q-001", "Question AC", "question-ac", 500m);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_WhenProductMissing_Throws()
    {
        var act = () => CreateHandler().Handle(
            new AskQuestionCommand { ProductId = Guid.NewGuid(), QuestionText = "Does it work in winter?" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_CreatesPendingQuestion_ForGuest()
    {
        var product = SeedProduct();

        var id = await CreateHandler().Handle(
            new AskQuestionCommand
            {
                ProductId = product.Id,
                QuestionText = "Is installation included?",
                AskerName = "Guest Asker",
                AskerEmail = "guest@example.com"
            },
            CancellationToken.None);

        id.Should().NotBeEmpty();
        var stored = _context.ProductQuestions.Single();
        stored.Id.Should().Be(id);
        stored.Status.Should().Be(QuestionStatus.Pending);
        stored.AskerName.Should().Be("Guest Asker");
        stored.AskerEmail.Should().Be("guest@example.com");
        stored.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AttachesUserId_WhenAuthenticated()
    {
        var product = SeedProduct();
        var userId = Guid.NewGuid();

        var id = await CreateHandler().Handle(
            new AskQuestionCommand
            {
                ProductId = product.Id,
                UserId = userId,
                QuestionText = "What is the warranty period exactly?"
            },
            CancellationToken.None);

        var stored = _context.ProductQuestions.Single();
        stored.Id.Should().Be(id);
        stored.UserId.Should().Be(userId);
    }
}
