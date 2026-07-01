using ClimaSite.Api.Services;
using ClimaSite.Application.Common.Options;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace ClimaSite.Api.Tests.Features.GuestSession;

/// <summary>
/// INV-01 Wave A0 — the per-IP mint cap in isolation. This is the authoritative, deterministic coverage of the
/// budget boundary and per-IP partitioning (the middleware wiring is exercised separately in
/// <see cref="GuestSessionMiddlewareTests"/>). A fresh <see cref="MemoryCache"/> per test guarantees a clean
/// window.
/// </summary>
public class GuestSessionMintLimiterTests
{
    private static GuestSessionMintLimiter Create(int cap) =>
        new(new MemoryCache(new MemoryCacheOptions()),
            new GuestSessionOptions { MintRateLimitPerMinutePerIp = cap });

    [Fact]
    public void AllowsUpToTheCap_ThenDenies()
    {
        var limiter = Create(cap: 3);

        limiter.TryReserveMint("1.1.1.1").Should().BeTrue();
        limiter.TryReserveMint("1.1.1.1").Should().BeTrue();
        limiter.TryReserveMint("1.1.1.1").Should().BeTrue();
        limiter.TryReserveMint("1.1.1.1").Should().BeFalse();
        limiter.TryReserveMint("1.1.1.1").Should().BeFalse();
    }

    [Fact]
    public void PartitionsPerIp_SoOneIpExhaustionDoesNotAffectAnother()
    {
        var limiter = Create(cap: 1);

        limiter.TryReserveMint("1.1.1.1").Should().BeTrue();
        limiter.TryReserveMint("1.1.1.1").Should().BeFalse("that IP's single-mint budget is spent");
        limiter.TryReserveMint("2.2.2.2").Should().BeTrue("a different IP has an independent bucket");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveCap_FallsBackToTheSafeDefault(int misconfiguredCap)
    {
        var limiter = Create(misconfiguredCap);

        for (var i = 0; i < 20; i++)
        {
            limiter.TryReserveMint("9.9.9.9").Should().BeTrue($"mint {i + 1} is within the default budget");
        }

        limiter.TryReserveMint("9.9.9.9").Should().BeFalse("the default budget of 20 is exhausted");
    }
}
