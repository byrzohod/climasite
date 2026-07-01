namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Options for the server-minted guest-session cookie (INV-01 Wave A0), bound from the "GuestSession"
/// configuration section. In A0 the cookie ships DARK — it is minted, validated and published on
/// <c>IGuestSessionAccessor</c>, but is NOT yet authoritative for cart-keying (that flip, with legacy-cart
/// migration + legacy-reject-for-reservations, lands in Wave A alongside stock reservations).
/// </summary>
public class GuestSessionOptions
{
    public const string SectionName = "GuestSession";

    /// <summary>
    /// Anti-grief cap on how many NEW guest cookies may be minted per client IP per minute. A request over
    /// the cap proceeds without a cookie rather than letting one IP farm unlimited guest identities. Values
    /// &lt;= 0 fall back to a safe built-in default so a misconfiguration can never disable the protection.
    /// </summary>
    public int MintRateLimitPerMinutePerIp { get; set; } = 20;

    /// <summary>
    /// Request path prefixes on which a fresh cookie may be MINTED. A valid cookie is still validated and
    /// published on EVERY path; only issuance is scoped — so infrastructure/crawler paths (<c>/health</c>,
    /// <c>/robots.txt</c>, <c>/sitemap.xml</c>, static) and cacheable GETs (<c>/api/products</c>) neither burn
    /// the per-IP mint budget nor risk an output-cache Set-Cookie interaction. Matched by path segments
    /// (case-insensitive). In A0 this is <c>/api/cart</c> only: the cookie is established during cart building,
    /// so it already exists by the time checkout runs. Wave A adds <c>/api/payments</c>/<c>/api/orders</c> when
    /// it wires those flows (and their frontend calls) to the cookie.
    /// </summary>
    public string[] MintPathPrefixes { get; set; } = ["/api/cart"];
}
