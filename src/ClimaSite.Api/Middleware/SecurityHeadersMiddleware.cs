namespace ClimaSite.Api.Middleware;

/// <summary>
/// Adds defensive response security headers to every response (SEC-08). The API serves JSON (it does not
/// serve the Angular SPA), so the Content-Security-Policy is intentionally strict — Swagger UI is the one
/// exception (it needs inline scripts/styles), so CSP is skipped for <c>/swagger</c> paths. The
/// Stripe-compatible <em>frontend</em> CSP, the CORS header allowlist, and AllowedHosts are deploy-time
/// concerns (OPS-08) handled where the SPA is served, not here.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Set just before the response is sent so the headers survive whatever later middleware/handlers
        // do (incl. the exception handler writing an error body).
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "0"; // modern guidance: disable the legacy XSS auditor
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Strict CSP for the JSON API. Skip Swagger UI, which needs inline scripts/styles.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            // B-044: keep API responses out of the search index (a JS-independent guard, layered over the
            // SPA route-meta noindex) while leaving /api crawlable so Googlebot can render the CSR app. The
            // crawler files themselves live at the site root (~/robots.txt, ~/sitemap.xml), not under /api,
            // so they are intentionally NOT marked noindex.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                headers["X-Robots-Tag"] = "noindex";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        => builder.UseMiddleware<SecurityHeadersMiddleware>();
}
