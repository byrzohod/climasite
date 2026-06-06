#nullable enable

using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClimaSite.Application.Features.Products.Queries;

public class GetRecommendationsQueryHandler : IRequestHandler<GetRecommendationsQuery, List<RecommendedProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly RecommendationScoringService _scoringService;

    public GetRecommendationsQueryHandler(
        IApplicationDbContext context,
        RecommendationScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<List<RecommendedProductDto>> Handle(
        GetRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        // Normalize inputs
        var areaM2 = request.AreaM2;
        var roomType = request.RoomType.ToLowerInvariant();
        var climateZone = char.ToUpperInvariant(request.ClimateZone[0]);

        // Load all active products with their variants and images
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        // Score and filter products
        var scoredProducts = products
            .Select(p => new
            {
                Product = p,
                Score = _scoringService.ScoreProduct(p, areaM2, climateZone, roomType)
            })
            .Where(x => x.Score.HasValue && x.Score.Value > 0)
            .ToList();

        // Sort by score (descending), then by tie-breakers
        var sorted = scoredProducts
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => GetTiebreakerInverter(x.Product))
            .ThenByDescending(x => GetTiebreakerQuietMode(x.Product))
            .ThenByDescending(x => GetLatestStockUpdate(x.Product))
            .Take(3)
            .ToList();

        // Map to RecommendedProductDto
        return sorted.Select(x => MapToRecommendedProductDto(
            x.Product,
            x.Score!.Value,
            areaM2,
            climateZone,
            roomType,
            request.LanguageCode))
            .ToList();
    }

    private RecommendedProductDto MapToRecommendedProductDto(
        Core.Entities.Product product,
        double score,
        int areaM2,
        char climateZone,
        string roomType,
        string? languageCode)
    {
        // Get translated content
        var (name, shortDescription, _, _, _) = product.GetTranslatedContent(languageCode);

        // Extract specifications
        var btu = ExtractIntSpec(product.Specifications, "btu");
        var isInverter = ExtractBoolSpec(product.Specifications, "isInverter");
        var noiseLevel = ExtractIntSpec(product.Specifications, "noiseLevel");

        // Build match reason (i18n key — can be localized in frontend)
        var matchReason = BuildMatchReasonKey(btu, areaM2, climateZone, roomType);

        return new RecommendedProductDto
        {
            // ProductBriefDto fields
            Id = product.Id,
            Name = name,
            Slug = product.Slug,
            ShortDescription = shortDescription,
            BasePrice = product.BasePrice,
            SalePrice = product.CompareAtPrice,
            IsOnSale = product.CompareAtPrice.HasValue && product.CompareAtPrice > product.BasePrice,
            DiscountPercentage = product.CompareAtPrice.HasValue && product.CompareAtPrice > product.BasePrice
                ? Math.Round((product.CompareAtPrice.Value - product.BasePrice) / product.CompareAtPrice.Value * 100, 0)
                : 0,
            Brand = product.Brand,
            AverageRating = 0, // Will be calculated from reviews if needed
            ReviewCount = 0,
            PrimaryImageUrl = product.Images
                .Where(i => i.IsPrimary)
                .Select(i => i.Url)
                .FirstOrDefault(),
            InStock = product.Variants.Any(v => v.StockQuantity > 0),

            // Recommendation fields
            Score = Math.Round(score, 4),
            MatchReason = matchReason,
            BtuCapacity = btu,
            IsInverter = isInverter,
            NoiseLevel = noiseLevel > 0 ? noiseLevel : null
        };
    }

    /// <summary>
    /// Build a human-readable match reason key (localizable).
    /// Examples: "recommendations.reason.perfect_fit", "recommendations.reason.excellent_inverter"
    /// </summary>
    private string BuildMatchReasonKey(int btu, int areaM2, char climateZone, string roomType)
    {
        // Rough categorization based on BTU fit and room type
        if (btu == 0)
            return "recommendations.reason.default";

        var multipliers = new Dictionary<char, int> { { 'A', 90 }, { 'B', 110 }, { 'C', 140 } };
        if (!multipliers.TryGetValue(climateZone, out var mult))
            mult = 110;

        var requiredBtu = areaM2 * mult;
        var percentage = requiredBtu > 0 ? (double)btu / requiredBtu : 0;

        // Determine fit level
        if (percentage >= 0.9 && percentage <= 1.1)
            return $"recommendations.reason.perfect_fit_for_{roomType}";

        if (percentage < 0.9)
            return $"recommendations.reason.efficient_fit_for_{roomType}";

        return $"recommendations.reason.powerful_fit_for_{roomType}";
    }

    private int GetTiebreakerInverter(Core.Entities.Product product)
    {
        return ExtractBoolSpec(product.Specifications, "isInverter") ? 1 : 0;
    }

    private int GetTiebreakerQuietMode(Core.Entities.Product product)
    {
        var noiseLevel = ExtractIntSpec(product.Specifications, "noiseLevel");
        return noiseLevel > 0 && noiseLevel < 30 ? 1 : 0;
    }

    private DateTime GetLatestStockUpdate(Core.Entities.Product product)
    {
        return product.Variants
            .Where(v => v.StockQuantity > 0)
            .Max(v => (DateTime?)v.UpdatedAt) ?? DateTime.MinValue;
    }

    private int ExtractIntSpec(Dictionary<string, object>? specs, string key)
    {
        if (specs == null || !specs.TryGetValue(key, out var value))
            return 0;

        if (value is int intVal)
            return intVal;

        if (value is long longVal)
            return (int)longVal;

        if (value is JsonElement jsonElement && jsonElement.TryGetInt32(out var jsonInt))
            return jsonInt;

        return 0;
    }

    private bool ExtractBoolSpec(Dictionary<string, object>? specs, string key)
    {
        if (specs == null || !specs.TryGetValue(key, out var value))
            return false;

        if (value is bool boolVal)
            return boolVal;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.True)
                return true;
            if (jsonElement.ValueKind == JsonValueKind.False)
                return false;
        }

        return false;
    }
}
