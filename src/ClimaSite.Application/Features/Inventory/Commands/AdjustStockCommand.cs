using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Inventory.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Inventory.Commands;

public record AdjustStockCommand : IRequest<Result>
{
    public Guid VariantId { get; init; }
    public int QuantityChange { get; init; }
    public StockAdjustmentReason Reason { get; init; }
    public string? Notes { get; init; }
}

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID is required");

        RuleFor(x => x.QuantityChange)
            .NotEqual(0).WithMessage("Quantity change cannot be zero");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Invalid adjustment reason");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AdjustStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        // BUG-05: adjust stock atomically with a non-negative guard so concurrent
        // adjustments can't race the old read-then-write into a wrong/negative quantity.
        var affected = await _context.ProductVariants
            .Where(v => v.Id == request.VariantId && v.StockQuantity + request.QuantityChange >= 0)
            .ExecuteUpdateAsync(
                s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + request.QuantityChange),
                cancellationToken);

        if (affected > 0)
        {
            return Result.Success();
        }

        // No row updated: either the variant doesn't exist or the change would go below zero.
        var exists = await _context.ProductVariants
            .AnyAsync(v => v.Id == request.VariantId, cancellationToken);
        return exists
            ? Result.Failure("Cannot reduce stock below zero.")
            : Result.Failure("Product variant not found");
    }
}
