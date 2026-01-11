using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Orders.Queries;

public record GetUserOrdersQuery : IRequest<PaginatedList<OrderBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    // Filtering
    public string? Status { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? SearchQuery { get; init; }

    // Sorting
    public string SortBy { get; init; } = "date";
    public string SortDirection { get; init; } = "desc";
}

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, PaginatedList<OrderBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserOrdersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<OrderBriefDto>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (!userId.HasValue)
        {
            return new PaginatedList<OrderBriefDto>([], 0, request.PageNumber, request.PageSize);
        }

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Where(o => o.UserId == userId);

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        // Apply date range filter
        if (request.DateFrom.HasValue)
        {
            var dateFrom = request.DateFrom.Value.Date;
            query = query.Where(o => o.CreatedAt >= dateFrom);
        }

        if (request.DateTo.HasValue)
        {
            var dateTo = request.DateTo.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAt < dateTo);
        }

        // Apply search filter (order number)
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var searchTerm = request.SearchQuery.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(searchTerm));
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDirection);

        // Project to DTO
        var projectedQuery = query.Select(o => new OrderBriefDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            Total = o.Total,
            ItemCount = o.Items.Sum(i => i.Quantity),
            CreatedAt = o.CreatedAt,
            Items = o.Items.Take(3).Select(i => new OrderItemBriefDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                ImageUrl = i.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                Quantity = i.Quantity
            }).ToList()
        });

        return await PaginatedList<OrderBriefDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Core.Entities.Order> ApplySorting(
        IQueryable<Core.Entities.Order> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "total" => isDescending
                ? query.OrderByDescending(o => o.Total)
                : query.OrderBy(o => o.Total),
            "ordernumber" => isDescending
                ? query.OrderByDescending(o => o.OrderNumber)
                : query.OrderBy(o => o.OrderNumber),
            _ => isDescending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };
    }
}
