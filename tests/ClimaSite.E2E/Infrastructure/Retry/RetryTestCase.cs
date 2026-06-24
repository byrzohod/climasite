using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// A <see cref="XunitTestCase"/> that re-runs the test on a retryable infra/timeout failure, up to a
/// hard cap of ONE retry (2 attempts total).
///
/// How the exception-type guardrail is enforced (this is the part the standard xUnit retry samples
/// lack): each attempt runs against a <see cref="DelayedMessageBus"/> that BUFFERS all messages. When
/// an attempt fails, we read the buffered <see cref="ITestFailed"/> message and pass its
/// <c>ExceptionTypes</c> through <see cref="RetryExceptionPolicy"/>. We retry ONLY if the policy says
/// the failure is infra/timeout AND attempts remain; otherwise we flush the (unchanged) messages so
/// the real failure — assertion error, HTTP setup failure, anything non-infra — surfaces immediately.
/// </summary>
public sealed class RetryTestCase : XunitTestCase
{
    /// <summary>Absolute ceiling on retries. The council was emphatic: a test needing 3 attempts is broken.</summary>
    private const int MaxAllowedRetries = 1;

    private int _maxRetries;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryTestCase()
    {
    }

    public RetryTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay testMethodDisplay,
        TestMethodDisplayOptions testMethodDisplayOptions,
        ITestMethod testMethod,
        int maxRetries,
        object[]? testMethodArguments = null)
        : base(diagnosticMessageSink, testMethodDisplay, testMethodDisplayOptions, testMethod, testMethodArguments)
    {
        _maxRetries = ClampRetries(maxRetries);
    }

    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var attempt = 0;
        var totalAttempts = _maxRetries + 1;

        while (true)
        {
            attempt++;

            // Buffer this attempt's messages so a retryable failure never reaches the real bus.
            // NOTE: we do NOT use a `using` here — disposing flushes the buffered messages to the real
            // bus, which we want ONLY for the attempt whose result we actually return. A flaky attempt
            // that we retry must NOT be flushed (otherwise the flake is reported as a failure).
            var delayedBus = new DelayedMessageBus(messageBus);

            var summary = await base.RunAsync(
                diagnosticMessageSink,
                delayedBus,
                constructorArguments,
                aggregator,
                cancellationTokenSource);

            var isLastAttempt = attempt >= totalAttempts;

            if (summary.Failed == 0 || isLastAttempt)
            {
                // Pass, or out of attempts: flush THIS attempt's messages and return its result.
                delayedBus.Dispose();
                return summary;
            }

            // Inspect the buffered failure: retry ONLY on an allowlisted infra/timeout exception.
            var failure = RetryMessageInspector.FindTestFailed(delayedBus.BufferedMessages);
            var exceptionTypes = failure?.ExceptionTypes;

            if (!RetryExceptionPolicy.IsRetryable(exceptionTypes))
            {
                // Non-infra failure (assertion, HTTP setup, etc.): flush and surface it now — do NOT mask.
                delayedBus.Dispose();
                return summary;
            }

            // Retryable + attempts remain: drop this failed attempt's buffered messages (do NOT flush,
            // so the flake is never reported as a failure), emit a LOUD + COUNTABLE retry line, and loop.
            var exceptionType = RetryMessageInspector.FirstOrUnknown(exceptionTypes);
            RetryMessageInspector.LogRetry(diagnosticMessageSink, DisplayName, attempt + 1, _maxRetries + 1, exceptionType);
        }
    }

    private static int ClampRetries(int requested)
    {
        if (requested < 0)
        {
            return 0;
        }

        return requested > MaxAllowedRetries ? MaxAllowedRetries : requested;
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        base.Serialize(data);
        data.AddValue(nameof(_maxRetries), _maxRetries);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        base.Deserialize(data);
        _maxRetries = ClampRetries(data.GetValue<int>(nameof(_maxRetries)));
    }
}
