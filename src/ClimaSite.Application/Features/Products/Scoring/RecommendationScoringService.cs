#nullable enable

using ClimaSite.Application.Features.Products.Specifications;
using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Products.Scoring;

/// <summary>
/// Pure scoring service for product recommendations based on weighted rules-based algorithm.
/// No I/O, no side effects — all calculations are deterministic and testable.
/// </summary>
public class RecommendationScoringService
{
    /// <summary>
    /// Zone multipliers: required BTU per m² for different climate zones (realistic HVAC sizing — the
    /// ~20 BTU/sqft ≈ 215 BTU/m² rule of thumb, adjusted per zone). A = coastal/warm (200), B = temperate/default
    /// (250), C = alpine/cold (320 — heating-dominated load needs the largest capacity). These MUST stay in sync
    /// with the frontend preview (home-wizard-state.service.ts) and <see cref="GetRecommendationsQueryHandler"/>.
    /// </summary>
    public static readonly Dictionary<char, int> ZoneMultipliers = new()
    {
        { 'A', 200 },
        { 'B', 250 },
        { 'C', 320 }
    };

    /// <summary>
    /// Allowed room types for validation.
    /// </summary>
    public static readonly string[] AllowedRoomTypes = { "living", "bedroom", "office", "commercial" };

    /// <summary>
    /// Score a single product against user requirements.
    /// Returns null if the product should be excluded (e.g., out of stock).
    /// </summary>
    public double? ScoreProduct(
        Product product,
        int areaM2,
        char climateZone,
        string roomType)
    {
        // Extract HVAC specifications via the shared alias-aware resolver (handles display keys like "BTU"/
        // "Noise Level"/"BTU Cooling" and JSONB round-tripped JsonElement values — see HvacSpecResolver).
        var btu = HvacSpecResolver.GetInt(product.Specifications, "btu") ?? 0;
        var isInverter = HvacSpecResolver.GetBool(product.Specifications, "isInverter") ?? false;
        var minTemp = HvacSpecResolver.GetInt(product.Specifications, "minTemp") ?? 0;
        var recommendedRoomTypes = HvacSpecResolver.GetStringList(product.Specifications, "recommendedRoomTypes");
        var inStock = CalculateInStock(product);

        // Early exit: out of stock products are excluded
        if (!inStock)
            return null;

        // Calculate individual score components
        var btuFitScore = CalculateBtuFitScore(btu, areaM2, climateZone);
        var inverterBonus = isInverter ? 1.0 : 0.0;
        var zoneFitScore = CalculateZoneFitScore(minTemp, climateZone);
        var roomTypeFitScore = CalculateRoomTypeFitScore(recommendedRoomTypes, roomType);
        var stockAvailableScore = 1.0; // Already excluded if out of stock
        var priceBandAffinity = CalculatePriceBandAffinity(product.BasePrice, climateZone);

        // Weighted total
        var totalScore =
            btuFitScore * 0.40
            + inverterBonus * 0.15
            + zoneFitScore * 0.15
            + roomTypeFitScore * 0.15
            + stockAvailableScore * 0.10
            + priceBandAffinity * 0.05;

        return totalScore;
    }

    /// <summary>
    /// BTU fit score: 1.0 when product BTU is within ±10% of (area × zone_multiplier).
    /// Linear fall-off to 0 at 50% over/under.
    /// </summary>
    private double CalculateBtuFitScore(int productBtu, int areaM2, char climateZone)
    {
        if (productBtu == 0)
            return 0.0;

        if (!ZoneMultipliers.TryGetValue(climateZone, out var multiplier))
            return 0.0;

        var requiredBtu = areaM2 * multiplier;
        if (requiredBtu == 0)
            return 0.0;

        var percentage = (double)productBtu / requiredBtu;

        // Perfect range: 0.9 to 1.1 (±10%)
        if (percentage >= 0.9 && percentage <= 1.1)
            return 1.0;

        // Below 0.9: linear fall-off to 0 at 0.5 (50% under)
        if (percentage < 0.9)
        {
            var falloffRange = 0.9 - 0.5; // 0.4
            var below = 0.9 - percentage;
            if (below >= falloffRange)
                return 0.0;
            return 1.0 - (below / falloffRange);
        }

        // Above 1.1: linear fall-off to 0 at 1.5 (50% over)
        var aboveRange = 1.5 - 1.1; // 0.4
        var above = percentage - 1.1;
        if (above >= aboveRange)
            return 0.0;
        return 1.0 - (above / aboveRange);
    }

    /// <summary>
    /// Zone fit score: for zone C (alpine/cold), check min operating temperature.
    /// C: 1.0 if min_temp <= -15°C, 0.5 if <= -5°C, else 0.
    /// A/B: always 1.0.
    /// </summary>
    private double CalculateZoneFitScore(int minTempCelsius, char climateZone)
    {
        if (climateZone != 'C')
            return 1.0;

        // Zone C: check cold-climate capability
        if (minTempCelsius <= -15)
            return 1.0;

        if (minTempCelsius <= -5)
            return 0.5;

        return 0.0;
    }

    /// <summary>
    /// Room type fit: 1.0 if room type matches product's recommended list, else 0.3 (partial).
    /// </summary>
    private double CalculateRoomTypeFitScore(List<string>? recommendedRoomTypes, string roomType)
    {
        if (recommendedRoomTypes == null || recommendedRoomTypes.Count == 0)
            return 0.3; // No specific recommendation = partial match

        var normalizedRoom = roomType.ToLowerInvariant();
        var normalized = recommendedRoomTypes.Select(r => r.ToLowerInvariant()).ToList();

        return normalized.Contains(normalizedRoom) ? 1.0 : 0.3;
    }

    /// <summary>
    /// Stock available: 1.0 if in stock, else excluded earlier (returns null).
    /// This is always 1.0 for products that reach here.
    /// </summary>
    private double CalculateStockAvailableScore(bool inStock)
    {
        return inStock ? 1.0 : 0.0;
    }

    /// <summary>
    /// Price band affinity: gentle tilt. Mid-band products (zone-adjusted) get 1.0,
    /// outliers get 0.6. Prevents extreme low/high prices from dominating.
    /// </summary>
    private double CalculatePriceBandAffinity(decimal productPrice, char climateZone)
    {
        // Rough price bands by zone (in USD, for HVAC)
        // These are typical ranges; adjust as needed based on actual product mix
        var (minBand, maxBand) = climateZone switch
        {
            'A' => (300m, 1200m),  // Coastal/warm: smaller units
            'C' => (800m, 2500m),  // Alpine/cold: larger units
            _ => (500m, 1500m)      // Temperate (B): default
        };

        if (productPrice >= minBand && productPrice <= maxBand)
            return 1.0;

        // Outliers: slightly penalized
        if (productPrice < minBand || productPrice > maxBand)
            return 0.6;

        return 1.0;
    }

    /// <summary>
    /// Check if product has available stock across all variants.
    /// </summary>
    private bool CalculateInStock(Product product)
    {
        return product.Variants.Any(v => v.IsActive && v.StockQuantity > 0);
    }
}
