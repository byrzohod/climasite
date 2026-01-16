using ClimaSite.E2E.Infrastructure;
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Pages;

/// <summary>
/// E2E tests for Contact page including contact form and map.
/// </summary>
[Collection("Playwright")]
public class ContactPageTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ContactPageTests(PlaywrightFixture fixture)
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
    public async Task ContactPage_Loads_ShowsAllSections()
    {
        // Arrange & Act
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page is visible
        await Assertions.Expect(_page.Locator("[data-testid='contact-page']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContactPage_ShowsContactForm()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Form fields are visible
        await Assertions.Expect(_page.Locator("[data-testid='contact-form']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='contact-name']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='contact-email']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='contact-subject']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='contact-message']")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("[data-testid='contact-submit']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContactPage_ShowsMap()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Map section is visible
        await Assertions.Expect(_page.Locator("[data-testid='contact-map']")).ToBeVisibleAsync();

        // Assert - Map iframe is present
        var mapIframe = _page.Locator("[data-testid='contact-map'] iframe");
        await Assertions.Expect(mapIframe).ToBeVisibleAsync();

        // Assert - Map link is present
        var mapLink = _page.Locator("[data-testid='contact-map'] a.map-link");
        await Assertions.Expect(mapLink).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContactForm_WithEmptyFields_ShowsValidationErrors()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Submit button is disabled when form is empty (invalid)
        var submitButton = _page.Locator("[data-testid='contact-submit']");
        await Assertions.Expect(submitButton).ToBeDisabledAsync();

        // Touch fields to trigger validation display
        await _page.FocusAsync("[data-testid='contact-name']");
        await _page.FocusAsync("[data-testid='contact-email']");
        await _page.FocusAsync("[data-testid='contact-subject']");
        await _page.FocusAsync("[data-testid='contact-message']");
        await _page.FocusAsync("[data-testid='contact-name']"); // Focus back to blur last field

        // Assert - Submit button is still disabled
        await Assertions.Expect(submitButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task ContactForm_WithValidData_SubmitsSuccessfully()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Fill out the form
        await _page.FillAsync("[data-testid='contact-name']", "Test User");
        await _page.FillAsync("[data-testid='contact-email']", "test@example.com");
        await _page.FillAsync("[data-testid='contact-subject']", "Test Subject");
        await _page.FillAsync("[data-testid='contact-message']", "This is a test message for the contact form.");

        // Submit the form
        await _page.ClickAsync("[data-testid='contact-submit']");

        // Assert - Success message is shown
        await _page.WaitForSelectorAsync("[data-testid='contact-success']", new PageWaitForSelectorOptions { Timeout = 5000 });
        await Assertions.Expect(_page.Locator("[data-testid='contact-success']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContactForm_WithInvalidEmail_ShowsEmailError()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Fill email with invalid format
        await _page.FillAsync("[data-testid='contact-email']", "invalid-email");
        await _page.ClickAsync("[data-testid='contact-name']"); // Blur the email field

        // Assert - Email validation error should be shown
        var emailInput = _page.Locator("[data-testid='contact-email']");
        await Assertions.Expect(emailInput).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("invalid"));
    }

    [Fact]
    public async Task ContactPage_ShowsContactInfo()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Contact info items are visible
        var infoItems = _page.Locator(".info-item");
        var count = await infoItems.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(4); // Address, Phone, Email, Hours
    }

    [Fact]
    public async Task ContactPage_MapLink_OpensInNewTab()
    {
        // Arrange
        await _page.GotoAsync("/contact");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Map link has target="_blank"
        var mapLink = _page.Locator("[data-testid='contact-map'] a.map-link");
        var target = await mapLink.GetAttributeAsync("target");
        target.Should().Be("_blank");

        // Assert - Has rel="noopener noreferrer" for security
        var rel = await mapLink.GetAttributeAsync("rel");
        rel.Should().Contain("noopener");
    }
}
