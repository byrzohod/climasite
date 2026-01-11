using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Orders.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Orders.Queries;

public record GetAdminOrdersQuery : IRequest<AdminOrdersListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortOrder { get; init; } = "desc";
}

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, AdminOrdersListDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminOrdersListDto> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .AsQueryable();

        // Search by order number or customer email
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                o.CustomerEmail.ToLower().Contains(searchLower) ||
                (o.User != null && (o.User.FirstName + " " + o.User.LastName).ToLower().Contains(searchLower)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        // Date range filter
        if (request.DateFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.DateFrom.Value);
        }
        if (request.DateTo.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.DateTo.Value);
        }

        // Sorting
        query = request.SortBy.ToLower() switch
        {
            "ordernumber" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(o => o.OrderNumber)
                : query.OrderByDescending(o => o.OrderNumber),
            "total" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(o => o.Total)
                : query.OrderByDescending(o => o.Total),
            "status" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(o => o.Status)
                : query.OrderByDescending(o => o.Status),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(o => o.CreatedAt)
                : query.OrderByDescending(o => o.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var orders = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o => new AdminOrderListItemDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.User != null
                ? $"{o.User.FirstName} {o.User.LastName}".Trim()
                : o.CustomerEmail,
            CustomerEmail = o.CustomerEmail,
            TotalAmount = o.Total,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaidAt.HasValue ? "Paid" : "Pending",
            ItemCount = o.Items.Count,
            CreatedAt = o.CreatedAt
        }).ToList();

        return new AdminOrdersListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }
}
