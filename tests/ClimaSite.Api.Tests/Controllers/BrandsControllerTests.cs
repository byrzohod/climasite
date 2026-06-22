using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class BrandsControllerTests : IntegrationTestBase
{
    public BrandsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // List/featured endpoints are output-cached per URL and MediatR-cached per query key, so
    // requests sharing the same URL leak data across tests. A unique query param per request
    // produces a distinct cache key at both layers, guaranteeing each test sees its own data.
    private static string Cb() => $"cb={Guid.NewGuid()}";

    private async Task<Brand> SeedBrandAsync(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = false,
        int sortOrder = 0)
    {
        var brand = new Brand(name, slug);
        brand.SetActive(isActive);
        brand.SetFeatured(isFeatured);
        brand.SetSortOrder(sortOrder);

        DbContext.Brands.Add(brand);
        await DbContext.SaveChangesAsync();
        return brand;
    }

    [Fact]
    public async Task GetBrands_ReturnsActiveBrands()
    {
        // Arrange
        await SeedBrandAsync("Daikin", "daikin");
        await SeedBrandAsync("Mitsubishi Electric", "mitsubishi-electric");

        // Act
        var response = await Client.GetAsync($"/api/brands?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Daikin");
        content.Should().Contain("Mitsubishi Electric");
    }

    [Fact]
    public async Task GetBrands_ExcludesInactiveBrands()
    {
        // Arrange
        await SeedBrandAsync("ActiveBrand", "active-brand", isActive: true);
        await SeedBrandAsync("HiddenBrand", "hidden-brand", isActive: false);

        // Act
        var response = await Client.GetAsync($"/api/brands?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ActiveBrand");
        content.Should().NotContain("HiddenBrand");
    }

    [Fact]
    public async Task GetBrands_ReturnsEmptyList_WhenNoBrandsExist()
    {
        // Act
        var response = await Client.GetAsync($"/api/brands?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"totalCount\":0");
    }

    [Fact]
    public async Task GetBrands_WithFeaturedFilter_ReturnsOnlyFeaturedBrands()
    {
        // Arrange
        await SeedBrandAsync("FeaturedBrand", "featured-brand", isFeatured: true);
        await SeedBrandAsync("RegularBrand", "regular-brand", isFeatured: false);

        // Act
        var response = await Client.GetAsync($"/api/brands?featured=true&{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("FeaturedBrand");
        content.Should().NotContain("RegularBrand");
    }

    [Fact]
    public async Task GetFeaturedBrands_ReturnsFeaturedBrands()
    {
        // Arrange
        await SeedBrandAsync("Toshiba", "toshiba", isFeatured: true);
        await SeedBrandAsync("NotFeatured", "not-featured", isFeatured: false);

        // Act - literal "featured" route must take priority over the {slug} route
        var response = await Client.GetAsync($"/api/brands/featured?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Toshiba");
        content.Should().NotContain("NotFeatured");
    }

    [Fact]
    public async Task GetBrandBySlug_ReturnsBrand_WhenSlugExists()
    {
        // Arrange
        await SeedBrandAsync("Panasonic", "panasonic");

        // Act
        var response = await Client.GetAsync("/api/brands/panasonic");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Panasonic");
    }

    [Fact]
    public async Task GetBrandBySlug_ReturnsNotFound_WhenSlugDoesNotExist()
    {
        // Act
        var response = await Client.GetAsync("/api/brands/does-not-exist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Brand not found");
    }

    [Fact]
    public async Task GetBrandBySlug_ReturnsTranslatedName_WhenTranslationExists()
    {
        // Arrange
        var brand = await SeedBrandAsync("Original Brand", "translated-brand");
        DbContext.BrandTranslations.Add(new BrandTranslation
        {
            Id = Guid.NewGuid(),
            BrandId = brand.Id,
            LanguageCode = "bg",
            Name = "Преведена Марка"
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/brands/translated-brand?lang=bg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Преведена Марка");
    }
}
