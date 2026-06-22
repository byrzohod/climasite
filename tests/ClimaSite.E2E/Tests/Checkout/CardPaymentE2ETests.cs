using System.Net.Http.Json;
using ClimaSite.E2E.Infrastructure;
using ClimaSite.E2E.PageObjects;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace ClimaSite.E2E.Tests.Checkout;

/// <summary>
/// Real Stripe card-payment E2E coverage. Drives a real Stripe test card through the live
/// checkout flow against a running, Stripe-configured stack — NO mocking, NO stubs.
///
/// Self-skipping: if the running stack does not expose a REAL Stripe publishable key
/// (GET /api/payments/config → publishableKey starts with "pk_" AND does not contain the
/// placeholder marker "Dummy"), both tests log "SKIP: Stripe not configured" and return early
/// instead of failing. This keeps the file safe in CI environments whose appsettings ships a dummy
/// "pk_test_...Dummy..." key, while still exercising a genuine end-to-end charge whenever a real
/// Stripe key is wired up (locally now; in CI once real STRIPE_* secrets are added).
///
/// The Stripe combined CardElement renders inside a same-origin-bridged iframe whose name
/// starts with "__privateStripeFrame". Inside that frame the four fields are named
/// cardnumber, exp-date, cvc and postal (the combined element bundles all of them).
/// </summary>
[Collection("Playwright")]
public class CardPaymentE2ETests : IClassFixture<CardPaymentDataFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private readonly CardPaymentDataFixture _data;
    private readonly ITestOutputHelper _output;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    // Stripe test cards (https://stripe.com/docs/testing).
    private const string SuccessCard = "4242 4242 4242 4242";
    private const string DeclineCard = "4000 0000 0000 0002";
    private const string FutureExpiry = "12 / 34";
    private const string AnyCvc = "123";
    private const string AnyPostal = "10115";

    // The Stripe combined CardElement iframe + its internal fields. The element mounts into the
    // app's #stripe-card-element container; Stripe also injects unrelated control iframes elsewhere
    // (e.g. a universal-link modal), so the card-input frame is located *inside the mount container*
    // and by its title ("Secure card payment input frame") to avoid a strict-mode multi-match.
    private const string StripeMountSelector = "[data-testid='stripe-card-element']";
    private const string StripeFrameSelector =
        "[data-testid='stripe-card-element'] iframe[title*='Secure card payment'], " +
        "[data-testid='stripe-card-element'] iframe[name^='__privateStripeFrame']";
    private const string CardNumberField = "input[name='cardnumber'], input[placeholder*='Card number']";
    private const string ExpiryField = "input[name='exp-date'], input[placeholder*='MM']";
    private const string CvcField = "input[name='cvc'], input[placeholder*='CVC']";
    private const string PostalField = "input[name='postal'], input[placeholder*='ZIP'], input[placeholder*='postal']";

    public CardPaymentE2ETests(PlaywrightFixture fixture, CardPaymentDataFixture data, ITestOutputHelper output)
    {
        _fixture = fixture;
        _data = data;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        // Reuse a single factory (one cached admin user) across both tests. The API auth policy is
        // 10 requests/min/IP; sharing the admin halves admin-auth calls so the two tests stay under
        // the limit even when run back-to-back. Both tests browse + checkout as guests (no UI login).
        _dataFactory = _data.Factory;
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Per-test data is cleaned up once at the fixture level (shared correlation id). Only the
        // page/context is torn down per test here.
        await _page.Context.CloseAsync();
    }

    [Fact]
    public async Task CardPayment_SuccessCard_CompletesCheckoutToConfirmation()
    {
        if (!await IsStripeConfiguredAsync())
        {
            _output.WriteLine("SKIP: Stripe not configured");
            return;
        }

        // Arrange — real product, then walk the real guest checkout UI to the payment step.
        var product = await _dataFactory.CreateProductAsync(name: "Stripe Card Happy AC", price: 1199.99m);
        await AdvanceToCardPaymentStepAsync(product);

        // Act — fill the real Stripe card iframe with the success test card and place the order.
        await FillStripeCardAsync(SuccessCard, FutureExpiry, AnyCvc, AnyPostal);
        await ProceedFromPaymentToReviewAsync();
        await PlaceOrderAsync();

        // Assert — the dedicated confirmation page renders with an order number + view-order CTA.
        await _page.WaitForSelectorAsync(
            "[data-testid='order-confirmation-page'], [data-testid='order-confirmation']",
            new PageWaitForSelectorOptions { Timeout = 30000 });

        var orderNumber = _page.Locator("[data-testid='order-number']");
        await Assertions.Expect(orderNumber).ToBeVisibleAsync(new() { Timeout = 30000 });
        var orderNumberText = await orderNumber.TextContentAsync();
        orderNumberText.Should().NotBeNullOrWhiteSpace("a real order should have been created and its number shown");

        var viewOrder = _page.Locator("[data-testid='view-order-btn'], [data-testid='track-order-btn']");
        await Assertions.Expect(viewOrder).ToBeVisibleAsync(new() { Timeout = 15000 });

        _page.Url.Should().NotContain("/login");
    }

    [Fact]
    public async Task CardPayment_DeclinedCard_ShowsErrorAndDoesNotReachConfirmation()
    {
        if (!await IsStripeConfiguredAsync())
        {
            _output.WriteLine("SKIP: Stripe not configured");
            return;
        }

        // Arrange — real product, walk the real guest checkout UI to the payment step.
        var product = await _dataFactory.CreateProductAsync(name: "Stripe Card Decline AC", price: 899.99m);
        await AdvanceToCardPaymentStepAsync(product);

        // Act — fill the generic-decline test card. The decline surfaces either when capturing the
        // PaymentMethod (proceedFromPayment) or when confirming the charge (placeOrder); both paths
        // keep the buyer off the confirmation page and surface a graceful error.
        await FillStripeCardAsync(DeclineCard, FutureExpiry, AnyCvc, AnyPostal);

        // Try to advance to review. A decline at PaymentMethod capture keeps us on the payment step
        // with an error; if capture succeeds, we proceed and the decline surfaces at place-order.
        var reachedReview = await TryProceedFromPaymentToReviewAsync();
        if (reachedReview)
        {
            await _page.Locator("[data-testid='place-order']").ClickAsync();
        }

        // Assert — a graceful error is shown and we NEVER reach the confirmation page.
        var error = _page.Locator("[data-testid='checkout-error'], .stripe-error");
        await Assertions.Expect(error.First).ToBeVisibleAsync(new() { Timeout = 30000 });

        var onConfirmation = await _page.Locator(
            "[data-testid='order-confirmation-page'], [data-testid='order-confirmation']").CountAsync();
        onConfirmation.Should().Be(0, "a declined card must not produce an order/confirmation");
        _page.Url.Should().NotContain("/confirmation");
    }

    /// <summary>
    /// Returns true only when the running stack exposes a REAL Stripe publishable key.
    ///
    /// The gate is deliberately strict: appsettings.json ships a DUMMY placeholder key
    /// ("pk_test_51DummyKeyForTestingPurposesOnly000000000000") that also starts with "pk_", so a
    /// naive prefix check would let the test run with a fake key and fail the required E2E gate in CI.
    /// We therefore require the key to start with "pk_" AND not contain "Dummy" (case-insensitive).
    /// This skips in CI (dummy key → SKIP), runs locally against the real pk_test key, and will run
    /// in the owner's CI once real STRIPE_* secrets are provided.
    /// </summary>
    private async Task<bool> IsStripeConfiguredAsync()
    {
        try
        {
            var config = await _fixture.ApiClient.GetFromJsonAsync<PaymentConfigDto>(
                $"{_fixture.ApiUrl}/api/payments/config");
            var key = config?.PublishableKey;
            if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("pk_", StringComparison.Ordinal))
            {
                return false;
            }
            if (key.Contains("Dummy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Stripe config probe failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds the product as a GUEST, goes to checkout, fills shipping, and lands on the payment step
    /// with the card method selected (card is the default) and the Stripe element mounted.
    ///
    /// Guest checkout (GAP-07) is deliberate here: it keeps the flow real end-to-end (anonymous
    /// create-intent + a real Stripe charge → guest confirmation) while avoiding a UI login. The
    /// API's auth policy is 10 requests/min/IP; with two tests each also paying the admin-creation
    /// auth cost (to seed a product), adding two UI logins on top tips the suite over that limit and
    /// throttles the second test. Skipping the login keeps both tests reliably under the limit.
    /// </summary>
    private async Task AdvanceToCardPaymentStepAsync(TestProduct product)
    {
        var guestEmail = $"stripe_guest_{Guid.NewGuid():N}@test.com";

        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Slug);
        await productPage.AddToCartAsync();

        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        await cartPage.ProceedToCheckoutAsync();

        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            firstName: "Stripe",
            lastName: "Tester",
            email: guestEmail,
            street: "1 Card Test Street",
            city: "Sofia",
            state: "Sofia",
            postalCode: "1000",
            country: "Bulgaria",
            phone: "+359888777666");
        await checkoutPage.SubmitShippingFormAsync();

        // Card is the default payment method; ensure it is selected explicitly and the element mounts.
        await checkoutPage.SelectPaymentMethodAsync("card");
        await _page.WaitForSelectorAsync("[data-testid='stripe-card-element']",
            new PageWaitForSelectorOptions { Timeout = 20000 });
    }

    /// <summary>
    /// Fills the four fields of the Stripe combined CardElement inside its iframe. Stripe entry can
    /// be slow to hydrate, so we wait for the iframe + the card-number field with generous timeouts
    /// and type with a small per-key delay (Stripe rejects programmatic instant fills on some fields).
    /// </summary>
    private async Task FillStripeCardAsync(string cardNumber, string expiry, string cvc, string postal)
    {
        // Wait for the Stripe card iframe (scoped to the app's mount container) to be present before
        // locating fields inside it. .First guards against any residual multi-match.
        await _page.WaitForSelectorAsync(StripeFrameSelector,
            new PageWaitForSelectorOptions { Timeout = 30000, State = WaitForSelectorState.Attached });

        // Scope to the card iframe inside the app's mount container. There is exactly one such frame
        // there (Stripe's other control iframes live elsewhere in the DOM), so this is unambiguous.
        var frame = _page.Locator(StripeMountSelector)
            .FrameLocator("iframe[title*='Secure card payment']");

        var numberField = frame.Locator(CardNumberField);
        await numberField.WaitForAsync(new() { Timeout = 30000 });
        await numberField.ClickAsync();
        await numberField.FillAsync(string.Empty);
        await numberField.PressSequentiallyAsync(cardNumber, new() { Delay = 60 });

        var expiryField = frame.Locator(ExpiryField);
        await expiryField.ClickAsync();
        await expiryField.PressSequentiallyAsync(expiry, new() { Delay = 60 });

        var cvcField = frame.Locator(CvcField);
        await cvcField.ClickAsync();
        await cvcField.PressSequentiallyAsync(cvc, new() { Delay = 60 });

        // The postal field is part of the combined element; fill it when present.
        var postalField = frame.Locator(PostalField);
        if (await postalField.CountAsync() > 0)
        {
            await postalField.First.ClickAsync();
            await postalField.First.PressSequentiallyAsync(postal, new() { Delay = 60 });
        }
    }

    /// <summary>Advances payment → review and asserts we landed on review (used by the happy path).</summary>
    private async Task ProceedFromPaymentToReviewAsync()
    {
        var reached = await TryProceedFromPaymentToReviewAsync();
        reached.Should().BeTrue("a valid card should let checkout advance from payment to the review step");
    }

    /// <summary>
    /// Clicks "Next" on the payment step. Returns true if the review step appears, false if we stayed
    /// on the payment step (e.g. the card was declined at PaymentMethod capture and an error showed).
    /// </summary>
    private async Task<bool> TryProceedFromPaymentToReviewAsync()
    {
        await _page.Locator("[data-testid='payment-section'] [data-testid='next-step']").ClickAsync();
        try
        {
            await _page.WaitForSelectorAsync("[data-testid='review-section'], [data-testid='place-order']",
                new PageWaitForSelectorOptions { Timeout = 15000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Places the order from the review step (happy path).</summary>
    private async Task PlaceOrderAsync()
    {
        await _page.Locator("[data-testid='place-order']").ClickAsync();
    }

    private sealed record PaymentConfigDto(
        [property: System.Text.Json.Serialization.JsonPropertyName("publishableKey")]
        string? PublishableKey);
}

/// <summary>
/// Shared data fixture for the card-payment tests: holds ONE <see cref="TestDataFactory"/> (and thus
/// one cached admin user) reused by both tests, plus a single end-of-class cleanup. Sharing the admin
/// halves the admin-auth round-trips so the two real-Stripe tests stay under the API's 10 req/min/IP
/// auth limit even when run back-to-back. It owns its own HttpClient (xUnit class fixtures cannot take
/// the collection's PlaywrightFixture via constructor injection), built from the same E2E_API_URL env.
/// </summary>
public sealed class CardPaymentDataFixture : IAsyncLifetime
{
    private readonly HttpClient _apiClient;
    public TestDataFactory Factory { get; }

    public CardPaymentDataFixture()
    {
        var apiUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5029";
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _apiClient = new HttpClient(handler) { BaseAddress = new Uri(apiUrl) };
        Factory = new TestDataFactory(_apiClient);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.CleanupAsync();
        _apiClient.Dispose();
    }
}
