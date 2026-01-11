# Backend API Implementation Plan

## Overview

This plan addresses the backend gaps required to make all 30 E2E tests pass. The E2E tests use **real API calls** with no mocking, which requires a fully functional backend with test infrastructure support.

**Current State:** 4/30 E2E tests passing
**Target State:** 30/30 E2E tests passing

---

## Gap Analysis Summary

### What's Working
- Authentication endpoints (login, register, refresh token)
- Product read endpoints (list, detail, featured, search)
- Basic admin controllers structure
- Database schema and migrations
- JWT authentication with role-based authorization

### What's Missing (Critical for E2E)
1. **Test Infrastructure Endpoints** - No way for E2E tests to create/cleanup test data
2. **Cart Query Handlers** - Cannot retrieve cart contents
3. **Order Query Handlers** - Cannot retrieve user orders
4. **Product/Category Creation Handlers** - Admin cannot create products via API
5. **Database Seeding** - Tests need pre-existing products to work with
6. **Stock Validation** - Checkout tests expect stock validation warnings

---

## Phase 1: Test Infrastructure (CRITICAL - DO FIRST)

The E2E tests require special endpoints to create and cleanup test data. These endpoints should only be enabled in Development/Test environments.

### 1.1 Create TestController

**File:** `src/ClimaSite.Api/Controllers/TestController.cs`

```csharp
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    // POST /api/test/elevate-admin
    // Elevates a user to Admin role (for tests to create products)
    [HttpPost("elevate-admin")]
    public async Task<IActionResult> ElevateToAdmin([FromBody] ElevateAdminRequest request)

    // DELETE /api/test/cleanup/{correlationId}
    // Deletes all test data created with the given correlation ID
    [HttpDelete("cleanup/{correlationId}")]
    public async Task<IActionResult> CleanupTestData(string correlationId)

    // POST /api/test/seed-products
    // Creates sample products for testing (optional, for faster test setup)
    [HttpPost("seed-products")]
    public async Task<IActionResult> SeedProducts([FromBody] SeedProductsRequest request)
}
```

### 1.2 Add Correlation ID Tracking

**Files to modify:**
- All entity base classes to include optional `TestCorrelationId` property
- All create handlers to accept and store correlation ID from header
- TestController to query and delete by correlation ID

**Database changes:**
```sql
-- Add to all tables
ALTER TABLE products ADD COLUMN test_correlation_id VARCHAR(50);
ALTER TABLE categories ADD COLUMN test_correlation_id VARCHAR(50);
ALTER TABLE orders ADD COLUMN test_correlation_id VARCHAR(50);
ALTER TABLE users ADD COLUMN test_correlation_id VARCHAR(50);
CREATE INDEX idx_products_test_correlation ON products(test_correlation_id);
-- (similar for other tables)
```

### 1.3 Environment-Only Registration

**File:** `src/ClimaSite.Api/Program.cs`

```csharp
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Test")
{
    app.MapControllers(); // Include TestController
}
```

### Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| TEST-001 | Create TestController with ElevateAdmin endpoint | `Controllers/TestController.cs` | Low |
| TEST-002 | Add TestCorrelationId to BaseEntity | `Core/Entities/BaseEntity.cs` | Low |
| TEST-003 | Create migration for correlation ID columns | `Migrations/` | Low |
| TEST-004 | Implement CleanupTestData handler | `Application/Features/Test/` | Medium |
| TEST-005 | Add correlation ID header middleware | `Middleware/` | Low |
| TEST-006 | Create SeedProducts endpoint | `Controllers/TestController.cs` | Medium |

---

## Phase 2: Cart API Completion

The Cart endpoints exist but some handlers are missing or incomplete.

### 2.1 Current State

| Endpoint | Controller | Handler | Status |
|----------|------------|---------|--------|
| GET /api/cart | CartController | GetCartQueryHandler | MISSING |
| POST /api/cart/items | CartController | AddToCartCommandHandler | EXISTS |
| PUT /api/cart/items/{id} | CartController | UpdateCartItemCommandHandler | MISSING |
| DELETE /api/cart/items/{id} | CartController | RemoveCartItemCommandHandler | MISSING |
| DELETE /api/cart | CartController | ClearCartCommandHandler | MISSING |
| POST /api/cart/merge | CartController | MergeGuestCartCommandHandler | MISSING |

### 2.2 Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| CART-001 | Create GetCartQuery and Handler | `Application/Features/Cart/Queries/` | Medium |
| CART-002 | Create UpdateCartItemCommand and Handler | `Application/Features/Cart/Commands/` | Medium |
| CART-003 | Create RemoveCartItemCommand and Handler | `Application/Features/Cart/Commands/` | Low |
| CART-004 | Create ClearCartCommand and Handler | `Application/Features/Cart/Commands/` | Low |
| CART-005 | Create MergeGuestCartCommand and Handler | `Application/Features/Cart/Commands/` | Medium |
| CART-006 | Add CartRepository if missing | `Infrastructure/Repositories/` | Low |
| CART-007 | Update CartController to wire all handlers | `Controllers/CartController.cs` | Low |

### 2.3 Cart Query Handler Implementation

```csharp
// GetCartQuery.cs
public record GetCartQuery(string UserId, string? SessionId) : IRequest<CartDto>;

// GetCartQueryHandler.cs
public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        // 1. If authenticated user, find cart by UserId
        // 2. If guest user, find cart by SessionId
        // 3. Include CartItems with Product details
        // 4. Map to CartDto with calculated totals
    }
}
```

---

## Phase 3: Order API Completion

Orders can be created but cannot be queried. The E2E tests need to verify order creation worked.

### 3.1 Current State

| Endpoint | Controller | Handler | Status |
|----------|------------|---------|--------|
| POST /api/orders | OrdersController | CreateOrderCommandHandler | EXISTS (needs testing) |
| GET /api/orders | OrdersController | GetUserOrdersQueryHandler | MISSING |
| GET /api/orders/{id} | OrdersController | GetOrderByIdQueryHandler | MISSING |
| GET /api/orders/number/{orderNumber} | OrdersController | GetOrderByNumberQueryHandler | MISSING |
| POST /api/orders/{id}/cancel | OrdersController | CancelOrderCommandHandler | MISSING |

### 3.2 Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| ORD-001 | Create GetUserOrdersQuery and Handler | `Application/Features/Orders/Queries/` | Medium |
| ORD-002 | Create GetOrderByIdQuery and Handler | `Application/Features/Orders/Queries/` | Low |
| ORD-003 | Create GetOrderByNumberQuery and Handler | `Application/Features/Orders/Queries/` | Low |
| ORD-004 | Create CancelOrderCommand and Handler | `Application/Features/Orders/Commands/` | Medium |
| ORD-005 | Add OrderRepository | `Infrastructure/Repositories/` | Low |
| ORD-006 | Test CreateOrderCommandHandler end-to-end | Tests | Medium |
| ORD-007 | Add stock validation to CreateOrderCommandHandler | Existing handler | Medium |

### 3.3 Order Creation Flow (Validate & Fix)

The CreateOrderCommandHandler should:
1. Validate cart is not empty
2. **Validate stock availability** (tests expect stock warnings)
3. Create Order with OrderItems from cart
4. Update product stock quantities
5. Clear the cart
6. Return created order with order number

---

## Phase 4: Admin Product/Category API

E2E tests create products via the admin API. The controllers exist but handlers may be missing.

### 4.1 Admin Products

| Endpoint | Controller | Handler | Status |
|----------|------------|---------|--------|
| GET /api/admin/products | AdminProductsController | GetProductsQueryHandler | EXISTS |
| POST /api/admin/products | AdminProductsController | CreateProductCommandHandler | VERIFY |
| PUT /api/admin/products/{id} | AdminProductsController | UpdateProductCommandHandler | VERIFY |
| DELETE /api/admin/products/{id} | AdminProductsController | DeleteProductCommandHandler | VERIFY |

### 4.2 Admin Categories

| Endpoint | Controller | Handler | Status |
|----------|------------|---------|--------|
| GET /api/categories | CategoriesController | GetCategoriesQueryHandler | EXISTS |
| POST /api/admin/categories | AdminCategoriesController | CreateCategoryCommandHandler | MISSING |
| PUT /api/admin/categories/{id} | AdminCategoriesController | UpdateCategoryCommandHandler | MISSING |
| DELETE /api/admin/categories/{id} | AdminCategoriesController | DeleteCategoryCommandHandler | MISSING |

### 4.3 Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| ADM-001 | Verify/Fix CreateProductCommandHandler | `Application/Features/Products/Commands/` | Medium |
| ADM-002 | Verify/Fix UpdateProductCommandHandler | `Application/Features/Products/Commands/` | Medium |
| ADM-003 | Verify/Fix DeleteProductCommandHandler | `Application/Features/Products/Commands/` | Low |
| ADM-004 | Create AdminCategoriesController | `Controllers/Admin/` | Low |
| ADM-005 | Create CreateCategoryCommand and Handler | `Application/Features/Categories/Commands/` | Medium |
| ADM-006 | Create UpdateCategoryCommand and Handler | `Application/Features/Categories/Commands/` | Medium |
| ADM-007 | Create DeleteCategoryCommand and Handler | `Application/Features/Categories/Commands/` | Low |

### 4.4 Product Creation Requirements

The CreateProductCommand must accept:
```csharp
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string? Slug,           // Auto-generate if not provided
    int StockQuantity,
    string CategoryId,      // Must exist
    List<string>? ImageUrls,
    bool IsFeatured,
    Dictionary<string, string>? Specifications,
    string? TestCorrelationId  // For test cleanup
) : IRequest<ProductDto>;
```

---

## Phase 5: Database Seeding Enhancement

### 5.1 Current DataSeeder Review

**File:** `src/ClimaSite.Infrastructure/Data/DataSeeder.cs`

The seeder needs to create:
- Admin user (for test admin operations)
- Sample categories (at least 3-4)
- Sample products (at least 10-20 with varied prices)
- Featured products (subset for homepage)

### 5.2 Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| SEED-001 | Review and fix existing DataSeeder | `Infrastructure/Data/DataSeeder.cs` | Medium |
| SEED-002 | Add admin user seeding | DataSeeder | Low |
| SEED-003 | Add category seeding (HVAC categories) | DataSeeder | Low |
| SEED-004 | Add product seeding (realistic HVAC products) | DataSeeder | Medium |
| SEED-005 | Mark some products as featured | DataSeeder | Low |
| SEED-006 | Add sample product images | DataSeeder | Low |

### 5.3 Seed Data Requirements

**Categories:**
1. Air Conditioners (slug: air-conditioners)
2. Heating Systems (slug: heating-systems)
3. Ventilation (slug: ventilation)
4. Accessories (slug: accessories)

**Sample Products (minimum):**
```
- Split Air Conditioner 12000BTU - €599.99 (Air Conditioners, Featured)
- Portable AC Unit - €349.99 (Air Conditioners)
- Gas Heater Premium - €899.99 (Heating Systems, Featured)
- Electric Radiator - €199.99 (Heating Systems)
- Ceiling Fan Industrial - €129.99 (Ventilation)
- Air Purifier HEPA - €249.99 (Ventilation, Featured)
- Universal Remote Control - €29.99 (Accessories)
- AC Installation Kit - €49.99 (Accessories)
- Replacement Filters Pack - €19.99 (Accessories)
- Smart Thermostat - €149.99 (Accessories, Featured)
```

---

## Phase 6: Validation & Error Handling

E2E tests expect specific error responses for validation failures.

### 6.1 Stock Validation

**In CreateOrderCommandHandler:**
```csharp
// Check stock before creating order
foreach (var item in cartItems)
{
    var product = await _productRepository.GetByIdAsync(item.ProductId);
    if (product.StockQuantity < item.Quantity)
    {
        throw new ValidationException(
            $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}"
        );
    }
}
```

### 6.2 Form Validation Errors

All commands should use FluentValidation and return proper ProblemDetails:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "Email": ["Email is required", "Invalid email format"],
    "Password": ["Password must be at least 8 characters"]
  }
}
```

### 6.3 Implementation Tasks

| Task ID | Description | Files | Complexity |
|---------|-------------|-------|------------|
| VAL-001 | Add stock validation to CreateOrderCommandHandler | Order handlers | Medium |
| VAL-002 | Create/verify FluentValidation validators for all commands | Validators | Medium |
| VAL-003 | Configure global exception handling with ProblemDetails | Program.cs | Low |
| VAL-004 | Add proper HTTP status codes for all error scenarios | Controllers | Low |

---

## Phase 7: Additional Features for E2E Tests

### 7.1 Wishlist (Lower Priority)

Some user journey tests use wishlist. Basic implementation needed:

| Task ID | Description | Complexity |
|---------|-------------|------------|
| WISH-001 | Create GetWishlistQueryHandler | Low |
| WISH-002 | Create AddToWishlistCommandHandler | Low |
| WISH-003 | Create RemoveFromWishlistCommandHandler | Low |

### 7.2 Search (Products Already Searchable)

Verify the search endpoint works:
- `GET /api/products?search=keyword`
- Should search in name, description
- Should return paginated results

### 7.3 Saved Addresses (For Checkout Tests)

Some checkout tests use saved addresses:

| Task ID | Description | Complexity |
|---------|-------------|------------|
| ADDR-001 | Create GetUserAddressesQueryHandler | Low |
| ADDR-002 | Create SaveAddressCommandHandler | Low |
| ADDR-003 | Create DeleteAddressCommandHandler | Low |

---

## Implementation Order

### Sprint 1: Test Infrastructure (MUST BE FIRST)
1. TEST-001 through TEST-006 (Test endpoints)
2. This unblocks all other E2E tests

### Sprint 2: Cart & Order Queries
1. CART-001 through CART-007
2. ORD-001 through ORD-007
3. This allows cart + checkout tests to pass

### Sprint 3: Admin API & Seeding
1. ADM-001 through ADM-007
2. SEED-001 through SEED-006
3. This allows product creation in tests

### Sprint 4: Validation & Polish
1. VAL-001 through VAL-004
2. WISH-001 through WISH-003 (if needed)
3. ADDR-001 through ADDR-003 (if needed)

---

## Testing Strategy

### After Each Phase

1. **Run related E2E tests:**
   ```bash
   # After Phase 1 (Test Infrastructure)
   npx playwright test --grep "can be created"

   # After Phase 2 (Cart)
   npx playwright test tests/ClimaSite.E2E/Tests/Cart/

   # After Phase 3 (Orders)
   npx playwright test tests/ClimaSite.E2E/Tests/Checkout/

   # After Phase 4 (Admin)
   npx playwright test tests/ClimaSite.E2E/Tests/Products/

   # Full suite
   npx playwright test
   ```

2. **Integration Tests:**
   - Write API integration tests for each new handler
   - Test happy path and error scenarios

3. **Manual Verification:**
   - Start API: `dotnet run --project src/ClimaSite.Api`
   - Start Frontend: `cd src/ClimaSite.Web && ng serve`
   - Test full flow in browser

---

## API Endpoint Summary

### Public Endpoints (No Auth Required)

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| POST | /api/auth/register | User registration | ✅ |
| POST | /api/auth/login | User login | ✅ |
| POST | /api/auth/refresh | Refresh JWT token | ✅ |
| GET | /api/products | List products (paginated, filtered) | ✅ |
| GET | /api/products/{slug} | Product detail | ✅ |
| GET | /api/products/featured | Featured products | ✅ |
| GET | /api/products/search | Search products | ✅ |
| GET | /api/categories | Category tree | ✅ |
| GET | /api/categories/{slug} | Category detail | ✅ |

### Authenticated Endpoints

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | /api/cart | Get user cart | ❌ MISSING |
| POST | /api/cart/items | Add to cart | ✅ |
| PUT | /api/cart/items/{id} | Update quantity | ❌ MISSING |
| DELETE | /api/cart/items/{id} | Remove item | ❌ MISSING |
| DELETE | /api/cart | Clear cart | ❌ MISSING |
| POST | /api/cart/merge | Merge guest cart | ❌ MISSING |
| POST | /api/orders | Create order | ⚠️ NEEDS TEST |
| GET | /api/orders | User order history | ❌ MISSING |
| GET | /api/orders/{id} | Order detail | ❌ MISSING |
| GET | /api/wishlist | Get wishlist | ❌ MISSING |
| POST | /api/wishlist | Add to wishlist | ❌ MISSING |
| DELETE | /api/wishlist/{id} | Remove from wishlist | ❌ MISSING |

### Admin Endpoints (Requires Admin Role)

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | /api/admin/products | List all products | ✅ |
| POST | /api/admin/products | Create product | ⚠️ VERIFY |
| PUT | /api/admin/products/{id} | Update product | ⚠️ VERIFY |
| DELETE | /api/admin/products/{id} | Delete product | ⚠️ VERIFY |
| POST | /api/admin/categories | Create category | ❌ MISSING |
| PUT | /api/admin/categories/{id} | Update category | ❌ MISSING |
| DELETE | /api/admin/categories/{id} | Delete category | ❌ MISSING |

### Test Endpoints (Development/Test Only)

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| POST | /api/test/elevate-admin | Elevate user to admin | ❌ CREATE |
| DELETE | /api/test/cleanup/{correlationId} | Cleanup test data | ❌ CREATE |
| POST | /api/test/seed-products | Seed sample products | ❌ CREATE |

---

## Success Criteria

- [ ] All 30 E2E tests pass
- [ ] API responds with proper HTTP status codes
- [ ] Validation errors return ProblemDetails format
- [ ] Test data can be created and cleaned up programmatically
- [ ] Cart operations work for both authenticated and guest users
- [ ] Orders can be created and retrieved
- [ ] Stock validation prevents overselling
- [ ] Database is properly seeded with sample data
- [ ] All endpoints have corresponding handlers
- [ ] Admin can create/update/delete products and categories

---

## Files to Create

1. `src/ClimaSite.Api/Controllers/TestController.cs`
2. `src/ClimaSite.Application/Features/Test/Commands/ElevateToAdminCommand.cs`
3. `src/ClimaSite.Application/Features/Test/Commands/CleanupTestDataCommand.cs`
4. `src/ClimaSite.Application/Features/Cart/Queries/GetCartQuery.cs`
5. `src/ClimaSite.Application/Features/Cart/Queries/GetCartQueryHandler.cs`
6. `src/ClimaSite.Application/Features/Cart/Commands/UpdateCartItemCommand.cs`
7. `src/ClimaSite.Application/Features/Cart/Commands/RemoveCartItemCommand.cs`
8. `src/ClimaSite.Application/Features/Cart/Commands/ClearCartCommand.cs`
9. `src/ClimaSite.Application/Features/Orders/Queries/GetUserOrdersQuery.cs`
10. `src/ClimaSite.Application/Features/Orders/Queries/GetOrderByIdQuery.cs`
11. `src/ClimaSite.Application/Features/Categories/Commands/CreateCategoryCommand.cs`
12. Migration for TestCorrelationId columns

## Files to Modify

1. `src/ClimaSite.Core/Entities/BaseEntity.cs` - Add TestCorrelationId
2. `src/ClimaSite.Infrastructure/Data/DataSeeder.cs` - Enhance seeding
3. `src/ClimaSite.Api/Controllers/CartController.cs` - Wire handlers
4. `src/ClimaSite.Api/Controllers/OrdersController.cs` - Wire handlers
5. `src/ClimaSite.Api/Program.cs` - Conditional test routes
6. All create command handlers - Accept correlation ID
