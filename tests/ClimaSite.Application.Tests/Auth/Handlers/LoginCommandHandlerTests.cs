using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class LoginCommandHandlerTests
{
    // The handler no longer mints the JWT itself — it delegates to ITokenService (SEC-05/B-011).
    // Token content (claims/issuer/expiry) is asserted in TokenServiceTests; here we assert the
    // handler invokes the service with (user, roles) and surfaces its token.
    private const string KnownAccessToken = "login.access.token";

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var logger = new Mock<ILogger<LoginCommandHandler>>();

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns(KnownAccessToken);

        _handler = new LoginCommandHandler(_userManagerMock.Object, _tokenServiceMock.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsServiceTokenAndUser()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(user.Email!, "Password123!");
        SetupSuccessfulLogin(user, command.Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be(KnownAccessToken);
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.User.Email.Should().Be(user.Email);
        result.Value.User.FirstName.Should().Be(user.FirstName);
        result.Value.User.Role.Should().Be("Customer");

        // The access token comes from ITokenService, called with the authenticated user + its roles.
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user, It.Is<IList<string>>(r => r.Contains("Customer"))),
            Times.Once);
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_PersistsRotatedRefreshTokenOnUser()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(user.Email!, "Password123!");
        SetupSuccessfulLogin(user, command.Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.RefreshToken.Should().Be(result.Value!.RefreshToken);
        user.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsFailureAndIncrementsLockoutCounter()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(user.Email!, "WrongPassword!");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
        // Lockout path: failed attempt is recorded, no token is issued.
        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithLockedOutUser_ReturnsFailureWithoutCheckingPassword()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(user.Email!, "Password123!");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("nobody@example.com", "Password123!");
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);

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
        var command = new LoginCommand(user.Email!, "Password123!");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    private void SetupSuccessfulLogin(ApplicationUser user, string password)
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
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
