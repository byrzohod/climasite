using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Customers.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Customers.Queries;

public record GetAdminCustomersQuery : IRequest<AdminCustomersListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public DateTime? RegisteredFrom { get; init; }
    public DateTime? RegisteredTo { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortOrder { get; init; } = "desc";
}

public class GetAdminCustomersQueryHandler : IRequestHandler<GetAdminCustomersQuery, AdminCustomersListDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminCustomersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminCustomersListDto> Handle(GetAdminCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Search by name, email, or phone
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(searchLower) ||
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchLower)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = request.Status.ToLower() switch
            {
                "active" => query.Where(u => u.IsActive),
                "inactive" => query.Where(u => !u.IsActive),
                "verified" => query.Where(u => u.EmailConfirmed),
                "unverified" => query.Where(u => !u.EmailConfirmed),
                _ => query
            };
        }

        // Registration date filter
        if (request.RegisteredFrom.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= request.RegisteredFrom.Value);
        }
        if (request.RegisteredTo.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= request.RegisteredTo.Value);
        }

        // Sorting
        query = request.SortBy.ToLower() switch
        {
            "email" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "name" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                : query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            "lastlogin" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(u => u.LastLoginAt)
                : query.OrderByDescending(u => u.LastLoginAt),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get order stats for users
        var userIds = users.Select(u => u.Id).ToList();
        var orderStats = await _context.Orders
            .Where(o => o.UserId.HasValue && userIds.Contains(o.UserId.Value))
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.Total)
            })
            .ToDictionaryAsync(x => x.UserId!.Value, x => (x.OrderCount, x.TotalSpent), cancellationToken);

        var items = users.Select(u => new AdminCustomerListItemDto
        {
            Id = u.Id,
            Email = u.Email!,
            FullName = u.FullName,
            Phone = u.PhoneNumber,
            IsActive = u.IsActive,
            EmailConfirmed = u.EmailConfirmed,
            OrderCount = orderStats.TryGetValue(u.Id, out var stats) ? stats.OrderCount : 0,
            TotalSpent = orderStats.TryGetValue(u.Id, out stats) ? stats.TotalSpent : 0,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        }).ToList();

        return new AdminCustomersListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }
}
