using ClimaSite.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Infrastructure.Services;

/// <summary>
/// Production <see cref="IGoogleTokenValidator"/> backed by <c>Google.Apis.Auth</c>. It verifies the
/// ID token's signature (against Google's JWKS), expiry and that the audience matches our configured
/// OAuth client id (<c>Authentication:Google:ClientId</c>). Any failure — including an unconfigured
/// client id — yields <c>null</c> so the sign-in flow degrades cleanly to "not authenticated".
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];

        // Feature is "dark" until the owner supplies a client id; a missing one means no audience to
        // validate against, so we reject rather than accept an unaudienced token.
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null)
            {
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.GivenName,
                payload.FamilyName,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            // Expected for tampered / expired / wrong-audience tokens. Never log the token itself.
            _logger.LogWarning("Rejected an invalid Google ID token: {Reason}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            // Network/JWKS hiccups etc. — fail closed; the caller treats null as "not authenticated".
            _logger.LogWarning(ex, "Google ID token validation failed unexpectedly.");
            return null;
        }
    }
}
