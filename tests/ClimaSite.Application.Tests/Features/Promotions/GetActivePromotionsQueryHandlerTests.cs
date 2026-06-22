using ClimaSite.Application.Features.Promotions.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Promotions;

public class GetActivePromotionsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetActivePromotionsQueryHandler CreateHandler() => new(_context);

    private Promotion SeedPromotion(
        string name,
        string slug,
        bool isActive = true,
        bool isFeatured = false,
        int sortOrder = 0,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow.AddDays(7);
        var promotion = new Promotion(name, slug, PromotionType.Percentage, 10m, start, end);
        promotion.SetActive(isActive);
        promotion.SetFeatured(isFeatured);
        promotion.SetSortOrder(sortOrder);
        _context.Promotions.Add(promotion);
        return promotion;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentlyActivePromotions()
    {
        SeedPromotion("Live", "live");
        SeedPromotion("Inactive", "inactive", isActive: false);
        SeedPromotion("Expired", "expired",
            startDate: DateTime.UtcNow.AddDays(-10), endDate: DateTime.UtcNow.AddDays(-1));
        SeedPromotion("Future", "future",
            startDate: DateTime.UtcNow.AddDays(1), endDate: DateTime.UtcNow.AddDays(10));

        var result = await CreateHandler().Handle(new GetActivePromotionsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(p => p.Slug == "live");
    }

    [Fact]
    public async Task Handle_OrdersBySortOrderThenEndDateDescending()
    {
        SeedPromotion("Second", "second", sortOrder: 1, endDate: DateTime.UtcNow.AddDays(5));
        SeedPromotion("FirstEarly", "first-early", sortOrder: 0, endDate: DateTime.UtcNow.AddDays(2));
        SeedPromotion("FirstLate", "first-late", sortOrder: 0, endDate: DateTime.UtcNow.AddDays(9));

        var result = await CreateHandler().Handle(new GetActivePromotionsQuery(), CancellationToken.None);

        result.Items.Select(p => p.Slug).Should().ContainInOrder("first-late", "first-early", "second");
    }

    [Fact]
    public async Task Handle_Paginates_AndReportsTotals()
    {
        for (var i = 1; i <= 20; i++)
        {
            SeedPromotion($"Promo {i:D2}", $"promo-{i:D2}", sortOrder: i);
        }

        var result = await CreateHandler().Handle(
            new GetActivePromotionsQuery { PageNumber = 2, PageSize = 12 },
            CancellationToken.None);

        result.TotalCount.Should().Be(20);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().HaveCount(8);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MapsProductCount_FromLinkedProducts()
    {
        var promotion = SeedPromotion("WithProducts", "with-products");
        promotion.Products.Add(new PromotionProduct { PromotionId = promotion.Id, ProductId = Guid.NewGuid() });
        promotion.Products.Add(new PromotionProduct { PromotionId = promotion.Id, ProductId = Guid.NewGuid() });

        var result = await CreateHandler().Handle(new GetActivePromotionsQuery(), CancellationToken.None);

        result.Items.Single(p => p.Slug == "with-products").ProductCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_MapsCoreFields_OntoBriefDto()
    {
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow.AddDays(7);
        var promotion = SeedPromotion("Summer Sale", "summer-sale", startDate: start, endDate: end);
        promotion.SetCode("summer");
        promotion.SetBannerImageUrl("https://example.com/banner.jpg");
        promotion.SetThumbnailImageUrl("https://example.com/thumb.jpg");

        var result = await CreateHandler().Handle(new GetActivePromotionsQuery(), CancellationToken.None);

        var item = result.Items.Single(p => p.Slug == "summer-sale");
        item.Name.Should().Be("Summer Sale");
        item.Code.Should().Be("SUMMER");
        item.Type.Should().Be(PromotionType.Percentage);
        item.DiscountValue.Should().Be(10m);
        item.BannerImageUrl.Should().Be("https://example.com/banner.jpg");
        item.ThumbnailImageUrl.Should().Be("https://example.com/thumb.jpg");
    }

    [Fact]
    public async Task Handle_UsesTranslatedContent_WhenLanguageMatches()
    {
        var promotion = SeedPromotion("Summer Sale", "summer-sale");
        promotion.Translations.Add(new PromotionTranslation
        {
            PromotionId = promotion.Id,
            LanguageCode = "de",
            Name = "Sommerschlussverkauf",
            Description = "Beschreibung"
        });

        var result = await CreateHandler().Handle(
            new GetActivePromotionsQuery { LanguageCode = "de" },
            CancellationToken.None);

        var item = result.Items.Single(p => p.Slug == "summer-sale");
        item.Name.Should().Be("Sommerschlussverkauf");
        item.Description.Should().Be("Beschreibung");
    }

    [Fact]
    public async Task Handle_NoPromotions_ReturnsEmptyPaginatedList()
    {
        var result = await CreateHandler().Handle(new GetActivePromotionsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
