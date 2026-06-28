using System.IdentityModel.Tokens.Jwt;
using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class GoogleSignInCommandHandlerTests
{
    // Secret must be >= 32 chars for HMAC-SHA256 token signing.
    private const string JwtSecret = "test-secret-key-that-is-at-least-32-bytes-long!!";
    private const string JwtIssuer = "https://test-issuer";
    private const string JwtAudience = "https://test-audience";

    private const string GoogleSubject = "google-subject-123";
    private const string GoogleEmail = "google.user@example.com";

    private readonly Mock<IGoogleTokenValidator> _validatorMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GoogleSignInCommandHandler _handler;

    public GoogleSignInCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var logger = new Mock<ILogger<GoogleSignInCommandHandler>>();

        _handler = new GoogleSignInCommandHandler(
            _validatorMock.Object, _userManagerMock.Object, BuildConfiguration(), logger.Object);
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_CreatesUserAndReturnsTokens()
    {
        // Arrange
        ValidatorReturns(VerifiedGoogleUser());

        _userManagerMock.Setup(x => x.FindByLoginAsync("Google", GoogleSubject)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync(GoogleEmail)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "Customer" });
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(new GoogleSignInCommand("id-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.User.Email.Should().Be(GoogleEmail);
        result.Value.User.FirstName.Should().Be("Goog");
        result.Value.User.LastName.Should().Be("User");
        result.Value.User.Role.Should().Be("Customer");

        // The access token is a real JWT signed with the configured issuer/audience.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        jwt.Issuer.Should().Be(JwtIssuer);
        jwt.Audiences.Should().Contain(JwtAudience);

        // The new account is created exactly once and the Google login is attached to it.
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u =>
            u.Email == GoogleEmail && u.EmailConfirmed && u.IsActive)), Times.Once);
        _userManagerMock.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(),
            It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == GoogleSubject)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyLinkedByGoogleSubject_FindsUserWithoutCreatingOrLinking()
    {
        // Arrange
        ValidatorReturns(VerifiedGoogleUser());
        var existing = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByLoginAsync("Google", GoogleSubject)).ReturnsAsync(existing);
        _userManagerMock.Setup(x => x.GetRolesAsync(existing)).ReturnsAsync(new List<string> { "Customer" });
        _userManagerMock.Setup(x => x.UpdateAsync(existing)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(new GoogleSignInCommand("id-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Email.Should().Be(existing.Email);
        existing.LastLoginAt.Should().NotBeNull();

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailMatchesExistingAccount_LinksGoogleLoginWithoutDuplicating()
    {
        // Arrange — the existing account has ALREADY confirmed this mailbox, so linking is safe.
        ValidatorReturns(VerifiedGoogleUser());
        var existing = CreateTestUser();
        existing.EmailConfirmed = true;
        _userManagerMock.Setup(x => x.FindByLoginAsync("Google", GoogleSubject)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync(GoogleEmail)).ReturnsAsync(existing);
        _userManagerMock.Setup(x => x.AddLoginAsync(existing, It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(existing)).ReturnsAsync(new List<string> { "Customer" });
        _userManagerMock.Setup(x => x.UpdateAsync(existing)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(new GoogleSignInCommand("id-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Email.Should().Be(existing.Email);

        // The existing account is reused (no new user) and the Google login is linked to it once.
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddLoginAsync(existing,
            It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == GoogleSubject)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailMatchesUNVERIFIEDAccount_RefusesToLink()
    {
        // SECURITY: an attacker could pre-register the victim's email as an UNCONFIRMED password account.
        // Google sign-in must NOT silently link onto it (federated pre-hijack) — it must reject.
        ValidatorReturns(VerifiedGoogleUser());
        var existingUnverified = CreateTestUser();
        existingUnverified.EmailConfirmed = false;
        _userManagerMock.Setup(x => x.FindByLoginAsync("Google", GoogleSubject)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync(GoogleEmail)).ReturnsAsync(existingUnverified);

        var result = await _handler.Handle(new GoogleSignInCommand("id-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        // No link, no duplicate account — the attacker's account is left untouched and no session is issued.
        _userManagerMock.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsFailureAndTouchesNoUsers()
    {
        // Arrange - the validator rejects the token (returns null).
        ValidatorReturns(null);

        // Act
        var result = await _handler.Handle(new GoogleSignInCommand("bad-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid Google credentials");
        _userManagerMock.Verify(x => x.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnverifiedGoogleEmail_ReturnsFailure()
    {
        // Arrange - a structurally valid token whose email Google has not verified.
        ValidatorReturns(new GoogleUserInfo(GoogleSubject, GoogleEmail, EmailVerified: false, "Goog", "User", null));

        // Act
        var result = await _handler.Handle(new GoogleSignInCommand("unverified-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid Google credentials");
        _userManagerMock.Verify(x => x.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private void ValidatorReturns(GoogleUserInfo? info) =>
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);

    private static GoogleUserInfo VerifiedGoogleUser() =>
        new(GoogleSubject, GoogleEmail, EmailVerified: true, "Goog", "User", "https://example.com/p.png");

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = JwtSecret,
                ["JwtSettings:Issuer"] = JwtIssuer,
                ["JwtSettings:Audience"] = JwtAudience,
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();

    private static ApplicationUser CreateTestUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = GoogleEmail,
            UserName = GoogleEmail,
            FirstName = "Existing",
            LastName = "Account",
            IsActive = true,
            PreferredLanguage = "en",
            PreferredCurrency = "USD",
            CreatedAt = DateTime.UtcNow
        };
}
