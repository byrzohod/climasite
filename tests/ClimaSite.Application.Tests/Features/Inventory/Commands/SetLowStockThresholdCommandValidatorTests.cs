using ClimaSite.Application.Features.Inventory.Commands;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Inventory.Commands;

public class SetLowStockThresholdCommandValidatorTests
{
    private readonly SetLowStockThresholdCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidFields_Passes()
    {
        var command = new SetLowStockThresholdCommand
        {
            VariantId = Guid.NewGuid(),
            Threshold = 5
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllowsZeroThreshold()
    {
        var command = new SetLowStockThresholdCommand
        {
            VariantId = Guid.NewGuid(),
            Threshold = 0
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenVariantIdEmpty_Fails()
    {
        var command = new SetLowStockThresholdCommand
        {
            VariantId = Guid.Empty,
            Threshold = 5
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetLowStockThresholdCommand.VariantId));
    }

    [Fact]
    public void Validate_WhenThresholdNegative_Fails()
    {
        var command = new SetLowStockThresholdCommand
        {
            VariantId = Guid.NewGuid(),
            Threshold = -1
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetLowStockThresholdCommand.Threshold)
            && e.ErrorMessage == "Threshold cannot be negative");
    }
}
