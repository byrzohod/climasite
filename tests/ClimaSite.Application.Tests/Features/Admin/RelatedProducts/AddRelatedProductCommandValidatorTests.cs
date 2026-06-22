using ClimaSite.Application.Features.Admin.RelatedProducts.Commands;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.RelatedProducts;

public class AddRelatedProductCommandValidatorTests
{
    private readonly AddRelatedProductCommandValidator _validator = new();

    [Theory]
    [InlineData(nameof(RelationType.Similar))]
    [InlineData(nameof(RelationType.Accessory))]
    [InlineData(nameof(RelationType.Bundle))]
    [InlineData("similar")]   // case-insensitive parse
    public void Validate_ValidCommand_Passes(string relationType)
    {
        var command = new AddRelatedProductCommand(Guid.NewGuid(), Guid.NewGuid(), relationType);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyProductId_FailsWithProductIdError()
    {
        var command = new AddRelatedProductCommand(
            Guid.Empty, Guid.NewGuid(), nameof(RelationType.Similar));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddRelatedProductCommand.ProductId));
    }

    [Fact]
    public void Validate_EmptyRelatedProductId_FailsWithRelatedProductIdError()
    {
        var command = new AddRelatedProductCommand(
            Guid.NewGuid(), Guid.Empty, nameof(RelationType.Similar));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddRelatedProductCommand.RelatedProductId));
    }

    [Fact]
    public void Validate_EmptyRelationType_FailsWithRelationTypeError()
    {
        var command = new AddRelatedProductCommand(Guid.NewGuid(), Guid.NewGuid(), "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddRelatedProductCommand.RelationType));
    }

    [Theory]
    [InlineData("NotAType")]
    [InlineData("Friend")]
    public void Validate_InvalidRelationType_FailsWithRelationTypeError(string relationType)
    {
        var command = new AddRelatedProductCommand(Guid.NewGuid(), Guid.NewGuid(), relationType);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(AddRelatedProductCommand.RelationType));
    }

    [Fact]
    public void Validate_ProductRelatedToItself_FailsWithProductIdError()
    {
        var id = Guid.NewGuid();
        var command = new AddRelatedProductCommand(id, id, nameof(RelationType.Similar));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "A product cannot be related to itself");
    }
}
