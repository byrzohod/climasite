using System.Text.Json;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Features.Outbox;

/// <inheritdoc />
public class OutboxProcessor : IOutboxProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly EmailOutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IApplicationDbContext context,
        IEmailService emailService,
        EmailOutboxOptions options,
        ILogger<OutboxProcessor> logger)
    {
        _context = context;
        _emailService = emailService;
        _options = options;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reclaimBefore = now.AddMinutes(-_options.ProcessingReclaimMinutes);

        // Due = Pending rows whose backoff has elapsed, PLUS stale Processing rows. A row stuck in
        // Processing means a worker crashed after BeginAttempt() saved the Processing state but
        // before the terminal save; without reclaiming it the email would be lost forever. The
        // in-flight row from the current run is NOT reclaimed because its UpdatedAt == now (it is
        // not strictly before reclaimBefore). A reclaimed row simply runs through BeginAttempt()
        // again, which re-counts the attempt — acceptable.
        var due = await _context.OutboxMessages
            .Where(m =>
                (m.Status == OutboxMessageStatus.Pending && m.NextAttemptAt <= now)
                || (m.Status == OutboxMessageStatus.Processing && m.UpdatedAt < reclaimBefore))
            .OrderBy(m => m.NextAttemptAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var message in due)
        {
            message.BeginAttempt();
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await DispatchAsync(message, cancellationToken);
                message.MarkSent();
                sent++;
            }
            catch (NotSupportedException ex)
            {
                // Poison message: an unknown message Type can NEVER be dispatched, so retrying it
                // only wastes the retry budget and delays the inevitable. Fail it immediately.
                _logger.LogError(ex,
                    "Outbox message {MessageId} ({Type}) is unsupported and was failed without retry.",
                    message.Id, message.Type);
                message.MarkFailed(ex.Message);
            }
            catch (Exception ex)
            {
                if (message.AttemptCount >= _options.MaxAttempts)
                {
                    _logger.LogError(ex,
                        "Outbox message {MessageId} ({Type}) permanently failed after {Attempts} attempts.",
                        message.Id, message.Type, message.AttemptCount);
                    message.MarkFailed(ex.Message);
                }
                else
                {
                    var delaySeconds = _options.BaseRetryDelaySeconds * (int)Math.Pow(2, message.AttemptCount - 1);
                    var nextAttempt = now.AddSeconds(delaySeconds);
                    _logger.LogWarning(ex,
                        "Outbox message {MessageId} ({Type}) failed on attempt {Attempts}; retrying at {NextAttempt:o}.",
                        message.Id, message.Type, message.AttemptCount, nextAttempt);
                    message.ScheduleRetry(ex.Message, nextAttempt);
                }
            }

            // Once a message reaches a terminal state (Sent or permanently Failed), drop the
            // password-reset payload so the live reset token is not retained in the database.
            // Only password-reset payloads are credential-bearing; others are kept for audit.
            if ((message.Status == OutboxMessageStatus.Sent || message.Status == OutboxMessageStatus.Failed)
                && message.Type == OutboxMessageTypes.PasswordReset)
            {
                message.ClearPayload();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case OutboxMessageTypes.Welcome:
                var welcome = Deserialize<WelcomeEmailPayload>(message);
                await _emailService.SendWelcomeEmailAsync(message.ToEmail, welcome.FirstName, cancellationToken);
                break;

            case OutboxMessageTypes.PasswordReset:
                var reset = Deserialize<PasswordResetEmailPayload>(message);
                await _emailService.SendPasswordResetEmailAsync(message.ToEmail, reset.ResetToken, cancellationToken);
                break;

            case OutboxMessageTypes.OrderConfirmation:
                var order = Deserialize<OrderEmailPayload>(message);
                await _emailService.SendOrderConfirmationEmailAsync(message.ToEmail, order.OrderId, cancellationToken);
                break;

            case OutboxMessageTypes.OrderShipped:
                var shipped = Deserialize<OrderShippedEmailPayload>(message);
                await _emailService.SendOrderShippedEmailAsync(message.ToEmail, shipped.OrderId, shipped.TrackingNumber, cancellationToken);
                break;

            case OutboxMessageTypes.Generic:
                var generic = Deserialize<GenericEmailPayload>(message);
                await _emailService.SendEmailAsync(message.ToEmail, generic.Subject, generic.Body, cancellationToken);
                break;

            default:
                // Distinct exception type so the processing loop can fail this immediately
                // (a poison message) instead of burning the whole retry budget on it.
                throw new NotSupportedException($"Unknown outbox message type '{message.Type}'.");
        }
    }

    private static T Deserialize<T>(OutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.Payload)
            ?? throw new InvalidOperationException(
                $"Outbox message {message.Id} ({message.Type}) has an empty or invalid payload.");
}
