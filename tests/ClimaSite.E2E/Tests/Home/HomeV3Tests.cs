using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Home;

[Collection("Playwright")]
public class HomeV3Tests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public HomeV3Tests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _dataFactory.CleanupAsync();
        await _page.Context.CloseAsync();
    }

    [Fact]
    public async Task HomeV3_RendersInSupportedLanguages()
    {
        var homePage = new HomePage(_page);
        await homePage.NavigateAsync();

        await Assertions.Expect(_page.Locator("[data-testid='home-v3-wizard']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByText("The right comfort, sized for your room.")).ToBeVisibleAsync();

        await homePage.SelectLanguageAsync("bg");
        await Assertions.Expect(_page.GetByText("Точният комфорт, оразмерен за вашата стая.")).ToBeVisibleAsync();

        await homePage.SelectLanguageAsync("de");
        await Assertions.Expect(_page.GetByText("Der richtige Komfort, exakt für Ihren Raum.")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData(375, 812)]
    [InlineData(768, 1024)]
    [InlineData(1440, 900)]
    public async Task HomeV3_RendersAtPrimaryViewports(int width, int height)
    {
        var productName = $"Home V3 Recommendation {width}x{height}";
        await _dataFactory.CreateProductAsync(
            name: productName,
            price: 999.99m,
            specifications: new Dictionary<string, object>
            {
                ["btu"] = 2600,
                ["isInverter"] = true,
                ["minTemp"] = -15,
                ["noiseLevel"] = 22,
                ["recommendedRoomTypes"] = new[] { "living", "bedroom", "office", "commercial" }
            });

        await _page.SetViewportSizeAsync(width, height);
        var recommendationResponseTask = _page.WaitForResponseAsync(response =>
            response.Url.Contains("/api/products/recommendations", StringComparison.OrdinalIgnoreCase) &&
            response.Status == 200);
        await _page.GotoAsync("/");
        var recommendationResponse = await recommendationResponseTask;
        recommendationResponse.Ok.Should().BeTrue();

        await Assertions.Expect(_page.Locator("[data-testid='home-v3-hero']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='home-v3-wizard']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='home-v3-recommendations']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='home-v3-rec-card-0']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByText(productName)).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("canvas")).ToBeVisibleAsync();

        var hasHorizontalOverflow = await _page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > window.innerWidth + 1");
        hasHorizontalOverflow.Should().BeFalse($"home v3 should not overflow horizontally at {width}x{height}");
    }

    [Fact]
    public async Task HomeV3_PrimaryInteractionsSupportKeyboardAndRouting()
    {
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var slider = _page.Locator("[data-testid='home-v3-area-slider']");
        await slider.FocusAsync();
        await _page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(slider).ToHaveAttributeAsync("aria-valuenow", "25");

        var bedroom = _page.Locator("[data-testid='home-v3-room-bedroom']");
        var living = _page.Locator("[data-testid='home-v3-room-living']");
        await living.FocusAsync();
        await _page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(bedroom).ToHaveAttributeAsync("aria-checked", "true");
        var activeRoom = await _page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-testid')");
        activeRoom.Should().Be("home-v3-room-bedroom");

        var zoneC = _page.Locator("[data-testid='home-v3-zone-C']");
        var zoneB = _page.Locator("[data-testid='home-v3-zone-B']");
        await zoneB.FocusAsync();
        await _page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(zoneC).ToHaveAttributeAsync("aria-checked", "true");
        var activeZone = await _page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-testid')");
        activeZone.Should().Be("home-v3-zone-C");

        await _page.Locator("[data-testid='home-v3-cta']").ClickAsync();
        await _page.WaitForURLAsync(url => url.Contains("/products"));
    }

    [Fact]
    public async Task HomeV3_ReducedMotionRendersCanvasFallback()
    {
        await using var context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _fixture.BaseUrl,
            ReducedMotion = ReducedMotion.Reduce,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(30000);
        page.SetDefaultNavigationTimeout(30000);

        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var reducedMotion = await page.EvaluateAsync<bool>(
            "() => window.matchMedia('(prefers-reduced-motion: reduce)').matches");
        reducedMotion.Should().BeTrue();
        await Assertions.Expect(page.Locator("[data-testid='home-v3-wizard']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("canvas")).ToBeVisibleAsync();
    }
}
