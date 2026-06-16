import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import {
  AdminOrdersService,
  AdminOrderDetail,
  getValidNextStatuses
} from '../../../core/services/admin-orders.service';

@Component({
  selector: 'app-admin-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  template: `
    <div class="detail-container" data-testid="admin-order-detail">
      <a class="back-link" [routerLink]="['/admin/orders']" data-testid="back-to-orders">
        &larr; {{ 'admin.orders.backToList' | translate }}
      </a>

      <!-- Loading -->
      @if (loading()) {
        <div class="loading" data-testid="order-detail-loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Load error -->
      @if (!loading() && loadError()) {
        <div class="error-message" data-testid="order-detail-error">
          <span>{{ loadError()! | translate }}</span>
          <button (click)="loadOrder()" data-testid="order-detail-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      }

      @if (!loading() && !loadError() && order(); as ord) {
        <div class="detail-header">
          <div>
            <h1>{{ 'admin.orders.orderTitle' | translate:{ number: ord.orderNumber } }}</h1>
            <span class="created-date">{{ ord.createdAt | date:'medium' }}</span>
          </div>
          <span
            class="status-badge"
            [class]="ord.status.toLowerCase()"
            data-testid="order-status-badge">
            {{ 'admin.orders.status.' + ord.status | translate }}
          </span>
        </div>

        <!-- Action feedback -->
        @if (actionError()) {
          <div class="inline-error" data-testid="order-action-error">
            {{ actionError()! | translate }}
          </div>
        }
        @if (actionSuccess()) {
          <div class="inline-success" data-testid="order-action-success">
            {{ actionSuccess()! | translate }}
          </div>
        }

        <div class="detail-grid">
          <!-- Customer -->
          <section class="card">
            <h2>{{ 'admin.orders.customer' | translate }}</h2>
            <p><strong>{{ ord.customerName }}</strong></p>
            <p>{{ ord.customerEmail }}</p>
            @if (ord.customerPhone) {
              <p>{{ ord.customerPhone }}</p>
            }
          </section>

          <!-- Shipping address -->
          <section class="card">
            <h2>{{ 'admin.orders.shippingAddress' | translate }}</h2>
            @if (shippingAddressLines().length > 0) {
              @for (line of shippingAddressLines(); track $index) {
                <p>{{ line }}</p>
              }
            } @else {
              <p class="muted">{{ 'admin.orders.noAddress' | translate }}</p>
            }
          </section>

          <!-- Billing address -->
          <section class="card">
            <h2>{{ 'admin.orders.billingAddress' | translate }}</h2>
            @if (billingAddressLines().length > 0) {
              @for (line of billingAddressLines(); track $index) {
                <p>{{ line }}</p>
              }
            } @else {
              <p class="muted">{{ 'admin.orders.noAddress' | translate }}</p>
            }
          </section>
        </div>

        <!-- Line items -->
        <section class="card full">
          <h2>{{ 'admin.orders.items' | translate }}</h2>
          <div class="table-wrapper">
            <table class="items-table">
              <thead>
                <tr>
                  <th>{{ 'admin.orders.itemColumns.product' | translate }}</th>
                  <th>{{ 'admin.orders.itemColumns.sku' | translate }}</th>
                  <th>{{ 'admin.orders.itemColumns.quantity' | translate }}</th>
                  <th>{{ 'admin.orders.itemColumns.unitPrice' | translate }}</th>
                  <th>{{ 'admin.orders.itemColumns.lineTotal' | translate }}</th>
                </tr>
              </thead>
              <tbody>
                @for (item of ord.items; track item.id) {
                  <tr data-testid="order-item-row">
                    <td>
                      <div class="product-cell">
                        @if (item.imageUrl) {
                          <img
                            class="item-image"
                            [src]="item.imageUrl"
                            [alt]="item.productName"
                            loading="lazy" />
                        }
                        <div>
                          <span class="product-name">{{ item.productName }}</span>
                          @if (item.variantName) {
                            <span class="variant-name">{{ item.variantName }}</span>
                          }
                        </div>
                      </div>
                    </td>
                    <td>{{ item.sku }}</td>
                    <td>{{ item.quantity }}</td>
                    <td>{{ item.unitPrice | currency:ord.currency }}</td>
                    <td>{{ item.lineTotal | currency:ord.currency }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Totals -->
          <div class="totals">
            <div class="total-row">
              <span>{{ 'admin.orders.totals.subtotal' | translate }}</span>
              <span>{{ ord.subtotal | currency:ord.currency }}</span>
            </div>
            <div class="total-row">
              <span>{{ 'admin.orders.totals.shipping' | translate }}</span>
              <span>{{ ord.shippingCost | currency:ord.currency }}</span>
            </div>
            <div class="total-row">
              <span>{{ 'admin.orders.totals.tax' | translate }}</span>
              <span>{{ ord.taxAmount | currency:ord.currency }}</span>
            </div>
            @if (ord.discountAmount > 0) {
              <div class="total-row">
                <span>{{ 'admin.orders.totals.discount' | translate }}</span>
                <span>-{{ ord.discountAmount | currency:ord.currency }}</span>
              </div>
            }
            <div class="total-row grand" data-testid="order-grand-total">
              <span>{{ 'admin.orders.totals.total' | translate }}</span>
              <span>{{ ord.totalAmount | currency:ord.currency }}</span>
            </div>
          </div>
        </section>

        <!-- Timeline -->
        <section class="card full">
          <h2>{{ 'admin.orders.timeline' | translate }}</h2>
          <ul class="timeline" data-testid="order-timeline">
            <li class="timeline-event">
              <span class="event-label">{{ 'admin.orders.events.created' | translate }}</span>
              <span class="event-date">{{ ord.createdAt | date:'medium' }}</span>
            </li>
            @if (ord.paidAt) {
              <li class="timeline-event">
                <span class="event-label">{{ 'admin.orders.events.paid' | translate }}</span>
                <span class="event-date">{{ ord.paidAt | date:'medium' }}</span>
              </li>
            }
            @if (ord.shippedAt) {
              <li class="timeline-event">
                <span class="event-label">{{ 'admin.orders.events.shipped' | translate }}</span>
                <span class="event-date">{{ ord.shippedAt | date:'medium' }}</span>
              </li>
            }
            @if (ord.deliveredAt) {
              <li class="timeline-event">
                <span class="event-label">{{ 'admin.orders.events.delivered' | translate }}</span>
                <span class="event-date">{{ ord.deliveredAt | date:'medium' }}</span>
              </li>
            }
            @if (ord.cancelledAt) {
              <li class="timeline-event">
                <span class="event-label">{{ 'admin.orders.events.cancelled' | translate }}</span>
                <span class="event-date">{{ ord.cancelledAt | date:'medium' }}</span>
              </li>
            }
          </ul>
          @if (ord.trackingNumber) {
            <p class="tracking" data-testid="order-tracking-number">
              <strong>{{ 'admin.orders.trackingNumber' | translate }}:</strong> {{ ord.trackingNumber }}
            </p>
          }
        </section>

        <!-- Action panels -->
        <div class="actions-grid">
          <!-- Change status -->
          <section class="card">
            <h2>{{ 'admin.orders.changeStatus' | translate }}</h2>
            @if (validNextStatuses().length > 0) {
              <label class="field">
                <span>{{ 'admin.orders.newStatus' | translate }}</span>
                <select [(ngModel)]="selectedStatus" data-testid="status-select">
                  <option value="">{{ 'admin.orders.selectStatus' | translate }}</option>
                  @for (next of validNextStatuses(); track next) {
                    <option [value]="next">{{ 'admin.orders.status.' + next | translate }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>{{ 'admin.orders.note' | translate }}</span>
                <textarea
                  [(ngModel)]="statusNote"
                  rows="2"
                  [placeholder]="'admin.orders.notePlaceholder' | translate"
                  data-testid="status-note"></textarea>
              </label>
              <label class="checkbox-field">
                <input type="checkbox" [(ngModel)]="notifyCustomer" data-testid="notify-customer" />
                <span>{{ 'admin.orders.notifyCustomer' | translate }}</span>
              </label>
              <button
                class="action-btn primary"
                [disabled]="!selectedStatus || saving()"
                (click)="applyStatus()"
                data-testid="apply-status">
                {{ 'admin.orders.applyStatus' | translate }}
              </button>
            } @else {
              <p class="muted">{{ 'admin.orders.noTransitions' | translate }}</p>
            }
          </section>

          <!-- Shipping / tracking -->
          <section class="card">
            <h2>{{ 'admin.orders.shippingTracking' | translate }}</h2>
            <label class="field">
              <span>{{ 'admin.orders.trackingNumber' | translate }}</span>
              <input
                type="text"
                [(ngModel)]="trackingNumber"
                [placeholder]="'admin.orders.trackingPlaceholder' | translate"
                data-testid="tracking-input" />
            </label>
            <label class="field">
              <span>{{ 'admin.orders.shippingMethod' | translate }}</span>
              <input
                type="text"
                [(ngModel)]="shippingMethod"
                [placeholder]="'admin.orders.shippingMethodPlaceholder' | translate"
                data-testid="shipping-method-input" />
            </label>
            <label class="checkbox-field">
              <input type="checkbox" [(ngModel)]="markAsShipped" data-testid="mark-shipped" />
              <span>{{ 'admin.orders.markAsShipped' | translate }}</span>
            </label>
            <button
              class="action-btn primary"
              [disabled]="saving()"
              (click)="saveShipping()"
              data-testid="save-shipping">
              {{ 'admin.orders.saveShipping' | translate }}
            </button>
          </section>

          <!-- Add note -->
          <section class="card">
            <h2>{{ 'admin.orders.addNote' | translate }}</h2>
            <label class="field">
              <span>{{ 'admin.orders.note' | translate }}</span>
              <textarea
                [(ngModel)]="newNote"
                rows="3"
                [placeholder]="'admin.orders.notePlaceholder' | translate"
                data-testid="note-input"></textarea>
            </label>
            <button
              class="action-btn primary"
              [disabled]="!newNote.trim() || saving()"
              (click)="addNote()"
              data-testid="add-note">
              {{ 'admin.orders.addNote' | translate }}
            </button>
            @if (ord.notes) {
              <div class="existing-notes">
                <strong>{{ 'admin.orders.existingNotes' | translate }}</strong>
                <p>{{ ord.notes }}</p>
              </div>
            }
          </section>
        </div>
      }
    </div>
  `,
  styles: [`
    .detail-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .back-link {
      display: inline-block;
      margin-bottom: 1.5rem;
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 500;

      &:hover {
        text-decoration: underline;
      }
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

    .detail-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;

      h1 {
        font-size: 1.75rem;
        color: var(--color-text-primary);
        margin: 0 0 0.25rem;
      }

      .created-date {
        color: var(--color-text-tertiary);
        font-size: 0.875rem;
      }
    }

    .status-badge {
      padding: 0.375rem 0.875rem;
      border-radius: 4px;
      font-size: 0.8125rem;
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

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .card {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
      padding: 1.25rem;

      &.full {
        margin-bottom: 1rem;
      }

      h2 {
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

    .table-wrapper {
      overflow-x: auto;
    }

    .items-table {
      width: 100%;
      border-collapse: collapse;

      th,
      td {
        padding: 0.75rem;
        text-align: left;
        border-bottom: 1px solid var(--color-border-primary);
        font-size: 0.9375rem;
      }

      th {
        font-size: 0.75rem;
        text-transform: uppercase;
        color: var(--color-text-secondary);
      }

      td {
        color: var(--color-text-primary);
      }
    }

    .product-cell {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .item-image {
      width: 48px;
      height: 48px;
      object-fit: cover;
      border-radius: 4px;
      border: 1px solid var(--color-border-primary);
    }

    .product-name {
      display: block;
      color: var(--color-text-primary);
    }

    .variant-name {
      display: block;
      font-size: 0.8125rem;
      color: var(--color-text-tertiary);
    }

    .totals {
      margin-top: 1rem;
      max-width: 320px;
      margin-left: auto;

      .total-row {
        display: flex;
        justify-content: space-between;
        padding: 0.375rem 0;
        color: var(--color-text-secondary);
        font-size: 0.9375rem;

        &.grand {
          border-top: 1px solid var(--color-border-primary);
          margin-top: 0.5rem;
          padding-top: 0.75rem;
          font-weight: 700;
          font-size: 1.0625rem;
          color: var(--color-text-primary);
        }
      }
    }

    .timeline {
      list-style: none;
      margin: 0;
      padding: 0;

      .timeline-event {
        display: flex;
        justify-content: space-between;
        padding: 0.5rem 0;
        border-bottom: 1px solid var(--color-border-primary);

        &:last-child {
          border-bottom: none;
        }

        .event-label {
          color: var(--color-text-primary);
          font-weight: 500;
        }

        .event-date {
          color: var(--color-text-tertiary);
          font-size: 0.875rem;
        }
      }
    }

    .tracking {
      margin-top: 1rem;
      color: var(--color-text-primary);
    }

    .field {
      display: block;
      margin-bottom: 0.875rem;

      span {
        display: block;
        margin-bottom: 0.25rem;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      input,
      textarea,
      select {
        width: 100%;
        padding: 0.5rem 0.625rem;
        border: 1px solid var(--color-border-primary);
        border-radius: 6px;
        background: var(--color-bg-input);
        color: var(--color-text-primary);
        font-size: 0.9375rem;
        font-family: inherit;
      }
    }

    .checkbox-field {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.875rem;
      color: var(--color-text-secondary);
      font-size: 0.9375rem;
      cursor: pointer;
    }

    .action-btn {
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
    }

    .existing-notes {
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border-primary);

      strong {
        display: block;
        margin-bottom: 0.25rem;
        color: var(--color-text-primary);
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

    .error-message {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-error-light);
      color: var(--color-error-dark);
      border-radius: 8px;

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
      .detail-container {
        padding: 1rem;
      }
    }
  `]
})
export class AdminOrderDetailComponent implements OnInit {
  private readonly ordersService = inject(AdminOrdersService);
  private readonly route = inject(ActivatedRoute);

  order = signal<AdminOrderDetail | null>(null);
  loading = signal(false);
  loadError = signal<string | null>(null);
  saving = signal(false);
  actionError = signal<string | null>(null);
  actionSuccess = signal<string | null>(null);

  private orderId = '';

  // Change-status form
  selectedStatus = '';
  statusNote = '';
  notifyCustomer = true;

  // Shipping form
  trackingNumber = '';
  shippingMethod = '';
  markAsShipped = false;

  // Note form
  newNote = '';

  validNextStatuses = computed(() => {
    const current = this.order()?.status;
    return current ? getValidNextStatuses(current) : [];
  });

  shippingAddressLines = computed(() => this.formatAddress(this.order()?.shippingAddress));
  billingAddressLines = computed(() => this.formatAddress(this.order()?.billingAddress));

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadOrder();
  }

  loadOrder(): void {
    if (!this.orderId) {
      this.loadError.set('admin.orders.errors.loadFailed');
      return;
    }

    this.loading.set(true);
    this.loadError.set(null);

    this.ordersService.getOrder(this.orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.trackingNumber = order.trackingNumber ?? '';
        this.shippingMethod = order.shippingMethod ?? '';
        this.selectedStatus = '';
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('admin.orders.errors.loadFailed');
        this.loading.set(false);
      }
    });
  }

  applyStatus(): void {
    if (!this.selectedStatus) {
      return;
    }

    this.startAction();
    this.ordersService.updateStatus(this.orderId, {
      status: this.selectedStatus,
      note: this.statusNote.trim() || undefined,
      notifyCustomer: this.notifyCustomer
    }).subscribe({
      next: () => {
        this.statusNote = '';
        this.finishAction('admin.orders.toasts.statusUpdated');
      },
      error: () => this.failAction('admin.orders.errors.statusFailed')
    });
  }

  saveShipping(): void {
    this.startAction();
    this.ordersService.updateShipping(this.orderId, {
      trackingNumber: this.trackingNumber.trim() || undefined,
      shippingMethod: this.shippingMethod.trim() || undefined,
      markAsShipped: this.markAsShipped
    }).subscribe({
      next: () => {
        this.markAsShipped = false;
        this.finishAction('admin.orders.toasts.shippingUpdated');
      },
      error: () => this.failAction('admin.orders.errors.shippingFailed')
    });
  }

  addNote(): void {
    const note = this.newNote.trim();
    if (!note) {
      return;
    }

    this.startAction();
    this.ordersService.addNote(this.orderId, note).subscribe({
      next: () => {
        this.newNote = '';
        this.finishAction('admin.orders.toasts.noteAdded');
      },
      error: () => this.failAction('admin.orders.errors.noteFailed')
    });
  }

  private startAction(): void {
    this.saving.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);
  }

  private finishAction(successKey: string): void {
    this.saving.set(false);
    this.actionSuccess.set(successKey);
    this.loadOrder();
  }

  private failAction(errorKey: string): void {
    this.saving.set(false);
    this.actionError.set(errorKey);
  }

  private formatAddress(address: Record<string, unknown> | null | undefined): string[] {
    if (!address) {
      return [];
    }

    const order = [
      'firstName', 'lastName', 'addressLine1', 'addressLine2',
      'city', 'state', 'postalCode', 'country', 'phone'
    ];

    const firstName = this.stringValue(address['firstName']);
    const lastName = this.stringValue(address['lastName']);
    const nameLine = [firstName, lastName].filter(Boolean).join(' ').trim();

    const lines: string[] = [];
    if (nameLine) {
      lines.push(nameLine);
    }

    for (const key of order) {
      if (key === 'firstName' || key === 'lastName') {
        continue;
      }
      const value = this.stringValue(address[key]);
      if (value) {
        lines.push(value);
      }
    }

    return lines;
  }

  private stringValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }
    return String(value).trim();
  }
}
