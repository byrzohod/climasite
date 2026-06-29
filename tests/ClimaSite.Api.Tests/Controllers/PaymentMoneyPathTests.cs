using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Common.Payments;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Application.Features.Payments.Commands;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// End-to-end coverage for the Stripe money path (BUG-02 / BUG-01 / BUG-18):
/// server-computed intent amount, server-side intent verification at order
/// creation, and webhook retry signalling when the order is not yet committed.
/// </summary>
public class PaymentMoneyPathTests : IntegrationTestBase
{
    public PaymentMoneyPathTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Factory.PaymentService.Reset();
    }

    [Fact]
    public async Task CreateIntentThenOrder_WithMatchingAmount_PersistsPaymentIntentId()
    {
        // Arrange: a guest cart with a known subtotal.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);

        await AuthenticateAsync();
        await AddToCartAsync(product.Id, variant.Id, 2, guestSessionId); // subtotal 200

        // Act 1: create the payment intent (amount computed server-side).
        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId
        });

        intentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();
        intent.Should().NotBeNull();

        var expectedTotal = CheckoutPricing.CalculateTotal(200m, "standard");
        intent!.Amount.Should().Be(expectedTotal);
        intent.Currency.Should().Be("EUR");
        intent.PaymentIntentId.Should().NotBeNullOrEmpty();

        // Act 2: create the order with that verified intent.
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "money-path@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });

        // Assert: order created and the verified PaymentIntentId is persisted.
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Total.Should().Be(expectedTotal);
        order.Status.Should().Be("Pending");

        var persisted = await QueryOrderAsync(order.Id);
        persisted.Should().NotBeNull();
        persisted!.PaymentIntentId.Should().Be(intent.PaymentIntentId);
        persisted.PaymentMethod.Should().Be("card");
    }

    [Fact]
    public async Task CreateOrder_WhenIntentAmountTampered_ReturnsBadRequest()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AuthenticateAsync();
        await AddToCartAsync(product.Id, variant.Id, 2, guestSessionId);

        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId
        });
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();

        // The intent reports a far smaller amount than the real order total.
        Factory.PaymentService.AmountOverride = 100; // 1.00 EUR in cents

        // Act
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "tampered@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });

        // Assert
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await orderResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Payment could not be verified");

        // BUG-04: the card was already charged client-side, so a failed verification must
        // refund the intent — otherwise the charge would be orphaned (captured, no order).
        Factory.PaymentService.Refunds.Should().ContainSingle()
            .Which.Should().Be(intent.PaymentIntentId);
    }

    [Fact]
    public async Task CreateOrder_WhenVariantOutOfStock_RefundsChargeAndReturnsBadRequest()
    {
        // Arrange: a verified, correctly-charged intent, but the variant has no stock left by
        // the time the order is created (BUG-05's atomic decrement returns insufficient stock).
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m, stockQuantity: 1);
        await AuthenticateAsync();
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId
        });
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();
        intent.Should().NotBeNull();

        // Drain all stock for the variant AFTER the intent was created so the order-time
        // atomic decrement fails. The intent still reports succeeded/eur/correct-amount.
        await SetVariantStockAsync(variant.Id, 0);

        // Act
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "out-of-stock@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });

        // Assert: order fails for insufficient stock AND the charge is refunded.
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await orderResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Insufficient stock");

        Factory.PaymentService.Refunds.Should().ContainSingle()
            .Which.Should().Be(intent.PaymentIntentId);
    }

    [Fact]
    public async Task Webhook_PaymentSucceeded_ForExistingOrder_MarksOrderPaid()
    {
        // Arrange: a fully created order with a verified intent.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AuthenticateAsync();
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId
        });
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "webhook-paid@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Act: dispatch the succeeded webhook command via the mediator.
        var result = await SendWebhookAsync(new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = intent.PaymentIntentId
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var persisted = await QueryOrderAsync(order!.Id);
        persisted!.Status.Should().Be(OrderStatus.Paid);
        persisted.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_ForUnknownIntent_ReturnsOrderNotFound()
    {
        // Act: no order exists for this intent (e.g. early-arriving webhook).
        var result = await SendWebhookAsync(new HandleStripeWebhookCommand
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = $"pi_unknown_{Guid.NewGuid():N}"
        });

        // Assert: the handler signals a retry so Stripe redelivers (BUG-18).
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateBankTransferOrder_NoPaymentIntent_PersistsMethodPendingAndQueuesInstructions()
    {
        // Arrange: a guest cart paid by bank transfer (GAP-06). No Stripe involved.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        // Act: create the order with paymentMethod = bank and NO paymentIntentId.
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "bank-buyer@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentMethod = "bank",
            guestSessionId
        });

        // Assert: order is created Pending with the bank method persisted.
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be("Pending");
        order.PaymentMethod.Should().Be("bank");

        var persisted = await QueryOrderAsync(order.Id);
        persisted.Should().NotBeNull();
        persisted!.PaymentMethod.Should().Be("bank");
        persisted.PaymentIntentId.Should().BeNull();
        persisted.Status.Should().Be(OrderStatus.Pending);

        // A bank-transfer instructions email is queued to the customer in the same unit of work.
        var bankEmail = await QueryOutboxAsync(
            OutboxMessageTypes.Generic, "bank-buyer@test.com");
        bankEmail.Should().NotBeNull();
        bankEmail!.Payload.Should().Contain(order.OrderNumber);
    }

    [Fact]
    public async Task CreateOrder_WithPaypalMethod_IsRejected()
    {
        // GAP-06: the fake "paypal" method is rejected by the validator.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "paypal-buyer@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentMethod = "paypal",
            guestSessionId
        });

        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConfig_ReturnsBankTransferDetails()
    {
        // GAP-06: the public payment config exposes the bank account details for the UI.
        var response = await Client.GetAsync("/api/payments/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<PaymentConfigResponse>();
        config.Should().NotBeNull();
        config!.BankTransfer.Should().NotBeNull();
        config.BankTransfer!.Iban.Should().NotBeNullOrEmpty();
        config.BankTransfer.AccountName.Should().NotBeNullOrEmpty();
        config.BankTransfer.BankName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GuestCheckout_Anonymous_CreatesOrder_AndConfirmationIsTokenGated()
    {
        // Arrange: a pure guest (no authentication) with a cart.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        // Act 1: anonymous create-intent (amount computed server-side, never client-supplied).
        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId
        });
        intentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();

        // Act 2: anonymous guest order creation.
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "guest-checkout@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();

        // The creation response carries the opaque guest access token.
        order!.GuestAccessToken.Should().NotBeNullOrEmpty();
        var token = order.GuestAccessToken!;

        // Confirmation IS readable anonymously WITH the token...
        var withToken = await Client.GetAsync($"/api/orders/{order.Id}/guest?token={Uri.EscapeDataString(token)}");
        withToken.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await withToken.Content.ReadFromJsonAsync<OrderDto>();
        fetched!.Id.Should().Be(order.Id);

        // ...and NOT with a wrong token (no enumeration / IDOR).
        var wrongToken = await Client.GetAsync($"/api/orders/{order.Id}/guest?token=not-the-token");
        wrongToken.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // ...and the owner-only by-id endpoint still rejects anonymous access (SEC-02 intact).
        var byId = await Client.GetAsync($"/api/orders/{order.Id}");
        byId.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateIntent_WithIdempotencyKey_RecordsTheNamespacedKey()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var clientKey = Guid.NewGuid().ToString("N");

        // Act
        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = clientKey
        });

        // Assert: the service received the "ci_"-namespaced client key.
        intentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.PaymentService.LastCreateIdempotencyKey.Should().Be("ci_" + clientKey);
        Factory.PaymentService.CreateIdempotencyKeys.Should().ContainSingle()
            .Which.Should().Be("ci_" + clientKey);
    }

    [Fact]
    public async Task CreateIntent_TwiceWithSameKeyAndCart_ReturnsTheSameIntent()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 2, guestSessionId);

        var clientKey = Guid.NewGuid().ToString("N");
        object Body() => new { shippingMethod = "standard", guestSessionId, idempotencyKey = clientKey };

        // Act: two create-intent calls with the SAME key and unchanged cart.
        var first = await Client.PostAsJsonAsync("/api/payments/create-intent", Body());
        var second = await Client.PostAsJsonAsync("/api/payments/create-intent", Body());

        // Assert: Stripe-style dedup -> the same PaymentIntent id is returned.
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstIntent = await first.Content.ReadFromJsonAsync<CreateIntentResponse>();
        var secondIntent = await second.Content.ReadFromJsonAsync<CreateIntentResponse>();

        secondIntent!.PaymentIntentId.Should().Be(firstIntent!.PaymentIntentId);
        // Only one underlying intent was created despite two POSTs.
        Factory.PaymentService.CreatedIntents.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateIntent_TwiceWithDifferentKeys_ReturnsDifferentIntents()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        // Act: two create-intent calls with DIFFERENT keys (a user retry after failure).
        var first = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        var second = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });

        // Assert: a fresh attempt key yields a fresh intent (closes the refund-replay [High]).
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstIntent = await first.Content.ReadFromJsonAsync<CreateIntentResponse>();
        var secondIntent = await second.Content.ReadFromJsonAsync<CreateIntentResponse>();
        secondIntent!.PaymentIntentId.Should().NotBe(firstIntent!.PaymentIntentId);
        Factory.PaymentService.CreatedIntents.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WhenVariantOutOfStock_RefundsWithDeterministicKey()
    {
        // Arrange: a verified, correctly-charged intent that becomes orphaned when stock drains.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m, stockQuantity: 1);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var intentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();
        intent.Should().NotBeNull();

        await SetVariantStockAsync(variant.Id, 0);

        // Act
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "refund-idem@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = intent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });

        // Assert: the orphaned charge is refunded with the deterministic server-derived key.
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Factory.PaymentService.Refunds.Should().ContainSingle()
            .Which.Should().Be(intent.PaymentIntentId);
        Factory.PaymentService.RefundIdempotencyKeys.Should().ContainSingle()
            .Which.Should().Be(PaymentIdempotency.ForRefund(intent.PaymentIntentId));
    }

    [Fact]
    public async Task CreateIntent_TwiceWithSameKeyButChangedCart_ReturnsBadRequest()
    {
        // Real Stripe rejects a reused idempotency key when ANY create param differs (it hashes the
        // whole request body). Here the SAME key is sent twice but the server-computed total changes
        // (a different shipping method => different amount + metadata), so the Fake returns the
        // param-mismatch failure -> the handler fails -> 400, and no second intent is minted.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId); // subtotal 100 (>=€50)

        var clientKey = Guid.NewGuid().ToString("N");

        // First: standard shipping (free at this subtotal).
        var first = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = clientKey
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second: SAME key, but express shipping changes the server-computed total (+€15.99).
        var second = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "express",
            guestSessionId,
            idempotencyKey = clientKey
        });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // Only the first intent was created; the mismatched replay never minted a second.
        Factory.PaymentService.CreatedIntents.Should().ContainSingle();
    }

    [Fact]
    public async Task ChargeRefundRetry_AfterRefund_IssuesAFreshIntentForTheNewAttempt()
    {
        // The regression this whole unit exists to prevent (and that the rejected cart-state-key design
        // would FAIL): a charge that is refunded on a post-charge failure must NOT be replayed on the
        // buyer's next attempt. Attempt 1 charges (key A) then fails out-of-stock -> the intent is
        // refunded; attempt 2 is a new click (key B) on the UNCHANGED cart -> a brand-new intent.
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m, stockQuantity: 1);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        // Attempt 1: create the intent (key A), then drain stock so order creation fails post-charge.
        var firstIntentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        firstIntentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstIntent = await firstIntentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();
        firstIntent.Should().NotBeNull();

        await SetVariantStockAsync(variant.Id, 0);

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "charge-refund-retry@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId = firstIntent!.PaymentIntentId,
            paymentMethod = "card",
            guestSessionId
        });

        // The orphaned charge is refunded (BUG-04) and the order is rejected.
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Factory.PaymentService.Refunds.Should().ContainSingle()
            .Which.Should().Be(firstIntent.PaymentIntentId);

        // Attempt 2: the buyer retries. A NEW per-attempt key on the (still-unchanged) cart must mint a
        // brand-new intent, never replay the refunded one.
        var secondIntentResponse = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        secondIntentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondIntent = await secondIntentResponse.Content.ReadFromJsonAsync<CreateIntentResponse>();

        secondIntent!.PaymentIntentId.Should().NotBe(firstIntent.PaymentIntentId);
        // Two distinct intents were created (the refunded one + the fresh retry); the refund stands.
        Factory.PaymentService.CreatedIntents.Should().HaveCount(2);
        Factory.PaymentService.CreatedIntents.Keys.Should().Contain(firstIntent.PaymentIntentId);
        Factory.PaymentService.CreatedIntents.Keys.Should().Contain(secondIntent.PaymentIntentId);
    }

    [Fact]
    public async Task CreateIntent_WithMalformedIdempotencyKey_ReturnsBadRequest()
    {
        // The per-attempt key is bounded to [A-Za-z0-9_-]{8,200} before it can reach Stripe; a key
        // with a space and '!' is rejected by the validator -> 400 (defence-in-depth on a
        // client-controlled value that is forwarded to the Stripe API).
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(basePrice: 100m);
        await AddToCartAsync(product.Id, variant.Id, 1, guestSessionId);

        var response = await Client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId,
            idempotencyKey = "bad key!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #region Helpers

    private async Task<ClimaSite.Application.Common.Models.Result<bool>> SendWebhookAsync(HandleStripeWebhookCommand command)
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    private async Task<Order?> QueryOrderAsync(Guid orderId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private async Task<OutboxMessage?> QueryOutboxAsync(string type, string toEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.OutboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Type == type && m.ToEmail == toEmail);
    }

    private async Task SetVariantStockAsync(Guid variantId, int stockQuantity)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
        variant.SetStockQuantity(stockQuantity);
        await db.SaveChangesAsync();
    }

    private async Task AddToCartAsync(Guid productId, Guid variantId, int quantity, string guestSessionId)
    {
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId,
            variantId,
            quantity,
            guestSessionId
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static object ValidAddress() => new
    {
        firstName = "Jane",
        lastName = "Doe",
        addressLine1 = "1 Test Way",
        city = "Sofia",
        state = "Sofia",
        postalCode = "1000",
        country = "Bulgaria",
        phone = "+359888000000"
    };

    private async Task<(Product product, ProductVariant variant)> CreateTestProductWithVariantAsync(
        decimal basePrice = 99.99m, int stockQuantity = 100)
    {
        var uniqueSku = $"PAY-{Guid.NewGuid():N}".Substring(0, 30);
        var product = new Product(uniqueSku, $"Test Product {uniqueSku}", $"test-product-{uniqueSku}", basePrice);
        product.SetShortDescription("Payment money-path test product");
        product.SetActive(true);

        var variant = new ProductVariant(product.Id, $"{uniqueSku}-VAR", "Default Variant");
        variant.SetStockQuantity(stockQuantity);
        variant.SetActive(true);
        product.Variants.Add(variant);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return (product, variant);
    }

    private record CreateIntentResponse
    {
        public string PaymentIntentId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
    }

    private record PaymentConfigResponse
    {
        public string? PublishableKey { get; init; }
        public BankTransferConfig? BankTransfer { get; init; }
    }

    private record BankTransferConfig
    {
        public string Iban { get; init; } = string.Empty;
        public string AccountName { get; init; } = string.Empty;
        public string BankName { get; init; } = string.Empty;
    }

    #endregion
}
