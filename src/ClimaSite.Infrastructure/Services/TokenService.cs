using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClimaSite.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Prefer environment variables (Railway), fallback to config
        _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET or JwtSettings:Secret.");
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? _configuration["JwtSettings:Issuer"]
            ?? "https://climasite.local";
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? _configuration["JwtSettings:Audience"]
            ?? "https://climasite.local";
        _accessTokenExpirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15");
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("preferred_language", user.PreferredLanguage),
            new("preferred_currency", user.PreferredCurrency),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
