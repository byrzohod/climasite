# Inventory Management Plan

## 1. Overview

The Inventory Management system provides comprehensive stock tracking for ClimaSite's HVAC e-commerce platform. It ensures accurate real-time stock visibility, prevents overselling through stock reservations during checkout, and provides alerts for low stock conditions.

### Key Capabilities

- **Variant-Level Stock Tracking**: Track inventory at the product variant level (e.g., different BTU ratings, colors, voltages)
- **Transactional Stock Updates**: All stock modifications use database transactions to ensure data integrity
- **Stock Reservations**: Temporarily reserve stock during checkout to prevent overselling
- **Low Stock Alerts**: Automated notifications when stock falls below configurable thresholds
- **Audit Trail**: Complete history of all inventory movements for accountability and analysis
- **Real-Time Updates**: WebSocket-based updates for admin dashboards

### Business Context

For HVAC products, accurate inventory is critical because:
- Products are often high-value items with significant lead times for restocking
- Seasonal demand fluctuations (AC units in summer, heaters in winter)
- Variants matter significantly (wrong BTU rating = unusable product)
- Installation scheduling depends on product availability

---

## 2. Database Schema

### 2.1 Core Tables

```sql
-- ============================================
-- Inventory Table
-- Tracks current stock levels per product/variant
-- ============================================
CREATE TABLE inventory (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES product_variants(id) ON DELETE CASCADE,
    sku VARCHAR(100) NOT NULL,
    quantity INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    reserved_quantity INT NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    low_stock_threshold INT NOT NULL DEFAULT 10 CHECK (low_stock_threshold >= 0),
    reorder_quantity INT DEFAULT 50,
    warehouse_location VARCHAR(100),
    is_tracking_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1, -- Optimistic concurrency
    UNIQUE(product_id, variant_id),
    UNIQUE(sku),
    CONSTRAINT chk_reserved_not_exceed_quantity CHECK (reserved_quantity <= quantity)
);

-- Indexes for common queries
CREATE INDEX idx_inventory_product_id ON inventory(product_id);
CREATE INDEX idx_inventory_variant_id ON inventory(variant_id);
CREATE INDEX idx_inventory_sku ON inventory(sku);
CREATE INDEX idx_inventory_low_stock ON inventory((quantity - reserved_quantity))
    WHERE (quantity - reserved_quantity) <= low_stock_threshold;

-- ============================================
-- Inventory Movements Table
-- Audit trail for all stock changes
-- ============================================
CREATE TABLE inventory_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id UUID NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    movement_type VARCHAR(50) NOT NULL,
    quantity_change INT NOT NULL, -- Positive for additions, negative for deductions
    quantity_before INT NOT NULL,
    quantity_after INT NOT NULL,
    reference_type VARCHAR(50), -- 'order', 'return', 'adjustment', 'transfer'
    reference_id UUID, -- Links to orders.id, returns.id, etc.
    notes TEXT,
    performed_by UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Movement types enum (enforced at application level)
COMMENT ON COLUMN inventory_movements.movement_type IS
    'Values: order_placed, order_cancelled, adjustment_add, adjustment_subtract,
     return_received, restock, transfer_in, transfer_out, reservation_expired, initial_stock';

CREATE INDEX idx_inventory_movements_inventory_id ON inventory_movements(inventory_id);
CREATE INDEX idx_inventory_movements_reference ON inventory_movements(reference_type, reference_id);
CREATE INDEX idx_inventory_movements_created_at ON inventory_movements(created_at DESC);
CREATE INDEX idx_inventory_movements_type ON inventory_movements(movement_type);

-- ============================================
-- Stock Reservations Table
-- Temporary holds during checkout process
-- ============================================
CREATE TABLE stock_reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id UUID NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    session_id VARCHAR(100), -- For anonymous users
    user_id UUID REFERENCES users(id), -- For authenticated users
    cart_id UUID REFERENCES shopping_carts(id),
    order_id UUID REFERENCES orders(id),
    quantity INT NOT NULL CHECK (quantity > 0),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    released_at TIMESTAMP WITH TIME ZONE,
    converted_at TIMESTAMP WITH TIME ZONE, -- When converted to actual order
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Reservation statuses: 'active', 'released', 'converted', 'expired'
CREATE INDEX idx_stock_reservations_inventory_id ON stock_reservations(inventory_id);
CREATE INDEX idx_stock_reservations_status ON stock_reservations(status) WHERE status = 'active';
CREATE INDEX idx_stock_reservations_expires_at ON stock_reservations(expires_at) WHERE status = 'active';
CREATE INDEX idx_stock_reservations_session ON stock_reservations(session_id) WHERE status = 'active';
CREATE INDEX idx_stock_reservations_user ON stock_reservations(user_id) WHERE status = 'active';

-- ============================================
-- Low Stock Alerts Table
-- Track alert history and acknowledgments
-- ============================================
CREATE TABLE low_stock_alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id UUID NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    alert_type VARCHAR(30) NOT NULL, -- 'low_stock', 'out_of_stock', 'back_in_stock'
    available_quantity INT NOT NULL,
    threshold INT NOT NULL,
    is_acknowledged BOOLEAN NOT NULL DEFAULT FALSE,
    acknowledged_by UUID REFERENCES users(id),
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    notification_sent BOOLEAN NOT NULL DEFAULT FALSE,
    notification_sent_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_low_stock_alerts_unacknowledged ON low_stock_alerts(is_acknowledged, created_at DESC)
    WHERE is_acknowledged = FALSE;
CREATE INDEX idx_low_stock_alerts_inventory ON low_stock_alerts(inventory_id, created_at DESC);

-- ============================================
-- Inventory Audit Log Table
-- Detailed audit for compliance
-- ============================================
CREATE TABLE inventory_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id UUID NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    action VARCHAR(50) NOT NULL,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    performed_by UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_inventory_audit_inventory_id ON inventory_audit_log(inventory_id);
CREATE INDEX idx_inventory_audit_created_at ON inventory_audit_log(created_at DESC);
```

### 2.2 Views for Reporting

```sql
-- Available stock view (quantity - reserved)
CREATE VIEW v_available_stock AS
SELECT
    i.id AS inventory_id,
    i.product_id,
    i.variant_id,
    i.sku,
    p.name AS product_name,
    pv.name AS variant_name,
    i.quantity AS total_quantity,
    i.reserved_quantity,
    (i.quantity - i.reserved_quantity) AS available_quantity,
    i.low_stock_threshold,
    CASE
        WHEN (i.quantity - i.reserved_quantity) = 0 THEN 'out_of_stock'
        WHEN (i.quantity - i.reserved_quantity) <= i.low_stock_threshold THEN 'low_stock'
        ELSE 'in_stock'
    END AS stock_status,
    i.updated_at
FROM inventory i
JOIN products p ON i.product_id = p.id
LEFT JOIN product_variants pv ON i.variant_id = pv.id
WHERE i.is_tracking_enabled = TRUE;

-- Low stock items view
CREATE VIEW v_low_stock_items AS
SELECT * FROM v_available_stock
WHERE stock_status IN ('low_stock', 'out_of_stock');

-- Inventory movement summary by day
CREATE VIEW v_inventory_movement_summary AS
SELECT
    DATE_TRUNC('day', im.created_at) AS date,
    im.inventory_id,
    i.sku,
    im.movement_type,
    COUNT(*) AS movement_count,
    SUM(im.quantity_change) AS total_quantity_change
FROM inventory_movements im
JOIN inventory i ON im.inventory_id = i.id
GROUP BY DATE_TRUNC('day', im.created_at), im.inventory_id, i.sku, im.movement_type;
```

### 2.3 Stored Procedures for Transactional Operations

```sql
-- ============================================
-- Reserve Stock Procedure
-- Atomically reserves stock for checkout
-- ============================================
CREATE OR REPLACE FUNCTION reserve_stock(
    p_inventory_id UUID,
    p_quantity INT,
    p_session_id VARCHAR(100),
    p_user_id UUID,
    p_cart_id UUID,
    p_expiry_minutes INT DEFAULT 15
) RETURNS UUID AS $$
DECLARE
    v_available INT;
    v_reservation_id UUID;
    v_current_version INT;
BEGIN
    -- Lock the inventory row
    SELECT quantity - reserved_quantity, version
    INTO v_available, v_current_version
    FROM inventory
    WHERE id = p_inventory_id
    FOR UPDATE;

    IF v_available IS NULL THEN
        RAISE EXCEPTION 'Inventory item not found: %', p_inventory_id;
    END IF;

    IF v_available < p_quantity THEN
        RAISE EXCEPTION 'Insufficient stock. Available: %, Requested: %', v_available, p_quantity;
    END IF;

    -- Create reservation
    INSERT INTO stock_reservations (
        inventory_id, session_id, user_id, cart_id, quantity,
        status, expires_at
    ) VALUES (
        p_inventory_id, p_session_id, p_user_id, p_cart_id, p_quantity,
        'active', NOW() + (p_expiry_minutes || ' minutes')::INTERVAL
    ) RETURNING id INTO v_reservation_id;

    -- Update reserved quantity
    UPDATE inventory
    SET reserved_quantity = reserved_quantity + p_quantity,
        version = version + 1,
        updated_at = NOW()
    WHERE id = p_inventory_id AND version = v_current_version;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Concurrent modification detected. Please retry.';
    END IF;

    RETURN v_reservation_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Release Reservation Procedure
-- Returns reserved stock to available pool
-- ============================================
CREATE OR REPLACE FUNCTION release_reservation(
    p_reservation_id UUID,
    p_reason VARCHAR(50) DEFAULT 'released'
) RETURNS BOOLEAN AS $$
DECLARE
    v_inventory_id UUID;
    v_quantity INT;
    v_status VARCHAR(20);
BEGIN
    -- Get and lock reservation
    SELECT inventory_id, quantity, status
    INTO v_inventory_id, v_quantity, v_status
    FROM stock_reservations
    WHERE id = p_reservation_id
    FOR UPDATE;

    IF v_status != 'active' THEN
        -- Already released or converted
        RETURN FALSE;
    END IF;

    -- Update reservation status
    UPDATE stock_reservations
    SET status = p_reason,
        released_at = NOW()
    WHERE id = p_reservation_id;

    -- Reduce reserved quantity
    UPDATE inventory
    SET reserved_quantity = reserved_quantity - v_quantity,
        version = version + 1,
        updated_at = NOW()
    WHERE id = v_inventory_id;

    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Convert Reservation to Order
-- Finalizes reservation when order is placed
-- ============================================
CREATE OR REPLACE FUNCTION convert_reservation_to_order(
    p_reservation_id UUID,
    p_order_id UUID,
    p_performed_by UUID
) RETURNS BOOLEAN AS $$
DECLARE
    v_inventory_id UUID;
    v_quantity INT;
    v_status VARCHAR(20);
    v_qty_before INT;
BEGIN
    -- Get and lock reservation
    SELECT inventory_id, quantity, status
    INTO v_inventory_id, v_quantity, v_status
    FROM stock_reservations
    WHERE id = p_reservation_id
    FOR UPDATE;

    IF v_status != 'active' THEN
        RAISE EXCEPTION 'Reservation is not active: %', v_status;
    END IF;

    -- Get current quantity for audit
    SELECT quantity INTO v_qty_before
    FROM inventory WHERE id = v_inventory_id;

    -- Update reservation
    UPDATE stock_reservations
    SET status = 'converted',
        order_id = p_order_id,
        converted_at = NOW()
    WHERE id = p_reservation_id;

    -- Deduct from both quantity and reserved_quantity
    UPDATE inventory
    SET quantity = quantity - v_quantity,
        reserved_quantity = reserved_quantity - v_quantity,
        version = version + 1,
        updated_at = NOW()
    WHERE id = v_inventory_id;

    -- Record movement
    INSERT INTO inventory_movements (
        inventory_id, movement_type, quantity_change,
        quantity_before, quantity_after,
        reference_type, reference_id, performed_by
    ) VALUES (
        v_inventory_id, 'order_placed', -v_quantity,
        v_qty_before, v_qty_before - v_quantity,
        'order', p_order_id, p_performed_by
    );

    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Cleanup Expired Reservations
-- Called by background job
-- ============================================
CREATE OR REPLACE FUNCTION cleanup_expired_reservations()
RETURNS INT AS $$
DECLARE
    v_count INT := 0;
    v_reservation RECORD;
BEGIN
    FOR v_reservation IN
        SELECT id, inventory_id, quantity
        FROM stock_reservations
        WHERE status = 'active' AND expires_at < NOW()
        FOR UPDATE SKIP LOCKED
    LOOP
        UPDATE stock_reservations
        SET status = 'expired',
            released_at = NOW()
        WHERE id = v_reservation.id;

        UPDATE inventory
        SET reserved_quantity = reserved_quantity - v_reservation.quantity,
            version = version + 1,
            updated_at = NOW()
        WHERE id = v_reservation.inventory_id;

        INSERT INTO inventory_movements (
            inventory_id, movement_type, quantity_change,
            quantity_before, quantity_after, notes
        )
        SELECT
            v_reservation.inventory_id,
            'reservation_expired',
            0, -- No actual stock change, just released reservation
            quantity,
            quantity,
            'Reservation expired after timeout'
        FROM inventory WHERE id = v_reservation.inventory_id;

        v_count := v_count + 1;
    END LOOP;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;
```

---

## 3. Business Rules

### 3.1 Stock Calculation Rules

| Rule ID | Rule | Formula/Logic |
|---------|------|---------------|
| BR-INV-001 | Available Stock | `available = quantity - reserved_quantity` |
| BR-INV-002 | Can Add to Cart | `requested_qty <= available_stock` |
| BR-INV-003 | Out of Stock | `available_stock = 0` |
| BR-INV-004 | Low Stock | `available_stock > 0 AND available_stock <= threshold` |
| BR-INV-005 | In Stock | `available_stock > threshold` |

### 3.2 Reservation Rules

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-RES-001 | Reservation Duration | Default 15 minutes, configurable per product category |
| BR-RES-002 | Reservation Extension | Can extend once by additional 10 minutes during active checkout |
| BR-RES-003 | Auto-Release | Expired reservations automatically released by background job |
| BR-RES-004 | User Reservation Limit | Max 5 active reservations per user/session |
| BR-RES-005 | Quantity Limit | Cannot reserve more than 10 units of same item |

### 3.3 Order Processing Rules

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-ORD-001 | Stock Validation | Re-validate stock availability before payment processing |
| BR-ORD-002 | Atomic Deduction | Stock deduction must be atomic with order creation |
| BR-ORD-003 | Failed Payment | Release reservation if payment fails |
| BR-ORD-004 | Order Cancellation | Return stock to inventory on order cancellation |
| BR-ORD-005 | Partial Fulfillment | Support partial shipments with split inventory updates |

### 3.4 Alert Rules

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-ALT-001 | Low Stock Alert | Trigger when available drops to/below threshold |
| BR-ALT-002 | Out of Stock Alert | Immediate notification when available reaches 0 |
| BR-ALT-003 | Back in Stock | Alert when previously OOS item has available > 0 |
| BR-ALT-004 | Alert Cooldown | No duplicate alerts within 4 hours for same item |
| BR-ALT-005 | Critical Alert | Email + SMS when high-demand item goes OOS |

### 3.5 Adjustment Rules

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-ADJ-001 | Audit Required | All manual adjustments require reason and user ID |
| BR-ADJ-002 | Negative Prevention | Cannot adjust below zero |
| BR-ADJ-003 | Large Adjustment Review | Adjustments > 100 units require manager approval |
| BR-ADJ-004 | Restock Recording | All restocks must reference PO or vendor |

---

## 4. API Endpoints

### 4.1 Public Endpoints (Customer-facing)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/inventory/{productId}` | Get product stock status | None |
| GET | `/api/v1/inventory/{productId}/variants` | Get all variant stock levels | None |
| GET | `/api/v1/inventory/check` | Bulk stock check (cart validation) | None |
| POST | `/api/v1/inventory/reserve` | Reserve stock for checkout | Session |
| DELETE | `/api/v1/inventory/reservations/{id}` | Release a reservation | Session |
| POST | `/api/v1/inventory/reservations/{id}/extend` | Extend reservation | Session |

### 4.2 Admin Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/admin/inventory` | List all inventory with filters | Admin |
| GET | `/api/v1/admin/inventory/{id}` | Get inventory details | Admin |
| PUT | `/api/v1/admin/inventory/{id}` | Update inventory settings | Admin |
| POST | `/api/v1/admin/inventory/{id}/adjust` | Adjust stock quantity | Admin |
| POST | `/api/v1/admin/inventory/bulk-adjust` | Bulk stock adjustment | Admin |
| GET | `/api/v1/admin/inventory/low-stock` | Get low stock items | Admin |
| GET | `/api/v1/admin/inventory/out-of-stock` | Get out of stock items | Admin |
| POST | `/api/v1/admin/inventory/restock` | Record restock | Admin |
| GET | `/api/v1/admin/inventory/{id}/movements` | Get movement history | Admin |
| GET | `/api/v1/admin/inventory/alerts` | Get active alerts | Admin |
| POST | `/api/v1/admin/inventory/alerts/{id}/acknowledge` | Acknowledge alert | Admin |
| GET | `/api/v1/admin/inventory/reports/summary` | Inventory summary report | Admin |
| GET | `/api/v1/admin/inventory/reports/movements` | Movement report | Admin |

### 4.3 Request/Response Examples

#### Get Product Stock
```http
GET /api/v1/inventory/550e8400-e29b-41d4-a716-446655440000
```

**Response:**
```json
{
  "productId": "550e8400-e29b-41d4-a716-446655440000",
  "productName": "Samsung WindFree AC 12000 BTU",
  "variants": [
    {
      "variantId": "660e8400-e29b-41d4-a716-446655440001",
      "variantName": "White",
      "sku": "SAM-WF-12K-WHT",
      "available": 15,
      "stockStatus": "in_stock",
      "lowStockThreshold": 10
    },
    {
      "variantId": "660e8400-e29b-41d4-a716-446655440002",
      "variantName": "Black",
      "sku": "SAM-WF-12K-BLK",
      "available": 3,
      "stockStatus": "low_stock",
      "lowStockThreshold": 10
    }
  ],
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

#### Reserve Stock
```http
POST /api/v1/inventory/reserve
Content-Type: application/json

{
  "items": [
    {
      "inventoryId": "770e8400-e29b-41d4-a716-446655440003",
      "quantity": 2
    }
  ],
  "cartId": "880e8400-e29b-41d4-a716-446655440004"
}
```

**Response:**
```json
{
  "reservations": [
    {
      "reservationId": "990e8400-e29b-41d4-a716-446655440005",
      "inventoryId": "770e8400-e29b-41d4-a716-446655440003",
      "quantity": 2,
      "expiresAt": "2024-01-15T10:45:00Z"
    }
  ],
  "expiresAt": "2024-01-15T10:45:00Z"
}
```

#### Adjust Stock (Admin)
```http
POST /api/v1/admin/inventory/550e8400-e29b-41d4-a716-446655440000/adjust
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "adjustmentType": "add",
  "quantity": 50,
  "reason": "Restock from supplier",
  "referenceNumber": "PO-2024-001234",
  "notes": "January shipment from Samsung distributor"
}
```

**Response:**
```json
{
  "inventoryId": "550e8400-e29b-41d4-a716-446655440000",
  "previousQuantity": 15,
  "adjustedQuantity": 50,
  "newQuantity": 65,
  "movementId": "aa0e8400-e29b-41d4-a716-446655440006",
  "adjustedAt": "2024-01-15T11:00:00Z",
  "adjustedBy": "admin@climasite.com"
}
```

---

## 5. Domain Models (C#)

### 5.1 Entities

```csharp
// src/ClimaSite.Core/Entities/Inventory.cs
namespace ClimaSite.Core.Entities;

public class Inventory : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 10;
    public int? ReorderQuantity { get; private set; } = 50;
    public string? WarehouseLocation { get; private set; }
    public bool IsTrackingEnabled { get; private set; } = true;
    public int Version { get; private set; } = 1;

    // Navigation properties
    public Product Product { get; private set; } = null!;
    public ProductVariant? Variant { get; private set; }
    public ICollection<InventoryMovement> Movements { get; private set; } = new List<InventoryMovement>();
    public ICollection<StockReservation> Reservations { get; private set; } = new List<StockReservation>();

    // Computed properties
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public StockStatus StockStatus => AvailableQuantity switch
    {
        0 => StockStatus.OutOfStock,
        _ when AvailableQuantity <= LowStockThreshold => StockStatus.LowStock,
        _ => StockStatus.InStock
    };

    // Factory method
    public static Inventory Create(
        Guid productId,
        Guid? variantId,
        string sku,
        int initialQuantity = 0,
        int lowStockThreshold = 10)
    {
        return new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            Sku = sku,
            Quantity = initialQuantity,
            ReservedQuantity = 0,
            LowStockThreshold = lowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Domain methods
    public void Reserve(int quantity)
    {
        if (quantity > AvailableQuantity)
            throw new InsufficientStockException(Sku, AvailableQuantity, quantity);

        ReservedQuantity += quantity;
        IncrementVersion();
    }

    public void ReleaseReservation(int quantity)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        IncrementVersion();
    }

    public void Deduct(int quantity)
    {
        if (quantity > Quantity)
            throw new InsufficientStockException(Sku, Quantity, quantity);

        Quantity -= quantity;
        IncrementVersion();
    }

    public void Add(int quantity)
    {
        Quantity += quantity;
        IncrementVersion();
    }

    public void SetThreshold(int threshold)
    {
        LowStockThreshold = threshold;
        IncrementVersion();
    }

    private void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum StockStatus
{
    InStock,
    LowStock,
    OutOfStock
}
```

```csharp
// src/ClimaSite.Core/Entities/InventoryMovement.cs
namespace ClimaSite.Core.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid InventoryId { get; private set; }
    public MovementType MovementType { get; private set; }
    public int QuantityChange { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public Guid? PerformedBy { get; private set; }

    // Navigation
    public Inventory Inventory { get; private set; } = null!;
    public User? PerformedByUser { get; private set; }

    public static InventoryMovement Create(
        Guid inventoryId,
        MovementType type,
        int quantityChange,
        int quantityBefore,
        string? referenceType = null,
        Guid? referenceId = null,
        string? notes = null,
        Guid? performedBy = null)
    {
        return new InventoryMovement
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            MovementType = type,
            QuantityChange = quantityChange,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityBefore + quantityChange,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            PerformedBy = performedBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum MovementType
{
    OrderPlaced,
    OrderCancelled,
    AdjustmentAdd,
    AdjustmentSubtract,
    ReturnReceived,
    Restock,
    TransferIn,
    TransferOut,
    ReservationExpired,
    InitialStock
}
```

```csharp
// src/ClimaSite.Core/Entities/StockReservation.cs
namespace ClimaSite.Core.Entities;

public class StockReservation : BaseEntity
{
    public Guid InventoryId { get; private set; }
    public string? SessionId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? CartId { get; private set; }
    public Guid? OrderId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? ConvertedAt { get; private set; }

    // Navigation
    public Inventory Inventory { get; private set; } = null!;
    public User? User { get; private set; }
    public ShoppingCart? Cart { get; private set; }
    public Order? Order { get; private set; }

    public bool IsExpired => Status == ReservationStatus.Active && DateTime.UtcNow > ExpiresAt;

    public static StockReservation Create(
        Guid inventoryId,
        int quantity,
        string? sessionId = null,
        Guid? userId = null,
        Guid? cartId = null,
        int expiryMinutes = 15)
    {
        return new StockReservation
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            SessionId = sessionId,
            UserId = userId,
            CartId = cartId,
            Quantity = quantity,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Release()
    {
        Status = ReservationStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = ReservationStatus.Expired;
        ReleasedAt = DateTime.UtcNow;
    }

    public void ConvertToOrder(Guid orderId)
    {
        OrderId = orderId;
        Status = ReservationStatus.Converted;
        ConvertedAt = DateTime.UtcNow;
    }

    public void Extend(int additionalMinutes = 10)
    {
        if (Status != ReservationStatus.Active)
            throw new InvalidOperationException("Can only extend active reservations");

        ExpiresAt = ExpiresAt.AddMinutes(additionalMinutes);
    }
}

public enum ReservationStatus
{
    Active,
    Released,
    Converted,
    Expired
}
```

### 5.2 Services

```csharp
// src/ClimaSite.Core/Interfaces/IInventoryService.cs
namespace ClimaSite.Core.Interfaces;

public interface IInventoryService
{
    // Stock queries
    Task<InventoryDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<InventoryDto?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default);
    Task<IEnumerable<InventoryDto>> GetByProductIdWithVariantsAsync(Guid productId, CancellationToken ct = default);
    Task<BulkStockCheckResult> CheckStockAsync(IEnumerable<StockCheckRequest> items, CancellationToken ct = default);

    // Stock reservations
    Task<StockReservationResult> ReserveStockAsync(ReserveStockCommand command, CancellationToken ct = default);
    Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken ct = default);
    Task<bool> ExtendReservationAsync(Guid reservationId, int minutes = 10, CancellationToken ct = default);
    Task<int> CleanupExpiredReservationsAsync(CancellationToken ct = default);

    // Stock modifications
    Task<InventoryMovement> AdjustStockAsync(AdjustStockCommand command, CancellationToken ct = default);
    Task<IEnumerable<InventoryMovement>> BulkAdjustStockAsync(BulkAdjustStockCommand command, CancellationToken ct = default);
    Task<bool> DeductStockForOrderAsync(Guid orderId, IEnumerable<OrderItem> items, CancellationToken ct = default);
    Task<bool> RestoreStockForCancelledOrderAsync(Guid orderId, CancellationToken ct = default);

    // Alerts
    Task<IEnumerable<LowStockAlertDto>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task<bool> AcknowledgeAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
}
```

```csharp
// src/ClimaSite.Infrastructure/Services/InventoryService.cs
namespace ClimaSite.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly IEventBus _eventBus;

    public InventoryService(
        IDbContext context,
        ILogger<InventoryService> logger,
        IEventBus eventBus)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<StockReservationResult> ReserveStockAsync(
        ReserveStockCommand command,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var reservations = new List<StockReservation>();

            foreach (var item in command.Items)
            {
                var inventory = await _context.Inventories
                    .Where(i => i.Id == item.InventoryId)
                    .FirstOrDefaultAsync(ct);

                if (inventory == null)
                    throw new InventoryNotFoundException(item.InventoryId);

                if (inventory.AvailableQuantity < item.Quantity)
                {
                    throw new InsufficientStockException(
                        inventory.Sku,
                        inventory.AvailableQuantity,
                        item.Quantity);
                }

                // Create reservation
                var reservation = StockReservation.Create(
                    inventory.Id,
                    item.Quantity,
                    command.SessionId,
                    command.UserId,
                    command.CartId,
                    command.ExpiryMinutes);

                inventory.Reserve(item.Quantity);

                _context.StockReservations.Add(reservation);
                reservations.Add(reservation);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Reserved stock for {Count} items, Cart: {CartId}",
                reservations.Count, command.CartId);

            return new StockReservationResult
            {
                Success = true,
                Reservations = reservations.Select(r => new ReservationDto
                {
                    ReservationId = r.Id,
                    InventoryId = r.InventoryId,
                    Quantity = r.Quantity,
                    ExpiresAt = r.ExpiresAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to reserve stock");
            throw;
        }
    }

    public async Task<bool> DeductStockForOrderAsync(
        Guid orderId,
        IEnumerable<OrderItem> items,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        try
        {
            foreach (var item in items)
            {
                var inventory = await _context.Inventories
                    .Where(i => i.ProductId == item.ProductId &&
                               i.VariantId == item.VariantId)
                    .FirstOrDefaultAsync(ct);

                if (inventory == null)
                    throw new InventoryNotFoundException(item.ProductId);

                var quantityBefore = inventory.Quantity;

                // Check for existing reservation
                var reservation = await _context.StockReservations
                    .Where(r => r.InventoryId == inventory.Id &&
                               r.CartId == item.CartId &&
                               r.Status == ReservationStatus.Active)
                    .FirstOrDefaultAsync(ct);

                if (reservation != null)
                {
                    // Convert reservation to order
                    reservation.ConvertToOrder(orderId);
                    inventory.ReleaseReservation(reservation.Quantity);
                }

                // Deduct stock
                inventory.Deduct(item.Quantity);

                // Record movement
                var movement = InventoryMovement.Create(
                    inventory.Id,
                    MovementType.OrderPlaced,
                    -item.Quantity,
                    quantityBefore,
                    "order",
                    orderId);

                _context.InventoryMovements.Add(movement);

                // Check for low stock alert
                if (inventory.StockStatus == StockStatus.LowStock ||
                    inventory.StockStatus == StockStatus.OutOfStock)
                {
                    await CreateLowStockAlertAsync(inventory, ct);
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await _eventBus.PublishAsync(new StockDeductedEvent(orderId, items));

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to deduct stock for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<int> CleanupExpiredReservationsAsync(CancellationToken ct = default)
    {
        var expiredReservations = await _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Active &&
                       r.ExpiresAt < DateTime.UtcNow)
            .Include(r => r.Inventory)
            .ToListAsync(ct);

        foreach (var reservation in expiredReservations)
        {
            reservation.Expire();
            reservation.Inventory.ReleaseReservation(reservation.Quantity);

            _logger.LogInformation(
                "Released expired reservation {ReservationId} for {Quantity} units",
                reservation.Id, reservation.Quantity);
        }

        await _context.SaveChangesAsync(ct);

        return expiredReservations.Count;
    }

    private async Task CreateLowStockAlertAsync(Inventory inventory, CancellationToken ct)
    {
        // Check for recent alert (cooldown period)
        var recentAlert = await _context.LowStockAlerts
            .Where(a => a.InventoryId == inventory.Id &&
                       a.CreatedAt > DateTime.UtcNow.AddHours(-4))
            .AnyAsync(ct);

        if (recentAlert) return;

        var alertType = inventory.AvailableQuantity == 0
            ? AlertType.OutOfStock
            : AlertType.LowStock;

        var alert = new LowStockAlert
        {
            Id = Guid.NewGuid(),
            InventoryId = inventory.Id,
            AlertType = alertType,
            AvailableQuantity = inventory.AvailableQuantity,
            Threshold = inventory.LowStockThreshold,
            CreatedAt = DateTime.UtcNow
        };

        _context.LowStockAlerts.Add(alert);

        await _eventBus.PublishAsync(new LowStockAlertCreatedEvent(alert));
    }
}
```

---

## 6. Background Jobs

### 6.1 Reservation Cleanup Job

```csharp
// src/ClimaSite.Infrastructure/Jobs/ReservationCleanupJob.cs
namespace ClimaSite.Infrastructure.Jobs;

public class ReservationCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationCleanupJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ReservationCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<ReservationCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var inventoryService = scope.ServiceProvider
                    .GetRequiredService<IInventoryService>();

                var cleanedCount = await inventoryService
                    .CleanupExpiredReservationsAsync(stoppingToken);

                if (cleanedCount > 0)
                {
                    _logger.LogInformation(
                        "Cleaned up {Count} expired reservations", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired reservations");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```

### 6.2 Low Stock Notification Job

```csharp
// src/ClimaSite.Infrastructure/Jobs/LowStockNotificationJob.cs
namespace ClimaSite.Infrastructure.Jobs;

public class LowStockNotificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LowStockNotificationJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var unsentAlerts = await context.LowStockAlerts
                    .Where(a => !a.NotificationSent && !a.IsAcknowledged)
                    .Include(a => a.Inventory)
                        .ThenInclude(i => i.Product)
                    .ToListAsync(stoppingToken);

                if (unsentAlerts.Any())
                {
                    await emailService.SendLowStockDigestAsync(unsentAlerts, stoppingToken);

                    foreach (var alert in unsentAlerts)
                    {
                        alert.NotificationSent = true;
                        alert.NotificationSentAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending low stock notifications");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```

---

## 7. Implementation Tasks

### Task INV-001: Database Schema & Migrations

**Priority:** Critical
**Estimate:** 4 hours
**Dependencies:** None

**Description:**
Create the database schema for inventory management including all tables, indexes, views, and stored procedures.

**Acceptance Criteria:**
- [ ] `inventory` table created with all columns and constraints
- [ ] `inventory_movements` table created with proper foreign keys
- [ ] `stock_reservations` table created with status tracking
- [ ] `low_stock_alerts` table created
- [ ] `inventory_audit_log` table created
- [ ] All indexes created for query optimization
- [ ] Views for available stock and low stock items created
- [ ] Stored procedures for atomic operations created
- [ ] EF Core migration generated and tested
- [ ] Seed data for development environment

**Technical Notes:**
```bash
# Generate migration
dotnet ef migrations add AddInventoryManagement --project src/ClimaSite.Infrastructure --startup-project src/ClimaSite.Api

# Apply migration
dotnet ef database update --project src/ClimaSite.Infrastructure --startup-project src/ClimaSite.Api
```

---

### Task INV-002: Domain Entities & Configurations

**Priority:** Critical
**Estimate:** 3 hours
**Dependencies:** INV-001

**Description:**
Create domain entities for Inventory, InventoryMovement, StockReservation, and LowStockAlert with proper encapsulation and business logic.

**Acceptance Criteria:**
- [ ] `Inventory` entity with computed `AvailableQuantity` and `StockStatus`
- [ ] `InventoryMovement` entity with factory method
- [ ] `StockReservation` entity with status transitions
- [ ] `LowStockAlert` entity
- [ ] EF Core configurations for all entities
- [ ] Proper concurrency handling with version field
- [ ] Domain events for stock changes

---

### Task INV-003: Inventory Service Implementation

**Priority:** Critical
**Estimate:** 8 hours
**Dependencies:** INV-002

**Description:**
Implement the core inventory service with all stock management operations using proper transaction handling.

**Acceptance Criteria:**
- [ ] `GetByProductIdAsync` - retrieve inventory by product
- [ ] `GetByVariantIdAsync` - retrieve inventory by variant
- [ ] `CheckStockAsync` - bulk stock availability check
- [ ] `ReserveStockAsync` - atomic stock reservation
- [ ] `ReleaseReservationAsync` - release held stock
- [ ] `ExtendReservationAsync` - extend reservation expiry
- [ ] `AdjustStockAsync` - manual stock adjustment with audit
- [ ] `DeductStockForOrderAsync` - order fulfillment deduction
- [ ] `RestoreStockForCancelledOrderAsync` - cancellation restoration
- [ ] All operations use proper transaction isolation
- [ ] Comprehensive logging for all operations
- [ ] Unit tests with >90% coverage

---

### Task INV-004: Stock Reservation System

**Priority:** Critical
**Estimate:** 6 hours
**Dependencies:** INV-003

**Description:**
Implement the complete stock reservation system for checkout flow with automatic expiry handling.

**Acceptance Criteria:**
- [ ] Reserve stock when checkout starts
- [ ] Validate reservation on each checkout step
- [ ] Release reservation on checkout abandonment
- [ ] Convert reservation to deduction on order completion
- [ ] Background job for expired reservation cleanup
- [ ] Handle concurrent reservation attempts gracefully
- [ ] Extend reservation during active checkout
- [ ] Integration tests for reservation flow

---

### Task INV-005: Public API Endpoints

**Priority:** High
**Estimate:** 4 hours
**Dependencies:** INV-003

**Description:**
Create customer-facing API endpoints for stock queries and reservation management.

**Acceptance Criteria:**
- [ ] `GET /api/v1/inventory/{productId}` - product stock status
- [ ] `GET /api/v1/inventory/{productId}/variants` - variant stock levels
- [ ] `POST /api/v1/inventory/check` - bulk stock validation
- [ ] `POST /api/v1/inventory/reserve` - reserve stock
- [ ] `DELETE /api/v1/inventory/reservations/{id}` - release reservation
- [ ] `POST /api/v1/inventory/reservations/{id}/extend` - extend reservation
- [ ] Proper error responses with problem details
- [ ] Rate limiting on reservation endpoints
- [ ] OpenAPI documentation

---

### Task INV-006: Admin API Endpoints

**Priority:** High
**Estimate:** 6 hours
**Dependencies:** INV-003

**Description:**
Create administrative API endpoints for inventory management, adjustments, and reporting.

**Acceptance Criteria:**
- [ ] `GET /api/v1/admin/inventory` - list with filtering/pagination
- [ ] `GET /api/v1/admin/inventory/{id}` - detailed inventory view
- [ ] `PUT /api/v1/admin/inventory/{id}` - update settings
- [ ] `POST /api/v1/admin/inventory/{id}/adjust` - stock adjustment
- [ ] `POST /api/v1/admin/inventory/bulk-adjust` - bulk adjustment
- [ ] `GET /api/v1/admin/inventory/low-stock` - low stock report
- [ ] `GET /api/v1/admin/inventory/out-of-stock` - out of stock items
- [ ] `POST /api/v1/admin/inventory/restock` - record restock
- [ ] `GET /api/v1/admin/inventory/{id}/movements` - movement history
- [ ] All endpoints require admin authentication
- [ ] Audit logging for all modifications

---

### Task INV-007: Low Stock Alert System

**Priority:** High
**Estimate:** 5 hours
**Dependencies:** INV-003

**Description:**
Implement the low stock alert system with notifications and acknowledgment workflow.

**Acceptance Criteria:**
- [ ] Automatically create alerts when stock drops below threshold
- [ ] Support different alert types (low_stock, out_of_stock, back_in_stock)
- [ ] Alert cooldown period (4 hours) to prevent spam
- [ ] Email notification for pending alerts
- [ ] Admin endpoint to view and acknowledge alerts
- [ ] WebSocket notifications for real-time alerts
- [ ] Configurable thresholds per product/category

---

### Task INV-008: Background Jobs

**Priority:** High
**Estimate:** 3 hours
**Dependencies:** INV-004, INV-007

**Description:**
Implement background jobs for reservation cleanup and alert notifications.

**Acceptance Criteria:**
- [ ] `ReservationCleanupJob` - runs every minute
- [ ] `LowStockNotificationJob` - runs every 15 minutes
- [ ] Proper error handling and retry logic
- [ ] Health checks for job status
- [ ] Metrics for job execution

---

### Task INV-009: Order Integration

**Priority:** Critical
**Estimate:** 6 hours
**Dependencies:** INV-003, INV-004

**Description:**
Integrate inventory management with the order processing flow.

**Acceptance Criteria:**
- [ ] Validate stock before order creation
- [ ] Deduct stock atomically with order creation
- [ ] Restore stock on order cancellation
- [ ] Handle partial cancellations
- [ ] Update stock on returns
- [ ] Event handlers for order state changes
- [ ] Rollback mechanism for failed orders

---

### Task INV-010: Admin Dashboard - Inventory List

**Priority:** Medium
**Estimate:** 6 hours
**Dependencies:** INV-006

**Description:**
Create Angular admin component for viewing and managing inventory.

**Acceptance Criteria:**
- [ ] Paginated inventory list with search
- [ ] Filter by stock status (in_stock, low_stock, out_of_stock)
- [ ] Filter by category/brand
- [ ] Sort by quantity, SKU, product name
- [ ] Quick stock adjustment modal
- [ ] Export to CSV functionality
- [ ] Responsive design

---

### Task INV-011: Admin Dashboard - Stock Adjustment

**Priority:** Medium
**Estimate:** 4 hours
**Dependencies:** INV-006

**Description:**
Create Angular admin component for stock adjustments with audit requirements.

**Acceptance Criteria:**
- [ ] Single item adjustment form
- [ ] Bulk adjustment via CSV upload
- [ ] Required reason field for all adjustments
- [ ] Reference number field (PO, etc.)
- [ ] Confirmation dialog for large adjustments
- [ ] Success/error notifications
- [ ] Adjustment history view

---

### Task INV-012: Admin Dashboard - Movement History

**Priority:** Medium
**Estimate:** 4 hours
**Dependencies:** INV-006

**Description:**
Create Angular admin component for viewing inventory movement history.

**Acceptance Criteria:**
- [ ] Paginated movement list per inventory item
- [ ] Filter by movement type
- [ ] Filter by date range
- [ ] Show who performed the action
- [ ] Link to related orders/returns
- [ ] Export to CSV

---

### Task INV-013: Admin Dashboard - Alerts

**Priority:** Medium
**Estimate:** 4 hours
**Dependencies:** INV-007

**Description:**
Create Angular admin component for low stock alerts management.

**Acceptance Criteria:**
- [ ] List of pending alerts with severity
- [ ] Acknowledge single/multiple alerts
- [ ] Quick restock action from alert
- [ ] Alert history view
- [ ] Real-time updates via WebSocket
- [ ] Badge count in navigation

---

### Task INV-014: Customer-Facing Stock Display

**Priority:** High
**Estimate:** 4 hours
**Dependencies:** INV-005

**Description:**
Update product pages to display stock status and handle out-of-stock scenarios.

**Acceptance Criteria:**
- [ ] Show stock status badge on product cards
- [ ] Show available quantity on product detail
- [ ] Disable add-to-cart when out of stock
- [ ] Show "Low stock" warning when applicable
- [ ] "Notify when available" option for OOS items
- [ ] Update cart when stock changes

---

### Task INV-015: Cart Stock Validation

**Priority:** Critical
**Estimate:** 5 hours
**Dependencies:** INV-004, INV-005

**Description:**
Implement real-time stock validation in the shopping cart.

**Acceptance Criteria:**
- [ ] Validate stock on add to cart
- [ ] Re-validate on cart page load
- [ ] Show warning when requested > available
- [ ] Auto-adjust quantity to available
- [ ] Reserve stock on checkout initiation
- [ ] Handle race conditions gracefully
- [ ] Clear messaging for stock issues

---

### Task INV-016: Checkout Stock Reservation

**Priority:** Critical
**Estimate:** 6 hours
**Dependencies:** INV-004, INV-015

**Description:**
Implement stock reservation during the checkout process.

**Acceptance Criteria:**
- [ ] Reserve all cart items on checkout start
- [ ] Show reservation timer to user
- [ ] Extend reservation on activity
- [ ] Release on checkout abandonment
- [ ] Handle reservation expiry during checkout
- [ ] Convert reservations on order completion
- [ ] Graceful handling of insufficient stock

---

### Task INV-017: Inventory Reports

**Priority:** Low
**Estimate:** 6 hours
**Dependencies:** INV-006

**Description:**
Create comprehensive inventory reporting functionality.

**Acceptance Criteria:**
- [ ] Inventory valuation report
- [ ] Stock movement summary report
- [ ] Low stock forecast report
- [ ] Turnover rate analysis
- [ ] Dead stock identification
- [ ] Export reports to PDF/Excel
- [ ] Schedule automated reports

---

### Task INV-018: Inventory Import/Export

**Priority:** Low
**Estimate:** 4 hours
**Dependencies:** INV-006

**Description:**
Implement bulk import/export functionality for inventory data.

**Acceptance Criteria:**
- [ ] Export current inventory to CSV
- [ ] Import stock levels from CSV
- [ ] Validation of import data
- [ ] Preview before import
- [ ] Error report for failed rows
- [ ] Support for initial stock setup

---

### Task INV-019: WebSocket Real-Time Updates

**Priority:** Medium
**Estimate:** 4 hours
**Dependencies:** INV-003

**Description:**
Implement WebSocket-based real-time inventory updates.

**Acceptance Criteria:**
- [ ] Broadcast stock changes to subscribed clients
- [ ] Admin dashboard receives all updates
- [ ] Product pages receive relevant updates
- [ ] Cart receives updates for cart items
- [ ] Handle reconnection gracefully
- [ ] Throttle updates for high-frequency changes

---

### Task INV-020: Unit Tests

**Priority:** High
**Estimate:** 8 hours
**Dependencies:** INV-003, INV-004

**Description:**
Comprehensive unit tests for all inventory management components.

**Acceptance Criteria:**
- [ ] Domain entity tests (Inventory, StockReservation, etc.)
- [ ] Service layer tests with mocked dependencies
- [ ] Controller tests
- [ ] Validation tests
- [ ] Edge case coverage (concurrent operations, boundary conditions)
- [ ] >90% code coverage

---

## 8. E2E Tests (Playwright - NO MOCKING)

All E2E tests run against a real database and API with no mocking. Tests use a test-specific database that is reset between test runs.

### Test Configuration

```typescript
// tests/e2e/playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './inventory',
  fullyParallel: false, // Run sequentially for data consistency
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'html',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5000',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'dotnet run --project ../src/ClimaSite.Api',
    url: 'http://localhost:5000/health',
    reuseExistingServer: !process.env.CI,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Testing',
      DATABASE_URL: process.env.TEST_DATABASE_URL,
    },
  },
});
```

### Test Fixtures

```typescript
// tests/e2e/inventory/fixtures.ts
import { test as base, expect, APIRequestContext } from '@playwright/test';

interface TestProduct {
  id: string;
  name: string;
  slug: string;
  sku: string;
  price: number;
  inventoryId: string;
}

interface TestUser {
  id: string;
  email: string;
  token: string;
}

interface InventoryFixtures {
  api: APIRequestContext;
  adminApi: APIRequestContext;
  testProduct: TestProduct;
  testUser: TestUser;
  adminUser: TestUser;
  createProduct: (data?: Partial<TestProduct>) => Promise<TestProduct>;
  setStock: (productId: string, quantity: number) => Promise<void>;
  getStock: (productId: string) => Promise<{ quantity: number; reserved: number; available: number }>;
  cleanupProducts: () => Promise<void>;
}

export const test = base.extend<InventoryFixtures>({
  api: async ({ playwright }, use) => {
    const api = await playwright.request.newContext({
      baseURL: process.env.BASE_URL || 'http://localhost:5000',
    });
    await use(api);
    await api.dispose();
  },

  adminApi: async ({ playwright }, use) => {
    // Login as admin and get token
    const api = await playwright.request.newContext({
      baseURL: process.env.BASE_URL || 'http://localhost:5000',
    });

    const loginResponse = await api.post('/api/v1/auth/login', {
      data: {
        email: 'admin@climasite.com',
        password: process.env.ADMIN_PASSWORD || 'Admin123!',
      },
    });
    const { token } = await loginResponse.json();

    const adminApi = await playwright.request.newContext({
      baseURL: process.env.BASE_URL || 'http://localhost:5000',
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    await use(adminApi);
    await adminApi.dispose();
    await api.dispose();
  },

  testUser: async ({ api }, use) => {
    const email = `test-${Date.now()}@example.com`;
    const password = 'Test123!';

    // Register user
    await api.post('/api/v1/auth/register', {
      data: { email, password, firstName: 'Test', lastName: 'User' },
    });

    // Login
    const loginResponse = await api.post('/api/v1/auth/login', {
      data: { email, password },
    });
    const { token, userId } = await loginResponse.json();

    await use({ id: userId, email, token });
  },

  createProduct: async ({ adminApi }, use) => {
    const createdProducts: string[] = [];

    const createFn = async (data?: Partial<TestProduct>): Promise<TestProduct> => {
      const timestamp = Date.now();
      const productData = {
        name: data?.name || `Test Product ${timestamp}`,
        sku: data?.sku || `TEST-SKU-${timestamp}`,
        slug: data?.slug || `test-product-${timestamp}`,
        basePrice: data?.price || 299.99,
        categoryId: '00000000-0000-0000-0000-000000000001', // Default category
        description: 'Test product for E2E testing',
        isActive: true,
      };

      const response = await adminApi.post('/api/v1/admin/products', {
        data: productData,
      });

      expect(response.ok()).toBeTruthy();
      const product = await response.json();
      createdProducts.push(product.id);

      return {
        id: product.id,
        name: product.name,
        slug: product.slug,
        sku: product.sku,
        price: product.basePrice,
        inventoryId: product.inventoryId,
      };
    };

    await use(createFn);

    // Cleanup created products
    for (const productId of createdProducts) {
      await adminApi.delete(`/api/v1/admin/products/${productId}`);
    }
  },

  setStock: async ({ adminApi }, use) => {
    await use(async (productId: string, quantity: number) => {
      const response = await adminApi.post(`/api/v1/admin/inventory/${productId}/adjust`, {
        data: {
          adjustmentType: 'set',
          quantity,
          reason: 'E2E test setup',
        },
      });
      expect(response.ok()).toBeTruthy();
    });
  },

  getStock: async ({ api }, use) => {
    await use(async (productId: string) => {
      const response = await api.get(`/api/v1/inventory/${productId}`);
      expect(response.ok()).toBeTruthy();
      const data = await response.json();
      return {
        quantity: data.variants[0]?.quantity || data.quantity,
        reserved: data.variants[0]?.reserved || data.reserved || 0,
        available: data.variants[0]?.available || data.available,
      };
    });
  },
});

export { expect };
```

### Test INV-E2E-001: Stock Prevents Overselling

```typescript
// tests/e2e/inventory/overselling.spec.ts
import { test, expect } from './fixtures';

test.describe('Stock Prevents Overselling', () => {
  test('cannot add more items than available stock to cart', async ({
    page,
    createProduct,
    setStock
  }) => {
    // Arrange: Create product with limited stock
    const product = await createProduct({
      name: 'Limited Stock AC Unit',
      sku: `LIMITED-${Date.now()}`,
    });
    await setStock(product.id, 2);

    // Act: Navigate to product and try to add 5 to cart
    await page.goto(`/products/${product.slug}`);
    await page.waitForSelector('[data-testid="product-detail"]');

    // Verify stock display shows 2 available
    await expect(page.getByTestId('stock-quantity')).toContainText('2');
    await expect(page.getByTestId('stock-status')).toContainText('In Stock');

    // Try to set quantity to 5
    const quantityInput = page.getByTestId('quantity-input');
    await quantityInput.fill('5');
    await page.getByTestId('add-to-cart-btn').click();

    // Assert: Should show error message
    await expect(page.getByTestId('stock-error')).toBeVisible();
    await expect(page.getByTestId('stock-error')).toContainText('Only 2 items available');

    // Verify cart was not updated
    await expect(page.getByTestId('cart-count')).toContainText('0');
  });

  test('shows out of stock message when quantity is 0', async ({
    page,
    createProduct,
    setStock
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Out of Stock Product' });
    await setStock(product.id, 0);

    // Act
    await page.goto(`/products/${product.slug}`);

    // Assert
    await expect(page.getByTestId('stock-status')).toContainText('Out of Stock');
    await expect(page.getByTestId('add-to-cart-btn')).toBeDisabled();
    await expect(page.getByTestId('notify-when-available')).toBeVisible();
  });

  test('shows low stock warning when below threshold', async ({
    page,
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Low Stock Product' });

    // Set threshold to 10, stock to 3
    await adminApi.put(`/api/v1/admin/inventory/${product.id}`, {
      data: { lowStockThreshold: 10 },
    });
    await setStock(product.id, 3);

    // Act
    await page.goto(`/products/${product.slug}`);

    // Assert
    await expect(page.getByTestId('low-stock-warning')).toBeVisible();
    await expect(page.getByTestId('low-stock-warning')).toContainText('Only 3 left');
  });

  test('concurrent users cannot oversell', async ({
    browser,
    createProduct,
    setStock
  }) => {
    // Arrange: Create product with stock of 1
    const product = await createProduct({ name: 'Single Unit Product' });
    await setStock(product.id, 1);

    // Create two browser contexts (simulating two users)
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();

    // Both users navigate to product
    await Promise.all([
      page1.goto(`/products/${product.slug}`),
      page2.goto(`/products/${product.slug}`),
    ]);

    // Both try to add to cart simultaneously
    const [result1, result2] = await Promise.allSettled([
      (async () => {
        await page1.getByTestId('add-to-cart-btn').click();
        await page1.waitForSelector('[data-testid="cart-success"], [data-testid="stock-error"]');
        return page1.getByTestId('cart-success').isVisible();
      })(),
      (async () => {
        await page2.getByTestId('add-to-cart-btn').click();
        await page2.waitForSelector('[data-testid="cart-success"], [data-testid="stock-error"]');
        return page2.getByTestId('cart-success').isVisible();
      })(),
    ]);

    // Assert: Exactly one should succeed, one should fail
    const successes = [result1, result2].filter(
      r => r.status === 'fulfilled' && r.value === true
    ).length;
    expect(successes).toBe(1);

    await context1.close();
    await context2.close();
  });
});
```

### Test INV-E2E-002: Stock Deduction After Order

```typescript
// tests/e2e/inventory/order-deduction.spec.ts
import { test, expect } from './fixtures';

test.describe('Stock Deduction After Order', () => {
  test('stock is deducted after successful order completion', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
  }) => {
    // Arrange: Create product with 10 units
    const product = await createProduct({
      name: 'Stock Deduction Test Product',
      price: 199.99,
    });
    await setStock(product.id, 10);

    // Verify initial stock
    const initialStock = await getStock(product.id);
    expect(initialStock.quantity).toBe(10);
    expect(initialStock.available).toBe(10);

    // Act: Login and complete purchase of 3 items
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');
    await page.waitForURL('/');

    // Add 3 items to cart
    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '3');
    await page.click('[data-testid="add-to-cart-btn"]');
    await expect(page.getByTestId('cart-success')).toBeVisible();

    // Go to checkout
    await page.goto('/checkout');
    await page.waitForSelector('[data-testid="checkout-form"]');

    // Fill shipping info
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="city"]', 'Test City');
    await page.fill('[data-testid="postal-code"]', '12345');
    await page.selectOption('[data-testid="country"]', 'US');
    await page.click('[data-testid="continue-to-payment"]');

    // Fill payment info (test card)
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/30');
    await page.fill('[data-testid="card-cvc"]', '123');

    // Complete order
    await page.click('[data-testid="place-order-btn"]');
    await page.waitForURL(/\/orders\/.*\/confirmation/);

    // Assert: Verify order confirmation
    await expect(page.getByTestId('order-success')).toBeVisible();

    // Verify stock was deducted
    const finalStock = await getStock(product.id);
    expect(finalStock.quantity).toBe(7); // 10 - 3 = 7
    expect(finalStock.available).toBe(7);
  });

  test('stock is restored when order is cancelled', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
    adminApi,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Cancellation Test Product' });
    await setStock(product.id, 10);

    // Complete an order for 2 items
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '2');
    await page.click('[data-testid="add-to-cart-btn"]');

    // Complete checkout flow (abbreviated)
    await page.goto('/checkout');
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="city"]', 'Test City');
    await page.fill('[data-testid="postal-code"]', '12345');
    await page.selectOption('[data-testid="country"]', 'US');
    await page.click('[data-testid="continue-to-payment"]');
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/30');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order-btn"]');

    // Get order ID from URL
    await page.waitForURL(/\/orders\/.*\/confirmation/);
    const orderId = page.url().split('/orders/')[1].split('/')[0];

    // Verify stock after order
    let stock = await getStock(product.id);
    expect(stock.quantity).toBe(8);

    // Admin cancels the order
    await adminApi.post(`/api/v1/admin/orders/${orderId}/cancel`, {
      data: { reason: 'Customer request' },
    });

    // Assert: Stock is restored
    stock = await getStock(product.id);
    expect(stock.quantity).toBe(10);
  });

  test('partial order cancellation restores partial stock', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
    adminApi,
  }) => {
    // Arrange: Create two products
    const product1 = await createProduct({ name: 'Partial Cancel Product 1' });
    const product2 = await createProduct({ name: 'Partial Cancel Product 2' });
    await setStock(product1.id, 10);
    await setStock(product2.id, 10);

    // Place order for both products
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    // Add product 1 (qty: 3)
    await page.goto(`/products/${product1.slug}`);
    await page.fill('[data-testid="quantity-input"]', '3');
    await page.click('[data-testid="add-to-cart-btn"]');

    // Add product 2 (qty: 2)
    await page.goto(`/products/${product2.slug}`);
    await page.fill('[data-testid="quantity-input"]', '2');
    await page.click('[data-testid="add-to-cart-btn"]');

    // Complete checkout
    await page.goto('/checkout');
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="city"]', 'Test City');
    await page.fill('[data-testid="postal-code"]', '12345');
    await page.selectOption('[data-testid="country"]', 'US');
    await page.click('[data-testid="continue-to-payment"]');
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/30');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order-btn"]');

    await page.waitForURL(/\/orders\/.*\/confirmation/);
    const orderId = page.url().split('/orders/')[1].split('/')[0];

    // Verify both stocks deducted
    expect((await getStock(product1.id)).quantity).toBe(7);
    expect((await getStock(product2.id)).quantity).toBe(8);

    // Cancel only product 1 from order
    const orderResponse = await adminApi.get(`/api/v1/admin/orders/${orderId}`);
    const order = await orderResponse.json();
    const product1LineItem = order.items.find((i: any) => i.productId === product1.id);

    await adminApi.post(`/api/v1/admin/orders/${orderId}/items/${product1LineItem.id}/cancel`, {
      data: { reason: 'Customer request', quantity: 3 },
    });

    // Assert: Only product 1 stock restored
    expect((await getStock(product1.id)).quantity).toBe(10);
    expect((await getStock(product2.id)).quantity).toBe(8); // Unchanged
  });
});
```

### Test INV-E2E-003: Stock Reservation During Checkout

```typescript
// tests/e2e/inventory/reservation.spec.ts
import { test, expect } from './fixtures';

test.describe('Stock Reservation During Checkout', () => {
  test('stock is reserved when checkout starts', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Reservation Test Product' });
    await setStock(product.id, 5);

    // Login and add to cart
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '2');
    await page.click('[data-testid="add-to-cart-btn"]');

    // Verify stock before checkout
    let stock = await getStock(product.id);
    expect(stock.quantity).toBe(5);
    expect(stock.reserved).toBe(0);
    expect(stock.available).toBe(5);

    // Start checkout
    await page.goto('/checkout');
    await page.waitForSelector('[data-testid="checkout-form"]');

    // Assert: Stock should now be reserved
    stock = await getStock(product.id);
    expect(stock.quantity).toBe(5);
    expect(stock.reserved).toBe(2);
    expect(stock.available).toBe(3);

    // Verify reservation timer is shown
    await expect(page.getByTestId('reservation-timer')).toBeVisible();
  });

  test('reservation is released when user abandons checkout', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Abandon Checkout Product' });
    await setStock(product.id, 5);

    // Login, add to cart, start checkout
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '2');
    await page.click('[data-testid="add-to-cart-btn"]');
    await page.goto('/checkout');
    await page.waitForSelector('[data-testid="checkout-form"]');

    // Verify reserved
    let stock = await getStock(product.id);
    expect(stock.reserved).toBe(2);

    // Abandon checkout by navigating away
    await page.goto('/');

    // Wait a moment for release to process
    await page.waitForTimeout(1000);

    // Assert: Reservation should be released
    stock = await getStock(product.id);
    expect(stock.reserved).toBe(0);
    expect(stock.available).toBe(5);
  });

  test('reservation expires after timeout', async ({
    page,
    createProduct,
    setStock,
    getStock,
    testUser,
    adminApi,
  }) => {
    // Arrange: Set very short reservation time for testing
    const product = await createProduct({ name: 'Expiry Test Product' });
    await setStock(product.id, 5);

    // Configure 1 minute reservation for this test
    await adminApi.put(`/api/v1/admin/settings/checkout`, {
      data: { reservationMinutes: 1 },
    });

    // Login, add to cart, start checkout
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '2');
    await page.click('[data-testid="add-to-cart-btn"]');
    await page.goto('/checkout');

    // Verify reserved
    let stock = await getStock(product.id);
    expect(stock.reserved).toBe(2);

    // Wait for expiry (1 minute + buffer for cleanup job)
    await page.waitForTimeout(75000); // 75 seconds

    // Assert: Reservation expired and released
    stock = await getStock(product.id);
    expect(stock.reserved).toBe(0);
    expect(stock.available).toBe(5);

    // User should see expiry message
    await expect(page.getByTestId('reservation-expired-message')).toBeVisible();

    // Reset setting
    await adminApi.put(`/api/v1/admin/settings/checkout`, {
      data: { reservationMinutes: 15 },
    });
  });

  test('another user cannot take reserved stock', async ({
    browser,
    createProduct,
    setStock,
    getStock,
  }) => {
    // Arrange: Product with 2 units
    const product = await createProduct({ name: 'Contested Stock Product' });
    await setStock(product.id, 2);

    // Create two user contexts
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();

    // User 1 logs in, adds both to cart, starts checkout
    await page1.goto('/login');
    await page1.fill('[data-testid="email-input"]', 'user1@test.com');
    await page1.fill('[data-testid="password-input"]', 'Test123!');
    await page1.click('[data-testid="login-btn"]');

    await page1.goto(`/products/${product.slug}`);
    await page1.fill('[data-testid="quantity-input"]', '2');
    await page1.click('[data-testid="add-to-cart-btn"]');
    await page1.goto('/checkout');
    await page1.waitForSelector('[data-testid="checkout-form"]');

    // User 1 now has both units reserved
    let stock = await getStock(product.id);
    expect(stock.reserved).toBe(2);
    expect(stock.available).toBe(0);

    // User 2 tries to add the product
    await page2.goto(`/products/${product.slug}`);

    // Assert: Should show out of stock for user 2
    await expect(page2.getByTestId('stock-status')).toContainText('Out of Stock');
    await expect(page2.getByTestId('add-to-cart-btn')).toBeDisabled();

    await context1.close();
    await context2.close();
  });
});
```

### Test INV-E2E-004: Admin Stock Adjustment

```typescript
// tests/e2e/inventory/admin-adjustment.spec.ts
import { test, expect } from './fixtures';

test.describe('Admin Stock Adjustment', () => {
  test('admin can add stock with audit trail', async ({
    page,
    createProduct,
    setStock,
    getStock,
    adminApi,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Admin Adjustment Product' });
    await setStock(product.id, 10);

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to inventory management
    await page.goto('/admin/inventory');
    await page.waitForSelector('[data-testid="inventory-list"]');

    // Find and click on the product
    await page.fill('[data-testid="search-input"]', product.sku);
    await page.click(`[data-testid="inventory-row-${product.id}"]`);

    // Open adjustment modal
    await page.click('[data-testid="adjust-stock-btn"]');
    await page.waitForSelector('[data-testid="adjustment-modal"]');

    // Add 50 units
    await page.selectOption('[data-testid="adjustment-type"]', 'add');
    await page.fill('[data-testid="adjustment-quantity"]', '50');
    await page.fill('[data-testid="adjustment-reason"]', 'Restock from supplier');
    await page.fill('[data-testid="adjustment-reference"]', 'PO-2024-001234');
    await page.click('[data-testid="confirm-adjustment-btn"]');

    // Assert: Success message
    await expect(page.getByTestId('adjustment-success')).toBeVisible();

    // Verify new stock level
    const stock = await getStock(product.id);
    expect(stock.quantity).toBe(60); // 10 + 50

    // Verify audit trail via API
    const movementsResponse = await adminApi.get(`/api/v1/admin/inventory/${product.id}/movements`);
    const movements = await movementsResponse.json();
    const latestMovement = movements.items[0];

    expect(latestMovement.movementType).toBe('adjustment_add');
    expect(latestMovement.quantityChange).toBe(50);
    expect(latestMovement.notes).toContain('Restock from supplier');
  });

  test('admin can subtract stock', async ({
    page,
    createProduct,
    setStock,
    getStock,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Stock Subtraction Product' });
    await setStock(product.id, 100);

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to inventory and adjust
    await page.goto(`/admin/inventory/${product.id}`);
    await page.click('[data-testid="adjust-stock-btn"]');

    await page.selectOption('[data-testid="adjustment-type"]', 'subtract');
    await page.fill('[data-testid="adjustment-quantity"]', '30');
    await page.fill('[data-testid="adjustment-reason"]', 'Damaged goods');
    await page.click('[data-testid="confirm-adjustment-btn"]');

    // Assert
    const stock = await getStock(product.id);
    expect(stock.quantity).toBe(70);
  });

  test('cannot adjust stock below zero', async ({
    page,
    createProduct,
    setStock,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'Negative Stock Test' });
    await setStock(product.id, 5);

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Try to subtract more than available
    await page.goto(`/admin/inventory/${product.id}`);
    await page.click('[data-testid="adjust-stock-btn"]');

    await page.selectOption('[data-testid="adjustment-type"]', 'subtract');
    await page.fill('[data-testid="adjustment-quantity"]', '10');
    await page.fill('[data-testid="adjustment-reason"]', 'Test');
    await page.click('[data-testid="confirm-adjustment-btn"]');

    // Assert: Error message
    await expect(page.getByTestId('adjustment-error')).toBeVisible();
    await expect(page.getByTestId('adjustment-error')).toContainText('Cannot reduce stock below zero');
  });

  test('bulk stock adjustment via CSV', async ({
    page,
    createProduct,
    setStock,
    getStock,
  }) => {
    // Arrange: Create multiple products
    const product1 = await createProduct({ name: 'Bulk Adjust 1', sku: 'BULK-001' });
    const product2 = await createProduct({ name: 'Bulk Adjust 2', sku: 'BULK-002' });
    const product3 = await createProduct({ name: 'Bulk Adjust 3', sku: 'BULK-003' });

    await setStock(product1.id, 10);
    await setStock(product2.id, 20);
    await setStock(product3.id, 30);

    // Create CSV content
    const csvContent = `sku,adjustment_type,quantity,reason
BULK-001,add,50,Restock
BULK-002,subtract,5,Damaged
BULK-003,set,100,Inventory count`;

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to bulk adjustment
    await page.goto('/admin/inventory/bulk-adjust');

    // Upload CSV
    const [fileChooser] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.click('[data-testid="upload-csv-btn"]'),
    ]);
    await fileChooser.setFiles({
      name: 'adjustments.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(csvContent),
    });

    // Preview should show
    await expect(page.getByTestId('csv-preview')).toBeVisible();
    await expect(page.getByTestId('csv-row-count')).toContainText('3 adjustments');

    // Confirm bulk adjustment
    await page.click('[data-testid="confirm-bulk-adjust-btn"]');
    await page.waitForSelector('[data-testid="bulk-adjust-success"]');

    // Assert: Verify all stock levels
    expect((await getStock(product1.id)).quantity).toBe(60);  // 10 + 50
    expect((await getStock(product2.id)).quantity).toBe(15);  // 20 - 5
    expect((await getStock(product3.id)).quantity).toBe(100); // Set to 100
  });
});
```

### Test INV-E2E-005: Low Stock Alerts

```typescript
// tests/e2e/inventory/low-stock-alerts.spec.ts
import { test, expect } from './fixtures';

test.describe('Low Stock Alerts', () => {
  test('alert is created when stock drops below threshold', async ({
    page,
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange: Create product with threshold of 10
    const product = await createProduct({ name: 'Low Stock Alert Product' });
    await adminApi.put(`/api/v1/admin/inventory/${product.id}`, {
      data: { lowStockThreshold: 10 },
    });
    await setStock(product.id, 15);

    // Act: Adjust stock to 5 (below threshold)
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: {
        adjustmentType: 'set',
        quantity: 5,
        reason: 'Test low stock alert',
      },
    });

    // Assert: Alert should be created
    const alertsResponse = await adminApi.get('/api/v1/admin/inventory/alerts?unacknowledged=true');
    const alerts = await alertsResponse.json();

    const productAlert = alerts.items.find((a: any) => a.inventoryId === product.inventoryId);
    expect(productAlert).toBeTruthy();
    expect(productAlert.alertType).toBe('low_stock');
    expect(productAlert.availableQuantity).toBe(5);
  });

  test('out of stock alert is created when stock reaches zero', async ({
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'OOS Alert Product' });
    await setStock(product.id, 5);

    // Act: Set stock to 0
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: {
        adjustmentType: 'set',
        quantity: 0,
        reason: 'Test OOS alert',
      },
    });

    // Assert
    const alertsResponse = await adminApi.get('/api/v1/admin/inventory/alerts?unacknowledged=true');
    const alerts = await alertsResponse.json();

    const productAlert = alerts.items.find((a: any) => a.inventoryId === product.inventoryId);
    expect(productAlert).toBeTruthy();
    expect(productAlert.alertType).toBe('out_of_stock');
  });

  test('admin can acknowledge alerts', async ({
    page,
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange: Create low stock alert
    const product = await createProduct({ name: 'Acknowledge Alert Product' });
    await adminApi.put(`/api/v1/admin/inventory/${product.id}`, {
      data: { lowStockThreshold: 10 },
    });
    await setStock(product.id, 5);

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to alerts
    await page.goto('/admin/inventory/alerts');
    await page.waitForSelector('[data-testid="alerts-list"]');

    // Find and acknowledge the alert
    const alertRow = page.locator(`[data-testid="alert-row"]`).filter({ hasText: product.name });
    await alertRow.getByTestId('acknowledge-btn').click();

    // Confirm acknowledgment
    await page.click('[data-testid="confirm-acknowledge-btn"]');

    // Assert: Alert no longer in unacknowledged list
    await expect(alertRow).not.toBeVisible();

    // Verify via API
    const alertsResponse = await adminApi.get('/api/v1/admin/inventory/alerts?unacknowledged=true');
    const alerts = await alertsResponse.json();
    const productAlert = alerts.items.find((a: any) => a.productName === product.name);
    expect(productAlert).toBeUndefined();
  });

  test('low stock items appear in admin dashboard', async ({
    page,
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange: Create products with varying stock levels
    const lowStockProduct = await createProduct({ name: 'Dashboard Low Stock' });
    const oosProduct = await createProduct({ name: 'Dashboard OOS' });
    const normalProduct = await createProduct({ name: 'Dashboard Normal Stock' });

    await adminApi.put(`/api/v1/admin/inventory/${lowStockProduct.id}`, {
      data: { lowStockThreshold: 10 },
    });
    await adminApi.put(`/api/v1/admin/inventory/${oosProduct.id}`, {
      data: { lowStockThreshold: 10 },
    });

    await setStock(lowStockProduct.id, 5);
    await setStock(oosProduct.id, 0);
    await setStock(normalProduct.id, 100);

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to inventory with low stock filter
    await page.goto('/admin/inventory?status=low_stock,out_of_stock');

    // Assert: Low stock and OOS products are shown
    await expect(page.getByText(lowStockProduct.name)).toBeVisible();
    await expect(page.getByText(oosProduct.name)).toBeVisible();

    // Normal stock product should NOT be shown
    await expect(page.getByText(normalProduct.name)).not.toBeVisible();
  });
});
```

### Test INV-E2E-006: Inventory Movement History

```typescript
// tests/e2e/inventory/movement-history.spec.ts
import { test, expect } from './fixtures';

test.describe('Inventory Movement History', () => {
  test('all stock movements are recorded with full audit trail', async ({
    page,
    createProduct,
    setStock,
    testUser,
    adminApi,
  }) => {
    // Arrange: Create product
    const product = await createProduct({ name: 'Movement History Product' });

    // Perform various stock operations
    // 1. Initial stock set
    await setStock(product.id, 100);

    // 2. Manual adjustment
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: {
        adjustmentType: 'subtract',
        quantity: 10,
        reason: 'Damaged goods',
      },
    });

    // 3. Create order (will deduct stock)
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '5');
    await page.click('[data-testid="add-to-cart-btn"]');
    await page.goto('/checkout');
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="city"]', 'Test City');
    await page.fill('[data-testid="postal-code"]', '12345');
    await page.selectOption('[data-testid="country"]', 'US');
    await page.click('[data-testid="continue-to-payment"]');
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/30');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order-btn"]');
    await page.waitForURL(/\/orders\/.*\/confirmation/);

    // 4. Restock
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: {
        adjustmentType: 'add',
        quantity: 50,
        reason: 'Restock from supplier',
        referenceNumber: 'PO-12345',
      },
    });

    // Assert: Check movement history via admin API
    const movementsResponse = await adminApi.get(`/api/v1/admin/inventory/${product.id}/movements`);
    const movements = await movementsResponse.json();

    expect(movements.items.length).toBeGreaterThanOrEqual(4);

    // Verify movement types
    const types = movements.items.map((m: any) => m.movementType);
    expect(types).toContain('initial_stock');
    expect(types).toContain('adjustment_subtract');
    expect(types).toContain('order_placed');
    expect(types).toContain('adjustment_add');

    // Verify audit fields
    const restockMovement = movements.items.find((m: any) => m.movementType === 'adjustment_add');
    expect(restockMovement.notes).toContain('Restock from supplier');
    expect(restockMovement.performedBy).toBeTruthy();
    expect(restockMovement.quantityBefore).toBeDefined();
    expect(restockMovement.quantityAfter).toBeDefined();
  });

  test('movement history is viewable in admin UI', async ({
    page,
    createProduct,
    setStock,
    adminApi,
  }) => {
    // Arrange
    const product = await createProduct({ name: 'UI Movement History' });
    await setStock(product.id, 100);

    // Create some movements
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: { adjustmentType: 'subtract', quantity: 20, reason: 'Sold offline' },
    });
    await adminApi.post(`/api/v1/admin/inventory/${product.id}/adjust`, {
      data: { adjustmentType: 'add', quantity: 50, reason: 'Restock' },
    });

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', 'admin@climasite.com');
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD || 'Admin123!');
    await page.click('[data-testid="login-btn"]');

    // Navigate to inventory detail
    await page.goto(`/admin/inventory/${product.id}`);
    await page.click('[data-testid="movement-history-tab"]');

    // Assert: Movements are displayed
    await expect(page.getByTestId('movement-list')).toBeVisible();

    const movementRows = page.locator('[data-testid="movement-row"]');
    await expect(movementRows).toHaveCount(3); // initial + 2 adjustments

    // Check content
    await expect(page.getByText('Sold offline')).toBeVisible();
    await expect(page.getByText('Restock')).toBeVisible();
  });
});
```

### Test INV-E2E-007: Variant-Level Stock

```typescript
// tests/e2e/inventory/variant-stock.spec.ts
import { test, expect } from './fixtures';

test.describe('Variant-Level Stock Tracking', () => {
  test('each variant has independent stock', async ({
    page,
    adminApi,
    getStock,
  }) => {
    // Arrange: Create product with variants
    const productResponse = await adminApi.post('/api/v1/admin/products', {
      data: {
        name: 'Multi-Variant AC Unit',
        sku: `VARIANT-${Date.now()}`,
        basePrice: 599.99,
        categoryId: '00000000-0000-0000-0000-000000000001',
        variants: [
          { name: 'White', sku: `VARIANT-${Date.now()}-WHT`, priceAdjustment: 0 },
          { name: 'Black', sku: `VARIANT-${Date.now()}-BLK`, priceAdjustment: 20 },
          { name: 'Silver', sku: `VARIANT-${Date.now()}-SLV`, priceAdjustment: 50 },
        ],
      },
    });
    const product = await productResponse.json();

    // Set different stock levels for each variant
    const whiteVariant = product.variants.find((v: any) => v.name === 'White');
    const blackVariant = product.variants.find((v: any) => v.name === 'Black');
    const silverVariant = product.variants.find((v: any) => v.name === 'Silver');

    await adminApi.post(`/api/v1/admin/inventory/variant/${whiteVariant.id}/adjust`, {
      data: { adjustmentType: 'set', quantity: 100, reason: 'Initial' },
    });
    await adminApi.post(`/api/v1/admin/inventory/variant/${blackVariant.id}/adjust`, {
      data: { adjustmentType: 'set', quantity: 50, reason: 'Initial' },
    });
    await adminApi.post(`/api/v1/admin/inventory/variant/${silverVariant.id}/adjust`, {
      data: { adjustmentType: 'set', quantity: 0, reason: 'Initial' },
    });

    // Act: Navigate to product page
    await page.goto(`/products/${product.slug}`);

    // Select White variant
    await page.click('[data-testid="variant-option-White"]');
    await expect(page.getByTestId('stock-quantity')).toContainText('100');
    await expect(page.getByTestId('add-to-cart-btn')).toBeEnabled();

    // Select Black variant
    await page.click('[data-testid="variant-option-Black"]');
    await expect(page.getByTestId('stock-quantity')).toContainText('50');
    await expect(page.getByTestId('add-to-cart-btn')).toBeEnabled();

    // Select Silver variant (out of stock)
    await page.click('[data-testid="variant-option-Silver"]');
    await expect(page.getByTestId('stock-status')).toContainText('Out of Stock');
    await expect(page.getByTestId('add-to-cart-btn')).toBeDisabled();

    // Cleanup
    await adminApi.delete(`/api/v1/admin/products/${product.id}`);
  });

  test('ordering one variant does not affect others', async ({
    page,
    adminApi,
    testUser,
  }) => {
    // Arrange: Create product with variants
    const productResponse = await adminApi.post('/api/v1/admin/products', {
      data: {
        name: 'Variant Independence Test',
        sku: `VINDEP-${Date.now()}`,
        basePrice: 399.99,
        categoryId: '00000000-0000-0000-0000-000000000001',
        variants: [
          { name: 'Small', sku: `VINDEP-${Date.now()}-S` },
          { name: 'Large', sku: `VINDEP-${Date.now()}-L` },
        ],
      },
    });
    const product = await productResponse.json();
    const smallVariant = product.variants.find((v: any) => v.name === 'Small');
    const largeVariant = product.variants.find((v: any) => v.name === 'Large');

    // Set stock
    await adminApi.post(`/api/v1/admin/inventory/variant/${smallVariant.id}/adjust`, {
      data: { adjustmentType: 'set', quantity: 10, reason: 'Initial' },
    });
    await adminApi.post(`/api/v1/admin/inventory/variant/${largeVariant.id}/adjust`, {
      data: { adjustmentType: 'set', quantity: 10, reason: 'Initial' },
    });

    // Login and order Small variant
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-btn"]');

    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="variant-option-Small"]');
    await page.fill('[data-testid="quantity-input"]', '3');
    await page.click('[data-testid="add-to-cart-btn"]');

    // Complete order
    await page.goto('/checkout');
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="city"]', 'Test City');
    await page.fill('[data-testid="postal-code"]', '12345');
    await page.selectOption('[data-testid="country"]', 'US');
    await page.click('[data-testid="continue-to-payment"]');
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/30');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order-btn"]');
    await page.waitForURL(/\/orders\/.*\/confirmation/);

    // Assert: Check variant stock levels via API
    const inventoryResponse = await adminApi.get(`/api/v1/inventory/${product.id}/variants`);
    const inventory = await inventoryResponse.json();

    const smallStock = inventory.variants.find((v: any) => v.variantId === smallVariant.id);
    const largeStock = inventory.variants.find((v: any) => v.variantId === largeVariant.id);

    expect(smallStock.available).toBe(7); // 10 - 3
    expect(largeStock.available).toBe(10); // Unchanged

    // Cleanup
    await adminApi.delete(`/api/v1/admin/products/${product.id}`);
  });
});
```

---

## 9. Performance Considerations

### 9.1 Database Optimization

- **Indexes**: All frequently queried columns are indexed
- **Partial Indexes**: Low stock queries use partial indexes for efficiency
- **Connection Pooling**: Use Npgsql connection pooling for high-throughput scenarios
- **Read Replicas**: Consider read replicas for inventory queries (eventual consistency acceptable)

### 9.2 Caching Strategy

```csharp
// Stock availability can be cached with short TTL
public class CachedInventoryService : IInventoryService
{
    private readonly IInventoryService _inner;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);

    public async Task<InventoryDto?> GetByProductIdAsync(Guid productId, CancellationToken ct)
    {
        var cacheKey = $"inventory:product:{productId}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);

        if (cached != null)
            return JsonSerializer.Deserialize<InventoryDto>(cached);

        var inventory = await _inner.GetByProductIdAsync(productId, ct);

        if (inventory != null)
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(inventory),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
                ct);
        }

        return inventory;
    }
}
```

### 9.3 Concurrency Handling

- **Optimistic Locking**: Version field on inventory table
- **Pessimistic Locking**: `FOR UPDATE` on critical operations
- **Retry Logic**: Exponential backoff for concurrent modification exceptions

---

## 10. Monitoring & Observability

### 10.1 Metrics to Track

| Metric | Type | Description |
|--------|------|-------------|
| `inventory.stock.level` | Gauge | Current stock level per SKU |
| `inventory.reservations.active` | Gauge | Active reservations count |
| `inventory.reservations.expired` | Counter | Expired reservations per hour |
| `inventory.adjustments.total` | Counter | Total adjustments by type |
| `inventory.oversell.attempts` | Counter | Attempted oversell (prevented) |
| `inventory.low_stock.alerts` | Counter | Low stock alerts generated |

### 10.2 Alerts

- **Critical**: Out of stock on high-demand product
- **Warning**: Low stock threshold breached
- **Info**: Large adjustment (>100 units) performed

### 10.3 Logging

All inventory operations should log:
- User/system performing action
- Before/after quantities
- Reference IDs (order, PO, etc.)
- Timestamp with timezone

---

## 11. Security Considerations

### 11.1 Authorization

- Public endpoints: Read-only stock status
- Admin endpoints: Require `inventory:manage` permission
- Bulk operations: Require `inventory:bulk` permission
- Audit log access: Require `inventory:audit` permission

### 11.2 Rate Limiting

- Stock check: 100 requests/minute per IP
- Reservation: 10 requests/minute per session
- Admin adjustments: 50 requests/minute per user

### 11.3 Input Validation

- Quantity must be positive integer
- Reason required for all adjustments
- Reference numbers validated against allowed patterns

---

## 12. Migration Strategy

### 12.1 From Legacy System

1. Export current inventory data
2. Map to new schema (product_id, variant_id, quantity)
3. Import with `initial_stock` movement type
4. Verify totals match
5. Enable new system with feature flag
6. Monitor for discrepancies
7. Decommission legacy system

### 12.2 Rollback Plan

- Feature flag to disable new inventory system
- Fallback to legacy inventory checks
- Data sync job to keep legacy updated during transition

---

## 13. Dependencies

### 13.1 Internal Dependencies

- Product Management (products, variants)
- Order Management (order creation, cancellation)
- User Authentication (admin actions)
- Notification Service (alerts)

### 13.2 External Dependencies

- PostgreSQL 15+
- Redis (caching)
- Background job processor (Hangfire/Quartz)

---

## 14. Timeline Estimate

| Phase | Tasks | Duration |
|-------|-------|----------|
| Phase 1: Foundation | INV-001, INV-002, INV-003 | 2 weeks |
| Phase 2: Reservations | INV-004, INV-005, INV-006 | 2 weeks |
| Phase 3: Integration | INV-007, INV-008, INV-009 | 2 weeks |
| Phase 4: Admin UI | INV-010 to INV-014 | 2 weeks |
| Phase 5: Customer UI | INV-015, INV-016 | 1 week |
| Phase 6: Polish | INV-017 to INV-020 | 2 weeks |
| **Total** | | **11 weeks** |

---

## 15. Success Criteria

1. **Zero overselling incidents** after launch
2. **Stock accuracy** > 99.9% compared to physical counts
3. **Reservation conversion rate** > 60%
4. **Low stock alert response time** < 4 hours average
5. **System performance**: Stock check < 50ms p99
6. **All E2E tests passing** in CI/CD pipeline
