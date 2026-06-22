using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Products.Queries;

public class GetRelatedProductsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetRelatedProductsQueryHandler CreateHandler() => new(_context);

    private static Product MakeProduct(
        string sku,
        string name,
        string slug,
        decimal basePrice = 100m,
        bool isActive = true)
    {
        var product = new Product(sku, name, slug, basePrice);
        product.SetActive(isActive);
        return product;
    }

    /// <summary>
    /// Builds a RelatedProduct edge and wires the <c>Related</c>/<c>Product</c> navigation
    /// properties via reflection — the MockDbContext does not perform EF Include joins, so the
    /// handler's <c>rp.Related.*</c> access must be satisfied by setting it manually.
    /// </summary>
    private static RelatedProduct MakeEdge(
        Product source,
        Product related,
        RelationType type = RelationType.Similar,
        int sortOrder = 0)
    {
        var edge = new RelatedProduct(source.Id, related.Id, type);
        edge.SetSortOrder(sortOrder);

        typeof(RelatedProduct).GetProperty(nameof(RelatedProduct.Related))!
            .SetValue(edge, related);
        typeof(RelatedProduct).GetProperty(nameof(RelatedProduct.Product))!
            .SetValue(edge, source);

        return edge;
    }

    [Fact]
    public async Task Handle_ReturnsRelatedProductsForProduct()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var related1 = MakeProduct("REL1", "Related One", "related-one");
        var related2 = MakeProduct("REL2", "Related Two", "related-two");
        _context.AddProduct(source);
        _context.AddProduct(related1);
        _context.AddProduct(related2);
        _context.RelatedProducts.Add(MakeEdge(source, related1, sortOrder: 0));
        _context.RelatedProducts.Add(MakeEdge(source, related2, sortOrder: 1));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(r => r.Slug).Should().BeEquivalentTo("related-one", "related-two");
    }

    [Fact]
    public async Task Handle_OrdersBySortOrder()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var first = MakeProduct("FIRST", "First", "first");
        var second = MakeProduct("SECOND", "Second", "second");
        _context.AddProduct(source);
        _context.AddProduct(first);
        _context.AddProduct(second);
        _context.RelatedProducts.Add(MakeEdge(source, second, sortOrder: 5));
        _context.RelatedProducts.Add(MakeEdge(source, first, sortOrder: 1));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id }, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Slug.Should().Be("first");
        result[1].Slug.Should().Be("second");
    }

    [Fact]
    public async Task Handle_ExcludesInactiveRelatedProducts()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var active = MakeProduct("ACTIVE", "Active", "active-related");
        var inactive = MakeProduct("INACTIVE", "Inactive", "inactive-related", isActive: false);
        _context.AddProduct(source);
        _context.AddProduct(active);
        _context.AddProduct(inactive);
        _context.RelatedProducts.Add(MakeEdge(source, active));
        _context.RelatedProducts.Add(MakeEdge(source, inactive));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id }, CancellationToken.None);

        result.Should().ContainSingle().Which.Slug.Should().Be("active-related");
    }

    [Fact]
    public async Task Handle_FiltersByRelationType()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var accessory = MakeProduct("ACC", "Accessory", "accessory");
        var similar = MakeProduct("SIM", "Similar", "similar");
        _context.AddProduct(source);
        _context.AddProduct(accessory);
        _context.AddProduct(similar);
        _context.RelatedProducts.Add(MakeEdge(source, accessory, RelationType.Accessory));
        _context.RelatedProducts.Add(MakeEdge(source, similar, RelationType.Similar));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id, RelationType = RelationType.Accessory },
            CancellationToken.None);

        result.Should().ContainSingle().Which.Slug.Should().Be("accessory");
    }

    [Fact]
    public async Task Handle_RespectsCountLimit()
    {
        var source = MakeProduct("SRC", "Source", "source");
        _context.AddProduct(source);
        for (var i = 0; i < 5; i++)
        {
            var related = MakeProduct($"REL-{i}", $"Related {i}", $"related-{i}");
            _context.AddProduct(related);
            _context.RelatedProducts.Add(MakeEdge(source, related, sortOrder: i));
        }

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id, Count = 3 }, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_NoRelatedProducts_ReturnsEmptyList()
    {
        var source = MakeProduct("SRC", "Source", "source");
        _context.AddProduct(source);

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DifferentProductId_ReturnsEmptyList()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var related = MakeProduct("REL", "Related", "related");
        _context.AddProduct(source);
        _context.AddProduct(related);
        _context.RelatedProducts.Add(MakeEdge(source, related));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProjectsTranslatedNameAndStockAndImage()
    {
        var source = MakeProduct("SRC", "Source", "source");
        var related = MakeProduct("REL", "English Related", "english-related", basePrice: 499.99m);
        related.SetCompareAtPrice(599.99m);
        related.Translations.Add(new ProductTranslation(related.Id, "bg", "Български продукт"));
        var image = new ProductImage(related.Id, "https://cdn.example.com/rel.jpg");
        image.SetPrimary(true);
        related.Images.Add(image);
        var variant = new ProductVariant(related.Id, "REL-V1", "Default");
        variant.SetStockQuantity(3);
        related.Variants.Add(variant);

        _context.AddProduct(source);
        _context.AddProduct(related);
        _context.RelatedProducts.Add(MakeEdge(source, related));

        var result = await CreateHandler().Handle(
            new GetRelatedProductsQuery { ProductId = source.Id, LanguageCode = "bg" },
            CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Name.Should().Be("Български продукт");
        dto.PrimaryImageUrl.Should().Be("https://cdn.example.com/rel.jpg");
        dto.InStock.Should().BeTrue();
        dto.IsOnSale.Should().BeTrue();
        dto.SalePrice.Should().Be(599.99m);
    }
}
