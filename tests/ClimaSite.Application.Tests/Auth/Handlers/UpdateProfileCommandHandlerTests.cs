using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var logger = new Mock<ILogger<UpdateProfileCommandHandler>>();

        _handler = new UpdateProfileCommandHandler(_userManagerMock.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_WithAllFields_UpdatesUserAndReturnsUpdatedDto()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateProfileCommand(
            user.Id,
            FirstName: "Jane",
            LastName: "Smith",
            Phone: "+359888999000",
            PreferredLanguage: "de",
            PreferredCurrency: "EUR");

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        result.Value.Phone.Should().Be("+359888999000");
        result.Value.PreferredLanguage.Should().Be("de");
        result.Value.PreferredCurrency.Should().Be("EUR");
        result.Value.Role.Should().Be("Customer");

        // Entity mutated in place.
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.PhoneNumber.Should().Be("+359888999000");
        user.PreferredLanguage.Should().Be("de");
        user.PreferredCurrency.Should().Be("EUR");
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullFields_LeavesExistingValuesUnchanged()
    {
        // Arrange — only FirstName supplied; the rest are null and must be preserved.
        var user = CreateTestUser();
        user.LastName = "Original";
        user.PhoneNumber = "+359000000000";
        user.PreferredLanguage = "bg";
        user.PreferredCurrency = "BGN";

        var command = new UpdateProfileCommand(
            user.Id,
            FirstName: "OnlyFirst",
            LastName: null,
            Phone: null,
            PreferredLanguage: null,
            PreferredCurrency: null);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("OnlyFirst");
        result.Value.LastName.Should().Be("Original");
        result.Value.Phone.Should().Be("+359000000000");
        result.Value.PreferredLanguage.Should().Be("bg");
        result.Value.PreferredCurrency.Should().Be("BGN");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Jane", null, null, null, null);
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ReturnsFailureWithErrors()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateProfileCommand(user.Id, "Jane", null, null, null, null);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency failure." }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Concurrency failure.");
        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static ApplicationUser CreateTestUser(string? email = null)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email ?? "test@example.com",
            UserName = email ?? "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            PreferredLanguage = "en",
            PreferredCurrency = "USD",
            CreatedAt = DateTime.UtcNow
        };
    }
}
