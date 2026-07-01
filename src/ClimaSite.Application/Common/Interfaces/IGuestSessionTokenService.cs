namespace ClimaSite.Application.Common.Interfaces;

/// <summary>
/// Mints and verifies the signed guest-session token that backs the <c>cs_guest</c> httpOnly cookie
/// (INV-01 Wave A0). The token replaces the previously spoofable client-supplied guest id: it carries a
/// high-entropy server-generated id plus an HMAC over that id, so a client can neither forge a new id nor
/// tamper with an existing one. Pure/stateless — no HTTP dependency — so it is trivially unit-testable and
/// registered as a singleton.
/// </summary>
public interface IGuestSessionTokenService
{
    /// <summary>
    /// Mint a fresh token for a new guest. The value is <c>"{id}.{signature}"</c> where <c>id</c> is a
    /// 128-bit CSPRNG value (base64url) and <c>signature</c> is the base64url HMAC-SHA256 of the id under
    /// the server key.
    /// </summary>
    string Issue();

    /// <summary>
    /// Validate a token (typically the raw <c>cs_guest</c> cookie value). Returns <see langword="true"/> and
    /// sets <paramref name="id"/> to the embedded id only when the token is well-formed AND its signature
    /// verifies in constant time; returns <see langword="false"/> (with <paramref name="id"/> empty) for a
    /// null/empty/malformed/tampered token.
    /// </summary>
    bool TryValidate(string? token, out string id);
}
