using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// BUG-05 regression: two concurrent orders for the last unit must not oversell.
/// Exactly one order succeeds and stock never goes negative (the atomic decrement
/// with a stock &gt;= qty guard replaces the old read-then-write).
/// </summary>
public class StockConcurrencyTests : IntegrationTestBase
{
    public StockConcurrencyTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConcurrentOrders_ForLastUnit_OnlyOneSucceeds_NoOversell()
    {
        var variantId = await SeedProductWithStockAsync(stock: 1);

        var clientA = await CreateAuthedClientWithCartItemAsync($"a-{Guid.NewGuid():N}@test.com", variantId);
        var clientB = await CreateAuthedClientWithCartItemAsync($"b-{Guid.NewGuid():N}@test.com", variantId);
        try
        {
            object OrderBody() => new
            {
                customerEmail = "buyer@test.com",
                shippingAddress = ValidAddress(),
                shippingMethod = "standard"
            };

            // Fire both orders concurrently against the same last unit.
            var taskA = clientA.PostAsJsonAsync("/api/orders", OrderBody());
            var taskB = clientB.PostAsJsonAsync("/api/orders", OrderBody());
            var responses = await Task.WhenAll(taskA, taskB);

            var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
            var rejected = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

            created.Should().Be(1, "exactly one order may take the last unit");
            rejected.Should().Be(1, "the other order must be rejected for insufficient stock");

            (await GetVariantStockAsync(variantId))
                .Should().Be(0, "stock must never oversell to a negative value");
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    private async Task<Guid> SeedProductWithStockAsync(int stock)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sku = $"STK-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Stock {sku}", $"stock-{sku}", 100m);
        product.SetShortDescription("Stock concurrency test");
        product.SetActive(true);

        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(stock);
        variant.SetActive(true);
        product.Variants.Add(variant);

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<HttpClient> CreateAuthedClientWithCartItemAsync(string email, Guid variantId)
    {
        var client = Factory.CreateClient();
        const string password = "Passw0rd!";
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, firstName = "Stock", lastName = "Test" });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())?.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid productId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            productId = await db.ProductVariants
                .Where(v => v.Id == variantId)
                .Select(v => v.ProductId)
                .FirstAsync();
        }

        var add = await client.PostAsJsonAsync("/api/cart/items", new { productId, variantId, quantity = 1 });
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        return client;
    }

    private async Task<int> GetVariantStockAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.ProductVariants
            .Where(v => v.Id == variantId)
            .Select(v => v.StockQuantity)
            .FirstAsync();
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

    private record LoginResult(string AccessToken);
}
