using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.Reservations;

/// <summary>
/// INV-01 A2 end-to-end money-path break-probes over the real stack (Testcontainers + the fake Stripe): the
/// loser of a last-unit race is blocked at reserve BEFORE any charge, and a double-submit of one intent places
/// exactly one order and never refunds the order that was actually placed.
/// </summary>
public class CheckoutReservationTests : IntegrationTestBase
{
    public CheckoutReservationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // A2 legacy-reject: a guest presenting only a spoofable legacy id (no server-trusted signed cookie) may NOT
    // create a reservation — create-intent is rejected before any Stripe call. (The cookie is minted only on
    // /api/cart; a cookieless client that posts straight to create-intent has none.)
    [Fact]
    public async Task CreateIntent_GuestWithoutSignedCookie_IsRejected_AndNeverCharged()
    {
        Factory.PaymentService.Reset();
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);

        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        // Establish a cart under the legacy id (no cookie is retained by the cookieless client).
        await client.PostAsJsonAsync("/api/cart/items", new { productId, variantId, quantity = 1 });

        var resp = await client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId = Guid.NewGuid().ToString()
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, "a legacy-id-only guest cannot mint a hold");
        Factory.PaymentService.CreatedIntents.Should().BeEmpty("no hold ⇒ no charge");
    }

    // (A) at the HTTP edge: two buyers race the last unit's create-intent. Exactly one reserves + gets an intent;
    // the loser is rejected at reserve so NO Stripe intent is created for it (never charged).
    [Fact]
    public async Task CreateIntent_ConcurrentLastUnit_OnlyOneReserves_LoserNeverCharged()
    {
        Factory.PaymentService.Reset();
        var variantId = await SeedProductWithStockAsync(stock: 1);
        var productId = await GetProductIdAsync(variantId);

        var clientA = await CreateAuthedClientWithCartItemAsync($"a-{Guid.NewGuid():N}@t.com", productId, variantId);
        var clientB = await CreateAuthedClientWithCartItemAsync($"b-{Guid.NewGuid():N}@t.com", productId, variantId);
        try
        {
            object IntentBody() => new { shippingMethod = "standard" };
            var responses = await Task.WhenAll(
                clientA.PostAsJsonAsync("/api/payments/create-intent", IntentBody()),
                clientB.PostAsJsonAsync("/api/payments/create-intent", IntentBody()));

            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1, "only one buyer may reserve the last unit");
            responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
            Factory.PaymentService.CreatedIntents.Should().HaveCount(1, "the loser must never reach Stripe");

            var (stock, reserved) = await ReadCountersAsync(variantId);
            reserved.Should().Be(1);
            stock.Should().Be(1, "reserving does not decrement physical stock");
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    // (S) A concurrent double-submit of the SAME payment intent must place exactly ONE order and must NEVER
    // refund the order that was actually placed (shipping it for free). BREAK-PROBE: drop the RefundOrFailAsync
    // guard (revert the refund sites to an unguarded refund) and the cart-cleared sibling refunds the placed order.
    [Fact]
    public async Task CreateOrder_DoubleSubmitSameIntent_PlacesOneOrder_NeverRefundsPlacedOrder()
    {
        Factory.PaymentService.Reset();
        var variantId = await SeedProductWithStockAsync(stock: 1);
        var productId = await GetProductIdAsync(variantId);
        var client = await CreateAuthedClientWithCartItemAsync($"buyer-{Guid.NewGuid():N}@t.com", productId, variantId);
        try
        {
            var intent = await client.PostAsJsonAsync("/api/payments/create-intent", new { shippingMethod = "standard" });
            intent.StatusCode.Should().Be(HttpStatusCode.OK);
            var paymentIntentId = (await intent.Content.ReadFromJsonAsync<IntentResult>())!.PaymentIntentId;

            object OrderBody() => new
            {
                customerEmail = "buyer@test.com",
                shippingAddress = ValidAddress(),
                shippingMethod = "standard",
                paymentIntentId,
                paymentMethod = "card"
            };

            var responses = await Task.WhenAll(
                client.PostAsJsonAsync("/api/orders", OrderBody()),
                client.PostAsJsonAsync("/api/orders", OrderBody()));

            responses.Should().OnlyContain(
                r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.BadRequest,
                "no request may 500");

            (await OrderCountForIntentAsync(paymentIntentId)).Should().Be(1, "exactly one order backs the intent");
            Factory.PaymentService.Refunds.Should().NotContain(paymentIntentId,
                "the placed order's charge must never be refunded");
            (await ReadCountersAsync(variantId)).stock.Should().Be(0, "the unit is sold exactly once");
        }
        finally
        {
            client.Dispose();
        }
    }

    // Happy path: reserve at create-intent, then order-create consumes the hold into a sale (stock -1, no refund).
    [Fact]
    public async Task ReserveThenOrder_ConsumesHold_SellsUnit_NoRefund()
    {
        Factory.PaymentService.Reset();
        var variantId = await SeedProductWithStockAsync(stock: 2);
        var productId = await GetProductIdAsync(variantId);
        var client = await CreateAuthedClientWithCartItemAsync($"happy-{Guid.NewGuid():N}@t.com", productId, variantId);
        try
        {
            var intent = await client.PostAsJsonAsync("/api/payments/create-intent", new { shippingMethod = "standard" });
            var paymentIntentId = (await intent.Content.ReadFromJsonAsync<IntentResult>())!.PaymentIntentId;

            var order = await client.PostAsJsonAsync("/api/orders", new
            {
                customerEmail = "happy@test.com",
                shippingAddress = ValidAddress(),
                shippingMethod = "standard",
                paymentIntentId,
                paymentMethod = "card"
            });

            order.StatusCode.Should().Be(HttpStatusCode.Created);
            var (stock, reserved) = await ReadCountersAsync(variantId);
            stock.Should().Be(1, "one unit sold");
            reserved.Should().Be(0, "the hold was consumed");
            Factory.PaymentService.Refunds.Should().BeEmpty();
        }
        finally
        {
            client.Dispose();
        }
    }

    // ---- helpers ----

    private async Task<Guid> SeedProductWithStockAsync(int stock)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sku = $"CHK-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Checkout {sku}", $"chk-{sku}", 100m);
        product.SetActive(true);
        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(stock);
        variant.SetActive(true);
        product.Variants.Add(variant);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> GetProductIdAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.ProductVariants.Where(v => v.Id == variantId).Select(v => v.ProductId).FirstAsync();
    }

    private async Task<HttpClient> CreateAuthedClientWithCartItemAsync(string email, Guid productId, Guid variantId)
    {
        var client = Factory.CreateClient();
        const string password = "Passw0rd!";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password, firstName = "Chk", lastName = "Test" });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())?.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { productId, variantId, quantity = 1 });
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        return client;
    }

    private async Task<(int stock, int reserved)> ReadCountersAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var v = await db.ProductVariants.AsNoTracking().FirstAsync(x => x.Id == variantId);
        return (v.StockQuantity, v.ReservedQuantity);
    }

    private async Task<int> OrderCountForIntentAsync(string paymentIntentId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Orders.CountAsync(o => o.PaymentIntentId == paymentIntentId);
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

    private record IntentResult(string PaymentIntentId);

    private record LoginResult(string AccessToken);
}
