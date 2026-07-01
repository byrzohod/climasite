using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.GuestSession;

/// <summary>
/// INV-01 A1 (diff-council High): two concurrent same-guest cart migrations must not 500. Without
/// serialization the losing merge collides on the unique <c>(cart_id, variant_id)</c> index (duplicate item
/// insert) and/or a 0-row legacy delete — a checkout-adjacent 500. A transaction-scoped Postgres advisory lock
/// (keyed on the cookie id) makes the loser wait, re-read (legacy now gone) and no-op, so the winner's
/// single-counted merge stands. The MockDbContext retry tests can't model a real DB race, so this drives the
/// handler over Testcontainers Postgres via two independent scopes (mirroring <c>StockConcurrencyTests</c>).
/// Break-probe: remove the advisory lock and this fails (one migration surfaces the DB conflict).
/// </summary>
public class MigrateGuestCartConcurrencyTests : IntegrationTestBase
{
    public MigrateGuestCartConcurrencyTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConcurrentMerge_SameGuest_NeitherThrows_AndMergesExactlyOnce()
    {
        // One product, three variants: V1 uncapped (proves no double-count: single=5 vs double=8),
        // V2 legacy-only (distinct), V3 low stock (proves the merge stays stock-capped under the race).
        var (product, v1, v2, v3) = await SeedProductWithVariantsAsync();

        // Repeat so the interleaving that produces the real merge conflict materialises reliably.
        for (var i = 0; i < 5; i++)
        {
            var legacyId = $"cmerge-legacy-{i}-{Guid.NewGuid():N}";
            var cookieId = $"cmerge-cookie-{i}-{Guid.NewGuid():N}";

            await SeedCartAsync(legacyId, (v1, 3), (v2, 4), (v3, 3));
            await SeedCartAsync(cookieId, (v1, 2), (v3, 2));

            // Fire both migrations concurrently on independent scopes (independent DbContexts/connections).
            var act = async () => await Task.WhenAll(MigrateAsync(legacyId, cookieId), MigrateAsync(legacyId, cookieId));
            await act.Should().NotThrowAsync($"concurrent same-guest migration must not 500 (iteration {i})");

            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cookieCart = await db.Carts.AsNoTracking().Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == cookieId);
            cookieCart.Should().NotBeNull();
            cookieCart!.Items.Single(x => x.VariantId == v1).Quantity.Should().Be(5, "2 + 3, merged exactly once");
            cookieCart.Items.Single(x => x.VariantId == v2).Quantity.Should().Be(4, "legacy-only item added once");
            cookieCart.Items.Single(x => x.VariantId == v3).Quantity.Should().Be(4, "2 + 3 = 5 capped at stock 4");

            (await db.Carts.CountAsync(c => c.SessionId == legacyId)).Should().Be(0, "the legacy cart is removed");
            (await db.Carts.CountAsync(c => c.SessionId == cookieId)).Should().Be(1, "exactly one cookie cart remains");
        }
    }

    private async Task MigrateAsync(string legacyId, string cookieId)
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new MigrateGuestCartCommand(legacyId, cookieId));
    }

    private async Task SeedCartAsync(string sessionId, params (Guid variantId, int quantity)[] items)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var productId = await db.ProductVariants
            .Where(v => v.Id == items[0].variantId)
            .Select(v => v.ProductId)
            .FirstAsync();

        var cart = new Core.Entities.Cart(null, sessionId);
        foreach (var (variantId, quantity) in items)
        {
            cart.AddItem(productId, variantId, quantity, 100m);
        }

        db.Carts.Add(cart);
        await db.SaveChangesAsync();
    }

    private async Task<(Product product, Guid v1, Guid v2, Guid v3)> SeedProductWithVariantsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sku = $"CM-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Concurrent Merge {sku}", $"cmerge-{sku}", 100m);
        product.SetShortDescription("Concurrent migration test product");
        product.SetActive(true);

        var v1 = new ProductVariant(product.Id, $"{sku}-V1", "V1");
        v1.SetStockQuantity(50);
        v1.SetActive(true);
        var v2 = new ProductVariant(product.Id, $"{sku}-V2", "V2");
        v2.SetStockQuantity(50);
        v2.SetActive(true);
        var v3 = new ProductVariant(product.Id, $"{sku}-V3", "V3");
        v3.SetStockQuantity(4);
        v3.SetActive(true);
        product.Variants.Add(v1);
        product.Variants.Add(v2);
        product.Variants.Add(v3);

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (product, v1.Id, v2.Id, v3.Id);
    }
}
