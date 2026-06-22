using ClimaSite.Application.Features.Categories.Commands;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    private static CreateCategoryCommand Valid() => new()
    {
        Name = "Air Conditioners",
        Description = "Cooling units",
        Icon = "ac",
        ImageUrl = "https://cdn.test/ac.png",
        MetaTitle = "AC",
        MetaDescription = "Browse"
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_Fails(string name)
    {
        var result = _validator.Validate(Valid() with { Name = name });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = new string('a', 101) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Fact]
    public void Validate_NameAtMaxLength_Passes()
    {
        var result = _validator.Validate(Valid() with { Name = new string('a', 100) });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ImageUrlTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { ImageUrl = new string('a', 501) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.ImageUrl));
    }

    [Fact]
    public void Validate_IconTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Icon = new string('a', 51) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.Icon));
    }

    [Fact]
    public void Validate_MetaTitleTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaTitle = new string('a', 201) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.MetaTitle));
    }

    [Fact]
    public void Validate_MetaDescriptionTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaDescription = new string('a', 501) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.MetaDescription));
    }

    [Fact]
    public void Validate_OptionalFieldsNull_Passes()
    {
        var command = Valid() with { ImageUrl = null, Icon = null, MetaTitle = null, MetaDescription = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
