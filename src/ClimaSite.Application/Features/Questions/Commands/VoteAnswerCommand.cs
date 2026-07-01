using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record VoteAnswerCommand : IRequest<Result<VoteAnswerResult>>
{
    public Guid AnswerId { get; init; }
    public bool IsHelpful { get; init; }
}

public record VoteAnswerResult
{
    public int HelpfulCount { get; init; }
    public int UnhelpfulCount { get; init; }

    /// <summary>The current user's own vote: true = helpful, false = unhelpful, null = no vote.</summary>
    public bool? UserVoteHelpful { get; init; }
}

public class VoteAnswerCommandValidator : AbstractValidator<VoteAnswerCommand>
{
    public VoteAnswerCommandValidator()
    {
        RuleFor(x => x.AnswerId)
            .NotEmpty().WithMessage("Answer ID is required.");
    }
}

/// <summary>
/// Records a per-voter helpful/unhelpful vote on a product answer. Authenticated-only; a repeat of
/// the same direction toggles the vote off, and the opposite direction flips it, moving the tally
/// (B-039). Every count delta is applied only via an atomic <c>ExecuteUpdate</c> gated on the rows
/// affected by an <c>INSERT ... ON CONFLICT DO NOTHING</c> / conditional <c>DELETE</c> / conditional
/// <c>UPDATE</c>, all inside one execution-strategy transaction — never via the tracked
/// <see cref="ProductAnswer"/> vote methods. See unit-plan §9.
/// </summary>
public class VoteAnswerCommandHandler : IRequestHandler<VoteAnswerCommand, Result<VoteAnswerResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public VoteAnswerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<VoteAnswerResult>> Handle(
        VoteAnswerCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<VoteAnswerResult>.Failure("User must be authenticated to vote on answers");
        }

        var answer = await _context.ProductAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnswerId, cancellationToken);

        // Missing OR not-approved answers are indistinguishable to the caller (privacy) -> 404.
        if (answer == null || answer.Status != AnswerStatus.Approved)
        {
            return Result<VoteAnswerResult>.Failure($"Answer with ID {request.AnswerId} not found");
        }

        // The answer is only publicly visible when its parent question is also approved.
        var parentApproved = await _context.ProductQuestions
            .AsNoTracking()
            .AnyAsync(q => q.Id == answer.QuestionId && q.Status == QuestionStatus.Approved, cancellationToken);

        if (!parentApproved)
        {
            return Result<VoteAnswerResult>.Failure($"Answer with ID {request.AnswerId} not found");
        }

        if (answer.UserId.HasValue && answer.UserId.Value == userId.Value)
        {
            return Result<VoteAnswerResult>.Failure("You cannot vote on your own answer");
        }

        var answerId = request.AnswerId;
        var voterId = userId.Value;
        var wantHelpful = request.IsHelpful;

        // Decide the transition ONCE, OUTSIDE the execution strategy. EnableRetryOnFailure makes
        // strategy.ExecuteAsync genuinely retry; if the ledger state were re-read inside the delegate a
        // commit-unknown retry would see the just-committed state and run the OPPOSITE transition (a
        // flip would undo itself, a toggle-off would re-vote). With the decision fixed here the retry
        // re-runs the SAME from-state-guarded conditional op, which is idempotent — after a committed
        // attempt it affects 0 rows, so no count delta is applied twice. (unit-plan §9 + retry-idempotency)
        var existing = await _context.ProductAnswerVotes
            .AsNoTracking()
            .Where(v => v.AnswerId == answerId && v.UserId == voterId)
            .Select(v => (bool?)v.IsHelpful)
            .FirstOrDefaultAsync(cancellationToken);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (existing is null)
            {
                // First vote: insert the ledger row (ON CONFLICT DO NOTHING keeps the txn alive under a
                // concurrent insert). Only the winning insert (rows == 1) moves the matching count.
                var inserted = await _context.TryInsertAnswerVoteAsync(answerId, voterId, wantHelpful, cancellationToken);
                if (inserted == 1)
                {
                    await _context.AdjustAnswerVoteCountAsync(answerId, wantHelpful, +1, cancellationToken);
                }
            }
            else if (existing.Value == wantHelpful)
            {
                // Same direction: toggle the vote off; the delete filters on the decided from-state so a
                // stale retry can't remove a row that has since been flipped. Only rows == 1 decrements.
                var deleted = await _context.DeleteAnswerVoteAsync(answerId, voterId, wantHelpful, cancellationToken);
                if (deleted == 1)
                {
                    await _context.AdjustAnswerVoteCountAsync(answerId, wantHelpful, -1, cancellationToken);
                }
            }
            else
            {
                // Flip: conditional update on the ledger row (WHERE is_helpful = old). Only the request
                // that actually flips it (rows == 1) applies BOTH count deltas; a retry finds the row
                // already in the new direction (rows == 0) and re-applies nothing.
                var flipped = await _context.FlipAnswerVoteAsync(answerId, voterId, existing.Value, wantHelpful, cancellationToken);
                if (flipped == 1)
                {
                    // Raise the new direction, lower the old (the decrement floors at zero).
                    await _context.AdjustAnswerVoteCountAsync(answerId, wantHelpful, +1, cancellationToken);
                    await _context.AdjustAnswerVoteCountAsync(answerId, !wantHelpful, -1, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });

        // Re-read the authoritative post-commit state for the response.
        var counts = await _context.ProductAnswers
            .AsNoTracking()
            .Where(a => a.Id == answerId)
            .Select(a => new { a.HelpfulCount, a.UnhelpfulCount })
            .FirstAsync(cancellationToken);

        var userVote = await _context.ProductAnswerVotes
            .AsNoTracking()
            .Where(v => v.AnswerId == answerId && v.UserId == voterId)
            .Select(v => (bool?)v.IsHelpful)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<VoteAnswerResult>.Success(new VoteAnswerResult
        {
            HelpfulCount = counts.HelpfulCount,
            UnhelpfulCount = counts.UnhelpfulCount,
            UserVoteHelpful = userVote
        });
    }
}
