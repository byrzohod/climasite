namespace ClimaSite.Core.Entities;

/// <summary>Lifecycle of a <see cref="StockReservation"/>. Only <c>Active</c> holds count toward
/// <c>product_variants.reserved_quantity</c> (the clock-independent invariant). Stored as a string.</summary>
public enum ReservationStatus
{
    /// <summary>Holds a unit against a cart; counts toward reserved_quantity regardless of expiry.</summary>
    Active,

    /// <summary>Converted to a sale at order-create (hold → sold).</summary>
    Consumed,

    /// <summary>Explicitly released (cart clear/remove/merge, or quantity dropped to 0).</summary>
    Released,

    /// <summary>Swept after its lease elapsed (the sweeper is the sole releaser of expired holds).</summary>
    Expired
}

/// <summary>Which checkout flow owns the hold. Wave A2 only mints <c>Card</c>; Wave B adds bank transfer.</summary>
public enum ReservationKind
{
    Card,
    BankTransfer
}

/// <summary>
/// A hold on a variant's stock taken at checkout-start (INV-01 Wave A2). The denormalized
/// <c>product_variants.reserved_quantity</c> counter equals the sum of a variant's <c>Active</c> holds; the
/// ledger row is the source of truth. Mutations run through the atomic SQL primitives (P1/P2/P3) under a
/// per-variant <c>FOR UPDATE</c> lock, so the domain mutators here exist for construction and for the
/// in-memory test double — never trust a pre-lock snapshot of Status/Quantity/ExpiresAt.
/// </summary>
public class StockReservation : BaseEntity
{
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;
    public DateTime ExpiresAt { get; private set; }
    public Guid? CartId { get; private set; }
    public string? PaymentIntentId { get; private set; }
    public Guid? OrderId { get; private set; }
    public ReservationKind Kind { get; private set; } = ReservationKind.Card;

    private StockReservation() { }

    public StockReservation(Guid variantId, Guid? cartId, int quantity, DateTime expiresAt, ReservationKind kind)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Reservation quantity must be positive.");
        }

        VariantId = variantId;
        CartId = cartId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
        Kind = kind;
        Status = ReservationStatus.Active;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Reservation quantity must be positive.");
        }

        Quantity = quantity;
        SetUpdatedAt();
    }

    public void SetStatus(ReservationStatus status)
    {
        Status = status;
        SetUpdatedAt();
    }

    public void SetExpiresAt(DateTime expiresAt)
    {
        ExpiresAt = expiresAt;
        SetUpdatedAt();
    }

    public void SetPaymentIntentId(string? paymentIntentId)
    {
        PaymentIntentId = paymentIntentId;
        SetUpdatedAt();
    }

    public void SetOrderId(Guid? orderId)
    {
        OrderId = orderId;
        SetUpdatedAt();
    }
}
