#nullable enable

using ClimaSite.Application.Features.Products.Scoring;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Application.Tests.Features.Products.Scoring;

public class RecommendationScoringServiceTests
{
    private readonly RecommendationScoringService _service = new();

    #region Helper Methods

    private Product CreateProduct(
        string name = "Test Product",
        decimal basePrice = 999m,
        int btu = 0,
        bool isInverter = false,
        int minTemp = 0,
        List<string>? recommendedRoomTypes = null,
        int noiseLevel = 0,
        int stockQuantity = 10)
    {
        var product = new Product("TEST-001", name, "test-product", basePrice);

        var specs = new Dictionary<string, object>();
        if (btu > 0)
            specs["btu"] = btu;
        if (isInverter)
            specs["isInverter"] = true;
        if (minTemp < 0)
            specs["minTemp"] = minTemp;
        if (recommendedRoomTypes != null && recommendedRoomTypes.Count > 0)
            specs["recommendedRoomTypes"] = recommendedRoomTypes;
        if (noiseLevel > 0)
            specs["noiseLevel"] = noiseLevel;

        if (specs.Count > 0)
            product.SetSpecifications(specs);

        // Add a variant with stock
        var variant = new ProductVariant(product.Id, "VAR-001", "Standard");
        variant.SetStockQuantity(stockQuantity);
        product.Variants.Add(variant);

        return product;
    }

    #endregion

    #region BTU Fit Score Tests

    [Fact]
    public void ScoreProduct_WithPerfectBtuFit_ReturnsHighScore()
    {
        // Arrange: 24 m² room, Zone B (110 BTU/m²) = 2640 BTU required
        // Product has 2640 BTU (perfect fit, within ±10%)
        var product = CreateProduct(btu: 2640);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4); // At least 40% due to BTU weight (0.40)
    }

    [Fact]
    public void ScoreProduct_WithBtuUnder10Percent_ReturnsHighScore()
    {
        // Arrange: 24 m² room, Zone B = 2640 BTU required
        // Product has 2376 BTU (90% of required, within ±10%)
        var product = CreateProduct(btu: 2376);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.35);
    }

    [Fact]
    public void ScoreProduct_WithBtuOver10Percent_ReturnsHighScore()
    {
        // Arrange: 24 m² room, Zone B = 2640 BTU required
        // Product has 2904 BTU (110% of required, within ±10%)
        var product = CreateProduct(btu: 2904);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.35);
    }

    [Fact]
    public void ScoreProduct_WithBtuUnder_LinearFalloff()
    {
        // Arrange: 24 m² room, Zone B = 2640 BTU required
        // Product has 1980 BTU (75% of required, falls off from 0.9 to 0.5)
        var product = CreateProduct(btu: 1980);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        var perfectProduct = CreateProduct(btu: 2640);
        var perfectScore = _service.ScoreProduct(perfectProduct, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Between falloff range (0.5 to 0.9), should get partial BTU score
        score.Should().NotBeNull();
        perfectScore.Should().NotBeNull();
        score.Should().BeGreaterThan(0.0);
        score.Should().BeLessThan(perfectScore!.Value);
    }

    [Fact]
    public void ScoreProduct_WithBtuWayUnder_ReturnsZeroBtuScore()
    {
        // Arrange: 24 m² room, Zone B = 2640 BTU required
        var product = CreateProduct(btu: 1320);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        var perfectProduct = CreateProduct(btu: 2640);
        var perfectScore = _service.ScoreProduct(perfectProduct, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: At 50% (falloff boundary), BTU component should be zero while other factors still contribute
        score.Should().NotBeNull();
        perfectScore.Should().NotBeNull();
        (perfectScore!.Value - score!.Value).Should().BeApproximately(0.4, 0.01);
    }

    [Fact]
    public void ScoreProduct_WithBtuOver_LinearFalloff()
    {
        // Arrange: 24 m² room, Zone B = 2640 BTU required
        // Product has 3960 BTU (150% of required, falls off from 1.1 to 1.5)
        var product = CreateProduct(btu: 3960);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Between falloff range, should get partial score
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.0);
        score.Should().BeLessThan(0.5);
    }

    [Fact]
    public void ScoreProduct_WithNoBtu_ReturnsZeroScore()
    {
        // Arrange: Product with no BTU specification
        var product = CreateProduct(btu: 0);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        var perfectProduct = CreateProduct(btu: 2640);
        var perfectScore = _service.ScoreProduct(perfectProduct, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: No BTU = zero BTU component while other factors still contribute
        score.Should().NotBeNull();
        perfectScore.Should().NotBeNull();
        (perfectScore!.Value - score!.Value).Should().BeApproximately(0.4, 0.01);
    }

    #endregion

    #region Inverter Bonus Tests

    [Fact]
    public void ScoreProduct_WithInverter_IncludesBonus()
    {
        // Arrange: Perfect BTU fit, with inverter
        var productWithInverter = CreateProduct(btu: 2640, isInverter: true);
        var productWithoutInverter = CreateProduct(btu: 2640, isInverter: false);

        // Act
        var scoreWith = _service.ScoreProduct(productWithInverter, 24, 'B', "living");
        var scoreWithout = _service.ScoreProduct(productWithoutInverter, 24, 'B', "living");

        // Assert: Inverter should add 15% bonus
        scoreWith.Should().NotBeNull();
        scoreWithout.Should().NotBeNull();
        (scoreWith - scoreWithout).Should().BeApproximately(0.15, 0.01);
    }

    #endregion

    #region Zone Fit Tests

    [Fact]
    public void ScoreProduct_ZoneA_AlwaysGets1_0Score()
    {
        // Arrange: Zone A (coastal/warm) - temperature requirements don't matter
        var product = CreateProduct(btu: 2640, minTemp: 0);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'A', roomType: "living");

        // Assert: Zone fit should be 1.0 for zone A
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4); // At least partial score
    }

    [Fact]
    public void ScoreProduct_ZoneB_AlwaysGets1_0Score()
    {
        // Arrange: Zone B (temperate) - temperature requirements don't matter
        var product = CreateProduct(btu: 2640, minTemp: -20);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Zone fit should be 1.0 for zone B
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4);
    }

    [Fact]
    public void ScoreProduct_ZoneCWithColdCapable_Gets1_0ZoneFitScore()
    {
        // Arrange: Zone C (alpine/cold), product min temp -20°C (qualifies for 1.0)
        var product = CreateProduct(btu: 2640, minTemp: -20);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'C', roomType: "living");

        // Assert: Should qualify for full zone fit score in C
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4);
    }

    [Fact]
    public void ScoreProduct_ZoneCWithModeratelyCold_Gets0_5ZoneFitScore()
    {
        // Arrange: Zone C, product min temp -10°C (qualifies for 0.5)
        var product = CreateProduct(btu: 2640, minTemp: -10);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'C', roomType: "living");

        var coldCapable = CreateProduct(btu: 2640, minTemp: -20);
        var coldScore = _service.ScoreProduct(coldCapable, areaM2: 24, climateZone: 'C', roomType: "living");

        // Assert: Should get reduced zone fit score versus cold-capable product
        score.Should().NotBeNull();
        coldScore.Should().NotBeNull();
        (coldScore!.Value - score!.Value).Should().BeApproximately(0.075, 0.01);
    }

    [Fact]
    public void ScoreProduct_ZoneCWithWarmLimit_GetsZeroZoneFitScore()
    {
        // Arrange: Zone C, product min temp 0°C (doesn't qualify for C)
        var product = CreateProduct(btu: 2640, minTemp: 0);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'C', roomType: "living");

        var coldCapable = CreateProduct(btu: 2640, minTemp: -20);
        var coldScore = _service.ScoreProduct(coldCapable, areaM2: 24, climateZone: 'C', roomType: "living");

        // Assert: Should get zero zone-fit component versus cold-capable product
        score.Should().NotBeNull();
        coldScore.Should().NotBeNull();
        (coldScore!.Value - score!.Value).Should().BeApproximately(0.15, 0.01);
    }

    #endregion

    #region Room Type Fit Tests

    [Fact]
    public void ScoreProduct_RoomTypeMatches_Gets1_0RoomFitScore()
    {
        // Arrange: Product recommended for living rooms
        var product = CreateProduct(
            btu: 2640,
            recommendedRoomTypes: new List<string> { "living", "bedroom" });

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Should get full room fit score
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4);
    }

    [Fact]
    public void ScoreProduct_RoomTypeDoesNotMatch_Gets0_3PartialScore()
    {
        // Arrange: Product only recommended for bedrooms
        var product = CreateProduct(
            btu: 2640,
            recommendedRoomTypes: new List<string> { "bedroom" });

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "office");

        var matchingProduct = CreateProduct(
            btu: 2640,
            recommendedRoomTypes: new List<string> { "office" });
        var matchingScore = _service.ScoreProduct(matchingProduct, areaM2: 24, climateZone: 'B', roomType: "office");

        // Assert: Should get partial room-fit score (0.3) versus full room match
        score.Should().NotBeNull();
        matchingScore.Should().NotBeNull();
        (matchingScore!.Value - score!.Value).Should().BeApproximately(0.105, 0.01);
    }

    [Fact]
    public void ScoreProduct_NoRoomTypeRecommendation_Gets0_3PartialScore()
    {
        // Arrange: Product has no recommended room types
        var product = CreateProduct(btu: 2640);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        var matchingProduct = CreateProduct(
            btu: 2640,
            recommendedRoomTypes: new List<string> { "living" });
        var matchingScore = _service.ScoreProduct(matchingProduct, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Should get partial room-fit score when no recommendation data exists
        score.Should().NotBeNull();
        matchingScore.Should().NotBeNull();
        (matchingScore!.Value - score!.Value).Should().BeApproximately(0.105, 0.01);
    }

    #endregion

    #region Stock Availability Tests

    [Fact]
    public void ScoreProduct_OutOfStock_ReturnsNull()
    {
        // Arrange: Product with no stock
        var product = CreateProduct(btu: 2640, stockQuantity: 0);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Out of stock should exclude product (return null)
        score.Should().BeNull();
    }

    [Fact]
    public void ScoreProduct_InStock_IncludesStockScore()
    {
        // Arrange: Product with stock
        var product = CreateProduct(btu: 2640, stockQuantity: 5);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Should include 10% stock score
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.4);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ScoreProduct_AllFactorsOptimal_HighScore()
    {
        // Arrange: All factors optimal
        var product = CreateProduct(
            btu: 2640,
            isInverter: true,
            minTemp: -20,
            recommendedRoomTypes: new List<string> { "living" },
            noiseLevel: 25,
            stockQuantity: 10);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'C', roomType: "living");

        // Assert: Should be high score (0.8+)
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void ScoreProduct_MixedFactors_ModerateScore()
    {
        // Arrange: Mixed quality factors
        var product = CreateProduct(
            btu: 2200,  // Slightly under requirement
            isInverter: false,
            minTemp: -5,
            recommendedRoomTypes: null,  // No recommendation
            stockQuantity: 3);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        var optimalProduct = CreateProduct(
            btu: 2640,
            isInverter: true,
            recommendedRoomTypes: new List<string> { "living" },
            stockQuantity: 3);
        var optimalScore = _service.ScoreProduct(optimalProduct, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert: Should be moderate relative to an optimal comparable product
        score.Should().NotBeNull();
        optimalScore.Should().NotBeNull();
        score.Should().BeGreaterThan(0.2);
        score.Should().BeLessThan(optimalScore!.Value);
    }

    [Fact]
    public void ScoreProduct_ZoneARequirements_ComparableScores()
    {
        // Arrange: Two products, one for warm climate
        var economyUnit = CreateProduct(
            btu: 1350,  // 15 m² × 90 BTU/m² (Zone A)
            isInverter: false,
            recommendedRoomTypes: new List<string> { "bedroom" });

        var premiumUnit = CreateProduct(
            btu: 1350,
            isInverter: true,
            recommendedRoomTypes: new List<string> { "bedroom" });

        // Act
        var economyScore = _service.ScoreProduct(economyUnit, areaM2: 15, climateZone: 'A', roomType: "bedroom");
        var premiumScore = _service.ScoreProduct(premiumUnit, areaM2: 15, climateZone: 'A', roomType: "bedroom");

        // Assert: Premium should be higher due to inverter
        economyScore.Should().NotBeNull();
        premiumScore.Should().NotBeNull();
        premiumScore.Should().BeGreaterThan(economyScore!.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ScoreProduct_SmallArea_CalculatesCorrectly()
    {
        // Arrange: Very small area (5 m²)
        var product = CreateProduct(btu: 550);  // 5 m² × 110 BTU/m²

        // Act
        var score = _service.ScoreProduct(product, areaM2: 5, climateZone: 'B', roomType: "living");

        // Assert: Should handle small areas correctly
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void ScoreProduct_LargeArea_CalculatesCorrectly()
    {
        // Arrange: Large area (500 m²)
        var product = CreateProduct(btu: 55000);  // 500 m² × 110 BTU/m²

        // Act
        var score = _service.ScoreProduct(product, areaM2: 500, climateZone: 'B', roomType: "commercial");

        // Assert: Should handle large areas correctly
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.3);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('B')]
    [InlineData('C')]
    public void ScoreProduct_AllZones_ProducesScores(char zone)
    {
        // Arrange
        var multipliers = new Dictionary<char, int> { { 'A', 90 }, { 'B', 110 }, { 'C', 140 } };
        var requiredBtu = 24 * multipliers[zone];
        var product = CreateProduct(btu: requiredBtu);

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: zone, roomType: "living");

        // Assert
        score.Should().NotBeNull();
        score.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ScoreProduct_DoesNotMutateProductSpecifications()
    {
        // Arrange
        var product = CreateProduct(
            btu: 2640,
            isInverter: true,
            noiseLevel: 22,
            recommendedRoomTypes: ["living"]);
        var originalKeys = product.Specifications.Keys.ToList();

        // Act
        var score = _service.ScoreProduct(product, areaM2: 24, climateZone: 'B', roomType: "living");

        // Assert
        score.Should().NotBeNull();
        product.Specifications.Keys.Should().BeEquivalentTo(originalKeys);
        product.Specifications.Should().NotContainKey("_tiebreaker_inverter");
        product.Specifications.Should().NotContainKey("_tiebreaker_quiet_mode");
    }

    #endregion
}
