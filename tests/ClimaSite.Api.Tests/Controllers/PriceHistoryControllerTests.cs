using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class PriceHistoryControllerTests : IntegrationTestBase
{
    public PriceHistoryControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Product> SeedProductAsync(string sku, string name, string slug, decimal price)
    {
        var product = new Product(sku, name, slug, price);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return product;
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsCurrentPricePoint_WhenNoHistoryRecorded()
    {
        // Arrange
        var product = await SeedProductAsync("PH-001", "Price History Unit", "price-history-unit", 1299.99m);

        // Act
        var response = await Client.GetAsync($"/api/price-history/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Price History Unit");
        content.Should().Contain("1299.99");
        content.Should().Contain("Current");
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsRecordedPricePoints_WhenHistoryExists()
    {
        // Arrange
        var product = await SeedProductAsync("PH-002", "Tracked Unit", "tracked-unit", 800m);

        var initial = ProductPriceHistory.Create(
            product.Id, 1000m, null, PriceChangeReason.Initial, "Initial price");
        var drop = ProductPriceHistory.Create(
            product.Id, 800m, null, PriceChangeReason.PriceChange, "Price drop");

        DbContext.ProductPriceHistory.AddRange(initial, drop);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/price-history/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tracked Unit");
        // Lowest of the recorded points
        content.Should().Contain("\"lowestPrice\":800");
        // Highest of the recorded points
        content.Should().Contain("\"highestPrice\":1000");
    }

    [Fact]
    public async Task GetPriceHistory_RespectsDaysBackWindow()
    {
        // Arrange
        var product = await SeedProductAsync("PH-003", "Window Unit", "window-window", 500m);

        // Recent point (within the 7-day window)
        DbContext.ProductPriceHistory.Add(
            ProductPriceHistory.Create(product.Id, 500m, null, PriceChangeReason.PriceChange, "Recent"));
        await DbContext.SaveChangesAsync();

        // Act - small window keeps only the recent point and the current product price
        var response = await Client.GetAsync($"/api/price-history/{product.Id}?daysBack=7");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Window Unit");
        content.Should().Contain("500");
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Act
        var response = await Client.GetAsync($"/api/price-history/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsNotFound_WhenProductIdIsNotAGuid()
    {
        // Act - the {productId:guid} route constraint rejects non-GUID segments,
        // so the request never matches the action and falls through to 404.
        var response = await Client.GetAsync("/api/price-history/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
