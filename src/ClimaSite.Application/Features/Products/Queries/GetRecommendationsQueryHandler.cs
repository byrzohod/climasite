#nullable enable

using System.Text.Json;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        // Score every eligible product before taking the top results; pre-score truncation can exclude the best fit.
        var candidates = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Variants.Any(v => v.IsActive && v.StockQuantity > 0))
            .Include(p => p.Variants.Where(v => v.IsActive && v.StockQuantity > 0))
            .ToListAsync(cancellationToken);

        // Score and filter products
        var scoredProducts = candidates
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

        var sortedProductIds = sorted.Select(x => x.Product.Id).ToList();
        var productDetails = await _context.Products
            .AsNoTracking()
            .Where(p => sortedProductIds.Contains(p.Id))
            .Include(p => p.Variants.Where(v => v.IsActive && v.StockQuantity > 0))
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Map to RecommendedProductDto
        return sorted.Select(x => MapToRecommendedProductDto(
            productDetails[x.Product.Id],
            x.Score!.Value,
            areaM2,
            climateZone,
            request.LanguageCode))
            .ToList();
    }

    private RecommendedProductDto MapToRecommendedProductDto(
        Core.Entities.Product product,
        double score,
        int areaM2,
        char climateZone,
        string? languageCode)
    {
        // Get translated content
        var (name, shortDescription, _, _, _) = product.GetTranslatedContent(languageCode);

        // Extract specifications
        var btu = ExtractIntSpec(product.Specifications, "btu");
        var isInverter = ExtractBoolSpec(product.Specifications, "isInverter");
        var noiseLevel = ExtractIntSpec(product.Specifications, "noiseLevel");

        // Build match reason as a frontend i18n key.
        var matchReason = BuildMatchReasonKey(btu, areaM2, climateZone);

        return new RecommendedProductDto
        {
            // ProductBriefDto fields
            Id = product.Id,
            Name = name,
            Slug = product.Slug,
            ShortDescription = shortDescription,
            BasePrice = product.BasePrice,
            SalePrice = ProductPricing.GetSalePrice(product.BasePrice, product.CompareAtPrice),
            IsOnSale = ProductPricing.IsOnSale(product.BasePrice, product.CompareAtPrice),
            DiscountPercentage = ProductPricing.GetDiscountPercentage(product.BasePrice, product.CompareAtPrice),
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
    /// Build a frontend translation key for the recommendation match reason.
    /// </summary>
    private string BuildMatchReasonKey(int btu, int areaM2, char climateZone)
    {
        if (btu == 0)
            return "homeV3.matchReason.fallback";

        var multipliers = new Dictionary<char, int> { { 'A', 90 }, { 'B', 110 }, { 'C', 140 } };
        if (!multipliers.TryGetValue(climateZone, out var mult))
            mult = 110;

        var requiredBtu = areaM2 * mult;
        var percentage = requiredBtu > 0 ? (double)btu / requiredBtu : 0;

        // Determine fit level
        if (percentage >= 0.9 && percentage <= 1.1)
            return "homeV3.matchReason.perfectFit";

        if (percentage < 0.9)
            return "homeV3.matchReason.efficient";

        return "homeV3.matchReason.powerful";
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
