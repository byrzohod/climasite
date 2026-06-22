using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Commands;

public class MergeGuestCartCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private MergeGuestCartCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private (Product product, ProductVariant variant) SeedProduct(string sku, int stock)
    {
        var product = new Product(sku, $"{sku} AC", $"{sku.ToLower()}-ac", 300m);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(stock);
        product.Variants.Add(variant);
        _context.AddProduct(product);
        return (product, variant);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new MergeGuestCartCommand { GuestSessionId = "guest-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be authenticated");
    }

    [Fact]
    public async Task Handle_WhenNoGuestCart_ReturnsExistingUserCart()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct("MG-1", 10);
        var userCart = new Core.Entities.Cart(userId, null);
        userCart.AddItem(product.Id, variant.Id, 2, 300m);
        _context.AddCart(userCart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new MergeGuestCartCommand { GuestSessionId = "no-such-guest" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenNoGuestCartAndNoUserCart_ReturnsEmptyCart()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new MergeGuestCartCommand { GuestSessionId = "nothing" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MergesGuestItemsIntoNewUserCart_AndRemovesGuestCart()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct("MG-2", 10);
        var guestCart = new Core.Entities.Cart(null, "guest-merge");
        guestCart.AddItem(product.Id, variant.Id, 3, 300m);
        _context.AddCart(guestCart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new MergeGuestCartCommand { GuestSessionId = "guest-merge" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(3);

        var carts = await _context.Carts.ToListAsync();
        carts.Should().ContainSingle().Which.UserId.Should().Be(userId, "guest cart is removed after merge");
    }

    [Fact]
    public async Task Handle_MergesQuantities_CappedAtAvailableStock()
    {
        var userId = Guid.NewGuid();
        var (product, variant) = SeedProduct("MG-3", 5);
        var userCart = new Core.Entities.Cart(userId, null);
        userCart.AddItem(product.Id, variant.Id, 3, 300m);
        _context.AddCart(userCart);

        var guestCart = new Core.Entities.Cart(null, "guest-cap");
        guestCart.AddItem(product.Id, variant.Id, 4, 300m);
        _context.AddCart(guestCart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new MergeGuestCartCommand { GuestSessionId = "guest-cap" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(5, "3 + 4 = 7 is capped at the 5 available in stock");
    }
}
