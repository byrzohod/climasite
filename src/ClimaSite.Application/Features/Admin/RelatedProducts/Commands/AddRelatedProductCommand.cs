using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.RelatedProducts.Commands;

public record AddRelatedProductCommand(
    Guid ProductId,
    Guid RelatedProductId,
    string RelationType) : IRequest<Guid>;

public class AddRelatedProductCommandValidator : AbstractValidator<AddRelatedProductCommand>
{
    public AddRelatedProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.RelatedProductId).NotEmpty();
        RuleFor(x => x.RelationType)
            .NotEmpty()
            .Must(BeValidRelationType)
            .WithMessage("Invalid relation type");
        RuleFor(x => x.ProductId)
            .NotEqual(x => x.RelatedProductId)
            .WithMessage("A product cannot be related to itself");
    }

    private bool BeValidRelationType(string type)
    {
        return Enum.TryParse<RelationType>(type, ignoreCase: true, out _);
    }
}

public class AddRelatedProductCommandHandler : IRequestHandler<AddRelatedProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddRelatedProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddRelatedProductCommand request, CancellationToken cancellationToken)
    {
        // Verify both products exist
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
            throw new InvalidOperationException($"Product {request.ProductId} not found");

        var relatedExists = await _context.Products
            .AnyAsync(p => p.Id == request.RelatedProductId, cancellationToken);
        if (!relatedExists)
            throw new InvalidOperationException($"Related product {request.RelatedProductId} not found");

        // Check if relation already exists
        var relationType = Enum.Parse<RelationType>(request.RelationType, ignoreCase: true);
        var existingRelation = await _context.RelatedProducts
            .AnyAsync(rp => rp.ProductId == request.ProductId &&
                          rp.RelatedProductId == request.RelatedProductId &&
                          rp.RelationType == relationType,
                cancellationToken);

        if (existingRelation)
            throw new InvalidOperationException("This relation already exists");

        // Get max sort order for this product and relation type
        var maxSortOrder = await _context.RelatedProducts
            .Where(rp => rp.ProductId == request.ProductId && rp.RelationType == relationType)
            .MaxAsync(rp => (int?)rp.SortOrder, cancellationToken) ?? -1;

        var relation = new RelatedProduct(
            request.ProductId,
            request.RelatedProductId,
            relationType);
        relation.SetSortOrder(maxSortOrder + 1);

        _context.RelatedProducts.Add(relation);
        await _context.SaveChangesAsync(cancellationToken);

        return relation.Id;
    }
}
