#nullable enable

using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Scoring;
using ClimaSite.Application.Features.Products.Specifications;
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

        // Extract specifications via the shared alias-aware resolver (display keys + JSONB round-trip safe).
        var btu = HvacSpecResolver.GetInt(product.Specifications, "btu") ?? 0;
        var isInverter = HvacSpecResolver.GetBool(product.Specifications, "isInverter") ?? false;
        var noiseLevel = HvacSpecResolver.GetInt(product.Specifications, "noiseLevel") ?? 0;

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

        // Single source of truth for zone multipliers (kept in sync with the scoring service + FE preview).
        if (!RecommendationScoringService.ZoneMultipliers.TryGetValue(climateZone, out var mult))
            mult = RecommendationScoringService.ZoneMultipliers['B'];

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
        return (HvacSpecResolver.GetBool(product.Specifications, "isInverter") ?? false) ? 1 : 0;
    }

    private int GetTiebreakerQuietMode(Core.Entities.Product product)
    {
        var noiseLevel = HvacSpecResolver.GetInt(product.Specifications, "noiseLevel") ?? 0;
        return noiseLevel > 0 && noiseLevel < 30 ? 1 : 0;
    }

    private DateTime GetLatestStockUpdate(Core.Entities.Product product)
    {
        return product.Variants
            .Where(v => v.StockQuantity > 0)
            .Max(v => (DateTime?)v.UpdatedAt) ?? DateTime.MinValue;
    }
}
