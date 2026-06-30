using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ClimaSite.Application.Features.Seo;

/// <summary>
/// Pure renderers for the crawler documents (B-044 Wave B). Kept side-effect-free (no DB, no cache, no host
/// state) so the host-independent enumeration can be cached and the absolute documents rendered per-request
/// from a freshly-resolved base URL (council #3 — no cache poisoning).
/// </summary>
public static class SeoDocumentBuilder
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>
    /// Renders a sitemap <c>&lt;urlset&gt;</c>. Each entry's absolute <c>&lt;loc&gt;</c> is
    /// <paramref name="baseUrl"/> + the entry's site-relative path; <c>&lt;lastmod&gt;</c> (W3C
    /// <c>yyyy-MM-dd</c>) is emitted only when the entry carries a timestamp.
    /// </summary>
    public static string BuildSitemapXml(string baseUrl, IReadOnlyList<SitemapEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var normalizedBase = baseUrl.TrimEnd('/');
        var urlset = new XElement(SitemapNs + "urlset");

        foreach (var entry in entries)
        {
            var url = new XElement(SitemapNs + "url",
                new XElement(SitemapNs + "loc", normalizedBase + entry.Path));

            if (entry.LastModified is { } lastModified)
            {
                url.Add(new XElement(SitemapNs + "lastmod",
                    lastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }

            urlset.Add(url);
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);

        using var writer = new Utf8StringWriter();
        document.Save(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Renders <c>robots.txt</c>. Allows everything, disallows the private SPA prefixes (never <c>/api</c>),
    /// and emits an absolute <c>Sitemap:</c> line when <paramref name="baseUrl"/> is non-null. When the base
    /// could not be resolved (Staging/Production fail-closed) the <c>Sitemap:</c> line is omitted.
    /// </summary>
    public static string BuildRobotsTxt(string? baseUrl)
    {
        var builder = new StringBuilder();
        builder.Append("User-agent: *\n");
        builder.Append("Allow: /\n");

        foreach (var prefix in SeoPaths.DisallowedPrefixes)
        {
            builder.Append("Disallow: ").Append(prefix).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            builder.Append('\n');
            builder.Append("Sitemap: ").Append(baseUrl.TrimEnd('/')).Append("/sitemap.xml\n");
        }

        return builder.ToString();
    }

    /// <summary>StringWriter that reports UTF-8 so the XML declaration reads <c>encoding="utf-8"</c>.</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
