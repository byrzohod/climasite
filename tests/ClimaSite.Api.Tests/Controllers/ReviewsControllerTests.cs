using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Regression coverage for the "reviews appear immediately" behaviour. Reviews auto-approve on
/// creation, and the reviews read endpoints opt out of the 5-minute base output-cache policy
/// ([OutputCache(NoStore = true)] on ReviewsController). Without that opt-out, a GET issued before
/// the POST caches an empty list and the post-submit GET returns the stale (empty) response, so a
/// user never sees the review they just left. This test pins both halves of that contract.
/// </summary>
public class ReviewsControllerTests : IntegrationTestBase
{
    public ReviewsControllerTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreatedReview_AppearsImmediately_EvenWhenListWasFetchedFirst()
    {
        // Arrange — a real, active product to review.
        var product = await SeedProductAsync();
        var url = $"/api/reviews/product/{product.Id}";

        // Prime the output cache with the empty list (this is what made the stale-cache bug visible).
        var before = await Client.GetAsync(url);
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTotalCountAsync(before)).Should().Be(0, "the product has no reviews yet");

        await AuthenticateAsync("review-immediate@test.com");

        // Act — submit a review.
        var post = await Client.PostAsJsonAsync("/api/reviews", new
        {
            productId = product.Id,
            rating = 5,
            title = "Appears immediately",
            content = "This review must be visible right after submission, not after the cache expires."
        });

        // Assert — created, then visible on the very next read (no stale cached empty list).
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        ClearAuthToken();
        var after = await Client.GetAsync(url);
        after.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("totalCount").GetInt32().Should().Be(1,
            "an auto-approved review must appear immediately, not be hidden by a stale output cache");
        var titles = root.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("title").GetString())
            .ToList();
        titles.Should().Contain("Appears immediately");
    }

    private static async Task<int> ReadTotalCountAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("totalCount").GetInt32();
    }

    private async Task<Product> SeedProductAsync()
    {
        var sku = $"REV-{Guid.NewGuid():N}".Substring(0, 20);
        var product = new Product(sku, $"Review Product {sku}", $"review-product-{sku}", 199.99m);
        product.SetShortDescription("A product used to verify review immediacy.");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return product;
    }
}
