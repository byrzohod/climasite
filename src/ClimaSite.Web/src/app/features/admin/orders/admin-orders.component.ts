import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import {
  AdminOrdersService,
  AdminOrderListItem,
  ORDER_STATUSES
} from '../../../core/services/admin-orders.service';
import { DualPricePipe } from '../../../shared/pipes/dual-price.pipe';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule, DualPricePipe],
  template: `
    <div class="orders-container" data-testid="admin-orders-page">
      <div class="orders-header">
        <h1>{{ 'admin.orders.title' | translate }}</h1>
        <p class="subtitle">{{ 'admin.orders.subtitle' | translate }}</p>
      </div>

      <!-- Filters -->
      <div class="filters">
        <input
          type="search"
          class="search-input"
          [ngModel]="search()"
          (keyup.enter)="applySearch($any($event.target).value)"
          [placeholder]="'admin.orders.searchPlaceholder' | translate"
          [attr.aria-label]="'admin.orders.searchPlaceholder' | translate"
          data-testid="order-search" />

        <select
          class="status-filter"
          [ngModel]="statusFilter()"
          (ngModelChange)="applyStatusFilter($event)"
          [attr.aria-label]="'admin.orders.statusFilterLabel' | translate"
          data-testid="status-filter">
          <option value="">{{ 'admin.orders.allStatuses' | translate }}</option>
          @for (status of statuses; track status) {
            <option [value]="status">{{ 'admin.orders.status.' + status | translate }}</option>
          }
        </select>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="loading" data-testid="orders-loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Error State -->
      @if (!loading() && error()) {
        <div class="error-message" data-testid="orders-error">
          <span>{{ error()! | translate }}</span>
          <button (click)="loadOrders()" data-testid="orders-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && !error() && orders().length === 0) {
        <div class="empty-state" data-testid="orders-empty">
          <p>{{ 'admin.orders.empty' | translate }}</p>
        </div>
      }

      <!-- Orders Table -->
      @if (!loading() && !error() && orders().length > 0) {
        <div class="table-wrapper">
          <table class="orders-table">
            <thead>
              <tr>
                <th>{{ 'admin.orders.columns.orderNumber' | translate }}</th>
                <th>{{ 'admin.orders.columns.customer' | translate }}</th>
                <th>{{ 'admin.orders.columns.total' | translate }}</th>
                <th>{{ 'admin.orders.columns.status' | translate }}</th>
                <th>{{ 'admin.orders.columns.items' | translate }}</th>
                <th>{{ 'admin.orders.columns.created' | translate }}</th>
                <th>{{ 'admin.orders.columns.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (order of orders(); track order.id) {
                <tr [attr.data-testid]="'order-row'" [attr.data-order-id]="order.id">
                  <td class="order-number">{{ order.orderNumber }}</td>
                  <td>
                    <div class="customer">
                      <span class="customer-name">{{ order.customerName }}</span>
                      <span class="customer-email">{{ order.customerEmail }}</span>
                    </div>
                  </td>
                  <td>{{ order.totalAmount | dualPrice }}</td>
                  <td>
                    <span class="status-badge" [class]="order.status.toLowerCase()">
                      {{ 'admin.orders.status.' + order.status | translate }}
                    </span>
                  </td>
                  <td class="item-count">{{ order.itemCount }}</td>
                  <td class="created">{{ order.createdAt | date:'medium' }}</td>
                  <td>
                    <a
                      class="view-link"
                      [routerLink]="['/admin/orders', order.id]"
                      [attr.data-testid]="'view-order'"
                      [attr.data-order-id]="order.id">
                      {{ 'admin.orders.view' | translate }}
                    </a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination">
          <button
            class="page-btn"
            [disabled]="page() <= 1"
            (click)="prevPage()"
            data-testid="orders-prev">
            {{ 'common.previous' | translate }}
          </button>
          <span class="page-indicator" data-testid="orders-page-indicator">
            {{ 'admin.orders.pageOf' | translate:{ page: page(), total: totalPages() } }}
          </span>
          <button
            class="page-btn"
            [disabled]="page() >= totalPages()"
            (click)="nextPage()"
            data-testid="orders-next">
            {{ 'common.next' | translate }}
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .orders-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .orders-header {
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .search-input,
    .status-filter {
      padding: 0.625rem 0.875rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-input);
      color: var(--color-text-primary);
      font-size: 0.9375rem;
    }

    .search-input {
      flex: 1;
      min-width: 220px;
    }

    .status-filter {
      min-width: 180px;
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

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem;
      background: var(--color-bg-card);
      border-radius: 8px;
      color: var(--color-text-secondary);
    }

    .table-wrapper {
      overflow-x: auto;
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
    }

    .orders-table {
      width: 100%;
      border-collapse: collapse;

      th,
      td {
        padding: 0.875rem 1rem;
        text-align: left;
        border-bottom: 1px solid var(--color-border-primary);
      }

      th {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-text-secondary);
        background: var(--color-bg-secondary);
      }

      td {
        color: var(--color-text-primary);
        font-size: 0.9375rem;
      }

      tbody tr:hover {
        background: var(--color-bg-hover);
      }
    }

    .order-number {
      font-weight: 600;
    }

    .customer {
      display: flex;
      flex-direction: column;

      .customer-name {
        color: var(--color-text-primary);
      }

      .customer-email {
        font-size: 0.8125rem;
        color: var(--color-text-tertiary);
      }
    }

    .item-count,
    .created {
      color: var(--color-text-secondary);
    }

    .status-badge {
      padding: 0.25rem 0.625rem;
      border-radius: 4px;
      font-size: 0.75rem;
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

    .view-link {
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 500;

      &:hover {
        text-decoration: underline;
      }
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .page-btn {
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-card);
      color: var(--color-text-primary);
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-bg-hover);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .page-indicator {
      color: var(--color-text-secondary);
      font-size: 0.9375rem;
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

    @media (max-width: 768px) {
      .orders-container {
        padding: 1rem;
      }

      .filters {
        flex-direction: column;
      }

      .search-input,
      .status-filter {
        width: 100%;
      }
    }
  `]
})
export class AdminOrdersComponent implements OnInit {
  private readonly ordersService = inject(AdminOrdersService);

  protected readonly statuses = ORDER_STATUSES;

  orders = signal<AdminOrderListItem[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(20);
  search = signal('');
  statusFilter = signal('');
  totalPages = signal(1);

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    this.error.set(null);

    this.ordersService.getOrders({
      pageNumber: this.page(),
      pageSize: this.pageSize(),
      search: this.search() || undefined,
      status: this.statusFilter() || undefined
    }).subscribe({
      next: (result) => {
        this.orders.set(result.items);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('admin.orders.errors.loadFailed');
        this.loading.set(false);
      }
    });
  }

  applySearch(value: string): void {
    this.search.set(value.trim());
    this.page.set(1);
    this.loadOrders();
  }

  applyStatusFilter(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadOrders();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadOrders();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
      this.loadOrders();
    }
  }
}
