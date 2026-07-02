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

        // Process in ascending variant_id order (lock-ordering consistency with the reservation loops).
        foreach (var adjustment in request.Adjustments.OrderBy(a => a.VariantId))
        {
            if (!variants.TryGetValue(adjustment.VariantId, out var variant))
            {
                errors.Add($"Variant {adjustment.VariantId} not found");
                continue;
            }

            // INV-01 A2: the WRITE is a single atomic reserved-aware set — never drop stock below the units held by
            // open checkouts (reserved_quantity), which would strand a card holder's consume (charge-then-refund).
            // rows==0 ⇒ the set would go below the held units (the guard re-checks reserved AT write time, so a
            // reserve committing after the load above can't sneak the stock below reserved). Skip + report per line.
            var rows = await _context.TrySetVariantStockAtOrAboveReservedAsync(
                adjustment.VariantId, adjustment.NewQuantity, cancellationToken);
            if (rows > 0)
            {
                successCount++;
            }
            else
            {
                errors.Add(
                    $"Variant {adjustment.VariantId}: cannot set stock below the {variant.ReservedQuantity} unit(s) currently held by open checkouts");
            }
        }

        return Result<BulkAdjustStockResult>.Success(new BulkAdjustStockResult
        {
            SuccessCount = successCount,
            FailureCount = errors.Count,
            Errors = errors
        });
    }
}
