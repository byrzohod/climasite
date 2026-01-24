using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Auth.Commands;
using ClimaSite.Application.Features.Auth.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "expired-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1); // Expired

        var command = new RefreshTokenCommand
        {
            RefreshToken = "expired-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "different-token"; // Token was revoked/changed
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var command = new RefreshTokenCommand
        {
            RefreshToken = "old-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "invalid-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((false, Guid.Empty));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand
        {
            RefreshToken = "token-for-deleted-user"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.IsActive = false;

        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
    }

    [Fact]
    public async Task Handle_RotatesRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "old-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var command = new RefreshTokenCommand
        {
            RefreshToken = "old-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("rotated-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefreshToken.Should().Be("rotated-refresh-token");
        result.Value.RefreshToken.Should().NotBe(command.RefreshToken);

        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.RefreshToken == "rotated-refresh-token" &&
            u.RefreshTokenExpiryTime > DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.FirstName = "Jane";
        user.LastName = "Smith";
        user.PreferredLanguage = "bg";
        user.PreferredCurrency = "BGN";

        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        var roles = new List<string> { "Customer", "Premium" };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Id.Should().Be(userId);
        result.Value.User.Email.Should().Be(user.Email);
        result.Value.User.FirstName.Should().Be("Jane");
        result.Value.User.LastName.Should().Be("Smith");
        result.Value.User.PreferredLanguage.Should().Be("bg");
        result.Value.User.PreferredCurrency.Should().Be("BGN");
        result.Value.User.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task Handle_SetsNewRefreshTokenExpiry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1); // About to expire

        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.RefreshTokenExpiryTime!.Value >= DateTime.UtcNow.AddDays(6))), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsAccessTokenExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.AccessTokenExpiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Validator_WithValidRefreshToken_PassesValidation()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithEmptyRefreshToken_FailsValidation()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand
        {
            RefreshToken = ""
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }

    [Fact]
    public void Validator_WithNullRefreshToken_FailsValidation()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand
        {
            RefreshToken = null!
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }

    [Fact]
    public async Task Handle_WithTokenMismatch_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.RefreshToken = "stored-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var command = new RefreshTokenCommand
        {
            RefreshToken = "submitted-different-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateRefreshToken(command.RefreshToken))
            .Returns((true, userId));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
    }

    private static ApplicationUser CreateTestUser(Guid? id = null)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            PreferredLanguage = "en",
            PreferredCurrency = "USD",
            CreatedAt = DateTime.UtcNow
        };
    }
}
