using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Translations.Commands;

public record AddProductTranslationCommand : IRequest<Guid>
{
    public Guid ProductId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public class AddProductTranslationCommandValidator : AbstractValidator<AddProductTranslationCommand>
{
    public AddProductTranslationCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Length(2)
            .WithMessage("Language code must be 2 characters (ISO 639-1)");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Name is required and cannot exceed 255 characters");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .When(x => x.ShortDescription != null)
            .WithMessage("Short description cannot exceed 500 characters");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200)
            .When(x => x.MetaTitle != null)
            .WithMessage("Meta title cannot exceed 200 characters");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500)
            .When(x => x.MetaDescription != null)
            .WithMessage("Meta description cannot exceed 500 characters");
    }
}

public class AddProductTranslationCommandHandler : IRequestHandler<AddProductTranslationCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddProductTranslationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddProductTranslationCommand request, CancellationToken cancellationToken)
    {
        // Verify product exists
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId} not found");
        }

        // Check if translation already exists for this language
        var existingTranslation = await _context.ProductTranslations
            .FirstOrDefaultAsync(
                t => t.ProductId == request.ProductId &&
                     t.LanguageCode == request.LanguageCode.ToLowerInvariant(),
                cancellationToken);

        if (existingTranslation != null)
        {
            throw new InvalidOperationException(
                $"Translation for language '{request.LanguageCode}' already exists. Use PUT to update.");
        }

        var translation = new ProductTranslation(
            request.ProductId,
            request.LanguageCode,
            request.Name)
        {
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription
        };

        _context.ProductTranslations.Add(translation);
        await _context.SaveChangesAsync(cancellationToken);

        return translation.Id;
    }
}
