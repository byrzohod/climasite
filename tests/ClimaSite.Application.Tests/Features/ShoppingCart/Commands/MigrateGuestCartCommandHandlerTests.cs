using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Commands;

/// <summary>
/// INV-01 A1: migrating a returning guest's legacy cart onto the trusted cookie id. Covers the three
/// convergent outcomes (re-key / merge+delete / no-op) plus idempotency under a simulated commit-unknown
/// retry (<c>ExecutionStrategyAttempts = 2</c>) — the effect must land exactly once.
/// </summary>
public class MigrateGuestCartCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private MigrateGuestCartCommandHandler CreateHandler() => new(_context);

    private (Product product, ProductVariant variant) SeedProduct(string sku, int stock)
    {
        var product = new Product(sku, $"{sku} AC", $"{sku.ToLower()}-ac", 300m);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(stock);
        product.Variants.Add(variant);
        _context.AddProduct(product);
        return (product, variant);
    }

    private Core.Entities.Cart SeedCart(string sessionId, Guid productId, Guid variantId, int quantity)
    {
        var cart = new Core.Entities.Cart(null, sessionId);
        cart.AddItem(productId, variantId, quantity, 300m);
        _context.AddCart(cart);
        return cart;
    }

    [Fact]
    public async Task Handle_NoCookieCart_ReKeysLegacyCartOntoTheCookieId()
    {
        var (product, variant) = SeedProduct("MI-1", 10);
        SeedCart("legacy-1", product.Id, variant.Id, 2);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-1", "cookie-1"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle();
        carts[0].SessionId.Should().Be("cookie-1", "the legacy cart is re-keyed onto the cookie id");
        carts[0].Items.Should().ContainSingle().Which.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_BothCartsExist_MergesLegacyIntoCookie_AndDeletesLegacy()
    {
        var (product, variant) = SeedProduct("MI-2", 10);
        SeedCart("cookie-2", product.Id, variant.Id, 2);
        SeedCart("legacy-2", product.Id, variant.Id, 3);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-2", "cookie-2"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle("the legacy cart is removed after merging");
        carts[0].SessionId.Should().Be("cookie-2");
        carts[0].Items.Should().ContainSingle().Which.Quantity.Should().Be(5, "2 + 3 combined");
    }

    [Fact]
    public async Task Handle_Merge_CapsCombinedQuantityAtAvailableStock()
    {
        var (product, variant) = SeedProduct("MI-3", 5);
        SeedCart("cookie-3", product.Id, variant.Id, 3);
        SeedCart("legacy-3", product.Id, variant.Id, 4);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-3", "cookie-3"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle().Which.Items.Should().ContainSingle()
            .Which.Quantity.Should().Be(5, "3 + 4 = 7 is capped at the 5 in stock");
    }

    [Fact]
    public async Task Handle_Merge_AddsLegacyOnlyItemsToTheCookieCart()
    {
        var (productA, variantA) = SeedProduct("MI-4A", 10);
        var (productB, variantB) = SeedProduct("MI-4B", 10);
        SeedCart("cookie-4", productA.Id, variantA.Id, 1);
        SeedCart("legacy-4", productB.Id, variantB.Id, 2);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-4", "cookie-4"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        var cookieCart = carts.Should().ContainSingle().Which;
        cookieCart.SessionId.Should().Be("cookie-4");
        cookieCart.Items.Should().HaveCount(2);
        cookieCart.Items.Should().Contain(i => i.ProductId == productB.Id && i.Quantity == 2);
    }

    [Theory]
    [InlineData("", "cookie")]
    [InlineData("legacy", "")]
    [InlineData("same", "same")]
    public async Task Handle_NoOp_WhenIdsAreEmptyOrEqual(string legacy, string cookie)
    {
        var (product, variant) = SeedProduct("MI-5", 10);
        SeedCart("legacy", product.Id, variant.Id, 2);

        await CreateHandler().Handle(new MigrateGuestCartCommand(legacy, cookie), CancellationToken.None);

        var carts = await _context.Carts.ToListAsync();
        carts.Should().ContainSingle().Which.SessionId.Should().Be("legacy", "no migration should have run");
    }

    [Fact]
    public async Task Handle_NoOp_WhenLegacyCartDoesNotExist()
    {
        var (product, variant) = SeedProduct("MI-6", 10);
        SeedCart("cookie-6", product.Id, variant.Id, 1);

        await CreateHandler().Handle(new MigrateGuestCartCommand("absent-legacy", "cookie-6"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle();
        carts[0].SessionId.Should().Be("cookie-6");
        carts[0].Items.Should().ContainSingle().Which.Quantity.Should().Be(1, "the cookie cart is untouched");
    }

    [Fact]
    public async Task Handle_ReKey_IsIdempotentUnderARetry()
    {
        _context.ExecutionStrategyAttempts = 2; // simulate a commit-unknown retry
        var (product, variant) = SeedProduct("MI-7", 10);
        SeedCart("legacy-7", product.Id, variant.Id, 2);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-7", "cookie-7"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle();
        carts[0].SessionId.Should().Be("cookie-7");
        carts[0].Items.Should().ContainSingle().Which.Quantity.Should().Be(2, "re-key applied exactly once");
    }

    [Fact]
    public async Task Handle_Merge_IsIdempotentUnderARetry()
    {
        _context.ExecutionStrategyAttempts = 2; // simulate a commit-unknown retry
        var (product, variant) = SeedProduct("MI-8", 10);
        SeedCart("cookie-8", product.Id, variant.Id, 2);
        SeedCart("legacy-8", product.Id, variant.Id, 3);

        await CreateHandler().Handle(new MigrateGuestCartCommand("legacy-8", "cookie-8"), CancellationToken.None);

        var carts = await _context.Carts.Include(c => c.Items).ToListAsync();
        carts.Should().ContainSingle("the legacy cart is removed exactly once");
        carts[0].Items.Should().ContainSingle().Which.Quantity.Should()
            .Be(5, "the merge is applied once, not twice — a second application would read 8");
    }
}
