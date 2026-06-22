using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

public class GetLowStockProductsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetLowStockProductsQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(
        string sku,
        string name,
        int stock,
        int threshold,
        bool variantActive = true,
        string? variantName = null,
        string? primaryImageUrl = null)
    {
        var product = new Product(sku, name, sku.ToLower(), 100m);

        if (primaryImageUrl != null)
        {
            var image = new ProductImage(product.Id, primaryImageUrl);
            image.SetPrimary(true);
            product.Images.Add(image);
        }

        var variant = new ProductVariant(product.Id, $"{sku}-V", variantName ?? "Default");
        variant.SetLowStockThreshold(threshold);
        variant.SetStockQuantity(stock);
        variant.SetActive(variantActive);
        // The query reads v.Product.Name / v.Product.Images via Include; the mock context does not
        // wire ProductVariant.Product, so set it the way EF would materialise it.
        typeof(ProductVariant).GetProperty("Product")!.SetValue(variant, product);
        product.Variants.Add(variant);

        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNothingLowOnStock()
    {
        SeedProduct("OK-PROD", "Healthy", stock: 50, threshold: 5);

        var result = await CreateHandler().Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsVariantsAtOrUnderThreshold_OrderedByStock()
    {
        SeedProduct("LOW-A", "Alpha", stock: 4, threshold: 5);   // low (4 <= 5)
        SeedProduct("LOW-B", "Bravo", stock: 1, threshold: 5);   // lowest
        SeedProduct("OK-C", "Charlie", stock: 20, threshold: 5); // healthy, excluded

        var result = await CreateHandler().Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        // Ordered ascending by current stock.
        result[0].CurrentStock.Should().Be(1);
        result[1].CurrentStock.Should().Be(4);
        result.Should().OnlyContain(p => p.CurrentStock <= p.Threshold);
    }

    [Fact]
    public async Task Handle_ExcludesInactiveVariants()
    {
        SeedProduct("INACT", "Inactive", stock: 0, threshold: 5, variantActive: false);

        var result = await CreateHandler().Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RespectsCountLimit()
    {
        SeedProduct("LOW-1", "One", stock: 1, threshold: 5);
        SeedProduct("LOW-2", "Two", stock: 2, threshold: 5);
        SeedProduct("LOW-3", "Three", stock: 3, threshold: 5);

        var result = await CreateHandler().Handle(
            new GetLowStockProductsQuery { Count = 2 },
            CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_MapsNameWithVariant_AndPrimaryImage()
    {
        SeedProduct(
            "LOW-MAP",
            "Mapped Unit",
            stock: 2,
            threshold: 5,
            variantName: "12000 BTU",
            primaryImageUrl: "https://example.com/primary.jpg");

        var result = await CreateHandler().Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        var dto = result.Single();
        dto.Name.Should().Be("Mapped Unit - 12000 BTU");
        dto.Sku.Should().Be("LOW-MAP-V");
        dto.Threshold.Should().Be(5);
        dto.ImageUrl.Should().Be("https://example.com/primary.jpg");
    }
}
