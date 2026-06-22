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
/// Integration coverage for the admin review moderation surface
/// (<c>/api/admin/reviews</c>). Real controller → MediatR handler → Postgres path via Testcontainers.
/// Reviews are seeded directly against the DB with real Product + User foreign keys, then approved /
/// rejected / flagged through the API; the Admin-role authorization contract is asserted too.
/// </summary>
public class AdminReviewsControllerTests : IntegrationTestBase
{
    private const string AdminSecret = "configured-secret";

    public AdminReviewsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authorization

    [Fact]
    public async Task GetPendingReviews_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/reviews/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPendingReviews_Returns403_WhenAuthenticatedButNotAdmin()
    {
        await AuthenticateAsync($"customer_{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync("/api/admin/reviews/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveReview_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PostAsync($"/api/admin/reviews/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region List

    [Fact]
    public async Task GetPendingReviews_AsAdmin_ReturnsPendingReview()
    {
        var review = await SeedReviewAsync("REV-LIST-001", ReviewStatus.Pending, "Great cooling power");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/reviews/pending?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(review.Id.ToString());
    }

    [Fact]
    public async Task GetReviewsByStatus_AsAdmin_FiltersByApproved()
    {
        var approved = await SeedReviewAsync("REV-FLT-001", ReviewStatus.Approved, "Approved review body");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/reviews?status=Approved&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(approved.Id.ToString());
    }

    #endregion

    #region Moderation happy paths

    [Fact]
    public async Task ApproveReview_AsAdmin_SetsStatusApproved()
    {
        var review = await SeedReviewAsync("REV-APR-001", ReviewStatus.Pending, "Pending review to approve");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsync($"/api/admin/reviews/{review.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.Reviews.AsNoTracking().FirstAsync(r => r.Id == review.Id);
        updated.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public async Task RejectReview_AsAdmin_SetsStatusRejected()
    {
        var review = await SeedReviewAsync("REV-REJ-001", ReviewStatus.Pending, "Pending review to reject");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/reviews/{review.Id}/reject",
            new { note = "Spam content" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.Reviews.AsNoTracking().FirstAsync(r => r.Id == review.Id);
        updated.Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public async Task FlagReview_AsAdmin_SetsStatusFlagged()
    {
        var review = await SeedReviewAsync("REV-FLG-001", ReviewStatus.Pending, "Pending review to flag");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/reviews/{review.Id}/flag",
            new { note = "Needs a second look" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await DbContext.Reviews.AsNoTracking().FirstAsync(r => r.Id == review.Id);
        updated.Status.Should().Be(ReviewStatus.Flagged);
    }

    [Fact]
    public async Task BulkApproveReviews_AsAdmin_ApprovesAll()
    {
        var first = await SeedReviewAsync("REV-BLK-001", ReviewStatus.Pending, "First bulk review");
        var second = await SeedReviewAsync("REV-BLK-002", ReviewStatus.Pending, "Second bulk review");

        using var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsJsonAsync(
            "/api/admin/reviews/bulk-approve",
            new { ids = new[] { first.Id, second.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"approved\":2");

        var statuses = await DbContext.Reviews.AsNoTracking()
            .Where(r => r.Id == first.Id || r.Id == second.Id)
            .Select(r => r.Status)
            .ToListAsync();
        statuses.Should().AllBeEquivalentTo(ReviewStatus.Approved);
    }

    #endregion

    #region Moderation not-found

    [Fact]
    public async Task ApproveReview_AsAdmin_Returns400_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsync($"/api/admin/reviews/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    #endregion

    /// <summary>
    /// Seeds a review with real Product and User foreign keys (both are required, cascade FKs).
    /// The user is created via the registration endpoint so it satisfies Identity's constraints.
    /// </summary>
    private async Task<Review> SeedReviewAsync(string productSku, ReviewStatus status, string content)
    {
        var product = new Product(productSku, $"Product {productSku}", $"product-{productSku.ToLowerInvariant()}", 999m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var userId = await RegisterUserAsync();

        var review = new Review(product.Id, userId, 5);
        review.SetTitle("Solid unit");
        review.SetContent(content);
        review.SetStatus(status);
        DbContext.Reviews.Add(review);
        await DbContext.SaveChangesAsync();

        return review;
    }

    private async Task<Guid> RegisterUserAsync()
    {
        var client = Factory.CreateClient();
        var email = $"reviewer_{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "ReviewerPass123!",
            firstName = "Review",
            lastName = "Author"
        });
        register.IsSuccessStatusCode.Should().BeTrue();
        var payload = await register.Content.ReadFromJsonAsync<RegisterPayload>();
        return payload!.Id;
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
