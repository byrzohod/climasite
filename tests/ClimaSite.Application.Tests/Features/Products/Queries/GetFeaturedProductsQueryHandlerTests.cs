using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Products.Queries;

public class GetFeaturedProductsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetFeaturedProductsQueryHandler CreateHandler() => new(_context);

    private static Product MakeProduct(
        string sku,
        string name,
        string slug,
        decimal basePrice = 100m,
        bool isActive = true,
        bool isFeatured = true)
    {
        var product = new Product(sku, name, slug, basePrice);
        product.SetActive(isActive);
        product.SetFeatured(isFeatured);
        return product;
    }

    private static void SetCreatedAt(Product product, DateTime createdAt) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!.SetValue(product, createdAt);

    [Fact]
    public async Task Handle_ReturnsOnlyActiveFeaturedProducts()
    {
        _context.AddProduct(MakeProduct("FEAT-1", "Featured One", "featured-one", isFeatured: true));
        _context.AddProduct(MakeProduct("NOT-FEAT", "Not Featured", "not-featured", isFeatured: false));
        _context.AddProduct(MakeProduct("INACTIVE", "Inactive Featured", "inactive-featured",
            isActive: false, isFeatured: true));

        var result = await CreateHandler().Handle(new GetFeaturedProductsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Slug.Should().Be("featured-one");
    }

    [Fact]
    public async Task Handle_OrdersByCreatedAtDescending()
    {
        var oldest = MakeProduct("OLD", "Oldest", "oldest");
        SetCreatedAt(oldest, DateTime.UtcNow.AddDays(-3));
        var newest = MakeProduct("NEW", "Newest", "newest");
        SetCreatedAt(newest, DateTime.UtcNow);
        _context.AddProduct(oldest);
        _context.AddProduct(newest);

        var result = await CreateHandler().Handle(new GetFeaturedProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Slug.Should().Be("newest");
        result[1].Slug.Should().Be("oldest");
    }

    [Fact]
    public async Task Handle_RespectsCountLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            var product = MakeProduct($"SKU-{i}", $"Product {i}", $"product-{i}");
            SetCreatedAt(product, DateTime.UtcNow.AddMinutes(-i));
            _context.AddProduct(product);
        }

        var result = await CreateHandler().Handle(
            new GetFeaturedProductsQuery { Count = 2 }, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoFeaturedProducts_ReturnsEmptyList()
    {
        _context.AddProduct(MakeProduct("PLAIN", "Plain", "plain", isFeatured: false));

        var result = await CreateHandler().Handle(new GetFeaturedProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProjectsPrimaryImageAndStockAndSale()
    {
        var product = MakeProduct("RICH", "Rich Product", "rich-product", basePrice: 799.99m);
        product.SetCompareAtPrice(999.99m);

        var primaryImage = new ProductImage(product.Id, "https://cdn.example.com/primary.jpg");
        primaryImage.SetPrimary(true);
        var secondaryImage = new ProductImage(product.Id, "https://cdn.example.com/secondary.jpg");
        product.Images.Add(primaryImage);
        product.Images.Add(secondaryImage);

        var variant = new ProductVariant(product.Id, "RICH-V1", "Default");
        variant.SetStockQuantity(7);
        product.Variants.Add(variant);

        _context.AddProduct(product);

        var result = await CreateHandler().Handle(new GetFeaturedProductsQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.PrimaryImageUrl.Should().Be("https://cdn.example.com/primary.jpg");
        dto.InStock.Should().BeTrue();
        dto.BasePrice.Should().Be(799.99m);
        dto.SalePrice.Should().Be(999.99m);
        dto.IsOnSale.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OutOfStockProduct_ReportsNotInStock()
    {
        var product = MakeProduct("OOS", "Out Of Stock", "out-of-stock");
        var variant = new ProductVariant(product.Id, "OOS-V1", "Default");
        variant.SetStockQuantity(0);
        product.Variants.Add(variant);
        _context.AddProduct(product);

        var result = await CreateHandler().Handle(new GetFeaturedProductsQuery(), CancellationToken.None);

        result.Should().ContainSingle().Which.InStock.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UsesTranslatedNameForRequestedLanguage()
    {
        var product = MakeProduct("TRANS", "English Name", "translated-product");
        product.Translations.Add(new ProductTranslation(product.Id, "de", "Deutscher Name")
        {
            ShortDescription = "Kurze Beschreibung"
        });
        _context.AddProduct(product);

        var result = await CreateHandler().Handle(
            new GetFeaturedProductsQuery { LanguageCode = "de" }, CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Name.Should().Be("Deutscher Name");
        dto.ShortDescription.Should().Be("Kurze Beschreibung");
    }

    [Fact]
    public async Task Handle_FallsBackToDefaultNameWhenTranslationMissing()
    {
        var product = MakeProduct("NOTRANS", "Default Name", "no-translation");
        _context.AddProduct(product);

        var result = await CreateHandler().Handle(
            new GetFeaturedProductsQuery { LanguageCode = "bg" }, CancellationToken.None);

        result.Should().ContainSingle().Which.Name.Should().Be("Default Name");
    }
}
