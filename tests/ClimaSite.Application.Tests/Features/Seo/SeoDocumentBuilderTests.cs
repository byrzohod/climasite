using System.Xml.Linq;
using ClimaSite.Application.Features.Seo;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Seo;

public class SeoDocumentBuilderTests
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [Fact]
    public void BuildSitemapXml_ProducesValidUrlsetWithAbsoluteLocs()
    {
        var entries = new List<SitemapEntry>
        {
            new("/", null),
            new("/products/split-12000", new DateTime(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc))
        };

        var xml = SeoDocumentBuilder.BuildSitemapXml("https://www.climasite.com", entries);

        var doc = XDocument.Parse(xml);
        doc.Root!.Name.Should().Be(Ns + "urlset");

        var locs = doc.Root.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();
        locs.Should().Contain("https://www.climasite.com/");
        locs.Should().Contain("https://www.climasite.com/products/split-12000");
        locs.Should().OnlyContain(loc => loc.StartsWith("https://www.climasite.com"));
    }

    [Fact]
    public void BuildSitemapXml_EmitsLastmodOnlyWhenPresent_InW3CDateFormat()
    {
        var entries = new List<SitemapEntry>
        {
            new("/products", null),
            new("/products/foo", new DateTime(2026, 1, 5, 23, 59, 0, DateTimeKind.Utc))
        };

        var xml = SeoDocumentBuilder.BuildSitemapXml("https://example.com", entries);
        var doc = XDocument.Parse(xml);

        var urls = doc.Root!.Elements(Ns + "url").ToList();
        var staticUrl = urls.Single(u => u.Element(Ns + "loc")!.Value == "https://example.com/products");
        var dynamicUrl = urls.Single(u => u.Element(Ns + "loc")!.Value == "https://example.com/products/foo");

        staticUrl.Element(Ns + "lastmod").Should().BeNull();
        dynamicUrl.Element(Ns + "lastmod")!.Value.Should().Be("2026-01-05");
    }

    [Fact]
    public void BuildSitemapXml_TrimsTrailingSlashFromBase()
    {
        var xml = SeoDocumentBuilder.BuildSitemapXml("https://example.com/", new List<SitemapEntry> { new("/products", null) });
        var doc = XDocument.Parse(xml);

        doc.Root!.Element(Ns + "url")!.Element(Ns + "loc")!.Value
            .Should().Be("https://example.com/products");
    }

    [Fact]
    public void BuildSitemapXml_DeclaresUtf8()
    {
        var xml = SeoDocumentBuilder.BuildSitemapXml("https://example.com", new List<SitemapEntry> { new("/", null) });

        xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"");
    }

    [Fact]
    public void BuildRobotsTxt_AllowsAll_DisallowsPrivatePrefixes_AndNeverApi()
    {
        var robots = SeoDocumentBuilder.BuildRobotsTxt("https://www.climasite.com");

        robots.Should().Contain("User-agent: *");
        robots.Should().Contain("Allow: /");
        robots.Should().Contain("Disallow: /admin/");
        robots.Should().Contain("Disallow: /account/");
        robots.Should().Contain("Disallow: /checkout");
        robots.Should().Contain("Disallow: /cart");
        robots.Should().Contain("Disallow: /login");
        robots.Should().Contain("Disallow: /register");
        robots.Should().Contain("Disallow: /forgot-password");
        robots.Should().Contain("Disallow: /reset-password");
        robots.Should().Contain("Disallow: /wishlist");

        // Googlebot needs the public API XHRs to render the CSR app — must NOT be disallowed.
        robots.Should().NotContain("Disallow: /api");
    }

    [Fact]
    public void BuildRobotsTxt_IncludesAbsoluteSitemapLine_WhenBaseProvided()
    {
        var robots = SeoDocumentBuilder.BuildRobotsTxt("https://www.climasite.com");

        robots.Should().Contain("Sitemap: https://www.climasite.com/sitemap.xml");
    }

    [Fact]
    public void BuildRobotsTxt_OmitsSitemapLine_WhenBaseNull()
    {
        var robots = SeoDocumentBuilder.BuildRobotsTxt(null);

        robots.Should().NotContain("Sitemap:");
        // Still serves the directives so crawlers honor the disallows even in the fail-closed case.
        robots.Should().Contain("Disallow: /admin/");
    }
}
