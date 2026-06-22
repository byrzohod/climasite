using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Inventory.Commands;

public class SetLowStockThresholdCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private SetLowStockThresholdCommandHandler CreateHandler() => new(_context);

    private ProductVariant SeedVariant(int threshold = 5)
    {
        var variant = new ProductVariant(Guid.NewGuid(), "THRESH-SKU", "Threshold Variant");
        variant.SetLowStockThreshold(threshold);
        _context.ProductVariants.Add(variant);
        return variant;
    }

    [Fact]
    public async Task Handle_UpdatesThreshold_WhenVariantExists()
    {
        var variant = SeedVariant(threshold: 5);

        var result = await CreateHandler().Handle(new SetLowStockThresholdCommand
        {
            VariantId = variant.Id,
            Threshold = 12
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        variant.LowStockThreshold.Should().Be(12);
    }

    [Fact]
    public async Task Handle_AllowsZeroThreshold()
    {
        var variant = SeedVariant(threshold: 8);

        var result = await CreateHandler().Handle(new SetLowStockThresholdCommand
        {
            VariantId = variant.Id,
            Threshold = 0
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        variant.LowStockThreshold.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenVariantNotFound()
    {
        SeedVariant();

        var result = await CreateHandler().Handle(new SetLowStockThresholdCommand
        {
            VariantId = Guid.NewGuid(),
            Threshold = 10
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product variant not found");
    }
}
