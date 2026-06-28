import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import {
  AdminDashboardService,
  DashboardKpiDto,
  OrderStatusChartDto,
  RecentOrderDto,
  LowStockProductDto,
  TopSellingProductDto
} from '../../../core/services/admin-dashboard.service';
import { DualPricePipe } from '../../../shared/pipes/dual-price.pipe';

interface OrderStatusBar {
  key: string;
  count: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, DualPricePipe],
  template: `
    <div class="admin-container" data-testid="admin-dashboard">
      <h1>{{ 'admin.title' | translate }}</h1>

      <!-- KPI section -->
      <section class="kpi-section">
        <h2 class="section-title">{{ 'admin.dashboard.title' | translate }}</h2>

        <!-- Loading State -->
        @if (loading()) {
          <div class="loading" data-testid="dashboard-loading">
            <div class="spinner"></div>
            <span>{{ 'common.loading' | translate }}</span>
          </div>
        }

        <!-- Error State -->
        @if (!loading() && error()) {
          <div class="error-message" data-testid="dashboard-error">
            <span>{{ error()! | translate }}</span>
            <button (click)="loadDashboard()" data-testid="dashboard-retry">
              {{ 'common.retry' | translate }}
            </button>
          </div>
        }

        <!-- Loaded content -->
        @if (!loading() && !error() && kpis(); as data) {
          <!-- KPI metric cards -->
          <div class="kpi-grid">
            <div class="kpi-card" data-testid="kpi-total-orders">
              <span class="kpi-label">{{ 'admin.dashboard.kpis.totalOrders' | translate }}</span>
              <span class="kpi-value">{{ data.totalOrders.thisMonth | number }}</span>
              <span class="kpi-trend" [class]="trendClass(data.totalOrders.trendPercentage)">
                {{ trendArrow(data.totalOrders.trendPercentage) }}
                {{ data.totalOrders.trendPercentage | number:'1.0-1' }}%
              </span>
              <span class="kpi-period">{{ 'admin.dashboard.period.month' | translate }}</span>
            </div>

            <div class="kpi-card" data-testid="kpi-revenue">
              <span class="kpi-label">{{ 'admin.dashboard.kpis.revenue' | translate }}</span>
              <span class="kpi-value">{{ data.revenue.thisMonth | dualPrice }}</span>
              <span class="kpi-trend" [class]="trendClass(data.revenue.trendPercentage)">
                {{ trendArrow(data.revenue.trendPercentage) }}
                {{ data.revenue.trendPercentage | number:'1.0-1' }}%
              </span>
              <span class="kpi-period">{{ 'admin.dashboard.period.month' | translate }}</span>
            </div>

            <div class="kpi-card" data-testid="kpi-new-customers">
              <span class="kpi-label">{{ 'admin.dashboard.kpis.newCustomers' | translate }}</span>
              <span class="kpi-value">{{ data.newCustomers.thisMonth | number }}</span>
              <span class="kpi-trend" [class]="trendClass(data.newCustomers.trendPercentage)">
                {{ trendArrow(data.newCustomers.trendPercentage) }}
                {{ data.newCustomers.trendPercentage | number:'1.0-1' }}%
              </span>
              <span class="kpi-period">{{ 'admin.dashboard.period.month' | translate }}</span>
            </div>

            <div class="kpi-card" data-testid="kpi-avg-order-value">
              <span class="kpi-label">{{ 'admin.dashboard.kpis.avgOrderValue' | translate }}</span>
              <span class="kpi-value">{{ data.averageOrderValue.thisMonth | dualPrice }}</span>
              <span class="kpi-trend" [class]="trendClass(data.averageOrderValue.trendPercentage)">
                {{ trendArrow(data.averageOrderValue.trendPercentage) }}
                {{ data.averageOrderValue.trendPercentage | number:'1.0-1' }}%
              </span>
              <span class="kpi-period">{{ 'admin.dashboard.period.month' | translate }}</span>
            </div>
          </div>

          <!-- Status badges -->
          <div class="badge-row">
            <div class="status-tile pending" data-testid="kpi-pending-orders">
              <span class="badge-count">{{ data.pendingOrders | number }}</span>
              <span class="badge-label">{{ 'admin.dashboard.kpis.pendingOrders' | translate }}</span>
            </div>
            <div class="status-tile low-stock" data-testid="kpi-low-stock">
              <span class="badge-count">{{ data.lowStockCount | number }}</span>
              <span class="badge-label">{{ 'admin.dashboard.kpis.lowStock' | translate }}</span>
            </div>
          </div>

          <!-- Order status breakdown (CSS-only bars) -->
          <div class="panel" data-testid="dashboard-order-status">
            <h3 class="panel-title">{{ 'admin.dashboard.orderStatus.title' | translate }}</h3>
            <div class="status-bars">
              @for (bar of orderStatusBars(); track bar.key) {
                <div class="status-bar-row">
                  <span class="status-bar-label">
                    {{ 'admin.dashboard.orderStatus.' + bar.key | translate }}
                  </span>
                  <div class="status-bar-track">
                    <div
                      class="status-bar-fill"
                      [class]="bar.key"
                      [style.width.%]="barWidth(bar.count)">
                    </div>
                  </div>
                  <span class="status-bar-count">{{ bar.count | number }}</span>
                </div>
              }
            </div>
          </div>

          <!-- Lists row -->
          <div class="lists-grid">
            <!-- Recent orders -->
            <div class="panel" data-testid="dashboard-recent-orders">
              <h3 class="panel-title">{{ 'admin.dashboard.recentOrders' | translate }}</h3>
              @if (recentOrders().length === 0) {
                <p class="empty-hint">{{ 'admin.dashboard.empty' | translate }}</p>
              } @else {
                <table class="mini-table">
                  <thead>
                    <tr>
                      <th>{{ 'admin.orders.columns.orderNumber' | translate }}</th>
                      <th>{{ 'admin.orders.columns.customer' | translate }}</th>
                      <th>{{ 'admin.orders.columns.total' | translate }}</th>
                      <th>{{ 'admin.orders.columns.status' | translate }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (order of recentOrders(); track order.id) {
                      <tr [attr.data-testid]="'recent-order-row'" [attr.data-order-id]="order.id">
                        <td>
                          <a
                            class="link"
                            [routerLink]="['/admin/orders', order.id]"
                            [attr.data-testid]="'recent-order-link'"
                            [attr.data-order-id]="order.id">
                            {{ order.orderNumber }}
                          </a>
                        </td>
                        <td>{{ order.customerName }}</td>
                        <td>{{ order.totalAmount | dualPrice }}</td>
                        <td>
                          <span class="status-badge" [class]="order.status.toLowerCase()">
                            {{ 'admin.orders.status.' + order.status | translate }}
                          </span>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              }
            </div>

            <!-- Low stock products -->
            <div class="panel" data-testid="dashboard-low-stock">
              <h3 class="panel-title">{{ 'admin.dashboard.lowStockProducts' | translate }}</h3>
              @if (lowStockProducts().length === 0) {
                <p class="empty-hint">{{ 'admin.dashboard.empty' | translate }}</p>
              } @else {
                <table class="mini-table">
                  <thead>
                    <tr>
                      <th>{{ 'admin.dashboard.columns.product' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.sku' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.stock' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.threshold' | translate }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (product of lowStockProducts(); track product.id) {
                      <tr [attr.data-testid]="'low-stock-row'" [attr.data-product-id]="product.id">
                        <td>{{ product.name }}</td>
                        <td class="muted">{{ product.sku }}</td>
                        <td><span class="stock-warn">{{ product.currentStock | number }}</span></td>
                        <td class="muted">{{ product.threshold | number }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              }
            </div>

            <!-- Top selling products -->
            <div class="panel" data-testid="dashboard-top-products">
              <h3 class="panel-title">{{ 'admin.dashboard.topProducts' | translate }}</h3>
              @if (topProducts().length === 0) {
                <p class="empty-hint">{{ 'admin.dashboard.empty' | translate }}</p>
              } @else {
                <table class="mini-table">
                  <thead>
                    <tr>
                      <th>{{ 'admin.dashboard.columns.product' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.sku' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.sold' | translate }}</th>
                      <th>{{ 'admin.dashboard.columns.revenue' | translate }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (product of topProducts(); track product.id) {
                      <tr [attr.data-testid]="'top-product-row'" [attr.data-product-id]="product.id">
                        <td>{{ product.name }}</td>
                        <td class="muted">{{ product.sku }}</td>
                        <td>{{ product.quantitySold | number }}</td>
                        <td>{{ product.revenue | dualPrice }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              }
            </div>
          </div>
        }
      </section>

      <!-- Navigation tiles (kept from the original dashboard) -->
      <div class="admin-links">
        <a routerLink="products" class="admin-link">
          <span class="icon">📦</span>
          <span>{{ 'admin.products.title' | translate }}</span>
        </a>
        <a routerLink="orders" class="admin-link">
          <span class="icon">📋</span>
          <span>{{ 'admin.orders.title' | translate }}</span>
        </a>
        <a routerLink="users" class="admin-link">
          <span class="icon">👥</span>
          <span>{{ 'admin.users.title' | translate }}</span>
        </a>
        <a routerLink="moderation" class="admin-link">
          <span class="icon">🛡️</span>
          <span>{{ 'admin.moderation.title' | translate }}</span>
        </a>
        <a routerLink="installation-requests" class="admin-link" data-testid="admin-installation-tile">
          <span class="icon">🔧</span>
          <span>{{ 'admin.installation.title' | translate }}</span>
        </a>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .kpi-section {
      margin-bottom: 2.5rem;
    }

    .section-title {
      font-size: 1.25rem;
      color: var(--color-text-primary);
      margin: 0 0 1.25rem;
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--color-border-primary);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .error-message {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-error-light);
      color: var(--color-error-dark);
      border-radius: 8px;
      margin-bottom: 1rem;

      button {
        padding: 0.5rem 1rem;
        background: var(--color-error);
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;

        &:hover {
          background: var(--color-error-dark);
        }
      }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1.25rem;
      margin-bottom: 1.5rem;
    }

    .kpi-card {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
      padding: 1.5rem;
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 12px;
    }

    .kpi-label {
      font-size: 0.8125rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--color-text-secondary);
    }

    .kpi-value {
      font-size: 1.875rem;
      font-weight: 700;
      color: var(--color-text-primary);
    }

    .kpi-trend {
      font-size: 0.875rem;
      font-weight: 600;

      &.trend-up { color: var(--color-success-dark); }
      &.trend-down { color: var(--color-error-dark); }
      &.trend-flat { color: var(--color-text-tertiary); }
    }

    .kpi-period {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
    }

    .badge-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1.25rem;
      margin-bottom: 1.5rem;
    }

    .status-tile {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      padding: 1.25rem 1.5rem;
      border-radius: 12px;
      border: 1px solid var(--color-border-primary);

      &.pending {
        background: var(--color-warning-light);
        .badge-count, .badge-label { color: var(--color-warning-dark); }
      }

      &.low-stock {
        background: var(--color-error-light);
        .badge-count, .badge-label { color: var(--color-error-dark); }
      }
    }

    .badge-count {
      font-size: 1.75rem;
      font-weight: 700;
    }

    .badge-label {
      font-size: 0.8125rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .panel {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .panel-title {
      font-size: 1rem;
      color: var(--color-text-primary);
      margin: 0 0 1rem;
    }

    .status-bars {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .status-bar-row {
      display: grid;
      grid-template-columns: 110px 1fr 48px;
      align-items: center;
      gap: 0.75rem;
    }

    .status-bar-label {
      font-size: 0.8125rem;
      color: var(--color-text-secondary);
      text-transform: capitalize;
    }

    .status-bar-track {
      height: 10px;
      background: var(--color-bg-secondary);
      border-radius: 6px;
      overflow: hidden;
    }

    .status-bar-fill {
      height: 100%;
      border-radius: 6px;
      background: var(--color-primary);
      transition: width 0.4s ease;

      &.pending { background: var(--color-warning); }
      &.processing { background: var(--color-primary); }
      &.shipped { background: var(--color-primary-dark); }
      &.delivered { background: var(--color-success); }
      &.cancelled { background: var(--color-error); }
    }

    .status-bar-count {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-primary);
      text-align: right;
    }

    .lists-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1.5rem;
    }

    .mini-table {
      width: 100%;
      border-collapse: collapse;

      th, td {
        padding: 0.625rem 0.75rem;
        text-align: left;
        border-bottom: 1px solid var(--color-border-primary);
      }

      th {
        font-size: 0.6875rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-text-secondary);
      }

      td {
        font-size: 0.875rem;
        color: var(--color-text-primary);
      }

      tbody tr:last-child td {
        border-bottom: none;
      }
    }

    .muted {
      color: var(--color-text-tertiary);
    }

    .stock-warn {
      font-weight: 700;
      color: var(--color-error-dark);
    }

    .link {
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 600;

      &:hover { text-decoration: underline; }
    }

    .empty-hint {
      color: var(--color-text-tertiary);
      font-size: 0.875rem;
      margin: 0;
    }

    .status-badge {
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      background: var(--color-bg-secondary);
      color: var(--color-text-secondary);

      &.pending { background: var(--color-warning-light); color: var(--color-warning-dark); }
      &.paid { background: var(--color-success-light); color: var(--color-success-dark); }
      &.processing { background: var(--color-primary-light); color: var(--color-primary-dark); }
      &.shipped { background: var(--color-primary-light); color: var(--color-primary-dark); }
      &.delivered { background: var(--color-success-light); color: var(--color-success-dark); }
      &.cancelled { background: var(--color-error-light); color: var(--color-error-dark); }
      &.refunded { background: var(--color-error-light); color: var(--color-error-dark); }
      &.returned { background: var(--color-warning-light); color: var(--color-warning-dark); }
      &.paymentfailed { background: var(--color-error-light); color: var(--color-error-dark); }
    }

    .admin-links {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .admin-link {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      text-decoration: none;
      color: var(--color-text-primary);
      transition: border-color 0.2s, box-shadow 0.2s;

      &:hover {
        border-color: var(--color-primary);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }

      .icon {
        font-size: 2.5rem;
      }
    }

    @media (min-width: 900px) {
      .lists-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 768px) {
      .admin-container {
        padding: 1rem;
      }

      .status-bar-row {
        grid-template-columns: 90px 1fr 40px;
      }
    }
  `]
})
export class AdminDashboardComponent implements OnInit {
  private readonly dashboardService = inject(AdminDashboardService);

  /** Fixed order of status keys for the breakdown bars. */
  private static readonly STATUS_KEYS: readonly (keyof OrderStatusChartDto)[] = [
    'pending',
    'processing',
    'shipped',
    'delivered',
    'cancelled'
  ];

  kpis = signal<DashboardKpiDto | null>(null);
  orderStatus = signal<OrderStatusChartDto | null>(null);
  recentOrders = signal<RecentOrderDto[]>([]);
  lowStockProducts = signal<LowStockProductDto[]>([]);
  topProducts = signal<TopSellingProductDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  /** Status counts in display order, derived from the loaded chart. */
  orderStatusBars = computed<OrderStatusBar[]>(() => {
    const chart = this.orderStatus();
    if (!chart) {
      return [];
    }
    return AdminDashboardComponent.STATUS_KEYS.map(key => ({
      key,
      count: chart[key]
    }));
  });

  /** Largest single status count, used to scale the bars. */
  private readonly maxStatusCount = computed(() => {
    const counts = this.orderStatusBars().map(b => b.count);
    return counts.length ? Math.max(...counts) : 0;
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      kpis: this.dashboardService.getKpis(),
      orderStatus: this.dashboardService.getOrderStatusChart(),
      recentOrders: this.dashboardService.getRecentOrders(),
      lowStock: this.dashboardService.getLowStockProducts(),
      topProducts: this.dashboardService.getTopSellingProducts()
    }).subscribe({
      next: (result) => {
        this.kpis.set(result.kpis);
        this.orderStatus.set(result.orderStatus);
        this.recentOrders.set(result.recentOrders);
        this.lowStockProducts.set(result.lowStock);
        this.topProducts.set(result.topProducts);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('admin.dashboard.error');
        this.loading.set(false);
      }
    });
  }

  /** Bar width as a percentage of the largest status count (min 2% so a non-zero value is visible). */
  barWidth(count: number): number {
    const max = this.maxStatusCount();
    if (max <= 0) {
      return 0;
    }
    const pct = (count / max) * 100;
    return count > 0 ? Math.max(pct, 2) : 0;
  }

  trendClass(value: number): string {
    if (value > 0) {
      return 'trend-up';
    }
    if (value < 0) {
      return 'trend-down';
    }
    return 'trend-flat';
  }

  trendArrow(value: number): string {
    if (value > 0) {
      return '▲';
    }
    if (value < 0) {
      return '▼';
    }
    return '–';
  }
}
