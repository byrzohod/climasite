using ClimaSite.Application.Common.Interfaces;

namespace ClimaSite.Api.Tests.Infrastructure;

/// <summary>
/// Deterministic in-memory <see cref="IGoogleTokenValidator"/> so integration tests exercise the
/// Google sign-in flow without contacting Google. Registered as a singleton in
/// <see cref="TestWebApplicationFactory"/>.
///
/// The fake is self-describing via the token string so tests need no shared mutable state:
/// <list type="bullet">
///   <item><description><see cref="InvalidToken"/> (or empty) → <c>null</c> (rejected).</description></item>
///   <item><description><c>"valid|{sub}|{email}|{verified}|{given}|{family}"</c> → the encoded identity.</description></item>
///   <item><description>any other non-sentinel token → a default verified identity.</description></item>
/// </list>
/// </summary>
public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    /// <summary>Sentinel token the fake always rejects (maps to <c>null</c>).</summary>
    public const string InvalidToken = "INVALID";

    /// <summary>
    /// Builds a self-describing valid token: <c>valid|sub|email|verified|given|family</c>.
    /// </summary>
    public static string ValidToken(
        string subject,
        string email,
        bool emailVerified = true,
        string givenName = "Test",
        string familyName = "User")
        => $"valid|{subject}|{email}|{emailVerified.ToString().ToLowerInvariant()}|{givenName}|{familyName}";

    public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(idToken) || idToken == InvalidToken)
        {
            return Task.FromResult<GoogleUserInfo?>(null);
        }

        if (idToken.StartsWith("valid|", StringComparison.Ordinal))
        {
            var parts = idToken.Split('|');
            var info = new GoogleUserInfo(
                Subject: parts.ElementAtOrDefault(1) ?? "google-sub",
                Email: parts.ElementAtOrDefault(2) ?? "user@example.com",
                EmailVerified: bool.Parse(parts.ElementAtOrDefault(3) ?? "true"),
                GivenName: parts.ElementAtOrDefault(4) ?? "Test",
                FamilyName: parts.ElementAtOrDefault(5) ?? "User",
                Picture: null);
            return Task.FromResult<GoogleUserInfo?>(info);
        }

        // Fallback canned identity for any other non-sentinel token.
        return Task.FromResult<GoogleUserInfo?>(new GoogleUserInfo(
            Subject: "google-sub-default",
            Email: "google.default@example.com",
            EmailVerified: true,
            GivenName: "Goog",
            FamilyName: "Default",
            Picture: null));
    }
}
