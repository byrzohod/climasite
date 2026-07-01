using ClimaSite.Application.Common.Options;
using Microsoft.Extensions.Caching.Memory;

namespace ClimaSite.Api.Services;

/// <summary>
/// Per-IP anti-grief cap on guest-cookie minting (INV-01 Wave A0). Extracted from the middleware so the
/// counter logic is deterministically unit-testable in isolation from HTTP. A fixed one-minute window per IP
/// is anchored at first mint (the expiry is set only when the counter is created), and the counter is bumped
/// with <see cref="Interlocked.Increment(ref long)"/>; a short lock guards ONLY the get-or-create so a cold-key
/// burst can't each create-and-discard a counter (which would drop increments and overshoot the cap).
/// </summary>
public interface IGuestSessionMintLimiter
{
    /// <summary>Returns <see langword="true"/> if a new cookie may be minted for <paramref name="clientIp"/>
    /// within the current window, consuming one unit of its budget.</summary>
    bool TryReserveMint(string clientIp);
}

public sealed class GuestSessionMintLimiter : IGuestSessionMintLimiter
{
    private const int DefaultCap = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly IMemoryCache _cache;
    private readonly GuestSessionOptions _options;
    private readonly object _createLock = new();

    public GuestSessionMintLimiter(IMemoryCache cache, GuestSessionOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public bool TryReserveMint(string clientIp)
    {
        var limit = _options.MintRateLimitPerMinutePerIp > 0
            ? _options.MintRateLimitPerMinutePerIp
            : DefaultCap;

        MintCounter counter;
        lock (_createLock)
        {
            counter = _cache.GetOrCreate($"guest-session-mint:{clientIp}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Window;
                return new MintCounter();
            })!;
        }

        return Interlocked.Increment(ref counter.Count) <= limit;
    }

    private sealed class MintCounter
    {
        public long Count;
    }
}
