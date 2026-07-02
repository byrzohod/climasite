using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ClimaSite.Api.Tests.Features.Reservations;

/// <summary>
/// INV-01 Wave B — bank-transfer hold-with-expiry break-probes over the real stack (Testcontainers): a bank
/// order HOLDS its stock at create (no physical decrement), an admin mark-paid CONSUMES the hold, an unpaid
/// hold past its TTL is swept + the order auto-cancelled (stock never leaked), cancel releases an unpaid hold,
/// and a Consumed card order restocks atomically on cancel. Two break-probes protect the available-gate and the
/// bank filtered-unique index.
/// </summary>
public class BankTransferReservationTests : IntegrationTestBase
{
    public BankTransferReservationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // A bank order placed via checkout HOLDS its stock: reserved += qty, physical stock UNCHANGED, and a
    // Kind=BankTransfer Active hold is linked to the order (cart_id null).
    [Fact]
    public async Task BankOrder_AtCreate_HoldsStock_WithoutDecrementing()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        using var client = await CreateAuthedClientWithCartItemAsync($"bank-{Guid.NewGuid():N}@t.com", productId, variantId);

        var order = await client.PostAsJsonAsync("/api/orders", BankOrderBody());
        order.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await order.Content.ReadFromJsonAsync<OrderResult>())!.Id;

        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(5, "a bank order holds stock, it does NOT decrement it at create");
        reserved.Should().Be(1, "the unit is held against the order");

        var hold = await SingleReservationForOrderAsync(orderId);
        hold.Status.Should().Be(ReservationStatus.Active);
        hold.Kind.Should().Be(ReservationKind.BankTransfer);
        hold.CartId.Should().BeNull("a bank hold is keyed on the order, not the cart");
    }

    // [council High] A bank order must NEVER carry a PaymentIntent — the Stripe webhook flips PI-backed orders
    // straight to Paid with no order lock and no bank-hold consume, so a bank+PI order would go Paid with stock
    // never taken. The invalid combination is rejected at create: 400, and no hold is reserved.
    [Fact]
    public async Task BankOrder_WithPaymentIntent_IsRejected_NoHoldReserved()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        using var client = await CreateAuthedClientWithCartItemAsync($"bankpi-{Guid.NewGuid():N}@t.com", productId, variantId);

        var resp = await client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "bankpi@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentMethod = "bank",
            paymentIntentId = "pi_bank_should_reject"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, "a bank order cannot carry a payment intent");
        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(5, "the rejected order took no stock");
        reserved.Should().Be(0, "no bank hold was reserved for the rejected order");
        (await ActiveReservationCountForVariantAsync(variantId)).Should().Be(0, "no reservation row was inserted");
    }

    // Cancelling an UNPAID bank order releases the hold (reserved → 0) and does NOT restock (nothing was
    // physically decremented).
    [Fact]
    public async Task Cancel_UnpaidBankOrder_ReleasesHold_NoRestock()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        using var client = await CreateAuthedClientWithCartItemAsync($"bankc-{Guid.NewGuid():N}@t.com", productId, variantId);

        var order = await client.PostAsJsonAsync("/api/orders", BankOrderBody());
        var orderId = (await order.Content.ReadFromJsonAsync<OrderResult>())!.Id;
        (await ReadCountersAsync(variantId)).reserved.Should().Be(1);

        var cancel = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { reason = "changed mind" });
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(0, "the bank hold is released on cancel");
        stock.Should().Be(5, "an unpaid bank order was never decremented — cancel must NOT restock");
        (await SingleReservationForOrderAsync(orderId)).Status.Should().Be(ReservationStatus.Released);
    }

    // Admin mark-paid CONSUMES the bank hold: physical stock -1, reserved -1, the hold flips Consumed, order Paid.
    [Fact]
    public async Task MarkBankOrderPaid_ConsumesHold_DecrementsStock()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        var orderId = await CreateBankOrderWithHoldAsync(productId, variantId, quantity: 1);
        (await ReadCountersAsync(variantId)).Should().Be((5, 1));

        using (var scope = Factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = nameof(OrderStatus.Paid),
                NotifyCustomer = false
            });
            result.IsSuccess.Should().BeTrue();
        }

        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(4, "mark-paid physically sells the held unit");
        reserved.Should().Be(0);
        (await SingleReservationForOrderAsync(orderId)).Status.Should().Be(ReservationStatus.Consumed);
        (await OrderStatusAsync(orderId)).Should().Be(OrderStatus.Paid);
    }

    // The sweeper expires an unpaid bank hold past its lease AND auto-cancels the order — stock is never leaked.
    [Fact]
    public async Task Sweeper_ExpiredBankHold_ReleasesHold_AndCancelsOrder_StockNeverLeaked()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        var orderId = await SeedExpiredBankOrderAsync(productId, variantId, quantity: 1);
        (await ReadCountersAsync(variantId)).Should().Be((5, 1), "the expired-but-unswept hold still counts");

        var cancelled = await SweepBankAsync();

        cancelled.Should().Be(1);
        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(0, "the swept hold no longer counts");
        stock.Should().Be(5, "an unpaid bank hold never physically decremented stock — nothing leaked");
        (await SingleReservationForOrderAsync(orderId)).Status.Should().Be(ReservationStatus.Expired);
        (await OrderStatusAsync(orderId)).Should().Be(OrderStatus.Cancelled);
    }

    // Cancelling a Consumed CARD order restocks atomically (the IncrementVariantStockAsync path): a card order
    // consumes stock at create, so cancel must add it back.
    [Fact]
    public async Task Cancel_ConsumedCardOrder_RestocksAtomically()
    {
        Factory.PaymentService.Reset();
        var variantId = await SeedProductWithStockAsync(stock: 3);
        var productId = await GetProductIdAsync(variantId);
        using var client = await CreateAuthedClientWithCartItemAsync($"cardc-{Guid.NewGuid():N}@t.com", productId, variantId);

        var intent = await client.PostAsJsonAsync("/api/payments/create-intent", new { shippingMethod = "standard" });
        var paymentIntentId = (await intent.Content.ReadFromJsonAsync<IntentResult>())!.PaymentIntentId;

        var order = await client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "cardc@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentIntentId,
            paymentMethod = "card"
        });
        order.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await order.Content.ReadFromJsonAsync<OrderResult>())!.Id;
        (await ReadCountersAsync(variantId)).stock.Should().Be(2, "a card order consumes stock at create");

        var cancel = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { reason = "return" });
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(3, "cancelling a consumed card order restocks the unit");
        reserved.Should().Be(0);
    }

    // BREAK-PROBE (available-gate): a bank reserve may take only units NOT held by a card checkout. With the last
    // unit held by a card hold, a bank order for the same variant must FAIL. Revert ReserveBankOrderAsync to a
    // plain stock>=qty gate and this over-reserves (reserved climbs to 2 on a stock of 1).
    [Fact]
    public async Task ReserveBankOrder_AvailableGated_CannotStealACardHeldUnit()
    {
        var variantId = await SeedProductWithStockAsync(stock: 1);
        var productId = await GetProductIdAsync(variantId);
        var cartId = await CreateCartAsync();
        await ReserveCardAsync(cartId, variantId); // reserved = 1 (card hold on the only unit)

        var orderId = await CreateEmptyBankOrderAsync(productId, variantId);
        var held = await ReserveBankOrderInTxAsync(orderId, variantId, quantity: 1);

        held.Should().BeFalse("the only unit is held by a card checkout");
        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(1, "reserved must not exceed stock");
        stock.Should().Be(1);
    }

    // BREAK-PROBE (filtered-unique index): the bank hold's own UNIQUE (order_id, variant_id) WHERE
    // status='Active' AND kind='BankTransfer' index dedupes a second live hold for the same (order, variant). The
    // first insert succeeding proves the index EXISTS (ON CONFLICT would error without it); the second returning 0
    // proves it dedupes.
    [Fact]
    public async Task BankHold_FilteredUnique_PreventsDuplicateActiveHoldPerOrderVariant()
    {
        var variantId = await SeedProductWithStockAsync(stock: 10);
        var productId = await GetProductIdAsync(variantId);
        var orderId = await CreateEmptyBankOrderAsync(productId, variantId);
        var future = DateTime.UtcNow.AddDays(3);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var first = await db.InsertActiveBankReservationAsync(Guid.NewGuid(), variantId, orderId, 1, future);
        var second = await db.InsertActiveBankReservationAsync(Guid.NewGuid(), variantId, orderId, 1, future);

        first.Should().Be(1, "the first Active bank hold for (order, variant) inserts");
        second.Should().Be(0, "the filtered-unique index dedupes a second live bank hold for the same (order, variant)");
        (await db.StockReservations.CountAsync(r => r.OrderId == orderId && r.Status == ReservationStatus.Active))
            .Should().Be(1);
    }

    // [council High #2] Order-lifecycle serialization: many concurrent mark-paid + cancel races on the same bank
    // order always settle CONSISTENTLY — Paid⇒unit sold, or Cancelled⇒unit not leaked (released, or
    // decremented-then-restocked); reserved back to 0. The forbidden Cancelled-with-decremented-no-restock state
    // never occurs. BREAK-PROBE: remove LockOrderForUpdateAsync from Cancel/UpdateStatus ⇒ a race leaves a
    // Cancelled order whose unit was decremented and never restocked (stock leak) and this fails.
    [Fact]
    public async Task ConcurrentMarkPaidAndCancel_BankOrder_NeverLeaksStock()
    {
        for (var i = 0; i < 15; i++)
        {
            var variantId = await SeedProductWithStockAsync(stock: 5);
            var productId = await GetProductIdAsync(variantId);
            var orderId = await CreateBankOrderWithHoldAsync(productId, variantId, quantity: 1);

            var markPaid = Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                return await mediator.Send(new UpdateOrderStatusCommand
                {
                    OrderId = orderId,
                    Status = nameof(OrderStatus.Paid),
                    NotifyCustomer = false
                });
            });
            var cancel = Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var reservations = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
                var currentUser = new Mock<ICurrentUserService>();
                currentUser.Setup(u => u.IsAdmin).Returns(true);
                var handler = new CancelOrderCommandHandler(db, currentUser.Object, reservations);
                return await handler.Handle(new CancelOrderCommand { OrderId = orderId }, CancellationToken.None);
            });
            await Task.WhenAll(markPaid, cancel);

            var status = await OrderStatusAsync(orderId);
            var (stock, reserved) = await ReadCountersAsync(variantId);
            reserved.Should().Be(0, $"iteration {i}: reserved must settle to 0");
            if (status == OrderStatus.Paid)
            {
                stock.Should().Be(4, $"iteration {i}: a Paid bank order sold its unit");
            }
            else
            {
                status.Should().Be(OrderStatus.Cancelled, $"iteration {i}: the order settled to one of the two consistent states");
                stock.Should().Be(5, $"iteration {i}: a Cancelled order must not leak the unit (released, or decremented-then-restocked)");
            }
        }
    }

    // [council High #1] The consume runs on ANY transition INTO Paid, not just Pending→Paid: a bank order that went
    // Pending→PaymentFailed→Paid must still decrement. BREAK-PROBE: restore the `order.Status == Pending` guard ⇒
    // PaymentFailed→Paid leaves the order Paid with stock unchanged.
    [Fact]
    public async Task MarkBankOrderPaid_FromPaymentFailed_ConsumesHold_DecrementsStock()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        var orderId = await CreateBankOrderWithHoldAsync(productId, variantId, quantity: 1);
        await SetOrderStatusDirectAsync(orderId, OrderStatus.PaymentFailed);

        using (var scope = Factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = nameof(OrderStatus.Paid),
                NotifyCustomer = false
            });
            result.IsSuccess.Should().BeTrue();
        }

        var (stock, reserved) = await ReadCountersAsync(variantId);
        stock.Should().Be(4, "PaymentFailed→Paid must still consume the bank hold");
        reserved.Should().Be(0);
        (await OrderStatusAsync(orderId)).Should().Be(OrderStatus.Paid);
    }

    // [council Medium #3] Mark-paid is ALL-OR-NOTHING. A 2-line bank order with one hold already expired must FAIL
    // and roll the whole tx back — the still-Active line must NOT be decremented. BREAK-PROBE: accept a positive
    // partial `consumed` ⇒ the Active line sells (stock−1) while the expired line does not.
    [Fact]
    public async Task MarkBankOrderPaid_MultiLine_OneHoldPreExpired_Fails_NoPartialDecrement()
    {
        var v1 = await SeedProductWithStockAsync(stock: 5);
        var p1 = await GetProductIdAsync(v1);
        var v2 = await SeedProductWithStockAsync(stock: 5);
        var p2 = await GetProductIdAsync(v2);
        var orderId = await CreateBankOrderTwoLinesAsync(p1, v1, p2, v2);
        await ForceExpireOneBankHoldAsync(orderId, v2);

        using (var scope = Factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = nameof(OrderStatus.Paid),
                NotifyCustomer = false
            });
            result.IsSuccess.Should().BeFalse("a partially-expired bank order cannot be marked paid");
        }

        (await OrderStatusAsync(orderId)).Should().Be(OrderStatus.Pending, "the failed mark-paid rolled back");
        var (stock, reserved) = await ReadCountersAsync(v1);
        stock.Should().Be(5, "the still-Active line must NOT be decremented (all-or-nothing rollback)");
        reserved.Should().Be(1, "the Active hold survives the rolled-back consume");
    }

    // [council Medium #4] An admin cancelling a Pending bank order via the STATUS endpoint releases the hold
    // IMMEDIATELY (reserved→0) rather than leaking it until the sweeper's TTL. BREAK-PROBE: drop the →Cancelled
    // ReleaseBankOrderAsync branch ⇒ reserved stays 1 after the cancel.
    [Fact]
    public async Task AdminStatusCancel_BankOrder_ReleasesHoldImmediately()
    {
        var variantId = await SeedProductWithStockAsync(stock: 5);
        var productId = await GetProductIdAsync(variantId);
        var orderId = await CreateBankOrderWithHoldAsync(productId, variantId, quantity: 1);
        (await ReadCountersAsync(variantId)).Should().Be((5, 1));

        using (var scope = Factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = nameof(OrderStatus.Cancelled),
                NotifyCustomer = false
            });
            result.IsSuccess.Should().BeTrue();
        }

        var (stock, reserved) = await ReadCountersAsync(variantId);
        reserved.Should().Be(0, "admin →Cancelled releases the bank hold immediately (no sweeper wait)");
        stock.Should().Be(5, "the held bank order was never decremented — no restock either");
        (await SingleReservationForOrderAsync(orderId)).Status.Should().Be(ReservationStatus.Released);
        (await OrderStatusAsync(orderId)).Should().Be(OrderStatus.Cancelled);
    }

    // ---- helpers ----

    private async Task<Guid> SeedProductWithStockAsync(int stock)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sku = $"BNK-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Bank {sku}", $"bnk-{sku}", 100m);
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
        await client.PostAsJsonAsync("/api/auth/register", new { email, password, firstName = "Bnk", lastName = "Test" });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())?.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { productId, variantId, quantity = 1 });
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        return client;
    }

    // Creates a persisted bank order carrying one line + reserves its hold (as CreateOrderCommand would).
    private async Task<Guid> CreateBankOrderWithHoldAsync(Guid productId, Guid variantId, int quantity)
    {
        var orderId = await CreateEmptyBankOrderAsync(productId, variantId, quantity);
        var held = await ReserveBankOrderInTxAsync(orderId, variantId, quantity);
        held.Should().BeTrue();
        return orderId;
    }

    private async Task<Guid> CreateEmptyBankOrderAsync(Guid productId, Guid variantId, int quantity = 1)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..14], "bank@test.com");
        order.SetPaymentMethod("bank");
        order.AddItem(productId, variantId, "Bank Product", "Default", "SKU", quantity, 100m);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    // Creates a persisted 2-line bank order + reserves both holds (as CreateOrderCommand would).
    private async Task<Guid> CreateBankOrderTwoLinesAsync(Guid product1, Guid variant1, Guid product2, Guid variant2)
    {
        Guid orderId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = new Order($"ORD-{Guid.NewGuid():N}"[..14], "bank@test.com");
            order.SetPaymentMethod("bank");
            order.AddItem(product1, variant1, "Bank Product 1", "Default", "SKU1", 1, 100m);
            order.AddItem(product2, variant2, "Bank Product 2", "Default", "SKU2", 1, 100m);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();
                var result = await svc.ReserveBankOrderAsync(orderId, new[]
                {
                    new ReservationRequestLine(variant1, 1, "Bank Product 1"),
                    new ReservationRequestLine(variant2, 1, "Bank Product 2")
                });
                result.Succeeded.Should().BeTrue();
                await tx.CommitAsync();
            });
        }

        return orderId;
    }

    // Synthesises a partial-expiry drift: flips ONE of the order's Active bank holds to Expired and drops its
    // counter, so mark-paid's all-or-nothing guard sees a line it can't fulfil.
    private async Task ForceExpireOneBankHoldAsync(Guid orderId, Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hold = await db.StockReservations.FirstAsync(
            r => r.OrderId == orderId && r.VariantId == variantId && r.Status == ReservationStatus.Active);
        hold.SetStatus(ReservationStatus.Expired);
        await db.SaveChangesAsync();
        await db.TryDecrementReservedQuantityAsync(variantId, hold.Quantity);
    }

    private async Task SetOrderStatusDirectAsync(Guid orderId, OrderStatus status)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.SetStatus(status);
        await db.SaveChangesAsync();
    }

    // Seeds a persisted bank order whose hold is already past its lease (for the sweeper), with the counter set.
    private async Task<Guid> SeedExpiredBankOrderAsync(Guid productId, Guid variantId, int quantity)
    {
        var orderId = await CreateEmptyBankOrderAsync(productId, variantId, quantity);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hold = new StockReservation(variantId, null, quantity, DateTime.UtcNow.AddMinutes(-1), ReservationKind.BankTransfer);
        hold.SetOrderId(orderId);
        db.StockReservations.Add(hold);
        await db.SaveChangesAsync();
        await db.SetVariantReservedQuantityAsync(variantId, quantity);
        return orderId;
    }

    private async Task<bool> ReserveBankOrderInTxAsync(Guid orderId, Guid variantId, int quantity)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var result = await svc.ReserveBankOrderAsync(
                orderId, new[] { new ReservationRequestLine(variantId, quantity, "Bank Product") });
            await tx.CommitAsync();
            return result.Succeeded;
        });
    }

    private async Task ReserveCardAsync(Guid cartId, Guid variantId, int quantity = 1)
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        var result = await svc.ReserveCartAsync(
            cartId, new[] { new ReservationRequestLine(variantId, quantity, "AC Unit") }, ReservationKind.Card, userId: null);
        result.Succeeded.Should().BeTrue();
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

    private async Task<int> SweepBankAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await svc.SweepExpiredBankHoldsAsync(batchSize: 100);
    }

    private async Task<(int stock, int reserved)> ReadCountersAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var v = await db.ProductVariants.AsNoTracking().FirstAsync(x => x.Id == variantId);
        return (v.StockQuantity, v.ReservedQuantity);
    }

    private async Task<StockReservation> SingleReservationForOrderAsync(Guid orderId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockReservations.AsNoTracking().SingleAsync(r => r.OrderId == orderId);
    }

    private async Task<int> ActiveReservationCountForVariantAsync(Guid variantId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockReservations.AsNoTracking()
            .CountAsync(r => r.VariantId == variantId && r.Status == ReservationStatus.Active);
    }

    private async Task<OrderStatus> OrderStatusAsync(Guid orderId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => o.Status).FirstAsync();
    }

    private static object BankOrderBody() => new
    {
        customerEmail = "bank@test.com",
        shippingAddress = ValidAddress(),
        shippingMethod = "standard",
        paymentMethod = "bank"
    };

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

    private record OrderResult(Guid Id);

    private record IntentResult(string PaymentIntentId);

    private record LoginResult(string AccessToken);
}
