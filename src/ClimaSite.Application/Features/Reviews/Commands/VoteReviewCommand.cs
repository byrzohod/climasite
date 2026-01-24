using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reviews.Commands;

public record VoteReviewCommand : IRequest<Result<VoteReviewResult>>
{
    public Guid ReviewId { get; init; }
    public bool IsHelpful { get; init; }
}

public record VoteReviewResult
{
    public int HelpfulCount { get; init; }
    public int UnhelpfulCount { get; init; }
    public bool UserVotedHelpful { get; init; }
}

public class VoteReviewCommandValidator : AbstractValidator<VoteReviewCommand>
{
    public VoteReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");
    }
}

public class VoteReviewCommandHandler : IRequestHandler<VoteReviewCommand, Result<VoteReviewResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public VoteReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<VoteReviewResult>> Handle(
        VoteReviewCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<VoteReviewResult>.Failure("User must be authenticated to vote on reviews");
        }

        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (review == null)
        {
            return Result<VoteReviewResult>.Failure($"Review with ID {request.ReviewId} not found");
        }

        // Check if user is trying to vote on their own review
        if (review.UserId == userId.Value)
        {
            return Result<VoteReviewResult>.Failure("You cannot vote on your own review");
        }

        // Check for existing vote
        var existingVote = await _context.ReviewVotes
            .FirstOrDefaultAsync(
                v => v.ReviewId == request.ReviewId && v.UserId == userId.Value,
                cancellationToken);

        if (existingVote != null)
        {
            // User already voted
            if (existingVote.IsHelpful == request.IsHelpful)
            {
                // Same vote type - no change needed
                return Result<VoteReviewResult>.Success(new VoteReviewResult
                {
                    HelpfulCount = review.HelpfulCount,
                    UnhelpfulCount = review.UnhelpfulCount,
                    UserVotedHelpful = existingVote.IsHelpful
                });
            }

            // Changing vote from helpful to unhelpful or vice versa
            if (existingVote.IsHelpful)
            {
                review.RemoveHelpfulVote();
                review.AddUnhelpfulVote();
            }
            else
            {
                review.RemoveUnhelpfulVote();
                review.AddHelpfulVote();
            }

            existingVote.ChangeVote(request.IsHelpful);
        }
        else
        {
            // New vote
            var vote = new ReviewVote(request.ReviewId, userId.Value, request.IsHelpful);
            _context.ReviewVotes.Add(vote);

            if (request.IsHelpful)
            {
                review.AddHelpfulVote();
            }
            else
            {
                review.AddUnhelpfulVote();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<VoteReviewResult>.Success(new VoteReviewResult
        {
            HelpfulCount = review.HelpfulCount,
            UnhelpfulCount = review.UnhelpfulCount,
            UserVotedHelpful = request.IsHelpful
        });
    }
}
