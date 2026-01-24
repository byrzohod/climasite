import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { CheckoutService } from '../../../core/services/checkout.service';
import { OrderBrief, OrdersFilterParams, PaginatedOrders, ORDER_STATUS_CONFIG, OrderStatus } from '../../../core/models/order.model';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule],
  template: `
    <div class="orders-container" data-testid="orders-page">
      <h1>{{ 'account.orders.title' | translate }}</h1>

      <!-- Filters Section -->
      <div class="filters-section" data-testid="orders-filters">
        <div class="filters-row">
          <!-- Search -->
          <div class="filter-group search-group">
            <input
              type="text"
              class="search-input"
              [placeholder]="'account.orders.searchPlaceholder' | translate"
              [(ngModel)]="searchQuery"
              (input)="onSearchChange()"
              data-testid="orders-search"
            />
          </div>

          <!-- Status Filter -->
          <div class="filter-group">
            <select
              class="filter-select"
              [(ngModel)]="selectedStatus"
              (change)="onFilterChange()"
              data-testid="orders-status-filter"
            >
              <option value="">{{ 'account.orders.allStatuses' | translate }}</option>
              @for (status of availableStatuses(); track status) {
                <option [value]="status">{{ 'account.orders.statuses.' + status.toLowerCase() | translate }}</option>
              }
            </select>
          </div>

          <!-- Date Range -->
          <div class="filter-group date-range">
            <input
              type="date"
              class="date-input"
              [(ngModel)]="dateFrom"
              (change)="onFilterChange()"
              [placeholder]="'account.orders.dateFrom' | translate"
              data-testid="orders-date-from"
            />
            <span class="date-separator">-</span>
            <input
              type="date"
              class="date-input"
              [(ngModel)]="dateTo"
              (change)="onFilterChange()"
              [placeholder]="'account.orders.dateTo' | translate"
              data-testid="orders-date-to"
            />
          </div>
        </div>

        <div class="filters-row secondary">
          <!-- Sort Options -->
          <div class="filter-group sort-group">
            <label>{{ 'account.orders.sortBy' | translate }}:</label>
            <select
              class="filter-select"
              [(ngModel)]="sortBy"
              (change)="onFilterChange()"
              data-testid="orders-sort-by"
            >
              <option value="date">{{ 'account.orders.sortOptions.date' | translate }}</option>
              <option value="total">{{ 'account.orders.sortOptions.total' | translate }}</option>
              <option value="status">{{ 'account.orders.sortOptions.status' | translate }}</option>
            </select>
            <button
              class="sort-direction-btn"
              (click)="toggleSortDirection()"
              [attr.data-direction]="sortDirection"
              data-testid="orders-sort-direction"
            >
              @if (sortDirection === 'desc') {
                <span>&#9660;</span>
              } @else {
                <span>&#9650;</span>
              }
            </button>
          </div>

          <!-- Page Size -->
          <div class="filter-group">
            <label>{{ 'account.orders.showPerPage' | translate }}:</label>
            <select
              class="filter-select page-size"
              [(ngModel)]="pageSize"
              (change)="onPageSizeChange()"
              data-testid="orders-page-size"
            >
              <option [value]="5">5</option>
              <option [value]="10">10</option>
              <option [value]="20">20</option>
              <option [value]="50">50</option>
            </select>
          </div>

          <!-- Clear Filters -->
          @if (hasActiveFilters()) {
            <button class="clear-filters-btn" (click)="clearFilters()" data-testid="orders-clear-filters">
              {{ 'account.orders.clearFilters' | translate }}
            </button>
          }
        </div>
      </div>

      <!-- Results Info -->
      @if (!isLoading() && paginatedOrders()) {
        <div class="results-info" data-testid="orders-results-info">
          {{ 'account.orders.showingResults' | translate:{count: paginatedOrders()!.totalCount} }}
        </div>
      }

      <!-- Loading State -->
      @if (isLoading()) {
        <div class="orders-list" data-testid="orders-loading">
          @for (i of [1, 2, 3]; track i) {
            <div class="order-card skeleton">
              <div class="order-header">
                <div class="skeleton-line skeleton-medium"></div>
                <div class="skeleton-line skeleton-small"></div>
              </div>
              <div class="order-items">
                <div class="skeleton-item"></div>
                <div class="skeleton-item"></div>
              </div>
              <div class="order-footer">
                <div class="skeleton-line skeleton-small"></div>
                <div class="skeleton-button"></div>
              </div>
            </div>
          }
        </div>
      } @else if (orders().length === 0) {
        <div class="empty-state" data-testid="orders-empty">
          @if (hasActiveFilters()) {
            <p>{{ 'account.orders.noMatchingOrders' | translate }}</p>
            <button class="btn-secondary" (click)="clearFilters()">{{ 'account.orders.clearFilters' | translate }}</button>
          } @else {
            <p>{{ 'account.orders.empty' | translate }}</p>
            <a routerLink="/products" class="btn-primary">{{ 'cart.continueShopping' | translate }}</a>
          }
        </div>
      } @else {
        <!-- Orders List -->
        <div class="orders-list" data-testid="orders-list">
          @for (order of orders(); track order.id) {
            <div class="order-card" data-testid="order-card">
              <div class="order-header">
                <div class="order-info">
                  <span class="order-number" data-testid="order-number">{{ 'account.orders.orderNumber' | translate }}{{ order.orderNumber }}</span>
                  <span class="order-date">{{ order.createdAt | date:'mediumDate' }}</span>
                </div>
                <span
                  class="order-status"
                  [style.background]="getStatusConfig(order.status).bgColor"
                  [style.color]="getStatusConfig(order.status).color"
                >
                  {{ 'account.orders.statuses.' + order.status.toLowerCase() | translate }}
                </span>
              </div>

              <div class="order-items">
                @for (item of order.items.slice(0, 3); track item.id) {
                  <div class="order-item">
                    @if (item.imageUrl) {
                      <img [src]="item.imageUrl" [alt]="item.productName" class="item-image" loading="lazy" />
                    } @else {
                      <div class="item-placeholder"></div>
                    }
                    <div class="item-details">
                      <span class="item-name">{{ item.productName }}</span>
                      <span class="item-qty">x{{ item.quantity }}</span>
                    </div>
                  </div>
                }
                @if (order.itemCount > 3) {
                  <span class="more-items">+{{ order.itemCount - 3 }} {{ 'account.orders.moreItems' | translate }}</span>
                }
              </div>

              <div class="order-footer">
                <div class="order-summary">
                  <span class="order-total">{{ 'account.orders.total' | translate }}: {{ order.total | currency:'EUR' }}</span>
                  <span class="order-item-count">{{ order.itemCount }} {{ order.itemCount === 1 ? ('account.orders.item' | translate) : ('account.orders.items' | translate) }}</span>
                </div>
                <a [routerLink]="['/account/orders', order.id]" class="btn-view-details" data-testid="view-order-details">
                  {{ 'account.orders.viewDetails' | translate }}
                </a>
              </div>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (paginatedOrders() && paginatedOrders()!.totalPages > 1) {
          <div class="pagination" data-testid="orders-pagination">
            <button
              class="pagination-btn"
              [disabled]="!paginatedOrders()!.hasPreviousPage"
              (click)="goToPage(currentPage - 1)"
              data-testid="pagination-prev"
            >
              {{ 'common.previous' | translate }}
            </button>

            <div class="pagination-pages">
              @for (page of getPageNumbers(); track page) {
                @if (page === -1) {
                  <span class="pagination-ellipsis">...</span>
                } @else {
                  <button
                    class="pagination-page"
                    [class.active]="page === currentPage"
                    (click)="goToPage(page)"
                    [attr.data-testid]="'pagination-page-' + page"
                  >
                    {{ page }}
                  </button>
                }
              }
            </div>

            <button
              class="pagination-btn"
              [disabled]="!paginatedOrders()!.hasNextPage"
              (click)="goToPage(currentPage + 1)"
              data-testid="pagination-next"
            >
              {{ 'common.next' | translate }}
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .orders-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
      width: 100%;
      box-sizing: border-box;
      overflow-x: hidden;

      h1 {
        font-size: 2rem;
        margin-bottom: 1.5rem;
        color: var(--color-text-primary);
      }
    }

    /* Filters Section */
    .filters-section {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
    }

    .filters-row {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;

      &.secondary {
        margin-top: 1rem;
        padding-top: 1rem;
        border-top: 1px solid var(--color-border);
      }
    }

    .filter-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      label {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        white-space: nowrap;
      }
    }

    .search-group {
      flex: 1;
      min-width: 200px;
    }

    .search-input {
      width: 100%;
      padding: 0.625rem 1rem;
      border: 1px solid var(--color-border);
      border-radius: 8px;
      font-size: 0.875rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);

      &::placeholder {
        color: var(--color-text-tertiary);
      }

      &:focus {
        outline: none;
        border-color: var(--color-primary);
      }
    }

    .filter-select {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      font-size: 0.875rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      cursor: pointer;

      &:focus {
        outline: none;
        border-color: var(--color-primary);
      }

      &.page-size {
        width: 70px;
      }
    }

    .date-range {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .date-input {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      font-size: 0.875rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);

      &:focus {
        outline: none;
        border-color: var(--color-primary);
      }
    }

    .date-separator {
      color: var(--color-text-secondary);
    }

    .sort-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .sort-direction-btn {
      padding: 0.375rem 0.5rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      cursor: pointer;
      font-size: 0.75rem;

      &:hover {
        background: var(--color-bg-tertiary);
      }
    }

    .clear-filters-btn {
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 6px;
      background: var(--color-error-light);
      color: var(--color-error);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      margin-left: auto;

      &:hover {
        background: var(--color-error);
        color: white;
      }
    }

    /* Results Info */
    .results-info {
      margin-bottom: 1rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    /* Loading Skeleton */
    .skeleton {
      pointer-events: none;
    }

    .skeleton-line {
      background: linear-gradient(90deg, var(--color-bg-secondary) 25%, var(--color-bg-tertiary) 50%, var(--color-bg-secondary) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 4px;
      height: 1rem;

      &.skeleton-medium {
        width: 150px;
        height: 1.25rem;
      }

      &.skeleton-small {
        width: 100px;
        height: 0.875rem;
      }
    }

    .skeleton-item {
      width: 60px;
      height: 60px;
      background: linear-gradient(90deg, var(--color-bg-secondary) 25%, var(--color-bg-tertiary) 50%, var(--color-bg-secondary) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 8px;
    }

    .skeleton-button {
      width: 120px;
      height: 36px;
      background: linear-gradient(90deg, var(--color-bg-secondary) 25%, var(--color-bg-tertiary) 50%, var(--color-bg-secondary) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 6px;
    }

    @keyframes shimmer {
      0% { background-position: -200% 0; }
      100% { background-position: 200% 0; }
    }

    /* Empty State */
    .empty-state {
      text-align: center;
      padding: 3rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;

      p {
        margin-bottom: 1.5rem;
        color: var(--color-text-secondary);
      }

      .btn-primary {
        display: inline-block;
        padding: 0.75rem 1.5rem;
        background: var(--color-primary);
        color: white;
        text-decoration: none;
        border-radius: 8px;
        font-weight: 600;

        &:hover {
          background: var(--color-primary-dark);
        }
      }

      .btn-secondary {
        padding: 0.75rem 1.5rem;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        font-weight: 600;
        cursor: pointer;

        &:hover {
          background: var(--color-bg-tertiary);
        }
      }
    }

    /* Orders List */
    .orders-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .order-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;
      transition: box-shadow 0.2s ease;

      &:hover {
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      }
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.25rem;
      background: var(--color-bg-secondary);
      border-bottom: 1px solid var(--color-border);
    }

    .order-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .order-number {
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .order-date {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .order-status {
      padding: 0.375rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .order-items {
      padding: 1rem 1.25rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .order-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .item-image, .item-placeholder {
      width: 50px;
      height: 50px;
      border-radius: 6px;
      object-fit: cover;
    }

    .item-placeholder {
      background: var(--color-bg-secondary);
    }

    .item-details {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .item-name {
      font-size: 0.8125rem;
      color: var(--color-text-primary);
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .item-qty {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
    }

    .more-items {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .order-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.25rem;
      border-top: 1px solid var(--color-border);
    }

    .order-summary {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .order-total {
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .order-item-count {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
    }

    .btn-view-details {
      padding: 0.5rem 1rem;
      background: var(--color-primary);
      color: white;
      text-decoration: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      transition: background 0.2s ease;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    /* Pagination */
    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 0.5rem;
      margin-top: 2rem;
      padding: 1rem;
    }

    .pagination-btn {
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: var(--color-bg-primary);
      color: var(--color-text-primary);
      font-size: 0.875rem;
      cursor: pointer;

      &:hover:not(:disabled) {
        background: var(--color-bg-secondary);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .pagination-pages {
      display: flex;
      gap: 0.25rem;
    }

    .pagination-page {
      width: 36px;
      height: 36px;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: var(--color-bg-primary);
      color: var(--color-text-primary);
      font-size: 0.875rem;
      cursor: pointer;

      &:hover {
        background: var(--color-bg-secondary);
      }

      &.active {
        background: var(--color-primary);
        border-color: var(--color-primary);
        color: white;
      }
    }

    .pagination-ellipsis {
      width: 36px;
      height: 36px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-text-secondary);
    }

    /* Mobile Responsive */
    @media (max-width: 768px) {
      .orders-container {
        padding: 1rem;
        max-width: 100%;

        h1 {
          font-size: 1.5rem;
        }
      }

      .filters-section {
        padding: 1rem;
        max-width: 100%;
      }

      .filters-row {
        flex-direction: column;
        align-items: stretch;

        &.secondary {
          flex-direction: row;
          flex-wrap: wrap;
        }
      }

      .filter-group {
        width: 100%;

        &.search-group {
          min-width: auto;
        }
      }

      .date-range {
        flex-direction: column;
        align-items: stretch;

        .date-separator {
          display: none;
        }

        .date-input {
          width: 100%;
        }
      }

      .sort-group {
        flex: 1;
      }

      .clear-filters-btn {
        width: 100%;
        margin-left: 0;
      }

      .order-card {
        max-width: 100%;
        overflow: hidden;
      }

      .order-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.75rem;
      }

      .order-items {
        padding: 0.75rem;
      }

      .item-name {
        max-width: 80px;
      }

      .order-footer {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
        padding: 1rem;
      }

      .btn-view-details {
        text-align: center;
      }

      .pagination {
        flex-wrap: wrap;
      }

      .pagination-pages {
        order: 3;
        width: 100%;
        justify-content: center;
        margin-top: 0.5rem;
      }
    }
  `]
})
export class OrdersComponent implements OnInit {
  private readonly checkoutService = inject(CheckoutService);

  // Data
  paginatedOrders = signal<PaginatedOrders | null>(null);
  orders = computed(() => this.paginatedOrders()?.items ?? []);
  availableStatuses = signal<string[]>([]);
  isLoading = signal(true);

  // Filter state
  searchQuery = '';
  selectedStatus = '';
  dateFrom = '';
  dateTo = '';
  sortBy: 'date' | 'total' | 'status' = 'date';
  sortDirection: 'asc' | 'desc' = 'desc';
  currentPage = 1;
  pageSize = 10;

  private searchDebounceTimer: any;

  ngOnInit(): void {
    this.loadStatuses();
    this.loadOrders();
  }

  private loadStatuses(): void {
    this.checkoutService.getOrderStatuses().subscribe({
      next: (statuses) => this.availableStatuses.set(statuses),
      error: () => {
        // Use default statuses if API fails
        this.availableStatuses.set(['Pending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded', 'Returned']);
      }
    });
  }

  loadOrders(): void {
    this.isLoading.set(true);
    const params = this.buildFilterParams();

    this.checkoutService.getOrders(params).subscribe({
      next: (result) => {
        this.paginatedOrders.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.paginatedOrders.set({ items: [], pageNumber: 1, totalPages: 0, totalCount: 0, hasPreviousPage: false, hasNextPage: false });
        this.isLoading.set(false);
      }
    });
  }

  private buildFilterParams(): OrdersFilterParams {
    return {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      status: this.selectedStatus || undefined,
      dateFrom: this.dateFrom || undefined,
      dateTo: this.dateTo || undefined,
      search: this.searchQuery || undefined,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  onSearchChange(): void {
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.currentPage = 1;
      this.loadOrders();
    }, 300);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onPageSizeChange(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'desc' ? 'asc' : 'desc';
    this.loadOrders();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchQuery || this.selectedStatus || this.dateFrom || this.dateTo);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedStatus = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.currentPage = 1;
    this.loadOrders();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= (this.paginatedOrders()?.totalPages ?? 1)) {
      this.currentPage = page;
      this.loadOrders();
    }
  }

  getPageNumbers(): number[] {
    const totalPages = this.paginatedOrders()?.totalPages ?? 1;
    const current = this.currentPage;
    const pages: number[] = [];

    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1); // ellipsis
      for (let i = Math.max(2, current - 1); i <= Math.min(totalPages - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < totalPages - 2) pages.push(-1); // ellipsis
      pages.push(totalPages);
    }

    return pages;
  }

  getStatusConfig(status: string): { bgColor: string; color: string } {
    const config = ORDER_STATUS_CONFIG[status as OrderStatus];
    return config
      ? { bgColor: config.bgColorVar, color: config.colorVar }
      : { bgColor: 'var(--color-status-neutral-bg)', color: 'var(--color-status-neutral-text)' };
  }
}
