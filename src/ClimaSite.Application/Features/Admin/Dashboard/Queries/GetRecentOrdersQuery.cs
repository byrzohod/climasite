using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetRecentOrdersQuery : IRequest<List<RecentOrderDto>>
{
    public int Count { get; init; } = 10;
}

public class GetRecentOrdersQueryHandler : IRequestHandler<GetRecentOrdersQuery, List<RecentOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRecentOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecentOrderDto>> Handle(GetRecentOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(request.Count)
            .Select(o => new RecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.User != null ? o.User.FirstName + " " + o.User.LastName : o.CustomerEmail ?? "Guest",
                TotalAmount = o.Total,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return orders;
    }
}
