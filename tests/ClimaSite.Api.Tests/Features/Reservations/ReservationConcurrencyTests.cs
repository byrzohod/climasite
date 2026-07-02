using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Features.Inventory.DTOs;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.Reservations;

/// <summary>
/// INV-01 A2 concurrency MERGE-BAR break-probes over real Postgres (Testcontainers): the atomic primitives,
/// the P0 <c>FOR UPDATE</c> lock, the reserved-aware decrement guard, the sweeper + reconciler, and ascending
/// variant-id lock-ordering. Each names the fix it protects — removing that fix makes the test fail.
/// </summary>
public class ReservationConcurrencyTests : IntegrationTestBase
{
    public ReservationConcurrencyTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // (A) N concurrent reserves for the last unit: exactly ONE succeeds; reserved never exceeds stock and equals
    // Σ Active after settle. (The stock-gated increment + the P0 lock keep the counter honest.)
    [Fact]
    public async Task Reserve_ConcurrentLastUnit_ExactlyOneSucceeds_NoOverReserve()
    {
        var variantId = await SeedVariantAsync(stock: 1);
        var cartA = await CreateCartAsync();
        var cartB = await CreateCartAsync();

        var results = await Task.WhenAll(ReserveOneAsync(cartA, variantId), ReserveOneAsync(cartB, variantId));

        results.Count(ok => ok).Should().Be(1, "only one cart may hold the last unit");
        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(1);
        stock.Should().Be(1, "reserving must not touch physical stock");
        (await SumActiveAsync(variantId)).Should().Be(reserved, "reserved_quantity must equal Σ Active holds");
    }

    // (D) Two carts reserve the SAME two variants in REVERSE item order. Ascending variant_id lock-ordering makes
    // both acquire the row locks in the same order, so neither deadlocks. BREAK-PROBE: drop the OrderBy in
    // ReserveCartAsync and this deadlocks (one side aborts).
    [Fact]
    public async Task Reserve_TwoCartsSharedVariantsReverseOrder_NoDeadlock()
    {
        var v1 = await SeedVariantAsync(stock: 5);
        var v2 = await SeedVariantAsync(stock: 5);
        var cartA = await CreateCartAsync();
        var cartB = await CreateCartAsync();

        var forward = new[] { new ReservationRequestLine(v1, 1, "v1"), new ReservationRequestLine(v2, 1, "v2") };
        var reverse = new[] { new ReservationRequestLine(v2, 1, "v2"), new ReservationRequestLine(v1, 1, "v1") };

        var results = await Task.WhenAll(ReserveAsync(cartA, forward), ReserveAsync(cartB, reverse));

        results.Should().OnlyContain(ok => ok, "no deadlock — both reserves complete");
        (await ReadCountersAsync(v1)).reserved.Should().Be(2);
        (await ReadCountersAsync(v2)).reserved.Should().Be(2);
    }

    // Consume converts a hold into a sale: stock AND reserved drop together, the hold flips Consumed.
    [Fact]
    public async Task ReserveThenConsume_SellsUnit_DropsHold()
    {
        var variantId = await SeedVariantAsync(stock: 3);
        var cartId = await CreateCartAsync();
        var orderId = Guid.NewGuid();
        (await ReserveOneAsync(cartId, variantId)).Should().BeTrue();

        var outcome = await ConsumeInTransactionAsync(cartId, variantId, orderId, 1);

        outcome.Should().Be(ReservationConsumeOutcome.Consumed);
        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(2);
        reserved.Should().Be(0);
    }

    // [council High #1] A prior order left a Consumed row on this persistent (reused) cart, and the new order's
    // fresh hold is absent (swept). Consume MUST re-reserve + sell for the new order — a Consumed row from
    // ANOTHER order is not a reason to skip the sale (that under-sells and leaks phantom stock). BREAK-PROBE: a
    // "Consumed-by-another-order ⇒ don't sell" branch leaves stock undecremented here.
    [Fact]
    public async Task Consume_ReusedCartWithStaleConsumedHold_ReSellsForTheNewOrder()
    {
        var variantId = await SeedVariantAsync(stock: 5);
        var cartId = await CreateCartAsync();

        // Prior order O1 consumes 1 (stock 5 -> 4), leaving a Consumed row on the reused cart.
        (await ReserveOneAsync(cartId, variantId)).Should().BeTrue();
        (await ConsumeInTransactionAsync(cartId, variantId, Guid.NewGuid(), 1))
            .Should().Be(ReservationConsumeOutcome.Consumed);

        // New order O2 on the same cart with no fresh Active hold present.
        var outcome = await ConsumeInTransactionAsync(cartId, variantId, Guid.NewGuid(), 1);

        outcome.Should().Be(ReservationConsumeOutcome.Consumed);
        (await ReadCountersAsync(variantId)).stock.Should().Be(3, "the new order re-sold a unit (4 -> 3)");
    }

    // The universal reserved-aware decrement guard: a non-consume decrement (bank order-create / admin drain)
    // may NOT take a unit another cart holds. BREAK-PROBE: revert TryDecrementVariantStockAsync to a plain
    // `stock >= qty` gate and this returns 1 (steals the held unit).
    [Fact]
    public async Task ReservedAwareDecrement_CannotDrainAHeldUnit()
    {
        var variantId = await SeedVariantAsync(stock: 1);
        var cartId = await CreateCartAsync();
        (await ReserveOneAsync(cartId, variantId)).Should().BeTrue();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rows = await db.TryDecrementVariantStockAsync(variantId, 1);

        rows.Should().Be(0, "the only unit is held by another cart");
        (await ReadCountersAsync(variantId)).stock.Should().Be(1);
    }

    // [council High #2] Admin stock decreases must be reserved-aware: a held stock=1/reserved=1 reduce-to-0 must
    // be rejected, else a valid card holder's consume fails ⇒ charge-then-refund. BREAK-PROBE: revert the
    // AdjustStockCommand guard to `stock + change >= 0` and this drains the held unit.
    [Fact]
    public async Task AdminAdjustStock_CannotDrainAHeldUnit()
    {
        var variantId = await SeedVariantAsync(stock: 1);
        var cartId = await CreateCartAsync();
        (await ReserveOneAsync(cartId, variantId)).Should().BeTrue(); // reserved = 1

        using (var scope = Factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new AdjustStockCommand
            {
                VariantId = variantId,
                QuantityChange = -1,
                Reason = StockAdjustmentReason.Correction
            });
            result.IsSuccess.Should().BeFalse("cannot reduce stock below the units held by open checkouts");
        }

        (await ReadCountersAsync(variantId)).stock.Should().Be(1, "the held unit must not be drained");
    }

    // [council Medium] Bulk stock edits use the atomic reserved-aware set: a held stock=2/reserved=1 bulk-set to 0
    // is rejected+reported (stock unchanged); a set to exactly reserved (1) succeeds. BREAK-PROBE: revert
    // BulkAdjustStock to the tracked SetStockQuantity (no reserved guard) and the below-held set applies.
    [Fact]
    public async Task AdminBulkAdjustStock_CannotSetBelowHeldUnits_ButAllowsAtOrAbove()
    {
        var variantId = await SeedVariantAsync(stock: 2);
        var cartId = await CreateCartAsync();
        (await ReserveOneAsync(cartId, variantId)).Should().BeTrue(); // reserved = 1

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var below = await mediator.Send(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Correction,
            Adjustments = [new StockAdjustmentItem { VariantId = variantId, NewQuantity = 0 }]
        });
        below.Value!.SuccessCount.Should().Be(0);
        below.Value.FailureCount.Should().Be(1);
        below.Value.Errors.Should().ContainSingle().Which.Should().Contain("held by open checkouts");
        (await ReadCountersAsync(variantId)).stock.Should().Be(2, "the below-reserved set must not apply");

        var atReserved = await mediator.Send(new BulkAdjustStockCommand
        {
            Reason = StockAdjustmentReason.Correction,
            Adjustments = [new StockAdjustmentItem { VariantId = variantId, NewQuantity = 1 }]
        });
        atReserved.Value!.SuccessCount.Should().Be(1);
        (await ReadCountersAsync(variantId)).stock.Should().Be(1, "a set at exactly the held count is allowed");
    }

    // The sweeper is the sole releaser of expired holds: an elapsed hold is expired and its unit returns to
    // availability.
    [Fact]
    public async Task Sweeper_ExpiresElapsedHold_RestoresAvailability()
    {
        var variantId = await SeedVariantAsync(stock: 1);
        var cartId = await CreateCartAsync();
        await SeedActiveHoldAsync(cartId, variantId, quantity: 1, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        await SetReservedAsync(variantId, 1);

        var expired = await SweepAsync();

        expired.Should().Be(1);
        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(0, "the expired hold no longer counts");
        stock.Should().Be(1);
    }

    // The reconciler (R) inside the sweep heals injected counter drift to Σ Active. BREAK-PROBE: remove the
    // reconcile pass and the residual drift (2) survives.
    [Fact]
    public async Task Sweeper_Reconciler_HealsInjectedDrift()
    {
        var variantId = await SeedVariantAsync(stock: 10);
        var cartId = await CreateCartAsync();
        await SeedActiveHoldAsync(cartId, variantId, quantity: 1, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        // Inject drift: the counter reads 3 while Σ Active is only the single qty-1 hold about to expire.
        await SetReservedAsync(variantId, 3);

        await SweepAsync();

        // After expiry Σ Active = 0, so the reconcile pass must drive the counter to 0 (not the drifted residue).
        (await ReadCountersAsync(variantId)).reserved.Should().Be(0);
        (await SumActiveAsync(variantId)).Should().Be(0);
    }

    // (B) reserve-vs-sweeper: the P3 expiry CAS is `... AND expires_at <= NOW()`, so a hold whose lease a
    // concurrent reserve just refreshed into the future is NOT expired even if the sweeper had already picked it
    // up — no lost unit / oversell. BREAK-PROBE: drop the `expires_at <= NOW()` clause from
    // TryExpireReservationAsync and this expires a live, future-leased hold (dropping its counter under a buyer).
    [Fact]
    public async Task ExpiryCas_DoesNotExpireAFutureLeasedHold()
    {
        var variantId = await SeedVariantAsync(stock: 1);
        var cartId = await CreateCartAsync();
        await SeedActiveHoldAsync(cartId, variantId, quantity: 1, expiresAt: DateTime.UtcNow.AddMinutes(15));
        await SetReservedAsync(variantId, 1);
        var holdId = await GetActiveHoldIdAsync(cartId, variantId);

        int rows;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            rows = await db.TryExpireReservationAsync(holdId);
        }

        rows.Should().Be(0, "the CAS must not expire a hold whose lease is still in the future");
        (await ReadCountersAsync(variantId)).reserved.Should().Be(1);
    }

    // An existing Active hold — even one past its lease — is reused as the live E on a re-reserve of the same
    // cart, so the counter is NOT inflated (reconcile-to-target). BREAK-PROBE: treat an expired hold as E=0 and
    // the counter double-counts.
    [Fact]
    public async Task Reserve_ReusesOwnExpiredHold_WithoutInflating()
    {
        var variantId = await SeedVariantAsync(stock: 10);
        var cartId = await CreateCartAsync();
        await SeedActiveHoldAsync(cartId, variantId, quantity: 2, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        await SetReservedAsync(variantId, 2);

        (await ReserveOneAsync(cartId, variantId, quantity: 2)).Should().BeTrue();

        (await ReadCountersAsync(variantId)).reserved.Should().Be(2, "the own hold is reused, not double-counted");
        (await SumActiveAsync(variantId)).Should().Be(2);
    }

    // ---- helpers ----

    private Task<bool> ReserveOneAsync(Guid cartId, Guid variantId, int quantity = 1) =>
        ReserveAsync(cartId, new[] { new ReservationRequestLine(variantId, quantity, "AC Unit") });

    private async Task<bool> ReserveAsync(Guid cartId, ReservationRequestLine[] lines)
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        var result = await svc.ReserveCartAsync(cartId, lines, ReservationKind.Card, userId: null);
        return result.Succeeded;
    }

    private async Task<ReservationConsumeOutcome> ConsumeInTransactionAsync(Guid cartId, Guid variantId, Guid orderId, int qty)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var outcome = await svc.ConsumeLineAsync(cartId, variantId, orderId, qty, ReservationKind.Card);
            await tx.CommitAsync();
            return outcome;
        });
    }

    private async Task<int> SweepAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await svc.SweepExpiredHoldsAsync(batchSize: 100);
    }

    private async Task<Guid> SeedVariantAsync(int stock)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sku = $"RSV-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Reservation {sku}", $"rsv-{sku}", 100m);
        product.SetActive(true);
        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(stock);
        variant.SetActive(true);
        product.Variants.Add(variant);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> CreateCartAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = new Cart(null, $"sess-{Guid.NewGuid():N}");
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return cart.Id;
    }

    private async Task SeedActiveHoldAsync(Guid cartId, Guid variantId, int quantity, DateTime expiresAt)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.StockReservations.Add(new StockReservation(variantId, cartId, quantity, expiresAt, ReservationKind.Card));
        await db.SaveChangesAsync();
    }

    private async Task SetReservedAsync(Guid variantId, int value)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SetVariantReservedQuantityAsync(variantId, value);
    }

    private async Task<Guid> GetActiveHoldIdAsync(Guid cartId, Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockReservations.AsNoTracking()
            .Where(r => r.CartId == cartId && r.VariantId == variantId && r.Status == ReservationStatus.Active)
            .Select(r => r.Id)
            .FirstAsync();
    }

    private async Task<(int stock, int reserved)> ReadCountersAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var v = await db.ProductVariants.AsNoTracking().FirstAsync(x => x.Id == variantId);
        return (v.StockQuantity, v.ReservedQuantity);
    }

    private async Task<int> SumActiveAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.SumActiveReservedQuantityAsync(variantId);
    }
}
