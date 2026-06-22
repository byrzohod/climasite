using ClimaSite.Application.Features.Admin.RelatedProducts.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.RelatedProducts;

public class AddRelatedProductCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private AddRelatedProductCommandHandler CreateHandler() => new(_context);

    private Product SeedProduct(string sku, string slug)
    {
        var product = new Product(sku, sku, slug, 100m);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_BothProductsExist_CreatesRelation_AndReturnsId()
    {
        var product = SeedProduct("SKU-P1", "p1");
        var related = SeedProduct("SKU-P2", "p2");

        var id = await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related.Id, nameof(RelationType.Accessory)), CancellationToken.None);

        id.Should().NotBeEmpty();

        var stored = _context.RelatedProducts.Single();
        stored.ProductId.Should().Be(product.Id);
        stored.RelatedProductId.Should().Be(related.Id);
        stored.RelationType.Should().Be(RelationType.Accessory);
        // First relation of this (product, type) gets sort order 0.
        stored.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SecondRelationOfSameType_GetsIncrementedSortOrder()
    {
        var product = SeedProduct("SKU-P1", "p1");
        var related1 = SeedProduct("SKU-P2", "p2");
        var related2 = SeedProduct("SKU-P3", "p3");

        await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related1.Id, nameof(RelationType.Similar)), CancellationToken.None);
        await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related2.Id, nameof(RelationType.Similar)), CancellationToken.None);

        var second = _context.RelatedProducts
            .Single(rp => rp.RelatedProductId == related2.Id);
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RelationTypeIsCaseInsensitive()
    {
        var product = SeedProduct("SKU-P1", "p1");
        var related = SeedProduct("SKU-P2", "p2");

        await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related.Id, "accessory"), CancellationToken.None);

        _context.RelatedProducts.Single().RelationType.Should().Be(RelationType.Accessory);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_Throws()
    {
        var related = SeedProduct("SKU-P2", "p2");

        var act = async () => await CreateHandler().Handle(new AddRelatedProductCommand(
            Guid.NewGuid(), related.Id, nameof(RelationType.Similar)), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_RelatedProductDoesNotExist_Throws()
    {
        var product = SeedProduct("SKU-P1", "p1");

        var act = async () => await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, Guid.NewGuid(), nameof(RelationType.Similar)), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_DuplicateRelation_Throws()
    {
        var product = SeedProduct("SKU-P1", "p1");
        var related = SeedProduct("SKU-P2", "p2");

        await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related.Id, nameof(RelationType.Similar)), CancellationToken.None);

        var act = async () => await CreateHandler().Handle(new AddRelatedProductCommand(
            product.Id, related.Id, nameof(RelationType.Similar)), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
}
