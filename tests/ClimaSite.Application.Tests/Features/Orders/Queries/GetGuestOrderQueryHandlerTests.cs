using ClimaSite.Application.Features.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Orders.Queries;

public class GetGuestOrderQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetGuestOrderQueryHandler CreateHandler() => new(_context);

    private Order SeedGuestOrder(string token, Guid? userId = null)
    {
        var order = new Order("ORD-GUEST-0001", "guest@example.com");
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        order.SetGuestAccessToken(token);
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task ValidToken_ReturnsOrder()
    {
        var order = SeedGuestOrder("correct-token");

        var result = await CreateHandler().Handle(
            new GetGuestOrderQuery { OrderId = order.Id, Token = "correct-token" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(order.Id);
        result.Value.CustomerEmail.Should().Be("guest@example.com");
    }

    [Fact]
    public async Task WrongToken_ReturnsNotFound()
    {
        var order = SeedGuestOrder("correct-token");

        var result = await CreateHandler().Handle(
            new GetGuestOrderQuery { OrderId = order.Id, Token = "WRONG" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
    }

    [Fact]
    public async Task EmptyToken_ReturnsNotFound()
    {
        var order = SeedGuestOrder("correct-token");

        var result = await CreateHandler().Handle(
            new GetGuestOrderQuery { OrderId = order.Id, Token = "" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AccountOrder_IsNotReachableViaGuestPath_EvenWithToken()
    {
        // An order owned by a user must never be served through the anonymous guest lookup.
        var order = SeedGuestOrder("correct-token", userId: Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new GetGuestOrderQuery { OrderId = order.Id, Token = "correct-token" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
    }

    [Fact]
    public async Task UnknownOrderId_ReturnsNotFound()
    {
        SeedGuestOrder("correct-token");

        var result = await CreateHandler().Handle(
            new GetGuestOrderQuery { OrderId = Guid.NewGuid(), Token = "correct-token" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
