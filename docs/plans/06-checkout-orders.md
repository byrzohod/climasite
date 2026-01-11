# Checkout & Orders Implementation Plan

## 1. Overview

This document outlines the comprehensive implementation plan for the Checkout and Orders system in ClimaSite HVAC e-commerce platform. The system provides a multi-step checkout process with Stripe payment integration, supporting both registered users and guest checkout.

### Key Features
- **Multi-step checkout**: Shipping, Payment, Review, Confirmation
- **Guest checkout support**: Complete purchases without registration
- **Stripe payment integration**: Secure card payments with Payment Intents API
- **Order state machine**: Complete order lifecycle management
- **Real-time inventory validation**: Stock checks throughout checkout
- **Order history and tracking**: Full visibility for customers and admins

### Dependencies
- User Authentication System (for registered user checkout)
- Product Catalog (for order items)
- Shopping Cart (checkout initiation source)
- Email Service (order notifications)

---

## 2. Database Schema

### 2.1 Orders Table

```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(20) NOT NULL UNIQUE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    guest_email VARCHAR(255),
    guest_phone VARCHAR(50),
    status VARCHAR(50) NOT NULL DEFAULT 'pending',

    -- Pricing
    subtotal DECIMAL(10,2) NOT NULL,
    shipping_cost DECIMAL(10,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    discount_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    total DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',

    -- Addresses (JSONB for flexibility)
    shipping_address JSONB NOT NULL,
    billing_address JSONB NOT NULL,
    same_billing_shipping BOOLEAN NOT NULL DEFAULT true,

    -- Payment
    stripe_payment_intent_id VARCHAR(255),
    stripe_charge_id VARCHAR(255),
    payment_method VARCHAR(50),
    payment_status VARCHAR(50) DEFAULT 'pending',
    paid_at TIMESTAMP WITH TIME ZONE,

    -- Shipping
    shipping_method VARCHAR(100),
    shipping_carrier VARCHAR(100),
    tracking_number VARCHAR(255),
    estimated_delivery_date DATE,
    shipped_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,

    -- Additional
    discount_code_id UUID REFERENCES discount_codes(id),
    notes TEXT,
    internal_notes TEXT,
    ip_address INET,
    user_agent TEXT,

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    cancelled_at TIMESTAMP WITH TIME ZONE,

    -- Constraints
    CONSTRAINT chk_order_user_or_guest CHECK (
        user_id IS NOT NULL OR guest_email IS NOT NULL
    ),
    CONSTRAINT chk_order_status CHECK (
        status IN ('pending', 'paid', 'processing', 'shipped', 'delivered', 'cancelled', 'refunded', 'returned')
    ),
    CONSTRAINT chk_payment_status CHECK (
        payment_status IN ('pending', 'processing', 'succeeded', 'failed', 'cancelled', 'refunded')
    )
);

-- Indexes
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_guest_email ON orders(guest_email);
CREATE INDEX idx_orders_order_number ON orders(order_number);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at DESC);
CREATE INDEX idx_orders_stripe_payment_intent ON orders(stripe_payment_intent_id);
```

### 2.2 Order Items Table

```sql
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id) ON DELETE SET NULL,
    variant_id UUID REFERENCES product_variants(id) ON DELETE SET NULL,

    -- Snapshot at order time (prevents price changes affecting historical orders)
    product_name VARCHAR(255) NOT NULL,
    product_sku VARCHAR(100) NOT NULL,
    variant_name VARCHAR(100),
    product_image_url VARCHAR(500),
    product_slug VARCHAR(255),

    -- Pricing
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL,
    discount_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    total_price DECIMAL(10,2) NOT NULL,

    -- Product attributes snapshot
    attributes JSONB,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);
```

### 2.3 Order Status History Table

```sql
CREATE TABLE order_status_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    previous_status VARCHAR(50),
    new_status VARCHAR(50) NOT NULL,
    changed_by_user_id UUID REFERENCES users(id),
    changed_by_type VARCHAR(20) NOT NULL DEFAULT 'system',
    notes TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT chk_changed_by_type CHECK (
        changed_by_type IN ('system', 'admin', 'customer', 'webhook')
    )
);

-- Indexes
CREATE INDEX idx_order_status_history_order_id ON order_status_history(order_id);
CREATE INDEX idx_order_status_history_created_at ON order_status_history(created_at);
```

### 2.4 Checkout Sessions Table

```sql
CREATE TABLE checkout_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_token VARCHAR(255) NOT NULL UNIQUE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    cart_id UUID REFERENCES carts(id) ON DELETE CASCADE,

    -- Current step
    current_step VARCHAR(20) NOT NULL DEFAULT 'shipping',

    -- Collected data
    shipping_address JSONB,
    billing_address JSONB,
    same_billing_shipping BOOLEAN DEFAULT true,
    shipping_method VARCHAR(100),

    -- Guest info
    guest_email VARCHAR(255),
    guest_phone VARCHAR(50),

    -- Pricing snapshot
    subtotal DECIMAL(10,2),
    shipping_cost DECIMAL(10,2),
    tax_amount DECIMAL(10,2),
    discount_amount DECIMAL(10,2),
    total DECIMAL(10,2),

    -- Stripe
    stripe_payment_intent_id VARCHAR(255),
    stripe_client_secret VARCHAR(255),

    -- Discount
    discount_code_id UUID REFERENCES discount_codes(id),

    -- Metadata
    ip_address INET,
    user_agent TEXT,

    -- Expiration
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT chk_checkout_step CHECK (
        current_step IN ('shipping', 'payment', 'review', 'processing', 'completed')
    )
);

-- Indexes
CREATE INDEX idx_checkout_sessions_token ON checkout_sessions(session_token);
CREATE INDEX idx_checkout_sessions_user_id ON checkout_sessions(user_id);
CREATE INDEX idx_checkout_sessions_expires_at ON checkout_sessions(expires_at);
```

### 2.5 Address Schema (JSONB Structure)

```json
{
  "firstName": "string",
  "lastName": "string",
  "company": "string | null",
  "addressLine1": "string",
  "addressLine2": "string | null",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string (ISO 3166-1 alpha-2)",
  "phone": "string | null"
}
```

---

## 3. Order Status State Machine

### 3.1 Status Flow Diagram

```
                              ┌─────────────┐
                              │   PENDING   │
                              │ (awaiting   │
                              │  payment)   │
                              └──────┬──────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
                    ▼                ▼                ▼
             ┌───────────┐    ┌───────────┐    ┌───────────┐
             │ CANCELLED │    │   PAID    │    │  EXPIRED  │
             │           │    │           │    │           │
             └───────────┘    └─────┬─────┘    └───────────┘
                                    │
                                    ▼
                              ┌───────────┐
                              │PROCESSING │◄──────────────┐
                              │           │               │
                              └─────┬─────┘               │
                                    │                     │
                    ┌───────────────┼───────────────┐     │
                    │               │               │     │
                    ▼               ▼               │     │
             ┌───────────┐    ┌───────────┐        │     │
             │ CANCELLED │    │  SHIPPED  │        │     │
             │           │    │           │        │     │
             └───────────┘    └─────┬─────┘        │     │
                                    │              │     │
                    ┌───────────────┼──────────────┤     │
                    │               │              │     │
                    ▼               ▼              ▼     │
             ┌───────────┐    ┌───────────┐    ┌───────────┐
             │ DELIVERED │    │ RETURNED  │    │ REFUNDED  │
             │           │    │           │    │ (partial) │
             └─────┬─────┘    └───────────┘    └───────────┘
                   │
                   ▼
             ┌───────────┐
             │ RETURNED  │
             │ (if RMA)  │
             └───────────┘
```

### 3.2 Status Definitions

| Status | Description | Triggers | Actions |
|--------|-------------|----------|---------|
| `pending` | Order created, awaiting payment | Checkout initiated | Create PaymentIntent |
| `paid` | Payment confirmed | Stripe webhook `payment_intent.succeeded` | Reserve inventory, send confirmation email |
| `processing` | Order being prepared | Admin action or automatic | Update inventory |
| `shipped` | Order dispatched | Admin adds tracking | Send shipping notification |
| `delivered` | Order received by customer | Carrier confirmation or manual | Complete order |
| `cancelled` | Order cancelled | Customer/admin request (before shipped) | Release inventory, refund if paid |
| `refunded` | Payment refunded | Admin action | Process Stripe refund |
| `returned` | Product returned | RMA process completed | Update inventory, process refund |

### 3.3 Valid Status Transitions

```csharp
public static class OrderStatusTransitions
{
    public static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Cancelled, OrderStatus.Refunded },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled, OrderStatus.Refunded },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered, OrderStatus.Returned },
        [OrderStatus.Delivered] = new[] { OrderStatus.Returned },
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.Refunded] = Array.Empty<OrderStatus>(),
        [OrderStatus.Returned] = new[] { OrderStatus.Refunded }
    };
}
```

---

## 4. API Endpoints

### 4.1 Checkout Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/checkout/start` | Initialize checkout session from cart | Optional |
| GET | `/api/v1/checkout/session` | Get current checkout session | Session Token |
| PUT | `/api/v1/checkout/guest-info` | Set guest email/phone (guest only) | Session Token |
| PUT | `/api/v1/checkout/shipping-address` | Set shipping address | Session Token |
| PUT | `/api/v1/checkout/billing-address` | Set billing address | Session Token |
| GET | `/api/v1/checkout/shipping-methods` | Get available shipping methods | Session Token |
| PUT | `/api/v1/checkout/shipping-method` | Select shipping method | Session Token |
| POST | `/api/v1/checkout/apply-discount` | Apply discount code | Session Token |
| DELETE | `/api/v1/checkout/discount` | Remove discount code | Session Token |
| POST | `/api/v1/checkout/payment-intent` | Create/update Stripe PaymentIntent | Session Token |
| POST | `/api/v1/checkout/complete` | Finalize order after payment | Session Token |
| POST | `/api/v1/checkout/cancel` | Cancel checkout session | Session Token |

### 4.2 Order Endpoints (Customer)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/orders` | List user's orders | Required |
| GET | `/api/v1/orders/{orderNumber}` | Get order details | Required |
| GET | `/api/v1/orders/guest/{orderNumber}` | Get guest order (with email verification) | Email Token |
| POST | `/api/v1/orders/{orderNumber}/cancel` | Request order cancellation | Required |

### 4.3 Order Endpoints (Admin)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/admin/orders` | List all orders (paginated, filterable) | Admin |
| GET | `/api/v1/admin/orders/{orderNumber}` | Get order details | Admin |
| PUT | `/api/v1/admin/orders/{orderNumber}/status` | Update order status | Admin |
| POST | `/api/v1/admin/orders/{orderNumber}/refund` | Process refund | Admin |
| PUT | `/api/v1/admin/orders/{orderNumber}/tracking` | Add tracking information | Admin |
| PUT | `/api/v1/admin/orders/{orderNumber}/notes` | Update internal notes | Admin |
| GET | `/api/v1/admin/orders/export` | Export orders (CSV/Excel) | Admin |

### 4.4 Stripe Webhook

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/webhooks/stripe` | Handle Stripe events |

### 4.5 Request/Response Examples

#### POST /api/v1/checkout/start

**Request:**
```json
{
  "cartId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "sessionToken": "cs_abc123xyz",
  "expiresAt": "2024-01-15T12:00:00Z",
  "currentStep": "shipping",
  "cart": {
    "items": [
      {
        "productId": "uuid",
        "productName": "Split AC 12000 BTU",
        "variantName": "White",
        "quantity": 1,
        "unitPrice": 799.99,
        "totalPrice": 799.99,
        "imageUrl": "/images/products/ac-12000.jpg"
      }
    ],
    "subtotal": 799.99
  },
  "requiresShipping": true
}
```

#### PUT /api/v1/checkout/shipping-address

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "addressLine1": "123 Main Street",
  "addressLine2": "Apt 4B",
  "city": "New York",
  "state": "NY",
  "postalCode": "10001",
  "country": "US",
  "phone": "+1-555-123-4567"
}
```

**Response:**
```json
{
  "success": true,
  "shippingAddress": { ... },
  "availableShippingMethods": [
    {
      "id": "standard",
      "name": "Standard Shipping",
      "description": "5-7 business days",
      "price": 9.99,
      "estimatedDays": { "min": 5, "max": 7 }
    },
    {
      "id": "express",
      "name": "Express Shipping",
      "description": "2-3 business days",
      "price": 24.99,
      "estimatedDays": { "min": 2, "max": 3 }
    }
  ],
  "taxAmount": 71.00,
  "updatedTotal": 880.98
}
```

#### POST /api/v1/checkout/payment-intent

**Request:**
```json
{
  "savePaymentMethod": false
}
```

**Response:**
```json
{
  "clientSecret": "pi_xxx_secret_yyy",
  "paymentIntentId": "pi_xxx",
  "amount": 88098,
  "currency": "usd"
}
```

---

## 5. Implementation Tasks

### Phase 1: Core Infrastructure

#### Task CHK-001: Order Database Schema & Migrations
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Create `orders` table with all fields and constraints
- [ ] Create `order_items` table with foreign keys
- [ ] Create `order_status_history` table
- [ ] Create `checkout_sessions` table
- [ ] Add proper indexes for query optimization
- [ ] Create EF Core entity configurations
- [ ] Generate and test migrations
- [ ] Add seed data for testing

**Implementation Notes:**
```csharp
// Order entity configuration
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.Property(o => o.ShippingAddress).HasColumnType("jsonb");
        builder.Property(o => o.BillingAddress).HasColumnType("jsonb");
        // ... additional configurations
    }
}
```

---

#### Task CHK-002: Order Domain Models
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Create `Order` aggregate root entity
- [ ] Create `OrderItem` entity
- [ ] Create `OrderStatusHistory` entity
- [ ] Create `CheckoutSession` entity
- [ ] Create `Address` value object
- [ ] Create `OrderStatus` enum
- [ ] Create `PaymentStatus` enum
- [ ] Implement domain events for state changes

**Implementation:**
```csharp
public class Order : BaseEntity, IAggregateRoot
{
    public string OrderNumber { get; private set; }
    public Guid? UserId { get; private set; }
    public string? GuestEmail { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; }
    public Address BillingAddress { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void TransitionTo(OrderStatus newStatus, string? notes = null, Guid? changedByUserId = null)
    {
        if (!OrderStatusTransitions.CanTransition(Status, newStatus))
            throw new InvalidOrderStatusTransitionException(Status, newStatus);

        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(new OrderStatusHistory(Id, previousStatus, newStatus, notes, changedByUserId));

        AddDomainEvent(new OrderStatusChangedEvent(this, previousStatus, newStatus));
    }
}
```

---

#### Task CHK-003: Order Number Generator Service
**Priority:** High
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Generate unique, human-readable order numbers
- [ ] Format: `ORD-YYYYMMDD-XXXXX` (e.g., `ORD-20240115-00001`)
- [ ] Thread-safe implementation
- [ ] Handle high concurrency scenarios
- [ ] Add daily counter reset logic

**Implementation:**
```csharp
public interface IOrderNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}

public class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly IDistributedCache _cache;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var key = $"order_counter:{today}";

        var counter = await _cache.IncrementAsync(key);
        if (counter == 1)
        {
            await _cache.SetExpirationAsync(key, TimeSpan.FromDays(2));
        }

        return $"ORD-{today}-{counter:D5}";
    }
}
```

---

### Phase 2: Checkout Flow

#### Task CHK-004: Checkout Session Service
**Priority:** High
**Estimate:** 6 hours

**Acceptance Criteria:**
- [ ] Initialize checkout from cart
- [ ] Validate cart items and stock availability
- [ ] Calculate initial pricing (subtotal)
- [ ] Generate secure session token
- [ ] Set session expiration (30 minutes default)
- [ ] Handle session retrieval and updates
- [ ] Support both authenticated and guest users
- [ ] Merge guest session on login

**Implementation:**
```csharp
public class CheckoutService : ICheckoutService
{
    public async Task<CheckoutSession> StartCheckoutAsync(
        Guid cartId,
        Guid? userId,
        CancellationToken ct)
    {
        var cart = await _cartRepository.GetByIdAsync(cartId, ct)
            ?? throw new CartNotFoundException(cartId);

        // Validate stock
        await ValidateStockAsync(cart.Items, ct);

        // Create session
        var session = new CheckoutSession
        {
            SessionToken = GenerateSecureToken(),
            CartId = cartId,
            UserId = userId,
            CurrentStep = CheckoutStep.Shipping,
            Subtotal = cart.Items.Sum(i => i.TotalPrice),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        await _sessionRepository.AddAsync(session, ct);
        return session;
    }
}
```

---

#### Task CHK-005: Shipping Address Handler
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Validate address format
- [ ] Support international addresses
- [ ] Auto-populate for logged-in users
- [ ] Calculate available shipping methods based on address
- [ ] Calculate tax based on shipping address
- [ ] Store address in session

**Implementation:**
```csharp
public record SetShippingAddressCommand(
    string SessionToken,
    AddressDto Address
) : IRequest<ShippingAddressResult>;

public class SetShippingAddressHandler : IRequestHandler<SetShippingAddressCommand, ShippingAddressResult>
{
    public async Task<ShippingAddressResult> Handle(
        SetShippingAddressCommand request,
        CancellationToken ct)
    {
        var session = await GetValidSessionAsync(request.SessionToken, ct);

        // Validate address
        var validation = await _addressValidator.ValidateAsync(request.Address, ct);
        if (!validation.IsValid)
            throw new InvalidAddressException(validation.Errors);

        // Update session
        session.ShippingAddress = request.Address.ToDomain();

        // Calculate shipping options
        var shippingMethods = await _shippingService.GetMethodsAsync(
            session.ShippingAddress,
            session.CartItems,
            ct);

        // Calculate tax
        session.TaxAmount = await _taxService.CalculateAsync(
            session.ShippingAddress,
            session.Subtotal,
            ct);

        await _sessionRepository.UpdateAsync(session, ct);

        return new ShippingAddressResult(
            session.ShippingAddress,
            shippingMethods,
            session.TaxAmount,
            session.CalculateTotal());
    }
}
```

---

#### Task CHK-006: Shipping Method Selection
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] List available shipping methods with prices
- [ ] Calculate estimated delivery dates
- [ ] Apply shipping method to session
- [ ] Recalculate order total
- [ ] Handle free shipping thresholds

**Shipping Methods:**
```csharp
public record ShippingMethod(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int MinDays,
    int MaxDays,
    bool IsDefault
);

// Available methods
public static readonly ShippingMethod[] StandardMethods = new[]
{
    new ShippingMethod("standard", "Standard Shipping", "5-7 business days", 9.99m, 5, 7, true),
    new ShippingMethod("express", "Express Shipping", "2-3 business days", 24.99m, 2, 3, false),
    new ShippingMethod("overnight", "Overnight Shipping", "Next business day", 49.99m, 1, 1, false)
};
```

---

#### Task CHK-007: Billing Address Handler
**Priority:** High
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Option to use shipping address for billing
- [ ] Separate billing address form
- [ ] Validate billing address format
- [ ] Store in session

---

#### Task CHK-008: Tax Calculation Service
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Calculate tax based on shipping address
- [ ] Support US state-based tax rates
- [ ] Support tax-exempt products
- [ ] Store tax rate used for audit
- [ ] Integration-ready for external tax services (TaxJar, Avalara)

**Implementation:**
```csharp
public interface ITaxCalculator
{
    Task<TaxCalculationResult> CalculateAsync(
        Address shippingAddress,
        IEnumerable<TaxableItem> items,
        CancellationToken ct);
}

public record TaxCalculationResult(
    decimal TaxAmount,
    decimal TaxRate,
    string Jurisdiction,
    IReadOnlyList<TaxLineItem> LineItems
);
```

---

#### Task CHK-009: Discount Code Application
**Priority:** Medium
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Validate discount code
- [ ] Check usage limits and expiration
- [ ] Apply percentage or fixed discounts
- [ ] Support minimum order requirements
- [ ] Recalculate totals
- [ ] Handle one discount per order rule

---

### Phase 3: Stripe Payment Integration

#### Task CHK-010: Stripe Configuration
**Priority:** High
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Configure Stripe SDK
- [ ] Set up API keys (test and production)
- [ ] Configure webhook endpoint
- [ ] Set up webhook signing secret
- [ ] Add Stripe health check

**Configuration:**
```csharp
// appsettings.json
{
  "Stripe": {
    "SecretKey": "sk_test_xxx",
    "PublishableKey": "pk_test_xxx",
    "WebhookSecret": "whsec_xxx",
    "PaymentMethodTypes": ["card"],
    "Currency": "usd"
  }
}

// Program.cs
builder.Services.AddStripe(builder.Configuration);
```

---

#### Task CHK-011: PaymentIntent Creation
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Create PaymentIntent with order amount
- [ ] Include order metadata (order number, customer email)
- [ ] Return client secret for frontend
- [ ] Update PaymentIntent on amount changes
- [ ] Handle currency correctly (cents for USD)

**Implementation:**
```csharp
public class StripePaymentService : IPaymentService
{
    private readonly PaymentIntentService _paymentIntentService;

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        CheckoutSession session,
        CancellationToken ct)
    {
        var amountInCents = (long)(session.Total * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "usd",
            PaymentMethodTypes = new List<string> { "card" },
            Metadata = new Dictionary<string, string>
            {
                ["session_id"] = session.Id.ToString(),
                ["customer_email"] = session.GuestEmail ?? session.User?.Email
            },
            Description = $"ClimaSite Order",
            ReceiptEmail = session.GuestEmail ?? session.User?.Email
        };

        var paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: ct);

        session.StripePaymentIntentId = paymentIntent.Id;
        session.StripeClientSecret = paymentIntent.ClientSecret;

        return new PaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.ClientSecret,
            amountInCents,
            "usd");
    }
}
```

---

#### Task CHK-012: Stripe Webhook Handler
**Priority:** High
**Estimate:** 6 hours

**Acceptance Criteria:**
- [ ] Verify webhook signatures
- [ ] Handle `payment_intent.succeeded` event
- [ ] Handle `payment_intent.payment_failed` event
- [ ] Handle `charge.refunded` event
- [ ] Idempotent event processing
- [ ] Log all webhook events
- [ ] Proper error handling and retry logic

**Implementation:**
```csharp
[ApiController]
[Route("api/v1/webhooks")]
public class StripeWebhookController : ControllerBase
{
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _webhookSecret,
                throwOnApiVersionMismatch: false);

            // Idempotency check
            if (await _eventStore.ExistsAsync(stripeEvent.Id))
            {
                return Ok();
            }

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    await HandlePaymentSucceeded(stripeEvent);
                    break;
                case Events.PaymentIntentPaymentFailed:
                    await HandlePaymentFailed(stripeEvent);
                    break;
                case Events.ChargeRefunded:
                    await HandleRefund(stripeEvent);
                    break;
            }

            await _eventStore.SaveAsync(stripeEvent.Id);
            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
    }

    private async Task HandlePaymentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

        await _mediator.Send(new CompleteOrderPaymentCommand(
            paymentIntent.Id,
            paymentIntent.LatestChargeId));
    }
}
```

---

#### Task CHK-013: Order Completion Handler
**Priority:** High
**Estimate:** 5 hours

**Acceptance Criteria:**
- [ ] Create order from checkout session
- [ ] Generate order number
- [ ] Reserve/deduct inventory
- [ ] Create order items with price snapshots
- [ ] Set initial status history
- [ ] Clear cart after successful order
- [ ] Send order confirmation email
- [ ] Handle partial payment scenarios

**Implementation:**
```csharp
public class CompleteOrderHandler : IRequestHandler<CompleteOrderCommand, OrderResult>
{
    public async Task<OrderResult> Handle(
        CompleteOrderCommand request,
        CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var session = await GetValidSessionAsync(request.SessionToken, ct);

            // Verify payment
            var paymentIntent = await _stripe.PaymentIntents.GetAsync(
                session.StripePaymentIntentId,
                cancellationToken: ct);

            if (paymentIntent.Status != "succeeded")
                throw new PaymentNotCompletedException();

            // Create order
            var order = new Order(
                orderNumber: await _orderNumberGenerator.GenerateAsync(ct),
                userId: session.UserId,
                guestEmail: session.GuestEmail,
                shippingAddress: session.ShippingAddress,
                billingAddress: session.BillingAddress ?? session.ShippingAddress,
                subtotal: session.Subtotal,
                shippingCost: session.ShippingCost,
                taxAmount: session.TaxAmount,
                discountAmount: session.DiscountAmount,
                total: session.Total,
                stripePaymentIntentId: session.StripePaymentIntentId);

            // Add items
            foreach (var item in session.CartItems)
            {
                order.AddItem(
                    item.ProductId,
                    item.VariantId,
                    item.ProductName,
                    item.VariantName,
                    item.Quantity,
                    item.UnitPrice,
                    item.ImageUrl);

                // Reserve inventory
                await _inventoryService.ReserveAsync(item.ProductId, item.VariantId, item.Quantity, ct);
            }

            // Set as paid
            order.TransitionTo(OrderStatus.Paid, "Payment confirmed via Stripe");

            await _orderRepository.AddAsync(order, ct);

            // Mark session complete
            session.Complete();

            // Clear cart
            await _cartService.ClearAsync(session.CartId, ct);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Publish events (for email, etc.)
            await _mediator.Publish(new OrderCreatedEvent(order), ct);

            return new OrderResult(order.OrderNumber, order.Total);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

### Phase 4: Order Management

#### Task CHK-014: Order Repository
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] CRUD operations for orders
- [ ] Query orders by user
- [ ] Query orders by status
- [ ] Query by date range
- [ ] Include items and status history
- [ ] Search by order number

---

#### Task CHK-015: Order Query Service
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] List orders with pagination
- [ ] Filter by status, date range, customer
- [ ] Sort by date, total, status
- [ ] Full-text search on order number
- [ ] Aggregate statistics

**Implementation:**
```csharp
public record OrderListQuery(
    int Page = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    string SortBy = "createdAt",
    bool Descending = true
) : IRequest<PagedResult<OrderSummaryDto>>;
```

---

#### Task CHK-016: Order Status Update Handler
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Validate status transitions
- [ ] Record status history with notes
- [ ] Trigger appropriate events
- [ ] Send customer notifications

**Implementation:**
```csharp
public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, Unit>
{
    public async Task<Unit> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(request.OrderNumber, ct)
            ?? throw new OrderNotFoundException(request.OrderNumber);

        order.TransitionTo(request.NewStatus, request.Notes, request.AdminUserId);

        await _orderRepository.UpdateAsync(order, ct);

        // Send notification
        await _mediator.Publish(new OrderStatusChangedEvent(order, request.NewStatus), ct);

        return Unit.Value;
    }
}
```

---

#### Task CHK-017: Order Cancellation Handler
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Validate order can be cancelled
- [ ] Release reserved inventory
- [ ] Process refund if paid
- [ ] Update order status
- [ ] Send cancellation email

---

#### Task CHK-018: Refund Handler
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Process full refund via Stripe
- [ ] Process partial refund
- [ ] Update order status
- [ ] Record refund details
- [ ] Send refund confirmation email

**Implementation:**
```csharp
public class ProcessRefundHandler : IRequestHandler<ProcessRefundCommand, RefundResult>
{
    public async Task<RefundResult> Handle(ProcessRefundCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(request.OrderNumber, ct);

        if (order.PaymentStatus != PaymentStatus.Succeeded)
            throw new CannotRefundUnpaidOrderException();

        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = order.StripePaymentIntentId,
            Amount = request.Amount.HasValue ? (long)(request.Amount.Value * 100) : null,
            Reason = RefundReasons.RequestedByCustomer
        };

        var refund = await _refundService.CreateAsync(refundOptions, cancellationToken: ct);

        order.RecordRefund(refund.Amount / 100m, refund.Id);

        if (request.Amount == null || request.Amount >= order.Total)
        {
            order.TransitionTo(OrderStatus.Refunded);
        }

        await _orderRepository.UpdateAsync(order, ct);

        return new RefundResult(refund.Id, refund.Amount / 100m);
    }
}
```

---

#### Task CHK-019: Tracking Number Handler
**Priority:** Medium
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Add tracking number and carrier
- [ ] Set estimated delivery date
- [ ] Update status to shipped
- [ ] Send shipping notification email

---

### Phase 5: Frontend Implementation

#### Task CHK-020: Checkout State Management
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Create checkout state service using Angular signals
- [ ] Track current step
- [ ] Store form data across steps
- [ ] Handle session token storage
- [ ] Sync with backend session

**Implementation:**
```typescript
// checkout.state.ts
import { Injectable, signal, computed } from '@angular/core';

export interface CheckoutState {
  sessionToken: string | null;
  currentStep: 'shipping' | 'payment' | 'review' | 'confirmation';
  shippingAddress: Address | null;
  billingAddress: Address | null;
  sameBillingShipping: boolean;
  shippingMethod: ShippingMethod | null;
  discountCode: string | null;
  subtotal: number;
  shippingCost: number;
  taxAmount: number;
  discountAmount: number;
  total: number;
  stripeClientSecret: string | null;
  isLoading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class CheckoutStateService {
  private readonly state = signal<CheckoutState>({
    sessionToken: null,
    currentStep: 'shipping',
    shippingAddress: null,
    billingAddress: null,
    sameBillingShipping: true,
    shippingMethod: null,
    discountCode: null,
    subtotal: 0,
    shippingCost: 0,
    taxAmount: 0,
    discountAmount: 0,
    total: 0,
    stripeClientSecret: null,
    isLoading: false,
    error: null
  });

  readonly sessionToken = computed(() => this.state().sessionToken);
  readonly currentStep = computed(() => this.state().currentStep);
  readonly total = computed(() => this.state().total);
  readonly canProceed = computed(() => this.validateCurrentStep());

  setShippingAddress(address: Address) {
    this.state.update(s => ({ ...s, shippingAddress: address }));
  }

  nextStep() {
    const steps = ['shipping', 'payment', 'review', 'confirmation'] as const;
    const currentIndex = steps.indexOf(this.state().currentStep);
    if (currentIndex < steps.length - 1) {
      this.state.update(s => ({ ...s, currentStep: steps[currentIndex + 1] }));
    }
  }
}
```

---

#### Task CHK-021: Checkout Routing
**Priority:** High
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Create checkout route with child routes
- [ ] Implement step guards (prevent skipping)
- [ ] Redirect to appropriate step based on session
- [ ] Handle session expiration

**Implementation:**
```typescript
// checkout.routes.ts
import { Routes } from '@angular/router';
import { checkoutGuard, stepGuard } from './guards';

export const checkoutRoutes: Routes = [
  {
    path: '',
    component: CheckoutLayoutComponent,
    canActivate: [checkoutGuard],
    children: [
      { path: '', redirectTo: 'shipping', pathMatch: 'full' },
      {
        path: 'shipping',
        component: ShippingStepComponent,
        canActivate: [stepGuard('shipping')]
      },
      {
        path: 'payment',
        component: PaymentStepComponent,
        canActivate: [stepGuard('payment')]
      },
      {
        path: 'review',
        component: ReviewStepComponent,
        canActivate: [stepGuard('review')]
      },
      {
        path: 'confirmation/:orderNumber',
        component: ConfirmationComponent
      }
    ]
  }
];
```

---

#### Task CHK-022: Checkout Layout Component
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Progress indicator showing current step
- [ ] Order summary sidebar
- [ ] Responsive layout (mobile/desktop)
- [ ] Step navigation
- [ ] Session timer display

**Implementation:**
```typescript
// checkout-layout.component.ts
@Component({
  selector: 'app-checkout-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, CheckoutProgressComponent, OrderSummaryComponent],
  template: `
    <div class="checkout-container">
      <div class="checkout-main">
        <app-checkout-progress [currentStep]="checkoutState.currentStep()" />
        <router-outlet />
      </div>
      <aside class="checkout-sidebar">
        <app-order-summary
          [items]="checkoutState.items()"
          [subtotal]="checkoutState.subtotal()"
          [shipping]="checkoutState.shippingCost()"
          [tax]="checkoutState.taxAmount()"
          [discount]="checkoutState.discountAmount()"
          [total]="checkoutState.total()"
        />
      </aside>
    </div>
  `
})
export class CheckoutLayoutComponent {
  readonly checkoutState = inject(CheckoutStateService);
}
```

---

#### Task CHK-023: Shipping Step Component
**Priority:** High
**Estimate:** 5 hours

**Acceptance Criteria:**
- [ ] Address form with validation
- [ ] Auto-fill for logged-in users
- [ ] Address autocomplete (optional)
- [ ] Country/state dropdowns
- [ ] Guest email input
- [ ] Shipping method selection after address
- [ ] Continue button to payment step

**Implementation:**
```typescript
// shipping-step.component.ts
@Component({
  selector: 'app-shipping-step',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AddressFormComponent, ShippingMethodsComponent],
  template: `
    <div class="shipping-step">
      <h2>Shipping Information</h2>

      @if (!authService.isAuthenticated()) {
        <div class="guest-info">
          <mat-form-field>
            <mat-label>Email Address</mat-label>
            <input matInput type="email" [formControl]="emailControl"
                   data-testid="guest-email-input" />
            <mat-error>Valid email required for order updates</mat-error>
          </mat-form-field>
        </div>
      }

      <app-address-form
        [address]="checkoutState.shippingAddress()"
        (addressChange)="onAddressChange($event)"
        [savedAddresses]="savedAddresses()"
      />

      @if (shippingMethods().length > 0) {
        <app-shipping-methods
          [methods]="shippingMethods()"
          [selected]="checkoutState.shippingMethod()"
          (methodSelect)="onMethodSelect($event)"
        />
      }

      <div class="step-actions">
        <button mat-button routerLink="/cart">Back to Cart</button>
        <button mat-raised-button color="primary"
                [disabled]="!canContinue()"
                (click)="continue()"
                data-testid="continue-to-payment">
          Continue to Payment
        </button>
      </div>
    </div>
  `
})
export class ShippingStepComponent {
  readonly checkoutState = inject(CheckoutStateService);
  readonly authService = inject(AuthService);
  readonly checkoutApi = inject(CheckoutApiService);

  readonly shippingMethods = signal<ShippingMethod[]>([]);
  readonly savedAddresses = signal<Address[]>([]);

  emailControl = new FormControl('', [Validators.required, Validators.email]);

  readonly canContinue = computed(() =>
    this.checkoutState.shippingAddress() !== null &&
    this.checkoutState.shippingMethod() !== null &&
    (this.authService.isAuthenticated() || this.emailControl.valid)
  );

  async onAddressChange(address: Address) {
    this.checkoutState.setShippingAddress(address);

    // Fetch shipping methods for this address
    const response = await this.checkoutApi.setShippingAddress(
      this.checkoutState.sessionToken()!,
      address
    );

    this.shippingMethods.set(response.availableShippingMethods);
    this.checkoutState.updateTotals(response);
  }

  async continue() {
    if (!this.authService.isAuthenticated()) {
      await this.checkoutApi.setGuestInfo(
        this.checkoutState.sessionToken()!,
        this.emailControl.value!
      );
    }

    this.checkoutState.nextStep();
    this.router.navigate(['/checkout/payment']);
  }
}
```

---

#### Task CHK-024: Address Form Component
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Reusable address form component
- [ ] All address fields with validation
- [ ] Country selector with ISO codes
- [ ] Dynamic state/province based on country
- [ ] Phone number with country code
- [ ] Accessibility support

---

#### Task CHK-025: Payment Step Component
**Priority:** High
**Estimate:** 6 hours

**Acceptance Criteria:**
- [ ] Stripe Payment Element integration
- [ ] Billing address option (same as shipping or different)
- [ ] Payment method selection
- [ ] Error handling for declined cards
- [ ] Loading states during payment processing
- [ ] Apply discount code option

**Implementation:**
```typescript
// payment-step.component.ts
import { loadStripe, Stripe, StripeElements } from '@stripe/stripe-js';

@Component({
  selector: 'app-payment-step',
  standalone: true,
  template: `
    <div class="payment-step">
      <h2>Payment Method</h2>

      <!-- Billing Address -->
      <div class="billing-section">
        <mat-checkbox [(ngModel)]="sameBillingShipping" data-testid="same-billing">
          Billing address same as shipping
        </mat-checkbox>

        @if (!sameBillingShipping) {
          <app-address-form
            [address]="checkoutState.billingAddress()"
            (addressChange)="onBillingAddressChange($event)"
          />
        }
      </div>

      <!-- Discount Code -->
      <div class="discount-section">
        <mat-form-field>
          <mat-label>Discount Code</mat-label>
          <input matInput [formControl]="discountControl" data-testid="discount-code" />
        </mat-form-field>
        <button mat-button (click)="applyDiscount()" data-testid="apply-discount">
          Apply
        </button>
      </div>

      <!-- Stripe Payment Element -->
      <div id="payment-element" data-testid="stripe-payment-element"></div>

      @if (paymentError()) {
        <mat-error class="payment-error">{{ paymentError() }}</mat-error>
      }

      <div class="step-actions">
        <button mat-button (click)="back()">Back</button>
        <button mat-raised-button color="primary"
                [disabled]="isProcessing()"
                (click)="continue()"
                data-testid="continue-to-review">
          @if (isProcessing()) {
            <mat-spinner diameter="20" />
          } @else {
            Continue to Review
          }
        </button>
      </div>
    </div>
  `
})
export class PaymentStepComponent implements OnInit {
  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;

  readonly isProcessing = signal(false);
  readonly paymentError = signal<string | null>(null);
  sameBillingShipping = true;

  async ngOnInit() {
    // Initialize Stripe
    this.stripe = await loadStripe(environment.stripePublishableKey);

    // Get client secret
    const { clientSecret } = await this.checkoutApi.createPaymentIntent(
      this.checkoutState.sessionToken()!
    );

    // Mount Payment Element
    this.elements = this.stripe!.elements({
      clientSecret,
      appearance: { theme: 'stripe' }
    });

    const paymentElement = this.elements.create('payment');
    paymentElement.mount('#payment-element');
  }

  async continue() {
    if (!this.stripe || !this.elements) return;

    this.isProcessing.set(true);
    this.paymentError.set(null);

    const { error } = await this.stripe.confirmPayment({
      elements: this.elements,
      confirmParams: {
        return_url: `${window.location.origin}/checkout/review`
      },
      redirect: 'if_required'
    });

    if (error) {
      this.paymentError.set(error.message ?? 'Payment failed');
      this.isProcessing.set(false);
      return;
    }

    // Payment succeeded
    this.checkoutState.nextStep();
    this.router.navigate(['/checkout/review']);
  }
}
```

---

#### Task CHK-026: Review Step Component
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Display complete order summary
- [ ] Show shipping and billing addresses
- [ ] Show payment method (last 4 digits)
- [ ] Edit links to go back to previous steps
- [ ] Place Order button
- [ ] Terms and conditions checkbox

**Implementation:**
```typescript
// review-step.component.ts
@Component({
  selector: 'app-review-step',
  standalone: true,
  template: `
    <div class="review-step">
      <h2>Review Your Order</h2>

      <!-- Order Items -->
      <section class="order-items">
        <h3>Items</h3>
        @for (item of checkoutState.items(); track item.productId) {
          <div class="order-item">
            <img [src]="item.imageUrl" [alt]="item.productName" />
            <div class="item-details">
              <span class="item-name">{{ item.productName }}</span>
              @if (item.variantName) {
                <span class="item-variant">{{ item.variantName }}</span>
              }
              <span class="item-quantity">Qty: {{ item.quantity }}</span>
            </div>
            <span class="item-price">{{ item.totalPrice | currency }}</span>
          </div>
        }
      </section>

      <!-- Addresses -->
      <section class="addresses">
        <div class="address-block">
          <h3>Shipping Address <a routerLink="/checkout/shipping">Edit</a></h3>
          <app-address-display [address]="checkoutState.shippingAddress()" />
        </div>
        <div class="address-block">
          <h3>Billing Address <a routerLink="/checkout/payment">Edit</a></h3>
          <app-address-display [address]="checkoutState.billingAddress() ?? checkoutState.shippingAddress()" />
        </div>
      </section>

      <!-- Shipping Method -->
      <section class="shipping-method">
        <h3>Shipping Method <a routerLink="/checkout/shipping">Edit</a></h3>
        <p>{{ checkoutState.shippingMethod()?.name }} - {{ checkoutState.shippingMethod()?.price | currency }}</p>
      </section>

      <!-- Order Totals -->
      <section class="order-totals">
        <div class="total-line">
          <span>Subtotal</span>
          <span>{{ checkoutState.subtotal() | currency }}</span>
        </div>
        <div class="total-line">
          <span>Shipping</span>
          <span>{{ checkoutState.shippingCost() | currency }}</span>
        </div>
        <div class="total-line">
          <span>Tax</span>
          <span>{{ checkoutState.taxAmount() | currency }}</span>
        </div>
        @if (checkoutState.discountAmount() > 0) {
          <div class="total-line discount">
            <span>Discount</span>
            <span>-{{ checkoutState.discountAmount() | currency }}</span>
          </div>
        }
        <div class="total-line grand-total">
          <span>Total</span>
          <span>{{ checkoutState.total() | currency }}</span>
        </div>
      </section>

      <!-- Terms -->
      <mat-checkbox [(ngModel)]="termsAccepted" data-testid="terms-checkbox">
        I agree to the <a href="/terms" target="_blank">Terms and Conditions</a>
      </mat-checkbox>

      <div class="step-actions">
        <button mat-button (click)="back()">Back</button>
        <button mat-raised-button color="primary"
                [disabled]="!termsAccepted || isPlacingOrder()"
                (click)="placeOrder()"
                data-testid="place-order">
          @if (isPlacingOrder()) {
            <mat-spinner diameter="20" />
          } @else {
            Place Order
          }
        </button>
      </div>
    </div>
  `
})
export class ReviewStepComponent {
  termsAccepted = false;
  readonly isPlacingOrder = signal(false);

  async placeOrder() {
    this.isPlacingOrder.set(true);

    try {
      const result = await this.checkoutApi.completeOrder(
        this.checkoutState.sessionToken()!
      );

      this.router.navigate(['/checkout/confirmation', result.orderNumber]);
    } catch (error) {
      // Handle error
      this.isPlacingOrder.set(false);
    }
  }
}
```

---

#### Task CHK-027: Order Confirmation Component
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Thank you message
- [ ] Order number display
- [ ] Order summary
- [ ] Email confirmation notice
- [ ] Continue shopping button
- [ ] Link to order tracking

**Implementation:**
```typescript
// confirmation.component.ts
@Component({
  selector: 'app-confirmation',
  standalone: true,
  template: `
    <div class="confirmation-page">
      <div class="confirmation-icon">
        <mat-icon>check_circle</mat-icon>
      </div>

      <h1>Thank you for your order!</h1>

      <p class="order-number">
        Order Number: <strong>{{ order()?.orderNumber }}</strong>
      </p>

      <p class="confirmation-email">
        A confirmation email has been sent to <strong>{{ order()?.email }}</strong>
      </p>

      @if (order(); as order) {
        <div class="order-summary">
          <h3>Order Summary</h3>

          @for (item of order.items; track item.id) {
            <div class="order-item">
              <span>{{ item.productName }} x {{ item.quantity }}</span>
              <span>{{ item.totalPrice | currency }}</span>
            </div>
          }

          <div class="total">
            <span>Total</span>
            <span>{{ order.total | currency }}</span>
          </div>
        </div>

        <div class="shipping-info">
          <h3>Shipping To</h3>
          <app-address-display [address]="order.shippingAddress" />
        </div>
      }

      <div class="actions">
        <a mat-raised-button color="primary" routerLink="/">
          Continue Shopping
        </a>
        <a mat-button [routerLink]="['/account/orders', order()?.orderNumber]">
          Track Order
        </a>
      </div>
    </div>
  `
})
export class ConfirmationComponent implements OnInit {
  readonly order = signal<OrderDetails | null>(null);

  async ngOnInit() {
    const orderNumber = this.route.snapshot.params['orderNumber'];
    const order = await this.orderService.getOrder(orderNumber);
    this.order.set(order);
  }
}
```

---

#### Task CHK-028: Order History Page
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] List all user orders
- [ ] Pagination
- [ ] Filter by status
- [ ] Order summary cards
- [ ] Link to order details
- [ ] Empty state for no orders

---

#### Task CHK-029: Order Details Page
**Priority:** High
**Estimate:** 4 hours

**Acceptance Criteria:**
- [ ] Complete order information
- [ ] Status timeline
- [ ] Item list
- [ ] Shipping tracking link
- [ ] Reorder button
- [ ] Cancel order button (if applicable)
- [ ] Request return button (if delivered)

---

### Phase 6: Admin Order Management

#### Task CHK-030: Admin Orders List Page
**Priority:** High
**Estimate:** 5 hours

**Acceptance Criteria:**
- [ ] Paginated orders table
- [ ] Filter by status, date range, customer
- [ ] Search by order number
- [ ] Bulk status update
- [ ] Export to CSV
- [ ] Quick status update dropdown

---

#### Task CHK-031: Admin Order Details Page
**Priority:** High
**Estimate:** 5 hours

**Acceptance Criteria:**
- [ ] Complete order information
- [ ] Customer details
- [ ] Edit internal notes
- [ ] Status update with notes
- [ ] Add tracking number
- [ ] Process refund
- [ ] Order timeline
- [ ] Fraud indicators (if any)

---

#### Task CHK-032: Admin Dashboard Order Widgets
**Priority:** Medium
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Recent orders widget
- [ ] Orders by status chart
- [ ] Revenue summary
- [ ] Pending orders alert
- [ ] Order volume trends

---

### Phase 7: Email Notifications

#### Task CHK-033: Order Confirmation Email
**Priority:** High
**Estimate:** 3 hours

**Acceptance Criteria:**
- [ ] Professional email template
- [ ] Order details
- [ ] Item list
- [ ] Shipping address
- [ ] Payment summary
- [ ] Support contact info

---

#### Task CHK-034: Shipping Notification Email
**Priority:** High
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Tracking number and link
- [ ] Carrier information
- [ ] Estimated delivery date
- [ ] Order summary

---

#### Task CHK-035: Order Cancellation Email
**Priority:** Medium
**Estimate:** 2 hours

**Acceptance Criteria:**
- [ ] Cancellation confirmation
- [ ] Refund information (if applicable)
- [ ] Reason for cancellation

---

## 6. E2E Tests (Playwright - NO MOCKING)

All E2E tests use real API calls and create actual data in the test database. No mocking of services or APIs.

### 6.1 Test Setup

```typescript
// tests/e2e/checkout/fixtures.ts
import { test as base, expect } from '@playwright/test';
import { APIRequestContext } from '@playwright/test';

interface TestFixtures {
  testUser: { email: string; password: string; token: string };
  testProduct: { id: string; name: string; slug: string; price: number };
  api: APIRequestContext;
}

export const test = base.extend<TestFixtures>({
  testUser: async ({ request }, use) => {
    const email = `checkout-user-${Date.now()}@test.climasite.com`;
    const password = 'TestPass123!';

    // Register user
    await request.post('/api/v1/auth/register', {
      data: {
        email,
        password,
        firstName: 'Test',
        lastName: 'User'
      }
    });

    // Login to get token
    const loginResponse = await request.post('/api/v1/auth/login', {
      data: { email, password }
    });
    const { token } = await loginResponse.json();

    await use({ email, password, token });

    // Cleanup: Delete user (or mark for cleanup)
  },

  testProduct: async ({ request }, use) => {
    const sku = `E2E-CHK-${Date.now()}`;

    // Create product via admin API
    const response = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: {
        name: 'E2E Test Air Conditioner',
        sku,
        slug: `e2e-test-ac-${Date.now()}`,
        description: 'Test product for E2E checkout testing',
        basePrice: 899.99,
        stockQuantity: 100,
        categoryId: process.env.TEST_CATEGORY_ID,
        isActive: true
      }
    });

    const product = await response.json();
    await use(product);

    // Cleanup: Deactivate product
    await request.delete(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` }
    });
  },

  api: async ({ request }, use) => {
    await use(request);
  }
});

export { expect };
```

### 6.2 Complete Checkout Flow Test

```typescript
// tests/e2e/checkout/complete-checkout.spec.ts
import { test, expect } from './fixtures';

test.describe('Complete Checkout Flow', () => {
  test('authenticated user can complete full checkout with Stripe', async ({
    page,
    testUser,
    testProduct
  }) => {
    // 1. Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');
    await expect(page).toHaveURL('/');

    // 2. Navigate to product and add to cart
    await page.goto(`/products/${testProduct.slug}`);
    await expect(page.getByText(testProduct.name)).toBeVisible();
    await page.click('[data-testid="add-to-cart"]');

    // Wait for cart notification
    await expect(page.getByText('Added to cart')).toBeVisible();

    // 3. Go to checkout
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');
    await expect(page).toHaveURL('/checkout/shipping');

    // 4. Fill shipping address
    await page.fill('[data-testid="address-first-name"]', 'John');
    await page.fill('[data-testid="address-last-name"]', 'Doe');
    await page.fill('[data-testid="address-line1"]', '123 Test Street');
    await page.fill('[data-testid="address-line2"]', 'Suite 456');
    await page.fill('[data-testid="address-city"]', 'Los Angeles');
    await page.selectOption('[data-testid="address-state"]', 'CA');
    await page.fill('[data-testid="address-postal-code"]', '90001');
    await page.selectOption('[data-testid="address-country"]', 'US');
    await page.fill('[data-testid="address-phone"]', '+1-555-123-4567');

    // Wait for shipping methods to load
    await expect(page.locator('[data-testid="shipping-methods"]')).toBeVisible();

    // 5. Select shipping method
    await page.click('[data-testid="shipping-method-standard"]');

    // 6. Continue to payment
    await page.click('[data-testid="continue-to-payment"]');
    await expect(page).toHaveURL('/checkout/payment');

    // 7. Fill Stripe payment form
    // Wait for Stripe iframe to load
    const stripeFrame = page.frameLocator('iframe[name*="__privateStripeFrame"]').first();
    await stripeFrame.locator('[placeholder="Card number"]').fill('4242424242424242');
    await stripeFrame.locator('[placeholder="MM / YY"]').fill('12/30');
    await stripeFrame.locator('[placeholder="CVC"]').fill('123');
    await stripeFrame.locator('[placeholder="ZIP"]').fill('90001');

    // 8. Continue to review
    await page.click('[data-testid="continue-to-review"]');
    await expect(page).toHaveURL('/checkout/review');

    // 9. Verify order summary
    await expect(page.getByText(testProduct.name)).toBeVisible();
    await expect(page.getByText('$899.99')).toBeVisible();
    await expect(page.getByText('123 Test Street')).toBeVisible();
    await expect(page.getByText('Los Angeles, CA 90001')).toBeVisible();

    // 10. Accept terms and place order
    await page.click('[data-testid="terms-checkbox"]');
    await page.click('[data-testid="place-order"]');

    // 11. Verify confirmation page
    await expect(page).toHaveURL(/\/checkout\/confirmation\/ORD-\d{8}-\d{5}/);
    await expect(page.getByText('Thank you for your order!')).toBeVisible();

    // Extract order number for verification
    const orderNumber = page.url().split('/').pop();

    // 12. Verify order exists in database via API
    const orderResponse = await page.request.get(`/api/v1/orders/${orderNumber}`, {
      headers: { Authorization: `Bearer ${testUser.token}` }
    });
    expect(orderResponse.ok()).toBeTruthy();

    const order = await orderResponse.json();
    expect(order.status).toBe('paid');
    expect(order.total).toBeCloseTo(testProduct.price + 9.99, 2); // Price + standard shipping
  });
});
```

### 6.3 Guest Checkout Test

```typescript
// tests/e2e/checkout/guest-checkout.spec.ts
import { test, expect } from './fixtures';

test.describe('Guest Checkout', () => {
  test('guest user can complete checkout without account', async ({
    page,
    testProduct
  }) => {
    const guestEmail = `guest-${Date.now()}@test.climasite.com`;

    // 1. Add product to cart (as guest)
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // 2. Go to checkout
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');

    // Should redirect to checkout, not login
    await expect(page).toHaveURL('/checkout/shipping');

    // 3. Should see guest email field
    await expect(page.locator('[data-testid="guest-email-input"]')).toBeVisible();
    await page.fill('[data-testid="guest-email-input"]', guestEmail);

    // 4. Fill shipping address
    await page.fill('[data-testid="address-first-name"]', 'Guest');
    await page.fill('[data-testid="address-last-name"]', 'Customer');
    await page.fill('[data-testid="address-line1"]', '456 Guest Lane');
    await page.fill('[data-testid="address-city"]', 'San Francisco');
    await page.selectOption('[data-testid="address-state"]', 'CA');
    await page.fill('[data-testid="address-postal-code"]', '94102');
    await page.selectOption('[data-testid="address-country"]', 'US');

    // 5. Select express shipping
    await page.click('[data-testid="shipping-method-express"]');
    await page.click('[data-testid="continue-to-payment"]');

    // 6. Fill payment
    const stripeFrame = page.frameLocator('iframe[name*="__privateStripeFrame"]').first();
    await stripeFrame.locator('[placeholder="Card number"]').fill('4242424242424242');
    await stripeFrame.locator('[placeholder="MM / YY"]').fill('12/30');
    await stripeFrame.locator('[placeholder="CVC"]').fill('123');
    await stripeFrame.locator('[placeholder="ZIP"]').fill('94102');

    await page.click('[data-testid="continue-to-review"]');

    // 7. Place order
    await page.click('[data-testid="terms-checkbox"]');
    await page.click('[data-testid="place-order"]');

    // 8. Verify confirmation
    await expect(page).toHaveURL(/\/checkout\/confirmation\/ORD-/);
    await expect(page.getByText('Thank you for your order!')).toBeVisible();
    await expect(page.getByText(guestEmail)).toBeVisible();

    // 9. Verify guest order lookup works
    const orderNumber = page.url().split('/').pop();
    await page.goto(`/orders/lookup`);
    await page.fill('[data-testid="order-number-input"]', orderNumber!);
    await page.fill('[data-testid="email-input"]', guestEmail);
    await page.click('[data-testid="lookup-order-button"]');

    await expect(page.getByText(testProduct.name)).toBeVisible();
  });
});
```

### 6.4 Payment Failure Test

```typescript
// tests/e2e/checkout/payment-failure.spec.ts
import { test, expect } from './fixtures';

test.describe('Payment Failure Handling', () => {
  test('shows error message when card is declined', async ({
    page,
    testUser,
    testProduct
  }) => {
    // Login and add to cart
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');

    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');

    // Fill shipping
    await page.fill('[data-testid="address-first-name"]', 'Test');
    await page.fill('[data-testid="address-last-name"]', 'User');
    await page.fill('[data-testid="address-line1"]', '789 Decline Street');
    await page.fill('[data-testid="address-city"]', 'Chicago');
    await page.selectOption('[data-testid="address-state"]', 'IL');
    await page.fill('[data-testid="address-postal-code"]', '60601');
    await page.selectOption('[data-testid="address-country"]', 'US');
    await page.click('[data-testid="shipping-method-standard"]');
    await page.click('[data-testid="continue-to-payment"]');

    // Use Stripe's declined test card
    const stripeFrame = page.frameLocator('iframe[name*="__privateStripeFrame"]').first();
    await stripeFrame.locator('[placeholder="Card number"]').fill('4000000000000002');
    await stripeFrame.locator('[placeholder="MM / YY"]').fill('12/30');
    await stripeFrame.locator('[placeholder="CVC"]').fill('123');
    await stripeFrame.locator('[placeholder="ZIP"]').fill('60601');

    await page.click('[data-testid="continue-to-review"]');

    // Should show error, stay on payment page
    await expect(page.getByText(/card was declined/i)).toBeVisible();
    await expect(page).toHaveURL('/checkout/payment');
  });

  test('handles insufficient funds card', async ({
    page,
    testUser,
    testProduct
  }) => {
    // Setup checkout...
    // Use card 4000000000009995 (insufficient funds)
    const stripeFrame = page.frameLocator('iframe[name*="__privateStripeFrame"]').first();
    await stripeFrame.locator('[placeholder="Card number"]').fill('4000000000009995');
    // ... rest of payment flow

    await expect(page.getByText(/insufficient funds/i)).toBeVisible();
  });
});
```

### 6.5 Discount Code Test

```typescript
// tests/e2e/checkout/discount-code.spec.ts
import { test, expect } from './fixtures';

test.describe('Discount Codes', () => {
  test('applies valid discount code', async ({ page, testUser, testProduct, api }) => {
    // Create discount code via API
    const discountCode = `E2E-DISCOUNT-${Date.now()}`;
    await api.post('/api/v1/admin/discounts', {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: {
        code: discountCode,
        type: 'percentage',
        value: 10,
        minOrderAmount: 0,
        maxUses: 100,
        expiresAt: new Date(Date.now() + 86400000).toISOString()
      }
    });

    // Login and go through checkout
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');

    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');

    // Fill shipping
    await page.fill('[data-testid="address-first-name"]', 'Test');
    await page.fill('[data-testid="address-last-name"]', 'User');
    await page.fill('[data-testid="address-line1"]', '123 Test St');
    await page.fill('[data-testid="address-city"]', 'Boston');
    await page.selectOption('[data-testid="address-state"]', 'MA');
    await page.fill('[data-testid="address-postal-code"]', '02101');
    await page.selectOption('[data-testid="address-country"]', 'US');
    await page.click('[data-testid="shipping-method-standard"]');
    await page.click('[data-testid="continue-to-payment"]');

    // Apply discount code
    await page.fill('[data-testid="discount-code"]', discountCode);
    await page.click('[data-testid="apply-discount"]');

    // Verify discount applied
    await expect(page.getByText('-$90.00')).toBeVisible(); // 10% of $899.99

    // Verify total updated
    const expectedTotal = 899.99 * 0.9 + 9.99; // Price with 10% off + shipping
    await expect(page.getByTestId('order-total')).toContainText(`$${expectedTotal.toFixed(2)}`);
  });

  test('rejects invalid discount code', async ({ page, testUser, testProduct }) => {
    // Go through checkout...
    await page.fill('[data-testid="discount-code"]', 'INVALID-CODE-XYZ');
    await page.click('[data-testid="apply-discount"]');

    await expect(page.getByText(/invalid discount code/i)).toBeVisible();
  });

  test('rejects expired discount code', async ({ page, testUser, testProduct, api }) => {
    // Create expired discount
    const expiredCode = `EXPIRED-${Date.now()}`;
    await api.post('/api/v1/admin/discounts', {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: {
        code: expiredCode,
        type: 'fixed',
        value: 50,
        expiresAt: new Date(Date.now() - 86400000).toISOString() // Yesterday
      }
    });

    // Try to apply expired code
    await page.fill('[data-testid="discount-code"]', expiredCode);
    await page.click('[data-testid="apply-discount"]');

    await expect(page.getByText(/expired/i)).toBeVisible();
  });
});
```

### 6.6 Order Status Updates Test

```typescript
// tests/e2e/checkout/order-status.spec.ts
import { test, expect } from './fixtures';

test.describe('Order Status Management', () => {
  test('admin can update order status through lifecycle', async ({
    page,
    testUser,
    testProduct,
    api
  }) => {
    // Create order via checkout
    // ... (abbreviated - assume order created with orderNumber)
    const orderNumber = 'ORD-20240115-00001'; // From checkout

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', process.env.ADMIN_EMAIL!);
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD!);
    await page.click('[data-testid="login-button"]');

    // Navigate to order
    await page.goto(`/admin/orders/${orderNumber}`);

    // Verify initial status is 'paid'
    await expect(page.getByTestId('order-status')).toContainText('paid');

    // Update to processing
    await page.click('[data-testid="update-status-button"]');
    await page.selectOption('[data-testid="status-select"]', 'processing');
    await page.fill('[data-testid="status-notes"]', 'Order being prepared');
    await page.click('[data-testid="confirm-status-update"]');

    await expect(page.getByTestId('order-status')).toContainText('processing');

    // Add tracking and ship
    await page.fill('[data-testid="tracking-number"]', '1Z999AA10123456784');
    await page.selectOption('[data-testid="carrier-select"]', 'ups');
    await page.click('[data-testid="mark-shipped"]');

    await expect(page.getByTestId('order-status')).toContainText('shipped');

    // Verify status history
    await expect(page.getByTestId('status-history')).toContainText('paid');
    await expect(page.getByTestId('status-history')).toContainText('processing');
    await expect(page.getByTestId('status-history')).toContainText('shipped');

    // Customer should see updated status
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');

    await page.goto(`/account/orders/${orderNumber}`);
    await expect(page.getByTestId('order-status')).toContainText('shipped');
    await expect(page.getByText('1Z999AA10123456784')).toBeVisible();
  });

  test('customer can cancel pending order', async ({ page, testUser, testProduct }) => {
    // Create order with delayed payment (for testing)
    // Customer cancels before payment completes

    await page.goto(`/account/orders/ORD-PENDING-123`);
    await page.click('[data-testid="cancel-order-button"]');
    await page.click('[data-testid="confirm-cancel"]');

    await expect(page.getByTestId('order-status')).toContainText('cancelled');
  });
});
```

### 6.7 Stock Validation Test

```typescript
// tests/e2e/checkout/stock-validation.spec.ts
import { test, expect } from './fixtures';

test.describe('Stock Validation', () => {
  test('prevents checkout when item out of stock', async ({ page, testUser, api }) => {
    // Create product with limited stock
    const response = await api.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: {
        name: 'Limited Stock AC',
        sku: `LIMITED-${Date.now()}`,
        slug: `limited-ac-${Date.now()}`,
        basePrice: 599.99,
        stockQuantity: 1,
        isActive: true
      }
    });
    const product = await response.json();

    // Login as test user
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');

    // Add to cart
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // Simulate someone else buying the last item
    await api.put(`/api/v1/admin/products/${product.id}/stock`, {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: { quantity: 0 }
    });

    // Try to checkout
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');

    // Should show out of stock error
    await expect(page.getByText(/out of stock/i)).toBeVisible();
    await expect(page.getByText('Limited Stock AC')).toBeVisible();
  });

  test('shows warning when quantity exceeds stock during checkout', async ({
    page,
    testUser,
    api
  }) => {
    // Create product with stock of 3
    const response = await api.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${process.env.ADMIN_TOKEN}` },
      data: {
        name: 'Low Stock Heater',
        sku: `LOW-${Date.now()}`,
        slug: `low-stock-${Date.now()}`,
        basePrice: 399.99,
        stockQuantity: 3,
        isActive: true
      }
    });
    const product = await response.json();

    // Add 5 to cart (more than available)
    await page.goto(`/products/${product.slug}`);
    await page.fill('[data-testid="quantity-input"]', '5');
    await page.click('[data-testid="add-to-cart"]');

    // Try checkout
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="proceed-to-checkout"]');

    // Should show stock warning with adjustment option
    await expect(page.getByText(/only 3 available/i)).toBeVisible();
  });
});
```

### 6.8 Refund Flow Test

```typescript
// tests/e2e/checkout/refund.spec.ts
import { test, expect } from './fixtures';

test.describe('Refund Processing', () => {
  test('admin can process full refund', async ({ page, api }) => {
    // Assume order exists: ORD-PAID-123
    const orderNumber = 'ORD-PAID-123';

    // Login as admin
    await page.goto('/admin/login');
    await page.fill('[data-testid="email-input"]', process.env.ADMIN_EMAIL!);
    await page.fill('[data-testid="password-input"]', process.env.ADMIN_PASSWORD!);
    await page.click('[data-testid="login-button"]');

    // Go to order
    await page.goto(`/admin/orders/${orderNumber}`);

    // Initiate refund
    await page.click('[data-testid="refund-button"]');
    await page.click('[data-testid="full-refund-option"]');
    await page.fill('[data-testid="refund-reason"]', 'Customer requested cancellation');
    await page.click('[data-testid="confirm-refund"]');

    // Wait for Stripe processing
    await expect(page.getByText('Refund processed successfully')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('order-status')).toContainText('refunded');

    // Verify refund amount displayed
    await expect(page.getByTestId('refund-amount')).toBeVisible();
  });

  test('admin can process partial refund', async ({ page }) => {
    const orderNumber = 'ORD-PARTIAL-123';

    await page.goto('/admin/login');
    // ... login

    await page.goto(`/admin/orders/${orderNumber}`);
    await page.click('[data-testid="refund-button"]');
    await page.click('[data-testid="partial-refund-option"]');
    await page.fill('[data-testid="refund-amount"]', '100.00');
    await page.fill('[data-testid="refund-reason"]', 'Partial return');
    await page.click('[data-testid="confirm-refund"]');

    await expect(page.getByText('Refund processed')).toBeVisible();
    // Order should still be in original status, with refund recorded
    await expect(page.getByTestId('refund-history')).toContainText('$100.00');
  });
});
```

---

## 7. Security Considerations

### 7.1 Payment Security

- **PCI Compliance**: Use Stripe Elements/Payment Element (card data never touches our servers)
- **HTTPS Only**: All checkout pages require HTTPS
- **Webhook Verification**: Always verify Stripe webhook signatures
- **Idempotency**: Handle duplicate webhook events gracefully

### 7.2 Session Security

- **Session Tokens**: Use cryptographically secure random tokens
- **Expiration**: Sessions expire after 30 minutes of inactivity
- **IP Binding**: Optional - bind session to IP address
- **CSRF Protection**: Include CSRF tokens in checkout forms

### 7.3 Data Protection

- **Address Encryption**: Encrypt stored addresses at rest
- **PII Handling**: Minimize logging of personal information
- **Order Access**: Users can only view their own orders
- **Admin Audit Log**: Track all admin order modifications

---

## 8. Performance Considerations

### 8.1 Database Optimization

- **Indexes**: On order_number, user_id, status, created_at
- **Partitioning**: Consider partitioning orders table by created_at (monthly)
- **Read Replicas**: Use read replica for order history queries

### 8.2 Caching

- **Session Cache**: Store checkout sessions in Redis
- **Shipping Rates**: Cache shipping calculations (5-minute TTL)
- **Tax Rates**: Cache tax rate lookups (1-hour TTL)

### 8.3 Background Processing

- **Order Confirmation Emails**: Send via background queue
- **Inventory Updates**: Process asynchronously for large orders
- **Webhook Processing**: Queue webhook events for processing

---

## 9. Monitoring & Observability

### 9.1 Key Metrics

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| Checkout Conversion Rate | > 3% | < 2% |
| Payment Success Rate | > 98% | < 95% |
| Average Checkout Time | < 3 min | > 5 min |
| Abandoned Checkout Rate | < 70% | > 80% |
| Order Processing Time | < 1 sec | > 3 sec |

### 9.2 Logging

```csharp
// Structured logging for checkout events
_logger.LogInformation(
    "Checkout session {SessionId} transitioned to {Step} for {CustomerId}",
    session.Id,
    session.CurrentStep,
    session.UserId ?? "guest");

_logger.LogInformation(
    "Order {OrderNumber} created with total {Total} for customer {CustomerId}",
    order.OrderNumber,
    order.Total,
    order.UserId ?? order.GuestEmail);
```

### 9.3 Alerts

- Payment failure rate spike
- Stripe webhook failures
- Checkout session timeout increase
- Order creation failures
- Refund processing failures

---

## 10. Dependencies & Integration Points

| System | Integration | Purpose |
|--------|-------------|---------|
| Stripe | Payment Intents API, Webhooks | Payment processing |
| Email Service | SMTP/SendGrid | Order notifications |
| Inventory Service | Internal API | Stock validation |
| User Service | Internal API | Customer data |
| Cart Service | Internal API | Checkout source |
| Tax Service | Internal/External | Tax calculation |
| Shipping Service | Internal | Shipping rates |

---

## 11. Task Summary

| Phase | Tasks | Priority | Total Estimate |
|-------|-------|----------|----------------|
| Phase 1: Core Infrastructure | CHK-001 to CHK-003 | High | 9 hours |
| Phase 2: Checkout Flow | CHK-004 to CHK-009 | High | 23 hours |
| Phase 3: Stripe Integration | CHK-010 to CHK-013 | High | 17 hours |
| Phase 4: Order Management | CHK-014 to CHK-019 | High | 20 hours |
| Phase 5: Frontend | CHK-020 to CHK-029 | High | 39 hours |
| Phase 6: Admin | CHK-030 to CHK-032 | High | 13 hours |
| Phase 7: Emails | CHK-033 to CHK-035 | Medium | 7 hours |
| **Total** | **35 tasks** | | **128 hours** |

---

## 12. Acceptance Criteria Summary

### Checkout Flow
- [ ] User can initiate checkout from cart
- [ ] Guest checkout available without registration
- [ ] Multi-step process: Shipping -> Payment -> Review -> Confirmation
- [ ] Address validation works correctly
- [ ] Shipping methods display with accurate pricing
- [ ] Tax calculated based on shipping address
- [ ] Discount codes can be applied
- [ ] Stripe Payment Element renders and processes payments
- [ ] Order created with correct totals
- [ ] Confirmation page displays order details
- [ ] Confirmation email sent

### Order Management
- [ ] Orders persist with all details
- [ ] Status transitions follow defined state machine
- [ ] Status history recorded
- [ ] Users can view their order history
- [ ] Users can view order details
- [ ] Admins can manage all orders
- [ ] Admins can update status with notes
- [ ] Admins can add tracking information
- [ ] Admins can process refunds

### E2E Tests
- [ ] Complete checkout flow test passes (real payment)
- [ ] Guest checkout test passes
- [ ] Payment failure handled correctly
- [ ] Discount code application works
- [ ] Stock validation prevents overselling
- [ ] Order status updates work end-to-end
- [ ] Refund processing works
