using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Queries;

public class GetAddressByIdQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private GetAddressByIdQueryHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private Address SeedAddress(Guid userId)
    {
        var address = new Address(userId, "Person", "1 St", "City", "1000", "Bulgaria", "BG");
        address.SetAddressLine2("Floor 2");
        address.SetState("Region");
        address.SetPhone("+359700000000");
        address.SetType(AddressType.Billing);
        _context.Addresses.Add(address);
        return address;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsNull()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new GetAddressByIdQuery { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAddressNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new GetAddressByIdQuery { AddressId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAddressBelongsToAnotherUser_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var otherAddress = SeedAddress(otherUserId);

        var result = await CreateHandler().Handle(
            new GetAddressByIdQuery { AddressId = otherAddress.Id }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedDto()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var address = SeedAddress(userId);

        var result = await CreateHandler().Handle(
            new GetAddressByIdQuery { AddressId = address.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(address.Id);
        result.FullName.Should().Be("Person");
        result.AddressLine1.Should().Be("1 St");
        result.AddressLine2.Should().Be("Floor 2");
        result.State.Should().Be("Region");
        result.City.Should().Be("City");
        result.PostalCode.Should().Be("1000");
        result.Country.Should().Be("Bulgaria");
        result.CountryCode.Should().Be("BG");
        result.Phone.Should().Be("+359700000000");
        result.Type.Should().Be(nameof(AddressType.Billing));
    }
}
