using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetOrderStatusChartQuery : IRequest<OrderStatusChartDto>;

public class GetOrderStatusChartQueryHandler : IRequestHandler<GetOrderStatusChartQuery, OrderStatusChartDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderStatusChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderStatusChartDto> Handle(GetOrderStatusChartQuery request, CancellationToken cancellationToken)
    {
        var statusCounts = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new OrderStatusChartDto
        {
            Pending = statusCounts.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0,
            Processing = statusCounts.FirstOrDefault(x => x.Status == OrderStatus.Processing)?.Count ?? 0,
            Shipped = statusCounts.FirstOrDefault(x => x.Status == OrderStatus.Shipped)?.Count ?? 0,
            Delivered = statusCounts.FirstOrDefault(x => x.Status == OrderStatus.Delivered)?.Count ?? 0,
            Cancelled = statusCounts.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0
        };
    }
}
