using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Features.Payments.Commands;

public record HandleStripeWebhookCommand : IRequest<Result<bool>>
{
    public string EventType { get; init; } = string.Empty;
    public string PaymentIntentId { get; init; } = string.Empty;
    public string? FailureMessage { get; init; }
    public string? ChargeId { get; init; }
    public long? AmountRefunded { get; init; }
}

public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(
        IApplicationDbContext context,
        ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing Stripe webhook event {EventType} for PaymentIntent {PaymentIntentId}",
            request.EventType,
            request.PaymentIntentId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PaymentIntentId == request.PaymentIntentId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning(
                "No order found for PaymentIntent {PaymentIntentId}",
                request.PaymentIntentId);
            // Return success anyway - the webhook was processed, just no matching order
            return Result<bool>.Success(true);
        }

        try
        {
            switch (request.EventType)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentSucceeded(order, cancellationToken);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentFailed(order, request.FailureMessage, cancellationToken);
                    break;

                case "charge.refunded":
                    await HandleChargeRefunded(order, request.AmountRefunded, cancellationToken);
                    break;

                default:
                    _logger.LogInformation(
                        "Unhandled webhook event type {EventType} for order {OrderNumber}",
                        request.EventType,
                        order.OrderNumber);
                    break;
            }

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid status transition for order {OrderNumber} on event {EventType}",
                order.OrderNumber,
                request.EventType);
            // Status transition not allowed - still acknowledge the webhook
            return Result<bool>.Success(true);
        }
    }

    private async Task HandlePaymentSucceeded(Order order, CancellationToken cancellationToken)
    {
        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.PaymentFailed)
        {
            order.SetStatus(OrderStatus.Paid, "Payment confirmed via Stripe webhook");
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderNumber} marked as Paid via webhook",
                order.OrderNumber);
        }
        else
        {
            _logger.LogInformation(
                "Order {OrderNumber} already in status {Status}, skipping payment_intent.succeeded",
                order.OrderNumber,
                order.Status);
        }
    }

    private async Task HandlePaymentFailed(Order order, string? failureMessage, CancellationToken cancellationToken)
    {
        if (order.Status == OrderStatus.Pending)
        {
            var description = string.IsNullOrEmpty(failureMessage)
                ? "Payment failed"
                : $"Payment failed: {failureMessage}";

            order.SetStatus(OrderStatus.PaymentFailed, description);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Order {OrderNumber} marked as PaymentFailed: {FailureMessage}",
                order.OrderNumber,
                failureMessage ?? "No failure message provided");
        }
        else
        {
            _logger.LogInformation(
                "Order {OrderNumber} in status {Status}, skipping payment_intent.payment_failed",
                order.OrderNumber,
                order.Status);
        }
    }

    private async Task HandleChargeRefunded(Order order, long? amountRefunded, CancellationToken cancellationToken)
    {
        if (order.CanBeRefunded)
        {
            var description = amountRefunded.HasValue
                ? $"Refunded {amountRefunded.Value / 100m:C} via Stripe"
                : "Refunded via Stripe";

            order.SetStatus(OrderStatus.Refunded, description);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderNumber} marked as Refunded via webhook, amount: {Amount}",
                order.OrderNumber,
                amountRefunded?.ToString() ?? "unknown");
        }
        else
        {
            _logger.LogWarning(
                "Order {OrderNumber} in status {Status} cannot be refunded",
                order.OrderNumber,
                order.Status);
        }
    }
}
