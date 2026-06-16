using System.Text.Json;

namespace ClimaSite.Core.Entities;

/// <summary>
/// A durable, transactional record of an email that must be delivered. Rows are written in the
/// same database transaction as the business state change that triggers them (the "outbox"
/// pattern), then drained asynchronously by a background worker with retry/backoff. This makes
/// email delivery reliable across crashes and SMTP outages without any external queue.
/// </summary>
public class OutboxMessage : BaseEntity
{
    /// <summary>Semantic message kind; one of <see cref="OutboxMessageTypes"/>. Drives how the worker renders/sends it.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Destination email address.</summary>
    public string ToEmail { get; private set; } = string.Empty;

    /// <summary>JSON-serialized, type-specific arguments the worker needs to send the message.</summary>
    public string Payload { get; private set; } = "{}";

    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    /// <summary>Number of delivery attempts made so far.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Earliest time the worker may (re)attempt delivery. Drives exponential backoff.</summary>
    public DateTime NextAttemptAt { get; private set; } = DateTime.UtcNow;

    /// <summary>When the message reached a terminal state (<see cref="OutboxMessageStatus.Sent"/> or <see cref="OutboxMessageStatus.Failed"/>).</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Last delivery error, retained for diagnostics.</summary>
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string toEmail, string payload)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Outbox message type cannot be empty", nameof(type));
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Outbox message recipient cannot be empty", nameof(toEmail));

        Type = type;
        ToEmail = toEmail.Trim();
        Payload = string.IsNullOrWhiteSpace(payload) ? "{}" : payload;
        Status = OutboxMessageStatus.Pending;
        NextAttemptAt = DateTime.UtcNow;
    }

    /// <summary>Marks the message as being delivered right now and counts the attempt.</summary>
    public void BeginAttempt()
    {
        Status = OutboxMessageStatus.Processing;
        AttemptCount++;
        SetUpdatedAt();
    }

    /// <summary>Records a successful delivery.</summary>
    public void MarkSent()
    {
        Status = OutboxMessageStatus.Sent;
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
        SetUpdatedAt();
    }

    /// <summary>Schedules another delivery attempt at <paramref name="nextAttemptAt"/> after a transient failure.</summary>
    public void ScheduleRetry(string error, DateTime nextAttemptAt)
    {
        Status = OutboxMessageStatus.Pending;
        LastError = Truncate(error);
        NextAttemptAt = nextAttemptAt;
        SetUpdatedAt();
    }

    /// <summary>Gives up permanently after exhausting retries.</summary>
    public void MarkFailed(string error)
    {
        Status = OutboxMessageStatus.Failed;
        LastError = Truncate(error);
        ProcessedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    private static string Truncate(string value) =>
        string.IsNullOrEmpty(value) || value.Length <= 2000 ? value : value[..2000];

    // --- Typed factories: keep payload construction in one place, shared by enqueuers and the worker. ---

    public static OutboxMessage ForWelcome(string to, string firstName) =>
        new(OutboxMessageTypes.Welcome, to, JsonSerializer.Serialize(new WelcomeEmailPayload(firstName)));

    public static OutboxMessage ForPasswordReset(string to, string resetToken) =>
        new(OutboxMessageTypes.PasswordReset, to, JsonSerializer.Serialize(new PasswordResetEmailPayload(resetToken)));

    public static OutboxMessage ForOrderConfirmation(string to, Guid orderId) =>
        new(OutboxMessageTypes.OrderConfirmation, to, JsonSerializer.Serialize(new OrderEmailPayload(orderId)));

    public static OutboxMessage ForOrderShipped(string to, Guid orderId, string trackingNumber) =>
        new(OutboxMessageTypes.OrderShipped, to, JsonSerializer.Serialize(new OrderShippedEmailPayload(orderId, trackingNumber)));

    public static OutboxMessage ForGeneric(string to, string subject, string body) =>
        new(OutboxMessageTypes.Generic, to, JsonSerializer.Serialize(new GenericEmailPayload(subject, body)));
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}

public static class OutboxMessageTypes
{
    public const string Generic = "generic";
    public const string Welcome = "welcome";
    public const string PasswordReset = "password_reset";
    public const string OrderConfirmation = "order_confirmation";
    public const string OrderShipped = "order_shipped";
}

public sealed record WelcomeEmailPayload(string FirstName);
public sealed record PasswordResetEmailPayload(string ResetToken);
public sealed record OrderEmailPayload(Guid OrderId);
public sealed record OrderShippedEmailPayload(Guid OrderId, string TrackingNumber);
public sealed record GenericEmailPayload(string Subject, string Body);
