using ClimaSite.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record VoteAnswerCommand : IRequest<VoteAnswerResult>
{
    public Guid AnswerId { get; init; }
    public bool IsHelpful { get; init; }
}

public record VoteAnswerResult
{
    public int HelpfulCount { get; init; }
    public int UnhelpfulCount { get; init; }
}

public class VoteAnswerCommandValidator : AbstractValidator<VoteAnswerCommand>
{
    public VoteAnswerCommandValidator()
    {
        RuleFor(x => x.AnswerId)
            .NotEmpty().WithMessage("Answer ID is required.");
    }
}

public class VoteAnswerCommandHandler : IRequestHandler<VoteAnswerCommand, VoteAnswerResult>
{
    private readonly IApplicationDbContext _context;

    public VoteAnswerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VoteAnswerResult> Handle(VoteAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _context.ProductAnswers
            .FirstOrDefaultAsync(a => a.Id == request.AnswerId, cancellationToken);

        if (answer == null)
        {
            throw new InvalidOperationException($"Answer with ID {request.AnswerId} not found.");
        }

        if (request.IsHelpful)
        {
            answer.AddHelpfulVote();
        }
        else
        {
            answer.AddUnhelpfulVote();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new VoteAnswerResult
        {
            HelpfulCount = answer.HelpfulCount,
            UnhelpfulCount = answer.UnhelpfulCount
        };
    }
}
