using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class PromotionsControllerTests : IntegrationTestBase
{
    public PromotionsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // Active/featured list endpoints are output-cached per URL and MediatR-cached per query key,
    // so requests sharing the same URL leak data across tests. A unique query param per request
    // produces a distinct cache key at both layers, guaranteeing each test sees its own data.
    private static string Cb() => $"cb={Guid.NewGuid()}";

    private async Task<Promotion> SeedPromotionAsync(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = false,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int sortOrder = 0)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow.AddDays(7);

        var promotion = new Promotion(name, slug, PromotionType.Percentage, 15m, start, end);
        promotion.SetActive(isActive);
        promotion.SetFeatured(isFeatured);
        promotion.SetSortOrder(sortOrder);

        DbContext.Promotions.Add(promotion);
        await DbContext.SaveChangesAsync();
        return promotion;
    }

    [Fact]
    public async Task GetActivePromotions_ReturnsCurrentlyRunningPromotions()
    {
        // Arrange
        await SeedPromotionAsync("Summer Sale", "summer-sale");

        // Act - unique cache-buster query busts the per-URL output cache shared across tests
        var response = await Client.GetAsync($"/api/promotions?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Summer Sale");
    }

    [Fact]
    public async Task GetActivePromotions_ExcludesExpiredPromotions()
    {
        // Arrange
        await SeedPromotionAsync("Live Promo", "live-promo");
        await SeedPromotionAsync(
            "Expired Promo",
            "expired-promo",
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(-10));

        // Act
        var response = await Client.GetAsync($"/api/promotions?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Live Promo");
        content.Should().NotContain("Expired Promo");
    }

    [Fact]
    public async Task GetActivePromotions_ExcludesInactivePromotions()
    {
        // Arrange
        await SeedPromotionAsync("Active Promo", "active-promo", isActive: true);
        await SeedPromotionAsync("Disabled Promo", "disabled-promo", isActive: false);

        // Act
        var response = await Client.GetAsync($"/api/promotions?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Active Promo");
        content.Should().NotContain("Disabled Promo");
    }

    [Fact]
    public async Task GetActivePromotions_ReturnsEmptyList_WhenNonePresent()
    {
        // Act
        var response = await Client.GetAsync($"/api/promotions?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"totalCount\":0");
    }

    [Fact]
    public async Task GetFeaturedPromotions_ReturnsFeaturedPromotions()
    {
        // Arrange
        await SeedPromotionAsync("Featured Deal", "featured-deal", isFeatured: true);
        await SeedPromotionAsync("Regular Deal", "regular-deal", isFeatured: false);

        // Act - literal "featured" route must take priority over the {slug} route
        var response = await Client.GetAsync($"/api/promotions/featured?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Featured Deal");
        content.Should().NotContain("Regular Deal");
    }

    [Fact]
    public async Task GetPromotionBySlug_ReturnsPromotion_WhenSlugExists()
    {
        // Arrange
        await SeedPromotionAsync("Black Friday", "black-friday");

        // Act
        var response = await Client.GetAsync("/api/promotions/black-friday");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Black Friday");
    }

    [Fact]
    public async Task GetPromotionBySlug_ReturnsNotFound_WhenSlugDoesNotExist()
    {
        // Act
        var response = await Client.GetAsync("/api/promotions/no-such-promotion");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Promotion not found");
    }

    [Fact]
    public async Task GetPromotionBySlug_ReturnsNotFound_WhenPromotionIsExpired()
    {
        // Arrange - active flag set but window is in the past
        await SeedPromotionAsync(
            "Past Window",
            "past-window",
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(-10));

        // Act
        var response = await Client.GetAsync("/api/promotions/past-window");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
