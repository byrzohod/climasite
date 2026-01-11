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
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant == null)
        {
            return Result.Failure("Product variant not found");
        }

        var newQuantity = variant.StockQuantity + request.QuantityChange;
        if (newQuantity < 0)
        {
            return Result.Failure($"Cannot reduce stock below zero. Current stock: {variant.StockQuantity}");
        }

        variant.SetStockQuantity(newQuantity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
