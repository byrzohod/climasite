# Data Integrity & Domain Rules - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Stock Reservation & Rollback**: Inventory reduction on order creation, restoration on order cancellation
- **Pricing Change Auditing**: ProductPriceHistory entity tracking price changes with reason codes
- **Order Number Generation**: Unique order number generation using timestamp + random suffix pattern
- **Cart Merge Logic**: Guest cart to user cart merging with quantity summing and stock validation
- **Transaction Boundaries**: Explicit database transactions for order creation ensuring atomicity

### API Endpoints
| Endpoint | Method | Description | Transaction Scope |
|----------|--------|-------------|-------------------|
| `/api/orders` | POST | Create order (stock reduction) | Explicit transaction |
| `/api/orders/{id}/cancel` | POST | Cancel order (stock restoration) | SaveChanges only |
| `/api/cart/merge` | POST | Merge guest cart into user cart | SaveChanges only |
| `/api/inventory/{variantId}/adjust` | POST | Adjust stock quantity | SaveChanges only |
| `/api/inventory/bulk-adjust` | POST | Bulk adjust stock | SaveChanges only |
| `/api/admin/products/{id}` | PUT | Update product (price tracking) | SaveChanges only |
| `/api/price-history/{productId}` | GET | Get product price history | Read-only |

### Domain Entities
| Entity | Purpose | Key Constraints |
|--------|---------|-----------------|
| `Order` | Order aggregate root | OrderNumber unique, status transitions validated |
| `OrderItem` | Order line items | Immutable quantity/price at order time |
| `ProductVariant` | Stock tracking | StockQuantity >= 0, throws on negative adjustment |
| `Cart` | Shopping cart | Either UserId OR SessionId required |
| `CartItem` | Cart line items | Mutable quantity, linked to variant |
| `ProductPriceHistory` | Price audit trail | Immutable records with timestamp and reason |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Stock Management** | `src/ClimaSite.Core/Entities/ProductVariant.cs` (AdjustStock method) |
| | `src/ClimaSite.Application/Features/Inventory/Commands/AdjustStockCommand.cs` |
| | `src/ClimaSite.Application/Features/Inventory/Commands/BulkAdjustStockCommand.cs` |
| **Order Creation** | `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs` |
| **Order Cancellation** | `src/ClimaSite.Application/Features/Orders/Commands/CancelOrderCommand.cs` |
| **Cart Merge** | `src/ClimaSite.Application/Features/Cart/Commands/MergeGuestCartCommand.cs` |
| **Price History** | `src/ClimaSite.Core/Entities/ProductPriceHistory.cs` |
| | `src/ClimaSite.Application/Features/PriceHistory/Queries/GetProductPriceHistoryQuery.cs` |
| **Domain Validation** | `src/ClimaSite.Core/Entities/Order.cs` (status transitions) |
| | `src/ClimaSite.Core/Entities/Cart.cs` (cart rules) |

### Infrastructure

| Layer | Files |
|-------|-------|
| **Configurations** | `src/ClimaSite.Infrastructure/Data/Configurations/OrderConfiguration.cs` |
| | `src/ClimaSite.Infrastructure/Data/Configurations/ProductPriceHistoryConfiguration.cs` |
| **Migrations** | `src/ClimaSite.Infrastructure/Data/Migrations/20260113191619_AddProductPriceHistory.cs` |

---

## 3. Test Coverage Audit

### Unit Tests - Domain Entities

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `OrderTests.cs` | `Constructor_WithValidData_CreatesOrder` | Order creation |
| | `SetOrderNumber_WithEmptyValue_ThrowsArgumentException` | Order number validation |
| | `SetStatus_FromPendingToPaid_UpdatesStatus` | Valid status transition |
| | `SetStatus_FromPendingToShipped_ThrowsInvalidOperationException` | Invalid transition blocked |
| | `SetStatus_FromCancelled_ThrowsInvalidOperationException` | Terminal state protection |
| | `CanBeCancelled_WhenPending_ReturnsTrue` | Business rule |
| | `CanBeCancelled_WhenShipped_ReturnsFalse` | Business rule |
| `CartTests.cs` | `Constructor_WithNoIdentifier_ThrowsArgumentException` | Cart identity validation |
| | `SetUser_UpdatesUserIdAndClearsSessionId` | Cart merge prep |
| | `AddItem_ExistingVariant_IncrementsQuantity` | Quantity merging |
| | `UpdateItemQuantity_WithZero_RemovesItem` | Edge case handling |
| | `Clear_RemovesAllItems` | Cart clear operation |

### Unit Tests - Command Handlers

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `CancelOrderCommandTests.cs` | `Handle_RestoresStockWhenCancelled` | **Stock rollback** |
| | `Handle_RestoresStockForMultipleItems` | Multi-item stock rollback |
| | `Handle_WhenOrderIsPending_CancelsSuccessfully` | Cancellation flow |
| | `Handle_WhenOrderIsShipped_CannotCancel` | Status-based protection |
| | `Handle_WhenOrderIsAlreadyCancelled_CannotCancelAgain` | Idempotency |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE** | - | No integration tests for CreateOrderCommand transaction |
| **NONE** | - | No integration tests for MergeGuestCartCommand |
| **NONE** | - | No integration tests for concurrent stock operations |

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `CheckoutTests.cs` | Order creation flow with stock reduction | End-to-end checkout |
| `OrderActionsTests.cs` | `CancelOrder_PendingOrder_CancelsSuccessfully` | Cancel with stock restoration |
| **NONE** | - | No E2E test for cart merge on login |
| **NONE** | - | No E2E test for concurrent order creation |
| **NONE** | - | No E2E test for price history display |

---

## 4. Manual Verification Steps

### Stock Reservation Flow
1. Note initial stock quantity for a product variant
2. Add item to cart and complete checkout
3. Verify stock reduced by order quantity in database
4. Check `ProductVariant.StockQuantity` value decreased

### Stock Rollback Flow
1. Create order for product with known stock (e.g., 10 units, order 3)
2. Verify stock reduced to 7
3. Cancel the order via UI or API
4. Verify stock restored to 10
5. Check `ProductVariant.UpdatedAt` timestamp changed

### Order Number Uniqueness
1. Create multiple orders in rapid succession (< 1 second apart)
2. Verify each order has unique order number
3. Pattern: `ORD-YYYYMMDD-HHMMSS-XXXX` where XXXX is random
4. Check database for duplicate order numbers (should be zero)

### Cart Merge Consistency
1. Open site in incognito (guest cart)
2. Add 2x Product A (variant X) to guest cart
3. Login with user who has 1x Product A (variant X) in cart
4. Verify merged cart has 3x Product A (variant X)
5. Verify quantities capped at available stock
6. Verify guest cart deleted from database

### Cart Merge with Different Products
1. Guest cart: 2x Product A
2. User cart: 1x Product B
3. Login
4. Verify merged cart: 2x Product A + 1x Product B
5. Verify item prices preserved from original carts

### Transaction Boundary Verification
1. Set up scenario where stock validation passes but cart clear would fail
2. Attempt order creation
3. Verify either:
   - Complete success (order created, stock reduced, cart cleared)
   - Complete rollback (no order, original stock, cart intact)
4. No partial state should be possible

### Price History Audit
1. Note current price of a product
2. Update product price via admin panel
3. Query `/api/price-history/{productId}`
4. Verify history shows both old and new prices with timestamps
5. Verify `PriceChangeReason` enum value recorded

---

## 5. Gaps & Risks

### Critical Gaps

| Gap | Risk Level | Description |
|-----|------------|-------------|
| **No explicit transaction in CancelOrderCommand** | HIGH | Stock restoration not atomic with order status change |
| **No transaction in MergeGuestCartCommand** | MEDIUM | Cart merge could leave partial state |
| **No price history creation on product update** | HIGH | `UpdateProductCommand` doesn't create `ProductPriceHistory` records |
| **No concurrency control on stock** | HIGH | No optimistic/pessimistic locking on `ProductVariant.StockQuantity` |
| **Order number collision risk** | LOW | Timestamp + 4-char random suffix could collide under extreme load |

### Missing Test Coverage

| Area | Missing Tests |
|------|---------------|
| **CreateOrderCommand** | Unit tests for transaction rollback on failure |
| **CreateOrderCommand** | Integration tests for concurrent order creation |
| **MergeGuestCartCommand** | Unit tests for merge handler logic |
| **MergeGuestCartCommand** | E2E test for login-triggered merge |
| **ProductPriceHistory** | Unit tests for price history creation |
| **ProductPriceHistory** | Integration tests for price change auditing |
| **Stock operations** | Tests for race conditions / concurrent access |

### Code Quality Issues

| Issue | Location | Impact |
|-------|----------|--------|
| Duplicate MapCartToDto logic | Multiple command handlers | Maintenance burden |
| Hardcoded VAT rate (20%) | `CreateOrderCommand.cs:195`, `MergeGuestCartCommand.cs:168` | Cannot configure per country |
| No isolation level specified | `CreateOrderCommand.cs:139` | Default isolation may allow dirty reads |
| Silent failure on cart merge | Stock = 0 items skipped silently | User not notified of skipped items |

### Business Logic Risks

| Risk | Description | Mitigation Needed |
|------|-------------|-------------------|
| **Double-spend on stock** | Concurrent orders could over-commit stock | Add row-level locking or optimistic concurrency |
| **Lost price audit** | Price changes not recorded to ProductPriceHistory | Wire up UpdateProductCommand to create history |
| **Orphaned guest carts** | No cleanup job for expired guest carts | Implement ICartRepository.CleanupExpiredCartsAsync |
| **Cart merge data loss** | If stock = 0, guest items silently dropped | Show user notification of unavailable items |

---

## 6. Recommended Fixes & Tests

### Priority 0 (Critical)

| Issue | Recommendation |
|-------|----------------|
| **Add transaction to CancelOrderCommand** | Wrap stock restoration + status change in explicit transaction |
| **Wire up price history tracking** | Create ProductPriceHistory record in UpdateProductCommand when BasePrice changes |
| **Add concurrency control** | Use EF Core `RowVersion` on ProductVariant for optimistic concurrency |

### Priority 1 (High)

| Issue | Recommendation |
|-------|----------------|
| **Add transaction to MergeGuestCartCommand** | Wrap merge operations in explicit transaction |
| **Add CreateOrderCommand unit tests** | Test transaction rollback, stock validation, cart clearing |
| **Add MergeGuestCartCommand unit tests** | Test quantity merging, stock capping, guest cart deletion |
| **Add concurrent stock operation tests** | Use parallel test execution to verify race condition handling |
| **Implement cart merge E2E test** | Test full login flow with cart merge verification |

### Priority 2 (Medium)

| Issue | Recommendation |
|-------|----------------|
| **Extract order number generation** | Move to IOrderRepository.GenerateOrderNumberAsync with retry logic |
| **Add isolation level** | Use `IsolationLevel.RepeatableRead` for CreateOrderCommand transaction |
| **User notification on cart merge** | Return list of unavailable/skipped items from MergeGuestCartCommand |
| **Implement guest cart cleanup** | Scheduled job calling ICartRepository.CleanupExpiredCartsAsync |

### Priority 3 (Low)

| Issue | Recommendation |
|-------|----------------|
| **Extract MapCartToDto** | Create shared CartDtoMapper service |
| **Make VAT configurable** | Add store settings or country-based VAT configuration |
| **Price history API security** | Consider if price history should be admin-only |
| **Extend order number format** | Consider adding server instance ID for distributed systems |

### Suggested New Tests

```csharp
// CreateOrderCommandTests.cs
- Handle_WhenStockInsufficient_RollsBackTransaction
- Handle_WhenConcurrentOrders_OnlyOneSucceeds
- Handle_ReducesStockAtomically

// MergeGuestCartCommandTests.cs
- Handle_WithEmptyGuestCart_ReturnsUserCart
- Handle_WithSameProduct_MergesQuantities
- Handle_WithStockLimit_CapsQuantity
- Handle_DeletesGuestCartAfterMerge
- Handle_WhenNoUserCart_CreatesNewCart

// CancelOrderCommandTests.cs (additions)
- Handle_RestoresStockAtomically
- Handle_WhenStockRestorationFails_OrderRemainsUncancelled

// ProductPriceHistoryTests.cs (new file)
- UpdateProduct_WhenPriceChanges_CreatesHistoryRecord
- UpdateProduct_WhenPriceUnchanged_NoHistoryRecord
- GetPriceHistory_ReturnsChronologicalRecords
- GetPriceHistory_CalculatesMinMaxAverage

// ConcurrencyTests.cs (new file)
- ConcurrentOrders_WithLimitedStock_OnlyAvailableSucceed
- ConcurrentCartMerge_HandledGracefully
```

---

## 7. Evidence & Notes

### Stock Reservation Pattern (CreateOrderCommand.cs:160-177)
```csharp
// Within explicit transaction
foreach (var cartItem in cart.Items)
{
    var product = products.First(p => p.Id == cartItem.ProductId);
    var variant = product.Variants.First(v => v.Id == cartItem.VariantId);

    order.AddItem(
        cartItem.ProductId,
        cartItem.VariantId,
        product.Name,
        variant.Name ?? "",
        variant.Sku,
        cartItem.Quantity,
        cartItem.UnitPrice
    );

    // Reduce stock - this is inside the transaction
    variant.AdjustStock(-cartItem.Quantity);
}
```

### Stock Rollback Pattern (CancelOrderCommand.cs:67-83)
```csharp
// NOTE: Not wrapped in explicit transaction!
foreach (var item in order.Items)
{
    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
    var variant = product?.Variants.FirstOrDefault(v => v.Id == item.VariantId);
    if (variant != null)
    {
        variant.AdjustStock(item.Quantity); // Add back the stock
    }
}

order.SetCancellationReason(request.CancellationReason);
order.SetStatus(OrderStatus.Cancelled);

await _context.SaveChangesAsync(cancellationToken); // Single SaveChanges, no transaction
```

### Order Number Generation (CreateOrderCommand.cs:220-225)
```csharp
private static string GenerateUniqueOrderNumber()
{
    var now = DateTime.UtcNow;
    var randomSuffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    return $"ORD-{now:yyyyMMdd}-{now:HHmmss}-{randomSuffix}";
}
```
**Analysis**: Collision probability is ~1 in 65,536 for orders in same second. Acceptable for low-medium volume but not for high-traffic e-commerce.

### Cart Merge Logic (MergeGuestCartCommand.cs:94-122)
```csharp
foreach (var guestItem in guestCart.Items)
{
    var existingItem = userCart.Items.FirstOrDefault(i =>
        i.ProductId == guestItem.ProductId && i.VariantId == guestItem.VariantId);

    var availableStock = variant?.StockQuantity ?? 0;

    if (existingItem != null)
    {
        // Merge quantities (up to available stock)
        var newQuantity = Math.Min(existingItem.Quantity + guestItem.Quantity, availableStock);
        if (newQuantity > 0) existingItem.SetQuantity(newQuantity);
    }
    else
    {
        // Add new item (up to available stock)
        var quantity = Math.Min(guestItem.Quantity, availableStock);
        if (quantity > 0) userCart.AddItem(...);
    }
}

// Remove the guest cart
_context.Carts.Remove(guestCart);
```
**Issue**: If stock is 0, items are silently skipped with no user notification.

### Price History Entity (ProductPriceHistory.cs)
```csharp
public enum PriceChangeReason
{
    Initial,
    PriceChange,
    Promotion,
    PromotionEnd,
    SeasonalSale,
    CostAdjustment
}
```
**Issue**: Entity exists but `UpdateProductCommand` doesn't create records when price changes.

### Transaction Configuration (CreateOrderCommand.cs:138-212)
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
try
{
    // ... all order operations ...
    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return Result<OrderDto>.Success(MapToDto(order, products));
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```
**Note**: Uses default isolation level (ReadCommitted in PostgreSQL). Consider RepeatableRead for stock operations.

### Domain Invariant: Stock Cannot Go Negative (ProductVariant.cs:79-87)
```csharp
public void AdjustStock(int adjustment)
{
    var newQuantity = StockQuantity + adjustment;
    if (newQuantity < 0)
        throw new InvalidOperationException(
            $"Cannot reduce stock below zero. Current: {StockQuantity}, Adjustment: {adjustment}");

    StockQuantity = newQuantity;
    SetUpdatedAt();
}
```
**Note**: Domain protection exists but doesn't prevent concurrent requests from both reading the same stock value before either writes.

### Order Status State Machine (Order.cs:106-124)
```csharp
private void ValidateStatusTransition(OrderStatus newStatus)
{
    var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Refunded, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Refunded, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Returned],
        [OrderStatus.Delivered] = [OrderStatus.Returned],
        [OrderStatus.Cancelled] = [],  // Terminal state
        [OrderStatus.Refunded] = [],   // Terminal state
        [OrderStatus.Returned] = [OrderStatus.Refunded]
    };

    if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
    {
        throw new InvalidOperationException($"Cannot transition order from {Status} to {newStatus}");
    }
}
```
**Note**: Well-implemented state machine with explicit transitions and terminal state protection.

### Database Constraints
- `OrderNumber` has unique index (`OrderConfiguration.cs:144`)
- `ProductPriceHistory` has foreign key to `Product`
- No `RowVersion` column on `ProductVariant` for optimistic concurrency
