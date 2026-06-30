using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClimaSite.Api.Controllers;

/// <summary>
/// Serves the crawler files (B-044 Wave B): a dynamic <c>robots.txt</c> and a dynamic <c>sitemap.xml</c>.
/// Both are mounted at the site root (<c>~/robots.txt</c>, <c>~/sitemap.xml</c>) — NOT under <c>/api</c> —
/// so they are not caught by the <c>X-Robots-Tag: noindex</c> applied to API responses. The absolute base
/// URL is resolved per-request (config-first, host-injection-safe); the host-independent entry enumeration
/// is cached so the absolute documents can be rendered fresh per host without cache poisoning.
/// </summary>
[ApiController]
[AllowAnonymous]
// Opt out of the app's global output-cache base policy: the rendered documents vary by the resolved
// canonical host, so a path-keyed server cache would risk cross-host poisoning. The host-INDEPENDENT
// entry enumeration is cached in-process instead (see GetEntriesAsync), and the absolute docs are
// rendered fresh per request (council #3).
[OutputCache(NoStore = true)]
public class SeoController : ControllerBase
{
    private const string SitemapEntriesCacheKey = "seo:sitemap-entries:v1";
    private static readonly TimeSpan SitemapCacheDuration = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SeoController> _logger;

    public SeoController(
        IApplicationDbContext context,
        IMemoryCache cache,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SeoController> logger)
    {
        _context = context;
        _cache = cache;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Renders robots.txt. Always served; omits the Sitemap line when the base fails closed.</summary>
    [HttpGet("~/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = ResolveBaseUrl();
        var body = SeoDocumentBuilder.BuildRobotsTxt(baseUrl);
        return Content(body, "text/plain; charset=utf-8");
    }

    /// <summary>
    /// Renders sitemap.xml. Fails closed with 503 in Staging/Production when no valid canonical base is
    /// configured (never emits canonical URLs from an untrusted request host).
    /// </summary>
    [HttpGet("~/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var entries = await GetEntriesAsync(cancellationToken);
        var xml = SeoDocumentBuilder.BuildSitemapXml(baseUrl, entries);
        return Content(xml, "application/xml; charset=utf-8");
    }

    private string? ResolveBaseUrl()
    {
        var settings = _configuration.GetSection(SeoSettings.SectionName).Get<SeoSettings>() ?? new SeoSettings();
        var isNonPublic = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");

        var result = SeoBaseUrlResolver.Resolve(
            settings.SiteBaseUrl,
            isNonPublic,
            Request.Scheme,
            Request.Host.HasValue ? Request.Host.Value : null);

        if (result.Warning is not null)
        {
            _logger.LogWarning("SEO base-URL resolution: {Warning}", result.Warning);
        }

        return result.BaseUrl;
    }

    private async Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        // Disable caching under Testing so each integration test sees its own freshly-seeded catalog.
        var cacheEnabled = !_environment.IsEnvironment("Testing");

        if (cacheEnabled
            && _cache.TryGetValue(SitemapEntriesCacheKey, out IReadOnlyList<SitemapEntry>? cached)
            && cached is not null)
        {
            return cached;
        }

        var entries = await BuildEntriesAsync(cancellationToken);

        if (cacheEnabled)
        {
            _cache.Set(SitemapEntriesCacheKey, entries, SitemapCacheDuration);
        }

        return entries;
    }

    private async Task<IReadOnlyList<SitemapEntry>> BuildEntriesAsync(CancellationToken cancellationToken)
    {
        var entries = new List<SitemapEntry>(SeoPaths.StaticPages.Count + 256);
        entries.AddRange(SeoPaths.StaticPages.Select(path => new SitemapEntry(path, null)));

        // Dynamic slugs are persisted lowercase but otherwise unvalidated for URL-reserved characters, so
        // each slug segment is percent-encoded (Uri.EscapeDataString) before it goes into a <loc> path —
        // XElement only XML-escapes, it does not URL-encode, so a slug with '?', '#', '/' or '%' would
        // otherwise publish a broken path/query/fragment. A normal kebab slug ([a-z0-9-]) is unaffected.
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(cancellationToken);
        entries.AddRange(products.Select(p =>
            new SitemapEntry($"/products/{Uri.EscapeDataString(p.Slug)}", p.UpdatedAt)));

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Slug, c.UpdatedAt })
            .ToListAsync(cancellationToken);
        // Categories are crawled via the product-list category filter route, NOT /categories/{slug}
        // (which soft-404s under the CSR app — council #4).
        entries.AddRange(categories.Select(c =>
            new SitemapEntry($"/products/category/{Uri.EscapeDataString(c.Slug)}", c.UpdatedAt)));

        var brands = await _context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => new { b.Slug, b.UpdatedAt })
            .ToListAsync(cancellationToken);
        entries.AddRange(brands.Select(b =>
            new SitemapEntry($"/brands/{Uri.EscapeDataString(b.Slug)}", b.UpdatedAt)));

        // Only currently-active promotions (IsActive AND within the start/end window) — IsCurrentlyActive
        // inlined so the predicate translates to SQL.
        var now = DateTime.UtcNow;
        var promotions = await _context.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive && p.StartDate <= now && now <= p.EndDate)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(cancellationToken);
        entries.AddRange(promotions.Select(p =>
            new SitemapEntry($"/promotions/{Uri.EscapeDataString(p.Slug)}", p.UpdatedAt)));

        return entries;
    }
}
