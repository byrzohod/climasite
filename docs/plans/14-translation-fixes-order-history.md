# Translation Fixes & Order History Implementation

## Issues to Fix

### Issue 1: Untranslated Elements Across Pages
**Problem:** Some UI elements remain in English regardless of language selection.

**Areas to investigate:**
- Checkout page hardcoded text
- Product card hardcoded text
- Navigation/header elements
- Button labels
- Form placeholders
- Error messages

### Issue 2: Product Details - Empty SKU Display
**Problem:** "SKU:" label shows with empty/no text on product details page.

**Root Cause:** Product data may not include SKU, or template doesn't handle missing SKU gracefully.

### Issue 3: Missing Translation Key - products.noImage
**Problem:** "products.noImage" key not in translation files.

**Fix:** Add the key to all translation files (en.json, bg.json, de.json).

### Issue 4: Order Confirmation Redirects to Non-Existing Page
**Problem:** After placing order, redirects to `/account/orders/{orderId}` which doesn't exist.

**Required:** Implement order history and order details pages.

### Issue 5: Missing Account Pages
**Required pages:**
- `/account` - Account dashboard
- `/account/orders` - Order history list
- `/account/orders/:id` - Order details page
- `/account/profile` - Profile settings (if not exists)

---

## Implementation Plan

### Phase 1: Fix Translation Issues

#### Task 1.1: Audit All Hardcoded Text
- [ ] Scan checkout.component.ts for hardcoded strings
- [ ] Scan product-card.component.ts for hardcoded strings
- [ ] Scan product-detail.component.ts for hardcoded strings
- [ ] Scan header.component.ts for hardcoded strings
- [ ] Scan cart.component.ts for hardcoded strings
- [ ] Scan all other components for hardcoded strings

#### Task 1.2: Add Missing Translation Keys
- [ ] Add `products.noImage` to en.json, bg.json, de.json
- [ ] Add any other missing keys discovered in audit
- [ ] Add shipping method translations (standard, express, overnight)
- [ ] Add payment method translations

#### Task 1.3: Replace Hardcoded Text
- [ ] Update components to use translation pipe for all user-facing text
- [ ] Update button labels, placeholders, error messages

### Phase 2: Fix Product Details SKU Issue

#### Task 2.1: Investigate SKU Display
- [ ] Check product-detail.component.ts template
- [ ] Check if SKU is being passed from API
- [ ] Add conditional display (only show SKU if present)

### Phase 3: Implement Order History & Account Pages

#### Task 3.1: Create Order History Page
- [ ] Create `/account/orders` route
- [ ] Create OrderHistoryComponent
- [ ] Display list of user's orders with:
  - Order number
  - Date
  - Status
  - Total
  - View details link

#### Task 3.2: Create Order Details Page
- [ ] Create `/account/orders/:id` route
- [ ] Create OrderDetailsComponent
- [ ] Display:
  - Order number and status
  - Order date
  - Shipping address
  - Billing address (if different)
  - Order items with images
  - Subtotal, shipping, tax, total
  - Payment method
  - Order timeline/status history

#### Task 3.3: Create/Update Account Service
- [ ] Add getOrders() method
- [ ] Add getOrderById(id) method
- [ ] Handle authentication for order access

#### Task 3.4: Fix Order Confirmation Redirect
- [ ] Update checkout.component.ts to redirect to valid order details page
- [ ] Or show order details inline on confirmation

#### Task 3.5: Update Account Dashboard
- [ ] Add link to order history
- [ ] Show recent orders summary

### Phase 4: Testing

#### Task 4.1: Unit Tests
- [ ] OrderHistoryComponent tests
- [ ] OrderDetailsComponent tests
- [ ] Order service tests (getOrders, getOrderById)
- [ ] Translation tests for new keys

#### Task 4.2: Integration Tests
- [ ] API tests for GET /api/orders (list)
- [ ] API tests for GET /api/orders/:id
- [ ] Test order access authorization

#### Task 4.3: E2E Tests
- [ ] Test viewing order history after login
- [ ] Test viewing order details
- [ ] Test order confirmation flow redirects correctly
- [ ] Test translations switch properly on all new pages
- [ ] Test empty order history state

---

## Files to Create/Modify

### New Files
1. `src/ClimaSite.Web/src/app/features/account/order-history/order-history.component.ts`
2. `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts`
3. `tests/ClimaSite.E2E/Tests/Account/OrderHistoryTests.cs`

### Files to Modify
1. `src/ClimaSite.Web/src/assets/i18n/en.json` - Add missing keys
2. `src/ClimaSite.Web/src/assets/i18n/bg.json` - Add missing keys
3. `src/ClimaSite.Web/src/assets/i18n/de.json` - Add missing keys
4. `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts` - Fix hardcoded text, fix redirect
5. `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts` - Fix hardcoded text
6. `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` - Fix SKU display
7. `src/ClimaSite.Web/src/app/app.routes.ts` - Add new routes
8. `src/ClimaSite.Web/src/app/core/services/checkout.service.ts` - Add order fetching methods
9. `src/ClimaSite.Web/src/app/features/account/account-dashboard/account-dashboard.component.ts` - Add orders link

---

### Phase 5: Multilingual Product Content

#### Task 5.1: Backend - Product Translations Model
- [ ] Create `ProductTranslation` entity with fields: ProductId, LanguageCode, Name, ShortDescription, Description
- [ ] Create migration for product_translations table
- [ ] Update Product entity with navigation property to translations
- [ ] Update ProductRepository to fetch translations by language

#### Task 5.2: Backend - API Updates
- [ ] Update GET /api/products to accept `lang` query parameter
- [ ] Update product DTOs to include translated content
- [ ] Return translated content based on requested language

#### Task 5.3: Admin Panel - Product Translation Management
- [ ] Add translation fields to product create/edit form
- [ ] Create UI for managing translations per language (EN, BG, DE)
- [ ] Show translation status (which languages have translations)

#### Task 5.4: Frontend - Request Products in Current Language
- [ ] Update ProductService to pass current language to API
- [ ] Update product display components to use translated content

#### Task 5.5: Tests for Product Translations
- [ ] Unit tests for ProductTranslation repository
- [ ] Integration tests for product translation API
- [ ] E2E tests for viewing products in different languages

---

## Verification Checklist

1. **Translations:**
   - [ ] All text on checkout page translates when switching language
   - [ ] All text on product pages translates
   - [ ] All text on account pages translates
   - [ ] No translation keys visible to users

2. **Product Details:**
   - [ ] SKU only shows when product has SKU value
   - [ ] No empty labels displayed

3. **Order Flow:**
   - [ ] After placing order, user sees order confirmation
   - [ ] User can navigate to order history
   - [ ] User can view individual order details
   - [ ] Order details show all relevant information

4. **Tests:**
   - [ ] All new unit tests pass
   - [ ] All E2E tests pass
   - [ ] No regressions in existing tests
