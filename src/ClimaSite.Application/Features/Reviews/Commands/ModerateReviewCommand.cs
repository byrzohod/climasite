using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reviews.Commands;

public record ModerateReviewCommand : IRequest<Result<bool>>
{
    public Guid ReviewId { get; init; }
    public ReviewStatus NewStatus { get; init; }
    public string? ModerationNote { get; init; }
}

public class ModerateReviewCommandHandler : IRequestHandler<ModerateReviewCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public ModerateReviewCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(
        ModerateReviewCommand request,
        CancellationToken cancellationToken)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (review == null)
        {
            return Result<bool>.Failure("Review not found");
        }

        review.SetStatus(request.NewStatus);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
