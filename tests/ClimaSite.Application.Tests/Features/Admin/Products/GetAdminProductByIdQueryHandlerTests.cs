using ClimaSite.Application.Features.Admin.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class GetAdminProductByIdQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminProductByIdQueryHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNull()
    {
        var result = await CreateHandler().Handle(
            new GetAdminProductByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExistingProduct_MapsScalarFields()
    {
        var product = new Product("AC-DETAIL", "Detail Product", "detail-product", 499m);
        product.SetShortDescription("Short");
        product.SetCompareAtPrice(599m);
        product.SetBrand("Daikin");
        product.SetFeatured(true);
        product.SetWarrantyMonths(60);
        product.AddFeature("Quiet", "Silent", "volume");
        product.SetTags(new List<string> { "inverter" });
        _context.AddProduct(product);

        var result = await CreateHandler().Handle(
            new GetAdminProductByIdQuery { Id = product.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Detail Product");
        result.Sku.Should().Be("AC-DETAIL");
        result.BasePrice.Should().Be(499m);
        result.CompareAtPrice.Should().Be(599m);
        result.Brand.Should().Be("Daikin");
        result.IsFeatured.Should().BeTrue();
        result.WarrantyMonths.Should().Be(60);
        result.Features.Should().ContainSingle(f => f.Title == "Quiet");
        result.Tags.Should().Contain("inverter");
    }

    [Fact]
    public async Task Handle_MapsVariantsAndImagesOrderedBySortOrder()
    {
        var product = new Product("AC-NAV", "Nav Product", "nav-product", 100m);

        var variantB = new ProductVariant(product.Id, "VAR-B", "Variant B");
        variantB.SetSortOrder(2);
        var variantA = new ProductVariant(product.Id, "VAR-A", "Variant A");
        variantA.SetSortOrder(1);
        product.Variants.Add(variantB);
        product.Variants.Add(variantA);

        var imageB = new ProductImage(product.Id, "https://example.com/b.jpg");
        imageB.SetSortOrder(2);
        var imageA = new ProductImage(product.Id, "https://example.com/a.jpg");
        imageA.SetSortOrder(1);
        imageA.SetPrimary(true);
        product.Images.Add(imageB);
        product.Images.Add(imageA);

        _context.AddProduct(product);

        var result = await CreateHandler().Handle(
            new GetAdminProductByIdQuery { Id = product.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Variants.Select(v => v.Sku).Should().ContainInOrder("VAR-A", "VAR-B");
        result.Images.Select(i => i.Url).Should().ContainInOrder(
            "https://example.com/a.jpg", "https://example.com/b.jpg");
        result.Images.First().IsPrimary.Should().BeTrue();
    }
}
