using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetRevenueChartQuery : IRequest<RevenueChartDto>
{
    public string Period { get; init; } = "7d";
}

public class GetRevenueChartQueryHandler : IRequestHandler<GetRevenueChartQuery, RevenueChartDto>
{
    private readonly IApplicationDbContext _context;

    public GetRevenueChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RevenueChartDto> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var days = request.Period switch
        {
            "30d" => 30,
            "12m" => 365,
            _ => 7
        };

        var startDate = now.Date.AddDays(-days + 1);
        var dataPoints = new List<ChartDataPointDto>();

        // Get all orders in the date range
        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync(cancellationToken);

        // Group by date
        var revenueByDate = orders
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Total));

        // Generate data points for each day
        for (var date = startDate; date <= now.Date; date = date.AddDays(1))
        {
            var revenue = revenueByDate.GetValueOrDefault(date, 0);
            dataPoints.Add(new ChartDataPointDto
            {
                Date = date,
                Value = revenue,
                Label = date.ToString("MMM dd")
            });
        }

        return new RevenueChartDto
        {
            DataPoints = dataPoints,
            Period = request.Period
        };
    }
}
