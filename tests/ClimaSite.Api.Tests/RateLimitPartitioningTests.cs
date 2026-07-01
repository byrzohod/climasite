using System.Net;
using System.Security.Claims;
using ClimaSite.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ClimaSite.Api.Tests;

/// <summary>
/// B-039 follow-up: the "strict-user" rate-limit policy partitions per authenticated user (falling back to
/// client IP for anonymous callers) so signed-in users behind one NAT/CGNAT IP each get an independent bucket.
/// These
/// assert the partition-KEY contract in isolation; the limiter's per-key partitioning itself is
/// framework-guaranteed.
/// </summary>
public class RateLimitPartitioningTests
{
    private static HttpContext Context(string? userId = null, string? ip = "203.0.113.10")
    {
        var ctx = new DefaultHttpContext();
        if (ip != null)
        {
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        }
        if (userId != null)
        {
            ctx.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth"));
        }
        return ctx;
    }

    [Fact]
    public void UserOrIpKey_Authenticated_UsesUserKey()
    {
        var userId = Guid.NewGuid().ToString();

        RateLimitPartitioning.UserOrIpKey(Context(userId: userId)).Should().Be($"user:{userId}");
    }

    [Fact]
    public void UserOrIpKey_TwoDistinctUsers_SameIp_GetDistinctKeys()
    {
        // The core B-039 follow-up guarantee (and the break-probe): revert "strict" to IP-only and this fails,
        // because both users behind one IP would collapse to the same partition key.
        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();

        var keyA = RateLimitPartitioning.UserOrIpKey(Context(userId: userA, ip: "198.51.100.7"));
        var keyB = RateLimitPartitioning.UserOrIpKey(Context(userId: userB, ip: "198.51.100.7"));

        keyA.Should().NotBe(keyB);
        keyA.Should().Be($"user:{userA}");
        keyB.Should().Be($"user:{userB}");
    }

    [Fact]
    public void UserOrIpKey_SameUser_IsStableAcrossRequests()
    {
        var userId = Guid.NewGuid().ToString();

        var first = RateLimitPartitioning.UserOrIpKey(Context(userId: userId, ip: "203.0.113.1"));
        var second = RateLimitPartitioning.UserOrIpKey(Context(userId: userId, ip: "203.0.113.2"));

        second.Should().Be(first, "the same user must map to a stable bucket regardless of source IP");
    }

    [Fact]
    public void UserOrIpKey_Anonymous_FallsBackToIpKey()
    {
        RateLimitPartitioning.UserOrIpKey(Context(userId: null, ip: "192.0.2.55")).Should().Be("ip:192.0.2.55");
    }

    [Fact]
    public void UserOrIpKey_Anonymous_NoIp_UsesUnknownIpKey()
    {
        RateLimitPartitioning.UserOrIpKey(Context(userId: null, ip: null)).Should().Be("ip:unknown");
    }

    [Fact]
    public void UserOrIpKey_UserTakesPrecedenceOverIp()
    {
        var userId = Guid.NewGuid().ToString();

        RateLimitPartitioning.UserOrIpKey(Context(userId: userId, ip: "203.0.113.10"))
            .Should().Be($"user:{userId}", "an authenticated request keys on the user, not the IP");
    }

    [Theory]
    [InlineData("10.1.2.3", "ip:10.1.2.3")]
    [InlineData(null, "ip:unknown")]
    public void IpKey_KeysOnRemoteIp_OrUnknown(string? ip, string expected)
    {
        RateLimitPartitioning.IpKey(Context(userId: null, ip: ip)).Should().Be(expected);
    }
}
