using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.RelatedProducts.Commands;

public record RemoveRelatedProductCommand(
    Guid ProductId,
    Guid RelationId) : IRequest<bool>;

public class RemoveRelatedProductCommandHandler : IRequestHandler<RemoveRelatedProductCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RemoveRelatedProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RemoveRelatedProductCommand request, CancellationToken cancellationToken)
    {
        var relation = await _context.RelatedProducts
            .FirstOrDefaultAsync(rp => rp.Id == request.RelationId &&
                                      rp.ProductId == request.ProductId,
                cancellationToken);

        if (relation == null)
            return false;

        _context.RelatedProducts.Remove(relation);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
