namespace ClimaSite.Application.Features.Admin.Dashboard.DTOs;

public record DashboardKpiDto
{
    public KpiMetricDto TotalOrders { get; init; } = new();
    public KpiMetricDto Revenue { get; init; } = new();
    public KpiMetricDto NewCustomers { get; init; } = new();
    public KpiMetricDto AverageOrderValue { get; init; } = new();
    public int PendingOrders { get; init; }
    public int LowStockCount { get; init; }
}

public record KpiMetricDto
{
    public decimal Today { get; init; }
    public decimal ThisWeek { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal TrendPercentage { get; init; }
}

public record RevenueChartDto
{
    public List<ChartDataPointDto> DataPoints { get; init; } = [];
    public string Period { get; init; } = "7d";
}

public record ChartDataPointDto
{
    public DateTime Date { get; init; }
    public decimal Value { get; init; }
    public string Label { get; init; } = string.Empty;
}

public record OrderStatusChartDto
{
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Shipped { get; init; }
    public int Delivered { get; init; }
    public int Cancelled { get; init; }
}

public record RecentOrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record LowStockProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int Threshold { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}

public record TopSellingProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}

public record AdminActivityDto
{
    public Guid Id { get; init; }
    public string AdminName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public DateTime Timestamp { get; init; }
}
