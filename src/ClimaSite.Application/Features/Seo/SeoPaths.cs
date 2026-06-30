namespace ClimaSite.Application.Features.Seo;

/// <summary>
/// Canonical, host-independent SEO path constants shared by the sitemap and robots builders. Every static
/// path here maps to a real registered frontend route (verified against app.routes.ts / legal.routes.ts);
/// the integration smoke test guards against soft-404 feed entries.
/// </summary>
public static class SeoPaths
{
    /// <summary>
    /// Public, indexable static pages included verbatim in the sitemap. No trailing slash except the
    /// home root ("/"), matching the frontend canonical normalization (Wave A).
    /// </summary>
    public static readonly IReadOnlyList<string> StaticPages = new[]
    {
        "/",
        "/products",
        "/categories",
        "/brands",
        "/promotions",
        "/contact",
        "/about",
        "/resources",
        "/faq",
        "/terms",
        "/privacy",
        "/cookies",
        "/returns",
        "/shipping",
        "/impressum"
    };

    /// <summary>
    /// Private / semi-private SPA prefixes crawlers must not index. Deliberately excludes "/api" — Googlebot
    /// fetches public API XHRs to render the CSR app, so disallowing it would break rendering (council #1).
    /// </summary>
    public static readonly IReadOnlyList<string> DisallowedPrefixes = new[]
    {
        "/admin/",
        "/account/",
        "/checkout",
        "/cart",
        "/login",
        "/register",
        "/forgot-password",
        "/reset-password",
        "/wishlist"
    };
}
