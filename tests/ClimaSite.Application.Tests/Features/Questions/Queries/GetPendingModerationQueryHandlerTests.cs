using ClimaSite.Application.Features.Questions.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Questions.Queries;

public class GetPendingModerationQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private static void WireProduct(ProductQuestion question, Product product) =>
        typeof(ProductQuestion).GetProperty(nameof(ProductQuestion.Product))!.SetValue(question, product);

    [Fact]
    public async Task Handle_ExposesAnsweredAt_OnlyWhenAnApprovedAnswerExists()
    {
        // B-038: the admin moderation queue mirrors the public read — AnsweredAt is derived from approved-
        // answer existence, so a legacy/racy stamped timestamp on a question without an approved answer is
        // suppressed rather than trusted.
        var product = new Product("QA-MOD", "Moderation Product", "mod-product", 1m);

        // Q1 (in the Pending queue): has an approved answer → AnsweredAt exposed.
        var q1 = new ProductQuestion(product.Id, "Question with an approved answer for moderation?");
        WireProduct(q1, product);
        var approved = new ProductAnswer(q1.Id, "An approved answer.");
        approved.SetStatus(AnswerStatus.Approved);
        q1.Answers.Add(approved);
        q1.RefreshAnsweredState();
        _context.ProductQuestions.Add(q1);

        // Q2 (in the Pending queue): dirty data — AnsweredAt stamped but only a Pending answer exists.
        var q2 = new ProductQuestion(product.Id, "Dirty question with only a pending answer here?");
        WireProduct(q2, product);
        q2.Answers.Add(new ProductAnswer(q2.Id, "A pending answer."));
        typeof(ProductQuestion).GetProperty(nameof(ProductQuestion.AnsweredAt))!.SetValue(q2, DateTime.UtcNow);
        _context.ProductQuestions.Add(q2);

        var handler = new GetPendingModerationQueryHandler(_context);
        // AnswerStatus=Rejected yields no answers, so the answer projection (which needs Question.Product
        // wired) is skipped — this test targets only the question-side AnsweredAt derivation.
        var result = await handler.Handle(
            new GetPendingModerationQuery { AnswerStatus = AnswerStatus.Rejected }, CancellationToken.None);

        result.Questions.Single(q => q.Id == q1.Id).AnsweredAt
            .Should().NotBeNull("an approved answer exists");
        result.Questions.Single(q => q.Id == q2.Id).AnsweredAt
            .Should().BeNull("only a pending answer exists — the stale timestamp is suppressed");
    }
}
