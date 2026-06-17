using ClimaSite.E2E.Infrastructure;
using FluentAssertions;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Legal;

/// <summary>
/// E2E for GAP-04: the legal/support pages render (closing the 404ing footer links) and the
/// cookie-consent banner appears and persists a decision.
/// </summary>
[Collection("Playwright")]
public class LegalPagesTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;

    public LegalPagesTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
    }

    public async Task DisposeAsync()
    {
        await _page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("/terms")]
    [InlineData("/privacy")]
    [InlineData("/cookies")]
    [InlineData("/returns")]
    [InlineData("/shipping")]
    [InlineData("/impressum")]
    public async Task LegalPage_RendersTranslatedContent(string path)
    {
        await _page.GotoAsync(path);
        await _page.Locator("[data-testid='legal-page']").WaitForAsync();

        var title = (await _page.Locator("[data-testid='legal-title']").InnerTextAsync()).Trim();
        title.Should().NotBeNullOrWhiteSpace();
        // No raw, unresolved translation key should leak through.
        title.Should().NotContain("legal.");

        (await _page.Locator("[data-testid='legal-section-0']").CountAsync())
            .Should().BeGreaterThan(0, "each legal page renders at least one content section");
    }

    [Fact]
    public async Task Faq_TogglesAnswer()
    {
        await _page.GotoAsync("/faq");
        await _page.Locator("[data-testid='faq-page']").WaitForAsync();

        await _page.Locator("[data-testid='faq-question-0']").ClickAsync();
        await Assertions.Expect(_page.Locator("[data-testid='faq-answer-0']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CookieConsent_ShownToFreshVisitor_AndDismissedOnAccept()
    {
        // A visitor who has not yet decided sees the banner (no seeded consent).
        var page = await _fixture.CreatePageAsync(seedCookieConsent: false);
        try
        {
            await page.GotoAsync("/");
            var banner = page.Locator("[data-testid='cookie-consent']");
            await Assertions.Expect(banner).ToBeVisibleAsync();

            await page.Locator("[data-testid='cookie-consent-accept']").ClickAsync();
            await Assertions.Expect(banner).Not.ToBeVisibleAsync();

            // The decision persists across reloads.
            await page.ReloadAsync();
            await Assertions.Expect(page.Locator("[data-testid='cookie-consent']")).Not.ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
