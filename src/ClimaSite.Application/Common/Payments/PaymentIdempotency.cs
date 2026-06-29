using System.Security.Cryptography;
using System.Text;

namespace ClimaSite.Application.Common.Payments;

/// <summary>
/// Builds and validates the idempotency keys forwarded to Stripe's two CREATE calls
/// (create-intent and refund). Keys are namespaced by a short, greppable prefix so a
/// client-supplied create key (<c>ci_</c>) can never collide with a server-derived
/// refund key (<c>re_v1_</c>), and so both are easy to find in the Stripe dashboard.
/// </summary>
public static class PaymentIdempotency
{
    /// <summary>
    /// Deterministic, server-derived idempotency key for the full-charge refund of a given
    /// PaymentIntent. We hash the intent id with SHA-256 rather than using it raw so that
    /// <b>no Stripe object identifier (or any caller data) is echoed back into the key</b> —
    /// the key carries no PII and is purely a stable, opaque token. Keying on the intent id
    /// alone is correct because today's refunds are full-charge compensation, so two refund
    /// attempts for the same intent must dedupe to a single Stripe refund.
    /// </summary>
    public static string ForRefund(string paymentIntentId)
        => "re_v1_" + Sha256Hex(paymentIntentId);

    /// <summary>
    /// Defensive bound on the client-controlled per-attempt key before it is forwarded to
    /// Stripe: 8..200 characters drawn only from <c>[A-Za-z0-9_-]</c> (a superset that comfortably
    /// fits a UUID). Null/empty and anything else returns false, keeping an untrusted value from
    /// reaching the Stripe API.
    /// </summary>
    public static bool IsValidClientKey(string? key)
        => !string.IsNullOrEmpty(key)
           && key.Length is >= 8 and <= 200
           && key.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');

    /// <summary>
    /// Namespaces a validated client key for the create-intent call: <c>null</c>/empty stays
    /// <c>null</c> (degrades to today's no-dedup behaviour), otherwise it is prefixed with
    /// <c>ci_</c> so it can never collide with a <c>re_v1_</c> refund key and is greppable.
    /// </summary>
    public static string? NormalizeClientKey(string? raw)
        => string.IsNullOrEmpty(raw) ? null : "ci_" + raw;

    /// <summary>Lowercase hex SHA-256 (64 chars) of the UTF-8 bytes of <paramref name="value"/>.</summary>
    private static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }
}
