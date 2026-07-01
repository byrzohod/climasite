using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record VoteQuestionCommand : IRequest<Result<VoteQuestionResult>>
{
    public Guid QuestionId { get; init; }
}

public record VoteQuestionResult
{
    public int HelpfulCount { get; init; }
    public bool HasVotedHelpful { get; init; }
}

public class VoteQuestionCommandValidator : AbstractValidator<VoteQuestionCommand>
{
    public VoteQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required.");
    }
}

/// <summary>
/// Records a per-voter "helpful" vote on a product question. Voting is authenticated-only and a
/// repeat vote toggles the previous one off (B-039). The count mutation is race-safe: it is applied
/// only via an atomic <c>ExecuteUpdate</c> gated on the rows affected by an <c>INSERT ... ON CONFLICT
/// DO NOTHING</c> / conditional <c>DELETE</c>, all inside one execution-strategy transaction — never
/// via the tracked <see cref="ProductQuestion.AddHelpfulVote"/> methods (which would lose concurrent
/// updates). See unit-plan §9.
/// </summary>
public class VoteQuestionCommandHandler : IRequestHandler<VoteQuestionCommand, Result<VoteQuestionResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public VoteQuestionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<VoteQuestionResult>> Handle(
        VoteQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<VoteQuestionResult>.Failure("User must be authenticated to vote on questions");
        }

        var question = await _context.ProductQuestions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

        // Missing OR not-yet-approved questions are indistinguishable to the caller (privacy) -> 404.
        if (question == null || question.Status != QuestionStatus.Approved)
        {
            return Result<VoteQuestionResult>.Failure($"Question with ID {request.QuestionId} not found");
        }

        if (question.UserId.HasValue && question.UserId.Value == userId.Value)
        {
            return Result<VoteQuestionResult>.Failure("You cannot vote on your own question");
        }

        var questionId = request.QuestionId;
        var voterId = userId.Value;

        // Decide the transition ONCE, OUTSIDE the execution strategy. EnableRetryOnFailure means
        // strategy.ExecuteAsync genuinely retries; if the decision were re-read inside the delegate, a
        // commit-unknown retry would see the just-committed state and execute the OPPOSITE transition
        // (first-vote -> toggle-off, etc.). With the decision fixed here, the retry re-runs the SAME
        // conditional op, which is from-state-guarded and idempotent: after a committed attempt it
        // affects 0 rows, so the count delta is never applied twice. (unit-plan §9 + retry-idempotency)
        var alreadyVoted = await _context.ProductQuestionVotes
            .AsNoTracking()
            .AnyAsync(v => v.QuestionId == questionId && v.UserId == voterId, cancellationToken);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (!alreadyVoted)
            {
                // First vote: insert the ledger row (ON CONFLICT DO NOTHING → a concurrent insert never
                // throws, so the txn stays alive). Only the winning insert (rows == 1) increments; a
                // retry after a committed insert conflicts (rows == 0) and re-applies nothing.
                var inserted = await _context.TryInsertQuestionVoteAsync(questionId, voterId, cancellationToken);
                if (inserted == 1)
                {
                    await _context.AdjustQuestionHelpfulCountAsync(questionId, +1, cancellationToken);
                }
            }
            else
            {
                // Toggle off: conditional delete; only the request that actually removes the row
                // (rows == 1) decrements (the adjust floors at zero). A retry finds no row -> rows == 0.
                var deleted = await _context.DeleteQuestionVoteAsync(questionId, voterId, cancellationToken);
                if (deleted == 1)
                {
                    await _context.AdjustQuestionHelpfulCountAsync(questionId, -1, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });

        // Re-read the authoritative post-commit state for the response.
        var helpfulCount = await _context.ProductQuestions
            .AsNoTracking()
            .Where(q => q.Id == questionId)
            .Select(q => q.HelpfulCount)
            .FirstAsync(cancellationToken);

        var hasVoted = await _context.ProductQuestionVotes
            .AsNoTracking()
            .AnyAsync(v => v.QuestionId == questionId && v.UserId == voterId, cancellationToken);

        return Result<VoteQuestionResult>.Success(new VoteQuestionResult
        {
            HelpfulCount = helpfulCount,
            HasVotedHelpful = hasVoted
        });
    }
}
