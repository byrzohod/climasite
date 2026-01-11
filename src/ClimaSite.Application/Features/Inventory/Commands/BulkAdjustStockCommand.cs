using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Inventory.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Inventory.Commands;

public record BulkAdjustStockCommand : IRequest<Result<BulkAdjustStockResult>>
{
    public List<StockAdjustmentItem> Adjustments { get; init; } = [];
    public StockAdjustmentReason Reason { get; init; }
    public string? Notes { get; init; }
}

public record StockAdjustmentItem
{
    public Guid VariantId { get; init; }
    public int NewQuantity { get; init; }
}

public record BulkAdjustStockResult
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<string> Errors { get; init; } = [];
}

public class BulkAdjustStockCommandValidator : AbstractValidator<BulkAdjustStockCommand>
{
    public BulkAdjustStockCommandValidator()
    {
        RuleFor(x => x.Adjustments)
            .NotEmpty().WithMessage("At least one adjustment is required")
            .Must(a => a.Count <= 100).WithMessage("Cannot process more than 100 adjustments at once");

        RuleForEach(x => x.Adjustments).ChildRules(item =>
        {
            item.RuleFor(x => x.VariantId).NotEmpty().WithMessage("Variant ID is required");
            item.RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative");
        });

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Invalid adjustment reason");
    }
}

public class BulkAdjustStockCommandHandler : IRequestHandler<BulkAdjustStockCommand, Result<BulkAdjustStockResult>>
{
    private readonly IApplicationDbContext _context;

    public BulkAdjustStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BulkAdjustStockResult>> Handle(BulkAdjustStockCommand request, CancellationToken cancellationToken)
    {
        var variantIds = request.Adjustments.Select(a => a.VariantId).ToList();
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var successCount = 0;
        var errors = new List<string>();

        foreach (var adjustment in request.Adjustments)
        {
            if (!variants.TryGetValue(adjustment.VariantId, out var variant))
            {
                errors.Add($"Variant {adjustment.VariantId} not found");
                continue;
            }

            try
            {
                variant.SetStockQuantity(adjustment.NewQuantity);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Variant {adjustment.VariantId}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<BulkAdjustStockResult>.Success(new BulkAdjustStockResult
        {
            SuccessCount = successCount,
            FailureCount = errors.Count,
            Errors = errors
        });
    }
}
