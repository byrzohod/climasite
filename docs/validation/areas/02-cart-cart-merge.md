# Cart & Cart Merging - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Add to Cart**: Add products with variants to cart, validates stock availability
- **Remove from Cart**: Remove individual items from cart
- **Update Quantity**: Modify item quantities with stock validation
- **Clear Cart**: Remove all items from cart
- **Guest Cart**: Session-based cart for unauthenticated users (7-day expiration)
- **User Cart**: Persistent cart for authenticated users
- **Cart Merge on Login**: Merge guest cart items into user cart when logging in
- **Cart Totals**: Subtotal, tax (20% VAT), shipping, and total calculations

### API Endpoints
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/cart` | Get current cart (user or guest) | Optional |
| POST | `/api/cart/items` | Add item to cart | Optional |
| PUT | `/api/cart/items/{itemId}` | Update item quantity | Optional |
| DELETE | `/api/cart/items/{itemId}` | Remove item from cart | Optional |
| DELETE | `/api/cart` | Clear entire cart | Optional |
| POST | `/api/cart/merge` | Merge guest cart with user cart | Required |

### Frontend Routes & Components
| Route | Component | Description |
|-------|-----------|-------------|
| `/cart` | `CartComponent` | Cart page with item list and summary |
| N/A | `CartService` | Signal-based cart state management |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| Controllers | `src/ClimaSite.Api/Controllers/CartController.cs` |
| Commands | `src/ClimaSite.Application/Features/Cart/Commands/AddToCartCommand.cs` |
| | `src/ClimaSite.Application/Features/Cart/Commands/UpdateCartItemCommand.cs` |
| | `src/ClimaSite.Application/Features/Cart/Commands/RemoveFromCartCommand.cs` |
| | `src/ClimaSite.Application/Features/Cart/Commands/ClearCartCommand.cs` |
| | `src/ClimaSite.Application/Features/Cart/Commands/MergeGuestCartCommand.cs` |
| Queries | `src/ClimaSite.Application/Features/Cart/Queries/GetCartQuery.cs` |
| DTOs | `src/ClimaSite.Application/Features/Cart/DTOs/CartDto.cs` |
| Entities | `src/ClimaSite.Core/Entities/Cart.cs` |
| | `src/ClimaSite.Core/Entities/CartItem.cs` |
| Interfaces | `src/ClimaSite.Core/Interfaces/ICartRepository.cs` |
| Configuration | `src/ClimaSite.Infrastructure/Data/Configurations/CartConfiguration.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| Components | `src/ClimaSite.Web/src/app/features/cart/cart.component.ts` |
| Services | `src/ClimaSite.Web/src/app/core/services/cart.service.ts` |
| Models | `src/ClimaSite.Web/src/app/core/models/cart.model.ts` |

---

## 3. Test Coverage Audit

### Unit Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `tests/ClimaSite.Core.Tests/Entities/CartTests.cs` | `Constructor_WithUserId_CreatesCart` | Cart entity creation |
| | `Constructor_WithSessionId_CreatesGuestCart` | Guest cart creation |
| | `Constructor_WithNoIdentifier_ThrowsArgumentException` | Validation |
| | `Constructor_WithEmptySessionId_ThrowsArgumentException` | Validation |
| | `SetUser_UpdatesUserIdAndClearsSessionId` | Cart merge prep |
| | `ExtendExpiration_ExtendsExpirationDate` | Expiration logic |
| | `AddItem_AddsNewItem` | Add item |
| | `AddItem_ExistingVariant_IncrementsQuantity` | Quantity merge |
| | `RemoveItem_RemovesExistingItem` | Remove item |
| | `RemoveItem_NonExistingItem_DoesNothing` | Edge case |
| | `UpdateItemQuantity_UpdatesQuantity` | Update quantity |
| | `UpdateItemQuantity_WithZero_RemovesItem` | Edge case |
| | `UpdateItemQuantity_WithNegative_RemovesItem` | Edge case |
| | `Clear_RemovesAllItems` | Clear cart |
| | `TotalItems_CalculatesCorrectly` | Calculations |
| | `Subtotal_CalculatesCorrectly` | Calculations |
| | `GetItem_ReturnsExistingItem` | Item lookup |
| | `GetItem_NonExisting_ReturnsNull` | Edge case |
| `src/ClimaSite.Web/src/app/core/services/cart.service.spec.ts` | `should be created` | Service creation |
| | `should have default empty state` | Initial state |
| | `should load cart from API` | Load cart |
| | `should create empty cart on error` | Error handling |
| | `should add item to cart` | Add to cart |
| | `should add item with variant` | Variant support |
| | `should set error on failure` | Error handling |
| | `should update item quantity` | Update quantity |
| | `should remove item from cart` | Remove item |
| | `should clear all items from cart` | Clear cart |
| | `should merge guest cart with user cart` | **Cart merge** |
| | `should return cart summary` | Summary |
| | `isInCart` / `getItemQuantity` tests | Utility methods |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE** | - | No API integration tests for CartController |

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `tests/ClimaSite.E2E/Tests/Cart/CartTests.cs` | `Cart_EmptyCart_ShowsEmptyMessage` | Empty state |
| | `Cart_AddSingleProduct_ShowsInCart` | Add single item |
| | `Cart_AddMultipleProducts_ShowsCorrectTotal` | Multiple items + totals |
| | `Cart_RemoveItem_UpdatesCart` | Remove item |
| | `Cart_UpdateQuantity_UpdatesTotal` | Update quantity |
| | `Cart_ContinueShopping_RedirectsToProducts` | Navigation |
| | `Cart_GuestUser_CanAddToCart` | Guest cart |
| | `Cart_PersistsAfterRefresh` | Persistence |
| `tests/ClimaSite.E2E/PageObjects/CartPage.cs` | Page object for cart tests | Test helpers |

---

## 4. Manual Verification Steps

### Guest Cart Flow
1. Open site in incognito/private window (no login)
2. Navigate to Products page
3. Add a product to cart
4. Verify cart badge updates in header
5. Go to Cart page
6. Verify item appears with correct price
7. Refresh page - verify cart persists via session ID
8. Close and reopen browser - verify cart persists (localStorage session)

### Authenticated Cart Flow
1. Login as existing user
2. Add product to cart
3. Logout and close browser
4. Login again
5. Verify cart items are still present

### Cart Merge Flow (Critical)
1. Open site in incognito window (guest)
2. Add 2x Product A to guest cart
3. Login with existing user who already has 1x Product B in cart
4. Verify merged cart contains:
   - 2x Product A (from guest cart)
   - 1x Product B (from user cart)
5. Verify guest cart is deleted after merge

### Cart Merge with Same Product
1. Guest cart: Add 2x Product A
2. User cart (pre-existing): Has 1x Product A
3. Login
4. Verify merged cart has 3x Product A (quantities summed)
5. Verify stock limits are respected (capped at available stock)

### Quantity Updates
1. Add item to cart
2. Increase quantity using + button
3. Decrease quantity using - button
4. Type new quantity in input field
5. Verify total updates correctly
6. Try to exceed stock - verify error message

### Remove Item
1. Add multiple items to cart
2. Click remove button on one item
3. Confirm removal in dialog
4. Verify item removed, totals updated
5. Remove all items - verify empty cart message

---

## 5. Gaps & Risks

### Missing Test Coverage
- [ ] **No API integration tests** for CartController endpoints
- [ ] **No E2E test for cart merge** - critical feature not tested end-to-end
- [ ] **No E2E test for cart merge with same product** (quantity summing)
- [ ] **No E2E test for stock limit enforcement** during merge
- [ ] **No unit tests for MergeGuestCartCommand handler**
- [ ] **No unit tests for AddToCartCommand handler**
- [ ] **No unit tests for UpdateCartItemCommand handler**
- [ ] **No unit tests for RemoveFromCartCommand handler**
- [ ] **No unit tests for GetCartQuery handler**

### Code Quality Issues
- [ ] `CartService.mergeCart()` sends wrong request format (POST body `{ userId }` vs query param)
- [ ] Frontend `mergeCart()` method not used - cart merge not triggered on login
- [ ] Error messages in `CartService` are hardcoded (not i18n) - noted in TODO comment
- [ ] Duplicate `MapCartToDto` logic in 4+ command handlers (DRY violation)
- [ ] Missing guest cart cleanup job (abandoned carts accumulate)

### Security Concerns
- [ ] Cart merge endpoint requires auth but doesn't validate session ownership
- [ ] No rate limiting on cart operations

### Edge Cases Not Tested
- [ ] Concurrent cart modifications
- [ ] Product deleted while in cart
- [ ] Variant deactivated while in cart
- [ ] Stock reduced below cart quantity
- [ ] Expired guest cart handling

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **P0** | No cart merge E2E test | Add `Cart_MergesOnLogin_CombinesItems` test |
| **P0** | Cart merge not triggered | Wire up `CartService.mergeCart()` on successful login |
| **P1** | No API integration tests | Add `CartControllerTests.cs` covering all 6 endpoints |
| **P1** | No command handler unit tests | Add tests for all 5 command handlers |
| **P1** | Duplicate MapCartToDto | Extract to shared `CartDtoMapper` service |
| **P2** | Hardcoded error messages | Use i18n keys via injected TranslateService |
| **P2** | No guest cart cleanup | Implement scheduled job to purge expired carts |
| **P2** | Frontend mergeCart wrong format | Change to use query param: `POST /api/cart/merge?guestSessionId=xxx` |
| **P3** | No rate limiting | Add rate limiting middleware for cart endpoints |
| **P3** | No concurrency tests | Add tests for concurrent cart modifications |

---

## 7. Evidence & Notes

### Cart Merge Logic (MergeGuestCartCommand.cs:94-122)
```csharp
// Merge guest cart items into user cart
foreach (var guestItem in guestCart.Items)
{
    var existingItem = userCart.Items.FirstOrDefault(i =>
        i.ProductId == guestItem.ProductId && i.VariantId == guestItem.VariantId);

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

### Cart Entity Business Rules (Cart.cs)
- Either `UserId` OR `SessionId` must be provided (not both null)
- Guest carts expire after 7 days by default
- `SetUser()` clears session ID (for merge operation)
- Adding existing variant increments quantity (not duplicates)

### Frontend Cart State (cart.service.ts)
- Uses Angular Signals for reactive state
- Generates session ID in localStorage
- Computed signals for `itemCount`, `subtotal`, `total`, `isEmpty`
- Error state managed via `_error` signal

### Test Data Factory Pattern
E2E tests create real products and users via `TestDataFactory`, ensuring no mocking and proper database state.

### Known TODOs in Code
1. `AddToCartCommand.cs:139` - Guest cart expiration/cleanup job needed
2. `AddToCartCommand.cs:185` - VAT rate should be configurable per country
3. `cart.service.ts:7-9` - Error messages should use i18n keys
