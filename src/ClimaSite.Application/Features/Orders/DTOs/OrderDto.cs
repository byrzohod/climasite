namespace ClimaSite.Application.Features.Orders.DTOs;

public class OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Total { get; init; }
    public decimal TotalAmount => Total; // Alias for API compatibility
    public string Currency { get; init; } = "EUR";
    public AddressDto? ShippingAddress { get; init; }
    public AddressDto? BillingAddress { get; init; }
    public string? ShippingMethod { get; init; }
    public string? TrackingNumber { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? Notes { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
    public List<OrderEventDto> Events { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public class AddressDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? State { get; init; }
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public class OrderBriefDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemBriefDto> Items { get; init; } = [];
}

public class OrderItemBriefDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
}

public class OrderEventDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
