using ClimaSite.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record VoteQuestionCommand : IRequest<int>
{
    public Guid QuestionId { get; init; }
}

public class VoteQuestionCommandValidator : AbstractValidator<VoteQuestionCommand>
{
    public VoteQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required.");
    }
}

public class VoteQuestionCommandHandler : IRequestHandler<VoteQuestionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public VoteQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(VoteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (question == null)
        {
            throw new InvalidOperationException($"Question with ID {request.QuestionId} not found.");
        }

        question.AddHelpfulVote();
        await _context.SaveChangesAsync(cancellationToken);

        return question.HelpfulCount;
    }
}
