using Microsoft.Playwright;

namespace ClimaSite.E2E.Infrastructure;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;
    public HttpClient ApiClient { get; private set; } = default!;

    private readonly string _baseUrl;
    private readonly string _apiUrl;

    public PlaywrightFixture()
    {
        _baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:4200";
        _apiUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5029";
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
