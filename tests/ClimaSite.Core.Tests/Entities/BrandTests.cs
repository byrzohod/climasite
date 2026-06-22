using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class BrandTests
{
    private static Brand CreateValid() => new("Daikin", "daikin");

    [Fact]
    public void Constructor_WithValidData_CreatesActiveBrand()
    {
        var brand = CreateValid();

        brand.Name.Should().Be("Daikin");
        brand.Slug.Should().Be("daikin");
        brand.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        var brand = CreateValid();

        var act = () => brand.SetName(name);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*name cannot be empty*");
    }

    [Fact]
    public void SetName_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetName(new string('a', 101));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 100 characters*");
    }

    [Fact]
    public void SetName_TrimsWhitespace()
    {
        var brand = CreateValid();

        brand.SetName("  Mitsubishi  ");

        brand.Name.Should().Be("Mitsubishi");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSlug_WithEmptyValue_ThrowsArgumentException(string slug)
    {
        var brand = CreateValid();

        var act = () => brand.SetSlug(slug);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*slug cannot be empty*");
    }

    [Fact]
    public void SetSlug_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetSlug(new string('a', 101));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 100 characters*");
    }

    [Fact]
    public void SetSlug_NormalizesToLowerInvariant()
    {
        var brand = CreateValid();

        brand.SetSlug("  MITSUBISHI-Electric  ");

        brand.Slug.Should().Be("mitsubishi-electric");
    }

    [Fact]
    public void SetLogoUrl_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetLogoUrl(new string('a', 501));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Logo URL cannot exceed 500 characters*");
    }

    [Fact]
    public void SetLogoUrl_WithNull_IsAllowed()
    {
        var brand = CreateValid();

        brand.SetLogoUrl(null);

        brand.LogoUrl.Should().BeNull();
    }

    [Fact]
    public void SetBannerImageUrl_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetBannerImageUrl(new string('a', 501));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Banner image URL cannot exceed 500 characters*");
    }

    [Fact]
    public void SetWebsiteUrl_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetWebsiteUrl(new string('a', 501));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Website URL cannot exceed 500 characters*");
    }

    [Fact]
    public void SetCountryOfOrigin_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetCountryOfOrigin(new string('a', 101));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Country of origin cannot exceed 100 characters*");
    }

    [Theory]
    [InlineData(1799)]
    [InlineData(3000)]
    public void SetFoundedYear_OutOfRange_ThrowsArgumentException(int year)
    {
        var brand = CreateValid();

        var act = () => brand.SetFoundedYear(year);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Invalid founded year*");
    }

    [Fact]
    public void SetFoundedYear_WithinRange_SetsValue()
    {
        var brand = CreateValid();

        brand.SetFoundedYear(1924);

        brand.FoundedYear.Should().Be(1924);
    }

    [Fact]
    public void SetMetaTitle_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetMetaTitle(new string('a', 201));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta title cannot exceed 200 characters*");
    }

    [Fact]
    public void SetMetaDescription_ExceedingMaxLength_ThrowsArgumentException()
    {
        var brand = CreateValid();

        var act = () => brand.SetMetaDescription(new string('a', 501));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta description cannot exceed 500 characters*");
    }

    [Fact]
    public void SetFeaturedAndSortOrder_UpdateProperties()
    {
        var brand = CreateValid();

        brand.SetFeatured(true);
        brand.SetSortOrder(3);
        brand.SetActive(false);

        brand.IsFeatured.Should().BeTrue();
        brand.SortOrder.Should().Be(3);
        brand.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GetTranslatedContent_WithNullLanguage_ReturnsBaseContent()
    {
        var brand = CreateValid();
        brand.SetDescription("Base description");
        brand.SetMetaTitle("Base title");

        var (name, description, metaTitle, _) = brand.GetTranslatedContent(null);

        name.Should().Be("Daikin");
        description.Should().Be("Base description");
        metaTitle.Should().Be("Base title");
    }

    [Fact]
    public void GetTranslatedContent_WithMissingTranslation_FallsBackToBase()
    {
        var brand = CreateValid();

        var (name, _, _, _) = brand.GetTranslatedContent("bg");

        name.Should().Be("Daikin");
    }

    [Fact]
    public void GetTranslatedContent_WithTranslation_ReturnsTranslatedFieldsWithFallback()
    {
        var brand = CreateValid();
        brand.SetDescription("Base description");
        brand.Translations.Add(new BrandTranslation
        {
            LanguageCode = "de",
            Name = "Daikin DE",
            Description = null,
            MetaTitle = "DE Title",
            MetaDescription = "DE Desc"
        });

        var (name, description, metaTitle, metaDescription) = brand.GetTranslatedContent("DE");

        name.Should().Be("Daikin DE");
        description.Should().Be("Base description"); // null falls back
        metaTitle.Should().Be("DE Title");
        metaDescription.Should().Be("DE Desc");
    }
}
