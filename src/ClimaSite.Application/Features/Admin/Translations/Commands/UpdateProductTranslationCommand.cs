using ClimaSite.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Translations.Commands;

public record UpdateProductTranslationCommand : IRequest<bool>
{
    public Guid ProductId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public class UpdateProductTranslationCommandValidator : AbstractValidator<UpdateProductTranslationCommand>
{
    public UpdateProductTranslationCommandValidator()
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

public class UpdateProductTranslationCommandHandler : IRequestHandler<UpdateProductTranslationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductTranslationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateProductTranslationCommand request, CancellationToken cancellationToken)
    {
        var languageCode = request.LanguageCode.ToLowerInvariant();

        var translation = await _context.ProductTranslations
            .FirstOrDefaultAsync(
                t => t.ProductId == request.ProductId && t.LanguageCode == languageCode,
                cancellationToken);

        if (translation == null)
        {
            return false;
        }

        translation.Name = request.Name;
        translation.ShortDescription = request.ShortDescription;
        translation.Description = request.Description;
        translation.MetaTitle = request.MetaTitle;
        translation.MetaDescription = request.MetaDescription;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
