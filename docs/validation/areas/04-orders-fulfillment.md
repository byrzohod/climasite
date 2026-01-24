# Orders & Fulfillment - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Order Creation**: Guest and authenticated checkout, cart-to-order conversion, stock validation
- **Order Status Lifecycle**: Pending -> Paid -> Processing -> Shipped -> Delivered (with Cancelled, Refunded, Returned branches)
- **Order Details**: Full order information, items, timeline/events, shipping/billing addresses
- **Reorder**: Re-add previous order items to cart with stock validation
- **Order Cancellation**: Cancel pending/paid orders with stock restoration
- **Invoice Generation**: PDF invoice download for orders
- **Inventory Reservation**: Stock reduction on order creation, restoration on cancellation

### API Endpoints
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/orders` | POST | Anonymous | Create order (supports guest checkout) |
| `/api/orders` | GET | Required | Get user's orders (paginated, filtered) |
| `/api/orders/statuses` | GET | None | Get available order statuses |
| `/api/orders/{id}` | GET | Required | Get order details by ID |
| `/api/orders/by-number/{orderNumber}` | GET | Required | Get order by order number |
| `/api/orders/{id}/cancel` | POST | Required | Cancel an order |
| `/api/orders/{id}/reorder` | POST | Required | Reorder items from previous order |
| `/api/orders/{id}/invoice` | GET | Required | Download invoice PDF |
| `/api/admin/orders` | GET | Admin | Admin order list with filters |
| `/api/admin/orders/{id}` | GET | Admin | Admin order details |
| `/api/admin/orders/{id}/status` | PUT | Admin | Update order status |
| `/api/admin/orders/{id}/shipping` | PUT | Admin | Update shipping info |
| `/api/admin/orders/{id}/notes` | POST | Admin | Add order note |

### Frontend Routes & Components
| Route | Component | Description |
|-------|-----------|-------------|
| `/account/orders` | `OrdersComponent` | User's order list with filters |
| `/account/orders/:id` | `OrderDetailsComponent` | Order details, timeline, actions |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Controllers** | `src/ClimaSite.Api/Controllers/OrdersController.cs` |
| **Commands** | `src/ClimaSite.Application/Features/Orders/Commands/CreateOrderCommand.cs` |
| | `src/ClimaSite.Application/Features/Orders/Commands/CancelOrderCommand.cs` |
| | `src/ClimaSite.Application/Features/Orders/Commands/ReorderCommand.cs` |
| **Queries** | `src/ClimaSite.Application/Features/Orders/Queries/GetUserOrdersQuery.cs` |
| | `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByIdQuery.cs` |
| | `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByNumberQuery.cs` |
| | `src/ClimaSite.Application/Features/Orders/Queries/GenerateInvoiceQuery.cs` |
| **DTOs** | `src/ClimaSite.Application/Features/Orders/DTOs/OrderDto.cs` |
| **Entities** | `src/ClimaSite.Core/Entities/Order.cs` |
| | `src/ClimaSite.Core/Entities/OrderItem.cs` |
| | `src/ClimaSite.Core/Entities/OrderEvent.cs` |
| **Admin Commands** | `src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateShippingInfoCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Commands/AddOrderNoteCommand.cs` |
| **Admin Queries** | `src/ClimaSite.Application/Features/Admin/Orders/Queries/GetAdminOrdersQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Queries/GetAdminOrderByIdQuery.cs` |
| **Admin DTOs** | `src/ClimaSite.Application/Features/Admin/Orders/DTOs/AdminOrderDtos.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| **Components** | `src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts` |
| | `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts` |
| **Services** | `src/ClimaSite.Web/src/app/core/services/checkout.service.ts` (includes order methods) |
| **Models** | `src/ClimaSite.Web/src/app/core/models/order.model.ts` |

---

## 3. Test Coverage Audit

### Unit Tests (Backend)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `CancelOrderCommandTests.cs` | `Handle_WhenOrderNotFound_ReturnsFailure` | Order not found |
| | `Handle_WhenUserDoesNotOwnOrder_ReturnsAccessDenied` | Authorization |
| | `Handle_WhenAdminCancelsAnyOrder_Succeeds` | Admin override |
| | `Handle_WhenOrderIsPending_CancelsSuccessfully` | Pending cancel |
| | `Handle_WhenOrderIsPaid_CancelsSuccessfully` | Paid cancel |
| | `Handle_WhenOrderIsShipped_CannotCancel` | Status validation |
| | `Handle_WhenOrderIsDelivered_CannotCancel` | Status validation |
| | `Handle_WhenOrderIsAlreadyCancelled_CannotCancelAgain` | Idempotency |
| | `Handle_RestoresStockWhenCancelled` | Inventory restoration |
| | `Handle_RestoresStockForMultipleItems` | Multi-item inventory |
| | `Handle_SetsCancellationReason` | Reason tracking |
| | `Handle_AddsOrderEvent` | Event logging |
| | `Handle_ReturnsUpdatedOrderDto` | DTO mapping |
| | `Validator_WhenOrderIdEmpty_ReturnsValidationError` | Validation |
| | `Validator_WhenOrderIdProvided_PassesValidation` | Validation |
| | `Validator_CancellationReasonIsOptional` | Validation |
| `GetOrderByIdQueryTests.cs` | `Handle_WhenOrderNotFound_ReturnsFailure` | Not found |
| | `Handle_WhenUserOwnsOrder_ReturnsOrder` | Authorization |
| | `Handle_WhenUserDoesNotOwnOrder_ReturnsAccessDenied` | Authorization |
| | `Handle_WhenAdminAccessesAnyOrder_ReturnsOrder` | Admin access |
| | `Handle_ReturnsOrderWithItems` | Item mapping |
| | `Handle_ReturnsOrderWithEvents` | Event mapping |
| | `Handle_ReturnsCorrectTotals` | Total calculation |
| | `Handle_ReturnsOrderStatus` | Status mapping |
| | `Handle_WhenNoUserIdProvided_ReturnsOrder` | Guest order access |
| | `Handle_ReturnsItemImageUrls` | Image URLs |
| | `Handle_ReturnsCancelledOrderInfo` | Cancelled order data |
| `GetUserOrdersQueryTests.cs` | `Handle_WhenUserNotAuthenticated_ReturnsEmptyList` | Auth check |
| | `Handle_WhenUserHasNoOrders_ReturnsEmptyList` | Empty state |
| | `Handle_WhenUserHasOrders_ReturnsUserOrders` | Basic retrieval |
| | `Handle_WithStatusFilter_ReturnsFilteredOrders` | Status filter |
| | `Handle_WithDateFromFilter_ReturnsFilteredOrders` | Date filter |
| | `Handle_WithDateToFilter_ReturnsFilteredOrders` | Date filter |
| | `Handle_WithSearchQuery_ReturnsFilteredOrders` | Search |
| | `Handle_WithPagination_ReturnsPagedResults` | Pagination |
| | `Handle_WithSorting_ReturnsOrderedResults` (date/total, asc/desc) | Sorting |
| | `Handle_ReturnsCorrectItemCount` | Item count |
| | `Handle_ReturnsMax3Items` | Item limit |
| `OrderTests.cs` (Domain) | `Constructor_WithValidData_CreatesOrder` | Entity creation |
| | `SetOrderNumber_WithEmptyValue_ThrowsArgumentException` | Validation |
| | `SetCustomerEmail_WithEmptyValue_ThrowsArgumentException` | Validation |
| | `SetCustomerEmail_NormalizesToLowerCase` | Normalization |
| | `SetShippingCost_WithNegativeValue_ThrowsArgumentException` | Validation |
| | `SetTaxAmount_WithNegativeValue_ThrowsArgumentException` | Validation |
| | `SetDiscountAmount_WithNegativeValue_ThrowsArgumentException` | Validation |
| | `SetCurrency_WithEmptyValue_ThrowsArgumentException` | Validation |
| | `SetCurrency_NormalizesToUpperCase` | Normalization |
| | `SetShippingAddress_WithNull_ThrowsArgumentNullException` | Validation |
| | `SetStatus_FromPendingToPaid_UpdatesStatus` | Status transition |
| | `SetStatus_FromPendingToShipped_ThrowsInvalidOperationException` | Invalid transition |
| | `SetStatus_FromPaidToProcessing_UpdatesStatus` | Status transition |
| | `SetStatus_FromProcessingToShipped_SetsShippedAt` | Timestamp |
| | `SetStatus_FromShippedToDelivered_SetsDeliveredAt` | Timestamp |
| | `SetStatus_ToCancelled_SetsCancelledAt` | Timestamp |
| | `SetStatus_FromCancelled_ThrowsInvalidOperationException` | Terminal state |
| | `CanBeCancelled_WhenPending_ReturnsTrue` | Business rule |
| | `CanBeCancelled_WhenPaid_ReturnsTrue` | Business rule |
| | `CanBeCancelled_WhenShipped_ReturnsFalse` | Business rule |
| | `CanBeRefunded_WhenPaid_ReturnsTrue` | Business rule |
| | `CanBeRefunded_WhenPending_ReturnsFalse` | Business rule |
| | `SetPaymentInfo_SetsPaymentIntentIdAndMethod` | Payment info |
| | `SetTrackingNumber_SetsTrackingNumber` | Tracking |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| *No dedicated integration tests found* | - | API endpoint integration tests should be added |

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `OrdersListTests.cs` | `Orders_NotLoggedIn_RedirectsToLogin` | Auth guard |
| | `Orders_NoOrders_ShowsEmptyMessage` | Empty state |
| | `Orders_WithOrders_DisplaysOrderList` | Order display |
| | `Orders_DisplaysOrderNumber` | Order number |
| | `Orders_ClickOrder_NavigatesToDetails` | Navigation |
| | `Orders_FilterByStatus_ShowsFilteredResults` | Status filter |
| | `Orders_SearchByOrderNumber_FiltersResults` | Search |
| | `Orders_SortByDate_ChangesOrder` | Sorting |
| | `Orders_SortByTotal_ChangesOrder` | Sorting |
| `OrderDetailsTests.cs` | `OrderDetails_DisplaysOrderNumber` | Order number |
| | `OrderDetails_DisplaysOrderStatus` | Status display |
| | `OrderDetails_DisplaysOrderTotal` | Total display |
| | `OrderDetails_DisplaysOrderItems` | Items display |
| | `OrderDetails_PendingOrder_ShowsCancelButton` | Cancel availability |
| | `OrderDetails_DisplaysShippingAddress` | Address display |
| | `OrderDetails_DisplaysPaymentMethod` | Payment display |
| | `OrderDetails_OtherUserOrder_AccessDenied` | Authorization |
| | `OrderDetails_InvalidOrderId_ShowsNotFound` | Not found |
| | `OrderDetails_BackToOrders_NavigatesCorrectly` | Navigation |
| `OrderActionsTests.cs` | `CancelOrder_PendingOrder_CancelsSuccessfully` | Cancel flow |
| | `CancelOrder_WithoutReason_StillCancels` | Cancel without reason |
| | `CancelOrder_AlreadyCancelled_ButtonDisabled` | UI state |
| | `Reorder_AddsItemsToCart` | Reorder flow |
| | `Reorder_OutOfStock_ShowsPartialSuccess` | Partial reorder |
| | `DownloadInvoice_DownloadsFile` | Invoice download |
| | `OrderTimeline_DisplaysEvents` | Timeline |
| | `TrackingNumber_WhenShipped_Displays` | Tracking |
| `OrdersMobileTests.cs` | `Mobile_OrdersList_DisplaysCorrectly` | Mobile layout |
| | `Mobile_OrderDetails_DisplaysCorrectly` | Mobile details |
| | `Mobile_OrderActions_AreAccessible` | Mobile buttons |
| | `Mobile_OrderNavigation_WorksWithTouch` | Touch navigation |
| | `Mobile_OrdersFilter_IsAccessible` | Mobile filters |
| | `Mobile_BackNavigation_WorksCorrectly` | Mobile back |
| | `Mobile_OrderItemDetails_DisplaysCorrectly` | Mobile items |
| | `Mobile_Landscape_AdjustsLayout` | Landscape |
| | `Mobile_CancelOrder_ModalDisplaysCorrectly` | Mobile modal |

### Frontend Unit Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `orders.component.spec.ts` | *Exists but content not reviewed* | Component tests |
| `order-details.component.spec.ts` | *Exists but content not reviewed* | Component tests |

---

## 4. Manual Verification Steps

### Order Creation Flow
1. Add products to cart as guest user
2. Proceed to checkout with guest email
3. Fill shipping address, select shipping method
4. Complete payment via Stripe
5. Verify order confirmation page displays order number
6. Verify order appears in backend database with status "Pending"

### Order Listing Flow
1. Login as user with existing orders
2. Navigate to `/account/orders`
3. Verify orders display with order number, status, total, date
4. Test status filter dropdown
5. Test date range filters
6. Test search by order number
7. Test sort by date (asc/desc) and total (asc/desc)
8. Test pagination with user having >10 orders

### Order Details Flow
1. Click on an order from the list
2. Verify order number, status badge, date display correctly
3. Verify timeline section shows status history
4. Verify all items display with images, names, quantities, prices
5. Verify subtotal, shipping, tax, discount, total calculations
6. Verify shipping address displays completely
7. Verify payment method section shows payment info
8. Verify customer email/phone display

### Reorder Flow
1. Navigate to a completed order
2. Click "Reorder" button
3. Verify items are added to cart
4. Verify success/partial success message displays
5. Verify navigation to cart page
6. For out-of-stock items, verify skip message

### Cancel Order Flow
1. Navigate to a Pending or Paid order
2. Verify "Cancel Order" button is visible
3. Click cancel button
4. Enter optional cancellation reason
5. Confirm cancellation
6. Verify order status changes to "Cancelled"
7. Verify stock is restored for order items
8. Navigate to a Shipped order, verify cancel button is hidden/disabled

### Invoice Download Flow
1. Navigate to any order details
2. Click "Download Invoice" button
3. Verify PDF file downloads with order number in filename

### Mobile Verification
1. Open orders list on mobile viewport (390x844)
2. Verify no horizontal scroll
3. Verify filters are accessible
4. Verify orders stack vertically
5. Test order details on mobile
6. Verify cancel modal fits screen
7. Test landscape orientation

---

## 5. Gaps & Risks

### Missing Tests
- [ ] **CreateOrderCommand unit tests** - No dedicated unit tests found for order creation handler
- [ ] **ReorderCommand unit tests** - No dedicated unit tests for reorder logic
- [ ] **GenerateInvoiceQuery unit tests** - Invoice generation not unit tested
- [ ] **API integration tests** - No controller-level integration tests for orders endpoints
- [ ] **Admin order management E2E tests** - Admin update status, shipping, notes not E2E tested

### Code Gaps
- [ ] **Currency hardcoded to EUR** - `CreateOrderCommand.cs:157` sets currency to "EUR" regardless of store config
- [ ] **Shipping costs hardcoded** - `CreateOrderCommand.cs:184-190` has hardcoded shipping rates
- [ ] **Tax rate hardcoded** - `CreateOrderCommand.cs:195` uses fixed 20% VAT rate
- [ ] **No order confirmation emails** - Order creation doesn't trigger notification
- [ ] **No stock reservation expiry** - Stock is reduced immediately, no temporary reservation pattern

### Business Logic Risks
- [ ] **Race condition on order number** - Mitigated by timestamp+random suffix pattern, but high-traffic scenarios untested
- [ ] **No payment verification before order creation** - Order created before payment confirmed
- [ ] **Reorder ignores price changes** - Uses current product prices, not original order prices

### UI/UX Gaps
- [ ] **No order tracking page** - Tracking number displayed but no tracking link/integration
- [ ] **No order email in user-facing UI** - Guest checkout email not shown in order details
- [ ] **No print order functionality** - Only PDF invoice, no print-friendly view

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **HIGH** | Missing CreateOrderCommand unit tests | Add comprehensive unit tests for order creation, stock validation, total calculations |
| **HIGH** | Missing ReorderCommand unit tests | Add unit tests for reorder with stock validation, partial success scenarios |
| **HIGH** | No API integration tests | Add integration tests for all `/api/orders/*` endpoints |
| **MEDIUM** | Hardcoded currency/shipping/tax | Extract to configuration or admin settings |
| **MEDIUM** | Missing admin E2E tests | Add E2E tests for admin order management workflows |
| **MEDIUM** | No order confirmation emails | Integrate notification service for order events |
| **LOW** | No order tracking integration | Add carrier tracking link generation |
| **LOW** | GenerateInvoiceQuery not tested | Add unit tests for invoice PDF generation |

### Suggested New Unit Tests

```csharp
// CreateOrderCommandTests.cs
- Handle_WhenCartEmpty_ReturnsFailure
- Handle_WhenProductOutOfStock_ReturnsFailure
- Handle_WhenProductInactive_ReturnsFailure
- Handle_WithGuestSession_CreatesGuestOrder
- Handle_WithAuthenticatedUser_CreatesUserOrder
- Handle_CalculatesCorrectTotals
- Handle_ReducesProductStock
- Handle_ClearsCart
- Handle_SetsCorrectShippingCost
- Handle_CalculatesTax

// ReorderCommandTests.cs
- Handle_WhenOrderNotFound_ReturnsFailure
- Handle_WhenUserNotOwner_ReturnsAccessDenied
- Handle_AddsAvailableItemsToCart
- Handle_SkipsOutOfStockItems
- Handle_SkipsInactiveProducts
- Handle_ReturnsPartialSuccessMessage
- Handle_UpdatesExistingCartItems
```

---

## 7. Evidence & Notes

### Order Status State Machine
```
Pending -----> Paid -----> Processing -----> Shipped -----> Delivered
    |           |              |                |
    v           v              v                v
Cancelled   Cancelled      Cancelled        Returned
            Refunded       Refunded            |
                                               v
                                           Refunded
```

### Key Code Patterns

1. **Order Number Generation** (`CreateOrderCommand.cs:220-225`):
   - Format: `ORD-YYYYMMDD-HHMMSS-XXXX`
   - Uses timestamp + 4-char random suffix for uniqueness
   - Race-condition safe (no DB count dependency)

2. **Status Transition Validation** (`Order.cs:106-124`):
   - Explicit valid transition map
   - Throws `InvalidOperationException` for invalid transitions
   - Automatic timestamp setting on status change

3. **Stock Restoration on Cancel** (`CancelOrderCommand.cs`):
   - Iterates order items and restores variant stock
   - Verified by unit tests

4. **Reorder with Stock Check** (`ReorderCommand.cs:114-148`):
   - Validates product/variant availability
   - Checks existing cart quantities
   - Returns detailed skip reasons

### Test Data Factory Usage
- E2E tests use `TestDataFactory.CreateOrderAsync()` for real order creation
- Orders created with configurable product count
- Automatic cleanup via `CleanupAsync()`
