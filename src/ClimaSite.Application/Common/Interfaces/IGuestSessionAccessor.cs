namespace ClimaSite.Application.Common.Interfaces;

/// <summary>
/// Per-request access to the trusted guest-session id (INV-01 Wave A0). Populated by the guest-session
/// middleware from the verified <c>cs_guest</c> cookie. In A0 this is PUBLISHED for Wave A to consume but is
/// NOT yet read for cart-keying (the cookie ships dark; the flip to keying guest carts on this server-trusted
/// id, with legacy-cart migration, lands in Wave A). Read-only to consumers — only the middleware (which
/// depends on the concrete implementation) sets it. Registered scoped.
/// </summary>
public interface IGuestSessionAccessor
{
    /// <summary>The verified guest-session id for this request, or <see langword="null"/> when no valid
    /// signed guest cookie was presented (and none could be minted).</summary>
    string? GuestSessionId { get; }
}
