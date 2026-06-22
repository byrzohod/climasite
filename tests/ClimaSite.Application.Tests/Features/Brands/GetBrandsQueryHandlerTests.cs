using ClimaSite.Application.Features.Brands.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Brands;

public class GetBrandsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetBrandsQueryHandler CreateHandler() => new(_context);

    private Brand SeedBrand(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = false,
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
    public async Task Handle_ReturnsOnlyActiveBrands()
    {
        SeedBrand("Daikin", "daikin");
        SeedBrand("Inactive", "inactive", isActive: false);

        var result = await CreateHandler().Handle(new GetBrandsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(b => b.Slug == "daikin");
    }

    [Fact]
    public async Task Handle_OrdersBySortOrderThenName()
    {
        SeedBrand("Zephyr", "zephyr", sortOrder: 0);
        SeedBrand("Acme", "acme", sortOrder: 0);
        SeedBrand("First", "first", sortOrder: -5);

        var result = await CreateHandler().Handle(new GetBrandsQuery(), CancellationToken.None);

        result.Items.Select(b => b.Slug).Should().ContainInOrder("first", "acme", "zephyr");
    }

    [Fact]
    public async Task Handle_FeaturedOnly_FiltersToFeaturedBrands()
    {
        SeedBrand("Featured", "featured", isFeatured: true);
        SeedBrand("Plain", "plain", isFeatured: false);

        var result = await CreateHandler().Handle(
            new GetBrandsQuery { FeaturedOnly = true },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(b => b.Slug == "featured" && b.IsFeatured);
    }

    [Fact]
    public async Task Handle_Paginates_ResultsAndReportsTotals()
    {
        for (var i = 1; i <= 30; i++)
        {
            SeedBrand($"Brand {i:D2}", $"brand-{i:D2}", sortOrder: i);
        }

        var result = await CreateHandler().Handle(
            new GetBrandsQuery { PageNumber = 2, PageSize = 24 },
            CancellationToken.None);

        result.TotalCount.Should().Be(30);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().HaveCount(6);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PopulatesProductCount_FromActiveProductsForThatBrand()
    {
        SeedBrand("Daikin", "daikin");

        var p1 = new Product("SKU-1", "AC One", "ac-one", 100m);
        p1.SetBrand("Daikin");
        p1.SetActive(true);
        var p2 = new Product("SKU-2", "AC Two", "ac-two", 200m);
        p2.SetBrand("Daikin");
        p2.SetActive(true);
        var inactive = new Product("SKU-3", "AC Three", "ac-three", 300m);
        inactive.SetBrand("Daikin");
        inactive.SetActive(false);
        var otherBrand = new Product("SKU-4", "AC Four", "ac-four", 400m);
        otherBrand.SetBrand("Mitsubishi");
        otherBrand.SetActive(true);

        _context.AddProduct(p1);
        _context.AddProduct(p2);
        _context.AddProduct(inactive);
        _context.AddProduct(otherBrand);

        var result = await CreateHandler().Handle(new GetBrandsQuery(), CancellationToken.None);

        var daikin = result.Items.Single(b => b.Slug == "daikin");
        daikin.ProductCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UsesTranslatedContent_WhenLanguageMatches()
    {
        var brand = SeedBrand("Daikin", "daikin");
        brand.Translations.Add(new BrandTranslation
        {
            BrandId = brand.Id,
            LanguageCode = "de",
            Name = "Daikin DE",
            Description = "Beschreibung"
        });

        var result = await CreateHandler().Handle(
            new GetBrandsQuery { LanguageCode = "de" },
            CancellationToken.None);

        var item = result.Items.Single(b => b.Slug == "daikin");
        item.Name.Should().Be("Daikin DE");
        item.Description.Should().Be("Beschreibung");
    }

    [Fact]
    public async Task Handle_NoBrands_ReturnsEmptyPaginatedList()
    {
        var result = await CreateHandler().Handle(new GetBrandsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
