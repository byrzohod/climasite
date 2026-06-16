using ClimaSite.Application.Common.Pricing;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Common.Pricing;

public class ProductPricingTests
{
    [Fact]
    public void OnSale_SalePriceIsCompareAt_AndDiscountIsCorrect()
    {
        // DualZone Pro 12000: current selling price 899.99, original 1099.99 -> -18%
        const decimal basePrice = 899.99m;
        decimal? compareAt = 1099.99m;

        ProductPricing.IsOnSale(basePrice, compareAt).Should().BeTrue();
        ProductPricing.GetSalePrice(basePrice, compareAt).Should().Be(1099.99m);
        ProductPricing.GetDiscountPercentage(basePrice, compareAt).Should().Be(18m);
    }

    [Fact]
    public void NotOnSale_NoCompareAt_SalePriceNull_DiscountZero()
    {
        const decimal basePrice = 899.99m;
        decimal? compareAt = null;

        ProductPricing.IsOnSale(basePrice, compareAt).Should().BeFalse();
        ProductPricing.GetSalePrice(basePrice, compareAt).Should().BeNull();
        ProductPricing.GetDiscountPercentage(basePrice, compareAt).Should().Be(0m);
    }

    [Fact]
    public void NotOnSale_CompareAtLowerThanBase_SalePriceNull_DiscountZero()
    {
        const decimal basePrice = 899.99m;
        decimal? compareAt = 799.99m; // compare-at must be HIGHER to be a sale

        ProductPricing.IsOnSale(basePrice, compareAt).Should().BeFalse();
        ProductPricing.GetSalePrice(basePrice, compareAt).Should().BeNull();
        ProductPricing.GetDiscountPercentage(basePrice, compareAt).Should().Be(0m);
    }

    [Fact]
    public void NotOnSale_CompareAtEqualToBase_SalePriceNull_DiscountZero()
    {
        const decimal basePrice = 899.99m;
        decimal? compareAt = 899.99m; // equal is not a sale

        ProductPricing.IsOnSale(basePrice, compareAt).Should().BeFalse();
        ProductPricing.GetSalePrice(basePrice, compareAt).Should().BeNull();
        ProductPricing.GetDiscountPercentage(basePrice, compareAt).Should().Be(0m);
    }

    [Theory]
    [InlineData(80, 100, 20)]    // 20% off
    [InlineData(50, 100, 50)]    // 50% off
    [InlineData(899.99, 1099.99, 18)] // rounds to 18
    public void GetDiscountPercentage_RoundsToWholeNumber(decimal basePrice, decimal compareAt, decimal expected)
    {
        ProductPricing.GetDiscountPercentage(basePrice, compareAt).Should().Be(expected);
    }
}
