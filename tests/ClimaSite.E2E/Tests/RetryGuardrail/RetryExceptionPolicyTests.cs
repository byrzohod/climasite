using ClimaSite.E2E.Infrastructure.Retry;

namespace ClimaSite.E2E.Tests.RetryGuardrail;

/// <summary>
/// Deterministic, always-green guardrail tests for <see cref="RetryExceptionPolicy"/> — the single
/// decision point for "is this failure retryable?". These run without a server (pure string logic) and
/// lock down the council's NON-NEGOTIABLE rule: ONLY infra/timeout exceptions are retried; assertion
/// failures, HTTP setup failures, and everything else are NEVER retried.
///
/// This is the kept counterpart to the (removed) end-to-end proof: if anyone ever loosens the policy so
/// an assertion failure becomes retryable, one of these tests goes red.
/// </summary>
public class RetryExceptionPolicyTests
{
    [Theory]
    [InlineData("Microsoft.Playwright.PlaywrightException")]
    [InlineData("Microsoft.Playwright.TimeoutException")]
    [InlineData("System.TimeoutException")]
    public void Retryable_For_Allowlisted_InfraAndTimeout_Types(string exceptionType)
    {
        RetryExceptionPolicy.IsRetryable(new[] { exceptionType })
            .Should().BeTrue($"{exceptionType} is an infra/timeout failure and must be retryable");
    }

    [Theory]
    // FluentAssertions + plain xUnit asserts both live under Xunit.Sdk.* — the riskiest case to mask.
    [InlineData("Xunit.Sdk.XunitException")]
    [InlineData("Xunit.Sdk.TrueException")]
    [InlineData("Xunit.Sdk.EqualException")]
    // HTTP setup failures (e.g. a 5xx from the test-data API) must surface, never retry.
    [InlineData("System.Net.Http.HttpRequestException")]
    // Generic programming errors must surface.
    [InlineData("System.NullReferenceException")]
    [InlineData("System.InvalidOperationException")]
    [InlineData("System.ArgumentException")]
    public void NotRetryable_For_Assertions_HttpSetup_And_Other_Types(string exceptionType)
    {
        RetryExceptionPolicy.IsRetryable(new[] { exceptionType })
            .Should().BeFalse($"{exceptionType} signals a real failure and must NOT be retried");
    }

    [Fact]
    public void NotRetryable_When_AssertionType_Is_Anywhere_In_The_Chain()
    {
        // Even if an infra type appears, an assertion type anywhere in the reported chain disqualifies
        // retry — so a wrapped/aggregated assertion failure can never be masked.
        var chain = new[]
        {
            "System.TimeoutException",
            "Xunit.Sdk.XunitException",
        };

        RetryExceptionPolicy.IsRetryable(chain)
            .Should().BeFalse("an assertion failure anywhere in the chain must block retry");
    }

    [Fact]
    public void Retryable_When_Whole_Chain_Is_Allowlisted()
    {
        var chain = new[]
        {
            "Microsoft.Playwright.TimeoutException",
            "Microsoft.Playwright.PlaywrightException",
        };

        RetryExceptionPolicy.IsRetryable(chain).Should().BeTrue();
    }

    [Fact]
    public void NotRetryable_When_Any_ChainEntry_Is_NotAllowlisted()
    {
        var chain = new[]
        {
            "System.TimeoutException",
            "System.Net.Http.HttpRequestException",
        };

        RetryExceptionPolicy.IsRetryable(chain)
            .Should().BeFalse("a single non-infra type in the chain blocks retry");
    }

    [Theory]
    [InlineData((object?)null)]
    public void FailClosed_For_Null_ExceptionTypes(IReadOnlyList<string?>? exceptionTypes)
    {
        RetryExceptionPolicy.IsRetryable(exceptionTypes)
            .Should().BeFalse("missing exception info must fail closed (no retry)");
    }

    [Fact]
    public void FailClosed_For_Empty_ExceptionTypes()
    {
        RetryExceptionPolicy.IsRetryable(Array.Empty<string?>())
            .Should().BeFalse("an empty type list is uninformative and must fail closed");
    }

    [Fact]
    public void FailClosed_For_Null_Or_Blank_ChainEntry()
    {
        RetryExceptionPolicy.IsRetryable(new[] { "System.TimeoutException", null })
            .Should().BeFalse("a null entry is uninformative and must fail closed");

        RetryExceptionPolicy.IsRetryable(new[] { "System.TimeoutException", "   " })
            .Should().BeFalse("a blank entry is uninformative and must fail closed");
    }
}
