namespace ClimaSite.Application.Features.Seo;

/// <summary>
/// Pure, host-injection-safe resolver for the absolute public base URL used to render canonical SEO URLs
/// (B-044 Wave B, council #2/#3 + R3). Resolution order:
/// <list type="number">
///   <item><c>Seo:SiteBaseUrl</c> when set — validated as an absolute <c>https://</c> URI (or <c>http://</c>
///   only in non-public environments); rejected + treated as unset otherwise. Used in ALL environments.</item>
///   <item>else, only in non-public (Development/Testing) environments, the request scheme + host (the nginx
///   SEO locations set the real <c>Host</c> header, so <c>Request.Host</c> is already the public host —
///   no global forwarded-host trust required).</item>
///   <item>else (Staging/Production with empty/invalid config): <c>null</c> — the caller fails closed
///   (sitemap → 503, robots omits the <c>Sitemap:</c> line) rather than emit URLs from an untrusted host.</item>
/// </list>
/// </summary>
public static class SeoBaseUrlResolver
{
    /// <param name="configuredBaseUrl">The raw <c>Seo:SiteBaseUrl</c> value (may be null/empty/invalid).</param>
    /// <param name="isNonPublicEnvironment">True for Development/Testing (local + CI), where a request-host
    /// fallback is safe; false for Staging/Production where we must fail closed.</param>
    /// <param name="requestScheme">The request scheme (already reflecting X-Forwarded-Proto via the
    /// ForwardedHeaders middleware). Used only for the non-public fallback.</param>
    /// <param name="requestHost">The request host header. Used only for the non-public fallback.</param>
    public static SeoBaseUrlResult Resolve(
        string? configuredBaseUrl,
        bool isNonPublicEnvironment,
        string? requestScheme,
        string? requestHost)
    {
        string? warning = null;

        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            if (TryNormalize(configuredBaseUrl, allowHttp: isNonPublicEnvironment, out var normalized))
            {
                return new SeoBaseUrlResult(normalized, null);
            }

            // Invalid configured value: reject + treat as unset, then fall through to the next rule.
            warning = $"Seo:SiteBaseUrl is set but is not a valid absolute https URL ('{configuredBaseUrl}'); " +
                      "treating it as unset.";
        }

        if (isNonPublicEnvironment
            && !string.IsNullOrWhiteSpace(requestScheme)
            && !string.IsNullOrWhiteSpace(requestHost))
        {
            var fallback = $"{requestScheme}://{requestHost}".TrimEnd('/');
            return new SeoBaseUrlResult(fallback, warning);
        }

        // Staging/Production with empty or invalid config — fail closed.
        warning ??= "Seo:SiteBaseUrl is not configured; serving robots without a Sitemap line and " +
                    "failing the sitemap closed (set Seo__SiteBaseUrl to the canonical public origin).";
        return new SeoBaseUrlResult(null, warning);
    }

    /// <summary>
    /// Validates and normalizes a configured base URL to <c>scheme://authority</c> (scheme + host + optional
    /// port, no path/query/fragment/trailing slash). Requires an absolute https URI, or http only when
    /// <paramref name="allowHttp"/> (non-public environments).
    /// </summary>
    private static bool TryNormalize(string value, bool allowHttp, out string normalized)
    {
        normalized = string.Empty;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isHttp = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        if (!isHttps && !(allowHttp && isHttp))
        {
            return false;
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        normalized = $"{uri.Scheme.ToLowerInvariant()}://{uri.Authority}";
        return true;
    }
}

/// <summary>
/// Result of base-URL resolution. <see cref="BaseUrl"/> is null when the caller must fail closed.
/// <see cref="Warning"/> carries a single log-worthy message (rejected config / fail-closed), or null.
/// </summary>
public readonly record struct SeoBaseUrlResult(string? BaseUrl, string? Warning);
