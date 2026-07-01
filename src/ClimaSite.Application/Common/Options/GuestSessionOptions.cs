namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Options for the server-minted guest-session cookie (INV-01), bound from the "GuestSession" configuration
/// section. As of Wave A1 the cookie is AUTHORITATIVE for cart + checkout keying: the verified cookie id wins
/// over any client-supplied guest id, and a returning guest's legacy cart is migrated onto the cookie id.
/// </summary>
public class GuestSessionOptions
{
    public const string SectionName = "GuestSession";

    /// <summary>
    /// Transition flag: when <see langword="true"/> (default), a request WITHOUT a trusted cookie id may still
    /// fall back to the client-supplied guest id for cart resolution, so guests keep working while the cookie
    /// rolls out (and when minting is over the per-IP cap). When <see langword="false"/>, only the signed
    /// cookie id is trusted and a client-supplied id resolves to nothing. Wave A2 additionally rejects legacy
    /// ids for reservation-bearing flows regardless of this flag.
    /// </summary>
    public bool AllowLegacyId { get; set; } = true;

    /// <summary>
    /// Anti-grief cap on how many NEW guest cookies may be minted per client IP per minute. A request over
    /// the cap proceeds without a cookie rather than letting one IP farm unlimited guest identities. Values
    /// &lt;= 0 fall back to a safe built-in default so a misconfiguration can never disable the protection.
    /// </summary>
    public int MintRateLimitPerMinutePerIp { get; set; } = 20;

    /// <summary>
    /// Request path prefixes on which a fresh cookie may be MINTED. A valid cookie is still validated and
    /// published on EVERY path; only issuance is scoped — so infrastructure/crawler paths (<c>/health</c>,
    /// <c>/robots.txt</c>, <c>/sitemap.xml</c>, static) and public checkout GETs (<c>/api/payments/config</c>,
    /// <c>/api/orders/statuses</c>) neither burn the per-IP mint budget nor risk an output-cache Set-Cookie
    /// interaction. Matched by path segments (case-insensitive). Minting is scoped to <c>/api/cart</c> alone:
    /// the cookie is always established while the guest builds their cart, so checkout only ever VALIDATES it
    /// (an empty cart can't be checked out) and never needs to mint.
    /// </summary>
    public string[] MintPathPrefixes { get; set; } = ["/api/cart"];
}
