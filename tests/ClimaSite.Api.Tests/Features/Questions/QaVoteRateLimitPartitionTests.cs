using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Features.Questions;

/// <summary>
/// B-039 follow-up — the regression guard for the "strict-user" per-user partition AND the middleware order it
/// depends on. Rate limiting is disabled in the default Testing factory, so this test spins a variant with it
/// re-enabled (RateLimiting:Enabled=true) and drives the REAL limiter pipeline: one authenticated user
/// exhausts the 5/min budget on the vote endpoint, while a DIFFERENT authenticated user on the same host is
/// still served. That only holds if the limiter keys by USER — which requires <c>UseAuthentication()</c> to
/// run before <c>UseRateLimiter()</c>. If a future edit reorders them (or reverts the policy to IP-only), the
/// second user collapses into the first's bucket and this fails.
/// </summary>
public class QaVoteRateLimitPartitionTests : IntegrationTestBase
{
    public QaVoteRateLimitPartitionTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task StrictUser_PartitionsPerUser_SoOneUsersBurstDoesNotThrottleAnother()
    {
        // Seed a votable approved answer authored anonymously (UserId null) so neither voter is the author
        // (a self-vote would 400, not 200/429).
        var product = new Product("QA-RL-001", "Rate-limit Q&A Product", "qa-rl-product", 599.99m);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var question = new ProductQuestion(product.Id, "Which wall mount is included with this unit?");
        question.SetStatus(QuestionStatus.Approved);
        DbContext.ProductQuestions.Add(question);
        await DbContext.SaveChangesAsync();

        var answer = new ProductAnswer(question.Id, "A wall bracket and fixings are included in the box.");
        answer.SetStatus(AnswerStatus.Approved);
        DbContext.ProductAnswers.Add(answer);
        await DbContext.SaveChangesAsync();

        // A variant of the app with the rate limiter ON (the shared Testing factory disables it). Shares the
        // same test database container as the base factory.
        using var rlFactory = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "true"
                })));

        var tokenA = await RegisterAndLoginAsync(rlFactory, "voter-a@ratelimit.local");
        var tokenB = await RegisterAndLoginAsync(rlFactory, "voter-b@ratelimit.local");

        using var clientA = rlFactory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using var clientB = rlFactory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var votePath = $"/api/questions/answers/{answer.Id}/vote";
        var voteBody = new { isHelpful = true };

        // User A bursts well past the 5/min budget. 11 rapid requests can't all pass even if the fixed window
        // happens to roll mid-burst (≤5 per window, ≤2 windows touched), so A is guaranteed to be throttled.
        var aStatuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            var response = await clientA.PostAsJsonAsync(votePath, voteBody);
            aStatuses.Add(response.StatusCode);
        }

        aStatuses.Should().Contain(HttpStatusCode.OK, "the first votes are within A's budget");
        aStatuses.Should().Contain(HttpStatusCode.TooManyRequests, "A's burst exceeds the 5/min strict-user budget");

        // User B — same host, fresh bucket. Served iff the limiter keyed by user, not IP.
        var bResponse = await clientB.PostAsJsonAsync(votePath, voteBody);
        bResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "a different authenticated user has an independent strict-user bucket (per-user partitioning + auth-before-limiter)");
    }

    private static async Task<string> RegisterAndLoginAsync(WebApplicationFactory<Program> factory, string email)
    {
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!", firstName = "Rate", lastName = "Limit" });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.AccessToken;
    }

    private sealed record TokenResponse(string AccessToken);
}
