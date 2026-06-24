using Xunit.Abstractions;
using Xunit.Sdk;

namespace ClimaSite.E2E.Infrastructure.Retry;

/// <summary>
/// Discoverer for <see cref="RetryTheoryAttribute"/>. Extends the built-in <see cref="TheoryDiscoverer"/>
/// so all of xUnit's normal theory machinery (pre-enumeration, skip handling, data sources) is reused;
/// it only swaps in retry-capable test cases:
///  - a pre-enumerated data row → <see cref="RetryTestCase"/> carrying that row's arguments;
///  - a non-enumerated theory   → <see cref="RetryTheoryTestCase"/> (expands rows at run time).
/// </summary>
public sealed class RetryTheoryDiscoverer : TheoryDiscoverer
{
    public RetryTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink)
    {
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo theoryAttribute,
        object[] dataRow)
    {
        var maxRetries = theoryAttribute.GetNamedArgument<int>(nameof(RetryTheoryAttribute.MaxRetries));

        yield return new RetryTestCase(
            DiagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            maxRetries,
            dataRow);
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo theoryAttribute)
    {
        var maxRetries = theoryAttribute.GetNamedArgument<int>(nameof(RetryTheoryAttribute.MaxRetries));

        yield return new RetryTheoryTestCase(
            DiagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            maxRetries);
    }
}
