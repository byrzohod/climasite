using ClimaSite.Application.Features.Cart.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Queries;

public class GetCartQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetCartQueryHandler CreateHandler() => new(_context);

    private (Product product, ProductVariant variant) SeedProduct(string sku, decimal basePrice, int stock)
    {
        var product = new Product(sku, $"{sku} AC", $"{sku.ToLower()}-ac", basePrice);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(stock);
        product.Variants.Add(variant);
        _context.AddProduct(product);
        return (product, variant);
    }

    [Fact]
    public async Task Handle_NoIdentifier_ReturnsEmptyCart()
    {
        var result = await CreateHandler().Handle(new GetCartQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(Guid.Empty);
        result.ItemCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyUserCart_ReturnsEmptyCartDto()
    {
        var userId = Guid.NewGuid();
        _context.AddCart(new Core.Entities.Cart(userId, null));

        var result = await CreateHandler().Handle(
            new GetCartQuery { UserId = userId },
            CancellationToken.None);

        result!.ItemCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PopulatedUserCart_MapsItemsAndTotals()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct("GC-1", 250m, 8);
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 2, 250m);
        _context.AddCart(cart);

        var result = await CreateHandler().Handle(
            new GetCartQuery { UserId = userId },
            CancellationToken.None);

        result!.ItemCount.Should().Be(2);
        result.Items.Should().ContainSingle();
        var item = result.Items.First();
        item.ProductName.Should().Be("GC-1 AC");
        item.IsAvailable.Should().BeTrue();
        item.AvailableStock.Should().Be(8);
        result.Subtotal.Should().Be(500m);
        result.Tax.Should().Be(100m);
        result.Total.Should().Be(600m);
    }

    [Fact]
    public async Task Handle_GuestCart_ResolvedBySessionId()
    {
        var (product, variant) = SeedProduct("GC-2", 100m, 5);
        var cart = new Core.Entities.Cart(null, "guest-get");
        cart.AddItem(product.Id, variant.Id, 1, 100m);
        _context.AddCart(cart);

        var result = await CreateHandler().Handle(
            new GetCartQuery { GuestSessionId = "guest-get" },
            CancellationToken.None);

        result!.GuestSessionId.Should().Be("guest-get");
        result.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_VariantWithActiveReservations_AvailableStockExcludesReserved()
    {
        // INV-01 A3: the cart's per-line available cap (AvailableStock / MaxQuantity) must exclude units
        // held by an in-flight checkout, so it reads stock − reserved, not raw stock.
        var userId = Guid.NewGuid();
        var product = new Product("GC-RSV", "GC-RSV AC", "gc-rsv-ac", 250m);
        var variant = new ProductVariant(product.Id, "GC-RSV-STD", "Standard");
        variant.SetStockQuantity(8);
        variant.SetReservedQuantity(3);
        product.Variants.Add(variant);
        _context.AddProduct(product);
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(product.Id, variant.Id, 2, 250m);
        _context.AddCart(cart);

        var result = await CreateHandler().Handle(
            new GetCartQuery { UserId = userId },
            CancellationToken.None);

        result!.Items.Should().ContainSingle();
        result.Items.First().AvailableStock.Should().Be(5, "8 stock − 3 reserved");
    }

    [Fact]
    public async Task Handle_WhenProductMissing_MarksItemUnavailable()
    {
        var userId = Guid.NewGuid();
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 199m);
        _context.AddCart(cart);

        var result = await CreateHandler().Handle(
            new GetCartQuery { UserId = userId },
            CancellationToken.None);

        result!.Items.Should().ContainSingle();
        var item = result.Items.First();
        item.ProductName.Should().Be("Product unavailable");
        item.IsAvailable.Should().BeFalse();
        item.LineTotal.Should().Be(199m);
    }
}
