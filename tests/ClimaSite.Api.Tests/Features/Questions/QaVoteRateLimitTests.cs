using System.Reflection;
using ClimaSite.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace ClimaSite.Api.Tests.Features.Questions;

/// <summary>
/// B-039: the Q&A vote endpoints are cheap to spam, so each carries the strict per-IP rate-limit budget.
/// The actual 429 is exercised in /acceptance against a Development run (rate limiting is intentionally
/// disabled in the Testing integration env — see Program.cs), so this guards the wiring deterministically:
/// both vote actions must be decorated with the "strict" policy. Note the policy partitions by remote IP,
/// not by user.
/// </summary>
public class QaVoteRateLimitTests
{
    [Theory]
    [InlineData(nameof(QuestionsController.VoteQuestion))]
    [InlineData(nameof(QuestionsController.VoteAnswer))]
    public void VoteEndpoints_AreGuardedByTheStrictRateLimitPolicy(string methodName)
    {
        var method = typeof(QuestionsController).GetMethod(methodName);
        method.Should().NotBeNull();

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attribute.Should().NotBeNull("the vote endpoints are cheap to spam and must be rate-limited (B-039)");
        attribute!.PolicyName.Should().Be("strict");
    }
}
