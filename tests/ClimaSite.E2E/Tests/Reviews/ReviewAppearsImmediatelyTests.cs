using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Reviews;

/// <summary>
/// SLICE D coverage for the review auto-approve flow: a logged-in user submits a review and it
/// appears IMMEDIATELY in the list (reviews auto-approve via CreateReviewCommand) and the form closes
/// as the success signal. A brand-new product starts with zero reviews, so the new review-card is
/// unambiguously the one just submitted. NO MOCKING — real product, real user, real review.
/// </summary>
[Collection("Playwright")]
public class ReviewAppearsImmediatelyTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ReviewAppearsImmediatelyTests(PlaywrightFixture fixture)
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
        await _fixture.CloseTracedContextAsync(_page);
    }

    [Fact]
    public async Task Review_SubmittedByLoggedInUser_AppearsImmediatelyInList()
    {
        // Arrange - a fresh product (zero reviews) and a logged-in user.
        var product = await _dataFactory.CreateProductAsync(name: "Review Visibility AC", price: 999.99m);
        var user = await _dataFactory.CreateUserAsync();

        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        // Open the reviews tab.
        var reviewsTab = _page.Locator("[data-testid='tab-reviews']");
        await Assertions.Expect(reviewsTab).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await reviewsTab.ClickAsync();
        await Assertions.Expect(_page.Locator("[data-testid='product-reviews']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });

        // A brand-new product has no approved reviews yet.
        var reviewCards = _page.Locator("[data-testid='review-card']");
        var countBefore = await reviewCards.CountAsync();
        countBefore.Should().Be(0, "a freshly created product should start with no reviews");

        // Act - open the review form and submit a 5-star review.
        var writeReviewBtn = _page.Locator("[data-testid='write-review-btn']");
        await Assertions.Expect(writeReviewBtn).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await writeReviewBtn.ClickAsync();

        var reviewForm = _page.Locator("[data-testid='review-form']");
        await Assertions.Expect(reviewForm).ToBeVisibleAsync();

        // 5 stars (0-indexed -> nth(4)), title and content.
        await _page.Locator(".star-input .star-btn").Nth(4).ClickAsync();
        await _page.FillAsync("#review-title", "Excellent cooling");
        await _page.FillAsync("#review-content", "Quiet, efficient, and easy to install. Appears right away!");

        await _page.ClickAsync("[data-testid='submit-review-btn']");

        // Assert (success signal) - the form closes once the review is created.
        await Assertions.Expect(reviewForm).Not.ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });

        // Assert - the new (auto-approved) review appears immediately in the list.
        await Assertions.Expect(reviewCards.First).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });
        var countAfter = await reviewCards.CountAsync();
        countAfter.Should().BeGreaterThan(countBefore,
            "the submitted review should appear in the list immediately (reviews auto-approve)");

        // The new review's content is rendered.
        await Assertions.Expect(_page.Locator("[data-testid='product-reviews']")
            .GetByText("Excellent cooling")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
    }
}
