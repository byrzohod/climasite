using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Infrastructure;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;
    public HttpClient ApiClient { get; private set; } = default!;

    private readonly string _baseUrl;
    private readonly string _apiUrl;

    // A3 — diagnosability. When tracing is on, each context records screenshots + DOM snapshots +
    // call sources, and the trace is saved on context close. Tracking the per-context save path lets
    // us name the artifact and avoid double-saves.
    private readonly bool _tracingEnabled;
    private readonly string _traceDir;
    private readonly ConcurrentDictionary<IBrowserContext, string> _tracePaths = new();
    private int _traceCounter;

    public PlaywrightFixture()
    {
        _baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:4200";
        _apiUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5029";

        // Enable tracing on CI by default (CI uploads test-results/ on failure), and allow opting in
        // locally with E2E_TRACE=1. Disable explicitly with E2E_TRACE=0.
        var traceFlag = Environment.GetEnvironmentVariable("E2E_TRACE");
        var onCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
        _tracingEnabled = traceFlag switch
        {
            "0" or "false" => false,
            "1" or "true" => true,
            _ => onCi
        };

        // Saved under test-results/traces so the existing `if: failure()` artifact upload in
        // .github/workflows/test.yml picks them up.
        _traceDir = Path.Combine(
            Environment.GetEnvironmentVariable("E2E_TRACE_DIR") ?? "test-results", "traces");
    }

    public string BaseUrl => _baseUrl;
    public string ApiUrl => _apiUrl;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("E2E_HEADLESS") != "false",
            SlowMo = int.TryParse(Environment.GetEnvironmentVariable("E2E_SLOW_MO"), out var slowMo)
                ? slowMo : 0
        });

        // Configure HttpClient to accept self-signed certificates for local development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        ApiClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_apiUrl)
        };

        if (_tracingEnabled)
        {
            Directory.CreateDirectory(_traceDir);
        }
    }

    public async Task<IPage> CreatePageAsync(bool seedCookieConsent = true)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = Environment.GetEnvironmentVariable("E2E_RECORD_VIDEO") == "true"
                ? "videos/" : null
        });

        // A3 — start tracing for this per-test context (the browser is shared, but each test owns its
        // own context via CreatePageAsync + Context.CloseAsync in teardown). PlaywrightFixture is an
        // ICollectionFixture, so there is no per-test xUnit failure hook; instead we proactively stop
        // + save the trace when the page begins closing — the page's Close event fires on the FIRST
        // CloseAsync (page or context), while the underlying connection is still alive, so the async
        // StopAsync round-trip completes successfully. The handler is async (never blocked with
        // .GetResult(), which would deadlock the Playwright dispatcher thread) and records its Task in
        // _pendingTraceSaves so DisposeAsync can drain any still-in-flight saves. We always save and
        // let CI upload test-results/ only on failure; tests that want a named, failure-only trace can
        // instead call StopTracingAsync explicitly before closing their context.
        if (_tracingEnabled)
        {
            await context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            var index = Interlocked.Increment(ref _traceCounter);
            var path = Path.Combine(_traceDir, $"trace-{index:D4}.zip");
            _tracePaths[context] = path;
        }

        // GAP-04: the cookie-consent banner is fixed at the viewport bottom and would intercept
        // clicks on bottom-of-page controls. Pre-accept it by default so existing flows are
        // unaffected; tests that exercise the banner itself pass seedCookieConsent: false.
        if (seedCookieConsent)
        {
            await context.AddInitScriptAsync(
                "try { localStorage.setItem('climasite_cookie_consent', 'accepted'); } catch (e) {}");
        }

        var page = await context.NewPageAsync();

        // Set default timeouts
        page.SetDefaultTimeout(30000);
        page.SetDefaultNavigationTimeout(30000);

        return page;
    }

    /// <summary>
    /// Teardown helper for traced contexts: stops + saves this page's trace (while the browser
    /// connection is still alive) and then closes its context. Replaces a bare
    /// <c>page.Context.CloseAsync()</c> in test <c>DisposeAsync</c>. The trace MUST be stopped before
    /// the context closes — Playwright's Close events fire only once the context is already tearing
    /// down, too late to export the trace — which is why this is an explicit teardown call rather than
    /// an event hook. When tracing is off this is just a context close. Always saves under
    /// test-results/traces (the CI workflow uploads that directory only on failure).
    /// </summary>
    public async Task CloseTracedContextAsync(IPage page)
    {
        var ctx = page.Context;

        if (_tracingEnabled && _tracePaths.TryRemove(ctx, out var path))
        {
            try
            {
                await ctx.Tracing.StopAsync(new TracingStopOptions { Path = path });
            }
            catch
            {
                // Best-effort: trace teardown must never fail a test result.
            }
        }

        await ctx.CloseAsync();
    }

    public TestDataFactory CreateDataFactory() => new(ApiClient);

    public async Task DisposeAsync()
    {
        ApiClient?.Dispose();

        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();
    }
}

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}
