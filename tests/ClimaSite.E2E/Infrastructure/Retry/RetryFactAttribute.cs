using Xunit;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// A <see cref="FactAttribute"/> that retries the test ONCE (2 attempts total) — but ONLY when the
/// failure is an infrastructure/timeout exception (see <see cref="RetryExceptionPolicy"/>).
///
/// GUARDRAILS (Plan 19 A4 — do not weaken):
///  - Max 1 retry. A test needing 3 attempts is broken, not flaky.
///  - Retries ONLY on the allowlisted infra/timeout exception types. Assertion failures
///    (FluentAssertions / xUnit asserts), HTTP 4xx/5xx setup failures, and every other
///    exception type fail immediately on attempt 1 — they are NEVER retried, so a genuine
///    regression is never masked.
///  - Every retry writes a LOUD, countable "RETRIED:" line to the test output so flakes are
///    surfaced, never silenced.
///
/// This is OPT-IN: apply it only to the historically-flaky test classes. Everything else stays
/// on plain <c>[Fact]</c>.
/// </summary>
[XunitTestCaseDiscoverer("ClimaSite.E2E.Infrastructure.Retry.RetryFactDiscoverer", "ClimaSite.E2E")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RetryFactAttribute : FactAttribute
{
    /// <summary>
    /// Number of retries after the first attempt. HARD-CAPPED at 1 (2 attempts total) by
    /// <see cref="RetryTestCase"/> regardless of what is requested. Exposed only for readability.
    /// </summary>
    public int MaxRetries { get; set; } = 1;
}
