# Admin Panel - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Admin Authentication** - Role-based access control with Admin role requirement
- **Admin Guard** - Route protection for `/admin/*` routes
- **Product Management** - CRUD operations for products (create, read, update, delete)
- **Category Management** - CRUD operations for categories with reordering
- **Inventory Management** - Stock levels, low stock alerts
- **Order Management** - Order listing, status updates, shipping info, notes
- **Customer Management** - Customer listing, status updates
- **Review Moderation** - Approve/reject/flag reviews, bulk operations
- **Q&A Moderation** - Approve/reject/flag questions and answers, bulk operations
- **Translation Management** - Multi-language product content management
- **Related Products** - Product relationship management (similar, accessories, upgrades)
- **Dashboard Metrics** - KPIs, revenue charts, order status, top products, low stock

### API Endpoints

#### Products (`/api/admin/products`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/products` | List products with filters | Admin |
| GET | `/api/admin/products/{slug}` | Get single product | Admin |
| POST | `/api/admin/products` | Create product | Admin |
| PUT | `/api/admin/products/{id}` | Update product | Admin |
| DELETE | `/api/admin/products/{id}` | Delete product | Admin |
| PATCH | `/api/admin/products/{id}/status` | Toggle product active status | Admin |
| PATCH | `/api/admin/products/{id}/featured` | Toggle product featured | Admin |
| GET | `/api/admin/products/{id}/relations` | Get product relations | Admin |
| POST | `/api/admin/products/{id}/relations` | Add related product | Admin |
| DELETE | `/api/admin/products/{id}/relations/{relationId}` | Remove related product | Admin |
| PUT | `/api/admin/products/{id}/relations/reorder` | Reorder relations | Admin |
| GET | `/api/admin/products/{id}/translations` | Get product translations | Admin |
| POST | `/api/admin/products/{id}/translations` | Add translation | Admin |
| PUT | `/api/admin/products/{id}/translations/{lang}` | Update translation | Admin |
| DELETE | `/api/admin/products/{id}/translations/{lang}` | Delete translation | Admin |

#### Categories (`/api/admin/categories`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/categories` | List all categories | Admin |
| GET | `/api/admin/categories/{slug}` | Get single category | Admin |
| POST | `/api/admin/categories` | Create category | Admin |
| PUT | `/api/admin/categories/{id}` | Update category | Admin |
| DELETE | `/api/admin/categories/{id}` | Delete category | Admin |
| PATCH | `/api/admin/categories/{id}/status` | Toggle category status | Admin |
| PATCH | `/api/admin/categories/reorder` | Reorder categories | Admin |

#### Orders (`/api/admin/orders`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/orders` | List orders with filters | Admin |
| GET | `/api/admin/orders/{id}` | Get order details | Admin |
| PUT | `/api/admin/orders/{id}/status` | Update order status | Admin |
| PUT | `/api/admin/orders/{id}/shipping` | Update shipping info | Admin |
| POST | `/api/admin/orders/{id}/notes` | Add order note | Admin |

#### Customers (`/api/admin/customers`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/customers` | List customers | Admin |
| GET | `/api/admin/customers/{id}` | Get customer details | Admin |
| PUT | `/api/admin/customers/{id}/status` | Update customer status | Admin |

#### Reviews (`/api/admin/reviews`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/reviews/pending` | Get pending reviews | Admin |
| GET | `/api/admin/reviews` | Get reviews by status | Admin |
| POST | `/api/admin/reviews/{id}/approve` | Approve review | Admin |
| POST | `/api/admin/reviews/{id}/reject` | Reject review | Admin |
| POST | `/api/admin/reviews/{id}/flag` | Flag review | Admin |
| POST | `/api/admin/reviews/bulk-approve` | Bulk approve reviews | Admin |

#### Questions & Answers (`/api/admin/questions`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/questions/pending` | Get pending Q&A | Admin |
| GET | `/api/admin/questions` | Get questions by status | Admin |
| POST | `/api/admin/questions/{id}/approve` | Approve question | Admin |
| POST | `/api/admin/questions/{id}/reject` | Reject question | Admin |
| POST | `/api/admin/questions/{id}/flag` | Flag question | Admin |
| POST | `/api/admin/questions/bulk-approve` | Bulk approve questions | Admin |
| POST | `/api/admin/questions/answers/{id}/approve` | Approve answer | Admin |
| POST | `/api/admin/questions/answers/{id}/reject` | Reject answer | Admin |
| POST | `/api/admin/questions/answers/{id}/flag` | Flag answer | Admin |
| POST | `/api/admin/questions/answers/bulk-approve` | Bulk approve answers | Admin |

#### Dashboard (`/api/admin/dashboard`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/dashboard` | Get dashboard KPIs | Admin |
| GET | `/api/admin/dashboard/revenue` | Get revenue chart data | Admin |
| GET | `/api/admin/dashboard/orders-chart` | Get order status chart | Admin |
| GET | `/api/admin/dashboard/recent-orders` | Get recent orders | Admin |
| GET | `/api/admin/dashboard/low-stock` | Get low stock products | Admin |
| GET | `/api/admin/dashboard/top-products` | Get top selling products | Admin |

### Frontend Routes
| Route | Component | Guard |
|-------|-----------|-------|
| `/admin` | AdminDashboardComponent | adminGuard |
| `/admin/products` | AdminProductsComponent | adminGuard |
| `/admin/orders` | AdminOrdersComponent | adminGuard |
| `/admin/users` | AdminUsersComponent | adminGuard |
| `/admin/moderation` | AdminModerationComponent | adminGuard |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Controllers** | `src/ClimaSite.Api/Controllers/Admin/AdminProductsController.cs` |
| | `src/ClimaSite.Api/Controllers/Admin/AdminCategoriesController.cs` |
| | `src/ClimaSite.Api/Controllers/Admin/AdminReviewsController.cs` |
| | `src/ClimaSite.Api/Controllers/Admin/AdminQuestionsController.cs` |
| | `src/ClimaSite.Api/Controllers/AdminOrdersController.cs` |
| | `src/ClimaSite.Api/Controllers/AdminCustomersController.cs` |
| | `src/ClimaSite.Api/Controllers/AdminDashboardController.cs` |
| **Products Commands** | `src/ClimaSite.Application/Features/Admin/Products/Commands/CreateProductCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Products/Commands/UpdateProductCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Products/Commands/DeleteProductCommand.cs` |
| **Products Queries** | `src/ClimaSite.Application/Features/Admin/Products/Queries/GetAdminProductsQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Products/Queries/GetAdminProductByIdQuery.cs` |
| **Related Products** | `src/ClimaSite.Application/Features/Admin/RelatedProducts/Commands/AddRelatedProductCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/RelatedProducts/Commands/RemoveRelatedProductCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/RelatedProducts/Commands/ReorderRelatedProductsCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/RelatedProducts/Queries/GetProductRelationsQuery.cs` |
| **Translations** | `src/ClimaSite.Application/Features/Admin/Translations/Commands/AddProductTranslationCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Translations/Commands/UpdateProductTranslationCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Translations/Commands/DeleteProductTranslationCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Translations/Queries/GetProductTranslationsQuery.cs` |
| **Orders Commands** | `src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateOrderStatusCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Commands/UpdateShippingInfoCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Commands/AddOrderNoteCommand.cs` |
| **Orders Queries** | `src/ClimaSite.Application/Features/Admin/Orders/Queries/GetAdminOrdersQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/Queries/GetAdminOrderByIdQuery.cs` |
| **Customers** | `src/ClimaSite.Application/Features/Admin/Customers/Commands/UpdateCustomerStatusCommand.cs` |
| | `src/ClimaSite.Application/Features/Admin/Customers/Queries/GetAdminCustomersQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Customers/Queries/GetAdminCustomerByIdQuery.cs` |
| **Dashboard** | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetDashboardKpisQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetRevenueChartQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetOrderStatusChartQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetRecentOrdersQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetTopSellingProductsQuery.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetLowStockProductsQuery.cs` |
| **DTOs** | `src/ClimaSite.Application/Features/Admin/Products/DTOs/AdminProductDtos.cs` |
| | `src/ClimaSite.Application/Features/Admin/Orders/DTOs/AdminOrderDtos.cs` |
| | `src/ClimaSite.Application/Features/Admin/Customers/DTOs/AdminCustomerDtos.cs` |
| | `src/ClimaSite.Application/Features/Admin/Dashboard/DTOs/DashboardDtos.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| **Routes** | `src/ClimaSite.Web/src/app/features/admin/admin.routes.ts` |
| **Components** | `src/ClimaSite.Web/src/app/features/admin/admin-dashboard/admin-dashboard.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/products/admin-products.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/orders/admin-orders.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/users/admin-users.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/moderation/admin-moderation.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/products/components/product-translation-editor/product-translation-editor.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/products/components/related-products-manager/related-products-manager.component.ts` |
| **Services** | `src/ClimaSite.Web/src/app/features/admin/products/services/admin-translations.service.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/products/services/admin-related-products.service.ts` |
| | `src/ClimaSite.Web/src/app/core/services/moderation.service.ts` |
| **Guards** | `src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts` (adminGuard) |

---

## 3. Test Coverage Audit

### Unit Tests - Backend

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No dedicated admin controller/handler unit tests |

**Note:** No `AdminProductsControllerTests.cs`, `AdminOrdersControllerTests.cs`, or handler tests found for admin features.

### Unit Tests - Frontend

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `admin-translations.service.spec.ts` | `should be created` | Service instantiation |
| | `getProductTranslations - should fetch product translations` | GET translations |
| | `addTranslation - should add a new translation` | POST translation |
| | `updateTranslation - should update an existing translation` | PUT translation |
| | `deleteTranslation - should delete a translation` | DELETE translation |
| `admin-related-products.service.spec.ts` | `should be created` | Service instantiation |
| | `getProductRelations - should fetch product relations` | GET relations |
| | `addRelation - should add a new relation` | POST relation |
| | `removeRelation - should remove a relation` | DELETE relation |
| | `reorderRelations - should reorder relations` | PUT reorder |
| `product-translation-editor.component.spec.ts` | `should create` | Component creation |
| | `should load translations on init` | Data loading |
| | `should display language tabs` | UI rendering |
| | `should show English as default active language` | Default state |
| | `should change active language when tab clicked` | Tab interaction |
| | `should return correct hasTranslation result` | Logic test |
| | `should show loading state initially` | Loading state |
| | `should show default language notice for English` | Notice display |
| | `should show form for non-English languages` | Form rendering |
| `related-products-manager.component.spec.ts` | `should create` | Component creation |
| | `should load relations on init` | Data loading |
| | `should display relation type tabs` | UI rendering |
| | `should show relations for active type` | Data display |
| | `should change active type` | Tab interaction |
| | `should return correct count for relation type` | Count logic |
| | `should remove relation when remove button clicked` | Delete action |
| | `should show loading state initially` | Loading state |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No admin API integration tests |

**Note:** No `AdminProductsControllerTests.cs` or similar integration tests found.

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `tests/ClimaSite.E2E/Tests/Authentication/UserMenuTests.cs` | `AdminUser_SeesAdminLinkInDropdown` | Admin link visibility |
| | `RegularUser_DoesNotSeeAdminLink` | Non-admin restriction |

**Note:** No dedicated E2E tests for admin panel functionality (CRUD operations, moderation, dashboard).

### Existing Admin-Related Test References

From `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`:
- `CreateAdminUserAsync()` - Creates admin users for tests
- `EnsureAdminAuthAsync()` - Sets admin auth for API calls
- Admin product creation via `/api/admin/products`
- Admin category creation via `/api/admin/categories`

From `tests/ClimaSite.Application.Tests/Features/Orders`:
- `Handle_WhenAdminCancelsAnyOrder_Succeeds` - Admin order cancellation
- `Handle_WhenAdminAccessesAnyOrder_ReturnsOrder` - Admin order access

---

## 4. Manual Verification Steps

### Admin Authentication
1. Login as regular user
2. Navigate to `/admin` directly
3. Verify redirect to home page (access denied)
4. Login as admin user (or use test admin credentials)
5. Navigate to `/admin`
6. Verify admin dashboard loads successfully
7. Verify admin link visible in user dropdown menu

### Product Management
1. Navigate to `/admin/products`
2. Verify product list loads with pagination
3. Click "Add Product" button
4. Fill in product form (name, description, price, category, images)
5. Click "Save" and verify product created
6. Click edit on a product
7. Modify fields and save
8. Verify changes persisted
9. Toggle product status (active/inactive)
10. Toggle featured flag
11. Delete a product and verify removal

### Category Management
1. Access category management via admin
2. Create new category with parent selection
3. Reorder categories using drag-and-drop
4. Edit category name and description
5. Toggle category active status
6. Delete empty category (should succeed)
7. Attempt to delete category with products (should warn/prevent)

### Order Management
1. Navigate to `/admin/orders`
2. Verify order list with filters (status, date range)
3. Click on order to view details
4. Update order status (Processing -> Shipped)
5. Add tracking number and shipping method
6. Add internal note to order
7. Verify customer notification option works

### Customer Management
1. Navigate to `/admin/users`
2. View customer list with search
3. Click customer to view details
4. View customer's order history
5. Toggle customer active status
6. Verify deactivated customer cannot login

### Review Moderation
1. Navigate to `/admin/moderation`
2. Click "Reviews" tab
3. View pending reviews list
4. Approve a review - verify status changes
5. Reject a review with note - verify status and note
6. Flag a review for further investigation
7. Use bulk approve for multiple reviews
8. Verify approved reviews appear on product page

### Q&A Moderation
1. Navigate to `/admin/moderation`
2. Click "Questions" tab
3. View pending questions
4. Approve a question
5. Reject a question with note
6. Click "Answers" tab
7. Approve answer as regular response
8. Approve answer as official response (verified badge)
9. Reject answer with note
10. Use bulk approve for multiple items

### Translation Management
1. Navigate to product edit page
2. Click "Translations" tab
3. View existing translations (EN default, BG, DE)
4. Add new Bulgarian translation
5. Fill in all fields (name, description, meta fields)
6. Save translation
7. Update existing translation
8. Delete a translation
9. Switch site language and verify translations appear

### Related Products Management
1. Navigate to product edit page
2. Click "Related Products" tab
3. View relation types (Similar, Accessory, Upgrade, Bundle)
4. Add similar product using search
5. Reorder related products via drag-and-drop
6. Remove a related product
7. Verify relations appear on product page

### Dashboard Metrics
1. Navigate to `/admin`
2. Verify KPI cards display (revenue, orders, customers, products)
3. Check revenue chart loads with data
4. View order status distribution chart
5. View recent orders list
6. View low stock alerts
7. View top selling products
8. Test period filters (7d, 30d, 90d)

---

## 5. Gaps & Risks

### Critical Gaps

- [ ] **No backend unit tests for admin handlers** - All admin command/query handlers lack unit test coverage
- [ ] **No API integration tests** - Admin controllers have zero integration test coverage
- [ ] **No E2E tests for admin CRUD** - Product, category, order management untested
- [ ] **No E2E tests for moderation** - Review and Q&A moderation workflows untested
- [ ] **No E2E tests for dashboard** - Dashboard KPIs and charts untested
- [ ] **Placeholder admin components** - `AdminProductsComponent`, `AdminOrdersComponent`, `AdminUsersComponent` are stub implementations ("Coming soon...")

### Medium Gaps

- [ ] **No moderation service unit tests** - `ModerationService` lacks spec file
- [ ] **No adminGuard unit tests** - Guard logic untested
- [ ] **N+1 query problems** - Category reorder and bulk moderation have documented N+1 issues (see `AdminCategoriesController.cs:103`, `AdminReviewsController.cs:133`, `AdminQuestionsController.cs:200`)
- [ ] **Inconsistent controller locations** - Some admin controllers in `Controllers/Admin/` subfolder, others in root `Controllers/`
- [ ] **No pagination E2E tests** - Large dataset handling untested

### Security Risks

- [ ] **Role validation not tested** - Admin role requirement on endpoints not verified in tests
- [ ] **No RBAC E2E tests** - Role escalation prevention not tested
- [ ] **Bulk operation limits** - No limits on bulk approve operations (could be abused)
- [ ] **Audit logging** - Admin actions may not be logged for audit trail

### Technical Debt

- [ ] **Stub components need implementation** - Admin products, orders, users pages are placeholders
- [ ] **Missing dashboard frontend** - Dashboard component exists but lacks full implementation
- [ ] **Duplicate DTO locations** - Some DTOs in `Features/Admin/*/DTOs/`, others inline in controllers

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **Critical** | Stub admin components | Implement full CRUD UI for `AdminProductsComponent`, `AdminOrdersComponent`, `AdminUsersComponent` |
| **Critical** | No admin handler tests | Create unit tests for all admin command handlers in `tests/ClimaSite.Application.Tests/Features/Admin/` |
| **Critical** | No admin API tests | Create `tests/ClimaSite.Api.Tests/Controllers/AdminProductsControllerTests.cs` and similar for all admin controllers |
| **Critical** | No admin E2E tests | Create `tests/ClimaSite.E2E/Tests/Admin/` folder with tests for products, orders, moderation, dashboard |
| **High** | No moderation E2E tests | Create `tests/ClimaSite.E2E/Tests/Admin/ModerationTests.cs` covering approve/reject/flag flows |
| **High** | No dashboard E2E tests | Create `tests/ClimaSite.E2E/Tests/Admin/DashboardTests.cs` verifying KPIs and charts |
| **High** | No adminGuard tests | Create `auth.guard.spec.ts` with tests for `adminGuard` function |
| **Medium** | Inconsistent controller locations | Consolidate all admin controllers to `Controllers/Admin/` subfolder |
| **Medium** | N+1 query issues | Implement batch commands: `BatchUpdateCategorySortOrderCommand`, `BulkModerateReviewsCommand` |
| **Medium** | No bulk operation limits | Add configurable limits (e.g., max 100 items) to bulk approve endpoints |
| **Medium** | No moderation service tests | Create `moderation.service.spec.ts` with full method coverage |
| **Low** | Audit logging | Implement admin action audit trail in database |
| **Low** | Inline DTOs | Extract request/response records from controllers to separate DTO files |

### Recommended Test Files to Create

```
tests/
  ClimaSite.Application.Tests/
    Features/
      Admin/
        Products/
          CreateProductCommandTests.cs
          UpdateProductCommandTests.cs
          DeleteProductCommandTests.cs
        Orders/
          UpdateOrderStatusCommandTests.cs
          AddOrderNoteCommandTests.cs
        Dashboard/
          GetDashboardKpisQueryTests.cs
        Moderation/
          ModerateReviewCommandTests.cs
          ModerateQuestionCommandTests.cs
  ClimaSite.Api.Tests/
    Controllers/
      AdminProductsControllerTests.cs
      AdminCategoriesControllerTests.cs
      AdminOrdersControllerTests.cs
      AdminCustomersControllerTests.cs
      AdminReviewsControllerTests.cs
      AdminQuestionsControllerTests.cs
      AdminDashboardControllerTests.cs
  ClimaSite.E2E/
    Tests/
      Admin/
        ProductManagementTests.cs
        CategoryManagementTests.cs
        OrderManagementTests.cs
        CustomerManagementTests.cs
        ModerationTests.cs
        DashboardTests.cs
        TranslationManagementTests.cs
        RelatedProductsTests.cs
```

---

## 7. Evidence & Notes

### Admin Guard Implementation

From `src/ClimaSite.Web/src/app/auth/guards/auth.guard.ts`:
```typescript
export const adminGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return toObservable(authService.authReady).pipe(
    filter(ready => ready === true),
    take(1),
    map(() => {
      if (authService.isAuthenticated() && authService.isAdmin()) {
        return true;
      }
      if (authService.isAuthenticated()) {
        return router.createUrlTree(['/']);
      }
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url }
      });
    })
  );
};
```

The guard properly:
1. Waits for auth initialization before checking
2. Returns true only for authenticated admins
3. Redirects authenticated non-admins to home
4. Redirects unauthenticated users to login with return URL

### Authorization Pattern

All admin controllers use `[Authorize(Roles = "Admin")]` attribute:
```csharp
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
```

### N+1 Query Issues (Documented TODOs)

From `AdminCategoriesController.cs`:
```csharp
/// <remarks>
/// TODO: API-008 - This endpoint has an N+1 query problem. Each category update triggers
/// a separate database call. For better performance with large datasets, implement a
/// BatchUpdateCategorySortOrderCommand that updates all categories in a single transaction.
/// </remarks>
[HttpPatch("reorder")]
public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesRequest request)
{
    foreach (var item in request.Items)
    {
        var command = new UpdateCategoryCommand { Id = item.Id, SortOrder = item.SortOrder };
        await _mediator.Send(command);
    }
    return Ok(new { success = true });
}
```

### Test Data Factory Admin Support

From `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs`:
```csharp
public async Task<TestUser> CreateAdminUserAsync()
{
    // Create regular user first
    var user = await CreateUserAsync();
    
    // Elevate to admin via test endpoint
    var response = await _apiClient.PostAsJsonAsync("/api/test/elevate-admin", new
    {
        userId = user.Id,
        testSecret = Environment.GetEnvironmentVariable("TEST_ADMIN_SECRET") ?? "test-admin-secret"
    });

    // Re-authenticate to get admin token
    // ...
}
```

### Moderation Component Features

The `AdminModerationComponent` implements:
- Tabbed interface for Questions, Answers, Reviews
- Stats cards showing pending counts
- Approve/Reject/Flag actions for each item
- Bulk approve capability (via service)
- Loading states with spinners
- Error handling with retry option
- Responsive design for mobile

### Missing Frontend Implementations

The following admin components are placeholders:
```typescript
// AdminProductsComponent
template: `<div class="admin-products"><h1>Product Management</h1><p>Coming soon...</p></div>`

// AdminOrdersComponent  
template: `<div class="admin-orders"><h1>Order Management</h1><p>Coming soon...</p></div>`

// AdminUsersComponent
template: `<div class="admin-users"><h1>User Management</h1><p>Coming soon...</p></div>`
```

These require full implementation with:
- Data tables with sorting/filtering
- CRUD forms with validation
- Pagination controls
- Search functionality
- Bulk operations
