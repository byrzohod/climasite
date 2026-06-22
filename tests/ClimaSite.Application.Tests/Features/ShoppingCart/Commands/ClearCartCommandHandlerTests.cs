using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Commands;

public class ClearCartCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private ClearCartCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    [Fact]
    public async Task Handle_WhenNoCart_ReturnsSuccess_Noop()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new ClearCartCommand { GuestSessionId = "missing" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("clearing a non-existent cart is idempotent");
    }

    [Fact]
    public async Task Handle_ClearsAllItems_ForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2, 50m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ClearCartCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Carts.Include(c => c.Items).SingleAsync();
        saved.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClearsAllItems_ForGuestSession()
    {
        var cart = new Core.Entities.Cart(null, "guest-clear");
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new ClearCartCommand { GuestSessionId = "guest-clear" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Carts.Include(c => c.Items).SingleAsync();
        saved.Items.Should().BeEmpty();
    }
}
