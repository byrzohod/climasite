using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Commands;

public class SetDefaultAddressCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private SetDefaultAddressCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private Address SeedAddress(Guid userId, bool isDefault = false)
    {
        var address = new Address(userId, "Person", "1 St", "City", "1000", "Bulgaria", "BG");
        address.SetDefault(isDefault);
        _context.Addresses.Add(address);
        return address;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task Handle_WhenAddressNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Address not found");
    }

    [Fact]
    public async Task Handle_WhenAddressBelongsToAnotherUser_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var otherAddress = SeedAddress(otherUserId, isDefault: false);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = otherAddress.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Address not found");
    }

    [Fact]
    public async Task Handle_WhenAlreadyDefault_ReturnsItUnchanged()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var address = SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = address.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(address.Id);
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PromotesAddress_AndClearsPreviousDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var previousDefault = SeedAddress(userId, isDefault: true);
        var target = SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = target.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(target.Id);
        result.Value.IsDefault.Should().BeTrue();

        var stored = await _context.Addresses.ToListAsync();
        stored.Single(a => a.Id == previousDefault.Id).IsDefault.Should().BeFalse();
        stored.Single(a => a.Id == target.Id).IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DoesNotAffectOtherUsersDefault()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var otherDefault = SeedAddress(otherUserId, isDefault: true);
        var target = SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(
            new SetDefaultAddressCommand { AddressId = target.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = await _context.Addresses.ToListAsync();
        stored.Single(a => a.Id == otherDefault.Id).IsDefault.Should().BeTrue();
    }
}
