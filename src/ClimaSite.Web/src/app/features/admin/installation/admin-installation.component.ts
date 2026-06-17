import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import {
  AdminInstallationService,
  AdminInstallationRequest,
  INSTALLATION_STATUS_FILTERS,
  INSTALLATION_STATUS_TARGETS
} from '../../../core/services/admin-installation.service';

@Component({
  selector: 'app-admin-installation',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="installation-container" data-testid="admin-installation-page">
      <div class="installation-header">
        <h1>{{ 'admin.installation.title' | translate }}</h1>
        <p class="subtitle">{{ 'admin.installation.subtitle' | translate }}</p>
      </div>

      <!-- Filters -->
      <div class="filters">
        <select
          class="status-filter"
          [ngModel]="statusFilter()"
          (ngModelChange)="applyStatusFilter($event)"
          [attr.aria-label]="'admin.installation.filters.label' | translate"
          data-testid="installation-status-filter">
          <option value="">{{ 'admin.installation.filters.all' | translate }}</option>
          @for (status of statusFilters; track status) {
            <option [value]="status">{{ 'admin.installation.status.' + status | translate }}</option>
          }
        </select>
      </div>

      <!-- Action feedback -->
      @if (actionError()) {
        <div class="inline-error" data-testid="installation-action-error">
          {{ actionError()! | translate }}
        </div>
      }
      @if (actionSuccess()) {
        <div class="inline-success" data-testid="installation-action-success">
          {{ actionSuccess()! | translate }}
        </div>
      }

      <!-- Loading State -->
      @if (loading()) {
        <div class="loading" data-testid="installation-loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Error State -->
      @if (!loading() && error()) {
        <div class="error-message" data-testid="installation-error">
          <span>{{ error()! | translate }}</span>
          <button (click)="loadRequests()" data-testid="installation-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && !error() && requests().length === 0) {
        <div class="empty-state" data-testid="installation-empty">
          <p>{{ 'admin.installation.empty' | translate }}</p>
        </div>
      }

      <!-- Requests Table -->
      @if (!loading() && !error() && requests().length > 0) {
        <div class="table-wrapper">
          <table class="installation-table">
            <thead>
              <tr>
                <th>{{ 'admin.installation.columns.product' | translate }}</th>
                <th>{{ 'admin.installation.columns.type' | translate }}</th>
                <th>{{ 'admin.installation.columns.customer' | translate }}</th>
                <th>{{ 'admin.installation.columns.city' | translate }}</th>
                <th>{{ 'admin.installation.columns.status' | translate }}</th>
                <th>{{ 'admin.installation.columns.preferredDate' | translate }}</th>
                <th>{{ 'admin.installation.columns.scheduledDate' | translate }}</th>
                <th>{{ 'admin.installation.columns.estimatedPrice' | translate }}</th>
                <th>{{ 'admin.installation.columns.created' | translate }}</th>
                <th>{{ 'admin.installation.columns.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (request of requests(); track request.id) {
                <tr [attr.data-testid]="'installation-row'" [attr.data-request-id]="request.id">
                  <td class="product">{{ request.productName }}</td>
                  <td>{{ 'admin.installation.type.' + request.installationType | translate }}</td>
                  <td>
                    <div class="customer">
                      <span class="customer-name">{{ request.customerName }}</span>
                      <span class="customer-contact">{{ request.customerEmail }}</span>
                      <span class="customer-contact">{{ request.customerPhone }}</span>
                    </div>
                  </td>
                  <td>{{ request.city }}, {{ request.country }}</td>
                  <td>
                    <span
                      class="status-badge"
                      [class]="request.status.toLowerCase()"
                      data-testid="installation-status-badge">
                      {{ 'admin.installation.status.' + request.status | translate }}
                    </span>
                  </td>
                  <td class="muted">{{ request.preferredDate ? (request.preferredDate | date:'mediumDate') : '—' }}</td>
                  <td class="muted">{{ request.scheduledDate ? (request.scheduledDate | date:'mediumDate') : '—' }}</td>
                  <td>{{ request.estimatedPrice | currency:'EUR' }}</td>
                  <td class="muted">{{ request.createdAt | date:'mediumDate' }}</td>
                  <td>
                    <div class="status-actions">
                      <select
                        class="status-select"
                        [ngModel]="targetStatus(request.id)"
                        (ngModelChange)="setTargetStatus(request.id, $event)"
                        [attr.aria-label]="'admin.installation.actions.selectStatus' | translate"
                        [attr.data-testid]="'installation-status-select'"
                        [attr.data-request-id]="request.id">
                        <option value="">{{ 'admin.installation.actions.choose' | translate }}</option>
                        @for (status of statusTargets; track status) {
                          <option [value]="status">{{ 'admin.installation.status.' + status | translate }}</option>
                        }
                      </select>

                      @if (targetStatus(request.id) === 'Scheduled') {
                        <input
                          type="date"
                          class="schedule-input"
                          [ngModel]="scheduledDate(request.id)"
                          (ngModelChange)="setScheduledDate(request.id, $event)"
                          [attr.aria-label]="'admin.installation.actions.scheduledDate' | translate"
                          [attr.data-testid]="'installation-scheduled-date'"
                          [attr.data-request-id]="request.id" />
                      }

                      <button
                        class="apply-btn"
                        type="button"
                        [disabled]="!targetStatus(request.id) || savingId() === request.id"
                        (click)="applyStatus(request)"
                        [attr.data-testid]="'apply-installation-status'"
                        [attr.data-request-id]="request.id">
                        {{ 'admin.installation.actions.apply' | translate }}
                      </button>
                    </div>
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
            data-testid="installation-prev">
            {{ 'common.previous' | translate }}
          </button>
          <span class="page-indicator" data-testid="installation-page-indicator">
            {{ 'admin.installation.pageOf' | translate:{ page: page(), total: totalPages() } }}
          </span>
          <button
            class="page-btn"
            [disabled]="page() >= totalPages()"
            (click)="nextPage()"
            data-testid="installation-next">
            {{ 'common.next' | translate }}
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .installation-container {
      padding: 2rem;
      max-width: 1280px;
      margin: 0 auto;
    }

    .installation-header {
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

    .status-filter {
      min-width: 200px;
      padding: 0.625rem 0.875rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-input);
      color: var(--color-text-primary);
      font-size: 0.9375rem;
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

    .installation-table {
      width: 100%;
      border-collapse: collapse;

      th,
      td {
        padding: 0.875rem 1rem;
        text-align: left;
        border-bottom: 1px solid var(--color-border-primary);
        vertical-align: top;
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

    .product {
      font-weight: 600;
    }

    .customer {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;

      .customer-name {
        font-weight: 600;
      }

      .customer-contact {
        font-size: 0.8125rem;
        color: var(--color-text-secondary);
      }
    }

    .muted {
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
      &.confirmed { background: var(--color-primary-light); color: var(--color-primary-dark); }
      &.scheduled { background: var(--color-primary-light); color: var(--color-primary-dark); }
      &.inprogress { background: var(--color-warning-light); color: var(--color-warning-dark); }
      &.completed { background: var(--color-success-light); color: var(--color-success-dark); }
      &.cancelled { background: var(--color-error-light); color: var(--color-error-dark); }
    }

    .status-actions {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      min-width: 180px;
    }

    .status-select,
    .schedule-input {
      padding: 0.4rem 0.5rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-input);
      color: var(--color-text-primary);
      font-size: 0.875rem;
    }

    .apply-btn {
      padding: 0.4rem 0.75rem;
      border: none;
      border-radius: 6px;
      background: var(--color-primary);
      color: white;
      font-weight: 500;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
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
      .installation-container {
        padding: 1rem;
      }

      .filters {
        flex-direction: column;
      }

      .status-filter {
        width: 100%;
      }
    }
  `]
})
export class AdminInstallationComponent implements OnInit {
  private readonly installationService = inject(AdminInstallationService);

  readonly statusFilters = INSTALLATION_STATUS_FILTERS;
  readonly statusTargets = INSTALLATION_STATUS_TARGETS;

  requests = signal<AdminInstallationRequest[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(20);
  statusFilter = signal('');
  totalPages = signal(1);

  savingId = signal<string | null>(null);
  actionError = signal<string | null>(null);
  actionSuccess = signal<string | null>(null);

  // Per-row pending status change + optional scheduled date, keyed by request id.
  private readonly targetStatuses = signal<Record<string, string>>({});
  private readonly scheduledDates = signal<Record<string, string>>({});

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    this.error.set(null);

    this.installationService.getRequests({
      pageNumber: this.page(),
      pageSize: this.pageSize(),
      status: this.statusFilter() || undefined
    }).subscribe({
      next: (result) => {
        this.requests.set(result.items);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('admin.installation.error');
        this.loading.set(false);
      }
    });
  }

  applyStatusFilter(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadRequests();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadRequests();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
      this.loadRequests();
    }
  }

  targetStatus(id: string): string {
    return this.targetStatuses()[id] ?? '';
  }

  setTargetStatus(id: string, value: string): void {
    this.targetStatuses.update(map => ({ ...map, [id]: value }));
  }

  scheduledDate(id: string): string {
    return this.scheduledDates()[id] ?? '';
  }

  setScheduledDate(id: string, value: string): void {
    this.scheduledDates.update(map => ({ ...map, [id]: value }));
  }

  applyStatus(request: AdminInstallationRequest): void {
    const status = this.targetStatus(request.id);
    if (!status) {
      return;
    }

    if (status === 'Scheduled' && !this.scheduledDate(request.id)) {
      this.actionError.set('admin.installation.toasts.scheduledDateRequired');
      this.actionSuccess.set(null);
      return;
    }

    this.savingId.set(request.id);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    const payload = {
      status,
      scheduledDate: status === 'Scheduled'
        ? new Date(this.scheduledDate(request.id)).toISOString()
        : undefined
    };

    this.installationService.updateStatus(request.id, payload).subscribe({
      next: () => {
        this.savingId.set(null);
        this.actionSuccess.set('admin.installation.toasts.updated');
        // Reset the per-row controls and refresh the list.
        this.setTargetStatus(request.id, '');
        this.setScheduledDate(request.id, '');
        this.loadRequests();
      },
      error: () => {
        this.savingId.set(null);
        this.actionError.set('admin.installation.toasts.updateFailed');
      }
    });
  }
}
