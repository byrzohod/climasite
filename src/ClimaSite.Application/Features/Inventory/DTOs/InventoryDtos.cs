namespace ClimaSite.Application.Features.Inventory.DTOs;

public record InventoryItemDto
{
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int LowStockThreshold { get; init; }
    public string StockStatus { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record InventoryListDto
{
    public List<InventoryItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
}

public record StockAdjustmentDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int QuantityBefore { get; init; }
    public int QuantityChange { get; init; }
    public int QuantityAfter { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record StockAdjustmentHistoryDto
{
    public List<StockAdjustmentDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

public enum StockAdjustmentReason
{
    Received,
    Damaged,
    Lost,
    Returned,
    Correction,
    Transfer,
    Sale,
    Initial
}
