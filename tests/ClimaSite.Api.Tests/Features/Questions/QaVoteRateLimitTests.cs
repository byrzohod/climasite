using System.Reflection;
using ClimaSite.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace ClimaSite.Api.Tests.Features.Questions;

/// <summary>
/// B-039 (+ follow-up): the Q&A vote endpoints are cheap to spam, so each carries the strict rate-limit
/// budget. They are <c>[Authorize]</c>, so they use the "strict-user" policy which partitions per
/// authenticated user (falling back to IP) — see the B-039 follow-up in Program.cs — so signed-in users
/// behind one NAT/CGNAT IP don't share (and exhaust) a single bucket. The actual 429 is exercised in
/// /acceptance against a Development run (rate limiting is intentionally disabled in the Testing integration
/// env — see Program.cs), so this guards the wiring deterministically: both vote actions must be decorated
/// with the "strict-user" policy. The key-resolution itself is unit-tested in RateLimitPartitioningTests.
/// </summary>
public class QaVoteRateLimitTests
{
    [Theory]
    [InlineData(nameof(QuestionsController.VoteQuestion))]
    [InlineData(nameof(QuestionsController.VoteAnswer))]
    public void VoteEndpoints_AreGuardedByTheStrictUserRateLimitPolicy(string methodName)
    {
        var method = typeof(QuestionsController).GetMethod(methodName);
        method.Should().NotBeNull();

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attribute.Should().NotBeNull("the vote endpoints are cheap to spam and must be rate-limited (B-039)");
        attribute!.PolicyName.Should().Be("strict-user",
            "authenticated vote endpoints partition the strict budget per user, not per IP (B-039 follow-up)");
    }
}
