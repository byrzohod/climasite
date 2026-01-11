import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { CheckoutService } from '../../../core/services/checkout.service';
import { Order, OrderEvent, ORDER_STATUS_CONFIG, OrderStatus } from '../../../core/models/order.model';

@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule],
  template: `
    <div class="order-details-container" data-testid="order-details-page">
      <a routerLink="/account/orders" class="back-link">
        <span class="back-arrow">&#8592;</span> {{ 'account.orders.backToOrders' | translate }}
      </a>

      @if (isLoading()) {
        <div class="loading-container">
          <div class="loading-spinner"></div>
          <p>{{ 'common.loading' | translate }}</p>
        </div>
      } @else if (error()) {
        <div class="error-container">
          <p class="error-message">{{ error() }}</p>
          <a routerLink="/account/orders" class="btn-primary">{{ 'account.orders.backToOrders' | translate }}</a>
        </div>
      } @else if (order()) {
        <div class="order-content">
          <!-- Order Header -->
          <div class="order-header">
            <div class="header-info">
              <h1>{{ 'account.orders.orderDetails' | translate }}</h1>
              <p class="order-number">{{ 'account.orders.orderNumber' | translate }}{{ order()!.orderNumber }}</p>
              <p class="order-date">{{ 'account.orders.orderDate' | translate }}: {{ order()!.createdAt | date:'medium' }}</p>
            </div>
            <div class="header-actions">
              <span
                class="order-status"
                [style.background]="getStatusConfig(order()!.status).bgColor"
                [style.color]="getStatusConfig(order()!.status).color"
              >
                {{ 'account.orders.statuses.' + order()!.status.toLowerCase() | translate }}
              </span>
              @if (canCancel()) {
                <button class="btn-cancel" (click)="showCancelModal = true" data-testid="cancel-order-btn">
                  {{ 'account.orders.actions.cancelOrder' | translate }}
                </button>
              }
            </div>
          </div>

          <div class="order-grid">
            <!-- Order Timeline -->
            @if (order()!.events && order()!.events.length > 0) {
              <div class="section timeline-section">
                <h2>{{ 'account.orders.timeline.title' | translate }}</h2>
                <div class="timeline">
                  @for (event of order()!.events; track event.id; let i = $index) {
                    <div class="timeline-item" [class.active]="i === 0">
                      <div class="timeline-marker"></div>
                      <div class="timeline-content">
                        <span class="timeline-status" [style.color]="getStatusConfig(event.status).color">
                          {{ 'account.orders.statuses.' + event.status.toLowerCase() | translate }}
                        </span>
                        @if (event.description) {
                          <p class="timeline-description">{{ event.description }}</p>
                        }
                        <span class="timeline-date">{{ event.createdAt | date:'medium' }}</span>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }

            <!-- Order Items -->
            <div class="section items-section">
              <h2>{{ 'account.orders.items' | translate }} ({{ order()!.items.length }})</h2>
              <div class="items-list">
                @for (item of order()!.items; track item.id) {
                  <div class="order-item" data-testid="order-item">
                    @if (item.imageUrl) {
                      <img [src]="item.imageUrl" [alt]="item.productName" class="item-image" />
                    } @else {
                      <div class="item-placeholder"></div>
                    }
                    <div class="item-info">
                      @if (item.productSlug) {
                        <a [routerLink]="['/products', item.productSlug]" class="item-name">{{ item.productName }}</a>
                      } @else {
                        <span class="item-name">{{ item.productName }}</span>
                      }
                      @if (item.variantName) {
                        <span class="item-variant">{{ item.variantName }}</span>
                      }
                      <span class="item-sku">SKU: {{ item.sku }}</span>
                    </div>
                    <div class="item-pricing">
                      <span class="item-unit-price">{{ item.unitPrice | currency:order()!.currency }}</span>
                      <span class="item-qty">x {{ item.quantity }}</span>
                    </div>
                    <div class="item-total">{{ item.lineTotal | currency:order()!.currency }}</div>
                  </div>
                }
              </div>
            </div>

            <!-- Order Summary -->
            <div class="section summary-section">
              <h2>{{ 'cart.summary.title' | translate }}</h2>
              <div class="summary-rows">
                <div class="summary-row">
                  <span>{{ 'account.orders.subtotal' | translate }}</span>
                  <span>{{ order()!.subtotal | currency:order()!.currency }}</span>
                </div>
                <div class="summary-row">
                  <span>{{ 'account.orders.shipping' | translate }}</span>
                  <span>{{ order()!.shippingCost | currency:order()!.currency }}</span>
                </div>
                <div class="summary-row">
                  <span>{{ 'account.orders.tax' | translate }}</span>
                  <span>{{ order()!.taxAmount | currency:order()!.currency }}</span>
                </div>
                @if (order()!.discountAmount > 0) {
                  <div class="summary-row discount">
                    <span>{{ 'account.orders.discount' | translate }}</span>
                    <span>-{{ order()!.discountAmount | currency:order()!.currency }}</span>
                  </div>
                }
                <div class="summary-row total">
                  <span>{{ 'account.orders.total' | translate }}</span>
                  <span>{{ order()!.total | currency:order()!.currency }}</span>
                </div>
              </div>
            </div>

            <!-- Shipping Address -->
            <div class="section address-section">
              <h2>{{ 'account.orders.shippingAddress' | translate }}</h2>
              <div class="address-card">
                <p class="address-name">{{ order()!.shippingAddress.firstName }} {{ order()!.shippingAddress.lastName }}</p>
                <p>{{ order()!.shippingAddress.addressLine1 }}</p>
                @if (order()!.shippingAddress.addressLine2) {
                  <p>{{ order()!.shippingAddress.addressLine2 }}</p>
                }
                <p>{{ order()!.shippingAddress.city }}@if (order()!.shippingAddress.state) {, {{ order()!.shippingAddress.state }}} {{ order()!.shippingAddress.postalCode }}</p>
                <p>{{ order()!.shippingAddress.country }}</p>
                @if (order()!.shippingAddress.phone) {
                  <p class="address-phone">{{ order()!.shippingAddress.phone }}</p>
                }
              </div>
              @if (order()!.trackingNumber) {
                <div class="tracking-info">
                  <span class="tracking-label">{{ 'account.orders.track' | translate }}:</span>
                  <span class="tracking-number">{{ order()!.trackingNumber }}</span>
                </div>
              }
            </div>

            <!-- Payment Info -->
            <div class="section payment-section">
              <h2>{{ 'account.orders.paymentMethod' | translate }}</h2>
              <div class="payment-card">
                @if (order()!.paymentMethod) {
                  <p class="payment-method">
                    @switch (order()!.paymentMethod) {
                      @case ('card') { <span class="payment-icon">&#128179;</span> {{ 'checkout.payment.card' | translate }} }
                      @case ('paypal') { <span class="payment-icon">&#127359;</span> {{ 'checkout.payment.paypal' | translate }} }
                      @case ('bank') { <span class="payment-icon">&#127974;</span> {{ 'checkout.payment.bank' | translate }} }
                      @default { {{ order()!.paymentMethod }} }
                    }
                  </p>
                }
                @if (order()!.paidAt) {
                  <p class="paid-date">{{ 'account.orders.paymentStatuses.paid' | translate }}: {{ order()!.paidAt | date:'medium' }}</p>
                }
              </div>
            </div>

            <!-- Customer Info -->
            <div class="section customer-section">
              <h2>{{ 'contact.info.title' | translate }}</h2>
              <div class="customer-card">
                <p class="customer-email">{{ order()!.customerEmail }}</p>
                @if (order()!.customerPhone) {
                  <p class="customer-phone">{{ order()!.customerPhone }}</p>
                }
              </div>
            </div>

            <!-- Order Notes -->
            @if (order()!.notes) {
              <div class="section notes-section">
                <h2>Notes</h2>
                <p class="notes-content">{{ order()!.notes }}</p>
              </div>
            }
          </div>
        </div>

        <!-- Cancel Order Modal -->
        @if (showCancelModal) {
          <div class="modal-overlay" (click)="showCancelModal = false">
            <div class="modal-content" (click)="$event.stopPropagation()" data-testid="cancel-order-modal">
              <h3>{{ 'account.orders.cancelModal.title' | translate }}</h3>
              <p>{{ 'account.orders.cancelModal.message' | translate }}</p>
              <div class="form-group">
                <label for="cancelReason">{{ 'account.orders.cancelModal.reasonLabel' | translate }}</label>
                <textarea
                  id="cancelReason"
                  [(ngModel)]="cancelReason"
                  rows="3"
                  data-testid="cancel-reason-input"
                ></textarea>
              </div>
              <div class="modal-actions">
                <button class="btn-secondary" (click)="showCancelModal = false">
                  {{ 'account.orders.cancelModal.cancel' | translate }}
                </button>
                <button
                  class="btn-danger"
                  (click)="cancelOrder()"
                  [disabled]="isCancelling()"
                  data-testid="confirm-cancel-btn"
                >
                  @if (isCancelling()) {
                    {{ 'common.loading' | translate }}
                  } @else {
                    {{ 'account.orders.cancelModal.confirm' | translate }}
                  }
                </button>
              </div>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .order-details-container {
      padding: 2rem;
      max-width: 1100px;
      margin: 0 auto;
    }

    .back-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 500;
      transition: color 0.2s ease;

      &:hover {
        color: var(--color-primary-dark);
      }

      .back-arrow {
        font-size: 1.25rem;
      }
    }

    .loading-container, .error-container {
      text-align: center;
      padding: 4rem 2rem;
      background: var(--color-bg-primary);
      border-radius: 12px;
      border: 1px solid var(--color-border);
    }

    .loading-spinner {
      width: 48px;
      height: 48px;
      border: 3px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 1rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .error-message {
      color: var(--color-error);
      margin-bottom: 1.5rem;
    }

    .btn-primary {
      display: inline-block;
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: white;
      text-decoration: none;
      border-radius: 8px;
      font-weight: 600;
      border: none;
      cursor: pointer;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 2rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);
      flex-wrap: wrap;
      gap: 1rem;

      h1 {
        font-size: 1.75rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      .order-number {
        color: var(--color-text-secondary);
        font-size: 1rem;
        margin-bottom: 0.25rem;
      }

      .order-date {
        color: var(--color-text-tertiary);
        font-size: 0.875rem;
      }
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .order-status {
      padding: 0.5rem 1rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .btn-cancel {
      padding: 0.5rem 1rem;
      background: transparent;
      color: var(--color-error);
      border: 1px solid var(--color-error);
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;

      &:hover {
        background: var(--color-error);
        color: white;
      }
    }

    .order-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 1.5rem;
    }

    .section {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;

      h2 {
        font-size: 1rem;
        color: var(--color-text-secondary);
        margin-bottom: 1rem;
        font-weight: 600;
      }
    }

    /* Timeline Section */
    .timeline-section {
      grid-column: 1 / -1;
    }

    .timeline {
      position: relative;
      padding-left: 2rem;
    }

    .timeline-item {
      position: relative;
      padding-bottom: 1.5rem;
      padding-left: 1rem;

      &:last-child {
        padding-bottom: 0;
      }

      &::before {
        content: '';
        position: absolute;
        left: -1.5rem;
        top: 0.5rem;
        bottom: -0.5rem;
        width: 2px;
        background: var(--color-border);
      }

      &:last-child::before {
        display: none;
      }
    }

    .timeline-marker {
      position: absolute;
      left: -2rem;
      top: 0.25rem;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--color-border);
      border: 2px solid var(--color-bg-primary);
    }

    .timeline-item.active .timeline-marker {
      background: var(--color-primary);
    }

    .timeline-content {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .timeline-status {
      font-weight: 600;
      font-size: 0.9375rem;
    }

    .timeline-description {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
    }

    .timeline-date {
      color: var(--color-text-tertiary);
      font-size: 0.75rem;
    }

    /* Items Section */
    .items-section {
      grid-column: 1 / -1;
    }

    .items-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .order-item {
      display: grid;
      grid-template-columns: 80px 1fr auto auto;
      gap: 1rem;
      align-items: center;
      padding: 1rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
    }

    .item-image, .item-placeholder {
      width: 80px;
      height: 80px;
      border-radius: 8px;
      object-fit: cover;
    }

    .item-placeholder {
      background: var(--color-border);
    }

    .item-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .item-name {
      font-weight: 500;
      color: var(--color-text-primary);
      text-decoration: none;

      &:hover {
        color: var(--color-primary);
      }
    }

    .item-variant, .item-sku {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .item-pricing {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 0.25rem;
    }

    .item-unit-price {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .item-qty {
      font-size: 0.875rem;
      color: var(--color-text-tertiary);
    }

    .item-total {
      font-weight: 600;
      font-size: 1rem;
      color: var(--color-text-primary);
      min-width: 80px;
      text-align: right;
    }

    /* Summary Section */
    .summary-rows {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      color: var(--color-text-secondary);

      &.discount {
        color: var(--color-success);
      }

      &.total {
        padding-top: 0.75rem;
        margin-top: 0.5rem;
        border-top: 1px solid var(--color-border);
        font-weight: 700;
        font-size: 1.125rem;
        color: var(--color-text-primary);
      }
    }

    /* Address Section */
    .address-card {
      p {
        margin: 0.25rem 0;
        color: var(--color-text-primary);
      }

      .address-name {
        font-weight: 600;
        margin-bottom: 0.5rem;
      }

      .address-phone {
        color: var(--color-text-secondary);
        margin-top: 0.5rem;
      }
    }

    .tracking-info {
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border);
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .tracking-label {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
    }

    .tracking-number {
      font-weight: 600;
      color: var(--color-primary);
    }

    /* Payment Section */
    .payment-card {
      p {
        margin: 0.25rem 0;
        color: var(--color-text-primary);
      }

      .payment-method {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 500;
      }

      .payment-icon {
        font-size: 1.25rem;
      }

      .paid-date {
        color: var(--color-success);
        font-size: 0.875rem;
        margin-top: 0.5rem;
      }
    }

    /* Customer Section */
    .customer-card {
      p {
        margin: 0.25rem 0;
        color: var(--color-text-primary);
      }

      .customer-email {
        font-weight: 500;
      }

      .customer-phone {
        color: var(--color-text-secondary);
      }
    }

    /* Notes Section */
    .notes-section {
      grid-column: 1 / -1;
    }

    .notes-content {
      color: var(--color-text-primary);
      white-space: pre-wrap;
    }

    /* Modal */
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }

    .modal-content {
      background: var(--color-bg-primary);
      border-radius: 12px;
      padding: 2rem;
      max-width: 500px;
      width: 100%;

      h3 {
        font-size: 1.25rem;
        color: var(--color-text-primary);
        margin-bottom: 0.75rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }
    }

    .form-group {
      margin-bottom: 1.5rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      textarea {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        font-family: inherit;
        font-size: 0.9375rem;
        resize: vertical;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }
      }
    }

    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
    }

    .btn-secondary {
      padding: 0.75rem 1.5rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      font-weight: 500;
      cursor: pointer;

      &:hover {
        background: var(--color-bg-tertiary);
      }
    }

    .btn-danger {
      padding: 0.75rem 1.5rem;
      background: var(--color-error);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 500;
      cursor: pointer;

      &:hover:not(:disabled) {
        background: var(--color-error-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    /* Responsive */
    @media (max-width: 768px) {
      .order-details-container {
        padding: 1rem;
      }

      .order-grid {
        grid-template-columns: 1fr;
      }

      .order-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .header-actions {
        width: 100%;
        justify-content: space-between;
      }

      .order-item {
        grid-template-columns: 60px 1fr;
        gap: 0.75rem;

        .item-pricing, .item-total {
          grid-column: 2;
        }

        .item-pricing {
          flex-direction: row;
          justify-content: space-between;
          align-items: center;
        }

        .item-total {
          text-align: left;
        }
      }

      .modal-content {
        padding: 1.5rem;
      }

      .modal-actions {
        flex-direction: column;

        button {
          width: 100%;
        }
      }
    }
  `]
})
export class OrderDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly checkoutService = inject(CheckoutService);

  order = signal<Order | null>(null);
  isLoading = signal(true);
  isCancelling = signal(false);
  error = signal<string | null>(null);

  showCancelModal = false;
  cancelReason = '';

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('id');
    if (orderId) {
      this.loadOrder(orderId);
    } else {
      this.error.set('Order not found');
      this.isLoading.set(false);
    }
  }

  private loadOrder(orderId: string): void {
    this.isLoading.set(true);
    this.checkoutService.getOrder(orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load order');
        this.isLoading.set(false);
      }
    });
  }

  canCancel(): boolean {
    const status = this.order()?.status;
    return status === 'Pending' || status === 'Paid';
  }

  cancelOrder(): void {
    const orderId = this.order()?.id;
    if (!orderId) return;

    this.isCancelling.set(true);
    this.checkoutService.cancelOrder(orderId, this.cancelReason).subscribe({
      next: (updatedOrder) => {
        this.order.set(updatedOrder);
        this.showCancelModal = false;
        this.cancelReason = '';
        this.isCancelling.set(false);
      },
      error: () => {
        this.isCancelling.set(false);
      }
    });
  }

  getStatusConfig(status: string): { bgColor: string; color: string } {
    const config = ORDER_STATUS_CONFIG[status as OrderStatus];
    return config || { bgColor: '#e5e7eb', color: '#374151' };
  }
}
