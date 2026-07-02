using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Reservations;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Reservations;

/// <summary>
/// Unit coverage for the INV-01 A2 <see cref="StockReservationService"/> over the in-memory
/// <see cref="MockDbContext"/> mirrors. These prove the from-state-gated arithmetic (reconcile-to-target, the
/// decrement-first consume, the rows==0 disambiguation, release/reconcile/sweep). They are
/// necessary-not-sufficient — the mock cannot model <c>SELECT ... FOR UPDATE</c>, so the concurrency proof
/// (over-reserve / oversell / deadlock / double-submit refund) lives in the Testcontainers integration gates.
/// </summary>
public class StockReservationServiceTests
{
    private readonly MockDbContext _context = new();
    private readonly ReservationOptions _options = new();

    private StockReservationService CreateService() => new(_context, _options);

    private ProductVariant SeedVariant(int stock, int reserved = 0)
    {
        var variant = new ProductVariant(Guid.NewGuid(), $"SKU-{Guid.NewGuid():N}"[..12], "Default");
        variant.SetStockQuantity(stock);
        if (reserved > 0)
        {
            variant.SetReservedQuantity(reserved);
        }

        _context.AddProductVariant(variant);
        return variant;
    }

    private int ActiveHoldCount(Guid cartId) =>
        _context.StockReservations.Count(r => r.CartId == cartId && r.Status == ReservationStatus.Active);

    private static ReservationRequestLine Line(Guid variantId, int qty, string name = "AC Unit") =>
        new(variantId, qty, name);

    // ---- P1 reserve ----

    [Fact]
    public async Task ReserveCart_ReconcileToTarget_IsIdempotentUnderRetry_NoInflation()
    {
        // A commit-unknown retry re-runs the whole reserve delegate. Because the existing Active hold is read as
        // the live E and the counter moves only by the delta, the second attempt is a no-op — reserved stays == Σ
        // Active. BREAK-PROBE: an absolute `reserved += target` (ignoring E) would double to 6 here.
        _context.ExecutionStrategyAttempts = 2;
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();

        var result = await CreateService().ReserveCartAsync(
            cartId, new[] { Line(variant.Id, 3) }, ReservationKind.Card, userId: null);

        result.Succeeded.Should().BeTrue();
        variant.ReservedQuantity.Should().Be(3);
        (await _context.SumActiveReservedQuantityAsync(variant.Id)).Should().Be(3);
        ActiveHoldCount(cartId).Should().Be(1);
    }

    [Fact]
    public async Task ReserveCart_InsufficientStock_RollsBackWholeBatch_AndNamesItem()
    {
        // Two lines; the SECOND is short. The whole batch must roll back (nothing reserved) and the failure names
        // the short item. BREAK-PROBE (integration): without the stock-gated increment the counter over-reserves.
        var ok = SeedVariant(stock: 10);
        var short_ = SeedVariant(stock: 1);
        var cartId = Guid.NewGuid();

        var result = await CreateService().ReserveCartAsync(
            cartId,
            new[] { Line(ok.Id, 2, "In stock"), Line(short_.Id, 5, "Almost sold out") },
            ReservationKind.Card,
            userId: null);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Almost sold out");
    }

    [Fact]
    public async Task ReserveCart_PerVariantCapExceeded_FailsBeforeAnyWork()
    {
        _options.MaxUnitsPerVariant = 10;
        var variant = SeedVariant(stock: 100);
        var cartId = Guid.NewGuid();

        var result = await CreateService().ReserveCartAsync(
            cartId, new[] { Line(variant.Id, 11) }, ReservationKind.Card, userId: null);

        result.Succeeded.Should().BeFalse();
        variant.ReservedQuantity.Should().Be(0);
        ActiveHoldCount(cartId).Should().Be(0);
    }

    [Fact]
    public async Task ReserveCart_TooManyDistinctLines_Fails()
    {
        _options.MaxActiveLinesPerCart = 1;
        var a = SeedVariant(stock: 10);
        var b = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();

        var result = await CreateService().ReserveCartAsync(
            cartId, new[] { Line(a.Id, 1), Line(b.Id, 1) }, ReservationKind.Card, userId: null);

        result.Succeeded.Should().BeFalse();
        ActiveHoldCount(cartId).Should().Be(0);
    }

    [Fact]
    public async Task ReserveCart_MultipleLines_ReservesEachVariant()
    {
        var a = SeedVariant(stock: 10);
        var b = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();

        var result = await CreateService().ReserveCartAsync(
            cartId, new[] { Line(a.Id, 2), Line(b.Id, 4) }, ReservationKind.Card, userId: null);

        result.Succeeded.Should().BeTrue();
        a.ReservedQuantity.Should().Be(2);
        b.ReservedQuantity.Should().Be(4);
        ActiveHoldCount(cartId).Should().Be(2);
    }

    [Fact]
    public async Task ReserveCart_ShrinksAnExistingHold_WhenTargetLower()
    {
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var service = CreateService();

        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 5) }, ReservationKind.Card, null);
        variant.ReservedQuantity.Should().Be(5);

        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);

        variant.ReservedQuantity.Should().Be(2);
        (await _context.SumActiveReservedQuantityAsync(variant.Id)).Should().Be(2);
    }

    // ---- P2 consume ----

    [Fact]
    public async Task ConsumeLine_WithActiveHold_DecrementsStockAndReserved_FlipsConsumed()
    {
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);

        var outcome = await service.ConsumeLineAsync(cartId, variant.Id, orderId, 2, ReservationKind.Card);

        outcome.Should().Be(ReservationConsumeOutcome.Consumed);
        variant.StockQuantity.Should().Be(8);
        variant.ReservedQuantity.Should().Be(0);
        _context.StockReservations.Single(r => r.CartId == cartId).Status.Should().Be(ReservationStatus.Consumed);
        _context.StockReservations.Single(r => r.CartId == cartId).OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task ConsumeLine_QuantityMismatch_ReturnsUnavailable_WithoutSelling()
    {
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);

        // The order line quantity (3) disagrees with the hold (2) — must not silently mis-sell.
        var outcome = await service.ConsumeLineAsync(cartId, variant.Id, Guid.NewGuid(), 3, ReservationKind.Card);

        outcome.Should().Be(ReservationConsumeOutcome.Unavailable);
        variant.StockQuantity.Should().Be(10);
        variant.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task ConsumeLine_AlreadyConsumedByThisOrder_IsIdempotent_NoDoubleDecrement()
    {
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);

        var first = await service.ConsumeLineAsync(cartId, variant.Id, orderId, 2, ReservationKind.Card);
        var second = await service.ConsumeLineAsync(cartId, variant.Id, orderId, 2, ReservationKind.Card);

        first.Should().Be(ReservationConsumeOutcome.Consumed);
        second.Should().Be(ReservationConsumeOutcome.AlreadyConsumedByThisOrder);
        variant.StockQuantity.Should().Be(8); // decremented once, not twice
    }

    [Fact]
    public async Task ConsumeLine_StaleConsumedHoldFromPriorOrder_ReSellsForTheNewOrder()
    {
        // A PRIOR order left a Consumed row on this persistent (reused) cart, and the new order's fresh Active
        // hold is absent (swept). The new order MUST re-reserve + sell — a Consumed row from another order is NOT
        // a reason to skip the sale (that would under-sell and leak phantom stock). [council High #1]
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var priorOrder = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);
        await service.ConsumeLineAsync(cartId, variant.Id, priorOrder, 2, ReservationKind.Card); // stock 10 -> 8

        // A brand-new order on the same reused cart with no fresh Active hold present.
        var outcome = await service.ConsumeLineAsync(cartId, variant.Id, Guid.NewGuid(), 2, ReservationKind.Card);

        outcome.Should().Be(ReservationConsumeOutcome.Consumed);
        variant.StockQuantity.Should().Be(6); // re-sold for the new order (8 -> 6), not skipped
    }

    [Fact]
    public async Task ConsumeLine_GenuinelyAbsentHold_ReReservesThenConsumes()
    {
        // No prior hold (e.g. it expired/was swept between reserve and order-create) — re-reserve then sell.
        var variant = SeedVariant(stock: 5);
        var cartId = Guid.NewGuid();

        var outcome = await CreateService().ConsumeLineAsync(cartId, variant.Id, Guid.NewGuid(), 2, ReservationKind.Card);

        outcome.Should().Be(ReservationConsumeOutcome.Consumed);
        variant.StockQuantity.Should().Be(3);
        variant.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task ConsumeLine_GenuinelyAbsentHold_NoStock_ReturnsUnavailable()
    {
        var variant = SeedVariant(stock: 0);
        var cartId = Guid.NewGuid();

        var outcome = await CreateService().ConsumeLineAsync(cartId, variant.Id, Guid.NewGuid(), 1, ReservationKind.Card);

        outcome.Should().Be(ReservationConsumeOutcome.Unavailable);
        variant.StockQuantity.Should().Be(0);
    }

    // ---- P3 release / reconcile ----

    [Fact]
    public async Task ReleaseCart_ReleasesAllActiveHolds_AndDropsCounters()
    {
        var a = SeedVariant(stock: 10);
        var b = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(a.Id, 2), Line(b.Id, 3) }, ReservationKind.Card, null);

        await service.ReleaseCartAsync(cartId);

        a.ReservedQuantity.Should().Be(0);
        b.ReservedQuantity.Should().Be(0);
        ActiveHoldCount(cartId).Should().Be(0);
    }

    [Fact]
    public async Task ReconcileCartToQuantities_ShrinksToTarget_AndReleasesMissingVariants()
    {
        var kept = SeedVariant(stock: 10);
        var dropped = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(kept.Id, 5), Line(dropped.Id, 3) }, ReservationKind.Card, null);

        // Cart now holds only 2 of `kept`; `dropped` was removed entirely.
        await service.ReconcileCartToQuantitiesAsync(
            cartId, new Dictionary<Guid, int> { [kept.Id] = 2 });

        kept.ReservedQuantity.Should().Be(2);
        dropped.ReservedQuantity.Should().Be(0);
        (await _context.SumActiveReservedQuantityAsync(kept.Id)).Should().Be(2);
        ActiveHoldCount(cartId).Should().Be(1);
    }

    [Fact]
    public async Task ReconcileCartToQuantities_NeverGrowsAHold()
    {
        var variant = SeedVariant(stock: 10);
        var cartId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveCartAsync(cartId, new[] { Line(variant.Id, 2) }, ReservationKind.Card, null);

        // A higher target must NOT grow the hold (growth only happens at reserve, stock-gated).
        await service.ReconcileCartToQuantitiesAsync(
            cartId, new Dictionary<Guid, int> { [variant.Id] = 5 });

        variant.ReservedQuantity.Should().Be(2);
    }

    // ---- R sweeper ----

    [Fact]
    public async Task Sweep_ExpiresElapsedCardHold_ReconcilesCounter_ReturnsCount()
    {
        var variant = SeedVariant(stock: 10, reserved: 2);
        var cartId = Guid.NewGuid();
        var hold = new StockReservation(variant.Id, cartId, 2, DateTime.UtcNow.AddMinutes(-5), ReservationKind.Card);
        _context.AddStockReservation(hold);

        var expired = await CreateService().SweepExpiredHoldsAsync(batchSize: 10);

        expired.Should().Be(1);
        hold.Status.Should().Be(ReservationStatus.Expired);
        variant.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_LeavesFreshHoldActive()
    {
        var variant = SeedVariant(stock: 10, reserved: 2);
        var cartId = Guid.NewGuid();
        var hold = new StockReservation(variant.Id, cartId, 2, DateTime.UtcNow.AddMinutes(10), ReservationKind.Card);
        _context.AddStockReservation(hold);

        var expired = await CreateService().SweepExpiredHoldsAsync(batchSize: 10);

        expired.Should().Be(0);
        hold.Status.Should().Be(ReservationStatus.Active);
        variant.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task Sweep_IsIdempotentUnderRetry_DecrementsCounterOnce()
    {
        // A commit-unknown retry re-runs the per-hold delegate; the expiry CAS is a no-op on the second pass, so
        // the counter drops exactly once. BREAK-PROBE: a relative decrement outside the CAS gate would double it.
        _context.ExecutionStrategyAttempts = 2;
        var variant = SeedVariant(stock: 10, reserved: 2);
        var cartId = Guid.NewGuid();
        var hold = new StockReservation(variant.Id, cartId, 2, DateTime.UtcNow.AddMinutes(-5), ReservationKind.Card);
        _context.AddStockReservation(hold);

        await CreateService().SweepExpiredHoldsAsync(batchSize: 10);

        hold.Status.Should().Be(ReservationStatus.Expired);
        variant.ReservedQuantity.Should().Be(0);
    }
}
