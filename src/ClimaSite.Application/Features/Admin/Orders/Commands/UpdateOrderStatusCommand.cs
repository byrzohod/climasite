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

            // GAP-09: emit an in-app notification in the same unit of work as the status change,
            // but only for authenticated orders (guest orders have no UserId / no inbox to read it).
            if (order.UserId is Guid uid)
            {
                var (type, title, message) = BuildNotificationContent(order, newStatus);
                var notification = new Notification(uid, type, title, message);
                notification.SetLink($"/account/orders/{order.Id}");
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Maps an order status to the in-app notification type/title/message. Title and message are
    /// short, human-readable English fallbacks; the frontend localizes by <c>type</c> where it can.
    /// </summary>
    private static (string Type, string Title, string Message) BuildNotificationContent(
        Core.Entities.Order order, OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Shipped => (
                NotificationTypes.OrderShipped,
                "Order shipped",
                $"Your order {order.OrderNumber} has shipped."),
            OrderStatus.Delivered => (
                NotificationTypes.OrderDelivered,
                "Order delivered",
                $"Your order {order.OrderNumber} has been delivered."),
            OrderStatus.Cancelled => (
                NotificationTypes.OrderCancelled,
                "Order cancelled",
                $"Your order {order.OrderNumber} has been cancelled."),
            OrderStatus.Paid => (
                NotificationTypes.PaymentReceived,
                "Payment received",
                $"We received payment for your order {order.OrderNumber}."),
            _ => (
                NotificationTypes.OrderPlaced,
                "Order updated",
                $"Your order {order.OrderNumber} status is now {status}.")
        };
    }
}
