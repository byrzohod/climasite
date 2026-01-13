using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.RelatedProducts.Commands;

public record ReorderRelatedProductsCommand(
    Guid ProductId,
    string RelationType,
    List<Guid> RelationIds) : IRequest<bool>;

public class ReorderRelatedProductsCommandValidator : AbstractValidator<ReorderRelatedProductsCommand>
{
    public ReorderRelatedProductsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.RelationType).NotEmpty();
        RuleFor(x => x.RelationIds).NotEmpty();
    }
}

public class ReorderRelatedProductsCommandHandler : IRequestHandler<ReorderRelatedProductsCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ReorderRelatedProductsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ReorderRelatedProductsCommand request, CancellationToken cancellationToken)
    {
        var relationType = Enum.Parse<RelationType>(request.RelationType, ignoreCase: true);

        var relations = await _context.RelatedProducts
            .Where(rp => rp.ProductId == request.ProductId &&
                        rp.RelationType == relationType)
            .ToListAsync(cancellationToken);

        if (!relations.Any())
            return false;

        // Reorder based on the provided IDs
        for (int i = 0; i < request.RelationIds.Count; i++)
        {
            var relation = relations.FirstOrDefault(r => r.Id == request.RelationIds[i]);
            if (relation != null)
            {
                relation.SetSortOrder(i);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
