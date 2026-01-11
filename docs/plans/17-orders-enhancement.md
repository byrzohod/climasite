# Plan 17: Orders Page Enhancement

## Overview
Enhance the Orders List and Order Details pages with full capabilities including filtering, sorting, status tracking, order actions, and comprehensive testing.

---

## Current State Analysis

### Orders List Page (`/account/orders`)
- **What exists:**
  - Basic list of orders with order number, status, total, date
  - Preview of first 2 items with images
  - Link to view details

- **What's missing:**
  - Filtering by status (Pending, Processing, Shipped, Delivered, Cancelled)
  - Filtering by date range
  - Search by order number
  - Sorting options (newest, oldest, total amount)
  - Pagination controls
  - Order status timeline/progress indicator
  - Quick actions (cancel, reorder, track)

### Order Details Page (`/account/orders/:id`)
- **What exists:**
  - Basic order information display

- **What's missing:**
  - Complete order status timeline with timestamps
  - Shipping tracking information
  - Detailed item list with links to products
  - Order summary (subtotal, shipping, tax, discounts, total)
  - Billing and shipping address display
  - Invoice download
  - Order cancellation functionality
  - Reorder functionality (add all items to cart)
  - Order notes/customer communication
  - Estimated delivery date

---

## Implementation Tasks

### Phase 1: Orders List Enhancement (ORD-001 to ORD-010)

#### Backend Tasks

**ORD-001: Enhanced GetUserOrdersQuery**
- Add filter parameters: status, dateFrom, dateTo, searchQuery
- Add sorting parameter: sortBy (date, total), sortDirection
- Update pagination to be more flexible
- File: `src/ClimaSite.Application/Features/Orders/Queries/GetUserOrdersQuery.cs`

**ORD-002: Order Status Filter API**
- Create endpoint to get available order statuses for filter dropdown
- File: `src/ClimaSite.Api/Controllers/OrdersController.cs`

#### Frontend Tasks

**ORD-003: Orders List Filters Component**
- Create filter component with:
  - Status dropdown (multi-select)
  - Date range picker (from/to)
  - Search input for order number
  - Clear filters button
- File: `src/ClimaSite.Web/src/app/features/account/orders/components/order-filters/order-filters.component.ts`

**ORD-004: Orders List Sorting**
- Add sort dropdown (newest, oldest, highest total, lowest total)
- Persist sort preference in localStorage
- File: `src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts`

**ORD-005: Pagination Component**
- Create reusable pagination component
- Show page numbers, prev/next, items per page selector
- File: `src/ClimaSite.Web/src/app/shared/components/pagination/pagination.component.ts`

**ORD-006: Enhanced Order Card**
- Add status badge with appropriate colors
- Add status progress indicator (4 steps: placed, processing, shipped, delivered)
- Add quick action buttons (view, track, cancel, reorder)
- File: `src/ClimaSite.Web/src/app/features/account/orders/components/order-card/order-card.component.ts`

**ORD-007: Order Service Enhancement**
- Add methods for filtering, sorting, pagination
- Add caching for orders list
- File: `src/ClimaSite.Web/src/app/core/services/order.service.ts`

**ORD-008: Empty States and Loading**
- Skeleton loading for orders list
- Empty state for no orders
- Empty state for no results (when filters applied)
- File: `src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts`

**ORD-009: Mobile Responsive Orders List**
- Cards layout for mobile
- Collapsible filters
- Touch-friendly interactions
- File: `src/ClimaSite.Web/src/app/features/account/orders/orders.component.scss`

**ORD-010: Orders List i18n**
- Add all translation keys for orders list
- Support for EN, BG, DE
- Files: `src/ClimaSite.Web/src/assets/i18n/{en,bg,de}.json`

---

### Phase 2: Order Details Enhancement (ORD-011 to ORD-020)

#### Backend Tasks

**ORD-011: Enhanced GetOrderByIdQuery**
- Include full order items with product details
- Include shipping/billing addresses
- Include order timeline events
- Include tracking information
- File: `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByIdQuery.cs`

**ORD-012: Order Timeline Entity**
- Create OrderEvent entity to track status changes with timestamps
- Add migration
- Files: `src/ClimaSite.Core/Entities/OrderEvent.cs`, migration

**ORD-013: Cancel Order Enhancement**
- Add validation (only pending/processing can be cancelled)
- Add cancellation reason
- Create order event for cancellation
- Restore inventory on cancellation
- File: `src/ClimaSite.Application/Features/Orders/Commands/CancelOrderCommand.cs`

**ORD-014: Reorder API**
- Create endpoint to add all order items to cart
- Handle out-of-stock items gracefully
- File: `src/ClimaSite.Api/Controllers/OrdersController.cs`

**ORD-015: Invoice PDF Generation**
- Create invoice PDF service
- Include company info, order details, itemized list
- File: `src/ClimaSite.Application/Services/InvoicePdfService.cs`

#### Frontend Tasks

**ORD-016: Order Details Page Layout**
- Two-column layout: order info (left), summary (right)
- Responsive design for mobile
- File: `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts`

**ORD-017: Order Timeline Component**
- Visual timeline showing order status progression
- Timestamps for each status change
- Current status highlighted
- File: `src/ClimaSite.Web/src/app/features/account/order-details/components/order-timeline/order-timeline.component.ts`

**ORD-018: Order Items Table**
- Product image, name (link to product), variant, quantity, price, subtotal
- Responsive table/cards for mobile
- File: `src/ClimaSite.Web/src/app/features/account/order-details/components/order-items/order-items.component.ts`

**ORD-019: Order Actions Component**
- Cancel order button (with confirmation modal)
- Reorder button
- Download invoice button
- Track shipment button (opens tracking URL)
- File: `src/ClimaSite.Web/src/app/features/account/order-details/components/order-actions/order-actions.component.ts`

**ORD-020: Address Display Component**
- Reusable component to display shipping/billing address
- Copy to clipboard functionality
- File: `src/ClimaSite.Web/src/app/shared/components/address-card/address-card.component.ts`

---

### Phase 3: Testing (ORD-021 to ORD-030)

#### Unit Tests (Backend)

**ORD-021: GetUserOrdersQuery Handler Tests**
- Test filtering by status
- Test filtering by date range
- Test search by order number
- Test sorting options
- Test pagination
- File: `tests/ClimaSite.Application.Tests/Features/Orders/Queries/GetUserOrdersQueryTests.cs`

**ORD-022: GetOrderByIdQuery Handler Tests**
- Test order retrieval with full details
- Test order not found
- Test unauthorized access
- File: `tests/ClimaSite.Application.Tests/Features/Orders/Queries/GetOrderByIdQueryTests.cs`

**ORD-023: CancelOrderCommand Handler Tests**
- Test successful cancellation
- Test cancellation of non-pending order (should fail)
- Test inventory restoration
- Test order event creation
- File: `tests/ClimaSite.Application.Tests/Features/Orders/Commands/CancelOrderCommandTests.cs`

#### Unit Tests (Frontend)

**ORD-024: OrderFiltersComponent Tests**
- Test filter selection
- Test date range validation
- Test search input
- Test clear filters
- File: `src/ClimaSite.Web/src/app/features/account/orders/components/order-filters/order-filters.component.spec.ts`

**ORD-025: OrderTimelineComponent Tests**
- Test timeline rendering for different statuses
- Test timestamp display
- File: `src/ClimaSite.Web/src/app/features/account/order-details/components/order-timeline/order-timeline.component.spec.ts`

**ORD-026: OrderService Tests**
- Test API calls with parameters
- Test error handling
- Test caching
- File: `src/ClimaSite.Web/src/app/core/services/order.service.spec.ts`

#### E2E Tests

**ORD-027: Orders List E2E Tests**
```
tests/ClimaSite.E2E/Tests/Orders/OrdersListTests.cs
```
- Test: User can view orders list
- Test: User can filter orders by status
- Test: User can search orders by number
- Test: User can sort orders
- Test: Pagination works correctly
- Test: Empty state shows when no orders
- Test: Filter clear works

**ORD-028: Order Details E2E Tests**
```
tests/ClimaSite.E2E/Tests/Orders/OrderDetailsTests.cs
```
- Test: User can view order details
- Test: Order timeline displays correctly
- Test: Order items display with correct info
- Test: Shipping/billing addresses display
- Test: User can navigate to product from order item

**ORD-029: Order Actions E2E Tests**
```
tests/ClimaSite.E2E/Tests/Orders/OrderActionsTests.cs
```
- Test: User can cancel pending order
- Test: User cannot cancel shipped order
- Test: Reorder adds items to cart
- Test: Reorder handles out-of-stock items
- Test: Invoice download works

**ORD-030: Orders Mobile E2E Tests**
```
tests/ClimaSite.E2E/Tests/Orders/OrdersMobileTests.cs
```
- Test: Orders list renders correctly on mobile
- Test: Order details renders correctly on mobile
- Test: Filters work on mobile
- Test: Actions work on mobile

---

## Data-TestId Conventions

### Orders List Page
```
orders-page                    - Main container
orders-filters                 - Filters section
orders-status-filter           - Status dropdown
orders-date-from              - Date from input
orders-date-to                - Date to input
orders-search-input           - Search input
orders-clear-filters          - Clear filters button
orders-sort-dropdown          - Sort dropdown
orders-list                   - Orders list container
order-card                    - Individual order card
order-card-number             - Order number
order-card-status             - Status badge
order-card-date               - Order date
order-card-total              - Order total
order-card-view               - View details button
order-card-cancel             - Cancel button
order-card-reorder            - Reorder button
orders-pagination             - Pagination container
orders-page-prev              - Previous page button
orders-page-next              - Next page button
orders-page-number            - Page number button
orders-empty                  - Empty state
orders-no-results             - No results state
orders-loading                - Loading skeleton
```

### Order Details Page
```
order-details-page            - Main container
order-number                  - Order number
order-date                    - Order date
order-status                  - Status badge
order-timeline                - Timeline component
order-timeline-step           - Timeline step
order-items                   - Items list
order-item-row                - Item row
order-item-image              - Item image
order-item-name               - Item name (link)
order-item-variant            - Variant name
order-item-quantity           - Quantity
order-item-price              - Unit price
order-item-subtotal           - Line subtotal
order-summary                 - Summary section
order-subtotal                - Subtotal
order-shipping                - Shipping cost
order-tax                     - Tax amount
order-discount                - Discount
order-total                   - Total
shipping-address              - Shipping address card
billing-address               - Billing address card
order-actions                 - Actions section
cancel-order-button           - Cancel button
cancel-order-modal            - Cancellation modal
cancel-reason-input           - Cancellation reason
confirm-cancel-button         - Confirm cancel
reorder-button                - Reorder button
download-invoice-button       - Download invoice
track-shipment-button         - Track shipment
```

---

## API Endpoints

### Existing (to enhance)
- `GET /api/orders` - List user orders (add filters, sorting, pagination)
- `GET /api/orders/{id}` - Get order details (add timeline, full items)
- `POST /api/orders/{id}/cancel` - Cancel order (add reason, validation)

### New Endpoints
- `POST /api/orders/{id}/reorder` - Add order items to cart
- `GET /api/orders/{id}/invoice` - Download invoice PDF

---

## Translation Keys to Add

```json
{
  "account.orders": {
    "filters": {
      "status": "Status",
      "allStatuses": "All Statuses",
      "dateFrom": "From",
      "dateTo": "To",
      "search": "Search by order number",
      "clear": "Clear Filters"
    },
    "sort": {
      "label": "Sort by",
      "newest": "Newest First",
      "oldest": "Oldest First",
      "highestTotal": "Highest Total",
      "lowestTotal": "Lowest Total"
    },
    "actions": {
      "cancel": "Cancel Order",
      "reorder": "Reorder",
      "track": "Track Shipment",
      "downloadInvoice": "Download Invoice"
    },
    "cancel": {
      "title": "Cancel Order",
      "confirm": "Are you sure you want to cancel this order?",
      "reason": "Reason for cancellation",
      "reasonPlaceholder": "Please tell us why you're cancelling...",
      "success": "Order cancelled successfully",
      "error": "Failed to cancel order"
    },
    "reorder": {
      "success": "Items added to cart",
      "partial": "Some items were out of stock",
      "error": "Failed to add items to cart"
    },
    "timeline": {
      "placed": "Order Placed",
      "processing": "Processing",
      "shipped": "Shipped",
      "delivered": "Delivered",
      "cancelled": "Cancelled"
    },
    "noResults": "No orders match your filters",
    "tryAgain": "Try adjusting your filters"
  }
}
```

---

## Implementation Order

1. **ORD-001, ORD-002** - Backend filtering/sorting
2. **ORD-007** - Order service frontend
3. **ORD-003, ORD-004, ORD-005** - Filters, sorting, pagination components
4. **ORD-006, ORD-008, ORD-009** - Order card, loading, mobile
5. **ORD-010** - i18n
6. **ORD-021, ORD-024** - Unit tests for phase 1
7. **ORD-027** - E2E tests for orders list

8. **ORD-011, ORD-012** - Backend order details enhancement
9. **ORD-013, ORD-014, ORD-015** - Backend actions
10. **ORD-016, ORD-017, ORD-018** - Frontend order details components
11. **ORD-019, ORD-020** - Actions and address components
12. **ORD-022, ORD-023, ORD-025, ORD-026** - Unit tests for phase 2
13. **ORD-028, ORD-029, ORD-030** - E2E tests for phase 2

---

## Acceptance Criteria

### Orders List Page
- [ ] User can view paginated list of their orders
- [ ] User can filter orders by status
- [ ] User can filter orders by date range
- [ ] User can search orders by order number
- [ ] User can sort orders (newest, oldest, total)
- [ ] Filters persist across navigation
- [ ] Loading skeleton shows while fetching
- [ ] Empty state shows when no orders
- [ ] "No results" state shows when filters return nothing
- [ ] Mobile responsive design works correctly

### Order Details Page
- [ ] User can view complete order details
- [ ] Order timeline shows all status changes with timestamps
- [ ] All order items display with images and product links
- [ ] Order summary shows subtotal, shipping, tax, discounts, total
- [ ] Shipping and billing addresses display correctly
- [ ] User can cancel pending/processing orders
- [ ] User can reorder items (add to cart)
- [ ] User can download invoice PDF
- [ ] User can track shipment (if tracking number exists)
- [ ] Mobile responsive design works correctly

### Testing
- [ ] All unit tests pass (backend and frontend)
- [ ] All E2E tests pass
- [ ] No accessibility issues (keyboard navigation, screen reader)
- [ ] i18n works for all supported languages
