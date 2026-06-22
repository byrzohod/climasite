using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Commands;

public class UpdateAddressCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private UpdateAddressCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private Address SeedAddress(Guid userId, bool isDefault = false)
    {
        var address = new Address(userId, "Existing Person", "1 Old St", "Plovdiv", "4000", "Bulgaria", "BG");
        address.SetDefault(isDefault);
        _context.Addresses.Add(address);
        return address;
    }

    private static UpdateAddressCommand UpdateOf(Guid addressId, bool isDefault = false) => new()
    {
        AddressId = addressId,
        FullName = "Updated Name",
        AddressLine1 = "99 New Road",
        AddressLine2 = null,
        City = "Varna",
        State = null,
        PostalCode = "9000",
        Country = "Bulgaria",
        CountryCode = "bg",
        Phone = "+359000000000",
        IsDefault = isDefault,
        Type = "Both"
    };

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(UpdateOf(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task Handle_WhenAddressNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(UpdateOf(Guid.NewGuid()), CancellationToken.None);

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

        var result = await CreateHandler().Handle(UpdateOf(otherAddress.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Address not found");
    }

    [Fact]
    public async Task Handle_UpdatesFieldsAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var address = SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(UpdateOf(address.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Updated Name");
        result.Value.AddressLine1.Should().Be("99 New Road");
        result.Value.City.Should().Be("Varna");
        result.Value.PostalCode.Should().Be("9000");
        result.Value.CountryCode.Should().Be("BG");
        result.Value.Type.Should().Be(nameof(AddressType.Both));
    }

    [Fact]
    public async Task Handle_PromotingToDefault_ClearsOtherDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var existingDefault = SeedAddress(userId, isDefault: true);
        var target = SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(UpdateOf(target.Id, isDefault: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();

        var stored = await _context.Addresses.ToListAsync();
        stored.Single(a => a.Id == existingDefault.Id).IsDefault.Should().BeFalse();
        stored.Single(a => a.Id == target.Id).IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RemovingDefault_WhenOnlyAddress_KeepsItDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var soleAddress = SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(UpdateOf(soleAddress.Id, isDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue("the only address must stay the default");
    }

    [Fact]
    public async Task Handle_RemovingDefault_WhenOtherAddressesExist_ClearsDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var defaultAddress = SeedAddress(userId, isDefault: true);
        SeedAddress(userId, isDefault: false);

        var result = await CreateHandler().Handle(UpdateOf(defaultAddress.Id, isDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidEntityInput_ReturnsFailureFromArgumentException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var address = SeedAddress(userId, isDefault: true);
        var command = UpdateOf(address.Id) with { City = "" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("City");
    }
}
