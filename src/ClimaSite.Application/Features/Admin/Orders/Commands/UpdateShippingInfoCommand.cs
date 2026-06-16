using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
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
    private readonly IEmailOutbox _emailOutbox;

    public UpdateShippingInfoCommandHandler(IApplicationDbContext context, IEmailOutbox emailOutbox)
    {
        _context = context;
        _emailOutbox = emailOutbox;
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

        var nowShipped = false;
        if (request.MarkAsShipped)
        {
            try
            {
                order.SetStatus(OrderStatus.Shipped);
                nowShipped = true;
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        // GAP-03: queue the "shipped" email in the same transaction as the status change, so it is
        // delivered reliably and never sent for a change that rolled back.
        if (nowShipped)
        {
            _emailOutbox.Add(OutboxMessage.ForOrderShipped(
                order.CustomerEmail, order.Id, order.TrackingNumber ?? string.Empty));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
