# Wishlist - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Add to Wishlist**: Add products to user's wishlist via heart icon toggle
- **Remove from Wishlist**: Remove individual items from wishlist
- **Clear Wishlist**: Remove all items from wishlist at once
- **View Wishlist**: Dedicated page showing all saved products with product cards
- **Guest/Local Storage**: LocalStorage persistence for guests
- **API Sync**: Automatic sync with backend for authenticated users
- **Wishlist Merge**: Merge guest wishlist with user wishlist on login
- **Public Sharing**: Share token generation (entity supports, not fully exposed in API)
- **Notification on Sale**: Entity supports sale notifications (not implemented in UI)

### API Endpoints
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/wishlist` | Get user's wishlist with items | Required |
| POST | `/api/wishlist/items/{productId}` | Add item to wishlist | Required |
| DELETE | `/api/wishlist/items/{productId}` | Remove item from wishlist | Required |

### Frontend Routes & Components
| Route | Component | Description |
|-------|-----------|-------------|
| `/wishlist` | `WishlistComponent` | Wishlist page with product grid |
| N/A | `WishlistService` | Signal-based wishlist state management |
| N/A | Header integration | Heart icon with badge count |
| N/A | Product Card integration | Quick wishlist toggle button |
| N/A | Product Detail integration | Wishlist toggle button |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| Controllers | `src/ClimaSite.Api/Controllers/WishlistController.cs` |
| Commands | `src/ClimaSite.Application/Features/Wishlist/Commands/AddToWishlistCommand.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Commands/RemoveFromWishlistCommand.cs` |
| Queries | `src/ClimaSite.Application/Features/Wishlist/Queries/GetWishlistQuery.cs` |
| DTOs | `src/ClimaSite.Application/Features/Wishlist/DTOs/WishlistDto.cs` |
| Entities | `src/ClimaSite.Core/Entities/Wishlist.cs` |
| | `src/ClimaSite.Core/Entities/WishlistItem.cs` |
| Interfaces | `src/ClimaSite.Core/Interfaces/IWishlistRepository.cs` |
| Configuration | `src/ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| Components | `src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts` |
| Services | `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts` |
| Integrations | `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts` |
| | `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts` |
| | `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` |
| Routes | `src/ClimaSite.Web/src/app/app.routes.ts` (line 56-59) |

---

## 3. Test Coverage Audit

### Unit Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE** | - | No unit tests for Wishlist entity |
| **NONE** | - | No unit tests for WishlistItem entity |
| **NONE** | - | No unit tests for AddToWishlistCommand handler |
| **NONE** | - | No unit tests for RemoveFromWishlistCommand handler |
| **NONE** | - | No unit tests for GetWishlistQuery handler |
| **NONE** | - | No unit tests for WishlistService (frontend) |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE** | - | No API integration tests for WishlistController |

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE** | - | No E2E tests for wishlist functionality |

---

## 4. Manual Verification Steps

### Add to Wishlist Flow
1. Login as existing user
2. Navigate to Products page
3. Click heart icon on a product card
4. Verify heart icon fills with color (active state)
5. Check header wishlist badge increments
6. Navigate to `/wishlist` page
7. Verify product appears in wishlist grid

### Remove from Wishlist Flow
1. Go to `/wishlist` page
2. Click the remove (trash) button on a wishlist item
3. Verify item is removed from the list
4. Verify header badge decrements
5. Navigate to product detail page
6. Verify heart icon is now empty (inactive state)

### Clear Wishlist Flow
1. Add multiple products to wishlist
2. Go to `/wishlist` page
3. Click "Clear All" button
4. Verify all items are removed
5. Verify empty state is displayed
6. Verify header badge shows 0 or is hidden

### Guest Wishlist Flow
1. Open site without logging in
2. Add product to wishlist via heart icon
3. Verify item is stored in localStorage
4. Refresh page
5. Verify wishlist persists from localStorage
6. Login with existing user
7. Verify guest items are merged with user wishlist

### Wishlist Persistence
1. Login and add items to wishlist
2. Logout
3. Login again
4. Verify wishlist items are still present (loaded from API)

### Product Detail Wishlist Toggle
1. Navigate to a product detail page
2. Click the wishlist heart button
3. Verify button shows loading spinner briefly
4. Verify button toggles to active state
5. Verify header badge updates
6. Click again to remove
7. Verify button returns to inactive state

---

## 5. Gaps & Risks

### Missing Test Coverage (Critical)
- [ ] **No unit tests for Wishlist entity** - Business logic untested (AddItem, RemoveItem, Clear, etc.)
- [ ] **No unit tests for WishlistItem entity** - Validation logic untested (SetNote, SetPriority, etc.)
- [ ] **No unit tests for AddToWishlistCommand handler** - Command handler untested
- [ ] **No unit tests for RemoveFromWishlistCommand handler** - Command handler untested
- [ ] **No unit tests for GetWishlistQuery handler** - Query handler untested
- [ ] **No frontend unit tests for WishlistService** - Signal state management untested
- [ ] **No API integration tests** for WishlistController endpoints
- [ ] **No E2E tests** for any wishlist user flows

### Missing Features (Planned but Not Implemented)
- [ ] **No ClearWishlist API endpoint** - Frontend `clearWishlist()` only clears localStorage
- [ ] **No WishlistRepository implementation** - Only interface exists (`IWishlistRepository`)
- [ ] **No shared wishlist page** - `/wishlist/shared/:token` route not implemented
- [ ] **No move-to-cart functionality** - Planned in design but not implemented
- [ ] **No item notes/priority UI** - Entities support but no UI to edit
- [ ] **No notify-on-sale functionality** - Entity has `NotifyOnSale` flag but unused

### Code Quality Issues
- [ ] **Frontend clearWishlist doesn't sync to API** - Only clears localStorage, authenticated users have orphaned backend data
- [ ] **No error handling for failed API sync** - `catchError(() => of(null))` silently swallows errors
- [ ] **Wishlist merge on login may cause duplicates** - Race condition between localStorage and API fetch
- [ ] **Missing i18n for some UI text** - Some hardcoded strings in component templates
- [ ] **WishlistComponent relies on cached product data** - If product cache is empty, items show without product info

### Security Concerns
- [ ] **No rate limiting** on wishlist API endpoints
- [ ] **No validation of product existence before add** (frontend) - Backend validates but frontend doesn't

### Edge Cases Not Tested
- [ ] Product deleted while in wishlist
- [ ] Product deactivated while in wishlist
- [ ] Concurrent wishlist modifications from multiple devices
- [ ] Large wishlist (100+ items) performance
- [ ] Guest wishlist with expired/invalid product IDs

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **P0** | No E2E tests | Add `WishlistTests.cs` with full user flow coverage |
| **P0** | No unit tests for entities | Add `WishlistTests.cs` and `WishlistItemTests.cs` in Core.Tests |
| **P0** | No command handler tests | Add tests for AddToWishlist, RemoveFromWishlist, GetWishlist handlers |
| **P1** | clearWishlist not synced | Add `DELETE /api/wishlist` endpoint and call from frontend |
| **P1** | No WishlistRepository | Implement `WishlistRepository.cs` in Infrastructure layer |
| **P1** | No frontend service tests | Add `wishlist.service.spec.ts` |
| **P2** | No move-to-cart | Implement `POST /api/wishlist/items/{id}/move-to-cart` endpoint |
| **P2** | No shared wishlist page | Implement SharedWishlistPageComponent |
| **P2** | Error handling | Add proper error handling with user notifications |
| **P3** | Rate limiting | Add rate limiting middleware for wishlist endpoints |
| **P3** | Notes/priority UI | Add UI to edit item notes and priority |

### Recommended E2E Tests

```typescript
// tests/ClimaSite.E2E/Tests/Wishlist/WishlistTests.cs

[Test] Wishlist_AddProductFromCard_ShowsInWishlist
[Test] Wishlist_AddProductFromDetailPage_ShowsInWishlist
[Test] Wishlist_RemoveProduct_RemovesFromWishlist
[Test] Wishlist_ClearAll_RemovesAllItems
[Test] Wishlist_EmptyState_ShowsBrowseProductsCTA
[Test] Wishlist_GuestUser_PersistsInLocalStorage
[Test] Wishlist_LoginMerge_CombinesGuestAndUserWishlists
[Test] Wishlist_HeaderBadge_UpdatesOnAddRemove
[Test] Wishlist_ProductDeletedWhileInWishlist_HandlesGracefully
[Test] Wishlist_Toggle_AddsAndRemovesCorrectly
```

### Recommended Unit Tests

```csharp
// tests/ClimaSite.Core.Tests/Entities/WishlistTests.cs

[Fact] Constructor_WithUserId_CreatesWishlist
[Fact] AddItem_NewProduct_AddsItem
[Fact] AddItem_ExistingProduct_ReturnsExistingItem
[Fact] RemoveItem_ExistingProduct_RemovesItem
[Fact] RemoveItem_NonExistingProduct_DoesNothing
[Fact] Clear_RemovesAllItems
[Fact] SetPublic_True_GeneratesShareToken
[Fact] SetPublic_False_KeepsExistingToken
[Fact] TotalItems_ReturnsCorrectCount
[Fact] GetItem_ExistingProduct_ReturnsItem
[Fact] GetItem_NonExistingProduct_ReturnsNull

// tests/ClimaSite.Core.Tests/Entities/WishlistItemTests.cs

[Fact] Constructor_SetsPropertiesCorrectly
[Fact] SetNote_ValidNote_SetsNote
[Fact] SetNote_TooLongNote_ThrowsArgumentException
[Fact] SetNote_Null_ClearsNote
[Fact] SetPriority_ValidPriority_SetsPriority
[Fact] SetPriority_NegativePriority_ThrowsArgumentException
[Fact] SetNotifyOnSale_True_EnablesNotification
```

---

## 7. Evidence & Notes

### Entity Business Rules (Wishlist.cs)

```csharp
// One wishlist per user
public Wishlist(Guid userId)
{
    UserId = userId;
}

// AddItem returns existing if duplicate
public WishlistItem AddItem(Guid productId, string? note = null, int priority = 0)
{
    var existingItem = GetItem(productId);
    if (existingItem != null)
    {
        return existingItem; // Prevents duplicates
    }
    // ... create new item
}

// Share token generation
public void SetPublic(bool isPublic)
{
    IsPublic = isPublic;
    if (isPublic && string.IsNullOrEmpty(ShareToken))
    {
        ShareToken = GenerateShareToken();
    }
}
```

### Frontend State Management (wishlist.service.ts)

```typescript
// Signal-based state
private readonly _items = signal<WishlistItem[]>([]);
readonly items = this._items.asReadonly();
readonly itemCount = computed(() => this._items().length);

// Dual storage: localStorage + API
private loadWishlist(): void {
    // Load from localStorage first (immediate display)
    const stored = localStorage.getItem(this.STORAGE_KEY);
    // ...
    // If authenticated, fetch from API and merge
    if (this.authService.isAuthenticated()) {
        this.fetchFromApi();
    }
}
```

### API Response Format (GetWishlistQuery)

```csharp
return new WishlistDto
{
    Id = wishlist.Id,
    UserId = wishlist.UserId,
    Items = items, // List<WishlistItemDto>
    ItemCount = items.Count,
    UpdatedAt = wishlist.UpdatedAt
};

// WishlistItemDto includes:
// ProductName, ProductSlug, ImageUrl, Price, SalePrice, IsOnSale, InStock, AddedAt
```

### Database Schema

| Table | Key Columns |
|-------|-------------|
| `wishlists` | id, user_id, is_public, share_token, created_at, updated_at |
| `wishlist_items` | id, wishlist_id, product_id, note, priority, price_when_added, notify_on_sale, created_at |

### Known TODOs in Code

1. `WishlistService.ts:177-179` - API sync errors silently swallowed
2. `WishlistComponent.ts:287-293` - Relies on cached product data from items
3. No `ClearWishlistCommand` exists - only frontend localStorage clear

### Plan Reference

Full implementation plan in `docs/plans/13-wishlist.md` includes:
- Tasks WISH-001 to WISH-019
- E2E test specifications (not implemented)
- Shared wishlist feature (not implemented)
- Move-to-cart feature (not implemented)
