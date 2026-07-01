using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClimaSite.Application.Common.Interfaces;

namespace ClimaSite.Infrastructure.Services;

/// <summary>
/// Signs and verifies guest-session tokens for the guest cookie (INV-01 Wave A0). The token is
/// <c>"{id}.{expUnixSeconds}.{signature}"</c> where <c>signature = base64url(HMAC-SHA256(key, "{id}.{exp}"))</c>,
/// so the expiry is cryptographically bound and cannot be extended by a client. The signing key is DERIVED from
/// the resolved JWT signing secret (<c>HMAC-SHA256(jwtSecret, "climasite-guest-session-v1")</c>) so it
/// introduces no new required secret and inherits the JWT secret's production fail-fast guarantee — the key is
/// computed once at construction and never stored or committed. The service is stateless and thread-safe
/// (immutable key + the allocation-free static <see cref="HMACSHA256.HashData(byte[], byte[])"/>), so it is
/// registered as a singleton.
/// </summary>
public sealed class GuestSessionTokenService : IGuestSessionTokenService
{
    /// <summary>Domain-separation label so the derived key can never collide with a raw JWT-HMAC use.</summary>
    private const string KeyDerivationLabel = "climasite-guest-session-v1";

    /// <summary>Guest-id entropy in bytes (128-bit).</summary>
    private const int IdByteLength = 16;

    /// <summary>Token/cookie lifetime — mirrored by the middleware's cookie <c>Expires</c>.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

    private readonly byte[] _key;

    public GuestSessionTokenService(string jwtSecret)
    {
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new ArgumentException("A signing secret is required to derive the guest-session key.", nameof(jwtSecret));
        }

        // Derive a dedicated key rather than HMAC'ing directly with the JWT secret, keeping the guest-token
        // key space disjoint from token issuance. Computed once; the raw secret is not retained.
        _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwtSecret), Encoding.UTF8.GetBytes(KeyDerivationLabel));
    }

    public string Issue() => Issue(DateTimeOffset.UtcNow.Add(TokenLifetime));

    /// <summary>
    /// Mint a token that expires at <paramref name="expiresAt"/>. The parameterless <see cref="Issue()"/> uses
    /// <see cref="TokenLifetime"/>; this overload exists so tests can mint already-expired (or near-expiry)
    /// tokens deterministically without a clock seam.
    /// </summary>
    public string Issue(DateTimeOffset expiresAt)
    {
        Span<byte> idBytes = stackalloc byte[IdByteLength];
        RandomNumberGenerator.Fill(idBytes);
        var id = Base64Url.EncodeToString(idBytes);
        var exp = expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var payload = $"{id}.{exp}";
        return $"{payload}.{Sign(payload)}";
    }

    public bool TryValidate(string? token, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // Exactly three non-empty parts: id . expUnixSeconds . signature.
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var idPart = parts[0];
        var expPart = parts[1];
        var signaturePart = parts[2];
        if (idPart.Length == 0 || expPart.Length == 0 || signaturePart.Length == 0)
        {
            return false;
        }

        if (!long.TryParse(expPart, NumberStyles.None, CultureInfo.InvariantCulture, out var expUnixSeconds))
        {
            return false;
        }

        byte[] providedSignature;
        try
        {
            providedSignature = Base64Url.DecodeFromChars(signaturePart);
        }
        catch (FormatException)
        {
            return false;
        }

        var expectedSignature = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{idPart}.{expPart}"));

        // Length check first (cheap, non-secret), then a constant-time compare so a valid-but-wrong signature
        // reveals no timing information about how many bytes matched.
        if (providedSignature.Length != expectedSignature.Length)
        {
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
        {
            return false;
        }

        // Signature authentic → enforce the (now trusted) expiry. Rejecting after the HMAC check means a
        // tampered exp fails on the signature, and only a genuinely expired token reaches this branch.
        if (DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds) <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        id = idPart;
        return true;
    }

    private string Sign(string payload) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
}
