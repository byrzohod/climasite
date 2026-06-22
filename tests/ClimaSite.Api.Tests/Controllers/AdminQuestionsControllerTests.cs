using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the admin Q&amp;A moderation surface
/// (<c>/api/admin/questions</c>). Real controller → MediatR handler → Postgres path via
/// Testcontainers. Questions and answers are seeded directly (ProductId/QuestionId are the required
/// foreign keys; UserId is optional), then moderated through the API; the Admin-role authorization
/// contract is asserted too.
/// </summary>
public class AdminQuestionsControllerTests : IntegrationTestBase
{
    private const string AdminSecret = "configured-secret";

    public AdminQuestionsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authorization

    [Fact]
    public async Task GetPendingModeration_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/questions/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPendingModeration_Returns403_WhenAuthenticatedButNotAdmin()
    {
        await AuthenticateAsync($"customer_{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync("/api/admin/questions/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveQuestion_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PostAsync($"/api/admin/questions/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region List

    [Fact]
    public async Task GetPendingModeration_AsAdmin_ReturnsPendingQuestion()
    {
        var question = await SeedQuestionAsync("QST-LIST-001", QuestionStatus.Pending, "Does it support wifi?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/questions/pending?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(question.Id.ToString());
    }

    [Fact]
    public async Task GetQuestionsByStatus_AsAdmin_FiltersByApproved()
    {
        var approved = await SeedQuestionAsync("QST-FLT-001", QuestionStatus.Approved, "Already approved question?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/questions?status=Approved&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(approved.Id.ToString());
    }

    #endregion

    #region Question moderation happy paths

    [Fact]
    public async Task ApproveQuestion_AsAdmin_SetsStatusApproved()
    {
        var question = await SeedQuestionAsync("QST-APR-001", QuestionStatus.Pending, "Pending question to approve?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsync($"/api/admin/questions/{question.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.ProductQuestions.AsNoTracking().FirstAsync(q => q.Id == question.Id);
        updated.Status.Should().Be(QuestionStatus.Approved);
    }

    [Fact]
    public async Task RejectQuestion_AsAdmin_SetsStatusRejected()
    {
        var question = await SeedQuestionAsync("QST-REJ-001", QuestionStatus.Pending, "Pending question to reject?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/questions/{question.Id}/reject",
            new { note = "Off topic" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.ProductQuestions.AsNoTracking().FirstAsync(q => q.Id == question.Id);
        updated.Status.Should().Be(QuestionStatus.Rejected);
    }

    [Fact]
    public async Task FlagQuestion_AsAdmin_SetsStatusFlagged()
    {
        var question = await SeedQuestionAsync("QST-FLG-001", QuestionStatus.Pending, "Pending question to flag?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/questions/{question.Id}/flag",
            new { note = "Needs review" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.ProductQuestions.AsNoTracking().FirstAsync(q => q.Id == question.Id);
        updated.Status.Should().Be(QuestionStatus.Flagged);
    }

    [Fact]
    public async Task BulkApproveQuestions_AsAdmin_ApprovesAll()
    {
        var first = await SeedQuestionAsync("QST-BLK-001", QuestionStatus.Pending, "First bulk question?");
        var second = await SeedQuestionAsync("QST-BLK-002", QuestionStatus.Pending, "Second bulk question?");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            "/api/admin/questions/bulk-approve",
            new { ids = new[] { first.Id, second.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"approved\":2");

        var statuses = await DbContext.ProductQuestions.AsNoTracking()
            .Where(q => q.Id == first.Id || q.Id == second.Id)
            .Select(q => q.Status)
            .ToListAsync();
        statuses.Should().AllBeEquivalentTo(QuestionStatus.Approved);
    }

    #endregion

    #region Answer moderation happy paths

    [Fact]
    public async Task ApproveAnswer_AsAdmin_SetsStatusApproved()
    {
        var question = await SeedQuestionAsync("ANS-APR-Q", QuestionStatus.Approved, "Question with answer?");
        var answer = await SeedAnswerAsync(question.Id, AnswerStatus.Pending, "Yes, it does.");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/questions/answers/{answer.Id}/approve",
            new { markAsOfficial = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.ProductAnswers.AsNoTracking().FirstAsync(a => a.Id == answer.Id);
        updated.Status.Should().Be(AnswerStatus.Approved);
        updated.IsOfficial.Should().BeTrue();
    }

    [Fact]
    public async Task RejectAnswer_AsAdmin_SetsStatusRejected()
    {
        var question = await SeedQuestionAsync("ANS-REJ-Q", QuestionStatus.Approved, "Question with bad answer?");
        var answer = await SeedAnswerAsync(question.Id, AnswerStatus.Pending, "Spam answer.");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/questions/answers/{answer.Id}/reject",
            new { note = "Spam" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.ProductAnswers.AsNoTracking().FirstAsync(a => a.Id == answer.Id);
        updated.Status.Should().Be(AnswerStatus.Rejected);
    }

    #endregion

    #region Moderation not-found

    [Fact]
    public async Task ApproveQuestion_AsAdmin_Returns400_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsync($"/api/admin/questions/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task ApproveAnswer_AsAdmin_Returns400_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/questions/answers/{Guid.NewGuid()}/approve",
            new { markAsOfficial = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    #endregion

    private async Task<ProductQuestion> SeedQuestionAsync(string productSku, QuestionStatus status, string text)
    {
        var product = new Product(productSku, $"Product {productSku}", $"product-{productSku.ToLowerInvariant()}", 999m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var question = new ProductQuestion(product.Id, text);
        question.SetAskerInfo("Curious Buyer", "buyer@example.com");
        question.SetStatus(status);
        DbContext.ProductQuestions.Add(question);
        await DbContext.SaveChangesAsync();

        return question;
    }

    private async Task<ProductAnswer> SeedAnswerAsync(Guid questionId, AnswerStatus status, string text)
    {
        var answer = new ProductAnswer(questionId, text);
        answer.SetAnswererName("Helpful Staff");
        answer.SetStatus(status);
        DbContext.ProductAnswers.Add(answer);
        await DbContext.SaveChangesAsync();

        return answer;
    }

    /// <summary>
    /// Registers a fresh user, elevates them to Admin via the test endpoint, then logs in again so
    /// the JWT carries the Admin role.
    /// </summary>
    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = Factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["TestSettings:AdminSecret"] = AdminSecret
                    });
                });
            })
            .CreateClient();

        var email = $"admin_{Guid.NewGuid():N}@example.com";
        const string password = "AdminPass123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Admin",
            lastName = "User"
        });
        register.IsSuccessStatusCode.Should().BeTrue();
        var registered = await register.Content.ReadFromJsonAsync<RegisterPayload>();

        var elevate = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = registered!.Id,
            testSecret = AdminSecret
        });
        elevate.IsSuccessStatusCode.Should().BeTrue();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.IsSuccessStatusCode.Should().BeTrue();
        var auth = await login.Content.ReadFromJsonAsync<AuthPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private record RegisterPayload(Guid Id);
    private record AuthPayload(string AccessToken);
}
