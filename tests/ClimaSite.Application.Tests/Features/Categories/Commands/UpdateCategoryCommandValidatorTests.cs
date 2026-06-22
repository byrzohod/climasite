using ClimaSite.Application.Features.Categories.Commands;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    private static UpdateCategoryCommand Valid() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Updated Name"
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(Valid() with { Id = Guid.Empty });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.Id));
    }

    [Fact]
    public void Validate_NullName_Passes()
    {
        // Name is optional on update; null skips the length rule.
        var result = _validator.Validate(Valid() with { Name = null });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = new string('a', 101) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.Name));
    }

    [Fact]
    public void Validate_ImageUrlTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { ImageUrl = new string('a', 501) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.ImageUrl));
    }

    [Fact]
    public void Validate_IconTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Icon = new string('a', 51) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.Icon));
    }

    [Fact]
    public void Validate_MetaTitleTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaTitle = new string('a', 201) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.MetaTitle));
    }

    [Fact]
    public void Validate_MetaDescriptionTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaDescription = new string('a', 501) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryCommand.MetaDescription));
    }

    [Fact]
    public void Validate_OptionalFieldsNull_Passes()
    {
        var command = Valid() with { Name = null, ImageUrl = null, Icon = null, MetaTitle = null, MetaDescription = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
