using ClimaSite.Application.Features.Promotions.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Promotions;

public class GetPromotionBySlugQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetPromotionBySlugQueryHandler CreateHandler() => new(_context);

    private Promotion SeedPromotion(
        string name,
        string slug,
        bool isActive = true,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow.AddDays(7);
        var promotion = new Promotion(name, slug, PromotionType.FixedAmount, 25m, start, end);
        promotion.SetActive(isActive);
        _context.Promotions.Add(promotion);
        return promotion;
    }

    private static PromotionProduct LinkProduct(Promotion promotion, Product product)
    {
        var link = new PromotionProduct
        {
            PromotionId = promotion.Id,
            ProductId = product.Id,
            Product = product
        };
        promotion.Products.Add(link);
        return link;
    }

    private Product SeedProduct(string slug, bool isActive = true)
    {
        var product = new Product($"SKU-{slug}", $"Product {slug}", slug, 199.99m);
        product.SetActive(isActive);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_UnknownSlug_ReturnsNull()
    {
        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "does-not-exist" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InactivePromotion_ReturnsNull()
    {
        SeedPromotion("Hidden", "hidden", isActive: false);

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "hidden" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExpiredPromotion_ReturnsNull()
    {
        SeedPromotion("Expired", "expired",
            startDate: DateTime.UtcNow.AddDays(-10), endDate: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "expired" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FuturePromotion_ReturnsNull()
    {
        SeedPromotion("Future", "future",
            startDate: DateTime.UtcNow.AddDays(1), endDate: DateTime.UtcNow.AddDays(10));

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "future" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActivePromotion_ReturnsDtoWithCoreFields()
    {
        var promotion = SeedPromotion("Spring Sale", "spring-sale");
        promotion.SetCode("spring");
        promotion.SetMinimumOrderAmount(50m);
        promotion.SetTermsAndConditions("Terms apply");

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "spring-sale" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("spring-sale");
        result.Name.Should().Be("Spring Sale");
        result.Code.Should().Be("SPRING");
        result.Type.Should().Be(PromotionType.FixedAmount);
        result.DiscountValue.Should().Be(25m);
        result.MinimumOrderAmount.Should().Be(50m);
        result.TermsAndConditions.Should().Be("Terms apply");
        result.IsActive.Should().BeTrue();
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IncludesOnlyActiveLinkedProducts()
    {
        var promotion = SeedPromotion("Bundle", "bundle");
        var active = SeedProduct("active-product");
        var inactive = SeedProduct("inactive-product", isActive: false);
        LinkProduct(promotion, active);
        LinkProduct(promotion, inactive);

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "bundle" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Products.Should().ContainSingle(p => p.Slug == "active-product");
    }

    [Fact]
    public async Task Handle_UsesTranslatedContent_WhenLanguageMatches()
    {
        var promotion = SeedPromotion("Spring Sale", "spring-sale");
        promotion.Translations.Add(new PromotionTranslation
        {
            PromotionId = promotion.Id,
            LanguageCode = "de",
            Name = "Frühlingsverkauf",
            Description = "Beschreibung",
            TermsAndConditions = "AGB"
        });

        var result = await CreateHandler().Handle(
            new GetPromotionBySlugQuery { Slug = "spring-sale", LanguageCode = "de" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Frühlingsverkauf");
        result.Description.Should().Be("Beschreibung");
        result.TermsAndConditions.Should().Be("AGB");
    }
}
