using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
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

    #endregion
}
