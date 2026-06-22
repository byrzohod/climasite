using ClimaSite.Application.Features.Promotions.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Promotions;

public class GetFeaturedPromotionsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetFeaturedPromotionsQueryHandler CreateHandler() => new(_context);

    private Promotion SeedPromotion(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = true,
        int sortOrder = 0,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow.AddDays(7);
        var promotion = new Promotion(name, slug, PromotionType.Percentage, 15m, start, end);
        promotion.SetActive(isActive);
        promotion.SetFeatured(isFeatured);
        promotion.SetSortOrder(sortOrder);
        _context.Promotions.Add(promotion);
        return promotion;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveFeaturedCurrentPromotions()
    {
        SeedPromotion("Featured", "featured", isFeatured: true);
        SeedPromotion("NotFeatured", "not-featured", isFeatured: false);
        SeedPromotion("InactiveFeatured", "inactive-featured", isActive: false, isFeatured: true);
        SeedPromotion("ExpiredFeatured", "expired-featured", isFeatured: true,
            startDate: DateTime.UtcNow.AddDays(-10), endDate: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(new GetFeaturedPromotionsQuery(), CancellationToken.None);

        result.Should().ContainSingle(p => p.Slug == "featured");
    }

    [Fact]
    public async Task Handle_OrdersBySortOrderThenEndDateDescending()
    {
        SeedPromotion("Second", "second", sortOrder: 1, endDate: DateTime.UtcNow.AddDays(5));
        SeedPromotion("FirstEarly", "first-early", sortOrder: 0, endDate: DateTime.UtcNow.AddDays(2));
        SeedPromotion("FirstLate", "first-late", sortOrder: 0, endDate: DateTime.UtcNow.AddDays(9));

        var result = await CreateHandler().Handle(new GetFeaturedPromotionsQuery(), CancellationToken.None);

        result.Select(p => p.Slug).Should().ContainInOrder("first-late", "first-early", "second");
    }

    [Fact]
    public async Task Handle_RespectsCountLimit()
    {
        for (var i = 1; i <= 10; i++)
        {
            SeedPromotion($"Promo {i:D2}", $"promo-{i:D2}", sortOrder: i);
        }

        var result = await CreateHandler().Handle(
            new GetFeaturedPromotionsQuery { Count = 4 },
            CancellationToken.None);

        result.Should().HaveCount(4);
        result.Select(p => p.Slug).Should().ContainInOrder("promo-01", "promo-02", "promo-03", "promo-04");
    }

    [Fact]
    public async Task Handle_MapsProductCount_FromLinkedProducts()
    {
        var promotion = SeedPromotion("WithProducts", "with-products");
        promotion.Products.Add(new PromotionProduct { PromotionId = promotion.Id, ProductId = Guid.NewGuid() });

        var result = await CreateHandler().Handle(new GetFeaturedPromotionsQuery(), CancellationToken.None);

        result.Single(p => p.Slug == "with-products").ProductCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UsesTranslatedContent_WhenLanguageMatches()
    {
        var promotion = SeedPromotion("Winter Sale", "winter-sale");
        promotion.Translations.Add(new PromotionTranslation
        {
            PromotionId = promotion.Id,
            LanguageCode = "bg",
            Name = "Зимна разпродажба"
        });

        var result = await CreateHandler().Handle(
            new GetFeaturedPromotionsQuery { LanguageCode = "bg" },
            CancellationToken.None);

        result.Single(p => p.Slug == "winter-sale").Name.Should().Be("Зимна разпродажба");
    }

    [Fact]
    public async Task Handle_NoFeaturedPromotions_ReturnsEmptyList()
    {
        SeedPromotion("NotFeatured", "not-featured", isFeatured: false);

        var result = await CreateHandler().Handle(new GetFeaturedPromotionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
