using System.Text.Json;

namespace ClimaSite.Core.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal Subtotal { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "USD";
    public Dictionary<string, object> ShippingAddress { get; private set; } = new();
    public Dictionary<string, object>? BillingAddress { get; private set; }
    public string? ShippingMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? PaymentIntentId { get; private set; }
    public string? PaymentMethod { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; private set; }
    public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public virtual ICollection<OrderEvent> Events { get; private set; } = new List<OrderEvent>();

    private Order() { }

    public Order(string orderNumber, string customerEmail)
    {
        SetOrderNumber(orderNumber);
        SetCustomerEmail(customerEmail);
    }

    public void SetOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number cannot be empty", nameof(orderNumber));

        OrderNumber = orderNumber;
        SetUpdatedAt();
    }

    public void SetUser(Guid? userId)
    {
        UserId = userId;
        SetUpdatedAt();
    }

    public void SetCustomerEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Customer email cannot be empty", nameof(email));

        CustomerEmail = email.Trim().ToLowerInvariant();
        SetUpdatedAt();
    }

    public void SetCustomerPhone(string? phone)
    {
        CustomerPhone = phone?.Trim();
        SetUpdatedAt();
    }

    public void SetStatus(OrderStatus status, string? description = null, string? notes = null)
    {
        ValidateStatusTransition(status);
        Status = status;

        switch (status)
        {
            case OrderStatus.Paid:
                PaidAt = DateTime.UtcNow;
                break;
            case OrderStatus.Shipped:
                ShippedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Delivered:
                DeliveredAt = DateTime.UtcNow;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = DateTime.UtcNow;
                break;
        }

        // Add status change event
        AddEvent(status, description, notes);
        SetUpdatedAt();
    }

    public void AddEvent(OrderStatus status, string? description = null, string? notes = null)
    {
        var orderEvent = OrderEvent.Create(Id, status, description, notes);
        Events.Add(orderEvent);
    }

    private void ValidateStatusTransition(OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
            [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Refunded, OrderStatus.Cancelled],
            [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Refunded, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Returned],
            [OrderStatus.Delivered] = [OrderStatus.Returned],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Refunded] = [],
            [OrderStatus.Returned] = [OrderStatus.Refunded]
        };

        if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition order from {Status} to {newStatus}");
        }
    }

    public void CalculateTotals()
    {
        Subtotal = Items.Sum(i => i.LineTotal);
        Total = Subtotal + ShippingCost + TaxAmount - DiscountAmount;
        SetUpdatedAt();
    }

    public void SetShippingCost(decimal shippingCost)
    {
        if (shippingCost < 0)
            throw new ArgumentException("Shipping cost cannot be negative", nameof(shippingCost));

        ShippingCost = shippingCost;
        CalculateTotals();
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(taxAmount));

        TaxAmount = taxAmount;
        CalculateTotals();
    }

    public void SetDiscountAmount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new ArgumentException("Discount amount cannot be negative", nameof(discountAmount));

        DiscountAmount = discountAmount;
        CalculateTotals();
    }

    public void SetShippingAddress(Dictionary<string, object> address)
    {
        ShippingAddress = address ?? throw new ArgumentNullException(nameof(address));
        SetUpdatedAt();
    }

    public void SetBillingAddress(Dictionary<string, object>? address)
    {
        BillingAddress = address;
        SetUpdatedAt();
    }

    public void SetShippingMethod(string? method)
    {
        ShippingMethod = method;
        SetUpdatedAt();
    }

    public void SetTrackingNumber(string? trackingNumber)
    {
        TrackingNumber = trackingNumber;
        SetUpdatedAt();
    }

    public void SetPaymentInfo(string paymentIntentId, string paymentMethod)
    {
        PaymentIntentId = paymentIntentId;
        PaymentMethod = paymentMethod;
        SetUpdatedAt();
    }

    public void SetCancellationReason(string? reason)
    {
        CancellationReason = reason;
        SetUpdatedAt();
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        SetUpdatedAt();
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Currency = currency.ToUpperInvariant();
        SetUpdatedAt();
    }

    public OrderItem AddItem(Guid productId, Guid variantId, string productName, string variantName, string sku, int quantity, decimal unitPrice)
    {
        var item = new OrderItem(Id, productId, variantId, productName, variantName, sku, quantity, unitPrice);
        Items.Add(item);
        CalculateTotals();
        return item;
    }

    public bool CanBeCancelled => Status is OrderStatus.Pending or OrderStatus.Paid;

    public bool CanBeRefunded => Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered;
}

public enum OrderStatus
{
    Pending,
    Paid,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded,
    Returned
}
