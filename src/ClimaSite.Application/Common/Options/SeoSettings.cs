namespace ClimaSite.Application.Common.Options;

/// <summary>
/// SEO settings bound from the "Seo" configuration section (B-044 Wave B).
/// </summary>
public class SeoSettings
{
    public const string SectionName = "Seo";

    /// <summary>
    /// The single canonical public origin (e.g. <c>https://www.climasite.com</c>) used to build the
    /// absolute <c>&lt;loc&gt;</c> entries in <c>sitemap.xml</c> and the <c>Sitemap:</c> line in
    /// <c>robots.txt</c>. Required (acceptance-blocking) for any non-Development/Testing deploy — when
    /// empty/invalid in Staging/Production the SEO endpoints fail closed rather than emit canonical URLs
    /// from an untrusted request host. Empty by default.
    /// </summary>
    public string SiteBaseUrl { get; set; } = string.Empty;
}
