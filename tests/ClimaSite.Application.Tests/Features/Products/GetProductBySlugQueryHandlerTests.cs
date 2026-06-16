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
}
