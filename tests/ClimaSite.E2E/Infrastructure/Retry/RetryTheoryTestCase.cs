using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// The non-pre-enumerated <c>[Theory]</c> counterpart to <see cref="RetryTestCase"/> — used when a
/// theory's data rows are resolved at run time (e.g. non-serialisable data). It applies the exact same
/// guardrails: max 1 retry, infra/timeout exceptions only, loud + countable.
///
/// The buffered-bus retry logic is identical to <see cref="RetryTestCase"/>; only the base class
/// differs (<see cref="XunitTheoryTestCase"/> expands the data rows itself at run time).
/// </summary>
public sealed class RetryTheoryTestCase : XunitTheoryTestCase
{
    private const int MaxAllowedRetries = 1;

    private int _maxRetries;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryTheoryTestCase()
    {
    }

    public RetryTheoryTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay testMethodDisplay,
        TestMethodDisplayOptions testMethodDisplayOptions,
        ITestMethod testMethod,
        int maxRetries)
        : base(diagnosticMessageSink, testMethodDisplay, testMethodDisplayOptions, testMethod)
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
                delayedBus.Dispose();
                return summary;
            }

            var failure = RetryMessageInspector.FindTestFailed(delayedBus.BufferedMessages);
            var exceptionTypes = failure?.ExceptionTypes;

            if (!RetryExceptionPolicy.IsRetryable(exceptionTypes))
            {
                delayedBus.Dispose();
                return summary;
            }

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
