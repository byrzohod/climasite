namespace ClimaSite.Application.Features.Outbox;

/// <summary>
/// Drains due, undelivered messages from the outbox and attempts delivery. Kept separate from the
/// hosting shell so it can be unit-tested and driven deterministically by integration tests.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Attempts delivery of one batch of due messages. Returns the number of messages that were
    /// successfully sent in this pass.
    /// </summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
