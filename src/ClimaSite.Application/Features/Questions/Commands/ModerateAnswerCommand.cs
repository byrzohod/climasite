using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record ModerateAnswerCommand : IRequest<Result<bool>>
{
    public Guid AnswerId { get; init; }
    public AnswerStatus NewStatus { get; init; }
    public bool? MarkAsOfficial { get; init; }
    public string? ModerationNote { get; init; }
}

public class ModerateAnswerCommandHandler : IRequestHandler<ModerateAnswerCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public ModerateAnswerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(
        ModerateAnswerCommand request,
        CancellationToken cancellationToken)
    {
        // Load the question WITH its sibling answers so answered-state can be reconciled purely from the
        // tracked in-memory graph after the status change — no post-mutation DB round-trip (which would
        // read the not-yet-saved row and diverge). EF identity-resolves the mutated `answer` into
        // `question.Answers`, so RefreshAnsweredState sees its new status.
        var answer = await _context.ProductAnswers
            .Include(a => a.Question)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(a => a.Id == request.AnswerId, cancellationToken);

        if (answer == null)
        {
            return Result<bool>.Failure("Answer not found");
        }

        answer.SetStatus(request.NewStatus);

        if (request.MarkAsOfficial.HasValue)
        {
            answer.SetOfficial(request.MarkAsOfficial.Value);
        }

        // Reconcile the question's answered-state from approved-answer existence. Sets AnsweredAt on the
        // first approved answer, clears it when the last approved answer is rejected/un-approved (B-038).
        answer.Question.RefreshAnsweredState();

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
