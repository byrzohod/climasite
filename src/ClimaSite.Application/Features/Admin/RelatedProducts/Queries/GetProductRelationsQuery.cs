using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.RelatedProducts.Queries;

public record GetProductRelationsQuery(Guid ProductId) : IRequest<ProductRelationsDto>;

public record ProductRelationsDto(
    Guid ProductId,
    string ProductName,
    List<RelationGroupDto> RelationGroups);

public record RelationGroupDto(
    string RelationType,
    List<RelatedProductDto> Relations);

public record RelatedProductDto(
    Guid Id,
    Guid RelatedProductId,
    string Name,
    string Sku,
    string? PrimaryImageUrl,
    decimal Price,
    int SortOrder);

public class GetProductRelationsQueryHandler : IRequestHandler<GetProductRelationsQuery, ProductRelationsDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductRelationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductRelationsDto> Handle(GetProductRelationsQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"Product {request.ProductId} not found");

        var relations = await _context.RelatedProducts
            .Where(rp => rp.ProductId == request.ProductId)
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Images)
            .OrderBy(rp => rp.RelationType)
            .ThenBy(rp => rp.SortOrder)
            .ToListAsync(cancellationToken);

        var relationGroups = relations
            .GroupBy(r => r.RelationType)
            .Select(g => new RelationGroupDto(
                RelationType: g.Key.ToString(),
                Relations: g.Select(r => new RelatedProductDto(
                    Id: r.Id,
                    RelatedProductId: r.RelatedProductId,
                    Name: r.Related.Name,
                    Sku: r.Related.Sku,
                    PrimaryImageUrl: r.Related.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                                    ?? r.Related.Images.FirstOrDefault()?.Url,
                    Price: r.Related.BasePrice,
                    SortOrder: r.SortOrder
                )).ToList()
            )).ToList();

        // Add empty groups for relation types that don't have any relations yet
        var existingTypes = relationGroups.Select(g => g.RelationType).ToHashSet();
        foreach (var type in Enum.GetNames<RelationType>())
        {
            if (!existingTypes.Contains(type))
            {
                relationGroups.Add(new RelationGroupDto(type, new List<RelatedProductDto>()));
            }
        }

        return new ProductRelationsDto(
            ProductId: product.Id,
            ProductName: product.Name,
            RelationGroups: relationGroups.OrderBy(g => g.RelationType).ToList());
    }
}
