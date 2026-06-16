using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Outbox;

/// <summary>
/// Enqueues emails into the durable outbox. Two paths are offered:
/// <list type="bullet">
/// <item><see cref="Add"/> stages a message on the shared <c>DbContext</c> WITHOUT saving, so it
/// commits atomically with the caller's business transaction (the transactional-outbox guarantee).</item>
/// <item><see cref="QueueAsync"/> persists immediately in its own SaveChanges, for callers that have
/// no surrounding transaction to piggyback on.</item>
/// </list>
/// </summary>
public interface IEmailOutbox
{
    /// <summary>Stages a message; the caller is responsible for calling SaveChanges to commit it.</summary>
    void Add(OutboxMessage message);

    /// <summary>Persists a message immediately.</summary>
    Task QueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
