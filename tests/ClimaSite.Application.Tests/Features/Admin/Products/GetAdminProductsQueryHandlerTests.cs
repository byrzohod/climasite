using ClimaSite.Application.Features.Admin.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class GetAdminProductsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminProductsQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(
        string sku,
        string name,
        decimal basePrice = 100m,
        bool isActive = true,
        string? brand = null,
        int stock = 50,
        int lowStockThreshold = 5,
        decimal? compareAtPrice = null)
    {
        var slug = name.ToLowerInvariant().Replace(" ", "-");
        var product = new Product(sku, name, slug, basePrice);
        product.SetActive(isActive);
        product.SetBrand(brand);
        product.SetCompareAtPrice(compareAtPrice);

        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetLowStockThreshold(lowStockThreshold);
        variant.SetStockQuantity(stock);
        product.Variants.Add(variant);

        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetAdminProductsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsAllProducts_WithPaginationMetadata()
    {
        SeedProduct("SKU-1", "Alpha");
        SeedProduct("SKU-2", "Beta");

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Search_FiltersByNameSkuOrBrand()
    {
        SeedProduct("AAA-111", "Cooling Tower", brand: "Carrier");
        SeedProduct("BBB-222", "Heater", brand: "Bosch");

        var byName = await CreateHandler().Handle(
            new GetAdminProductsQuery { Search = "cooling" }, CancellationToken.None);
        byName.Items.Should().ContainSingle(i => i.Sku == "AAA-111");

        var bySku = await CreateHandler().Handle(
            new GetAdminProductsQuery { Search = "bbb" }, CancellationToken.None);
        bySku.Items.Should().ContainSingle(i => i.Sku == "BBB-222");

        var byBrand = await CreateHandler().Handle(
            new GetAdminProductsQuery { Search = "carrier" }, CancellationToken.None);
        byBrand.Items.Should().ContainSingle(i => i.Sku == "AAA-111");
    }

    [Fact]
    public async Task Handle_StatusActiveFilter_ReturnsOnlyActive()
    {
        SeedProduct("ACT-1", "Active One", isActive: true);
        SeedProduct("INA-1", "Inactive One", isActive: false);

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { Status = "active" }, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Sku == "ACT-1");
    }

    [Fact]
    public async Task Handle_StatusInactiveFilter_ReturnsOnlyInactive()
    {
        SeedProduct("ACT-2", "Active Two", isActive: true);
        SeedProduct("INA-2", "Inactive Two", isActive: false);

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { Status = "inactive" }, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Sku == "INA-2");
    }

    [Fact]
    public async Task Handle_PriceRangeFilter_ReturnsWithinRange()
    {
        SeedProduct("LOW", "Low", basePrice: 50m);
        SeedProduct("MID", "Mid", basePrice: 150m);
        SeedProduct("HIGH", "High", basePrice: 500m);

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { MinPrice = 100m, MaxPrice = 200m }, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Sku == "MID");
    }

    [Fact]
    public async Task Handle_SortByPriceAscending_OrdersByBasePrice()
    {
        SeedProduct("P-HIGH", "Pricey", basePrice: 300m);
        SeedProduct("P-LOW", "Cheap", basePrice: 100m);

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { SortBy = "price", SortOrder = "asc" }, CancellationToken.None);

        result.Items.Select(i => i.Sku).Should().ContainInOrder("P-LOW", "P-HIGH");
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsRequestedPageSlice()
    {
        for (var i = 0; i < 5; i++)
        {
            SeedProduct($"PG-{i}", $"Product {i}");
        }

        var result = await CreateHandler().Handle(
            new GetAdminProductsQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(2);
    }

    // --- B-002 price mapping ---
    // NOTE on what kills the B-002 mutation: the fix changed `SalePrice = p.CompareAtPrice` (raw)
    // to `SalePrice = ProductPricing.GetSalePrice(p.BasePrice, p.CompareAtPrice)`. Those two
    // expressions are IDENTICAL whenever CompareAt > Base, so a *genuine-sale* assertion can NOT
    // distinguish the fix from the bug. The mutation only manifests when CompareAt <= Base — that
    // is the explicit guard below (Handle_CompareAtNotAboveBase_SalePriceIsNull). The on-sale and
    // null Facts are positive characterizations of the contract, not mutation-killers.

    [Fact]
    public async Task Handle_OnSaleProduct_MapsBasePriceAndOriginalCompareAt()
    {
        // Characterization: on a genuine sale, Price carries the current selling price and
        // SalePrice carries the higher original (struck-through on the UI). Passes on both the
        // old and new mapping — see the NOTE above; the mutation guard is the Theory below.
        SeedProduct("SALE", "On Sale", basePrice: 499.99m, compareAtPrice: 599.99m);

        var result = await CreateHandler().Handle(new GetAdminProductsQuery(), CancellationToken.None);

        var item = result.Items.Single(i => i.Sku == "SALE");
        item.Price.Should().Be(499.99m);
        item.SalePrice.Should().Be(599.99m);
    }

    [Fact]
    public async Task Handle_NotOnSale_NullCompareAt_SalePriceIsNull()
    {
        // Characterization of the no-compare-at branch (null on both old and new mapping).
        SeedProduct("PLAIN", "No Sale", basePrice: 100m, compareAtPrice: null);

        var result = await CreateHandler().Handle(new GetAdminProductsQuery(), CancellationToken.None);

        result.Items.Single(i => i.Sku == "PLAIN").SalePrice.Should().BeNull();
    }

    [Theory]
    [InlineData(100)] // compare-at equal to base — not a real sale
    [InlineData(80)]  // compare-at below base — legacy/garbage data, not a sale
    public async Task Handle_CompareAtNotAboveBase_SalePriceIsNull(decimal compareAt)
    {
        // THE B-002 mutation guard. The old raw mapping (SalePrice = p.CompareAtPrice) echoes a
        // non-null value here and paints a fake sale; GetSalePrice returns null unless CompareAt
        // strictly exceeds Base. This is the ONLY case where the two mappings differ, so reverting
        // the handler fix turns this test (and only this test) red.
        SeedProduct("FAKE", "Fake Sale", basePrice: 100m, compareAtPrice: compareAt);

        var result = await CreateHandler().Handle(new GetAdminProductsQuery(), CancellationToken.None);

        result.Items.Single(i => i.Sku == "FAKE").SalePrice.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsStatusFromProductState()
    {
        SeedProduct("ST-ACTIVE", "In Stock", isActive: true, stock: 50, lowStockThreshold: 5);
        SeedProduct("ST-LOW", "Low Stock", isActive: true, stock: 3, lowStockThreshold: 5);
        SeedProduct("ST-OUT", "Out Of Stock", isActive: true, stock: 0, lowStockThreshold: 5);
        SeedProduct("ST-INACTIVE", "Inactive", isActive: false, stock: 50);

        var result = await CreateHandler().Handle(new GetAdminProductsQuery(), CancellationToken.None);

        result.Items.Single(i => i.Sku == "ST-ACTIVE").Status.Should().Be("Active");
        result.Items.Single(i => i.Sku == "ST-LOW").Status.Should().Be("LowStock");
        result.Items.Single(i => i.Sku == "ST-OUT").Status.Should().Be("OutOfStock");
        result.Items.Single(i => i.Sku == "ST-INACTIVE").Status.Should().Be("Inactive");
    }
}
