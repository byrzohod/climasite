using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record ModerateQuestionCommand : IRequest<Result<bool>>
{
    public Guid QuestionId { get; init; }
    public QuestionStatus NewStatus { get; init; }
    public string? ModerationNote { get; init; }
}

public class ModerateQuestionCommandHandler : IRequestHandler<ModerateQuestionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public ModerateQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(
        ModerateQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var question = await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (question == null)
        {
            return Result<bool>.Failure("Question not found");
        }

        question.SetStatus(request.NewStatus);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
