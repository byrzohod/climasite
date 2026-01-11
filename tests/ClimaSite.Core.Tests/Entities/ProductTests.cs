using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductTests
{
    private Product CreateValidProduct(
        string sku = "TEST-SKU-001",
        string name = "Test Product",
        string slug = "test-product",
        decimal basePrice = 999.99m) =>
        new(sku, name, slug, basePrice);

    [Fact]
    public void Constructor_WithValidData_CreatesProduct()
    {
        // Arrange & Act
        var product = CreateValidProduct();

        // Assert
        product.Sku.Should().Be("TEST-SKU-001");
        product.Name.Should().Be("Test Product");
        product.Slug.Should().Be("test-product");
        product.BasePrice.Should().Be(999.99m);
        product.IsActive.Should().BeTrue();
        product.WarrantyMonths.Should().Be(12);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSku_WithEmptyValue_ThrowsArgumentException(string sku)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetSku(sku);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*SKU cannot be empty*");
    }

    [Fact]
    public void SetSku_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longSku = new string('A', 51);

        // Act & Assert
        var act = () => product.SetSku(longSku);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*SKU cannot exceed 50 characters*");
    }

    [Fact]
    public void SetSku_NormalizesToUpperCase()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetSku("abc-123");

        // Assert
        product.Sku.Should().Be("ABC-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetName(name);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product name cannot be empty*");
    }

    [Fact]
    public void SetName_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longName = new string('A', 256);

        // Act & Assert
        var act = () => product.SetName(longName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product name cannot exceed 255 characters*");
    }

    [Fact]
    public void SetName_TrimsWhitespace()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetName("  Test Product  ");

        // Assert
        product.Name.Should().Be("Test Product");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSlug_WithEmptyValue_ThrowsArgumentException(string slug)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetSlug(slug);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product slug cannot be empty*");
    }

    [Fact]
    public void SetSlug_NormalizesToLowerCase()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetSlug("Test-Product-SLUG");

        // Assert
        product.Slug.Should().Be("test-product-slug");
    }

    [Fact]
    public void SetBasePrice_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetBasePrice(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Base price cannot be negative*");
    }

    [Fact]
    public void SetBasePrice_WithZero_IsAllowed()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetBasePrice(0);

        // Assert
        product.BasePrice.Should().Be(0);
    }

    [Fact]
    public void SetCompareAtPrice_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetCompareAtPrice(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Compare at price cannot be negative*");
    }

    [Fact]
    public void SetCompareAtPrice_WithNull_IsAllowed()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetCompareAtPrice(1000m);

        // Act
        product.SetCompareAtPrice(null);

        // Assert
        product.CompareAtPrice.Should().BeNull();
    }

    [Fact]
    public void SetShortDescription_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longDescription = new string('A', 501);

        // Act & Assert
        var act = () => product.SetShortDescription(longDescription);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Short description cannot exceed 500 characters*");
    }

    [Fact]
    public void SetBrand_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longBrand = new string('A', 101);

        // Act & Assert
        var act = () => product.SetBrand(longBrand);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Brand cannot exceed 100 characters*");
    }

    [Fact]
    public void SetModel_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longModel = new string('A', 101);

        // Act & Assert
        var act = () => product.SetModel(longModel);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Model cannot exceed 100 characters*");
    }

    [Fact]
    public void SetWarrantyMonths_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetWarrantyMonths(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Warranty months cannot be negative*");
    }

    [Fact]
    public void SetWeightKg_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var act = () => product.SetWeightKg(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Weight cannot be negative*");
    }

    [Fact]
    public void SetMetaTitle_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longTitle = new string('A', 201);

        // Act & Assert
        var act = () => product.SetMetaTitle(longTitle);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta title cannot exceed 200 characters*");
    }

    [Fact]
    public void SetMetaDescription_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var product = CreateValidProduct();
        var longDescription = new string('A', 501);

        // Act & Assert
        var act = () => product.SetMetaDescription(longDescription);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta description cannot exceed 500 characters*");
    }

    [Fact]
    public void IsOnSale_WhenCompareAtPriceIsHigherThanBasePrice_ReturnsTrue()
    {
        // Arrange
        var product = CreateValidProduct(basePrice: 799.99m);
        product.SetCompareAtPrice(999.99m);

        // Assert
        product.IsOnSale.Should().BeTrue();
    }

    [Fact]
    public void IsOnSale_WhenCompareAtPriceIsNull_ReturnsFalse()
    {
        // Arrange
        var product = CreateValidProduct();

        // Assert
        product.IsOnSale.Should().BeFalse();
    }

    [Fact]
    public void IsOnSale_WhenCompareAtPriceIsLowerThanBasePrice_ReturnsFalse()
    {
        // Arrange
        var product = CreateValidProduct(basePrice: 999.99m);
        product.SetCompareAtPrice(799.99m);

        // Assert
        product.IsOnSale.Should().BeFalse();
    }

    [Fact]
    public void DiscountPercentage_CalculatesCorrectly()
    {
        // Arrange
        var product = CreateValidProduct(basePrice: 800m);
        product.SetCompareAtPrice(1000m);

        // Assert
        product.DiscountPercentage.Should().Be(20m);
    }

    [Fact]
    public void DiscountPercentage_WhenNotOnSale_ReturnsNull()
    {
        // Arrange
        var product = CreateValidProduct();

        // Assert
        product.DiscountPercentage.Should().BeNull();
    }

    [Fact]
    public void SetTags_NormalizesToLowerCase()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetTags(new List<string> { "HVAC", "Inverter", "WIFI" });

        // Assert
        product.Tags.Should().BeEquivalentTo(new[] { "hvac", "inverter", "wifi" });
    }

    [Fact]
    public void SetTags_RemovesDuplicates()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetTags(new List<string> { "hvac", "HVAC", "  hvac  " });

        // Assert
        product.Tags.Should().HaveCount(1);
        product.Tags.Should().Contain("hvac");
    }

    [Fact]
    public void AddTag_AddsNewTag()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetTags(new List<string> { "hvac" });

        // Act
        product.AddTag("inverter");

        // Assert
        product.Tags.Should().HaveCount(2);
        product.Tags.Should().Contain("inverter");
    }

    [Fact]
    public void AddTag_DoesNotAddDuplicate()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetTags(new List<string> { "hvac" });

        // Act
        product.AddTag("HVAC");

        // Assert
        product.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void AddFeature_AddsNewFeature()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.AddFeature("WiFi Control", "Control from anywhere", "wifi");

        // Assert
        product.Features.Should().HaveCount(1);
        product.Features[0].Title.Should().Be("WiFi Control");
        product.Features[0].Description.Should().Be("Control from anywhere");
        product.Features[0].Icon.Should().Be("wifi");
    }

    [Fact]
    public void SetSpecification_AddsOrUpdatesValue()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.SetSpecification("btu", 12000);
        product.SetSpecification("energyRating", "A++");

        // Assert
        product.Specifications.Should().HaveCount(2);
        product.Specifications["btu"].Should().Be(12000);
        product.Specifications["energyRating"].Should().Be("A++");
    }

    [Fact]
    public void SetActive_UpdatesIsActive()
    {
        // Arrange
        var product = CreateValidProduct();
        product.IsActive.Should().BeTrue();

        // Act
        product.SetActive(false);

        // Assert
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetFeatured_UpdatesIsFeatured()
    {
        // Arrange
        var product = CreateValidProduct();
        product.IsFeatured.Should().BeFalse();

        // Act
        product.SetFeatured(true);

        // Assert
        product.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void SetRequiresInstallation_UpdatesRequiresInstallation()
    {
        // Arrange
        var product = CreateValidProduct();
        product.RequiresInstallation.Should().BeFalse();

        // Act
        product.SetRequiresInstallation(true);

        // Assert
        product.RequiresInstallation.Should().BeTrue();
    }

    #region GetTranslatedContent Tests

    [Fact]
    public void GetTranslatedContent_WithNullLanguageCode_ReturnsDefaultContent()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetShortDescription("Short description in English");
        product.SetDescription("Full description in English");
        product.SetMetaTitle("Meta Title");
        product.SetMetaDescription("Meta Description");

        // Act
        var (name, shortDesc, desc, metaTitle, metaDesc) = product.GetTranslatedContent(null);

        // Assert
        name.Should().Be("Test Product");
        shortDesc.Should().Be("Short description in English");
        desc.Should().Be("Full description in English");
        metaTitle.Should().Be("Meta Title");
        metaDesc.Should().Be("Meta Description");
    }

    [Fact]
    public void GetTranslatedContent_WithEnglishLanguageCode_ReturnsDefaultContent()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetShortDescription("Short description in English");
        product.SetDescription("Full description in English");

        // Act
        var (name, shortDesc, desc, _, _) = product.GetTranslatedContent("en");

        // Assert
        name.Should().Be("Test Product");
        shortDesc.Should().Be("Short description in English");
        desc.Should().Be("Full description in English");
    }

    [Fact]
    public void GetTranslatedContent_WithEnglishUpperCase_ReturnsDefaultContent()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetShortDescription("Short description in English");

        // Act
        var (name, shortDesc, _, _, _) = product.GetTranslatedContent("EN");

        // Assert
        name.Should().Be("Test Product");
        shortDesc.Should().Be("Short description in English");
    }

    [Fact]
    public void GetTranslatedContent_WithNoMatchingTranslation_ReturnsDefaultContent()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetShortDescription("Short description in English");
        product.SetDescription("Full description in English");

        // Act
        var (name, shortDesc, desc, _, _) = product.GetTranslatedContent("fr");

        // Assert
        name.Should().Be("Test Product");
        shortDesc.Should().Be("Short description in English");
        desc.Should().Be("Full description in English");
    }

    #endregion
}
