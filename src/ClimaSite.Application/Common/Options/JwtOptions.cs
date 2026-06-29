namespace ClimaSite.Application.Common.Options;

/// <summary>
/// The single resolved + validated JWT configuration, bound once at startup, used by BOTH the
/// bearer-token validation (<c>TokenValidationParameters</c>) and <c>TokenService</c> issuance so
/// the two provably share one secret/issuer/audience.
/// </summary>
public class JwtOptions
{
    /// <summary>Signing secret (HMAC-SHA256). Resolved + validated by <c>JwtConfiguration.ResolveSecret</c>.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Token issuer (<c>iss</c>).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token audience (<c>aud</c>).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;
}
