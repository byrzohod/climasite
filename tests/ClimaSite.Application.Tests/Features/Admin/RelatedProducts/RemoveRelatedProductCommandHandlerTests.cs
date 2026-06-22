using ClimaSite.Application.Features.Admin.RelatedProducts.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.RelatedProducts;

public class RemoveRelatedProductCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private RemoveRelatedProductCommandHandler CreateHandler() => new(_context);

    private RelatedProduct SeedRelation(Guid productId, Guid relatedProductId)
    {
        var relation = new RelatedProduct(productId, relatedProductId, RelationType.Similar);
        _context.RelatedProducts.Add(relation);
        return relation;
    }

    [Fact]
    public async Task Handle_ExistingRelation_RemovesIt_AndReturnsTrue()
    {
        var productId = Guid.NewGuid();
        var relation = SeedRelation(productId, Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new RemoveRelatedProductCommand(productId, relation.Id), CancellationToken.None);

        result.Should().BeTrue();
        _context.RelatedProducts.Should().NotContain(rp => rp.Id == relation.Id);
    }

    [Fact]
    public async Task Handle_RelationNotFound_ReturnsFalse()
    {
        var result = await CreateHandler().Handle(
            new RemoveRelatedProductCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RelationBelongsToDifferentProduct_ReturnsFalse_AndDoesNotRemove()
    {
        var owningProductId = Guid.NewGuid();
        var relation = SeedRelation(owningProductId, Guid.NewGuid());

        // Same relation id but a different (wrong) owning product id -> must not match.
        var result = await CreateHandler().Handle(
            new RemoveRelatedProductCommand(Guid.NewGuid(), relation.Id), CancellationToken.None);

        result.Should().BeFalse();
        _context.RelatedProducts.Should().Contain(rp => rp.Id == relation.Id);
    }
}
