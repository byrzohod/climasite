namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// The single source of truth for "is this failure retryable?".
///
/// We retry ONLY infrastructure/timeout failures that a real-browser + real-backend E2E suite can
/// hit irreducibly (CI GC pauses, port races, Playwright protocol hiccups). We NEVER retry an
/// assertion failure, an HTTP 4xx/5xx setup failure, or any other exception type — those signal a
/// genuine regression and must surface on attempt 1.
///
/// The decision is made on the exception-type NAMES reported by xUnit's <c>ITestFailed</c> message
/// (xUnit serialises the exception chain to fully-qualified type-name strings, not live Type
/// objects), so this works purely on strings.
/// </summary>
internal static class RetryExceptionPolicy
{
    /// <summary>
    /// Fully-qualified type names that ARE retryable (infra/timeout only). Matched case-sensitively
    /// against each entry in <c>ITestFailed.ExceptionTypes</c>.
    /// </summary>
    private static readonly string[] RetryableExact =
    {
        // Playwright protocol / browser-launch / navigation infrastructure errors.
        "Microsoft.Playwright.PlaywrightException",
        // Playwright's own timeout, which derives from PlaywrightException.
        "Microsoft.Playwright.TimeoutException",
        // BCL timeouts (e.g. HttpClient/socket timeouts during page interaction, not assertion waits).
        "System.TimeoutException",
    };

    /// <summary>
    /// Exception types that must NEVER be retried even if a subtype/name somehow looks infra-like.
    /// This is a belt-and-braces denylist so an assertion failure can never be masked: FluentAssertions
    /// and xUnit asserts both surface as <c>Xunit.Sdk.XunitException</c> (or a subtype thereof).
    /// Checked BEFORE the allowlist.
    /// </summary>
    private static readonly string[] NeverRetryContains =
    {
        // FluentAssertions throws Xunit.Sdk.XunitException via the xUnit assertion adapter; plain
        // xUnit asserts throw Xunit.Sdk.*Exception (EqualException, TrueException, ...). Both live
        // under the Xunit.Sdk namespace.
        "Xunit.Sdk.",
        // Legacy/other assertion-library shapes, defensively.
        "AssertionException",
        "AssertActualExpectedException",
    };

    /// <summary>
    /// Returns true ONLY if EVERY reported exception type is on the retryable allowlist and NONE is on
    /// the never-retry denylist. If xUnit reported no types at all, we do NOT retry (fail-closed).
    ///
    /// Requiring *all* reported types to be retryable is deliberate: xUnit reports the full exception
    /// chain (outer + inners + aggregates). If an assertion failure is anywhere in that chain we must
    /// not retry it.
    /// </summary>
    /// <param name="exceptionTypes">
    /// The <c>ITestFailed.ExceptionTypes</c> array — fully-qualified type names of the failure chain.
    /// </param>
    public static bool IsRetryable(IReadOnlyList<string?>? exceptionTypes)
    {
        if (exceptionTypes is null || exceptionTypes.Count == 0)
        {
            // Unknown / no exception info => fail-closed, do not retry.
            return false;
        }

        var sawRetryable = false;

        foreach (var raw in exceptionTypes)
        {
            var name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                // A null/blank entry is uninformative — fail-closed.
                return false;
            }

            // Denylist first: any assertion-shaped type in the chain disqualifies the whole failure.
            foreach (var deny in NeverRetryContains)
            {
                if (name.Contains(deny, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (IsAllowlisted(name))
            {
                sawRetryable = true;
            }
            else
            {
                // A non-allowlisted, non-denied type (e.g. HttpRequestException from a 5xx setup call,
                // NullReferenceException, etc.) => do not retry.
                return false;
            }
        }

        return sawRetryable;
    }

    private static bool IsAllowlisted(string name)
    {
        foreach (var allow in RetryableExact)
        {
            if (string.Equals(name, allow, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
