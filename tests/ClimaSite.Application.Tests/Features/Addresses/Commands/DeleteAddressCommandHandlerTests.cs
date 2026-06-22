using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Commands;

public class DeleteAddressCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private DeleteAddressCommandHandler CreateHandler() =>
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
            new DeleteAddressCommand { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task Handle_WhenAddressNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new DeleteAddressCommand { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Address not found");
    }

    [Fact]
    public async Task Handle_WhenAddressBelongsToAnotherUser_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var otherAddress = SeedAddress(otherUserId);

        var result = await CreateHandler().Handle(
            new DeleteAddressCommand { AddressId = otherAddress.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Address not found");

        var stored = await _context.Addresses.ToListAsync();
        stored.Should().Contain(a => a.Id == otherAddress.Id, "the other user's address must not be removed");
    }

    [Fact]
    public async Task Handle_DeletesNonDefaultAddress()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var keep = SeedAddress(userId, isDefault: true);
        var remove = SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(
            new DeleteAddressCommand { AddressId = remove.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var stored = await _context.Addresses.ToListAsync();
        stored.Should().ContainSingle().Which.Id.Should().Be(keep.Id);
    }

    [Fact]
    public async Task Handle_DeletingDefault_PromotesAnotherAddressToDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var defaultAddress = SeedAddress(userId, isDefault: true);
        var other = SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(
            new DeleteAddressCommand { AddressId = defaultAddress.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stored = await _context.Addresses.ToListAsync();
        stored.Should().ContainSingle();
        stored.Single(a => a.Id == other.Id).IsDefault.Should().BeTrue("the remaining address becomes default");
    }

    [Fact]
    public async Task Handle_DeletingOnlyDefault_LeavesNoAddresses()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var soleAddress = SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(
            new DeleteAddressCommand { AddressId = soleAddress.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Addresses.ToListAsync()).Should().BeEmpty();
    }
}
