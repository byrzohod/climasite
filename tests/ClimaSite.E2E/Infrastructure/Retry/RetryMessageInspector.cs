using Xunit.Abstractions;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// Shared helpers for the Fact/Theory retry test cases: locating the buffered <see cref="ITestFailed"/>
/// message, formatting the offending exception type, and emitting the loud + countable retry line.
/// Keeps the two test-case classes DRY and the retry/log behaviour identical.
/// </summary>
internal static class RetryMessageInspector
{
    /// <summary>Returns the last buffered <see cref="ITestFailed"/> message, or null if the attempt did not fail.</summary>
    public static ITestFailed? FindTestFailed(IReadOnlyList<IMessageSinkMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i] is ITestFailed failed)
            {
                return failed;
            }
        }

        return null;
    }

    public static string FirstOrUnknown(IReadOnlyList<string?>? exceptionTypes)
    {
        if (exceptionTypes is { Count: > 0 })
        {
            var first = exceptionTypes[0];
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first!;
            }
        }

        return "(unknown exception type)";
    }

    /// <summary>
    /// Emits the LOUD, countable retry line to both the diagnostic sink and the console so flakes are
    /// always surfaced (grep "RETRIED:" to tally them) and never silenced.
    /// </summary>
    public static void LogRetry(IMessageSink diagnosticMessageSink, string displayName, int nextAttempt, int totalAttempts, string exceptionType)
    {
        var line = $"RETRIED: {displayName} — attempt {nextAttempt} of {totalAttempts} after {exceptionType}";
        diagnosticMessageSink.OnMessage(new DiagnosticMessage(line));
        Console.WriteLine(line);
    }
}
