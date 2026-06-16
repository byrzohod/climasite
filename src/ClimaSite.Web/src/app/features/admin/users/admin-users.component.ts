import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import {
  AdminCustomersService,
  AdminCustomerListItem,
  AdminCustomerDetail
} from '../../../core/services/admin-customers.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="customers-container" data-testid="admin-customers-page">
      <div class="customers-header">
        <h1>{{ 'admin.users.title' | translate }}</h1>
        <p class="subtitle">{{ 'admin.users.subtitle' | translate }}</p>
      </div>

      <!-- Filters -->
      <div class="filters">
        <input
          type="search"
          class="search-input"
          [ngModel]="search()"
          (keyup.enter)="applySearch($any($event.target).value)"
          [placeholder]="'admin.users.search' | translate"
          [attr.aria-label]="'admin.users.search' | translate"
          data-testid="customer-search" />

        <select
          class="status-filter"
          [ngModel]="statusFilter()"
          (ngModelChange)="applyStatusFilter($event)"
          [attr.aria-label]="'admin.users.filter.label' | translate"
          data-testid="customer-status-filter">
          <option value="">{{ 'admin.users.filter.all' | translate }}</option>
          <option value="active">{{ 'admin.users.filter.active' | translate }}</option>
          <option value="inactive">{{ 'admin.users.filter.inactive' | translate }}</option>
        </select>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="loading" data-testid="customers-loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Error State -->
      @if (!loading() && error()) {
        <div class="error-message" data-testid="customers-error">
          <span>{{ error()! | translate }}</span>
          <button (click)="loadCustomers()" data-testid="customers-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && !error() && customers().length === 0) {
        <div class="empty-state" data-testid="customers-empty">
          <p>{{ 'admin.users.empty' | translate }}</p>
        </div>
      }

      <!-- Customers Table -->
      @if (!loading() && !error() && customers().length > 0) {
        <div class="table-wrapper">
          <table class="customers-table">
            <thead>
              <tr>
                <th>{{ 'admin.users.columns.email' | translate }}</th>
                <th>{{ 'admin.users.columns.name' | translate }}</th>
                <th>{{ 'admin.users.columns.phone' | translate }}</th>
                <th>{{ 'admin.users.columns.status' | translate }}</th>
                <th>{{ 'admin.users.columns.orders' | translate }}</th>
                <th>{{ 'admin.users.columns.spent' | translate }}</th>
                <th>{{ 'admin.users.columns.created' | translate }}</th>
                <th>{{ 'admin.users.columns.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (customer of customers(); track customer.id) {
                <tr [attr.data-testid]="'customer-row'" [attr.data-customer-id]="customer.id">
                  <td class="email">{{ customer.email }}</td>
                  <td>{{ customer.fullName }}</td>
                  <td class="phone">{{ customer.phone || '—' }}</td>
                  <td>
                    <span
                      class="status-badge"
                      [class.active]="customer.isActive"
                      [class.inactive]="!customer.isActive">
                      {{ (customer.isActive ? 'admin.users.status.active' : 'admin.users.status.inactive') | translate }}
                    </span>
                  </td>
                  <td class="orders">{{ customer.orderCount }}</td>
                  <td>{{ customer.totalSpent | currency:'EUR' }}</td>
                  <td class="created">{{ customer.createdAt | date:'mediumDate' }}</td>
                  <td>
                    <button
                      class="view-link"
                      type="button"
                      (click)="openDetail(customer.id)"
                      [attr.data-testid]="'view-customer'"
                      [attr.data-customer-id]="customer.id">
                      {{ 'admin.users.view' | translate }}
                    </button>
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
            data-testid="customers-prev">
            {{ 'common.previous' | translate }}
          </button>
          <span class="page-indicator" data-testid="customers-page-indicator">
            {{ 'admin.users.pageOf' | translate:{ page: page(), total: totalPages() } }}
          </span>
          <button
            class="page-btn"
            [disabled]="page() >= totalPages()"
            (click)="nextPage()"
            data-testid="customers-next">
            {{ 'common.next' | translate }}
          </button>
        </div>
      }

      <!-- Detail Panel (modal overlay) -->
      @if (detailOpen()) {
        <div class="detail-overlay" (click)="closeDetail()">
          <div
            class="detail-panel"
            role="dialog"
            aria-modal="true"
            (click)="$event.stopPropagation()"
            data-testid="customer-detail">
            <button
              class="close-btn"
              type="button"
              (click)="closeDetail()"
              [attr.aria-label]="'common.close' | translate"
              data-testid="close-customer-detail">
              &times;
            </button>

            <h2 class="detail-title">{{ 'admin.users.detail.title' | translate }}</h2>

            <!-- Detail loading -->
            @if (detailLoading()) {
              <div class="loading" data-testid="customer-detail-loading">
                <div class="spinner"></div>
                <span>{{ 'common.loading' | translate }}</span>
              </div>
            }

            <!-- Detail error -->
            @if (!detailLoading() && detailError()) {
              <div class="error-message" data-testid="customer-detail-error">
                <span>{{ detailError()! | translate }}</span>
                <button (click)="reloadDetail()" data-testid="customer-detail-retry">
                  {{ 'common.retry' | translate }}
                </button>
              </div>
            }

            @if (!detailLoading() && !detailError() && detail(); as cust) {
              <!-- Action feedback -->
              @if (actionError()) {
                <div class="inline-error" data-testid="customer-action-error">
                  {{ actionError()! | translate }}
                </div>
              }
              @if (actionSuccess()) {
                <div class="inline-success" data-testid="customer-action-success">
                  {{ actionSuccess()! | translate }}
                </div>
              }

              <!-- Profile -->
              <section class="card">
                <div class="card-head">
                  <h3>{{ 'admin.users.detail.profile' | translate }}</h3>
                  <span
                    class="status-badge"
                    [class.active]="cust.isActive"
                    [class.inactive]="!cust.isActive"
                    data-testid="customer-active-badge">
                    {{ (cust.isActive ? 'admin.users.status.active' : 'admin.users.status.inactive') | translate }}
                  </span>
                </div>
                <p><strong>{{ cust.firstName }} {{ cust.lastName }}</strong></p>
                <p>{{ cust.email }}</p>
                @if (cust.phoneNumber) {
                  <p>{{ cust.phoneNumber }}</p>
                }
                <dl class="meta">
                  <div>
                    <dt>{{ 'admin.users.detail.emailConfirmed' | translate }}</dt>
                    <dd>{{ (cust.emailConfirmed ? 'common.yes' : 'common.no') | translate }}</dd>
                  </div>
                  <div>
                    <dt>{{ 'admin.users.detail.language' | translate }}</dt>
                    <dd>{{ cust.preferredLanguage | uppercase }}</dd>
                  </div>
                  <div>
                    <dt>{{ 'admin.users.detail.currency' | translate }}</dt>
                    <dd>{{ cust.preferredCurrency }}</dd>
                  </div>
                  <div>
                    <dt>{{ 'admin.users.columns.created' | translate }}</dt>
                    <dd>{{ cust.createdAt | date:'medium' }}</dd>
                  </div>
                  @if (cust.lastLoginAt) {
                    <div>
                      <dt>{{ 'admin.users.detail.lastLogin' | translate }}</dt>
                      <dd>{{ cust.lastLoginAt | date:'medium' }}</dd>
                    </div>
                  }
                </dl>

                <button
                  class="action-btn"
                  type="button"
                  [class.danger]="cust.isActive"
                  [class.primary]="!cust.isActive"
                  [disabled]="saving()"
                  (click)="toggleStatus(cust)"
                  data-testid="toggle-customer-status">
                  {{ (cust.isActive ? 'admin.users.deactivate' : 'admin.users.activate') | translate }}
                </button>
              </section>

              <!-- Stats -->
              <section class="card">
                <h3>{{ 'admin.users.detail.stats' | translate }}</h3>
                <div class="stats-grid">
                  <div class="stat">
                    <span class="stat-value">{{ cust.stats.totalOrders }}</span>
                    <span class="stat-label">{{ 'admin.users.stats.totalOrders' | translate }}</span>
                  </div>
                  <div class="stat">
                    <span class="stat-value">{{ cust.stats.totalSpent | currency:'EUR' }}</span>
                    <span class="stat-label">{{ 'admin.users.stats.totalSpent' | translate }}</span>
                  </div>
                  <div class="stat">
                    <span class="stat-value">{{ cust.stats.averageOrderValue | currency:'EUR' }}</span>
                    <span class="stat-label">{{ 'admin.users.stats.averageOrder' | translate }}</span>
                  </div>
                  <div class="stat">
                    <span class="stat-value">{{ cust.stats.reviewsWritten }}</span>
                    <span class="stat-label">{{ 'admin.users.stats.reviews' | translate }}</span>
                  </div>
                  <div class="stat">
                    <span class="stat-value">{{ cust.stats.wishlistItems }}</span>
                    <span class="stat-label">{{ 'admin.users.stats.wishlist' | translate }}</span>
                  </div>
                </div>
              </section>

              <!-- Addresses -->
              <section class="card">
                <h3>{{ 'admin.users.detail.addresses' | translate }}</h3>
                @if (cust.addresses.length > 0) {
                  <ul class="address-list">
                    @for (address of cust.addresses; track address.id) {
                      <li class="address" data-testid="customer-address">
                        @if (address.isDefault) {
                          <span class="default-tag">{{ 'admin.users.detail.defaultAddress' | translate }}</span>
                        }
                        <p>{{ address.addressLine1 }}</p>
                        @if (address.addressLine2) {
                          <p>{{ address.addressLine2 }}</p>
                        }
                        <p>{{ address.city }}, {{ address.state }} {{ address.postalCode }}</p>
                        <p>{{ address.country }}</p>
                      </li>
                    }
                  </ul>
                } @else {
                  <p class="muted">{{ 'admin.users.detail.noAddresses' | translate }}</p>
                }
              </section>

              <!-- Recent orders -->
              <section class="card">
                <h3>{{ 'admin.users.detail.recentOrders' | translate }}</h3>
                @if (cust.recentOrders.length > 0) {
                  <div class="table-wrapper">
                    <table class="orders-table">
                      <thead>
                        <tr>
                          <th>{{ 'admin.users.orderColumns.number' | translate }}</th>
                          <th>{{ 'admin.users.orderColumns.total' | translate }}</th>
                          <th>{{ 'admin.users.orderColumns.status' | translate }}</th>
                          <th>{{ 'admin.users.orderColumns.created' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (order of cust.recentOrders; track order.id) {
                          <tr data-testid="customer-order-row">
                            <td>{{ order.orderNumber }}</td>
                            <td>{{ order.total | currency:'EUR' }}</td>
                            <td>{{ order.status }}</td>
                            <td>{{ order.createdAt | date:'mediumDate' }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                } @else {
                  <p class="muted">{{ 'admin.users.detail.noOrders' | translate }}</p>
                }
              </section>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .customers-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .customers-header {
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

    .customers-table,
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

    .email {
      font-weight: 600;
    }

    .phone,
    .orders,
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

      &.active { background: var(--color-success-light); color: var(--color-success-dark); }
      &.inactive { background: var(--color-error-light); color: var(--color-error-dark); }
    }

    .view-link {
      color: var(--color-primary);
      background: none;
      border: none;
      padding: 0;
      text-decoration: none;
      font-weight: 500;
      font-size: 0.9375rem;
      cursor: pointer;

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

    /* Detail panel */
    .detail-overlay {
      position: fixed;
      inset: 0;
      background: var(--color-overlay, rgba(0, 0, 0, 0.5));
      display: flex;
      align-items: flex-start;
      justify-content: center;
      padding: 2rem 1rem;
      overflow-y: auto;
      z-index: 1000;
    }

    .detail-panel {
      position: relative;
      width: 100%;
      max-width: 760px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: 12px;
      padding: 1.75rem;
      box-shadow: var(--shadow-lg, 0 10px 30px rgba(0, 0, 0, 0.2));
    }

    .close-btn {
      position: absolute;
      top: 1rem;
      right: 1rem;
      width: 2rem;
      height: 2rem;
      border: none;
      border-radius: 6px;
      background: var(--color-bg-secondary);
      color: var(--color-text-secondary);
      font-size: 1.5rem;
      line-height: 1;
      cursor: pointer;

      &:hover {
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
      }
    }

    .detail-title {
      font-size: 1.5rem;
      color: var(--color-text-primary);
      margin: 0 0 1.25rem;
    }

    .card {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
      padding: 1.25rem;
      margin-bottom: 1rem;

      &:last-child {
        margin-bottom: 0;
      }

      h3 {
        font-size: 1rem;
        color: var(--color-text-primary);
        margin: 0 0 0.75rem;
      }

      p {
        margin: 0 0 0.25rem;
        color: var(--color-text-secondary);
        font-size: 0.9375rem;
      }

      .muted {
        color: var(--color-text-tertiary);
      }
    }

    .card-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 0.5rem;

      h3 {
        margin: 0;
      }
    }

    .meta {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: 0.75rem;
      margin: 1rem 0;

      dt {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--color-text-tertiary);
        margin-bottom: 0.125rem;
      }

      dd {
        margin: 0;
        color: var(--color-text-primary);
        font-size: 0.9375rem;
      }
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: 0.75rem;
    }

    .stat {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      padding: 0.875rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;

      .stat-value {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }

      .stat-label {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
      }
    }

    .address-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 0.75rem;
    }

    .address {
      position: relative;
      padding: 0.875rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;

      p {
        color: var(--color-text-primary);
      }

      .default-tag {
        display: inline-block;
        margin-bottom: 0.375rem;
        padding: 0.125rem 0.5rem;
        border-radius: 4px;
        font-size: 0.6875rem;
        font-weight: 600;
        text-transform: uppercase;
        background: var(--color-primary-light);
        color: var(--color-primary-dark);
      }
    }

    .action-btn {
      margin-top: 0.5rem;
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      &.primary {
        background: var(--color-primary);
        color: white;

        &:hover:not(:disabled) {
          background: var(--color-primary-dark);
        }
      }

      &.danger {
        background: var(--color-error);
        color: white;

        &:hover:not(:disabled) {
          background: var(--color-error-dark);
        }
      }
    }

    .inline-error,
    .inline-success {
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin-bottom: 1rem;
      font-size: 0.9375rem;
    }

    .inline-error {
      background: var(--color-error-light);
      color: var(--color-error-dark);
    }

    .inline-success {
      background: var(--color-success-light);
      color: var(--color-success-dark);
    }

    @media (max-width: 768px) {
      .customers-container {
        padding: 1rem;
      }

      .filters {
        flex-direction: column;
      }

      .search-input,
      .status-filter {
        width: 100%;
      }

      .detail-overlay {
        padding: 1rem 0.5rem;
      }

      .detail-panel {
        padding: 1.25rem;
      }
    }
  `]
})
export class AdminUsersComponent implements OnInit {
  private readonly customersService = inject(AdminCustomersService);

  customers = signal<AdminCustomerListItem[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(20);
  search = signal('');
  statusFilter = signal('');
  totalPages = signal(1);

  // Detail panel state
  detailOpen = signal(false);
  detail = signal<AdminCustomerDetail | null>(null);
  detailLoading = signal(false);
  detailError = signal<string | null>(null);
  saving = signal(false);
  actionError = signal<string | null>(null);
  actionSuccess = signal<string | null>(null);

  private selectedId = '';

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.customersService.getCustomers({
      pageNumber: this.page(),
      pageSize: this.pageSize(),
      search: this.search() || undefined,
      status: this.statusFilter() || undefined
    }).subscribe({
      next: (result) => {
        this.customers.set(result.items);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('admin.users.error');
        this.loading.set(false);
      }
    });
  }

  applySearch(value: string): void {
    this.search.set(value.trim());
    this.page.set(1);
    this.loadCustomers();
  }

  applyStatusFilter(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadCustomers();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadCustomers();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
      this.loadCustomers();
    }
  }

  openDetail(id: string): void {
    this.selectedId = id;
    this.detailOpen.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);
    this.loadDetail();
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.detail.set(null);
    this.detailError.set(null);
    this.actionError.set(null);
    this.actionSuccess.set(null);
    this.selectedId = '';
  }

  reloadDetail(): void {
    this.loadDetail();
  }

  private loadDetail(): void {
    if (!this.selectedId) {
      return;
    }

    this.detailLoading.set(true);
    this.detailError.set(null);

    this.customersService.getCustomer(this.selectedId).subscribe({
      next: (customer) => {
        this.detail.set(customer);
        this.detailLoading.set(false);
      },
      error: () => {
        this.detailError.set('admin.users.detail.error');
        this.detailLoading.set(false);
      }
    });
  }

  toggleStatus(customer: AdminCustomerDetail): void {
    const newStatus = !customer.isActive;

    this.saving.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.customersService.updateStatus(customer.id, newStatus).subscribe({
      next: () => {
        this.saving.set(false);
        this.actionSuccess.set(
          newStatus ? 'admin.users.toasts.activated' : 'admin.users.toasts.deactivated'
        );
        // Refresh the detail panel and the list row.
        this.loadDetail();
        this.loadCustomers();
      },
      error: () => {
        this.saving.set(false);
        this.actionError.set('admin.users.toasts.statusFailed');
      }
    });
  }
}
