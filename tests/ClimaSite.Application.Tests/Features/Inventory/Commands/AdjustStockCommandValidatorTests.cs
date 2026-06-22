using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Features.Inventory.DTOs;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Inventory.Commands;

// NOTE: AdjustStockCommandHandler itself is NOT unit-tested here. Its Handle() calls
// _context.ProductVariants.Where(...).ExecuteUpdateAsync(...), which is an EF Core
// *relational* extension that requires a real database provider. Under MockDbContext
// (in-memory LINQ provider) it throws
//   "There is no method 'ExecuteUpdate' on type EntityFrameworkQueryableExtensions".
// This was verified with a probe test, so the handler is deliberately skipped (see notes).
// The validator below is pure (no DB) and is fully covered.
public class AdjustStockCommandValidatorTests
{
    private readonly AdjustStockCommandValidator _validator = new();

    private static AdjustStockCommand ValidCommand() => new()
    {
        VariantId = Guid.NewGuid(),
        QuantityChange = 5,
        Reason = StockAdjustmentReason.Received,
        Notes = "Restocked"
    };

    [Fact]
    public void Validate_WithAllValidFields_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllowsNullNotes()
    {
        var command = ValidCommand() with { Notes = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(5)]   // increment
    [InlineData(-3)]  // decrement
    public void Validate_AcceptsBothIncrementAndDecrement(int change)
    {
        var command = ValidCommand() with { QuantityChange = change };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenVariantIdEmpty_Fails()
    {
        var command = ValidCommand() with { VariantId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AdjustStockCommand.VariantId));
    }

    [Fact]
    public void Validate_WhenQuantityChangeIsZero_Fails()
    {
        var command = ValidCommand() with { QuantityChange = 0 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(AdjustStockCommand.QuantityChange)
            && e.ErrorMessage == "Quantity change cannot be zero");
    }

    [Fact]
    public void Validate_WhenReasonNotInEnum_Fails()
    {
        var command = ValidCommand() with { Reason = (StockAdjustmentReason)999 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AdjustStockCommand.Reason));
    }

    [Fact]
    public void Validate_WhenNotesExceed500Chars_Fails()
    {
        var command = ValidCommand() with { Notes = new string('x', 501) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(AdjustStockCommand.Notes)
            && e.ErrorMessage == "Notes cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WhenNotesExactly500Chars_Passes()
    {
        var command = ValidCommand() with { Notes = new string('x', 500) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
