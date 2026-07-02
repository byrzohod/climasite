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

public class RemoveFromCartCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private RemoveFromCartCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object, new StockReservationService(_context, new ReservationOptions()));

    [Fact]
    public async Task Handle_WhenNoCart_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new RemoveFromCartCommand { ItemId = Guid.NewGuid(), GuestSessionId = "guest-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cart not found");
    }

    [Fact]
    public async Task Handle_WhenItemNotInCart_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new RemoveFromCartCommand { ItemId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cart item not found");
    }

    [Fact]
    public async Task Handle_RemovesItem_ForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var cart = new Core.Entities.Cart(userId, null);
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2, 50m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new RemoveFromCartCommand { ItemId = item.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Carts.Include(c => c.Items).SingleAsync();
        saved.Items.Should().ContainSingle().Which.Id.Should().NotBe(item.Id);
    }

    [Fact]
    public async Task Handle_RemovesItem_ForGuestSession()
    {
        var cart = new Core.Entities.Cart(null, "guest-9");
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new RemoveFromCartCommand { ItemId = item.Id, GuestSessionId = "guest-9" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Carts.Include(c => c.Items).SingleAsync();
        saved.Items.Should().BeEmpty();
    }
}
