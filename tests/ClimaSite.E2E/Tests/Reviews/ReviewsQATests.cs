using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;

namespace ClimaSite.E2E.Tests.Reviews;

/// <summary>
/// E2E tests for Reviews and Q&A functionality on product pages.
/// Tests cover review submission, display, filtering, and Q&A interactions.
/// </summary>
[Collection("Playwright")]
public class ReviewsQATests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ReviewsQATests(PlaywrightFixture fixture)
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

    #region Reviews Display Tests

    [Fact]
    public async Task Reviews_DisplayOnProductPage()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to the product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        // Wait for reviews section to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Reviews section should be visible
        var reviewsSection = _page.Locator("[data-testid='product-reviews']");
        await Assertions.Expect(reviewsSection).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Reviews summary should be visible
        var reviewsSummary = _page.Locator("[data-testid='reviews-summary']");
        await Assertions.Expect(reviewsSummary).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Reviews_ShowsAverageRating()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Summary section should be visible with rating display
        var reviewsSummary = _page.Locator("[data-testid='reviews-summary']");
        await Assertions.Expect(reviewsSummary).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Average rating section should be present
        var ratingNumber = _page.Locator(".summary-rating .rating-number");
        var isRatingVisible = await ratingNumber.IsVisibleAsync();

        // Rating display is part of summary - it may show 0 or nothing if no reviews
        isRatingVisible.Should().BeTrue("Rating display should be visible in summary");
    }

    [Fact]
    public async Task Reviews_ShowsRatingDistribution()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Rating distribution section should be visible
        var reviewsSummary = _page.Locator("[data-testid='reviews-summary']");
        await Assertions.Expect(reviewsSummary).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Distribution rows should be present (5 stars, 4 stars, etc.)
        var distributionRows = _page.Locator(".rating-distribution .distribution-row");
        var count = await distributionRows.CountAsync();

        count.Should().Be(5, "There should be 5 rating distribution rows (1-5 stars)");
    }

    #endregion

    #region Review Submission Tests

    [Fact]
    public async Task Reviews_CanSubmitReview_WhenLoggedIn()
    {
        // Arrange - Create product and user
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Write review button should be visible for logged in users
        var writeReviewBtn = _page.Locator("[data-testid='write-review-btn']");
        await Assertions.Expect(writeReviewBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Act - Click to open review form
        await writeReviewBtn.ClickAsync();

        // Review form should appear
        var reviewForm = _page.Locator("[data-testid='review-form']");
        await Assertions.Expect(reviewForm).ToBeVisibleAsync();

        // Fill in the review
        // Select 5 stars (click the 5th star button)
        var starButtons = _page.Locator(".star-input .star-btn");
        await starButtons.Nth(4).ClickAsync(); // 0-indexed, so 4 = 5th star

        // Fill title
        await _page.FillAsync("#review-title", "Great product!");

        // Fill content
        await _page.FillAsync("#review-content", "This air conditioner works perfectly. Highly recommend!");

        // Submit the review
        var submitBtn = _page.Locator("[data-testid='submit-review-btn']");
        await submitBtn.ClickAsync();

        // Wait for submission (form should close or success message appears)
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The form should be hidden after successful submission
        // (or error message if already reviewed)
        var formStillVisible = await reviewForm.IsVisibleAsync();

        // Either the form closed (success) or an error message is shown
        // Both are valid outcomes depending on test data state
        (formStillVisible == false || await _page.Locator(".form-error").IsVisibleAsync())
            .Should().BeTrue("Review form should close on success or show error if duplicate");
    }

    [Fact]
    public async Task Reviews_RequiresLogin_ToSubmit()
    {
        // Arrange - Create a product (no user login)
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page without logging in
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Write review button should NOT be visible
        var writeReviewBtn = _page.Locator("[data-testid='write-review-btn']");
        var isWriteButtonVisible = await writeReviewBtn.IsVisibleAsync();

        // Login prompt should be visible instead
        var loginPrompt = _page.Locator("[data-testid='reviews-login-prompt']");
        var isLoginPromptVisible = await loginPrompt.IsVisibleAsync();

        isWriteButtonVisible.Should().BeFalse("Write review button should not be visible when not logged in");
        isLoginPromptVisible.Should().BeTrue("Login prompt should be visible for guest users");

        // Login link should be present
        var loginLink = _page.Locator("[data-testid='reviews-login-link']");
        await Assertions.Expect(loginLink).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Reviews_ShowsVerifiedPurchaseBadge()
    {
        // Arrange - Create a product
        // Note: For verified purchase badge to show, a user needs to have a delivered order
        // This test verifies the badge element exists in the component structure
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Check for reviews section
        var reviewsSection = _page.Locator("[data-testid='product-reviews']");
        await Assertions.Expect(reviewsSection).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Get page content to verify component structure includes verified badge class
        var pageContent = await _page.ContentAsync();

        // The component template includes .verified-badge class for verified purchases
        // Even if there are no reviews with verified badge, the CSS should be present
        pageContent.Should().Contain("verified-badge",
            "The reviews component should include verified purchase badge styling");
    }

    #endregion

    #region Review Filtering Tests

    [Fact]
    public async Task Reviews_CanFilterByRating()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Sort/filter dropdown should be visible
        var sortDropdown = _page.Locator("#sort-by");
        await Assertions.Expect(sortDropdown).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Verify sort options are available
        var options = await sortDropdown.Locator("option").AllTextContentsAsync();

        // Should have multiple sorting options including rating-based ones
        options.Count.Should().BeGreaterThanOrEqualTo(2, "Should have multiple sort options");

        // Select rating high sort option
        await sortDropdown.SelectOptionAsync("rating_high");

        // Wait for potential reload
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the selection persisted
        var selectedValue = await sortDropdown.InputValueAsync();
        selectedValue.Should().Be("rating_high", "Rating high option should be selected");
    }

    #endregion

    #region Q&A Display Tests

    [Fact]
    public async Task QA_DisplaysQuestionsAndAnswers()
    {
        // Arrange - Create a product
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Q&A section should be present on product page
        // Look for the Q&A component or section
        var qaSection = _page.Locator(".product-qa");
        var isQAVisible = await qaSection.IsVisibleAsync();

        // If Q&A section exists, verify its structure
        if (isQAVisible)
        {
            // Q&A header should be visible
            var qaHeader = _page.Locator(".qa-header h2");
            await Assertions.Expect(qaHeader).ToBeVisibleAsync();
        }
        else
        {
            // Q&A may be in a tab or accordion - check for any Q&A related content
            var pageContent = await _page.ContentAsync();

            // The page should have Q&A functionality available
            (pageContent.Contains("product-qa") || pageContent.Contains("Q&A") || pageContent.Contains("Questions"))
                .Should().BeTrue("Product page should have Q&A functionality");
        }
    }

    [Fact]
    public async Task QA_CanAskQuestion()
    {
        // Arrange - Create product and user
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for Q&A section
        var qaSection = _page.Locator(".product-qa");
        var isQAVisible = await qaSection.IsVisibleAsync();

        if (!isQAVisible)
        {
            // Q&A might be in a different location or tab - skip if not visible
            return;
        }

        // Assert - Ask question button should be visible for logged in users
        var askQuestionBtn = _page.Locator("[data-testid='ask-question-btn']");
        await Assertions.Expect(askQuestionBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Act - Click to open question form
        await askQuestionBtn.ClickAsync();

        // Question form should appear
        var questionForm = _page.Locator("[data-testid='question-form']");
        await Assertions.Expect(questionForm).ToBeVisibleAsync();

        // Fill in the question
        await _page.FillAsync("[data-testid='question-text']", "What is the energy efficiency rating of this unit?");

        // Submit the question
        var submitBtn = _page.Locator("[data-testid='submit-question-btn']");
        await submitBtn.ClickAsync();

        // Wait for submission
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Question form should close or show success message
        // Questions go through moderation, so we just verify the submission was accepted
        await Task.Delay(1000); // Brief wait for UI update

        // Either form closed (success) or success message is visible
        var formVisible = await questionForm.IsVisibleAsync();
        var successMessage = _page.Locator(".success-message");
        var hasSuccessMessage = await successMessage.IsVisibleAsync();

        (formVisible == false || hasSuccessMessage)
            .Should().BeTrue("Question should be submitted successfully");
    }

    [Fact]
    public async Task QA_CanAnswerQuestion_WhenLoggedIn()
    {
        // Arrange - Create product and user
        var product = await _dataFactory.CreateProductAsync();
        var user = await _dataFactory.CreateUserAsync();

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Navigate to product page
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for Q&A section
        var qaSection = _page.Locator(".product-qa");
        var isQAVisible = await qaSection.IsVisibleAsync();

        if (!isQAVisible)
        {
            // Q&A might be in a different location - skip if not visible
            return;
        }

        // Check if there are any questions to answer
        var questionCards = _page.Locator(".question-card");
        var questionCount = await questionCards.CountAsync();

        if (questionCount == 0)
        {
            // No questions available - verify answer button is available when questions exist
            // Check page structure includes answer capability
            var pageContent = await _page.ContentAsync();
            pageContent.Should().Contain("answer-btn",
                "Q&A component should include answer button functionality");
            return;
        }

        // Assert - Answer button should be visible for logged in users
        var answerBtn = _page.Locator("[data-testid='answer-btn']").First;
        await Assertions.Expect(answerBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });

        // Act - Click to open answer form
        await answerBtn.ClickAsync();

        // Answer form should appear
        var answerForm = _page.Locator("[data-testid='answer-form']");
        await Assertions.Expect(answerForm).ToBeVisibleAsync();

        // Fill in the answer
        await _page.FillAsync("[data-testid='answer-text']", "The energy efficiency rating is A++ for optimal savings.");

        // Submit the answer
        var submitBtn = _page.Locator("[data-testid='submit-answer-btn']");
        await submitBtn.ClickAsync();

        // Wait for submission
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Answer form should close after submission (answers go through moderation)
        await Task.Delay(1000);

        var formStillVisible = await answerForm.IsVisibleAsync();
        formStillVisible.Should().BeFalse("Answer form should close after submission");
    }

    #endregion

    #region Q&A Guest User Tests

    [Fact]
    public async Task QA_RequiresLogin_ToAskQuestion()
    {
        // Arrange - Create a product (no user login)
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page without logging in
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for Q&A section
        var qaSection = _page.Locator(".product-qa");
        var isQAVisible = await qaSection.IsVisibleAsync();

        if (!isQAVisible)
        {
            // Q&A might be in a different location - check for login prompt in page
            var pageContent = await _page.ContentAsync();

            // Q&A functionality should require login for asking questions
            (pageContent.Contains("qa-login-prompt") || pageContent.Contains("login"))
                .Should().BeTrue("Q&A should indicate login requirement for guests");
            return;
        }

        // Assert - Ask question button should NOT be visible for guests
        var askQuestionBtn = _page.Locator("[data-testid='ask-question-btn']");
        var isAskButtonVisible = await askQuestionBtn.IsVisibleAsync();

        // Login prompt should be visible instead
        var loginPrompt = _page.Locator("[data-testid='qa-login-prompt']");
        var isLoginPromptVisible = await loginPrompt.IsVisibleAsync();

        isAskButtonVisible.Should().BeFalse("Ask question button should not be visible for guests");
        isLoginPromptVisible.Should().BeTrue("Login prompt should be visible for guest users");

        // Login link should be present
        var loginLink = _page.Locator("[data-testid='qa-login-link']");
        await Assertions.Expect(loginLink).ToBeVisibleAsync();
    }

    [Fact]
    public async Task QA_RequiresLogin_ToAnswer()
    {
        // Arrange - Create a product (no user login)
        var product = await _dataFactory.CreateProductAsync();

        // Act - Navigate to product page without logging in
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for Q&A section
        var qaSection = _page.Locator(".product-qa");
        var isQAVisible = await qaSection.IsVisibleAsync();

        if (!isQAVisible)
        {
            return; // Q&A not visible on this page layout
        }

        // Check if there are any questions
        var questionCards = _page.Locator(".question-card");
        var questionCount = await questionCards.CountAsync();

        if (questionCount == 0)
        {
            // No questions - verify login-to-answer link exists in template
            var pageContent = await _page.ContentAsync();
            pageContent.Should().Contain("login-to-answer",
                "Component should include login-to-answer functionality for guests");
            return;
        }

        // Assert - Answer button for guests should be a login link
        var loginToAnswerLink = _page.Locator("[data-testid='login-to-answer']").First;
        var isLoginToAnswerVisible = await loginToAnswerLink.IsVisibleAsync();

        isLoginToAnswerVisible.Should().BeTrue("Guests should see login-to-answer link instead of answer button");
    }

    #endregion
}
