using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class VoteQuestionCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private VoteQuestionCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private ProductQuestion SeedApprovedQuestion(Guid? authorId = null)
    {
        var question = new ProductQuestion(Guid.NewGuid(), "How energy efficient is this model?");
        if (authorId.HasValue)
        {
            question.SetUser(authorId.Value);
        }
        question.SetStatus(QuestionStatus.Approved);
        _context.ProductQuestions.Add(question);
        return question;
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var question = SeedApprovedQuestion();

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be authenticated");
    }

    [Fact]
    public async Task Handle_WhenQuestionMissing_ReturnsNotFoundFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenQuestionNotApproved_ReturnsNotFoundFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        // Pending (default) question is not publicly votable.
        var pending = new ProductQuestion(Guid.NewGuid(), "A pending, non-votable question here?");
        _context.ProductQuestions.Add(pending);

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = pending.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenVotingOnOwnQuestion_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var question = SeedApprovedQuestion(authorId: userId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot vote on your own question");
    }

    [Fact]
    public async Task Handle_FirstVote_IncrementsAndRecordsLedgerRow()
    {
        var voterId = Guid.NewGuid();
        var question = SeedApprovedQuestion();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HelpfulCount.Should().Be(1);
        result.Value.HasVotedHelpful.Should().BeTrue();
        question.HelpfulCount.Should().Be(1);

        var vote = await _context.ProductQuestionVotes.SingleAsync();
        vote.QuestionId.Should().Be(question.Id);
        vote.UserId.Should().Be(voterId);
    }

    [Fact]
    public async Task Handle_RepeatVote_TogglesOff_AndDoesNotInflate()
    {
        var voterId = Guid.NewGuid();
        var question = SeedApprovedQuestion();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        // First vote records the helpful vote.
        var first = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);
        // Voting again toggles it off rather than inflating the count (the pre-B-039 bug asserted 1 -> 2).
        var second = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        first.Value!.HelpfulCount.Should().Be(1);
        first.Value.HasVotedHelpful.Should().BeTrue();

        second.Value!.HelpfulCount.Should().Be(0, "the second vote is a toggle-off, not a duplicate");
        second.Value.HasVotedHelpful.Should().BeFalse();
        question.HelpfulCount.Should().Be(0);

        var remainingVotes = await _context.ProductQuestionVotes.CountAsync();
        remainingVotes.Should().Be(0, "the ledger row is removed on toggle-off");
    }

    [Fact]
    public async Task Handle_ToggleOff_NeverDrivesCountNegative_BelowLegacyBaseline()
    {
        // A pre-cutover legacy baseline exists, but this user never had a ledger row -> toggle-off must
        // not be reachable, and even a spurious decrement is floored at zero. Here we prove the floor by
        // toggling a single ledger vote off when the baseline is already 0.
        var voterId = Guid.NewGuid();
        var question = SeedApprovedQuestion();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        await CreateHandler().Handle(new VoteQuestionCommand { QuestionId = question.Id }, CancellationToken.None);
        await CreateHandler().Handle(new VoteQuestionCommand { QuestionId = question.Id }, CancellationToken.None);
        // A third click re-votes (fresh row), a fourth toggles off again — count must stay 0..1, never negative.
        await CreateHandler().Handle(new VoteQuestionCommand { QuestionId = question.Id }, CancellationToken.None);
        var fourth = await CreateHandler().Handle(new VoteQuestionCommand { QuestionId = question.Id }, CancellationToken.None);

        fourth.Value!.HelpfulCount.Should().Be(0);
        question.HelpfulCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_FirstVote_IsIdempotentUnderRetry()
    {
        var voterId = Guid.NewGuid();
        var question = SeedApprovedQuestion();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);
        // Simulate a commit-unknown retry: the execution-strategy delegate runs twice.
        _context.ExecutionStrategyAttempts = 2;

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        // The retry re-runs the SAME (insert) transition -> ON CONFLICT affects 0 rows -> +1 applied once.
        result.Value!.HelpfulCount.Should().Be(1);
        result.Value.HasVotedHelpful.Should().BeTrue();
        question.HelpfulCount.Should().Be(1, "the increment must be applied exactly once under a retry");
        (await _context.ProductQuestionVotes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ToggleOff_IsIdempotentUnderRetry()
    {
        var voterId = Guid.NewGuid();
        var question = SeedApprovedQuestion();
        question.AddHelpfulVote(); // pre-existing ledger vote + count baseline of 1
        _context.ProductQuestionVotes.Add(new ProductQuestionVote(question.Id, voterId));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);
        _context.ExecutionStrategyAttempts = 2;

        var result = await CreateHandler().Handle(
            new VoteQuestionCommand { QuestionId = question.Id },
            CancellationToken.None);

        // The retry re-runs the SAME (delete) transition -> the row is already gone -> -1 applied once.
        result.Value!.HelpfulCount.Should().Be(0);
        result.Value.HasVotedHelpful.Should().BeFalse();
        question.HelpfulCount.Should().Be(0, "the decrement must be applied exactly once under a retry");
        (await _context.ProductQuestionVotes.CountAsync()).Should().Be(0);
    }
}
