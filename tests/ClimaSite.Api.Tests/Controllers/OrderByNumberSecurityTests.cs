using System.Net;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// SEC-02 regression: GET /api/orders/by-number/{n} must not leak customer PII.
/// Anonymous callers get 401; a non-owner gets 404 (existence not revealed); only the
/// owner (or an admin) can read it.
/// </summary>
public class OrderByNumberSecurityTests : IntegrationTestBase
{
    public OrderByNumberSecurityTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ByNumber_AnonymousCaller_Is401_AndLeaksNoPii()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.com";
        await AuthenticateAsync(ownerEmail);
        var orderNumber = await SeedOrderForAsync(ownerEmail);

        ClearAuthToken();
        var response = await Client.GetAsync($"/api/orders/by-number/{orderNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("owner@example.com");
        body.Should().NotContain("+359888000000");
    }

    [Fact]
    public async Task ByNumber_Owner_CanRead()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.com";
        await AuthenticateAsync(ownerEmail);
        var orderNumber = await SeedOrderForAsync(ownerEmail);

        // Still authenticated as the owner.
        var response = await Client.GetAsync($"/api/orders/by-number/{orderNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ByNumber_DifferentUser_Is404_AndLeaksNoPii()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.com";
        await AuthenticateAsync(ownerEmail);
        var orderNumber = await SeedOrderForAsync(ownerEmail);

        // Log in as a different user.
        await AuthenticateAsync($"other-{Guid.NewGuid():N}@test.com");

        var response = await Client.GetAsync($"/api/orders/by-number/{orderNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("owner@example.com");
        body.Should().NotContain("+359888000000");
    }

    private async Task<string> SeedOrderForAsync(string ownerEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var owner = await userManager.FindByEmailAsync(ownerEmail);
        owner.Should().NotBeNull();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderNumber = $"SEC02-{Guid.NewGuid():N}"[..16];
        var order = new Order(orderNumber, "owner@example.com");
        order.SetUser(owner!.Id);
        order.SetCustomerPhone("+359888000000");
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return orderNumber;
    }
}
