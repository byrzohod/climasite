using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class ProductsControllerTests : IntegrationTestBase
{
    public ProductsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_WithoutLanguageParameter_ReturnsProducts()
    {
        // Arrange - Create a test product
        var product = new Product("TEST-001", "Test AC Unit", "test-ac-unit", 999.99m);
        product.SetShortDescription("A great AC unit for testing");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test AC Unit");
    }

    [Fact]
    public async Task GetProducts_WithLanguageParameter_ReturnsProducts()
    {
        // Arrange - Create a test product with translation
        var product = new Product("TEST-002", "Test Heater", "test-heater", 499.99m);
        product.SetShortDescription("A great heater for testing");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Add translation
        var translation = new ProductTranslation(product.Id, "bg", "Тестов Нагревател");
        translation.ShortDescription = "Страхотен нагревател за тестване";
        DbContext.ProductTranslations.Add(translation);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products?lang=bg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Тестов Нагревател");
    }

    [Fact]
    public async Task GetProductBySlug_WithoutLanguageParameter_ReturnsDefaultContent()
    {
        // Arrange - Create a test product
        var product = new Product("TEST-003", "Mini Split AC", "mini-split-ac", 1299.99m);
        product.SetShortDescription("Efficient mini split");
        product.SetDescription("Full description of the mini split");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/mini-split-ac");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Mini Split AC");
        content.Should().Contain("Efficient mini split");
    }

    [Fact]
    public async Task GetProductBySlug_WithLanguageParameter_ReturnsTranslatedContent()
    {
        // Arrange - Create a test product with translation
        var product = new Product("TEST-004", "Window AC", "window-ac", 399.99m);
        product.SetShortDescription("Compact window unit");
        product.SetDescription("Full description of the window AC");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Add Bulgarian translation
        var translation = new ProductTranslation(product.Id, "bg", "Прозоречен Климатик");
        translation.ShortDescription = "Компактен прозоречен климатик";
        translation.Description = "Пълно описание на прозоречния климатик";
        DbContext.ProductTranslations.Add(translation);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/window-ac?lang=bg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Прозоречен Климатик");
        content.Should().Contain("Компактен прозоречен климатик");
    }

    [Fact]
    public async Task GetProductBySlug_WithUnsupportedLanguage_ReturnsDefaultContent()
    {
        // Arrange - Create a test product without translation for the requested language
        var product = new Product("TEST-005", "Portable AC", "portable-ac", 599.99m);
        product.SetShortDescription("Portable cooling solution");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act - Request with unsupported language
        var response = await Client.GetAsync("/api/products/portable-ac?lang=fr");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Portable AC");
        content.Should().Contain("Portable cooling solution");
    }

    [Fact]
    public async Task GetProductBySlug_WithEnglishLanguage_ReturnsDefaultContent()
    {
        // Arrange - Create a test product with Bulgarian translation
        var product = new Product("TEST-006", "Central AC", "central-ac", 2999.99m);
        product.SetShortDescription("Whole house cooling");
        product.SetActive(true);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Add Bulgarian translation
        var translation = new ProductTranslation(product.Id, "bg", "Централен Климатик");
        translation.ShortDescription = "Охлаждане на цялата къща";
        DbContext.ProductTranslations.Add(translation);
        await DbContext.SaveChangesAsync();

        // Act - Request with English language explicitly
        var response = await Client.GetAsync("/api/products/central-ac?lang=en");

        // Assert - Should return English (default) content
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Central AC");
        content.Should().Contain("Whole house cooling");
    }

    [Fact]
    public async Task GetProductBySlug_NonExistentProduct_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/products/non-existent-product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
