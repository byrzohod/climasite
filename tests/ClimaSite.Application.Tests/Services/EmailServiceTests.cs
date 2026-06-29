using ClimaSite.Infrastructure.Services;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Services;

public class EmailServiceTests
{
    [Fact]
    public void BuildOrderUrl_ProducesTheAccountOrdersPath_WithNoStrayDollarSign()
    {
        // B-007: the interpolation once carried a literal `$` → `/account/orders/$<guid>` (a 404 CTA).
        var orderId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var url = EmailService.BuildOrderUrl("https://shop.example", orderId);

        url.Should().Be("https://shop.example/account/orders/11111111-2222-3333-4444-555555555555");
        url.Should().NotContain("$");
        url.Should().NotContain("/account/orders/$");
    }

    [Theory]
    [InlineData("https://climasite.local")]
    [InlineData("https://www.shop.bg")]
    public void BuildOrderUrl_AppendsTheOrderIdDirectlyAfterTheOrdersSegment(string baseUrl)
    {
        var orderId = Guid.NewGuid();

        var url = EmailService.BuildOrderUrl(baseUrl, orderId);

        url.Should().Be($"{baseUrl}/account/orders/{orderId}");
    }
}
