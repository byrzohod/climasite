using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductTranslationTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTranslation()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var translation = new ProductTranslation(productId, "bg", "Test Product BG");

        // Assert
        translation.ProductId.Should().Be(productId);
        translation.LanguageCode.Should().Be("bg");
        translation.Name.Should().Be("Test Product BG");
    }

    [Fact]
    public void LanguageCode_NormalizesToLowerCase()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var translation = new ProductTranslation(productId, "BG", "Test Product BG");

        // Assert
        translation.LanguageCode.Should().Be("bg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LanguageCode_WithEmptyValue_ThrowsArgumentException(string languageCode)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ProductTranslation(productId, languageCode, "Test");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Language code is required*");
    }

    [Theory]
    [InlineData("b")]
    [InlineData("bul")]
    [InlineData("bulgarian")]
    public void LanguageCode_WithInvalidLength_ThrowsArgumentException(string languageCode)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ProductTranslation(productId, languageCode, "Test");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Language code must be 2 characters*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_WithEmptyValue_ThrowsArgumentException(string name)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ProductTranslation(productId, "bg", name);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name is required*");
    }

    [Fact]
    public void Name_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var longName = new string('A', 256);

        // Act & Assert
        var act = () => new ProductTranslation(productId, "bg", longName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name cannot exceed 255 characters*");
    }

    [Fact]
    public void Name_TrimsWhitespace()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var translation = new ProductTranslation(productId, "bg", "  Test Product BG  ");

        // Assert
        translation.Name.Should().Be("Test Product BG");
    }

    [Fact]
    public void OptionalFields_CanBeSet()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var translation = new ProductTranslation(productId, "bg", "Test Product BG");

        // Act
        translation.ShortDescription = "Short description in Bulgarian";
        translation.Description = "Full description in Bulgarian";
        translation.MetaTitle = "Meta Title BG";
        translation.MetaDescription = "Meta Description BG";

        // Assert
        translation.ShortDescription.Should().Be("Short description in Bulgarian");
        translation.Description.Should().Be("Full description in Bulgarian");
        translation.MetaTitle.Should().Be("Meta Title BG");
        translation.MetaDescription.Should().Be("Meta Description BG");
    }

    [Fact]
    public void OptionalFields_CanBeNull()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var translation = new ProductTranslation(productId, "bg", "Test Product BG");

        // Assert
        translation.ShortDescription.Should().BeNull();
        translation.Description.Should().BeNull();
        translation.MetaTitle.Should().BeNull();
        translation.MetaDescription.Should().BeNull();
    }
}
