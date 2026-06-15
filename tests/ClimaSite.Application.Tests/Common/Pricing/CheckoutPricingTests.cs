using ClimaSite.Application.Common.Pricing;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Common.Pricing;

public class CheckoutPricingTests
{
    [Fact]
    public void Currency_IsEur()
    {
        CheckoutPricing.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData("express", 15.99)]
    [InlineData("standard", 5.99)]
    [InlineData("free", 0)]
    [InlineData("overnight", 9.99)] // unknown -> default
    [InlineData(null, 9.99)]
    [InlineData("", 9.99)]
    public void GetShippingCost_ReturnsExpectedRate(string? method, decimal expected)
    {
        CheckoutPricing.GetShippingCost(method).Should().Be(expected);
    }

    [Theory]
    [InlineData("EXPRESS", 15.99)]
    [InlineData("Standard", 5.99)]
    [InlineData("Free", 0)]
    public void GetShippingCost_IsCaseInsensitive(string method, decimal expected)
    {
        CheckoutPricing.GetShippingCost(method).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 20.00)]
    [InlineData(0, 0)]
    [InlineData(99.99, 20.00)] // 19.998 rounds to 20.00
    public void GetTax_ReturnsTwentyPercentRounded(decimal subtotal, decimal expectedTax)
    {
        CheckoutPricing.GetTax(subtotal).Should().Be(expectedTax);
    }

    [Theory]
    // subtotal + shipping + tax
    [InlineData(100, "standard", 125.99)]  // 100 + 5.99 + 20.00
    [InlineData(100, "express", 135.99)]   // 100 + 15.99 + 20.00
    [InlineData(100, "free", 120.00)]      // 100 + 0 + 20.00
    [InlineData(50, "standard", 65.99)]    // 50 + 5.99 + 10.00
    public void CalculateTotal_EqualsSubtotalPlusShippingPlusTax(decimal subtotal, string method, decimal expectedTotal)
    {
        var total = CheckoutPricing.CalculateTotal(subtotal, method);

        total.Should().Be(expectedTotal);
        total.Should().Be(subtotal + CheckoutPricing.GetShippingCost(method) + CheckoutPricing.GetTax(subtotal));
    }

    [Theory]
    [InlineData(125.99, 12599)]
    [InlineData(100, 10000)]
    [InlineData(0, 0)]
    [InlineData(9.995, 1000)] // rounds away from zero
    public void ToMinorUnits_ConvertsToCents(decimal amount, long expected)
    {
        CheckoutPricing.ToMinorUnits(amount).Should().Be(expected);
    }
}
