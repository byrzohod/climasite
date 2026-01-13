using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Translations.Queries;

public record GetProductTranslationsQuery(Guid ProductId) : IRequest<ProductTranslationsDto>;

public class ProductTranslationsDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "en";
    public List<ProductTranslationDto> Translations { get; set; } = new();
    public List<string> AvailableLanguages { get; set; } = new() { "en", "bg", "de" };
}

public class ProductTranslationDto
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetProductTranslationsQueryHandler : IRequestHandler<GetProductTranslationsQuery, ProductTranslationsDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductTranslationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductTranslationsDto> Handle(
        GetProductTranslationsQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId} not found");
        }

        var existingLanguages = product.Translations
            .Select(t => t.LanguageCode)
            .ToList();

        // Available languages that don't have translations yet (excluding English, which is the default)
        var missingLanguages = new[] { "bg", "de" }
            .Except(existingLanguages)
            .ToList();

        return new ProductTranslationsDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            DefaultLanguage = "en",
            Translations = product.Translations
                .OrderBy(t => t.LanguageCode)
                .Select(t => new ProductTranslationDto
                {
                    Id = t.Id,
                    LanguageCode = t.LanguageCode,
                    Name = t.Name,
                    ShortDescription = t.ShortDescription,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToList(),
            AvailableLanguages = missingLanguages
        };
    }
}
