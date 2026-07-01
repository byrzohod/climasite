#nullable enable

using ClimaSite.Application.Features.Products.Scoring;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Application.Tests.Features.Products.Scoring;

/// <summary>
/// The exact B-016 regression: a product built with the DISPLAY keys the seeder actually writes
/// ("BTU" not "btu", "Noise Level" as a string) must score a meaningful, size-discriminating fit — NOT the
/// flat fallback it collapsed to before the alias-aware resolver. The pre-existing scoring tests seed canonical
/// keys, which masked this bug.
/// </summary>
public class RecommendationDisplayKeyScoringTests
{
    private readonly RecommendationScoringService _service = new();

    private static Product ProductWithSpecs(Dictionary<string, object> specs, decimal basePrice = 899.99m)
    {
        var product = new Product("DZP-12000", "DualZone Pro 12000", "dualzone-pro-12000", basePrice);
        product.SetSpecifications(specs);
        var variant = new ProductVariant(product.Id, "DZP-12000-DEFAULT", "Standard");
        variant.SetStockQuantity(20);
        product.Variants.Add(variant);
        return product;
    }

    /// <summary>DualZone Pro shape from DataSeeder: display keys + the added canonical machine fields.</summary>
    private static Product SeederShapedDualZone() => ProductWithSpecs(new Dictionary<string, object>
    {
        { "BTU", 12000 },
        { "SEER Rating", 21 },
        { "Room Size", "Up to 550 sq ft" },
        { "Noise Level", "22 dB" },
        { "Refrigerant", "R-32" },
        { "isInverter", true },
        { "minTemp", -15 },
        { "recommendedRoomTypes", new[] { "living", "bedroom", "office" } }
    });

    [Fact]
    public void ScoreProduct_WithSeederDisplayKeys_ScoresNearPerfectForFittingRoom()
    {
        var product = SeederShapedDualZone();

        // 48 m² Zone B → required 48 × 250 = 12000 BTU → this 12000 BTU inverter unit is a near-perfect fit.
        var score = _service.ScoreProduct(product, areaM2: 48, climateZone: 'B', roomType: "living");

        score.Should().NotBeNull();
        score!.Value.Should().BeGreaterThan(0.9, "btu (from the \"BTU\" display key) + inverter + zone + room + stock + price all fire");
    }

    [Fact]
    public void ScoreProduct_WithSeederDisplayKeys_IsSizeDiscriminating_NotFlatFallback()
    {
        var product = SeederShapedDualZone();

        var fit = _service.ScoreProduct(product, areaM2: 48, climateZone: 'B', roomType: "living");         // req 12000 → perfect
        var wrongSize = _service.ScoreProduct(product, areaM2: 12, climateZone: 'B', roomType: "living");   // req 3000 → 4× over → btu fit 0

        fit.Should().NotBeNull();
        wrongSize.Should().NotBeNull();
        // Before B-016 the "BTU" display key was invisible → both queries returned the SAME flat fallback.
        // A materially lower score for the wrong room size proves the BTU is actually being read.
        (fit!.Value - wrongSize!.Value).Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void ScoreProduct_HeatPumpDisplayKeys_UsesCoolingBtu()
    {
        // EcoHeat shape: "BTU Cooling"/"BTU Heating" (no plain "BTU") → resolver must read the cooling capacity.
        var product = ProductWithSpecs(new Dictionary<string, object>
        {
            { "BTU Cooling", 24000 },
            { "BTU Heating", 26000 },
            { "SEER Rating", 22 },
            { "isInverter", true },
            { "minTemp", -26 },
            { "recommendedRoomTypes", new[] { "living", "commercial" } }
        }, basePrice: 2499.99m);

        // 96 m² Zone B → required 24000 BTU → cooling BTU is the near-perfect fit.
        var score = _service.ScoreProduct(product, areaM2: 96, climateZone: 'B', roomType: "living");

        score.Should().NotBeNull();
        score!.Value.Should().BeGreaterThan(0.9);
    }
}
