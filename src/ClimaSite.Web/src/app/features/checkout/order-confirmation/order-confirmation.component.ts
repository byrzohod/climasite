import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { CheckoutService } from '../../../core/services/checkout.service';
import { ConfettiService } from '../../../core/services/confetti.service';
import { Order, OrderStatus, ORDER_STATUS_CONFIG, Address } from '../../../core/models/order.model';
import { IconComponent } from '../../../shared/components/icon';

/**
 * Order Confirmation Component
 * 
 * A dedicated page for displaying order confirmation after successful checkout.
 * Features:
 * - Order number with copy button
 * - Order summary (items, totals)
 * - Shipping address
 * - Estimated delivery date
 * - "Continue Shopping" and "Track Order" CTAs
 * - Confetti animation on initial load
 * 
 * Route: /checkout/confirmation/:orderId
 */
@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, IconComponent],
  template: `
    <div class="confirmation-container" data-testid="order-confirmation-page">
      @if (loading()) {
        <div class="loading-state" data-testid="loading-state">
          <div class="loading-spinner"></div>
          <p>{{ 'common.loading' | translate }}</p>
        </div>
      } @else if (error()) {
        <div class="error-state" data-testid="error-state">
          <div class="error-icon">
            <app-icon name="alert-circle" size="xl" />
          </div>
          <h2>{{ 'errors.serverError' | translate }}</h2>
          <p>{{ error() }}</p>
          <div class="error-actions">
            <button class="btn-primary" (click)="retryLoad()" data-testid="retry-btn">
              {{ 'common.retry' | translate }}
            </button>
            <a routerLink="/products" class="btn-secondary">
              {{ 'checkout.orderComplete.continueShopping' | translate }}
            </a>
          </div>
        </div>
      } @else if (order()) {
        <!-- Success Header -->
        <div class="confirmation-header" data-testid="confirmation-header">
          <div class="success-icon">
            <app-icon name="check" size="xl" />
          </div>
          <h1>{{ 'checkout.orderConfirmation.title' | translate }}</h1>
          <p class="confirmation-message">
            {{ 'checkout.orderConfirmation.thankYou' | translate:{ name: customerName() } }}
          </p>
          <p class="email-notice">
            {{ 'checkout.orderConfirmation.emailSent' | translate:{ email: order()!.customerEmail } }}
          </p>
        </div>

        <!-- Order Details Card -->
        <div class="order-card" data-testid="order-card">
          <div class="order-header">
            <div class="order-number-section">
              <span class="label">{{ 'checkout.orderComplete.orderNumber' | translate }}</span>
              <div class="order-number-row">
                <span class="order-number" data-testid="order-number">{{ order()!.orderNumber }}</span>
                <button 
                  class="copy-btn" 
                  (click)="copyOrderNumber()"
                  [attr.aria-label]="'common.copyToClipboard' | translate"
                  data-testid="copy-order-btn"
                >
                  <app-icon [name]="copied() ? 'check' : 'copy'" size="sm" />
                </button>
              </div>
            </div>
            <div class="order-date-section">
              <span class="label">{{ 'checkout.orderConfirmation.orderDate' | translate }}</span>
              <span class="value">{{ order()!.createdAt | date:'mediumDate' }}</span>
            </div>
          </div>

          <div class="order-sections">
            <!-- Shipping & Payment Info -->
            <div class="info-grid">
              <div class="info-block" data-testid="shipping-info">
                <div class="info-header">
                  <app-icon name="package" size="md" />
                  <h3>{{ 'checkout.orderConfirmation.shippingTo' | translate }}</h3>
                </div>
                <div class="info-content">
                  <p class="name">{{ order()!.shippingAddress.firstName }} {{ order()!.shippingAddress.lastName }}</p>
                  <p>{{ order()!.shippingAddress.addressLine1 }}</p>
                  @if (order()!.shippingAddress.addressLine2) {
                    <p>{{ order()!.shippingAddress.addressLine2 }}</p>
                  }
                  <p>{{ order()!.shippingAddress.city }}, {{ order()!.shippingAddress.state }} {{ order()!.shippingAddress.postalCode }}</p>
                  <p>{{ order()!.shippingAddress.country }}</p>
                </div>
              </div>

              <div class="info-block" data-testid="payment-info">
                <div class="info-header">
                  <app-icon name="credit-card" size="md" />
                  <h3>{{ 'checkout.orderConfirmation.paymentMethod' | translate }}</h3>
                </div>
                <div class="info-content">
                  <p class="payment-method">
                    @switch (order()!.paymentMethod) {
                      @case ('card') { {{ 'checkout.payment.card' | translate }} }
                      @case ('paypal') { {{ 'checkout.payment.paypal' | translate }} }
                      @case ('bank') { {{ 'checkout.payment.bank' | translate }} }
                      @default { {{ order()!.paymentMethod }} }
                    }
                  </p>
                  <p class="total-charged">
                    {{ 'checkout.orderConfirmation.totalCharged' | translate }}: 
                    <strong>{{ order()!.total | currency:order()!.currency }}</strong>
                  </p>
                </div>
              </div>
            </div>

            <!-- Estimated Delivery -->
            <div class="delivery-section" data-testid="delivery-info">
              <div class="info-header">
                <app-icon name="truck" size="md" />
                <h3>{{ 'checkout.orderConfirmation.estimatedDelivery' | translate }}</h3>
              </div>
              <div class="delivery-content">
                <p class="shipping-method">
                  @switch (order()!.shippingMethod) {
                    @case ('standard') { {{ 'checkout.shipping.standard' | translate }} }
                    @case ('express') { {{ 'checkout.shipping.express' | translate }} }
                    @case ('overnight') { {{ 'checkout.shipping.overnight' | translate }} }
                    @default { {{ order()!.shippingMethod }} }
                  }
                </p>
                <p class="delivery-date">{{ estimatedDelivery() }}</p>
              </div>
            </div>

            <!-- Order Items -->
            <div class="items-section" data-testid="order-items">
              <h3>
                {{ 'checkout.orderConfirmation.itemsOrdered' | translate }} 
                ({{ order()!.items.length }})
              </h3>
              <div class="items-list">
                @for (item of order()!.items; track item.id) {
                  <div class="order-item">
                    <div class="item-image">
                      @if (item.imageUrl) {
                        <img [src]="item.imageUrl" [alt]="item.productName" loading="lazy" />
                      } @else {
                        <div class="no-image">
                          <app-icon name="image" size="lg" />
                        </div>
                      }
                    </div>
                    <div class="item-details">
                      <p class="item-name">{{ item.productName }}</p>
                      @if (item.variantName) {
                        <p class="item-variant">{{ item.variantName }}</p>
                      }
                      <p class="item-sku">{{ 'products.details.sku' | translate }}: {{ item.sku }}</p>
                    </div>
                    <div class="item-qty">
                      <span class="qty-label">{{ 'products.details.quantity' | translate }}:</span>
                      {{ item.quantity }}
                    </div>
                    <div class="item-price">{{ item.lineTotal | currency:order()!.currency }}</div>
                  </div>
                }
              </div>
            </div>

            <!-- Order Totals -->
            <div class="totals-section" data-testid="order-totals">
              <div class="totals-row">
                <span>{{ 'cart.summary.subtotal' | translate }}</span>
                <span>{{ order()!.subtotal | currency:order()!.currency }}</span>
              </div>
              <div class="totals-row">
                <span>{{ 'cart.summary.shipping' | translate }}</span>
                <span>
                  @if (order()!.shippingCost === 0) {
                    {{ 'cart.summary.freeShipping' | translate }}
                  } @else {
                    {{ order()!.shippingCost | currency:order()!.currency }}
                  }
                </span>
              </div>
              <div class="totals-row">
                <span>{{ 'cart.summary.tax' | translate }}</span>
                <span>{{ order()!.taxAmount | currency:order()!.currency }}</span>
              </div>
              @if (order()!.discountAmount > 0) {
                <div class="totals-row discount">
                  <span>{{ 'account.orders.discount' | translate }}</span>
                  <span>-{{ order()!.discountAmount | currency:order()!.currency }}</span>
                </div>
              }
              <div class="totals-row total">
                <span>{{ 'cart.summary.total' | translate }}</span>
                <span data-testid="order-total">{{ order()!.total | currency:order()!.currency }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- What's Next Section -->
        <div class="whats-next-card" data-testid="whats-next">
          <h3>{{ 'checkout.orderConfirmation.whatsNext.title' | translate }}</h3>
          <ol class="next-steps">
            <li>
              <app-icon name="mail" size="md" />
              <span>{{ 'checkout.orderConfirmation.whatsNext.step1' | translate }}</span>
            </li>
            <li>
              <app-icon name="package" size="md" />
              <span>{{ 'checkout.orderConfirmation.whatsNext.step2' | translate }}</span>
            </li>
            <li>
              <app-icon name="truck" size="md" />
              <span>{{ 'checkout.orderConfirmation.whatsNext.step3' | translate:{ date: estimatedDelivery() } }}</span>
            </li>
          </ol>
        </div>

        <!-- Action Buttons -->
        <div class="action-buttons" data-testid="action-buttons">
          @if (order()!.trackingNumber) {
            <a 
              [routerLink]="['/account/orders', order()!.id]" 
              class="btn-primary"
              data-testid="track-order-btn"
            >
              <app-icon name="map-pin" size="sm" />
              {{ 'checkout.orderConfirmation.trackOrder' | translate }}
            </a>
          } @else {
            <a 
              [routerLink]="['/account/orders', order()!.id]" 
              class="btn-primary"
              data-testid="view-order-btn"
            >
              <app-icon name="file-text" size="sm" />
              {{ 'checkout.orderComplete.viewOrder' | translate }}
            </a>
          }
          <a routerLink="/products" class="btn-secondary" data-testid="continue-shopping-btn">
            <app-icon name="shopping-bag" size="sm" />
            {{ 'checkout.orderComplete.continueShopping' | translate }}
          </a>
        </div>

        <!-- Help Section -->
        <div class="help-section" data-testid="help-section">
          <app-icon name="help-circle" size="md" />
          <div class="help-content">
            <h4>{{ 'checkout.orderConfirmation.needHelp' | translate }}</h4>
            <p>
              {{ 'checkout.orderConfirmation.contactInfo' | translate }}
              <a href="tel:+35988812345">{{ 'footer.phone' | translate }}</a>
              {{ 'checkout.orderConfirmation.or' | translate }}
              <a href="mailto:support@climasite.bg">support&#64;climasite.bg</a>
            </p>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .confirmation-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 2rem;
    }

    /* Loading State */
    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 400px;
      gap: 1rem;

      .loading-spinner {
        width: 48px;
        height: 48px;
        border: 4px solid var(--color-border);
        border-top-color: var(--color-primary);
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }

      p {
        color: var(--color-text-secondary);
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    /* Error State */
    .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 400px;
      text-align: center;
      gap: 1rem;

      .error-icon {
        color: var(--color-error);
      }

      h2 {
        color: var(--color-text-primary);
        margin: 0;
      }

      p {
        color: var(--color-text-secondary);
        max-width: 400px;
      }

      .error-actions {
        display: flex;
        gap: 1rem;
        margin-top: 1rem;
      }
    }

    /* Confirmation Header */
    .confirmation-header {
      text-align: center;
      margin-bottom: 2rem;

      .success-icon {
        width: 80px;
        height: 80px;
        margin: 0 auto 1.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-success);
        color: var(--color-text-inverse);
        border-radius: 50%;
        animation: scaleIn 0.4s ease-out;
      }

      h1 {
        font-size: 1.75rem;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .confirmation-message {
        font-size: 1.125rem;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .email-notice {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin: 0;
      }
    }

    @keyframes scaleIn {
      0% {
        transform: scale(0);
        opacity: 0;
      }
      50% {
        transform: scale(1.1);
      }
      100% {
        transform: scale(1);
        opacity: 1;
      }
    }

    /* Order Card */
    .order-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;
      margin-bottom: 1.5rem;
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-bottom: 1px solid var(--color-border);
      flex-wrap: wrap;
      gap: 1rem;

      .label {
        display: block;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-text-secondary);
        margin-bottom: 0.25rem;
      }

      .order-number-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .order-number {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-primary);
        font-family: monospace;
      }

      .copy-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 28px;
        height: 28px;
        background: var(--color-bg-primary);
        border: 1px solid var(--color-border);
        border-radius: 6px;
        cursor: pointer;
        color: var(--color-text-secondary);
        transition: all 0.2s;

        &:hover {
          border-color: var(--color-primary);
          color: var(--color-primary);
        }
      }

      .value {
        color: var(--color-text-primary);
        font-weight: 500;
      }
    }

    .order-sections {
      padding: 1.5rem;
    }

    /* Info Grid */
    .info-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: 1.5rem;
    }

    .info-block {
      .info-header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.75rem;
        color: var(--color-text-secondary);

        h3 {
          font-size: 0.875rem;
          font-weight: 600;
          margin: 0;
          text-transform: uppercase;
          letter-spacing: 0.025em;
        }
      }

      .info-content {
        p {
          margin: 0.25rem 0;
          color: var(--color-text-primary);
          font-size: 0.9375rem;

          &.name {
            font-weight: 600;
          }

          &.payment-method {
            font-weight: 500;
          }

          &.total-charged {
            margin-top: 0.5rem;
            color: var(--color-text-secondary);
            font-size: 0.875rem;
          }
        }
      }
    }

    /* Delivery Section */
    .delivery-section {
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: 1.5rem;

      .info-header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.75rem;
        color: var(--color-text-secondary);

        h3 {
          font-size: 0.875rem;
          font-weight: 600;
          margin: 0;
          text-transform: uppercase;
          letter-spacing: 0.025em;
        }
      }

      .delivery-content {
        .shipping-method {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
          margin: 0 0 0.25rem;
        }

        .delivery-date {
          color: var(--color-text-primary);
          font-size: 1.125rem;
          font-weight: 600;
          margin: 0;
        }
      }
    }

    /* Items Section */
    .items-section {
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: 1.5rem;

      h3 {
        font-size: 0.875rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.025em;
        color: var(--color-text-secondary);
        margin: 0 0 1rem;
      }
    }

    .items-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .order-item {
      display: grid;
      grid-template-columns: 60px 1fr auto auto;
      gap: 1rem;
      align-items: center;
      padding: 0.75rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;

      .item-image {
        width: 60px;
        height: 60px;
        border-radius: 6px;
        overflow: hidden;
        background: var(--color-bg-primary);

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }

        .no-image {
          width: 100%;
          height: 100%;
          display: flex;
          align-items: center;
          justify-content: center;
          color: var(--color-text-tertiary);
        }
      }

      .item-details {
        .item-name {
          font-weight: 600;
          color: var(--color-text-primary);
          margin: 0 0 0.25rem;
        }

        .item-variant {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
          margin: 0 0 0.25rem;
        }

        .item-sku {
          color: var(--color-text-tertiary);
          font-size: 0.75rem;
          margin: 0;
        }
      }

      .item-qty {
        color: var(--color-text-secondary);
        font-size: 0.875rem;

        .qty-label {
          display: none;
        }
      }

      .item-price {
        font-weight: 600;
        color: var(--color-text-primary);
      }
    }

    /* Totals Section */
    .totals-section {
      .totals-row {
        display: flex;
        justify-content: space-between;
        padding: 0.5rem 0;
        color: var(--color-text-secondary);

        &.discount {
          color: var(--color-success);
        }

        &.total {
          padding-top: 1rem;
          margin-top: 0.5rem;
          border-top: 2px solid var(--color-border);
          font-size: 1.125rem;
          font-weight: 700;
          color: var(--color-text-primary);
        }
      }
    }

    /* What's Next Card */
    .whats-next-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 1.5rem;

      h3 {
        font-size: 1rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 1rem;
      }

      .next-steps {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 1rem;

        li {
          display: flex;
          align-items: flex-start;
          gap: 0.75rem;
          color: var(--color-text-primary);

          app-icon {
            flex-shrink: 0;
            color: var(--color-primary);
            margin-top: 0.125rem;
          }
        }
      }
    }

    /* Action Buttons */
    .action-buttons {
      display: flex;
      gap: 1rem;
      justify-content: center;
      margin-bottom: 2rem;
    }

    .btn-primary,
    .btn-secondary {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.875rem 1.5rem;
      border-radius: 8px;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
    }

    .btn-primary {
      background: var(--color-primary);
      color: white;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    .btn-secondary {
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);

      &:hover {
        background: var(--color-border);
      }
    }

    /* Help Section */
    .help-section {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 12px;

      app-icon {
        flex-shrink: 0;
        color: var(--color-text-secondary);
      }

      .help-content {
        h4 {
          font-size: 0.875rem;
          font-weight: 600;
          color: var(--color-text-primary);
          margin: 0 0 0.5rem;
        }

        p {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
          margin: 0;

          a {
            color: var(--color-primary);
            text-decoration: none;

            &:hover {
              text-decoration: underline;
            }
          }
        }
      }
    }

    /* Responsive Styles */
    @media (max-width: 768px) {
      .confirmation-container {
        padding: 1rem;
      }

      .order-header {
        flex-direction: column;
      }

      .info-grid {
        grid-template-columns: 1fr;
      }

      .order-item {
        grid-template-columns: 50px 1fr;
        grid-template-rows: auto auto;

        .item-image {
          width: 50px;
          height: 50px;
        }

        .item-qty {
          grid-column: 2;
          justify-self: start;

          .qty-label {
            display: inline;
          }
        }

        .item-price {
          grid-column: 2;
          justify-self: start;
        }
      }

      .action-buttons {
        flex-direction: column;

        .btn-primary,
        .btn-secondary {
          width: 100%;
          justify-content: center;
        }
      }
    }

    /* Reduced Motion */
    @media (prefers-reduced-motion: reduce) {
      .success-icon,
      .loading-spinner {
        animation: none;
      }

      .success-icon {
        transform: scale(1);
        opacity: 1;
      }

      .loading-spinner {
        border-top-color: var(--color-primary);
      }
    }
  `]
})
export class OrderConfirmationComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly checkoutService = inject(CheckoutService);
  private readonly confettiService = inject(ConfettiService);

  // State signals
  order = signal<Order | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  copied = signal(false);

  // Track if confetti has been shown (to prevent re-showing on reload)
  private confettiShown = false;

  // Computed values
  customerName = computed(() => {
    const o = this.order();
    if (!o) return '';
    return o.shippingAddress.firstName;
  });

  estimatedDelivery = computed(() => {
    const o = this.order();
    if (!o) return '';

    const orderDate = new Date(o.createdAt);
    let minDays = 5;
    let maxDays = 7;

    switch (o.shippingMethod) {
      case 'express':
        minDays = 2;
        maxDays = 3;
        break;
      case 'overnight':
        minDays = 1;
        maxDays = 1;
        break;
      default: // standard
        minDays = 5;
        maxDays = 7;
    }

    const minDate = new Date(orderDate);
    minDate.setDate(minDate.getDate() + minDays);

    const maxDate = new Date(orderDate);
    maxDate.setDate(maxDate.getDate() + maxDays);

    const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    const minStr = minDate.toLocaleDateString('en-US', options);
    const maxStr = maxDate.toLocaleDateString('en-US', options);

    if (minDays === maxDays) {
      return minStr;
    }
    return `${minStr} - ${maxStr}`;
  });

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');
    if (orderId) {
      this.loadOrder(orderId);
    } else {
      this.error.set('Order ID not found');
      this.loading.set(false);
    }
  }

  ngOnDestroy(): void {
    this.confettiService.stop();
  }

  private loadOrder(orderId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.checkoutService.getOrder(orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);

        // Show confetti only on first successful load and if order is recent (within 5 minutes)
        if (!this.confettiShown) {
          const orderDate = new Date(order.createdAt);
          const now = new Date();
          const fiveMinutesAgo = new Date(now.getTime() - 5 * 60 * 1000);
          
          if (orderDate > fiveMinutesAgo) {
            this.confettiService.burst();
          }
          this.confettiShown = true;
        }
      },
      error: (err) => {
        console.error('Failed to load order:', err);
        this.error.set(err.error?.message || 'Failed to load order details');
        this.loading.set(false);
      }
    });
  }

  retryLoad(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');
    if (orderId) {
      this.loadOrder(orderId);
    }
  }

  copyOrderNumber(): void {
    const orderNumber = this.order()?.orderNumber;
    if (orderNumber) {
      navigator.clipboard.writeText(orderNumber).then(() => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 2000);
      });
    }
  }
}
