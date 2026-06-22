using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Queries;

public class GetUserAddressesQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private GetUserAddressesQueryHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private Address SeedAddress(Guid userId, string fullName, bool isDefault, DateTime updatedAt)
    {
        var address = new Address(userId, fullName, "1 St", "City", "1000", "Bulgaria", "BG");
        address.SetDefault(isDefault);
        var property = typeof(BaseEntity).GetProperty("UpdatedAt");
        property?.SetValue(address, updatedAt);
        _context.Addresses.Add(address);
        return address;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsEmptyList()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAddresses_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUsersAddresses()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        SeedAddress(userId, "Mine A", isDefault: true, DateTime.UtcNow);
        SeedAddress(userId, "Mine B", isDefault: false, DateTime.UtcNow);
        SeedAddress(otherUserId, "Theirs", isDefault: true, DateTime.UtcNow);

        var result = await CreateHandler().Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.FullName == "Mine A" || a.FullName == "Mine B");
    }

    [Fact]
    public async Task Handle_OrdersDefaultFirst_ThenByUpdatedAtDescending()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        // Non-default addresses with distinct UpdatedAt timestamps.
        SeedAddress(userId, "Older", isDefault: false, DateTime.UtcNow.AddDays(-2));
        SeedAddress(userId, "Newer", isDefault: false, DateTime.UtcNow.AddDays(-1));
        // The default must surface first regardless of timestamp.
        SeedAddress(userId, "Default", isDefault: true, DateTime.UtcNow.AddDays(-5));

        var result = await CreateHandler().Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].FullName.Should().Be("Default", "default address is ordered first");
        result[0].IsDefault.Should().BeTrue();
        result[1].FullName.Should().Be("Newer", "then most-recently-updated");
        result[2].FullName.Should().Be("Older");
    }
}
