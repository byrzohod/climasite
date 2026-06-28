namespace ClimaSite.Application.Common.Interfaces;

/// <summary>
/// Verifies a Google Identity Services ID token (the OIDC ID-token flow). The interface is the seam
/// that keeps the Google sign-in handler unit-testable: production uses the real Google JWKS-backed
/// validator, tests substitute a fake.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the supplied Google ID token. Returns the verified user info on success, or
    /// <c>null</c> for any invalid / expired / wrong-audience / malformed token. Never throws.
    /// </summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

/// <summary>
/// The verified subset of a Google ID token's claims we rely on for sign-in.
/// </summary>
public record GoogleUserInfo(
    string Subject,
    string Email,
    bool EmailVerified,
    string? GivenName,
    string? FamilyName,
    string? Picture);
