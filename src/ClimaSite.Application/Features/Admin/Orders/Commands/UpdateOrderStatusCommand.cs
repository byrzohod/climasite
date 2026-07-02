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
    private readonly IStockReservationService _reservations;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        IEmailOutbox emailOutbox,
        IStockReservationService reservations)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _reservations = reservations;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return Result.Failure("Invalid order status");
        }

        // INV-01 B: order-status transitions that touch bank-transfer stock holds (consume on → Paid, release on
        // → Cancelled) run in the SAME unit of work as the status change, serialized per order by the order-row
        // lock, so wrap the whole apply in an execution-strategy transaction (all must commit or roll back together).
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy is null ? await ApplyAsync() : await strategy.ExecuteAsync(ApplyAsync);

        async Task<Result> ApplyAsync()
        {
            // Re-derive from committed state on every attempt (a commit-unknown retry reuses this scoped context).
            _context.ClearChangeTracker();
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // INV-01 B [council High #2]: lock the ORDER row FIRST — before reading its status/holds or any variant
            // lock — so mark-paid / cancel / sweeper-auto-cancel of this order are mutually exclusive. Every branch
            // below then decides on the status RE-READ under this lock, not a stale snapshot.
            await _context.LockOrderForUpdateAsync(request.OrderId, cancellationToken);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure("Order not found");
            }

            // Idempotent (retry-safe): a commit-unknown retry of a prior successful apply finds the order already
            // in the target status — return success without re-consuming a bank hold or re-notifying.
            if (order.Status == newStatus)
            {
                await transaction.CommitAsync(cancellationToken);
                return Result.Success();
            }

            try
            {
                // Is this a bank-transfer order (has, or had, bank holds)? Card orders never do.
                var isBankOrder = await _context.StockReservations.AsNoTracking().AnyAsync(
                    r => r.OrderId == order.Id && r.Kind == ReservationKind.BankTransfer,
                    cancellationToken);

                // INV-01 B [council High #1 + Medium #3]: a bank-transfer order transitioning INTO Paid — from ANY
                // from-state the domain allows (Pending OR PaymentFailed) — consumes its Active holds (physical
                // decrement) here. Card orders consumed their stock at order-create, so they skip this. The consume
                // is ALL-OR-NOTHING: unless EVERY Active hold sold (AllConsumed), refuse and roll the whole tx back
                // — a hold that expired (sweeper) or a partial consume must never leave a Paid order half-sold.
                if (newStatus == OrderStatus.Paid && isBankOrder)
                {
                    var consume = await _reservations.ConsumeBankOrderAsync(order.Id, cancellationToken);
                    if (!consume.AllConsumed)
                    {
                        return Result.Failure(
                            "This bank-transfer order can no longer be marked as paid; its stock hold has expired.");
                    }
                }

                // INV-01 B [council Medium #4]: an explicit admin cancellation of a bank order that still HOLDS its
                // stock must RELEASE the hold immediately (drop reserved) — same as CancelOrderCommand — rather than
                // leak it until the sweeper's TTL. (A Paid bank order's holds are already Consumed ⇒ nothing Active.)
                if (newStatus == OrderStatus.Cancelled && isBankOrder)
                {
                    await _reservations.ReleaseBankOrderAsync(order.Id, cancellationToken);
                }

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
                await transaction.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
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
