using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Products;

public class GetProductBySlugQueryHandlerTests
{
    [Fact]
    public async Task Handle_OnSaleProduct_ProjectsCurrentPriceAsBaseAndOriginalAsSale()
    {
        // BUG-06: "DualZone Pro 12000" sells for 899.99 with an original list price of
        // 1099.99 -> the customer pays 899.99 (BasePrice), the struck-through original
        // (SalePrice) is 1099.99, and the discount is 18%.
        var context = new MockDbContext();
        var product = new Product("DZP-12000", "DualZone Pro 12000", "dualzone-pro-12000", 899.99m);
        product.SetCompareAtPrice(1099.99m);
        product.SetActive(true);
        context.AddProduct(product);

        var handler = new GetProductBySlugQueryHandler(context);
        var query = new GetProductBySlugQuery { Slug = "dualzone-pro-12000" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.BasePrice.Should().Be(899.99m);
        result.SalePrice.Should().Be(1099.99m);
        result.IsOnSale.Should().BeTrue();
        result.DiscountPercentage.Should().Be(18m);
    }

    [Fact]
    public async Task Handle_NotOnSaleProduct_HasNullSalePriceAndZeroDiscount()
    {
        var context = new MockDbContext();
        var product = new Product("REG-001", "Regular Unit", "regular-unit", 599.99m);
        product.SetActive(true);
        context.AddProduct(product);

        var handler = new GetProductBySlugQueryHandler(context);
        var query = new GetProductBySlugQuery { Slug = "regular-unit" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.BasePrice.Should().Be(599.99m);
        result.SalePrice.Should().BeNull();
        result.IsOnSale.Should().BeFalse();
        result.DiscountPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_PassesThroughCuratedMeta_WhenSet()
    {
        // B-044 (M8): curated SEO meta on the product must flow through the detail DTO so the frontend
        // can prefer metaTitle/metaDescription over the name/short-description fallbacks.
        var context = new MockDbContext();
        var product = new Product("META-001", "Curated Unit", "curated-unit", 499.99m);
        product.SetMetaTitle("Curated Unit — Quiet 12000 BTU Inverter AC | ClimaSite");
        product.SetMetaDescription("Energy-efficient inverter air conditioner, whisper-quiet at 19 dB.");
        product.SetActive(true);
        context.AddProduct(product);

        var handler = new GetProductBySlugQueryHandler(context);
        var query = new GetProductBySlugQuery { Slug = "curated-unit" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.MetaTitle.Should().Be("Curated Unit — Quiet 12000 BTU Inverter AC | ClimaSite");
        result.MetaDescription.Should().Be("Energy-efficient inverter air conditioner, whisper-quiet at 19 dB.");
    }

    [Fact]
    public async Task Handle_LeavesMetaNull_WhenNoCuratedMetaSet()
    {
        var context = new MockDbContext();
        var product = new Product("REG-002", "Plain Unit", "plain-unit", 599.99m);
        product.SetActive(true);
        context.AddProduct(product);

        var handler = new GetProductBySlugQueryHandler(context);
        var query = new GetProductBySlugQuery { Slug = "plain-unit" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.MetaTitle.Should().BeNull();
        result.MetaDescription.Should().BeNull();
    }
}
