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
    // Flat-rate methods are independent of the subtotal (using a sub-threshold subtotal here).
    [InlineData("express", 10, 15.99)]
    [InlineData("overnight", 10, 19.99)]
    [InlineData("free", 10, 9.99)]   // "free" is no longer a recognized method -> default rate (security fix)
    [InlineData("unknown", 10, 9.99)] // unknown -> default
    [InlineData(null, 10, 9.99)]
    [InlineData("", 10, 9.99)]
    // Flat-rate methods stay flat even above the free-standard threshold.
    [InlineData("express", 100, 15.99)]
    [InlineData("overnight", 100, 19.99)]
    [InlineData("unknown", 100, 9.99)]
    public void GetShippingCost_ReturnsExpectedRate(string? method, decimal subtotal, decimal expected)
    {
        CheckoutPricing.GetShippingCost(method, subtotal).Should().Be(expected);
    }

    [Theory]
    // DEC-SHIPPING: standard shipping is free at/above the €50 threshold, else €5.99.
    [InlineData(0, 5.99)]
    [InlineData(49.99, 5.99)]
    [InlineData(50.00, 0)]    // boundary: exactly €50 ships free
    [InlineData(50.01, 0)]
    [InlineData(100, 0)]
    public void GetShippingCost_Standard_IsFreeAtOrAboveThreshold(decimal subtotal, decimal expected)
    {
        CheckoutPricing.GetShippingCost("standard", subtotal).Should().Be(expected);
    }

    [Fact]
    public void FreeShippingThreshold_IsFiftyEuros()
    {
        CheckoutPricing.FreeShippingThreshold.Should().Be(50m);
    }

    [Theory]
    // Case-insensitive matching is preserved; standard uses a sub-threshold subtotal here.
    [InlineData("EXPRESS", 10, 15.99)]
    [InlineData("Standard", 10, 5.99)]
    [InlineData("Standard", 100, 0)]   // case-insensitive + threshold together
    [InlineData("OverNight", 10, 19.99)]
    public void GetShippingCost_IsCaseInsensitive(string method, decimal subtotal, decimal expected)
    {
        CheckoutPricing.GetShippingCost(method, subtotal).Should().Be(expected);
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
    [InlineData(100, "standard", 120.00)]  // 100 + 0 (free, >= €50) + 20.00
    [InlineData(100, "express", 135.99)]   // 100 + 15.99 + 20.00
    [InlineData(100, "overnight", 139.99)] // 100 + 19.99 + 20.00
    // DEC-SHIPPING boundary at the €50 free-standard line:
    [InlineData(49.99, "standard", 65.98)] // 49.99 + 5.99 + 10.00 (49.99*0.20 = 9.998 -> 10.00)
    [InlineData(50.00, "standard", 60.00)] // 50.00 + 0 (free) + 10.00
    public void CalculateTotal_EqualsSubtotalPlusShippingPlusTax(decimal subtotal, string method, decimal expectedTotal)
    {
        var total = CheckoutPricing.CalculateTotal(subtotal, method);

        total.Should().Be(expectedTotal);
        total.Should().Be(subtotal + CheckoutPricing.GetShippingCost(method, subtotal) + CheckoutPricing.GetTax(subtotal));
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
