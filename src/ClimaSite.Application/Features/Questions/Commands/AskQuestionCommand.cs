using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Commands;

public record AskQuestionCommand : IRequest<Guid>
{
    public Guid ProductId { get; init; }
    public Guid? UserId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string? AskerName { get; init; }
    public string? AskerEmail { get; init; }
}

public class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.QuestionText)
            .NotEmpty().WithMessage("Question text is required.")
            .MinimumLength(10).WithMessage("Question must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Question cannot exceed 2000 characters.");

        RuleFor(x => x.AskerName)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.AskerEmail)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrEmpty(x.AskerEmail));
    }
}

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AskQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AskQuestionCommand request, CancellationToken cancellationToken)
    {
        // Verify product exists
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId} not found.");
        }

        var question = new ProductQuestion(request.ProductId, request.QuestionText);

        if (request.UserId.HasValue)
        {
            question.SetUser(request.UserId.Value);
        }

        if (!string.IsNullOrEmpty(request.AskerName) || !string.IsNullOrEmpty(request.AskerEmail))
        {
            question.SetAskerInfo(request.AskerName, request.AskerEmail);
        }

        _context.ProductQuestions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}
