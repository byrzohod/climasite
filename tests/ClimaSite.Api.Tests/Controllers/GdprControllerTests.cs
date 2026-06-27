using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Gdpr.DTOs;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the GDPR compliance surface (<c>/api/gdpr</c>):
/// real HTTP -> MediatR handler -> Postgres. Covers data export (Article 20),
/// account deletion (Article 17), the anonymous transparency endpoints, plus
/// 401 for the authenticated-only endpoints and password-confirmation validation.
/// </summary>
public class GdprControllerTests : IntegrationTestBase
{
    public GdprControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Auth

    [Fact]
    public async Task ExportData_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/gdpr/export");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_Returns401_WhenUnauthenticated()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/gdpr/delete")
        {
            Content = JsonContent.Create(new { password = "Password123!", confirmDeletion = true })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Anonymous transparency endpoints

    [Fact]
    public async Task GetDataCategories_IsPubliclyAccessible()
    {
        var response = await Client.GetAsync("/api/gdpr/data-categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<DataCategoryDto>>();
        categories.Should().NotBeNull();
        categories!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetGdprRights_IsPubliclyAccessible_AndDescribesRights()
    {
        var response = await Client.GetAsync("/api/gdpr/rights");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Right to Erasure");
        body.Should().Contain("Article 17");
        body.Should().Contain("dpo@climasite.com");
    }

    #endregion

    #region Export happy path

    [Fact]
    public async Task ExportData_ReturnsCurrentUsersData_WithAttachmentHeader()
    {
        var email = $"gdpr-export-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);

        // Seed an address and a notification so the export has real, owned content.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Addresses.Add(new Address(userId, "Export User", "5 Export Rd", "Burgas", "8000", "Bulgaria", "BG"));
            db.Notifications.Add(new Notification(userId, "order_placed", "Exported notification", "body"));
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/gdpr/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.TryGetValues("Content-Disposition", out var disposition);
        disposition.Should().NotBeNull();
        string.Join(string.Empty, disposition!).Should().Contain("attachment");

        var export = await response.Content.ReadFromJsonAsync<UserDataExportDto>();
        export.Should().NotBeNull();
        export!.Profile.Should().NotBeNull();
        export.Profile.Id.Should().Be(userId);
        export.Profile.Email.Should().Be(email);
        export.Addresses.Should().ContainSingle(a => a.FullName == "Export User");
        export.Notifications.Should().ContainSingle(n => n.Title == "Exported notification");
    }

    [Fact]
    public async Task ExportData_DoesNotIncludeAnotherUsersData()
    {
        var email = $"gdpr-isolation-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(email);
        var userId = await GetUserIdAsync(email);
        var otherUserId = await CreateUserAsync($"gdpr-other-{Guid.NewGuid():N}@example.com");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Addresses.Add(new Address(userId, "Mine", "1 Mine St", "Sofia", "1000", "Bulgaria", "BG"));
            db.Addresses.Add(new Address(otherUserId, "NotMine", "2 Other St", "Sofia", "1000", "Bulgaria", "BG"));
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/gdpr/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await response.Content.ReadFromJsonAsync<UserDataExportDto>();
        export!.Addresses.Should().ContainSingle();
        export.Addresses.Should().OnlyContain(a => a.FullName == "Mine");
    }

    #endregion

    #region Delete account

    [Fact]
    public async Task DeleteAccount_Returns400_WhenDeletionNotConfirmed()
    {
        await AuthenticateAsync($"gdpr-noconfirm-{Guid.NewGuid():N}@example.com");

        var response = await SendDeleteAsync(new { password = "Password123!", confirmDeletion = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("confirm");
    }

    [Fact]
    public async Task DeleteAccount_Returns400_WithWrongPassword()
    {
        await AuthenticateAsync($"gdpr-wrongpw-{Guid.NewGuid():N}@example.com");

        var response = await SendDeleteAsync(new { password = "TotallyWrong123!", confirmDeletion = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid password");
    }

    /// <summary>
    /// Verifies the right-to-erasure endpoint (GDPR Article 17) with a correct password +
    /// confirmation: the account is anonymized and login is blocked. The handler runs the
    /// multi-step deletion inside <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c>
    /// so the manual <c>BeginTransactionAsync</c> is compatible with the
    /// <c>NpgsqlRetryingExecutionStrategy</c> (<c>EnableRetryOnFailure(3)</c>) — the prior
    /// manual transaction threw under retry and made deletion fail in production (BUG-26).
    /// </summary>
    [Fact]
    public async Task DeleteAccount_WithValidPasswordAndConfirmation_AnonymizesAndBlocksLogin()
    {
        var email = $"gdpr-delete-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        await AuthenticateAsync(email, password);
        var userId = await GetUserIdAsync(email);

        var response = await SendDeleteAsync(new { password, reason = "No longer needed", confirmDeletion = true });
        var body = await response.Content.ReadAsStringAsync();

        // Article 17 right-to-erasure (BUG-26 fix): the deletion runs inside the execution strategy so
        // the manual transaction is compatible with EnableRetryOnFailure — it must SUCCEED.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("successfully deleted");

        // The account is anonymized atomically: deactivated, email scrubbed, password cleared.
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user.Should().NotBeNull();
            user!.IsActive.Should().BeFalse();
            user.Email.Should().NotBe(email);
            user.Email.Should().StartWith("deleted_");
        }

        // The original credentials no longer work after erasure.
        ClearAuthToken();
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// SEC-14 / ADR-0004: account deletion ANONYMIZES the user's order PII (email/phone/address) but
    /// RETAINS the order/invoice record (for the legal accounting-retention period).
    /// </summary>
    [Fact]
    public async Task DeleteAccount_AnonymizesOrderPii_ButRetainsTheOrderRecord()
    {
        var email = $"gdpr-order-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        await AuthenticateAsync(email, password);
        var userId = await GetUserIdAsync(email);

        // Seed a real order owned by this user, with PII.
        Guid orderId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = new Order($"TEST-ORD-{Guid.NewGuid():N}", email);
            order.SetUser(userId);
            order.SetCustomerPhone("+359888123456");
            order.SetShippingAddress(new Dictionary<string, object>
            {
                ["fullName"] = "Real Customer",
                ["street"] = "1 Real St",
                ["city"] = "Sofia"
            });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        var response = await SendDeleteAsync(new { password, confirmDeletion = true });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await db.Orders.FindAsync(orderId);

            order.Should().NotBeNull("the invoice record is retained for legal/tax compliance");
            order!.CustomerEmail.Should().Be("anonymized@deleted.local");
            order.CustomerPhone.Should().BeNull();
            order.ShippingAddress.Should().NotContainKey("fullName");
            order.ShippingAddress.Values.Select(v => v?.ToString())
                .Should().NotContain("Real Customer", "the customer name must be scrubbed");
            order.BillingAddress.Should().BeNull();
        }
    }

    #endregion

    private Task<HttpResponseMessage> SendDeleteAsync(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/gdpr/delete")
        {
            Content = JsonContent.Create(payload)
        };
        return Client.SendAsync(request);
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private async Task<Guid> CreateUserAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Other",
            LastName = "User"
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        result.Succeeded.Should().BeTrue();
        return user.Id;
    }
}
