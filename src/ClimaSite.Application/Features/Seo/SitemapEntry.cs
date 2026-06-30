namespace ClimaSite.Application.Features.Seo;

/// <summary>
/// A single host-independent sitemap entry: a site-relative <paramref name="Path"/> (always starting with
/// "/") and an optional <paramref name="LastModified"/> timestamp. The absolute <c>&lt;loc&gt;</c> is
/// composed per-request from the resolved base URL so the cached enumeration never embeds a host
/// (council #3 — no cache poisoning).
/// </summary>
public sealed record SitemapEntry(string Path, DateTime? LastModified);
