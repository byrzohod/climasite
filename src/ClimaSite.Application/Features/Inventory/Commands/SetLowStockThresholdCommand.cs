using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Inventory.Commands;

public record SetLowStockThresholdCommand : IRequest<Result>
{
    public Guid VariantId { get; init; }
    public int Threshold { get; init; }
}

public class SetLowStockThresholdCommandValidator : AbstractValidator<SetLowStockThresholdCommand>
{
    public SetLowStockThresholdCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID is required");

        RuleFor(x => x.Threshold)
            .GreaterThanOrEqualTo(0).WithMessage("Threshold cannot be negative");
    }
}

public class SetLowStockThresholdCommandHandler : IRequestHandler<SetLowStockThresholdCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public SetLowStockThresholdCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(SetLowStockThresholdCommand request, CancellationToken cancellationToken)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant == null)
        {
            return Result.Failure("Product variant not found");
        }

        variant.SetLowStockThreshold(request.Threshold);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
