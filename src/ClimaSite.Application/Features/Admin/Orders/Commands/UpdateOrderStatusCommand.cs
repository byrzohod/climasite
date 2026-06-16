using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Orders.Commands;

public record UpdateOrderStatusCommand : IRequest<Result>
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
    public bool NotifyCustomer { get; init; } = true;
}

public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage("Invalid order status");
    }

    private static bool BeValidStatus(string status)
    {
        return Enum.TryParse<OrderStatus>(status, true, out _);
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context, IEmailOutbox emailOutbox)
    {
        _context = context;
        _emailOutbox = emailOutbox;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result.Failure("Order not found");
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return Result.Failure("Invalid order status");
        }

        try
        {
            order.SetStatus(newStatus);

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                var existingNotes = order.Notes ?? "";
                var noteEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status changed to {newStatus}: {request.Note}";
                order.SetNotes(string.IsNullOrEmpty(existingNotes) ? noteEntry : $"{existingNotes}\n{noteEntry}");
            }

            // BUG-16/GAP-03: honor NotifyCustomer by queuing a status-update email in the same
            // transaction as the change. (Shipped notifications with tracking go through the
            // dedicated shipping flow; this is the generic status notice.)
            if (request.NotifyCustomer)
            {
                var subject = $"ClimaSite Order {order.OrderNumber} — {newStatus}";
                var body = $"Your order {order.OrderNumber} status has been updated to {newStatus}.";
                _emailOutbox.Add(OutboxMessage.ForGeneric(order.CustomerEmail, subject, body));
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
