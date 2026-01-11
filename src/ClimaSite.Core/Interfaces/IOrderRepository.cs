using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetByStatusAsync(OrderStatus status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken = default);
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
    Task<OrderStatistics> GetStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

public record OrderSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? OrderNumber = null,
    string? CustomerEmail = null,
    Guid? UserId = null,
    OrderStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    decimal? MinTotal = null,
    decimal? MaxTotal = null,
    OrderSortBy SortBy = OrderSortBy.Newest
);

public enum OrderSortBy
{
    Newest,
    Oldest,
    TotalAsc,
    TotalDesc
}

public record OrderStatistics(
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    Dictionary<OrderStatus, int> OrdersByStatus,
    int NewCustomers,
    int ReturningCustomers
);
