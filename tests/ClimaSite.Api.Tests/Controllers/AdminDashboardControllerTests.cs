using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <c>AdminDashboardController</c> (<c>/api/admin/dashboard</c>).
/// Covers the KPI, revenue chart, order-status chart, and recent-orders endpoints
/// (happy path + reflects seeded data) plus the 401/403 auth gates. Real HTTP +
/// real Postgres.
/// </summary>
public class AdminDashboardControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _admin;

    public AdminDashboardControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
        _admin = AdminTestHelpers.CreateClientWithAdminSecret(factory);
    }

    public override async Task DisposeAsync()
    {
        _admin.Dispose();
        await base.DisposeAsync();
    }

    // --- Auth gates ---------------------------------------------------------

    [Fact]
    public async Task GetDashboardKpis_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardKpis_Returns403_WhenAuthenticatedAsNonAdmin()
    {
        var (token, _) = await AdminTestHelpers.AuthenticateCustomerAsync(
            Factory, _admin, $"plain-{Guid.NewGuid()}@example.com");

        using var customer = Factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await customer.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRevenueChart_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/dashboard/revenue");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- KPIs ---------------------------------------------------------------

    [Fact]
    public async Task GetDashboardKpis_ReturnsMetrics_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var kpis = await response.Content.ReadFromJsonAsync<DashboardKpiResponse>(JsonOptions);
        kpis.Should().NotBeNull();
        kpis!.TotalOrders.Should().NotBeNull();
        kpis.Revenue.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardKpis_CountsPendingOrders_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        await SeedOrderAsync("DASH-PENDING-1", OrderStatus.Pending);
        await SeedOrderAsync("DASH-PENDING-2", OrderStatus.Pending);

        var response = await _admin.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var kpis = await response.Content.ReadFromJsonAsync<DashboardKpiResponse>(JsonOptions);
        kpis!.PendingOrders.Should().BeGreaterThanOrEqualTo(2);
    }

    // --- Revenue chart ------------------------------------------------------

    [Fact]
    public async Task GetRevenueChart_ReturnsDataPoints_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync("/api/admin/dashboard/revenue?period=7d");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chart = await response.Content.ReadFromJsonAsync<RevenueChartResponse>(JsonOptions);
        chart.Should().NotBeNull();
        chart!.Period.Should().Be("7d");
        chart.DataPoints.Should().NotBeNull();
        // A 7-day window should yield 7 daily buckets.
        chart.DataPoints.Should().HaveCount(7);
    }

    // --- Order status chart -------------------------------------------------

    [Fact]
    public async Task GetOrderStatusChart_ReflectsSeededStatuses_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        await SeedOrderAsync("DASH-CHART-PENDING", OrderStatus.Pending);
        await SeedOrderAsync("DASH-CHART-CANCELLED", OrderStatus.Cancelled);

        var response = await _admin.GetAsync("/api/admin/dashboard/orders-chart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chart = await response.Content.ReadFromJsonAsync<OrderStatusChartResponse>(JsonOptions);
        chart.Should().NotBeNull();
        chart!.Pending.Should().BeGreaterThanOrEqualTo(1);
        chart.Cancelled.Should().BeGreaterThanOrEqualTo(1);
    }

    // --- Recent orders ------------------------------------------------------

    [Fact]
    public async Task GetRecentOrders_ReturnsSeededOrder_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var order = await SeedOrderAsync("DASH-RECENT-1", OrderStatus.Pending);

        var response = await _admin.GetAsync("/api/admin/dashboard/recent-orders?count=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recent = await response.Content.ReadFromJsonAsync<List<RecentOrderItem>>(JsonOptions);
        recent.Should().NotBeNull();
        recent!.Should().Contain(o => o.Id == order.Id && o.OrderNumber == "DASH-RECENT-1");
    }

    [Fact]
    public async Task GetTopProducts_ReturnsOk_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync("/api/admin/dashboard/top-products?count=5&period=30d");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLowStock_ReturnsOk_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync("/api/admin/dashboard/low-stock?count=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Order> SeedOrderAsync(string orderNumber, OrderStatus status)
    {
        // Seed the order header only (no OrderItems) so we don't need real product/variant rows
        // for the FK on order_items — the dashboard reads order-level totals/status, which is enough.
        var order = new Order(orderNumber, $"{orderNumber.ToLowerInvariant()}@example.com");
        order.SetShippingAddress(new Dictionary<string, object> { ["city"] = "Sofia" });
        order.SetShippingCost(100m);

        // Walk valid transitions to reach the requested status (validated by the entity).
        if (status == OrderStatus.Cancelled)
        {
            order.SetStatus(OrderStatus.Cancelled);
        }
        else if (status != OrderStatus.Pending)
        {
            order.SetStatus(status);
        }

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();
        return order;
    }

    // --- Response DTOs ------------------------------------------------------

    private sealed record DashboardKpiResponse(
        KpiMetric TotalOrders, KpiMetric Revenue, int PendingOrders, int LowStockCount);

    private sealed record KpiMetric(decimal Today, decimal ThisWeek, decimal ThisMonth, decimal TrendPercentage);

    private sealed record RevenueChartResponse(List<ChartPoint> DataPoints, string Period);

    private sealed record ChartPoint(DateTime Date, decimal Value, string Label);

    private sealed record OrderStatusChartResponse(
        int Pending, int Processing, int Shipped, int Delivered, int Cancelled);

    private sealed record RecentOrderItem(Guid Id, string OrderNumber, decimal TotalAmount, string Status);
}
