using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClimaSite.Application.Common.Options;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClimaSite.Api.Tests.Services;

/// <summary>
/// SEC-05/B-011: token issuance is centralized in <see cref="TokenService"/>. These tests own the
/// end-to-end token-shape assertions (claims/issuer/audience/expiry) that previously lived in the
/// handler tests, and prove an issued token validates against the SAME secret/issuer/audience.
/// </summary>
public class TokenServiceTests
{
    private const string Secret = "token-service-tests-signing-secret-at-least-32-chars";
    private const string Issuer = "https://issuer.test";
    private const string Audience = "https://audience.test";
    private const int ExpiryMinutes = 30;

    private readonly TokenService _sut = new(Options.Create(new JwtOptions
    {
        Secret = Secret,
        Issuer = Issuer,
        Audience = Audience,
        AccessTokenExpirationMinutes = ExpiryMinutes
    }));

    [Fact]
    public void GenerateAccessToken_CarriesLiveClaimSet()
    {
        var user = CreateUser();

        var token = _sut.GenerateAccessToken(user, new List<string> { "Admin", "Customer" });
        var principal = ValidateAndRead(token);

        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(user.Id.ToString());
        principal.FindFirstValue(ClaimTypes.Email).Should().Be(user.Email);
        principal.FindFirstValue("firstName").Should().Be(user.FirstName);
        principal.FindFirstValue("lastName").Should().Be(user.LastName);
        principal.FindFirstValue(JwtRegisteredClaimNames.Jti).Should().NotBeNullOrWhiteSpace();
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo("Admin", "Customer");

        // The dead GivenName/Surname/preferred_* claims must NOT be emitted any more.
        principal.FindFirst(ClaimTypes.GivenName).Should().BeNull();
        principal.FindFirst(ClaimTypes.Surname).Should().BeNull();
        principal.FindFirst("preferred_language").Should().BeNull();
        principal.FindFirst("preferred_currency").Should().BeNull();
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredIssuerAudienceAndExpiry()
    {
        var token = _sut.GenerateAccessToken(CreateUser(), new List<string>());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Contain(Audience);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(ExpiryMinutes), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateAccessToken_HandlesUserWithNoRoles()
    {
        var token = _sut.GenerateAccessToken(CreateUser(), new List<string>());

        var principal = ValidateAndRead(token);
        principal.FindAll(ClaimTypes.Role).Should().BeEmpty();
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAccessToken_IsSignedWithTheConfiguredSecret_AndRejectsAnother()
    {
        var token = _sut.GenerateAccessToken(CreateUser(), new List<string> { "Customer" });

        // Validating with a different secret must fail — proving the signature is bound to ours.
        var validateWithWrongKey = () => new JwtSecurityTokenHandler().ValidateToken(
            token,
            ValidationParameters("a-completely-different-secret-key-32-characters!!"),
            out _);

        validateWithWrongKey.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    private static ClaimsPrincipal ValidateAndRead(string token) =>
        new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(Secret), out _);

    private static TokenValidationParameters ValidationParameters(string secret) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };

    private static ApplicationUser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "token.user@example.com",
        UserName = "token.user@example.com",
        FirstName = "Token",
        LastName = "User",
        IsActive = true,
        PreferredLanguage = "en",
        PreferredCurrency = "EUR",
        CreatedAt = DateTime.UtcNow
    };
}
