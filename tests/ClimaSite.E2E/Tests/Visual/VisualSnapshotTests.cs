using ClimaSite.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace ClimaSite.E2E.Tests.Visual;

/// <summary>
/// PROC-01 Wave 5d — EPHEMERAL visual capture. Saves full-page screenshots of key public pages across
/// light/dark themes (+ a mobile capture of the home page) into test-results/visual/, which the E2E job
/// uploads as a short-retention artifact for AI/human review. There are NO committed baselines (owner
/// decision) — this is a review aid, not a pixel-diff gate, so the tests only assert a non-trivial image
/// was produced. Language (EN/BG/DE) breadth is a follow-up; theme + viewport are the high-value axes here.
/// </summary>
[Collection("Playwright")]
public class VisualSnapshotTests
{
    private static readonly string VisualDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-results", "visual"));

    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public VisualSnapshotTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public static IEnumerable<object[]> Matrix()
    {
        string[] routes = ["/", "/products", "/cart", "/login", "/contact", "/about"];
        foreach (var route in routes)
        {
            // Desktop, both themes.
            yield return [route, "light", 1280, 800];
            yield return [route, "dark", 1280, 800];
        }
        // Mobile home, both themes.
        yield return ["/", "light", 390, 844];
        yield return ["/", "dark", 390, 844];
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Capture(string route, string theme, int width, int height)
    {
        Directory.CreateDirectory(VisualDir);
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(width, height);
            await page.Context.AddInitScriptAsync(
                $"try {{ localStorage.setItem('climasite-theme-preference', '{theme}'); }} catch (e) {{}}");

            await page.GotoAsync(route, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            await page.WaitForTimeoutAsync(1500);

            var slug = route == "/" ? "home" : route.Trim('/').Replace('/', '-');
            var file = Path.Combine(VisualDir, $"{slug}__{theme}__{width}x{height}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = file, FullPage = true });

            _output.WriteLine($"[visual] captured {Path.GetFileName(file)}");
            var info = new FileInfo(file);
            Assert.True(info.Exists && info.Length > 1024,
                $"screenshot {file} was not produced (or is suspiciously small: {info.Length} bytes)");
        }
        finally
        {
            await _fixture.CloseTracedContextAsync(page);
        }
    }
}
