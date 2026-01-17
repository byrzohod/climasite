using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Interfaces;
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
        var answer = await _context.ProductAnswers
            .Include(a => a.Question)
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

        // If this is the first approved answer, mark the question as answered
        if (request.NewStatus == AnswerStatus.Approved && !answer.Question.AnsweredAt.HasValue)
        {
            answer.Question.MarkAsAnswered();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
