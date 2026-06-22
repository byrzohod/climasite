using ClimaSite.Application.Features.Admin.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class GetAdminOrdersQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminOrdersQueryHandler CreateHandler() => new(_context);

    private Order SeedOrder(string number, string email, OrderStatus? paidTo = null)
    {
        var order = new Order(number, email);
        if (paidTo == OrderStatus.Paid)
        {
            order.SetStatus(OrderStatus.Paid);
        }
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllOrders()
    {
        SeedOrder("ORD-A-0001", "a@test.com");
        SeedOrder("ORD-A-0002", "b@test.com");

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SearchByOrderNumber_FiltersResults()
    {
        SeedOrder("ORD-MATCH-9999", "a@test.com");
        SeedOrder("ORD-OTHER-1111", "b@test.com");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Search = "MATCH" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.OrderNumber == "ORD-MATCH-9999");
    }

    [Fact]
    public async Task Handle_SearchByCustomerEmail_FiltersResults()
    {
        SeedOrder("ORD-S-0001", "findme@test.com");
        SeedOrder("ORD-S-0002", "other@test.com");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Search = "findme" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.CustomerEmail == "findme@test.com");
    }

    [Fact]
    public async Task Handle_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        SeedOrder("ORD-ST-0001", "a@test.com", paidTo: OrderStatus.Paid);
        SeedOrder("ORD-ST-0002", "b@test.com"); // remains Pending

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { Status = nameof(OrderStatus.Paid) }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Status == nameof(OrderStatus.Paid));
    }

    [Fact]
    public async Task Handle_PaidOrder_ReportsPaidPaymentStatus()
    {
        SeedOrder("ORD-PS-0001", "a@test.com", paidTo: OrderStatus.Paid);

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.PaymentStatus == "Paid");
    }

    [Fact]
    public async Task Handle_PendingOrder_ReportsPendingPaymentStatus()
    {
        SeedOrder("ORD-PS-0002", "a@test.com");

        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.PaymentStatus == "Pending");
    }

    [Fact]
    public async Task Handle_Pagination_LimitsPageSizeAndComputesTotalPages()
    {
        for (var i = 0; i < 5; i++)
        {
            SeedOrder($"ORD-PG-000{i}", $"p{i}@test.com");
        }

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { PageNumber = 1, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_SortByOrderNumberAscending_OrdersResults()
    {
        SeedOrder("ORD-Z-0001", "z@test.com");
        SeedOrder("ORD-A-0001", "a@test.com");

        var result = await CreateHandler().Handle(
            new GetAdminOrdersQuery { SortBy = "orderNumber", SortOrder = "asc" },
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].OrderNumber.Should().Be("ORD-A-0001");
        result.Items[1].OrderNumber.Should().Be("ORD-Z-0001");
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetAdminOrdersQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
    }
}
