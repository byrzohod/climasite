using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Addresses.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Addresses.Commands;

public class CreateAddressCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private CreateAddressCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private static CreateAddressCommand ValidCommand(bool isDefault = false) => new()
    {
        FullName = "Jane Buyer",
        AddressLine1 = "12 Heating Way",
        AddressLine2 = "Apt 4",
        City = "Sofia",
        State = "Sofia City",
        PostalCode = "1000",
        Country = "Bulgaria",
        CountryCode = "bg",
        Phone = "+359888123456",
        IsDefault = isDefault,
        Type = "Billing"
    };

    private void SeedAddress(Guid userId, bool isDefault)
    {
        var address = new Address(userId, "Existing Person", "1 Old St", "Plovdiv", "4000", "Bulgaria", "BG");
        address.SetDefault(isDefault);
        _context.Addresses.Add(address);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task Handle_FirstAddress_IsForcedDefault_AndMappedToDto()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(ValidCommand(isDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsDefault.Should().BeTrue("the very first address must become the default");
        result.Value.FullName.Should().Be("Jane Buyer");
        result.Value.AddressLine2.Should().Be("Apt 4");
        result.Value.State.Should().Be("Sofia City");
        result.Value.Phone.Should().Be("+359888123456");
        result.Value.CountryCode.Should().Be("BG", "the entity upper-cases the country code");
        result.Value.Type.Should().Be(nameof(AddressType.Billing));

        var stored = await _context.Addresses.ToListAsync();
        stored.Should().ContainSingle(a => a.UserId == userId && a.IsDefault);
    }

    [Fact]
    public async Task Handle_SecondAddressNotDefault_DoesNotBecomeDefault()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(ValidCommand(isDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeFalse("an existing default already exists");
    }

    [Fact]
    public async Task Handle_MarkedDefault_ClearsDefaultFromExistingAddresses()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        SeedAddress(userId, isDefault: true);

        var result = await CreateHandler().Handle(ValidCommand(isDefault: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();

        var defaults = (await _context.Addresses.ToListAsync()).Where(a => a.IsDefault).ToList();
        defaults.Should().HaveCount(1, "only the newly created address should remain default");
        defaults[0].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task Handle_OnlyScopesDefaultClearingToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        SeedAddress(otherUserId, isDefault: true);

        var result = await CreateHandler().Handle(ValidCommand(isDefault: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var otherUserAddress = (await _context.Addresses.ToListAsync()).Single(a => a.UserId == otherUserId);
        otherUserAddress.IsDefault.Should().BeTrue("another user's default must be untouched");
    }

    [Fact]
    public async Task Handle_InvalidType_FallsBackToDefaultShippingType()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var command = ValidCommand() with { Type = "NotARealType" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(nameof(AddressType.Shipping));
    }

    [Fact]
    public async Task Handle_InvalidEntityInput_ReturnsFailureFromArgumentException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var command = ValidCommand() with { FullName = "   " };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Full name");
    }
}
