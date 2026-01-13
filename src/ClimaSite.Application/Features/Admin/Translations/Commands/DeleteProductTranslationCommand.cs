using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Translations.Commands;

public record DeleteProductTranslationCommand(Guid ProductId, string LanguageCode) : IRequest<bool>;

public class DeleteProductTranslationCommandHandler : IRequestHandler<DeleteProductTranslationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductTranslationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteProductTranslationCommand request, CancellationToken cancellationToken)
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

        _context.ProductTranslations.Remove(translation);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
