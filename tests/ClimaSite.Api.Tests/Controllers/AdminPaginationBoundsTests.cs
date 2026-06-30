using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// FOUND-QW-admin-pagination: the admin list endpoints (auth-gated) drive their own
/// <c>Math.Ceiling(count / pageSize)</c> + <c>Skip</c>/<c>Take</c>, so an out-of-bounds <c>pageSize</c>/
/// <c>pageNumber</c> could 500 (div-by-zero / negative-Skip overflow) on an admin's own screen. Each admin
/// list now clamps via <c>QueryBounds</c> at the controller — these assert no 5xx on garbage input
/// (the exact clamp values are pinned by <c>QueryBoundsTests</c>).
/// </summary>
public class AdminPaginationBoundsTests : IntegrationTestBase
{
    public AdminPaginationBoundsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("/api/admin/products?pageSize=0")]
    [InlineData("/api/admin/products?pageNumber=2147483647")]
    [InlineData("/api/admin/products?pageSize=100000")]
    [InlineData("/api/admin/questions/pending?pageSize=0")]
    [InlineData("/api/admin/questions?pageSize=0")]
    [InlineData("/api/admin/reviews/pending?pageSize=0")]
    [InlineData("/api/admin/reviews?pageSize=0")]
    [InlineData("/api/admin/orders?pageSize=0")]
    [InlineData("/api/admin/orders?pageNumber=2147483647")]
    [InlineData("/api/admin/customers?pageSize=0")]
    [InlineData("/api/admin/inventory?pageSize=0")]
    [InlineData("/api/admin/inventory?pageNumber=2147483647")]
    [InlineData("/api/admin/installation-requests?pageSize=0")]
    [InlineData("/api/admin/installation-requests?pageNumber=2147483647")]
    [InlineData("/api/notifications?pageSize=0")]
    [InlineData("/api/notifications?pageNumber=2147483647")]
    // Dashboard "recent/top N" widgets: count=-1 would crash Take(-1); count=100000 is a fetch-all.
    [InlineData("/api/admin/dashboard/recent-orders?count=-1")]
    [InlineData("/api/admin/dashboard/recent-orders?count=100000")]
    [InlineData("/api/admin/dashboard/low-stock?count=-1")]
    [InlineData("/api/admin/dashboard/top-products?count=100000")]
    public async Task AdminListEndpoints_WithOutOfBoundsParams_NeverServerError(string url)
    {
        using var admin = AdminTestHelpers.CreateClientWithAdminSecret(Factory);
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, admin, $"admin_{Guid.NewGuid():N}@example.com");

        var response = await admin.GetAsync(url);

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}
