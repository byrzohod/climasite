using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Questions.Commands;

public class VoteAnswerCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private VoteAnswerCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    /// <summary>Seeds an approved answer under an approved parent question and returns the answer.</summary>
    private ProductAnswer SeedApprovedAnswer(Guid? authorId = null)
    {
        var question = new ProductQuestion(Guid.NewGuid(), "A publicly approved parent question here?");
        question.SetStatus(QuestionStatus.Approved);
        _context.ProductQuestions.Add(question);

        var answer = new ProductAnswer(question.Id, "A helpful answer about this product.");
        if (authorId.HasValue)
        {
            answer.SetUser(authorId.Value);
        }
        answer.SetStatus(AnswerStatus.Approved);
        _context.ProductAnswers.Add(answer);
        return answer;
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var answer = SeedApprovedAnswer();

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be authenticated");
    }

    [Fact]
    public async Task Handle_WhenAnswerMissing_ReturnsNotFoundFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = Guid.NewGuid(), IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenAnswerNotApproved_ReturnsNotFoundFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        var question = new ProductQuestion(Guid.NewGuid(), "An approved parent question here?");
        question.SetStatus(QuestionStatus.Approved);
        _context.ProductQuestions.Add(question);
        var pendingAnswer = new ProductAnswer(question.Id, "A pending, non-votable answer here.");
        _context.ProductAnswers.Add(pendingAnswer);

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = pendingAnswer.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenParentQuestionNotApproved_ReturnsNotFoundFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        // Approved answer, but its parent question is still pending -> not publicly visible.
        var question = new ProductQuestion(Guid.NewGuid(), "A pending parent question here?");
        _context.ProductQuestions.Add(question);
        var answer = new ProductAnswer(question.Id, "An approved answer under a hidden question.");
        answer.SetStatus(AnswerStatus.Approved);
        _context.ProductAnswers.Add(answer);

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenVotingOnOwnAnswer_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var answer = SeedApprovedAnswer(authorId: userId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot vote on your own answer");
    }

    [Fact]
    public async Task Handle_FirstHelpfulVote_IncrementsAndRecordsLedgerRow()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HelpfulCount.Should().Be(1);
        result.Value.UnhelpfulCount.Should().Be(0);
        result.Value.UserVoteHelpful.Should().BeTrue();
        answer.HelpfulCount.Should().Be(1);

        var vote = await _context.ProductAnswerVotes.SingleAsync();
        vote.AnswerId.Should().Be(answer.Id);
        vote.UserId.Should().Be(voterId);
        vote.IsHelpful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FirstUnhelpfulVote_IncrementsUnhelpfulCount()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = false },
            CancellationToken.None);

        result.Value!.HelpfulCount.Should().Be(0);
        result.Value.UnhelpfulCount.Should().Be(1);
        result.Value.UserVoteHelpful.Should().BeFalse();
        answer.UnhelpfulCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RepeatSameVote_TogglesOff_AndDoesNotInflate()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);
        var second = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        second.Value!.HelpfulCount.Should().Be(0, "voting the same way again is a toggle-off");
        second.Value.UserVoteHelpful.Should().BeNull();
        answer.HelpfulCount.Should().Be(0);
        (await _context.ProductAnswerVotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_FlipHelpfulToUnhelpful_MovesTheTally()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);

        await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);
        var flip = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = false },
            CancellationToken.None);

        flip.Value!.HelpfulCount.Should().Be(0, "the helpful vote moved to unhelpful");
        flip.Value.UnhelpfulCount.Should().Be(1);
        flip.Value.UserVoteHelpful.Should().BeFalse();
        answer.HelpfulCount.Should().Be(0);
        answer.UnhelpfulCount.Should().Be(1);

        var vote = await _context.ProductAnswerVotes.SingleAsync();
        vote.IsHelpful.Should().BeFalse("the single ledger row flipped direction, it was not duplicated");
    }

    [Fact]
    public async Task Handle_FirstVote_IsIdempotentUnderRetry()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);
        _context.ExecutionStrategyAttempts = 2; // simulate a commit-unknown retry

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.Value!.HelpfulCount.Should().Be(1, "the increment must be applied exactly once under a retry");
        result.Value.UserVoteHelpful.Should().BeTrue();
        answer.HelpfulCount.Should().Be(1);
        (await _context.ProductAnswerVotes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ToggleOff_IsIdempotentUnderRetry()
    {
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        answer.AddHelpfulVote(); // pre-existing ledger vote + baseline of 1
        _context.ProductAnswerVotes.Add(new ProductAnswerVote(answer.Id, voterId, isHelpful: true));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);
        _context.ExecutionStrategyAttempts = 2;

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = true },
            CancellationToken.None);

        result.Value!.HelpfulCount.Should().Be(0, "the decrement must be applied exactly once under a retry");
        result.Value.UserVoteHelpful.Should().BeNull();
        answer.HelpfulCount.Should().Be(0);
        (await _context.ProductAnswerVotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Flip_IsIdempotentUnderRetry()
    {
        // The critical uncovered case: a flip must NOT undo itself when the strategy retries.
        var voterId = Guid.NewGuid();
        var answer = SeedApprovedAnswer();
        answer.AddHelpfulVote(); // pre-existing helpful vote + baseline of 1
        _context.ProductAnswerVotes.Add(new ProductAnswerVote(answer.Id, voterId, isHelpful: true));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(voterId);
        _context.ExecutionStrategyAttempts = 2;

        var result = await CreateHandler().Handle(
            new VoteAnswerCommand { AnswerId = answer.Id, IsHelpful = false },
            CancellationToken.None);

        // Decision (flip helpful->unhelpful) is fixed OUTSIDE the strategy, so the retry re-runs the SAME
        // conditional flip (WHERE is_helpful = true), which now matches 0 rows -> the tally moves once.
        result.Value!.HelpfulCount.Should().Be(0);
        result.Value.UnhelpfulCount.Should().Be(1, "the flip must be applied exactly once, not undone by the retry");
        result.Value.UserVoteHelpful.Should().BeFalse();
        answer.HelpfulCount.Should().Be(0);
        answer.UnhelpfulCount.Should().Be(1);
        var vote = await _context.ProductAnswerVotes.SingleAsync();
        vote.IsHelpful.Should().BeFalse();
    }
}
