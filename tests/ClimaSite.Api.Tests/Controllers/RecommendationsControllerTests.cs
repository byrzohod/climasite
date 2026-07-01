#nullable enable

using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class RecommendationsControllerTests : IntegrationTestBase
{
    public RecommendationsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private static readonly HashSet<string> ValidMatchReasonKeys =
    [
        "homeV3.matchReason.perfectFit",
        "homeV3.matchReason.efficient",
        "homeV3.matchReason.powerful",
        "homeV3.matchReason.fallback"
    ];

    private static async Task<List<RecommendedProductDto>> ReadRecommendationsAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<List<RecommendedProductDto>>();
        content.Should().NotBeNull();
        return content!;
    }

    private static Product CreateRecommendationProduct(
        string sku,
        string name,
        string slug,
        int btu,
        bool isInverter,
        int minTemp,
        List<string> recommendedRoomTypes,
        bool isFeatured = false)
    {
        var product = new Product(sku, name, slug, 999m);
        product.SetFeatured(isFeatured);
        product.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", btu },
            { "isInverter", isInverter },
            { "minTemp", minTemp },
            { "recommendedRoomTypes", recommendedRoomTypes },
            { "noiseLevel", isInverter ? 22 : 34 }
        });

        var variant = new ProductVariant(product.Id, $"{sku}-VAR", "Standard");
        variant.SetStockQuantity(10);
        product.Variants.Add(variant);

        return product;
    }

    [Fact]
    public async Task GetRecommendations_WithValidQuery_ReturnsTop3Products()
    {
        // Arrange: Create test products with specifications
        var livingRoomProduct = new Product("HVAC-001", "Perfect Living Room AC", "perfect-living-ac", 999m);
        livingRoomProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 6000 },  // Perfect for 24 m² at 250 BTU/m²
            { "isInverter", true },
            { "minTemp", -5 },
            { "recommendedRoomTypes", new List<string> { "living", "bedroom" } },
            { "noiseLevel", 22 }
        });
        var variant1 = new ProductVariant(livingRoomProduct.Id, "VAR-001", "Standard");
        variant1.SetStockQuantity(10);
        livingRoomProduct.Variants.Add(variant1);

        var economyProduct = new Product("HVAC-002", "Economy Living AC", "economy-living-ac", 599m);
        economyProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 5000 },  // Slightly under 6000 (non-inverter → ranks below the perfect fit)
            { "isInverter", false },
            { "minTemp", 0 },
            { "recommendedRoomTypes", new List<string> { "living" } },
            { "noiseLevel", 28 }
        });
        var variant2 = new ProductVariant(economyProduct.Id, "VAR-002", "Standard");
        variant2.SetStockQuantity(5);
        economyProduct.Variants.Add(variant2);

        var coldZoneProduct = new Product("HVAC-003", "Cold Climate AC", "cold-climate-ac", 1499m);
        coldZoneProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 4200 },
            { "isInverter", true },
            { "minTemp", -20 },
            { "recommendedRoomTypes", new List<string> { "living" } },
            { "noiseLevel", 24 }
        });
        var variant3 = new ProductVariant(coldZoneProduct.Id, "VAR-003", "Standard");
        variant3.SetStockQuantity(8);
        coldZoneProduct.Variants.Add(variant3);

        // Add a product out of stock (should be excluded)
        var outOfStockProduct = new Product("HVAC-004", "Out of Stock AC", "out-of-stock-ac", 799m);
        outOfStockProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "isInverter", true }
        });
        var variant4 = new ProductVariant(outOfStockProduct.Id, "VAR-004", "Standard");
        variant4.SetStockQuantity(0);
        outOfStockProduct.Variants.Add(variant4);

        DbContext.Products.Add(livingRoomProduct);
        DbContext.Products.Add(economyProduct);
        DbContext.Products.Add(coldZoneProduct);
        DbContext.Products.Add(outOfStockProduct);
        await DbContext.SaveChangesAsync();

        // Act: Get recommendations for 24 m² living room in Zone B
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        content.Should().HaveCount(3);
        content.Should().AllSatisfy(p => p.BtuCapacity.Should().BeGreaterThan(0));
        content.Should().AllSatisfy(p => p.Score.Should().BeGreaterThan(0));
        content.Should().AllSatisfy(p => ValidMatchReasonKeys.Should().Contain(p.MatchReason));

        // First result should be the perfect fit
        content[0].Name.Should().Contain("Perfect");
        content[0].Score.Should().BeGreaterThan(content[1].Score);

        // Out of stock should not appear
        content.Should().NotContain(p => p.Name.Contains("Out of Stock"));
    }

    [Fact]
    public async Task GetRecommendations_WithLargeCatalog_ScoresEveryEligibleProductBeforeTakingTopResults()
    {
        // Arrange: the previous implementation took the first 250 featured products before scoring.
        var distractors = Enumerable.Range(0, 260)
            .Select(i => CreateRecommendationProduct(
                sku: $"REC-BAD-{i:D3}",
                name: $"Featured Distractor AC {i:D3}",
                slug: $"featured-distractor-ac-{i:D3}",
                btu: 12000,
                isInverter: false,
                minTemp: 10,
                recommendedRoomTypes: ["office"],
                isFeatured: true))
            .ToList();

        var perfectMatch = CreateRecommendationProduct(
            sku: "REC-PERFECT-OLDER",
            name: "Older Perfect Fit AC",
            slug: "older-perfect-fit-ac",
            btu: 6000, // perfect fit for 24 m² at 250 BTU/m²
            isInverter: true,
            minTemp: -15,
            recommendedRoomTypes: ["living"],
            isFeatured: false);

        DbContext.Products.AddRange(distractors);
        DbContext.Products.Add(perfectMatch);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        content.Should().NotBeEmpty();
        content[0].Name.Should().Be("Older Perfect Fit AC");
        content.Should().ContainSingle(p => p.Name == "Older Perfect Fit AC");
    }

    [Fact]
    public async Task GetRecommendations_ZoneCColdCapable_PrioritizesColdCapable()
    {
        // Arrange: Compare cold-capable vs non-cold-capable in Zone C
        var coldCapable = new Product("HVAC-C-001", "Zone C Cold AC", "zone-c-cold-ac", 1299m);
        coldCapable.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 10560 },  // 33 m² × 320 BTU/m² (Zone C)
            { "isInverter", false },
            { "minTemp", -20 },
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var var1 = new ProductVariant(coldCapable.Id, "VAR-1", "Std");
        var1.SetStockQuantity(10);
        coldCapable.Variants.Add(var1);

        var notColdCapable = new Product("HVAC-C-002", "Zone C Warm AC", "zone-c-warm-ac", 899m);
        notColdCapable.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 10560 },
            { "isInverter", false },
            { "minTemp", 5 },  // Not suitable for Zone C
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var var2 = new ProductVariant(notColdCapable.Id, "VAR-2", "Std");
        var2.SetStockQuantity(10);
        notColdCapable.Variants.Add(var2);

        DbContext.Products.Add(coldCapable);
        DbContext.Products.Add(notColdCapable);
        await DbContext.SaveChangesAsync();

        // Act: Get recommendations for Zone C
        var response = await Client.GetAsync("/api/products/recommendations?area=33&type=living&zone=C");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        // Cold-capable should rank higher in Zone C
        var coldCapableResult = content.FirstOrDefault(p => p.Name.Contains("Cold"));
        var warmResult = content.FirstOrDefault(p => p.Name.Contains("Warm"));

        if (coldCapableResult != null && warmResult != null)
        {
            coldCapableResult.Score.Should().BeGreaterThan(warmResult.Score);
        }
    }

    [Fact]
    public async Task GetRecommendations_InverterBonus_IncludesInScoring()
    {
        // Arrange: Two products with same BTU, one with inverter
        var inverterProduct = new Product("HVAC-INV-001", "Inverter AC", "inverter-ac", 1199m);
        inverterProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "isInverter", true },
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var var1 = new ProductVariant(inverterProduct.Id, "VAR-1", "Std");
        var1.SetStockQuantity(10);
        inverterProduct.Variants.Add(var1);

        var standardProduct = new Product("HVAC-STD-001", "Standard AC", "standard-ac", 799m);
        standardProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "isInverter", false },
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var var2 = new ProductVariant(standardProduct.Id, "VAR-2", "Std");
        var2.SetStockQuantity(10);
        standardProduct.Variants.Add(var2);

        DbContext.Products.Add(inverterProduct);
        DbContext.Products.Add(standardProduct);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        var inverter = content.FirstOrDefault(p => p.IsInverter);
        var standard = content.FirstOrDefault(p => !p.IsInverter);

        if (inverter != null && standard != null)
        {
            inverter.Score.Should().BeGreaterThan(standard.Score);
        }
    }

    [Fact]
    public async Task GetRecommendations_RoomTypeMatchBonus_AffectsScoring()
    {
        // Arrange: Same BTU, one matches room type
        var matchingProduct = new Product("HVAC-MATCH-001", "Living Specific", "living-specific-ac", 999m);
        matchingProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "isInverter", false },
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var var1 = new ProductVariant(matchingProduct.Id, "VAR-1", "Std");
        var1.SetStockQuantity(10);
        matchingProduct.Variants.Add(var1);

        var nonMatchingProduct = new Product("HVAC-NOMATCH-001", "Office Only", "office-only-ac", 999m);
        nonMatchingProduct.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "isInverter", false },
            { "recommendedRoomTypes", new List<string> { "office" } }
        });
        var var2 = new ProductVariant(nonMatchingProduct.Id, "VAR-2", "Std");
        var2.SetStockQuantity(10);
        nonMatchingProduct.Variants.Add(var2);

        DbContext.Products.Add(matchingProduct);
        DbContext.Products.Add(nonMatchingProduct);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        var matching = content.FirstOrDefault(p => p.Name.Contains("Living"));
        var nonMatching = content.FirstOrDefault(p => p.Name.Contains("Office"));

        if (matching != null && nonMatching != null)
        {
            matching.Score.Should().BeGreaterThan(nonMatching.Score);
        }
    }

    [Fact]
    public async Task GetRecommendations_ReturnedTop3InOrder_SortedByScore()
    {
        // Arrange: Create 5 products with varying scores
        for (int i = 1; i <= 5; i++)
        {
            var product = new Product($"HVAC-{i:D2}", $"Product {i}", $"product-{i}", 900m + (i * 100m));
            product.SetSpecifications(new Dictionary<string, object>
            {
                { "btu", 2500 + (i * 50) },  // Varying BTU
                { "isInverter", i % 2 == 0 },  // Some with inverter
                { "recommendedRoomTypes", new List<string> { "living" } }
            });
            var variant = new ProductVariant(product.Id, $"VAR-{i}", "Standard");
            variant.SetStockQuantity(10);
            product.Variants.Add(variant);
            DbContext.Products.Add(product);
        }
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);

        content.Should().HaveCount(3);
        content.Should().BeInDescendingOrder(p => p.Score);
    }

    [Fact]
    public async Task GetRecommendations_InvalidArea_ReturnsBadRequest()
    {
        // Act: Invalid area (too low)
        var response = await Client.GetAsync("/api/products/recommendations?area=2&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRecommendations_InvalidRoomType_ReturnsBadRequest()
    {
        // Act: Invalid room type
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=invalid&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRecommendations_InvalidZone_ReturnsBadRequest()
    {
        // Act: Invalid zone
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=X");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRecommendations_NoMatchingProducts_ReturnsEmptyList()
    {
        // Arrange: Create a product that's completely out of stock
        var product = new Product("HVAC-NONE", "No Stock", "no-stock-ac", 999m);
        product.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 }
        });
        var variant = new ProductVariant(product.Id, "VAR-1", "Std");
        variant.SetStockQuantity(0);
        product.Variants.Add(variant);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendations_WithLanguageParameter_IncludesTranslations()
    {
        // Arrange: Create product with Bulgarian translation
        var product = new Product("HVAC-BG-001", "English Product", "english-product", 999m);
        product.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 2640 },
            { "recommendedRoomTypes", new List<string> { "living" } }
        });
        var variant = new ProductVariant(product.Id, "VAR-1", "Std");
        variant.SetStockQuantity(10);
        product.Variants.Add(variant);

        var translation = new ProductTranslation(product.Id, "bg", "Български Продукт");
        translation.ShortDescription = "Описание на продукта";

        DbContext.Products.Add(product);
        DbContext.ProductTranslations.Add(translation);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B&lang=bg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadRecommendationsAsync(response);
        content.Should().NotBeEmpty();
        content[0].Name.Should().Contain("Български");
    }
}
