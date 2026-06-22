using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Features.Inventory.DTOs;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Inventory.Commands;

public class BulkAdjustStockCommandValidatorTests
{
    private readonly BulkAdjustStockCommandValidator _validator = new();

    private static BulkAdjustStockCommand ValidCommand() => new()
    {
        Reason = StockAdjustmentReason.Received,
        Adjustments =
        [
            new StockAdjustmentItem { VariantId = Guid.NewGuid(), NewQuantity = 10 }
        ]
    };

    [Fact]
    public void Validate_WithValidAdjustments_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenAdjustmentsEmpty_Fails()
    {
        var command = ValidCommand() with { Adjustments = [] };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "At least one adjustment is required");
    }

    [Fact]
    public void Validate_WhenMoreThan100Adjustments_Fails()
    {
        var command = ValidCommand() with
        {
            Adjustments = Enumerable.Range(0, 101)
                .Select(_ => new StockAdjustmentItem { VariantId = Guid.NewGuid(), NewQuantity = 1 })
                .ToList()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Cannot process more than 100 adjustments at once");
    }

    [Fact]
    public void Validate_WhenExactly100Adjustments_Passes()
    {
        var command = ValidCommand() with
        {
            Adjustments = Enumerable.Range(0, 100)
                .Select(_ => new StockAdjustmentItem { VariantId = Guid.NewGuid(), NewQuantity = 1 })
                .ToList()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenItemVariantIdEmpty_Fails()
    {
        var command = ValidCommand() with
        {
            Adjustments = [new StockAdjustmentItem { VariantId = Guid.Empty, NewQuantity = 5 }]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Variant ID is required");
    }

    [Fact]
    public void Validate_WhenItemQuantityNegative_Fails()
    {
        var command = ValidCommand() with
        {
            Adjustments = [new StockAdjustmentItem { VariantId = Guid.NewGuid(), NewQuantity = -1 }]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity cannot be negative");
    }

    [Fact]
    public void Validate_WhenReasonNotInEnum_Fails()
    {
        var command = ValidCommand() with { Reason = (StockAdjustmentReason)999 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(BulkAdjustStockCommand.Reason));
    }
}
