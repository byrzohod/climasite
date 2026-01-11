# Shopping Cart Implementation Plan

## 1. Overview

The Shopping Cart module provides a complete cart experience for the ClimaSite HVAC e-commerce platform. It supports both guest and authenticated users with seamless cart persistence and merging capabilities.

### Key Features
- **Guest cart support** - Visitors can add items without logging in (stored in Redis)
- **Authenticated cart persistence** - Logged-in users have carts saved to PostgreSQL
- **Cart merging** - Guest cart items automatically merge when user logs in
- **Real-time stock validation** - Prevents adding items beyond available inventory
- **Price snapshot** - Captures unit price at time of adding to cart
- **Session management** - Secure session ID cookie for guest cart association

### Architecture Decision Records
- **Redis for guest carts**: Provides fast access and automatic expiration (7 days)
- **PostgreSQL for user carts**: Ensures durability and supports complex queries
- **Price snapshot**: Unit price stored at add-time to maintain cart integrity during price changes

---

## 2. Database Schema

### 2.1 Shopping Carts Table

```sql
CREATE TABLE shopping_carts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE,
    CONSTRAINT unique_user_cart UNIQUE (user_id),
    CONSTRAINT chk_cart_owner CHECK (
        (user_id IS NOT NULL AND session_id IS NULL) OR
        (user_id IS NULL AND session_id IS NOT NULL)
    )
);

CREATE INDEX idx_shopping_carts_user_id ON shopping_carts(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_shopping_carts_session_id ON shopping_carts(session_id) WHERE session_id IS NOT NULL;
CREATE INDEX idx_shopping_carts_status ON shopping_carts(status);
```

### 2.2 Cart Items Table

```sql
CREATE TABLE cart_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id UUID NOT NULL REFERENCES shopping_carts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    variant_id UUID REFERENCES product_variants(id),
    quantity INT NOT NULL CHECK (quantity > 0 AND quantity <= 100),
    unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),
    added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT unique_cart_product_variant UNIQUE (cart_id, product_id, variant_id)
);

CREATE INDEX idx_cart_items_cart_id ON cart_items(cart_id);
CREATE INDEX idx_cart_items_product_id ON cart_items(product_id);
```

### 2.3 Cart Events Table (Audit Log)

```sql
CREATE TABLE cart_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id UUID NOT NULL REFERENCES shopping_carts(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    event_data JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_cart_events_cart_id ON cart_events(cart_id);
CREATE INDEX idx_cart_events_created_at ON cart_events(created_at);
```

### 2.4 EF Core Entity Configurations

```csharp
// ShoppingCart.cs
public class ShoppingCart
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public CartStatus Status { get; set; } = CartStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public User? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

// CartItem.cs
public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ShoppingCart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
```

---

## 3. API Endpoints

### 3.1 Cart Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/v1/cart` | Get current cart with items | No (uses session/auth) |
| POST | `/api/v1/cart/items` | Add item to cart | No |
| PUT | `/api/v1/cart/items/{itemId}` | Update item quantity | No |
| DELETE | `/api/v1/cart/items/{itemId}` | Remove item from cart | No |
| DELETE | `/api/v1/cart` | Clear all items from cart | No |
| POST | `/api/v1/cart/merge` | Merge guest cart with user cart | Yes |
| POST | `/api/v1/cart/validate` | Validate cart items (stock, prices) | No |
| GET | `/api/v1/cart/summary` | Get cart totals and item count | No |

### 3.2 Request/Response DTOs

```csharp
// AddCartItemRequest.cs
public record AddCartItemRequest(
    Guid ProductId,
    Guid? VariantId,
    int Quantity
);

// UpdateCartItemRequest.cs
public record UpdateCartItemRequest(
    int Quantity
);

// CartResponse.cs
public record CartResponse(
    Guid Id,
    List<CartItemResponse> Items,
    decimal Subtotal,
    decimal EstimatedTax,
    decimal Total,
    int TotalItems,
    DateTime UpdatedAt
);

// CartItemResponse.cs
public record CartItemResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string ProductSlug,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int AvailableStock,
    bool IsAvailable
);

// CartValidationResponse.cs
public record CartValidationResponse(
    bool IsValid,
    List<CartValidationIssue> Issues
);

public record CartValidationIssue(
    Guid ItemId,
    string IssueType,
    string Message,
    int? AvailableQuantity
);
```

### 3.3 API Response Examples

**GET /api/v1/cart**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "items": [
    {
      "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
      "productId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "variantId": null,
      "productName": "Mitsubishi Split System AC 12000 BTU",
      "variantName": null,
      "productSlug": "mitsubishi-split-system-ac-12000-btu",
      "imageUrl": "/images/products/mitsubishi-ac-12k.jpg",
      "quantity": 2,
      "unitPrice": 899.99,
      "lineTotal": 1799.98,
      "availableStock": 15,
      "isAvailable": true
    }
  ],
  "subtotal": 1799.98,
  "estimatedTax": 144.00,
  "total": 1943.98,
  "totalItems": 2,
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

---

## 4. Implementation Tasks

### Task CART-001: Cart Database Schema and Migrations

**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** None

**Description:**
Create EF Core migrations for shopping cart tables with proper constraints and indexes.

**Acceptance Criteria:**
- [ ] `shopping_carts` table created with all columns
- [ ] `cart_items` table with foreign key constraints
- [ ] `cart_events` table for audit logging
- [ ] Check constraint ensures cart has either user_id OR session_id (not both)
- [ ] Unique constraint on user_id (one cart per user)
- [ ] Unique constraint on cart_id + product_id + variant_id combination
- [ ] Indexes on frequently queried columns
- [ ] EF Core entity classes created
- [ ] Entity configurations with proper mappings
- [ ] Migration runs successfully on PostgreSQL

**Implementation Notes:**
```bash
dotnet ef migrations add AddShoppingCartSchema --project src/ClimaSite.Infrastructure
dotnet ef database update
```

---

### Task CART-002: Cart Repository and Service Layer

**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** CART-001

**Description:**
Implement the cart repository and service with core CRUD operations and business logic.

**Acceptance Criteria:**
- [ ] `ICartRepository` interface defined
- [ ] `CartRepository` implements all data access methods
- [ ] `ICartService` interface with business operations
- [ ] `CartService` implements cart logic
- [ ] Get or create cart based on user/session
- [ ] Add item with duplicate handling (sum quantities)
- [ ] Update quantity with validation
- [ ] Remove single item
- [ ] Clear entire cart
- [ ] Calculate subtotal, tax, and total
- [ ] Unit tests for CartService (90%+ coverage)

**Service Interface:**
```csharp
public interface ICartService
{
    Task<CartResponse> GetCartAsync(Guid? userId, string? sessionId);
    Task<CartResponse> AddItemAsync(Guid? userId, string? sessionId, AddCartItemRequest request);
    Task<CartResponse> UpdateItemAsync(Guid? userId, string? sessionId, Guid itemId, UpdateCartItemRequest request);
    Task<CartResponse> RemoveItemAsync(Guid? userId, string? sessionId, Guid itemId);
    Task ClearCartAsync(Guid? userId, string? sessionId);
    Task<CartValidationResponse> ValidateCartAsync(Guid? userId, string? sessionId);
    Task<CartSummaryResponse> GetCartSummaryAsync(Guid? userId, string? sessionId);
}
```

---

### Task CART-003: Real-time Stock Validation

**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** CART-002

**Description:**
Implement stock checking when adding/updating cart items to prevent overselling.

**Acceptance Criteria:**
- [ ] Check product availability before adding to cart
- [ ] Validate quantity against available stock
- [ ] Return clear error messages when stock insufficient
- [ ] Handle concurrent cart updates (optimistic locking)
- [ ] Support variant-level stock checking
- [ ] Cart validation endpoint checks all items
- [ ] Flag unavailable items in cart response
- [ ] Unit tests for stock validation scenarios

**Validation Logic:**
```csharp
public async Task<StockValidationResult> ValidateStockAsync(Guid productId, Guid? variantId, int requestedQuantity)
{
    var availableStock = variantId.HasValue
        ? await _productRepository.GetVariantStockAsync(variantId.Value)
        : await _productRepository.GetProductStockAsync(productId);

    return new StockValidationResult
    {
        IsValid = requestedQuantity <= availableStock,
        AvailableQuantity = availableStock,
        RequestedQuantity = requestedQuantity
    };
}
```

---

### Task CART-004: Guest Cart Redis Storage

**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** CART-002

**Description:**
Implement Redis-based storage for guest shopping carts with session management.

**Acceptance Criteria:**
- [ ] Redis connection configured in DI container
- [ ] `IGuestCartStorage` interface defined
- [ ] `RedisGuestCartStorage` implementation
- [ ] Session ID cookie generation and management
- [ ] Secure, HttpOnly cookie with SameSite=Lax
- [ ] Cart serialization/deserialization with JSON
- [ ] 7-day expiration on guest carts
- [ ] Sliding expiration on cart access
- [ ] Handle Redis connection failures gracefully
- [ ] Unit tests with Redis test container

**Redis Key Structure:**
```
cart:guest:{sessionId}
```

**Implementation:**
```csharp
public class RedisGuestCartStorage : IGuestCartStorage
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _expiration = TimeSpan.FromDays(7);

    public async Task<GuestCart?> GetCartAsync(string sessionId)
    {
        var db = _redis.GetDatabase();
        var data = await db.StringGetAsync($"cart:guest:{sessionId}");
        return data.IsNullOrEmpty ? null : JsonSerializer.Deserialize<GuestCart>(data!);
    }

    public async Task SaveCartAsync(string sessionId, GuestCart cart)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(cart);
        await db.StringSetAsync($"cart:guest:{sessionId}", json, _expiration);
    }
}
```

---

### Task CART-005: Session ID Middleware

**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** CART-004

**Description:**
Create middleware to manage session IDs for guest cart association.

**Acceptance Criteria:**
- [ ] Middleware checks for existing session cookie
- [ ] Generates secure session ID if not present
- [ ] Cookie is HttpOnly, Secure, SameSite=Lax
- [ ] Session ID available via `ISessionIdProvider`
- [ ] Cookie expiration matches Redis cart expiration
- [ ] Works correctly with CORS for SPA
- [ ] Integration tests for cookie handling

**Cookie Configuration:**
```csharp
public class SessionIdMiddleware
{
    private const string CookieName = "ClimaSite.CartSession";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(CookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7)
            });
        }

        context.Items["CartSessionId"] = sessionId;
        await _next(context);
    }
}
```

---

### Task CART-006: Cart Merging Logic

**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** CART-004, CART-005

**Description:**
Implement cart merging when a guest user logs in.

**Acceptance Criteria:**
- [ ] Merge triggered on successful login
- [ ] Guest cart items added to user cart
- [ ] Duplicate products have quantities summed
- [ ] Stock validation during merge
- [ ] Guest cart cleared after successful merge
- [ ] Redis guest cart deleted after merge
- [ ] Merge event logged for audit
- [ ] Handle edge cases (empty carts, conflicts)
- [ ] Unit tests for all merge scenarios

**Merge Algorithm:**
```csharp
public async Task<CartResponse> MergeCartsAsync(Guid userId, string sessionId)
{
    var guestCart = await _guestCartStorage.GetCartAsync(sessionId);
    if (guestCart == null || !guestCart.Items.Any())
        return await GetCartAsync(userId, null);

    var userCart = await GetOrCreateUserCartAsync(userId);

    foreach (var guestItem in guestCart.Items)
    {
        var existingItem = userCart.Items.FirstOrDefault(i =>
            i.ProductId == guestItem.ProductId &&
            i.VariantId == guestItem.VariantId);

        if (existingItem != null)
        {
            // Sum quantities, respecting stock limits
            var newQuantity = Math.Min(
                existingItem.Quantity + guestItem.Quantity,
                await GetAvailableStockAsync(guestItem.ProductId, guestItem.VariantId)
            );
            existingItem.Quantity = newQuantity;
        }
        else
        {
            userCart.Items.Add(guestItem.ToCartItem(userCart.Id));
        }
    }

    await _guestCartStorage.DeleteCartAsync(sessionId);
    await _cartRepository.UpdateAsync(userCart);

    return MapToResponse(userCart);
}
```

---

### Task CART-007: Cart API Controller

**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** CART-002, CART-003, CART-006

**Description:**
Implement the REST API controller for cart operations.

**Acceptance Criteria:**
- [ ] GET `/api/v1/cart` - Returns current cart
- [ ] POST `/api/v1/cart/items` - Adds item with validation
- [ ] PUT `/api/v1/cart/items/{id}` - Updates quantity
- [ ] DELETE `/api/v1/cart/items/{id}` - Removes item
- [ ] DELETE `/api/v1/cart` - Clears cart
- [ ] POST `/api/v1/cart/merge` - Merges guest cart (auth required)
- [ ] POST `/api/v1/cart/validate` - Validates all items
- [ ] GET `/api/v1/cart/summary` - Returns totals only
- [ ] Proper HTTP status codes (200, 201, 400, 404, 409)
- [ ] OpenAPI/Swagger documentation
- [ ] Request validation with FluentValidation
- [ ] Integration tests for all endpoints

**Controller Implementation:**
```csharp
[ApiController]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ISessionIdProvider _sessionIdProvider;

    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        var userId = User.GetUserIdOrNull();
        var sessionId = _sessionIdProvider.GetSessionId();

        var cart = await _cartService.GetCartAsync(userId, sessionId);
        return Ok(cart);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartResponse>> AddItem([FromBody] AddCartItemRequest request)
    {
        var userId = User.GetUserIdOrNull();
        var sessionId = _sessionIdProvider.GetSessionId();

        try
        {
            var cart = await _cartService.AddItemAsync(userId, sessionId, request);
            return CreatedAtAction(nameof(GetCart), cart);
        }
        catch (InsufficientStockException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Insufficient Stock",
                Detail = ex.Message,
                Extensions = { ["availableStock"] = ex.AvailableStock }
            });
        }
    }

    [HttpPost("merge")]
    [Authorize]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> MergeCart()
    {
        var userId = User.GetUserId();
        var sessionId = _sessionIdProvider.GetSessionId();

        var cart = await _cartService.MergeCartsAsync(userId, sessionId);
        return Ok(cart);
    }
}
```

---

### Task CART-008: Cart Event Logging

**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** CART-002

**Description:**
Implement audit logging for cart events to support analytics and debugging.

**Acceptance Criteria:**
- [ ] Log item added events
- [ ] Log item updated events
- [ ] Log item removed events
- [ ] Log cart cleared events
- [ ] Log cart merged events
- [ ] Event data includes before/after state
- [ ] Async logging (non-blocking)
- [ ] Integration with application logging

**Event Types:**
```csharp
public enum CartEventType
{
    ItemAdded,
    ItemUpdated,
    ItemRemoved,
    CartCleared,
    CartMerged,
    CartValidated,
    CartExpired
}
```

---

### Task CART-009: Angular Cart Service

**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** CART-007

**Description:**
Implement the Angular cart service with signals for reactive state management.

**Acceptance Criteria:**
- [ ] `CartService` as singleton (providedIn: 'root')
- [ ] Cart state using Angular signals
- [ ] Computed signals for totals and item count
- [ ] API integration for all cart operations
- [ ] Optimistic UI updates
- [ ] Error handling with rollback
- [ ] Loading state signals
- [ ] Cart refresh on app initialization
- [ ] Unit tests for CartService

**Implementation:**
```typescript
@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);

  // State signals
  private _cart = signal<Cart | null>(null);
  private _loading = signal<boolean>(false);
  private _error = signal<string | null>(null);

  // Public readonly signals
  readonly cart = this._cart.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signals
  readonly items = computed(() => this._cart()?.items ?? []);
  readonly totalItems = computed(() =>
    this.items().reduce((sum, item) => sum + item.quantity, 0)
  );
  readonly subtotal = computed(() =>
    this.items().reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
  );
  readonly isEmpty = computed(() => this.totalItems() === 0);

  constructor() {
    // Initialize cart on service creation
    this.refreshCart();
  }

  async refreshCart(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const cart = await firstValueFrom(
        this.http.get<Cart>('/api/v1/cart')
      );
      this._cart.set(cart);
    } catch (err) {
      this._error.set('Failed to load cart');
      console.error('Cart refresh failed:', err);
    } finally {
      this._loading.set(false);
    }
  }

  async addItem(productId: string, variantId: string | null, quantity: number): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const cart = await firstValueFrom(
        this.http.post<Cart>('/api/v1/cart/items', { productId, variantId, quantity })
      );
      this._cart.set(cart);
    } catch (err: any) {
      if (err.status === 409) {
        this._error.set('Not enough stock available');
      } else {
        this._error.set('Failed to add item to cart');
      }
      throw err;
    } finally {
      this._loading.set(false);
    }
  }

  async updateQuantity(itemId: string, quantity: number): Promise<void> {
    const previousCart = this._cart();

    // Optimistic update
    if (previousCart) {
      const updatedItems = previousCart.items.map(item =>
        item.id === itemId ? { ...item, quantity } : item
      );
      this._cart.set({ ...previousCart, items: updatedItems });
    }

    try {
      const cart = await firstValueFrom(
        this.http.put<Cart>(`/api/v1/cart/items/${itemId}`, { quantity })
      );
      this._cart.set(cart);
    } catch (err) {
      // Rollback on error
      this._cart.set(previousCart);
      this._error.set('Failed to update quantity');
      throw err;
    }
  }

  async removeItem(itemId: string): Promise<void> {
    const previousCart = this._cart();

    // Optimistic update
    if (previousCart) {
      const updatedItems = previousCart.items.filter(item => item.id !== itemId);
      this._cart.set({ ...previousCart, items: updatedItems });
    }

    try {
      const cart = await firstValueFrom(
        this.http.delete<Cart>(`/api/v1/cart/items/${itemId}`)
      );
      this._cart.set(cart);
    } catch (err) {
      // Rollback on error
      this._cart.set(previousCart);
      this._error.set('Failed to remove item');
      throw err;
    }
  }

  async clearCart(): Promise<void> {
    this._loading.set(true);

    try {
      await firstValueFrom(this.http.delete('/api/v1/cart'));
      this._cart.set(null);
    } catch (err) {
      this._error.set('Failed to clear cart');
      throw err;
    } finally {
      this._loading.set(false);
    }
  }

  async mergeGuestCart(): Promise<void> {
    try {
      const cart = await firstValueFrom(
        this.http.post<Cart>('/api/v1/cart/merge', {})
      );
      this._cart.set(cart);
    } catch (err) {
      console.error('Cart merge failed:', err);
    }
  }
}
```

---

### Task CART-010: Mini Cart Component (Header Dropdown)

**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** CART-009

**Description:**
Create a mini cart dropdown component for the site header showing cart summary and quick actions.

**Acceptance Criteria:**
- [ ] Standalone component with OnPush change detection
- [ ] Shows cart icon with item count badge
- [ ] Dropdown displays first 3 items
- [ ] Each item shows image, name, quantity, price
- [ ] Quick quantity adjustment buttons
- [ ] Remove item button
- [ ] Subtotal display
- [ ] "View Cart" and "Checkout" buttons
- [ ] Empty cart state message
- [ ] Loading skeleton during updates
- [ ] Closes on outside click
- [ ] Responsive design (mobile drawer)
- [ ] Unit tests for component

**Component Structure:**
```typescript
@Component({
  selector: 'app-mini-cart',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './mini-cart.component.html',
  styleUrl: './mini-cart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MiniCartComponent {
  cartService = inject(CartService);

  isOpen = signal(false);

  displayedItems = computed(() => this.cartService.items().slice(0, 3));
  remainingItemsCount = computed(() =>
    Math.max(0, this.cartService.items().length - 3)
  );

  toggle(): void {
    this.isOpen.update(v => !v);
  }

  close(): void {
    this.isOpen.set(false);
  }

  async updateQuantity(itemId: string, delta: number): Promise<void> {
    const item = this.cartService.items().find(i => i.id === itemId);
    if (item) {
      const newQuantity = item.quantity + delta;
      if (newQuantity > 0) {
        await this.cartService.updateQuantity(itemId, newQuantity);
      } else {
        await this.cartService.removeItem(itemId);
      }
    }
  }
}
```

---

### Task CART-011: Cart Page Component

**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** CART-009

**Description:**
Create the full cart page with complete item management and checkout preparation.

**Acceptance Criteria:**
- [ ] Standalone component at route `/cart`
- [ ] Displays all cart items with full details
- [ ] Product image, name, variant, unit price
- [ ] Quantity input with +/- buttons
- [ ] Stock availability indicator
- [ ] Line total per item
- [ ] Remove item button with confirmation
- [ ] Cart summary sidebar (subtotal, tax, total)
- [ ] "Continue Shopping" button
- [ ] "Proceed to Checkout" button
- [ ] Empty cart state with CTA
- [ ] Out-of-stock warnings
- [ ] Price change warnings
- [ ] Responsive layout (mobile-first)
- [ ] Loading states
- [ ] Unit tests

**Template Structure:**
```html
<div class="cart-page">
  <h1>Shopping Cart</h1>

  @if (cartService.isEmpty()) {
    <div class="empty-cart">
      <img src="/assets/empty-cart.svg" alt="Empty cart">
      <h2>Your cart is empty</h2>
      <p>Looks like you haven't added any HVAC products yet.</p>
      <a routerLink="/products" class="btn btn-primary">Browse Products</a>
    </div>
  } @else {
    <div class="cart-content">
      <div class="cart-items">
        @for (item of cartService.items(); track item.id) {
          <app-cart-item
            [item]="item"
            (quantityChange)="onQuantityChange($event)"
            (remove)="onRemoveItem($event)" />
        }
      </div>

      <aside class="cart-summary">
        <h2>Order Summary</h2>
        <div class="summary-row">
          <span>Subtotal ({{ cartService.totalItems() }} items)</span>
          <span>{{ cartService.subtotal() | currency }}</span>
        </div>
        <div class="summary-row">
          <span>Estimated Tax</span>
          <span>{{ estimatedTax() | currency }}</span>
        </div>
        <div class="summary-row total">
          <span>Total</span>
          <span>{{ total() | currency }}</span>
        </div>
        <button
          class="btn btn-primary btn-checkout"
          [disabled]="!isValid()"
          routerLink="/checkout">
          Proceed to Checkout
        </button>
      </aside>
    </div>
  }
</div>
```

---

### Task CART-012: Cart Item Component

**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** CART-011

**Description:**
Create a reusable cart item component for displaying individual cart items.

**Acceptance Criteria:**
- [ ] Standalone component with input/output
- [ ] Product thumbnail image
- [ ] Product name with link to PDP
- [ ] Variant details if applicable
- [ ] Unit price display
- [ ] Quantity selector with validation
- [ ] Line total calculation
- [ ] Remove button
- [ ] Stock status indicator
- [ ] Responsive design
- [ ] Unit tests

**Component Interface:**
```typescript
@Component({
  selector: 'app-cart-item',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './cart-item.component.html',
  styleUrl: './cart-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CartItemComponent {
  @Input({ required: true }) item!: CartItem;
  @Output() quantityChange = new EventEmitter<{ itemId: string; quantity: number }>();
  @Output() remove = new EventEmitter<string>();

  lineTotal = computed(() => this.item.unitPrice * this.item.quantity);

  isLowStock = computed(() => this.item.availableStock < 5);
  isOutOfStock = computed(() => this.item.availableStock === 0);
  exceedsStock = computed(() => this.item.quantity > this.item.availableStock);

  onQuantityChange(quantity: number): void {
    if (quantity >= 1 && quantity <= this.item.availableStock) {
      this.quantityChange.emit({ itemId: this.item.id, quantity });
    }
  }

  onRemove(): void {
    this.remove.emit(this.item.id);
  }
}
```

---

### Task CART-013: Add to Cart Button Component

**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** CART-009

**Description:**
Create a reusable "Add to Cart" button component for use on product listings and detail pages.

**Acceptance Criteria:**
- [ ] Standalone component with configurable inputs
- [ ] Product ID and variant ID inputs
- [ ] Quantity input (default: 1)
- [ ] Loading state during API call
- [ ] Success feedback (animation/message)
- [ ] Error handling for stock issues
- [ ] Disabled state for out-of-stock
- [ ] Accessible (ARIA labels)
- [ ] Unit tests

**Component:**
```typescript
@Component({
  selector: 'app-add-to-cart-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      class="add-to-cart-btn"
      [class.loading]="loading()"
      [class.success]="showSuccess()"
      [disabled]="disabled() || loading()"
      (click)="addToCart()"
      data-testid="add-to-cart">
      @if (loading()) {
        <span class="spinner"></span>
        Adding...
      } @else if (showSuccess()) {
        <span class="checkmark"></span>
        Added!
      } @else {
        <span class="cart-icon"></span>
        Add to Cart
      }
    </button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddToCartButtonComponent {
  private cartService = inject(CartService);

  @Input({ required: true }) productId!: string;
  @Input() variantId: string | null = null;
  @Input() quantity = 1;
  @Input() disabled = false;

  loading = signal(false);
  showSuccess = signal(false);

  async addToCart(): Promise<void> {
    this.loading.set(true);

    try {
      await this.cartService.addItem(this.productId, this.variantId, this.quantity);
      this.showSuccess.set(true);
      setTimeout(() => this.showSuccess.set(false), 2000);
    } catch (err) {
      // Error handled by CartService
    } finally {
      this.loading.set(false);
    }
  }
}
```

---

### Task CART-014: Cart Notification Toast

**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** CART-009

**Description:**
Implement toast notifications for cart actions.

**Acceptance Criteria:**
- [ ] Toast service for showing notifications
- [ ] Success toast on item added
- [ ] Warning toast on stock issues
- [ ] Error toast on failures
- [ ] Auto-dismiss after 3 seconds
- [ ] Manual dismiss button
- [ ] Stack multiple toasts
- [ ] Accessible (ARIA live region)
- [ ] Unit tests

---

### Task CART-015: Cart Badge Animation

**Priority:** Low
**Estimated Time:** 2 hours
**Dependencies:** CART-010

**Description:**
Add animations to the cart icon badge when items are added.

**Acceptance Criteria:**
- [ ] Pulse animation on count change
- [ ] Count number increment animation
- [ ] Smooth transitions
- [ ] Respects reduced motion preferences
- [ ] CSS-only implementation (no JS animation library)

---

### Task CART-016: Cart Auto-Login Merge

**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** CART-006, CART-009

**Description:**
Automatically trigger cart merge when user logs in via the Angular app.

**Acceptance Criteria:**
- [ ] Listen to authentication state changes
- [ ] Trigger merge API call on login
- [ ] Update cart state after merge
- [ ] Handle merge errors gracefully
- [ ] Clear session cookie after successful merge
- [ ] Integration tests

**Implementation:**
```typescript
@Injectable({ providedIn: 'root' })
export class AuthCartIntegrationService {
  private authService = inject(AuthService);
  private cartService = inject(CartService);

  constructor() {
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        // User just logged in, merge cart
        this.cartService.mergeGuestCart();
      }
    });
  }
}
```

---

### Task CART-017: Cart Quantity Debounce

**Priority:** Medium
**Estimated Time:** 3 hours
**Dependencies:** CART-012

**Description:**
Implement debounced quantity updates to reduce API calls.

**Acceptance Criteria:**
- [ ] Debounce quantity input changes (300ms)
- [ ] Show pending state during debounce
- [ ] Cancel pending update on component destroy
- [ ] Immediate update on blur
- [ ] Unit tests

---

### Task CART-018: Cart Persistence Check

**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** CART-009

**Description:**
Implement background validation to check cart item availability.

**Acceptance Criteria:**
- [ ] Validate cart on page focus
- [ ] Validate cart on checkout initiation
- [ ] Show warnings for unavailable items
- [ ] Show warnings for price changes
- [ ] Allow user to update cart from validation modal
- [ ] Unit tests

---

### Task CART-019: Cart Cleanup Job

**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** CART-001

**Description:**
Implement background job to clean up expired database carts.

**Acceptance Criteria:**
- [ ] Hosted service for periodic cleanup
- [ ] Remove carts older than 30 days (configurable)
- [ ] Log cleanup statistics
- [ ] Respect active checkout sessions
- [ ] Unit tests

**Implementation:**
```csharp
public class CartCleanupBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var cartRepository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

            var expiredCarts = await cartRepository.GetExpiredCartsAsync(TimeSpan.FromDays(30));
            await cartRepository.DeleteRangeAsync(expiredCarts);

            _logger.LogInformation("Cleaned up {Count} expired carts", expiredCarts.Count);

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

---

### Task CART-020: Cart Analytics Events

**Priority:** Low
**Estimated Time:** 4 hours
**Dependencies:** CART-009

**Description:**
Implement analytics event tracking for cart actions.

**Acceptance Criteria:**
- [ ] Track add to cart events
- [ ] Track remove from cart events
- [ ] Track quantity changes
- [ ] Track cart abandonment
- [ ] Include product details in events
- [ ] Integration with analytics service
- [ ] Unit tests

---

## 5. E2E Tests (Playwright - NO MOCKING)

All E2E tests use real API calls to create test data. No mocking is used to ensure tests validate actual system behavior.

### Test Setup and Utilities

```typescript
// tests/e2e/fixtures/cart-fixtures.ts
import { test as base, expect } from '@playwright/test';
import { APIRequestContext } from '@playwright/test';

interface CartTestFixtures {
  testProduct: TestProduct;
  testUser: TestUser;
  authenticatedPage: Page;
  cartApi: CartApiHelper;
}

interface TestProduct {
  id: string;
  name: string;
  slug: string;
  sku: string;
  price: number;
}

interface TestUser {
  id: string;
  email: string;
  password: string;
}

class CartApiHelper {
  constructor(private request: APIRequestContext) {}

  async createProduct(data: Partial<TestProduct> = {}): Promise<TestProduct> {
    const timestamp = Date.now();
    const response = await this.request.post('/api/v1/admin/products', {
      data: {
        name: data.name ?? `Test AC Unit ${timestamp}`,
        sku: data.sku ?? `SKU-CART-${timestamp}`,
        basePrice: data.price ?? 499.99,
        stockQuantity: 100,
        status: 'active',
        ...data
      }
    });
    expect(response.ok()).toBeTruthy();
    return response.json();
  }

  async createUser(data: Partial<TestUser> = {}): Promise<TestUser> {
    const timestamp = Date.now();
    const email = data.email ?? `cart-test-${timestamp}@example.com`;
    const password = data.password ?? 'TestPass123!';

    const response = await this.request.post('/api/v1/auth/register', {
      data: { email, password, confirmPassword: password }
    });
    expect(response.ok()).toBeTruthy();
    const user = await response.json();
    return { ...user, email, password };
  }

  async getCart(sessionId?: string): Promise<Cart> {
    const headers: Record<string, string> = {};
    if (sessionId) {
      headers['Cookie'] = `ClimaSite.CartSession=${sessionId}`;
    }
    const response = await this.request.get('/api/v1/cart', { headers });
    return response.json();
  }

  async addToCart(productId: string, quantity: number, sessionId?: string): Promise<Cart> {
    const headers: Record<string, string> = {};
    if (sessionId) {
      headers['Cookie'] = `ClimaSite.CartSession=${sessionId}`;
    }
    const response = await this.request.post('/api/v1/cart/items', {
      data: { productId, variantId: null, quantity },
      headers
    });
    return response.json();
  }

  async cleanupProduct(productId: string): Promise<void> {
    await this.request.delete(`/api/v1/admin/products/${productId}`);
  }

  async cleanupUser(userId: string): Promise<void> {
    await this.request.delete(`/api/v1/admin/users/${userId}`);
  }
}

export const test = base.extend<CartTestFixtures>({
  cartApi: async ({ request }, use) => {
    await use(new CartApiHelper(request));
  },

  testProduct: async ({ cartApi }, use) => {
    const product = await cartApi.createProduct();
    await use(product);
    await cartApi.cleanupProduct(product.id);
  },

  testUser: async ({ cartApi }, use) => {
    const user = await cartApi.createUser();
    await use(user);
    await cartApi.cleanupUser(user.id);
  },

  authenticatedPage: async ({ page, testUser }, use) => {
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');
    await page.waitForURL('/');
    await use(page);
  }
});

export { expect };
```

---

### Test CART-E2E-001: Add Product to Cart as Guest

```typescript
// tests/e2e/cart/add-to-cart.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Add to Cart', () => {
  test('guest user can add product to cart', async ({ page, testProduct }) => {
    // Navigate to product page
    await page.goto(`/products/${testProduct.slug}`);

    // Verify product details displayed
    await expect(page.getByRole('heading', { name: testProduct.name })).toBeVisible();
    await expect(page.getByText(`$${testProduct.price}`)).toBeVisible();

    // Add to cart
    await page.click('[data-testid="add-to-cart"]');

    // Verify cart icon updated
    await expect(page.getByTestId('cart-count')).toHaveText('1');

    // Open mini cart and verify item
    await page.click('[data-testid="cart-icon"]');
    await expect(page.getByTestId('mini-cart-dropdown')).toBeVisible();
    await expect(page.getByText(testProduct.name)).toBeVisible();
    await expect(page.getByText(`$${testProduct.price.toFixed(2)}`)).toBeVisible();
  });

  test('guest user can add multiple quantities', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Set quantity to 3
    await page.fill('[data-testid="quantity-input"]', '3');
    await page.click('[data-testid="add-to-cart"]');

    // Verify cart count
    await expect(page.getByTestId('cart-count')).toHaveText('3');

    // Verify in cart page
    await page.goto('/cart');
    await expect(page.getByTestId('item-quantity')).toHaveValue('3');

    const expectedTotal = (testProduct.price * 3).toFixed(2);
    await expect(page.getByTestId('cart-subtotal')).toContainText(expectedTotal);
  });

  test('adding same product twice increases quantity', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Add once
    await page.click('[data-testid="add-to-cart"]');
    await expect(page.getByTestId('cart-count')).toHaveText('1');

    // Add again
    await page.click('[data-testid="add-to-cart"]');
    await expect(page.getByTestId('cart-count')).toHaveText('2');

    // Verify single item with quantity 2
    await page.goto('/cart');
    const cartItems = page.getByTestId('cart-item');
    await expect(cartItems).toHaveCount(1);
    await expect(page.getByTestId('item-quantity')).toHaveValue('2');
  });
});
```

---

### Test CART-E2E-002: Cart Merging on Login

```typescript
// tests/e2e/cart/cart-merge.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Cart Merging', () => {
  test('guest cart merges with empty user cart on login', async ({ page, cartApi, testUser }) => {
    // Create product for this test
    const product = await cartApi.createProduct({
      name: 'Merge Test AC',
      sku: `SKU-MERGE-${Date.now()}`
    });

    try {
      // Add to cart as guest
      await page.goto(`/products/${product.slug}`);
      await page.click('[data-testid="add-to-cart"]');
      await expect(page.getByTestId('cart-count')).toHaveText('1');

      // Login
      await page.goto('/login');
      await page.fill('[data-testid="email-input"]', testUser.email);
      await page.fill('[data-testid="password-input"]', testUser.password);
      await page.click('[data-testid="login-button"]');

      // Wait for redirect and cart merge
      await page.waitForURL('/');

      // Verify cart still has the item
      await expect(page.getByTestId('cart-count')).toHaveText('1');

      // Verify in cart page
      await page.goto('/cart');
      await expect(page.getByText(product.name)).toBeVisible();
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });

  test('guest cart items added to existing user cart items', async ({
    page, cartApi, testUser, authenticatedPage
  }) => {
    // Create two products
    const product1 = await cartApi.createProduct({ name: 'User Cart Product', price: 299.99 });
    const product2 = await cartApi.createProduct({ name: 'Guest Cart Product', price: 399.99 });

    try {
      // Add product1 to user cart while logged in
      await authenticatedPage.goto(`/products/${product1.slug}`);
      await authenticatedPage.click('[data-testid="add-to-cart"]');
      await expect(authenticatedPage.getByTestId('cart-count')).toHaveText('1');

      // Logout
      await authenticatedPage.click('[data-testid="user-menu"]');
      await authenticatedPage.click('[data-testid="logout-button"]');
      await authenticatedPage.waitForURL('/');

      // Add product2 as guest
      await authenticatedPage.goto(`/products/${product2.slug}`);
      await authenticatedPage.click('[data-testid="add-to-cart"]');
      await expect(authenticatedPage.getByTestId('cart-count')).toHaveText('1');

      // Login again
      await authenticatedPage.goto('/login');
      await authenticatedPage.fill('[data-testid="email-input"]', testUser.email);
      await authenticatedPage.fill('[data-testid="password-input"]', testUser.password);
      await authenticatedPage.click('[data-testid="login-button"]');
      await authenticatedPage.waitForURL('/');

      // Verify merged cart has both items
      await expect(authenticatedPage.getByTestId('cart-count')).toHaveText('2');

      await authenticatedPage.goto('/cart');
      await expect(authenticatedPage.getByText(product1.name)).toBeVisible();
      await expect(authenticatedPage.getByText(product2.name)).toBeVisible();
    } finally {
      await cartApi.cleanupProduct(product1.id);
      await cartApi.cleanupProduct(product2.id);
    }
  });

  test('duplicate products have quantities summed on merge', async ({ page, cartApi, testUser }) => {
    const product = await cartApi.createProduct({
      name: 'Duplicate Merge Product',
      price: 199.99
    });

    try {
      // Login and add product with quantity 2
      await page.goto('/login');
      await page.fill('[data-testid="email-input"]', testUser.email);
      await page.fill('[data-testid="password-input"]', testUser.password);
      await page.click('[data-testid="login-button"]');
      await page.waitForURL('/');

      await page.goto(`/products/${product.slug}`);
      await page.fill('[data-testid="quantity-input"]', '2');
      await page.click('[data-testid="add-to-cart"]');

      // Logout
      await page.click('[data-testid="user-menu"]');
      await page.click('[data-testid="logout-button"]');

      // Add same product as guest with quantity 3
      await page.goto(`/products/${product.slug}`);
      await page.fill('[data-testid="quantity-input"]', '3');
      await page.click('[data-testid="add-to-cart"]');

      // Login again
      await page.goto('/login');
      await page.fill('[data-testid="email-input"]', testUser.email);
      await page.fill('[data-testid="password-input"]', testUser.password);
      await page.click('[data-testid="login-button"]');
      await page.waitForURL('/');

      // Verify total quantity is 5 (2 + 3)
      await expect(page.getByTestId('cart-count')).toHaveText('5');

      await page.goto('/cart');
      await expect(page.getByTestId('item-quantity')).toHaveValue('5');
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });
});
```

---

### Test CART-E2E-003: Update Cart Quantity

```typescript
// tests/e2e/cart/update-quantity.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Update Cart Quantity', () => {
  test('user can increase item quantity', async ({ page, testProduct }) => {
    // Add item to cart
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // Go to cart page
    await page.goto('/cart');

    // Increase quantity
    await page.click('[data-testid="quantity-increase"]');

    // Verify quantity updated
    await expect(page.getByTestId('item-quantity')).toHaveValue('2');
    await expect(page.getByTestId('cart-count')).toHaveText('2');

    // Verify total updated
    const expectedTotal = (testProduct.price * 2).toFixed(2);
    await expect(page.getByTestId('cart-subtotal')).toContainText(expectedTotal);
  });

  test('user can decrease item quantity', async ({ page, testProduct }) => {
    // Add item with quantity 3
    await page.goto(`/products/${testProduct.slug}`);
    await page.fill('[data-testid="quantity-input"]', '3');
    await page.click('[data-testid="add-to-cart"]');

    await page.goto('/cart');

    // Decrease quantity
    await page.click('[data-testid="quantity-decrease"]');

    await expect(page.getByTestId('item-quantity')).toHaveValue('2');
  });

  test('user can type exact quantity', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.goto('/cart');

    // Clear and type new quantity
    await page.fill('[data-testid="item-quantity"]', '5');
    await page.press('[data-testid="item-quantity"]', 'Enter');

    // Wait for update
    await expect(page.getByTestId('cart-count')).toHaveText('5');
  });

  test('quantity cannot be set below 1', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.goto('/cart');

    // Try to decrease below 1
    const decreaseButton = page.getByTestId('quantity-decrease');
    await expect(decreaseButton).toBeDisabled();
  });
});
```

---

### Test CART-E2E-004: Remove Item from Cart

```typescript
// tests/e2e/cart/remove-item.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Remove from Cart', () => {
  test('user can remove item from cart', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.goto('/cart');

    // Click remove button
    await page.click('[data-testid="remove-item"]');

    // Confirm removal if dialog appears
    const confirmDialog = page.getByRole('dialog');
    if (await confirmDialog.isVisible()) {
      await page.click('[data-testid="confirm-remove"]');
    }

    // Verify cart is empty
    await expect(page.getByTestId('empty-cart-message')).toBeVisible();
    await expect(page.getByTestId('cart-count')).toHaveText('0');
  });

  test('user can remove one of multiple items', async ({ page, cartApi }) => {
    const product1 = await cartApi.createProduct({ name: 'Keep This Product' });
    const product2 = await cartApi.createProduct({ name: 'Remove This Product' });

    try {
      // Add both products
      await page.goto(`/products/${product1.slug}`);
      await page.click('[data-testid="add-to-cart"]');

      await page.goto(`/products/${product2.slug}`);
      await page.click('[data-testid="add-to-cart"]');

      await page.goto('/cart');

      // Remove second product
      const removeButtons = page.getByTestId('remove-item');
      await removeButtons.nth(1).click();

      // Verify only first product remains
      await expect(page.getByText(product1.name)).toBeVisible();
      await expect(page.getByText(product2.name)).not.toBeVisible();
      await expect(page.getByTestId('cart-count')).toHaveText('1');
    } finally {
      await cartApi.cleanupProduct(product1.id);
      await cartApi.cleanupProduct(product2.id);
    }
  });

  test('user can remove from mini cart', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // Open mini cart
    await page.click('[data-testid="cart-icon"]');

    // Remove from mini cart
    await page.click('[data-testid="mini-cart-remove-item"]');

    // Verify removed
    await expect(page.getByTestId('cart-count')).toHaveText('0');
    await expect(page.getByTestId('mini-cart-empty')).toBeVisible();
  });
});
```

---

### Test CART-E2E-005: Clear Entire Cart

```typescript
// tests/e2e/cart/clear-cart.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Clear Cart', () => {
  test('user can clear entire cart', async ({ page, cartApi }) => {
    const product1 = await cartApi.createProduct({ name: 'Clear Test 1' });
    const product2 = await cartApi.createProduct({ name: 'Clear Test 2' });

    try {
      // Add multiple products
      await page.goto(`/products/${product1.slug}`);
      await page.click('[data-testid="add-to-cart"]');

      await page.goto(`/products/${product2.slug}`);
      await page.fill('[data-testid="quantity-input"]', '2');
      await page.click('[data-testid="add-to-cart"]');

      await page.goto('/cart');
      await expect(page.getByTestId('cart-count')).toHaveText('3');

      // Clear cart
      await page.click('[data-testid="clear-cart"]');

      // Confirm in dialog
      await page.click('[data-testid="confirm-clear"]');

      // Verify empty
      await expect(page.getByTestId('empty-cart-message')).toBeVisible();
      await expect(page.getByTestId('cart-count')).toHaveText('0');
    } finally {
      await cartApi.cleanupProduct(product1.id);
      await cartApi.cleanupProduct(product2.id);
    }
  });
});
```

---

### Test CART-E2E-006: Stock Validation

```typescript
// tests/e2e/cart/stock-validation.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Stock Validation', () => {
  test('cannot add more than available stock', async ({ page, cartApi }) => {
    // Create product with limited stock
    const product = await cartApi.createProduct({
      name: 'Limited Stock AC',
      stockQuantity: 5
    });

    try {
      await page.goto(`/products/${product.slug}`);

      // Try to add 10 units
      await page.fill('[data-testid="quantity-input"]', '10');
      await page.click('[data-testid="add-to-cart"]');

      // Verify error message
      await expect(page.getByTestId('stock-error')).toBeVisible();
      await expect(page.getByTestId('stock-error')).toContainText('Only 5 available');

      // Cart should not be updated
      await expect(page.getByTestId('cart-count')).toHaveText('0');
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });

  test('cannot increase quantity beyond stock in cart', async ({ page, cartApi }) => {
    const product = await cartApi.createProduct({
      name: 'Stock Limit AC',
      stockQuantity: 3
    });

    try {
      // Add 2 to cart
      await page.goto(`/products/${product.slug}`);
      await page.fill('[data-testid="quantity-input"]', '2');
      await page.click('[data-testid="add-to-cart"]');

      await page.goto('/cart');

      // Increase to 3 (should work)
      await page.click('[data-testid="quantity-increase"]');
      await expect(page.getByTestId('item-quantity')).toHaveValue('3');

      // Try to increase to 4 (should fail)
      await page.click('[data-testid="quantity-increase"]');

      // Verify still at 3 and error shown
      await expect(page.getByTestId('item-quantity')).toHaveValue('3');
      await expect(page.getByTestId('stock-warning')).toBeVisible();
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });

  test('out of stock product shows warning in cart', async ({ page, request, cartApi }) => {
    const product = await cartApi.createProduct({
      name: 'Soon Out of Stock',
      stockQuantity: 2
    });

    try {
      // Add to cart
      await page.goto(`/products/${product.slug}`);
      await page.click('[data-testid="add-to-cart"]');

      // Simulate stock being depleted (by another user/admin)
      await request.patch(`/api/v1/admin/products/${product.id}`, {
        data: { stockQuantity: 0 }
      });

      // Refresh cart page
      await page.goto('/cart');

      // Verify out of stock warning
      await expect(page.getByTestId('item-out-of-stock')).toBeVisible();
      await expect(page.getByTestId('checkout-button')).toBeDisabled();
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });
});
```

---

### Test CART-E2E-007: Cart Persistence

```typescript
// tests/e2e/cart/cart-persistence.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Cart Persistence', () => {
  test('guest cart persists across page refreshes', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // Refresh the page
    await page.reload();

    // Cart should still have the item
    await expect(page.getByTestId('cart-count')).toHaveText('1');

    // Navigate away and back
    await page.goto('/');
    await expect(page.getByTestId('cart-count')).toHaveText('1');
  });

  test('logged in user cart persists across sessions', async ({
    page, testProduct, testUser, browser
  }) => {
    // Login and add to cart
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');
    await page.waitForURL('/');

    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await expect(page.getByTestId('cart-count')).toHaveText('1');

    // Close browser context (simulates closing browser)
    await page.context().close();

    // Open new context and login again
    const newContext = await browser.newContext();
    const newPage = await newContext.newPage();

    await newPage.goto('/login');
    await newPage.fill('[data-testid="email-input"]', testUser.email);
    await newPage.fill('[data-testid="password-input"]', testUser.password);
    await newPage.click('[data-testid="login-button"]');
    await newPage.waitForURL('/');

    // Cart should still have the item
    await expect(newPage.getByTestId('cart-count')).toHaveText('1');

    await newContext.close();
  });
});
```

---

### Test CART-E2E-008: Mini Cart Interactions

```typescript
// tests/e2e/cart/mini-cart.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Mini Cart', () => {
  test('mini cart opens on cart icon click', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.click('[data-testid="cart-icon"]');

    await expect(page.getByTestId('mini-cart-dropdown')).toBeVisible();
    await expect(page.getByText(testProduct.name)).toBeVisible();
  });

  test('mini cart closes on outside click', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.click('[data-testid="cart-icon"]');
    await expect(page.getByTestId('mini-cart-dropdown')).toBeVisible();

    // Click outside
    await page.click('body', { position: { x: 0, y: 0 } });

    await expect(page.getByTestId('mini-cart-dropdown')).not.toBeVisible();
  });

  test('mini cart shows limited items with view all link', async ({ page, cartApi }) => {
    // Create 5 products
    const products = await Promise.all([
      cartApi.createProduct({ name: 'Mini Cart Product 1' }),
      cartApi.createProduct({ name: 'Mini Cart Product 2' }),
      cartApi.createProduct({ name: 'Mini Cart Product 3' }),
      cartApi.createProduct({ name: 'Mini Cart Product 4' }),
      cartApi.createProduct({ name: 'Mini Cart Product 5' })
    ]);

    try {
      // Add all to cart
      for (const product of products) {
        await page.goto(`/products/${product.slug}`);
        await page.click('[data-testid="add-to-cart"]');
      }

      await page.click('[data-testid="cart-icon"]');

      // Should show only 3 items
      const miniCartItems = page.getByTestId('mini-cart-item');
      await expect(miniCartItems).toHaveCount(3);

      // Should show "2 more items" link
      await expect(page.getByText('+2 more items')).toBeVisible();

      // Click view all
      await page.click('[data-testid="view-all-cart"]');
      await expect(page).toHaveURL('/cart');
    } finally {
      for (const product of products) {
        await cartApi.cleanupProduct(product.id);
      }
    }
  });

  test('mini cart quantity buttons work', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.click('[data-testid="cart-icon"]');

    // Increase quantity
    await page.click('[data-testid="mini-cart-quantity-increase"]');
    await expect(page.getByTestId('cart-count')).toHaveText('2');

    // Decrease quantity
    await page.click('[data-testid="mini-cart-quantity-decrease"]');
    await expect(page.getByTestId('cart-count')).toHaveText('1');
  });
});
```

---

### Test CART-E2E-009: Cart with Product Variants

```typescript
// tests/e2e/cart/product-variants.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Cart with Product Variants', () => {
  test('can add different variants as separate items', async ({ page, request }) => {
    // Create product with variants
    const timestamp = Date.now();
    const productResponse = await request.post('/api/v1/admin/products', {
      data: {
        name: `AC with BTU Variants ${timestamp}`,
        sku: `SKU-VAR-${timestamp}`,
        basePrice: 499.99,
        variants: [
          { name: '12000 BTU', sku: `SKU-VAR-12K-${timestamp}`, priceModifier: 0 },
          { name: '18000 BTU', sku: `SKU-VAR-18K-${timestamp}`, priceModifier: 200 }
        ]
      }
    });
    const product = await productResponse.json();

    try {
      await page.goto(`/products/${product.slug}`);

      // Select first variant and add
      await page.click('[data-testid="variant-12000-btu"]');
      await page.click('[data-testid="add-to-cart"]');

      // Select second variant and add
      await page.click('[data-testid="variant-18000-btu"]');
      await page.click('[data-testid="add-to-cart"]');

      // Cart should have 2 items
      await expect(page.getByTestId('cart-count')).toHaveText('2');

      await page.goto('/cart');
      const cartItems = page.getByTestId('cart-item');
      await expect(cartItems).toHaveCount(2);

      // Verify variant names displayed
      await expect(page.getByText('12000 BTU')).toBeVisible();
      await expect(page.getByText('18000 BTU')).toBeVisible();

      // Verify different prices
      await expect(page.getByText('$499.99')).toBeVisible();
      await expect(page.getByText('$699.99')).toBeVisible();
    } finally {
      await request.delete(`/api/v1/admin/products/${product.id}`);
    }
  });
});
```

---

### Test CART-E2E-010: Cart Navigation to Checkout

```typescript
// tests/e2e/cart/checkout-navigation.spec.ts
import { test, expect } from '../fixtures/cart-fixtures';

test.describe('Cart to Checkout Navigation', () => {
  test('proceed to checkout button navigates to checkout', async ({
    page, testProduct, testUser
  }) => {
    // Login first
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', testUser.email);
    await page.fill('[data-testid="password-input"]', testUser.password);
    await page.click('[data-testid="login-button"]');
    await page.waitForURL('/');

    // Add to cart
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    // Go to cart and checkout
    await page.goto('/cart');
    await page.click('[data-testid="checkout-button"]');

    await expect(page).toHaveURL('/checkout');
  });

  test('guest is redirected to login before checkout', async ({ page, testProduct }) => {
    await page.goto(`/products/${testProduct.slug}`);
    await page.click('[data-testid="add-to-cart"]');

    await page.goto('/cart');
    await page.click('[data-testid="checkout-button"]');

    // Should redirect to login with return URL
    await expect(page).toHaveURL(/\/login\?returnUrl=.*checkout/);
  });

  test('checkout button disabled for invalid cart', async ({ page, request, cartApi }) => {
    const product = await cartApi.createProduct({ stockQuantity: 1 });

    try {
      await page.goto(`/products/${product.slug}`);
      await page.click('[data-testid="add-to-cart"]');

      // Deplete stock
      await request.patch(`/api/v1/admin/products/${product.id}`, {
        data: { stockQuantity: 0 }
      });

      await page.goto('/cart');

      // Button should be disabled
      await expect(page.getByTestId('checkout-button')).toBeDisabled();
    } finally {
      await cartApi.cleanupProduct(product.id);
    }
  });
});
```

---

## 6. Performance Considerations

### 6.1 Redis Optimization
- Use Redis pipelining for batch operations
- Implement connection pooling
- Set appropriate memory limits and eviction policies

### 6.2 Database Optimization
- Index on `user_id`, `session_id`, and `product_id`
- Eager load cart items with products in single query
- Use database-level constraints instead of application validation where possible

### 6.3 Frontend Optimization
- Debounce quantity updates (300ms)
- Optimistic UI updates with rollback
- Lazy load cart page component
- Use trackBy in cart item loops
- Cache product images

### 6.4 API Optimization
- Return minimal data in cart responses
- Use ETags for cache validation
- Implement request coalescing for rapid updates

---

## 7. Security Considerations

### 7.1 Session Security
- HttpOnly, Secure, SameSite=Lax cookies
- Session ID rotation on login
- CSRF protection for cart modifications

### 7.2 Input Validation
- Validate product IDs exist before adding
- Validate quantities are positive integers
- Sanitize all user inputs
- Rate limit cart API endpoints

### 7.3 Authorization
- Cart merge requires authenticated user
- Users can only access their own carts
- Admin endpoints require admin role

---

## 8. Monitoring and Observability

### 8.1 Metrics
- Cart abandonment rate
- Average items per cart
- Cart conversion rate
- Redis cache hit/miss ratio

### 8.2 Logging
- All cart modifications logged
- Stock validation failures logged
- Cart merge events logged
- Error tracking for failed operations

### 8.3 Alerts
- Redis connection failures
- High cart abandonment rate
- Stock validation errors spike

---

## 9. Future Enhancements

- **Saved for Later** - Allow users to move items out of cart but save for future
- **Wishlist Integration** - Move items between wishlist and cart
- **Cart Sharing** - Generate shareable cart links
- **Bulk Pricing** - Quantity-based discounts
- **Product Bundles** - HVAC system packages
- **Real-time Inventory** - WebSocket updates for stock changes
- **Abandoned Cart Emails** - Recovery campaigns for logged-in users

---

## 10. Dependencies

### Backend
- StackExchange.Redis (Redis client)
- FluentValidation (Request validation)
- MediatR (CQRS pattern, optional)

### Frontend
- @angular/cdk (Overlay for mini cart dropdown)
- @angular/animations (UI animations)

### Testing
- Testcontainers (Redis integration tests)
- Playwright (E2E tests)

---

## 11. Task Summary

| Task ID | Title | Priority | Est. Hours | Dependencies |
|---------|-------|----------|------------|--------------|
| CART-001 | Database Schema & Migrations | High | 4 | - |
| CART-002 | Cart Repository & Service | High | 8 | CART-001 |
| CART-003 | Real-time Stock Validation | High | 6 | CART-002 |
| CART-004 | Guest Cart Redis Storage | High | 8 | CART-002 |
| CART-005 | Session ID Middleware | High | 4 | CART-004 |
| CART-006 | Cart Merging Logic | High | 8 | CART-004, CART-005 |
| CART-007 | Cart API Controller | High | 6 | CART-002, CART-003, CART-006 |
| CART-008 | Cart Event Logging | Medium | 4 | CART-002 |
| CART-009 | Angular Cart Service | High | 8 | CART-007 |
| CART-010 | Mini Cart Component | High | 6 | CART-009 |
| CART-011 | Cart Page Component | High | 8 | CART-009 |
| CART-012 | Cart Item Component | High | 4 | CART-011 |
| CART-013 | Add to Cart Button | High | 4 | CART-009 |
| CART-014 | Cart Notification Toast | Medium | 4 | CART-009 |
| CART-015 | Cart Badge Animation | Low | 2 | CART-010 |
| CART-016 | Cart Auto-Login Merge | High | 4 | CART-006, CART-009 |
| CART-017 | Cart Quantity Debounce | Medium | 3 | CART-012 |
| CART-018 | Cart Persistence Check | Medium | 4 | CART-009 |
| CART-019 | Cart Cleanup Job | Medium | 4 | CART-001 |
| CART-020 | Cart Analytics Events | Low | 4 | CART-009 |

**Total Estimated Hours:** 103 hours

---

## 12. Acceptance Criteria Summary

The Shopping Cart feature is complete when:

1. Guest users can add products to cart without logging in
2. Cart persists in Redis for 7 days for guest users
3. Logged-in users have carts persisted in PostgreSQL
4. When a guest logs in, their cart merges with their user cart
5. Duplicate products have quantities summed during merge
6. Stock is validated in real-time when adding/updating items
7. Clear error messages shown when stock is insufficient
8. Mini cart dropdown shows cart summary in header
9. Full cart page allows complete item management
10. All E2E tests pass with real data (no mocking)
11. Unit test coverage is 80%+ for cart services
12. API documentation is complete in Swagger
