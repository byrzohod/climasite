namespace ClimaSite.Core.Entities;

public class OrderEvent : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }

    // Navigation property
    public virtual Order Order { get; private set; } = null!;

    private OrderEvent() { }

    public OrderEvent(Guid orderId, OrderStatus status, string? description = null, string? notes = null)
    {
        OrderId = orderId;
        Status = status;
        Description = description;
        Notes = notes;
    }

    public static OrderEvent Create(Guid orderId, OrderStatus status, string? description = null, string? notes = null)
    {
        return new OrderEvent(orderId, status, description, notes);
    }
}
