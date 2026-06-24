using Xunit;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// A <see cref="TheoryAttribute"/> counterpart to <see cref="RetryFactAttribute"/>. Same guardrails:
/// max 1 retry, infra/timeout exceptions only, loud + countable. See <see cref="RetryFactAttribute"/>.
/// None of the historically-flaky target classes currently use <c>[Theory]</c>, but this is provided
/// so the retry policy is available uniformly if a flaky data-driven test appears.
/// </summary>
[XunitTestCaseDiscoverer("ClimaSite.E2E.Infrastructure.Retry.RetryTheoryDiscoverer", "ClimaSite.E2E")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RetryTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Number of retries after the first attempt. HARD-CAPPED at 1 (2 attempts total) by
    /// <see cref="RetryTestCase"/> regardless of what is requested. Exposed only for readability.
    /// </summary>
    public int MaxRetries { get; set; } = 1;
}
