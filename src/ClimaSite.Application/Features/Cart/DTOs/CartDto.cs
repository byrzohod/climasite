namespace ClimaSite.Application.Features.Cart.DTOs;

public record CartDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? GuestSessionId { get; init; }
    // Alias for frontend compatibility
    public string? SessionId => GuestSessionId;
    public List<CartItemDto> Items { get; init; } = new();
    public decimal Subtotal { get; init; }
    public decimal Shipping { get; init; } = 0;
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal EffectivePrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
    // Alias for frontend compatibility
    public decimal Subtotal => LineTotal;
    public int AvailableStock { get; init; }
    // Alias for frontend compatibility
    public int MaxQuantity => AvailableStock;
    public bool IsAvailable { get; init; }
}
