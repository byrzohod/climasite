using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Features.Reservations;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Commands;

public class UpdateCartItemCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private UpdateCartItemCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object, new StockReservationService(_context, new ReservationOptions()));

    private (Product product, ProductVariant variant) SeedProduct(int stock)
    {
        var product = new Product("UP-001", "Updatable AC", "updatable-ac", 400m);
        var variant = new ProductVariant(product.Id, "UP-001-STD", "Standard");
        variant.SetStockQuantity(stock);
        product.Variants.Add(variant);
        _context.AddProduct(product);
        return (product, variant);
    }

    [Fact]
    public async Task Handle_WhenNoCart_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new UpdateCartItemCommand { ItemId = Guid.NewGuid(), Quantity = 1, GuestSessionId = "g1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cart not found");
    }

    [Fact]
    public async Task Handle_WhenItemMissing_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new UpdateCartItemCommand { ItemId = Guid.NewGuid(), Quantity = 2 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cart item not found");
    }

    [Fact]
    public async Task Handle_QuantityZero_RemovesItem()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct(stock: 10);
        var cart = new Core.Entities.Cart(userId, null);
        var item = cart.AddItem(product.Id, variant.Id, 2, 400m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new UpdateCartItemCommand { ItemId = item.Id, Quantity = 0 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(0);
        var saved = await _context.Carts.Include(c => c.Items).SingleAsync();
        saved.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsStock_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct(stock: 3);
        var cart = new Core.Entities.Cart(userId, null);
        var item = cart.AddItem(product.Id, variant.Id, 1, 400m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new UpdateCartItemCommand { ItemId = item.Id, Quantity = 5 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 3 items available");
    }

    [Fact]
    public async Task Handle_UpdatesQuantity_AndRecomputesTotals()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct(stock: 10);
        var cart = new Core.Entities.Cart(userId, null);
        var item = cart.AddItem(product.Id, variant.Id, 1, 400m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new UpdateCartItemCommand { ItemId = item.Id, Quantity = 3 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(3);
        result.Value.Subtotal.Should().Be(1200m);
        result.Value.Tax.Should().Be(240m);
        result.Value.Total.Should().Be(1440m);
    }
}
