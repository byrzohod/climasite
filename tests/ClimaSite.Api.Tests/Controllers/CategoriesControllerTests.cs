using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class CategoriesControllerTests : IntegrationTestBase
{
    public CategoriesControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // The category-tree endpoint is output-cached per URL and MediatR-cached per query key, so
    // requests sharing the same URL leak data across tests. A unique query param per request
    // produces a distinct cache key at both layers, guaranteeing each test sees its own data.
    private static string Cb() => $"cb={Guid.NewGuid()}";

    private async Task<Category> SeedCategoryAsync(
        string name,
        string slug,
        bool isActive = true,
        Guid? parentId = null,
        int sortOrder = 0)
    {
        var category = new Category(name, slug);
        category.SetActive(isActive);
        category.SetSortOrder(sortOrder);
        if (parentId.HasValue)
        {
            category.SetParent(parentId.Value);
        }

        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task GetCategoryTree_ReturnsRootCategories()
    {
        // Arrange
        await SeedCategoryAsync("Air Conditioners", "air-conditioners");
        await SeedCategoryAsync("Heating", "heating");

        // Act
        var response = await Client.GetAsync($"/api/categories?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Air Conditioners");
        content.Should().Contain("Heating");
    }

    [Fact]
    public async Task GetCategoryTree_NestsChildrenUnderParent()
    {
        // Arrange
        var parent = await SeedCategoryAsync("Cooling", "cooling");
        await SeedCategoryAsync("Split Systems", "split-systems", parentId: parent.Id);

        // Act
        var response = await Client.GetAsync($"/api/categories?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cooling");
        content.Should().Contain("Split Systems");
        // The child should be nested under the parent in the children array
        content.Should().Contain("\"children\"");
    }

    [Fact]
    public async Task GetCategoryTree_ExcludesInactiveCategories()
    {
        // Arrange
        await SeedCategoryAsync("VisibleCategory", "visible-category", isActive: true);
        await SeedCategoryAsync("HiddenCategory", "hidden-category", isActive: false);

        // Act
        var response = await Client.GetAsync($"/api/categories?{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("VisibleCategory");
        content.Should().NotContain("HiddenCategory");
    }

    [Fact]
    public async Task GetCategoryTree_WithNameFilter_ReturnsMatchingCategories()
    {
        // Arrange
        await SeedCategoryAsync("Heat Pumps", "heat-pumps");
        await SeedCategoryAsync("Ventilation", "ventilation");

        // Act
        var response = await Client.GetAsync($"/api/categories?name=Heat&{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Heat Pumps");
        content.Should().NotContain("Ventilation");
    }

    [Fact]
    public async Task GetCategoryBySlug_ReturnsCategory_WhenSlugExists()
    {
        // Arrange
        await SeedCategoryAsync("Thermostats", "thermostats");

        // Act
        var response = await Client.GetAsync("/api/categories/thermostats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Thermostats");
    }

    [Fact]
    public async Task GetCategoryBySlug_ReturnsNotFound_WhenSlugDoesNotExist()
    {
        // Act
        var response = await Client.GetAsync("/api/categories/no-such-category");

        // Assert - the slug query throws NotFoundException, which the middleware maps to a 404
        // whose body reports the missing entity (the controller's null-branch is unreachable here).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category");
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task GetCategoryTree_ReturnsTranslatedName_WhenTranslationExists()
    {
        // Arrange
        var category = await SeedCategoryAsync("Original Category", "translated-category");
        DbContext.CategoryTranslations.Add(new CategoryTranslation
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            LanguageCode = "de",
            Name = "Uebersetzte Kategorie"
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/categories?lang=de&{Cb()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Uebersetzte Kategorie");
    }
}
