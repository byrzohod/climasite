using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <c>AdminOrdersController</c> (<c>/api/admin/orders</c>).
/// Real HTTP + real Postgres (Testcontainers); the user is elevated to Admin via
/// the test-only endpoint. Covers list/detail/status-update happy paths, not-found,
/// validation, and the 401 (unauthenticated) / 403 (non-admin) auth gates.
/// </summary>
public class AdminOrdersControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _admin;

    public AdminOrdersControllerTests(TestWebApplicationFactory factory) : base(factory)
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
    public async Task GetOrders_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_Returns403_WhenAuthenticatedAsNonAdmin()
    {
        var (token, _) = await AdminTestHelpers.AuthenticateCustomerAsync(
            Factory, _admin, $"customer-{Guid.NewGuid()}@example.com");

        using var customer = Factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await customer.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrderStatus_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/admin/orders/{Guid.NewGuid()}/status",
            new { status = "Paid" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- List ---------------------------------------------------------------

    [Fact]
    public async Task GetOrders_ReturnsSeededOrder_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var order = await SeedOrderAsync("ORD-LIST-001", "buyer-list@example.com");

        var response = await _admin.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrdersListResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(i => i.Id == order.Id && i.OrderNumber == "ORD-LIST-001");
    }

    [Fact]
    public async Task GetOrders_FiltersByStatus_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        await SeedOrderAsync("ORD-PENDING-1", "pending@example.com");
        var cancelled = await SeedOrderAsync("ORD-CANCELLED-1", "cancelled@example.com");
        cancelled.SetStatus(OrderStatus.Cancelled);
        await DbContext.SaveChangesAsync();

        var response = await _admin.GetAsync("/api/admin/orders?status=Cancelled");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrdersListResponse>(JsonOptions);
        result!.Items.Should().OnlyContain(i => i.Status == "Cancelled");
        result.Items.Should().Contain(i => i.Id == cancelled.Id);
    }

    // --- Detail -------------------------------------------------------------

    [Fact]
    public async Task GetOrder_ReturnsDetail_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var order = await SeedOrderAsync("ORD-DETAIL-001", "detail@example.com");

        var response = await _admin.GetAsync($"/api/admin/orders/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(order.Id);
        detail.OrderNumber.Should().Be("ORD-DETAIL-001");
        detail.CustomerEmail.Should().Be("detail@example.com");
    }

    [Fact]
    public async Task GetOrder_Returns404_ForUnknownId()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.GetAsync($"/api/admin/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Order not found");
    }

    // --- Status update ------------------------------------------------------

    [Fact]
    public async Task UpdateOrderStatus_TransitionsOrder_WhenAdmin()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var order = await SeedOrderAsync("ORD-STATUS-001", "status@example.com");

        // Pending -> Paid is a valid transition.
        var response = await _admin.PutAsJsonAsync(
            $"/api/admin/orders/{order.Id}/status",
            new { status = "Paid", note = "Marked paid by admin", notifyCustomer = false });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the change persisted.
        var detail = await (await _admin.GetAsync($"/api/admin/orders/{order.Id}"))
            .Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        detail!.Status.Should().Be("Paid");
        detail.Notes.Should().Contain("Marked paid by admin");
    }

    [Fact]
    public async Task UpdateOrderStatus_Returns400_ForInvalidStatusValue()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");
        var order = await SeedOrderAsync("ORD-BADSTATUS-001", "badstatus@example.com");

        var response = await _admin.PutAsJsonAsync(
            $"/api/admin/orders/{order.Id}/status",
            new { status = "NotARealStatus", notifyCustomer = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateOrderStatus_Returns400_ForUnknownOrder()
    {
        await AdminTestHelpers.AuthenticateAdminAsync(Factory, _admin, $"admin-{Guid.NewGuid()}@example.com");

        var response = await _admin.PutAsJsonAsync(
            $"/api/admin/orders/{Guid.NewGuid()}/status",
            new { status = "Paid", notifyCustomer = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Order not found");
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Order> SeedOrderAsync(string orderNumber, string customerEmail)
    {
        var order = new Order(orderNumber, customerEmail);
        order.SetShippingAddress(new Dictionary<string, object>
        {
            ["line1"] = "1 Test Street",
            ["city"] = "Sofia",
            ["country"] = "BG"
        });
        order.SetShippingCost(10m);
        order.SetTaxAmount(5m);

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();
        return order;
    }

    // --- Response DTOs ------------------------------------------------------

    private sealed record OrdersListResponse(
        List<OrderListItem> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);

    private sealed record OrderListItem(Guid Id, string OrderNumber, string CustomerEmail, string Status);

    private sealed record OrderDetailResponse(
        Guid Id, string OrderNumber, string CustomerEmail, string Status, string? Notes);
}
