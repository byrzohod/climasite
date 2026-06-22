using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Products.Queries;

public class GetFilterOptionsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetFilterOptionsQueryHandler CreateHandler() => new(_context);

    private static Product MakeProduct(
        string sku,
        string slug,
        decimal basePrice,
        string? brand = null,
        bool isActive = true)
    {
        var product = new Product(sku, $"Name {sku}", slug, basePrice);
        product.SetActive(isActive);
        if (brand != null)
        {
            product.SetBrand(brand);
        }
        return product;
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyFacetsAndZeroPriceRange()
    {
        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.Brands.Should().BeEmpty();
        result.Tags.Should().BeEmpty();
        result.Specifications.Should().BeEmpty();
        result.PriceRange.Min.Should().Be(0);
        result.PriceRange.Max.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AggregatesBrandCountsOrderedByCountThenName()
    {
        _context.AddProduct(MakeProduct("A1", "a1", 100m, brand: "Daikin"));
        _context.AddProduct(MakeProduct("A2", "a2", 100m, brand: "Daikin"));
        _context.AddProduct(MakeProduct("B1", "b1", 100m, brand: "Mitsubishi"));

        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.Brands.Should().HaveCount(2);
        result.Brands[0].Name.Should().Be("Daikin");
        result.Brands[0].Count.Should().Be(2);
        result.Brands[1].Name.Should().Be("Mitsubishi");
        result.Brands[1].Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ComputesPriceRangeAcrossActiveProducts()
    {
        _context.AddProduct(MakeProduct("LOW", "low", 199.99m, brand: "X"));
        _context.AddProduct(MakeProduct("MID", "mid", 549.50m, brand: "X"));
        _context.AddProduct(MakeProduct("HIGH", "high", 1299m, brand: "X"));

        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.PriceRange.Min.Should().Be(199.99m);
        result.PriceRange.Max.Should().Be(1299m);
    }

    [Fact]
    public async Task Handle_ExcludesInactiveProductsFromFacets()
    {
        _context.AddProduct(MakeProduct("ACTIVE", "active", 100m, brand: "Active"));
        _context.AddProduct(MakeProduct("INACTIVE", "inactive", 9999m, brand: "Inactive", isActive: false));

        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.Brands.Should().ContainSingle(b => b.Name == "Active");
        result.PriceRange.Max.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_AggregatesTags()
    {
        var p1 = MakeProduct("T1", "t1", 100m, brand: "X");
        p1.SetTags(new List<string> { "quiet", "inverter" });
        var p2 = MakeProduct("T2", "t2", 100m, brand: "X");
        p2.SetTags(new List<string> { "quiet" });
        _context.AddProduct(p1);
        _context.AddProduct(p2);

        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.Tags.Should().HaveCount(2);
        result.Tags[0].Name.Should().Be("quiet");
        result.Tags[0].Count.Should().Be(2);
        result.Tags.Should().Contain(t => t.Name == "inverter" && t.Count == 1);
    }

    [Fact]
    public async Task Handle_ExtractsKnownHvacSpecificationsAndFormatsLabels()
    {
        var p1 = MakeProduct("S1", "s1", 100m, brand: "X");
        p1.SetSpecifications(new Dictionary<string, object>
        {
            ["btu"] = "12000",
            ["seer"] = "20",
            ["color"] = "white" // not an HVAC spec key -> ignored
        });
        var p2 = MakeProduct("S2", "s2", 100m, brand: "X");
        p2.SetSpecifications(new Dictionary<string, object>
        {
            ["btu"] = "12000"
        });
        _context.AddProduct(p1);
        _context.AddProduct(p2);

        var result = await CreateHandler().Handle(new GetFilterOptionsQuery(), CancellationToken.None);

        result.Specifications.Should().ContainKey("btu");
        result.Specifications.Should().ContainKey("seer");
        result.Specifications.Should().NotContainKey("color");

        var btu = result.Specifications["btu"].Should().ContainSingle().Subject;
        btu.Value.Should().Be("12000");
        btu.Label.Should().Be("12000 BTU");
        btu.Count.Should().Be(2);

        result.Specifications["seer"].Should().ContainSingle()
            .Which.Label.Should().Be("SEER 20");
    }

    [Fact]
    public async Task Handle_UnknownCategorySlug_DoesNotFilterAndReturnsAllFacets()
    {
        _context.AddProduct(MakeProduct("ANY", "any", 100m, brand: "Anybrand"));

        var result = await CreateHandler().Handle(
            new GetFilterOptionsQuery { CategorySlug = "does-not-exist" }, CancellationToken.None);

        // Unknown slug yields no category ids -> the where clause is skipped, all products counted.
        result.Brands.Should().ContainSingle(b => b.Name == "Anybrand");
    }

    [Fact]
    public async Task Handle_FiltersByCategorySlugIncludingDescendants()
    {
        var parent = new Category("Cooling", "cooling");
        var child = new Category("Air Conditioners", "air-conditioners");
        child.SetParent(parent.Id);
        _context.Categories.Add(parent);
        _context.Categories.Add(child);

        var inParent = MakeProduct("IN-PARENT", "in-parent", 100m, brand: "ParentBrand");
        inParent.SetCategory(parent.Id);
        var inChild = MakeProduct("IN-CHILD", "in-child", 200m, brand: "ChildBrand");
        inChild.SetCategory(child.Id);
        var elsewhere = MakeProduct("ELSEWHERE", "elsewhere", 300m, brand: "OtherBrand");
        _context.AddProduct(inParent);
        _context.AddProduct(inChild);
        _context.AddProduct(elsewhere);

        var result = await CreateHandler().Handle(
            new GetFilterOptionsQuery { CategorySlug = "cooling" }, CancellationToken.None);

        result.Brands.Should().HaveCount(2);
        result.Brands.Select(b => b.Name).Should().BeEquivalentTo("ParentBrand", "ChildBrand");
        result.PriceRange.Min.Should().Be(100m);
        result.PriceRange.Max.Should().Be(200m);
    }
}
