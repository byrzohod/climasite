using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Orders.Commands;

public record UpdateShippingInfoCommand : IRequest<Result>
{
    public Guid OrderId { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ShippingMethod { get; init; }
    public bool MarkAsShipped { get; init; }
}

public class UpdateShippingInfoCommandValidator : AbstractValidator<UpdateShippingInfoCommand>
{
    public UpdateShippingInfoCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");
    }
}

public class UpdateShippingInfoCommandHandler : IRequestHandler<UpdateShippingInfoCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateShippingInfoCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateShippingInfoCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result.Failure("Order not found");
        }

        if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            order.SetTrackingNumber(request.TrackingNumber);
        }

        if (!string.IsNullOrWhiteSpace(request.ShippingMethod))
        {
            order.SetShippingMethod(request.ShippingMethod);
        }

        if (request.MarkAsShipped)
        {
            try
            {
                order.SetStatus(Core.Entities.OrderStatus.Shipped);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
