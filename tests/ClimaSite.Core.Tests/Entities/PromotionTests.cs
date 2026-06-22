using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class PromotionTests
{
    private static Promotion CreateValid(
        PromotionType type = PromotionType.Percentage,
        decimal discountValue = 10m) =>
        new("Summer Sale", "summer-sale", type, discountValue,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

    [Fact]
    public void Constructor_WithValidData_CreatesActivePromotion()
    {
        var promotion = CreateValid();

        promotion.Name.Should().Be("Summer Sale");
        promotion.Slug.Should().Be("summer-sale");
        promotion.Type.Should().Be(PromotionType.Percentage);
        promotion.DiscountValue.Should().Be(10m);
        promotion.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        var promotion = CreateValid();

        var act = () => promotion.SetName(name);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*name cannot be empty*");
    }

    [Fact]
    public void SetName_TrimsWhitespace()
    {
        var promotion = CreateValid();

        promotion.SetName("  Black Friday  ");

        promotion.Name.Should().Be("Black Friday");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSlug_WithEmptyValue_ThrowsArgumentException(string slug)
    {
        var promotion = CreateValid();

        var act = () => promotion.SetSlug(slug);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*slug cannot be empty*");
    }

    [Fact]
    public void SetSlug_NormalizesToLowerInvariant()
    {
        var promotion = CreateValid();

        promotion.SetSlug("  BLACK-Friday  ");

        promotion.Slug.Should().Be("black-friday");
    }

    [Fact]
    public void SetCode_NormalizesToUpperInvariant()
    {
        var promotion = CreateValid();

        promotion.SetCode("  save10  ");

        promotion.Code.Should().Be("SAVE10");
    }

    [Fact]
    public void SetCode_WithNull_SetsNull()
    {
        var promotion = CreateValid();

        promotion.SetCode(null);

        promotion.Code.Should().BeNull();
    }

    [Fact]
    public void SetDiscountValue_WithNegativeValue_ThrowsArgumentException()
    {
        var promotion = CreateValid();

        var act = () => promotion.SetDiscountValue(-1m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Discount value cannot be negative*");
    }

    [Fact]
    public void SetDiscountValue_PercentageOver100_ThrowsArgumentException()
    {
        var promotion = CreateValid(PromotionType.Percentage);

        var act = () => promotion.SetDiscountValue(101m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Percentage discount cannot exceed 100*");
    }

    [Fact]
    public void SetDiscountValue_FixedAmountOver100_IsAllowed()
    {
        var promotion = CreateValid(PromotionType.FixedAmount, 50m);

        promotion.SetDiscountValue(250m);

        promotion.DiscountValue.Should().Be(250m);
    }

    [Fact]
    public void SetMinimumOrderAmount_WithNegativeValue_ThrowsArgumentException()
    {
        var promotion = CreateValid();

        var act = () => promotion.SetMinimumOrderAmount(-1m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Minimum order amount cannot be negative*");
    }

    [Fact]
    public void SetMinimumOrderAmount_WithNull_IsAllowed()
    {
        var promotion = CreateValid();

        promotion.SetMinimumOrderAmount(null);

        promotion.MinimumOrderAmount.Should().BeNull();
    }

    [Fact]
    public void SetDates_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var promotion = CreateValid();
        var start = DateTime.UtcNow;

        var act = () => promotion.SetDates(start, start.AddDays(-1));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*End date must be after start date*");
    }

    [Fact]
    public void SetDates_EndDateEqualToStartDate_ThrowsArgumentException()
    {
        var promotion = CreateValid();
        var start = DateTime.UtcNow;

        var act = () => promotion.SetDates(start, start);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsCurrentlyActive_WhenActiveAndWithinDates_ReturnsTrue()
    {
        var promotion = CreateValid();

        promotion.IsCurrentlyActive.Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_WhenInactive_ReturnsFalse()
    {
        var promotion = CreateValid();
        promotion.SetActive(false);

        promotion.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_WhenDatesInFuture_ReturnsFalse()
    {
        var promotion = new Promotion("Future", "future", PromotionType.Percentage, 10m,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        promotion.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void GetTranslatedContent_WithNullLanguage_ReturnsBaseContent()
    {
        var promotion = CreateValid();
        promotion.SetDescription("Base description");
        promotion.SetTermsAndConditions("Base terms");

        var (name, description, terms) = promotion.GetTranslatedContent(null);

        name.Should().Be("Summer Sale");
        description.Should().Be("Base description");
        terms.Should().Be("Base terms");
    }

    [Fact]
    public void GetTranslatedContent_WithEnglish_ReturnsBaseContent()
    {
        var promotion = CreateValid();

        var (name, _, _) = promotion.GetTranslatedContent("EN");

        name.Should().Be("Summer Sale");
    }

    [Fact]
    public void GetTranslatedContent_WithMissingTranslation_FallsBackToBase()
    {
        var promotion = CreateValid();

        var (name, _, _) = promotion.GetTranslatedContent("bg");

        name.Should().Be("Summer Sale");
    }

    [Fact]
    public void GetTranslatedContent_WithTranslation_ReturnsTranslatedFields()
    {
        var promotion = CreateValid();
        promotion.SetDescription("Base description");
        promotion.Translations.Add(new PromotionTranslation
        {
            LanguageCode = "de",
            Name = "Sommerschlussverkauf",
            Description = null,
            TermsAndConditions = "Bedingungen"
        });

        var (name, description, terms) = promotion.GetTranslatedContent("DE");

        name.Should().Be("Sommerschlussverkauf");
        description.Should().Be("Base description"); // null translation falls back
        terms.Should().Be("Bedingungen");
    }

    [Fact]
    public void SetFeaturedAndSortOrder_UpdateProperties()
    {
        var promotion = CreateValid();

        promotion.SetFeatured(true);
        promotion.SetSortOrder(5);

        promotion.IsFeatured.Should().BeTrue();
        promotion.SortOrder.Should().Be(5);
    }
}
