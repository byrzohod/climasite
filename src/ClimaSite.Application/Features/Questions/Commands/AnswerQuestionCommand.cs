using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record AnswerQuestionCommand : IRequest<Guid>
{
    public Guid QuestionId { get; init; }
    public Guid? UserId { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswererName { get; init; }
    public bool IsOfficial { get; init; }
}

public class AnswerQuestionCommandValidator : AbstractValidator<AnswerQuestionCommand>
{
    public AnswerQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required.");

        RuleFor(x => x.AnswerText)
            .NotEmpty().WithMessage("Answer text is required.")
            .MinimumLength(10).WithMessage("Answer must be at least 10 characters.")
            .MaximumLength(5000).WithMessage("Answer cannot exceed 5000 characters.");

        RuleFor(x => x.AnswererName)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
    }
}

public class AnswerQuestionCommandHandler : IRequestHandler<AnswerQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AnswerQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AnswerQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (question == null)
        {
            throw new InvalidOperationException($"Question with ID {request.QuestionId} not found.");
        }

        var answer = new ProductAnswer(request.QuestionId, request.AnswerText);

        if (request.UserId.HasValue)
        {
            answer.SetUser(request.UserId.Value);
        }

        if (!string.IsNullOrEmpty(request.AnswererName))
        {
            answer.SetAnswererName(request.AnswererName);
        }

        if (request.IsOfficial)
        {
            answer.SetOfficial(true);
        }

        _context.ProductAnswers.Add(answer);

        // Mark question as answered if this is the first answer
        if (!question.AnsweredAt.HasValue)
        {
            question.MarkAsAnswered();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return answer.Id;
    }
}
