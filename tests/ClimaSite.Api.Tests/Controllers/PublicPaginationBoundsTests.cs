using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// B-036 (council round 2): every anonymous public list/count endpoint must clamp untrusted pagination/count
/// params at the edge, so out-of-bounds values can never produce a 5xx (div-by-zero on pageSize=0, negative-Skip
/// overflow on a huge pageNumber, or a fetch-all DoS). Asserts status &lt; 500 — robust to whether the target
/// resource exists (a non-existent product/brand may 404, which is fine; a 500 is the bug).
/// </summary>
public class PublicPaginationBoundsTests : IntegrationTestBase
{
    public PublicPaginationBoundsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("/api/promotions?pageSize=0")]
    [InlineData("/api/promotions?pageNumber=2147483647")]
    [InlineData("/api/promotions/featured?count=100000")]
    [InlineData("/api/brands?pageSize=0")]
    [InlineData("/api/brands?pageNumber=2147483647")]
    [InlineData("/api/brands/featured?limit=100000")]
    [InlineData("/api/brands/featured?limit=-1")]
    public async Task AnonymousListEndpoints_WithOutOfBoundsParams_NeverServerError(string url)
    {
        var response = await Client.GetAsync(url);

        ((int)response.StatusCode).Should().BeLessThan(500);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageNumber=2147483647")]
    [InlineData("pageSize=100000")]
    public async Task ProductScopedEndpoints_WithOutOfBoundsParams_NeverServerError(string qs)
    {
        var productId = Guid.NewGuid(); // need not exist — a 404/200 is fine, a 500 is the bug

        var reviews = await Client.GetAsync($"/api/reviews/product/{productId}?{qs}");
        var questions = await Client.GetAsync($"/api/questions/product/{productId}?{qs}");

        ((int)reviews.StatusCode).Should().BeLessThan(500);
        ((int)questions.StatusCode).Should().BeLessThan(500);
    }

    [Theory]
    [InlineData("daysBack=0")]
    [InlineData("daysBack=-5")]
    [InlineData("daysBack=2147483647")] // huge → would overflow DateTime.AddDays without the clamp
    public async Task PriceHistory_WithOutOfBoundsDaysBack_NeverServerError(string qs)
    {
        var response = await Client.GetAsync($"/api/price-history/{Guid.NewGuid()}?{qs}");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}
