using System.Text.RegularExpressions;
using Serilog.Context;

namespace ClimaSite.Api.Middleware;

/// <summary>
/// Correlation IDs (OPS-05): read an inbound <c>X-Correlation-Id</c> (or generate one), make it
/// available to the rest of the request via <see cref="HttpContext.Items"/>, push it onto the Serilog
/// <c>LogContext</c> so every log line for the request shares the id, and echo it on the response so a
/// caller can quote it. Run this EARLY (before the exception handler + request logging).
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    // Only accept an inbound id that is a short, safe token. This blocks log-forging (CR/LF/control
    // chars in the value) and oversized-id bloat — anything else is replaced with a fresh GUID (B-055).
    // \A…\z (absolute anchors, NOT ^…$) so a value ending in a newline can't slip through — in .NET `$`
    // also matches the position before a trailing \n, which would defeat the anti-log-forging intent.
    private static readonly Regex ValidId = new(@"\A[A-Za-z0-9._-]{1,128}\z", RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || !ValidId.IsMatch(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        => builder.UseMiddleware<CorrelationIdMiddleware>();
}
