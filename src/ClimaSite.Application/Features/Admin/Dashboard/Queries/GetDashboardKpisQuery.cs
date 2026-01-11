using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Dashboard.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Dashboard.Queries;

public record GetDashboardKpisQuery : IRequest<DashboardKpiDto>;

public class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardKpiDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardKpisQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardKpiDto> Handle(GetDashboardKpisQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevWeekStart = weekStart.AddDays(-7);
        var prevMonthStart = monthStart.AddMonths(-1);

        // Orders
        var ordersToday = await _context.Orders
            .CountAsync(o => o.CreatedAt >= today, cancellationToken);
        var ordersThisWeek = await _context.Orders
            .CountAsync(o => o.CreatedAt >= weekStart, cancellationToken);
        var ordersThisMonth = await _context.Orders
            .CountAsync(o => o.CreatedAt >= monthStart, cancellationToken);
        var ordersPrevWeek = await _context.Orders
            .CountAsync(o => o.CreatedAt >= prevWeekStart && o.CreatedAt < weekStart, cancellationToken);

        // Revenue (exclude cancelled orders)
        var revenueToday = await _context.Orders
            .Where(o => o.CreatedAt >= today && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total ?? 0, cancellationToken);
        var revenueThisWeek = await _context.Orders
            .Where(o => o.CreatedAt >= weekStart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total ?? 0, cancellationToken);
        var revenueThisMonth = await _context.Orders
            .Where(o => o.CreatedAt >= monthStart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total ?? 0, cancellationToken);
        var revenuePrevWeek = await _context.Orders
            .Where(o => o.CreatedAt >= prevWeekStart && o.CreatedAt < weekStart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total ?? 0, cancellationToken);

        // New Customers
        var customersToday = await _context.Users
            .CountAsync(u => u.CreatedAt >= today, cancellationToken);
        var customersThisWeek = await _context.Users
            .CountAsync(u => u.CreatedAt >= weekStart, cancellationToken);
        var customersThisMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= monthStart, cancellationToken);
        var customersPrevWeek = await _context.Users
            .CountAsync(u => u.CreatedAt >= prevWeekStart && u.CreatedAt < weekStart, cancellationToken);

        // Average Order Value
        var avgOrderValueThisWeek = ordersThisWeek > 0 ? revenueThisWeek / ordersThisWeek : 0;
        var avgOrderValueThisMonth = ordersThisMonth > 0 ? revenueThisMonth / ordersThisMonth : 0;

        // Pending Orders
        var pendingOrders = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing, cancellationToken);

        // Low Stock Count
        var lowStockCount = await _context.ProductVariants
            .CountAsync(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold, cancellationToken);

        return new DashboardKpiDto
        {
            TotalOrders = new KpiMetricDto
            {
                Today = ordersToday,
                ThisWeek = ordersThisWeek,
                ThisMonth = ordersThisMonth,
                TrendPercentage = CalculateTrend(ordersThisWeek, ordersPrevWeek)
            },
            Revenue = new KpiMetricDto
            {
                Today = revenueToday,
                ThisWeek = revenueThisWeek,
                ThisMonth = revenueThisMonth,
                TrendPercentage = CalculateTrend(revenueThisWeek, revenuePrevWeek)
            },
            NewCustomers = new KpiMetricDto
            {
                Today = customersToday,
                ThisWeek = customersThisWeek,
                ThisMonth = customersThisMonth,
                TrendPercentage = CalculateTrend(customersThisWeek, customersPrevWeek)
            },
            AverageOrderValue = new KpiMetricDto
            {
                Today = 0, // Not meaningful for single day
                ThisWeek = avgOrderValueThisWeek,
                ThisMonth = avgOrderValueThisMonth,
                TrendPercentage = 0
            },
            PendingOrders = pendingOrders,
            LowStockCount = lowStockCount
        };
    }

    private static decimal CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
