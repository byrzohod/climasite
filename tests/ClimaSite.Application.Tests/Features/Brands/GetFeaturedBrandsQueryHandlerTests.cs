using ClimaSite.Application.Features.Brands.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Brands;

public class GetFeaturedBrandsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetFeaturedBrandsQueryHandler CreateHandler() => new(_context);

    private Brand SeedBrand(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = true,
        int sortOrder = 0)
    {
        var brand = new Brand(name, slug);
        brand.SetActive(isActive);
        brand.SetFeatured(isFeatured);
        brand.SetSortOrder(sortOrder);
        _context.Brands.Add(brand);
        return brand;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveFeaturedBrands()
    {
        SeedBrand("Featured", "featured", isFeatured: true);
        SeedBrand("NotFeatured", "not-featured", isFeatured: false);
        SeedBrand("InactiveFeatured", "inactive-featured", isActive: false, isFeatured: true);

        var result = await CreateHandler().Handle(new GetFeaturedBrandsQuery(), CancellationToken.None);

        result.Should().ContainSingle(b => b.Slug == "featured");
    }

    [Fact]
    public async Task Handle_OrdersBySortOrderThenName()
    {
        SeedBrand("Zephyr", "zephyr", sortOrder: 0);
        SeedBrand("Acme", "acme", sortOrder: 0);
        SeedBrand("First", "first", sortOrder: -1);

        var result = await CreateHandler().Handle(new GetFeaturedBrandsQuery(), CancellationToken.None);

        result.Select(b => b.Slug).Should().ContainInOrder("first", "acme", "zephyr");
    }

    [Fact]
    public async Task Handle_RespectsLimit()
    {
        for (var i = 1; i <= 10; i++)
        {
            SeedBrand($"Brand {i:D2}", $"brand-{i:D2}", sortOrder: i);
        }

        var result = await CreateHandler().Handle(
            new GetFeaturedBrandsQuery { Limit = 3 },
            CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(b => b.Slug).Should().ContainInOrder("brand-01", "brand-02", "brand-03");
    }

    [Fact]
    public async Task Handle_PopulatesProductCount_FromActiveProductsForThatBrand()
    {
        SeedBrand("Daikin", "daikin");

        var p1 = new Product("SKU-1", "AC One", "ac-one", 100m);
        p1.SetBrand("Daikin");
        p1.SetActive(true);
        var inactive = new Product("SKU-2", "AC Two", "ac-two", 200m);
        inactive.SetBrand("Daikin");
        inactive.SetActive(false);

        _context.AddProduct(p1);
        _context.AddProduct(inactive);

        var result = await CreateHandler().Handle(new GetFeaturedBrandsQuery(), CancellationToken.None);

        result.Single(b => b.Slug == "daikin").ProductCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UsesTranslatedContent_WhenLanguageMatches()
    {
        var brand = SeedBrand("Daikin", "daikin");
        brand.Translations.Add(new BrandTranslation
        {
            BrandId = brand.Id,
            LanguageCode = "bg",
            Name = "Дайкин"
        });

        var result = await CreateHandler().Handle(
            new GetFeaturedBrandsQuery { LanguageCode = "bg" },
            CancellationToken.None);

        result.Single(b => b.Slug == "daikin").Name.Should().Be("Дайкин");
    }

    [Fact]
    public async Task Handle_NoFeaturedBrands_ReturnsEmptyList()
    {
        SeedBrand("NotFeatured", "not-featured", isFeatured: false);

        var result = await CreateHandler().Handle(new GetFeaturedBrandsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
