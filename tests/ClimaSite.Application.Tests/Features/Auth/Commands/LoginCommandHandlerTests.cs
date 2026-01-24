using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Auth.Commands;
using ClimaSite.Application.Features.Auth.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.User.Email.Should().Be(user.Email);
        result.Value.User.FirstName.Should().Be(user.FirstName);
        result.Value.User.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "WrongPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.IsActive = false;

        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
    }

    [Fact]
    public async Task Handle_WithLockedOutUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_ResetsAccessFailedCountOnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_IncrementsAccessFailedCountOnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "WrongPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesLastLoginAndRefreshToken()
    {
        // Arrange
        var user = CreateTestUser();
        var beforeLogin = DateTime.UtcNow;

        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.RefreshToken == "new-refresh-token" &&
            u.LastLoginAt >= beforeLogin &&
            u.RefreshTokenExpiryTime > DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectUserRoles()
    {
        // Arrange
        var user = CreateTestUser();
        var expectedRoles = new List<string> { "Customer", "Premium" };

        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(expectedRoles);

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.User.Roles.Should().BeEquivalentTo(expectedRoles);
    }

    [Fact]
    public async Task Handle_ReturnsUserPreferences()
    {
        // Arrange
        var user = CreateTestUser();
        user.PreferredLanguage = "de";
        user.PreferredCurrency = "EUR";

        var command = new LoginCommand
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.User.PreferredLanguage.Should().Be("de");
        result.Value.User.PreferredCurrency.Should().Be("EUR");
    }

    [Fact]
    public void Validator_WithValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validator_WithEmptyPassword_FailsValidation()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validator_WithInvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = "not-an-email",
            Password = "Password123!"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("Invalid email"));
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
