using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Features.Inventory.DTOs;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Inventory.Commands;

public class BulkAdjustStockCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private BulkAdjustStockCommandHandler CreateHandler() => new(_context);

    private ProductVariant SeedVariant(string sku, int stock)
    {
        var variant = new ProductVariant(Guid.NewGuid(), sku, $"Variant {sku}");
        variant.SetStockQuantity(stock);
        _context.ProductVariants.Add(variant);
        return variant;
    }

    [Fact]
    public async Task Handle_SetsNewQuantitiesForAllVariants()
    {
        var a = SeedVariant("BULK-A", stock: 10);
        var b = SeedVariant("BULK-B", stock: 3);

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Correction,
            Adjustments =
            [
                new StockAdjustmentItem { VariantId = a.Id, NewQuantity = 25 },
                new StockAdjustmentItem { VariantId = b.Id, NewQuantity = 0 }
            ]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(2);
        result.Value.FailureCount.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();

        a.StockQuantity.Should().Be(25); // increment to a higher level
        b.StockQuantity.Should().Be(0);  // decrement to zero
    }

    [Fact]
    public async Task Handle_PartialSuccess_WhenSomeVariantsMissing()
    {
        var a = SeedVariant("BULK-PART", stock: 5);
        var missingId = Guid.NewGuid();

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Received,
            Adjustments =
            [
                new StockAdjustmentItem { VariantId = a.Id, NewQuantity = 12 },
                new StockAdjustmentItem { VariantId = missingId, NewQuantity = 7 }
            ]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        result.Value.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString())
            .And.Contain("not found");

        a.StockQuantity.Should().Be(12);
    }

    [Fact]
    public async Task Handle_AllVariantsMissing_ReturnsZeroSuccessAndAllErrors()
    {
        var firstMissing = Guid.NewGuid();
        var secondMissing = Guid.NewGuid();

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Lost,
            Adjustments =
            [
                new StockAdjustmentItem { VariantId = firstMissing, NewQuantity = 1 },
                new StockAdjustmentItem { VariantId = secondMissing, NewQuantity = 2 }
            ]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_RejectsSettingStockBelowReservedUnits()
    {
        // INV-01 A2 (council High #2): an absolute set below the units currently held by open checkouts
        // (reserved_quantity) must be skipped + reported, never applied — it would strand a card holder's consume.
        var v = SeedVariant("BULK-RSV", stock: 10);
        v.SetReservedQuantity(3); // 3 units held by open checkouts

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Correction,
            Adjustments = [new StockAdjustmentItem { VariantId = v.Id, NewQuantity = 2 }] // below reserved
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(1);
        result.Value.Errors.Should().ContainSingle().Which.Should().Contain("held by open checkouts");
        v.StockQuantity.Should().Be(10); // unchanged
    }

    [Fact]
    public async Task Handle_AllowsSettingStockDownToReservedUnits()
    {
        var v = SeedVariant("BULK-RSV-OK", stock: 10);
        v.SetReservedQuantity(3);

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Correction,
            Adjustments = [new StockAdjustmentItem { VariantId = v.Id, NewQuantity = 3 }] // exactly at reserved
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(1);
        v.StockQuantity.Should().Be(3);
    }

    [Fact]
    public async Task Handle_DecrementToZero_IsAllowed()
    {
        var a = SeedVariant("BULK-ZERO", stock: 50);

        var result = await CreateHandler().Handle(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Damaged,
            Adjustments = [new StockAdjustmentItem { VariantId = a.Id, NewQuantity = 0 }]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuccessCount.Should().Be(1);
        a.StockQuantity.Should().Be(0);
    }
}
