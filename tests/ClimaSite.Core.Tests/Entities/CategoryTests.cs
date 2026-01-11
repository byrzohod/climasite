using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class CategoryTests
{
    private Category CreateValidCategory(
        string name = "Air Conditioners",
        string slug = "air-conditioners",
        string? description = null) =>
        new(name, slug, description);

    [Fact]
    public void Constructor_WithValidData_CreatesCategory()
    {
        // Arrange & Act
        var category = CreateValidCategory();

        // Assert
        category.Name.Should().Be("Air Conditioners");
        category.Slug.Should().Be("air-conditioners");
        category.IsActive.Should().BeTrue();
        category.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        // Arrange & Act
        var category = CreateValidCategory(description: "All air conditioning units");

        // Assert
        category.Description.Should().Be("All air conditioning units");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        // Arrange
        var category = CreateValidCategory();

        // Act & Assert
        var act = () => category.SetName(name);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Category name cannot be empty*");
    }

    [Fact]
    public void SetName_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longName = new string('A', 101);

        // Act & Assert
        var act = () => category.SetName(longName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Category name cannot exceed 100 characters*");
    }

    [Fact]
    public void SetName_TrimsWhitespace()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act
        category.SetName("  Heating Systems  ");

        // Assert
        category.Name.Should().Be("Heating Systems");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSlug_WithEmptyValue_ThrowsArgumentException(string slug)
    {
        // Arrange
        var category = CreateValidCategory();

        // Act & Assert
        var act = () => category.SetSlug(slug);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Category slug cannot be empty*");
    }

    [Fact]
    public void SetSlug_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longSlug = new string('a', 101);

        // Act & Assert
        var act = () => category.SetSlug(longSlug);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Category slug cannot exceed 100 characters*");
    }

    [Fact]
    public void SetSlug_NormalizesToLowerCase()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act
        category.SetSlug("Heating-SYSTEMS");

        // Assert
        category.Slug.Should().Be("heating-systems");
    }

    [Fact]
    public void SetParent_WithOwnId_ThrowsInvalidOperationException()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act & Assert
        var act = () => category.SetParent(category.Id);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*category cannot be its own parent*");
    }

    [Fact]
    public void SetParent_WithDifferentId_SetsParentId()
    {
        // Arrange
        var category = CreateValidCategory();
        var parentId = Guid.NewGuid();

        // Act
        category.SetParent(parentId);

        // Assert
        category.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void SetParent_WithNull_ClearsParentId()
    {
        // Arrange
        var category = CreateValidCategory();
        category.SetParent(Guid.NewGuid());

        // Act
        category.SetParent(null);

        // Assert
        category.ParentId.Should().BeNull();
    }

    [Fact]
    public void SetImageUrl_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longUrl = new string('a', 501);

        // Act & Assert
        var act = () => category.SetImageUrl(longUrl);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Image URL cannot exceed 500 characters*");
    }

    [Fact]
    public void SetIcon_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longIcon = new string('a', 51);

        // Act & Assert
        var act = () => category.SetIcon(longIcon);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Icon cannot exceed 50 characters*");
    }

    [Fact]
    public void SetMetaTitle_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longTitle = new string('A', 201);

        // Act & Assert
        var act = () => category.SetMetaTitle(longTitle);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta title cannot exceed 200 characters*");
    }

    [Fact]
    public void SetMetaDescription_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var category = CreateValidCategory();
        var longDescription = new string('A', 501);

        // Act & Assert
        var act = () => category.SetMetaDescription(longDescription);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Meta description cannot exceed 500 characters*");
    }

    [Fact]
    public void SetActive_UpdatesIsActive()
    {
        // Arrange
        var category = CreateValidCategory();
        category.IsActive.Should().BeTrue();

        // Act
        category.SetActive(false);

        // Assert
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetSortOrder_UpdatesSortOrder()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act
        category.SetSortOrder(5);

        // Assert
        category.SortOrder.Should().Be(5);
    }

    [Fact]
    public void SetDescription_TrimsWhitespace()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act
        category.SetDescription("  Test description  ");

        // Assert
        category.Description.Should().Be("Test description");
    }

    [Fact]
    public void SetDescription_WithNull_SetsToNull()
    {
        // Arrange
        var category = CreateValidCategory(description: "Some description");

        // Act
        category.SetDescription(null);

        // Assert
        category.Description.Should().BeNull();
    }
}
