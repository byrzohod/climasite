using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClimaSite.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        // SEC-05 / B-011: secret/issuer/audience are resolved + validated ONCE at startup and bound into
        // JwtOptions, so issuance here uses the SAME values as bearer validation — no direct config/env
        // reads, no divergent fallback defaults.
        var options = jwtOptions.Value;
        _secretKey = options.Secret;
        _issuer = options.Issuer;
        _audience = options.Audience;
        _accessTokenExpirationMinutes = options.AccessTokenExpirationMinutes;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        // Live claim shape (matches the former handler-local generators): the backend reads
        // NameIdentifier + Role, the SPA uses the response body. firstName/lastName are the custom
        // claims the handlers emitted; the old GivenName/Surname/preferred_* claims were dead.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public (bool IsValid, Guid UserId) ValidateRefreshToken(string refreshToken)
    {
        // Basic validation - refresh tokens are validated against stored tokens in the database
        // This method is used for additional validation logic if needed
        if (string.IsNullOrEmpty(refreshToken))
        {
            return (false, Guid.Empty);
        }

        // The actual validation happens in the handler by comparing with stored token
        // Here we just do basic format validation
        try
        {
            var bytes = Convert.FromBase64String(refreshToken);
            return (bytes.Length == 64, Guid.Empty);
        }
        catch
        {
            return (false, Guid.Empty);
        }
    }
}
