using System.Security.Claims;

namespace ClimaSite.Api.RateLimiting;

/// <summary>
/// Partition-key resolvers for the API rate-limiter policies. Extracted from <c>Program.cs</c> so the
/// keying logic is unit-testable in isolation (the limiter's per-key partitioning is framework-guaranteed).
/// </summary>
public static class RateLimitPartitioning
{
    /// <summary>Fallback partition token when the client IP is unavailable.</summary>
    public const string UnknownIp = "unknown";

    /// <summary>
    /// IP-only partition key. Used by the pre-authentication policies (global, auth) where no user identity
    /// exists yet, and as the anonymous fallback for <see cref="UserOrIpKey"/>.
    /// </summary>
    public static string IpKey(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? UnknownIp}";

    /// <summary>
    /// Partition by authenticated user when present, else fall back to the client IP. Used by the
    /// "strict-user" policy on the <c>[Authorize]</c> Q&amp;A vote endpoints — user partitioning gives every
    /// signed-in user an independent bucket so shared NAT/CGNAT/corporate-egress clients don't share (and
    /// exhaust) one bucket, while anonymous callers (which those endpoints then reject) keep IP-based
    /// limiting. Mirrors how <c>CurrentUserService</c>/<c>TokenService</c> use
    /// <see cref="ClaimTypes.NameIdentifier"/>. The <c>user:</c>/<c>ip:</c> prefixes keep the two key spaces
    /// disjoint. (B-039 follow-up.)
    /// </summary>
    public static string UserOrIpKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return context.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : IpKey(context);
    }
}
