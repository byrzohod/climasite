using System.Net;
using System.Xml.Linq;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Seo;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Controllers;

public class SeoControllerTests : IntegrationTestBase
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public SeoControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task SeedCatalogAsync()
    {
        var activeProduct = new Product("AC-ACTIVE", "Active AC", "active-ac", 499.99m);
        activeProduct.SetActive(true);
        var inactiveProduct = new Product("AC-HIDDEN", "Hidden AC", "inactive-ac", 399.99m);
        inactiveProduct.SetActive(false);
        DbContext.Products.AddRange(activeProduct, inactiveProduct);

        var activeCategory = new Category("Cooling", "cooling");
        activeCategory.SetActive(true);
        var inactiveCategory = new Category("Hidden Category", "hidden-cat");
        inactiveCategory.SetActive(false);
        DbContext.Categories.AddRange(activeCategory, inactiveCategory);

        var activeBrand = new Brand("Daikin", "daikin");
        activeBrand.SetActive(true);
        var inactiveBrand = new Brand("Hidden Brand", "hidden-brand");
        inactiveBrand.SetActive(false);
        DbContext.Brands.AddRange(activeBrand, inactiveBrand);

        var now = DateTime.UtcNow;
        var inWindowPromo = new Promotion("Summer Sale", "summer-sale", PromotionType.Percentage, 10m,
            now.AddDays(-1), now.AddDays(1));
        inWindowPromo.SetActive(true);
        var expiredPromo = new Promotion("Winter Sale", "winter-sale", PromotionType.Percentage, 10m,
            now.AddDays(-10), now.AddDays(-5));
        expiredPromo.SetActive(true); // active flag set, but the window has passed -> excluded
        DbContext.Promotions.AddRange(inWindowPromo, expiredPromo);

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Sitemap_IsValidXml_IncludesActive_ExcludesInactiveAndExpired()
    {
        await SeedCatalogAsync();

        var response = await Client.GetAsync("/sitemap.xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml); // valid XML or this throws
        doc.Root!.Name.Should().Be(Ns + "urlset");

        var locs = doc.Root.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        // Every loc is absolute.
        locs.Should().OnlyContain(loc => loc.StartsWith("http://") || loc.StartsWith("https://"));

        // Active dynamic entries are present.
        locs.Should().Contain(loc => loc.EndsWith("/products/active-ac"));
        locs.Should().Contain(loc => loc.EndsWith("/products/category/cooling"));
        locs.Should().Contain(loc => loc.EndsWith("/brands/daikin"));
        locs.Should().Contain(loc => loc.EndsWith("/promotions/summer-sale"));

        // Inactive / expired entries are excluded.
        locs.Should().NotContain(loc => loc.EndsWith("/products/inactive-ac"));
        locs.Should().NotContain(loc => loc.EndsWith("/products/category/hidden-cat"));
        locs.Should().NotContain(loc => loc.EndsWith("/brands/hidden-brand"));
        locs.Should().NotContain(loc => loc.EndsWith("/promotions/winter-sale"));

        // Categories are crawled via the product-list filter route, never /categories/{slug} (soft-404).
        locs.Should().NotContain(loc => loc.Contains("/categories/cooling"));

        // At least one entry carries a <lastmod> (the dynamic ones do).
        doc.Root.Elements(Ns + "url").Elements(Ns + "lastmod").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sitemap_PercentEncodesReservedCharactersInSlugs()
    {
        // Slugs are persisted lowercase but not validated against URL-reserved characters, so a slug with
        // '/' or '?' must be percent-encoded in the <loc> path (XElement only XML-escapes) and the XML
        // must stay well-formed.
        var product = new Product("AC-RESERVED", "Reserved Slug AC", "a/b?c", 299.99m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        var doc = XDocument.Parse(xml); // well-formed or this throws
        var locs = doc.Root!.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        locs.Should().Contain(loc => loc.EndsWith("/products/a%2Fb%3Fc"));
        // The raw reserved characters must not leak into the path.
        locs.Should().NotContain(loc => loc.EndsWith("/products/a/b?c"));
    }

    [Fact]
    public async Task Sitemap_IncludesEveryStaticPublicPage()
    {
        var response = await Client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        var locs = doc.Root!.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        foreach (var path in SeoPaths.StaticPages)
        {
            // Smoke: each static path is a real frontend route (verified against app.routes.ts/legal.routes.ts),
            // so the feed is not a soft-404 list.
            locs.Should().Contain(loc => loc.EndsWith(path == "/" ? "/" : path),
                $"static page {path} should be in the sitemap");
        }
    }

    [Fact]
    public async Task Robots_IsPlainText_DisallowsPrivatePrefixes_NotApi_AndEmitsAbsoluteSitemap()
    {
        var response = await Client.GetAsync("/robots.txt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("User-agent: *");
        body.Should().Contain("Allow: /");
        body.Should().Contain("Disallow: /admin/");
        body.Should().Contain("Disallow: /account/");
        body.Should().Contain("Disallow: /checkout");
        body.Should().Contain("Disallow: /cart");
        body.Should().Contain("Disallow: /wishlist");
        body.Should().NotContain("Disallow: /api");

        // Default Testing factory resolves the base from the request host -> absolute Sitemap line.
        body.Should().MatchRegex(@"Sitemap: https?://[^/]+/sitemap\.xml");
    }

    [Fact]
    public async Task Robots_IsNotMarkedNoindex()
    {
        var response = await Client.GetAsync("/robots.txt");

        // The crawler files live at the site root, not under /api, so they must stay indexable/crawlable.
        response.Headers.Contains("X-Robots-Tag").Should().BeFalse();
    }

    [Fact]
    public async Task ApiResponses_CarryXRobotsTagNoindex()
    {
        var response = await Client.GetAsync($"/api/categories?cb={Guid.NewGuid()}");

        response.Headers.TryGetValues("X-Robots-Tag", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("noindex");
    }

    [Fact]
    public async Task Sitemap_ResolvesPublicHostAndScheme_FromForwardedHeaders()
    {
        await SeedCatalogAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Host = "shop.example.com";
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await Client.SendAsync(request);
        var xml = await response.Content.ReadAsStringAsync();
        var locs = XDocument.Parse(xml).Root!.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        locs.Should().OnlyContain(loc => loc.StartsWith("https://shop.example.com"));
        locs.Should().Contain("https://shop.example.com/");
    }

    [Fact]
    public async Task Sitemap_ConfiguredSiteBaseUrl_WinsOverRequestHost()
    {
        using var client = Factory
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seo:SiteBaseUrl"] = "https://canonical.climasite.com"
                })))
            .CreateClient();

        // Even with a different request Host, the configured canonical base must win.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Host = "attacker.example";

        var response = await client.SendAsync(request);
        var xml = await response.Content.ReadAsStringAsync();
        var locs = XDocument.Parse(xml).Root!.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        locs.Should().OnlyContain(loc => loc.StartsWith("https://canonical.climasite.com"));
    }

    [Fact]
    public async Task Sitemap_InvalidSiteBaseUrl_IsTreatedAsUnset_AndFallsBackToRequestHost()
    {
        using var client = Factory
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seo:SiteBaseUrl"] = "not-a-valid-url"
                })))
            .CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Host = "fallback.example.com";

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var xml = await response.Content.ReadAsStringAsync();
        var locs = XDocument.Parse(xml).Root!.Elements(Ns + "url").Elements(Ns + "loc").Select(e => e.Value).ToList();

        // The invalid value is ignored; Testing env falls back to the request host.
        locs.Should().Contain("http://fallback.example.com/");
        locs.Should().NotContain(loc => loc.Contains("not-a-valid-url"));
    }

    [Fact]
    public async Task Sitemap_FailsClosed_With503_InNonPublicEnv_WhenBaseUrlMissing()
    {
        using var client = Factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Staging"))
            .CreateClient();

        var response = await client.GetAsync("/sitemap.xml");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Robots_OmitsSitemapLine_WhenBaseUrlMissingInPublicEnv()
    {
        using var client = Factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Staging"))
            .CreateClient();

        var response = await client.GetAsync("/robots.txt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        // Fail-closed: no Sitemap line (no trusted canonical host), but the directives are still served.
        body.Should().NotContain("Sitemap:");
        body.Should().Contain("Disallow: /admin/");
    }
}
