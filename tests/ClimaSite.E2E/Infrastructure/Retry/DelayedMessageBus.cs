using Xunit.Abstractions;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// Buffers all messages produced during a single test attempt instead of forwarding them to the real
/// bus immediately. This lets the retry test case inspect the outcome (specifically the
/// <see cref="ITestFailed"/> message and its <c>ExceptionTypes</c>) and decide whether to retry —
/// WITHOUT a failed-but-retryable attempt ever reaching the runner/reporter.
///
/// On <see cref="Dispose"/> the buffered messages are flushed to the underlying bus, so the final
/// attempt's messages (pass or fail) are reported normally.
///
/// This mirrors the standard xUnit retry-sample DelayedMessageBus.
/// </summary>
internal sealed class DelayedMessageBus : IMessageBus
{
    private readonly IMessageBus _innerBus;
    private readonly List<IMessageSinkMessage> _messages = new();

    public DelayedMessageBus(IMessageBus innerBus)
    {
        _innerBus = innerBus;
    }

    public bool QueueMessage(IMessageSinkMessage message)
    {
        lock (_messages)
        {
            _messages.Add(message);
        }

        // Always keep running this attempt to completion; we decide what to flush later.
        return true;
    }

    /// <summary>Flush every buffered message to the real bus.</summary>
    public void Dispose()
    {
        foreach (var message in _messages)
        {
            _innerBus.QueueMessage(message);
        }
    }

    /// <summary>Snapshot of the messages buffered for this attempt (used to inspect the outcome).</summary>
    public IReadOnlyList<IMessageSinkMessage> BufferedMessages
    {
        get
        {
            lock (_messages)
            {
                return _messages.ToArray();
            }
        }
    }
}
