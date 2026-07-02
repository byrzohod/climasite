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

    // ---- INV-01 Wave B: bank-transfer hold-with-expiry ----

    private Order SeedBankOrder()
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..14], "bank@test.com");
        order.SetPaymentMethod("bank");
        _context.AddOrder(order);
        return order;
    }

    private StockReservation SeedBankHold(Guid orderId, Guid variantId, int quantity, DateTime expiresAt)
    {
        var hold = new StockReservation(variantId, null, quantity, expiresAt, ReservationKind.BankTransfer);
        hold.SetOrderId(orderId);
        _context.AddStockReservation(hold);
        return hold;
    }

    [Fact]
    public async Task ReserveBankOrder_HoldsStock_WithoutDecrementing()
    {
        var variant = SeedVariant(stock: 10);
        var orderId = Guid.NewGuid();

        var result = await CreateService().ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 2) });

        result.Succeeded.Should().BeTrue();
        variant.ReservedQuantity.Should().Be(2);
        variant.StockQuantity.Should().Be(10, "a bank hold reserves but does NOT physically decrement stock");
        var hold = _context.StockReservations.Single(r => r.OrderId == orderId);
        hold.Status.Should().Be(ReservationStatus.Active);
        hold.Kind.Should().Be(ReservationKind.BankTransfer);
        hold.CartId.Should().BeNull("a bank hold is keyed on the order, not a cart");
    }

    [Fact]
    public async Task ReserveBankOrder_InsufficientStock_Fails_NamesItem_NoReserve()
    {
        var variant = SeedVariant(stock: 1);

        var result = await CreateService().ReserveBankOrderAsync(
            Guid.NewGuid(), new[] { Line(variant.Id, 5, "Almost sold out") });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Almost sold out");
        variant.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task ReserveBankOrder_CalledTwiceForSameOrder_IsIdempotent_NoDoubleCount()
    {
        // A commit-unknown retry re-runs the reserve for the same order. The (order, variant) idempotency check
        // reuses the existing Active hold and does NOT re-increment. BREAK-PROBE: drop the `existing` check and
        // the second call increments reserved to 4 while Σ Active stays 1 hold ⇒ drift.
        var variant = SeedVariant(stock: 10);
        var orderId = Guid.NewGuid();
        var service = CreateService();

        await service.ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 2) });
        await service.ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 2) });

        variant.ReservedQuantity.Should().Be(2, "the own hold is reused, not double-counted");
        _context.StockReservations.Count(r => r.OrderId == orderId && r.Status == ReservationStatus.Active).Should().Be(1);
    }

    [Fact]
    public async Task ConsumeBankOrder_DecrementsStockAndReserved_FlipsConsumed()
    {
        var variant = SeedVariant(stock: 10);
        var orderId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 2) });

        var consumed = await service.ConsumeBankOrderAsync(orderId);

        consumed.ExpectedHolds.Should().Be(1);
        consumed.ConsumedHolds.Should().Be(1);
        consumed.AllConsumed.Should().BeTrue();
        variant.StockQuantity.Should().Be(8, "mark-paid physically sells the held units");
        variant.ReservedQuantity.Should().Be(0);
        _context.StockReservations.Single(r => r.OrderId == orderId).Status.Should().Be(ReservationStatus.Consumed);
    }

    [Fact]
    public async Task ConsumeBankOrder_IsIdempotent_NoDoubleDecrement()
    {
        var variant = SeedVariant(stock: 10);
        var orderId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 2) });

        var first = await service.ConsumeBankOrderAsync(orderId);
        var second = await service.ConsumeBankOrderAsync(orderId);

        first.AllConsumed.Should().BeTrue();
        first.ConsumedHolds.Should().Be(1);
        // The second pass finds no Active holds (already Consumed) — nothing left to sell, so NOT AllConsumed.
        second.ExpectedHolds.Should().Be(0);
        second.ConsumedHolds.Should().Be(0);
        second.AllConsumed.Should().BeFalse();
        variant.StockQuantity.Should().Be(8, "decremented exactly once");
    }

    [Fact]
    public async Task ReleaseBankOrder_DropsReserved_NoRestock()
    {
        var variant = SeedVariant(stock: 10);
        var orderId = Guid.NewGuid();
        var service = CreateService();
        await service.ReserveBankOrderAsync(orderId, new[] { Line(variant.Id, 3) });

        await service.ReleaseBankOrderAsync(orderId);

        variant.ReservedQuantity.Should().Be(0);
        variant.StockQuantity.Should().Be(10, "a held bank order was never decremented, so cancel must NOT restock");
        _context.StockReservations.Single(r => r.OrderId == orderId).Status.Should().Be(ReservationStatus.Released);
    }

    [Fact]
    public async Task SweepBank_ExpiredHold_ReleasesHold_AndCancelsOrder_StockNeverLeaked()
    {
        var variant = SeedVariant(stock: 10, reserved: 2);
        var order = SeedBankOrder();
        var hold = SeedBankHold(order.Id, variant.Id, quantity: 2, expiresAt: DateTime.UtcNow.AddMinutes(-5));

        var cancelled = await CreateService().SweepExpiredBankHoldsAsync(batchSize: 10);

        cancelled.Should().Be(1);
        hold.Status.Should().Be(ReservationStatus.Expired);
        variant.ReservedQuantity.Should().Be(0, "the expired hold no longer counts");
        variant.StockQuantity.Should().Be(10, "an unpaid bank hold never physically decremented stock");
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task SweepBank_FreshHold_LeftActive_OrderStaysPending()
    {
        var variant = SeedVariant(stock: 10, reserved: 2);
        var order = SeedBankOrder();
        var hold = SeedBankHold(order.Id, variant.Id, quantity: 2, expiresAt: DateTime.UtcNow.AddDays(3));

        var cancelled = await CreateService().SweepExpiredBankHoldsAsync(batchSize: 10);

        cancelled.Should().Be(0);
        hold.Status.Should().Be(ReservationStatus.Active);
        order.Status.Should().Be(OrderStatus.Pending);
        variant.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task SweepBank_HoldAlreadyConsumed_DoesNotCancelPaidOrder()
    {
        // A mark-paid consumed the hold + paid the order before the sweep. The sweeper only scans Active holds, so
        // a Consumed hold is never selected — the paid order is left untouched (no spurious cancel).
        var variant = SeedVariant(stock: 8, reserved: 0);
        var order = SeedBankOrder();
        order.SetStatus(OrderStatus.Paid);
        var hold = SeedBankHold(order.Id, variant.Id, quantity: 2, expiresAt: DateTime.UtcNow.AddMinutes(-5));
        hold.SetStatus(ReservationStatus.Consumed);

        var cancelled = await CreateService().SweepExpiredBankHoldsAsync(batchSize: 10);

        cancelled.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Paid);
    }
}
