using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.GuestSession;

/// <summary>
/// INV-01 A1 (re-council): the migration clears the EF change tracker at the top of every execution-strategy
/// attempt. EnableRetryOnFailure reuses the request-scoped context and does NOT reset the tracker on a
/// rollback, so a retried merge would otherwise re-read the STALE, already-merged cookie cart from the
/// identity map and merge again (double-count). This reproduces that stale-tracker state deterministically on
/// a real context: a bogus unsaved quantity is left in the tracker, then the handler runs on the SAME context;
/// the merge must re-derive from committed state (2 + 3 = 5), not the stale 500. Break-probe: drop the
/// <c>ClearChangeTracker()</c> call and the merge reads 500 → the assertion fails.
/// </summary>
public class MigrateGuestCartRetrySafetyTests : IntegrationTestBase
{
    public MigrateGuestCartRetrySafetyTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Migrate_ReDerivesFromCommittedState_IgnoringStaleTrackedMutations()
    {
        var (v1, legacyId, cookieId) = await SeedCartsAsync();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Leave a stale, UNSAVED mutation in this context's change tracker (as a rolled-back retry attempt
        // would), then run the handler on the SAME context.
        var trackedCookieCart = await context.Carts.Include(c => c.Items)
            .FirstAsync(c => c.SessionId == cookieId);
        trackedCookieCart.Items.Single(i => i.VariantId == v1).SetQuantity(500);

        await new MigrateGuestCartCommandHandler(context)
            .Handle(new MigrateGuestCartCommand(legacyId, cookieId), CancellationToken.None);

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var merged = await verifyDb.Carts.AsNoTracking().Include(c => c.Items)
            .FirstAsync(c => c.SessionId == cookieId);

        merged.Items.Single(i => i.VariantId == v1).Quantity.Should()
            .Be(5, "the merge re-derives from committed state (2 + 3), not the stale tracked 500");
        (await verifyDb.Carts.CountAsync(c => c.SessionId == legacyId)).Should().Be(0, "legacy cart removed");
    }

    private async Task<(Guid v1, string legacyId, string cookieId)> SeedCartsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sku = $"RS-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Retry Safety {sku}", $"rs-{sku}", 100m);
        product.SetActive(true);
        var v1 = new ProductVariant(product.Id, $"{sku}-V1", "V1");
        v1.SetStockQuantity(50);
        v1.SetActive(true);
        product.Variants.Add(v1);
        db.Products.Add(product);

        var cookieId = $"rs-cookie-{Guid.NewGuid():N}";
        var legacyId = $"rs-legacy-{Guid.NewGuid():N}";
        var cookieCart = new Core.Entities.Cart(null, cookieId);
        cookieCart.AddItem(product.Id, v1.Id, 2, 100m);
        var legacyCart = new Core.Entities.Cart(null, legacyId);
        legacyCart.AddItem(product.Id, v1.Id, 3, 100m);
        db.Carts.Add(cookieCart);
        db.Carts.Add(legacyCart);

        await db.SaveChangesAsync();
        return (v1.Id, legacyId, cookieId);
    }
}
