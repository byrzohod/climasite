using ClimaSite.E2E.Infrastructure;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace ClimaSite.E2E.Tests.Accessibility;

/// <summary>
/// PROC-01 Wave 5c — accessibility (axe-core) matrix across key public pages × light/dark themes.
///
/// REPORTING-FIRST: these tests run axe and log every serious/critical violation to the test output
/// (so CI surfaces the a11y baseline), but they do NOT fail on violations yet — real violations
/// likely exist and fixing them is its own tracked work. Flip <see cref="FailOnViolations"/> to true
/// (or gate it behind an env var) once the baseline is clean, to make a11y a hard gate.
/// </summary>
[Collection("Playwright")]
public class AxeAccessibilityMatrixTests
{
    // When true (or env A11Y_ENFORCE=1), the matrix fails on any serious/critical violation.
    private static bool FailOnViolations =>
        Environment.GetEnvironmentVariable("A11Y_ENFORCE") == "1";

    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AxeAccessibilityMatrixTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public static IEnumerable<object[]> Matrix()
    {
        // Public, no-auth routes that render without per-test data.
        string[] routes =
        [
            "/", "/products", "/cart", "/login", "/contact", "/about",
            "/categories", "/brands", "/promotions"
        ];
        foreach (var route in routes)
        {
            yield return [route, "light"];
            yield return [route, "dark"];
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task PublicPage_AxeAudit(string route, string theme)
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Apply the theme before the app boots (ThemeService reads this on init).
            await page.Context.AddInitScriptAsync(
                $"try {{ localStorage.setItem('climasite-theme-preference', '{theme}'); }} catch (e) {{}}");

            await page.GotoAsync(route, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });
            // Let Angular finish the initial render (avoid NetworkIdle — the app may long-poll).
            await page.WaitForTimeoutAsync(1500);

            AxeResult results = await page.RunAxe();
            Assert.NotNull(results);

            var serious = results.Violations
                .Where(v => v.Impact is "serious" or "critical")
                .ToList();

            _output.WriteLine(
                $"[a11y] {route} [{theme}] — {results.Violations.Length} violations total, " +
                $"{serious.Count} serious/critical:");
            foreach (var v in serious)
            {
                _output.WriteLine($"    {v.Impact?.ToUpperInvariant()}  {v.Id}  ({v.Nodes.Length} node(s))  {v.Help}");
            }

            if (FailOnViolations)
            {
                Assert.True(serious.Count == 0,
                    $"{serious.Count} serious/critical a11y violation(s) on {route} [{theme}]: " +
                    string.Join(", ", serious.Select(v => v.Id)));
            }
        }
        finally
        {
            await _fixture.CloseTracedContextAsync(page);
        }
    }
}
