using ClimaSite.Api.Services;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;

namespace ClimaSite.Api.Middleware;

/// <summary>
/// Establishes a trusted guest identity for anonymous shoppers (INV-01 Wave A0). A request presenting a valid
/// signed guest cookie has its verified id published on <see cref="IGuestSessionAccessor"/> (and
/// <c>HttpContext.Items["GuestSessionId"]</c>) on EVERY path; a request without a valid cookie is minted a
/// fresh signed httpOnly cookie ONLY on checkout-relevant paths (<see cref="GuestSessionOptions.MintPathPrefixes"/>),
/// subject to a per-IP anti-grief cap. In A0 this ships DARK — the published id is not yet authoritative for
/// cart-keying (that flips in Wave A). Runs after CORS (so credentialed cross-origin responses are handled) and
/// before authentication (the guest identity is orthogonal to the bearer principal). The cookie value is a
/// signed token; only the bare verified id is ever exposed to the app.
/// </summary>
public class GuestSessionMiddleware
{
    private const string ItemsKey = "GuestSessionId";
    private const int CookieLifetimeDays = 30;

    private readonly RequestDelegate _next;
    private readonly IGuestSessionTokenService _tokenService;
    private readonly IGuestSessionMintLimiter _mintLimiter;
    private readonly GuestSessionOptions _options;
    private readonly bool _secureCookie;
    private readonly string _cookieName;

    public GuestSessionMiddleware(
        RequestDelegate next,
        IGuestSessionTokenService tokenService,
        IGuestSessionMintLimiter mintLimiter,
        GuestSessionOptions options,
        IWebHostEnvironment environment)
    {
        _next = next;
        _tokenService = tokenService;
        _mintLimiter = mintLimiter;
        _options = options;

        // Any deployed (HTTPS) environment — Production AND Staging/QA — gets a Secure, __Host--prefixed cookie;
        // only local Development and the http TestServer (Testing) fall back to the unprefixed non-Secure name
        // (__Host- and Secure both require HTTPS). __Host- hard-binds the cookie to Secure + Path=/ + no Domain,
        // all satisfied below.
        _secureCookie = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
        _cookieName = _secureCookie ? "__Host-cs_guest" : "cs_guest";
    }

    // The accessor is scoped, so it is method-injected (per request) rather than captured in the singleton ctor.
    public async Task InvokeAsync(HttpContext context, GuestSessionAccessor accessor)
    {
        var cookie = context.Request.Cookies[_cookieName];

        if (_tokenService.TryValidate(cookie, out var id))
        {
            // Valid signed cookie on ANY path — publish the trusted id (Wave A consumes it), never re-issue.
            Publish(context, accessor, id);
        }
        else if (ShouldMint(context.Request.Path) && _mintLimiter.TryReserveMint(ResolveClientIp(context)))
        {
            // Mint ONLY on checkout-relevant paths and only under the per-IP budget, so infra/crawler/cacheable
            // traffic neither burns the budget nor risks the output-cache Set-Cookie interaction.
            var token = _tokenService.Issue();
            if (_tokenService.TryValidate(token, out var mintedId))
            {
                context.Response.Cookies.Append(_cookieName, token, BuildCookieOptions());
                Publish(context, accessor, mintedId);
            }
        }
        // Otherwise (non-mint path, or over the cap): leave the accessor null and set no cookie.

        await _next(context);
    }

    private bool ShouldMint(PathString path)
    {
        var prefixes = _options.MintPathPrefixes;
        if (prefixes is null)
        {
            return false;
        }

        foreach (var prefix in prefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void Publish(HttpContext context, GuestSessionAccessor accessor, string id)
    {
        accessor.GuestSessionId = id;
        context.Items[ItemsKey] = id;
    }

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = _secureCookie,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddDays(CookieLifetimeDays)
    };

    // Connection.RemoteIpAddress is already the real client — UseForwardedHeaders resolved it upstream. Do NOT
    // re-parse the raw X-Forwarded-For header here; the cleared-KnownProxies XFF trust surface is an inherited
    // platform-wide concern (OPS-08), not one to widen in this middleware.
    private static string ResolveClientIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null)
        {
            return "unknown";
        }

        // Only collapse an IPv4-mapped IPv6 address to IPv4; keep a NATIVE IPv6 address intact so distinct v6
        // clients don't collide into one mint bucket (MapToIPv4 on native IPv6 derives a bogus 32-bit value).
        return (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
    }
}

public static class GuestSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseGuestSession(this IApplicationBuilder builder)
        => builder.UseMiddleware<GuestSessionMiddleware>();
}
