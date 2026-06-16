using System.IdentityModel.Tokens.Jwt;
using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class RefreshTokenCommandHandlerTests
{
    // Secret must be >= 32 chars for HMAC-SHA256 token signing.
    private const string JwtSecret = "test-secret-key-that-is-at-least-32-bytes-long!!";
    private const string JwtIssuer = "https://test-issuer";
    private const string JwtAudience = "https://test-audience";

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var logger = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(_userManagerMock.Object, BuildConfiguration(), logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidStoredToken_RotatesAndReturnsNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        user.SetRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(7));
        var originalToken = user.RefreshToken;
        SetupUsers(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        // Refresh token is rotated and persisted.
        result.Value.RefreshToken.Should().NotBe(originalToken);
        user.RefreshToken.Should().Be(result.Value.RefreshToken);
        user.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        jwt.Issuer.Should().Be(JwtIssuer);
        jwt.Audiences.Should().Contain(JwtAudience);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.SetRefreshToken("stored-token", DateTime.UtcNow.AddDays(7));
        SetupUsers(user);

        var command = new RefreshTokenCommand("not-the-stored-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid or expired refresh token");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        // Matching token but expired in the past.
        user.SetRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1));
        SetupUsers(user);

        var command = new RefreshTokenCommand("expired-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid or expired refresh token");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.IsActive = false;
        user.SetRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(7));
        SetupUsers(user);

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private void SetupUsers(params ApplicationUser[] users)
    {
        var queryable = new TestAsyncEnumerable<ApplicationUser>(users.AsQueryable());
        _userManagerMock.Setup(x => x.Users).Returns(queryable);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = JwtSecret,
                ["JwtSettings:Issuer"] = JwtIssuer,
                ["JwtSettings:Audience"] = JwtAudience,
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();
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
