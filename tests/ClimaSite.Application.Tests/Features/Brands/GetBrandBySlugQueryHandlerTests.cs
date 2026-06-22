using ClimaSite.Application.Features.Brands.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Brands;

public class GetBrandBySlugQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetBrandBySlugQueryHandler CreateHandler() => new(_context);

    private Brand SeedBrand(string name, string slug, bool isActive = true)
    {
        var brand = new Brand(name, slug);
        brand.SetActive(isActive);
        _context.Brands.Add(brand);
        return brand;
    }

    private Product SeedProductForBrand(string brandName, string slug, bool isActive = true, bool isFeatured = false)
    {
        var product = new Product($"SKU-{slug}", $"Product {slug}", slug, 100m);
        product.SetBrand(brandName);
        product.SetActive(isActive);
        product.SetFeatured(isFeatured);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_UnknownSlug_ReturnsNull()
    {
        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "does-not-exist" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InactiveBrand_ReturnsNull()
    {
        SeedBrand("Hidden", "hidden", isActive: false);

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "hidden" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MatchingSlug_ReturnsBrandDto()
    {
        SeedBrand("Daikin", "daikin");

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "daikin" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("daikin");
        result.Name.Should().Be("Daikin");
        result.ProductCount.Should().Be(0);
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LowercasesIncomingSlug_BeforeMatching()
    {
        // Brand slug is stored lowercase; query passes a mixed-case slug that the
        // handler lowercases before comparing.
        SeedBrand("Daikin", "daikin");

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "DAIKIN" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("daikin");
    }

    [Fact]
    public async Task Handle_IncludesOnlyActiveProductsOfThatBrand()
    {
        SeedBrand("Daikin", "daikin");
        SeedProductForBrand("Daikin", "active-one");
        SeedProductForBrand("Daikin", "inactive-one", isActive: false);
        SeedProductForBrand("Mitsubishi", "other-brand");

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "daikin" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProductCount.Should().Be(1);
        result.Products.Should().ContainSingle(p => p.Slug == "active-one");
    }

    [Fact]
    public async Task Handle_PaginatesProducts()
    {
        SeedBrand("Daikin", "daikin");
        for (var i = 1; i <= 15; i++)
        {
            SeedProductForBrand("Daikin", $"prod-{i:D2}");
        }

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "daikin", ProductPageNumber = 1, ProductPageSize = 12 },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProductCount.Should().Be(15);
        result.Products.Should().HaveCount(12);
    }

    [Fact]
    public async Task Handle_UsesTranslatedBrandContent_WhenLanguageMatches()
    {
        var brand = SeedBrand("Daikin", "daikin");
        brand.Translations.Add(new BrandTranslation
        {
            BrandId = brand.Id,
            LanguageCode = "de",
            Name = "Daikin DE",
            Description = "Beschreibung",
            MetaTitle = "Titel"
        });

        var result = await CreateHandler().Handle(
            new GetBrandBySlugQuery { Slug = "daikin", LanguageCode = "de" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Daikin DE");
        result.Description.Should().Be("Beschreibung");
        result.MetaTitle.Should().Be("Titel");
    }
}
