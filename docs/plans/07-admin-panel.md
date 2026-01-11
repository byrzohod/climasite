# Admin Panel Implementation Plan

## 1. Overview

The Admin Panel is a comprehensive administrative interface for managing the ClimaSite HVAC e-commerce platform. It provides authorized administrators with tools to manage products, orders, customers, inventory, discounts, and view business analytics through an intuitive dashboard.

### Goals
- Provide a secure, role-restricted administrative interface
- Enable efficient management of all e-commerce operations
- Deliver real-time business insights through KPI dashboards
- Support bulk operations for high-volume management tasks
- Ensure complete audit trails for all administrative actions

### Tech Stack
- **Backend**: ASP.NET Core .NET 10
- **Frontend**: Angular 19+ with standalone components
- **Database**: PostgreSQL
- **Charts**: ng2-charts (Chart.js wrapper)
- **UI Components**: Angular Material or PrimeNG
- **E2E Testing**: Playwright (NO MOCKING - real scenarios)

---

## 2. Admin Features

### 2.1 Dashboard KPIs

The dashboard provides at-a-glance metrics for business performance.

#### Key Performance Indicators
| KPI | Description | Time Periods |
|-----|-------------|--------------|
| Total Orders | Number of orders placed | Today, Week, Month, Year |
| Revenue | Total sales revenue | Today, Week, Month, Year |
| Average Order Value | Revenue / Orders | Week, Month |
| New Customers | Newly registered users | Today, Week, Month |
| Conversion Rate | Orders / Visitors | Week, Month |
| Low Stock Alerts | Products below threshold | Current |
| Pending Orders | Orders awaiting processing | Current |
| Returns/Refunds | Returned orders count | Week, Month |

#### Dashboard Widgets
1. **KPI Cards** - Real-time metrics with trend indicators
2. **Revenue Chart** - Line/bar chart showing 7/30 day revenue
3. **Orders by Status** - Pie chart of order status distribution
4. **Recent Orders** - Last 10 orders with quick actions
5. **Top Selling Products** - Best performers by quantity/revenue
6. **Low Stock Alerts** - Products requiring restocking
7. **Recent Reviews** - Latest customer reviews for moderation
8. **Activity Feed** - Recent admin actions log

---

### 2.2 Product Management

Full CRUD operations for product catalog management.

#### Features
- **Product List View**
  - Paginated table with sorting
  - Search by name, SKU, category
  - Filter by status, category, price range, stock level
  - Bulk actions (activate, deactivate, delete)

- **Product Create/Edit**
  - Basic info (name, SKU, description, brand)
  - Pricing (base price, sale price, cost)
  - Inventory (stock quantity, low stock threshold)
  - Category assignment (multi-select)
  - SEO fields (meta title, description, slug)
  - Specifications (key-value pairs)
  - Rich text editor for description

- **Product Variants**
  - Create variants (size, color, capacity)
  - Individual pricing per variant
  - Separate inventory tracking
  - Variant images

- **Image Management**
  - Multiple image upload
  - Drag-drop reordering
  - Primary image selection
  - Alt text for accessibility
  - Image cropping/resizing

- **Bulk Operations**
  - CSV/Excel import
  - CSV/Excel export
  - Bulk price updates
  - Bulk category assignment

---

### 2.3 Order Management

Complete order lifecycle management.

#### Features
- **Order List View**
  - Filter by status, date range, customer, payment status
  - Search by order number, customer email
  - Export orders to CSV

- **Order Detail View**
  - Customer information
  - Order items with images
  - Shipping details
  - Payment information
  - Order timeline/history
  - Internal notes

- **Order Status Management**
  - Status transitions: Pending -> Processing -> Shipped -> Delivered
  - Automatic email notifications on status change
  - Cancellation workflow
  - Partial fulfillment support

- **Refund Processing**
  - Full/partial refund
  - Refund reason tracking
  - Automatic stock restoration option
  - Payment gateway integration

- **Shipping**
  - Add tracking number
  - Select carrier
  - Print shipping labels (integration)
  - Estimated delivery dates

---

### 2.4 Customer Management

User account administration.

#### Features
- **Customer List View**
  - Search by name, email, phone
  - Filter by registration date, order count, total spent
  - Segment customers (new, returning, VIP)

- **Customer Detail View**
  - Profile information
  - Address book
  - Order history
  - Wishlist items
  - Cart contents
  - Account activity log

- **Account Actions**
  - Enable/disable account
  - Reset password (send email)
  - Verify email manually
  - Assign customer tier/group
  - Add internal notes

- **Customer Insights**
  - Lifetime value calculation
  - Average order value
  - Purchase frequency
  - Preferred categories

---

### 2.5 Category Management

Hierarchical category structure management.

#### Features
- **Category Tree View**
  - Visual tree structure
  - Drag-drop reordering
  - Nested categories (unlimited depth)
  - Expand/collapse all

- **Category CRUD**
  - Name and slug
  - Parent category selection
  - Description (rich text)
  - Category image
  - SEO fields
  - Display order

- **Category Settings**
  - Active/inactive status
  - Show in navigation
  - Featured category flag

---

### 2.6 Inventory Management

Stock control and alerts.

#### Features
- **Stock Overview**
  - Current stock levels
  - Reserved stock (in carts/pending orders)
  - Available stock calculation

- **Low Stock Alerts**
  - Configurable threshold per product
  - Alert notifications (email, dashboard)
  - Automatic reorder suggestions

- **Stock Adjustments**
  - Manual stock updates
  - Adjustment reasons (received, damaged, returned, etc.)
  - Batch updates via CSV
  - Stock audit history

- **Inventory Reports**
  - Stock valuation
  - Stock movement history
  - Dead stock identification
  - Inventory turnover

---

### 2.7 Discount Management

Promotional codes and discounts.

#### Features
- **Discount Types**
  - Percentage discount
  - Fixed amount discount
  - Free shipping
  - Buy X Get Y
  - Bundle discounts

- **Discount Rules**
  - Minimum order amount
  - Specific products/categories
  - Customer groups
  - First-time purchase only
  - Usage limit (total and per customer)
  - Date range validity

- **Discount Codes**
  - Manual code creation
  - Auto-generated codes
  - Bulk code generation
  - Code prefix/suffix

- **Discount Analytics**
  - Usage statistics
  - Revenue impact
  - Top performing discounts

---

### 2.8 Reports & Analytics

Business intelligence and reporting.

#### Features
- **Sales Reports**
  - Daily/weekly/monthly sales
  - Sales by category/product
  - Sales by region
  - Comparison periods

- **Customer Reports**
  - New vs returning customers
  - Customer acquisition
  - Geographic distribution

- **Inventory Reports**
  - Stock levels
  - Stock movement
  - Low stock report
  - Dead stock report

- **Export Options**
  - PDF reports
  - CSV/Excel export
  - Scheduled reports (email)

---

### 2.9 Settings & Configuration

System administration settings.

#### Features
- **General Settings**
  - Store name, logo
  - Contact information
  - Currency and locale
  - Tax configuration

- **Email Templates**
  - Order confirmation
  - Shipping notification
  - Password reset
  - Newsletter templates

- **Admin Users**
  - User list
  - Role assignment
  - Permission management
  - Activity logs

- **Integrations**
  - Payment gateway settings
  - Shipping provider settings
  - Analytics integration
  - Third-party APIs

---

## 3. API Endpoints (Admin)

All admin endpoints require authentication and Admin role authorization.

### 3.1 Dashboard Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/dashboard` | Get all dashboard KPIs |
| GET | `/api/v1/admin/dashboard/revenue` | Get revenue data with period filter |
| GET | `/api/v1/admin/dashboard/orders-chart` | Get orders chart data |
| GET | `/api/v1/admin/dashboard/top-products` | Get top selling products |
| GET | `/api/v1/admin/dashboard/recent-orders` | Get recent orders |
| GET | `/api/v1/admin/dashboard/low-stock` | Get low stock alerts |
| GET | `/api/v1/admin/dashboard/activity` | Get recent admin activity |

### 3.2 Product Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/products` | List all products (paginated) |
| GET | `/api/v1/admin/products/{id}` | Get product details |
| POST | `/api/v1/admin/products` | Create new product |
| PUT | `/api/v1/admin/products/{id}` | Update product |
| DELETE | `/api/v1/admin/products/{id}` | Soft delete product |
| POST | `/api/v1/admin/products/{id}/images` | Upload product images |
| DELETE | `/api/v1/admin/products/{id}/images/{imageId}` | Delete product image |
| PUT | `/api/v1/admin/products/{id}/images/reorder` | Reorder images |
| POST | `/api/v1/admin/products/bulk-import` | Import products from CSV |
| GET | `/api/v1/admin/products/export` | Export products to CSV |
| POST | `/api/v1/admin/products/bulk-update` | Bulk update products |
| POST | `/api/v1/admin/products/{id}/variants` | Add product variant |
| PUT | `/api/v1/admin/products/{id}/variants/{variantId}` | Update variant |
| DELETE | `/api/v1/admin/products/{id}/variants/{variantId}` | Delete variant |

### 3.3 Order Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/orders` | List all orders (paginated) |
| GET | `/api/v1/admin/orders/{id}` | Get order details |
| PUT | `/api/v1/admin/orders/{id}/status` | Update order status |
| POST | `/api/v1/admin/orders/{id}/notes` | Add internal note |
| POST | `/api/v1/admin/orders/{id}/refund` | Process refund |
| PUT | `/api/v1/admin/orders/{id}/shipping` | Update shipping info |
| GET | `/api/v1/admin/orders/export` | Export orders to CSV |

### 3.4 Customer Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/customers` | List all customers (paginated) |
| GET | `/api/v1/admin/customers/{id}` | Get customer details |
| PUT | `/api/v1/admin/customers/{id}` | Update customer |
| PUT | `/api/v1/admin/customers/{id}/status` | Enable/disable account |
| POST | `/api/v1/admin/customers/{id}/reset-password` | Send password reset |
| GET | `/api/v1/admin/customers/{id}/orders` | Get customer orders |
| POST | `/api/v1/admin/customers/{id}/notes` | Add internal note |

### 3.5 Category Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/categories` | List all categories (tree) |
| GET | `/api/v1/admin/categories/{id}` | Get category details |
| POST | `/api/v1/admin/categories` | Create category |
| PUT | `/api/v1/admin/categories/{id}` | Update category |
| DELETE | `/api/v1/admin/categories/{id}` | Delete category |
| PUT | `/api/v1/admin/categories/reorder` | Reorder categories |

### 3.6 Inventory Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/inventory` | List inventory levels |
| PUT | `/api/v1/admin/inventory/{productId}` | Update stock level |
| POST | `/api/v1/admin/inventory/adjustments` | Create stock adjustment |
| GET | `/api/v1/admin/inventory/adjustments` | Get adjustment history |
| GET | `/api/v1/admin/inventory/low-stock` | Get low stock products |
| POST | `/api/v1/admin/inventory/bulk-update` | Bulk stock update |

### 3.7 Discount Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/discounts` | List all discounts |
| GET | `/api/v1/admin/discounts/{id}` | Get discount details |
| POST | `/api/v1/admin/discounts` | Create discount |
| PUT | `/api/v1/admin/discounts/{id}` | Update discount |
| DELETE | `/api/v1/admin/discounts/{id}` | Delete discount |
| GET | `/api/v1/admin/discounts/{id}/usage` | Get discount usage stats |
| POST | `/api/v1/admin/discounts/generate-codes` | Generate bulk codes |

### 3.8 Reports Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/reports/sales` | Get sales report |
| GET | `/api/v1/admin/reports/customers` | Get customer report |
| GET | `/api/v1/admin/reports/inventory` | Get inventory report |
| GET | `/api/v1/admin/reports/products` | Get product performance |
| POST | `/api/v1/admin/reports/export` | Export report to file |

### 3.9 Settings Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/settings` | Get all settings |
| PUT | `/api/v1/admin/settings` | Update settings |
| GET | `/api/v1/admin/admin-users` | List admin users |
| POST | `/api/v1/admin/admin-users` | Create admin user |
| PUT | `/api/v1/admin/admin-users/{id}` | Update admin user |
| DELETE | `/api/v1/admin/admin-users/{id}` | Delete admin user |
| GET | `/api/v1/admin/audit-logs` | Get audit logs |

---

## 4. Data Models

### 4.1 Admin Dashboard DTOs

```csharp
public record DashboardKpiDto
{
    public KpiMetricDto TotalOrders { get; init; }
    public KpiMetricDto Revenue { get; init; }
    public KpiMetricDto NewCustomers { get; init; }
    public KpiMetricDto AverageOrderValue { get; init; }
    public int PendingOrders { get; init; }
    public int LowStockCount { get; init; }
}

public record KpiMetricDto
{
    public decimal Today { get; init; }
    public decimal ThisWeek { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal TrendPercentage { get; init; } // vs previous period
}

public record RevenueChartDto
{
    public List<ChartDataPointDto> DataPoints { get; init; }
    public string Period { get; init; } // "7d", "30d", "12m"
}

public record ChartDataPointDto
{
    public DateTime Date { get; init; }
    public decimal Value { get; init; }
    public string Label { get; init; }
}
```

### 4.2 Admin Product DTOs

```csharp
public record AdminProductListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public string Status { get; init; }
    public string PrimaryImageUrl { get; init; }
    public string CategoryName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Sku { get; init; }
    public string Slug { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? CostPrice { get; init; }
    public int StockQuantity { get; init; }
    public int LowStockThreshold { get; init; }
    public string Status { get; init; }
    public string Brand { get; init; }
    public List<Guid> CategoryIds { get; init; }
    public List<ProductImageDto> Images { get; init; }
    public List<ProductVariantDto> Variants { get; init; }
    public List<ProductSpecificationDto> Specifications { get; init; }
    public SeoMetadataDto SeoMetadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateProductCommand
{
    public string Name { get; init; }
    public string Sku { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? CostPrice { get; init; }
    public int StockQuantity { get; init; }
    public int LowStockThreshold { get; init; } = 10;
    public string Brand { get; init; }
    public List<Guid> CategoryIds { get; init; }
    public List<ProductSpecificationDto> Specifications { get; init; }
    public SeoMetadataDto SeoMetadata { get; init; }
}
```

### 4.3 Admin Order DTOs

```csharp
public record AdminOrderListItemDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; }
    public string CustomerName { get; init; }
    public string CustomerEmail { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; }
    public string PaymentStatus { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminOrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; }
    public CustomerSummaryDto Customer { get; init; }
    public List<OrderItemDetailDto> Items { get; init; }
    public AddressDto ShippingAddress { get; init; }
    public AddressDto BillingAddress { get; init; }
    public OrderPaymentDto Payment { get; init; }
    public OrderShippingDto Shipping { get; init; }
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; }
    public List<OrderTimelineEventDto> Timeline { get; init; }
    public List<OrderNoteDto> Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UpdateOrderStatusCommand
{
    public string Status { get; init; }
    public string Note { get; init; }
    public bool NotifyCustomer { get; init; } = true;
}

public record ProcessRefundCommand
{
    public decimal Amount { get; init; }
    public string Reason { get; init; }
    public bool RestockItems { get; init; }
    public List<RefundItemDto> Items { get; init; }
}
```

### 4.4 Discount DTOs

```csharp
public record DiscountDto
{
    public Guid Id { get; init; }
    public string Code { get; init; }
    public string Name { get; init; }
    public string Type { get; init; } // Percentage, FixedAmount, FreeShipping
    public decimal Value { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? UsageLimitPerCustomer { get; init; }
    public int TimesUsed { get; init; }
    public DateTime? StartsAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public List<Guid> ApplicableProductIds { get; init; }
    public List<Guid> ApplicableCategoryIds { get; init; }
    public bool FirstPurchaseOnly { get; init; }
}

public record CreateDiscountCommand
{
    public string Code { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }
    public decimal Value { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? UsageLimitPerCustomer { get; init; }
    public DateTime? StartsAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<Guid> ApplicableProductIds { get; init; }
    public List<Guid> ApplicableCategoryIds { get; init; }
    public bool FirstPurchaseOnly { get; init; }
}
```

---

## 5. Frontend Components

### 5.1 Admin Module Structure

```
src/app/admin/
├── admin.routes.ts
├── admin.component.ts
├── guards/
│   └── admin.guard.ts
├── layout/
│   ├── admin-layout.component.ts
│   ├── admin-sidebar.component.ts
│   └── admin-header.component.ts
├── dashboard/
│   ├── dashboard.component.ts
│   ├── kpi-card.component.ts
│   ├── revenue-chart.component.ts
│   ├── recent-orders.component.ts
│   └── low-stock-alerts.component.ts
├── products/
│   ├── product-list.component.ts
│   ├── product-form.component.ts
│   ├── product-images.component.ts
│   ├── product-variants.component.ts
│   └── product-import.component.ts
├── orders/
│   ├── order-list.component.ts
│   ├── order-detail.component.ts
│   ├── order-status.component.ts
│   └── refund-dialog.component.ts
├── customers/
│   ├── customer-list.component.ts
│   └── customer-detail.component.ts
├── categories/
│   ├── category-list.component.ts
│   ├── category-tree.component.ts
│   └── category-form.component.ts
├── inventory/
│   ├── inventory-list.component.ts
│   └── stock-adjustment.component.ts
├── discounts/
│   ├── discount-list.component.ts
│   └── discount-form.component.ts
├── reports/
│   ├── sales-report.component.ts
│   ├── customer-report.component.ts
│   └── inventory-report.component.ts
├── settings/
│   ├── settings.component.ts
│   ├── admin-users.component.ts
│   └── email-templates.component.ts
└── shared/
    ├── data-table.component.ts
    ├── confirm-dialog.component.ts
    ├── file-upload.component.ts
    └── rich-text-editor.component.ts
```

### 5.2 Admin Routes Configuration

```typescript
// admin.routes.ts
import { Routes } from '@angular/router';
import { adminGuard } from './guards/admin.guard';
import { AdminLayoutComponent } from './layout/admin-layout.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/dashboard.component')
          .then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./products/product-list.component')
          .then(m => m.ProductListComponent)
      },
      {
        path: 'products/new',
        loadComponent: () => import('./products/product-form.component')
          .then(m => m.ProductFormComponent)
      },
      {
        path: 'products/:id',
        loadComponent: () => import('./products/product-form.component')
          .then(m => m.ProductFormComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./orders/order-list.component')
          .then(m => m.OrderListComponent)
      },
      {
        path: 'orders/:id',
        loadComponent: () => import('./orders/order-detail.component')
          .then(m => m.OrderDetailComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./customers/customer-list.component')
          .then(m => m.CustomerListComponent)
      },
      {
        path: 'customers/:id',
        loadComponent: () => import('./customers/customer-detail.component')
          .then(m => m.CustomerDetailComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./categories/category-list.component')
          .then(m => m.CategoryListComponent)
      },
      {
        path: 'inventory',
        loadComponent: () => import('./inventory/inventory-list.component')
          .then(m => m.InventoryListComponent)
      },
      {
        path: 'discounts',
        loadComponent: () => import('./discounts/discount-list.component')
          .then(m => m.DiscountListComponent)
      },
      {
        path: 'discounts/new',
        loadComponent: () => import('./discounts/discount-form.component')
          .then(m => m.DiscountFormComponent)
      },
      {
        path: 'discounts/:id',
        loadComponent: () => import('./discounts/discount-form.component')
          .then(m => m.DiscountFormComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./reports/sales-report.component')
          .then(m => m.SalesReportComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./settings/settings.component')
          .then(m => m.SettingsComponent)
      },
    ]
  }
];
```

---

## 6. Implementation Tasks

### Phase 1: Foundation (Sprint 1-2)

#### Task ADM-001: Admin Route Guard
**Priority**: Critical
**Estimated Hours**: 4

**Description**: Implement route guard to restrict admin area access to authorized users only.

**Acceptance Criteria**:
- [ ] Create `adminGuard` as a functional guard
- [ ] Check if user is authenticated
- [ ] Verify user has Admin role from JWT claims
- [ ] Redirect unauthenticated users to login page
- [ ] Redirect non-admin users to 403 Forbidden page
- [ ] Show appropriate error messages
- [ ] Guard applies to all admin child routes

**Implementation**:
```typescript
// admin.guard.ts
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: '/admin' }
    });
    return false;
  }

  if (!authService.hasRole('Admin')) {
    router.navigate(['/403']);
    return false;
  }

  return true;
};
```

---

#### Task ADM-002: Admin Authorization Policy (Backend)
**Priority**: Critical
**Estimated Hours**: 6

**Description**: Implement server-side authorization policies for admin endpoints.

**Acceptance Criteria**:
- [ ] Create `AdminOnly` authorization policy
- [ ] Apply policy to all admin controllers
- [ ] Return 401 for unauthenticated requests
- [ ] Return 403 for authenticated non-admin users
- [ ] Log unauthorized access attempts
- [ ] Unit tests for authorization

**Implementation**:
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// AdminBaseController.cs
[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Policy = "AdminOnly")]
public abstract class AdminBaseController : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
}
```

---

#### Task ADM-003: Admin Layout Component
**Priority**: High
**Estimated Hours**: 8

**Description**: Create the main admin layout with sidebar navigation and header.

**Acceptance Criteria**:
- [ ] Responsive sidebar navigation
- [ ] Collapsible sidebar for mobile
- [ ] Header with admin user info
- [ ] Breadcrumb navigation
- [ ] Logout functionality
- [ ] Active link highlighting
- [ ] Smooth animations

**Implementation**:
```typescript
// admin-layout.component.ts
@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent, AdminHeaderComponent],
  template: `
    <div class="admin-layout" [class.sidebar-collapsed]="sidebarCollapsed()">
      <app-admin-sidebar
        [collapsed]="sidebarCollapsed()"
        (toggle)="toggleSidebar()" />
      <div class="admin-main">
        <app-admin-header
          (toggleSidebar)="toggleSidebar()"
          [user]="currentUser()" />
        <main class="admin-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent {
  private authService = inject(AuthService);

  sidebarCollapsed = signal(false);
  currentUser = this.authService.currentUser;

  toggleSidebar() {
    this.sidebarCollapsed.update(v => !v);
  }
}
```

---

#### Task ADM-004: Dashboard Component
**Priority**: High
**Estimated Hours**: 16

**Description**: Implement the main admin dashboard with KPIs and charts.

**Acceptance Criteria**:
- [ ] KPI cards showing orders, revenue, customers metrics
- [ ] Trend indicators (up/down percentages)
- [ ] Revenue chart (line chart, 7 days default)
- [ ] Orders by status pie chart
- [ ] Recent orders table (last 10)
- [ ] Low stock alerts list
- [ ] Top selling products list
- [ ] Data refreshes on load
- [ ] Loading states for each widget
- [ ] Error handling for failed data fetches

**Implementation**:
```typescript
// dashboard.component.ts
@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    KpiCardComponent,
    RevenueChartComponent,
    RecentOrdersComponent,
    LowStockAlertsComponent,
    NgChartsModule
  ],
  template: `
    <div class="dashboard">
      <h1>Dashboard</h1>

      <div class="kpi-grid">
        @if (loading()) {
          <app-kpi-card-skeleton [count]="4" />
        } @else {
          <app-kpi-card
            title="Orders"
            [value]="kpis()?.totalOrders.today"
            [trend]="kpis()?.totalOrders.trendPercentage"
            icon="shopping_cart" />
          <app-kpi-card
            title="Revenue"
            [value]="kpis()?.revenue.today"
            [trend]="kpis()?.revenue.trendPercentage"
            format="currency"
            icon="attach_money" />
          <app-kpi-card
            title="New Customers"
            [value]="kpis()?.newCustomers.today"
            [trend]="kpis()?.newCustomers.trendPercentage"
            icon="person_add" />
          <app-kpi-card
            title="Avg Order Value"
            [value]="kpis()?.averageOrderValue.thisWeek"
            format="currency"
            icon="trending_up" />
        }
      </div>

      <div class="charts-row">
        <app-revenue-chart [data]="revenueData()" />
        <app-orders-status-chart [data]="ordersStatusData()" />
      </div>

      <div class="lists-row">
        <app-recent-orders [orders]="recentOrders()" />
        <app-low-stock-alerts [products]="lowStockProducts()" />
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(AdminDashboardService);

  loading = signal(true);
  kpis = signal<DashboardKpiDto | null>(null);
  revenueData = signal<ChartDataPointDto[]>([]);
  recentOrders = signal<AdminOrderListItemDto[]>([]);
  lowStockProducts = signal<LowStockProductDto[]>([]);

  ngOnInit() {
    this.loadDashboardData();
  }

  private async loadDashboardData() {
    try {
      const [kpis, revenue, orders, lowStock] = await Promise.all([
        firstValueFrom(this.dashboardService.getKpis()),
        firstValueFrom(this.dashboardService.getRevenueChart('7d')),
        firstValueFrom(this.dashboardService.getRecentOrders()),
        firstValueFrom(this.dashboardService.getLowStockAlerts())
      ]);

      this.kpis.set(kpis);
      this.revenueData.set(revenue);
      this.recentOrders.set(orders);
      this.lowStockProducts.set(lowStock);
    } finally {
      this.loading.set(false);
    }
  }
}
```

---

#### Task ADM-005: Dashboard API Endpoints
**Priority**: High
**Estimated Hours**: 12

**Description**: Implement backend endpoints for dashboard data.

**Acceptance Criteria**:
- [ ] GET `/api/v1/admin/dashboard` returns all KPIs
- [ ] Efficient queries with proper indexing
- [ ] Cache frequently accessed metrics (5 min TTL)
- [ ] Period parameters (today, week, month)
- [ ] Trend calculations vs previous period
- [ ] Unit tests for calculations

**Implementation**:
```csharp
// AdminDashboardController.cs
[HttpGet]
public async Task<ActionResult<DashboardKpiDto>> GetDashboard()
{
    var kpis = await _dashboardService.GetKpisAsync();
    return Ok(kpis);
}

[HttpGet("revenue")]
public async Task<ActionResult<RevenueChartDto>> GetRevenueChart(
    [FromQuery] string period = "7d")
{
    var data = await _dashboardService.GetRevenueChartAsync(period);
    return Ok(data);
}

// DashboardService.cs
public async Task<DashboardKpiDto> GetKpisAsync()
{
    var today = DateTime.UtcNow.Date;
    var weekStart = today.AddDays(-(int)today.DayOfWeek);
    var monthStart = new DateTime(today.Year, today.Month, 1);

    var ordersToday = await _context.Orders
        .CountAsync(o => o.CreatedAt >= today);

    var revenueToday = await _context.Orders
        .Where(o => o.CreatedAt >= today && o.Status != OrderStatus.Cancelled)
        .SumAsync(o => o.TotalAmount);

    // ... more calculations

    return new DashboardKpiDto
    {
        TotalOrders = new KpiMetricDto
        {
            Today = ordersToday,
            ThisWeek = ordersWeek,
            ThisMonth = ordersMonth,
            TrendPercentage = CalculateTrend(ordersWeek, ordersPrevWeek)
        },
        // ...
    };
}
```

---

### Phase 2: Product Management (Sprint 3-4)

#### Task ADM-006: Product List Component
**Priority**: High
**Estimated Hours**: 12

**Description**: Implement product listing with search, filter, and pagination.

**Acceptance Criteria**:
- [ ] Paginated table with configurable page size
- [ ] Search by name, SKU
- [ ] Filter by category, status, price range
- [ ] Sort by name, price, stock, date
- [ ] Bulk selection with checkboxes
- [ ] Bulk actions (activate, deactivate, delete)
- [ ] Quick edit inline (price, stock)
- [ ] Product image thumbnails
- [ ] Link to edit page
- [ ] Export to CSV button

---

#### Task ADM-007: Product Form Component
**Priority**: High
**Estimated Hours**: 16

**Description**: Implement product create/edit form.

**Acceptance Criteria**:
- [ ] Form validation with error messages
- [ ] Basic info section (name, SKU, description)
- [ ] Pricing section (price, sale price, cost)
- [ ] Inventory section (stock, threshold)
- [ ] Category multi-select
- [ ] Brand selection/creation
- [ ] Specifications key-value editor
- [ ] SEO fields section
- [ ] Rich text editor for description
- [ ] Form dirty state tracking
- [ ] Unsaved changes warning
- [ ] Create and Edit modes

---

#### Task ADM-008: Product Image Management
**Priority**: High
**Estimated Hours**: 10

**Description**: Implement product image upload and management.

**Acceptance Criteria**:
- [ ] Multiple image upload (drag-drop)
- [ ] Image preview before upload
- [ ] Progress indicator during upload
- [ ] Drag-drop reordering
- [ ] Primary image selection
- [ ] Alt text editing
- [ ] Image deletion with confirmation
- [ ] Supported formats: JPG, PNG, WebP
- [ ] Max file size: 5MB
- [ ] Auto-resize/optimize on upload

---

#### Task ADM-009: Product Variants Management
**Priority**: Medium
**Estimated Hours**: 12

**Description**: Implement product variant creation and management.

**Acceptance Criteria**:
- [ ] Add variant options (e.g., BTU capacity, color)
- [ ] Generate variant combinations
- [ ] Individual SKU per variant
- [ ] Individual pricing per variant
- [ ] Individual stock per variant
- [ ] Variant images
- [ ] Enable/disable variants
- [ ] Bulk price adjustment

---

#### Task ADM-010: Product Import/Export
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement bulk product operations via CSV.

**Acceptance Criteria**:
- [ ] Export all products to CSV
- [ ] Export filtered products
- [ ] Download CSV template
- [ ] Import products from CSV
- [ ] Validation report for imports
- [ ] Preview before import
- [ ] Update existing vs create new option
- [ ] Progress indicator for large imports

---

#### Task ADM-011: Product API Endpoints
**Priority**: High
**Estimated Hours**: 16

**Description**: Implement all product management API endpoints.

**Acceptance Criteria**:
- [ ] All CRUD endpoints working
- [ ] Proper validation
- [ ] Image upload/management endpoints
- [ ] Variant management endpoints
- [ ] Bulk operations endpoints
- [ ] Soft delete implementation
- [ ] Audit logging for changes
- [ ] Unit and integration tests

---

### Phase 3: Order Management (Sprint 5-6)

#### Task ADM-012: Order List Component
**Priority**: High
**Estimated Hours**: 10

**Description**: Implement order listing with filters.

**Acceptance Criteria**:
- [ ] Paginated table
- [ ] Filter by status, date range, customer
- [ ] Search by order number, customer email
- [ ] Status color coding
- [ ] Quick status update
- [ ] Export to CSV
- [ ] Link to order detail

---

#### Task ADM-013: Order Detail Component
**Priority**: High
**Estimated Hours**: 14

**Description**: Implement comprehensive order detail view.

**Acceptance Criteria**:
- [ ] Order summary header
- [ ] Customer information section
- [ ] Order items with images and links
- [ ] Shipping address display
- [ ] Billing address display
- [ ] Payment information
- [ ] Order timeline/history
- [ ] Internal notes section
- [ ] Status update dropdown
- [ ] Shipping info update
- [ ] Refund button/dialog
- [ ] Print order button

---

#### Task ADM-014: Order Status Management
**Priority**: High
**Estimated Hours**: 8

**Description**: Implement order status transitions and notifications.

**Acceptance Criteria**:
- [ ] Valid status transitions only
- [ ] Status change reason/note
- [ ] Customer notification option
- [ ] Email sent on status change
- [ ] Status history recorded
- [ ] Webhook triggers for integrations

---

#### Task ADM-015: Refund Processing
**Priority**: High
**Estimated Hours**: 10

**Description**: Implement refund workflow.

**Acceptance Criteria**:
- [ ] Full refund option
- [ ] Partial refund with item selection
- [ ] Refund amount validation
- [ ] Reason selection/input
- [ ] Restock items option
- [ ] Payment gateway integration
- [ ] Refund confirmation email
- [ ] Refund history tracking

---

#### Task ADM-016: Order API Endpoints
**Priority**: High
**Estimated Hours**: 12

**Description**: Implement all order management API endpoints.

**Acceptance Criteria**:
- [ ] All endpoints implemented
- [ ] Status transition validation
- [ ] Refund processing
- [ ] Notes management
- [ ] Export functionality
- [ ] Audit logging
- [ ] Unit and integration tests

---

### Phase 4: Customer & Category Management (Sprint 7)

#### Task ADM-017: Customer List Component
**Priority**: Medium
**Estimated Hours**: 8

**Description**: Implement customer listing.

**Acceptance Criteria**:
- [ ] Paginated table
- [ ] Search by name, email
- [ ] Filter by registration date, order count
- [ ] Customer segments display
- [ ] Quick actions (enable/disable)
- [ ] Link to customer detail

---

#### Task ADM-018: Customer Detail Component
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement customer detail view.

**Acceptance Criteria**:
- [ ] Customer profile info
- [ ] Address book
- [ ] Order history list
- [ ] Customer statistics (LTV, AOV)
- [ ] Account actions (enable/disable, reset password)
- [ ] Internal notes
- [ ] Wishlist view
- [ ] Current cart view

---

#### Task ADM-019: Category Management
**Priority**: Medium
**Estimated Hours**: 12

**Description**: Implement category tree management.

**Acceptance Criteria**:
- [ ] Visual category tree
- [ ] Drag-drop reordering
- [ ] Drag-drop parent change
- [ ] Create category modal/form
- [ ] Edit category form
- [ ] Delete with confirmation
- [ ] Category image upload
- [ ] SEO fields
- [ ] Active/inactive toggle

---

#### Task ADM-020: Customer & Category API Endpoints
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement all customer and category API endpoints.

**Acceptance Criteria**:
- [ ] All CRUD endpoints
- [ ] Category tree endpoint
- [ ] Category reorder endpoint
- [ ] Customer statistics calculations
- [ ] Unit and integration tests

---

### Phase 5: Inventory & Discounts (Sprint 8)

#### Task ADM-021: Inventory List Component
**Priority**: Medium
**Estimated Hours**: 8

**Description**: Implement inventory management view.

**Acceptance Criteria**:
- [ ] Stock levels table
- [ ] Filter by low stock
- [ ] Quick stock update
- [ ] Stock adjustment modal
- [ ] Adjustment reason selection
- [ ] History view per product

---

#### Task ADM-022: Discount List Component
**Priority**: Medium
**Estimated Hours**: 8

**Description**: Implement discount code listing.

**Acceptance Criteria**:
- [ ] Discount codes table
- [ ] Filter by status, type
- [ ] Usage statistics display
- [ ] Quick enable/disable
- [ ] Copy code button
- [ ] Link to edit form

---

#### Task ADM-023: Discount Form Component
**Priority**: Medium
**Estimated Hours**: 12

**Description**: Implement discount create/edit form.

**Acceptance Criteria**:
- [ ] Discount type selection
- [ ] Value configuration
- [ ] Validity dates
- [ ] Usage limits
- [ ] Product/category restrictions
- [ ] Customer restrictions
- [ ] Preview discount behavior
- [ ] Form validation

---

#### Task ADM-024: Inventory & Discount API Endpoints
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement all inventory and discount API endpoints.

**Acceptance Criteria**:
- [ ] All CRUD endpoints
- [ ] Stock adjustment endpoint
- [ ] Discount validation logic
- [ ] Bulk code generation
- [ ] Usage statistics endpoint
- [ ] Unit and integration tests

---

### Phase 6: Reports & Settings (Sprint 9)

#### Task ADM-025: Sales Report Component
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement sales reporting.

**Acceptance Criteria**:
- [ ] Date range selection
- [ ] Sales summary metrics
- [ ] Sales chart (daily/weekly/monthly)
- [ ] Sales by category breakdown
- [ ] Sales by product breakdown
- [ ] Comparison with previous period
- [ ] Export to PDF/CSV

---

#### Task ADM-026: Reports API Endpoints
**Priority**: Medium
**Estimated Hours**: 10

**Description**: Implement reporting API endpoints.

**Acceptance Criteria**:
- [ ] Sales report endpoint with date filtering
- [ ] Customer report endpoint
- [ ] Inventory report endpoint
- [ ] Product performance endpoint
- [ ] Export generation
- [ ] Efficient queries for large datasets

---

#### Task ADM-027: Settings Component
**Priority**: Low
**Estimated Hours**: 8

**Description**: Implement admin settings management.

**Acceptance Criteria**:
- [ ] General settings form
- [ ] Email template editor
- [ ] Admin user management
- [ ] Role/permission display
- [ ] Audit log viewer

---

#### Task ADM-028: Admin User Management
**Priority**: Low
**Estimated Hours**: 8

**Description**: Implement admin user CRUD.

**Acceptance Criteria**:
- [ ] Admin users list
- [ ] Create admin user form
- [ ] Edit admin user
- [ ] Role assignment
- [ ] Deactivate admin user
- [ ] Activity log per user

---

### Phase 7: Polish & Testing (Sprint 10)

#### Task ADM-029: Admin UI Polish
**Priority**: Medium
**Estimated Hours**: 16

**Description**: UI/UX improvements across admin panel.

**Acceptance Criteria**:
- [ ] Consistent styling throughout
- [ ] Loading states for all components
- [ ] Error states and messages
- [ ] Empty states
- [ ] Keyboard navigation
- [ ] Accessibility audit and fixes
- [ ] Mobile responsiveness
- [ ] Dark mode support

---

#### Task ADM-030: Unit Tests
**Priority**: High
**Estimated Hours**: 20

**Description**: Comprehensive unit test coverage.

**Acceptance Criteria**:
- [ ] All services tested
- [ ] All components tested
- [ ] Guard tests
- [ ] Pipe and directive tests
- [ ] >80% code coverage
- [ ] CI/CD integration

---

## 7. E2E Tests (Playwright - NO MOCKING)

All E2E tests use real database operations, real API calls, and real user interactions. No mocking is allowed.

### Test Setup

```typescript
// e2e/admin/admin.setup.ts
import { test as setup, expect } from '@playwright/test';

// Helper to create admin user
export async function createAdminUser(request: APIRequestContext) {
  const email = `admin-${Date.now()}@climasite-test.com`;
  const password = 'AdminPass123!';

  // Register user
  await request.post('/api/v1/auth/register', {
    data: {
      email,
      password,
      firstName: 'Test',
      lastName: 'Admin'
    }
  });

  // Set admin role (via test helper endpoint or direct DB)
  await request.post('/api/v1/test/set-user-role', {
    data: { email, role: 'Admin' }
  });

  return { email, password };
}

// Helper to create test product
export async function createTestProduct(request: APIRequestContext, authToken: string) {
  const response = await request.post('/api/v1/admin/products', {
    headers: { Authorization: `Bearer ${authToken}` },
    data: {
      name: `Test AC Unit ${Date.now()}`,
      sku: `TEST-AC-${Date.now()}`,
      description: 'Test product for E2E testing',
      price: 999.99,
      stockQuantity: 100,
      categoryIds: ['air-conditioners-category-id']
    }
  });
  return response.json();
}

// Helper to login and get auth token
export async function loginAndGetToken(
  request: APIRequestContext,
  email: string,
  password: string
) {
  const response = await request.post('/api/v1/auth/login', {
    data: { email, password }
  });
  const data = await response.json();
  return data.accessToken;
}
```

---

### Test ADM-E2E-001: Admin Access Control

```typescript
// e2e/admin/admin-access.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser } from './admin.setup';

test.describe('Admin Access Control', () => {

  test('non-authenticated user cannot access admin panel', async ({ page }) => {
    // Attempt to access admin dashboard without login
    await page.goto('/admin/dashboard');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText('Please log in to continue')).toBeVisible();
  });

  test('regular user cannot access admin panel', async ({ page, request }) => {
    // Create regular user (not admin)
    const email = `user-${Date.now()}@climasite-test.com`;
    const password = 'UserPass123!';

    await request.post('/api/v1/auth/register', {
      data: { email, password, firstName: 'Regular', lastName: 'User' }
    });

    // Login as regular user
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Wait for login to complete
    await expect(page).toHaveURL('/');

    // Try to access admin
    await page.goto('/admin/dashboard');

    // Should see 403 forbidden page
    await expect(page).toHaveURL('/403');
    await expect(page.getByText('Access Denied')).toBeVisible();
    await expect(page.getByText('You do not have permission')).toBeVisible();
  });

  test('admin user can access admin panel', async ({ page, request }) => {
    // Create admin user
    const { email, password } = await createAdminUser(request);

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Navigate to admin
    await page.goto('/admin/dashboard');

    // Should see admin dashboard
    await expect(page).toHaveURL('/admin/dashboard');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByTestId('admin-sidebar')).toBeVisible();
  });
});
```

---

### Test ADM-E2E-002: Dashboard KPIs

```typescript
// e2e/admin/dashboard.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Dashboard', () => {

  test('dashboard displays real-time KPIs', async ({ page, request }) => {
    // Setup: Create admin and some test data
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create a test order to ensure KPIs have data
    const product = await createTestProduct(request, token);

    // Create a customer and place an order
    const customerEmail = `customer-${Date.now()}@climasite-test.com`;
    await request.post('/api/v1/auth/register', {
      data: {
        email: customerEmail,
        password: 'CustPass123!',
        firstName: 'Test',
        lastName: 'Customer'
      }
    });

    const customerToken = await loginAndGetToken(request, customerEmail, 'CustPass123!');

    // Add to cart and checkout
    await request.post('/api/v1/cart/items', {
      headers: { Authorization: `Bearer ${customerToken}` },
      data: { productId: product.id, quantity: 1 }
    });

    await request.post('/api/v1/orders', {
      headers: { Authorization: `Bearer ${customerToken}` },
      data: {
        shippingAddressId: 'test-address-id',
        paymentMethodId: 'test-payment-method'
      }
    });

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to dashboard
    await page.goto('/admin/dashboard');

    // Verify KPI cards are displayed
    await expect(page.getByTestId('kpi-orders')).toBeVisible();
    await expect(page.getByTestId('kpi-revenue')).toBeVisible();
    await expect(page.getByTestId('kpi-customers')).toBeVisible();
    await expect(page.getByTestId('kpi-avg-order')).toBeVisible();

    // Verify charts are rendered
    await expect(page.getByTestId('revenue-chart')).toBeVisible();
    await expect(page.getByTestId('orders-status-chart')).toBeVisible();

    // Verify recent orders shows our test order
    await expect(page.getByTestId('recent-orders-table')).toBeVisible();
    await expect(page.getByText(customerEmail)).toBeVisible();
  });

  test('low stock alerts show products below threshold', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create product with low stock
    const lowStockProduct = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Low Stock AC ${Date.now()}`,
        sku: `LOW-STOCK-${Date.now()}`,
        description: 'Product with low stock',
        price: 599.99,
        stockQuantity: 3,
        lowStockThreshold: 10,
        categoryIds: []
      }
    });
    const product = await lowStockProduct.json();

    // Login and go to dashboard
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/dashboard');

    // Verify low stock alert is shown
    await expect(page.getByTestId('low-stock-alerts')).toBeVisible();
    await expect(page.getByText(product.name)).toBeVisible();
    await expect(page.getByText('3 in stock')).toBeVisible();
  });
});
```

---

### Test ADM-E2E-003: Product Management

```typescript
// e2e/admin/products.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser } from './admin.setup';

test.describe('Admin Product Management', () => {

  test('admin can create a new product', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Navigate to products
    await page.goto('/admin/products');
    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();

    // Click add product
    await page.click('[data-testid="add-product-button"]');
    await expect(page).toHaveURL('/admin/products/new');

    // Fill product form
    const productName = `Test Split AC ${Date.now()}`;
    const sku = `TEST-SPLIT-${Date.now()}`;

    await page.fill('[data-testid="product-name"]', productName);
    await page.fill('[data-testid="product-sku"]', sku);
    await page.fill('[data-testid="product-price"]', '1499.99');
    await page.fill('[data-testid="product-sale-price"]', '1299.99');
    await page.fill('[data-testid="product-stock"]', '50');
    await page.fill('[data-testid="product-low-stock-threshold"]', '10');

    // Select category
    await page.click('[data-testid="category-select"]');
    await page.click('[data-testid="category-option-air-conditioners"]');

    // Fill description (rich text editor)
    await page.fill('[data-testid="product-description"]',
      'High-efficiency split air conditioner with inverter technology.');

    // Add specification
    await page.click('[data-testid="add-specification"]');
    await page.fill('[data-testid="spec-key-0"]', 'BTU');
    await page.fill('[data-testid="spec-value-0"]', '18000');

    // Fill SEO fields
    await page.fill('[data-testid="seo-title"]', `${productName} | ClimaSite`);
    await page.fill('[data-testid="seo-description"]',
      'Buy the best split AC with inverter technology.');

    // Save product
    await page.click('[data-testid="save-product"]');

    // Verify success
    await expect(page.getByText('Product created successfully')).toBeVisible();
    await expect(page).toHaveURL('/admin/products');

    // Verify product appears in list
    await page.fill('[data-testid="search-input"]', sku);
    await expect(page.getByText(productName)).toBeVisible();
    await expect(page.getByText('$1,299.99')).toBeVisible();
  });

  test('admin can edit an existing product', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create a product via API
    const productResponse = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Edit Test AC ${Date.now()}`,
        sku: `EDIT-TEST-${Date.now()}`,
        description: 'Original description',
        price: 799.99,
        stockQuantity: 25,
        categoryIds: []
      }
    });
    const product = await productResponse.json();

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to product edit page
    await page.goto(`/admin/products/${product.id}`);

    // Verify existing data is loaded
    await expect(page.locator('[data-testid="product-name"]')).toHaveValue(product.name);
    await expect(page.locator('[data-testid="product-price"]')).toHaveValue('799.99');

    // Update price
    await page.fill('[data-testid="product-price"]', '899.99');
    await page.fill('[data-testid="product-description"]', 'Updated description with new features.');

    // Save changes
    await page.click('[data-testid="save-product"]');

    // Verify success
    await expect(page.getByText('Product updated successfully')).toBeVisible();

    // Verify changes via API
    const updatedResponse = await request.get(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const updatedProduct = await updatedResponse.json();
    expect(updatedProduct.price).toBe(899.99);
    expect(updatedProduct.description).toBe('Updated description with new features.');
  });

  test('admin can delete a product', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create a product via API
    const productResponse = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Delete Test AC ${Date.now()}`,
        sku: `DELETE-TEST-${Date.now()}`,
        description: 'Product to be deleted',
        price: 499.99,
        stockQuantity: 10,
        categoryIds: []
      }
    });
    const product = await productResponse.json();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to products list
    await page.goto('/admin/products');

    // Search for the product
    await page.fill('[data-testid="search-input"]', product.sku);
    await page.waitForTimeout(500); // Debounce

    // Click delete button on the product row
    await page.click(`[data-testid="delete-product-${product.id}"]`);

    // Confirm deletion in dialog
    await expect(page.getByText('Delete Product?')).toBeVisible();
    await expect(page.getByText('This action cannot be undone')).toBeVisible();
    await page.click('[data-testid="confirm-delete"]');

    // Verify success
    await expect(page.getByText('Product deleted successfully')).toBeVisible();

    // Verify product no longer visible in list
    await expect(page.getByText(product.name)).not.toBeVisible();

    // Verify via API that product is soft-deleted
    const checkResponse = await request.get(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(checkResponse.status()).toBe(404);
  });

  test('admin can upload product images', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create a product
    const productResponse = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Image Test AC ${Date.now()}`,
        sku: `IMG-TEST-${Date.now()}`,
        description: 'Product for image testing',
        price: 699.99,
        stockQuantity: 20,
        categoryIds: []
      }
    });
    const product = await productResponse.json();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to product edit page
    await page.goto(`/admin/products/${product.id}`);

    // Upload image
    const fileInput = page.locator('[data-testid="image-upload-input"]');
    await fileInput.setInputFiles('./e2e/fixtures/test-product-image.jpg');

    // Wait for upload to complete
    await expect(page.getByText('Image uploaded')).toBeVisible();

    // Verify image appears in gallery
    await expect(page.getByTestId('product-image-0')).toBeVisible();

    // Set as primary
    await page.click('[data-testid="set-primary-image-0"]');
    await expect(page.getByText('Primary image updated')).toBeVisible();

    // Save and verify
    await page.click('[data-testid="save-product"]');

    // Verify via API
    const updatedResponse = await request.get(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const updatedProduct = await updatedResponse.json();
    expect(updatedProduct.images.length).toBeGreaterThan(0);
  });

  test('admin can filter and search products', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create multiple products with different attributes
    const products = [
      { name: 'Budget AC Unit', sku: `BUD-${Date.now()}`, price: 299.99, stockQuantity: 50 },
      { name: 'Premium AC Unit', sku: `PRE-${Date.now()}`, price: 1999.99, stockQuantity: 5 },
      { name: 'Mid-range AC Unit', sku: `MID-${Date.now()}`, price: 899.99, stockQuantity: 0 }
    ];

    for (const p of products) {
      await request.post('/api/v1/admin/products', {
        headers: { Authorization: `Bearer ${token}` },
        data: { ...p, description: 'Test product', categoryIds: [] }
      });
    }

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/products');

    // Test search by name
    await page.fill('[data-testid="search-input"]', 'Premium');
    await page.waitForTimeout(500);
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();

    // Clear search
    await page.fill('[data-testid="search-input"]', '');

    // Test filter by price range
    await page.click('[data-testid="filter-button"]');
    await page.fill('[data-testid="price-min"]', '500');
    await page.fill('[data-testid="price-max"]', '1000');
    await page.click('[data-testid="apply-filters"]');

    await expect(page.getByText('Mid-range AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
    await expect(page.getByText('Premium AC Unit')).not.toBeVisible();

    // Test filter by stock status (out of stock)
    await page.click('[data-testid="clear-filters"]');
    await page.click('[data-testid="filter-button"]');
    await page.click('[data-testid="stock-filter-out-of-stock"]');
    await page.click('[data-testid="apply-filters"]');

    await expect(page.getByText('Mid-range AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
  });
});
```

---

### Test ADM-E2E-004: Order Management

```typescript
// e2e/admin/orders.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Order Management', () => {

  async function createTestOrder(request: APIRequestContext, adminToken: string) {
    // Create product
    const product = await createTestProduct(request, adminToken);

    // Create customer
    const customerEmail = `customer-${Date.now()}@climasite-test.com`;
    await request.post('/api/v1/auth/register', {
      data: {
        email: customerEmail,
        password: 'CustPass123!',
        firstName: 'Order',
        lastName: 'Tester'
      }
    });

    const customerToken = await loginAndGetToken(request, customerEmail, 'CustPass123!');

    // Add address
    await request.post('/api/v1/addresses', {
      headers: { Authorization: `Bearer ${customerToken}` },
      data: {
        street: '123 Test St',
        city: 'Test City',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
        isDefault: true
      }
    });

    // Add to cart
    await request.post('/api/v1/cart/items', {
      headers: { Authorization: `Bearer ${customerToken}` },
      data: { productId: product.id, quantity: 2 }
    });

    // Place order
    const orderResponse = await request.post('/api/v1/orders', {
      headers: { Authorization: `Bearer ${customerToken}` },
      data: { paymentMethodId: 'test-payment' }
    });

    return {
      order: await orderResponse.json(),
      customer: { email: customerEmail },
      product
    };
  }

  test('admin can view order list with filters', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create test orders
    const { order } = await createTestOrder(request, token);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to orders
    await page.goto('/admin/orders');

    // Verify orders table
    await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
    await expect(page.getByTestId('orders-table')).toBeVisible();
    await expect(page.getByText(order.orderNumber)).toBeVisible();

    // Test filter by status
    await page.selectOption('[data-testid="status-filter"]', 'pending');
    await expect(page.getByText(order.orderNumber)).toBeVisible();

    await page.selectOption('[data-testid="status-filter"]', 'shipped');
    await expect(page.getByText(order.orderNumber)).not.toBeVisible();
  });

  test('admin can update order status', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    const { order, customer } = await createTestOrder(request, token);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to order detail
    await page.goto(`/admin/orders/${order.id}`);

    // Verify order details
    await expect(page.getByText(order.orderNumber)).toBeVisible();
    await expect(page.getByText(customer.email)).toBeVisible();
    await expect(page.getByText('Pending')).toBeVisible();

    // Update status to Processing
    await page.selectOption('[data-testid="order-status-select"]', 'processing');
    await page.fill('[data-testid="status-note"]', 'Order confirmed and being prepared');
    await page.check('[data-testid="notify-customer"]');
    await page.click('[data-testid="update-status-button"]');

    // Verify status updated
    await expect(page.getByText('Status updated successfully')).toBeVisible();
    await expect(page.getByText('Processing')).toBeVisible();

    // Verify timeline shows update
    await expect(page.getByTestId('order-timeline')).toContainText('Status changed to Processing');
    await expect(page.getByTestId('order-timeline')).toContainText('Order confirmed and being prepared');

    // Update to Shipped
    await page.selectOption('[data-testid="order-status-select"]', 'shipped');
    await page.fill('[data-testid="tracking-number"]', 'TRACK123456789');
    await page.selectOption('[data-testid="carrier-select"]', 'fedex');
    await page.click('[data-testid="update-status-button"]');

    await expect(page.getByText('Shipped')).toBeVisible();
    await expect(page.getByText('TRACK123456789')).toBeVisible();

    // Verify via API
    const updatedOrder = await request.get(`/api/v1/admin/orders/${order.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const orderData = await updatedOrder.json();
    expect(orderData.status).toBe('shipped');
    expect(orderData.shipping.trackingNumber).toBe('TRACK123456789');
  });

  test('admin can process a refund', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    const { order, product } = await createTestOrder(request, token);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to order detail
    await page.goto(`/admin/orders/${order.id}`);

    // Click refund button
    await page.click('[data-testid="refund-button"]');

    // Refund dialog should open
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByText('Process Refund')).toBeVisible();

    // Select partial refund
    await page.click('[data-testid="refund-type-partial"]');
    await page.fill('[data-testid="refund-amount"]', '999.99');
    await page.selectOption('[data-testid="refund-reason"]', 'customer_request');
    await page.fill('[data-testid="refund-notes"]', 'Customer requested refund for one item');
    await page.check('[data-testid="restock-items"]');

    // Confirm refund
    await page.click('[data-testid="confirm-refund"]');

    // Verify success
    await expect(page.getByText('Refund processed successfully')).toBeVisible();

    // Verify refund appears in order timeline
    await expect(page.getByTestId('order-timeline')).toContainText('Refund of $999.99');

    // Verify stock was restored
    const productResponse = await request.get(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const productData = await productResponse.json();
    expect(productData.stockQuantity).toBe(101); // Original 100 - 2 ordered + 1 refunded
  });

  test('admin can add internal notes to order', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    const { order } = await createTestOrder(request, token);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to order detail
    await page.goto(`/admin/orders/${order.id}`);

    // Add internal note
    await page.fill('[data-testid="add-note-input"]', 'Customer called to confirm delivery address');
    await page.click('[data-testid="add-note-button"]');

    // Verify note added
    await expect(page.getByText('Note added')).toBeVisible();
    await expect(page.getByTestId('order-notes')).toContainText('Customer called to confirm delivery address');

    // Verify note persists after reload
    await page.reload();
    await expect(page.getByTestId('order-notes')).toContainText('Customer called to confirm delivery address');
  });
});
```

---

### Test ADM-E2E-005: Customer Management

```typescript
// e2e/admin/customers.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken } from './admin.setup';

test.describe('Admin Customer Management', () => {

  test('admin can view customer list and details', async ({ page, request }) => {
    const { email: adminEmail, password } = await createAdminUser(request);

    // Create test customers
    const customers = [
      { email: `vip-${Date.now()}@test.com`, firstName: 'VIP', lastName: 'Customer' },
      { email: `new-${Date.now()}@test.com`, firstName: 'New', lastName: 'Customer' }
    ];

    for (const c of customers) {
      await request.post('/api/v1/auth/register', {
        data: { ...c, password: 'Pass123!' }
      });
    }

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', adminEmail);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to customers
    await page.goto('/admin/customers');

    // Verify customer list
    await expect(page.getByRole('heading', { name: 'Customers' })).toBeVisible();
    await expect(page.getByText(customers[0].email)).toBeVisible();
    await expect(page.getByText(customers[1].email)).toBeVisible();

    // Search for specific customer
    await page.fill('[data-testid="search-input"]', 'VIP');
    await page.waitForTimeout(500);
    await expect(page.getByText(customers[0].email)).toBeVisible();
    await expect(page.getByText(customers[1].email)).not.toBeVisible();

    // Click to view customer details
    await page.click(`[data-testid="view-customer-${customers[0].email}"]`);

    // Verify customer detail page
    await expect(page.getByRole('heading', { name: 'VIP Customer' })).toBeVisible();
    await expect(page.getByText(customers[0].email)).toBeVisible();
    await expect(page.getByTestId('customer-orders')).toBeVisible();
    await expect(page.getByTestId('customer-stats')).toBeVisible();
  });

  test('admin can disable and enable customer account', async ({ page, request }) => {
    const { email: adminEmail, password } = await createAdminUser(request);

    // Create test customer
    const customerEmail = `disable-test-${Date.now()}@test.com`;
    await request.post('/api/v1/auth/register', {
      data: {
        email: customerEmail,
        password: 'Pass123!',
        firstName: 'Disable',
        lastName: 'Test'
      }
    });

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', adminEmail);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to customer detail
    await page.goto('/admin/customers');
    await page.fill('[data-testid="search-input"]', customerEmail);
    await page.waitForTimeout(500);
    await page.click(`[data-testid="view-customer-${customerEmail}"]`);

    // Disable account
    await page.click('[data-testid="disable-account-button"]');
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.fill('[data-testid="disable-reason"]', 'Suspicious activity detected');
    await page.click('[data-testid="confirm-disable"]');

    // Verify disabled
    await expect(page.getByText('Account disabled')).toBeVisible();
    await expect(page.getByTestId('account-status')).toContainText('Disabled');

    // Verify customer cannot login
    const loginResponse = await request.post('/api/v1/auth/login', {
      data: { email: customerEmail, password: 'Pass123!' }
    });
    expect(loginResponse.status()).toBe(403);

    // Re-enable account
    await page.click('[data-testid="enable-account-button"]');
    await expect(page.getByText('Account enabled')).toBeVisible();
    await expect(page.getByTestId('account-status')).toContainText('Active');

    // Verify customer can now login
    const loginResponse2 = await request.post('/api/v1/auth/login', {
      data: { email: customerEmail, password: 'Pass123!' }
    });
    expect(loginResponse2.status()).toBe(200);
  });
});
```

---

### Test ADM-E2E-006: Category Management

```typescript
// e2e/admin/categories.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken } from './admin.setup';

test.describe('Admin Category Management', () => {

  test('admin can create and manage categories', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to categories
    await page.goto('/admin/categories');

    // Create parent category
    await page.click('[data-testid="add-category-button"]');

    const parentName = `HVAC Systems ${Date.now()}`;
    await page.fill('[data-testid="category-name"]', parentName);
    await page.fill('[data-testid="category-description"]', 'All HVAC system products');
    await page.fill('[data-testid="category-slug"]', `hvac-systems-${Date.now()}`);
    await page.click('[data-testid="save-category"]');

    await expect(page.getByText('Category created')).toBeVisible();
    await expect(page.getByText(parentName)).toBeVisible();

    // Create child category
    await page.click('[data-testid="add-category-button"]');

    const childName = `Split AC Units ${Date.now()}`;
    await page.fill('[data-testid="category-name"]', childName);
    await page.selectOption('[data-testid="parent-category"]', parentName);
    await page.click('[data-testid="save-category"]');

    await expect(page.getByText(childName)).toBeVisible();

    // Verify tree structure
    await expect(page.getByTestId('category-tree')).toContainText(parentName);

    // Edit category
    await page.click(`[data-testid="edit-category-${childName}"]`);
    await page.fill('[data-testid="category-name"]', `${childName} Updated`);
    await page.click('[data-testid="save-category"]');

    await expect(page.getByText('Category updated')).toBeVisible();
    await expect(page.getByText(`${childName} Updated`)).toBeVisible();
  });

  test('admin can reorder categories via drag-drop', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create test categories
    const categories = ['Category A', 'Category B', 'Category C'].map(name => ({
      name: `${name} ${Date.now()}`,
      slug: `${name.toLowerCase().replace(' ', '-')}-${Date.now()}`
    }));

    for (const cat of categories) {
      await request.post('/api/v1/admin/categories', {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: cat.name, slug: cat.slug, description: 'Test category' }
      });
    }

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/categories');

    // Perform drag-drop to reorder
    const catA = page.locator(`[data-testid="category-item-${categories[0].name}"]`);
    const catC = page.locator(`[data-testid="category-item-${categories[2].name}"]`);

    await catA.dragTo(catC);

    // Verify reorder success message
    await expect(page.getByText('Categories reordered')).toBeVisible();

    // Verify new order via API
    const response = await request.get('/api/v1/admin/categories', {
      headers: { Authorization: `Bearer ${token}` }
    });
    const cats = await response.json();

    // Category A should now be after Category C
    const indexA = cats.findIndex((c: any) => c.name === categories[0].name);
    const indexC = cats.findIndex((c: any) => c.name === categories[2].name);
    expect(indexA).toBeGreaterThan(indexC);
  });
});
```

---

### Test ADM-E2E-007: Discount Management

```typescript
// e2e/admin/discounts.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Discount Management', () => {

  test('admin can create a percentage discount', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to discounts
    await page.goto('/admin/discounts');
    await page.click('[data-testid="add-discount-button"]');

    // Fill discount form
    const discountCode = `SAVE20-${Date.now()}`;
    await page.fill('[data-testid="discount-code"]', discountCode);
    await page.fill('[data-testid="discount-name"]', '20% Off Summer Sale');
    await page.selectOption('[data-testid="discount-type"]', 'percentage');
    await page.fill('[data-testid="discount-value"]', '20');
    await page.fill('[data-testid="min-order-amount"]', '100');
    await page.fill('[data-testid="usage-limit"]', '1000');
    await page.fill('[data-testid="usage-limit-per-customer"]', '1');

    // Set validity dates
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);

    await page.fill('[data-testid="starts-at"]', tomorrow.toISOString().split('T')[0]);
    await page.fill('[data-testid="expires-at"]', nextMonth.toISOString().split('T')[0]);

    await page.click('[data-testid="save-discount"]');

    // Verify created
    await expect(page.getByText('Discount created successfully')).toBeVisible();
    await expect(page).toHaveURL('/admin/discounts');
    await expect(page.getByText(discountCode)).toBeVisible();
    await expect(page.getByText('20%')).toBeVisible();
  });

  test('admin can create a free shipping discount', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/discounts/new');

    const discountCode = `FREESHIP-${Date.now()}`;
    await page.fill('[data-testid="discount-code"]', discountCode);
    await page.fill('[data-testid="discount-name"]', 'Free Shipping on Orders $50+');
    await page.selectOption('[data-testid="discount-type"]', 'free_shipping');
    await page.fill('[data-testid="min-order-amount"]', '50');

    await page.click('[data-testid="save-discount"]');

    await expect(page.getByText('Discount created successfully')).toBeVisible();
    await expect(page.getByText(discountCode)).toBeVisible();
    await expect(page.getByText('Free Shipping')).toBeVisible();
  });

  test('discount code works in checkout flow', async ({ page, request }) => {
    const { email: adminEmail, password: adminPassword } = await createAdminUser(request);
    const token = await loginAndGetToken(request, adminEmail, adminPassword);

    // Create product
    const product = await createTestProduct(request, token);

    // Create discount
    const discountCode = `TEST10-${Date.now()}`;
    await request.post('/api/v1/admin/discounts', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        code: discountCode,
        name: '10% Off Test',
        type: 'percentage',
        value: 10,
        minimumOrderAmount: 0,
        usageLimit: 100,
        isActive: true
      }
    });

    // Create customer and login
    const customerEmail = `discount-test-${Date.now()}@test.com`;
    await request.post('/api/v1/auth/register', {
      data: {
        email: customerEmail,
        password: 'CustPass123!',
        firstName: 'Discount',
        lastName: 'Tester'
      }
    });

    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', customerEmail);
    await page.fill('[data-testid="password-input"]', 'CustPass123!');
    await page.click('[data-testid="login-button"]');

    // Add product to cart
    await page.goto(`/products/${product.id}`);
    await page.click('[data-testid="add-to-cart"]');

    // Go to cart and apply discount
    await page.goto('/cart');
    await page.fill('[data-testid="discount-code-input"]', discountCode);
    await page.click('[data-testid="apply-discount"]');

    // Verify discount applied
    await expect(page.getByText('Discount applied')).toBeVisible();
    await expect(page.getByTestId('discount-amount')).toContainText('-$');

    // Verify 10% discount on product price
    const originalPrice = product.price;
    const expectedDiscount = originalPrice * 0.1;
    await expect(page.getByTestId('discount-amount')).toContainText(
      expectedDiscount.toFixed(2)
    );
  });

  test('admin can view discount usage statistics', async ({ page, request }) => {
    const { email: adminEmail, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, adminEmail, password);

    // Create discount with some usage
    const discountCode = `STATS-${Date.now()}`;
    const discountResponse = await request.post('/api/v1/admin/discounts', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        code: discountCode,
        name: 'Stats Test Discount',
        type: 'fixed_amount',
        value: 50,
        isActive: true
      }
    });
    const discount = await discountResponse.json();

    // Simulate some usage via test helper
    await request.post('/api/v1/test/simulate-discount-usage', {
      headers: { Authorization: `Bearer ${token}` },
      data: { discountId: discount.id, usageCount: 5, totalRevenue: 2500 }
    });

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', adminEmail);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to discount detail
    await page.goto(`/admin/discounts/${discount.id}`);

    // Verify usage stats
    await expect(page.getByTestId('times-used')).toContainText('5');
    await expect(page.getByTestId('total-discount-given')).toContainText('$250.00');
    await expect(page.getByTestId('revenue-generated')).toContainText('$2,500.00');
  });
});
```

---

### Test ADM-E2E-008: Inventory Management

```typescript
// e2e/admin/inventory.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Inventory Management', () => {

  test('admin can view and update stock levels', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create product with specific stock
    const product = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Inventory Test ${Date.now()}`,
        sku: `INV-TEST-${Date.now()}`,
        description: 'Product for inventory testing',
        price: 499.99,
        stockQuantity: 50,
        lowStockThreshold: 20,
        categoryIds: []
      }
    });
    const productData = await product.json();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to inventory
    await page.goto('/admin/inventory');

    // Find product
    await page.fill('[data-testid="search-input"]', productData.sku);
    await page.waitForTimeout(500);

    // Verify stock displayed
    await expect(page.getByText(productData.name)).toBeVisible();
    await expect(page.locator(`[data-testid="stock-${productData.id}"]`)).toContainText('50');

    // Click to adjust stock
    await page.click(`[data-testid="adjust-stock-${productData.id}"]`);

    // Adjustment dialog
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.fill('[data-testid="adjustment-quantity"]', '25');
    await page.selectOption('[data-testid="adjustment-type"]', 'add');
    await page.selectOption('[data-testid="adjustment-reason"]', 'received');
    await page.fill('[data-testid="adjustment-notes"]', 'New shipment received');
    await page.click('[data-testid="confirm-adjustment"]');

    // Verify update
    await expect(page.getByText('Stock updated')).toBeVisible();
    await expect(page.locator(`[data-testid="stock-${productData.id}"]`)).toContainText('75');

    // Verify via API
    const updatedProduct = await request.get(`/api/v1/admin/products/${productData.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const updated = await updatedProduct.json();
    expect(updated.stockQuantity).toBe(75);
  });

  test('admin can view stock adjustment history', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create product and make adjustments
    const productResponse = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `History Test ${Date.now()}`,
        sku: `HIST-${Date.now()}`,
        description: 'Product for history testing',
        price: 299.99,
        stockQuantity: 100,
        categoryIds: []
      }
    });
    const product = await productResponse.json();

    // Make adjustments via API
    await request.post('/api/v1/admin/inventory/adjustments', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        productId: product.id,
        quantity: -10,
        type: 'subtract',
        reason: 'damaged',
        notes: 'Damaged in warehouse'
      }
    });

    await request.post('/api/v1/admin/inventory/adjustments', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        productId: product.id,
        quantity: 20,
        type: 'add',
        reason: 'received',
        notes: 'Supplier delivery'
      }
    });

    // Login and view history
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/inventory');
    await page.fill('[data-testid="search-input"]', product.sku);
    await page.waitForTimeout(500);
    await page.click(`[data-testid="view-history-${product.id}"]`);

    // Verify history
    await expect(page.getByTestId('adjustment-history')).toBeVisible();
    await expect(page.getByText('Damaged in warehouse')).toBeVisible();
    await expect(page.getByText('Supplier delivery')).toBeVisible();
    await expect(page.getByText('-10')).toBeVisible();
    await expect(page.getByText('+20')).toBeVisible();
  });

  test('low stock filter shows only low stock products', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create products with different stock levels
    const highStockProduct = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `High Stock ${Date.now()}`,
        sku: `HIGH-${Date.now()}`,
        description: 'High stock product',
        price: 199.99,
        stockQuantity: 100,
        lowStockThreshold: 10,
        categoryIds: []
      }
    });

    const lowStockProduct = await request.post('/api/v1/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `Low Stock ${Date.now()}`,
        sku: `LOW-${Date.now()}`,
        description: 'Low stock product',
        price: 299.99,
        stockQuantity: 5,
        lowStockThreshold: 10,
        categoryIds: []
      }
    });

    const highStock = await highStockProduct.json();
    const lowStock = await lowStockProduct.json();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/inventory');

    // Apply low stock filter
    await page.click('[data-testid="low-stock-filter"]');

    // Verify only low stock product visible
    await expect(page.getByText(lowStock.name)).toBeVisible();
    await expect(page.getByText(highStock.name)).not.toBeVisible();

    // Verify low stock badge
    await expect(page.locator(`[data-testid="low-stock-badge-${lowStock.id}"]`)).toBeVisible();
  });
});
```

---

### Test ADM-E2E-009: Reports

```typescript
// e2e/admin/reports.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Reports', () => {

  test('admin can view sales report with date filtering', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create some test orders for the report
    const product = await createTestProduct(request, token);

    // Create multiple orders
    for (let i = 0; i < 3; i++) {
      const customerEmail = `report-test-${i}-${Date.now()}@test.com`;
      await request.post('/api/v1/auth/register', {
        data: { email: customerEmail, password: 'Pass123!', firstName: 'Report', lastName: 'Test' }
      });
      const customerToken = await loginAndGetToken(request, customerEmail, 'Pass123!');

      await request.post('/api/v1/cart/items', {
        headers: { Authorization: `Bearer ${customerToken}` },
        data: { productId: product.id, quantity: i + 1 }
      });

      await request.post('/api/v1/orders', {
        headers: { Authorization: `Bearer ${customerToken}` },
        data: { paymentMethodId: 'test' }
      });
    }

    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to reports
    await page.goto('/admin/reports');

    // Verify report page
    await expect(page.getByRole('heading', { name: 'Sales Report' })).toBeVisible();

    // Set date range to today
    const today = new Date().toISOString().split('T')[0];
    await page.fill('[data-testid="date-from"]', today);
    await page.fill('[data-testid="date-to"]', today);
    await page.click('[data-testid="apply-date-filter"]');

    // Verify report data
    await expect(page.getByTestId('total-orders')).toContainText('3');
    await expect(page.getByTestId('total-revenue')).toBeVisible();
    await expect(page.getByTestId('sales-chart')).toBeVisible();

    // Verify can export
    const downloadPromise = page.waitForEvent('download');
    await page.click('[data-testid="export-csv"]');
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toContain('sales-report');
  });

  test('admin can view product performance report', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Create products with sales
    const products = [];
    for (let i = 0; i < 3; i++) {
      const productResponse = await request.post('/api/v1/admin/products', {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          name: `Performance Product ${i} ${Date.now()}`,
          sku: `PERF-${i}-${Date.now()}`,
          description: 'Test product',
          price: 100 * (i + 1),
          stockQuantity: 100,
          categoryIds: []
        }
      });
      products.push(await productResponse.json());
    }

    // Create orders for each product
    for (let i = 0; i < products.length; i++) {
      const customerEmail = `perf-${i}-${Date.now()}@test.com`;
      await request.post('/api/v1/auth/register', {
        data: { email: customerEmail, password: 'Pass123!', firstName: 'Perf', lastName: 'Test' }
      });
      const customerToken = await loginAndGetToken(request, customerEmail, 'Pass123!');

      await request.post('/api/v1/cart/items', {
        headers: { Authorization: `Bearer ${customerToken}` },
        data: { productId: products[i].id, quantity: 3 - i } // Different quantities
      });

      await request.post('/api/v1/orders', {
        headers: { Authorization: `Bearer ${customerToken}` },
        data: { paymentMethodId: 'test' }
      });
    }

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    // Go to product performance report
    await page.goto('/admin/reports/products');

    // Verify report
    await expect(page.getByRole('heading', { name: 'Product Performance' })).toBeVisible();
    await expect(page.getByTestId('product-performance-table')).toBeVisible();

    // Verify products listed with metrics
    for (const product of products) {
      await expect(page.getByText(product.name)).toBeVisible();
    }

    // Verify sorting by revenue
    await page.click('[data-testid="sort-by-revenue"]');

    // First product should be highest revenue
    const firstRow = page.locator('[data-testid="product-row"]:first-child');
    await expect(firstRow).toContainText(products[2].name); // Highest price product
  });
});
```

---

### Test ADM-E2E-010: Audit Trail

```typescript
// e2e/admin/audit.spec.ts
import { test, expect } from '@playwright/test';
import { createAdminUser, loginAndGetToken, createTestProduct } from './admin.setup';

test.describe('Admin Audit Trail', () => {

  test('admin actions are logged in audit trail', async ({ page, request }) => {
    const { email, password } = await createAdminUser(request);
    const token = await loginAndGetToken(request, email, password);

    // Perform various admin actions
    const product = await createTestProduct(request, token);

    // Update the product
    await request.put(`/api/v1/admin/products/${product.id}`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { ...product, price: 1199.99 }
    });

    // Login and view audit log
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', email);
    await page.fill('[data-testid="password-input"]', password);
    await page.click('[data-testid="login-button"]');

    await page.goto('/admin/settings/audit-logs');

    // Verify audit entries
    await expect(page.getByTestId('audit-log-table')).toBeVisible();
    await expect(page.getByText('Product Created')).toBeVisible();
    await expect(page.getByText('Product Updated')).toBeVisible();
    await expect(page.getByText(email)).toBeVisible();

    // Filter by action type
    await page.selectOption('[data-testid="action-filter"]', 'product_created');
    await expect(page.getByText('Product Created')).toBeVisible();
    await expect(page.getByText('Product Updated')).not.toBeVisible();

    // View detail of audit entry
    await page.click('[data-testid="audit-entry"]:first-child');

    // Verify audit detail
    await expect(page.getByTestId('audit-detail-dialog')).toBeVisible();
    await expect(page.getByText('Changes')).toBeVisible();
    await expect(page.getByText(product.name)).toBeVisible();
  });
});
```

---

## 8. Security Considerations

### Authentication & Authorization
- All admin endpoints require valid JWT token
- JWT must contain Admin role claim
- Token expiration: 30 minutes (configurable)
- Refresh token rotation on each use
- Session invalidation on logout

### Input Validation
- Server-side validation on all inputs
- File upload validation (type, size)
- SQL injection prevention via parameterized queries
- XSS prevention via output encoding

### Audit Logging
- Log all admin actions with:
  - Timestamp
  - Admin user ID
  - Action type
  - Affected entity ID
  - Before/after values for updates
  - IP address

### Rate Limiting
- 100 requests per minute per admin user
- Stricter limits on sensitive operations (password reset, bulk operations)

### CORS & CSP
- Strict CORS policy for admin API
- Content Security Policy headers
- HTTP-only cookies for refresh tokens

---

## 9. Performance Considerations

### Database
- Index frequently queried columns:
  - `orders.created_at`
  - `orders.status`
  - `products.sku`
  - `customers.email`
- Use materialized views for dashboard KPIs
- Implement pagination for all list endpoints (default: 25 items)

### Caching
- Cache dashboard KPIs (5 minute TTL)
- Cache category tree (10 minute TTL)
- Cache product counts and totals
- Invalidate caches on relevant data changes

### Frontend
- Lazy load admin modules
- Virtual scrolling for large lists
- Debounce search inputs (300ms)
- Image lazy loading and optimization
- Bundle size monitoring

---

## 10. Timeline Estimate

| Phase | Sprints | Duration | Focus |
|-------|---------|----------|-------|
| Phase 1 | 1-2 | 4 weeks | Foundation, Auth, Layout, Dashboard |
| Phase 2 | 3-4 | 4 weeks | Product Management |
| Phase 3 | 5-6 | 4 weeks | Order Management |
| Phase 4 | 7 | 2 weeks | Customer & Category Management |
| Phase 5 | 8 | 2 weeks | Inventory & Discounts |
| Phase 6 | 9 | 2 weeks | Reports & Settings |
| Phase 7 | 10 | 2 weeks | Polish & Testing |

**Total Estimated Duration**: 20 weeks (5 months)

---

## 11. Dependencies

### External Packages

**Backend (NuGet)**:
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `FluentValidation.AspNetCore`
- `Serilog.AspNetCore`
- `CsvHelper` (for import/export)
- `ClosedXML` (for Excel export)

**Frontend (npm)**:
- `ng2-charts` / `chart.js`
- `@angular/material` or `primeng`
- `@ngrx/signals` (state management)
- `ngx-file-drop` (file uploads)
- `quill` or `tinymce` (rich text editor)
- `@angular/cdk/drag-drop`

**Testing**:
- `@playwright/test`

---

## 12. Success Metrics

| Metric | Target |
|--------|--------|
| Page Load Time (Dashboard) | < 2 seconds |
| API Response Time (95th percentile) | < 500ms |
| E2E Test Coverage | > 80% of admin flows |
| Unit Test Coverage | > 80% |
| Accessibility Score | WCAG 2.1 AA compliant |
| Mobile Responsiveness | Fully responsive |

---

## 13. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex state management | High | Use Angular signals, modular services |
| Large data sets performance | Medium | Implement pagination, virtual scrolling, indexing |
| Security vulnerabilities | Critical | Regular security audits, OWASP guidelines |
| Browser compatibility | Low | Target modern browsers, test on multiple |
| Third-party integration failures | Medium | Circuit breakers, fallback UI |

---

## 14. Future Enhancements

1. **Multi-tenant Support** - Multiple stores per admin
2. **Advanced Analytics** - ML-powered insights
3. **Mobile Admin App** - Native iOS/Android
4. **Real-time Updates** - WebSocket for live data
5. **Workflow Automation** - Automated order processing rules
6. **Multi-language Admin** - Internationalization
7. **Themes** - Customizable admin themes
8. **API Rate Monitoring** - Admin API usage dashboard
